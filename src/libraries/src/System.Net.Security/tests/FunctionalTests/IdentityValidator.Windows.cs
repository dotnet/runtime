// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

using Xunit;

namespace System.Net.Security.Tests
{
    public class IdentityValidator
    {
        public static void AssertIsCurrentIdentity(IIdentity identity)
        {
            Assert.Equal(WindowsIdentity.GetCurrent().Name, identity.Name);
        }

        public static void AssertHasName(IIdentity identity, string expectedName)
        {
            Assert.Equal(expectedName, identity.Name);
        }
    }
}
