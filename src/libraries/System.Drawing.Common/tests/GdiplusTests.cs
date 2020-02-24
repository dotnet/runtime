// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using Xunit;

namespace System.Drawing.Tests
{
    public class GdiplusTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsOSX))]
        public void IsAtLeastLibgdiplus6()
        {
            Assert.True(Helpers.GetIsWindowsOrAtLeastLibgdiplus6());
        }
    }
}
