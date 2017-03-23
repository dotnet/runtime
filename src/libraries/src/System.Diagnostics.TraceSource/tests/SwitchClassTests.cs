// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference PruneMakeRef()
        {
            return new WeakReference(new TestSwitch());
        }

        [Fact]
        public void PruneTest()
        {
            var strongSwitch = new TestSwitch();
            var weakSwitch = PruneMakeRef();
            GC.Collect(2);
            Trace.Refresh();
            Assert.False(weakSwitch.IsAlive);
            GC.Collect(2);
            Trace.Refresh();
        }
    }
}
