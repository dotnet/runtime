// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    public readonly partial struct CustomAttributeTypedArgument
    {
        public static bool operator ==(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => left.Equals(right);

        public static bool operator !=(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => !left.Equals(right);

        private readonly object? m_value;
        private readonly Type m_argumentType;

        public CustomAttributeTypedArgument(Type argumentType, object? value)
        {
            // value can be null.
            if (argumentType == null)
                throw new ArgumentNullException(nameof(argumentType));

            m_value = (value is null) ? null : CanonicalizeValue(value);
            m_argumentType = argumentType;
        }

        public CustomAttributeTypedArgument(object value)
        {
            // value cannot be null.
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            m_value = CanonicalizeValue(value);
            m_argumentType = value.GetType();
        }


        public override string ToString() => ToString(false);

        internal string ToString(bool typed)
        {
            if (m_argumentType == null)
                return base.ToString()!;

            if (ArgumentType.IsEnum)
                return typed ? $"{Value}" : $"({ArgumentType.FullName}){Value}";

            if (Value == null)
                return typed ? "null" : $"({ArgumentType.Name})null";

            if (ArgumentType == typeof(string))
                return $"\"{Value}\"";

            if (ArgumentType == typeof(char))
                return $"'{Value}'";

            if (ArgumentType == typeof(Type))
                return $"typeof({((Type)Value!).FullName})";

            if (ArgumentType.IsArray)
            {
                IList<CustomAttributeTypedArgument> array = (IList<CustomAttributeTypedArgument>)Value!;
                Type elementType = ArgumentType.GetElementType()!;

                var result = new ValueStringBuilder(stackalloc char[256]);
                result.Append("new ");
                result.Append(elementType.IsEnum ? elementType.FullName : elementType.Name);
                result.Append('[');
                result.Append(array.Count.ToString());
                result.Append(']');

                for (int i = 0; i < array.Count; i++)
                {
                    if (i != 0)
                    {
                        result.Append(", ");
                    }
                    result.Append(array[i].ToString(elementType != typeof(object)));
                }

                result.Append(" }");

                return result.ToString();
            }

            return typed ? $"{Value}" : $"({ArgumentType.Name}){Value}";
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj) => obj == (object)this;

        public Type ArgumentType => m_argumentType;
        public object? Value => m_value;
    }
}
