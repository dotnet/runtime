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
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }

        [Fact]
        public static void MarshalFunctionLooptyLoop()
        {
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                for (x = 0; x < 1000; x++)
                {
                    res = funcDelegate (10, 20);
                }
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }

        [Fact]
        public static void MarshalFunctionLooptyLoopIncrement()
        {
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;
            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                for (x = 0; x < 1000; x++)
                {
                    res = funcDelegate (x, x);
                }
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(1998, HelperMarshal._functionResultValue);
            Assert.Equal(3996, HelperMarshal._i32Value);
        }

        [Fact]
        public static void MarshalFunctionReturnAction()
        {
            HelperMarshal._functionActionResultValue = 0;
            HelperMarshal._functionActionResultValueOfAction = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegateWithAction"", [  ]);
                var actionDelegate = funcDelegate (10, 20);
                actionDelegate(30,40);
            ");

            Assert.Equal(30, HelperMarshal._functionActionResultValue);
            Assert.Equal(70, HelperMarshal._functionActionResultValueOfAction);
        }

        [Fact]
        public static void MarshalActionIntInt()
        {
            HelperMarshal._actionResultValue = 0;

            Runtime.InvokeJS(@"
                var actionDelegate = App.call_test_method (""CreateActionDelegate"", [  ]);
                actionDelegate(30,40);
            ");

            Assert.Equal(70, HelperMarshal._actionResultValue);
        }

        [Fact]
        public static void MarshalActionFloatIntToIntInt()
        {
            HelperMarshal._actionResultValue = 0;
            Runtime.InvokeJS(@"
                var actionDelegate = App.call_test_method (""CreateActionDelegate"", [  ]);
                actionDelegate(3.14,40);
            ");

            Assert.Equal(43, HelperMarshal._actionResultValue);
        }

        [Fact]
        public static void MarshalDelegateMethod()
        {
            HelperMarshal._delMethodResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateMethod"", [  ]);
                del(""Hic sunt dracones"");
            ");

            Assert.Equal("Hic sunt dracones", HelperMarshal._delMethodResultValue);
        }

        [Fact]
        public static void MarshalAnonDelegateMethod()
        {
            HelperMarshal._delAnonMethodStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateAnonMethodReturnString"", [  ]);
                del(""Hic sunt dracones"");
            ");

            Assert.Equal("Notification received for: Hic sunt dracones", HelperMarshal._delAnonMethodStringResultValue);
        }


        [Fact]
        public static void MarshalLambdaDelegateMethod()
        {
            HelperMarshal._delLambdaMethodStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateLambdaMethodReturnString"", [  ]);
                del(""Hic sunt dracones"");
            ");

            Assert.Equal("Notification received for: Hic sunt dracones", HelperMarshal._delLambdaMethodStringResultValue);
        }

        [Fact]
        public static void MarshalDelegateMethodReturnString()
        {
            HelperMarshal._delMethodStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateMethodReturnString"", [  ]);
                var res = del(""Hic sunt dracones"");
                App.call_test_method (""SetTestString1"", [ res ]);
            ");

            Assert.Equal("Received: Hic sunt dracones", HelperMarshal._delMethodStringResultValue);
        }

        [Fact]
        public static void MarshalCustomMultiDelegateAcceptingString()
        {
            HelperMarshal._custMultiDelStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateCustomMultiDelegateAcceptingString"", [  ]);
                del(""Moin"");
            ");

            Assert.Equal("  Hello, Moin!  GoodMorning, Moin!", HelperMarshal._custMultiDelStringResultValue);
        }

        [Fact]
        public static void MarshalCustomMultiActionAcceptingString()
        {
            HelperMarshal._custMultiActionStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateCustomMultiActionAcceptingString"", [  ]);
                del(""MoinMoin"");
            ");

            Assert.Equal("  Hello, MoinMoin!  GoodMorning, MoinMoin!", HelperMarshal._custMultiActionStringResultValue);
        }

    }
}
