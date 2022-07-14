// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection
{
    public readonly partial struct CustomAttributeTypedArgument : IEquatable<CustomAttributeTypedArgument>
    {
        public static bool operator ==(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => !left.Equals(right);

        private readonly object? _value;
        private readonly Type _argumentType;

        public CustomAttributeTypedArgument(Type argumentType, object? value)
        {
            ArgumentNullException.ThrowIfNull(argumentType);

            _value = CanonicalizeValue(value);
            _argumentType = argumentType;
        }

        public CustomAttributeTypedArgument(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            _value = CanonicalizeValue(value);
            _argumentType = value.GetType();
        }


        public override string ToString() => ToString(false);

        internal string ToString(bool typed)
        {
            if (_argumentType is null)
                return base.ToString()!;

            if (ArgumentType.IsEnum)
                return typed ? $"{Value}" : $"({ArgumentType.FullName}){Value}";

            if (Value is null)
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
                int count = array.Count;
                result.Append(count.ToString());
                result.Append("] { ");

                for (int i = 0; i < count; i++)
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

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CustomAttributeTypedArgument cata && Equals(cata);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(CustomAttributeTypedArgument other) => _value == other._value && _argumentType == other._argumentType;

        public Type ArgumentType => _argumentType;
        public object? Value => _value;

        private static object? CanonicalizeValue(object? value) => (value is Enum e) ? e.GetValue() : value;
    }
}
