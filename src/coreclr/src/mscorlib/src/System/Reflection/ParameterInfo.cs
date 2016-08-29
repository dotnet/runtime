// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Metadata;
#endif //FEATURE_REMOTING
    using System.Security.Permissions;
    using System.Threading;
    using MdToken = System.Reflection.MetadataToken;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_ParameterInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ParameterInfo : _ParameterInfo, ICustomAttributeProvider, IObjectReference
    {
        #region Legacy Protected Members
        protected String NameImpl; 
        protected Type ClassImpl; 
        protected int PositionImpl; 
        protected ParameterAttributes AttrsImpl; 
        protected Object DefaultValueImpl; // cannot cache this as it may be non agile user defined enum
        protected MemberInfo MemberImpl;
        #endregion

        #region Legacy Private Members
        // These are here only for backwards compatibility -- they are not set
        // until this instance is serialized, so don't rely on their values from
        // arbitrary code.
#pragma warning disable 169
        [OptionalField]
        private IntPtr _importer;
        [OptionalField]
        private int _token;
        [OptionalField]
        private bool bExtraConstChecked;
#pragma warning restore 169
        #endregion

        #region Constructor
        protected ParameterInfo() 
        { 
        }         
        #endregion

        #region Internal Members
        // this is an internal api for DynamicMethod. A better solution is to change the relationship
        // between ParameterInfo and ParameterBuilder so that a ParameterBuilder can be seen as a writer
        // api over a ParameterInfo. However that is a possible breaking change so it needs to go through some process first
        internal void SetName(String name) 
        {
            NameImpl = name;
        }
        
        internal void SetAttributes(ParameterAttributes attributes) 
        {
            AttrsImpl = attributes;
        }
        #endregion

        #region Public Methods
        public virtual Type ParameterType 
        { 
            get 
            {
                return ClassImpl;
            } 
        }            
        
        public virtual String Name 
        { 
            get 
            {
                return NameImpl;
            } 
        }

        public virtual bool HasDefaultValue { get { throw new NotImplementedException(); }  }

        public virtual Object DefaultValue { get { throw new NotImplementedException(); } }
        public virtual Object RawDefaultValue  { get { throw new NotImplementedException(); } } 

        public virtual int Position { get { return PositionImpl; } }                                    
        public virtual ParameterAttributes Attributes { get { return AttrsImpl; } }

        public virtual MemberInfo Member {
            get {
                Contract.Ensures(Contract.Result<MemberInfo>() != null);
                return MemberImpl;
            }
        }

        public bool IsIn { get { return((Attributes & ParameterAttributes.In) != 0); } }        
        public bool IsOut { get { return((Attributes & ParameterAttributes.Out) != 0); } }
        public bool IsLcid { get { return((Attributes & ParameterAttributes.Lcid) != 0); } }
        public bool IsRetval { get { return((Attributes & ParameterAttributes.Retval) != 0); } }        
        public bool IsOptional { get { return((Attributes & ParameterAttributes.Optional) != 0); } }

        public virtual int MetadataToken
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeParameterInfo rtParam = this as RuntimeParameterInfo;
                if (rtParam != null)
                    return rtParam.MetadataToken;

                // return a null token
                return (int)MetadataTokenType.ParamDef;
            }
        }

        public virtual Type[] GetRequiredCustomModifiers() 
        {
            return EmptyArray<Type>.Value;
        }

        public virtual Type[] GetOptionalCustomModifiers() 
        {
            return EmptyArray<Type>.Value;
        }
        #endregion

        #region Object Overrides
        public override String ToString()
        {
            return ParameterType.FormatTypeName() + " " + Name;
        }
        #endregion

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }
        #region ICustomAttributeProvider
        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            return EmptyArray<Object>.Value;
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            return EmptyArray<Object>.Value;
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            return false;
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region _ParameterInfo implementation

#if !FEATURE_CORECLR
        void _ParameterInfo.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _ParameterInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _ParameterInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _ParameterInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

        #endregion

        #region IObjectReference
        // In V4 RuntimeParameterInfo is introduced. 
        // To support deserializing ParameterInfo instances serialized in earlier versions
        // we need to implement IObjectReference.
        [System.Security.SecurityCritical]
        public object GetRealObject(StreamingContext context)
        {
            Contract.Ensures(Contract.Result<Object>() != null);

            // Once all the serializable fields have come in we can set up the real
            // instance based on just two of them (MemberImpl and PositionImpl).

            if (MemberImpl == null)
                throw new SerializationException(Environment.GetResourceString(ResId.Serialization_InsufficientState));

            ParameterInfo[] args = null;

            switch (MemberImpl.MemberType)
            {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    if (PositionImpl == -1)
                    {
                        if (MemberImpl.MemberType == MemberTypes.Method)
                            return ((MethodInfo)MemberImpl).ReturnParameter;
                        else
                            throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));
                    }
                    else
                    {
                        args = ((MethodBase)MemberImpl).GetParametersNoCopy();

                        if (args != null && PositionImpl < args.Length)
                            return args[PositionImpl];
                        else
                            throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));
                    }

                case MemberTypes.Property:
                    args = ((RuntimePropertyInfo)MemberImpl).GetIndexParametersNoCopy();

                    if (args != null && PositionImpl > -1 && PositionImpl < args.Length)
                        return args[PositionImpl];
                    else
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));

                default:
                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NoParameterInfo));
            }
        }
        #endregion
    }

    [Serializable]
    internal unsafe sealed class RuntimeParameterInfo : ParameterInfo, ISerializable
    {
        #region Static Members
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            ParameterInfo dummy;
            return GetParameters(method, member, sig, out dummy, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            ParameterInfo returnParameter;
            GetParameters(method, member, sig, out returnParameter, true);
            return returnParameter;
        }

        [System.Security.SecurityCritical]  // auto-generated
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
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));

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
                            throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));

                        returnParameter = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    else if (fetchReturnParameter == false && position >= 0)
                    {
                        // position beyong sigArgCount?
                        if (position >= sigArgCount)
                            throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));

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
                Contract.Assert(result != null);
                return result;
            }
        }
        #endregion

        #region VTS magic to serialize/deserialized to/from pre-Whidbey endpoints.
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            // We could be serializing for consumption by a pre-Whidbey
            // endpoint. Therefore we set up all the serialized fields to look
            // just like a v1.0/v1.1 instance.

            // Need to set the type to ParameterInfo so that pre-Whidbey and Whidbey code
            // can deserialize this. This is also why we cannot simply use [OnSerializing].
            info.SetType(typeof(ParameterInfo));

            // Use the properties intead of the fields in case the fields haven't been et
            // _importer, bExtraConstChecked, and m_cachedData don't need to be set

            // Now set the legacy fields that the current implementation doesn't
            // use any more. Note that _importer is a raw pointer that should
            // never have been serialized in V1. We set it to zero here; if the
            // deserializer uses it (by calling GetCustomAttributes() on this
            // instance) they'll AV, but at least it will be a well defined
            // exception and not a random AV.

            info.AddValue("AttrsImpl", Attributes);
            info.AddValue("ClassImpl", ParameterType);
            info.AddValue("DefaultValueImpl", DefaultValue);
            info.AddValue("MemberImpl", Member);
            info.AddValue("NameImpl", Name);
            info.AddValue("PositionImpl", Position);
            info.AddValue("_token", m_tkParamDef);
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
            Contract.Assert(m_originalMember != null);

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
            Contract.Assert(MdToken.IsNullToken(tkParamDef) == scope.Equals(MetadataImport.EmptyImport));
            Contract.Assert(MdToken.IsNullToken(tkParamDef) || MdToken.IsTokenOfType(tkParamDef, MetadataTokenType.ParamDef));

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

                    Contract.Assert(parameterType != null);
                    // different thread could only write ClassImpl to the same value, so a race condition is not a problem here
                    ClassImpl = parameterType;
                }

                return ClassImpl;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
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
        [System.Security.SecuritySafeCritical]
        private Object GetDefaultValueInternal(bool raw)
        {
            Contract.Assert(!m_noMetadata);

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
                return EmptyArray<Object>.Value;

            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            if (MdToken.IsNullToken(m_tkParamDef))
                return EmptyArray<Object>.Value;

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            if (MdToken.IsNullToken(m_tkParamDef))
                return false;

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

#if FEATURE_REMOTING
        #region Remoting Cache
        private RemotingParameterCachedData m_cachedData;

        internal RemotingParameterCachedData RemotingCache
        {
            get
            {
                // This grabs an internal copy of m_cachedData and uses
                // that instead of looking at m_cachedData directly because
                // the cache may get cleared asynchronously.  This prevents
                // us from having to take a lock.
                RemotingParameterCachedData cache = m_cachedData;
                if (cache == null)
                {
                    cache = new RemotingParameterCachedData(this);
                    RemotingParameterCachedData ret = Interlocked.CompareExchange(ref m_cachedData, cache, null);
                    if (ret != null)
                        cache = ret;
                }
                return cache;
            }
        }
        #endregion
#endif //FEATURE_REMOTING
    }
}
