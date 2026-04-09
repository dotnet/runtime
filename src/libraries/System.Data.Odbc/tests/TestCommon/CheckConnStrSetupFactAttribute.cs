// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Odbc.Tests
{
    public class CheckConnStrSetupFactAttribute : FactAttribute
    {
        public CheckConnStrSetupFactAttribute()
        {
            if (!DataTestUtility.AreConnStringsSetup())
            {
                Skip = "Connection Strings Not Setup";
            }
        }
    }
}
