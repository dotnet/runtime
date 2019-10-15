// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Linq;
using TestLibrary;

namespace NetClient
{
    public class ArrayTests
    {
        public static void RunTest()
        {
            Component.Contracts.IArrayTesting target = (Component.Contracts.IArrayTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.ArrayTesting");
            TestIntArray(target);
            TestBoolArray(target);
        }

        private static void TestIntArray(Component.Contracts.IArrayTesting target)
        {
            int[] array = Enumerable.Range(1, 30).ToArray();
            Assert.AreEqual(array.Sum(), target.Sum(array));

        }

        private static void TestBoolArray(Component.Contracts.IArrayTesting target)
        {
            bool[] array = new []{ true, false, true, true, true, false, false};
            bool expected = array.Aggregate(false, (left, right) => left ^ right);

            Assert.AreEqual(expected, target.Xor(array));
        }
    }
}
