// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Globalization;

namespace System.Globalization.Extensions.Tests
{
    public class StringNormalization
    {
        [Fact]
        public void NormalizeTest()
        {
            string composed = "\u00C4\u00C7"; // "ÄÇ"
            string decomposed = "A\u0308C\u0327";

            Assert.Throws<ArgumentNullException>("value", () => { string nullString = null; nullString.IsNormalized(); });

            Assert.True(composed.IsNormalized());
            Assert.True(composed.IsNormalized(NormalizationForm.FormC));
            Assert.False(composed.IsNormalized(NormalizationForm.FormD));

            Assert.True(decomposed.IsNormalized(NormalizationForm.FormD));
            Assert.False(decomposed.IsNormalized(NormalizationForm.FormC));

            Assert.True("".Normalize(NormalizationForm.FormC).Length == 0);

            Assert.True(composed.Normalize(NormalizationForm.FormD).Equals(decomposed));
            Assert.True(decomposed.Normalize().Equals(composed));
            Assert.True(decomposed.Normalize(NormalizationForm.FormC).Equals(composed));

            string fi = "\uFB01";
            string decomposedFi = "fi";

            Assert.True(fi.Normalize(NormalizationForm.FormC).Equals(fi));
            Assert.True(fi.Normalize(NormalizationForm.FormD).Equals(fi));
            Assert.True(fi.Normalize(NormalizationForm.FormKD).Equals(decomposedFi));
            Assert.True(fi.Normalize(NormalizationForm.FormKC).Equals(decomposedFi));

            string fwith2dots = "\u1E9b\u0323";
            string decomposedFwith2dots = "\u017f\u0323\u0307";
            string decomposedCompatFwith2dots = "\u0073\u0323\u0307";
            string composedCompatFwith2dots = "\u1E69";

            Assert.True(fwith2dots.Normalize(NormalizationForm.FormC).Equals(fwith2dots));
            Assert.True(fwith2dots.Normalize(NormalizationForm.FormD).Equals(decomposedFwith2dots));
            Assert.True(fwith2dots.Normalize(NormalizationForm.FormKC).Equals(composedCompatFwith2dots));
            Assert.True(fwith2dots.Normalize(NormalizationForm.FormKD).Equals(decomposedCompatFwith2dots));
        }

        [Fact]
        public void ExceptionsTest()
        {
            string fi = "\uFB01";
            //  "Expected to throw with invalid Normalization"
            Assert.Throws<ArgumentException>(() => fi.IsNormalized((NormalizationForm)10));
            // "Expected to throw with invalid Normalization"
            Assert.Throws<ArgumentException>(() => fi.Normalize((NormalizationForm)7));

            string invalidCodepoint = "\uFFFE";
            string invalidSurrogate = "\uD800\uD800";

            // "Expected to throw with invalid codepoint"
            Assert.Throws<ArgumentException>(() => invalidCodepoint.Normalize());
            Assert.Throws<ArgumentException>(() => invalidCodepoint.IsNormalized());
            // "Expected to throw with invalid surrogate pair"
            Assert.Throws<ArgumentException>(() => invalidSurrogate.Normalize());
            Assert.Throws<ArgumentException>(() => invalidSurrogate.IsNormalized());

            //  "Expected ArgumentNullException when passing null string"
            Assert.Throws<ArgumentNullException>(() => StringNormalizationExtensions.Normalize(null));
            Assert.Throws<ArgumentNullException>(() => StringNormalizationExtensions.IsNormalized(null));
        }
    }
}
