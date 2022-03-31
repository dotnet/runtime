// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JSImportAttribute : Attribute
    {
        public string FunctionName { get; }
        public Type[]? CustomMarshallers { get; }

        public JSImportAttribute(string functionName)
        {
            FunctionName = functionName;
            CustomMarshallers = null;
        }

        public JSImportAttribute(string functionName, params Type[] customMarshallers)
        {
            FunctionName = functionName;
            CustomMarshallers = customMarshallers;
        }
    }
}
