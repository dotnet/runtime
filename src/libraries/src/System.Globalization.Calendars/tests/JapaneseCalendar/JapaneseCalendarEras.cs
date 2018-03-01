// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarEras
    {
        [Fact]
        public void Eras()
        {
            int[] eras = new JapaneseCalendar().Eras;
            int noOfEras = eras.Length;
            
            Assert.True(noOfEras >= 4);

            // eras should be [ noOfEras, noOfEras - 1, ..., 1 ]
            Assert.Equal(noOfEras, eras[0]);
            for (int i = 0; i < noOfEras; i++)
            {
                Assert.Equal(noOfEras - i, eras[i]);
            }
        }
    }
}
