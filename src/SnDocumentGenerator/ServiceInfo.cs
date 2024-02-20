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
    public MethodDeclarationSyntax Method { get; }
    public ServiceRegistrationInfo[] Registrations { get; }
    public string Namespace { get; }
    public string ClassName { get; }

    public string File { get; }
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

    public ServiceInfo(MethodDeclarationSyntax method, ServiceRegistrationInfo[] registrations, string file, string @namespace, string className)
    {
        Method = method;
        Registrations = registrations;
        File = file;
        Namespace = @namespace;
        ClassName = className;
    }
}