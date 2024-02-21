using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator.Parser
{
    internal class WalkerBase : CSharpSyntaxWalker
    {
        protected static int Tabs;
        public bool ShowAst { get; }

        static WalkerBase()
        {
            InitializeColors();
        }

        public WalkerBase(bool showAst)
        {
            ShowAst = showAst;
        }

        public override void Visit(SyntaxNode node)
        {
            Tabs++;
            var indents = new String(' ', Tabs * 2);
            if (ShowAst)
                Console.WriteLine(indents + node.Kind());
            base.Visit(node);
            Tabs--;
        }

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

        /* ================================================================================ COLOR SUPPORT */

        protected static IDisposable Color(ConsoleColor foreground, ConsoleColor? background = null)
        {
            return new ColoredBlock(foreground, background ?? _defaultBackgroundColor);
        }

        private class ColoredBlock : IDisposable
        {
            public ColoredBlock(ConsoleColor foreground, ConsoleColor background)
            {
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
            }

            public void Dispose()
            {
                SetDefaultColor();
            }
        }

        private static void SetDefaultColor()
        {
            Console.BackgroundColor = _defaultBackgroundColor;
            Console.ForegroundColor = _defaultForegroundColor;
        }

        private static ConsoleColor _defaultBackgroundColor;
        private static ConsoleColor _defaultForegroundColor;

        private static void InitializeColors()
        {
            _defaultBackgroundColor = Console.BackgroundColor;
            _defaultForegroundColor = Console.ForegroundColor;
        }
    }
}
