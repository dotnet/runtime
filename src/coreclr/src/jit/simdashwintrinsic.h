// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMD_AS_HWINTRINSIC_H_
#define _SIMD_AS_HWINTRINSIC_H_

enum class SimdAsHWIntrinsicClassId
{
    Unknown,
    Vector2,
    Vector3,
    Vector4,
    VectorT128,
    VectorT256,
};

enum class SimdAsHWIntrinsicFlag : unsigned int
{
    None = 0,

    // Indicates compFloatingPointUsed does not need to be set.
    NoFloatingPointUsed = 0x1,

    // Indicates the intrinsic is for an instance method.
    InstanceMethod = 0x02,

    // Indicates the operands should be swapped in importation.
    NeedsOperandsSwapped = 0x04,
};

inline SimdAsHWIntrinsicFlag operator~(SimdAsHWIntrinsicFlag value)
{
    return static_cast<SimdAsHWIntrinsicFlag>(~static_cast<unsigned int>(value));
}

inline SimdAsHWIntrinsicFlag operator|(SimdAsHWIntrinsicFlag lhs, SimdAsHWIntrinsicFlag rhs)
{
    return static_cast<SimdAsHWIntrinsicFlag>(static_cast<unsigned int>(lhs) | static_cast<unsigned int>(rhs));
}

inline SimdAsHWIntrinsicFlag operator&(SimdAsHWIntrinsicFlag lhs, SimdAsHWIntrinsicFlag rhs)
{
    return static_cast<SimdAsHWIntrinsicFlag>(static_cast<unsigned int>(lhs) & static_cast<unsigned int>(rhs));
}

inline SimdAsHWIntrinsicFlag operator^(SimdAsHWIntrinsicFlag lhs, SimdAsHWIntrinsicFlag rhs)
{
    return static_cast<SimdAsHWIntrinsicFlag>(static_cast<unsigned int>(lhs) ^ static_cast<unsigned int>(rhs));
}

struct SimdAsHWIntrinsicInfo
{
    NamedIntrinsic           id;
    const char*              name;
    SimdAsHWIntrinsicClassId classId;
    int                      numArgs;
    NamedIntrinsic           hwIntrinsic[10];
    SimdAsHWIntrinsicFlag    flags;

    static const SimdAsHWIntrinsicInfo& lookup(NamedIntrinsic id);

    static NamedIntrinsic lookupId(CORINFO_SIG_INFO* sig,
                                   const char*       className,
                                   const char*       methodName,
                                   const char*       enclosingClassName,
                                   int               sizeOfVectorT);
    static SimdAsHWIntrinsicClassId lookupClassId(const char* className,
                                                  const char* enclosingClassName,
                                                  int         sizeOfVectorT);

    // Member lookup

    static NamedIntrinsic lookupId(NamedIntrinsic id)
    {
        return lookup(id).id;
    }

    static const char* lookupName(NamedIntrinsic id)
    {
        return lookup(id).name;
    }

    static SimdAsHWIntrinsicClassId lookupClassId(NamedIntrinsic id)
    {
        return lookup(id).classId;
    }

    static int lookupNumArgs(NamedIntrinsic id)
    {
        return lookup(id).numArgs;
    }

    static NamedIntrinsic lookupHWIntrinsic(NamedIntrinsic id, var_types type)
    {
        if ((type < TYP_BYTE) || (type > TYP_DOUBLE))
        {
            assert(!"Unexpected type");
            return NI_Illegal;
        }
        return lookup(id).hwIntrinsic[type - TYP_BYTE];
    }

    static SimdAsHWIntrinsicFlag lookupFlags(NamedIntrinsic id)
    {
        return lookup(id).flags;
    }

    // Flags lookup

    static bool IsFloatingPointUsed(NamedIntrinsic id)
    {
        SimdAsHWIntrinsicFlag flags = lookupFlags(id);
        return (flags & SimdAsHWIntrinsicFlag::NoFloatingPointUsed) == SimdAsHWIntrinsicFlag::None;
    }

    static bool IsInstanceMethod(NamedIntrinsic id)
    {
        SimdAsHWIntrinsicFlag flags = lookupFlags(id);
        return (flags & SimdAsHWIntrinsicFlag::InstanceMethod) == SimdAsHWIntrinsicFlag::InstanceMethod;
    }

    static bool NeedsOperandsSwapped(NamedIntrinsic id)
    {
        SimdAsHWIntrinsicFlag flags = lookupFlags(id);
        return (flags & SimdAsHWIntrinsicFlag::NeedsOperandsSwapped) == SimdAsHWIntrinsicFlag::NeedsOperandsSwapped;
    }
};

#endif // _SIMD_AS_HWINTRINSIC_H_
