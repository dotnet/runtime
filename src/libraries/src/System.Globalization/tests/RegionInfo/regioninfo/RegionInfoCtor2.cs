// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoCtor2
    {
        [Fact]
        public void PosTest1()
        {
            string name = "en-US";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.True(myRegInfo.Name == name || myRegInfo.Name == "US");
        }

        [Fact]
        public void PosTest2()
        {
            string name = "US";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.Equal(name, myRegInfo.Name);
        }

        [Fact]
        public void PosTest3()
        {
            string name = "zh-CN";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.True(myRegInfo.Name == name || myRegInfo.Name == "CN");
        }

        [Fact]
        public void PosTest4()
        {
            string name = "CN";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.Equal(name, myRegInfo.Name);
        }

        [Fact]
        public void PosTest5()
        {
            string name = "en-IE";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.True(myRegInfo.Name == name || myRegInfo.Name == "IE");
        }

        [Fact]
        public void PosTest6()
        {
            string name = "IE";
            RegionInfo myRegInfo = new RegionInfo(name);
            Assert.Equal(name, myRegInfo.Name);
        }

        // NegTest1:the name is not valid region
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void TestInvalidRegion()
        {
            string name = "HELLOWORLD";
            Assert.Throws<ArgumentException>(() =>
            {
                RegionInfo myRegInfo = new RegionInfo(name);
            });
        }

        // NegTest2:the name is null reference
        [Fact]
        public void TestNull()
        {
            string name = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                RegionInfo myRegInfo = new RegionInfo(name);
            });
        }
    }
}