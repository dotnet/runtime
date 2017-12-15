// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace Tests.Integration
{
    public class DiscoveryTests
    {
        public abstract class AbstractClassWithExports
        {
            [Export("StaticExport")]
            public static string StaticExport { get { return "ExportedValue"; } }

            [Export("InstanceExport")]
            public string InstanceExport { get { return "InstanceExportedValue"; } }
        }

        [Fact]
        public void Export_StaticOnAbstractClass_ShouldExist()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(AbstractClassWithExports));

            Assert.True(container.IsPresent("StaticExport"));
            Assert.False(container.IsPresent("InstanceExport"));
        }

        public class ClassWithStaticImport
        {
            [Import("StaticImport")]
            public static string MyImport
            {
                get; set;
            }
        }

        [Fact]
        public void Import_StaticImport_ShouldNotBeSet()
        {
            var container = ContainerFactory.Create();
            container.AddAndComposeExportedValue("StaticImport", "String that shouldn't be imported");

            var importer = new ClassWithStaticImport();

            container.SatisfyImportsOnce(importer);

            Assert.Null(ClassWithStaticImport.MyImport);
        }

        [Export]
        public class BaseWithNonPublicImportAndExport
        {
            [Import("BasePrivateImport")]
            private string _basePrivateImport = null;

            public string BasePrivateImport { get { return this._basePrivateImport; } }
        }

        [Export]
        public class DerivedBaseWithNonPublicImportAndExport : BaseWithNonPublicImportAndExport
        {

        }

        [Fact]
        public void Import_PrivateOnClass_ShouldSetImport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(BaseWithNonPublicImportAndExport));
            container.AddAndComposeExportedValue("BasePrivateImport", "Imported String");

            var importer = container.GetExportedValue<BaseWithNonPublicImportAndExport>();
            Assert.Equal("Imported String", importer.BasePrivateImport);
        }

        [Fact]
        public void Import_PrivateOnBase_ShouldSetImport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(DerivedBaseWithNonPublicImportAndExport));
            container.AddAndComposeExportedValue("BasePrivateImport", "Imported String");

            var importer = container.GetExportedValue<DerivedBaseWithNonPublicImportAndExport>();
            Assert.Equal("Imported String", importer.BasePrivateImport);
        }

        public interface InterfaceWithImport
        {
            [Import("InterfaceImport")]
            int MyImport { get; set; }
        }

        public interface InterfaceWithExport
        {
            [Export("InterfaceExport")]
            int MyExport { get; set; }
        }

        [Fact]
        public void AttributesOnInterface_ShouldNotBeConsiderAPart()
        {
            var catalog = CatalogFactory.CreateAttributed(
                typeof(InterfaceWithImport),
                typeof(InterfaceWithExport));

            Assert.Equal(0, catalog.Parts.Count());
        }

        [Export]
        public class ClassWithInterfaceInheritedImport : InterfaceWithImport
        {
            public int MyImport { get; set; }
        }

        [Fact]
        public void Import_InheritImportFromInterface_ShouldExposeImport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(ClassWithInterfaceInheritedImport));

            container.AddAndComposeExportedValue("InterfaceImport", 42);

            var importer = container.GetExportedValue<ClassWithInterfaceInheritedImport>();

            Assert.True(importer.MyImport == default(int), "Imports declared on interfaces should not be discovered");
        }

        public class ClassWithInterfaceInheritedExport : InterfaceWithExport
        {
            public ClassWithInterfaceInheritedExport()
            {
                MyExport = 42;
            }

            public int MyExport { get; set; }
        }

        [Fact]
        public void Import_InheritExportFromInterface_ShouldNotExposeExport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(ClassWithInterfaceInheritedExport));

            Assert.False(container.IsPresent("InterfaceExport"), "Export defined on interface should not be discovered!");
        }

        public interface IFoo { }

        [InheritedExport]
        public abstract class BaseWithVirtualExport
        {
            [Export]
            public virtual IFoo MyProp { get; set; }
        }

        [InheritedExport(typeof(BaseWithVirtualExport))]
        public class DerivedWithOverrideExport : BaseWithVirtualExport
        {
            [Export]
            public override IFoo MyProp { get; set; }
        }

        [Fact]
        public void Export_BaseAndDerivedShouldAmountInTwoExports()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(BaseWithVirtualExport),
                typeof(DerivedWithOverrideExport));

            var exports1 = container.GetExportedValues<BaseWithVirtualExport>();
            Assert.Equal(1, exports1.Count());

            var exports2 = container.GetExportedValues<IFoo>();
            Assert.Equal(1, exports2.Count());
        }

        public interface IDocument { }

        [Export(typeof(IDocument))]
        [ExportMetadata("Name", "TextDocument")]
        public class TextDocument : IDocument
        {
        }

        [Export(typeof(IDocument))]
        [ExportMetadata("Name", "XmlDocument")]
        public class XmlDocument : TextDocument
        {
        }

        [Fact]
        public void Export_ExportingSameContractInDerived_ShouldResultInHidingBaseExport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(IDocument),
                typeof(XmlDocument));

            var export = container.GetExport<IDocument, IDictionary<string, object>>();

            Assert.Equal("XmlDocument", export.Metadata["Name"]);
        }

        [Fact]
        public void Export_ExportingBaseAndDerivedSameContract_ShouldResultInOnlyTwoExports()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(IDocument),
                typeof(TextDocument),
                typeof(XmlDocument));

            var exports = container.GetExports<IDocument, IDictionary<string, object>>();

            Assert.Equal(2, exports.Count());
            Assert.Equal("TextDocument", exports.ElementAt(0).Metadata["Name"]);
            Assert.IsType<TextDocument>(exports.ElementAt(0).Value);

            Assert.Equal("XmlDocument", exports.ElementAt(1).Metadata["Name"]);
            Assert.IsType<XmlDocument>(exports.ElementAt(1).Value);
        }

        public interface IObjectSerializer { }

        [Export(typeof(IDocument))]
        [Export(typeof(IObjectSerializer))]
        [ExportMetadata("Name", "XamlDocument")]
        public class XamlDocument : XmlDocument, IObjectSerializer
        {
        }

        [Fact]
        public void Export_ExportingSameContractInDerivedAndNewContract_ShouldResultInHidingBaseAndExportingNewContract()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(XamlDocument));

            var export = container.GetExport<IDocument, IDictionary<string, object>>();

            Assert.Equal("XamlDocument", export.Metadata["Name"]);

            var export2 = container.GetExport<IObjectSerializer, IDictionary<string, object>>();

            Assert.Equal("XamlDocument", export2.Metadata["Name"]);
        }

        [Export(typeof(IDocument))]
        [ExportMetadata("Name", "WPFDocument")]
        public class WPFDocument : XamlDocument
        {
        }

        [Fact]
        public void Export_ExportingSameContractInDerivedAndAnotherContractInBase_ShouldResultInHidingOneBaseAndInheritingNewContract()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(WPFDocument));

            var export = container.GetExport<IDocument, IDictionary<string, object>>();

            Assert.Equal("WPFDocument", export.Metadata["Name"]);

            var export2 = container.GetExportedValueOrDefault<IObjectSerializer>();

            Assert.Null(export2);
        }

        [InheritedExport]
        public abstract class Plugin
        {
            public virtual string GetLocation()
            {
                return "NoWhere";
            }

            public virtual int Version
            {
                get
                {
                    return 0;
                }
            }
        }

        private void VerifyValidPlugin(CompositionContainer container, int version, string location)
        {
            var plugins = container.GetExports<Plugin>();
            Assert.Equal(1, plugins.Count());

            var plugin = plugins.Single().Value;

            Assert.Equal(location, plugin.GetLocation());
            Assert.Equal(version, plugin.Version);
        }

        public class Plugin1 : Plugin
        {
        }

        [Fact]
        public void Export_Plugin1()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(Plugin1));

            VerifyValidPlugin(container, 0, "NoWhere");
        }

        public class Plugin2 : Plugin
        {
            public override string GetLocation()
            {
                return "SomeWhere";
            }
            public override int Version
            {
                get
                {
                    return 1;
                }
            }
        }

        [Fact]
        public void Export_Plugin2()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(Plugin2));

            VerifyValidPlugin(container, 1, "SomeWhere");
        }

        public class Plugin3 : Plugin
        {
            [Export("PluginLocation")]
            public override string GetLocation()
            {
                return "SomeWhere3";
            }

            [Export("PluginVersion")]
            public override int Version
            {
                get
                {
                    return 3;
                }
            }
        }

        [Fact]
        public void Export_Plugin3()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(Plugin3));

            VerifyValidPlugin(container, 3, "SomeWhere3");

            var plVer = container.GetExportedValue<int>("PluginVersion");
            Assert.Equal(3, plVer);

            var plLoc = container.GetExportedValue<Func<string>>("PluginLocation");
            Assert.Equal("SomeWhere3", plLoc());
        }

        [InheritedExport(typeof(Plugin))]
        public class Plugin4 : Plugin
        {
            public override string GetLocation()
            {
                return "SomeWhere4";
            }

            public override int Version
            {
                get
                {
                    return 4;
                }
            }
        }

        [Fact]
        public void Export_Plugin4()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(Plugin4));

            VerifyValidPlugin(container, 4, "SomeWhere4");
        }

        public interface IPlugin
        {
            int Id { get; }
        }

        public class MyPlugin : IPlugin
        {
            [Export("PluginId")]
            public int Id { get { return 0; } }
        }

        [Fact]
        public void Export_MyPlugin()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MyPlugin));

            var export = container.GetExportedValue<int>("PluginId");
        }

        [InheritedExport]
        public interface IApplicationPlugin
        {
            string Name { get; }

            object Application { get; set; }
        }

        [InheritedExport]
        public interface IToolbarPlugin : IApplicationPlugin
        {
            object ToolBar { get; set; }
        }

        public class MyToolbarPlugin : IToolbarPlugin
        {
            [Export("ApplicationPluginNames")]
            public string Name { get { return "MyToolbarPlugin"; } }

            [Import("Application")]
            public object Application { get; set; }

            [Import("ToolBar")]
            public object ToolBar { get; set; }
        }

        [Fact]
        public void TestInterfaces()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MyToolbarPlugin));

            var app = new object();
            container.AddAndComposeExportedValue<object>("Application", app);

            var toolbar = new object();
            container.AddAndComposeExportedValue<object>("ToolBar", toolbar);

            var export = container.GetExportedValue<IToolbarPlugin>();

            Assert.Equal(app, export.Application);
            Assert.Equal(toolbar, export.ToolBar);
            Assert.Equal("MyToolbarPlugin", export.Name);

            var pluginNames = container.GetExportedValues<string>("ApplicationPluginNames");
            Assert.Equal(1, pluginNames.Count());
        }

        public class ImportOnVirtualProperty
        {
            public int ImportSetCount = 0;
            private int _value;

            [Import("VirtualImport")]
            public virtual int VirtualImport
            {
                get
                {
                    return this._value;
                }
                set
                {
                    this._value = value;
                    ImportSetCount++;
                }
            }
        }

        public class ImportOnOverridenPropertyWithSameContract : ImportOnVirtualProperty
        {
            [Import("VirtualImport")]
            public override int VirtualImport
            {
                get
                {
                    return base.VirtualImport;
                }
                set
                {
                    base.VirtualImport = value;
                }
            }
        }

        [Fact]
        public void Import_VirtualPropertyOverrideWithSameContract_ShouldSucceed()
        {
            var container = ContainerFactory.Create();
            container.AddAndComposeExportedValue<int>("VirtualImport", 21);

            var import = new ImportOnOverridenPropertyWithSameContract();

            container.SatisfyImportsOnce(import);

            // Import will get set twice because there are 2 imports on the same property.
            // We would really like to either elminate it getting set twice or error in this case
            // but we figure it is a rare enough corner case that it doesn't warrented the run time cost
            // and can be covered by an FxCop rule.

            Assert.Equal(2, import.ImportSetCount);
            Assert.Equal(21, import.VirtualImport);
        }

        public class ImportOnOverridenPropertyWithDifferentContract : ImportOnVirtualProperty
        {
            [Import("OverriddenImport")]
            public override int VirtualImport
            {
                set
                {
                    base.VirtualImport = value;
                }
            }
        }

        [Fact]
        public void Import_VirtualPropertyOverrideWithDifferentContract_ShouldSucceed()
        {
            var container = ContainerFactory.Create();
            container.AddAndComposeExportedValue<int>("VirtualImport", 21);
            container.AddAndComposeExportedValue<int>("OverriddenImport", 42);

            var import = new ImportOnOverridenPropertyWithSameContract();

            container.SatisfyImportsOnce(import);

            // Import will get set twice because there are 2 imports on the same property.
            // We would really like to either elminate it getting set twice or error in this case
            // but we figure it is a rare enough corner case that it doesn't warrented the run time cost
            // and can be covered by an FxCop rule.

            Assert.Equal(2, import.ImportSetCount);

            // The derived most import should be discovered first and so it will get set first
            // and thus the value should be the base import which is 21.
            Assert.Equal(21, import.VirtualImport);
        }

        [InheritedExport]
        public interface IOrderScreen { }

        public class NorthwindOrderScreen : IOrderScreen
        {
        }

        public class SouthsandOrderScreen : IOrderScreen
        {
        }

        [Fact]
        public void Export_ExportOnlyOnBaseInterfacewithInheritedMarked_ShouldFindAllImplementers()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(NorthwindOrderScreen),
                typeof(SouthsandOrderScreen));

            var exports = container.GetExportedValues<IOrderScreen>();

            Assert.Equal(2, exports.Count());
            Assert.IsType<NorthwindOrderScreen>(exports.ElementAt(0));
            Assert.IsType<SouthsandOrderScreen>(exports.ElementAt(1));
        }

        [Export]
        public class PartWithStaticConstructor
        {
            static PartWithStaticConstructor()
            {
                throw new Exception();
            }
        }

        [Fact]
        public void StaticConstructor()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(PartWithStaticConstructor));

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue,
                ErrorId.ImportEngine_PartCannotActivate,
                () => container.GetExportedValue<PartWithStaticConstructor>());
        }

        public interface IAddin
        {
            void LoadAddin(object application);
            void Shutdown();
        }

        [MetadataAttribute]
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class AddinAttribute : ExportAttribute, ITrans_AddinMetadata
        {
            private string _name;
            private string _version;
            private string _id;

            public AddinAttribute(string name, string version, string id)
                : base(typeof(IAddin))
            {
                this._name = name;
                this._version = version;
                this._id = id;
            }

            public string Name { get { return this._name; } }
            public string Version { get { return this._version; } }
            public string Id { get { return this._id; } }
        }

        [Addin("Addin1", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101C}")]
        public class Addin1 : IAddin
        {
            public void LoadAddin(object application)
            {
            }
            public void Shutdown()
            {
            }
        }

        [Addin("Addin2", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101D}")]
        public class Addin2 : IAddin
        {
            public void LoadAddin(object application)
            {
            }
            public void Shutdown()
            {
            }
        }

        [Addin("Addin3", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101E}")]
        public class Addin3 : IAddin
        {
            public void LoadAddin(object application)
            {
            }
            public void Shutdown()
            {
            }
        }

        [Fact]
        public void DiscoverAddinsWithCombinedCustomExportAndMetadataAttribute()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Addin1), typeof(Addin2), typeof(Addin3));

            var addins = container.GetExports<IAddin, ITrans_AddinMetadata>().ToArray();

            Assert.Equal(3, addins.Length);

            var values = new AddinAttribute[]
                {
                    new AddinAttribute("Addin1", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101C}"),
                    new AddinAttribute("Addin2", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101D}"),
                    new AddinAttribute("Addin3", "1.0", "{63D1B00F-AD2F-4F14-8A36-FFA59E4A101E}"),
                };

            for (int i = 0; i < values.Length; i++)
            {
                var addinMetadata = addins[i].Metadata;

                Assert.Equal(values[i].Name, addinMetadata.Name);
                Assert.Equal(values[i].Version, addinMetadata.Version);
                Assert.Equal(values[i].Id, addinMetadata.Id);
            }
        }

        [Fact]
        public void CombinedCustomExportMetadataAttribute_ShouldNotContainMetadataFromExportAttribute()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Addin1));
            var addin = container.GetExport<IAddin, IDictionary<string, object>>();

            Assert.Equal(4, addin.Metadata.Count); // 3 metadata values and type identity

            Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(IAddin)), addin.Metadata[CompositionConstants.ExportTypeIdentityMetadataName]);
            Assert.Equal("Addin1", addin.Metadata["Name"]);
            Assert.Equal("1.0", addin.Metadata["Version"]);
            Assert.Equal("{63D1B00F-AD2F-4F14-8A36-FFA59E4A101C}", addin.Metadata["Id"]);
        }

        public class CustomInheritedExportAttribute : InheritedExportAttribute
        {
        }

        [CustomInheritedExport]
        public interface IUsesCustomInheritedExport
        {
            int Property { get; }
        }

        public class UsesCustomInheritedExportOnInterface : IUsesCustomInheritedExport
        {
            public int Property
            {
                get { return 42; }
            }
        }

        [Fact]
        public void Test_CustomInheritedExportAttribute_OnInterface()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(UsesCustomInheritedExportOnInterface));
            var exporter = container.GetExportedValue<IUsesCustomInheritedExport>();
            Assert.Equal(42, exporter.Property);
        }

        [CustomInheritedExport]
        public class BaseClassWithCustomInheritedExport
        {
            public int Property { get; set; }
        }

        public class DerivedFromBaseWithCustomInheritedExport : BaseClassWithCustomInheritedExport
        {
            public DerivedFromBaseWithCustomInheritedExport()
            {
                Property = 43;
            }
        }

        [Fact]
        public void Test_CustomInheritedExportAttribute_OnBaseClass()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(DerivedFromBaseWithCustomInheritedExport));
            var exporter = container.GetExportedValue<BaseClassWithCustomInheritedExport>();
            Assert.Equal(43, exporter.Property);
        }

        [InheritedExport("Foo")]
        [ExportMetadata("Name", "IFoo1")]
        public interface IFoo1 { }

        [InheritedExport("Foo")]
        [ExportMetadata("Name", "IFoo2")]
        public interface IFoo2 { }

        [InheritedExport("Foo")]
        [ExportMetadata("Name", "FooWithOneFoo")]
        public class FooWithOneFoo : IFoo1
        {
        }

        [Fact]
        public void InheritedExport_OnTypeAndInterface()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithOneFoo));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();

            Assert.Equal(1, foos.Length);
            Assert.Equal("FooWithOneFoo", foos[0].Metadata["Name"]);
        }

        public class FooWithTwoFoos : IFoo1, IFoo2 { }

        [Fact]
        public void InheritedExport_TwoInterfaces()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithTwoFoos));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();

            Assert.Equal(2, foos.Length);

            Assert.Equal(new string[] { "IFoo1", "IFoo2" }, foos.Select(e => (string)e.Metadata["Name"]));
        }

        public class FooWithIfaceByOneFoo : FooWithOneFoo, IFoo1 { }

        [Fact]
        public void InheritedExport_BaseAndInterface()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithIfaceByOneFoo));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();

            Assert.Equal(1, foos.Length);

            Assert.Equal("FooWithOneFoo", foos[0].Metadata["Name"]);
        }

        [InheritedExport("Foo")]
        [ExportMetadata("Name", "FooWithInheritedOnSelf")]
        public class FooWithInheritedOnSelf : FooWithOneFoo, IFoo1 { }

        [Fact]
        public void InheritedExport_BaseInterfaceAndSelf()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithInheritedOnSelf));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();

            Assert.Equal(1, foos.Length);

            Assert.Equal("FooWithInheritedOnSelf", foos[0].Metadata["Name"]);
        }

        [InheritedExport("Foo")]
        [ExportMetadata("Name", "IFoo3")]
        public interface IFoo3 : IFoo1 { }

        public class FooWithInterfaceWithMultipleFoos : IFoo3 { }

        [Fact]
        [ActiveIssue(25498)]
        public void InheritedExport_InterfaceHiearchy()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithInterfaceWithMultipleFoos));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();
            Assert.Equal(2, foos.Length);

            Assert.Equal(new string[] { "IFoo1", "IFoo3" }, foos.Select(e => (string)e.Metadata["Name"]));
        }

        [InheritedExport("Foo2")]
        [ExportMetadata("Name", "FooWithMultipleInheritedExports")]
        public class FooWithMultipleInheritedExports : IFoo1 { }

        [Fact]
        public void InheritedExport_MultipleDifferentContracts()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(FooWithMultipleInheritedExports));

            var foos = container.GetExports<object, IDictionary<string, object>>("Foo").ToArray();

            Assert.Equal(1, foos.Length);

            Assert.Equal("IFoo1", foos[0].Metadata["Name"]);

            var foo2s = container.GetExports<object, IDictionary<string, object>>("Foo2").ToArray();

            Assert.Equal(1, foo2s.Length);

            Assert.Equal("FooWithMultipleInheritedExports", foo2s[0].Metadata["Name"]);
        }
    }
}
