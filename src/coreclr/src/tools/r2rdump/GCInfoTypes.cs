// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;

namespace R2RDump
{
    class GcInfoTypes
    {
        public int SIZE_OF_RETURN_KIND_SLIM { get; } = 2;
        public int SIZE_OF_RETURN_KIND_FAT { get; } = 2;
        public int CODE_LENGTH_ENCBASE { get; } = 8;
        public int NORM_PROLOG_SIZE_ENCBASE { get; } = 5;
        public int SECURITY_OBJECT_STACK_SLOT_ENCBASE { get; } = 6;
        public int GS_COOKIE_STACK_SLOT_ENCBASE { get; } = 6;
        public int PSP_SYM_STACK_SLOT_ENCBASE { get; } = 6;
        public int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE { get; } = 6;
        public int STACK_BASE_REGISTER_ENCBASE { get; } = 3;
        public int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE { get; } = 4;
        public int REVERSE_PINVOKE_FRAME_ENCBASE { get; } = 6;
        public int SIZE_OF_STACK_AREA_ENCBASE { get; } = 3;
        public int NUM_SAFE_POINTS_ENCBASE { get; } = 3;
        public int NUM_INTERRUPTIBLE_RANGES_ENCBASE { get; } = 1;
        public int INTERRUPTIBLE_RANGE_DELTA1_ENCBASE { get; } = 6;
        public int INTERRUPTIBLE_RANGE_DELTA2_ENCBASE { get; } = 6;

        public int MAX_PREDECODED_SLOTS { get; } = 64;
        public int NUM_REGISTERS_ENCBASE { get; } = 2;
        public int NUM_STACK_SLOTS_ENCBASE { get; } = 2;
        public int NUM_UNTRACKED_SLOTS_ENCBASE { get; } = 1;
        public int REGISTER_ENCBASE { get; } = 3;
        public int REGISTER_DELTA_ENCBASE { get; } = 2;
        public int STACK_SLOT_ENCBASE { get; } = 6;
        public int STACK_SLOT_DELTA_ENCBASE { get; } = 4;

        public GcInfoTypes(Machine machine)
        {
            switch (machine)
            {
                case Machine.Amd64:
                    SIZE_OF_RETURN_KIND_FAT = 4;
                    NUM_SAFE_POINTS_ENCBASE = 2;
                    break;
                case Machine.Arm:
                    CODE_LENGTH_ENCBASE = 7;
                    SECURITY_OBJECT_STACK_SLOT_ENCBASE = 5;
                    GS_COOKIE_STACK_SLOT_ENCBASE = 5;
                    PSP_SYM_STACK_SLOT_ENCBASE = 5;
                    GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE = 5;
                    STACK_BASE_REGISTER_ENCBASE = 1;
                    SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE = 3;
                    REVERSE_PINVOKE_FRAME_ENCBASE = 5;
                    NUM_INTERRUPTIBLE_RANGES_ENCBASE = 2;
                    INTERRUPTIBLE_RANGE_DELTA1_ENCBASE = 4;
                    NUM_STACK_SLOTS_ENCBASE = 3;
                    NUM_UNTRACKED_SLOTS_ENCBASE = 3;
                    REGISTER_ENCBASE = 2;
                    REGISTER_DELTA_ENCBASE = 1;
                    break;
                case Machine.Arm64:
                    SIZE_OF_RETURN_KIND_FAT = 4;
                    STACK_BASE_REGISTER_ENCBASE = 2;
                    NUM_REGISTERS_ENCBASE = 3;
                    break;
                case Machine.I386:
                    CODE_LENGTH_ENCBASE = 6;
                    NORM_PROLOG_SIZE_ENCBASE = 4;
                    SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE = 3;
                    SIZE_OF_STACK_AREA_ENCBASE = 6;
                    NUM_SAFE_POINTS_ENCBASE = 4;
                    INTERRUPTIBLE_RANGE_DELTA1_ENCBASE = 5;
                    INTERRUPTIBLE_RANGE_DELTA2_ENCBASE = 5;
                    NUM_REGISTERS_ENCBASE = 3;
                    NUM_STACK_SLOTS_ENCBASE = 5;
                    NUM_UNTRACKED_SLOTS_ENCBASE = 5;
                    REGISTER_DELTA_ENCBASE = 3;
                    break;
            }
        }
            
}
}
