// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace Tests.Integration
{
    public class ConstructorInjectionTests
    {
        [Fact]
        public void SimpleConstructorInjection()
        {
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(PartFactory.CreateAttributed(typeof(SimpleConstructorInjectedObject)));
            batch.AddExportedValue("CISimpleValue", 42);
            container.Compose(batch);

            SimpleConstructorInjectedObject simple = container.GetExportedValue<SimpleConstructorInjectedObject>();

            Assert.Equal(42, simple.CISimpleValue);
        }

        public interface IOptionalRef { }

        [Export]
        public class OptionalExportProvided { }

        [Export]
        public class AWithOptionalParameter
        {
            [ImportingConstructor]
            public AWithOptionalParameter([Import(AllowDefault = true)]IOptionalRef import,
                [Import("ContractThatShouldNotBeFound", AllowDefault = true)]int value,
                [Import(AllowDefault=true)]OptionalExportProvided provided)
            {
                Assert.Null(import);
                Assert.Equal(0, value);
                Assert.NotNull(provided);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // Actual:   typeof(System.Reflection.ReflectionTypeLoadException): Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void OptionalConstructorArgument()
        {
            var container = GetContainerWithCatalog();
            var a = container.GetExportedValue<AWithOptionalParameter>();

            // A should verify that it receieved optional arguments properly
            Assert.NotNull(a);
        }

        [Export]
        public class AWithCollectionArgument
        {
            private IEnumerable<int> _values;

            [ImportingConstructor]
            public AWithCollectionArgument([ImportMany("MyConstructorCollectionItem")]IEnumerable<int> values)
            {
                this._values = values;
            }

            public IEnumerable<int> Values { get { return this._values; } }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // Actual:   typeof(System.Reflection.ReflectionTypeLoadException): Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void RebindingShouldNotHappenForConstructorArguments()
        {
            var container = GetContainerWithCatalog();
            CompositionBatch batch = new CompositionBatch();

            var p1 = batch.AddExportedValue("MyConstructorCollectionItem", 1);
            batch.AddExportedValue("MyConstructorCollectionItem", 2);
            batch.AddExportedValue("MyConstructorCollectionItem", 3);
            container.Compose(batch);

            var a = container.GetExportedValue<AWithCollectionArgument>();
            Assert.Equal(a.Values, new int[3] { 1, 2, 3 });

            batch = new CompositionBatch();
            batch.AddExportedValue("MyConstructorCollectionItem", 4);
            batch.AddExportedValue("MyConstructorCollectionItem", 5);
            batch.AddExportedValue("MyConstructorCollectionItem", 6);
            // After rejection changes that are incompatible with existing assumptions are no
            // longer silently ignored.  The batch attempting to make this change is rejected
            // with a ChangeRejectedException
            Assert.Throws<ChangeRejectedException>(() =>
            {
                container.Compose(batch);
            });

            // The collection which is a constructor import should not be rebound
            Assert.Equal(a.Values, new int[3] { 1, 2, 3 });

            batch.RemovePart(p1);
            // After rejection changes that are incompatible with existing assumptions are no
            // longer silently ignored.  The batch attempting to make this change is rejected
            // with a ChangeRejectedException
            Assert.Throws<ChangeRejectedException>(() =>
            {
                container.Compose(batch);
            });

            // The collection which is a constructor import should not be rebound
            Assert.Equal(a.Values, new int[3] { 1, 2, 3 });
        }

        [Fact]
        public void MissingConstructorArgsWithAlreadyCreatedInstance()
        {
            var container = GetContainerWithCatalog();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new ClassWithNotFoundConstructorArgs(21));
            container.Compose(batch);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // Actual:   typeof(System.Reflection.ReflectionTypeLoadException): Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void MissingConstructorArgsWithTypeFromCatalogMissingArg()
        {
            var container = GetContainerWithCatalog();

            // After rejection part definitions in catalogs whose dependencies cannot be
            // satisfied are now silently ignored, turning this into a cardinality
            // exception for the GetExportedValue call
            Assert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<ClassWithNotFoundConstructorArgs>();
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // Actual:   typeof(System.Reflection.ReflectionTypeLoadException): Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void MissingConstructorArgsWithWithTypeFromCatalogWithArg()
        {
            var container = GetContainerWithCatalog();
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("ContractThatDoesntExist", 21);
            container.Compose(batch);

            Assert.True(container.IsPresent<ClassWithNotFoundConstructorArgs>());
        }

        [Export]
        public class ClassWithNotFoundConstructorArgs
        {
            [ImportingConstructor]
            public ClassWithNotFoundConstructorArgs([Import("ContractThatDoesntExist")]int i)
            {
            }
        }

        private CompositionContainer GetContainerWithCatalog()
        {
            var catalog = new AssemblyCatalog(typeof(ConstructorInjectionTests).Assembly);

            return new CompositionContainer(catalog);
        }

        [Export]
        public class InvalidImportManyCI
        {
            [ImportingConstructor]
            public InvalidImportManyCI(
                [ImportMany]List<MyExport> exports)
            {
            }
        }

        [Fact]
        public void ImportMany_ConstructorParameter_OnNonAssiganbleType_ShouldThrowCompositionException()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(InvalidImportManyCI));

            Assert.Throws<CompositionException>(
                () => container.GetExportedValue<InvalidImportManyCI>());
        }
    }
}
