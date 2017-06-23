// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class TraceSwitchClassTests
    {
        [Fact]
        public void ConstructorTest()
        {
            var item = new TraceSwitch("SwitchName", "Description");
            item = new TraceSwitch("SwitchName", null);

            item = new TraceSwitch(null, null);
            Assert.Equal("", item.DisplayName);
        }

        [Fact]
        public void ConstructorTest2()
        {
            var item = new TraceSwitch("SwitchName", "Description", "Error");
            Assert.Equal(TraceLevel.Error, item.Level);
            item = new TraceSwitch("SwitchName", null, "warning");
            Assert.Equal(TraceLevel.Warning, item.Level);
            item = new TraceSwitch("Name", null, "NO_EXIST");
            AssertExtensions.Throws<ArgumentException>(null, () => item.Level);
            item = new TraceSwitch("Name", null, null);

            Assert.Throws<ArgumentNullException>(() => item.Level);
        }


        [Fact]
        public void LevelTest()
        {
            var item = new TraceSwitch("SwitchName", "Description", "Error");
            Assert.Equal(TraceLevel.Error, item.Level);
            item.Level = TraceLevel.Info;
            Assert.Equal(TraceLevel.Info, item.Level);
            AssertExtensions.Throws<ArgumentException>(null, () => item.Level = (TraceLevel)(TraceLevel.Off - 1));
            AssertExtensions.Throws<ArgumentException>(null, () => item.Level = (TraceLevel)(TraceLevel.Verbose + 1));
        }

        [Theory]
        [InlineData(TraceLevel.Off, false, false, false, false)]
        public void TraceLevelTest(TraceLevel level, bool error, bool warning, bool info, bool verbose)
        {
            var item = new TraceSwitch("SwitchName", "Description");
            item.Level = level;
            Assert.Equal(error, item.TraceError);
            Assert.Equal(warning, item.TraceWarning);
            Assert.Equal(info, item.TraceInfo);
            Assert.Equal(verbose, item.TraceVerbose);
        }

        class TestTraceSwitch : TraceSwitch
        {
            public TestTraceSwitch() : base(null, null) { }

            public void SetSwitchSetting(int value)
            {
                this.SwitchSetting = value;
            }
        }

        [Fact]
        public void SwitchSettingChangedTest()
        {
            var item = new TestTraceSwitch();
            item.SetSwitchSetting((int)TraceLevel.Off - 1);
            Assert.Equal(TraceLevel.Off, item.Level);

            item.SetSwitchSetting((int)TraceLevel.Verbose + 1);
            Assert.Equal(TraceLevel.Verbose, item.Level);
        }
    }
}
