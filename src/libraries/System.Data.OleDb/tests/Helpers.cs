// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;

namespace System.Data.OleDb.Tests
{
    public static class Helpers
    {
        public const string IsDriverAvailable = nameof(Helpers) + "." + nameof(GetIsDriverAvailable);
        public const string IsAceDriverAvailable = nameof(Helpers) + "." + nameof(GetIsAceDriverAvailable);
        public static bool GetIsDriverAvailable() => Nested.IsAvailable;
        public static bool GetIsAceDriverAvailable() => GetIsDriverAvailable() && !PlatformDetection.Is32BitProcess;
        public static string ProviderName => Nested.ProviderName;
        public static string GetTableName(string memberName) => memberName + ".csv";

        private class Nested
        {
            public static readonly bool IsAvailable;
            public static readonly string ProviderName;
            public static Nested Instance => s_instance;
            private static readonly Nested s_instance = new Nested();
            private const string ExpectedProviderName = @"Microsoft.ACE.OLEDB.12.0";
            private Nested() { }
            static Nested()
            {
                // Get the sources rowset for the SQLOLEDB enumerator
                DataTable table = (new OleDbEnumerator()).GetElements();
                DataColumn providersRegistered = table.Columns["SOURCES_NAME"];
                List<object> providerNames = new List<object>();
                foreach (DataRow row in table.Rows)
                {
                    providerNames.Add((string)row[providersRegistered]);
                }
                // skip if x86 or if the expected driver not available 
                IsAvailable = !PlatformDetection.Is32BitProcess && providerNames.Contains(ExpectedProviderName);
                if (!CultureInfo.CurrentCulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                {
                    IsAvailable = false; // ActiveIssue: https://github.com/dotnet/corefx/issues/38737
                }
                ProviderName = IsAvailable ? ExpectedProviderName : null;
            }
        }
    }
}
