// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace System.Numerics.Colors.Tests
{
    public static class TestHelpers
    {
        public static IReadOnlyList<(byte, byte, byte, byte)> ByteColorsList =
            [
                ( 0, 0, 0, 0 ),

                ( 111, 222, 77, 188 ),
                ( 43, 222, 77, 188 ),
                ( 111, 154, 77, 188 ),
                ( 111, 222, 9, 188 ),
                ( 111, 222, 77, 120 ),

                ( 255, 255, 255, 255 ),
            ];

        public static TheoryData<byte, byte, byte, byte> ByteColors
        {
            get
            {
                var data = new TheoryData<byte, byte, byte, byte>();
                foreach (var color in ByteColorsList)
                    data.Add(color.Item1, color.Item2, color.Item3, color.Item3);
                return data;
            }
        }


        public static TheoryData<(byte, byte, byte, byte), (byte, byte, byte, byte)> ByteColorsTwoTimes
        {
            get
            {
                var ColorsTwoTimes = ByteColorsList.SelectMany(color => ByteColorsList, (color1, color2) => (color1, color2));
                var data = new TheoryData<(byte, byte, byte, byte), (byte, byte, byte, byte)>();
                foreach (var twoColors in ColorsTwoTimes)
                    data.Add(twoColors.color1, twoColors.color2);
                return data;
            }
        }

        public static float FloatC<T>(this T colorComponent) where T : IBinaryInteger<T>, IMinMaxValue<T>
            => Math.Clamp(float.CreateChecked(colorComponent) / float.CreateChecked(T.MaxValue), 0f, 1f);
    }
}
