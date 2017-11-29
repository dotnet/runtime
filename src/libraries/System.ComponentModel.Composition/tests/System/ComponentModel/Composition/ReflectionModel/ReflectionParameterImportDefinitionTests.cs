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
    public class ReflectionParameterImportDefinitionTests
    {
        [Fact]
        public void Constructor()
        {
            Lazy<ParameterInfo> parameter = CreateLazyParameter();
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Foo", typeof(object)) };
            IDictionary<string, object> metadata = new Dictionary<string, object>();
            metadata["Key"] = "value";

            ReflectionParameterImportDefinition definition = new ReflectionParameterImportDefinition(
                            parameter, "Contract", (string)null, requiredMetadata, ImportCardinality.ZeroOrMore, CreationPolicy.NonShared, metadata, null);

            Assert.Same(parameter, definition.ImportingLazyParameter);
            Assert.Equal("Contract", definition.ContractName);
            Assert.Same(requiredMetadata, definition.RequiredMetadata);
            Assert.Same(metadata, definition.Metadata);
            Assert.Equal(CreationPolicy.NonShared, definition.RequiredCreationPolicy);
            Assert.Equal(false, definition.IsRecomposable);
            Assert.Equal(true, definition.IsPrerequisite);
            Assert.Null(((ICompositionElement)definition).Origin);
            Assert.NotNull(((ICompositionElement)definition).DisplayName);
        }

        [Fact]
        public void Constructor_WithNullRequiredMetadata()
        {
            Lazy<ParameterInfo> parameter = CreateLazyParameter();

            ReflectionParameterImportDefinition definition = new ReflectionParameterImportDefinition(
                parameter, "Contract", (string)null, null, ImportCardinality.ZeroOrMore, CreationPolicy.NonShared, null, null);

            Assert.NotNull(definition.RequiredMetadata);
            Assert.Equal(0, definition.RequiredMetadata.Count());
        }

        [Fact]
        public void SetDefinition_OriginIsSet()
        {
            Lazy<ParameterInfo> parameter = CreateLazyParameter();
            var expectedPartDefinition = PartDefinitionFactory.CreateAttributed(typeof(object));

            ReflectionParameterImportDefinition definition = new ReflectionParameterImportDefinition(
                parameter, "Contract", (string)null, null, ImportCardinality.ZeroOrMore, CreationPolicy.NonShared, null, expectedPartDefinition);

            Assert.Same(expectedPartDefinition, ((ICompositionElement)definition).Origin);
        }

        [Fact]
        public void ICompositionElementDisplayName_ValueAsParameter_ShouldIncludeParameterName()
        {
            var names = Expectations.GetContractNamesWithEmpty();

            Assert.All(names, name =>
            {
                var definition = CreateReflectionParameterImportDefinition(name);

                var e = CreateDisplayNameExpectationFromParameterName(definition, name);

                Assert.Equal(e, ((ICompositionElement)definition).DisplayName);
            });
        }

        [Fact]
        public void ICompositionElementDisplayName_ValueAsParameter_ShouldIncludeContractName()
        {
            var types = Expectations.GetTypes();

            Assert.All(types, type =>
            {
                var definition = CreateReflectionParameterImportDefinition(type);

                var e = CreateDisplayNameExpectationFromContractName(definition, type);

                Assert.Equal(e, ((ICompositionElement)definition).DisplayName);
            });
        }

        private Lazy<ParameterInfo> CreateLazyParameter()
        {
            return typeof(SimpleConstructorInjectedObject).GetConstructors().First().GetParameters().First().AsLazy();
        }

        private static string CreateDisplayNameExpectationFromContractName(ReflectionParameterImportDefinition definition, Type type)
        {
            string contractName = AttributedModelServices.GetContractName(type);

            return String.Format("{0} (Parameter=\"\", ContractName=\"{1}\")", definition.ImportingLazyParameter.Value.Member.GetDisplayName(), contractName);
        }

        private static string CreateDisplayNameExpectationFromParameterName(ReflectionParameterImportDefinition definition, string name)
        {
            return String.Format("{0} (Parameter=\"{1}\", ContractName=\"System.String\")", definition.ImportingLazyParameter.Value.Member.GetDisplayName(), name);
        }

        private static ReflectionParameterImportDefinition CreateReflectionParameterImportDefinition(Type parameterType)
        {
            var parameter = ReflectionFactory.CreateParameter(parameterType);

            return CreateReflectionParameterImportDefinition(parameter);
        }

        private static ReflectionParameterImportDefinition CreateReflectionParameterImportDefinition(string name)
        {
            var parameter = ReflectionFactory.CreateParameter(name);

            return CreateReflectionParameterImportDefinition(parameter);
        }

        private static ReflectionParameterImportDefinition CreateReflectionParameterImportDefinition(ParameterInfo parameter)
        {
            return new ReflectionParameterImportDefinition(
                parameter.AsLazy(), AttributedModelServices.GetContractName(parameter.ParameterType), (string)null, null, ImportCardinality.ZeroOrMore, CreationPolicy.NonShared, null, null);
        }
    }
}
