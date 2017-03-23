// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.Serialization;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
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

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR; // this is a given

                    Type declaringType = DeclaringType;

                    //
                    // first take care of all the NO_INVOKE cases. 
                    if (declaringType == typeof(void) ||
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

                        if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) == 0 &&
                             ((Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public ||
                              (declaringType != null && declaringType.NeedsReflectionSecurityCheck)))
                        {
                            // If method is non-public, or declaring type is not visible
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                        }

                        // Check for attempt to create a delegate class, we demand unmanaged
                        // code permission for this since it's hard to validate the target address.
                        if (typeof(Delegate).IsAssignableFrom(DeclaringType))
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR;
                    }

                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }
        #endregion

        #region Constructor
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

        #region NonPublic Methods
        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
        {
            get
            {
                return new RuntimeMethodHandleInternal(m_handle);
            }
        }

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
                    throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);

                throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
            }
        }

        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }
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
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

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


        #region MemberInfo Overrides
        public override String Name
        {
            get { return RuntimeMethodHandle.GetName(this); }
        }
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
                    throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInReflectionOnly);
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
                throw new ArgumentNullException(nameof(declaringType));
            Contract.EndContractBlock();

            // ctor is ReflectOnly
            if (declaringType is ReflectionOnlyType)
                throw new InvalidOperationException(SR.Arg_ReflectionOnlyInvoke);

            // ctor is declared on interface class
            else if (declaringType.IsInterface)
                throw new MemberAccessException(
                    String.Format(CultureInfo.CurrentUICulture, SR.Acc_CreateInterfaceEx, declaringType));

            // ctor is on an abstract class
            else if (declaringType.IsAbstract)
                throw new MemberAccessException(
                    String.Format(CultureInfo.CurrentUICulture, SR.Acc_CreateAbstEx, declaringType));

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
                    String.Format(CultureInfo.CurrentUICulture, SR.Acc_CreateGenericEx, declaringType));
            }

            // ctor is declared on System.Void
            else if (declaringType == typeof(void))
                throw new MemberAccessException(SR.Access_Void);
        }

        internal void ThrowNoInvokeException()
        {
            CheckCanCreateInstance(DeclaringType, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);

            // ctor is .cctor
            if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                throw new MemberAccessException(SR.Acc_NotClassInit);

            throw new TargetException();
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Object Invoke(
            Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
                ThrowNoInvokeException();

            // check basic method consistency. This call will throw if there are problems in the target/method relationship
            CheckConsistency(obj);

            if (obj != null)
            {
                // For unverifiable code, we require the caller to be critical.
                // Adding the INVOCATION_FLAGS_NEED_SECURITY flag makes that check happen
                invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
            }

            Signature sig = Signature;

            // get the signature
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

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

        public override MethodBody GetMethodBody()
        {
            MethodBody mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb.m_methodBase = this;
            return mb;
        }

        public override bool IsSecurityCritical
        {
            get { return true; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return false; }
        }

        public override bool IsSecurityTransparent
        {
            get { return false; }
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
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            // get the declaring TypeHandle early for consistent exceptions in IntrospectionOnly context
            RuntimeTypeHandle declaringTypeHandle = m_declaringType.TypeHandle;

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS | INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE)) != 0)
                ThrowNoInvokeException();

            // get the signature
            Signature sig = Signature;

            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

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
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();
            MemberInfoSerializationHolder.GetSerializationInfo(info, this);
        }

        internal string SerializationToString()
        {
            // We don't need the return type for constructors.
            return FormatNameAndSig(true);
        }
        #endregion
    }
}
