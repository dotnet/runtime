// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    public abstract partial class MethodBase : MemberInfo
    {
        protected MethodBase() { }

        public abstract ParameterInfo[] GetParameters();
        public abstract MethodAttributes Attributes { get; }
        public virtual MethodImplAttributes MethodImplementationFlags => GetMethodImplementationFlags();
        public abstract MethodImplAttributes GetMethodImplementationFlags();

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public virtual MethodBody? GetMethodBody() { throw new InvalidOperationException(); }

        public virtual CallingConventions CallingConvention => CallingConventions.Standard;

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;
        public bool IsConstructor =>
            // To be backward compatible we only return true for instance RTSpecialName ctors.
            this is ConstructorInfo &&
            !IsStatic &&
            (Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;
        public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;
        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;
        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;
        public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public virtual bool IsConstructedGenericMethod => IsGenericMethod && !IsGenericMethodDefinition;
        public virtual bool IsGenericMethod => false;
        public virtual bool IsGenericMethodDefinition => false;
        public virtual Type[] GetGenericArguments() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual bool ContainsGenericParameters => false;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object? Invoke(object? obj, object?[]? parameters) => Invoke(obj, BindingFlags.Default, binder: null, parameters: parameters, culture: null);
        public abstract object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

        public abstract RuntimeMethodHandle MethodHandle { get; }

        public virtual bool IsSecurityCritical => throw NotImplemented.ByDesign;
        public virtual bool IsSecuritySafeCritical => throw NotImplemented.ByDesign;
        public virtual bool IsSecurityTransparent => throw NotImplemented.ByDesign;

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MethodBase? left, MethodBase? right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                return left is null;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return (left is null) ? false : left.Equals(right);
        }

        public static bool operator !=(MethodBase? left, MethodBase? right) => !(left == right);

        internal const int MethodNameBufferSize = 100;

        internal static void AppendParameters(ref ValueStringBuilder sbParamList, Type[] parameterTypes, CallingConventions callingConvention)
        {
            string comma = "";

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Type t = parameterTypes[i];

                sbParamList.Append(comma);

                string typeName = t.FormatTypeName();

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (t.IsByRef)
                {
                    sbParamList.Append(typeName.AsSpan().TrimEnd('&'));
                    sbParamList.Append(" ByRef");
                }
                else
                {
                    sbParamList.Append(typeName);
                }

                comma = ", ";
            }

            if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                sbParamList.Append(comma);
                sbParamList.Append("...");
            }
        }

        internal virtual Type[] GetParameterTypes()
        {
            ParameterInfo[] paramInfo = GetParametersNoCopy();
            if (paramInfo.Length == 0)
            {
                return Type.EmptyTypes;
            }

            Type[] parameterTypes = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
                parameterTypes[i] = paramInfo[i].ParameterType;

            return parameterTypes;
        }

#if !NATIVEAOT
        private protected void ValidateInvokeTarget(object? target)
        {
            // Confirm member invocation has an instance and is of the correct type
            if (!IsStatic)
            {
                if (target == null)
                {
                    throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
                }

                if (!DeclaringType!.IsInstanceOfType(target))
                {
                    throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
                }
            }
        }

        private protected unsafe void CheckArguments(
            Span<object?> copyOfParameters,
            IntPtr* byrefParameters,
            Span<ParameterCopyBackAction> shouldCopyBack,
            ReadOnlySpan<object?> parameters,
            RuntimeType[] sigTypes,
            Binder? binder,
            CultureInfo? culture,
            BindingFlags invokeAttr
        )
        {
            Debug.Assert(parameters.Length > 0);

            ParameterInfo[]? paramInfos = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterCopyBackAction copyBackArg = default;
                bool isValueType = false;
                object? arg = parameters[i];
                RuntimeType sigType = sigTypes[i];

                // Convert a Type.Missing to the default value.
                if (ReferenceEquals(arg, Type.Missing))
                {
                    paramInfos ??= GetParametersNoCopy();
                    ParameterInfo paramInfo = paramInfos[i];

                    if (paramInfo.DefaultValue == DBNull.Value)
                    {
                        throw new ArgumentException(SR.Arg_VarMissNull, nameof(parameters));
                    }

                    arg = paramInfo.DefaultValue;

                    if (sigType.IsNullableOfT)
                    {
                        copyBackArg = ParameterCopyBackAction.CopyNullable;

                        if (arg is not null)
                        {
                            // For nullable Enum types, the ParameterInfo.DefaultValue returns a raw value which
                            // needs to be parsed to the Enum type, for more info: https://github.com/dotnet/runtime/issues/12924
                            Type argumentType = sigType.GetGenericArguments()[0];
                            if (argumentType.IsEnum)
                            {
                                arg = Enum.ToObject(argumentType, arg);
                            }
                        }
                    }
                    else
                    {
                        copyBackArg = ParameterCopyBackAction.Copy;
                    }
                }

                if (arg is null)
                {
                    // Fast path for null reference types.
                    isValueType = RuntimeTypeHandle.IsValueType(sigType);
                    if (isValueType || RuntimeTypeHandle.IsByRef(sigType))
                    {
                        isValueType = sigType.CheckValue(ref arg, ref copyBackArg, binder, culture, invokeAttr);
                    }
                }
                else
                {
                    RuntimeType argType = (RuntimeType)arg.GetType();

                    if (ReferenceEquals(argType, sigType))
                    {
                        // Fast path when the value's type matches the signature type.
                        isValueType = RuntimeTypeHandle.IsValueType(argType);
                    }
                    else if (sigType.TryByRefFastPath(ref arg, ref isValueType))
                    {
                        // Fast path when the value's type matches the signature type of a byref parameter.
                        copyBackArg = ParameterCopyBackAction.Copy;
                    }
                    else
                    {
                        // Slow path that supports type conversions.
                        isValueType = sigType.CheckValue(ref arg, ref copyBackArg, binder, culture, invokeAttr);
                    }
                }

                // We need to perform type safety validation against the incoming arguments, but we also need
                // to be resilient against the possibility that some other thread (or even the binder itself!)
                // may mutate the array after we've validated the arguments but before we've properly invoked
                // the method. The solution is to copy the arguments to a different, not-user-visible buffer
                // as we validate them. n.b. This disallows use of ArrayPool, as ArrayPool-rented arrays are
                // considered user-visible to threads which may still be holding on to returned instances.
                // This separate array is also used to hold default values when 'null' is specified for value
                // types, and also used to hold the results from conversions such as from Int16 to Int32. For
                // compat, these default values and conversions are not be applied to the incoming arguments.
                shouldCopyBack[i] = copyBackArg;
                copyOfParameters[i] = arg;

#pragma warning disable 8500
                if (isValueType)
                {
                    Debug.Assert(arg != null);
                    Debug.Assert(
                        arg.GetType() == sigType ||
                        (sigType.IsPointer && (arg.GetType() == typeof(IntPtr) || arg.GetType() == typeof(UIntPtr))) ||
                        (sigType.IsByRef && arg.GetType() == RuntimeTypeHandle.GetElementType(sigType)) ||
                        ((sigType.IsEnum || arg.GetType().IsEnum) && RuntimeType.GetUnderlyingType((RuntimeType)arg.GetType()) == RuntimeType.GetUnderlyingType(sigType)));
                    ByReference valueTypeRef = ByReference.Create(ref copyOfParameters[i]!.GetRawData());
                    *(ByReference*)(byrefParameters + i) = valueTypeRef;
                }
                else
                {
                    ByReference objRef = ByReference.Create(ref copyOfParameters[i]);
                    *(ByReference*)(byrefParameters + i) = objRef;
                }
#pragma warning restore 8500
            }
        }

        internal const int MaxStackAllocArgCount = 4;

#if CORECLR
        [InlineArray(MaxStackAllocArgCount)]
#endif
        private protected struct ArgumentData<T>
        {
            private T _arg0;
#if !CORECLR
#pragma warning disable CA1823, CS0169, IDE0051, IDE0044 // accessed via 'CheckArguments' ref arithmetic
            private T _arg1;
            private T _arg2;
            private T _arg3;
#pragma warning restore CA1823, CS0169, IDE0051, IDE0044
#endif
            [UnscopedRef]
            public Span<T> AsSpan(int length)
            {
                Debug.Assert((uint)length <= (uint) MaxStackAllocArgCount);
                return new Span<T>(ref _arg0, length);
            }
        }

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // When argument count <= MaxStackAllocArgCount, define a local of type default(StackAllocatedByRefs)
        // and pass it to CheckArguments().
        // For argument count > MaxStackAllocArgCount, do a stackalloc of void* pointers along with
        // GCReportingRegistration to safely track references.
        [StructLayout(LayoutKind.Sequential)]
        private protected ref struct StackAllocedArguments
        {
            internal ArgumentData<object?> _args;
            internal ArgumentData<ParameterCopyBackAction> _copyBacks;
        }

        // Helper struct to avoid intermediate IntPtr[] allocation and RegisterForGCReporting in calls to the native reflection stack.
#if CORECLR
        [InlineArray(MaxStackAllocArgCount)]
#endif
        private protected ref struct StackAllocatedByRefs
        {
            internal ref byte _arg0;
#if !CORECLR
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via 'CheckArguments' ref arithmetic
            private ref byte _arg1;
            private ref byte _arg2;
            private ref byte _arg3;
#pragma warning restore CA1823, CS0169, IDE0051
#endif
        }
#endif
        }
}
