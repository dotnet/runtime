// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed class RuntimeConstructorInfo : ConstructorInfo, IRuntimeMethodInfo
    {
        #region Private Data Members
        private volatile RuntimeType m_declaringType;
        private RuntimeTypeCache m_reflectedTypeCache;
        private string? m_toString;
        private ParameterInfo[]? m_parameters; // Created lazily when GetParameters() is called.
#pragma warning disable CA1823, 414, 169
        private object? _empty1; // These empties are used to ensure that RuntimeConstructorInfo and RuntimeMethodInfo are have a layout which is sufficiently similar
        private object? _empty2;
        private object? _empty3;
#pragma warning restore CA1823, 414, 169
        private IntPtr m_handle;
        private MethodAttributes m_methodAttributes;
        private BindingFlags m_bindingFlags;
        private Signature? m_signature;
        private INVOCATION_FLAGS m_invocationFlags;

        internal INVOCATION_FLAGS InvocationFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                [MethodImpl(MethodImplOptions.NoInlining)] // move lazy invocation flags population out of the hot path
                INVOCATION_FLAGS LazyCreateInvocationFlags()
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR; // this is a given

                    Type? declaringType = DeclaringType;

                    //
                    // first take care of all the NO_INVOKE cases.
                    if (declaringType == typeof(void) ||
                         (declaringType != null && declaringType.ContainsGenericParameters) ||
                         ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs))
                    {
                        // We don't need other flags if this method cannot be invoked
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }
                    else if (IsStatic)
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_RUN_CLASS_CONSTRUCTOR |
                                           INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
                    }
                    else if (declaringType != null && declaringType.IsAbstract)
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
                    }
                    else
                    {
                        // Check for byref-like types
                        if (declaringType != null && declaringType.IsByRefLike)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS;

                        // Check for attempt to create a delegate class.
                        if (typeof(Delegate).IsAssignableFrom(DeclaringType))
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR;
                    }

                    invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                    m_invocationFlags = invocationFlags; // accesses are guaranteed atomic
                    return invocationFlags;
                }

                INVOCATION_FLAGS flags = m_invocationFlags;
                if ((flags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    flags = LazyCreateInvocationFlags();
                }
                return flags;
            }
        }
        #endregion

        #region Constructor
        internal RuntimeConstructorInfo(
            RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeTypeCache reflectedTypeCache,
            MethodAttributes methodAttributes, BindingFlags bindingFlags)
        {
            m_bindingFlags = bindingFlags;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaringType;
            m_handle = handle.Value;
            m_methodAttributes = methodAttributes;
        }
        #endregion

        #region NonPublic Methods
        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => new RuntimeMethodHandleInternal(m_handle);

        internal override bool CacheEquals(object? o) =>
            o is RuntimeConstructorInfo m && m.m_handle == m_handle;

        private Signature Signature
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

        private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

        private void CheckConsistency(object? target)
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

        internal BindingFlags BindingFlags => m_bindingFlags;
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            if (m_toString == null)
            {
                var sbName = new ValueStringBuilder(MethodNameBufferSize);

                // "Void" really doesn't make sense here. But we'll keep it for compat reasons.
                sbName.Append("Void ");

                sbName.Append(Name);

                sbName.Append('(');
                AppendParameters(ref sbName, GetParameterTypes(), CallingConvention);
                sbName.Append(')');

                m_toString = sbName.ToString();
            }

            return m_toString;
        }
        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override string Name => RuntimeMethodHandle.GetName(this);
        public override MemberTypes MemberType => MemberTypes.Constructor;

        public override Type? DeclaringType => m_reflectedTypeCache.IsGlobal ? null : m_declaringType;

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeConstructorInfo>(other);

        public override Type? ReflectedType => m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;

        public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);
        public override Module Module => GetRuntimeModule();

        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal RuntimeModule GetRuntimeModule() { return RuntimeTypeHandle.GetModule(m_declaringType); }
        internal RuntimeAssembly GetRuntimeAssembly() { return GetRuntimeModule().GetRuntimeAssembly(); }
        #endregion

        #region MethodBase Overrides

        // This seems to always returns System.Void.
        internal override Type GetReturnType() { return Signature.ReturnType; }

        internal override ParameterInfo[] GetParametersNoCopy() =>
            m_parameters ??= RuntimeParameterInfo.GetParameters(this, this, Signature);

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

        public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

        public override MethodAttributes Attributes => m_methodAttributes;

        public override CallingConventions CallingConvention => Signature.CallingConvention;

        internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
        {
            if (declaringType == null)
                throw new ArgumentNullException(nameof(declaringType));

            // ctor is declared on interface class
            if (declaringType.IsInterface)
                throw new MemberAccessException(
                    SR.Format(SR.Acc_CreateInterfaceEx, declaringType));

            // ctor is on an abstract class
            else if (declaringType.IsAbstract)
                throw new MemberAccessException(
                    SR.Format(SR.Acc_CreateAbstEx, declaringType));

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
                    SR.Format(SR.Acc_CreateGenericEx, declaringType));
            }

            // ctor is declared on System.Void
            else if (declaringType == typeof(void))
                throw new MemberAccessException(SR.Access_Void);
        }

        [DoesNotReturn]
        internal void ThrowNoInvokeException()
        {
            CheckCanCreateInstance(DeclaringType!, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);

            // ctor is .cctor
            if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                throw new MemberAccessException(SR.Acc_NotClassInit);

            throw new TargetException();
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor",
            Justification = "This ConstructorInfo instance represents the static constructor itself, so if this object was created, the static constructor exists.")]
        public override object? Invoke(
            object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
                ThrowNoInvokeException();

            // check basic method consistency. This call will throw if there are problems in the target/method relationship
            CheckConsistency(obj);

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_RUN_CLASS_CONSTRUCTOR) != 0)
            {
                // Run the class constructor through the class constructor mechanism instead of the Invoke path.
                // This avoids allowing mutation of readonly static fields, and initializes the type correctly.

                var declaringType = DeclaringType;

                if (declaringType != null)
                    RuntimeHelpers.RunClassConstructor(declaringType.TypeHandle);
                else
                    RuntimeHelpers.RunModuleConstructor(Module.ModuleHandle);

                return null;
            }

            Signature sig = Signature;

            // get the signature
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

            // if we are here we passed all the previous checks. Time to look at the arguments
            bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;

            StackAllocedArguments stackArgs = default;
            Span<object?> arguments = default;
            if (actualCount != 0)
            {
                arguments = CheckArguments(ref stackArgs, parameters, binder, invokeAttr, culture, sig);
            }

            object? retValue = RuntimeMethodHandle.InvokeMethod(obj, arguments, sig, false, wrapExceptions);

            // copy out. This should be made only if ByRef are present.
            // n.b. cannot use Span<T>.CopyTo, as parameters.GetType() might not actually be typeof(object[])
            for (int index = 0; index < arguments.Length; index++)
                parameters![index] = arguments[index];

            return retValue;
        }

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public override MethodBody? GetMethodBody()
        {
            RuntimeMethodBody? mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb._methodBase = this;
            return mb;
        }

        public override bool IsSecurityCritical => true;

        public override bool IsSecuritySafeCritical => false;

        public override bool IsSecurityTransparent => false;

        public override bool ContainsGenericParameters => DeclaringType != null && DeclaringType.ContainsGenericParameters;
        #endregion

        #region ConstructorInfo Overrides
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;

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
            bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;

            StackAllocedArguments stackArgs = default;
            Span<object?> arguments = default;
            if (actualCount != 0)
            {
                arguments = CheckArguments(ref stackArgs, parameters, binder, invokeAttr, culture, sig);
            }

            object retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, true, wrapExceptions)!; // ctor must return non-null

            // copy out. This should be made only if ByRef are present.
            // n.b. cannot use Span<T>.CopyTo, as parameters.GetType() might not actually be typeof(object[])
            for (int index = 0; index < arguments.Length; index++)
                parameters![index] = arguments[index];

            return retValue;
        }
        #endregion
    }
}
