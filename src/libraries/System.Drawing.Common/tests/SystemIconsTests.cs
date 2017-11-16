// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Tests
{
    public class SystemIconsTests
    {
        public static IEnumerable<object[]> SystemIcons_TestData()
        {
            yield return Icon(() => SystemIcons.Application);
            yield return Icon(() => SystemIcons.Asterisk);
            yield return Icon(() => SystemIcons.Error);
            yield return Icon(() => SystemIcons.Exclamation);
            yield return Icon(() => SystemIcons.Hand);
            yield return Icon(() => SystemIcons.Information);
            yield return Icon(() => SystemIcons.Question);
            yield return Icon(() => SystemIcons.Shield);
            yield return Icon(() => SystemIcons.Warning);
            yield return Icon(() => SystemIcons.WinLogo);
        }

        public static object[] Icon(Func<Icon> getIcon) => new object[] { getIcon };

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(SystemIcons_TestData))]
        public void SystemIcons_Get_ReturnsExpected(Func<Icon> getIcon)
        {
            Icon icon = getIcon();
            Assert.Same(icon, getIcon());
        }
    }
}
