// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed partial class RuntimeMethodInfo : MethodInfo, IRuntimeMethodInfo
    {
        #region Private Data Members
        private readonly IntPtr m_handle;
        private readonly RuntimeTypeCache m_reflectedTypeCache;
        private string? m_name;
        private string? m_toString;
        private ParameterInfo[]? m_parameters;
        private ParameterInfo? m_returnParameter;
        private readonly BindingFlags m_bindingFlags;
        private readonly MethodAttributes m_methodAttributes;
        private Signature? m_signature;
        private readonly RuntimeType m_declaringType;
        private readonly object? m_keepalive;
        private MethodBaseInvoker? m_invoker;

        internal InvocationFlags InvocationFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                InvocationFlags flags = Invoker._invocationFlags;
                Debug.Assert((flags & InvocationFlags.Initialized) == InvocationFlags.Initialized);
                return flags;
            }
        }

        private MethodBaseInvoker Invoker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                m_invoker ??= new MethodBaseInvoker(this);
                return m_invoker;
            }
        }
        #endregion

        #region Constructor
        internal RuntimeMethodInfo(
            RuntimeMethodHandleInternal handle, RuntimeType declaringType,
            RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags, object? keepalive)
        {
            Debug.Assert(!handle.IsNullHandle());
            Debug.Assert(methodAttributes == RuntimeMethodHandle.GetAttributes(handle));

            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_keepalive = keepalive;
            m_handle = handle.Value;
            m_reflectedTypeCache = reflectedTypeCache;
            m_methodAttributes = methodAttributes;
        }
        #endregion

        #region Private Methods
        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => new RuntimeMethodHandleInternal(m_handle);

        private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

        private ParameterInfo[] FetchNonReturnParameters() =>
            m_parameters ??= RuntimeParameterInfo.GetParameters(this, this, Signature);

        private ParameterInfo FetchReturnParameter() =>
            m_returnParameter ??= RuntimeParameterInfo.GetReturnParameter(this, this, Signature);
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object? o) =>
            o is RuntimeMethodInfo m && m.m_handle == m_handle;

        internal Signature Signature
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                [MethodImpl(MethodImplOptions.NoInlining)] // move lazy sig generation out of the hot path
                Signature LazyCreateSignature()
                {
                    Signature newSig = new Signature(this, m_declaringType);
                    Volatile.Write(ref m_signature, newSig);
                    return newSig;
                }

                return m_signature ?? LazyCreateSignature();
            }
        }

        internal BindingFlags BindingFlags => m_bindingFlags;

        internal RuntimeMethodInfo? GetParentDefinition()
        {
            if (!IsVirtual || m_declaringType.IsInterface)
                return null;

            RuntimeType? parent = (RuntimeType?)m_declaringType.BaseType;

            if (parent == null)
                return null;

            int slot = RuntimeMethodHandle.GetSlot(this);

            if (RuntimeTypeHandle.GetNumVirtuals(parent) <= slot)
                return null;

            return (RuntimeMethodInfo?)RuntimeType.GetMethodBase(parent, RuntimeTypeHandle.GetMethodAt(parent, slot));
        }

        // Unlike DeclaringType, this will return a valid type even for global methods
        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        internal sealed override int GenericParameterCount => RuntimeMethodHandle.GetGenericParameterCount(this);
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            if (m_toString == null)
            {
                var sbName = new ValueStringBuilder(MethodNameBufferSize);

                sbName.Append(ReturnType.FormatTypeName());
                sbName.Append(' ');
                sbName.Append(Name);

                if (IsGenericMethod)
                    sbName.Append(RuntimeMethodHandle.ConstructInstantiation(this, TypeNameFormatFlags.FormatBasic));

                sbName.Append('(');
                AppendParameters(ref sbName, GetParameterTypes(), CallingConvention);
                sbName.Append(')');

                m_toString = sbName.ToString();
            }

            return m_toString;
        }

        // We cannot do simple object identity comparisons due to generic methods.
        // Equals and GetHashCode will be called in CerHashTable when RuntimeType+RuntimeTypeCache.GetGenericMethodInfo()
        // retrieve items from and insert items into s_methodInstantiations.

        public override int GetHashCode() =>
            HashCode.Combine(m_handle.GetHashCode(), m_declaringType.GetUnderlyingNativeHandle().GetHashCode());

        public override bool Equals(object? obj) =>
            obj is RuntimeMethodInfo m && m_handle == m.m_handle &&
            ReferenceEquals(m_declaringType, m.m_declaringType) &&
            ReferenceEquals(m_reflectedTypeCache.GetRuntimeType(), m.m_reflectedTypeCache.GetRuntimeType());

        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override string Name => m_name ??= RuntimeMethodHandle.GetName(this);

        public override Type? DeclaringType
        {
            get
            {
                if (m_reflectedTypeCache.IsGlobal)
                    return null;

                return m_declaringType;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeMethodInfo>(other);

        public override Type? ReflectedType
        {
            get
            {
                if (m_reflectedTypeCache.IsGlobal)
                    return null;

                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        public override MemberTypes MemberType => MemberTypes.Method;
        public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);
        public override Module Module => GetRuntimeModule();
        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        internal RuntimeAssembly GetRuntimeAssembly() { return GetRuntimeModule().GetRuntimeAssembly(); }

        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;
        #endregion

        #region MethodBase Overrides
        internal override ParameterInfo[] GetParametersNoCopy() =>
            FetchNonReturnParameters();

        public override ParameterInfo[] GetParameters()
        {
            ParameterInfo[] parameters = FetchNonReturnParameters();

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

        public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

        public override MethodAttributes Attributes => m_methodAttributes;

        public override CallingConventions CallingConvention => Signature.CallingConvention;

        internal RuntimeType[] ArgumentTypes => Signature.Arguments;

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public override MethodBody? GetMethodBody()
        {
            RuntimeMethodBody? mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb._methodBase = this;
            return mb;
        }

        #endregion

        #region Invocation Logic
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal void InvokePropertySetter(object? obj, BindingFlags invokeAttr, Binder? binder, object? parameter, CultureInfo? culture)
        {
            // ContainsStackPointers means that the struct (either the declaring type or the return type)
            // contains pointers that point to the stack. This is either a ByRef or a TypedReference. These structs cannot
            // be boxed and thus cannot be invoked through reflection which only deals with boxed value type objects.
            if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
            {
                ThrowNoInvokeException();
            }

            // check basic method consistency. This call will throw if there are problems in the target/method relationship
            if (!IsStatic)
            {
                MethodInvokerCommon.ValidateInvokeTarget(obj, this);
            }

            Signature sig = Signature;
            if (sig.Arguments.Length != 1)
            {
                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            Invoker.InvokePropertySetter(obj, invokeAttr, binder, parameter, culture);
        }

        #endregion

        #region MethodInfo Overrides

        public override Type ReturnType => Signature.ReturnType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;

        public override ParameterInfo ReturnParameter => FetchReturnParameter();

        public override bool IsCollectible => RuntimeMethodHandle.GetIsCollectible(new RuntimeMethodHandleInternal(m_handle)) != Interop.BOOL.FALSE;

        public override MethodInfo GetBaseDefinition()
        {
            if (!IsVirtual || IsStatic || m_declaringType == null || m_declaringType.IsInterface)
                return this;

            int slot = RuntimeMethodHandle.GetSlot(this);
            RuntimeType declaringType = (RuntimeType)DeclaringType!;
            RuntimeType? baseDeclaringType = declaringType;
            RuntimeMethodHandleInternal baseMethodHandle = default;

            do
            {
                int cVtblSlots = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                if (cVtblSlots <= slot)
                    break;

                baseMethodHandle = RuntimeTypeHandle.GetMethodAt(declaringType, slot);
                baseDeclaringType = declaringType;

                declaringType = (RuntimeType)declaringType.BaseType!;
            } while (declaringType != null);

            return (MethodInfo)RuntimeType.GetMethodBase(baseDeclaringType, baseMethodHandle)!;
        }

        public override Delegate CreateDelegate(Type delegateType)
        {
            // This API existed in v1/v1.1 and only expected to create closed
            // instance delegates. Constrain the call to BindToMethodInfo to
            // open delegates only for backwards compatibility. But we'll allow
            // relaxed signature checking and open static delegates because
            // there's no ambiguity there (the caller would have to explicitly
            // pass us a static method or a method with a non-exact signature
            // and the only change in behavior from v1.1 there is that we won't
            // fail the call).
            return CreateDelegateInternal(
                delegateType,
                null,
                DelegateBindingFlags.OpenDelegateOnly | DelegateBindingFlags.RelaxedSignature);
        }

        public override Delegate CreateDelegate(Type delegateType, object? target)
        {
            // This API is new in Whidbey and allows the full range of delegate
            // flexability (open or closed delegates binding to static or
            // instance methods with relaxed signature checking). The delegate
            // can also be closed over null. There's no ambiguity with all these
            // options since the caller is providing us a specific MethodInfo.
            return CreateDelegateInternal(
                delegateType,
                target,
                DelegateBindingFlags.RelaxedSignature);
        }

        private Delegate CreateDelegateInternal(Type delegateType, object? firstArgument, DelegateBindingFlags bindingFlags)
        {
            ArgumentNullException.ThrowIfNull(delegateType);

            RuntimeType? rtType = delegateType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(delegateType));

            if (!rtType.IsDelegate())
                throw new ArgumentException(SR.Arg_MustBeDelegate, nameof(delegateType));

            Delegate? d = Delegate.CreateDelegateInternal(rtType, this, firstArgument, bindingFlags);
            if (d == null)
            {
                throw new ArgumentException(SR.Arg_DlgtTargMeth);
            }

            return d;
        }

        #endregion

        #region Generics
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
        {
            ArgumentNullException.ThrowIfNull(methodInstantiation);

            RuntimeType[] methodInstantionRuntimeType = new RuntimeType[methodInstantiation.Length];

            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(
                    SR.Format(SR.Arg_NotGenericMethodDefinition, this));

            for (int i = 0; i < methodInstantiation.Length; i++)
            {
                Type methodInstantiationElem = methodInstantiation[i];
                ArgumentNullException.ThrowIfNull(methodInstantiationElem, null);

                RuntimeType? rtMethodInstantiationElem = methodInstantiationElem as RuntimeType;

                if (rtMethodInstantiationElem == null)
                {
                    Type[] methodInstantiationCopy = new Type[methodInstantiation.Length];
                    for (int iCopy = 0; iCopy < methodInstantiation.Length; iCopy++)
                        methodInstantiationCopy[iCopy] = methodInstantiation[iCopy];
                    methodInstantiation = methodInstantiationCopy;
                    return Emit.MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
                }

                methodInstantionRuntimeType[i] = rtMethodInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();

            RuntimeType.SanityCheckGenericArguments(methodInstantionRuntimeType, genericParameters);

            MethodInfo? ret;

            try
            {
                ret = RuntimeType.GetMethodBase(ReflectedTypeInternal,
                    RuntimeMethodHandle.GetStubIfNeeded(new RuntimeMethodHandleInternal(m_handle), m_declaringType, methodInstantionRuntimeType)) as MethodInfo;
            }
            catch (VerificationException e)
            {
                RuntimeType.ValidateGenericArguments(this, methodInstantionRuntimeType, e);
                throw;
            }

            return ret!;
        }

        internal RuntimeType[] GetGenericArgumentsInternal() =>
            RuntimeMethodHandle.GetMethodInstantiationInternal(this);

        public override Type[] GetGenericArguments() =>
            RuntimeMethodHandle.GetMethodInstantiationPublic(this) ?? Type.EmptyTypes;

        public override MethodInfo GetGenericMethodDefinition()
        {
            if (!IsGenericMethod)
                throw new InvalidOperationException();

            return (RuntimeType.GetMethodBase(m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo)!;
        }

        public override bool IsGenericMethod => RuntimeMethodHandle.HasMethodInstantiation(this);

        public override bool IsGenericMethodDefinition => RuntimeMethodHandle.IsGenericMethodDefinition(this);

        public override bool ContainsGenericParameters
        {
            get
            {
                if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
                    return true;

                if (!IsGenericMethod)
                    return false;

                Type[] pis = GetGenericArguments();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (pis[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }
        #endregion

        #region Legacy Internal
        internal static MethodBase? InternalGetCurrentMethod(ref StackCrawlMark stackMark)
        {
            IRuntimeMethodInfo? method = RuntimeMethodHandle.GetCurrentMethod(ref stackMark);

            if (method == null)
                return null;

            return RuntimeType.GetMethodBase(method);
        }
        #endregion
    }
}
