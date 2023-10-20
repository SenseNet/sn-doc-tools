//using System;

//namespace SenseNet.ApplicationModel
//{
//    public abstract class ODataOperationAttribute : Attribute
//    {
//        public abstract bool CauseStateChange { get; }

//        public string OperationName { get; set; }
//        public string Description { get; set; }
//        public string Icon { get; set; }

//        protected ODataOperationAttribute() { }

//        protected ODataOperationAttribute(string operationName)
//        {
//            OperationName = operationName;
//        }
//    }

//    [AttributeUsage(AttributeTargets.Method)]
//    public class ODataAction : ODataOperationAttribute
//    {
//        public override bool CauseStateChange => true;
//        public ODataAction() { }
//        public ODataAction(string operationName) : base(operationName) { }
//    }

//    [AttributeUsage(AttributeTargets.Method)]
//    public class ODataFunction : ODataOperationAttribute
//    {
//        public override bool CauseStateChange => false;
//        public ODataFunction() { }
//        public ODataFunction(string operationName) : base(operationName) { }
//    }

//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
//    public class ContentTypesAttribute : Attribute
//    {
//        public string[] Names { get; set; }
//        public ContentTypesAttribute(params string[] contentTypeNames)
//        {
//            Names = contentTypeNames;
//        }
//    }
//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    
//    public class AllowedRolesAttribute : Attribute
//    {
//        public string[] Names { get; set; }
//        public AllowedRolesAttribute(params string[] roleNames)
//        {
//            Names = roleNames;
//        }
//    }
//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    
//    public class RequiredPermissionsAttribute : Attribute
//    {
//        public string[] Names { get; set; }
//        public RequiredPermissionsAttribute(params string[] permissions)
//        {
//            Names = permissions;
//        }
//    }
//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    
//    public class RequiredPoliciesAttribute : Attribute
//    {
//        public string[] Names { get; set; }
//        public RequiredPoliciesAttribute() { }
//        public RequiredPoliciesAttribute(params string[] policyNames)
//        {
//            Names = policyNames;
//        }
//    }
//    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]

//    public class ScenarioAttribute : Attribute
//    {
//        public string Name { get; set; }
//        public bool AllowSingleton { get; set; }
//        public ScenarioAttribute(string name = null, bool allowSingleton = true)
//        {
//            Name = name;
//            AllowSingleton = allowSingleton;
//        }
//        public ScenarioAttribute(params string[] names)
//        {
//            if (names != null)
//                Name = string.Join(",", names);
//            AllowSingleton = true;
//        }
//    }

//    public static class N
//    {
//        public static class CT
//        {
//            public const string User = "User";
//            public const string Group = "Group";
//        }
//        public static class R
//        {
//            public const string Administrators = "Administrators";
//            public const string Developers = "Developers";
//            public const string Everyone = "Everyone";
//            public const string IdentifiedUsers = "IdentifiedUsers";
//            public const string Visitor = "Visitor";
//            public const string All = "All";
//        }
//    }
//}

//namespace SenseNet.ContentRepository
//{
//    public class Content
//    {

//    }
//}
