using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator.Parser
{
    internal class WalkerBase : CSharpSyntaxWalker
    {
        /* ================================================================================ TOOLS */

        protected void GetNamespaceAndClassName(SyntaxNode node, out string @namespace,
            out string className, out bool isInterface, out bool isStruct)
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
                if (!(node is EnumDeclarationSyntax))
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
    }
}
