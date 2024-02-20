using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SnDocumentGenerator.Parser
{
    /// <summary>
    /// Searches ODataAction or ODataFunction attributes and visits their methods in a csharp file.
    /// </summary>
    internal class MainWalker : WalkerBase
    {
        public List<string> UsingDirectives { get; } = new();
        public List<OperationInfo> Operations { get; } = new();
        public List<OptionsClassInfo> OptionsClasses { get; } = new();
        public Dictionary<string, ClassInfo> Classes { get; } = new();
        public Dictionary<string, EnumInfo> Enums { get; } = new();
        public Dictionary<MethodDeclarationSyntax, List<ServiceInfo>> Services { get; } = new();

        private readonly string _path;
        private readonly SemanticModel _semanticModel;

        public MainWalker(string path, bool showAst, SemanticModel semanticModel) : base(showAst)
        {
            _path = path;
            _semanticModel = semanticModel;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            UsingDirectives.Add(node.Name.ToString());
            base.VisitUsingDirective(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            var name = node.Name.ToString();
            if (name == "OptionsClass")
            {
                if (node.Parent?.Parent is ClassDeclarationSyntax classNode)
                {
                    var optionsClass = new OptionsClassParser(_semanticModel).Parse(classNode, node);
                    if (optionsClass != null)
                    {
                        optionsClass.File = _path;

                        GetNamespaceAndClassName(node, out var @namespace, out var className, out var isInterface, out var isStruct);
                        optionsClass.Namespace = @namespace;
                        optionsClass.ClassName = className;
                        optionsClass.UsingDirectives = UsingDirectives.ToList();

                        optionsClass.Normalize();
                        OptionsClasses.Add(optionsClass);
                    }
                }
            }
            else if (name == "ODataFunction" || name == "ODataAction")
            {
                // MethodDeclarationSyntax -> AttributeListSyntax -> AttributeSyntax
                var walker = new ODataOperationWalker(this.ShowAst);
                using (Color(ConsoleColor.Cyan))
                    walker.Visit(node.Parent.Parent);

                var op = walker.Operation;
                op.File = _path;

                GetNamespaceAndClassName(node, out var @namespace, out var className, out var isInterface, out var isStruct);
                op.Namespace = @namespace;
                op.ClassName = className;

                op.Normalize();
                Operations.Add(op);
            }
            else
            {
                base.VisitAttribute(node);
            }
        }

        private void GetNamespaceAndClassName(SyntaxNode node, out string @namespace, out string className,
            out bool isInterface, out bool isStruct)
        {
            TypeDeclarationSyntax classNode;
            EnumDeclarationSyntax enumNode = node is EnumDeclarationSyntax syntax ? syntax : null;
            NamespaceDeclarationSyntax namespaceNode;
            SyntaxNode n = node;
            while ((classNode = n as ClassDeclarationSyntax) == null)
            {
                if (n == null)
                    break;
                n = n.Parent;
            }

            if (classNode == null)
            {
                n = node;
                while ((classNode = n as InterfaceDeclarationSyntax) == null)
                {
                    if (n == null)
                        break;
                    n = n.Parent;
                }
            }
            if (classNode == null)
            {
                n = node;
                while ((classNode = n as StructDeclarationSyntax) == null)
                {
                    if (n == null)
                        break;
                    n = n.Parent;
                }
            }

            if (classNode == null)
            {
                if(!(node is EnumDeclarationSyntax))
                {
                    @namespace = string.Empty;
                    className = null;
                    isInterface = false;
                    isStruct = false;
                    return;
                }

                n = node;
            }

            while ((namespaceNode = n as NamespaceDeclarationSyntax) == null)
            {
                if (n == null)
                    break;
                n = n.Parent;
            }

            @namespace = namespaceNode?.Name.ToString() ?? string.Empty;
            className = classNode?.Identifier.Text ?? enumNode?.Identifier.Text;
            isInterface = classNode is InterfaceDeclarationSyntax;
            isStruct = classNode is StructDeclarationSyntax;
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            GetNamespaceAndClassName(node, out var @namespace, out var classname, out var isInterface, out var isStruct);
            var fullClassName = $"{@namespace}.{classname}";
            if (!Classes.TryGetValue(fullClassName, out var currentClass))
            {
                currentClass = new ClassInfo
                {
                    Namespace = @namespace,
                    ClassName = classname,
                    IsInterface = isInterface,
                    IsStruct = isStruct,
                    File = _path
                };
                Classes.Add(fullClassName, currentClass);
            }

            var hasGetter = false;
            var hasSetter = false;

            var accessorNodes = node.AccessorList?.Accessors;
            if (accessorNodes != null)
            {
                foreach (var accessorNode in accessorNodes)
                {
                    if (accessorNode.Kind() == SyntaxKind.GetAccessorDeclaration)
                        hasGetter = true;
                    if (accessorNode.Kind() == SyntaxKind.SetAccessorDeclaration)
                        hasSetter = true;
                }
            }

            var propertySymbol = _semanticModel.GetDeclaredSymbol(node);
            var typeFullName = propertySymbol.Type.ToDisplayString();
            currentClass.Properties.Add(new OptionsPropertyInfo
            {
                Name = node.Identifier.Text,
                Type = node.Type.ToString(),
                HasGetter = hasGetter,
                HasSetter = hasSetter,
                Initializer = node.Initializer?.ToString(),
                Documentation = node.GetLeadingTrivia().ToFullString(),

                TypeFullName = typeFullName,
                TypeIsEnum = propertySymbol.Type.TypeKind == TypeKind.Enum,
                TypeIsBackendOnly = OptionsClassParser.IsTypeBackendOnly(typeFullName)
            });

            base.VisitPropertyDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            GetNamespaceAndClassName(node, out var @namespace, out var classname, out var isInterface, out var isStruct);

            var fullEnumName = $"{@namespace}.{classname}";
            if (!Enums.TryGetValue(fullEnumName, out var enumInfo))
            {
                enumInfo = new EnumInfo
                {
                    Namespace = @namespace,
                    Name = classname,
                    File = _path,
                    Members = node.Members.Select(x => x.Identifier.Text).ToArray()
                };
                Enums.Add(fullEnumName, enumInfo);
            }

            base.VisitEnumDeclaration(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                var typeName = _semanticModel.GetSymbolInfo(identifier).Symbol?.ToString();
                if (typeName == "IServiceCollection")
                {
                    SyntaxNode n = node;
                    MemberAccessExpressionSyntax currentMemberAccess = node;
                    var serviceRegistrations = new List<ServiceRegistrationInfo>();
                    while ((n = n.Parent) != null)
                    {
                        if ( n is MemberAccessExpressionSyntax memberAccess)
                        {
                            currentMemberAccess = memberAccess;
                        }
                        else if(n is InvocationExpressionSyntax invocation)
                        {
                            var typeParams = currentMemberAccess.Name is GenericNameSyntax genericName
                                ? genericName.TypeArgumentList.Arguments.Select(x => x.ToString()).ToArray()
                                : Array.Empty<string>();
                            serviceRegistrations.Add(new ServiceRegistrationInfo
                            {
                                Name = currentMemberAccess.Name.Identifier.Text,
                                TypeParameters = typeParams,
                                Parameters = invocation.ArgumentList
                            });
                        }
                        else
                        {
                            break;
                        }
                    }
                    MethodDeclarationSyntax methodDeclaration = null;
                    while (true)
                    {
                        methodDeclaration = n as MethodDeclarationSyntax;
                        if (methodDeclaration != null)
                            break;
                        n = n.Parent;
                        if (n == null)
                            break;
                    }
                    if (methodDeclaration != null)
                    {
                        Console.WriteLine($"{methodDeclaration.Identifier.Text,-70}");
                        foreach (var item in serviceRegistrations)
                            Console.WriteLine($"    {item}");
                        if (!Services.TryGetValue(methodDeclaration, out var serviceInfos))
                        {
                            serviceInfos = new List<ServiceInfo>();
                            Services.Add(methodDeclaration, serviceInfos);
                        }
                        GetNamespaceAndClassName(node, out var @namespace, out var classname, out var isInterface, out var isStruct);
                        
                        // Documentation
                        var trivias = methodDeclaration.GetLeadingTrivia();
                        var xmlCommentTrivia =
                            trivias.FirstOrDefault(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);
                        var documentation = xmlCommentTrivia.ToFullString();

                        serviceInfos.Add(new ServiceInfo
                        {
                            Method = methodDeclaration,
                            Registrations = serviceRegistrations.ToArray(),
                            File = _path,
                            Namespace = @namespace,
                            ClassName = classname,
                            Documentation = documentation
                        });
                    }
                }
            }

            base.VisitMemberAccessExpression(node);
        }
    }
}
