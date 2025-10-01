// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class AMD64GCInfoTraits : IGCInfoTraits
{
    public static uint DenormalizeStackBaseRegister(uint reg) => reg ^ 0x5u;
    public static uint DenormalizeCodeLength(uint len) => len;
    public static uint NormalizeCodeLength(uint len) => len;
    public static uint DenormalizeCodeOffset(uint offset) => offset;
    public static uint NormalizeCodeOffset(uint offset) => offset;
    public static int DenormalizeStackSlot(int x) => x << 3;

    public static int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE => 6;

    public static int GS_COOKIE_STACK_SLOT_ENCBASE => 6;
    public static int CODE_LENGTH_ENCBASE => 8;

    public static int STACK_BASE_REGISTER_ENCBASE => 3;

    public static int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE => 4;
    public static int REVERSE_PINVOKE_FRAME_ENCBASE => 6;
    public static int NUM_REGISTERS_ENCBASE => 2;
    public static int NUM_STACK_SLOTS_ENCBASE => 2;
    public static int NUM_UNTRACKED_SLOTS_ENCBASE => 1;
    public static int NORM_PROLOG_SIZE_ENCBASE => 5;
    public static int NORM_EPILOG_SIZE_ENCBASE => 3;
    public static int INTERRUPTIBLE_RANGE_DELTA1_ENCBACE => 6;
    public static int INTERRUPTIBLE_RANGE_DELTA2_ENCBACE => 6;
    public static int REGISTER_ENCBASE => 3;
    public static int REGISTER_DELTA_ENCBASE => 2;
    public static int STACK_SLOT_ENCBASE => 6;
    public static int STACK_SLOT_DELTA_ENCBASE => 4;
    public static int NUM_SAFE_POINTS_ENCBASE => 2;
    public static int NUM_INTERRUPTIBLE_RANGES_ENCBASE => 1;
}
