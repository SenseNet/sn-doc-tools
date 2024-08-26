using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace SnDocumentGenerator.Parser;

internal class ServiceRegistrationMethodVisitor : WalkerBase
{
    private readonly string _path;
    private readonly SemanticModel _semanticModel;

    private readonly List<(string TypeName, string Variance)> _typeParameters = new();
    private readonly Dictionary<string, List<string>> _constraints = new();
    public List<(string Name, string Type, string DefaultValue)> Parameters { get; } = new();
    public List<ServiceRegistrationCallingInfo> Registrations { get; } = new();

    public ServiceRegistrationMethodInfo ServiceRegistrationMethod { get; private set; }

    public ServiceRegistrationMethodVisitor(string path, bool showAst, SemanticModel semanticModel) : base(showAst)
    {
        _path = path;
        _semanticModel = semanticModel;
    }

    private MethodDeclarationSyntax _methodDeclarationSyntax;
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        _methodDeclarationSyntax = node;
        base.VisitMethodDeclaration(node);

        GetNamespaceAndClassName(node, out var @namespace, out var className, out var isInterface, out var isStruct);

        var xmlCommentTrivia = node.GetLeadingTrivia()
            .FirstOrDefault(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);
        var documentation = xmlCommentTrivia.ToFullString();

        // Combine type parameters and constraints
        var typeParams = _typeParameters
            .Select(x => new TypeParameterInfo {Name = x.TypeName, Variance = x.Variance})
            .ToArray();
        foreach (var constraint in _constraints)
        {
            var typeParam = typeParams.FirstOrDefault(x => x.Name == constraint.Key);
            if (typeParam != null)
                typeParam.Constraints = constraint.Value.ToArray();
        }

        // Create product
        ServiceRegistrationMethod = new ServiceRegistrationMethodInfo
        {
            Namespace = @namespace,
            ClassName = className,
            File = _path,
            Method = _methodDeclarationSyntax,
            TypeParams = typeParams,
            Parameters = ParseParameters(_methodDeclarationSyntax.ParameterList),
            Documentation = documentation,
            Registrations = Registrations.ToArray()
        };
    }

    public override void VisitTypeParameter(TypeParameterSyntax node)
    {
        var typeName = node.Identifier.Text;
        var variance = node.VarianceKeyword.Text;
        _typeParameters.Add((typeName, variance));

        base.VisitTypeParameter(node);
    }

    public override void VisitParameter(ParameterSyntax node)
    {
        var name = node.Identifier.Text;
        var type = node.Type?.ToString();
        var defaultValue = node.Default?.ToString();
        Parameters.Add((name, type, defaultValue));

        base.VisitParameter(node);
    }

    public override void VisitTypeConstraint(TypeConstraintSyntax node)
    {
        var type = node.Type?.ToString();

        string name = null;
        var clause = node.Parent as TypeParameterConstraintClauseSyntax;
        if (clause != null)
            name = clause.Name.Identifier.Text;

        if (name != null && type != null)
        {
            if (!_constraints.TryGetValue(name, out var types))
            {
                types = new List<string>();
                _constraints.Add(name, types);
            }
            types.Add(type);
        }

        base.VisitTypeConstraint(node);
    }

    public override void VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node)
    {
        var type = node.ToString();

        string name = null;
        var clause = node.Parent as TypeParameterConstraintClauseSyntax;
        if (clause != null)
            name = clause.Name.Identifier.Text;

        if (name != null && type != null)
        {
            if (!_constraints.TryGetValue(name, out var types))
            {
                types = new List<string>();
                _constraints.Add(name, types);
            }
            types.Add(type);
        }

        base.VisitClassOrStructConstraint(node);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.Expression is IdentifierNameSyntax identifier)
        {
            var typeName = ModelExtensions.GetSymbolInfo(_semanticModel, identifier).Symbol?.ToString();
            if (typeName == "IServiceCollection")
            {
                SyntaxNode n = node;
                MemberAccessExpressionSyntax currentMemberAccess = node;
                while ((n = n.Parent) != null)
                {
                    if (n is MemberAccessExpressionSyntax memberAccess)
                    {
                        currentMemberAccess = memberAccess;
                    }
                    else if (n is InvocationExpressionSyntax invocation)
                    {
                        var typeParams = currentMemberAccess.Name is GenericNameSyntax genericName
                            ? genericName.TypeArgumentList.Arguments.Select(x => x.ToString()).ToArray()
                            : Array.Empty<string>();
                        Registrations.Add(new ServiceRegistrationCallingInfo
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
            }
        }

        base.VisitMemberAccessExpression(node);
    }

    private List<OperationParameterInfo> ParseParameters(ParameterListSyntax parameterList)
    {
        var result = new List<OperationParameterInfo>();
        foreach (var parameter in parameterList.Parameters)
            if (TryParseParameter(parameter, out var parsed))
                result.Add(parsed);
        return result;
    }
    public bool TryParseParameter(ParameterSyntax node, out OperationParameterInfo parsed)
    {
        parsed = null;
        var parent = node.Parent;
        if (parent is LambdaExpressionSyntax)
            return false;

        if (node.Type == null)
            return false;

        if (parent.Parent is LocalFunctionStatementSyntax)
            return false;

        var type = node.Type.GetText().ToString().Trim();
        var name = node.Identifier.Text;
        parsed = new OperationParameterInfo
        {
            Name = name,
            Type = type,
            IsOptional = node.Default != null
        };
        return true;
    }

}