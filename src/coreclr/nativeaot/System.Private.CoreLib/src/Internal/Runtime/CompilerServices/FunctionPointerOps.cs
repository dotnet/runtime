// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerServices
{
    [System.Runtime.CompilerServices.ReflectionBlocked]
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

        private static uint s_genericFunctionPointerNextIndex;
        private const uint c_genericDictionaryChunkSize = 1024;
        private static LowLevelList<IntPtr> s_genericFunctionPointerCollection = new LowLevelList<IntPtr>();
        private static LowLevelDictionary<GenericMethodDescriptorInfo, uint> s_genericFunctionPointerDictionary = new LowLevelDictionary<GenericMethodDescriptorInfo, uint>();

        public static unsafe IntPtr GetGenericMethodFunctionPointer(IntPtr canonFunctionPointer, IntPtr instantiationArgument)
        {
            if (instantiationArgument == IntPtr.Zero)
                return canonFunctionPointer;

            lock (s_genericFunctionPointerDictionary)
            {
                var key = new GenericMethodDescriptorInfo
                {
                    MethodFunctionPointer = canonFunctionPointer,
                    InstantiationArgument = instantiationArgument
                };

                uint index = 0;
                if (!s_genericFunctionPointerDictionary.TryGetValue(key, out index))
                {
                    // Capture new index value
                    index = s_genericFunctionPointerNextIndex;

                    int newChunkIndex = (int)(index / c_genericDictionaryChunkSize);
                    uint newSubChunkIndex = index % c_genericDictionaryChunkSize;

                    // Generate new chunk if existing chunks are insufficient
                    if (s_genericFunctionPointerCollection.Count <= newChunkIndex)
                    {
                        System.Diagnostics.Debug.Assert(newSubChunkIndex == 0);

                        // New generic descriptors are allocated on the native heap and not tracked in the GC.
                        IntPtr pNewMem = Marshal.AllocHGlobal((int)(c_genericDictionaryChunkSize * sizeof(GenericMethodDescriptor)));
                        s_genericFunctionPointerCollection.Add(pNewMem);
                    }

                    ((GenericMethodDescriptor*)s_genericFunctionPointerCollection[newChunkIndex])[newSubChunkIndex] =
                        new GenericMethodDescriptor(canonFunctionPointer, instantiationArgument);

                    s_genericFunctionPointerDictionary.LookupOrAdd(key, index);

                    // Now that we can no longer have failed, update the next index.
                    s_genericFunctionPointerNextIndex++;
                }

                // Lookup within list
                int chunkIndex = (int)(index / c_genericDictionaryChunkSize);
                uint subChunkIndex = index % c_genericDictionaryChunkSize;
                GenericMethodDescriptor* genericFunctionPointer = &((GenericMethodDescriptor*)s_genericFunctionPointerCollection[chunkIndex])[subChunkIndex];

                System.Diagnostics.Debug.Assert(canonFunctionPointer == genericFunctionPointer->MethodFunctionPointer);
                System.Diagnostics.Debug.Assert(instantiationArgument == genericFunctionPointer->InstantiationArgument);

                return (IntPtr)((byte*)genericFunctionPointer + FatFunctionPointerOffset);
            }
        }

        public static unsafe bool IsGenericMethodPointer(IntPtr functionPointer)
        {
            // Check the low bit to find out what kind of function pointer we have here.
#if TARGET_64BIT
            if ((functionPointer.ToInt64() & FatFunctionPointerOffset) == FatFunctionPointerOffset)
#else
            if ((functionPointer.ToInt32() & FatFunctionPointerOffset) == FatFunctionPointerOffset)
#endif
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
                IntPtr codeTargetA = RuntimeAugments.GetCodeTarget(functionPointerA);
                IntPtr codeTargetB = RuntimeAugments.GetCodeTarget(functionPointerB);
                return codeTargetA == codeTargetB;
            }
            else
            {
                if (!IsGenericMethodPointer(functionPointerB))
                    return false;

                GenericMethodDescriptor* pointerDefA = ConvertToGenericDescriptor(functionPointerA);
                GenericMethodDescriptor* pointerDefB = ConvertToGenericDescriptor(functionPointerB);

                if (pointerDefA->InstantiationArgument != pointerDefB->InstantiationArgument)
                    return false;

                IntPtr codeTargetA = RuntimeAugments.GetCodeTarget(pointerDefA->MethodFunctionPointer);
                IntPtr codeTargetB = RuntimeAugments.GetCodeTarget(pointerDefB->MethodFunctionPointer);
                return codeTargetA == codeTargetB;
            }
        }
    }
}
