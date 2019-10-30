// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;

using Xunit;

namespace System.Net.Security.Tests
{
    public class ExtendedProtectionPolicyTest
    {
        [Fact]
        public void Constructor_PolicyEnforcement_NeverParam()
        {
            AssertExtensions.Throws<ArgumentException>("policyEnforcement", () => new ExtendedProtectionPolicy(PolicyEnforcement.Never, ProtectionScenario.TransportSelected, null));
        }

        [Fact]
        public void Constructor_ServiceNameCollection_ZeroElementsParam()
        {
            var paramValue = new ServiceNameCollection(new List<string>());
            AssertExtensions.Throws<ArgumentException>("customServiceNames", () => new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, paramValue));
        }

        [Fact]
        public void Constructor_PolicyEnforcementChannelBinding_NeverParam()
        {
            var customChannelBinding = new MockCustomChannelBinding();
            AssertExtensions.Throws<ArgumentException>("policyEnforcement", () => new ExtendedProtectionPolicy(PolicyEnforcement.Never, customChannelBinding));
        }

        [Fact]
        public void Constructor_ChannelBinding_NullParam()
        {
            AssertExtensions.Throws<ArgumentNullException>("customChannelBinding", () => new ExtendedProtectionPolicy(PolicyEnforcement.Always, null));
        }

        [Fact]
        public void Constructor_CollectionParam()
        {
            var paramValue = new List<string> { "Test1", "Test2" };
            var expectedServiceNameCollection = new ServiceNameCollection(paramValue);
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, paramValue);

            Assert.Equal(2, extendedProtectionPolicy.CustomServiceNames.Count);
            Assert.Equal(expectedServiceNameCollection, extendedProtectionPolicy.CustomServiceNames);
        }

        [Fact]
        public void Constructor_PolicyEnforcement_MembersAreSet()
        {
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

            Assert.Equal(PolicyEnforcement.Never, extendedProtectionPolicy.PolicyEnforcement);
            Assert.Equal(ProtectionScenario.TransportSelected, extendedProtectionPolicy.ProtectionScenario);
        }

        [Fact]
        public void ExtendedProtectionPolicy_OSSupportsExtendedProtection()
        {
            Assert.True(ExtendedProtectionPolicy.OSSupportsExtendedProtection);
        }

        [Fact]
        public void ExtendedProtectionPolicy_Properties()
        {
            var customChannelBindingParam = new MockCustomChannelBinding();

            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, customChannelBindingParam);

            Assert.Null(extendedProtectionPolicy.CustomServiceNames);
            Assert.Equal(PolicyEnforcement.Always, extendedProtectionPolicy.PolicyEnforcement);
            Assert.Equal(ProtectionScenario.TransportSelected, extendedProtectionPolicy.ProtectionScenario);
            Assert.Equal(customChannelBindingParam, extendedProtectionPolicy.CustomChannelBinding);
        }

        [Fact]
        public void ExtendedProtectionPolicy_ToString()
        {
            string serviceName1 = "Test1";
            string serviceName2 = "Test2";
            var serviceNameCollectionParam = new ServiceNameCollection(new List<string> { serviceName1, serviceName2 });
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, serviceNameCollectionParam);
            string expectedResult = $"ProtectionScenario={ProtectionScenario.TransportSelected}; PolicyEnforcement={PolicyEnforcement.Always}; CustomChannelBinding=<null>; ServiceNames={serviceName1}, {serviceName2}";

            string result = extendedProtectionPolicy.ToString();

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ExtendedProtectionPolicy_NoCustomServiceNamesAndNoCustomChannelBinding_ToString()
        {
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always);
            string expectedResult = $"ProtectionScenario={extendedProtectionPolicy.ProtectionScenario}; PolicyEnforcement={PolicyEnforcement.Always}; CustomChannelBinding=<null>; ServiceNames=<null>";

            string result = extendedProtectionPolicy.ToString();

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ExtendedProtectionPolicy_NoCustomServiceNames_ToString()
        {
            var channelBinding = new MockCustomChannelBinding();
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, channelBinding);
            string expectedResult = $"ProtectionScenario={extendedProtectionPolicy.ProtectionScenario}; PolicyEnforcement={PolicyEnforcement.Always}; CustomChannelBinding={channelBinding.ToString()}; ServiceNames=<null>";

            string result = extendedProtectionPolicy.ToString();

            Assert.Equal(expectedResult, result);
        }

        public class MockCustomChannelBinding : ChannelBinding
        {
            protected override bool ReleaseHandle()
            {
                throw new NotImplementedException();
            }

            public override int Size { get; }
        }
    }
}
