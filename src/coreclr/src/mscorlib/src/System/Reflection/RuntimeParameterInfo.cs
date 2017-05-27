// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using MdToken = System.Reflection.MetadataToken;

namespace System.Reflection
{
    internal unsafe sealed class RuntimeParameterInfo : ParameterInfo, ISerializable
    {
        #region Static Members
        internal unsafe static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            ParameterInfo dummy;
            return GetParameters(method, member, sig, out dummy, false);
        }

        internal unsafe static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            ParameterInfo returnParameter;
            GetParameters(method, member, sig, out returnParameter, true);
            return returnParameter;
        }

        internal unsafe static ParameterInfo[] GetParameters(
            IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo returnParameter, bool fetchReturnParameter)
        {
            returnParameter = null;
            int sigArgCount = sig.Arguments.Length;
            ParameterInfo[] args = fetchReturnParameter ? null : new ParameterInfo[sigArgCount];

            int tkMethodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
            int cParamDefs = 0;

            // Not all methods have tokens. Arrays, pointers and byRef types do not have tokens as they
            // are generated on the fly by the runtime. 
            if (!MdToken.IsNullToken(tkMethodDef))
            {
                MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));

                MetadataEnumResult tkParamDefs;
                scope.EnumParams(tkMethodDef, out tkParamDefs);

                cParamDefs = tkParamDefs.Length;

                // Not all parameters have tokens. Parameters may have no token 
                // if they have no name and no attributes.
                if (cParamDefs > sigArgCount + 1 /* return type */)
                    throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                for (int i = 0; i < cParamDefs; i++)
                {
                    #region Populate ParameterInfos
                    ParameterAttributes attr;
                    int position, tkParamDef = tkParamDefs[i];

                    scope.GetParamDefProps(tkParamDef, out position, out attr);

                    position--;

                    if (fetchReturnParameter == true && position == -1)
                    {
                        // more than one return parameter?
                        if (returnParameter != null)
                            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                        returnParameter = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    else if (fetchReturnParameter == false && position >= 0)
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
                if (returnParameter == null)
                {
                    returnParameter = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, (ParameterAttributes)0, member);
                }
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

        #region Private Statics
        private static readonly Type s_DecimalConstantAttributeType = typeof(DecimalConstantAttribute);
        private static readonly Type s_CustomConstantAttributeType = typeof(CustomConstantAttribute);
        #endregion

        #region Private Data Members
        // These are new in Whidbey, so we cannot serialize them directly or we break backwards compatibility.
        [NonSerialized]
        private int m_tkParamDef;
        [NonSerialized]
        private MetadataImport m_scope;
        [NonSerialized]
        private Signature m_signature;
        [NonSerialized]
        private volatile bool m_nameIsCached = false;
        [NonSerialized]
        private readonly bool m_noMetadata = false;
        [NonSerialized]
        private bool m_noDefaultValue = false;
        [NonSerialized]
        private MethodBase m_originalMember = null;
        #endregion

        #region Internal Properties
        internal MethodBase DefiningMethod
        {
            get
            {
                MethodBase result = m_originalMember != null ? m_originalMember : MemberImpl as MethodBase;
                Debug.Assert(result != null);
                return result;
            }
        }
        #endregion

        #region Internal Methods
        internal void SetName(string name)
        {
            NameImpl = name;
        }

        internal void SetAttributes(ParameterAttributes attributes)
        {
            AttrsImpl = attributes;
        }
        #endregion

        #region VTS magic to serialize/deserialized to/from pre-Whidbey endpoints.
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
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

            // Strictly speeking, property's don't contain paramter tokens
            // However we need this to make ca's work... oh well...
            m_tkParamDef = MdToken.IsNullToken(accessor.MetadataToken) ? (int)MetadataTokenType.ParamDef : accessor.MetadataToken;
            m_scope = accessor.m_scope;
        }

        private RuntimeParameterInfo(
            Signature signature, MetadataImport scope, int tkParamDef,
            int position, ParameterAttributes attributes, MemberInfo member)
        {
            Contract.Requires(member != null);
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
        internal RuntimeParameterInfo(MethodInfo owner, String name, Type parameterType, int position)
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

        public override String Name
        {
            get
            {
                if (!m_nameIsCached)
                {
                    if (!MdToken.IsNullToken(m_tkParamDef))
                    {
                        string name;
                        name = m_scope.GetName(m_tkParamDef).ToString();
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

                object defaultValue = GetDefaultValueInternal(false);

                return (defaultValue != DBNull.Value);
            }
        }

        public override Object DefaultValue { get { return GetDefaultValue(false); } }
        public override Object RawDefaultValue { get { return GetDefaultValue(true); } }

        private Object GetDefaultValue(bool raw)
        {
            // OLD COMMENT (Is this even true?)
            // Cannot cache because default value could be non-agile user defined enumeration.
            // OLD COMMENT ends
            if (m_noMetadata)
                return null;

            // for dynamic method we pretend to have cached the value so we do not go to metadata
            object defaultValue = GetDefaultValueInternal(raw);

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

        // returns DBNull.Value if the parameter doesn't have a default value
        private Object GetDefaultValueInternal(bool raw)
        {
            Debug.Assert(!m_noMetadata);

            if (m_noDefaultValue)
                return DBNull.Value;

            object defaultValue = null;

            // Why check the parameter type only for DateTime and only for the ctor arguments? 
            // No check on the parameter type is done for named args and for Decimal.

            // We should move this after MdToken.IsNullToken(m_tkParamDef) and combine it 
            // with the other custom attribute logic. But will that be a breaking change?
            // For a DateTime parameter on which both an md constant and a ca constant are set,
            // which one should win?
            if (ParameterType == typeof(DateTime))
            {
                if (raw)
                {
                    CustomAttributeTypedArgument value =
                        CustomAttributeData.Filter(
                            CustomAttributeData.GetCustomAttributes(this), typeof(DateTimeConstantAttribute), 0);

                    if (value.ArgumentType != null)
                        return new DateTime((long)value.Value);
                }
                else
                {
                    object[] dt = GetCustomAttributes(typeof(DateTimeConstantAttribute), false);
                    if (dt != null && dt.Length != 0)
                        return ((DateTimeConstantAttribute)dt[0]).Value;
                }
            }

            #region Look for a default value in metadata
            if (!MdToken.IsNullToken(m_tkParamDef))
            {
                // This will return DBNull.Value if no constant value is defined on m_tkParamDef in the metadata.
                defaultValue = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.GetTypeHandleInternal(), raw);
            }
            #endregion

            if (defaultValue == DBNull.Value)
            {
                #region Look for a default value in the custom attributes
                if (raw)
                {
                    foreach (CustomAttributeData attr in CustomAttributeData.GetCustomAttributes(this))
                    {
                        Type attrType = attr.Constructor.DeclaringType;

                        if (attrType == typeof(DateTimeConstantAttribute))
                        {
                            defaultValue = DateTimeConstantAttribute.GetRawDateTimeConstant(attr);
                        }
                        else if (attrType == typeof(DecimalConstantAttribute))
                        {
                            defaultValue = DecimalConstantAttribute.GetRawDecimalConstant(attr);
                        }
                        else if (attrType.IsSubclassOf(s_CustomConstantAttributeType))
                        {
                            defaultValue = CustomConstantAttribute.GetRawConstant(attr);
                        }
                    }
                }
                else
                {
                    Object[] CustomAttrs = GetCustomAttributes(s_CustomConstantAttributeType, false);
                    if (CustomAttrs.Length != 0)
                    {
                        defaultValue = ((CustomConstantAttribute)CustomAttrs[0]).Value;
                    }
                    else
                    {
                        CustomAttrs = GetCustomAttributes(s_DecimalConstantAttributeType, false);
                        if (CustomAttrs.Length != 0)
                        {
                            defaultValue = ((DecimalConstantAttribute)CustomAttrs[0]).Value;
                        }
                    }
                }
                #endregion
            }

            if (defaultValue == DBNull.Value)
                m_noDefaultValue = true;

            return defaultValue;
        }

        internal RuntimeModule GetRuntimeModule()
        {
            RuntimeMethodInfo method = Member as RuntimeMethodInfo;
            RuntimeConstructorInfo constructor = Member as RuntimeConstructorInfo;
            RuntimePropertyInfo property = Member as RuntimePropertyInfo;

            if (method != null)
                return method.GetRuntimeModule();
            else if (constructor != null)
                return constructor.GetRuntimeModule();
            else if (property != null)
                return property.GetRuntimeModule();
            else
                return null;
        }

        public override int MetadataToken
        {
            get
            {
                return m_tkParamDef;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return m_signature.GetCustomModifiers(PositionImpl + 1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return m_signature.GetCustomModifiers(PositionImpl + 1, false);
        }

        #endregion

        #region ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            if (MdToken.IsNullToken(m_tkParamDef))
                return Array.Empty<Object>();

            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            if (MdToken.IsNullToken(m_tkParamDef))
                return Array.Empty<Object>();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            if (MdToken.IsNullToken(m_tkParamDef))
                return false;

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion
    }
}
