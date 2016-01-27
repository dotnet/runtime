// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class GregorianCalendarEras
    {
        private static readonly int[] s_ADEra = new int[]
        {
            1
        };

        #region Positive tests
        // PosTest1: Get the eras of Gregorian calendar
        [Fact]
        public void PosTest1()
        {
            PosTest(GregorianCalendarTypes.Arabic);
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(GregorianCalendarTypes.Localized);
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(GregorianCalendarTypes.MiddleEastFrench);
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(GregorianCalendarTypes.TransliteratedEnglish);
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(GregorianCalendarTypes.TransliteratedFrench);
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(GregorianCalendarTypes.USEnglish);
        }

        private void PosTest(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int[] expectedEras, actualEras;
            expectedEras = s_ADEra;
            actualEras = myCalendar.Eras;
            Assert.Equal(expectedEras.Length, actualEras.Length);
            Assert.Equal(expectedEras[0], actualEras[0]);
        }
        #endregion
    }
}
