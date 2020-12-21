// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(DEBUG) || defined(SMGEN_COMPILE)

//
// The array of state-machine-opcode names
//
const char* const smOpcodeNames[] = {
#define SMOPDEF(smname, string) string,
#include "smopcode.def"
#undef SMOPDEF
};

//
// The code sequences the state machine will look for.
//

const SM_OPCODE s_CodeSeqs[][MAX_CODE_SEQUENCE_LENGTH] = {

#define SMOPDEF(smname, string) {smname, CODE_SEQUENCE_END},
// ==== Single opcode states ====
#include "smopcode.def"
#undef SMOPDEF

    // ==== Legel prefixed opcode sequences ====
    {SM_CONSTRAINED, SM_CALLVIRT, CODE_SEQUENCE_END},

    // ==== Interesting patterns ====

    // Fetching of object field
    {SM_LDARG_0, SM_LDFLD, CODE_SEQUENCE_END},
    {SM_LDARG_1, SM_LDFLD, CODE_SEQUENCE_END},
    {SM_LDARG_2, SM_LDFLD, CODE_SEQUENCE_END},
    {SM_LDARG_3, SM_LDFLD, CODE_SEQUENCE_END},

    // Fetching of struct field
    {SM_LDARGA_S, SM_LDFLD, CODE_SEQUENCE_END},
    {SM_LDLOCA_S, SM_LDFLD, CODE_SEQUENCE_END},

    // Fetching of struct field from a normed struct
    {SM_LDARGA_S_NORMED, SM_LDFLD, CODE_SEQUENCE_END},
    {SM_LDLOCA_S_NORMED, SM_LDFLD, CODE_SEQUENCE_END},

    // stloc/ldloc --> dup
    {SM_STLOC_0, SM_LDLOC_0, CODE_SEQUENCE_END},
    {SM_STLOC_1, SM_LDLOC_1, CODE_SEQUENCE_END},
    {SM_STLOC_2, SM_LDLOC_2, CODE_SEQUENCE_END},
    {SM_STLOC_3, SM_LDLOC_3, CODE_SEQUENCE_END},

    // FPU operations
    {SM_LDC_R4, SM_ADD, CODE_SEQUENCE_END},
    {SM_LDC_R4, SM_SUB, CODE_SEQUENCE_END},
    {SM_LDC_R4, SM_MUL, CODE_SEQUENCE_END},
    {SM_LDC_R4, SM_DIV, CODE_SEQUENCE_END},

    {SM_LDC_R8, SM_ADD, CODE_SEQUENCE_END},
    {SM_LDC_R8, SM_SUB, CODE_SEQUENCE_END},
    {SM_LDC_R8, SM_MUL, CODE_SEQUENCE_END},
    {SM_LDC_R8, SM_DIV, CODE_SEQUENCE_END},

    {SM_CONV_R4, SM_ADD, CODE_SEQUENCE_END},
    {SM_CONV_R4, SM_SUB, CODE_SEQUENCE_END},
    {SM_CONV_R4, SM_MUL, CODE_SEQUENCE_END},
    {SM_CONV_R4, SM_DIV, CODE_SEQUENCE_END},

    // {SM_CONV_R8,       SM_ADD,        CODE_SEQUENCE_END},  // Removed since it collides with ldelem.r8 in
    // Math.InternalRound
    // {SM_CONV_R8,       SM_SUB,        CODE_SEQUENCE_END},  // Just remove the SM_SUB as well.
    {SM_CONV_R8, SM_MUL, CODE_SEQUENCE_END},
    {SM_CONV_R8, SM_DIV, CODE_SEQUENCE_END},

    /* Constant init constructor:
        L_0006: ldarg.0
        L_0007: ldc.r8 0
        L_0010: stfld float64 raytracer.Vec::x
    */

    {SM_LDARG_0, SM_LDC_I4_0, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_LDC_R4, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_LDC_R8, SM_STFLD, CODE_SEQUENCE_END},

    /* Copy constructor:
        L_0006: ldarg.0
        L_0007: ldarg.1
        L_0008: ldfld float64 raytracer.Vec::x
        L_000d: stfld float64 raytracer.Vec::x
    */

    {SM_LDARG_0, SM_LDARG_1, SM_LDFLD, SM_STFLD, CODE_SEQUENCE_END},

    /* Field setter:

        [DebuggerNonUserCode]
        private void CtorClosed(object target, IntPtr methodPtr)
        {
            if (target == null)
            {
                this.ThrowNullThisInDelegateToInstance();
            }
            base._target = target;
            base._methodPtr = methodPtr;
        }


        .method private hidebysig instance void CtorClosed(object target, native int methodPtr) cil managed
        {
            .custom instance void System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor()
            .maxstack 8
            L_0000: ldarg.1
            L_0001: brtrue.s L_0009
            L_0003: ldarg.0
            L_0004: call instance void System.MulticastDelegate::ThrowNullThisInDelegateToInstance()

            L_0009: ldarg.0
            L_000a: ldarg.1
            L_000b: stfld object System.Delegate::_target

            L_0010: ldarg.0
            L_0011: ldarg.2
            L_0012: stfld native int System.Delegate::_methodPtr

            L_0017: ret
        }
    */

    {SM_LDARG_0, SM_LDARG_1, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_LDARG_2, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_LDARG_3, SM_STFLD, CODE_SEQUENCE_END},

    /* Scale operator:

        L_0000: ldarg.0
        L_0001: dup
        L_0002: ldfld float64 raytracer.Vec::x
        L_0007: ldarg.1
        L_0008: mul
        L_0009: stfld float64 raytracer.Vec::x
    */

    {SM_LDARG_0, SM_DUP, SM_LDFLD, SM_LDARG_1, SM_ADD, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_DUP, SM_LDFLD, SM_LDARG_1, SM_SUB, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_DUP, SM_LDFLD, SM_LDARG_1, SM_MUL, SM_STFLD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_DUP, SM_LDFLD, SM_LDARG_1, SM_DIV, SM_STFLD, CODE_SEQUENCE_END},

    /* Add operator
        L_0000: ldarg.0
        L_0001: ldfld float64 raytracer.Vec::x
        L_0006: ldarg.1
        L_0007: ldfld float64 raytracer.Vec::x
        L_000c: add
    */

    {SM_LDARG_0, SM_LDFLD, SM_LDARG_1, SM_LDFLD, SM_ADD, CODE_SEQUENCE_END},
    {SM_LDARG_0, SM_LDFLD, SM_LDARG_1, SM_LDFLD, SM_SUB, CODE_SEQUENCE_END},
    // No need for mul and div since there is no mathemetical meaning of it.

    {SM_LDARGA_S, SM_LDFLD, SM_LDARGA_S, SM_LDFLD, SM_ADD, CODE_SEQUENCE_END},
    {SM_LDARGA_S, SM_LDFLD, SM_LDARGA_S, SM_LDFLD, SM_SUB, CODE_SEQUENCE_END},
    // No need for mul and div since there is no mathemetical meaning of it.

    // The end:
    {CODE_SEQUENCE_END}};

#endif // defined(DEBUG) || defined(SMGEN_COMPILE)
