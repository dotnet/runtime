// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using TestLibrary;

namespace NetClient
{
    public class NullableTests
    {
        public static void RunTest()
        {
            Component.Contracts.INullableTesting target = (Component.Contracts.INullableTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.NullableTesting");
            Assert.IsTrue(target.IsNull(null));
            Assert.IsFalse(target.IsNull(5));
            Assert.AreEqual(5, target.GetIntValue(5));
        }
    }
}
