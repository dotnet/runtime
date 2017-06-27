// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies a series of conversion methods that are useful when interoperating with the raw Win32 printing API.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class PrinterUnitConvert
    {
        private PrinterUnitConvert()
        {
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static double Convert(double value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            double fromUnitsPerDisplay = UnitsPerDisplay(fromUnit);
            double toUnitsPerDisplay = UnitsPerDisplay(toUnit);
            return value * toUnitsPerDisplay / fromUnitsPerDisplay;
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static int Convert(int value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            return (int)Math.Round(Convert((double)value, fromUnit, toUnit));
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static Point Convert(Point value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            return new Point(
                            Convert(value.X, fromUnit, toUnit),
                            Convert(value.Y, fromUnit, toUnit)
                            );
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static Size Convert(Size value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            return new Size(
                           Convert(value.Width, fromUnit, toUnit),
                           Convert(value.Height, fromUnit, toUnit)
                           );
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static Rectangle Convert(Rectangle value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            return new Rectangle(
                                Convert(value.X, fromUnit, toUnit),
                                Convert(value.Y, fromUnit, toUnit),
                                Convert(value.Width, fromUnit, toUnit),
                                Convert(value.Height, fromUnit, toUnit)
                                );
        }

        /// <summary>
        /// Converts the value, in fromUnit units, to toUnit units.
        /// </summary>
        public static Margins Convert(Margins value, PrinterUnit fromUnit, PrinterUnit toUnit)
        {
            Margins result = new Margins();

            result.DoubleLeft = Convert(value.DoubleLeft, fromUnit, toUnit);
            result.DoubleRight = Convert(value.DoubleRight, fromUnit, toUnit);
            result.DoubleTop = Convert(value.DoubleTop, fromUnit, toUnit);
            result.DoubleBottom = Convert(value.DoubleBottom, fromUnit, toUnit);

            return result;
        }

        private static double UnitsPerDisplay(PrinterUnit unit)
        {
            double result;
            switch (unit)
            {
                case PrinterUnit.Display:
                    result = 1.0;
                    break;
                case PrinterUnit.ThousandthsOfAnInch:
                    result = 10.0;
                    break;
                case PrinterUnit.HundredthsOfAMillimeter:
                    result = 25.4;
                    break;
                case PrinterUnit.TenthsOfAMillimeter:
                    result = 2.54;
                    break;
                default:
                    Debug.Fail("Unknown PrinterUnit " + unit);
                    result = 1.0;
                    break;
            }

            return result;
        }
    }
}

