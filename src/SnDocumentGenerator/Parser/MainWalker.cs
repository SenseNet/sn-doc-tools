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
        public List<ServiceRegistrationMethodInfo> ServiceRegistrationMethods { get; } = new ();

        protected readonly string _path;
        protected readonly SemanticModel _semanticModel;

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


        /* ============================================================================ Service registrations */

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Parameters
            if (IsServiceRegistrationMethod(node))
            {
                Console.WriteLine($"SRVREG: {node.Identifier.Text,-70}");

                var visitor = new ServiceRegistrationMethodVisitor(_path, ShowAst, _semanticModel);
                visitor.Visit(node);

                if(visitor.ServiceRegistrationMethod != null)
                    ServiceRegistrationMethods.Add(visitor.ServiceRegistrationMethod);
            }

            base.VisitMethodDeclaration(node);
        }
        private bool IsServiceRegistrationMethod(MethodDeclarationSyntax node)
        {
            if (node.ReturnType.ToString() != "IServiceCollection")
                return false;
            if (node.ParameterList.Parameters.Count < 1)
                return false;
            var firstParam = node.ParameterList.Parameters[0];
            if (firstParam.Type?.ToString() != "IServiceCollection")
                return false;
            if (firstParam.Modifiers.All(x => x.ToString() != "this"))
                return false;
            return true;
        }

    }
}
