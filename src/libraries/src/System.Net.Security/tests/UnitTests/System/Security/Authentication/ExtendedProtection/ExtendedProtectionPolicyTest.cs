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
            var ex = AssertExtensions.Throws<ArgumentException>("policyEnforcement", () => new ExtendedProtectionPolicy(PolicyEnforcement.Never, ProtectionScenario.TransportSelected, null));
            Assert.Equal(typeof(ArgumentException), ex.GetType());
        }

        [Fact]
        public void Constructor_ServiceNameCollection_ZeroElementsParam()
        {
            var paramValue = new ServiceNameCollection(new List<string>());
            var ex = AssertExtensions.Throws<ArgumentException>("customServiceNames", () => new ExtendedProtectionPolicy(PolicyEnforcement.Always, ProtectionScenario.TransportSelected, paramValue));
            Assert.Equal(typeof(ArgumentException), ex.GetType());
        }

        [Fact]
        public void Constructor_ChannelBinding_NullParam()
        {
            var ex = AssertExtensions.Throws<ArgumentNullException>("customChannelBinding", () => new ExtendedProtectionPolicy(PolicyEnforcement.Always, null));
            Assert.Equal(typeof(ArgumentNullException), ex.GetType());
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
        public void ExtendedProtectionPolicy_OSSupportsExtendedProtection()
        {
            Assert.True(ExtendedProtectionPolicy.OSSupportsExtendedProtection);
        }

        [Fact]
        public void ExtendedProtectionPolicy_Properties()
        {
            var policyEnforcementParam = PolicyEnforcement.Always;
            var protectionScenarioParam = ProtectionScenario.TransportSelected;
            var customChannelBindingParam = new MockCustomChannelBinding();

            var extendedProtectionPolicy = new ExtendedProtectionPolicy(policyEnforcementParam, customChannelBindingParam);

            Assert.Null(extendedProtectionPolicy.CustomServiceNames);
            Assert.Equal(policyEnforcementParam, extendedProtectionPolicy.PolicyEnforcement);
            Assert.Equal(protectionScenarioParam, extendedProtectionPolicy.ProtectionScenario);
            Assert.Equal(customChannelBindingParam, extendedProtectionPolicy.CustomChannelBinding);
        }

        [Fact]
        public void ExtendedProtectionPolicy_ToString()
        {
            var serviceName1 = "Test1";
            var serviceName2 = "Test2";
            var serviceNameCollectionParam = new ServiceNameCollection(new List<string> { serviceName1, serviceName2 });
            var policyEnforcementParam = PolicyEnforcement.Always;
            var protectionScenarioParam = ProtectionScenario.TransportSelected;
            var extendedProtectionPolicy = new ExtendedProtectionPolicy(policyEnforcementParam, protectionScenarioParam, serviceNameCollectionParam);
            var expectedString = $"ProtectionScenario={protectionScenarioParam}; PolicyEnforcement={policyEnforcementParam}; CustomChannelBinding=<null>; ServiceNames={serviceName1}, {serviceName2}";

            var result = extendedProtectionPolicy.ToString();

            Assert.NotNull(result);
            Assert.Equal(expectedString, result);
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
