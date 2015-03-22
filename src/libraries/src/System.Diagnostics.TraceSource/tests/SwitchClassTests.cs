// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class SwitchClassTests
    {
        class TestSwitch : Switch
        {
            public TestSwitch(String description = null) : base(null, description) { }

            public String SwitchValue
            {
                get { return this.Value; }
                set { this.Value = value; }
            }
        }

        [Fact]
        public void ValueTest()
        {
            var item = new TestSwitch();
            item.SwitchValue = "1";
            item.SwitchValue = "0";
            item.SwitchValue = "-1";
        }

        [Fact]
        public void DescriptionTest()
        {
            var item = new TestSwitch("Desc");
            Assert.Equal("Desc", item.Description);
            item = new TestSwitch();
            Assert.Equal("", item.Description);
        }

        [Fact]
        public void PruneTest()
        {
            var strongSwitch = new TestSwitch();
            var weakSwitch = new WeakReference(new TestSwitch());
            Assert.True(weakSwitch.IsAlive);
            GC.Collect(2);
            TraceSwitch.RefreshAll();
            Assert.False(weakSwitch.IsAlive);
            GC.Collect(2);
            TraceSwitch.RefreshAll();
        }
    }
}
