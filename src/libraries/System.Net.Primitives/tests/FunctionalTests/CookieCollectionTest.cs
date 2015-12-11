// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class CookieCollectionTest
    {
        //These cookies are designed to have some similar and different properties so that each is unique in the eyes of a CookieComparer object
        private static Cookie c1 = new Cookie("name1", "value");
        private static Cookie c2 = new Cookie("name2", "value", "path"); //Same name, has a path
        private static Cookie c3 = new Cookie("name2", "value", "different-path"); //Same name, different path
        private static Cookie c4 = new Cookie("name3", "value", "path", "domain"); //Different name, has a domain
        private static Cookie c5 = new Cookie("name3", "value", "path", "different-domain"); //Same name, different domain

        private static CookieCollection CreateCookieCollection1()
        {
            CookieCollection cc = new CookieCollection();

            cc.Add(c1);
            cc.Add(c2);
            cc.Add(c3);
            cc.Add(c4);
            cc.Add(c5);

            return cc;
        } 

        private static CookieCollection CreateCookieCollection2()
        {
            CookieCollection cc = new CookieCollection();

            cc.Add(CreateCookieCollection1());

            return cc;
        }

        [Fact]
        public static void Add_Cookie_Success()
        {
            CookieCollection cc = CreateCookieCollection1();
            Assert.Equal(5, cc.Count);
        }

        [Fact]
        public static void Add_ExistingCookie_NameUpdatesCookie()
        {
            CookieCollection cc = CreateCookieCollection1();
            
            c4.Name = "new-name";
            cc.Add(c4);

            Assert.Equal(c4, cc[c4.Name]);

            c4.Name = "name3"; //Reset
        }

        [Fact]
        public static void Add_ExistingCookie_PathUpdatesCookie()
        {
            CookieCollection cc = CreateCookieCollection1();

            c4.Path = "new-path";
            cc.Add(c4);

            Assert.Equal(c4, cc[c4.Name]);

            c4.Path = "path"; //Reset
        }

        [Fact]
        public static void Add_ExistingCookie_DomainUpdatesCookie()
        {
            CookieCollection cc = CreateCookieCollection1();
            
            c4.Domain = "new-domain";
            cc.Add(c4);

            Assert.Equal(c4, cc[c4.Name]);

            c4.Domain = "domain"; //Reset
        }

        [Fact]
        public static void Add_Cookie_Invalid()
        {
            CookieCollection cc = new CookieCollection();

            Assert.Throws<ArgumentNullException>(() => cc.Add((Cookie)null));
        }

        [Fact]
        public static void Add_CookieCollection_Success()
        {
            CookieCollection cc = CreateCookieCollection2();
            Assert.Equal(5, cc.Count);
        }

        [Fact]
        public static void Add_CookieCollection_Invalid()
        {
            CookieCollection cc = new CookieCollection();

            Assert.Throws<ArgumentNullException>(() => cc.Add((CookieCollection)null));
        }

        [Fact]
        public static void IndexSubscript_Get_Success()
        {
            CookieCollection cc = CreateCookieCollection1();

            Assert.Equal(cc[0], c1);
            Assert.Equal(cc[1], c2);
            Assert.Equal(cc[2], c3);
            Assert.Equal(cc[3], c4);
            Assert.Equal(cc[4], c5);

            Assert.Equal(cc["name1"], c1);
            Assert.Equal(cc["name2"], c2);
            Assert.Equal(cc["name3"], c4);
        }

        [Fact]
        public static void IndexSubscript_Get_Invalid()
        {
            CookieCollection cc = CreateCookieCollection1();

            Assert.Throws<ArgumentOutOfRangeException>(() => cc[-1]); //Index < 0
            Assert.Throws<ArgumentOutOfRangeException>(() => cc[cc.Count]); //Index >= Count

            Assert.Null(cc["invalidname"]);
        }

        [Fact]
        public static void IsSynchronized_Get_Success()
        {
            ICollection cc = new CookieCollection();
            Assert.False(cc.IsSynchronized);
        }

        [Fact]
        public static void SyncRoot_Get_Success()
        {
            ICollection cc = new CookieCollection();
            Assert.Equal(cc, cc.SyncRoot);
        }
    }
}
