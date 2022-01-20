// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class ApiTest
    {
        [Fact]
        public static void InvokeJSFunctionByName()
        {
            JavaScriptMarshal.InvokeJSFunctionByName("console.log", "Hello JS!");
        }
    }
}
