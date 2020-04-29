// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public class Function : CoreObject
        {
            public Function(params object[] args) : base(Runtime.New<Function>(args))
            { }

            internal Function(IntPtr js_handle) : base(js_handle)
            { }


            public object Apply(object? thisArg = null, object[]? argsArray = null) => Invoke("apply", thisArg, argsArray);

            public Function Bind(object? thisArg = null, object[]? argsArray = null) => (Function)Invoke("bind", thisArg, argsArray);

            public object Call(object? thisArg = null, params object[] argsArray)
            {
                object?[] argsList = new object[argsArray.Length + 1];
                argsList[0] = thisArg;
                System.Array.Copy(argsArray, 0, argsList, 1, argsArray.Length);
                return Invoke("call", argsList);
            }


        }
    }
}
