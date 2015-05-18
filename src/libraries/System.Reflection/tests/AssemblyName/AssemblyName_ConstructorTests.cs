// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class AssemblyNameConstructorTests
    {
        //AssemblyName C'tor with Null
        [Fact]
        public void Case1_String()
        {
            Assert.Throws<ArgumentNullException>(() => { AssemblyName n = new AssemblyName(null); });
        }

        //AssemblyName C'tor with Empty String
        [Fact]
        public void Case2_String()
        {
            Assert.Throws<ArgumentException>(() => { AssemblyName n = new AssemblyName(""); });
        }

        //AssemblyName C'tor with '\0'
        [Fact]
        public void Case3_String()
        {
            Assert.Throws<ArgumentException>(() => { AssemblyName n = new AssemblyName("\0"); });
        }

        [Fact]
        public void Case4_String()
        {
            AssemblyName n = new AssemblyName("blabla");
            Assert.Equal(n.Name, "blabla");
        }

        [Fact]
        public void Case5_EmptyCtor()
        {
            AssemblyName n = new AssemblyName();
            Assert.NotNull(n);
        }
    }
}
