// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using TestLibrary;

namespace NetClient
{
    public class KeyValuePairTests
    {
        public static void RunTest()
        {
            Component.Contracts.IKeyValuePairTesting target = (Component.Contracts.IKeyValuePairTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.KeyValuePairTesting");
            TestSimplePair(target);
            TestMarshaledPair(target);
        }

        private static void TestSimplePair(Component.Contracts.IKeyValuePairTesting target)
        {
            int key = 5;
            int value = 27;

            Assert.AreEqual(new KeyValuePair<int, int>(key, value), target.MakeSimplePair(key, value));
        }

        private static void TestMarshaledPair(Component.Contracts.IKeyValuePairTesting target)
        {
            string key = "Key";
            string value = "Value";

            Assert.AreEqual(new KeyValuePair<string, string>(key, value), target.MakeMarshaledPair(key, value));
        }
    }
}
