using System.Collections.Generic;
using System.Linq;

namespace SnDocumentGenerator;

internal class CallHierarchyMapper
{
    private readonly Dictionary<string, ClassInfo> _allTypes;

    public CallHierarchyMapper(Dictionary<string, ClassInfo> allTypes)
    {
        _allTypes = allTypes;
    }

    private readonly string[] _skippedCalls = new[] {"AddSingleton", "AddScoped", "AddTransient"};
    public void Map(List<ServiceRegistrationMethodInfo> serviceRegistrationMethods)
    {
        foreach (var serviceRegistrationMethod in serviceRegistrationMethods)
        {
            foreach (var registration in serviceRegistrationMethod.Registrations)
            {
                if (_skippedCalls.Contains(registration.Name))
                    continue;

                var typeParamMutations = GetTypeParamMutations(registration.TypeParameters);

                var calledMethodCandidates = serviceRegistrationMethods
                    .Where(m => m.Method.Identifier.Text == registration.Name)
                    .Where(m => IsTypeParamsMatched(m, typeParamMutations))
                    .ToArray();

                if (registration.TypeParameters.Length == 1)
                {
                    if (calledMethodCandidates.Length > 1)
                    {
                        //TODO needs to filter by parameters (full signature matching)
                        if (registration.Name == "MapMiddlewareWhen")
                        {
                            // hardcoded: MapMiddlewareWhen<T>(this IApplicationBuilder builder, string pathSegmentStart, ...
                            if (registration.Parameters.Arguments[0].Expression.GetType().Name == "LiteralExpressionSyntax")
                            {
                                var candidate = calledMethodCandidates.FirstOrDefault(x => x.Parameters[1].Type == "string");
                                if(candidate != null)
                                    if (!candidate.CalledBy.Contains(serviceRegistrationMethod))
                                        candidate.CalledBy.Add(serviceRegistrationMethod);
                            }
                            // hardcoded: MapMiddlewareWhen<T>(this IApplicationBuilder builder, Func<HttpContext, bool> predicate, ...
                            if (registration.Parameters.Arguments[0].Expression.GetType().Name == "IdentifierNameSyntax")
                            {
                                var candidate = calledMethodCandidates.FirstOrDefault(x => x.Parameters[1].Type == "Func<HttpContext, bool>");
                                if (candidate != null)
                                    if (!candidate.CalledBy.Contains(serviceRegistrationMethod))
                                        candidate.CalledBy.Add(serviceRegistrationMethod);
                            }
                        }
                    }
                    else
                    {
                        // add serviceRegistrationMethod to calledMethodCandidates as a caller
                        foreach (var calledMethodCandidate in calledMethodCandidates)
                            if (!calledMethodCandidate.CalledBy.Contains(serviceRegistrationMethod))
                                calledMethodCandidate.CalledBy.Add(serviceRegistrationMethod);
                    }
                }
                else if(registration.TypeParameters.Length > 1)
                {
                    //throw new NotImplementedException();
                }
            }
        }
    }

    private readonly string[] _skippedConstraints = new[] {"class"};
    private bool IsTypeParamsMatched(ServiceRegistrationMethodInfo serviceRegistrationMethodInfo, string[][] typeParamMutations)
    {
        if (serviceRegistrationMethodInfo.TypeParams.Length == 0)
        {
            // not supported?
            return false;
        }

        if (serviceRegistrationMethodInfo.TypeParams.Length != 1)
        {
            // not supported?
            return false;
        }

        var constraints = serviceRegistrationMethodInfo.TypeParams[0].Constraints;
        if (constraints.Length == 1 && _skippedConstraints.Contains(constraints[0]))
            // e.g. if the constraints == "class", everything is ok if the param counts are equal
            return typeParamMutations.Length == 1;

        var expectedTypeParam = serviceRegistrationMethodInfo.TypeParams[0].Constraints.Except(_skippedConstraints).ToArray();
        if (expectedTypeParam.Length != 1)
        {
            // ??
            return false;
        }

        if (typeParamMutations.Length != 1)
            return false;

        if (typeParamMutations[0].Contains(expectedTypeParam[0]))
            return true;
        return false;
    }

    private string[][] GetTypeParamMutations(string[] typeNames)
    {
        var result = new string[typeNames.Length][];

        for (var i = 0; i < typeNames.Length; i++)
        {
            var typeName = typeNames[i];
            var mutations = new List<string> {typeName};

            var index = 0;
            while (index < mutations.Count)
            {
                var types = _allTypes.Values.Where(t => t.ClassName == mutations[index]).ToArray();
                index++;
                if (types.Length == 0)
                    continue;
                if (types.Length > 1)
                {
                    // Not implemented but can be misleading. It would be better if an exact type system was used
                    continue;
                }

                var type = types[0];
                foreach (var tName in type.BaseList)
                {
                    if (!mutations.Contains(tName))
                        mutations.Add(tName);
                }
            }


            result[i] = mutations.ToArray();
        }

        return result;
    }
}