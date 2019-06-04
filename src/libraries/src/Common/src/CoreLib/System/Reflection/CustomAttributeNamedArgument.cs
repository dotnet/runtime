// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    public readonly partial struct CustomAttributeNamedArgument
    {
        public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => !left.Equals(right);

        private readonly MemberInfo m_memberInfo;
        private readonly CustomAttributeTypedArgument m_value;

        public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            Type type;
            if (memberInfo is FieldInfo field)
            {
                type = field.FieldType;
            }
            else if (memberInfo is PropertyInfo property)
            {
                type = property.PropertyType;
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidMemberForNamedArgument);
            }

            m_memberInfo = memberInfo;
            m_value = new CustomAttributeTypedArgument(type, value);
        }

        public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            m_memberInfo = memberInfo;
            m_value = typedArgument;
        }

        public override string ToString()
        {
            if (m_memberInfo == null)
                return base.ToString()!;

            return string.Format("{0} = {1}", MemberInfo.Name, TypedValue.ToString(ArgumentType != typeof(object)));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj == (object)this;
        }

        internal Type ArgumentType
        {
            get
            {
                return m_memberInfo is FieldInfo ?
                    ((FieldInfo)m_memberInfo).FieldType :
                    ((PropertyInfo)m_memberInfo).PropertyType;
            }
        }

        public MemberInfo MemberInfo => m_memberInfo;
        public CustomAttributeTypedArgument TypedValue => m_value;
        public string MemberName => MemberInfo.Name;
        public bool IsField => MemberInfo is FieldInfo;
    }
}
