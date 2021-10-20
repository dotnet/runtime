// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public readonly partial struct CustomAttributeNamedArgument
    {
        public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => !left.Equals(right);

        private readonly MemberInfo _memberInfo;
        private readonly CustomAttributeTypedArgument _value;

        public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
        {
            if (memberInfo is null)
                throw new ArgumentNullException(nameof(memberInfo));

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
            _memberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
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

        public override bool Equals(object? obj)
        {
            return obj == (object)this;
        }

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
