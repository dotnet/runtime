// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public partial class AppContextTests
    {
        [Fact]
        public void SwitchNotFound()
        {
            string switchName = GetSwitchName();
            bool isEnabled;
            Assert.False(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.False(isEnabled);
        }

        [Fact]
        public void EnableSwitch()
        {
            string switchName = GetSwitchName();
            AppContext.SetSwitch(switchName, true);

            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.True(isEnabled);
        }

        [Fact]
        public void EnableMultipleSwitches()
        {
            string switchName1 = GetSwitchName();
            string switchName2 = GetSwitchName();

            AppContext.SetSwitch(switchName1, true);
            AppContext.SetSwitch(switchName2, true);

            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName1, out isEnabled));
            Assert.True(isEnabled);

            isEnabled = false;
            Assert.True(AppContext.TryGetSwitch(switchName2, out isEnabled));
            Assert.True(isEnabled);
        }

        [Fact]
        public void EnableSwitchMultipleTimes()
        {
            string switchName = GetSwitchName();

            AppContext.SetSwitch(switchName, true);
            AppContext.SetSwitch(switchName, true);

            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.True(isEnabled);
        }

        [Fact]
        public void DisableSwitch()
        {
            string switchName = GetSwitchName();

            AppContext.SetSwitch(switchName, true);
            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.True(isEnabled);

            // Ensure we can keep the value around
            AppContext.SetSwitch(switchName, false);
            Assert.True(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.False(isEnabled);
        }

        [Fact]
        public void DisableMultipleSwitches()
        {
            string switchName1 = GetSwitchName();
            string switchName2 = GetSwitchName();

            AppContext.SetSwitch(switchName1, false);
            AppContext.SetSwitch(switchName2, false);

            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName1, out isEnabled));
            Assert.False(isEnabled);
            Assert.True(AppContext.TryGetSwitch(switchName2, out isEnabled));
            Assert.False(isEnabled);
        }

        [Fact]
        public void DisableSwitchMultipleTimes()
        {
            string switchName = GetSwitchName();

            AppContext.SetSwitch(switchName, false);
            AppContext.SetSwitch(switchName, false);

            bool isEnabled;
            Assert.True(AppContext.TryGetSwitch(switchName, out isEnabled));
            Assert.False(isEnabled);
        }

        [Fact]
        public void TryGetSwitch_SwitchNotDefined()
        {
            string switchName = GetSwitchName();

            bool isEnabled;
            var exists = AppContext.TryGetSwitch(switchName, out isEnabled);
            Assert.False(exists);
        }

        [Fact]
        public void TryGetSwitch_SwitchDefined()
        {
            bool isEnabled, exists;
            string randomSwitchName = GetSwitchName();

            // Enable switch
            AppContext.SetSwitch(randomSwitchName, true);

            // Get value
            exists = AppContext.TryGetSwitch(randomSwitchName, out isEnabled);
            Assert.True(exists);
            Assert.True(isEnabled);

            Assert.True(AppContext.TryGetSwitch(randomSwitchName, out isEnabled));
            Assert.True(isEnabled);

            // Disable switch
            AppContext.SetSwitch(randomSwitchName, false);

            // Get value
            exists = AppContext.TryGetSwitch(randomSwitchName, out isEnabled);
            Assert.True(exists);
            Assert.False(isEnabled);
            Assert.True(AppContext.TryGetSwitch(randomSwitchName, out isEnabled));
            Assert.False(isEnabled);
        }

        private static string GetSwitchName([Runtime.CompilerServices.CallerLineNumber] int sourceLine = -1)
        {
            Assert.True(sourceLine != -1, "The 'sourceLine' should have retrieved from its caller");
            return "Switch.Line" + sourceLine;
        }
    }
}
