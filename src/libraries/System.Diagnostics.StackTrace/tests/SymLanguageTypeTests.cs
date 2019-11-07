// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    public class SymLanguageTypeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            Assert.NotNull(new SymLanguageType());
        }

        public static IEnumerable<object[]> LanguageTypes_TestData()
        {
            yield return new object[] { SymLanguageType.C, new Guid("63a08714-fc37-11d2-904c-00c04fa302a1") };
            yield return new object[] { SymLanguageType.CPlusPlus, new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2") };
            yield return new object[] { SymLanguageType.CSharp, new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1") };
            yield return new object[] { SymLanguageType.Basic, new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2") };
            yield return new object[] { SymLanguageType.Java, new Guid("3a12d0b4-c26c-11d0-b442-00a0244a1dd2") };
            yield return new object[] { SymLanguageType.Cobol, new Guid("af046cd1-d0e1-11d2-977c-00a0c9b4d50c") };
            yield return new object[] { SymLanguageType.Pascal, new Guid("af046cd2-d0e1-11d2-977c-00a0c9b4d50c") };
            yield return new object[] { SymLanguageType.ILAssembly, new Guid("af046cd3-d0e1-11d2-977c-00a0c9b4d50c") };
            yield return new object[] { SymLanguageType.JScript, new Guid("3a12d0b6-c26c-11d0-b442-00a0244a1dd2") };
            yield return new object[] { SymLanguageType.SMC, new Guid("0d9b9f7b-6611-11d3-bd2a-0000f80849bd") };
            yield return new object[] { SymLanguageType.MCPlusPlus, new Guid("4b35fde8-07c6-11d3-9053-00c04fa302a1") };
        }

        [Theory]
        [MemberData(nameof(LanguageTypes_TestData))]
        public void LanguageTypes_Get_ReturnsExpected(Guid languageType, Guid expected)
        {
            Assert.Equal(expected, languageType);
        }
    }
}
