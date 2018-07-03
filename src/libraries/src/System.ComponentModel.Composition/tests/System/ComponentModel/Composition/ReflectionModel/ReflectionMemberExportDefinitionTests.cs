// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class ReflectionMemberExportDefinitionTests
    {
        private static ReflectionMemberExportDefinition CreateReflectionExportDefinition(LazyMemberInfo exportMember, string contractname, IDictionary<string, object> metadata)
        {
            return CreateReflectionExportDefinition(exportMember, contractname, metadata, null);
        }

        private static ReflectionMemberExportDefinition CreateReflectionExportDefinition(LazyMemberInfo exportMember, string contractname, IDictionary<string, object> metadata, ICompositionElement origin)
        {
            return (ReflectionMemberExportDefinition)ReflectionModelServices.CreateExportDefinition(
                exportMember, contractname, CreateLazyMetadata(metadata), origin);
        }

        private static Lazy<IDictionary<string, object>> CreateLazyMetadata(IDictionary<string, object> metadata)
        {
            return new Lazy<IDictionary<string, object>>(() => metadata, false);
        }

        [Fact]
        public void Constructor()
        {
            MemberInfo expectedMember = this.GetType();
            LazyMemberInfo expectedExportingMemberInfo = new LazyMemberInfo(expectedMember);

            string expectedContractName = "Contract";
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            ReflectionMemberExportDefinition definition = CreateReflectionExportDefinition(expectedExportingMemberInfo, expectedContractName, expectedMetadata);

            Assert.Equal(expectedExportingMemberInfo, definition.ExportingLazyMember);
            Assert.Same(expectedMember, definition.ExportingLazyMember.GetAccessors()[0]);
            Assert.Equal(MemberTypes.TypeInfo, definition.ExportingLazyMember.MemberType);

            Assert.Same(expectedContractName, definition.ContractName);

            Assert.NotNull(definition.Metadata);
            Assert.True(definition.Metadata.Keys.SequenceEqual(expectedMetadata.Keys));
            Assert.True(definition.Metadata.Values.SequenceEqual(expectedMetadata.Values));

            Assert.Null(((ICompositionElement)definition).Origin);
        }

        [Fact]
        public void Constructor_NullMetadata()
        {
            MemberInfo expectedMember = this.GetType();
            LazyMemberInfo expectedExportingMemberInfo = new LazyMemberInfo(expectedMember);

            string expectedContractName = "Contract";

            ReflectionMemberExportDefinition definition = CreateReflectionExportDefinition(expectedExportingMemberInfo, expectedContractName, null);

            Assert.Equal(expectedExportingMemberInfo, definition.ExportingLazyMember);
            Assert.Same(expectedMember, definition.ExportingLazyMember.GetAccessors()[0]);
            Assert.Equal(MemberTypes.TypeInfo, definition.ExportingLazyMember.MemberType);

            Assert.Same(expectedContractName, definition.ContractName);

            Assert.NotNull(definition.Metadata);
            Assert.Equal(0, definition.Metadata.Count);

            Assert.Null(((ICompositionElement)definition).Origin);
        }

        [Fact]
        public void SetDefinition_OriginIsSet()
        {
            var expectedPartDefinition = PartDefinitionFactory.CreateAttributed(typeof(object));
            var exportDefinition = CreateReflectionExportDefinition(new LazyMemberInfo(this.GetType()), "ContractName", null, expectedPartDefinition);

            Assert.Same(expectedPartDefinition, ((ICompositionElement)exportDefinition).Origin);
        }
        
        [Fact]
        public void SetDefinition_PartDefinitionDoesNotContainCreationPolicy_CreationPolicyShouldNotBeInMetadata()
        {
            var expectedPartDefinition = PartDefinitionFactory.CreateAttributed(typeof(object));
            var exportDefinition = CreateReflectionExportDefinition(new LazyMemberInfo(this.GetType()), "ContractName", null);

            Assert.False(exportDefinition.Metadata.ContainsKey(CompositionConstants.PartCreationPolicyMetadataName));
        }

        [Fact]
        public void ICompositionElementDisplayName_ValueAsContractName_ShouldIncludeContractName()
        {
            var contractNames = Expectations.GetContractNamesWithEmpty();

            foreach (var contractName in contractNames)
            {
                if (string.IsNullOrEmpty(contractName)) continue;
                var definition = (ICompositionElement)CreateReflectionExportDefinition(new LazyMemberInfo(typeof(string)), contractName, null);

                var e = CreateDisplayNameExpectation(contractName);

                Assert.Equal(e, definition.DisplayName);
            }
        }

        [Fact]
        public void ICompositionElementDisplayName_TypeAsMember_ShouldIncludeMemberDisplayName()
        {
            var types = Expectations.GetTypes();

            foreach (var type in types)
            {
                var definition = (ICompositionElement)CreateReflectionExportDefinition(new LazyMemberInfo(type), "Contract", null);

                var e = CreateDisplayNameExpectation(type);

                Assert.Equal(e, definition.DisplayName);
            }
        }

        [Fact]
        public void ICompositionElementDisplayName_ValueAsMember_ShouldIncludeMemberDisplayName()
        {
            var members = Expectations.GetMembers();

            foreach (var member in members)
            {
                var definition = (ICompositionElement)CreateReflectionExportDefinition(new LazyMemberInfo(member), "Contract", null);

                var e = CreateDisplayNameExpectation(member);

                Assert.Equal(e, definition.DisplayName);
            }
        }

        [Fact]
        public void ToString_ShouldReturnDisplayName()
        {
            var members = Expectations.GetMembers();

            foreach (var member in members)
            {
                var definition = (ICompositionElement)CreateReflectionExportDefinition(new LazyMemberInfo(member), "Contract", null);

                Assert.Equal(definition.DisplayName, definition.ToString());
            }
        }

        private static string CreateDisplayNameExpectation(string contractName)
        {
            return string.Format("System.String (ContractName=\"{0}\")", contractName);
        }

        private static string CreateDisplayNameExpectation(MemberInfo member)
        {
            return string.Format("{0} (ContractName=\"Contract\")", member.GetDisplayName());
        }

    }
}
