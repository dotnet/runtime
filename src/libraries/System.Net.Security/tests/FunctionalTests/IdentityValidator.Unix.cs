// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Principal;

using Xunit;

namespace System.Net.Security.Tests
{
    public class IdentityValidator
    {
        public static void AssertIsCurrentIdentity(IIdentity identity)
        {
            throw new PlatformNotSupportedException();
        }

        public static void AssertHasName(IIdentity identity, string expectedName)
        {
            Assert.Equal(expectedName, identity.Name);
        }
    }
}
