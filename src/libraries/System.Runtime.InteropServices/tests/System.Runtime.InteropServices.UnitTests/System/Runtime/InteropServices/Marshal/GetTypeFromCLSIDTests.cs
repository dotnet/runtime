// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetTypeFromCLSIDTests
    {
        // Represents a COM server that exists on all Windows skus that support IDispatch.
        // See System.DirectoryServices.AccountManagement.ADsLargeInteger.
        private const string IDispatchSupportedComServer = "927971f5-0939-11d1-8be1-00c04fd8d503";
        private const string IDispatchSupportedComServerProgId = "LargeInteger";

        private static readonly Guid TestCLSID = new Guid(IDispatchSupportedComServer);

        private const string TestProgID = IDispatchSupportedComServerProgId;
        private const string TestServerName = "____NonExistingServer____";

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetTypeFromCLSID_NoSuchCLSIDExists_ReturnsExpected()
        {
            Type type = Marshal.GetTypeFromCLSID(Guid.Empty);
            Assert.NotNull(type);
            Assert.Same(type, Marshal.GetTypeFromCLSID(Guid.Empty));

            Assert.Same(type, Type.GetTypeFromCLSID(Guid.Empty));
            Assert.Same(type, Type.GetTypeFromCLSID(Guid.Empty, throwOnError: true));

            Assert.Throws<COMException>(() => Activator.CreateInstance(type));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabledWithOSAutomationSupport))]
        public void GetTypeFromCLSID_CLSIDExists_ReturnsExpected()
        {
            Type type = Marshal.GetTypeFromCLSID(TestCLSID);
            Assert.NotNull(type);
            Assert.Same(type, Marshal.GetTypeFromCLSID(TestCLSID));

            Assert.Same(type, Type.GetTypeFromCLSID(TestCLSID));
            Assert.Same(type, Type.GetTypeFromCLSID(TestCLSID, throwOnError: true));
            Assert.Same(type, Type.GetTypeFromCLSID(TestCLSID, server: null, throwOnError: true));

            Assert.Same(type, Type.GetTypeFromProgID(TestProgID));
            Assert.Same(type, Type.GetTypeFromProgID(TestProgID, throwOnError: true));
            Assert.Same(type, Type.GetTypeFromProgID(TestProgID, server: null, throwOnError: true));

            Assert.NotNull(Activator.CreateInstance(type));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabledWithOSAutomationSupport))]
        public void GetTypeFromCLSID_CLSIDExists_Server_ReturnsExpected()
        {
            Type type = Type.GetTypeFromCLSID(TestCLSID, server: TestServerName, throwOnError: true);
            Assert.NotNull(type);
            Assert.Same(type, Type.GetTypeFromProgID(TestProgID, server: TestServerName, throwOnError: true));

            Assert.Throws<COMException>(() => Activator.CreateInstance(type));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypeFromCLSID_Unix()
        {
            Assert.Null(Marshal.GetTypeFromCLSID(Guid.Empty));
            Assert.Null(Type.GetTypeFromCLSID(Guid.Empty, throwOnError: false));
            Assert.Throws<PlatformNotSupportedException>(() => Type.GetTypeFromCLSID(Guid.Empty, throwOnError: true));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypeFromProgID_Unix()
        {
            Assert.Null(Type.GetTypeFromProgID(TestProgID, throwOnError: false));
            Assert.Throws<PlatformNotSupportedException>(() => Type.GetTypeFromProgID(TestProgID, throwOnError: true));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetTypeFromProgID_ReturnsExpected()
        {
            AssertExtensions.Throws<ArgumentNullException>("progID", () => Type.GetTypeFromProgID(null));
        }
    }
}
