// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    internal sealed partial class RuntimeType : TypeInfo, ICloneable
    {
        public override Assembly Assembly => RuntimeTypeHandle.GetAssembly(this);
        public override Type? BaseType => GetBaseType();
        public override bool IsByRefLike => RuntimeTypeHandle.IsByRefLike(this);
        public override bool IsConstructedGenericType => IsGenericType && !IsGenericTypeDefinition;
        public override bool IsGenericType => RuntimeTypeHandle.HasInstantiation(this);
        public override bool IsGenericTypeDefinition => RuntimeTypeHandle.IsGenericTypeDefinition(this);
        public override bool IsGenericParameter => RuntimeTypeHandle.IsGenericVariable(this);
        public override bool IsTypeDefinition => RuntimeTypeHandle.IsTypeDefinition(this);
        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;
        public override MemberTypes MemberType => (IsPublic || IsNotPublic) ? MemberTypes.TypeInfo : MemberTypes.NestedType;
        public override int MetadataToken => RuntimeTypeHandle.GetToken(this);
        public override Module Module => GetRuntimeModule();
        public override Type? ReflectedType => DeclaringType;
        public override RuntimeTypeHandle TypeHandle => new RuntimeTypeHandle(this);
        public override Type UnderlyingSystemType => this;

        public object Clone() => this;

        public override bool Equals(object? obj)
        {
            // ComObjects are identified by the instance of the Type object and not the TypeHandle.
            return obj == (object)this;
        }

        public override int GetArrayRank()
        {
            if (!IsArrayImpl())
                throw new ArgumentException(SR.Argument_HasToBeArrayClass);

            return RuntimeTypeHandle.GetArrayRank(this);
        }

        protected override TypeAttributes GetAttributeFlagsImpl() => RuntimeTypeHandle.GetAttributes(this);

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, ObjectType, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType is null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        // GetDefaultMembers
        // This will return a MemberInfo that has been marked with the [DefaultMemberAttribute]
        public override MemberInfo[] GetDefaultMembers()
        {
            // See if we have cached the default member name
            MemberInfo[] members = null!;

            string? defaultMemberName = GetDefaultMemberName();
            if (defaultMemberName != null)
            {
                members = GetMember(defaultMemberName);
            }

            if (members == null)
                members = Array.Empty<MemberInfo>();

            return members;
        }

        public override Type GetElementType() => RuntimeTypeHandle.GetElementType(this);

        public override string? GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Type valueType = value.GetType();

            if (!(valueType.IsEnum || IsIntegerType(valueType)))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            ulong ulValue = Enum.ToUInt64(value);

            return Enum.GetEnumName(this, ulValue);
        }

        public override string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            string[] ret = Enum.InternalGetNames(this);

            // Make a copy since we can't hand out the same array since users can modify them
            return new ReadOnlySpan<string>(ret).ToArray();
        }

        public override Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            // Get all of the values
            ulong[] values = Enum.InternalGetValues(this);

            // Create a generic Array
            Array ret = Array.CreateInstance(this, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                object val = Enum.ToObject(this, values[i]);
                ret.SetValue(val, i);
            }

            return ret;
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return Enum.InternalGetUnderlyingType(this);
        }

        public override Type GetGenericTypeDefinition()
        {
            if (!IsGenericType)
                throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);

            return RuntimeTypeHandle.GetGenericTypeDefinition(this);
        }

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        internal RuntimeModule GetRuntimeModule() => RuntimeTypeHandle.GetModule(this);

        protected override TypeCode GetTypeCodeImpl()
        {
            TypeCode typeCode = Cache.TypeCode;

            if (typeCode != TypeCode.Empty)
                return typeCode;

            CorElementType corElementType = RuntimeTypeHandle.GetCorElementType(this);
            switch (corElementType)
            {
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    typeCode = TypeCode.Boolean; break;
                case CorElementType.ELEMENT_TYPE_CHAR:
                    typeCode = TypeCode.Char; break;
                case CorElementType.ELEMENT_TYPE_I1:
                    typeCode = TypeCode.SByte; break;
                case CorElementType.ELEMENT_TYPE_U1:
                    typeCode = TypeCode.Byte; break;
                case CorElementType.ELEMENT_TYPE_I2:
                    typeCode = TypeCode.Int16; break;
                case CorElementType.ELEMENT_TYPE_U2:
                    typeCode = TypeCode.UInt16; break;
                case CorElementType.ELEMENT_TYPE_I4:
                    typeCode = TypeCode.Int32; break;
                case CorElementType.ELEMENT_TYPE_U4:
                    typeCode = TypeCode.UInt32; break;
                case CorElementType.ELEMENT_TYPE_I8:
                    typeCode = TypeCode.Int64; break;
                case CorElementType.ELEMENT_TYPE_U8:
                    typeCode = TypeCode.UInt64; break;
                case CorElementType.ELEMENT_TYPE_R4:
                    typeCode = TypeCode.Single; break;
                case CorElementType.ELEMENT_TYPE_R8:
                    typeCode = TypeCode.Double; break;
                case CorElementType.ELEMENT_TYPE_STRING:
                    typeCode = TypeCode.String; break;
                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    if (this == Convert.ConvertTypes[(int)TypeCode.Decimal])
                        typeCode = TypeCode.Decimal;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.DateTime])
                        typeCode = TypeCode.DateTime;
                    else if (IsEnum)
                        typeCode = GetTypeCode(Enum.GetUnderlyingType(this));
                    else
                        typeCode = TypeCode.Object;
                    break;
                default:
                    if (this == Convert.ConvertTypes[(int)TypeCode.DBNull])
                        typeCode = TypeCode.DBNull;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.String])
                        typeCode = TypeCode.String;
                    else
                        typeCode = TypeCode.Object;
                    break;
            }

            Cache.TypeCode = typeCode;

            return typeCode;
        }

        protected override bool HasElementTypeImpl() => RuntimeTypeHandle.HasElementType(this);

        protected override bool IsArrayImpl() => RuntimeTypeHandle.IsArray(this);

        protected override bool IsContextfulImpl() => false;

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType is null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Check if both of them are of the same type
            RuntimeType valueType = (RuntimeType)value.GetType();

            // If the value is an Enum then we need to extract the underlying value from it
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, this));

                valueType = (RuntimeType)valueType.GetEnumUnderlyingType();
            }

            // If a string is passed in
            if (valueType == StringType)
            {
                // Get all of the Fields, calling GetHashEntry directly to avoid copying
                string[] names = Enum.InternalGetNames(this);
                return Array.IndexOf(names, value) >= 0;
            }

            // If an enum or integer value is passed in
            if (IsIntegerType(valueType))
            {
                RuntimeType underlyingType = Enum.InternalGetUnderlyingType(this);
                if (underlyingType != valueType)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, valueType, underlyingType));

                ulong[] ulValues = Enum.InternalGetValues(this);
                ulong ulValue = Enum.ToUInt64(value);

                return (Array.BinarySearch(ulValues, ulValue) >= 0);
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        protected override bool IsValueTypeImpl()
        {
            // We need to return true for generic parameters with the ValueType constraint.
            // So we cannot use the faster RuntimeTypeHandle.IsValueType because it returns 
            // false for all generic parameters.
            if (this == typeof(ValueType) || this == typeof(Enum))
                return false;

            return IsSubclassOf(typeof(ValueType));
        }

        protected override bool IsByRefImpl() => RuntimeTypeHandle.IsByRef(this);

        protected override bool IsPrimitiveImpl() => RuntimeTypeHandle.IsPrimitive(this);

        protected override bool IsPointerImpl() => RuntimeTypeHandle.IsPointer(this);

        protected override bool IsCOMObjectImpl() => RuntimeTypeHandle.IsComObject(this, false);

        public override bool IsInstanceOfType(object? o) => RuntimeTypeHandle.IsInstanceOfType(this, o);

        public override bool IsAssignableFrom(TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;

            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsAssignableFrom(Type? c)
        {
            if (c is null)
                return false;

            if (ReferenceEquals(c, this))
                return true;

            // For runtime type, let the VM decide.
            if (c.UnderlyingSystemType is RuntimeType fromType)
            {
                // both this and c (or their underlying system types) are runtime types
                return RuntimeTypeHandle.CanCastTo(fromType, this);
            }

            // Special case for TypeBuilder to be backward-compatible.
            if (c is System.Reflection.Emit.TypeBuilder)
            {
                // If c is a subclass of this class, then c can be cast to this type.
                if (c.IsSubclassOf(this))
                    return true;

                if (IsInterface)
                {
                    return c.ImplementInterface(this);
                }
                else if (IsGenericParameter)
                {
                    Type[] constraints = GetGenericParameterConstraints();
                    for (int i = 0; i < constraints.Length; i++)
                        if (!constraints[i].IsAssignableFrom(c))
                            return false;

                    return true;
                }
            }

            // For anything else we return false.
            return false;
        }

        private RuntimeType? GetBaseType()
        {
            if (IsInterface)
                return null;

            if (RuntimeTypeHandle.IsGenericVariable(this))
            {
                Type[] constraints = GetGenericParameterConstraints();

                RuntimeType baseType = ObjectType;

                for (int i = 0; i < constraints.Length; i++)
                {
                    RuntimeType constraint = (RuntimeType)constraints[i];

                    if (constraint.IsInterface)
                        continue;

                    if (constraint.IsGenericParameter)
                    {
                        GenericParameterAttributes special;
                        special = constraint.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

                        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) == 0 &&
                            (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                            continue;
                    }

                    baseType = constraint;
                }

                if (baseType == ObjectType)
                {
                    GenericParameterAttributes special;
                    special = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                    if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        baseType = ValueType;
                }

                return baseType;
            }

            return RuntimeTypeHandle.GetBaseType(this);
        }        
    }
}