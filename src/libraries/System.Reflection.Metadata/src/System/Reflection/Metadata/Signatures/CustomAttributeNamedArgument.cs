// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public readonly struct CustomAttributeNamedArgument<TType>
    {
        public string Name { get; }
        public CustomAttributeNamedArgumentKind Kind { get; }
        public TType Type { get; }
        public object Value { get; }

        public CustomAttributeNamedArgument(string name, CustomAttributeNamedArgumentKind kind, TType type, object value)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Value = value;
        }
    }
}
