// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.CompilerServices
{
    public static class FunctionPointerOps
    {
#if TARGET_WASM
        private const int FatFunctionPointerOffset = 1 << 31;
#else
        private const int FatFunctionPointerOffset = 2;
#endif

        private struct GenericMethodDescriptorInfo : IEquatable<GenericMethodDescriptorInfo>
        {
            public override bool Equals(object? obj)
            {
                if (!(obj is GenericMethodDescriptorInfo))
                    return false;

                return Equals((GenericMethodDescriptorInfo)obj);
            }

            public bool Equals(GenericMethodDescriptorInfo other)
            {
                if (MethodFunctionPointer != other.MethodFunctionPointer)
                    return false;

                if (InstantiationArgument != other.InstantiationArgument)
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                int a = InstantiationArgument.GetHashCode();
                int b = MethodFunctionPointer.GetHashCode();
                return (a ^ b) + (a << 11) - (b >> 13);
            }

            public IntPtr MethodFunctionPointer;
            public IntPtr InstantiationArgument;
        }

        private static LowLevelDictionary<GenericMethodDescriptorInfo, IntPtr> s_genericFunctionPointerDictionary = new LowLevelDictionary<GenericMethodDescriptorInfo, IntPtr>();

        public static unsafe IntPtr GetGenericMethodFunctionPointer(IntPtr canonFunctionPointer, IntPtr instantiationArgument)
        {
            Debug.Assert(canonFunctionPointer != IntPtr.Zero);

            if (instantiationArgument == IntPtr.Zero)
                return canonFunctionPointer;

            lock (s_genericFunctionPointerDictionary)
            {
                var key = new GenericMethodDescriptorInfo
                {
                    MethodFunctionPointer = canonFunctionPointer,
                    InstantiationArgument = instantiationArgument
                };

                if (!s_genericFunctionPointerDictionary.TryGetValue(key, out IntPtr descriptor))
                {
                    descriptor = (IntPtr)NativeMemory.Alloc((uint)sizeof(GenericMethodDescriptor));

                    *(GenericMethodDescriptor*)descriptor =
                        new GenericMethodDescriptor(canonFunctionPointer, instantiationArgument);

                    s_genericFunctionPointerDictionary.LookupOrAdd(key, descriptor);
                }

                GenericMethodDescriptor* genericFunctionPointer = (GenericMethodDescriptor*)descriptor;

                Debug.Assert(canonFunctionPointer == genericFunctionPointer->MethodFunctionPointer);
                Debug.Assert(instantiationArgument == genericFunctionPointer->InstantiationArgument);

                return (IntPtr)((byte*)genericFunctionPointer + FatFunctionPointerOffset);
            }
        }

        public static unsafe bool IsGenericMethodPointer(IntPtr functionPointer)
        {
            // Check the low bit to find out what kind of function pointer we have here.
            if ((functionPointer & FatFunctionPointerOffset) == FatFunctionPointerOffset)
            {
                return true;
            }
            return false;
        }

        [CLSCompliant(false)]
        public static unsafe GenericMethodDescriptor* ConvertToGenericDescriptor(IntPtr functionPointer)
        {
            return (GenericMethodDescriptor*)((byte*)functionPointer - FatFunctionPointerOffset);
        }

        public static unsafe bool Compare(IntPtr functionPointerA, IntPtr functionPointerB)
        {
            if (!IsGenericMethodPointer(functionPointerA))
            {
                return functionPointerA == functionPointerB;
            }

            if (!IsGenericMethodPointer(functionPointerB))
            {
                return false;
            }

            GenericMethodDescriptor* pointerDefA = ConvertToGenericDescriptor(functionPointerA);
            GenericMethodDescriptor* pointerDefB = ConvertToGenericDescriptor(functionPointerB);

            if (pointerDefA->InstantiationArgument != pointerDefB->InstantiationArgument)
            {
                return false;
            }

            return pointerDefA->MethodFunctionPointer == pointerDefB->MethodFunctionPointer;
        }

        public static unsafe int GetHashCode(IntPtr functionPointer)
        {
            if (!IsGenericMethodPointer(functionPointer))
            {
                return functionPointer.GetHashCode();
            }

            GenericMethodDescriptor* pointerDef = ConvertToGenericDescriptor(functionPointer);
            return pointerDef->GetHashCode();
        }
    }
}
