// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Native constants and enumerations for ARM EABI binary format.
    /// </summary>
    internal static class EabiNative
    {
        public const uint Tag_File = 1;
        public const uint Tag_CPU_raw_name = 4;
        public const uint Tag_CPU_name = 5;
        public const uint Tag_CPU_arch = 6;
        public const uint Tag_CPU_arch_profile = 7;
        public const uint Tag_ARM_ISA_use = 8;
        public const uint Tag_THUMB_ISA_use = 9;
        public const uint Tag_FP_arch = 10;
        public const uint Tag_ABI_PCS_R9_use = 14;
        public const uint Tag_ABI_PCS_RW_data = 15;
        public const uint Tag_ABI_PCS_RO_data = 16;
        public const uint Tag_ABI_PCS_GOT_use = 17;
        public const uint Tag_ABI_PCS_wchar_t = 18;
        public const uint Tag_ABI_FP_rounding = 19;
        public const uint Tag_ABI_FP_denormal = 20;
        public const uint Tag_ABI_FP_exceptions = 21;
        public const uint Tag_ABI_FP_user_exceptions = 22;
        public const uint Tag_ABI_FP_number_model = 23;
        public const uint Tag_ABI_align_needed = 24;
        public const uint Tag_ABI_align_preserved = 25;
        public const uint Tag_ABI_enum_size = 26;
        public const uint Tag_ABI_VFP_args = 28;
        public const uint Tag_ABI_optimization_goals = 30;
        public const uint Tag_CPU_unaligned_access = 34;
        public const uint Tag_ABI_FP_16bit_format = 38;
        public const uint Tag_conformance = 67;
    }
}
