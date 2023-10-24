using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator.Parser
{
    /// <summary>
    /// Visits one argument of an attribute
    /// </summary>
    internal class AttributeArgumentWalker : WalkerBase
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public AttributeArgumentWalker(bool showAst) : base(showAst)
        {
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            Value = node.ToString();
            base.VisitLiteralExpression(node);
        }

        public override void VisitNameEquals(NameEqualsSyntax node)
        {
            Name = node.Name.ToString();
            base.VisitNameEquals(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            Value = node.ToString();
            //base.VisitMemberAccessExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var src = node.ToString();
            if (src.StartsWith("nameof(") && src.EndsWith(")"))
            {
                Value = src.Substring(7, src.Length - 8);
            }
            else
            {
                base.VisitInvocationExpression(node);
            }
        }
    }
}
