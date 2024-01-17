// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoHashCodeTests : CompareInfoTestsBase
    {
        public class CustomComparer : StringComparer
        {
            private readonly CompareInfo _compareInfo;
            private readonly CompareOptions _compareOptions;

            public CustomComparer(CompareInfo cmpInfo)
            {
                _compareInfo = cmpInfo;
                _compareOptions = CompareOptions.IgnoreCase;
            }

            public override int Compare(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions);

            public override bool Equals(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions) == 0;

            public override int GetHashCode(string obj) =>
                _compareInfo.GetHashCode(obj, _compareOptions);
        }

        
        public static IEnumerable<object[]> HashCodeLocalized_TestData()
        {
            yield return new object[] { s_invariantCompare, "foo", "Foo", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "İGLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase };

            if (!PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { new CultureInfo("ja-JP").CompareInfo, "\u30A2", "\u3042", CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "café", "cafe\u0301", CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "100", "100!", CompareOptions.IgnoreSymbols }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "İGLOO", CompareOptions.IgnoreCase }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase }; // HG: equal: False, hashCodesEqual: True
            }
            
        }

        [Theory]
        [MemberData(nameof(HashCodeLocalized_TestData))]
        public void HashCodeLocalized(CompareInfo cmpInfo, string str1, string str2, CompareOptions options)
        {
            bool areEqual = cmpInfo.Compare(str1, str2, options) == 0;
            var hashCode1 = cmpInfo.GetHashCode(str1, options);
            var hashCode2 = cmpInfo.GetHashCode(str2, options);
            bool areHashCodesEqual = hashCode1 == hashCode2;
            if (areEqual)
            {
                Assert.True(areHashCodesEqual);
            }
            else
            {
                Assert.False(areHashCodesEqual);
            }

            // implication of the above behavior:
            Dictionary<string, int> customDictionary = new Dictionary<string, int>(new CustomComparer(cmpInfo));
            customDictionary.Add(str1, 0);
            if (customDictionary.ContainsKey(str2))
            {
                Assert.True(areHashCodesEqual);
            }
            else
            {
                customDictionary.Add(str2, 1);
                Assert.False(areHashCodesEqual);
            }
        }
    }
}
