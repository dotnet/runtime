// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public readonly struct CustomAttributeTypedArgument<TType>
    {
        public TType Type { get; }
        public object Value { get; }

        public CustomAttributeTypedArgument(TType type, object value)
        {
            Type = type;
            Value = value;
        }
    }
}
