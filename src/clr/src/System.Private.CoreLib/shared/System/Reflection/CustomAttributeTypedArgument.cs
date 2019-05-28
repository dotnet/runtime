// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

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
                return base.ToString();

            if (ArgumentType.IsEnum)
                return string.Format(typed ? "{0}" : "({1}){0}", Value, ArgumentType.FullName);

            else if (Value == null)
                return string.Format(typed ? "null" : "({0})null", ArgumentType.Name);

            else if (ArgumentType == typeof(string))
                return string.Format("\"{0}\"", Value);

            else if (ArgumentType == typeof(char))
                return string.Format("'{0}'", Value);

            else if (ArgumentType == typeof(Type))
                return string.Format("typeof({0})", ((Type)Value!).FullName);

            else if (ArgumentType.IsArray)
            {
                IList<CustomAttributeTypedArgument> array = (IList<CustomAttributeTypedArgument>)Value!;

                Type elementType = ArgumentType.GetElementType()!;
                string result = string.Format(@"new {0}[{1}] {{ ", elementType.IsEnum ? elementType.FullName : elementType.Name, array.Count);

                for (int i = 0; i < array.Count; i++)
                {
                    result += string.Format(i == 0 ? "{0}" : ", {0}", array[i].ToString(elementType != typeof(object)));
                }

                result += " }";

                return result;
            }

            return string.Format(typed ? "{0}" : "({1}){0}", Value, ArgumentType.Name);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj) => obj == (object)this;

        public Type ArgumentType => m_argumentType;
        public object? Value => m_value;
    }
}
