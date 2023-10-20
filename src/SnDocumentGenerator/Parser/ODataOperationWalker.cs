using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator.Parser
{
    /// <summary>
    /// Visits ODataOperation method (method that annotated with ODataAction or ODataFunction attribute)
    /// </summary>
    internal class ODataOperationWalker : WalkerBase
    {
        private readonly string[] _expectedModifiers = new[] { "public", "static" };

        private string _currentAttributeName;
        public OperationInfo Operation { get; } = new OperationInfo();

        public ODataOperationWalker(bool showAst) : base(showAst)
        {
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Identifier
            Operation.MethodName = node.Identifier.Text;

            // Return type
            Operation.ReturnValue.Type = node.ReturnType.ToString();

            // Modifiers
            if (_expectedModifiers.Length != node.Modifiers.Select(x => x.Text).Intersect(_expectedModifiers).Count())
                Operation.IsValid = false;

            // Documentation
            var trivias = node.GetLeadingTrivia();
            var xmlCommentTrivia =
                trivias.FirstOrDefault(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);
            Operation.Documentation = xmlCommentTrivia.ToFullString();

            base.VisitMethodDeclaration(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            var name = node.Name.ToString();
            if (name.EndsWith("Attribute"))
                name = name.Substring(0, name.Length - "Attribute".Length);

            if (name == "ODataAction")
                Operation.IsAction = true;

            _currentAttributeName = name;
            base.VisitAttribute(node);
            _currentAttributeName = null;
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var parent = node.Parent;
            if (parent is LambdaExpressionSyntax)
                return;

            if (node.Type == null)
                return;

            if (parent.Parent is LocalFunctionStatementSyntax)
                return;

            var type = node.Type.GetText().ToString().Trim();
            var name = node.Identifier.Text;
            if (Operation.Parameters.Count == 0 && !type.EndsWith("Content"))
                Operation.IsValid = false;
            Operation.Parameters.Add(new OperationParameterInfo
            {
                Name = name,
                Type = type,
                IsOptional = node.Default != null
            });
            base.VisitParameter(node);
        }

        public override void VisitAttributeArgumentList(AttributeArgumentListSyntax node)
        {
            var index = 0;
            foreach (var attrArg in node.Arguments)
            {
                var visitor = new AttributeArgumentWalker(this.ShowAst);
                visitor.Visit(attrArg);
                AddParameter(visitor.Name, visitor.Value, index++);
            }
        }

        private void AddParameter(string name, string value, int index)
        {
            switch (_currentAttributeName)
            {
                case "ContentTypes":
                    Operation.ContentTypes.Add(value);
                    break;
                case "AllowedRoles":
                    Operation.AllowedRoles.Add(value);
                    break;
                case "RequiredPermissions":
                    Operation.RequiredPermissions.Add(value);
                    break;
                case "RequiredPolicies":
                    Operation.RequiredPolicies.Add(value);
                    break;
                case "Scenario":
                    Operation.Scenarios.Add(value);
                    break;
                case "ODataAction":
                case "ODataFunction":
                    switch (name)
                    {
                        case null:
                            if (index == 0)
                                Operation.OperationName = value;
                            break;
                        case "OperationName":
                            Operation.OperationName = value;
                            break;
                        case "Description":
                            Operation.Description = value;
                            break;
                        case "Icon":
                            Operation.Icon = value;
                            break;
                    }

                    break;
            }
        }
    }
}
