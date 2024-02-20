using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnDocumentGenerator;

[DebuggerDisplay("{ToString()}")]
public class ServiceRegistrationInfo
{
    public string Name { get; set; }
    public string[] TypeParameters { get; set; }
    public ArgumentListSyntax Parameters { get; set; }

    public override string ToString()
    {
        return $"{Name}" +
               $"{(TypeParameters.Length > 0 ? $"<{string.Join(", ", TypeParameters)}>" : string.Empty)}" +
               $"({string.Join(", ", Parameters.Arguments.Select(_ => "..."))})";
    }
}
public class ServiceInfo
{
    public MethodDeclarationSyntax Method { get; set; }
    public ServiceRegistrationInfo[] Registrations { get; set; }
    public string Namespace { get; set; }
    public string ClassName { get; set; }

    public string File { get; set; }
    private string _githubRepository;
    public string GithubRepository
    {
        get
        {
            if (_githubRepository == null)
            {
                _githubRepository = File.Split('\\')
                    .TakeWhile(x => !x.Equals("src", StringComparison.OrdinalIgnoreCase))
                    .Last();
            }
            return _githubRepository;
        }
    }

    public string Documentation { get; set; }
}