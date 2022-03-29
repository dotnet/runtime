// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

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
                // return true/false not the test result https://github.com/dotnet/runtime/issues/4207
                return (left is null) ? true : false;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if ((object?)left == (object)right)
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

#if !CORERT
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

        private protected Span<object?> CheckArguments(ref StackAllocedArguments stackArgs, ReadOnlySpan<object?> parameters, Binder? binder,
            BindingFlags invokeAttr, CultureInfo? culture, RuntimeType[] sigTypes)
        {
            Debug.Assert(Unsafe.SizeOf<StackAllocedArguments>() == StackAllocedArguments.MaxStackAllocArgCount * Unsafe.SizeOf<object>(),
                "MaxStackAllocArgCount not properly defined.");
            Debug.Assert(!parameters.IsEmpty);

            // We need to perform type safety validation against the incoming arguments, but we also need
            // to be resilient against the possibility that some other thread (or even the binder itself!)
            // may mutate the array after we've validated the arguments but before we've properly invoked
            // the method. The solution is to copy the arguments to a different, not-user-visible buffer
            // as we validate them. n.b. This disallows use of ArrayPool, as ArrayPool-rented arrays are
            // considered user-visible to threads which may still be holding on to returned instances.

            Span<object?> copyOfParameters = (parameters.Length <= StackAllocedArguments.MaxStackAllocArgCount)
                    ? MemoryMarshal.CreateSpan(ref stackArgs._arg0, parameters.Length)
                    : new Span<object?>(new object?[parameters.Length]);

            ParameterInfo[]? p = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];
                RuntimeType argRT = sigTypes[i];

                if (arg == Type.Missing)
                {
                    p ??= GetParametersNoCopy();
                    if (p[i].DefaultValue == System.DBNull.Value)
                        throw new ArgumentException(SR.Arg_VarMissNull, nameof(parameters));
                    arg = p[i].DefaultValue!;
                }
                copyOfParameters[i] = argRT.CheckValue(arg, binder, culture, invokeAttr);
            }

            return copyOfParameters;
        }

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // Typical usage is to define a local of type default(StackAllocedArguments), then pass 'ref theLocal'
        // as the first parameter to CheckArguments. CheckArguments will try to utilize storage within this
        // struct instance if there's sufficient space; otherwise CheckArguments will allocate a temp array.
        private protected struct StackAllocedArguments
        {
            internal const int MaxStackAllocArgCount = 4;
            internal object? _arg0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via 'CheckArguments' ref arithmetic
            private object? _arg1;
            private object? _arg2;
            private object? _arg3;
#pragma warning restore CA1823, CS0169, IDE0051
        }
#endif
    }
}
