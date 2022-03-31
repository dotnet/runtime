// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    /// <remarks>
    /// It will make the exported function not trimmable by AOT
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JSExportAttribute : Attribute
    {
        public string? FunctionName { get; }
        public Type[]? CustomMarshallers { get; }

        public JSExportAttribute()
        {
            FunctionName = null;
            CustomMarshallers = null;
        }

        public JSExportAttribute(string functionName)
        {
            FunctionName = functionName;
            CustomMarshallers = null;
        }

        public JSExportAttribute(string functionName, params Type[] customMarshallers)
        {
            FunctionName = functionName;
            CustomMarshallers = customMarshallers;
        }
    }
}
