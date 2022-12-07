// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class HostProtectionTests
    {
        private const int COR_E_HOSTPROTECTION = unchecked((int)0x80131640);

        [Fact]
        public static void HostProtectionExceptionCallMethods()
        {
            HostProtectionException hpe = new HostProtectionException();
            hpe.ToString();
        }

        [Fact]
        public static void HostProtectionException_Ctor_Empty()
        {
            HostProtectionException hpe = new();
            Assert.Equal(COR_E_HOSTPROTECTION, hpe.HResult);
            Assert.Equal(HostProtectionResource.None, hpe.ProtectedResources);
            Assert.Equal(HostProtectionResource.None, hpe.DemandedResources);
        }

        [Fact]
        public static void HostProtectionException_Ctor_String()
        {
            string message = "Created HostProtectionException";
            HostProtectionException hpe = new(message);
            Assert.Equal(message, hpe.Message);
            Assert.Equal(COR_E_HOSTPROTECTION, hpe.HResult);
            Assert.Equal(HostProtectionResource.None, hpe.ProtectedResources);
            Assert.Equal(HostProtectionResource.None, hpe.DemandedResources);
        }

        [Fact]
        public static void HostProtectionException_Ctor_String_Exception()
        {
            string message = "Created HostProtectionException";
            Exception innerException = new("Created inner exception");
            HostProtectionException hpe = new(message, innerException);
            Assert.Equal(message, hpe.Message);
            Assert.Equal(COR_E_HOSTPROTECTION, hpe.HResult);
            Assert.Equal(HostProtectionResource.None, hpe.ProtectedResources);
            Assert.Equal(HostProtectionResource.None, hpe.DemandedResources);
            Assert.Same(innerException, hpe.InnerException);
        }

        [Fact]
        public static void HostProtectionException_Ctor_String_HostProtectionResource_HostProtectionResource()
        {
            string message = "Created HostProtectionException";
            HostProtectionException hpe = new(message, HostProtectionResource.SecurityInfrastructure, HostProtectionResource.MayLeakOnAbort);
            Assert.Equal(message, hpe.Message);
            Assert.Equal(COR_E_HOSTPROTECTION, hpe.HResult);
            Assert.Equal(HostProtectionResource.SecurityInfrastructure, hpe.ProtectedResources);
            Assert.Equal(HostProtectionResource.MayLeakOnAbort, hpe.DemandedResources);
        }

        [Fact]
        public static void HostProtectionAttributeCallMethods()
        {
            HostProtectionAttribute hpa = new HostProtectionAttribute();
            IPermission ip = hpa.CreatePermission();
        }
    }
}
