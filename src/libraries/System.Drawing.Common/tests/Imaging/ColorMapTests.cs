// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class ColorMapTests
    {
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Default()
        {
            ColorMap cm = new ColorMap();
            Assert.Equal(new Color(), cm.OldColor);
            Assert.Equal(new Color(), cm.NewColor);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void NewColor_SetValid_ReturnsExpected()
        {
            ColorMap cm = new ColorMap();
            cm.NewColor = Color.AliceBlue;
            Assert.Equal(Color.AliceBlue, cm.NewColor);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void OldColor_SetValid_ReturnsExpected()
        {
            ColorMap cm = new ColorMap();
            cm.OldColor = Color.AliceBlue;
            Assert.Equal(Color.AliceBlue, cm.OldColor);
        }
    }
}
