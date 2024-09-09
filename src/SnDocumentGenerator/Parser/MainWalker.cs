using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public MainWalker(string path, SemanticModel semanticModel) : base()
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
                var walker = new ODataOperationWalker();
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



        private string _currentNamespace;
        private readonly Stack<string> _currentClassNames = new Stack<string>();
        private string _fullTypeName;
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _currentNamespace = node.Name.ToString();
            base.VisitNamespaceDeclaration(node);
        }
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var baseList = node.BaseList?.Types.ToString().Split(' ');
            var modifiers = node.Modifiers;
            var cName = node.Identifier.Text;
            _currentClassNames.Push(cName);
            _fullTypeName = GetFullClassName();
            if (node.TypeParameterList != null)
                _fullTypeName += "`" + node.TypeParameterList.Parameters.Count;

            RegisterType(_fullTypeName, cName, baseList, false, true);

            base.VisitClassDeclaration(node);
            _currentClassNames.Pop();
        }
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var baseList = node.BaseList?.Types.ToString().Split(' ');
            var modifiers = node.Modifiers;
            var interfaceName = node.Identifier.Text;
            _fullTypeName = GetFullClassName() + "." + interfaceName;
            if (node.TypeParameterList != null)
                _fullTypeName += "`" + node.TypeParameterList.Parameters.Count;

            RegisterType(_fullTypeName, interfaceName, baseList, false, true);

            base.VisitInterfaceDeclaration(node);
        }
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var baseList = node.BaseList?.Types.ToString().Split(' ');
            var modifiers = node.Modifiers;
            var structName = node.Identifier.Text;
            _fullTypeName = GetFullClassName() + "." + structName;

            RegisterType(_fullTypeName, structName, baseList, false, true);

            base.VisitStructDeclaration(node);
        }
        private void RegisterType(string fullTypeName, string className, string[] baseList, bool isInterface, bool isStruct)
        {
            var currentClass = new ClassInfo
            {
                Namespace = _currentNamespace,
                ClassName = className,
                FullTypeName = fullTypeName,
                BaseList = baseList ?? [],
                IsInterface = isInterface,
                IsStruct = isStruct,
                File = _path
            };
            Classes.Add(fullTypeName, currentClass);
        }
        private string GetFullClassName()
        {
            var ns = _currentNamespace;
            if (_currentClassNames.Count == 0)
                return ns;
            var names = _currentClassNames.ToList();
            names.Reverse();
            foreach (var name in names)
                ns += "." + name;
            return ns;
        }


        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (_fullTypeName != null)
            {
                if (!Classes.TryGetValue(_fullTypeName, out var currentClass))
                    throw new Exception("Type discover exception. Missing type:" + _fullTypeName);

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
            }

            base.VisitPropertyDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var className = GetFullClassName();
            var enumName = node.Identifier.Text;

            var fullEnumName = $"{className}.{enumName}";
            if (!Enums.TryGetValue(fullEnumName, out var enumInfo))
            {
                enumInfo = new EnumInfo
                {
                    Namespace = className,
                    Name = enumName,
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

                var visitor = new ServiceRegistrationMethodVisitor(_path, _semanticModel);
                visitor.Visit(node);

                if(visitor.ServiceRegistrationMethod != null)
                    ServiceRegistrationMethods.Add(visitor.ServiceRegistrationMethod);
            }

            base.VisitMethodDeclaration(node);
        }
        private bool IsServiceRegistrationMethod(MethodDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.ToString();
            if(/*!modifiers.Contains("public") || */!modifiers.Contains("static"))
                return false;
            if (node.ParameterList.Parameters.Count < 1)
                return false;
            var firstParam = node.ParameterList.Parameters[0];
            var firstParamType = firstParam.Type?.ToString();
            if (!ServiceRegistrationMethodInfo.ExtensionTargets.Contains(firstParamType))
                return false;
            if (firstParam.Modifiers.All(x => x.ToString() != "this"))
                return false;
            return true;
        }


    }
}
