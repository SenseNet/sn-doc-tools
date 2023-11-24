﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator.Parser
{
    internal class OptionsClassParser
    {
        private SemanticModel _semanticModel;

        public OptionsClassParser(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public OptionsClassInfo Parse(ClassDeclarationSyntax classNode, AttributeSyntax attribute)
        {
            // Only public classes
            if (classNode.Modifiers.All(x => x.Text != "public"))
                return null;

            var result = new OptionsClassInfo
            {
                //ClassName = classNode.Identifier.Text,
                Documentation = classNode.GetLeadingTrivia().ToFullString(),
                ConfigSection = ParseAttributeArguments(attribute.ArgumentList)
            };

            bool hasCtor = false;
            bool hasParameterlessCtor = false;

            foreach (var member in classNode.Members)
            {
                if (member is PropertyDeclarationSyntax propertyNode)
                {
                    if (propertyNode.Modifiers.Any(x => x.Text == "public"))
                    {
                        var hasGetter = false;
                        var hasSetter = false;

                        var accessorNodes = propertyNode.AccessorList?.Accessors;
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

                        var propertySymbol = _semanticModel.GetDeclaredSymbol(propertyNode);
                        var typeFullName = propertySymbol.Type.ToDisplayString();
                        result.Properties.Add(new OptionsPropertyInfo
                        {
                            Name = propertyNode.Identifier.Text,
                            Type = propertyNode.Type.ToString(),
                            HasGetter = hasGetter,
                            HasSetter = hasSetter,
                            Initializer = propertyNode.Initializer?.ToString(),
                            Documentation = propertyNode.GetLeadingTrivia().ToFullString(),

                            TypeFullName = typeFullName,
                            TypeIsEnum = propertySymbol.Type.TypeKind == TypeKind.Enum,
                            TypeIsBackendOnly = IsTypeBackendOnly(typeFullName)
                        });
                    }
                }
                else
                {
                    if (member is ConstructorDeclarationSyntax ctorNode)
                    {
                        hasCtor = true;
                        if (ctorNode.ParameterList.Parameters.Count == 0)
                            hasParameterlessCtor = true;
                    }
                }
            }

            if (hasCtor && !hasParameterlessCtor)
                return null;

            return result;
        }

        public string ParseAttributeArguments(AttributeArgumentListSyntax node)
        {
            foreach (var attrArg in node.Arguments)
            {
                var visitor = new AttributeArgumentWalker(false);
                visitor.Visit(attrArg);
                return visitor.Value.Trim('"');
            }
            return null;
        }

        public static bool IsTypeBackendOnly(string typeFullName)
        {
            return typeFullName.StartsWith("System.Func<");
        }
    }
}
