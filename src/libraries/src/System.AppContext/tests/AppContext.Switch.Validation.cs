// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public partial class AppContextTests
    {
        [Fact]
        public void NullSwitchName_EnableSwitch()
        {
            AssertExtensions.Throws<ArgumentNullException>("switchName", () => AppContext.SetSwitch(null, true));
        }

        [Fact]
        public void NullSwitchName_DisableSwitch()
        {
            AssertExtensions.Throws<ArgumentNullException>("switchName", () => AppContext.SetSwitch(null, false));
        }

        [Fact]
        public void NullSwitchName_TryGetSwitchValue()
        {
            AssertExtensions.Throws<ArgumentNullException>("switchName", () =>
            {
                bool output;
                AppContext.TryGetSwitch(null, out output);
            });
        }

        [Fact]
        public void EmptySwitchName_EnableSwitch()
        {
            AssertExtensions.Throws<ArgumentException>("switchName", () => AppContext.SetSwitch(string.Empty, true));
        }

        [Fact]
        public void EmptySwitchName_DisableSwitch()
        {
            AssertExtensions.Throws<ArgumentException>("switchName", () => AppContext.SetSwitch(string.Empty, false));
        }

        [Fact]
        public void EmptySwitchName_TryGetSwitchValue()
        {
            bool output;
            AssertExtensions.Throws<ArgumentException>("switchName", () => AppContext.TryGetSwitch(string.Empty, out output));
        }
    }
}
