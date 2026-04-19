// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

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
    static abstract uint DenormalizeSizeOfStackArea(uint size);

    static abstract int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE { get; }

    static abstract int GS_COOKIE_STACK_SLOT_ENCBASE { get; }
    static abstract int CODE_LENGTH_ENCBASE { get; }

    static abstract int STACK_BASE_REGISTER_ENCBASE { get; }
    static abstract int SIZE_OF_STACK_AREA_ENCBASE { get; }
    static abstract int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE { get; }
    static abstract int SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE { get; }
    static abstract int REVERSE_PINVOKE_FRAME_ENCBASE { get; }
    static abstract int NUM_REGISTERS_ENCBASE { get; }
    static abstract int NUM_STACK_SLOTS_ENCBASE { get; }
    static abstract int NUM_UNTRACKED_SLOTS_ENCBASE { get; }
    static abstract int NORM_PROLOG_SIZE_ENCBASE { get; }
    static abstract int NORM_EPILOG_SIZE_ENCBASE { get; }
    static abstract int INTERRUPTIBLE_RANGE_DELTA1_ENCBASE { get; }
    static abstract int INTERRUPTIBLE_RANGE_DELTA2_ENCBASE { get; }
    static abstract int REGISTER_ENCBASE { get; }
    static abstract int REGISTER_DELTA_ENCBASE { get; }
    static abstract int STACK_SLOT_ENCBASE { get; }
    static abstract int STACK_SLOT_DELTA_ENCBASE { get; }
    static abstract int NUM_SAFE_POINTS_ENCBASE { get; }
    static abstract int NUM_INTERRUPTIBLE_RANGES_ENCBASE { get; }

    static abstract bool HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA { get; }

    /// <summary>
    /// Returns true if the given register is a scratch (volatile) register.
    /// Scratch register slots should only be reported for the active (leaf) stack frame.
    /// </summary>
    static abstract bool IsScratchRegister(uint regNum);

    /// <summary>
    /// Returns true if a stack slot at the given offset and base is in the scratch/outgoing area.
    /// Scratch stack slots should only be reported for the active (leaf) stack frame.
    /// spBase uses the GcStackSlotBase encoding: 0=CALLER_SP_REL, 1=SP_REL, 2=FRAMEREG_REL.
    /// </summary>
    static virtual bool IsScratchStackSlot(int spOffset, uint spBase, uint fixedStackParameterScratchArea)
        => false;

    // These are the same across all platforms
    static virtual int POINTER_SIZE_ENCBASE { get; } = 3;
    static virtual int LIVESTATE_RLE_RUN_ENCBASE { get; } = 2;
    static virtual int LIVESTATE_RLE_SKIP_ENCBASE { get; } = 4;
    static virtual uint NUM_NORM_CODE_OFFSETS_PER_CHUNK { get; } = 64;
    static virtual int NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 { get; } = 6;
}
