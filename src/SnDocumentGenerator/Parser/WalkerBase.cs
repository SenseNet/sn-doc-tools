using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
