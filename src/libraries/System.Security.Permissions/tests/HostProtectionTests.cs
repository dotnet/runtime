// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class HostProtectionTests
    {
        [Fact]
        public static void HostProtectionExceptionCallMethods()
        {
            HostProtectionException hpe = new HostProtectionException();
            hpe.ToString();
        }

        [Fact]
        public static void HostProtectionAttributeCallMethods()
        {
            HostProtectionAttribute hpa = new HostProtectionAttribute();
            IPermission ip = hpa.CreatePermission();
        }
    }
}
