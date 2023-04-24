// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    public readonly partial struct CustomAttributeNamedArgument : IEquatable<CustomAttributeNamedArgument>
    {
        public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => !left.Equals(right);

        private readonly MemberInfo _memberInfo;
        private readonly CustomAttributeTypedArgument _value;

        public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
        {
            ArgumentNullException.ThrowIfNull(memberInfo);

            Type type = memberInfo switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => throw new ArgumentException(SR.Argument_InvalidMemberForNamedArgument)
            };

            _memberInfo = memberInfo;
            _value = new CustomAttributeTypedArgument(type, value);
        }

        public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
        {
            ArgumentNullException.ThrowIfNull(memberInfo);

            _memberInfo = memberInfo;
            _value = typedArgument;
        }

        public override string ToString()
        {
            if (_memberInfo is null)
                return base.ToString()!;

            return $"{MemberInfo.Name} = {TypedValue.ToString(ArgumentType != typeof(object))}";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is CustomAttributeNamedArgument other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(CustomAttributeNamedArgument other) =>
            _memberInfo == other._memberInfo &&
            _value == other._value;

        internal Type ArgumentType =>
            _memberInfo is FieldInfo fi ?
                fi.FieldType :
                ((PropertyInfo)_memberInfo).PropertyType;

        public MemberInfo MemberInfo => _memberInfo;
        public CustomAttributeTypedArgument TypedValue => _value;
        public string MemberName => MemberInfo.Name;
        public bool IsField => MemberInfo is FieldInfo;
    }
}
