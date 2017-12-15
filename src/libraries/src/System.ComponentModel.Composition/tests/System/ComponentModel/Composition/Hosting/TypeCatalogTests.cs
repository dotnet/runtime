// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    [Export]
    public class TypeCatalogTestsExporter { }

    // This is a glorious do nothing ReflectionContext
    public class TypeCatalogTestsReflectionContext : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly assembly)
        {
            return assembly;
        }

        public override TypeInfo MapType(TypeInfo type)
        {
            return type;
        }
    }

    public class TypeCatalogTests
    {
        public static void Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull(Func<ReflectionContext, TypeCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentNullException>("reflectionContext", () =>
            {
                var catalog = catalogCreator(null);
            });
        }

        public static void Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull(Func<ICompositionElement, TypeCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentNullException>("definitionOrigin", () =>
            {
                var catalog = catalogCreator(null);
            });
        }

        [Fact]
        public void Constructor1_ReflectOnlyTypes_ShouldThrowArgumentNull()
        {
            TypeCatalogTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new TypeCatalog(new Type[0], rc);
            });
        }

        [Fact]
        public void Constructor3_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            TypeCatalogTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new TypeCatalog(new Type[0], dO);
            });
        }

        [Fact]
        public void Constructor4_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            TypeCatalogTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new TypeCatalog(new Type[0], rc, new TypeCatalog(new Type[0]));
            });
        }

        [Fact]
        public void Constructor4_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            TypeCatalogTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new TypeCatalog(new Type[0], new TypeCatalogTestsReflectionContext(), dO);
            });
        }

        [Fact]
        public void Constructor2_NullAsTypesArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("types", () =>
            {
                new TypeCatalog((Type[])null);
            });
        }

        [Fact]
        public void Constructor3_NullAsTypesArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("types", () =>
            {
                new TypeCatalog((IEnumerable<Type>)null);
            });
        }

        [Fact]
        public void Constructor2_ArrayWithNullAsTypesArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("types", () =>
            {
                new TypeCatalog(new Type[] { null });
            });
        }

        [Fact]
        public void Constructor3_ArrayWithNullAsTypesArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("types", () =>
            {
                new TypeCatalog((IEnumerable<Type>)new Type[] { null });
            });
        }

        [Fact]
        public void Constructor2_EmptyEnumerableAsTypesArgument_ShouldSetPartsPropertyToEmptyEnumerable()
        {
            var catalog = new TypeCatalog(Enumerable.Empty<Type>());

            Assert.Empty(catalog.Parts);
        }

        [Fact]
        public void Constructor3_EmptyArrayAsTypesArgument_ShouldSetPartsPropertyToEmpty()
        {
            var catalog = new TypeCatalog(new Type[0]);

            Assert.Empty(catalog.Parts);
        }

        [Fact]
        public void Constructor2_ArrayAsTypesArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var types = new Type[] { PartFactory.GetAttributedExporterType() };
            var catalog = new TypeCatalog(types);

            types[0] = null;

            Assert.NotNull(catalog.Parts.First());
        }

        [Fact]
        public void Constructor3_ArrayAsTypesArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var types = new Type[] { PartFactory.GetAttributedExporterType() };
            var catalog = new TypeCatalog((IEnumerable<Type>)types);

            types[0] = null;

            Assert.NotNull(catalog.Parts.First());
        }

        [Fact]
        public void Constructor2_ShouldSetOriginToNull()
        {
            var catalog = (ICompositionElement)new TypeCatalog(PartFactory.GetAttributedExporterType());

            Assert.Null(catalog.Origin);
        }

        [Fact]
        public void Constructor3_ShouldSetOriginToNull()
        {
            var catalog = (ICompositionElement)new TypeCatalog((IEnumerable<Type>)new Type[] { PartFactory.GetAttributedExporterType() });

            Assert.Null(catalog.Origin);
        }

        [Fact]
        public void DisplayName_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();

            var displayName = ((ICompositionElement)catalog).DisplayName;
        }

        [Fact]
        public void Origin_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();

            var origin = ((ICompositionElement)catalog).Origin;
        }

        [Fact]
        public void Parts_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var parts = catalog.Parts;
            });
        }

        [Fact]
        public void ToString_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();

            catalog.ToString();
        }

        [Fact]
        public void GetExports_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();
            var definition = ImportDefinitionFactory.Create();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                catalog.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports_NullAsConstraintArgument_ShouldThrowArgumentNull()
        {
            var catalog = CreateTypeCatalog();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                catalog.GetExports((ImportDefinition)null);
            });
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            using (var catalog = CreateTypeCatalog())
            {
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var catalog = CreateTypeCatalog();
            catalog.Dispose();
            catalog.Dispose();
            catalog.Dispose();
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Parts()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            Assert.NotNull(catalog.Parts);
            Assert.True(catalog.Parts.Count() > 0);
        }

        [Fact]
        public void Parts_ShouldSetDefinitionOriginToCatalogItself()
        {
            var catalog = CreateTypeCatalog();
            Assert.True(catalog.Parts.Count() > 0);

            foreach (ICompositionElement definition in catalog.Parts)
            {
                Assert.Same(catalog, definition.Origin);
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ICompositionElementDisplayName_SingleTypeAsTypesArgument_ShouldIncludeCatalogTypeNameAndTypeFullName()
        {
            var expectations = Expectations.GetAttributedTypes();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)CreateTypeCatalog(e);

                string expected = string.Format(SR.TypeCatalog_DisplayNameFormat, typeof(TypeCatalog).Name, AttributedModelServices.GetTypeIdentity(e));

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        public void ICompositionElementDisplayName_ValueAsTypesArgument_ShouldIncludeCatalogTypeNameAndTypeFullNames()
        {
            var expectations = new ExpectationCollection<Type[], string>();
            expectations.Add(new Type[] { typeof(Type) },
                             GetDisplayName(false, typeof(TypeCatalog)));

            expectations.Add(new Type[] { typeof(ExportValueTypeSingleton) },
                             GetDisplayName(false, typeof(TypeCatalog), typeof(ExportValueTypeSingleton)));

            expectations.Add(new Type[] { typeof(ExportValueTypeSingleton), typeof(ExportValueTypeSingleton) },
                             GetDisplayName(false, typeof(TypeCatalog), typeof(ExportValueTypeSingleton), typeof(ExportValueTypeSingleton)));

            expectations.Add(new Type[] { typeof(ExportValueTypeSingleton), typeof(string), typeof(ExportValueTypeSingleton) },
                             GetDisplayName(false, typeof(TypeCatalog), typeof(ExportValueTypeSingleton), typeof(ExportValueTypeSingleton)));

            expectations.Add(new Type[] { typeof(ExportValueTypeSingleton), typeof(ExportValueTypeFactory) },
                             GetDisplayName(false, typeof(TypeCatalog), typeof(ExportValueTypeSingleton), typeof(ExportValueTypeFactory)));

            expectations.Add(new Type[] { typeof(ExportValueTypeSingleton), typeof(ExportValueTypeFactory), typeof(CallbackExecuteCodeDuringCompose) },
                             GetDisplayName(true, typeof(TypeCatalog), typeof(ExportValueTypeSingleton), typeof(ExportValueTypeFactory)));

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)CreateTypeCatalog(e.Input);

                Assert.Equal(e.Output, catalog.DisplayName);
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ICompositionElementDisplayName_ShouldIncludeDerivedCatalogTypeNameAndTypeFullNames()
        {
            var expectations = Expectations.GetAttributedTypes();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)new DerivedTypeCatalog(e);

                string expected = string.Format(SR.TypeCatalog_DisplayNameFormat, typeof(DerivedTypeCatalog).Name, AttributedModelServices.GetTypeIdentity(e));

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ToString_ShouldReturnICompositionElementDisplayName()
        {
            var expectations = Expectations.GetAttributedTypes();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)CreateTypeCatalog(e);

                Assert.Equal(catalog.DisplayName, catalog.ToString());
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void GetExports()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            Expression<Func<ExportDefinition, bool>> constraint = (ExportDefinition exportDefinition) => exportDefinition.ContractName == AttributedModelServices.GetContractName(typeof(MyExport));
            IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> matchingExports = catalog.GetExports(constraint);
            Assert.NotNull(matchingExports);
            Assert.True(matchingExports.Count() >= 0);

            IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> expectedMatchingExports = catalog.Parts
                .SelectMany(part => part.ExportDefinitions, (part, export) => new Tuple<ComposablePartDefinition, ExportDefinition>(part, export))
                .Where(partAndExport => partAndExport.Item2.ContractName == AttributedModelServices.GetContractName(typeof(MyExport)));
            Assert.True(matchingExports.SequenceEqual(expectedMatchingExports));
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TwoTypesWithSameSimpleName()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);

            NotSoUniqueName unique1 = container.GetExportedValue<NotSoUniqueName>();

            Assert.NotNull(unique1);

            Assert.Equal(23, unique1.MyIntProperty);

            NotSoUniqueName2.NotSoUniqueName nestedUnique = container.GetExportedValue<NotSoUniqueName2.NotSoUniqueName>();

            Assert.NotNull(nestedUnique);

            Assert.Equal("MyStringProperty", nestedUnique.MyStringProperty);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void GettingFunctionExports()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);

            ImportDefaultFunctions import = container.GetExportedValue<ImportDefaultFunctions>("ImportDefaultFunctions");
            import.VerifyIsBound();
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void AnExportOfAnInstanceThatFailsToCompose()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);

            // Rejection causes the part in the catalog whose imports cannot be
            // satisfied to be ignored, resulting in a cardinality mismatch instead of a
            // composition exception
            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>("ExportMyString");
            });
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void SharedPartCreation()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new Int32Exporter(41));
            container.Compose(batch);

            var sharedPart1 = container.GetExportedValue<MySharedPartExport>();
            Assert.Equal(41, sharedPart1.Value);
            var sharedPart2 = container.GetExportedValue<MySharedPartExport>();
            Assert.Equal(41, sharedPart2.Value);

            Assert.Equal(sharedPart1, sharedPart2);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void NonSharedPartCreation()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new Int32Exporter(41));
            container.Compose(batch);

            var nonSharedPart1 = container.GetExportedValue<MyNonSharedPartExport>();
            Assert.Equal(41, nonSharedPart1.Value);
            var nonSharedPart2 = container.GetExportedValue<MyNonSharedPartExport>();
            Assert.Equal(41, nonSharedPart2.Value);

            Assert.NotEqual(nonSharedPart1, nonSharedPart2);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void RecursiveNonSharedPartCreation()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<DirectCycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart1>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart2>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleWithSharedPartAndNonSharedPart>();
            });

            Assert.NotNull(container.GetExportedValue<CycleSharedPart>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart1>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart2>());
            Assert.NotNull(container.GetExportedValue<NoCycleNonSharedPart>());
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TryToDiscoverExportWithGenericParameter()
        {
            var catalog = new TypeCatalog(Assembly.GetExecutingAssembly().GetTypes());
            var container = new CompositionContainer(catalog);

            // Should find a type that inherits from an export
            Assert.NotNull(container.GetExportedValueOrDefault<object>(AttributedModelServices.GetContractName(typeof(ExportWhichInheritsFromGeneric))));

            // This should be exported because it is inherited by ExportWhichInheritsFromGeneric
            Assert.NotNull(container.GetExportedValueOrDefault<object>(AttributedModelServices.GetContractName(typeof(ExportWithGenericParameter<string>))));
        }

        private string GetDisplayName(bool useEllipses, Type catalogType, params Type[] types)
        {
            return String.Format(CultureInfo.CurrentCulture,
                    SR.TypeCatalog_DisplayNameFormat,
                    catalogType.Name,
                    this.GetTypesDisplay(useEllipses, types));
        }

        private string GetTypesDisplay(bool useEllipses, Type[] types)
        {
            int count = types.Length;
            if (count == 0)
            {
                return SR.TypeCatalog_Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (Type type in types)
            {
                if (builder.Length > 0)
                {
                    builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    builder.Append(" ");
                }

                builder.Append(type.FullName);
            }

            if (useEllipses)
            {   // Add an elipse to indicate that there 
                // are more types than actually listed
                builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                builder.Append(" ...");
            }

            return builder.ToString();
        }

        private TypeCatalog CreateTypeCatalog()
        {
            var type = PartFactory.GetAttributedExporterType();

            return CreateTypeCatalog(type);
        }

        private TypeCatalog CreateTypeCatalog(params Type[] types)
        {
            return new TypeCatalog(types);
        }

        private class DerivedTypeCatalog : TypeCatalog
        {
            public DerivedTypeCatalog(params Type[] types)
                : base(types)
            {
            }
        }
    }
}
