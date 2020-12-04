// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    //
    // The DefaultParameterValueAttribute is used in C# to set
    // the default value for parameters when calling methods
    // from other languages. This is particularly useful for
    // methods defined in COM interop interfaces.
    //
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object? value)
        {
            Value = value;
        }

        public object? Value { get; }
    }
}
