// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM
    public 
#endif
    struct CustomAttributeNamedArgument<TType>
    {
        private readonly string _name;
        private readonly CustomAttributeNamedArgumentKind _kind;
        private readonly TType _type;
        private readonly object _value;

        public CustomAttributeNamedArgument(string name, CustomAttributeNamedArgumentKind kind, TType type, object value)
        {
            _name = name;
            _kind = kind;
            _type = type;
            _value = value;
        }

        public string Name
        {
            get { return _name; }
        }

        public CustomAttributeNamedArgumentKind Kind
        {
            get { return _kind; }
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
