// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public partial class CookieContainerTest
    {
        [Fact]
        public static void Ctor_Empty_Success()
        {
            CookieContainer cc = new CookieContainer();
        }

        [Fact]
        public static void Ctor_Capacity_Success()
        {
            CookieContainer cc = new CookieContainer(5);
            Assert.Equal(5, cc.Capacity);
        }

        [Fact]
        public static void Ctor_Capacity_Invalid()
        {
            Assert.Throws<ArgumentException>(() => new CookieContainer(0)); //Capacity <= 0
        }

        [Fact]
        public static void Ctor_CapacityPerDomainCapacityMaxCookieSize_Success()
        {
            CookieContainer cc = new CookieContainer(5, 4, 3);
            Assert.Equal(5, cc.Capacity);
            Assert.Equal(4, cc.PerDomainCapacity);
            Assert.Equal(3, cc.MaxCookieSize);

            cc = new CookieContainer(10, int.MaxValue, 4); //Even though PerDomainCapacity > Capacity, this shouldn't throw
        }

        [Fact]
        public static void Ctor_CapacityPerDomainCapacityMaxCookieSize_Invalid()
        {
            Assert.Throws<ArgumentException>(() => new CookieContainer(0, 10, 5)); //Capacity <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() => new CookieContainer(5, 0, 5)); //Per domain capacity <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() => new CookieContainer(5, 10, 5)); //Per domain capacity > Capacity

            Assert.Throws<ArgumentException>(() => new CookieContainer(15, 10, 0)); //Max cookie size <= 0
        }

        [Fact]
        public static void Capacity_GetSet_Success()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(CookieContainer.DefaultCookieLimit, cc.Capacity);

            cc.Capacity = 900;
            Assert.Equal(900, cc.Capacity);

            cc.Capacity = 40; //Shrink
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
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.MaxCookieSize = 0); // <= 0
        }

        [Fact]
        public static void PerDomainCapacity_GetSet()
        {
            CookieContainer cc = new CookieContainer();
            Assert.Equal(CookieContainer.DefaultPerDomainCookieLimit, cc.PerDomainCapacity);

            cc.PerDomainCapacity = 50;
            Assert.Equal(50, cc.PerDomainCapacity);

            cc.PerDomainCapacity = 40; //Shrink
            Assert.Equal(40, cc.PerDomainCapacity);
        }

        [Fact]
        public static void PerDomainCapacity_Set_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentOutOfRangeException>(() => cc.PerDomainCapacity = 0); // <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.PerDomainCapacity = cc.Capacity + 1); // >= Capacity
        }

        [Fact]
        public static void Add_Cookie_Success()
        {
            Cookie c1 = new Cookie("name1", "value", "", "contoso.com");
            Cookie c2 = new Cookie("name2", "", "", "contoso.com") { Secure = true };
            Cookie c3 = new Cookie("name3", "value", "", "contoso.com") { Port = "\"80, 90, 100\"" };
            Cookie c4 = new Cookie("name4", "value", "", ".contoso.com");
            Cookie c5 = new Cookie("name5", "value", "", "127.0.0.1");

            CookieContainer cc1 = new CookieContainer();
            Assert.Equal(0, cc1.Count);

            cc1.Add(c1);
            cc1.Add(c2);
            cc1.Add(c3);
            cc1.Add(c4);
            cc1.Add(c5);

            Assert.Equal(5, cc1.Count);
        }

        [Fact]
        public static void Add_Cookie_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentNullException>(() => cc.Add((Cookie)null)); //Null cookie

            cc.MaxCookieSize = 1;
            Assert.Throws<CookieException>(() => cc.Add(new Cookie("name", "long-text", "", "contoso.com"))); //Value.Length > MaxCookieSize
        }
        
        [Fact]
        public static void Add_VerificationFailedCookie_Throws()
        {
            CookieContainer cc = new CookieContainer();
            
            foreach (Cookie c in CookieTest.InvalidCookies())
            {
                Assert.Throws<CookieException>(() => cc.Add(c));
            }
        }

        [Fact]
        public static void Add_CookieCollection_Success()
        {
            CookieContainer cc1 = new CookieContainer();
            CookieCollection cc2 = new CookieCollection();

            cc2.Add(new Cookie("name1", "value", "", "contoso.com"));
            cc2.Add(new Cookie("name2", "value", "", "contoso.com"));

            cc1.Add(cc2);
            Assert.Equal(2, cc1.Count);
        }

        [Fact]
        public static void Add_CookieCollection_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentNullException>(() => cc.Add((CookieCollection)null)); //Null cookie
        }        
        
        [Fact]
        public static void Add_CookieUri_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentNullException>(() => cc.Add(null, new Cookie("name", "value"))); //Null uri
            Assert.Throws<ArgumentNullException>(() => cc.Add(new Uri("http://contoso.com"), (Cookie)null)); //Null cookie
        }

        [Fact]
        public static void AddCookieCollectionUri_Success()
        {
            Uri uri = new Uri("http://contoso.com");
            String domain = "contoso.com";

            CookieContainer cc1 = new CookieContainer();
            CookieCollection cc2 = new CookieCollection();
            cc2.Add(new Cookie("name1", "value") { Domain = domain });
            cc2.Add(new Cookie("name2", "value") { Domain = domain });

            cc1.Add(uri, cc2);
            Assert.Equal(2, cc1.Count);
        }

        [Fact]
        public static void AddCookieCollectionUri_Invalid()
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

        [Fact]
        public static void SetCookies_Invalid()
        {
            CookieContainer cc = new CookieContainer();

            Assert.Throws<ArgumentNullException>(() => cc.SetCookies(null, "")); //Null uri
            Assert.Throws<ArgumentNullException>(() => cc.SetCookies(new Uri("http://contoso.com"), null)); //Null header
        }
    }
}
