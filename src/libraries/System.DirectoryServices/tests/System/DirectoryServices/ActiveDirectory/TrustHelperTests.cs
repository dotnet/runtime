// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.DirectoryServices.ActiveDirectory.Tests
{
    public static class TrustHelperTests
    {
        [Fact]
        public static void CreateTrustPassword_Random()
        {
            Type trustHelperType = typeof(Domain).Assembly.GetType(
                "System.DirectoryServices.ActiveDirectory.TrustHelper",
                throwOnError: true);
            MethodInfo createTrustPasswordMethod = trustHelperType.GetMethod(
                "CreateTrustPassword",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(createTrustPasswordMethod);

            HashSet<string> passwords = new HashSet<string>();

            for (int i = 0; i < 100; i++)
            {
                string password = (string)createTrustPasswordMethod.Invoke(null, null);

                // password is a 15 character length string from a 87 character set.
                // log2(87)*15 is about 96, the odds of which we will generate a duplicate
                // here are next to nothing.
                Assert.True(passwords.Add(password), "password is not a duplicate");
            }
        }
    }
}
