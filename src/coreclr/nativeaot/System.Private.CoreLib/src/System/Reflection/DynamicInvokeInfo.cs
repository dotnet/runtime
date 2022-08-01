// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    [Flags]
    internal enum DynamicInvokeTransform
    {
        ByRef               = 0x0001,
        Nullable            = 0x0002,
        Pointer             = 0x0004,
        Reference           = 0x0008,
        AllocateReturnBox   = 0x0010,
    }

    internal readonly struct ArgumentInfo
    {
        internal ArgumentInfo(DynamicInvokeTransform transform, EETypePtr type)
        {
            Transform = transform;
            Type = type;
        }

        internal DynamicInvokeTransform Transform { get; }
        internal EETypePtr Type { get; }
    }

    // DynamicInvokeInfo caches information required for efficient argument validation and type coercion for reflection Invoke.
    [ReflectionBlocked]
    public class DynamicInvokeInfo
    {
        // We use negative argument count to signal unsupported invoke signatures
        public const int ArgumentCount_NotSupported = -1;
        public const int ArgumentCount_NotSupported_ByRefLike = -2;

        public DynamicInvokeInfo(MethodBase method, IntPtr invokeThunk)
        {
            Method = method;
            InvokeThunk = invokeThunk;

            IsStatic = method.IsStatic;

            // IsValueTypeInstanceMethod = method.DeclaringType?.IsValueType ?? false;

            ParameterInfo[] parameters = method.GetParametersNoCopy();

            ArgumentCount = parameters.Length;

            if (ArgumentCount != 0)
            {
                ArgumentInfo[] arguments = new ArgumentInfo[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    DynamicInvokeTransform transform = default;

                    Type argumentType = parameters[i].ParameterType;
                    if (argumentType.IsByRef)
                    {
                        NeedsCopyBack = true;
                        transform |= DynamicInvokeTransform.ByRef;
                        argumentType = argumentType.GetElementType()!;
                    }
                    Debug.Assert(!argumentType.IsByRef);

                    EETypePtr eeArgumentType = argumentType.GetEEType();

                    if (eeArgumentType.IsValueType)
                    {
                        Debug.Assert(argumentType.IsValueType);

                        if (eeArgumentType.IsByRefLike)
                            ArgumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if (eeArgumentType.IsNullable)
                            transform |= DynamicInvokeTransform.Nullable;
                    }
                    else if (eeArgumentType.IsPointer)
                    {
                        Debug.Assert(argumentType.IsPointer);

                        transform |= DynamicInvokeTransform.Pointer;
                    }
                    else
                    {
                        transform |= DynamicInvokeTransform.Reference;
                    }

                    arguments[i] = new ArgumentInfo(transform, eeArgumentType);
                }
                Arguments = arguments;
            }

            if (method is MethodInfo methodInfo)
            {
                DynamicInvokeTransform transform = default;

                Type returnType = methodInfo.ReturnType;
                if (returnType.IsByRef)
                {
                    transform |= DynamicInvokeTransform.ByRef;
                    returnType = returnType.GetElementType()!;
                }
                Debug.Assert(!returnType.IsByRef);

                EETypePtr eeReturnType = returnType.GetEEType();

                if (eeReturnType.IsValueType)
                {
                    Debug.Assert(returnType.IsValueType);

                    if (returnType != typeof(void))
                    {
                        if (eeReturnType.IsByRefLike)
                            ArgumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if ((transform & DynamicInvokeTransform.ByRef) == 0)
                            transform |= DynamicInvokeTransform.AllocateReturnBox;

                        if (eeReturnType.IsNullable)
                            transform |= DynamicInvokeTransform.Nullable;
                    }
                    else
                    {
                        if ((transform & DynamicInvokeTransform.ByRef) != 0)
                            ArgumentCount = ArgumentCount_NotSupported; // ByRef to void return
                    }
                }
                else if (eeReturnType.IsPointer)
                {
                    Debug.Assert(returnType.IsPointer);

                    transform |= DynamicInvokeTransform.Pointer;
                    if ((transform & DynamicInvokeTransform.ByRef) == 0)
                        transform |= DynamicInvokeTransform.AllocateReturnBox;
                }
                else
                {
                    transform |= DynamicInvokeTransform.Reference;
                }

                ReturnTransform = transform;
                ReturnType = eeReturnType;
            }
        }

        public MethodBase Method { get; }
        public IntPtr InvokeThunk { get; }

        internal int ArgumentCount { get; }
        internal bool IsStatic { get; }
        // internal bool IsValueTypeInstanceMethod { get; }
        internal bool NeedsCopyBack { get; }
        internal DynamicInvokeTransform ReturnTransform { get; }
        internal EETypePtr ReturnType { get; }
        internal ArgumentInfo[] Arguments { get; }
    }
}
