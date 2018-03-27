using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCacheTests : RemoteExecutorTestBase
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
            RemoteInvoke(() =>
            {
                Regex.CacheSize = 1;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2")); // previous removed from cache
                Assert.True(GetCachedItemsNum() == 1);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_Cache_Shrink_cache()
        {
            RemoteInvoke(() =>
            {
                Regex.CacheSize = 2;
                Assert.True(Regex.IsMatch("1", "1"));
                Assert.True(Regex.IsMatch("2", "2"));
                Assert.True(GetCachedItemsNum() == 2);
                Regex.CacheSize = 1;
                Assert.True(GetCachedItemsNum() == 1);
                Regex.CacheSize = 0; // clear
                Assert.True(GetCachedItemsNum() == 0);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_Cache_Promote_entries()
        {
            RemoteInvoke(() =>
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
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_Cache_Uses_culture_and_options()
        {
            RemoteInvoke(() =>
            {
                Regex.CacheSize = 0;
                Regex.CacheSize = 3;
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.IgnoreCase));
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.Multiline));
                Assert.True(GetCachedItemsNum() == 2);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.True(Regex.IsMatch("1", "1", RegexOptions.Multiline));
                Assert.True(GetCachedItemsNum() == 3);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // different cache structure
        public void Ctor_Cache_Uses_dictionary_linked_list_switch_does_not_throw()
        {
            // assume the limit is less than the cache size so we cross it two times:
            RemoteInvoke(() =>
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
                return SuccessExitCode;
            }).Dispose();
        }

        private int GetCachedItemsNum()
        {
            // On .NET Framework we have a different cache structure.
            if (PlatformDetection.IsFullFramework)
            {
                object linkedList = typeof(Regex)
                    .GetField("livecode", BindingFlags.NonPublic | BindingFlags.Static)
                    .GetValue(null);
                return (int)linkedList.GetType()
                    .GetProperty("Count", BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(linkedList);
            }

            string cacheFieldName = PlatformDetection.IsFullFramework ? "cacheSize" : "s_cacheCount";
            return (int)typeof(Regex)
                .GetField(cacheFieldName, BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
        }
    }
}
