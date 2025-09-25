// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class ARM64GCInfoTraits : IGCInfoTraits
{
    public static uint DenormalizeStackBaseRegister(uint reg) => throw new NotImplementedException();
    public static uint DenormalizeCodeLength(uint len) => throw new NotImplementedException();
    public static uint NormalizeCodeLength(uint len) => throw new NotImplementedException();
    public static uint DenormalizeCodeOffset(uint offset) => throw new NotImplementedException();
    public static uint NormalizeCodeOffset(uint offset) => throw new NotImplementedException();
    public static int DenormalizeStackSlot(int x) => throw new NotImplementedException();

    public static int CODE_LENGTH_ENCBASE => throw new NotImplementedException();
    public static int NORM_PROLOG_SIZE_ENCBASE => throw new NotImplementedException();
    public static int NORM_EPILOG_SIZE_ENCBASE => throw new NotImplementedException();
    public static int GS_COOKIE_STACK_SLOT_ENCBASE => throw new NotImplementedException();
    public static int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE => throw new NotImplementedException();
    public static int STACK_BASE_REGISTER_ENCBASE => throw new NotImplementedException();
    public static int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE => throw new NotImplementedException();
    public static int REVERSE_PINVOKE_FRAME_ENCBASE => throw new NotImplementedException();
    public static int NUM_SAFE_POINTS_ENCBASE => throw new NotImplementedException();
    public static int NUM_INTERRUPTIBLE_RANGES_ENCBASE => throw new NotImplementedException();

    public static int INTERRUPTIBLE_RANGE_DELTA1_ENCBACE => throw new NotImplementedException();

    public static int INTERRUPTIBLE_RANGE_DELTA2_ENCBACE => throw new NotImplementedException();

    public static int NUM_REGISTERS_ENCBASE => throw new NotImplementedException();

    public static int NUM_STACK_SLOTS_ENCBASE => throw new NotImplementedException();

    public static int NUM_UNTRACKED_SLOTS_ENCBASE => throw new NotImplementedException();

    public static int REGISTER_ENCBASE => throw new NotImplementedException();

    public static int REGISTER_DELTA_ENCBASE => throw new NotImplementedException();

    public static int STACK_SLOT_ENCBASE => throw new NotImplementedException();

    public static int STACK_SLOT_DELTA_ENCBASE => throw new NotImplementedException();
}
