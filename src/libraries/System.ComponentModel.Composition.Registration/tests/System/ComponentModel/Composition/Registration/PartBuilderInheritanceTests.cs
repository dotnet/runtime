// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
    public class PartBuilderInheritanceTests
    {
        private abstract class BaseClass
        {
            public string P1 { get; set; }
            public string P2 { get; set; }
            public IEnumerable<int> P3 { get; set; }
        }

        private class DerClass : BaseClass
        {
            public string P4 { get; set; }
            public string P5 { get; set; }
        }

        [Fact]
        public void ImportPropertyTargetingBaseClass_ShouldGenerateImportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.ImportProperties(p => p.Name == "P2"); // P2 is string

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(DerClass));

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(BaseClass).GetProperty("P2"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var importAttribute = atts[0] as ImportAttribute;
            Assert.NotNull(importAttribute);
            Assert.Null(importAttribute.ContractName);
            Assert.Null(importAttribute.ContractType);
        }

        [Fact]
        public void ImportPropertyTargetingBaseClass_ShouldGenerateImportManyForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.ImportProperties(p => p.Name == "P3"); // P3 is IEnumerable<int>

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(DerClass));

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(BaseClass).GetProperty("P3"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var importManyAttribute = atts[0] as ImportManyAttribute;
            Assert.NotNull(importManyAttribute);
            Assert.Null(importManyAttribute.ContractName);
            Assert.Null(importManyAttribute.ContractType);
        }

        [Fact]
        public void ImportPropertyTargetingDerivedClass_ShouldGenerateImportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.ImportProperties(p => p.Name == "P4"); // P4 is string

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(DerClass));

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(DerClass).GetProperty("P4"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var importAttribute = atts[0] as ImportAttribute;
            Assert.NotNull(importAttribute);
            Assert.Null(importAttribute.ContractName);
            Assert.Null(importAttribute.ContractType);
        }

        [Fact]
        public void ExportPropertyTargetingDerivedClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.ExportProperties(p => p.Name == "P4"); // P4 is string

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(DerClass));

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(DerClass).GetProperty("P4"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var exportAttribute = atts[0] as ExportAttribute;
            Assert.NotNull(exportAttribute);
            Assert.Null(exportAttribute.ContractName);
            Assert.Null(exportAttribute.ContractType);
        }

        [Fact]
        public void ExportPropertyTargetingBaseClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.ExportProperties(p => p.Name == "P2"); // P2 is string

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(DerClass));

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(BaseClass).GetProperty("P2"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var exportAttribute = atts[0] as ExportAttribute;
            Assert.NotNull(exportAttribute);
            Assert.Null(exportAttribute.ContractName);
            Assert.Null(exportAttribute.ContractType);
        }

        private static void GetConfiguredMembers(PartBuilder builder,
            out List<Tuple<object, List<Attribute>>> configuredMembers, out IEnumerable<Attribute> typeAtts,
            Type targetType)
        {
            configuredMembers = new List<Tuple<object, List<Attribute>>>();
            typeAtts = builder.BuildTypeAttributes(targetType);
            builder.BuildConstructorAttributes(targetType, ref configuredMembers);
            builder.BuildPropertyAttributes(targetType, ref configuredMembers);
        }
    }
}
