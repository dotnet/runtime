// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MdToken = System.Reflection.MetadataToken;

namespace System.Reflection
{
    internal sealed unsafe class RuntimeParameterInfo : ParameterInfo
    {
        #region Static Members
        internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            return GetParameters(method, member, sig, out _, fetchReturnParameter: false);
        }

        internal static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            GetParameters(method, member, sig, out ParameterInfo? returnParameter, fetchReturnParameter: true);
            return returnParameter!;
        }

        private static ParameterInfo[] GetParameters(
            IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo? returnParameter, bool fetchReturnParameter)
        {
            returnParameter = null;
            int sigArgCount = sig.Arguments.Length;
            ParameterInfo[] args =
                fetchReturnParameter ? null! :
                sigArgCount == 0 ? Array.Empty<ParameterInfo>() :
                new ParameterInfo[sigArgCount];

            int tkMethodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
            int cParamDefs = 0;

            // Not all methods have tokens. Arrays, pointers and byRef types do not have tokens as they
            // are generated on the fly by the runtime.
            if (!MdToken.IsNullToken(tkMethodDef))
            {
                MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));

                scope.EnumParams(tkMethodDef, out MetadataEnumResult tkParamDefs);

                cParamDefs = tkParamDefs.Length;

                // Not all parameters have tokens. Parameters may have no token
                // if they have no name and no attributes.
                if (cParamDefs > sigArgCount + 1 /* return type */)
                    throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                for (int i = 0; i < cParamDefs; i++)
                {
                    #region Populate ParameterInfos
                    int tkParamDef = tkParamDefs[i];

                    scope.GetParamDefProps(tkParamDef, out int position, out ParameterAttributes attr);

                    position--;

                    if (fetchReturnParameter && position == -1)
                    {
                        // more than one return parameter?
                        if (returnParameter != null)
                            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                        returnParameter = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    else if (!fetchReturnParameter && position >= 0)
                    {
                        // position beyong sigArgCount?
                        if (position >= sigArgCount)
                            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                        args[position] = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    #endregion
                }
            }

            // Fill in empty ParameterInfos for those without tokens
            if (fetchReturnParameter)
            {
                returnParameter ??= new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, (ParameterAttributes)0, member);
            }
            else
            {
                if (cParamDefs < args.Length + 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] != null)
                            continue;

                        args[i] = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, i, (ParameterAttributes)0, member);
                    }
                }
            }

            return args;
        }
        #endregion

        #region Private Data Members
        private int m_tkParamDef;
        private MetadataImport m_scope;
        private Signature? m_signature;
        private volatile bool m_nameIsCached;
        private readonly bool m_noMetadata;
        private bool m_noDefaultValue;
        private MethodBase? m_originalMember;
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
            m_signature = property.Signature;
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

            // Strictly speeking, property's don't contain parameter tokens
            // However we need this to make ca's work... oh well...
            m_tkParamDef = MdToken.IsNullToken(accessor.MetadataToken) ? (int)MetadataTokenType.ParamDef : accessor.MetadataToken;
            m_scope = accessor.m_scope;
        }

        private RuntimeParameterInfo(
            Signature signature, MetadataImport scope, int tkParamDef,
            int position, ParameterAttributes attributes, MemberInfo member)
        {
            Debug.Assert(member != null);
            Debug.Assert(MdToken.IsNullToken(tkParamDef) == scope.Equals(MetadataImport.EmptyImport));
            Debug.Assert(MdToken.IsNullToken(tkParamDef) || MdToken.IsTokenOfType(tkParamDef, MetadataTokenType.ParamDef));

            PositionImpl = position;
            MemberImpl = member;
            m_signature = signature;
            m_tkParamDef = MdToken.IsNullToken(tkParamDef) ? (int)MetadataTokenType.ParamDef : tkParamDef;
            m_scope = scope;
            AttrsImpl = attributes;

            ClassImpl = null;
            NameImpl = null;
        }

        // ctor for no metadata MethodInfo in the DynamicMethod and RuntimeMethodInfo cases
        internal RuntimeParameterInfo(MethodInfo owner, string? name, Type parameterType, int position)
        {
            MemberImpl = owner;
            NameImpl = name;
            m_nameIsCached = true;
            m_noMetadata = true;
            ClassImpl = parameterType;
            PositionImpl = position;
            AttrsImpl = ParameterAttributes.None;
            m_tkParamDef = (int)MetadataTokenType.ParamDef;
            m_scope = MetadataImport.EmptyImport;
        }
        #endregion

        #region Public Methods
        public override Type ParameterType
        {
            get
            {
                // only instance of ParameterInfo has ClassImpl, all its subclasses don't
                if (ClassImpl == null)
                {
                    Debug.Assert(m_signature != null);

                    RuntimeType parameterType;
                    if (PositionImpl == -1)
                        parameterType = m_signature.ReturnType;
                    else
                        parameterType = m_signature.Arguments[PositionImpl];

                    Debug.Assert(parameterType != null);
                    // different thread could only write ClassImpl to the same value, so a race condition is not a problem here
                    ClassImpl = parameterType;
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

                object? defaultValue = GetDefaultValueInternal(false);

                return defaultValue != DBNull.Value;
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
            object? defaultValue = GetDefaultValueInternal(raw);

            if (defaultValue == DBNull.Value)
            {
                #region Handle case if no default value was found
                if (IsOptional)
                {
                    // If the argument is marked as optional then the default value is Missing.Value.
                    defaultValue = Type.Missing;
                }
                #endregion
            }

            return defaultValue;
        }

        private object? GetDefaultValueFromCustomAttributeData()
        {
            foreach (CustomAttributeData attributeData in RuntimeCustomAttributeData.GetCustomAttributes(this))
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
        private object? GetDefaultValueInternal(bool raw)
        {
            Debug.Assert(!m_noMetadata);

            if (m_noDefaultValue)
                return DBNull.Value;

            object? defaultValue = null;

            // Prioritize metadata constant over custom attribute constant
            #region Look for a default value in metadata
            if (!MdToken.IsNullToken(m_tkParamDef))
            {
                // This will return DBNull.Value if no constant value is defined on m_tkParamDef in the metadata.
                defaultValue = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.TypeHandle, raw);
            }
            #endregion

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
            }

            if (defaultValue == DBNull.Value)
                m_noDefaultValue = true;

            return defaultValue;
        }

        private static decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.Constructor.DeclaringType == typeof(DecimalConstantAttribute));
            System.Collections.Generic.IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;
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

        internal RuntimeModule? GetRuntimeModule()
        {
            RuntimeMethodInfo? method = Member as RuntimeMethodInfo;
            RuntimeConstructorInfo? constructor = Member as RuntimeConstructorInfo;
            RuntimePropertyInfo? property = Member as RuntimePropertyInfo;

            if (method != null)
                return method.GetRuntimeModule();
            else if (constructor != null)
                return constructor.GetRuntimeModule();
            else if (property != null)
                return property.GetRuntimeModule();
            else
                return null;
        }

        public override int MetadataToken => m_tkParamDef;

        public override Type[] GetRequiredCustomModifiers()
        {
            return m_signature is null ?
                Type.EmptyTypes :
                m_signature.GetCustomModifiers(PositionImpl + 1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return m_signature is null ?
                Type.EmptyTypes :
                m_signature.GetCustomModifiers(PositionImpl + 1, false);
        }

        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            if (MdToken.IsNullToken(m_tkParamDef))
                return Array.Empty<object>();

            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            if (MdToken.IsNullToken(m_tkParamDef))
                return CustomAttribute.CreateAttributeArrayHelper(attributeRuntimeType, 0);

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (MdToken.IsNullToken(m_tkParamDef))
                return false;

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion
    }
}
