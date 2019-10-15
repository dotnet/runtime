// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class SymbolDocumentInfoTests
    {
        public static IEnumerable<object[]> TestData()
        {
            Guid documentType = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd);
            yield return new object[] { "", Guid.Empty, Guid.Empty, documentType };
            yield return new object[] { " \t \r ", Guid.Empty, Guid.NewGuid(), documentType };
            yield return new object[] { "abc", Guid.NewGuid(), Guid.NewGuid(), documentType };
            yield return new object[] { "\uD800\uDC00", Guid.NewGuid(), Guid.NewGuid(), Guid.Empty };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void SymbolDocument(string fileName, Guid language, Guid languageVendor, Guid documentType)
        {
            if (documentType == new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd))
            {
                if (languageVendor == Guid.Empty)
                {
                    if (language == Guid.Empty)
                    {
                        // SymbolDocument(string)
                        SymbolDocumentInfo symbolDocument1 = Expression.SymbolDocument(fileName);
                        VerifySymbolDocumentInfo(symbolDocument1, fileName, language, languageVendor, documentType);
                    }
                    // SymbolDocument(string, Guid)
                    SymbolDocumentInfo symbolDocument2 = Expression.SymbolDocument(fileName, language);
                    VerifySymbolDocumentInfo(symbolDocument2, fileName, language, languageVendor, documentType);
                }
                // SymbolDocument(string, Guid)
                SymbolDocumentInfo symbolDocument3 = Expression.SymbolDocument(fileName, language, languageVendor);
                VerifySymbolDocumentInfo(symbolDocument3, fileName, language, languageVendor, documentType);
            }
            // SymbolDocument(string, Guid, Guid)
            SymbolDocumentInfo symbolDocument4 = Expression.SymbolDocument(fileName, language, languageVendor, documentType);
            VerifySymbolDocumentInfo(symbolDocument4, fileName, language, languageVendor, documentType);
        }

        [Fact]
        public static void SymbolDocument_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Expression.SymbolDocument(null));
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Expression.SymbolDocument(null, Guid.Empty));
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Expression.SymbolDocument(null, Guid.Empty, Guid.Empty));
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Expression.SymbolDocument(null, Guid.Empty, Guid.Empty, Guid.Empty));
        }

        private static void VerifySymbolDocumentInfo(SymbolDocumentInfo document, string fileName, Guid language, Guid languageVendor, Guid documentType)
        {
            Assert.Equal(fileName, document.FileName);
            Assert.Equal(language, document.Language);
            Assert.Equal(languageVendor, document.LanguageVendor);
            Assert.Equal(documentType, document.DocumentType);
        }
    }
}
