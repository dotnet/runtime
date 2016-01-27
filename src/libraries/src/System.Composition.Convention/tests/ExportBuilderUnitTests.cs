// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Convention.UnitTests;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Composition.Convention
{
    public class ExportBuilderUnitTests
    {
        public interface IFoo { }

        public class CFoo : IFoo { }

        [Fact]
        public void ExportInterfaceWithTypeOf1()
        {
            var builder = new ConventionBuilder();
            builder.ForType<CFoo>().Export<IFoo>();

            var exports = builder.GetDeclaredAttributes(typeof(CFoo), typeof(CFoo).GetTypeInfo()).Where<Attribute>(e => e is ExportAttribute).Cast<ExportAttribute>();
            Assert.Equal(1, exports.Count());
            Assert.Equal(exports.First().ContractType, typeof(IFoo));
        }

        [Fact]
        public void ExportInterfaceWithTypeOf2()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(CFoo)).Export((c) => c.AsContractType(typeof(IFoo)));

            var exports = builder.GetDeclaredAttributes(typeof(CFoo), typeof(CFoo).GetTypeInfo()).Where<Attribute>(e => e is ExportAttribute).Cast<ExportAttribute>();
            Assert.Equal(1, exports.Count());
            Assert.Equal(exports.First().ContractType, typeof(IFoo));
        }

        [Fact]
        public void ExportBuilderApiTestsNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("contractName", () => builder.ForTypesMatching((t) => true).Export(c => c.AsContractName(null as string)));
            ExceptionAssert.ThrownMessageContains<ArgumentException>("contractName", () => builder.ForTypesMatching((t) => true).Export(c => c.AsContractName("")));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("getContractNameFromPartType", () => builder.ForTypesMatching((t) => true).Export(c => c.AsContractName(null as Func<Type, string>)));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("type", () => builder.ForTypesMatching((t) => true).Export(c => c.AsContractType(null as Type)));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("name", () => builder.ForTypesMatching((t) => true).Export(c => c.AddMetadata(null as string, null as object)));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("name", () => builder.ForTypesMatching((t) => true).Export(c => c.AddMetadata(null as string, null as Func<Type, object>)));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("getValueFromPartType", () => builder.ForTypesMatching((t) => true).Export(c => c.AddMetadata("name", null as Func<Type, object>)));
        }
    }
}
