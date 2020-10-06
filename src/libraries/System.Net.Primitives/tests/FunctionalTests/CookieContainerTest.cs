// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public partial class CookieContainerTest
    {
        private const string u1 = ".url1.com"; // Basic domain
        private const string u2 = "127.0.0.1"; // Local domain
        private const string u3 = "url3.com"; // Basic domain without a leading dot
        private static readonly Uri u4 = new Uri("http://url4.com"); // Basic uri
        private static readonly Uri u5 = new Uri("http://url5.com/path"); // Set explicit path
        private static readonly Uri u6 = new Uri("http://url6.com"); // No cookies should be added with this uri

        private static readonly Cookie c1 = new Cookie("name1", "value", "", u1);
        private static readonly Cookie c2 = new Cookie("name2", "value", "", u1) { Secure = true };
        private static readonly Cookie c3 = new Cookie("name3", "value", "", u1) { Port = "\"80, 90, 100, 443\"" };
        private static readonly Cookie c4 = new Cookie("name4", "value", "", u2);
        private static readonly Cookie c5 = new Cookie("name5", "value", "/path", u2);
        private static readonly Cookie c6 = new Cookie("name6", "value", "", "." + u3);
        private static readonly Cookie c7 = new Cookie("name7", "value", "", "." + u3);
        private static readonly Cookie c8 = new Cookie("name8", "value");
        private static readonly Cookie c9 = new Cookie("name9", "value");
        private static readonly Cookie c10 = new Cookie("name10", "value");
        private static readonly Cookie c11 = new Cookie("name11", "value") { Port = "\"80, 90, 100\"" };

        [Fact]
        public static void Ctor_Empty_Success()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(0, cc.Count);
        }

        [Fact]
        public static void Capacity_GetSet_Success()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(CookieContainer.DefaultCookieLimit, cc.Capacity);

            cc.Capacity = 900;
            Assert.Equal(900, cc.Capacity);

            cc.Capacity = 40; // Shrink
            Assert.Equal(40, cc.Capacity);
        }

        [Fact]
        public static void Capacity_Set_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentOutOfRangeException>(() => cc.Capacity = 0); // <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.Capacity = cc.PerDomainCapacity - 1); // < per domain capacity
        }

        [Fact]
        public static void MaxCookieSize_GetSet_Success()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(CookieContainer.DefaultCookieLengthLimit, cc.MaxCookieSize);

            cc.MaxCookieSize = 8192;
            Assert.Equal(8192, cc.MaxCookieSize);
        }

        [Fact]
        public static void MaxCookieSize_Set_Invalid()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.MaxCookieSize = 0); // Max cookie size <= 0
        }

        [Fact]
        public static void PerDomainCapacity_GetSet()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(CookieContainer.DefaultPerDomainCookieLimit, cc.PerDomainCapacity);

            cc.PerDomainCapacity = 50;
            Assert.Equal(50, cc.PerDomainCapacity);

            cc.PerDomainCapacity = 40; // Shrink
            Assert.Equal(40, cc.PerDomainCapacity);

            // Shrink to one - this should get rid of all cookies since there are no possible cookies that can be expired
            cc.PerDomainCapacity = 1;
            Assert.Equal(1, cc.PerDomainCapacity);
            Assert.Equal(0, cc.Count);
        }

        [Fact]
        public static void PerDomainCapacity_Set_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentOutOfRangeException>(() => cc.PerDomainCapacity = 0); // Per domain capacity <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.PerDomainCapacity = cc.Capacity + 1); // Per domain capacity >= Capacity
        }

        [Fact]
        public static void Add_CookieUri_Invalid()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentNullException>(() => cc.Add(null, new Cookie("name", "value"))); // Null uri
            Assert.Throws<ArgumentNullException>(() => cc.Add(new Uri("http://contoso.com"), (Cookie)null)); // Null cookie
        }

        [Fact]
        public static void Add_CookieCollectionUri_Invalid()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentNullException>(() => cc.Add(null, new CookieCollection())); //Null uri
            Assert.Throws<ArgumentNullException>(() => cc.Add(new Uri("http://contoso.com"), (CookieCollection)null)); //Null collection
        }

        [Fact]
        public static void GetCookies_Invalid()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentNullException>(() => cc.GetCookies(null));
        }

        [Fact]
        public static void GetCookieHeader_Invalid()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentNullException>(() => cc.GetCookieHeader(null));
        }

        public static IEnumerable<object[]> SetCookiesInvalidData()
        {
            yield return new object[] { u5, "=value" }; // No name
            yield return new object[] { u5, "$=value" }; // Invalid name
            yield return new object[] { new Uri("http://url.com"), "na\tme=value; domain=.domain.com" }; // Invalid name
            yield return new object[] { new Uri("http://url.com"), "name=value; domain=.domain.com" }; // Domain not the same
            yield return new object[] { new Uri("http://url.com:90"), "name=value; port=\"80\"" }; // Port not the same
            yield return new object[] { new Uri("http://url.com"), "name=value; domain=" }; // Empty domain
            yield return new object[] { u6, "name11=value11; version=invalidversion" }; // Invalid version
            yield return new object[] { u6, "name11=value11; expires=invaliddate" }; // Invalid date
            yield return new object[] { u6, "name11=value11; max-age=invalidmaxage" }; // Invalid max age
            yield return new object[] { u6, "name11=value11; domain=invaliddomain" }; // Invalid domain
            yield return new object[] { u6, "name11=value11; port=invalidport" }; // Invalid port
        }

        [Theory]
        [MemberData(nameof(SetCookiesInvalidData))]
        public static void SetCookies_InvalidData_Throws(Uri uri, string cookieHeader)
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<CookieException>(() => cc.SetCookies(uri, cookieHeader));
        }

        [Fact]
        public static void SetCookies_InvalidInput_Throws()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Throws<ArgumentNullException>(() => cc.SetCookies(null, "")); // Null uri
            Assert.Throws<ArgumentNullException>(() => cc.SetCookies(u5, null)); // Null header
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void AddCookies_CapacityReached_OldCookiesRemoved(bool isFromSameDomain)
        {
            const int Capacity = 10;
            const int TotalCookieCount = 100;
            var cookieContainer = new CookieContainer(Capacity);
            Cookie cookie;

            for (int i = 0; i < TotalCookieCount; i++)
            {
                if (isFromSameDomain)
                {
                    cookie = new Cookie("name1", "value1", $"/{i}", "test.com");
                }
                else
                {
                    cookie = new Cookie("name1", "value1", "/", $"test{i}.com");
                }

                cookieContainer.Add(cookie);
            }

            Assert.Equal(Capacity, cookieContainer.Count);

            if (!isFromSameDomain)
            {
                FieldInfo domainTableField = typeof(CookieContainer).GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(domainTableField);
                Hashtable domainTable = domainTableField.GetValue(cookieContainer) as Hashtable;
                Assert.NotNull(domainTable);
                Assert.Equal(Capacity, domainTable.Count);
            }
        }
    }
}
