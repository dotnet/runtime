// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_POWERPC64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "ppc64le";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_R3, REG_R4, REG_R5, REG_R6, REG_R7, REG_R8, REG_R9, REG_R10};
const regMaskTP intArgMasks[] = {RBM_R0, RBM_R1, RBM_R2, RBM_R3, RBM_R4, RBM_R5, RBM_R6, RBM_R7};

const regNumber fltArgRegs [] = {REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7, REG_F8, REG_F9, REG_F10, REG_F11, REG_F12, REG_F13};
const regMaskTP fltArgMasks[] = {RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7, RBM_F8, RBM_F9, RBM_F10, RBM_F11, RBM_F12, RBM_F13};
// clang-format on

//-----------------------------------------------------------------------------
// IsPpc64leHfaLikeStruct: Check if a struct is a Homogeneous Float Aggregate (HFA)
//
// Arguments:
//    comp       - Compiler instance
//    hClass     - Class handle for the struct
//    pHfaType   - [out] Type of HFA elements (TYP_FLOAT or TYP_DOUBLE), or TYP_UNDEF if not HFA
//    pNumFields - [out] Number of HFA fields
//
// Return Value:
//    true if the struct is an HFA (all float or all double fields), false otherwise
//
// Notes:
//    This function detects HFA structs without requiring FEATURE_HFA to be enabled.
//    It calls the VM's getHFAType() directly to determine if a struct qualifies as HFA.
//    Per PPC64LE ELFv2 ABI:
//    - For parameters: HFA can use all 13 float registers (f1-f13)
//    - For return values: HFA limited to 8 fields maximum
//
bool IsPpc64leHfaLikeStruct(Compiler* comp, CORINFO_CLASS_HANDLE hClass, var_types* pHfaType, unsigned* pNumFields)
{
    assert(comp != nullptr);
    assert(hClass != NO_CLASS_HANDLE);
    assert(pHfaType != nullptr);
    assert(pNumFields != nullptr);
    
    // Initialize output parameters
    *pHfaType = TYP_UNDEF;
    *pNumFields = 0;
    
    // Call VM to detect HFA type
    CorInfoHFAElemType hfaElemKind = comp->info.compCompHnd->getHFAType(hClass);
    
    // Check if it's an HFA (all float or all double fields)
    bool isHfa = (hfaElemKind == CORINFO_HFA_ELEM_FLOAT || hfaElemKind == CORINFO_HFA_ELEM_DOUBLE);
    
    if (isHfa)
    {
        // Determine the HFA element type
        *pHfaType = (hfaElemKind == CORINFO_HFA_ELEM_FLOAT) ? TYP_FLOAT : TYP_DOUBLE;
        
        // Calculate number of fields based on struct size
        unsigned structSize = comp->info.compCompHnd->getClassSize(hClass);
        unsigned fieldSize = (*pHfaType == TYP_FLOAT) ? 4 : 8;
        *pNumFields = structSize / fieldSize;
        
        // For parameters: can use up to 13 float registers (f1-f13)
        // For return values: limited to 8 fields (checked elsewhere)
        assert(*pNumFields > 0 && *pNumFields <= 13);
    }
    
    return isHfa;
}


//-----------------------------------------------------------------------------
// S390xClassifier:
//   Construct a new instance of the S390X ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
Ppc64leClassifier::Ppc64leClassifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the PPC64LE ELFv2 ABI.
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
//   PPC64LE ELFv2 ABI has a unique characteristic: when a floating-point argument
//   is passed in a float register (f1-f13), it also consumes the corresponding
//   integer register slot (r3-r10) if within the first 8 parameter slots.
//
ABIPassingInformation Ppc64leClassifier::Classify(Compiler*    comp,
                                                  var_types    type,
                                                  ClassLayout* structLayout,
                                                  WellKnownArg wellKnownParam)
{
    // PPC64LE ELFv2 ABI: Stack offset = 32 + (m_numTotalSlots * 8)
    // - 32 bytes: mandatory header
    // - m_numTotalSlots: cumulative count of 8-byte slots consumed by all previous arguments
    unsigned currentArgOffset = 32 + (m_numTotalSlots * 8);
    
    if ((wellKnownParam == WellKnownArg::RetBuffer) && hasFixedRetBuffReg(m_info.CallConv))
    {
        // Return buffer is passed in r3
        m_intRegs.Dequeue(); // Consume r3
        m_argNum++; // Increment argument count
        m_numTotalSlots++; // Consumes 1 slot
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_ARG_RET_BUFF, 0, TARGET_POINTER_SIZE));
    }

    // Handle floating-point and double arguments
    if (varTypeUsesFloatArgReg(type) && !m_info.IsVarArgs)
    {
        // PPC64LE ELFv2 ABI: Float args consume both float reg AND corresponding int reg slot(s)
        if (m_floatRegs.Count() > 0)
        {
            regNumber floatReg = m_floatRegs.Dequeue();
            unsigned size = (type == TYP_FLOAT) ? 4 : 8;
            
            // Determine how many slots this float argument consumes
            // TYP_FLOAT and TYP_DOUBLE consume 1 slot (8 bytes)
            // Future: 16-byte float types would consume 2 slots
            unsigned slotsNeeded = 1;
            
            // Consume corresponding integer register slot(s) if within first 8 total slots
            // This maintains the parallel consumption of int and float register spaces
            if (m_numTotalSlots < 8)
            {
                unsigned slotsToConsume = min(slotsNeeded, 8 - m_numTotalSlots);
                for (unsigned i = 0; i < slotsToConsume && m_intRegs.Count() > 0; i++)
                {
                    m_intRegs.Dequeue();
                }
            }
            
            m_argNum++; // Increment argument count
            m_numTotalSlots += slotsNeeded; // Float/double consumes slot(s)
            return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(floatReg, 0, size));
        }
        else
        {
            // No float registers available, pass on stack
            unsigned size = (type == TYP_FLOAT) ? 4 : 8;
            ABIPassingInformation info = ABIPassingInformation::FromSegment(comp,
                ABIPassingSegment::OnStack(currentArgOffset, 0, size));
            m_argNum++; // Increment argument count
            m_numTotalSlots++; // Consumes 1 slot
            m_stackArgSize = currentArgOffset + TARGET_POINTER_SIZE;
            
            // Clear remaining registers once we go to stack
            m_floatRegs.Clear();
            m_intRegs.Clear();
            
            return info;
        }
    }

    // Handle struct types
    if (varTypeIsStruct(type))
    {
        unsigned size = structLayout->GetSize();
        
        // PPC64LE ELFv2 ABI: Check if struct is a Homogeneous Float Aggregate (HFA)
        // HFA: struct with all float or all double fields (up to 8 fields)
        CORINFO_CLASS_HANDLE classHandle = structLayout->GetClassHandle();
        var_types hfaType = TYP_UNDEF;
        unsigned numFields = 0;
        bool isHfa = IsPpc64leHfaLikeStruct(comp, classHandle, &hfaType, &numFields);
        
        // If HFA, pass in float registers
        if (isHfa && !m_info.IsVarArgs)
        {
            // Get field size from HFA type
            unsigned fieldSize = (hfaType == TYP_FLOAT) ? 4 : 8;
            
            // PPC64LE: Each HFA field consumes an 8-byte slot, even for 4-byte floats
            unsigned slotSize = TARGET_POINTER_SIZE; // Always 8 bytes per slot
            
            // Check if we have enough float registers
            if (m_floatRegs.Count() >= numFields)
            {
                // Pass HFA in float registers
                ABIPassingInformation info = ABIPassingInformation(comp, numFields);
                unsigned offset = 0;
                
                for (unsigned i = 0; i < numFields; i++)
                {
                    info.Segment(i) = ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), offset, fieldSize);
                    offset += fieldSize;
                }
                
                // HFA also consumes corresponding int register slots if within first 8 slots
                if (m_numTotalSlots < 8)
                {
                    unsigned slotsToConsume = min(numFields, 8 - m_numTotalSlots);
                    for (unsigned i = 0; i < slotsToConsume && m_intRegs.Count() > 0; i++)
                    {
                        m_intRegs.Dequeue();
                    }
                }
                
                m_argNum++;
                m_numTotalSlots += numFields;
                return info;
            }
            else if (m_floatRegs.Count() > 0)
            {
                // Split HFA between float registers and stack
                unsigned regsAvailable = m_floatRegs.Count();
                unsigned stackFields = numFields - regsAvailable;
                
                ABIPassingInformation info = ABIPassingInformation(comp, numFields);
                unsigned offset = 0;
                
                // Fill available float registers
                for (unsigned i = 0; i < regsAvailable; i++)
                {
                    info.Segment(i) = ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), offset, fieldSize);
                    offset += fieldSize;
                }
                
                // Consume corresponding int register slots
                if (m_numTotalSlots < 8)
                {
                    unsigned slotsToConsume = min(regsAvailable, 8 - m_numTotalSlots);
                    for (unsigned i = 0; i < slotsToConsume && m_intRegs.Count() > 0; i++)
                    {
                        m_intRegs.Dequeue();
                    }
                }
                
                m_numTotalSlots += regsAvailable;
                unsigned stackOffset = 32 + (m_numTotalSlots * 8);
                
                // Put remainder on stack - each field uses 8-byte slot
                for (unsigned i = regsAvailable; i < numFields; i++)
                {
                    info.Segment(i) = ABIPassingSegment::OnStack(stackOffset, offset, fieldSize);
                    offset += fieldSize;
                    stackOffset += slotSize; // Advance by 8 bytes per slot
                }
                
                m_argNum++;
                m_numTotalSlots += stackFields;
                m_stackArgSize = stackOffset;
                m_floatRegs.Clear();
                return info;
            }
            else
            {
                // Pass HFA entirely on stack - each field uses 8-byte slot
                ABIPassingInformation info = ABIPassingInformation(comp, numFields);
                unsigned offset = 0;
                unsigned stackOffset = 32 + (m_numTotalSlots * 8);
                
                for (unsigned i = 0; i < numFields; i++)
                {
                    info.Segment(i) = ABIPassingSegment::OnStack(stackOffset, offset, fieldSize);
                    offset += fieldSize;
                    stackOffset += slotSize; // Advance by 8 bytes per slot
                }
                
                m_argNum++;
                m_numTotalSlots += numFields;
                m_stackArgSize = stackOffset;
                m_floatRegs.Clear();
                m_intRegs.Clear();
                return info;
            }
        }
        
        // Non-HFA struct: pass in integer registers
        // PPC64LE ELFv2 ABI: Structs are passed by value in registers or on stack
        // Calculate number of 8-byte slots needed
        unsigned slots = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
        
        if (m_intRegs.Count() >= slots)
        {
            // Pass entirely in registers
            ABIPassingInformation info = ABIPassingInformation(comp, slots);
            unsigned offset = 0;
            
            for (unsigned i = 0; i < slots; i++)
            {
                unsigned slotSize = min(size - offset, (unsigned)TARGET_POINTER_SIZE);
                info.Segment(i) = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), offset, slotSize);
                offset += slotSize;
            }
            
            m_argNum++; // Increment argument count
            m_numTotalSlots += slots; // Struct consumes 'slots' number of slots
            return info;
        }
        else if (m_intRegs.Count() > 0)
        {
            // Split between registers and stack
            // Pass what fits in remaining registers, rest on stack
            unsigned regsAvailable = m_intRegs.Count();
            unsigned regsUsed = min(regsAvailable, slots);
            unsigned stackSlots = slots - regsUsed;
            
            ABIPassingInformation info = ABIPassingInformation(comp, slots);
            unsigned offset = 0;
            
            // Fill available registers
            for (unsigned i = 0; i < regsUsed; i++)
            {
                unsigned slotSize = min(size - offset, (unsigned)TARGET_POINTER_SIZE);
                info.Segment(i) = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), offset, slotSize);
                offset += slotSize;
            }
            
            // Put remainder on stack
            // For split arguments, we need to recalculate the stack offset after incrementing m_numTotalSlots
            // by the number of register slots used, so the stack portion gets the correct offset
            m_numTotalSlots += regsUsed; // Increment by register slots used
            unsigned stackOffset = 32 + (m_numTotalSlots * 8); // Recalculate stack offset
            
            for (unsigned i = regsUsed; i < slots; i++)
            {
                unsigned slotSize = min(size - offset, (unsigned)TARGET_POINTER_SIZE);
                info.Segment(i) = ABIPassingSegment::OnStack(stackOffset, offset, slotSize);
                offset += slotSize;
                stackOffset += TARGET_POINTER_SIZE;
            }
            
            m_argNum++; // Increment argument count
            m_numTotalSlots += stackSlots; // Add remaining stack slots
            m_stackArgSize = stackOffset; // Update to the final stack offset
            return info;
        }
        else
        {
            // Pass entirely on stack
            ABIPassingInformation info = ABIPassingInformation(comp, slots);
            unsigned offset = 0;
            unsigned stackOffset = currentArgOffset;
            
            for (unsigned i = 0; i < slots; i++)
            {
                unsigned slotSize = min(size - offset, (unsigned)TARGET_POINTER_SIZE);
                info.Segment(i) = ABIPassingSegment::OnStack(stackOffset, offset, slotSize);
                offset += slotSize;
                stackOffset += TARGET_POINTER_SIZE;
            }
            
            m_argNum++; // Increment argument count
            m_numTotalSlots += slots; // Struct consumes 'slots' number of slots
            m_stackArgSize = currentArgOffset + (slots * TARGET_POINTER_SIZE);
            m_intRegs.Clear();
            return info;
        }
    }

    // Handle integer and pointer types
    assert(genTypeSize(type) <= TARGET_POINTER_SIZE);
    
    if (m_intRegs.Count() > 0)
    {
        // Pass in integer register
        unsigned size = genTypeSize(type);
        m_argNum++; // Increment argument count
        m_numTotalSlots++; // Integer consumes 1 slot
        return ABIPassingInformation::FromSegment(comp,
            ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, size));
    }
    else
    {
        // Pass on stack
        unsigned size = genTypeSize(type);
        ABIPassingInformation info = ABIPassingInformation::FromSegment(comp,
            ABIPassingSegment::OnStack(currentArgOffset, 0, size));
        m_argNum++; // Increment argument count
        m_numTotalSlots++; // Integer consumes 1 slot
        m_stackArgSize = currentArgOffset + TARGET_POINTER_SIZE;
        m_intRegs.Clear();
        return info;
    }
}
#endif
