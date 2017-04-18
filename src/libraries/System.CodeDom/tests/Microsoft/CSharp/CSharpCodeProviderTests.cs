// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CSharp;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CSharpCodeProviderTests
    {
        [Fact]
        public void Ctor_Default()
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
#pragma warning disable 0618
            Assert.NotNull(provider.CreateGenerator());
            Assert.Same(provider.CreateGenerator(), provider.CreateCompiler());
#pragma warning restore 0618
        }

        public static IEnumerable<object[]> Ctor_IDictionary_TestData()
        {
            yield return new object[] { new Dictionary<string, string>() };
            yield return new object[] { new Dictionary<string, string>() { { "option", "value" } } };
            yield return new object[] { new Dictionary<string, string>() { { "option1", "value1" }, { "option2", "value2" } } };
            yield return new object[] { new Dictionary<string, string>() { { "option", null } } };
        }

        [Theory]
        [MemberData(nameof(Ctor_IDictionary_TestData))]
        public void Ctor_IDictionaryStringString(IDictionary<string, string> providerOptions)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
#pragma warning disable 0618
            Assert.NotNull(provider.CreateGenerator());
            Assert.Same(provider.CreateGenerator(), provider.CreateCompiler());
#pragma warning restore 0618
        }

        [Fact]
        public void Ctor_NullProviderOptions_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("providerOptions", () => new CSharpCodeProvider(null));
        }

        [Fact]
        public void LanguageOptions_ReturnsCaseInsensitive()
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            Assert.Equal(LanguageOptions.None, provider.LanguageOptions);
        }

        [Fact]
        public void FileExtension_ReturnsExpected()
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            Assert.Equal("cs", provider.FileExtension);
        }

        [Fact]
        public void CreateGenerator_ReturnsSame()
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
#pragma warning disable 0618
            Assert.Same(provider.CreateGenerator(), provider.CreateGenerator());
#pragma warning restore 0618
        }

        [Fact]
        public void CreateCompiler_ReturnsSame()
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
#pragma warning disable 0618
            Assert.Same(provider.CreateCompiler(), provider.CreateCompiler());
#pragma warning restore 0618
        }
    }
}
