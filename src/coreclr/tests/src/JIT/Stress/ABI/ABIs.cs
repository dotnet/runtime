// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace ABIStress
{
    internal interface IAbi
    {
        // Different ABI's have different conditions on when a method can be
        // the target of a tailcall. For example, on Windows x64 any struct
        // larger than 8 bytes will be passed by reference to a copy on the
        // local stack frame, which inhibits tailcalling. This is the
        // collection of types we can use in tail callee while still allowing
        // fast tail calls.
        Type[] TailCalleeCandidateArgTypes { get; }

        // Multiple calling conventions are supported in pinvokes on x86. This
        // is a collection of the supported ones.
        CallingConvention[] PInvokeConventions { get; }

        // Fast tailcalling is only possible when the caller has more arg stack
        // space then the callee. This approximates the size of the incoming
        // arg stack area for an ABI. It is only an approximation as we do not
        // want to implement the full ABI rules. This is fine as we generate a
        // lot of functions with a lot of parameters so in practice most of
        // them successfully tailcall.
        int ApproximateArgStackAreaSize(List<TypeEx> parameters);
    }

    internal static class Util
    {
        public static int RoundUp(int value, int alignment)
        {
            return (value + alignment - 1) / alignment * alignment;
        }
    }

    internal class Win86Abi : IAbi
    {
        public Type[] TailCalleeCandidateArgTypes { get; } =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(Vector<int>), typeof(Vector128<int>), typeof(Vector256<int>),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U), typeof(S17U),
                typeof(S31U), typeof(S32U),
            };

        public CallingConvention[] PInvokeConventions { get; } = { CallingConvention.Cdecl, CallingConvention.StdCall, };

        public int ApproximateArgStackAreaSize(List<TypeEx> parameters)
        {
            int size = 0;
            foreach (TypeEx pm in parameters)
                size += Util.RoundUp(pm.Size, 4);

            return size;
        }
    }

    internal class Win64Abi : IAbi
    {
        // On Win x64, only 1, 2, 4, and 8-byte sized structs can be passed on
        // the stack. Other structs will be passed by reference and will
        // require helper.
        public Type[] TailCalleeCandidateArgTypes { get; } =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S4P),
                typeof(S4U), typeof(S8P), typeof(S8U),
            };

        public CallingConvention[] PInvokeConventions { get; } = { CallingConvention.Cdecl };

        public int ApproximateArgStackAreaSize(List<TypeEx> parameters)
        {
            // 1, 2, 4 and 8 byte structs are passed directly by value,
            // everything else by ref. That means all args on windows are 8
            // bytes.
            int size = parameters.Count * 8;

            // On win64 there's always at least 32 bytes of stack space allocated.
            size = Math.Max(size, 32);
            return size;
        }
    }

    internal class SysVAbi : IAbi
    {
        // For SysV everything can be passed by value.
        public Type[] TailCalleeCandidateArgTypes { get; } =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(Vector<int>), typeof(Vector128<int>), typeof(Vector256<int>),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U), typeof(S17U),
                typeof(S31U), typeof(S32U),
            };

        public CallingConvention[] PInvokeConventions { get; } = { CallingConvention.Cdecl };

        public int ApproximateArgStackAreaSize(List<TypeEx> parameters)
        {
            int size = 0;
            foreach (TypeEx pm in parameters)
                size += Util.RoundUp(pm.Size, 8);

            return size;
        }
    }

    internal class Arm64Abi : IAbi
    {
        // For Arm64 structs larger than 16 bytes are passed by-ref and will
        // inhibit tailcalls, so we exclude those.
        public Type[] TailCalleeCandidateArgTypes { get; } =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(Vector<int>), typeof(Vector128<int>), typeof(Vector256<int>),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U),
                typeof(Hfa1), typeof(Hfa2),
            };

        public CallingConvention[] PInvokeConventions { get; } = { CallingConvention.Cdecl };

        public int ApproximateArgStackAreaSize(List<TypeEx> parameters)
        {
            int size = 0;
            foreach (TypeEx pm in parameters)
                size += Util.RoundUp(pm.Size, 8);

            return size;
        }
    }

    internal class Arm32Abi : IAbi
    {
        // For arm32 everything can be passed by value
        public Type[] TailCalleeCandidateArgTypes { get; } =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U), typeof(S17U),
                typeof(S31U), typeof(S32U),
                typeof(Hfa1), typeof(Hfa2),
            };

        public CallingConvention[] PInvokeConventions { get; } = { CallingConvention.Cdecl };

        public int ApproximateArgStackAreaSize(List<TypeEx> parameters)
        {
            int size = 0;
            foreach (TypeEx pm in parameters)
                size += Util.RoundUp(pm.Size, 4);

            return size;
        }
    }
}
