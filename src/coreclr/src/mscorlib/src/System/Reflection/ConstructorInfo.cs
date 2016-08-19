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
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Metadata;
#endif //FEATURE_REMOTING
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using MemberListType = System.RuntimeType.MemberListType;
    using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;
    using System.Runtime.CompilerServices;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_ConstructorInfo))]
#pragma warning disable 618
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
#pragma warning restore 618
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class ConstructorInfo : MethodBase, _ConstructorInfo
    {
        #region Static Members
        [System.Runtime.InteropServices.ComVisible(true)]
        public readonly static String ConstructorName = ".ctor";

        [System.Runtime.InteropServices.ComVisible(true)]
        public readonly static String TypeConstructorName = ".cctor";
        #endregion

        #region Constructor
        protected ConstructorInfo() { }
        #endregion

        public static bool operator ==(ConstructorInfo left, ConstructorInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeConstructorInfo || right is RuntimeConstructorInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(ConstructorInfo left, ConstructorInfo right)
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

        #region Internal Members
        internal virtual Type GetReturnType() { throw new NotImplementedException(); }
        #endregion

        #region MemberInfo Overrides
        [System.Runtime.InteropServices.ComVisible(true)]
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Constructor; } }
        #endregion
    
        #region Public Abstract\Virtual Members
        public abstract Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object Invoke(Object[] parameters)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            return Invoke(BindingFlags.Default, null, parameters, null);
        }
        #endregion

#if !FEATURE_CORECLR
        #region COM Interop Support
        Type _ConstructorInfo.GetType()
        {
            return base.GetType();
        }
        
        Object _ConstructorInfo.Invoke_2(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            return Invoke(obj, invokeAttr, binder, parameters, culture);
        }
        
        Object _ConstructorInfo.Invoke_3(Object obj, Object[] parameters)
        {
            return Invoke(obj, parameters);
        }
        
        Object _ConstructorInfo.Invoke_4(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            return Invoke(invokeAttr, binder, parameters, culture);
        }
        
        Object _ConstructorInfo.Invoke_5(Object[] parameters)
        {
            return Invoke(parameters);
        }

        void _ConstructorInfo.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _ConstructorInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _ConstructorInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _ConstructorInfo.Invoke in VM\DangerousAPIs.h and 
        // include _ConstructorInfo in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _ConstructorInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
        #endregion
#endif
    }

    [Serializable]
    internal sealed class RuntimeConstructorInfo : ConstructorInfo, ISerializable, IRuntimeMethodInfo
    {
        #region Private Data Members
        private volatile RuntimeType m_declaringType;
        private RuntimeTypeCache m_reflectedTypeCache;
        private string m_toString;
        private ParameterInfo[] m_parameters = null; // Created lazily when GetParameters() is called.
#pragma warning disable 169
        private object _empty1; // These empties are used to ensure that RuntimeConstructorInfo and RuntimeMethodInfo are have a layout which is sufficiently similar
        private object _empty2;
        private object _empty3;
#pragma warning restore 169
        private IntPtr m_handle;
        private MethodAttributes m_methodAttributes;
        private BindingFlags m_bindingFlags;
        private volatile Signature m_signature;
        private INVOCATION_FLAGS m_invocationFlags;

#if FEATURE_APPX
        private bool IsNonW8PFrameworkAPI()
        {
            if (DeclaringType.IsArray && IsPublic && !IsStatic)
                return false;

            RuntimeAssembly rtAssembly = GetRuntimeAssembly();
            if (rtAssembly.IsFrameworkAssembly())
            {
                int ctorToken = rtAssembly.InvocableAttributeCtorToken;
                if (System.Reflection.MetadataToken.IsNullToken(ctorToken) || 
                    !CustomAttribute.IsAttributeDefined(GetRuntimeModule(), MetadataToken, ctorToken))
                return true;
            }

            if (GetRuntimeType().IsNonW8PFrameworkAPI())
                return true;

            return false;
        }

        internal override bool IsDynamicallyInvokable
        {
            get
            {
                return !AppDomain.ProfileAPICheck || !IsNonW8PFrameworkAPI();
            }
        }
#endif // FEATURE_APPX

        internal INVOCATION_FLAGS InvocationFlags
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR; // this is a given

                    Type declaringType = DeclaringType;

                    //
                    // first take care of all the NO_INVOKE cases. 
                    if ( declaringType == typeof(void) ||
                         (declaringType != null && declaringType.ContainsGenericParameters) ||
                         ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs) ||
                         ((Attributes & MethodAttributes.RequireSecObject) == MethodAttributes.RequireSecObject))
                    {
                        // We don't need other flags if this method cannot be invoked
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }
                    else if (IsStatic || declaringType != null && declaringType.IsAbstract)
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
                    }
                    else
                    {
                        // this should be an invocable method, determine the other flags that participate in invocation
                        invocationFlags |= RuntimeMethodHandle.GetSecurityFlags(this);

                        if ( (invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) == 0 &&
                             ((Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public ||
                              (declaringType != null && declaringType.NeedsReflectionSecurityCheck)) )
                        {
                            // If method is non-public, or declaring type is not visible
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                        }

                        // Check for attempt to create a delegate class, we demand unmanaged
                        // code permission for this since it's hard to validate the target address.
                        if (typeof(Delegate).IsAssignableFrom(DeclaringType))
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR;
                    }

#if FEATURE_APPX
                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
#endif // FEATURE_APPX

                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }
        #endregion

        #region Constructor
        [System.Security.SecurityCritical]  // auto-generated
        internal RuntimeConstructorInfo(
            RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeTypeCache reflectedTypeCache,
            MethodAttributes methodAttributes, BindingFlags bindingFlags)
        {
            Contract.Ensures(methodAttributes == RuntimeMethodHandle.GetAttributes(handle));

            m_bindingFlags = bindingFlags;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaringType;
            m_handle = handle.Value;
            m_methodAttributes = methodAttributes;
        }
        #endregion

#if FEATURE_REMOTING
        #region Legacy Remoting Cache
        // The size of CachedData is accounted for by BaseObjectWithCachedData in object.h.
        // This member is currently being used by Remoting for caching remoting data. If you
        // need to cache data here, talk to the Remoting team to work out a mechanism, so that
        // both caching systems can happily work together.
        private RemotingMethodCachedData m_cachedData;

        internal RemotingMethodCachedData RemotingCache
        {
            get
            {
                // This grabs an internal copy of m_cachedData and uses
                // that instead of looking at m_cachedData directly because
                // the cache may get cleared asynchronously.  This prevents
                // us from having to take a lock.
                RemotingMethodCachedData cache = m_cachedData;
                if (cache == null)
                {
                    cache = new RemotingMethodCachedData(this);
                    RemotingMethodCachedData ret = Interlocked.CompareExchange(ref m_cachedData, cache, null);
                    if (ret != null)
                        cache = ret;
                }
                return cache;
            }
        }
        #endregion
#endif //FEATURE_REMOTING

        #region NonPublic Methods
        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return new RuntimeMethodHandleInternal(m_handle);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal override bool CacheEquals(object o)
        {
            RuntimeConstructorInfo m = o as RuntimeConstructorInfo;

            if ((object)m == null)
                return false;

            return m.m_handle == m_handle;
        }

        private Signature Signature
        {
            get
            {
                if (m_signature == null)
                    m_signature = new Signature(this, m_declaringType);

                return m_signature;
            }
        }

        private RuntimeType ReflectedTypeInternal
        { 
            get 
            { 
                return m_reflectedTypeCache.GetRuntimeType(); 
            } 
        }

        private void CheckConsistency(Object target) 
        {
            if (target == null && IsStatic)
                return;

            if (!m_declaringType.IsInstanceOfType(target))
            {
                if (target == null) 
                    throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatMethReqTarg"));

                throw new TargetException(Environment.GetResourceString("RFLCT.Targ_ITargMismatch"));
            }
        }

        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }

        // Differs from MethodHandle in that it will return a valid handle even for reflection only loaded types
        internal RuntimeMethodHandle GetMethodHandle()
        {
            return new RuntimeMethodHandle(this);
        }

        internal bool IsOverloaded
        { 
            get 
            { 
                return m_reflectedTypeCache.GetConstructorList(MemberListType.CaseSensitive, Name).Length > 1;
            }
        }
        #endregion

        #region Object Overrides
        public override String ToString() 
        {
            // "Void" really doesn't make sense here. But we'll keep it for compat reasons.
            if (m_toString == null)
                m_toString = "Void " + FormatNameAndSig();

            return m_toString;
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


        #region MemberInfo Overrides
        public override String Name
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return RuntimeMethodHandle.GetName(this); }
        }
[System.Runtime.InteropServices.ComVisible(true)]
        public override MemberTypes MemberType { get { return MemberTypes.Constructor; } }
        
        public override Type DeclaringType 
        { 
            get 
            { 
                return m_reflectedTypeCache.IsGlobal ? null : m_declaringType; 
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return RuntimeMethodHandle.GetMethodDef(this); }
        }
        public override Module Module
        {
            get { return GetRuntimeModule(); }
        }

        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal RuntimeModule GetRuntimeModule() { return RuntimeTypeHandle.GetModule(m_declaringType); }
        internal RuntimeAssembly GetRuntimeAssembly() { return GetRuntimeModule().GetRuntimeAssembly(); }
        #endregion

        #region MethodBase Overrides

        // This seems to always returns System.Void.
        internal override Type GetReturnType() { return Signature.ReturnType; } 
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal override ParameterInfo[] GetParametersNoCopy()
        {
            if (m_parameters == null)
                m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature);

            return m_parameters;
        }

        [Pure]
        public override ParameterInfo[] GetParameters()
        {
            ParameterInfo[] parameters = GetParametersNoCopy();

            if (parameters.Length == 0)
                return parameters;
            
            ParameterInfo[] ret = new ParameterInfo[parameters.Length];
            Array.Copy(parameters, ret, parameters.Length);
            return ret;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return RuntimeMethodHandle.GetImplAttributes(this);
        }

        public override RuntimeMethodHandle MethodHandle 
        {
            get
            {
                Type declaringType = DeclaringType;
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
                return new RuntimeMethodHandle(this);
            }
        }

        public override MethodAttributes Attributes 
        {
            get
            {
                return m_methodAttributes;
            }
        }

        public override CallingConventions CallingConvention 
        {
            get
            {
                return Signature.CallingConvention;
            }
        }

        internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
        {
            if (declaringType == null)
                throw new ArgumentNullException("declaringType");
            Contract.EndContractBlock();
            
            // ctor is ReflectOnly
            if (declaringType is ReflectionOnlyType)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));
            
            // ctor is declared on interface class
            else if (declaringType.IsInterface)
                throw new MemberAccessException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateInterfaceEx"), declaringType));
            
            // ctor is on an abstract class
            else if (declaringType.IsAbstract)
                throw new MemberAccessException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateAbstEx"), declaringType));
            
            // ctor is on a class that contains stack pointers
            else if (declaringType.GetRootElementType() == typeof(ArgIterator))
                throw new NotSupportedException();
            
            // ctor is vararg
            else if (isVarArg)
                throw new NotSupportedException();
                        
            // ctor is generic or on a generic class
            else if (declaringType.ContainsGenericParameters)
            {
                throw new MemberAccessException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateGenericEx"), declaringType));
            }

            // ctor is declared on System.Void
            else if (declaringType == typeof(void))
                throw new MemberAccessException(Environment.GetResourceString("Access_Void"));
        }

        internal void ThrowNoInvokeException()
        {
            CheckCanCreateInstance(DeclaringType, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);

            // ctor is .cctor
            if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                throw new MemberAccessException(Environment.GetResourceString("Acc_NotClassInit"));
            
            throw new TargetException();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Object Invoke(
            Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0) 
                ThrowNoInvokeException();

            // check basic method consistency. This call will throw if there are problems in the target/method relationship
            CheckConsistency(obj);

#if FEATURE_APPX
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0) 
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }
#endif

            if (obj != null)
            {

#if FEATURE_CORECLR
                // For unverifiable code, we require the caller to be critical.
                // Adding the INVOCATION_FLAGS_NEED_SECURITY flag makes that check happen
                invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
#else // FEATURE_CORECLR
                new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
#endif // FEATURE_CORECLR

            }

#if !FEATURE_CORECLR
            if ((invocationFlags &(INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0) 
            {
                if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD) != 0)
                    CodeAccessPermission.Demand(PermissionType.ReflectionMemberAccess);
                if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) != 0)
                    RuntimeMethodHandle.PerformSecurityCheck(obj, this, m_declaringType, (uint)m_invocationFlags);
            }
#endif // !FEATURE_CORECLR

            Signature sig = Signature;

            // get the signature
            int formalCount = sig.Arguments.Length;
            int actualCount =(parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
            
            // if we are here we passed all the previous checks. Time to look at the arguments
            if (actualCount > 0) 
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                Object retValue = RuntimeMethodHandle.InvokeMethod(obj, arguments, sig, false);
                // copy out. This should be made only if ByRef are present.
                for (int index = 0; index < arguments.Length; index++) 
                    parameters[index] = arguments[index];
                return retValue;
            }
            return RuntimeMethodHandle.InvokeMethod(obj, null, sig, false);
        }
        

        [System.Security.SecuritySafeCritical] // overrides SC member
#pragma warning disable 618
        [ReflectionPermissionAttribute(SecurityAction.Demand, Flags = ReflectionPermissionFlag.MemberAccess)]
#pragma warning restore 618
        public override MethodBody GetMethodBody()
        {
            MethodBody mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null) 
                mb.m_methodBase = this;
            return mb;
        }

        public override bool IsSecurityCritical
        {
            get { return RuntimeMethodHandle.IsSecurityCritical(this); }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return RuntimeMethodHandle.IsSecuritySafeCritical(this); }
        }

        public override bool IsSecurityTransparent
        {
            get { return RuntimeMethodHandle.IsSecurityTransparent(this); }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                return (DeclaringType != null && DeclaringType.ContainsGenericParameters);
            }
        }
        #endregion

        #region ConstructorInfo Overrides
        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            // get the declaring TypeHandle early for consistent exceptions in IntrospectionOnly context
            RuntimeTypeHandle declaringTypeHandle = m_declaringType.TypeHandle;
            
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS | INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE)) != 0)
                ThrowNoInvokeException();

#if FEATURE_APPX
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0) 
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }
#endif

#if !FEATURE_CORECLR
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY | INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR)) != 0) 
            {
                if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD) != 0) 
                    CodeAccessPermission.Demand(PermissionType.ReflectionMemberAccess);
                if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)  != 0) 
                    RuntimeMethodHandle.PerformSecurityCheck(null, this, m_declaringType, (uint)(m_invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_CONSTRUCTOR_INVOKE));
                if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR) != 0)
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
#endif // !FEATURE_CORECLR

            // get the signature
            Signature sig = Signature;

            int formalCount = sig.Arguments.Length;
            int actualCount =(parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));

            // We don't need to explicitly invoke the class constructor here,
            // JIT/NGen will insert the call to .cctor in the instance ctor.

            // if we are here we passed all the previous checks. Time to look at the arguments
            if (actualCount > 0) 
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                Object retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, true);
                // copy out. This should be made only if ByRef are present.
                for (int index = 0; index < arguments.Length; index++) 
                    parameters[index] = arguments[index];
                return retValue;
            }
            return RuntimeMethodHandle.InvokeMethod(null, null, sig, true);
        }
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
                SerializationToString(),
                MemberTypes.Constructor,
                null);
        }

        internal string SerializationToString()
        {
            // We don't need the return type for constructors.
            return FormatNameAndSig(true);
        }

        internal void SerializationInvoke(Object target, SerializationInfo info, StreamingContext context)
        {
            RuntimeMethodHandle.SerializationInvoke(this, target, info, ref context);
        }
       #endregion
    }

}
