// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed class RuntimeMethodInfo : MethodInfo, IRuntimeMethodInfo
    {
        #region Private Data Members
        private IntPtr m_handle;
        private RuntimeTypeCache m_reflectedTypeCache;
        private string? m_name;
        private string? m_toString;
        private ParameterInfo[]? m_parameters;
        private ParameterInfo? m_returnParameter;
        private BindingFlags m_bindingFlags;
        private MethodAttributes m_methodAttributes;
        private Signature? m_signature;
        private RuntimeType m_declaringType;
        private object? m_keepalive;
        private INVOCATION_FLAGS m_invocationFlags;

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;

                    Type? declaringType = DeclaringType;

                    //
                    // first take care of all the NO_INVOKE cases. 
                    if (ContainsGenericParameters ||
                         IsDisallowedByRefType(ReturnType) ||
                         (declaringType != null && declaringType.ContainsGenericParameters) ||
                         ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs))
                    {
                        // We don't need other flags if this method cannot be invoked
                        invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }
                    else
                    {
                        // Check for byref-like types
                        if ((declaringType != null && declaringType.IsByRefLike) || ReturnType.IsByRefLike)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS;
                    }

                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }

        private bool IsDisallowedByRefType(Type type)
        {
            if (!type.IsByRef)
                return false;

            Type elementType = type.GetElementType()!;
            return elementType.IsByRefLike || elementType == typeof(void);
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
        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
        {
            get
            {
                return new RuntimeMethodHandleInternal(m_handle);
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        private ParameterInfo[] FetchNonReturnParameters()
        {
            if (m_parameters == null)
                m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature);

            return m_parameters;
        }

        private ParameterInfo FetchReturnParameter()
        {
            if (m_returnParameter == null)
                m_returnParameter = RuntimeParameterInfo.GetReturnParameter(this, this, Signature);

            return m_returnParameter;
        }
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object? o)
        {
            RuntimeMethodInfo? m = o as RuntimeMethodInfo;

            if (m is null)
                return false;

            return m.m_handle == m_handle;
        }

        internal Signature Signature
        {
            get
            {
                if (m_signature == null)
                    m_signature = new Signature(this, m_declaringType);

                return m_signature;
            }
        }

        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }

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

        public override int GetHashCode()
        {
            // See RuntimeMethodInfo.Equals() below.
            if (IsGenericMethod)
                return ValueType.GetHashCodeOfPtr(m_handle);
            else
                return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (!IsGenericMethod)
                return obj == (object)this;

            // We cannot do simple object identity comparisons for generic methods.
            // Equals will be called in CerHashTable when RuntimeType+RuntimeTypeCache.GetGenericMethodInfo()
            // retrieve items from and insert items into s_methodInstantiations which is a CerHashtable.

            RuntimeMethodInfo? mi = obj as RuntimeMethodInfo;

            if (mi == null || !mi.IsGenericMethod)
                return false;

            // now we know that both operands are generic methods

            IRuntimeMethodInfo handle1 = RuntimeMethodHandle.StripMethodInstantiation(this);
            IRuntimeMethodInfo handle2 = RuntimeMethodHandle.StripMethodInstantiation(mi);
            if (handle1.Value.Value != handle2.Value.Value)
                return false;

            Type[] lhs = GetGenericArguments();
            Type[] rhs = mi.GetGenericArguments();

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i] != rhs[i])
                    return false;
            }

            if (DeclaringType != mi.DeclaringType)
                return false;

            if (ReflectedType != mi.ReflectedType)
                return false;

            return true;
        }
        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override string Name
        {
            get
            {
                if (m_name == null)
                    m_name = RuntimeMethodHandle.GetName(this);

                return m_name;
            }
        }

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

        public override MemberTypes MemberType { get { return MemberTypes.Method; } }
        public override int MetadataToken
        {
            get { return RuntimeMethodHandle.GetMethodDef(this); }
        }
        public override Module Module { get { return GetRuntimeModule(); } }
        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        internal RuntimeAssembly GetRuntimeAssembly() { return GetRuntimeModule().GetRuntimeAssembly(); }

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
        #endregion

        #region MethodBase Overrides
        internal override ParameterInfo[] GetParametersNoCopy()
        {
            FetchNonReturnParameters();

            return m_parameters!;
        }

        public override ParameterInfo[] GetParameters()
        {
            FetchNonReturnParameters();

            if (m_parameters!.Length == 0)
                return m_parameters;

            ParameterInfo[] ret = new ParameterInfo[m_parameters.Length];

            Array.Copy(m_parameters, 0, ret, 0, m_parameters.Length);

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
                return new RuntimeMethodHandle(this);
            }
        }

        public override MethodAttributes Attributes { get { return m_methodAttributes; } }

        public override CallingConventions CallingConvention
        {
            get
            {
                return Signature.CallingConvention;
            }
        }

        public override MethodBody? GetMethodBody()
        {
            RuntimeMethodBody? mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb._methodBase = this;
            return mb;
        }
        #endregion

        #region Invocation Logic(On MemberBase)
        private void CheckConsistency(object? target)
        {
            // only test instance methods
            if ((m_methodAttributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                if (!m_declaringType.IsInstanceOfType(target))
                {
                    if (target == null)
                        throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
                    else
                        throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
                }
            }
        }

        [DoesNotReturn]
        private void ThrowNoInvokeException()
        {
            // method is on a class that contains stack pointers
            if ((InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS) != 0)
            {
                throw new NotSupportedException();
            }
            // method is vararg
            else if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                throw new NotSupportedException();
            }
            // method is generic or on a generic class
            else if (DeclaringType!.ContainsGenericParameters || ContainsGenericParameters)
            {
                throw new InvalidOperationException(SR.Arg_UnboundGenParam);
            }
            // method is abstract class
            else if (IsAbstract)
            {
                throw new MemberAccessException();
            }
            else if (ReturnType.IsByRef)
            {
                Type elementType = ReturnType.GetElementType()!;
                if (elementType.IsByRefLike)
                    throw new NotSupportedException(SR.NotSupported_ByRefToByRefLikeReturn);    
                if (elementType == typeof(void))
                    throw new NotSupportedException(SR.NotSupported_ByRefToVoidReturn);
            }

            throw new TargetException();
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            object[]? arguments = InvokeArgumentsCheck(obj, invokeAttr, binder, parameters, culture);

            bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            if (arguments == null || arguments.Length == 0)
                return RuntimeMethodHandle.InvokeMethod(obj, null, Signature, false, wrapExceptions);
            else
            {
                object retValue = RuntimeMethodHandle.InvokeMethod(obj, arguments, Signature, false, wrapExceptions);

                // copy out. This should be made only if ByRef are present.
                for (int index = 0; index < arguments.Length; index++)
                    parameters![index] = arguments[index];

                return retValue;
            }
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        private object[]? InvokeArgumentsCheck(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            Signature sig = Signature;

            // get the signature 
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;

            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            // INVOCATION_FLAGS_CONTAINS_STACK_POINTERS means that the struct (either the declaring type or the return type)
            // contains pointers that point to the stack. This is either a ByRef or a TypedReference. These structs cannot
            // be boxed and thus cannot be invoked through reflection which only deals with boxed value type objects.
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != 0)
                ThrowNoInvokeException();

            // check basic method consistency. This call will throw if there are problems in the target/method relationship
            CheckConsistency(obj);

            if (formalCount != actualCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

            if (actualCount != 0)
                return CheckArguments(parameters!, binder, invokeAttr, culture, sig);
            else
                return null;
        }

        #endregion

        #region MethodInfo Overrides

#pragma warning disable CS8608 // TODO-NULLABLE: Covariant return types (https://github.com/dotnet/roslyn/issues/23268)
        public override Type ReturnType
        {
            get { return Signature.ReturnType; }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return ReturnParameter; }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                FetchReturnParameter();
                return (m_returnParameter as ParameterInfo)!;
            }
        }
#pragma warning restore CS8608

        public override bool IsCollectible => (RuntimeMethodHandle.GetIsCollectible(new RuntimeMethodHandleInternal(m_handle)) != Interop.BOOL.FALSE);

        public override MethodInfo? GetBaseDefinition()
        {
            if (!IsVirtual || IsStatic || m_declaringType == null || m_declaringType.IsInterface)
                return this;

            int slot = RuntimeMethodHandle.GetSlot(this);
            RuntimeType declaringType = (RuntimeType)DeclaringType!;
            RuntimeType? baseDeclaringType = declaringType;
            RuntimeMethodHandleInternal baseMethodHandle = new RuntimeMethodHandleInternal();

            do
            {
                int cVtblSlots = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                if (cVtblSlots <= slot)
                    break;

                baseMethodHandle = RuntimeTypeHandle.GetMethodAt(declaringType, slot);
                baseDeclaringType = declaringType;

                declaringType = (RuntimeType)declaringType.BaseType!;
            } while (declaringType != null);

            return (MethodInfo?)RuntimeType.GetMethodBase(baseDeclaringType, baseMethodHandle);
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
            // Validate the parameters.
            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

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
        public override MethodInfo? MakeGenericMethod(params Type[] methodInstantiation)
        {
            if (methodInstantiation == null)
                throw new ArgumentNullException(nameof(methodInstantiation));

            RuntimeType[] methodInstantionRuntimeType = new RuntimeType[methodInstantiation.Length];

            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(
                    SR.Format(SR.Arg_NotGenericMethodDefinition, this));

            for (int i = 0; i < methodInstantiation.Length; i++)
            {
                Type methodInstantiationElem = methodInstantiation[i];

                if (methodInstantiationElem == null)
                    throw new ArgumentNullException();

                RuntimeType? rtMethodInstantiationElem = methodInstantiationElem as RuntimeType;

                if (rtMethodInstantiationElem == null)
                {
                    Type[] methodInstantiationCopy = new Type[methodInstantiation.Length];
                    for (int iCopy = 0; iCopy < methodInstantiation.Length; iCopy++)
                        methodInstantiationCopy[iCopy] = methodInstantiation[iCopy];
                    methodInstantiation = methodInstantiationCopy;
                    return System.Reflection.Emit.MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
                }

                methodInstantionRuntimeType[i] = rtMethodInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();

            RuntimeType.SanityCheckGenericArguments(methodInstantionRuntimeType, genericParameters);

            MethodInfo? ret = null;

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

            return ret;
        }

        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return RuntimeMethodHandle.GetMethodInstantiationInternal(this);
        }

        public override Type[] GetGenericArguments()
        {
            Type[] types = RuntimeMethodHandle.GetMethodInstantiationPublic(this);

            if (types == null)
            {
                types = Array.Empty<Type>();
            }
            return types;
        }

        public override MethodInfo? GetGenericMethodDefinition()
        {
            if (!IsGenericMethod)
                throw new InvalidOperationException();

            return RuntimeType.GetMethodBase(m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo;
        }

        public override bool IsGenericMethod
        {
            get { return RuntimeMethodHandle.HasMethodInstantiation(this); }
        }

        public override bool IsGenericMethodDefinition
        {
            get { return RuntimeMethodHandle.IsGenericMethodDefinition(this); }
        }

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
