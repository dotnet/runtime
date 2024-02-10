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
        internal const int MaxStackAllocArgCount = 4;

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
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return left is not null && left.Equals(right);
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
            ReadOnlySpan<ParameterInfo> paramInfo = GetParametersAsSpan();
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
        internal static object? HandleTypeMissing(ParameterInfo paramInfo, RuntimeType sigType)
        {
            if (paramInfo.DefaultValue == DBNull.Value)
            {
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");
            }

            object? arg = paramInfo.DefaultValue;

            if (sigType.IsNullableOfT)
            {
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

            return arg;
        }

        [Flags]
        internal enum InvokerStrategy : int
        {
            HasBeenInvoked_ObjSpanArgs = 0x1,
            StrategyDetermined_ObjSpanArgs = 0x2,

            HasBeenInvoked_Obj4Args = 0x4,
            StrategyDetermined_Obj4Args = 0x8,

            HasBeenInvoked_RefArgs = 0x10,
            StrategyDetermined_RefArgs = 0x20,
        }

        [Flags]
        internal enum InvokerArgFlags : int
        {
            IsValueType = 0x1,
            IsValueType_ByRef_Or_Pointer = 0x2,
            IsNullableOfT = 0x4,
        }

        [InlineArray(MaxStackAllocArgCount)]
        internal struct ArgumentData<T>
        {
            private T _arg0;

            [UnscopedRef]
            public Span<T> AsSpan(int length)
            {
                Debug.Assert((uint)length <= MaxStackAllocArgCount);
                return new Span<T>(ref _arg0, length);
            }

            public void Set(int index, T value)
            {
                Debug.Assert((uint)index < MaxStackAllocArgCount);
                Unsafe.Add(ref _arg0, index) = value;
            }
        }

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // When argument count <= MaxStackAllocArgCount, define a local of these helper structs.
        // For argument count > MaxStackAllocArgCount, do a stackalloc of void* pointers along with
        // GCReportingRegistration to safely track references.
        [StructLayout(LayoutKind.Sequential)]
        internal ref struct StackAllocatedArguments
        {
            public StackAllocatedArguments(object? obj1, object? obj2, object? obj3, object? obj4)
            {
                _args.Set(0, obj1);
                _args.Set(1, obj2);
                _args.Set(2, obj3);
                _args.Set(3, obj4);
            }

            internal ArgumentData<object?> _args;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal ref struct StackAllocatedArgumentsWithCopyBack
        {
            internal ArgumentData<object?> _args;
            internal ArgumentData<bool> _shouldCopyBack;
        }

        // Helper struct to avoid intermediate IntPtr[] allocation and RegisterForGCReporting in calls to the native reflection stack.
        [InlineArray(MaxStackAllocArgCount)]
        internal ref struct StackAllocatedByRefs
        {
            // We're intentionally taking advantage of the runtime functionality, even if the language functionality won't work
            // CS9184: 'Inline arrays' language feature is not supported for inline array types with element field which is either a 'ref' field, or has type that is not valid as a type argument.

#pragma warning disable CS9184
            internal ref byte _arg0;
#pragma warning restore CS9184
        }
#endif
    }
}
