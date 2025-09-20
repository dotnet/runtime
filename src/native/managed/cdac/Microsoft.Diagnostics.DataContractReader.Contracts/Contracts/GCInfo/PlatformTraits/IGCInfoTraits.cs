// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IGCInfoTraits
{
    static virtual int NO_GS_COOKIE { get; } = -1;
    static virtual uint NO_STACK_BASE_REGISTER { get; } = 0xFFFFFFFF;
    static virtual uint NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA { get; } = 0xFFFFFFFF;
    static virtual int NO_GENERICS_INST_CONTEXT { get; } = -1;
    static virtual int NO_REVERSE_PINVOKE_FRAME { get; } = -1;
    static virtual int NO_PSP_SYM { get; } = -1;

    static abstract uint DenormalizeStackBaseRegister(uint reg);
    static abstract uint DenormalizeCodeOffset(uint offset);
    static abstract uint NormalizeCodeOffset(uint offset);
    static abstract uint DenormalizeCodeLength(uint len);
    static abstract uint NormalizeCodeLength(uint len);
    static abstract int DenormalizeStackSlot(int x);
    static abstract int CODE_LENGTH_ENCBASE { get; }
    static abstract int GS_COOKIE_STACK_SLOT_ENCBASE { get; }
    static abstract int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE { get; }
    static abstract int STACK_BASE_REGISTER_ENCBASE { get; }
    static abstract int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE { get; }
    static abstract int REVERSE_PINVOKE_FRAME_ENCBASE { get; }
    static abstract int NUM_SAFE_POINTS_ENCBASE { get; }
    static abstract int NUM_INTERRUPTIBLE_RANGES_ENCBASE { get; }
    static abstract int NORM_PROLOG_SIZE_ENCBASE { get; }
    static abstract int NORM_EPILOG_SIZE_ENCBASE { get; }
    static abstract int INTERRUPTIBLE_RANGE_DELTA1_ENCBACE { get; }
    static abstract int INTERRUPTIBLE_RANGE_DELTA2_ENCBACE { get; }
    static abstract int NUM_REGISTERS_ENCBASE { get; }
    static abstract int NUM_STACK_SLOTS_ENCBASE { get; }
    static abstract int NUM_UNTRACKED_SLOTS_ENCBASE { get; }
    static abstract int REGISTER_ENCBASE { get; }
    static abstract int REGISTER_DELTA_ENCBASE { get; }
    static abstract int STACK_SLOT_ENCBASE { get; }
    static abstract int STACK_SLOT_DELTA_ENCBASE { get; }
}
