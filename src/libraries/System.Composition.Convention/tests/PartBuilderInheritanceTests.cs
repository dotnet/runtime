// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Convention.UnitTests;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention
{
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
        public void ImportPropertyTargetingBaseClass_ShouldGenerateImportAttributeForP1Selected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperties((p) => p.Name == "P1");                // P1 is string

            var pAttr = GetAttributeFromMember(builder, typeof(DerClass), "P1") as ImportAttribute;

            Assert.NotNull(pAttr);                   // Ensure P1 has ImportAttribute (default configured)
            Assert.Null(pAttr.ContractName);
        }

        [Fact]
        public void ImportPropertyTargetingBaseClass_ShouldGenerateImportManyForP3Selected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperties((p) => p.Name == "P3");                // P2 is Enumerable

            var pAttr = GetAttributeFromMember(builder, typeof(DerClass), "P3") as ImportManyAttribute;

            Assert.NotNull(pAttr);                   // Ensure P3 has ImportManyAttribute (default configured)
            Assert.Null(pAttr.ContractName);
        }


        [Fact]
        public void ImportPropertyTargetingDerivedClass_ShouldGenerateImportAttributeForP4Selected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperties((p) => p.Name == "P4");                // P1 is string

            var pAttr = GetAttributeFromMember(builder, typeof(DerClass), "P4") as ImportAttribute;

            Assert.NotNull(pAttr);                   // Ensure P1 has ImportAttribute (default configured)
            Assert.Null(pAttr.ContractName);
        }


        [Fact]
        public void ExportPropertyTargetingDerivedClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ExportProperties((p) => p.Name == "P4");                // P1 is string

            var pAttr = GetAttributeFromMember(builder, typeof(DerClass), "P4") as ExportAttribute;

            Assert.NotNull(pAttr);
            Assert.Null(pAttr.ContractName);
            Assert.Null(pAttr.ContractType);
        }

        [Fact]
        public void ExportPropertyTargetingBaseClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ExportProperties((p) => p.Name == "P2");                // P2 is string

            var exportAttribute = GetAttributeFromMember(builder, typeof(DerClass), "P2") as ExportAttribute;

            Assert.NotNull(exportAttribute);
            Assert.Null(exportAttribute.ContractName);
            Assert.Null(exportAttribute.ContractType);
        }

        private static Attribute GetAttributeFromMember(ConventionBuilder builder, Type type, string member)
        {
            var pi = type.GetRuntimeProperty(member);
            var list = builder.GetDeclaredAttributes(type, pi);
            return list[0] as Attribute;
        }
    }
}
