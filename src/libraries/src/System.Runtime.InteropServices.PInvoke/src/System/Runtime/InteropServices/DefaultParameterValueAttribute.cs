// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    //
    // The DefaultParameterValueAttribute is used in C# to set 
    // the default value for parameters when calling methods
    // from other languages. This is particularly useful for 
    // methods defined in COM interop interfaces.
    //
    [AttributeUsageAttribute(AttributeTargets.Parameter)]
    public sealed class DefaultParameterValueAttribute : Attribute
    {
        public DefaultParameterValueAttribute(object value)
        {
            _value = value;
        }

        public object Value { get { return _value; } }

        private object _value;
    }
}
