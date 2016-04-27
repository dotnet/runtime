// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata.Decoding
{
    public struct CustomAttributeTypedArgument<TType>
    {
        private readonly TType _type;
        private readonly object _value;

        public CustomAttributeTypedArgument(TType type, object value)
        {
            _type = type;
            _value = value;
        }

        public TType Type
        {
            get { return _type; }
        }

        public object Value
        {
            get { return _value; }
        }
    }
}
