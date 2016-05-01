// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

using Xunit;

namespace System.Net.Primitives.Unit.Tests
{
    public class CookieCollectionTest
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
        public void IndexSubscript_Get_Success()
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
        public void IndexSubscript_Get_Invalid()
        {
            CookieCollection cc = CreateCookieCollection1();

            Assert.Throws<ArgumentOutOfRangeException>(() => cc[-1]); // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>(() => cc[cc.Count]); // Index >= Count

            Assert.Null(cc["no such name"]);
        }        
    }
}
