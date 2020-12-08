// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    public static class TestDataProvider
    {
        private static readonly char[] s_charData;
        private static readonly char[] s_smallData;
        private static readonly char[] s_largeData;

        public static object FirstObject { get; } = (object)1;
        public static object SecondObject { get; } = (object)"[second object]";
        public static object ThirdObject { get; } = (object)"<third object>";
        public static object[] MultipleObjects { get; } = new object[] { FirstObject, SecondObject, ThirdObject };

        public static string FormatStringOneObject { get; } = "Object is {0}";
        public static string FormatStringTwoObjects { get; } = $"Object are '{0}', {SecondObject}";
        public static string FormatStringThreeObjects { get; } = $"Objects are {0}, {SecondObject}, {ThirdObject}";
        public static string FormatStringMultipleObjects { get; } = "Multiple Objects are: {0}, {1}, {2}";

        static TestDataProvider()
        {
            s_charData = new char[]
            {
                char.MinValue,
                char.MaxValue,
                '\t',
                ' ',
                '$',
                '@',
                '#',
                '\0',
                '\v',
                '\'',
                '\u3190',
                '\uC3A0',
                'A',
                '5',
                '\r',
                '\uFE70',
                '-',
                ';',
                '\r',
                '\n',
                'T',
                '3',
                '\n',
                'K',
                '\u00E6'
            };

            s_smallData = "HELLO".ToCharArray();

            var data = new List<char>();
            for (int count = 0; count < 1000; ++count)
            {
                data.AddRange(s_smallData);
            }
            s_largeData = data.ToArray();
        }

        public static char[] CharData => s_charData;

        public static char[] SmallData => s_smallData;

        public static char[] LargeData => s_largeData;
    }
}
