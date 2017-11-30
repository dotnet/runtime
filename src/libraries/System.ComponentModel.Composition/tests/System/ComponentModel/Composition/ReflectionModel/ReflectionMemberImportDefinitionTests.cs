// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class ReflectionMemberImportDefinitionTests
    {
        [Fact]
        public void Constructor()
        {
            PropertyInfo expectedMember = typeof(PublicImportsExpectingPublicExports).GetProperty("PublicImportPublicProperty");
            LazyMemberInfo expectedImportingMemberInfo = new LazyMemberInfo(expectedMember);
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Foo", typeof(object)) };
            IDictionary<string, object> metadata = new Dictionary<string, object>();
            metadata["Key"] = "value";

            ReflectionMemberImportDefinition definition = new ReflectionMemberImportDefinition(
                expectedImportingMemberInfo, "Contract", (string)null, requiredMetadata, ImportCardinality.ZeroOrMore, true, false, CreationPolicy.NonShared, metadata,  null);

            Assert.Equal(expectedImportingMemberInfo, definition.ImportingLazyMember);

            Assert.Equal("Contract", definition.ContractName);
            Assert.Same(requiredMetadata, definition.RequiredMetadata);
            Assert.Same(metadata, definition.Metadata);
            Assert.Equal(CreationPolicy.NonShared, definition.RequiredCreationPolicy);
            Assert.Equal(true, definition.IsRecomposable);
            Assert.Equal(false, definition.IsPrerequisite);
            Assert.Null(((ICompositionElement)definition).Origin);
            Assert.NotNull(((ICompositionElement)definition).DisplayName);
            Assert.True(((ICompositionElement)definition).DisplayName.Contains(expectedMember.GetDisplayName()));
        }

        [Fact]
        public void Constructor_WithNullRequiredMetadata()
        {
            LazyMemberInfo member = CreateLazyMemberInfo();

            ReflectionMemberImportDefinition definition = new ReflectionMemberImportDefinition(
                member, "Contract", (string)null, null, ImportCardinality.ZeroOrMore, true, false, CreationPolicy.NonShared, null, null);

            Assert.NotNull(definition.RequiredMetadata);
            Assert.Equal(0, definition.RequiredMetadata.Count());
        }

        [Fact]
        public void SetDefinition_OriginIsSet()
        {
            LazyMemberInfo member = CreateLazyMemberInfo();
            var expectedPartDefinition = PartDefinitionFactory.CreateAttributed(typeof(object));
            ReflectionMemberImportDefinition definition = new ReflectionMemberImportDefinition(
                member, "Contract", (string)null, null, ImportCardinality.ZeroOrMore, true, false, CreationPolicy.NonShared, null, expectedPartDefinition);

            Assert.Same(expectedPartDefinition, ((ICompositionElement)definition).Origin);
        }

        private static LazyMemberInfo CreateLazyMemberInfo()
        {
            PropertyInfo expectedMember = typeof(PublicImportsExpectingPublicExports).GetProperty("PublicImportPublicProperty");
            return new LazyMemberInfo(expectedMember);
        }
    }
}
