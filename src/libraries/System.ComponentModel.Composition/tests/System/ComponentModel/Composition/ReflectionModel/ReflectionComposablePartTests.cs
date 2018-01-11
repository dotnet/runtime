// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Diagnostics;
using System.ComponentModel.Composition.Extensibility;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.UnitTesting;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class ReflectionComposablePartTests
    {
        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types.Retrieve the LoaderExceptions property for more information.
        public void Constructor1_DefinitionAsDefinitionArgument_ShouldSetOriginProperty()
        {
            var expectations = Expectations.GetAttributedDefinitions();

            foreach (var e in expectations)
            {
                var definition = (ICompositionElement)new ReflectionComposablePart(e);

                Assert.Same(e, definition.Origin);
            }
        }

        [Fact]
        public void Constructor1_NullAsDefinitionArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                new ReflectionComposablePart((ReflectionComposablePartDefinition)null);
            });
        }

        [Fact]
        public void Constructor2_NullAsAttributedPartArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("attributedPart", () =>
            {
                new ReflectionComposablePart(PartDefinitionFactory.CreateAttributed(), (object)null);
            });
        }

        [Fact]
        public void Constructor2_ValueTypeAsAttributedPartArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("attributedPart", () =>
            {
                new ReflectionComposablePart(PartDefinitionFactory.CreateAttributed(), 42);
            });
        }

        [Fact]
        public void Constructor1_AttributedComposablePartDefintion_ShouldProduceValidObject()
        {
            var definition = PartDefinitionFactory.CreateAttributed(typeof(MyExport));
            var part = new ReflectionComposablePart(definition);

            Assert.Equal(definition, part.Definition);
            Assert.NotNull(part.Metadata);

            Assert.False(part is IDisposable);
        }

        [Fact]
        public void Constructor1_AttributedComposablePartDefintion_Disposable_ShouldProduceValidObject()
        {
            var definition = PartDefinitionFactory.CreateAttributed(typeof(DisposablePart));
            var part = new DisposableReflectionComposablePart(definition);

            Assert.Equal(definition, part.Definition);
            Assert.NotNull(part.Metadata);

            Assert.True(part is IDisposable);
        }

        [Fact]
        public void Constructor1_Type_ShouldProduceValidObject()
        {
            var part = new ReflectionComposablePart(PartDefinitionFactory.CreateAttributed(typeof(MyExport)));
        }

        [Fact]
        public void Constructor1_Object_ShouldProduceValidObject()
        {
            var part = new ReflectionComposablePart(PartDefinitionFactory.CreateAttributed(typeof(MyExport)), new MyExport());
        }

        [Fact]
        public void Metadata_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                var metadata = part.Metadata;
            });
        }

        [Fact]
        public void ImportDefinitions_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                var definitions = part.ImportDefinitions;
            });
        }

        [Fact]
        public void ExportDefinitions_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                var definitions = part.ExportDefinitions;
            });
        }

        [Fact]
        public void OnComposed_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                part.Activate();
            });
        }

        [Fact]
        public void OnComposed_MissingPostImportsOnInstance_ShouldThrowComposition()
        {
            var part = CreatePart(new MySharedPartExport());

            // Dev10:484204 - This used to cause a failure but after we made 
            // ReflectionComposablePart internal we needed to back remove this 
            // validation for post imports to make declarative composition work.
            //part.Activate().VerifyFailure(CompositionIssueId.ImportNotSetOnPart);
            part.Activate();
        }

        [Fact]
        public void OnComposed_ProperlyComposed_ShouldSucceed()
        {
            var import = new TrivialImporter();
            var export = new TrivialExporter();

            var part = CreatePart(import);

            var importDef = part.ImportDefinitions.First();
            part.SetImport(importDef, CreateSimpleExports(export));
            part.Activate();
            Assert.True(export.done, "OnImportsSatisfied should have been called");
        }

        [Fact]
        public void OnComposed_UnhandledExceptionThrowInOnImportsSatisfied_ShouldThrowComposablePart()
        {
            var part = CreatePart(typeof(ExceptionDuringINotifyImport));
            var definition = part.ImportDefinitions.First();
            part.SetImport(definition, CreateSimpleExports(21));

            CompositionAssert.ThrowsPart<NotImplementedException>(RetryMode.DoNotRetry, () =>
            {
                part.Activate();
            });
        }

        [Fact]
        public void SetImport_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            var definition = part.ImportDefinitions.First();

            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void SetImport_NullAsImportDefinitionArgument_ShouldThrowArgumentNull()
        {
            var part = CreateDefaultPart();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.SetImport((ImportDefinition)null, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void SetImport_NullAsExportsArgument_ShouldThrowArgumentNull()
        {
            var part = CreatePart(typeof(MySharedPartExport));
            var import = part.ImportDefinitions.First();

            Assert.Throws<ArgumentNullException>("exports", () =>
            {
                part.SetImport(import, (IEnumerable<Export>)null);
            });
        }

        [Fact]
        public void SetImport_ExportsArrayWithNullElementAsExportsArgument_ShouldThrowArgument()
        {
            var part = CreatePart(typeof(MySharedPartExport));
            var definition = part.ImportDefinitions.First();

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, new Export[] { null });
            });
        }

        [Fact]
        public void SetImport_WrongDefinitionAsDefinitionArgument_ShouldThrowArgument()
        {
            var part = CreateDefaultPart();

            var definition = ImportDefinitionFactory.Create();

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void SetImport_SetNonRecomposableDefinitionAsDefinitionArgumentAfterOnComposed_ShouldThrowInvalidOperation()
        {
            var part = CreatePartWithNonRecomposableImport();
            var definition = part.ImportDefinitions.First();

            part.SetImport(definition, Enumerable.Empty<Export>());
            part.Activate();

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void SetImport_ZeroOrOneDefinitionAsDefinitionArgumentAndTwoExportsAsExportsArgument_ShouldThrowArgument()
        {
            var part = CreatePartWithZeroOrOneImport();
            var definition = part.ImportDefinitions.First();

            var exports = ExportFactory.Create("Import", 2);

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, exports);
            });
        }

        [Fact]
        public void SetImport_ExactlyOneDefinitionAsDefinitionArgumentAndTwoExportsAsExportsArgument_ShouldThrowArgument()
        {
            var part = CreatePartWithExactlyOneImport();
            var definition = part.ImportDefinitions.First();

            var exports = ExportFactory.Create("Import", 2);

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, exports);
            });
        }

        [Fact]
        public void SetImport_ExactlyOneDefinitionAsDefinitionArgumentAndEmptyExportsAsExportsArgument_ShouldThrowArgument()
        {
            var part = CreatePartWithExactlyOneImport();
            var definition = part.ImportDefinitions.First();

            var exports = Enumerable.Empty<Export>();

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, exports);
            });
        }

        [Fact]
        public void SetImport_WrongTypeExportGiven_ShouldThrowComposablePart()
        {
            var part = CreatePart(new MySharedPartExport());
            var import = part.ImportDefinitions.First();

            CompositionAssert.ThrowsPart(() =>
           {
               part.SetImport(import, CreateSimpleExports("21"));
           });
        }

        [Fact]
        public void SetImport_SetPostValueAndSetAgainOnInstance_ShouldSetProperty()
        {
            var import = new MySharedPartExport();
            var part = CreatePart(import);
            var importDef = part.ImportDefinitions.First();

            part.SetImport(importDef, CreateSimpleExports(21));

            Assert.NotEqual(import.Value, 21);
            part.Activate();

            Assert.Equal(import.Value, 21);

            part.SetImport(importDef, CreateSimpleExports(42));

            Assert.NotEqual(import.Value, 42);

            part.Activate();

            Assert.Equal(import.Value, 42);
        }

        [Fact]
        public void GetExportedValue_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var part = CreateDefaultDisposablePart();
            var definition = part.ExportDefinitions.First();

            ((IDisposable)part).Dispose();

            ExceptionAssert.ThrowsDisposed(part, () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void GetExportedValue_NullAsDefinitionArgument_ShouldThrowArgumentNull()
        {
            var part = CreateDefaultPart();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.GetExportedValue((ExportDefinition)null);
            });
        }

        [Fact]
        public void GetExportedValue_WrongDefinitionAsDefinitionArgument_ShouldThrowArgument()
        {
            var part = CreateDefaultPart();
            var definition = ExportDefinitionFactory.Create();

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void GetExportedValue_MissingPrerequisiteImport_ShouldThrowInvalidOperation()
        {
            var part = CreatePart(typeof(SimpleConstructorInjectedObject));
            var definition = part.ExportDefinitions.First();

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        [ActiveIssue(484204)]
        public void GetExportedValue_MissingPostImports_ShouldThrowComposition()
        {
            var part = CreatePart(typeof(MySharedPartExport));

            // Signal that the composition should be finished
            part.Activate();

            var definition = part.ExportDefinitions.First();

            // Dev10:484204 - This used to cause a failure but after we made 
            // ReflectionComposablePart internal we needed to back remove this 
            // validation for post imports to make declarative composition work.
            CompositionAssert.ThrowsError(ErrorId.ImportNotSetOnPart, () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void GetExportedValue_NoConstructorOnDefinition_ShouldThrowComposablePart()
        {
            var part = CreatePart(typeof(ClassWithNoMarkedOrDefaultConstructor));

            var definition = part.ExportDefinitions.First();

            CompositionAssert.ThrowsPart(() =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void GetExportedValue_UnhandledExceptionThrowInConstructor_ShouldThrowComposablePart()
        {
            var part = CreatePart(typeof(ExportWithExceptionDuringConstruction));

            var definition = part.ExportDefinitions.First();

            CompositionAssert.ThrowsPart<NotImplementedException>(() =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void GetExportedValue_GetObjectAfterSetPreImport_ShouldGetValue()
        {
            var part = CreatePart(typeof(SimpleConstructorInjectedObject));

            var import = part.ImportDefinitions.First();
            part.SetImport(import, CreateSimpleExports(21));

            part.Activate();

            var definition = part.ExportDefinitions.First();
            var exportObject = (SimpleConstructorInjectedObject)part.GetExportedValue(definition);

            Assert.Equal(21, exportObject.CISimpleValue);
        }

        [Fact]
        public void GetExportedValue_GetObjectAfterSetPostImport_ShouldGetValue()
        {
            var part = CreatePart(typeof(MySharedPartExport));

            var import = part.ImportDefinitions.First();
            part.SetImport(import, CreateSimpleExports(21));

            part.Activate();

            var definition = part.ExportDefinitions.First();
            var exportObject = (MySharedPartExport)part.GetExportedValue(definition);

            Assert.NotNull(exportObject);
            Assert.Equal(21, exportObject.Value);
        }

        [Fact]
        public void GetExportedValue_CallMultipleTimes_ShouldReturnSame()
        {
            var part = CreatePart(typeof(MySharedPartExport));

            var import = part.ImportDefinitions.First();
            part.SetImport(import, CreateSimpleExports(21));

            part.Activate();

            var definition = part.ExportDefinitions.First();
            var exportedValue1 = part.GetExportedValue(definition);
            var exportedValue2 = part.GetExportedValue(definition);

            Assert.Same(exportedValue1, exportedValue2);
        }

        [Fact]
        public void GetExportedValue_FromStaticClass_ShouldReturnExport()
        {
            var part = CreatePart(typeof(StaticExportClass));

            var definition = part.ExportDefinitions.First();

            var exportObject = (string)part.GetExportedValue(definition);

            Assert.Equal("StaticString", exportObject);
        }

        [Fact]
        public void GetExportedValue_OptionalPostNotGiven_ShouldReturnValidObject()
        {
            var part = CreatePart(typeof(ClassWithOptionalPostImport));
            part.Activate();

            var definition = part.ExportDefinitions.First();
            var exportObject = (ClassWithOptionalPostImport)part.GetExportedValue(definition);

            Assert.Null(exportObject.Formatter);
        }

        [Fact]
        public void GetExportedValue_OptionalPreNotGiven_ShouldReturnValidObject()
        {
            var part = CreatePart(typeof(ClassWithOptionalPreImport));
            part.Activate();

            var definition = part.ExportDefinitions.First();

            var exportedValue = (ClassWithOptionalPreImport)part.GetExportedValue(definition);
            Assert.Null(exportedValue.Formatter);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types.Retrieve the LoaderExceptions property for more information.
        public void ICompositionElementDisplayName_ShouldReturnTypeDisplayName()
        {
            var expectations = Expectations.GetAttributedTypes();
            foreach (var e in expectations)
            {
                var part = (ICompositionElement)CreatePart(e);

                Assert.Equal(e.GetDisplayName(), part.DisplayName);
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ToString_ShouldReturnICompositionElementDisplayName()
        {
            var expectations = Expectations.GetAttributedTypes();
            foreach (var e in expectations)
            {
                var part = (ICompositionElement)CreatePart(e);

                Assert.Equal(part.DisplayName, part.ToString());
            }
        }

        [PartNotDiscoverable]
        public class PropertyExporter
        {
            [Export]
            public object Property { get { return new object(); } }
        }

        [PartNotDiscoverable]
        public class FieldExporter
        {
            [Export]
            public object Field = null;
        }

        [PartNotDiscoverable]
        public class MethodExporter
        {
            [Export("Method")]
            public void Method() { }
        }

        [PartNotDiscoverable]
        [Export]
        public class TypeExporter
        {
        }

        [Fact]
        public void GetExportedObjectAlwaysReturnsSameReference_ForProperty()
        {
            var cp = CreatePart(new PropertyExporter());
            var ed = cp.ExportDefinitions.Single();
            var eo1 = cp.GetExportedValue(ed);
            var eo2 = cp.GetExportedValue(ed);
            Assert.Same(eo1, eo2);
        }

        [Fact]
        public void GetExportedObjectAlwaysReturnsSameReference_ForField()
        {
            var exporter = new FieldExporter();
            var cp = CreatePart(new FieldExporter());
            var ed = cp.ExportDefinitions.Single();

            exporter.Field = new object();
            var eo1 = cp.GetExportedValue(ed);
            exporter.Field = new object();
            var eo2 = cp.GetExportedValue(ed);
            Assert.Same(eo1, eo2);
        }

        [Fact]
        public void GetExportedObjectAlwaysReturnsSameReference_ForMethod()
        {
            var cp = CreatePart(new MethodExporter());
            var ed = cp.ExportDefinitions.Single();
            var eo1 = cp.GetExportedValue(ed);
            var eo2 = cp.GetExportedValue(ed);
            Assert.Same(eo1, eo2);
        }

        [Fact]
        public void GetExportedObjectAlwaysReturnsSameReference_ForType()
        {
            var cp = CreatePart(new TypeExporter());
            var ed = cp.ExportDefinitions.Single();
            var eo1 = cp.GetExportedValue(ed);
            var eo2 = cp.GetExportedValue(ed);
            Assert.Same(eo1, eo2);
        }

        [PartNotDiscoverable]
        public class MethodWithoutContractName
        {
            [Export]
            public void MethodWithoutContractNameNotAllowed()
            {
            }
        }

        public interface IContract
        {
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public class CustomImportAttributeInvalidTarget : ImportAttribute
        {
            public CustomImportAttributeInvalidTarget()
                : base(typeof(IContract))
            {
            }
        }

        [PartNotDiscoverable]
        public class ImportWithCustomImport
        {
            [CustomImport]
            IContract ImportWithCustomAttributeImport { get; set; }
        }

        [PartNotDiscoverable]
        public class ImportWithCustomImportInvalidTarget
        {
            [CustomImportAttributeInvalidTarget]
            void InvalidImport() { }
        }

        [Fact]
        public void ImportDefinitions_ImportWithCustomAttributeImports()
        {
            var part = CreatePart(typeof(ImportWithCustomImport));
            Assert.Equal(part.ImportDefinitions.Count(), 1);
            ContractBasedImportDefinition import = part.ImportDefinitions.First() as ContractBasedImportDefinition;
            Assert.NotNull(import);

            Assert.Equal(AttributedModelServices.GetContractName(typeof(IContract)), import.ContractName);
            Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(IContract)), import.RequiredTypeIdentity);
        }

        [Fact]
        public void ImportDefinitions_ImportWithCustomImportInvalidTarget_ShouldbeIgnored()
        {
            var part = CreatePart(typeof(ImportWithCustomImportInvalidTarget));
            Assert.Equal(part.ImportDefinitions.Count(), 0);
        }

        [PartNotDiscoverable]
        public class ImportManyWithCustomImportMany
        {
            [CustomImportMany]
            IContract ImportManyWithCustomAttributeImportMany { get; set; }
        }

        [PartNotDiscoverable]
        public class ImportManyWithCustomImportManyInvalidTarget
        {
            [CustomImportMany]
            void InvalidImportMany() { }
        }

        [Fact]
        public void ImportDefinitions_ImportManyWithCustomAttributeImportManys()
        {
            var part = CreatePart(typeof(ImportManyWithCustomImportMany));
            Assert.Equal(part.ImportDefinitions.Count(), 1);
            ContractBasedImportDefinition import = part.ImportDefinitions.First() as ContractBasedImportDefinition;
            Assert.NotNull(import);

            Assert.Equal(AttributedModelServices.GetContractName(typeof(IContract)), import.ContractName);
            Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(IContract)), import.RequiredTypeIdentity);
        }

        [Fact]
        public void ImportDefinitions_ImportManyWithCustomImportManyInvalidTarget_ShouldbeIgnored()
        {
            var part = CreatePart(typeof(ImportManyWithCustomImportManyInvalidTarget));
            Assert.Equal(part.ImportDefinitions.Count(), 0);
        }

        [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
        public class CustomImportingConstructorAttribute : ImportingConstructorAttribute
        {
            public CustomImportingConstructorAttribute()
                : base()
            {
            }
        }

        [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
        public class CustomImportingConstructorAllowMultipleAttribute : ImportingConstructorAttribute
        {
            public CustomImportingConstructorAllowMultipleAttribute()
                : base()
            {
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public class CustomImportingConstructorInvalidTargetAttribute : ImportingConstructorAttribute
        {
            public CustomImportingConstructorInvalidTargetAttribute()
                : base()
            {
            }
        }

        [PartNotDiscoverable]
        public class ImportingConstructorWithCustomImportingConstructor
        {
            [CustomImportingConstructor]
            ImportingConstructorWithCustomImportingConstructor([Import] IContract argument) { }
        }

        [PartNotDiscoverable]
        public class ImportingConstructorWithCustomImportingConstructorAllowMultiple
        {
            [CustomImportingConstructorAllowMultiple]
            [CustomImportingConstructorAllowMultiple]
            ImportingConstructorWithCustomImportingConstructorAllowMultiple([Import] IContract argument) { }
        }

        [PartNotDiscoverable]
        public class ImportingConstructorWithCustomImportingConstructorInvalidTarget
        {
            [CustomImportingConstructorInvalidTarget]
            void InvalidImportingConstructor() { }
        }

        [Fact]
        public void ImportDefinitions_ImportingConstructorWithCustomAttributeImportingConstructors()
        {
            var part = CreatePart(typeof(ImportingConstructorWithCustomImportingConstructor));
            Assert.Equal(part.ImportDefinitions.Count(), 1);
            ContractBasedImportDefinition import = part.ImportDefinitions.First() as ContractBasedImportDefinition;
            Assert.NotNull(import);

            Assert.Equal(AttributedModelServices.GetContractName(typeof(IContract)), import.ContractName);
            Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(IContract)), import.RequiredTypeIdentity);
        }

        [Fact]
        public void ImportDefinitions_ImportingConstructorWithCustomAttributeImportingConstructorsWithAllowMultiple_ShouldNotThrowInvalidOperation()
        {
            var part = CreatePart(typeof(ImportingConstructorWithCustomImportingConstructorAllowMultiple));

            Assert.Equal(part.ImportDefinitions.Count(), 1);
            ContractBasedImportDefinition import = part.ImportDefinitions.First() as ContractBasedImportDefinition;
            Assert.NotNull(import);

            Assert.Equal(AttributedModelServices.GetContractName(typeof(IContract)), import.ContractName);
            Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(IContract)), import.RequiredTypeIdentity);
        }

        [Fact]
        public void ImportDefinitions_ImportingConstructorWithCustomImportingConstructorInvalidTarget_ShouldbeIgnored()
        {
            var part = CreatePart(typeof(ImportingConstructorWithCustomImportingConstructorInvalidTarget));
            Assert.Equal(part.ImportDefinitions.Count(), 0);
        }

        private Export[] CreateSimpleExports(object value)
        {
            var export = ExportFactory.Create("NoContract", () => value);

            return new Export[] { export };
        }

        private ReflectionComposablePart CreatePartWithExport()
        {
            return CreatePart(typeof(StaticExportClass));
        }

        private ReflectionComposablePart CreatePartWithNonRecomposableImport()
        {
            return CreatePart(typeof(SingleImportWithAllowDefault));
        }

        private ReflectionComposablePart CreatePartWithZeroOrOneImport()
        {
            return CreatePart(typeof(SingleImportWithAllowDefault));
        }

        private ReflectionComposablePart CreatePartWithExactlyOneImport()
        {
            return CreatePart(typeof(SingleImport));
        }

        private ReflectionComposablePart CreateDefaultPart()
        {
            return CreatePart(new object());
        }

        [PartNotDiscoverable]
        [Export]
        public class DisposablePart : IDisposable
        {
            [Import(AllowDefault = true)]
            public int Foo { get; set; }

            public void Dispose() { }
        }

        private ReflectionComposablePart CreateDefaultDisposablePart()
        {
            return CreatePart(typeof(DisposablePart));
        }

        private ReflectionComposablePart CreatePart(object instance)
        {
            if (instance is Type)
            {
                var definition = PartDefinitionFactory.CreateAttributed((Type)instance);

                return (ReflectionComposablePart)definition.CreatePart();
            }
            else
            {
                var definition = PartDefinitionFactory.CreateAttributed(instance.GetType());

                return new ReflectionComposablePart(definition, instance);
            }
        }
    }
}
