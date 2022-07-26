// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class ReflectionModelServicesTests
    {
        [Fact]
        public void CreatePartDefinition()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);

            Assert.Same(expectedType, definition.GetPartType());
            Assert.True(definition.Metadata.Keys.SequenceEqual(expectedMetadata.Keys));
            Assert.True(definition.Metadata.Values.SequenceEqual(expectedMetadata.Values));
            Assert.True(definition.ExportDefinitions.SequenceEqual(expectedExports.Cast<ExportDefinition>()));
            Assert.True(definition.ImportDefinitions.SequenceEqual(expectedImports.Cast<ImportDefinition>()));
            Assert.Same(expectedOrigin, ((ICompositionElement)definition).Origin);
            Assert.NotNull(((ICompositionElement)definition).DisplayName);
            Assert.False(definition.IsDisposalRequired);
        }

        [Fact]
        public void CreatePartDefinition_Disposable()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, true,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);

            Assert.Same(expectedType, definition.GetPartType());
            Assert.True(definition.Metadata.Keys.SequenceEqual(expectedMetadata.Keys));
            Assert.True(definition.Metadata.Values.SequenceEqual(expectedMetadata.Values));
            Assert.True(definition.ExportDefinitions.SequenceEqual(expectedExports.Cast<ExportDefinition>()));
            Assert.True(definition.ImportDefinitions.SequenceEqual(expectedImports.Cast<ImportDefinition>()));
            Assert.Same(expectedOrigin, ((ICompositionElement)definition).Origin);
            Assert.NotNull(((ICompositionElement)definition).DisplayName);
            Assert.True(definition.IsDisposalRequired);
        }

        [Fact]
        public void CreatePartDefinition_NullMetadataAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                null, expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.Metadata);
            Assert.Equal(0, definition.Metadata.Count);
        }

        [Fact]
        public void CreatePartDefinition_EvaluatedNullMetadataAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = null;

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.Metadata);
            Assert.Equal(0, definition.Metadata.Count);
        }

        [Fact]
        public void CreatePartDefinition_NullExportsAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                null,
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.ExportDefinitions);
            Assert.Equal(0, definition.ExportDefinitions.Count());
        }

        [Fact]
        public void CreatePartDefinition_EvaluatedNullExportsAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => null),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.ExportDefinitions);
            Assert.Equal(0, definition.ExportDefinitions.Count());
        }

        [Fact]
        public void CreatePartDefinition_ExportsMustBeOfRightType()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => CreateInvalidExports()),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                definition.ExportDefinitions.Count();
            });
        }

        [Fact]
        public void CreatePartDefinition_NullImportsAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                null,
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.ImportDefinitions);
            Assert.Equal(0, definition.ImportDefinitions.Count());
        }

        [Fact]
        public void CreatePartDefinition_EvaluatedNullImportsAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => null),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            Assert.NotNull(definition.ImportDefinitions);
            Assert.Equal(0, definition.ImportDefinitions.Count());
        }

        [Fact]
        public void CreatePartDefinition_ImportsMustBeOfRightType()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => CreateInvalidImports()),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);
            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                definition.ImportDefinitions.Count();
            });

        }

        [Fact]
        public void CreatePartDefinition_NullTypeNotAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            Assert.Throws<ArgumentNullException>("partType", () =>
            {
                ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(null, false,
                    new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                    new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                    expectedMetadata.AsLazy(), expectedOrigin);
            });
        }

        [Fact]
        public void CreatePartDefinition_NullEvaluatedTypeNotAllowed()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(new Lazy<Type>(() => null), false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);

            ReflectionComposablePartDefinition definition = partDefinition as ReflectionComposablePartDefinition;
            Assert.NotNull(definition);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                definition.GetPartType();
            });
        }

        [Fact]
        public void GetPartType()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            Lazy<Type> lazyPartType = ReflectionModelServices.GetPartType(partDefinition);
            Assert.Equal(expectedLazyType, lazyPartType);
        }

        [Fact]
        public void GetPartType_NullAsPart_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("partDefinition", () =>
            {
                ReflectionModelServices.GetPartType(null);
            });
        }

        [Fact]
        public void GetPartType_InvalidPart_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("partDefinition", () =>
            {
                ReflectionModelServices.GetPartType(new InvalidPartDefinition());
            });
        }

        [Fact]
        public void IsDisposalRequired_ForNonDisposable()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, false,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            bool isDisposalRequired = ReflectionModelServices.IsDisposalRequired(partDefinition);
            Assert.False(isDisposalRequired);
        }

        [Fact]
        public void IsDisposalRequired_ForDisposable()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ComposablePartDefinition partDefinition = ReflectionModelServices.CreatePartDefinition(expectedLazyType, true,
                new Lazy<IEnumerable<ImportDefinition>>(() => expectedImports),
                new Lazy<IEnumerable<ExportDefinition>>(() => expectedExports),
                expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(partDefinition);

            bool isDisposalRequired = ReflectionModelServices.IsDisposalRequired(partDefinition);
            Assert.True(isDisposalRequired);
        }

        [Fact]
        public void IsDisposalRequired_NullAsPart_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("partDefinition", () =>
            {
                ReflectionModelServices.IsDisposalRequired(null);
            });
        }

        [Fact]
        public void IsDisposalRequired_InvalidPart_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("partDefinition", () =>
            {
                ReflectionModelServices.IsDisposalRequired(new InvalidPartDefinition());
            });
        }

        [Fact]
        public void CreateExportDefinition()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            string expectedContractName = "Foo";

            ICompositionElement expectedOrigin = new MockOrigin();

            ExportDefinition exportDefinition = ReflectionModelServices.CreateExportDefinition(expectedLazyMember, expectedContractName, expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(exportDefinition);
            ReflectionMemberExportDefinition definition = exportDefinition as ReflectionMemberExportDefinition;
            Assert.NotNull(definition);

            Assert.Equal(expectedContractName, definition.ContractName);
            Assert.True(definition.Metadata.Keys.SequenceEqual(expectedMetadata.Keys));
            Assert.True(definition.Metadata.Values.SequenceEqual(expectedMetadata.Values));
            Assert.Equal(expectedOrigin, ((ICompositionElement)definition).Origin);
            Assert.Equal(expectedLazyMember, definition.ExportingLazyMember);
        }

        [Fact]
        public void CreateExportDefinition_NullAsContractName_ThrowsNullArgument()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            ICompositionElement expectedOrigin = new MockOrigin();

            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                ReflectionModelServices.CreateExportDefinition(expectedLazyMember, null, expectedMetadata.AsLazy(), expectedOrigin);
            });
        }

        [Fact]
        public void CreateExportDefinition_NullAsMetadata_Allowed()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            string expectedContractName = "Foo";
            ICompositionElement expectedOrigin = new MockOrigin();

            ExportDefinition definition = ReflectionModelServices.CreateExportDefinition(expectedLazyMember, expectedContractName, expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(definition.Metadata);
            Assert.Equal(2, definition.Metadata.Count);
        }

        [Fact]
        public void CreateExportDefinition_InvalidLazymemberInfo_ShouldThrowArtument()
        {
            EventInfo _event = typeof(TestPart).GetEvents().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(_event);

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            string expectedContractName = "Foo";

            ICompositionElement expectedOrigin = new MockOrigin();

            Assert.Throws<ArgumentException>("exportingMember", () =>
            {
                ReflectionModelServices.CreateExportDefinition(expectedLazyMember, expectedContractName, expectedMetadata.AsLazy(), expectedOrigin);
            });
        }

        [Fact]
        public void GetExportingMember()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            string expectedContractName = "Foo";

            ICompositionElement expectedOrigin = new MockOrigin();

            ExportDefinition exportDefinition = ReflectionModelServices.CreateExportDefinition(expectedLazyMember, expectedContractName, expectedMetadata.AsLazy(), expectedOrigin);
            Assert.NotNull(exportDefinition);

            LazyMemberInfo lazyMember = ReflectionModelServices.GetExportingMember(exportDefinition);
            Assert.Equal(expectedLazyMember, lazyMember);
        }

        [Fact]
        public void GetExportingMember_NullAsExportDefinition_ShouldThrowArhumentNull()
        {
            Assert.Throws<ArgumentNullException>("exportDefinition", () =>
            {
                ReflectionModelServices.GetExportingMember(null);
            });
        }

        [Fact]
        public void GetExportingMember_InvalidExportDefinition_ShouldThrowArhumentNull()
        {
            Assert.Throws<ArgumentException>("exportDefinition", () =>
            {
                ReflectionModelServices.GetExportingMember(new ExportDefinition("Foo", null));
            });
        }

        [Fact]
        public void CreateImportDefinition_Member()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;
            bool expectedRecomposable = true;

            ICompositionElement expectedOrigin = new MockOrigin();

            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyMember,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedRecomposable,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            ReflectionMemberImportDefinition definition = importDefinition as ReflectionMemberImportDefinition;
            Assert.NotNull(definition);

            Assert.Equal(expectedLazyMember, definition.ImportingLazyMember);
            Assert.Equal(definition.ContractName, expectedContractName);
            Assert.Equal(definition.RequiredTypeIdentity, expectedRequiredTypeIdentity);
            Assert.True(definition.RequiredMetadata.SequenceEqual(expectedRequiredMetadata));
            Assert.Equal(definition.Cardinality, expectedCardinality);
            Assert.Equal(definition.RequiredCreationPolicy, expectedCreationPolicy);
            Assert.Equal(definition.IsRecomposable, expectedRecomposable);
            Assert.Same(expectedOrigin, ((ICompositionElement)definition).Origin);
            Assert.False(definition.IsPrerequisite);
        }

        [Fact]
        public void CreateImportDefinition_Member_InvalidMember_ShouldThrowArgument()
        {
            MethodInfo method = typeof(TestPart).GetMethods().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(method);

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;
            bool expectedRecomposable = true;

            ICompositionElement expectedOrigin = new MockOrigin();

            Assert.Throws<ArgumentException>("importingMember", () =>
            {
                ReflectionModelServices.CreateImportDefinition(
                expectedLazyMember,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedRecomposable,
                expectedCreationPolicy,
                expectedOrigin);
            });
        }

        [Fact]
        public void GetImporingMember()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;
            bool expectedRecomposable = true;

            ICompositionElement expectedOrigin = new MockOrigin();

            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyMember,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedRecomposable,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            LazyMemberInfo lazyMember = ReflectionModelServices.GetImportingMember(importDefinition);
            Assert.Equal(expectedLazyMember, lazyMember);
        }

        [Fact]
        public void GetImporingMember_NullAsImport_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("importDefinition", () =>
            {
                ReflectionModelServices.GetImportingMember(null);
            });
        }

        [Fact]
        public void GetImporingMember_InvalidImport_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("importDefinition", () =>
            {
                ReflectionModelServices.GetImportingMember(new ContractBasedImportDefinition("Foo", "Foo", null, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any));
            });
        }

        [Fact]
        public void CreateImportDefinition_Parameter()
        {

            ParameterInfo parameter = typeof(TestPart).GetConstructor(new Type[] { typeof(int) }).GetParameters()[0];
            Lazy<ParameterInfo> expectedLazyParameter = parameter.AsLazy();

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;

            ICompositionElement expectedOrigin = new MockOrigin();

            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyParameter,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            ReflectionParameterImportDefinition definition = importDefinition as ReflectionParameterImportDefinition;
            Assert.NotNull(definition);

            Assert.Equal(expectedLazyParameter, definition.ImportingLazyParameter);
            Assert.Equal(definition.ContractName, expectedContractName);
            Assert.Equal(definition.RequiredTypeIdentity, expectedRequiredTypeIdentity);
            Assert.True(definition.RequiredMetadata.SequenceEqual(expectedRequiredMetadata));
            Assert.Equal(definition.Cardinality, expectedCardinality);
            Assert.Equal(definition.RequiredCreationPolicy, expectedCreationPolicy);
            Assert.False(definition.IsRecomposable);
            Assert.Same(expectedOrigin, ((ICompositionElement)definition).Origin);
            Assert.True(definition.IsPrerequisite);
        }

        [Fact]
        public void CreateImportDefinition_Parameter_NullAsParameter_ShouldThrowArgumentNull()
        {
            ParameterInfo parameter = typeof(TestPart).GetConstructor(new Type[] { typeof(int) }).GetParameters()[0];
            Lazy<ParameterInfo> expectedLazyParameter = parameter.AsLazy();

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;

            ICompositionElement expectedOrigin = new MockOrigin();

            Assert.Throws<ArgumentNullException>("parameter", () =>
            {
                ReflectionModelServices.CreateImportDefinition(
                                null,
                                expectedContractName,
                                expectedRequiredTypeIdentity,
                                expectedRequiredMetadata,
                                expectedCardinality,
                                expectedCreationPolicy,
                                expectedOrigin);
            });
        }

        [Fact]
        public void GetImportingParameter()
        {
            ParameterInfo parameter = typeof(TestPart).GetConstructor(new Type[] { typeof(int) }).GetParameters()[0];
            Lazy<ParameterInfo> expectedLazyParameter = parameter.AsLazy();

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;

            ICompositionElement expectedOrigin = new MockOrigin();
            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyParameter,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            Lazy<ParameterInfo> lazyParameter = ReflectionModelServices.GetImportingParameter(importDefinition);
            Assert.Equal(expectedLazyParameter, lazyParameter);
        }

        [Fact]
        public void GetImportingParameter_NullAsImport_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("importDefinition", () =>
            {
                ReflectionModelServices.GetImportingParameter(null);
            });
        }

        [Fact]
        public void GetImportingParameter_InvalidImport_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("importDefinition", () =>
            {
                ReflectionModelServices.GetImportingParameter(new ContractBasedImportDefinition("Foo", "Foo", null, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any));
            });
        }

        [Fact]
        public void IsImportingParameter_OnParameterImport()
        {
            ParameterInfo parameter = typeof(TestPart).GetConstructor(new Type[] { typeof(int) }).GetParameters()[0];
            Lazy<ParameterInfo> expectedLazyParameter = parameter.AsLazy();

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;

            ICompositionElement expectedOrigin = new MockOrigin();
            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyParameter,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            Assert.True(ReflectionModelServices.IsImportingParameter(importDefinition));
        }

        [Fact]
        public void IsImportingParameter_OnMemberImport()
        {
            PropertyInfo property = typeof(TestPart).GetProperties().First();
            LazyMemberInfo expectedLazyMember = new LazyMemberInfo(property);

            string expectedContractName = "Foo";
            string expectedRequiredTypeIdentity = "Bar";
            KeyValuePair<string, Type>[] expectedRequiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) };
            ImportCardinality expectedCardinality = ImportCardinality.ExactlyOne;
            CreationPolicy expectedCreationPolicy = CreationPolicy.NonShared;
            bool expectedRecomposable = true;

            ICompositionElement expectedOrigin = new MockOrigin();

            ImportDefinition importDefinition = ReflectionModelServices.CreateImportDefinition(
                expectedLazyMember,
                expectedContractName,
                expectedRequiredTypeIdentity,
                expectedRequiredMetadata,
                expectedCardinality,
                expectedRecomposable,
                expectedCreationPolicy,
                expectedOrigin);
            Assert.NotNull(importDefinition);

            Assert.False(ReflectionModelServices.IsImportingParameter(importDefinition));
        }

        [Fact]
        public void IsImportingParameter_NullAsImport_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("importDefinition", () =>
            {
                ReflectionModelServices.IsImportingParameter(null);
            });
        }

        [Fact]
        public void IsImportingParameter_InvalidImport_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("importDefinition", () =>
            {
                ReflectionModelServices.IsImportingParameter(new ContractBasedImportDefinition("Foo", "Foo", null, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any));
            });
        }

        [Fact]
        public void IsExportFactoryImportDefinition_NullImport_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("importDefinition", () =>
                ReflectionModelServices.IsExportFactoryImportDefinition(null));
        }

        [Fact]
        public void IsExportFactoryImportDefinition_InvalidImport_ShouldReturnFalse()
        {
            Assert.False(ReflectionModelServices.IsExportFactoryImportDefinition(CreateInvalidImport()));
        }

        [Fact]
        public void IsExportFactoryImportDefinition_NonPartCreatorImport_ShouldReturnFalse()
        {
            var import = ReflectionModelServices.CreateImportDefinition(
                new LazyMemberInfo(MemberTypes.Field, () => new MemberInfo[] { typeof(ReflectionModelServicesTests) }), // bogus member
                "Foo",
                "Foo",
                Enumerable.Empty<KeyValuePair<string, Type>>(),
                ImportCardinality.ZeroOrMore,
                false,
                CreationPolicy.Any,
                null);

            Assert.False(ReflectionModelServices.IsExportFactoryImportDefinition(import));
        }

        [Fact]
        public void IsExportFactoryImportDefinition_PartCreatorImport_ShouldReturnTrue()
        {
            var import = ReflectionModelServices.CreateImportDefinition(
                new LazyMemberInfo(MemberTypes.Field, () => new MemberInfo[] { typeof(ReflectionModelServicesTests) }), // bogus member
                "Foo",
                "Foo",
                Enumerable.Empty<KeyValuePair<string, Type>>(),
                ImportCardinality.ZeroOrMore,
                false,
                CreationPolicy.Any,
                MetadataServices.EmptyMetadata,
                true, //isPartCreator
                null);

            Assert.True(ReflectionModelServices.IsExportFactoryImportDefinition(import));
        }

        [Fact]
        public void GetExportFactoryProductImportDefinition_NullImport_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("importDefinition", () =>
                ReflectionModelServices.GetExportFactoryProductImportDefinition(null));
        }

        [Fact]
        public void GetExportFactoryProductImportDefinition_InvalidImport_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("importDefinition", () =>
                ReflectionModelServices.GetExportFactoryProductImportDefinition(CreateInvalidImport()));
        }

        [Fact]
        public void GetExportFactoryProductImportDefinition_()
        {

        }

        [Fact]
        public void GetExportFactoryProductImportDefinition_PartCreatorImport_()
        {
            LazyMemberInfo bogusMember = new LazyMemberInfo(MemberTypes.Field, () => new MemberInfo[] { typeof(ReflectionModelServicesTests) });
            var import = ReflectionModelServices.CreateImportDefinition(
                bogusMember,
                "Foo",
                "Foo",
                Enumerable.Empty<KeyValuePair<string, Type>>(),
                ImportCardinality.ZeroOrMore,
                false,
                CreationPolicy.Any,
                null,
                true, //isPartCreator
                null);

            var productImport = ReflectionModelServices.GetExportFactoryProductImportDefinition(import);

            var import2 = ReflectionModelServices.CreateImportDefinition(
                bogusMember,
                productImport.ContractName,
                productImport.RequiredTypeIdentity,
                productImport.RequiredMetadata,
                productImport.Cardinality,
                productImport.IsRecomposable,
                productImport.RequiredCreationPolicy,
                productImport.Metadata,
                true, //isPartCreator
                null);

            Assert.Equal(import.ContractName, import2.ContractName);
            Assert.Equal(import.Cardinality, import2.Cardinality);
            Assert.Equal(import.IsRecomposable, import2.IsRecomposable);
            Assert.Equal(import.RequiredCreationPolicy, import2.RequiredCreationPolicy);
            Assert.Equal(import.RequiredTypeIdentity, import2.RequiredTypeIdentity);
            EnumerableAssert.AreEqual(import.RequiredMetadata, import2.RequiredMetadata);
        }

        private static IEnumerable<ImportDefinition> CreateInvalidImports()
        {
            yield return new ContractBasedImportDefinition("Foo", "Foo", null, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any);
        }

        private static ImportDefinition CreateInvalidImport()
        {
            return new ContractBasedImportDefinition("Foo", "Foo", null, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any);
        }

        private static IEnumerable<ExportDefinition> CreateInvalidExports()
        {
            yield return new ExportDefinition("Foo", null);
        }

        class InvalidPartDefinition : ComposablePartDefinition
        {
            public override ComposablePart CreatePart()
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get { throw new NotImplementedException(); }
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get { throw new NotImplementedException(); }
            }
        }

        private static List<ImportDefinition> CreateImports(Type type)
        {
            List<ImportDefinition> imports = new List<ImportDefinition>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                imports.Add(new ReflectionMemberImportDefinition(new LazyMemberInfo(property), "Contract", (string)null, new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(string)), new KeyValuePair<string, Type>("Key2", typeof(int)) }, ImportCardinality.ZeroOrOne, true, false, CreationPolicy.Any, MetadataServices.EmptyMetadata, new TypeOrigin(type)));
            }

            return imports;
        }

        private static List<ExportDefinition> CreateExports(Type type)
        {
            List<ExportDefinition> exports = new List<ExportDefinition>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                exports.Add(ReflectionModelServices.CreateExportDefinition(new LazyMemberInfo(property), "Contract", new Lazy<IDictionary<string, object>>(() => null), new TypeOrigin(type)));
            }

            return exports;
        }

        public class TestPart
        {
            public TestPart(int arg1)
            {
            }

            public int field1;
            public string field2;
            public int Property1 { get; set; }
            public string Property2
            {
                get { return null; }
                set
                {
                    this.Event.Invoke(this, null);
                }
            }
            public event EventHandler Event;
        }

        private class TypeOrigin : ICompositionElement
        {
            private readonly Type _type;
            private readonly ICompositionElement _orgin;

            public TypeOrigin(Type type)
                : this(type, null)
            {
            }

            public TypeOrigin(Type type, ICompositionElement origin)
            {
                this._type = type;
                this._orgin = origin;
            }

            public string DisplayName
            {
                get
                {
                    return this._type.GetDisplayName();
                }
            }

            public ICompositionElement Origin
            {
                get
                {
                    return this._orgin;
                }
            }
        }

        private class MockOrigin : ICompositionElement
        {
            public string DisplayName
            {
                get { throw new NotImplementedException(); }
            }

            public ICompositionElement Origin
            {
                get { throw new NotImplementedException(); }
            }
        }

    }
}
