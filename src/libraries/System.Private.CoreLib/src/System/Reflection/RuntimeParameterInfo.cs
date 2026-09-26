// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NATIVEAOT
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.PropertyInfos;
using Internal.Metadata.NativeFormat;
#else
using MdToken = System.Reflection.MetadataToken;
#endif

namespace System.Reflection
{
    internal sealed partial class RuntimeParameterInfo : ParameterInfo
    {
        #region Private Data Members
        private volatile bool m_nameIsCached;
        private readonly bool m_noMetadata;
        private bool m_noDefaultValue;
        private readonly MethodBase? m_originalMember;
        #endregion

        #region Internal Properties
        internal MethodBase DefiningMethod
        {
            get
            {
                MethodBase? result = m_originalMember ?? MemberImpl as MethodBase;
                Debug.Assert(result != null);
                return result;
            }
        }
        #endregion

        #region Internal Methods
        internal void SetName(string? name)
        {
            NameImpl = name;
        }

        internal void SetAttributes(ParameterAttributes attributes)
        {
            AttrsImpl = attributes;
        }
        #endregion

        #region Constructor
        // used by RuntimePropertyInfo
        internal RuntimeParameterInfo(RuntimeParameterInfo accessor, RuntimePropertyInfo property)
            : this(accessor, (MemberInfo)property)
        {
#if NATIVEAOT
            m_signature = property.GetParameterTypeHandle(PositionImpl);
#else
            m_signature = property.Signature;
#endif
        }

        private RuntimeParameterInfo(RuntimeParameterInfo accessor, MemberInfo member)
        {
            // Change ownership
            MemberImpl = member;

            // The original owner should always be a method, because this method is only used to
            // change the owner from a method to a property.
            m_originalMember = accessor.MemberImpl as MethodBase;
            Debug.Assert(m_originalMember != null);

            // Populate all the caches -- we inherit this behavior from RTM
            NameImpl = accessor.Name;
            m_nameIsCached = true;
            ClassImpl = accessor.ParameterType;
            PositionImpl = accessor.Position;
            AttrsImpl = accessor.Attributes;

            // Strictly speaking, properties don't contain parameter tokens
            // However we need this to make ca's work... oh well...
#if NATIVEAOT
            m_tkParamDef = accessor.m_tkParamDef;
            m_typeContext = accessor.m_typeContext;
#else
            m_tkParamDef = MdToken.IsNullToken(accessor.MetadataToken) ? (int)MetadataTokenType.ParamDef : accessor.MetadataToken;
#endif
            m_scope = accessor.m_scope;
        }

        #endregion

        #region Public Methods
        public override bool Equals(object? obj) =>
            obj is RuntimeParameterInfo other &&
            PositionImpl == other.PositionImpl &&
            MemberImpl.Equals(other.MemberImpl);

        public override int GetHashCode() =>
            HashCode.Combine(MemberImpl, PositionImpl);

        public override Type ParameterType
        {
            get
            {
                // only instance of ParameterInfo has ClassImpl, all its subclasses don't
                if (ClassImpl == null)
                {
#if NATIVEAOT
                    ClassImpl = m_signature.Resolve(m_typeContext).ToType();
#else
                    Debug.Assert(m_signature != null);

                    RuntimeType parameterType;
                    if (PositionImpl == -1)
                        parameterType = m_signature.ReturnType;
                    else
                        parameterType = m_signature.Arguments[PositionImpl];

                    Debug.Assert(parameterType != null);
                    // different thread could only write ClassImpl to the same value, so a race condition is not a problem here
                    ClassImpl = parameterType;
#endif
                }

                return ClassImpl;
            }
        }

        public override string? Name
        {
            get
            {
                if (!m_nameIsCached)
                {
                    if (!MdToken.IsNullToken(m_tkParamDef))
                    {
                        string name = m_scope.GetName(m_tkParamDef).ToString();
                        GC.KeepAlive(this);
                        NameImpl = name;
                    }

                    // other threads could only write it to true, so a race condition is OK
                    // this field is volatile, so the write ordering is guaranteed
                    m_nameIsCached = true;
                }

                // name may be null
                return NameImpl;
            }
        }

        public override bool HasDefaultValue
        {
            get
            {
                if (m_noMetadata || m_noDefaultValue)
                    return false;

                return TryGetDefaultValueInternal(false, out _);
            }
        }

        public override object? DefaultValue => GetDefaultValue(false);
        public override object? RawDefaultValue => GetDefaultValue(true);

        private object? GetDefaultValue(bool raw)
        {
            // OLD COMMENT (Is this even true?)
            // Cannot cache because default value could be non-agile user defined enumeration.
            // OLD COMMENT ends
            if (m_noMetadata)
                return null;

            // for dynamic method we pretend to have cached the value so we do not go to metadata
            if (!TryGetDefaultValueInternal(raw, out object? defaultValue))
            {
                #region Handle case if no default value was found
                if (IsOptional)
                {
                    // If the argument is marked as optional then the default value is Missing.Value.
                    defaultValue = Missing.Value;
                }
                #endregion
            }

            return defaultValue;
        }

        private object? GetDefaultValueFromCustomAttributeData()
        {
            foreach (CustomAttributeData attributeData in CustomAttributeData.GetCustomAttributes(this))
            {
                Type attributeType = attributeData.AttributeType;
                if (attributeType == typeof(DecimalConstantAttribute))
                {
                    return GetRawDecimalConstant(attributeData);
                }
                else if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    if (attributeType == typeof(DateTimeConstantAttribute))
                    {
                        return GetRawDateTimeConstant(attributeData);
                    }
                    return GetRawConstant(attributeData);
                }
            }
            return DBNull.Value;
        }

        private object? GetDefaultValueFromCustomAttributes()
        {
            object[] customAttributes = GetCustomAttributes(typeof(CustomConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((CustomConstantAttribute)customAttributes[0]).Value;

            customAttributes = GetCustomAttributes(typeof(DecimalConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((DecimalConstantAttribute)customAttributes[0]).Value;

            return DBNull.Value;
        }

        // returns DBNull.Value if the parameter doesn't have a default value
        private bool TryGetDefaultValueInternal(bool raw, out object? defaultValue)
        {
            Debug.Assert(!m_noMetadata);

            if (m_noDefaultValue || MdToken.IsNullToken(m_tkParamDef))
            {
                defaultValue = DBNull.Value;
                m_noDefaultValue = true;
                return false;
            }

            // Prioritize metadata constant over custom attribute constant
            #region Look for a default value in metadata
            // This will return DBNull.Value if no constant value is defined on m_tkParamDef in the metadata.
#if NATIVEAOT
            defaultValue = GetDefaultValueFromMetadata(raw);
#else
            defaultValue = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.TypeHandle, raw);
            GC.KeepAlive(this);
#endif

            // If default value is not specified in metadata, look for it in custom attributes
            if (defaultValue == DBNull.Value)
            {
                // The resolution of default value is done by following these rules:
                // 1. For RawDefaultValue, we pick the first custom attribute holding the constant value
                //  in the following order: DecimalConstantAttribute, DateTimeConstantAttribute, CustomConstantAttribute
                // 2. For DefaultValue, we first look for CustomConstantAttribute and pick the first occurrence.
                //  If none is found, then we repeat the same process searching for DecimalConstantAttribute.
                // IMPORTANT: Please note that there is a subtle difference in order custom attributes are inspected for
                //  RawDefaultValue and DefaultValue.
                defaultValue = raw ? GetDefaultValueFromCustomAttributeData() : GetDefaultValueFromCustomAttributes();

                if (defaultValue == DBNull.Value)
                {
                    m_noDefaultValue = true;
                    return false;
                }
            }

            return true;

            #endregion
        }

        private static decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.Constructor.DeclaringType == typeof(DecimalConstantAttribute));
            IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;
            Debug.Assert(args.Count == 5);

            return new decimal(
                lo: GetConstructorArgument(args, 4),
                mid: GetConstructorArgument(args, 3),
                hi: GetConstructorArgument(args, 2),
                isNegative: ((byte)args[1].Value!) != 0,
                scale: (byte)args[0].Value!);

            static int GetConstructorArgument(IList<CustomAttributeTypedArgument> args, int index)
            {
                // The constructor is overloaded to accept both signed and unsigned arguments
                object obj = args[index].Value!;
                return (obj is int value) ? value : (int)(uint)obj;
            }
        }

        private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.Constructor.DeclaringType == typeof(DateTimeConstantAttribute));
            Debug.Assert(attr.ConstructorArguments.Count == 1);

            return new DateTime((long)attr.ConstructorArguments[0].Value!);
        }

        private static object? GetRawConstant(CustomAttributeData attr)
        {
            // We are relying only on named arguments for historical reasons
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }

            // Return DBNull to indicate that no default value is available.
            // Not to be confused with a null return which indicates a null default value.
            return DBNull.Value;
        }

        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            if (MdToken.IsNullToken(m_tkParamDef))
                return [];

            return RuntimeCustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            if (MdToken.IsNullToken(m_tkParamDef))
                return RuntimeCustomAttribute.CreateAttributeArrayHelper(attributeRuntimeType, 0);

            return RuntimeCustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (MdToken.IsNullToken(m_tkParamDef))
                return false;

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return RuntimeCustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            if (MdToken.IsNullToken(m_tkParamDef))
                return Array.Empty<CustomAttributeData>();

            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion
    }
#if NATIVEAOT

    file static class MdToken
    {
        public static bool IsNullToken(ParameterHandle token) => token.IsNil;
    }

    file static class MetadataImportExtensions
    {
        public static string GetName(this MetadataReader? scope, ParameterHandle token) =>
            scope!.GetParameter(token).Name.GetStringOrNull(scope!) ?? string.Empty;
    }
#endif
}
