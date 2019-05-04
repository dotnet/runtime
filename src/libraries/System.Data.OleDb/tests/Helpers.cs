// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Data.OleDb.Tests
{
    public static class Helpers
    {
        public const string IsDriverAvailable = nameof(Helpers) + "." + nameof(GetIsDriverAvailable);
        public static bool GetIsDriverAvailable() => Nested.IsAvailable;
        public static string ProviderName => Nested.ProviderName;
        public static string GetTableName(string memberName) => memberName + ".csv";

        private class Nested
        {
            public static readonly bool IsAvailable;
            public static readonly string ProviderName;
            public static Nested Instance => s_instance;
            private static readonly Nested s_instance = new Nested();
            private Nested() { }
            static Nested()
            {
                // Get the sources rowset for the SQLOLEDB enumerator
                DataTable table = (new OleDbEnumerator()).GetElements();
                DataColumn providersRegistered = table.Columns["SOURCES_NAME"];
                List<object> providerNames = new List<object>();
                foreach (DataRow row in table.Rows)
                {
                    providerNames.Add(row[providersRegistered]);
                }
                string providerName = PlatformDetection.Is32BitProcess ? 
                    @"Microsoft.Jet.OLEDB.4.0" : 
                    @"Microsoft.ACE.OLEDB.12.0";
                IsAvailable = providerNames.Contains(providerName);
                ProviderName = IsAvailable ? providerName : null;
            }
        }
    }
}
