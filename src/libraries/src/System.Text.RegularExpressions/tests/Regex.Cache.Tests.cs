// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
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

        [Fact]
        public void Ctor_Cache_Second_drops_first()
        {
            RemoteExecutor.Invoke(() =>
            {
                Regex.CacheSize = 1;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2")); // previous removed from cache
                Assert.True(GetCachedItemsNum() == 1);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        private int GetCachedItemsNum()
        {
            return (int)typeof(Regex)
                .GetField("s_cacheCount", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
        }
    }
}
