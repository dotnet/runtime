// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class MetadataAttributeTests
    {
        [Fact]
        [Trait("Type", "Integration")]
        public void UntypedStructureTest()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(AttributedModelServices.CreatePart(new BasicTestComponent()));
            container.Compose(batch);
            var export = container.GetExport<BasicTestComponent, IDictionary<string, object>>();

            Assert.NotNull(export.Metadata);
            Assert.Equal("One", export.Metadata["String1"]);
            Assert.Equal("Two", export.Metadata["String2"]);
            var e = export.Metadata["Numbers"] as IList<int>;
            Assert.NotNull(e);
            Assert.True(e.Contains(1), "Should have 1 in the list");
            Assert.True(e.Contains(2), "Should have 2 in the list");
            Assert.True(e.Contains(3), "Should have 3 in the list");
            Assert.Equal(3, e.Count);
        }

        // Silverlight doesn't support strongly typed metadata
        [Fact]
        [Trait("Type", "Integration")]
        public void StronglyTypedStructureTest()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(AttributedModelServices.CreatePart(new BasicTestComponent()));
            container.Compose(batch);

            var export = container.GetExport<BasicTestComponent, IStronglyTypedStructure>();

            Assert.NotNull(export.Metadata);
            Assert.Equal("One", export.Metadata.String1);
            Assert.Equal("Two", export.Metadata.String2);
            Assert.True(export.Metadata.Numbers.Contains(1), "Should have 1 in the list");
            Assert.True(export.Metadata.Numbers.Contains(2), "Should have 2 in the list");
            Assert.True(export.Metadata.Numbers.Contains(3), "Should have 3 in the list");
            Assert.Equal(3, export.Metadata.Numbers.Length);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void StronglyTypedStructureTestWithTransparentViews()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(AttributedModelServices.CreatePart(new BasicTestComponent()));
            container.Compose(batch);

            var export = container.GetExport<BasicTestComponent, ITrans_StronglyTypedStructure>();

            Assert.NotNull(export.Metadata);
            Assert.Equal("One", export.Metadata.String1);
            Assert.Equal("Two", export.Metadata.String2);
            Assert.True(export.Metadata.Numbers.Contains(1), "Should have 1 in the list");
            Assert.True(export.Metadata.Numbers.Contains(2), "Should have 2 in the list");
            Assert.True(export.Metadata.Numbers.Contains(3), "Should have 3 in the list");
            Assert.Equal(3, export.Metadata.Numbers.Length);
        }

        [Export]
        // Should cause a conflict with the multiple nature of Name.Bar because 
        // it isn't marked with IsMultiple=true
        [ExportMetadata("Bar", "Blah")]
        [Name("MEF")]
        [Name("MEF2")]
        [PartNotDiscoverable]
        public class BasicTestComponentWithInvalidMetadata
        {
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void InvalidMetadataAttributeTest()
        {
            ComposablePart part = AttributedModelServices.CreatePart(new BasicTestComponentWithInvalidMetadata());
            ExportDefinition export = part.ExportDefinitions.First();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var metadata = export.Metadata;
            });

            Assert.True(ex.Message.Contains("Bar"));
        }

        [AttributeUsage(AttributeTargets.All)]
        [MetadataAttribute]
        public class MetadataWithInvalidCustomAttributeType : Attribute
        {
            public PersonClass Person { get { return new PersonClass(); } }

            public class PersonClass
            {
                public string First { get { return "George"; } }
                public string Last { get { return "Washington"; } }
            }
        }

        [Export]
        [MetadataWithInvalidCustomAttributeType]
        [PartNotDiscoverable]
        public class ClassWithInvalidCustomAttributeType
        {

        }

        [Fact]
        public void InvalidAttributType_CustomType_ShouldThrow()
        {
            ComposablePart part = AttributedModelServices.CreatePart(new ClassWithInvalidCustomAttributeType());
            ExportDefinition export = part.ExportDefinitions.First();

            // Should throw InvalidOperationException during discovery because
            // the person class is an invalid metadata type
            Assert.Throws<InvalidOperationException>(() =>
            {
                var metadata = export.Metadata;
            });
        }

        [AttributeUsage(AttributeTargets.All)]
        [MetadataAttribute]
        public class MetadataWithInvalidVersionPropertyAttributeType : Attribute
        {
            public MetadataWithInvalidVersionPropertyAttributeType()
            {
                this.Version = new Version(1, 1);
            }
            public Version Version { get; set; }
        }

        [Export]
        [MetadataWithInvalidVersionPropertyAttributeType]
        [PartNotDiscoverable]
        public class ClassWithInvalidVersionPropertyAttributeType
        {

        }

        [Fact]
        public void InvalidAttributType_VersionPropertyType_ShouldThrow()
        {
            ComposablePart part = AttributedModelServices.CreatePart(new ClassWithInvalidVersionPropertyAttributeType());
            ExportDefinition export = part.ExportDefinitions.First();

            // Should throw InvalidOperationException during discovery because
            // the person class is an invalid metadata type
            Assert.Throws<InvalidOperationException>(() =>
            {
                var metadata = export.Metadata;
            });
        }

        [MetadataAttribute]
        public class BaseMetadataAttribute : Attribute
        {
            public string BaseKey { get { return "BaseValue"; } }
        }

        public class DerivedMetadataAttribute : BaseMetadataAttribute
        {
            public string DerivedKey { get { return "DerivedValue"; } }
        }

        [Export]
        [DerivedMetadata]
        public class ExportWithDerivedMetadataAttribute { }

        [Fact]
        public void DerivedMetadataAttributeAttribute_ShouldSupplyMetadata()
        {
            ComposablePart part = AttributedModelServices.CreatePart(new ExportWithDerivedMetadataAttribute());
            ExportDefinition export = part.ExportDefinitions.Single();

            Assert.Equal("BaseValue", export.Metadata["BaseKey"]);
            Assert.Equal("DerivedValue", export.Metadata["DerivedKey"]);
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    [MetadataAttribute]
    public class BasicMetadataAttribute : Attribute
    {
        public string String1 { get { return "One"; } }

        public string String2 { get { return "Two"; } }

        public int[] Numbers { get { return new int[] { 1, 2, 3 }; } }

        public CreationPolicy Policy { get { return CreationPolicy.NonShared; } }

        public Type Type { get { return typeof(BasicMetadataAttribute); } }
    }

    public interface IStronglyTypedStructure
    {
        string String1 { get; }
        string String2 { get; }
        int[] Numbers { get; }
        CreationPolicy Policy { get; }
        Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [MetadataAttribute]
    public class Name : Attribute
    {
        public Name(string name) { Bar = name; }

        public string Bar { set; get; }
    }

    [PartNotDiscoverable]
    [Export]
    [BasicMetadata]
    public class BasicTestComponent
    {
    }
}
