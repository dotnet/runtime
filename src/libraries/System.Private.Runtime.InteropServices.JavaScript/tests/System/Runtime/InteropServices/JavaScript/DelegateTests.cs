// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class DelegateTests
    {
        [Fact]
        public static void MarshalFunction()
        {
            HelperMarshal._object1 = null;
            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }

        [Fact]
        public static void MarshalFunctionReturnAction()
        {
            HelperMarshal._object1 = null;
            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegateWithAction"", [  ]);
                var actionDelegate = funcDelegate (10, 20);
                actionDelegate(30,40);
            ");

            Assert.Equal(30, HelperMarshal._functionActionResultValue);
            Assert.Equal(70, HelperMarshal._functionActionResultValueOfAction);
        }

    }
}
