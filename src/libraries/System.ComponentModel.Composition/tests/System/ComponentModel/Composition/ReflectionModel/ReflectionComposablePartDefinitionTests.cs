// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class ReflectionComposablePartDefinitionTests
    {
        private ReflectionComposablePartDefinition CreateReflectionPartDefinition(
            Lazy<Type> partType,
            bool requiresDisposal,
            Func<IEnumerable<ImportDefinition>> imports,
            Func<IEnumerable<ExportDefinition>>exports,
            IDictionary<string, object> metadata,
            ICompositionElement origin)
        {
            return (ReflectionComposablePartDefinition)ReflectionModelServices.CreatePartDefinition(partType, requiresDisposal, 
                new Lazy<IEnumerable<ImportDefinition>>(imports, false), 
                new Lazy<IEnumerable<ExportDefinition>>(exports, false), 
                metadata.AsLazy(), origin);
        }

        [Fact]
        public void Constructor()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedLazyType,
                false,
                () => expectedImports,
                () => expectedExports,
                expectedMetadata,
                expectedOrigin);

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
        public void Constructor_DisposablePart()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedLazyType,
                true,
                () => expectedImports,
                () => expectedExports,
                expectedMetadata,
                expectedOrigin);

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
        public void CreatePart()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedLazyType,
                false,
                () => expectedImports,
                () => expectedExports,
                expectedMetadata,
                expectedOrigin);

            var part = definition.CreatePart();
            Assert.NotNull(part);
            Assert.False(part is IDisposable);
        }

        [Fact]
        public void CreatePart_Disposable()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = expectedType.AsLazy();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedLazyType,
                true,
                () => expectedImports,
                () => expectedExports,
                expectedMetadata,
                expectedOrigin);

            var part = definition.CreatePart();
            Assert.NotNull(part);
            Assert.True(part is IDisposable);
        }

        [Fact]
        public void CreatePart_DoesntLoadType()
        {
            Type expectedType = typeof(TestPart);
            Lazy<Type> expectedLazyType = new Lazy<Type>(() => { throw new NotImplementedException(); /*"Part should not be loaded" */ });
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>();
            expectedMetadata["Key1"] = 1;
            expectedMetadata["Key2"] = "Value2";

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            ICompositionElement expectedOrigin = new MockOrigin();

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedLazyType,
                true,
                () => expectedImports,
                () => expectedExports,
                expectedMetadata,
                expectedOrigin);

            var part = definition.CreatePart();
            Assert.NotNull(part);
            Assert.True(part is IDisposable);
        }

        [Fact]
        public void Constructor_NullMetadata_ShouldSetMetadataPropertyToEmpty()
        {
            ReflectionComposablePartDefinition definition = CreateEmptyDefinition(typeof(object), typeof(object).GetConstructors().First(), null, new MockOrigin());
            Assert.NotNull(definition.Metadata);
            Assert.Equal(0, definition.Metadata.Count);
        }

        [Fact]
        public void Constructor_NullOrigin_ShouldSetOriginPropertyToNull()
        {
            ReflectionComposablePartDefinition definition = CreateEmptyDefinition(typeof(object), typeof(object).GetConstructors().First(), MetadataServices.EmptyMetadata, null);
            Assert.NotNull(((ICompositionElement)definition).DisplayName);
            Assert.Null(((ICompositionElement)definition).Origin);
        }

        [Fact]
        public void ImportaAndExports_CreatorsShouldBeCalledLazilyAndOnce()
        {
            Type expectedType = typeof(TestPart);

            IEnumerable<ImportDefinition> expectedImports = CreateImports(expectedType);
            IEnumerable<ExportDefinition> expectedExports = CreateExports(expectedType);

            bool importsCreatorCalled = false;
            Func<IEnumerable<ImportDefinition>> importsCreator = () =>
            {
                Assert.False(importsCreatorCalled);
                importsCreatorCalled = true;
                return expectedImports.Cast<ImportDefinition>();
            };

            bool exportsCreatorCalled = false;
            Func<IEnumerable<ExportDefinition>> exportsCreator = () =>
            {
                Assert.False(exportsCreatorCalled);
                exportsCreatorCalled = true;
                return expectedExports.Cast<ExportDefinition>();
            };

            ReflectionComposablePartDefinition definition = CreateReflectionPartDefinition(
                expectedType.AsLazy(),
                false,
                importsCreator,
                exportsCreator,
                null,
                null);

            IEnumerable<ExportDefinition> exports;
            Assert.False(exportsCreatorCalled);
            exports = definition.ExportDefinitions;
            Assert.True(exportsCreatorCalled);
            exports = definition.ExportDefinitions;

IEnumerable<ImportDefinition> imports;
            Assert.False(importsCreatorCalled);
            imports = definition.ImportDefinitions;
            Assert.True(importsCreatorCalled);
            imports = definition.ImportDefinitions;
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ICompositionElementDisplayName_ShouldReturnTypeDisplayName()
        {
            var expectations = Expectations.GetAttributedTypes();
            foreach (var e in expectations)
            {
                var definition = (ICompositionElement)CreateEmptyDefinition(e, null, null, null);

                Assert.Equal(e.GetDisplayName(), definition.DisplayName);
            }
        }

        private ReflectionComposablePartDefinition CreateEmptyDefinition(Type type, ConstructorInfo constructor, IDictionary<string, object> metadata, ICompositionElement origin)
        {
            return (ReflectionComposablePartDefinition)ReflectionModelServices.CreatePartDefinition(
                (type != null) ? type.AsLazy() : null,
                false,
                Enumerable.Empty<ImportDefinition>().AsLazy(),
                Enumerable.Empty<ExportDefinition>().AsLazy(),
                metadata.AsLazy(),
                origin);
        }

        private static List<ImportDefinition> CreateImports(Type type)
        {
            List<ImportDefinition> imports = new List<ImportDefinition>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                imports.Add(new ReflectionMemberImportDefinition(new LazyMemberInfo(property), "Contract", (string)null, new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Key1", typeof(object)) }, ImportCardinality.ZeroOrOne, true, false, CreationPolicy.Any, MetadataServices.EmptyMetadata, new TypeOrigin(type)));
            }

            return imports;
        }

        private static List<ExportDefinition> CreateExports(Type type)
        {
            List<ExportDefinition> exports = new List<ExportDefinition>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                exports.Add(ReflectionModelServices.CreateExportDefinition(new LazyMemberInfo(property), "Contract", new Lazy<IDictionary<string, object>>(() => null, false), new TypeOrigin(type)));
            }

            return exports;
        }

        public class TestPart
        {
            public int field1;
            public string field2;
            public int Property1 { get; set; }
            public string Property2 { get; set; }
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
