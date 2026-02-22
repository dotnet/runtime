// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_AMD64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "x64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
#ifdef UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_EDI, REG_ESI, REG_EDX, REG_ECX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_EDI, RBM_ESI, RBM_EDX, RBM_ECX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5, REG_XMM6, REG_XMM7 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3, RBM_XMM4, RBM_XMM5, RBM_XMM6, RBM_XMM7 };
#else // !UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_ECX, REG_EDX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_ECX, RBM_EDX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3 };
#endif // !UNIX_AMD64_ABI
// clang-format on

#ifdef UNIX_AMD64_ABI
//-----------------------------------------------------------------------------
// SysVX64Classifier:
//   Construct a new instance of the SysV x64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
SysVX64Classifier::SysVX64Classifier(const ClassifierInfo& info)
    : m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the SysV x64 ABI.
//
// Parameters:
//   comp           - Compiler instance
//   type           - The type of the parameter
//   structLayout   - The layout of the struct. Expected to be non-null if
//                    varTypeIsStruct(type) is true.
//   wellKnownParam - Well known type of the parameter (if it may affect its ABI classification)
//
// Returns:
//   Classification information for the parameter.
//
ABIPassingInformation SysVX64Classifier::Classify(Compiler*    comp,
                                                  var_types    type,
                                                  ClassLayout* structLayout,
                                                  WellKnownArg wellKnownParam)
{
    bool                                                canEnreg = false;
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    if (varTypeIsStruct(type))
    {
        comp->eeGetSystemVAmd64PassStructInRegisterDescriptor(structLayout->GetClassHandle(), &structDesc);

        if (structDesc.passedInRegisters)
        {
            unsigned intRegCount   = 0;
            unsigned floatRegCount = 0;

            for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
            {
                if (structDesc.IsIntegralSlot(i))
                {
                    intRegCount++;
                }
                else if (structDesc.IsSseSlot(i))
                {
                    floatRegCount++;
                }
                else
                {
                    assert(!"Invalid eightbyte classification type.");
                    break;
                }
            }

            canEnreg = (intRegCount <= m_intRegs.Count()) && (floatRegCount <= m_floatRegs.Count());
        }
    }
    else
    {
        unsigned availRegs = varTypeUsesFloatArgReg(type) ? m_floatRegs.Count() : m_intRegs.Count();
        canEnreg           = availRegs > 0;
    }

    ABIPassingInformation info;
    if (canEnreg)
    {
        if (varTypeIsStruct(type))
        {
            info = ABIPassingInformation(comp, structDesc.eightByteCount);

            for (unsigned i = 0; i < structDesc.eightByteCount; i++)
            {
                regNumber reg = structDesc.IsIntegralSlot(i) ? m_intRegs.Dequeue() : m_floatRegs.Dequeue();
                info.Segment(i) =
                    ABIPassingSegment::InRegister(reg, structDesc.eightByteOffsets[i], structDesc.eightByteSizes[i]);
            }
        }
        else
        {
            regNumber reg = varTypeUsesFloatArgReg(type) ? m_floatRegs.Dequeue() : m_intRegs.Dequeue();
            info          = ABIPassingInformation::FromSegmentByValue(comp,
                                                                      ABIPassingSegment::InRegister(reg, 0, genTypeSize(type)));
        }
    }
    else
    {
        assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);
        unsigned size = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
        info = ABIPassingInformation::FromSegmentByValue(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, size));
        m_stackArgSize += roundUp(size, TARGET_POINTER_SIZE);
    }

    return info;
}

#else // !UNIX_AMD64_ABI

//-----------------------------------------------------------------------------
// WinX64Classifier:
//   Construct a new instance of the Windows x64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
WinX64Classifier::WinX64Classifier(const ClassifierInfo& info)
    : m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the Windows x64 ABI.
//
// Parameters:
//   comp           - Compiler instance
//   type           - The type of the parameter
//   structLayout   - The layout of the struct. Expected to be non-null if
//                    varTypeIsStruct(type) is true.
//   wellKnownParam - Well known type of the parameter (if it may affect its ABI classification)
//
// Returns:
//   Classification information for the parameter.
//
ABIPassingInformation WinX64Classifier::Classify(Compiler*    comp,
                                                 var_types    type,
                                                 ClassLayout* structLayout,
                                                 WellKnownArg wellKnownParam)
{
    // On windows-x64 ABI all parameters take exactly 1 stack slot (structs
    // that do not fit are passed implicitly by reference). Passing a parameter
    // in an int register also consumes the corresponding float register and
    // vice versa.
    assert(m_intRegs.Count() == m_floatRegs.Count());

    bool     passedByRef  = false;
    unsigned typeSize     = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
    bool     passSimdInReg = varTypeIsSIMD(type) && JitConfig.JitPassSimdInReg() != 0;

    if (!passSimdInReg && ((typeSize > TARGET_POINTER_SIZE) || !isPow2(typeSize)))
    {
        passedByRef = true;
        typeSize    = TARGET_POINTER_SIZE;
    }

    ABIPassingSegment segment;
    if (m_intRegs.Count() > 0)
    {
        // SIMD types use float registers (XMM/YMM/ZMM) when passed directly.
        // varTypeUsesFloatArgReg returns false for SIMD on x64, so check explicitly.
        bool      useFloatReg = passSimdInReg || varTypeUsesFloatArgReg(type);
        regNumber reg         = useFloatReg ? m_floatRegs.Peek() : m_intRegs.Peek();
        segment               = ABIPassingSegment::InRegister(reg, 0, typeSize);
        m_intRegs.Dequeue();
        m_floatRegs.Dequeue();
    }
    else
    {
        unsigned stackSlotSize = passSimdInReg ? roundUp(typeSize, TARGET_POINTER_SIZE) : TARGET_POINTER_SIZE;
        segment = ABIPassingSegment::OnStack(m_stackArgSize, 0, typeSize);
        m_stackArgSize += stackSlotSize;
    }

    return ABIPassingInformation::FromSegment(comp, passedByRef, segment);
}

//-----------------------------------------------------------------------------
// GetShadowSpaceCallerOffsetForReg:
//   Get the offset (starting at 0) at which a parameter register has shadow
//   stack space allocated by the caller.
//
// Parameters:
//   reg    - The register
//   offset - [out] Offset, starting at 0.
//
// Returns:
//   True if the register is a parameter register with shadow space allocated
//   by the caller; otherwise false.
//
bool ABIPassingInformation::GetShadowSpaceCallerOffsetForReg(regNumber reg, int* offset)
{
    switch (reg)
    {
        case REG_ECX:
        case REG_XMM0:
            *offset = 0;
            return true;
        case REG_EDX:
        case REG_XMM1:
            *offset = 8;
            return true;
        case REG_R8:
        case REG_XMM2:
            *offset = 16;
            return true;
        case REG_R9:
        case REG_XMM3:
            *offset = 24;
            return true;
        default:
            return false;
    }
}

#ifdef VECTORCALL_SUPPORT
// Integer argument registers for vectorcall (same as Win64)
// clang-format off
static const regNumber vectorcallIntArgRegs [] = { REG_RCX, REG_RDX, REG_R8, REG_R9 };
// clang-format on

// Vector argument registers for vectorcall (6 registers vs 4 for Win64)
// clang-format off
static const regNumber vectorcallFltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5 };
// clang-format on

//-----------------------------------------------------------------------------
// IsSimdVectorType:
//   Check if a class handle represents a SIMD vector type (Vector64/128/256/512).
//
// Parameters:
//   comp   - Compiler instance for EE calls
//   clsHnd - Class handle to check
//
// Returns:
//   true if the type is a recognized SIMD vector intrinsic type
//
static bool IsSimdVectorType(Compiler* comp, CORINFO_CLASS_HANDLE clsHnd)
{
    if (clsHnd == NO_CLASS_HANDLE)
    {
        return false;
    }

    if (!comp->info.compCompHnd->isIntrinsicType(clsHnd))
    {
        return false;
    }

    const char* namespaceName = nullptr;
    const char* className     = comp->info.compCompHnd->getClassNameFromMetadata(clsHnd, &namespaceName);

    if (namespaceName == nullptr || strcmp(namespaceName, "System.Runtime.Intrinsics") != 0)
    {
        return false;
    }

    if (className == nullptr)
    {
        return false;
    }

    return (strncmp(className, "Vector64", 8) == 0 || strncmp(className, "Vector128", 9) == 0 ||
            strncmp(className, "Vector256", 9) == 0 || strncmp(className, "Vector512", 9) == 0);
}

//-----------------------------------------------------------------------------
// IsSimdCompatibleStruct:
//   Check if a struct is SIMD-compatible for vectorcall HVA purposes.
//   A struct is SIMD-compatible if its size is a valid SIMD element size
//   (8, 16, 32, or 64 bytes).
//
//   Note: This function is only called for structs already known to have no GC
//   pointers (the caller checks structLayout->HasGCPtr() before calling this).
//
// Parameters:
//   comp   - Compiler instance for EE calls
//   clsHnd - Class handle of the struct to check
//
// Returns:
//   true if the struct is SIMD-compatible for HVA purposes
//
static bool IsSimdCompatibleStruct(Compiler* comp, CORINFO_CLASS_HANDLE clsHnd)
{
    if (clsHnd == NO_CLASS_HANDLE)
    {
        return false;
    }

    // Check for intrinsic SIMD types first (Vector64, Vector128, etc.)
    if (IsSimdVectorType(comp, clsHnd))
    {
        return true;
    }

    // For non-intrinsic types, check if it's a valid SIMD-compatible size.
    // Valid SIMD element sizes: 8 (Vector64/__m64), 16 (Vector128/__m128),
    // 32 (Vector256/__m256), 64 (Vector512/__m512)
    unsigned size = comp->info.compCompHnd->getClassSize(clsHnd);

    return (size == 8 || size == 16 || size == 32 || size == 64);
}

//-----------------------------------------------------------------------------
// IsHvaByFieldInspection:
//   Determine if a struct is an HVA (Homogeneous Vector Aggregate) by inspecting
//   its fields. An HVA is a struct containing 2-4 fields where ALL fields are
//   SIMD-compatible structs of the same size.
//
// Parameters:
//   comp   - Compiler instance for EE calls
//   clsHnd - Class handle of the struct to check
//   size   - Size of the struct in bytes
//
// Returns:
//   true if the struct is a valid HVA for vectorcall
//
static bool IsHvaByFieldInspection(Compiler* comp, CORINFO_CLASS_HANDLE clsHnd, unsigned size)
{
    if (clsHnd == NO_CLASS_HANDLE)
    {
        return false;
    }

    unsigned fieldCount = comp->info.compCompHnd->getClassNumInstanceFields(clsHnd);

    // HVA must have 2-4 elements
    if (fieldCount < 2 || fieldCount > 4)
    {
        return false;
    }

    // Check each field to verify they're all SIMD-compatible structs of the same size
    unsigned expectedFieldSize = 0;

    for (unsigned i = 0; i < fieldCount; i++)
    {
        CORINFO_FIELD_HANDLE fieldHnd      = comp->info.compCompHnd->getFieldInClass(clsHnd, i);
        CORINFO_CLASS_HANDLE fieldClassHnd = nullptr;
        CorInfoType          fieldCorType  = comp->info.compCompHnd->getFieldType(fieldHnd, &fieldClassHnd);

        // Field must be a value type (struct)
        if (fieldCorType != CORINFO_TYPE_VALUECLASS)
        {
            return false;
        }

        // Field must be a SIMD-compatible struct (intrinsic SIMD type or user-defined __m128-like)
        if (!IsSimdCompatibleStruct(comp, fieldClassHnd))
        {
            return false;
        }

        // Get the size of this field
        unsigned fieldSize = comp->info.compCompHnd->getClassSize(fieldClassHnd);

        if (i == 0)
        {
            expectedFieldSize = fieldSize;
        }
        else if (fieldSize != expectedFieldSize)
        {
            // All fields must be the same size (homogeneous)
            return false;
        }
    }

    // Verify the total size matches field count * element size
    if (size != fieldCount * expectedFieldSize)
    {
        return false;
    }

    return true;
}

//-----------------------------------------------------------------------------
// VectorcallX64Classifier:
//   Construct a new instance of the vectorcall x64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
VectorcallX64Classifier::VectorcallX64Classifier(const ClassifierInfo& info)
{
}

//-----------------------------------------------------------------------------
// PreScanForVectorPositions:
//   Pre-scan all arguments to identify which positions will be used by regular
//   vector arguments. This information is needed for correct discontiguous HVA
//   allocation per the vectorcall ABI.
//
// Parameters:
//   comp - Compiler instance
//   args - The call arguments to scan
//
// Notes:
//   Per Microsoft's vectorcall documentation:
//   "After registers are allocated for vector arguments, the data members of
//   HVA arguments are allocated, in ascending order, to unused vector registers."
//
//   This means regular vectors are allocated FIRST (positionally), then HVAs get
//   the remaining unused registers. To implement this correctly, we need to know
//   which positions will be used by regular vectors BEFORE we encounter any HVAs.
//
void VectorcallX64Classifier::PreScanForVectorPositions(Compiler* comp, CallArgs* args)
{
    unsigned tempPosition = 0;

    for (CallArg& arg : args->Args())
    {
        const var_types            argSigType  = arg.GetSignatureType();
        const CORINFO_CLASS_HANDLE argSigClass = arg.GetSignatureClassHandle();
        ClassLayout* argLayout = argSigClass == NO_CLASS_HANDLE ? nullptr : comp->typGetObjLayout(argSigClass);

        // Check if this is a regular vector type (not an HVA)
        bool isSimdType             = varTypeIsSIMD(argSigType);
        bool isSimdCompatibleStruct = false;
        bool isHva                  = false;

        if (argSigType == TYP_STRUCT && argLayout != nullptr && !argLayout->HasGCPtr())
        {
            unsigned             size   = argLayout->GetSize();
            CORINFO_CLASS_HANDLE clsHnd = argLayout->GetClassHandle();

            // Check for intrinsic SIMD types (Vector64, Vector128, Vector256, Vector512)
            if (IsSimdVectorType(comp, clsHnd))
            {
                isSimdCompatibleStruct = true;
            }

            // For user-defined structs (not intrinsic types), treat appropriate sizes as single SIMD
            // 8/12/16 bytes -> XMM, 32 bytes -> YMM, 64 bytes -> ZMM
            if (!isSimdCompatibleStruct)
            {
                isSimdCompatibleStruct = (size == 8 || size == 12 || size == 16 || size == 32 || size == 64);
            }

            // Check if this is an HVA by inspecting field types
            // An HVA overrides the single-SIMD classification if all fields are vectors
            if (isSimdCompatibleStruct &&
                (size == 32 || size == 48 || size == 64 || size == 96 || size == 128 || size == 192 || size == 256))
            {
                if (IsHvaByFieldInspection(comp, clsHnd, size))
                {
                    isSimdCompatibleStruct = false;
                    isHva                  = true;
                }
            }
        }

        bool isFloatType     = varTypeUsesFloatArgReg(argSigType);
        bool isRegularVector = (isSimdType || isSimdCompatibleStruct || isFloatType) && !isHva;

        if (isHva)
        {
            // HVA takes one position but doesn't use positional XMM allocation
            tempPosition++;
        }
        else if (isRegularVector)
        {
            // Regular vector uses positional allocation - mark this position
            if (tempPosition < 6)
            {
                m_futureVectorPositions |= (1 << tempPosition);
            }
            tempPosition++;
        }
        else
        {
            // Integer/pointer type - just advance position
            tempPosition++;
        }
    }

    // Pre-mark the XMM registers that will be used by regular vectors
    // This is used later when allocating HVAs to unused registers
    m_usedXmmMask = m_futureVectorPositions;
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the vectorcall x64 ABI.
//
// Parameters:
//   comp           - Compiler instance
//   type           - The type of the parameter
//   structLayout   - The layout of the struct. Expected to be non-null if
//                    varTypeIsStruct(type) is true.
//   wellKnownParam - Well known type of the parameter (if it may affect its ABI classification)
//
// Returns:
//   Classification information for the parameter.
//
// Notes:
//   Vectorcall on x64 uses POSITIONAL argument passing (like Win64), not independent
//   allocation. The key differences from Win64:
//   - Vector arguments can use positions 0-5 (6 XMM registers) vs positions 0-3 in Win64
//   - Integer arguments still use positions 0-3 (RCX, RDX, R8, R9)
//   - The register used depends on the argument's position in the parameter list
//
//   For example, MixedIntFloat(int a, float b, int c, double d):
//   - a (int, position 0): RCX
//   - b (float, position 1): XMM1
//   - c (int, position 2): R8
//   - d (double, position 3): XMM3
//
ABIPassingInformation VectorcallX64Classifier::Classify(Compiler*    comp,
                                                        var_types    type,
                                                        ClassLayout* structLayout,
                                                        WellKnownArg wellKnownParam)
{
    bool     passedByRef = false;
    unsigned typeSize    = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);

    // For vectorcall, SIMD types (Vector128, Vector256) are passed directly in XMM/YMM registers,
    // not by reference like in standard x64 calling convention.
    // - TYP_SIMD8 (8 bytes): Vector64 - passed in XMM
    // - TYP_SIMD12 (12 bytes): Vector3 - passed in XMM (padded to 16)
    // - TYP_SIMD16 (16 bytes): Vector128/__m128 - passed in XMM
    // - TYP_SIMD32 (32 bytes): Vector256/__m256 - passed in YMM
    // - TYP_SIMD64 (64 bytes): Vector512/__m512 - passed in ZMM (if AVX-512 available)
    //
    // For structs passed through interop, the signature type is TYP_STRUCT but we can
    // check if it's a SIMD-compatible size (8, 16, 32, 64 bytes with no GC pointers).
    bool isSimdType = varTypeIsSIMD(type);

    // Check if this struct is actually a SIMD-compatible type (e.g., user-defined struct
    // matching __m128/__m256/__m512 layout, or interop structs representing Vector types).
    //
    // For 32/64 byte structs, we need to distinguish between:
    // - A single Vector256/Vector512 (should be passed in one YMM/ZMM register)
    // - An HVA of 2-4 smaller vectors (should be passed in multiple XMM/YMM registers)
    //
    // We use field-type inspection to make this determination.
    bool isSimdCompatibleStruct = false;
    if (type == TYP_STRUCT && structLayout != nullptr && !structLayout->HasGCPtr())
    {
        unsigned             size   = structLayout->GetSize();
        CORINFO_CLASS_HANDLE clsHnd = structLayout->GetClassHandle();

        // Check for intrinsic SIMD types (Vector64, Vector128, Vector256, Vector512)
        if (IsSimdVectorType(comp, clsHnd))
        {
            isSimdCompatibleStruct = true;
        }

        // For user-defined structs (not intrinsic types), treat appropriate sizes as single SIMD
        // 8/12/16 bytes -> XMM, 32 bytes -> YMM, 64 bytes -> ZMM
        if (!isSimdCompatibleStruct)
        {
            isSimdCompatibleStruct = (size == 8 || size == 12 || size == 16 || size == 32 || size == 64);
        }

        // For larger sizes that could be HVA, use field inspection to determine
        // This will override isSimdCompatibleStruct if all fields are vector types
    }

    bool isVectorType = isSimdType || isSimdCompatibleStruct;

    // Check for HVA (Homogeneous Vector Aggregate) - a struct of 2-4 identical vector types.
    // For x64 vectorcall, HVAs are passed in consecutive XMM/YMM registers.
    //
    // We use field-type inspection to determine if a struct is an HVA:
    // - Check if all fields are SIMD vector types (Vector64/128/256/512)
    // - All fields must be the same size (homogeneous)
    // - Must have 2-4 fields
    if (type == TYP_STRUCT && structLayout != nullptr && !structLayout->HasGCPtr())
    {
        var_types            hvaElemType = TYP_UNDEF;
        unsigned             hvaElemSize = 0;
        unsigned             structSize  = structLayout->GetSize();
        CORINFO_CLASS_HANDLE clsHnd      = structLayout->GetClassHandle();

        // First try VM's HFA type detection
        var_types hvaType = comp->GetHfaType(clsHnd);
        if (varTypeIsSIMD(hvaType))
        {
            hvaElemType = hvaType;
            hvaElemSize = genTypeSize(hvaType);
        }
        // Use field-type inspection for HVA detection
        // This properly distinguishes Vec256 (8 floats -> single YMM) from HVA2 (2 Vec128 -> 2 XMM)
        else if (IsHvaByFieldInspection(comp, clsHnd, structSize))
        {
            // Determine element size from field count
            unsigned fieldCount = comp->info.compCompHnd->getClassNumInstanceFields(clsHnd);
            hvaElemSize         = structSize / fieldCount;
            switch (hvaElemSize)
            {
                case 8:
                    hvaElemType = TYP_SIMD8;
                    break;
                case 16:
                    hvaElemType = TYP_SIMD16;
                    break;
                case 32:
                    hvaElemType = TYP_SIMD32;
                    break;
                case 64:
                    hvaElemType = TYP_SIMD64;
                    break;
            }

            // Since we confirmed it's an HVA, override isVectorType
            isVectorType = false;
        }

        if (hvaElemType != TYP_UNDEF)
        {
            unsigned elemCount = structSize / hvaElemSize;

            // x64 vectorcall supports HVAs with 2-4 elements (1 element is just a single vector)
            if (elemCount >= 2 && elemCount <= 4)
            {
                // Per Microsoft docs: "After registers are allocated for vector arguments,
                // the data members of HVA arguments are allocated, in ascending order,
                // to unused vector registers XMM0 to XMM5"
                //
                // HVAs use UNUSED vector registers, not positional allocation.
                // Count available (unused) XMM registers
                unsigned availableRegs = 0;
                for (unsigned i = 0; i < 6; i++)
                {
                    if ((m_usedXmmMask & (1 << i)) == 0)
                    {
                        availableRegs++;
                    }
                }

                if (availableRegs >= elemCount)
                {
                    // Pass HVA in consecutive unused XMM/YMM registers
                    ABIPassingInformation info(comp, elemCount);

                    unsigned elemIdx = 0;
                    for (unsigned regIdx = 0; regIdx < 6 && elemIdx < elemCount; regIdx++)
                    {
                        if ((m_usedXmmMask & (1 << regIdx)) == 0)
                        {
                            // This register is unused, allocate it to this HVA element
                            m_usedXmmMask |= (1 << regIdx);
                            regNumber reg = vectorcallFltArgRegs[regIdx];
                            info.Segment(elemIdx) =
                                ABIPassingSegment::InRegister(reg, elemIdx * hvaElemSize, hvaElemSize);
                            elemIdx++;
                        }
                    }

                    // HVA consumes a single argument position for the whole struct
                    m_argPosition++;

                    return info;
                }
                else
                {
                    // Not enough registers - pass entire HVA on stack
                    // HVA consumes one argument position
                    m_argPosition++;

                    // HVAs consist of SIMD elements; align to 16 bytes,
                    // consistent with other SIMD stack arguments.
                    m_stackArgSize              = roundUp(m_stackArgSize, 16u);
                    unsigned          stackSize = roundUp(structLayout->GetSize(), 16u);
                    ABIPassingSegment segment = ABIPassingSegment::OnStack(m_stackArgSize, 0, structLayout->GetSize());
                    m_stackArgSize += stackSize;

                    return ABIPassingInformation::FromSegmentByValue(comp, segment);
                }
            }
        }
    }

    if (!isVectorType)
    {
        // Structs larger than 8 bytes or non-power-of-2 are passed by reference
        if ((typeSize > TARGET_POINTER_SIZE) || !isPow2(typeSize))
        {
            passedByRef = true;
            typeSize    = TARGET_POINTER_SIZE;
        }
    }

    unsigned          position = m_argPosition++;
    ABIPassingSegment segment;

    // For vectorcall, use XMM/YMM registers for:
    // 1. float/double (standard floating point) - varTypeUsesFloatArgReg returns true
    // 2. SIMD types (Vector128, Vector256, etc.) - varTypeIsSIMD returns true
    // 3. SIMD-compatible structs (e.g., user-defined structs matching __m128 layout)
    bool useFloatReg = (varTypeUsesFloatArgReg(type) || isVectorType) && !passedByRef;

    if (useFloatReg)
    {
        // Vector/float types use XMM registers based on position (0-5)
        if (position < ArrLen(vectorcallFltArgRegs))
        {
            regNumber reg = vectorcallFltArgRegs[position];
            segment       = ABIPassingSegment::InRegister(reg, 0, typeSize);
            // Mark this XMM register as used so HVAs won't use it
            m_usedXmmMask |= (1 << position);
        }
        else
        {
            // Stack alignment: SIMD types need proper alignment
            m_stackArgSize     = roundUp(m_stackArgSize, 16);
            unsigned stackSize = max(typeSize, (unsigned)TARGET_POINTER_SIZE);
            segment            = ABIPassingSegment::OnStack(m_stackArgSize, 0, typeSize);
            m_stackArgSize += stackSize;
        }
    }
    else
    {
        // Integer types use int registers based on position (0-3)
        if (position < ArrLen(vectorcallIntArgRegs))
        {
            regNumber reg = vectorcallIntArgRegs[position];
            segment       = ABIPassingSegment::InRegister(reg, 0, typeSize);
        }
        else
        {
            segment = ABIPassingSegment::OnStack(m_stackArgSize, 0, typeSize);
            m_stackArgSize += TARGET_POINTER_SIZE;
        }
    }

    return ABIPassingInformation::FromSegment(comp, passedByRef, segment);
}
#endif // VECTORCALL_SUPPORT

#endif

#endif // TARGET_AMD64
