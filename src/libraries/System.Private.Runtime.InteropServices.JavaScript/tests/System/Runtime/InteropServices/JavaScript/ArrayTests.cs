// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class ArrayTests
    {
        [Fact]
        public static void ArrayLength()
        {
            Array d = new Array();
            Assert.Equal(0, d.Length);
        }

        [Fact]
        public static void ArrayLength1()
        {
            Array d = new Array(50);
            Assert.Equal(50, d.Length);
        }

        [Fact]
        public static void Array_GetSetItem()
        {
            var jsArray = new Array(7, 8, 9, 10, 11, 12, 13);
            IList<int> iList = new int[] { 7, 8, 9, 10, 11, 12, 13 };

            Assert.Equal(jsArray.Length, iList.Count);
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(jsArray[i], iList[i]);

                iList[i] = 99;
                jsArray[i] = 99;
                Assert.Equal(99, iList[i]);
                Assert.Equal(99, jsArray[i]);
            }
        }

        [Fact]
        public static void Array_GetSetItemInvalid()
        {
            var jsArray = new Array(7, 8, 9, 10, 11, 12, 13);
            Assert.Null(jsArray[-1]);
            Assert.Null(jsArray[jsArray.Length]);
            Assert.Equal(0, jsArray[-1] = 0);
            Assert.Equal(0, jsArray[jsArray.Length] = 0);
        }

        [Fact]
        public static void Array_GetItemIndex()
        {
            var jsArray = new Array(7, 8, 9, 10, 11, 12, 13);
            Assert.Equal(7, jsArray[0]);
            Assert.Equal(8, jsArray[1]);
            Assert.Equal(9, jsArray[2]);
            Assert.Equal(10, jsArray[3]);
            Assert.Equal(11, jsArray[4]);
            Assert.Equal(12, jsArray[5]);
            Assert.Equal(13, jsArray[6]);
        }

        [Fact]
        public static void Array_GetSetItemIndex()
        {
            var jsArray = new Array(7, 8, 9, 10, 11, 12, 13);
            for (int d = 0; d < jsArray.Length; d++)
            {
                jsArray[d] = (int)jsArray[d] + 1;
            }
            Assert.Equal(8, jsArray[0]);
            Assert.Equal(9, jsArray[1]);
            Assert.Equal(10, jsArray[2]);
            Assert.Equal(11, jsArray[3]);
            Assert.Equal(12, jsArray[4]);
            Assert.Equal(13, jsArray[5]);
            Assert.Equal(14, jsArray[6]);
        }

        [Fact]
        public static void Array_Pop()
        {
            var jsArray = new Array(7, 8, 9, 10, 11, 12, 13);
            Assert.Equal(13, jsArray.Pop());
            Assert.Equal(12, jsArray.Pop());
            Assert.Equal(11, jsArray.Pop());
            Assert.Equal(10, jsArray.Pop());
            Assert.Equal(9, jsArray.Pop());
            Assert.Equal(8, jsArray.Pop());
            Assert.Equal(7, jsArray.Pop());
            Assert.Equal(0, jsArray.Length);
        }

        [Fact]
        public static void Array_PushPop()
        {
            var objArray = new object[] { "test7", "test8", "test9", "test10", "test11", "test12", "test13" };
            var jsArray = new Array();
            for (int d = 0; d < objArray.Length; d++)
            {
                jsArray.Push(objArray[d]);
            }
            Assert.Equal("test13", jsArray.Pop());
            Assert.Equal("test12", jsArray.Pop());
            Assert.Equal("test11", jsArray.Pop());
            Assert.Equal("test10", jsArray.Pop());
            Assert.Equal("test9", jsArray.Pop());
            Assert.Equal("test8", jsArray.Pop());
            Assert.Equal("test7", jsArray.Pop());
            Assert.Equal(0, jsArray.Length);
        }

        [Fact]
        public static void Array_PushShift()
        {
            var objArray = new object[] { "test7", "test8", "test9", "test10", "test11", "test12", "test13" };
            var jsArray = new Array();
            for (int d = 0; d < objArray.Length; d++)
            {
                jsArray.Push(objArray[d]);
            }
            Assert.Equal("test7", jsArray.Shift());
            Assert.Equal("test8", jsArray.Shift());
            Assert.Equal("test9", jsArray.Shift());
            Assert.Equal("test10", jsArray.Shift());
            Assert.Equal("test11", jsArray.Shift());
            Assert.Equal("test12", jsArray.Shift());
            Assert.Equal("test13", jsArray.Shift());
            Assert.Equal(0, jsArray.Length);
        }

        [Fact]
        public static void Array_UnShiftShift()
        {
            var objArray = new object[] { "test7", "test8", "test9", "test10", "test11", "test12", "test13" };
            var jsArray = new Array();
            for (int d = 0; d < objArray.Length; d++)
            {
                Assert.Equal(d + 1, jsArray.UnShift(objArray[d]));
            }
            Assert.Equal("test13", jsArray.Shift());
            Assert.Equal("test12", jsArray.Shift());
            Assert.Equal("test11", jsArray.Shift());
            Assert.Equal("test10", jsArray.Shift());
            Assert.Equal("test9", jsArray.Shift());
            Assert.Equal("test8", jsArray.Shift());
            Assert.Equal("test7", jsArray.Shift());
            Assert.Equal(0, jsArray.Length);
        }

        [Fact]
        public static void Array_IndexOf()
        {
            var beasts = new Array("ant", "bison", "camel", "duck", "bison");
            Assert.Equal(1, beasts.IndexOf("bison"));
            Assert.Equal(4, beasts.IndexOf("bison", 2));
            Assert.Equal(-1, beasts.IndexOf("giraffe"));
        }

        [Fact]
        public static void Array_LastIndexOf()
        {
            var beasts = new Array("Dodo", "Tiger", "Penguin", "Dodo");
            Assert.Equal(3, beasts.LastIndexOf("Dodo"));
            Assert.Equal(1, beasts.LastIndexOf("Tiger"));
            Assert.Equal(0, beasts.LastIndexOf("Dodo", 2));  // The array is searched backwards
            Assert.Equal(-1, beasts.LastIndexOf("giraffe"));
        }
    }
}
