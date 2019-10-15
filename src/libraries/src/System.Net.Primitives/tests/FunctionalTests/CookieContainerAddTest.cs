// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public partial class CookieContainerTest
    {
        [Fact]
        public static void AddCookieCollection_Null_Throws()
        {
            CookieContainer cc = new CookieContainer();
            CookieCollection cookieCollection = null;
            Assert.Throws<ArgumentNullException>(() => cc.Add(cookieCollection));
        }

        [Fact]
        public static void AddCookieCollection_Success()
        {
            CookieContainer cc = new CookieContainer();
            CookieCollection cookieCollection = new CookieCollection();
            cookieCollection.Add(new Cookie("name3", "value","/",".contoso.com"));
            cc.Add(cookieCollection);
            Assert.Equal(1, cc.Count);
        }
    }
}
