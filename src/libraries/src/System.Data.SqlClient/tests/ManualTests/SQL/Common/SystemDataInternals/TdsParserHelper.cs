// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Data.SqlClient.ManualTesting.Tests.SystemDataInternals
{
    internal static class TdsParserHelper
    {
        private static Assembly s_systemDotData = Assembly.Load(new AssemblyName(typeof(SqlConnection).GetTypeInfo().Assembly.FullName));
        private static Type s_tdsParser = s_systemDotData.GetType("System.Data.SqlClient.TdsParser");
        private static FieldInfo s_tdsParserPhysicalStateObject = s_tdsParser.GetField("_physicalStateObj", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void VerifyObjectIsTdsParser(object parser)
        {
            if (parser == null)
                throw new ArgumentNullException("stateObject");
            if (!s_tdsParser.IsInstanceOfType(parser))
                throw new ArgumentException("Object provided was not a DbConnectionInternal", "internalConnection");
        }

        internal static object GetStateObject(object parser)
        {
            VerifyObjectIsTdsParser(parser);
            return s_tdsParserPhysicalStateObject.GetValue(parser);
        }
    }
}
