// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Metadata;
#endif //FEATURE_REMOTING
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Threading;
    using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_FieldInfo))]
#pragma warning disable 618
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
#pragma warning restore 618
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class FieldInfo : MemberInfo, _FieldInfo
    {
        #region Static Members
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"), "handle");
                
            FieldInfo f = RuntimeType.GetFieldInfo(handle.GetRuntimeFieldInfo());
                       
            Type declaringType = f.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(String.Format(
                    CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_FieldDeclaringTypeGeneric"), 
                    f.Name, declaringType.GetGenericTypeDefinition()));

            return f;            
        }           
        
        [System.Runtime.InteropServices.ComVisible(false)]
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));

            return RuntimeType.GetFieldInfo(declaringType.GetRuntimeType(), handle.GetRuntimeFieldInfo());
        }           
        #endregion

        #region Constructor
        protected FieldInfo() { }       
        #endregion

        public static bool operator ==(FieldInfo left, FieldInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeFieldInfo || right is RuntimeFieldInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(FieldInfo left, FieldInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Field; } }
        #endregion

        #region Public Abstract\Virtual Members
        
        public virtual Type[] GetRequiredCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public virtual Type[] GetOptionalCustomModifiers()
        {
            throw new NotImplementedException();
        }

        [CLSCompliant(false)]
        public virtual void SetValueDirect(TypedReference obj, Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        [CLSCompliant(false)]
        public virtual Object GetValueDirect(TypedReference obj)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }    

        public abstract RuntimeFieldHandle FieldHandle { get; }

        public abstract Type FieldType { get; }    
     
        public abstract Object GetValue(Object obj);

        public virtual Object GetRawConstantValue() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS")); }

        public abstract void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);

        public abstract FieldAttributes Attributes { get; }
        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public void SetValue(Object obj, Object value)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
        }

        public bool IsPublic { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public; } }

        public bool IsPrivate { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private; } }

        public bool IsFamily { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family; } }

        public bool IsAssembly { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly; } }

        public bool IsFamilyAndAssembly { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem; } }

        public bool IsFamilyOrAssembly { get { return(Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem; } }

        public bool IsStatic { get { return(Attributes & FieldAttributes.Static) != 0; } }

        public bool IsInitOnly { get { return(Attributes & FieldAttributes.InitOnly) != 0; } }

        public bool IsLiteral { get { return(Attributes & FieldAttributes.Literal) != 0; } }

        public bool IsNotSerialized { get { return(Attributes & FieldAttributes.NotSerialized) != 0; } }

        public bool IsSpecialName  { get { return(Attributes & FieldAttributes.SpecialName) != 0; } }

        public bool IsPinvokeImpl { get { return(Attributes & FieldAttributes.PinvokeImpl) != 0; } }

        public virtual bool IsSecurityCritical
        {
            get { return FieldHandle.IsSecurityCritical(); }
        }

        public virtual bool IsSecuritySafeCritical
        {
            get { return FieldHandle.IsSecuritySafeCritical(); }
        }

        public virtual bool IsSecurityTransparent
        {
            get { return FieldHandle.IsSecurityTransparent(); }
        }

        #endregion

#if !FEATURE_CORECLR
        Type _FieldInfo.GetType()
        {
            return base.GetType();
        }

        void _FieldInfo.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _FieldInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _FieldInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _FieldInfo.Invoke in VM\DangerousAPIs.h and 
        // include _FieldInfo in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _FieldInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }

    [Serializable]
    internal abstract class RuntimeFieldInfo : FieldInfo, ISerializable
    {
        #region Private Data Members
        private BindingFlags m_bindingFlags;
        protected RuntimeTypeCache m_reflectedTypeCache;
        protected RuntimeType m_declaringType;        
        #endregion

        #region Constructor
        protected RuntimeFieldInfo()
        {
            // Used for dummy head node during population
        }
        protected RuntimeFieldInfo(RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
        {
            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_reflectedTypeCache = reflectedTypeCache;
        }
        #endregion

#if FEATURE_REMOTING
        #region Legacy Remoting Cache
        // The size of CachedData is accounted for by BaseObjectWithCachedData in object.h.
        // This member is currently being used by Remoting for caching remoting data. If you
        // need to cache data here, talk to the Remoting team to work out a mechanism, so that
        // both caching systems can happily work together.
        private RemotingFieldCachedData m_cachedData;

        internal RemotingFieldCachedData RemotingCache
        {
            get
            {
                // This grabs an internal copy of m_cachedData and uses
                // that instead of looking at m_cachedData directly because
                // the cache may get cleared asynchronously.  This prevents
                // us from having to take a lock.
                RemotingFieldCachedData cache = m_cachedData;
                if (cache == null)
                {
                    cache = new RemotingFieldCachedData(this);
                    RemotingFieldCachedData ret = Interlocked.CompareExchange(ref m_cachedData, cache, null);
                    if (ret != null)
                        cache = ret;
                }
                return cache;
            }
        }
        #endregion
#endif //FEATURE_REMOTING

        #region NonPublic Members
        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }
        private RuntimeType ReflectedTypeInternal
        { 
            get 
            { 
                return m_reflectedTypeCache.GetRuntimeType(); 
            } 
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal abstract RuntimeModule GetRuntimeModule();
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Field; } }
        public override Type ReflectedType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;
            }
        }
        
        public override Type DeclaringType 
        { 
            get 
            { 
                return m_reflectedTypeCache.IsGlobal ? null : m_declaringType; 
            } 
        }
        
        public override Module Module { get { return GetRuntimeModule(); } }
        #endregion

        #region Object Overrides
        public unsafe override String ToString() 
        {
            return FieldType.FormatTypeName() + " " + Name;
        }
        #endregion

        #region ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region FieldInfo Overrides
        // All implemented on derived classes
        #endregion

        #region ISerializable Implementation
        [System.Security.SecurityCritical]  // auto-generated
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            MemberInfoSerializationHolder.GetSerializationInfo(
                info,
                Name,
                ReflectedTypeInternal,
                ToString(),
                MemberTypes.Field);
        }
        #endregion
    }

    [Serializable]
    internal unsafe sealed class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
    {
        #region FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static private extern void PerformVisibilityCheckOnField(IntPtr field, Object target, RuntimeType declaringType, FieldAttributes attr, uint invocationFlags);
        #endregion

        #region Private Data Members
        // agressive caching
        private IntPtr m_fieldHandle;
        private FieldAttributes m_fieldAttributes;
        // lazy caching
        private string m_name;
        private RuntimeType m_fieldType;
        private INVOCATION_FLAGS m_invocationFlags;

#if FEATURE_APPX
        private bool IsNonW8PFrameworkAPI()
        {
            if (GetRuntimeType().IsNonW8PFrameworkAPI())
                return true;

            // Allow "value__"
            if (m_declaringType.IsEnum)
                return false;

            RuntimeAssembly rtAssembly = GetRuntimeAssembly();
            if (rtAssembly.IsFrameworkAssembly())
            {
                int ctorToken = rtAssembly.InvocableAttributeCtorToken;
                if (System.Reflection.MetadataToken.IsNullToken(ctorToken) ||
                    !CustomAttribute.IsAttributeDefined(GetRuntimeModule(), MetadataToken, ctorToken))
                    return true;
            }

            return false;
        }
#endif

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    Type declaringType = DeclaringType;
                    bool fIsReflectionOnlyType = (declaringType is ReflectionOnlyType);

                    INVOCATION_FLAGS invocationFlags = 0;

                    // first take care of all the NO_INVOKE cases
                    if (
                        (declaringType != null && declaringType.ContainsGenericParameters) ||
                        (declaringType == null && Module.Assembly.ReflectionOnly) ||
                        (fIsReflectionOnlyType)
                       )
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }

                    // If the invocationFlags are still 0, then
                    // this should be an usable field, determine the other flags 
                    if (invocationFlags == 0)
                    {
                        if ((m_fieldAttributes & FieldAttributes.InitOnly) != (FieldAttributes)0)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;

                        if ((m_fieldAttributes & FieldAttributes.HasFieldRVA) != (FieldAttributes)0)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;

                        // A public field is inaccesible to Transparent code if the field is Critical.
                        bool needsTransparencySecurityCheck = IsSecurityCritical && !IsSecuritySafeCritical;
                        bool needsVisibilitySecurityCheck = ((m_fieldAttributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) ||
                                                            (declaringType != null && declaringType.NeedsReflectionSecurityCheck);
                        if (needsTransparencySecurityCheck || needsVisibilitySecurityCheck)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;

                        // find out if the field type is one of the following: Primitive, Enum or Pointer
                        Type fieldType = FieldType;
                        if (fieldType.IsPointer || fieldType.IsEnum || fieldType.IsPrimitive)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_FIELD_SPECIAL_CAST;
                    }

#if FEATURE_APPX
                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
#endif // FEATURE_APPX

                    // must be last to avoid threading problems
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }
        #endregion

        private RuntimeAssembly GetRuntimeAssembly() { return m_declaringType.GetRuntimeAssembly(); }

        #region Constructor
        [System.Security.SecurityCritical]  // auto-generated
        internal RtFieldInfo(
            RuntimeFieldHandleInternal handle, RuntimeType declaringType, RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags) 
            : base(reflectedTypeCache, declaringType, bindingFlags)
        {
            m_fieldHandle = handle.Value;
            m_fieldAttributes = RuntimeFieldHandle.GetAttributes(handle);
        }
        #endregion

        #region Private Members
        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return new RuntimeFieldHandleInternal(m_fieldHandle);
            }
        }

        #endregion

        #region Internal Members
        internal void CheckConsistency(Object target) 
        {
            // only test instance fields
            if ((m_fieldAttributes & FieldAttributes.Static) != FieldAttributes.Static) 
            {
                if (!m_declaringType.IsInstanceOfType(target))
                {
                    if (target == null)
                    {
                        throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatFldReqTarg"));
                    }
                    else
                    {
                        throw new ArgumentException(
                            String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_FieldDeclTarget"),
                                Name, m_declaringType, target.GetType()));
                    }
                }
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal override bool CacheEquals(object o)
        {
            RtFieldInfo m = o as RtFieldInfo;

            if ((object)m == null)
                return false;

            return m.m_fieldHandle == m_fieldHandle;
        }

        [System.Security.SecurityCritical]
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal void InternalSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && declaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenField"));

                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyField"));

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);

            #region Security Check
#if FEATURE_APPX
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }
#endif

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0) 
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)m_invocationFlags);
            #endregion

            bool domainInitialized = false;
            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        // UnsafeSetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform visibility checks they should call
        // InternalSetValue() instead. When the caller needs to perform 
        // consistency checks they should call CheckConsistency() before 
        // calling this method.
        [System.Security.SecurityCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal void UnsafeSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;
            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);

            bool domainInitialized = false;
            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        [System.Security.SecuritySafeCritical]
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object InternalGetValue(Object obj, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && DeclaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenField"));

                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyField"));

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

#if FEATURE_APPX
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }
#endif

            RuntimeType fieldType = (RuntimeType)FieldType;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) != 0)
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)(m_invocationFlags & ~INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD));

            return UnsafeGetValue(obj);
        }

        // UnsafeGetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform visibility checks they should call
        // InternalGetValue() instead. When the caller needs to perform 
        // consistency checks they should call CheckConsistency() before 
        // calling this method.
        [System.Security.SecurityCritical]
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object UnsafeGetValue(Object obj)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;

            RuntimeType fieldType = (RuntimeType)FieldType;

            bool domainInitialized = false;
            if (declaringType == null)
            {
                return RuntimeFieldHandle.GetValue(this, obj, fieldType, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                object retVal = RuntimeFieldHandle.GetValue(this, obj, fieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
                return retVal;
            }               
        } 

        #endregion

        #region MemberInfo Overrides
        public override String Name 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (m_name == null)
                    m_name = RuntimeFieldHandle.GetName(this);

                return m_name;
            }
        }

        internal String FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType.FullName, Name);
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return RuntimeFieldHandle.GetToken(this); }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal override RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(RuntimeFieldHandle.GetApproxDeclaringType(this));
        }

        #endregion

        #region FieldInfo Overrides        
        public override Object GetValue(Object obj)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetValue(obj, ref stackMark);
        } 
        
        public override object GetRawConstantValue() { throw new InvalidOperationException(); }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override Object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(Environment.GetResourceString("Arg_TypedReference_Null"));
            Contract.EndContractBlock();

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType)DeclaringType);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            InternalSetValue(obj, value, invokeAttr, binder, culture, ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, Object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(Environment.GetResourceString("Arg_TypedReference_Null"));
            Contract.EndContractBlock();

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType)DeclaringType);
            }
        }

        public override RuntimeFieldHandle FieldHandle 
        {
            get
            {
                Type declaringType = DeclaringType;
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
                return new RuntimeFieldHandle(this);
            }
        }

        internal IntPtr GetFieldHandle() 
        {
            return m_fieldHandle;
        }

        public override FieldAttributes Attributes 
        {
            get
            {
                return m_fieldAttributes;
            }
        }

        public override Type FieldType 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (m_fieldType == null)
                    m_fieldType = new Signature(this, m_declaringType).FieldType;

                return m_fieldType;
            }
        }       
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type[] GetRequiredCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type[] GetOptionalCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, false);
        }

        #endregion
    }

    [Serializable]
    internal sealed unsafe class MdFieldInfo : RuntimeFieldInfo, ISerializable
    {
        #region Private Data Members
        private int m_tkField;
        private string m_name;
        private RuntimeType m_fieldType;
        private FieldAttributes m_fieldAttributes;
        #endregion

        #region Constructor
        internal MdFieldInfo(
        int tkField, FieldAttributes fieldAttributes, RuntimeTypeHandle declaringTypeHandle, RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
            : base(reflectedTypeCache, declaringTypeHandle.GetRuntimeType(), bindingFlags)
        {
            m_tkField = tkField;
            m_name = null; 
            m_fieldAttributes = fieldAttributes;
        }
        #endregion

        #region Internal Members
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal override bool CacheEquals(object o)
        {
            MdFieldInfo m = o as MdFieldInfo;

            if ((object)m == null)
                return false;

            return m.m_tkField == m_tkField && 
                m_declaringType.GetTypeHandleInternal().GetModuleHandle().Equals(
                    m.m_declaringType.GetTypeHandleInternal().GetModuleHandle());
        }
        #endregion

        #region MemberInfo Overrides
        public override String Name 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (m_name == null)
                    m_name = GetRuntimeModule().MetadataImport.GetName(m_tkField).ToString();

                return m_name;
            }
        }

        public override int MetadataToken { get { return m_tkField; } }
        internal override RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        #endregion

        #region FieldInfo Overrides
        public override RuntimeFieldHandle FieldHandle { get { throw new NotSupportedException(); } }
        public override FieldAttributes Attributes { get { return m_fieldAttributes; } }

        public override bool IsSecurityCritical { get { return DeclaringType.IsSecurityCritical; } }
        public override bool IsSecuritySafeCritical { get { return DeclaringType.IsSecuritySafeCritical; } }
        public override bool IsSecurityTransparent { get { return DeclaringType.IsSecurityTransparent; } }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override Object GetValueDirect(TypedReference obj)
        {
            return GetValue(null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValueDirect(TypedReference obj,Object value)
        {
            throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public unsafe override Object GetValue(Object obj)
        {
            return GetValue(false);
        }

        public unsafe override Object GetRawConstantValue() { return GetValue(true); }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe Object GetValue(bool raw)
        {
            // Cannot cache these because they could be user defined non-agile enumerations

            Object value = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_tkField, FieldType.GetTypeHandleInternal(), raw);

            if (value == DBNull.Value)
                throw new NotSupportedException(Environment.GetResourceString("Arg_EnumLitValueNotFound"));

            return value;
        } 

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
        }

        public override Type FieldType 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (m_fieldType == null)
                {
                    ConstArray fieldMarshal = GetRuntimeModule().MetadataImport.GetSigOfFieldDef(m_tkField);

                    m_fieldType = new Signature(fieldMarshal.Signature.ToPointer(), 
                        (int)fieldMarshal.Length, m_declaringType).FieldType;
                }

                return m_fieldType;
            }
        }       
    
        public override Type[] GetRequiredCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        #endregion
    }

}
