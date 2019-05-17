// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using TestLibrary;

namespace NetClient
{
    public class StringTests
    {
        public static void RunTest()
        {
            Component.Contracts.IStringTesting target = (Component.Contracts.IStringTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.StringTesting");
            string left = "Hello C++/WinRT";
            string right = " from .NET Core";
            Assert.AreEqual(string.Concat(left, right), target.ConcatStrings(left, right));
        }
    }
}
