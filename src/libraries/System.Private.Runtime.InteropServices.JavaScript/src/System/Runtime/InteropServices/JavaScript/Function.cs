// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// The Function constructor creates a new Function object.
    /// </summary>
    /// <remarks>
    /// Calling the constructor directly can create functions dynamically, but suffers from security and similar
    /// (but far less significant) performance issues similar to eval. However, unlike eval, the Function constructor
    /// allows executing code in the global scope, prompting better programming habits and allowing for more efficient
    /// code minification.
    /// </remarks>
    public class Function : CoreObject
    {
        public Function(params object[] args) : base(Interop.Runtime.New<Function>(args))
        { }

        internal Function(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// The Apply() method calls a function with a given this value, and arguments provided as an array (or an array-like object).
        /// </summary>
        /// <returns>The apply.</returns>
        /// <param name="thisArg">This argument.</param>
        /// <param name="argsArray">Arguments.</param>
        public object Apply(object? thisArg = null, object[]? argsArray = null) => Invoke("apply", thisArg, argsArray);

        /// <summary>
        /// Creates a new Function that, when called, has its this keyword set to the provided value, with a given sequence of arguments preceding any provided when the new function is called.
        /// </summary>
        /// <returns>The bind.</returns>
        /// <param name="thisArg">This argument.</param>
        /// <param name="argsArray">Arguments.</param>
        public Function Bind(object? thisArg = null, object[]? argsArray = null) => (Function)Invoke("bind", thisArg, argsArray);

        /// <summary>
        /// Calls a function with a given `this` value and arguments provided individually.
        /// </summary>
        /// <returns>The result of calling the function with the specified `this` value and arguments.</returns>
        /// <param name="thisArg">Optional (null value). The value of this provided for the call to a function. Note that this may not be the actual value seen by the method: if the method is a function in non-strict mode, null and undefined will be replaced with the global object and primitive values will be converted to objects.</param>
        /// <param name="argsArray">Optional. Arguments for the function.</param>
        public object Call(object? thisArg = null, params object[] argsArray)
        {
            object?[] argsList = new object[argsArray.Length + 1];
            argsList[0] = thisArg;
            System.Array.Copy(argsArray, 0, argsList, 1, argsArray.Length);
            return Invoke("call", argsList);
        }
    }
}
