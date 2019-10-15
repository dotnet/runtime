// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class CodeConnectAccessTests
    {
        [Fact]
        public static void CodeConnectAccessCallMethods()
        {
            CodeConnectAccess cca = new CodeConnectAccess("test", 0);
            string teststring = CodeConnectAccess.AnyScheme;
            int testint = CodeConnectAccess.DefaultPort;
            testint = CodeConnectAccess.OriginPort;
            teststring = CodeConnectAccess.OriginScheme;
            cca = CodeConnectAccess.CreateAnySchemeAccess(0);
            cca = CodeConnectAccess.CreateOriginSchemeAccess(0);
            cca = new CodeConnectAccess("test", 0);
            bool testbool = cca.Equals(new object());
            testint = cca.GetHashCode();
        }
    }
}
