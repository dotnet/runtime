// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace BadMax1
{
    public struct Size
    {
        public double _width;
        public double _height;

        public double Width
        {
            [MethodImpl(MethodImplOptions.NoInlining)] get { return this._width; }
            [MethodImpl(MethodImplOptions.NoInlining)] set { this._width = value; }
        }

        public double Height
        {
            [MethodImpl(MethodImplOptions.NoInlining)] get { return this._height; }
            [MethodImpl(MethodImplOptions.NoInlining)] set { this._height = value; }
        }
    }



    public class RowInfo
    {
        public Size _rowSize;
        public double _verticalOffset;
        public int _firstPage;
        public int _pageCount;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddPage(Size pageSize)
        {
            this._pageCount++;

            this._rowSize.Width += pageSize.Width;
            this._rowSize.Height = Math.Max(pageSize.Height, _rowSize.Height);

            return;
        }
    }


    public static class FpUtils
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AreClose(double d1, double d2)
        {
            double delta;

            delta = (d1 - d2);

            if ((delta >= -0.01) && (delta <= 0.01))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    internal static class App
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int RunRepro()
        {
            double computedHeight;
            double expectedHeight;
            RowInfo rowInfo;
            Size pageSize;


            rowInfo = new RowInfo();
            pageSize._width = 826.0;
            pageSize._height = 1066.0;


            rowInfo.AddPage(pageSize);


            expectedHeight = 1066.0;
            computedHeight = rowInfo._rowSize._height;

            if (FpUtils.AreClose(expectedHeight, computedHeight))
            {
                Console.WriteLine("Test passed.");
                return 100;
            }
            else
            {
                Console.WriteLine(
                    "Test failed.\r\n" +
                    "    Expected: ({0})\r\n" +
                    "    Computed: ({1})",

                    expectedHeight,
                    computedHeight
                );
            }
            return 101;
        }


        private static int Main()
        {
            return App.RunRepro();
        }
    }
}
