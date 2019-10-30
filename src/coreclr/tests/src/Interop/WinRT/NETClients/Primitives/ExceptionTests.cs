// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using TestLibrary;

namespace NetClient
{
    public class ExceptionTests
    {
        public static void RunTest()
        {
            Component.Contracts.IExceptionTesting target = (Component.Contracts.IExceptionTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.ExceptionTesting");

            Assert.Throws<InvalidOperationException>(() => target.ThrowException(new InvalidOperationException()));

            Assert.OfType<ArgumentOutOfRangeException>(target.GetException(new ArgumentOutOfRangeException().HResult));
            Assert.OfType<NullReferenceException>(target.GetException(new ArgumentNullException().HResult));
            Assert.OfType<NullReferenceException>(target.GetException(new NullReferenceException().HResult));
        }
    }
}
