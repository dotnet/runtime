// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Convention;
using System.Linq;
using System.Reflection;
using System.Composition.Convention.UnitTests;
using Xunit;

namespace System.Composition.Convention
{
    public class PartBuilderOfTInheritanceTests
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
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperty(p => p.P2); // P2 is string

            var importAttribute = GetAttributeFromMember(builder, typeof(DerClass), "P2") as ImportAttribute;

            Assert.NotNull(importAttribute);
            Assert.Null(importAttribute.ContractName);
        }

        [Fact]
        public void ImportPropertyTargetingBaseClass_ShouldGenerateImportManyForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperty(p => p.P3); // P3 is IEnumerable<int>

            var importManyAttribute = GetAttributeFromMember(builder, typeof(DerClass), "P3") as ImportManyAttribute;
            Assert.NotNull(importManyAttribute);
            Assert.Null(importManyAttribute.ContractName);
        }

        [Fact]
        public void ImportPropertyTargetingDerivedClass_ShouldGenerateImportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ImportProperty(p => p.P4); // P4 is string

            var importAttribute = GetAttributeFromMember(builder, typeof(DerClass), "P4") as ImportAttribute;

            Assert.NotNull(importAttribute);
            Assert.Null(importAttribute.ContractName);
        }

        [Fact]
        public void ExportPropertyTargetingDerivedClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ExportProperty(p => p.P4); // P4 is string

            var exportAttribute = GetAttributeFromMember(builder, typeof(DerClass), "P4") as ExportAttribute;

            Assert.NotNull(exportAttribute);
            Assert.Null(exportAttribute.ContractName);
            Assert.Null(exportAttribute.ContractType);
        }

        [Fact]
        public void ExportPropertyTargetingBaseClass_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<DerClass>().ExportProperty(p => p.P2); // P2 is string

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
