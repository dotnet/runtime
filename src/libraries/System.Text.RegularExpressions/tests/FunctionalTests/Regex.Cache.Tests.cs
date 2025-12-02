// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public class RegexCacheTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        public void CacheSize_Set(int newCacheSize)
        {
            int originalCacheSize = Regex.CacheSize;

            try
            {
                Regex.CacheSize = newCacheSize;
                Assert.Equal(newCacheSize, Regex.CacheSize);
            }
            finally
            {
                Regex.CacheSize = originalCacheSize;
            }
        }

        [Fact]
        public void CacheSize_Set_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => Regex.CacheSize = -1);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Ctor_Cache_Second_drops_first()
        {
            RemoteExecutor.Invoke(() =>
            {
                Regex.CacheSize = 1;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2")); // previous removed from cache
                Assert.True(GetCachedItemsNum() == 1);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Ctor_Cache_Shrink_cache()
        {
            RemoteExecutor.Invoke(() =>
            {
                Regex.CacheSize = 2;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2"));
                Assert.True(GetCachedItemsNum() == 2);
                Regex.CacheSize = 1;
                Assert.True(GetCachedItemsNum() == 1);
                Regex.CacheSize = 0; // clear
                Assert.True(GetCachedItemsNum() == 0);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Ctor_Cache_Promote_entries()
        {
            RemoteExecutor.Invoke(() =>
            {
                Regex.CacheSize = 3;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2"));
                Assert.True(Regex.IsMatch("3", "3"));
                Assert.True(GetCachedItemsNum() == 3);
                Assert.True(Regex.IsMatch("1", "1")); // should be put first
                Assert.True(GetCachedItemsNum() == 3);
                Regex.CacheSize = 1;  // only 1 stays
                Assert.True(GetCachedItemsNum() == 1);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Ctor_Cache_Uses_culture_and_options()
        {
            RemoteExecutor.Invoke(() =>
            {
                Regex.CacheSize = 0;
                Regex.CacheSize = 3;
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.IgnoreCase));
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.Multiline));
                Assert.True(GetCachedItemsNum() == 2);
                // Force to set a different culture than the current culture!
                CultureInfo.CurrentCulture = CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("de-DE")) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("de-DE");
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.Multiline));
                Assert.True(GetCachedItemsNum() == 3);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Ctor_Cache_Uses_dictionary_linked_list_switch_does_not_throw()
        {
            // assume the limit is less than the cache size so we cross it two times:
            RemoteExecutor.Invoke(() =>
            {
                int original = Regex.CacheSize;
                Regex.CacheSize = 0;
                Fill(original);
                const int limit = 10;
                Regex.CacheSize = limit - 1;
                Regex.CacheSize = 0;
                Fill(original);
                Remove(original);

                void Fill(int n)
                {
                    for (int i = 0; i < n; i++)
                    {
                        Regex.CacheSize++;
                        Assert.True(Regex.IsMatch(i.ToString(), i.ToString()));
                        Assert.True(GetCachedItemsNum() == i + 1);
                    }
                }

                void Remove(int n)
                {
                    for (int i = 0; i < original; i++)
                    {
                        Regex.CacheSize--;
                        Assert.True(GetCachedItemsNum() == Regex.CacheSize);
                    }
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Cache_Add_additional_pattern_exceed_max_size_should_not_remove_previous_new_one()
        {
            RemoteExecutor.Invoke(() =>
            {
                string pattern;

                // Fill cache list
                for (int round = 0; round < 2; round++)
                {
                    for (int i = 0; i < Regex.CacheSize; i++)
                    {
                        pattern = i.ToString();

                        _ = Regex.IsMatch(pattern, pattern);
                    }
                }

                // Adding additional patterns which triggers cache item removal but should not remove previous New one
                for (int i = Regex.CacheSize; i < Regex.CacheSize * 2; i++)
                {
                    pattern = i.ToString();

                    _ = Regex.IsMatch(pattern, pattern);
                    Assert.Contains(GetCachedRegexList(), r => r.ToString() == (i - 1).ToString());
                }

                static IEnumerable<Regex> GetCachedRegexList()
                {
                    IList innerCacheList = GetInnerCacheList();

                    foreach (object node in innerCacheList)
                    {
                        yield return GetRegex(node);
                    }
                }

                static Regex GetRegex(object node)
                {
                    Type nodeType = typeof(Regex).Assembly.GetType("System.Text.RegularExpressions.RegexCache+Node")!;
                    FieldInfo regexField = nodeType.GetField("Regex", BindingFlags.Public | BindingFlags.Instance)!;
                    return (Regex)regexField.GetValue(node)!;
                }
            }).Dispose();
        }

        private static int GetCachedItemsNum()
        {
            return GetInnerCacheList().Count;
        }

        private static IList GetInnerCacheList()
        {
            Type regexCacheType = typeof(Regex).Assembly.GetType("System.Text.RegularExpressions.RegexCache")!;

            FieldInfo cacheListFieldInfo = regexCacheType.GetField("s_cacheList", BindingFlags.Static | BindingFlags.NonPublic)!;
            return (IList)cacheListFieldInfo.GetValue(null)!;
        }
    }
}
