//using SenseNet.ApplicationModel;
//using SenseNet.ContentRepository;

//namespace ConsoleApp1.ToParse
//{
//    public static class ODataController1
//    {
//        public static string DoIt1(Content content, string param1)
//        {
//            return param1;
//        }

//        /// <summary>
//        /// Summary of <c>DoIt2</c> operation. The <paramref name="param1"/> is a parameter.
//        /// The <see cref="ODataController1"/> is an operation container.
//        /// Other class is <seealso cref="ODataAction"/>.
//        /// <para>Para1.</para>
//        /// </summary>
//        /// <remarks>
//        /// <para>Para2.</para>
//        /// <para>Para3.</para>
//        /// Last line.</remarks>
//        /// <example>
//        /// Example head.
//        /// <code>
//        /// // csharp comment
//        /// </code>
//        /// <code lang="javascript">
//        /// // javascript comment
//        /// </code>
//        /// <code lang="typescript">
//        /// // typescript comment
//        /// </code>
//        /// </example>
//        /// <param name="content">The content param</param>
//        /// <param name="param1">The param1 param</param>
//        /// <returns>A string</returns>
//        /// <exception cref="System.AggregateException">Thrown when ...1</exception>
//        /// <exception cref="System.AccessViolationException">Thrown when ...2</exception>
//        [ODataFunction("Op9_Renamed", Description = "Lorem ipsum ...", Icon = "icon94")]
//        [ContentTypes(N.CT.User, N.CT.Group, "OrgUnit")]
//        public static string DoIt2(Content content, string param1)
//        {
//            return param1;
//        }

//        [ODataFunction]
//        [ContentTypes(N.CT.User, N.CT.Group, "OrgUnit")]
//        [AllowedRoles(N.R.Administrators, "Editors")]
//        [RequiredPolicies("Policy1")]
//        [RequiredPermissions("See, Run")]
//        [Scenario("Scenario1, Scenario2")]
//        [Scenario("Scenario2", "Scenario3, Scenario4")]
//        public static string DoIt3(Content content, string param1)
//        {
//            return param1;
//        }

//        public static string DoIt4(Content content, string param1)
//        {
//            return param1;
//        }
//    }
//}
