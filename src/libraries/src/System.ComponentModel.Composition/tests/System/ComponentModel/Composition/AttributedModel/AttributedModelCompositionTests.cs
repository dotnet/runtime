// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.AttributedModel
{
    public class AttributedModelCompositionTests
    {
        [Fact]
        public void PartContainingOnlyStaticExports_ShouldNotCauseInstanceToBeCreated()
        {  
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(HasOnlyStaticExports));
            
            Assert.Equal("Field", container.GetExportedValue<string>("Field"));
            Assert.Equal("Property", container.GetExportedValue<string>("Property"));
            Assert.NotNull(container.GetExportedValue<Func<string>>("Method")());

            Assert.False(HasOnlyStaticExports.InstanceCreated);
        }

        [Fact]
        public void ExportOnAbstractBase_DoesNotReturnNull()
        {   // 499393 - Classes inheriting from an exported 
            // abstract class are exported as 'null'

            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            var definition = PartDefinitionFactory.CreateAttributed(typeof(Derived));
            batch.AddPart(definition.CreatePart());
            container.Compose(batch);

            Assert.NotNull(container.GetExportedValueOrDefault<Base>());
        }

        [Fact]
        public void ReadOnlyFieldImport_ShouldThrowComposition()
        {
            var importer = PartFactory.CreateAttributed(new ReadOnlyPropertyImport());
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("readonly", "new value");

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ReadOnlyPropertyImport_ShouldThrowComposition()
        {
            var importer = PartFactory.CreateAttributed(new ReadOnlyPropertyImport());
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("readonly", "new value");

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void WriteOnlyPropertyExport_ShouldThrowComposition()
        {
            var importer = PartFactory.CreateAttributed(new WriteOnlyPropertyExport());
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<string>("writeonly");
            });
        }

        [Fact]
        public void ImportValueMismatchFromInt32ToDateTime()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("DateTime", typeof(DateTime), 42);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromInt16ToInt32()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Int32", typeof(int), (short)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                              ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                              RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });

        }

        [Fact]
        public void ImportValueMismatchFromInt32ToInt16()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Int16", typeof(short), (int)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromInt32ToUInt32()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("UInt32", typeof(uint), (int)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromUInt32ToInt32()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Int32", typeof(int), (uint)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromInt32ToInt64()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Int64", typeof(long), (int)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromSingleToDouble()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Double", typeof(double), (float)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromDoubleToSingle()
        {
            var container = ContainerFactory.Create(); 
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Single", typeof(float), (double)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromSingleToInt32()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Int32", typeof(int), (float)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueMismatchFromInt32ToSingle()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ImportValueTypes());
            batch.AddExportedValue("Single", typeof(float), (int)10);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        [ActiveIssue(25498)]
        public void MemberExports()
        {
            var exporter = PartFactory.CreateAttributed(new ObjectWithMemberExports());
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(exporter);
            container.Compose(batch);

            var filedExports = container.GetExports<int>("field");
            ExportsAssert.AreEqual(filedExports, 1, 5);

            var readonlyExports = container.GetExports<int>("readonly");
            ExportsAssert.AreEqual(readonlyExports, 2, 6);

            var propertyExports = container.GetExports<int>("property");
            ExportsAssert.AreEqual(propertyExports, 3, 7);

            var methodExports = container.GetExportedValues<Func<int, int>>("func").Select(f => f(0));
            EnumerableAssert.AreEqual(methodExports, 4, 8);
        }

        [Fact]
        public void TestExportedValueCachesValue()
        {
            var container = ContainerFactory.Create();
            var exporter = new ExportsMutableProperty();
            exporter.Property = "Value1";

            container.ComposeParts(exporter);

            Assert.Equal("Value1", container.GetExportedValue<string>("Property"));

            exporter.Property = "Value2";

            //  Exported value should have been cached and so it shouldn't change
            Assert.Equal("Value1", container.GetExportedValue<string>("Property"));
        }

        [Fact]
        [ActiveIssue(739354)]
        public void TestExportedValueCachesNullValue()
        {
            var container = ContainerFactory.Create();
            var exporter = new ExportsMutableProperty();
            exporter.Property = null;

            container.ComposeParts(exporter);

            Assert.Null(container.GetExportedValue<string>("Property"));

            exporter.Property = "Value1";

            //  Exported value should have been cached and so it shouldn't change
            Assert.Null(container.GetExportedValue<string>("Property"));
        }

        public class ExportsMutableProperty
        {
            [Export("Property")]
            public string Property { get; set; }
        }

        public class ReadOnlyFieldImport
        {
            [Import("readonly")]
            public readonly string Value = "Value";
        }

        public class ReadOnlyPropertyImport
        {
            [Import("readonly")]
            public string Value
            {
                get { return "Value"; }
            }
        }

        public class WriteOnlyPropertyExport
        {
            [Export("writeonly")]
            public string Value
            {
                set { }
            }
        }

        public class ObjectWithMemberExports
        {
            [Export("field")]
            public static int staticField = 1;

            [Export("readonly")]
            public static readonly int staticReadonly = 2;

            [Export("property")]
            public static int StaticProperty { get { return 3; } }

            [Export("func")]
            public static int StaticMethod(int arg1) { return 4; }

            [Export("field")]
            public int instanceField = 5;

            [Export("readonly")]
            public readonly int instanceReadonly = 6;

            [Export("property")]
            public int InstanceProperty { get { return 7; } }

            [Export("func")]
            public int InstanceMethod(int arg1) { return 8; }
        }

        public class HasOnlyStaticExports
        {
            public static bool InstanceCreated;

            public HasOnlyStaticExports()
            {
                InstanceCreated = true;
            }

            [Export("Field")]
            public static string Field = "Field";

            [Export("Property")]
            public static string Property
            {
                get { return "Property"; }
            }

            [Export("Method")]
            public static string Method()
            {
                return "Method";
            }
        }

        [InheritedExport(typeof(Base))]
        public abstract class Base
        {
        }

        [Export(typeof(Derived))]
        public class Derived : Base
        {
        }

        public class ImportValueTypes
        {
            [Import("Int16", AllowDefault = true)]
            public short Int16
            {
                get;
                set;
            }

            [Import("Int32", AllowDefault = true)]
            public int Int32
            {
                get;
                set;
            }

            [Import("UInt32", AllowDefault = true)]
            public uint UInt32
            {
                get;
                set;
            }

            [Import("Int64", AllowDefault = true)]
            public long Int64
            {
                get;
                set;
            }

            [Import("Single", AllowDefault = true)]
            public float Single
            {
                get;
                set;
            }

            [Import("Double", AllowDefault = true)]
            public double Double
            {
                get;
                set;
            }

            [Import("DateTime", AllowDefault = true)]
            public DateTime DateTime
            {
                get;
                set;
            }
        }
    }
}
