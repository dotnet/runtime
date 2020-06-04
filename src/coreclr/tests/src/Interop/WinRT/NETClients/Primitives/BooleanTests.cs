// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using TestLibrary;

namespace NetClient
{
    public class BooleanTests
    {
        public static void RunTest()
        {
            Component.Contracts.IBooleanTesting target = (Component.Contracts.IBooleanTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.BooleanTesting");
            Assert.IsTrue(target.And(true, true));
            Assert.IsFalse(target.And(false, true));
        }
    }
}
