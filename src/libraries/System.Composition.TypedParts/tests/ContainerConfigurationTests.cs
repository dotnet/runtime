// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Composition.Convention;
using System.Composition.Hosting.Core;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Composition.Hosting.Tests
{
    public class ContainerConfigurationTests
    {
        [Fact]
        public void WithProvider_ValidProvider_RegistersProvider()
        {
            var configuration = new ContainerConfiguration();

            var provider = new ExportProvider { Result = 10 };
            Assert.Same(configuration, configuration.WithProvider(provider));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal(0, provider.CalledGetExportDescriptors);
            Assert.Equal(0, provider.CalledGetExportDescriptors);

            Assert.Equal(10, container.GetExport<int>());
            Assert.Equal(1, provider.CalledGetExportDescriptors);
            Assert.Equal(1, provider.CalledGetExportDescriptors);
        }

        public class ExportProvider : ExportDescriptorProvider
        {
            public object Result { get; set; }
            public int CalledGetExportDescriptors { get; set; }
            public int CalledCompositeActivator { get; set; }

            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                CalledGetExportDescriptors++;
                return new[]
                {
                    new ExportDescriptorPromise(contract, "origin", false, () => new CompositionDependency[0], dependencies => ExportDescriptor.Create(CompositeActivator, new Dictionary<string, object>()))
                };
            }

            public object CompositeActivator(LifetimeContext context, CompositionOperation operation)
            {
                CalledCompositeActivator++;
                return Result;
            }
        }

        [Fact]
        public void WithProvider_NullProvider_ThrowsArgumentNullException()
        {
            var configuration = new ContainerConfiguration();
            AssertExtensions.Throws<ArgumentNullException>("exportDescriptorProvider", () => configuration.WithProvider(null));
        }

        [Fact]
        public void WithDefaultConventions_PartWithNoMatchingConvention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            configuration.WithPart(typeof(ExportedProperty));
            Assert.Same(configuration, configuration.WithDefaultConventions(conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithDefaultConventions_IEnumerablePartsWithNoMatchingConvention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            configuration.WithParts((IEnumerable<Type>)new Type[] { typeof(ExportedProperty) });
            Assert.Same(configuration, configuration.WithDefaultConventions(conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithDefaultConventions_PartsArrayWithNoMatchingConvention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            configuration.WithParts(new Type[] { typeof(ExportedProperty) });
            Assert.Same(configuration, configuration.WithDefaultConventions(conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithDefaultConventions_PartTNoMatchingConvention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            configuration.WithPart<ExportedProperty>();
            Assert.Same(configuration, configuration.WithDefaultConventions(conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        public class ExportedProperty
        {
            [Export]
            public string Property => "A";
        }

        [Fact]
        public void WithDefaultConventions_NullConventions_ThrowsArgumentNullException()
        {
            var configuration = new ContainerConfiguration();
            AssertExtensions.Throws<ArgumentNullException>("conventions", () => configuration.WithDefaultConventions(null));
        }

        [Fact]
        public void WithDefaultConventions_AlreadyHasDefaultConventions_ThrowsInvalidOperationException()
        {
            var configuration = new ContainerConfiguration();
            configuration.WithDefaultConventions(new ConventionBuilder());
            Assert.Throws<InvalidOperationException>(() => configuration.WithDefaultConventions(new ConventionBuilder()));
        }

        [Fact]
        public void WithPartT_Convention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithPart<ExportedProperty>(conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithPart_Convention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithPart(typeof(ExportedProperty), conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithPart_NullPartType_ThrowsArgumentNullException()
        {
            var configuration = new ContainerConfiguration();
            AssertExtensions.Throws<ArgumentNullException>("partType", () => configuration.WithPart(null));
            AssertExtensions.Throws<ArgumentNullException>("partType", () => configuration.WithPart(null, new ConventionBuilder()));
        }

        [Fact]
        public void WithParts_Convention_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithParts(new Type[] { typeof(ExportedProperty) }, conventions));

            CompositionHost container = configuration.CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        [Fact]
        public void WithParts_NullPartTypes_ThrowsArgumentNullException()
        {
            var configuration = new ContainerConfiguration();
            AssertExtensions.Throws<ArgumentNullException>("partTypes", () => configuration.WithParts(null));
            AssertExtensions.Throws<ArgumentNullException>("partTypes", () => configuration.WithParts((IEnumerable<Type>)null));
            AssertExtensions.Throws<ArgumentNullException>("partTypes", () => configuration.WithParts(null, new ConventionBuilder()));
        }

        [Fact]
        public void WithParts_NullItemInPartTypes_ThrowsArgumentNullExceptionOnCreation()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(new Type[] { null });
            AssertExtensions.Throws<ArgumentNullException>("type", () => configuration.CreateContainer());
        }

        [Fact]
        public void WithAssembly_Assembly_ThrowsCompositionFailedExceptionOnCreation()
        {
            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithAssembly(typeof(ExportedProperty).Assembly));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Fact]
        public void WithAssembly_AssemblyConventions_ThrowsCompositionFailedExceptionOnCreation()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithAssembly(typeof(ExportedProperty).Assembly, conventions));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Fact]
        public void WithAssemblies_Assemblies_ThrowsCompositionFailedExceptionOnCreation()
        {
            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithAssemblies(new Assembly[] { typeof(ExportedProperty).Assembly }));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Fact]
        public void WithAssemblies_AssembliesAndConvention_ThrowsCompositionFailedExceptionOnCreation()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<ExportedProperty>().ExportProperty(b => b.Property);

            var configuration = new ContainerConfiguration();
            Assert.Same(configuration, configuration.WithAssemblies(new Assembly[] { typeof(ExportedProperty).Assembly }, conventions));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Fact]
        public void WithAssemblies_NullAssemblies_ThrowsArgumentNullException()
        {
            var configuration = new ContainerConfiguration();
            AssertExtensions.Throws<ArgumentNullException>("assemblies", () => configuration.WithAssemblies(null));
            AssertExtensions.Throws<ArgumentNullException>("assemblies", () => configuration.WithAssemblies(null, new ConventionBuilder()));
        }

        [Fact]
        public void WithAssemby_Null_ThrowsNullReferenceExceptionOnCreation()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithAssembly(null);
            Assert.Throws<NullReferenceException>(() => configuration.CreateContainer());
        }

        [Fact]
        public void CreateContainer_ExportedSubClass_Success()
        {
            CompositionHost container = new ContainerConfiguration()
                .WithPart(typeof(Derived))
                .CreateContainer();
            Assert.Equal("Derived", container.GetExport<Derived>().Prop);
        }

        [Export]
        public class Derived : Base
        {
            public new string Prop { get; set; } = "Derived";
        }

        public class Base
        {
            public object Prop { get; set; } = "Derived";
        }

        [Fact]
        public void CreateContainer_OpenGenericTypes_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<IContainer>()
                .Export<IContainer>();
            conventions.ForTypesDerivedFrom(typeof(IRepository<>))
                .Export(t => t.AsContractType(typeof(IRepository<>)));

            CompositionHost container = new ContainerConfiguration()
                .WithParts(new Type[] { typeof(EFRepository<>), typeof(Container) }, conventions)
                .CreateContainer();
            Assert.Equal(0, container.GetExport<IRepository<int>>().Fetch());
        }

        public interface IContainer { }
        public class Container : IContainer { }

        public interface IRepository<T>
        {
            T Fetch();
        }

        public class EFRepository<T> : IRepository<T>
        {
            public EFRepository(IContainer test) { }
            public T Fetch() => default(T);
        }

        [Fact]
        public void CreateContainer_ImportConventionsWithInheritedProperties_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<Imported>().Export();
            conventions.ForType<DerivedFromBaseWithImport>()
                .ImportProperty(b => b.Imported)
                .Export();

            CompositionHost container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(Imported), typeof(DerivedFromBaseWithImport))
                .CreateContainer();

            DerivedFromBaseWithImport export = container.GetExport<DerivedFromBaseWithImport>();
            Assert.IsAssignableFrom<Imported>(export.Imported);
        }

        public class Imported { }

        public class BaseWithImport
        {
            public virtual Imported Imported { get; set; }
        }

        public class DerivedFromBaseWithImport : BaseWithImport { }

        [Fact]
        public void CreateContainer_ExportConventionsWithInheritedProperties_Success()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<DerivedFromBaseWithExport>()
                .ExportProperty(b => b.Exported);

            CompositionHost container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(DerivedFromBaseWithExport))
                .CreateContainer();
            Assert.Equal("A", container.GetExport<string>());
        }

        public class BaseWithExport
        {
            public string Exported { get { return "A"; } }
        }

        public class DerivedFromBaseWithExport : BaseWithExport { }

        [Fact]
        public void CreateContainer_ExportsToInheritedProperties_DontInterfereWithBase()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<DerivedFromBaseWithExport2>()
                .ExportProperty(b => b.Exported);

            CompositionHost container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(BaseWithExport2))
                .WithParts(typeof(DerivedFromBaseWithExport2))
                .CreateContainer();
            Assert.Equal(new string[] { "A", "A" }, container.GetExports<string>());
        }

        public class BaseWithExport2
        {
            [Export]
            public virtual string Exported => "A";
        }

        public class DerivedFromBaseWithExport2 : BaseWithExport { }

        [Fact]
        public void CreateContainer_HasConventions_ClassExportsAreNotInherited()
        {
            CompositionHost container = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithDeclaredExports>(new ConventionBuilder())
                .CreateContainer();
            Assert.False(container.TryGetExport(out BaseWithDeclaredExports export));
        }

        [Fact]
        public void CreateContainer_HasConventions_PropertyExportsAreNotInherited()
        {
            CompositionHost container = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithDeclaredExports>(new ConventionBuilder())
                .CreateContainer();
            Assert.False(container.TryGetExport(out string export));
        }

        [Export]
        public class BaseWithDeclaredExports
        {
            public BaseWithDeclaredExports() => Property = "foo";

            [Export]
            public string Property { get; set; }
        }

        public class DerivedFromBaseWithDeclaredExports : BaseWithDeclaredExports { }

        public class CustomExport : ExportAttribute { }

        [Fact]
        public void CreateContainer_HasConventions_CustomAttributesAreNotInherited()
        {
            CompositionHost container = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithCustomExport>(new ConventionBuilder())
                .CreateContainer();
            Assert.False(container.TryGetExport(out BaseWithCustomExport bce));
        }

        [CustomExport]
        public class BaseWithCustomExport { }

        public class DerivedFromBaseWithCustomExport : BaseWithCustomExport { }

        [Fact]
        public void CreateContainer_OpenGenericTypePart_Success()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(typeof(GenericExportedType<>));
            CompositionHost container = configuration.CreateContainer();

            Assert.Equal("C", container.GetExport<GenericExportedType<int>>().Property);
        }

        [Export(typeof(GenericExportedType<>))]
        public class GenericExportedType<T>
        {
            public string Property => "C";
        }

        [Theory]
        [InlineData(typeof(IncompatibleGenericExportedType<>))]
        [InlineData(typeof(IncompatibleGenericExportedType<int>))]
        [InlineData(typeof(IncompatibleGenericExportedTypeDerived<>))]
        public void CreateContainer_GenericTypeExport_ThrowsCompositionFailedException(Type partType)
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(partType);
            Assert.Throws<CompositionFailedException>(  () => configuration.CreateContainer());
        }

        [Export(typeof(GenericExportedType<>))]
        public class IncompatibleGenericExportedType<T> { }

        [Export(typeof(GenericExportedType<>))]
        public class IncompatibleGenericExportedTypeDerived<T> : GenericExportedType<int> { }

        [Theory]
        [InlineData(typeof(NonGenericExportedType<>))]
        [InlineData(typeof(NonGenericExportedType<int>))]
        public void CreateContainer_NonGenericTypeExportWithGenericPart_ThrowsCompositionFailedException(Type partType)
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(partType);
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Export(typeof(string))]
        public class NonGenericExportedType<T> { }

        [Fact]
        public void CreateContainer_UnassignableType_ThrowsCompositionFailedException()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(typeof(ContractExportedType));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        [Export(typeof(Derived))]
        public class ContractExportedType { }

        [Fact]
        public void CreateContainer_AbstractOrStructType_Success()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithParts(typeof(AbstractClass), typeof(StructType));
            CompositionHost container = configuration.CreateContainer();

            Assert.Throws<CompositionFailedException>(() => container.GetExport<AbstractClass>());
            Assert.Throws<CompositionFailedException>(() => container.GetExport<StructType>());
        }

        public abstract class AbstractClass { }
        public struct StructType { }

        [Fact]
        public void CreateContainer_MetadataProperty_Success()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithPart(typeof(MetadataProperty));
            CompositionHost container = configuration.CreateContainer();

            Assert.Throws<CompositionFailedException>(() => container.GetExport<MetadataProperty>());
        }

        [MetadataAttribute]
        public class CustomMetadataExportAttribute : ExportAttribute
        {
            public object NullName { get; set; } = null;
            public string StringName { get; set; } = "value";
            public int[] ArrayName { get; set; } = new int[] { 1, 2, 3 };
        }

        public class MetadataProperty
        {
            [CustomMetadataExport]
            [ExportMetadata("NullName", null)]
            public object NullMetadata { get; set; }

            [CustomMetadataExport]
            [ExportMetadata("StringName", "value")]
            public string StringMetadata { get; set; }

            [CustomMetadataExport]
            [ExportMetadata("ArrayName", 4)]
            public int[] ArrayMetadata { get; set; }

            [CustomMetadataExport]
            [ExportMetadata("NewName", 1)]
            [Required]
            public int NewMetadata { get; set; }
        }

        [Fact]
        public void CreateContainer_MetadataClass_Success()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithPart(typeof(MetadataClass));
            CompositionHost container = configuration.CreateContainer();

            Assert.Throws<CompositionFailedException>(() => container.GetExport<MetadataProperty>());
        }

        [CustomMetadataExport]
        public class MetadataClass { }

        [Fact]
        public void CreateContainer_ExportIncompatibleNonGenericProperty_ThrowsCompositionFailedException()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithPart(typeof(IncompatibleExportProperty));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        public class IncompatibleExportProperty
        {
            [Export(typeof(int))]
            public string Property { get; set; }
        }

        [Fact]
        public void CreateContainer_ExportGenericProperty_Success()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithPart(typeof(GenericExportProperty<>));
            Assert.NotNull(configuration.CreateContainer());
        }

        public class GenericExportProperty<T>
        {
            [Export(typeof(List<>))]
            public List<T> Property { get; set; } = new List<T>();
        }

        [Fact]
        public void CreateContainer_ExportIncompatibleGenericProperty_ThrowsCompositionFailedException()
        {
            ContainerConfiguration configuration = new ContainerConfiguration().WithPart(typeof(IncompatibleGenericExportProperty<>));
            Assert.Throws<CompositionFailedException>(() => configuration.CreateContainer());
        }

        public class IncompatibleGenericExportProperty<T>
        {
            [Export(typeof(List<string>))]
            public List<T> Property { get; set; }
        }

        public static IEnumerable<object[]> DebuggerAttributes_TestData()
        {
            yield return new object[] { new ContainerConfiguration() };
            yield return new object[] { new ContainerConfiguration().WithPart(typeof(int)) };
            yield return new object[] { new ContainerConfiguration().WithDefaultConventions(new ConventionBuilder()).WithPart(typeof(int)) };
            yield return new object[] { new ContainerConfiguration().WithPart(typeof(int), new ConventionBuilder()) };
            yield return new object[] { new ContainerConfiguration().WithPart(typeof(ExportedProperty)) };
        }

        [Theory]
        [MemberData(nameof(DebuggerAttributes_TestData))]
        public void DebuggerAttributes_GetViaReflection_Success(ContainerConfiguration configuration)
        {
            DebuggerAttributeInfo debuggerAttributeInfo = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(configuration);
            foreach (PropertyInfo property in debuggerAttributeInfo.Properties)
            {
                Assert.NotNull(property.GetValue(debuggerAttributeInfo.Instance));
            }
        }
    }
}
