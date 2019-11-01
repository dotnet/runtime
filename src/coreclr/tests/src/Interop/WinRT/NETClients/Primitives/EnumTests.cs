// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using TestLibrary;

namespace NetClient
{
    public class EnumTests
    {
        public static void RunTest()
        {
            Component.Contracts.IEnumTesting target = (Component.Contracts.IEnumTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.EnumTesting");
            Assert.AreEqual(Component.Contracts.TestEnum.A, target.GetA());
            Assert.IsTrue(target.IsB(Component.Contracts.TestEnum.B));
        }
    }
}
