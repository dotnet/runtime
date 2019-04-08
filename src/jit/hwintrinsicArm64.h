// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _HW_INTIRNSIC_ARM64_H_
#define _HW_INTIRNSIC_ARM64_H_

#ifdef FEATURE_HW_INTRINSICS

struct HWIntrinsicInfo
{
    // Forms are used to gather inrinsics with similar characteristics
    // Generally instructions with the same form will be treated
    // identically by the Importer, LSRA, Lowering, and CodeGen
    enum Form
    {
        // Non SIMD forms
        UnaryOp,      // Non SIMD intrinsics which take a single argument
        CrcOp,        // Crc intrinsics.
        Sha1RotateOp, // SHA1 Hash Rotate intrinsics. Takes hash index unsigned int and returns unsigned int.

        // SIMD common forms
        SimdBinaryOp,     // SIMD intrinsics which take two vector operands and return a vector
        SimdUnaryOp,      // SIMD intrinsics which take one vector operand and return a vector
        SimdBinaryRMWOp,  // Same as SimdBinaryOp , with first source vector used as destination vector also.
        SimdTernaryRMWOp, // SIMD intrinsics which take three vector operands and return a vector ,
                          // with destination vector same as first source vector
        // SIMD custom forms
        SimdExtractOp, // SIMD intrinsics which take one vector operand and a lane index and return an element
        SimdInsertOp,  // SIMD intrinsics which take one vector operand and a lane index and value and return a vector
        SimdSelectOp,  // BitwiseSelect intrinsic which takes three vector operands and returns a vector
        SimdSetAllOp,  // Simd intrinsics which take one numeric operand and return a vector
        Sha1HashOp     // SIMD instrisics for SHA1 Hash operations. Takes two vectors and hash index and returns vector
    };

    // Flags will be used to handle secondary meta-data which will help
    // Reduce the number of forms
    enum Flags
    {
        None          = 0,
        LowerCmpUZero = (1UL << 0), // Unsigned zero compare form must be lowered
    };

    NamedIntrinsic id;
    const char*    name;
    uint64_t       isaflags;
    Form           form;
    Flags          flags;
    instruction    instrs[3];

    static const HWIntrinsicInfo& lookup(NamedIntrinsic id);
    static int lookupNumArgs(const GenTreeHWIntrinsic* node);

    static bool isFullyImplementedIsa(InstructionSet isa);
    static bool isScalarIsa(InstructionSet isa);

    // Member lookup

    static NamedIntrinsic lookupId(NamedIntrinsic id)
    {
        return lookup(id).id;
    }

    static const char* lookupName(NamedIntrinsic id)
    {
        return lookup(id).name;
    }
};

#endif // FEATURE_HW_INTRINSICS
#endif // _HW_INTIRNSIC_ARM64_H_
