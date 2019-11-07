// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ContractTests
    {
        private class AType { }

        private static readonly Type s_defaultContractType = typeof(AType);

        [Fact]
        public void FormattingAContractWithNoDiscriminatorShowsTheSimpleTypeName()
        {
            var c = new CompositionContract(typeof(AType));
            var s = c.ToString();
            Assert.Equal("AType", s);
        }

        [Fact]
        public void FormattingAContractWithAStringDiscriminatorShowsTheDiscriminatorInQuotes()
        {
            var c = new CompositionContract(typeof(AType), "at");
            var s = c.ToString();
            Assert.Equal("AType \"at\"", s);
        }

        [Fact]
        public void ChangingTheTypeOfAContractPreservesTheContractName()
        {
            var name = "a";
            var c = new CompositionContract(typeof(object), name);
            var d = c.ChangeType(typeof(AType));
            Assert.Equal(name, d.ContractName);
        }

        [Fact]
        public void ChangingTheTypeOfAContractChangesTheContractType()
        {
            var c = new CompositionContract(typeof(object));
            var d = c.ChangeType(typeof(AType));
            Assert.Equal(typeof(AType), d.ContractType);
        }

        [Fact]
        public void ConstraintsWithEquivalentKeysAndValuesAreEqual()
        {
            var mcd1 = new CompositionContract(s_defaultContractType, null, new Dictionary<string, object> { { "A", new[] { "B" } } });
            var mcd2 = new CompositionContract(s_defaultContractType, null, new Dictionary<string, object> { { "A", new[] { "B" } } });
            Assert.True(mcd1.Equals(mcd2));
        }

        [Fact]
        public void ConstraintsWithEquivalentKeysAndValuesHaveTheSameHashCode()
        {
            var mcd1 = new CompositionContract(s_defaultContractType, null, new Dictionary<string, object> { { "A", new[] { "B" } } });
            var mcd2 = new CompositionContract(s_defaultContractType, null, new Dictionary<string, object> { { "A", new[] { "B" } } });
            Assert.Equal(mcd1.GetHashCode(), mcd2.GetHashCode());
        }

        [Fact]
        public void FormattingTheContractPrintsConstraintKeysAndValues()
        {
            var mcd = new CompositionContract(typeof(AType), null, new Dictionary<string, object> { { "A", 1 }, { "B", "C" } });
            var s = mcd.ToString();
            Assert.Equal("AType { A = 1, B = \"C\" }", s);
        }

        [Fact]
        public void FormattingTheContractPrintsNameAndDiscriminator()
        {
            var mcd = new CompositionContract(typeof(AType), "inner", new Dictionary<string, object> { { "A", 1 } });
            var s = mcd.ToString();
            Assert.Equal("AType \"inner\" { A = 1 }", s);
        }

        [Fact]
        public void AContractWithConstraintIsNotEqualToAContractWithoutConstraint()
        {
            var first = new CompositionContract(typeof(string), null, new Dictionary<string, object> { { "A", 1 } });
            var second = new CompositionContract(typeof(string));
            Assert.False(first.Equals(second));
        }
    }
}
