// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: ValueHome.cpp
//

//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

// constructor to initialize an instance of EnregisteredValueHome
// Arguments:
//     input:  pFrame  - frame to which the value belongs
//     output: no out parameters, but the instance has been initialized
EnregisteredValueHome::EnregisteredValueHome(const CordbNativeFrame * pFrame):
    m_pFrame(pFrame)
{
    _ASSERTE(pFrame != NULL);
}

// ----------------------------------------------------------------------------
// RegValueHome member function implementations
// ----------------------------------------------------------------------------

// initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
// instance of a derived class of EnregisteredValueHome (see EnregisteredValueHome::CopyToIPCEType for full
// header comment)
void RegValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_REG;
    pRegAddr->reg1 = m_reg1Info.m_kRegNumber;
    pRegAddr->reg1Addr = m_reg1Info.m_regAddr;
    pRegAddr->reg1Value = m_reg1Info.m_regValue;
} // RegValueHome::CopyToIPCEType

// RegValueHome::SetContextRegister
// This will update a register in a given context, and in the regdisplay of a given frame.
// Arguments:
// updates a register in a given context buffer using the DAC.
// Arguments:
//     input:  pContext - context buffer in which the register is to be updated
//             regnum   - enumeration constant indicating which register is to be updated
//             newVal   - the new value for the register contents
//     output: no out parameters, but the new value will be written to the context buffer
void RegValueHome::SetContextRegister(BYTE *           pContext,
                                      CorDebugRegister regNum,
                                      SIZE_T           newVal)
{
    IDacDbiInterface * pDAC = m_pFrame->GetProcess()->GetDAC();
    TADDR value = (TADDR)newVal;
    ULONG32 cbCtx = m_pFrame->GetProcess()->GetTargetContextSize();
    HRESULT hr = pDAC->WriteRegistersToContext(pContext, cbCtx, &regNum, 1, &value);
    if (FAILED(hr))
    {
        _ASSERTE(!"Invalid register number!");
        ThrowHR(E_FAIL);
    }
} // RegValueHome::SetContextRegister

// RegValueHome::SetEnregisteredValue
// set a remote enregistered location to a new value (see code:EnregisteredValueHome::SetEnregisteredValue
// for full header comment)
void RegValueHome::SetEnregisteredValue(MemoryRange newValue, BYTE * pContext, bool fIsSigned)
{
    SIZE_T extendedVal = 0;

    // If the value is in a reg, then it's going to be a register's width (regardless of
    // the actual width of the data).
    // For signed types, like i2, i1, make sure we sign extend.

    if (fIsSigned)
    {
        // Sign extend. SSIZE_T is a register size signed value.
        // Casting
        switch(newValue.Size())
        {
            case 1:  _ASSERTE(sizeof( BYTE) == 1);
                     extendedVal = (SSIZE_T) *(char*)newValue.StartAddress();           break;
            case 2:  _ASSERTE(sizeof( WORD) == 2);
                     extendedVal = (SSIZE_T) *(short*)newValue.StartAddress();          break;
            case 4:  _ASSERTE(sizeof(DWORD) == 4);
                     extendedVal = (SSIZE_T) *(int*)newValue.StartAddress();            break;
#if defined(TARGET_64BIT)
            case 8:  _ASSERTE(sizeof(ULONGLONG) == 8);
                     extendedVal = (SSIZE_T) *(ULONGLONG*)newValue.StartAddress();      break;
#endif // TARGET_64BIT
            default: _ASSERTE(!"bad size");
        }
    }
    else
    {
        // Zero extend.
        switch(newValue.Size())
        {
            case 1:  _ASSERTE(sizeof( BYTE) == 1);
                     extendedVal = *( BYTE*)newValue.StartAddress();     break;
            case 2:  _ASSERTE(sizeof( WORD) == 2);
                     extendedVal = *( WORD*)newValue.StartAddress();     break;
            case 4:  _ASSERTE(sizeof(DWORD) == 4);
                     extendedVal = *(DWORD*)newValue.StartAddress();     break;
#if defined(TARGET_64BIT)
            case 8:  _ASSERTE(sizeof(ULONGLONG) == 8);
                     extendedVal = *(ULONGLONG*)newValue.StartAddress(); break;
#endif // TARGET_64BIT
            default: _ASSERTE(!"bad size");
        }
    }

    SetContextRegister(pContext, m_reg1Info.m_kRegNumber, extendedVal); // throws
} // RegValueHome::SetEnregisteredValue

// RegValueHome::GetEnregisteredValue
// Gets an enregistered value and returns it to the caller (see EnregisteredValueHome::GetEnregisteredValue
// for full header comment)
void RegValueHome::GetEnregisteredValue(MemoryRange valueOutBuffer)
{
    _ASSERTE(sizeof(TADDR) == valueOutBuffer.Size());

    TADDR value = 0;
    HRESULT hr = m_pFrame->ReadContextRegister(m_reg1Info.m_kRegNumber, &value);
    IfFailThrow(hr);

    memcpy(valueOutBuffer.StartAddress(), &value, sizeof(value));
} // RegValueHome::GetEnregisteredValue


// ----------------------------------------------------------------------------
// RegRegValueHome member function implementations
// ----------------------------------------------------------------------------

// initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
// instance of a derived class of EnregisteredValueHome (see EnregisteredValueHome::CopyToIPCEType for full
// header comment)
void RegRegValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_REGREG;
    pRegAddr->reg1 = m_reg1Info.m_kRegNumber;
    pRegAddr->reg1Addr = m_reg1Info.m_regAddr;
    pRegAddr->reg1Value = m_reg1Info.m_regValue;
    pRegAddr->u.reg2 = m_reg2Info.m_kRegNumber;
    pRegAddr->u.reg2Addr = m_reg2Info.m_regAddr;
    pRegAddr->u.reg2Value = m_reg2Info.m_regValue;
} // RegRegValueHome::CopyToIPCEType

// RegRegValueHome::SetEnregisteredValue
// set a remote enregistered location to a new value (see EnregisteredValueHome::SetEnregisteredValue
// for full header comment)
void RegRegValueHome::SetEnregisteredValue(MemoryRange newValue, BYTE * pContext, bool fIsSigned)
{
    _ASSERTE(newValue.Size() == 8);
    _ASSERTE(REG_SIZE == sizeof(void*));

    // Split the new value into high and low parts.
    SIZE_T highPart;
    SIZE_T lowPart;

    memcpy(&lowPart, newValue.StartAddress(), REG_SIZE);
    memcpy(&highPart, (BYTE *)newValue.StartAddress() + REG_SIZE, REG_SIZE);

    // Update the proper registers. SetContextRegister writes through to the
    // active CONTEXT - which is the only source of truth now that REGDISPLAY is gone.
    SetContextRegister(pContext, m_reg1Info.m_kRegNumber, highPart); // throws
    SetContextRegister(pContext, m_reg2Info.m_kRegNumber, lowPart); // throws
} // RegRegValueHome::SetEnregisteredValue

// RegRegValueHome::GetEnregisteredValue
// Gets an enregistered value and returns it to the caller (see EnregisteredValueHome::GetEnregisteredValue
// for full header comment)
void RegRegValueHome::GetEnregisteredValue(MemoryRange valueOutBuffer)
{
    TADDR highWord = 0;
    TADDR lowWord = 0;
    IfFailThrow(m_pFrame->ReadContextRegister(m_reg1Info.m_kRegNumber, &highWord));
    IfFailThrow(m_pFrame->ReadContextRegister(m_reg2Info.m_kRegNumber, &lowWord));

    _ASSERTE(sizeof(highWord) + sizeof(lowWord) == valueOutBuffer.Size());

    memcpy(valueOutBuffer.StartAddress(), &lowWord, sizeof(lowWord));
    memcpy((BYTE *)valueOutBuffer.StartAddress() + sizeof(lowWord), &highWord, sizeof(highWord));

} // RegRegValueHome::GetEnregisteredValue


// ----------------------------------------------------------------------------
// RegMemValueHome member function implementations
// ----------------------------------------------------------------------------

// initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
// instance of a derived class of EnregisteredValueHome (see EnregisteredValueHome::CopyToIPCEType for full
// header comment)
void RegMemValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_REGMEM;
    pRegAddr->reg1 = m_reg1Info.m_kRegNumber;
    pRegAddr->reg1Addr = m_reg1Info.m_regAddr;
    pRegAddr->reg1Value = m_reg1Info.m_regValue;
    pRegAddr->addr = m_memAddr;
} // RegMemValueHome::CopyToIPCEType

// RegMemValueHome::SetEnregisteredValue
// set a remote enregistered location to a new value (see EnregisteredValueHome::SetEnregisteredValue
// for full header comment)
void RegMemValueHome::SetEnregisteredValue(MemoryRange newValue, BYTE * pContext, bool fIsSigned)
{
    _ASSERTE(newValue.Size() == REG_SIZE >> 1); // make sure we have bytes for two registers
    _ASSERTE(REG_SIZE == sizeof(void*));

    // Split the new value into high and low parts.
    SIZE_T highPart;
    SIZE_T lowPart;

    memcpy(&lowPart, newValue.StartAddress(), REG_SIZE);
    memcpy(&highPart, (BYTE *)newValue.StartAddress() + REG_SIZE, REG_SIZE);

    // Update the proper registers.
    SetContextRegister(pContext, m_reg1Info.m_kRegNumber, highPart); // throws

    _ASSERTE(REG_SIZE == sizeof(lowPart));
    HRESULT hr = m_pFrame->GetProcess()->SafeReadStruct(m_memAddr, &lowPart);
    IfFailThrow(hr);

} // RegMemValueHome::SetEnregisteredValue

// RegMemValueHome::GetEnregisteredValue
// Gets an enregistered value and returns it to the caller (see EnregisteredValueHome::GetEnregisteredValue
// for full header comment)
void RegMemValueHome::GetEnregisteredValue(MemoryRange valueOutBuffer)
{
    // Read the high bits from the register...
    TADDR highBits = 0;
    IfFailThrow(m_pFrame->ReadContextRegister(m_reg1Info.m_kRegNumber, &highBits));

    // ... and the low bits from the remote process
    DWORD lowBits;
    HRESULT hr = m_pFrame->GetProcess()->SafeReadStruct(m_memAddr, &lowBits);
    IfFailThrow(hr);

    _ASSERTE(sizeof(lowBits) + sizeof(highBits) == valueOutBuffer.Size());

    memcpy(valueOutBuffer.StartAddress(), &lowBits, sizeof(lowBits));
    memcpy((BYTE *)valueOutBuffer.StartAddress() + sizeof(lowBits), &highBits, sizeof(highBits));

} // RegMemValueHome::GetEnregisteredValue


// ----------------------------------------------------------------------------
// MemRegValueHome member function implementations
// ----------------------------------------------------------------------------

// initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
// instance of a derived class of EnregisteredValueHome (see EnregisteredValueHome::CopyToIPCEType for full
// header comment)
void MemRegValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_MEMREG;
    pRegAddr->reg1 = m_reg1Info.m_kRegNumber;
    pRegAddr->reg1Addr = m_reg1Info.m_regAddr;
    pRegAddr->reg1Value = m_reg1Info.m_regValue;
    pRegAddr->addr = m_memAddr;
} // MemRegValueHome::CopyToIPCEType

// MemRegValueHome::SetEnregisteredValue
// set a remote enregistered location to a new value (see EnregisteredValueHome::SetEnregisteredValue
// for full header comment)
void MemRegValueHome::SetEnregisteredValue(MemoryRange newValue, BYTE * pContext, bool fIsSigned)
{
    _ASSERTE(newValue.Size() == REG_SIZE << 1); // make sure we have bytes for two registers
    _ASSERTE(REG_SIZE == sizeof(void *));

    // Split the new value into high and low parts.
    SIZE_T highPart;
    SIZE_T lowPart;

    memcpy(&lowPart, newValue.StartAddress(), REG_SIZE);
    memcpy(&highPart, (BYTE *)newValue.StartAddress() + REG_SIZE, REG_SIZE);

    // Update the proper registers.
    SetContextRegister(pContext, m_reg1Info.m_kRegNumber, lowPart); // throws

    _ASSERTE(REG_SIZE == sizeof(highPart));
    HRESULT hr = m_pFrame->GetProcess()->SafeWriteStruct(m_memAddr, &highPart);
    IfFailThrow(hr);
} // MemRegValueHome::SetEnregisteredValue

// MemRegValueHome::GetEnregisteredValue
// Gets an enregistered value and returns it to the caller (see EnregisteredValueHome::GetEnregisteredValue
// for full header comment)
void MemRegValueHome::GetEnregisteredValue(MemoryRange valueOutBuffer)
{
    // Read the high bits from the remote process' memory
    DWORD highBits;
    HRESULT hr = m_pFrame->GetProcess()->SafeReadStruct(m_memAddr, &highBits);
    IfFailThrow(hr);

    // and the low bits from a register
    TADDR lowBits = 0;
    IfFailThrow(m_pFrame->ReadContextRegister(m_reg1Info.m_kRegNumber, &lowBits));

    _ASSERTE(sizeof(lowBits) + sizeof(highBits) == valueOutBuffer.Size());

    memcpy(valueOutBuffer.StartAddress(), &lowBits, sizeof(lowBits));
    memcpy((BYTE *)valueOutBuffer.StartAddress() + sizeof(lowBits), &highBits, sizeof(highBits));

} // MemRegValueHome::GetEnregisteredValue


// ----------------------------------------------------------------------------
// FloatRegValueHome member function implementations
// ----------------------------------------------------------------------------

// initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
// instance of a derived class of EnregisteredValueHome (see EnregisteredValueHome::CopyToIPCEType for full
// header comment)
void FloatRegValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_FLOAT;
    pRegAddr->reg1Addr = (CORDB_ADDRESS)0;
    pRegAddr->floatIndex = m_floatIndex;
} // FloatRegValueHome::CopyToIPCEType

// FloatValueHome::SetEnregisteredValue
// set a remote enregistered location to a new value.
void FloatRegValueHome::SetEnregisteredValue(MemoryRange newValue,
                                             BYTE *      pContext,
                                             bool        fIsSigned)
{
    _ASSERTE((newValue.Size() == 4) || (newValue.Size() == 8));

    // update the thread's floating point stack
    void * valueAddress = (void *) &(m_pFrame->m_pThread->m_floatValues[m_floatIndex]);
    memcpy(valueAddress, newValue.StartAddress(), newValue.Size());
} // FloatValueHome::SetEnregisteredValue

// FloatRegValueHome::GetEnregisteredValue
// Throws E_NOTIMPL for attempts to get an enregistered value for a float register
void FloatRegValueHome::GetEnregisteredValue(MemoryRange valueOutBuffer)
{
    _ASSERTE(!"invalid variable home");
    ThrowHR(E_NOTIMPL);
} // FloatRegValueHome::GetEnregisteredValue


// ============================================================================
// RemoteValueHome implementation
// ============================================================================

// constructor
// Arguments:
//     input: pProcess    - the process to which the value belongs
//            remoteValue - a buffer with the target address of the value and its size
// Note: It's possible a particular instance of CordbGenericValue may have neither a remote address nor a
// register address--FuncEval makes empty GenericValues for literals but for those, we will make a
// RegisterValueHome,so we can assert that we have a non-null remote address here
RemoteValueHome::RemoteValueHome(CordbProcess * pProcess, TargetBuffer remoteValue):
    ValueHome(pProcess),
    m_remoteValue(remoteValue)
{
    _ASSERTE(remoteValue.pAddress != 0);
} // RemoteValueHome::RemoteValueHome

// Gets a value and returns it in dest
// virtual
void RemoteValueHome::GetValue(MemoryRange dest)
{
    _ASSERTE(dest.Size() == m_remoteValue.cbSize);
    _ASSERTE((!m_remoteValue.IsEmpty()) && (dest.StartAddress() != NULL));
    m_pProcess->SafeReadBuffer(m_remoteValue, (BYTE *)dest.StartAddress());
} // RemoteValueHome::GetValue

// Sets a location to the value provided in src
// virtual
void RemoteValueHome::SetValue(MemoryRange src, CordbType * pType)
{
    _ASSERTE(!m_remoteValue.IsEmpty());
    _ASSERTE(src.Size() == m_remoteValue.cbSize);
    _ASSERTE(src.StartAddress() != NULL);
    m_pProcess->SafeWriteBuffer(m_remoteValue, (BYTE *)src.StartAddress());
} // RemoteValueHome::SetValue

// creates an ICDValue for a field or array element or for the value type of a boxed object
// virtual
void RemoteValueHome::CreateInternalValue(CordbType *       pType,
                                          CORDB_ADDRESS     offset,
                                          void *            localAddress,
                                          ULONG32           size,
                                          ICorDebugValue ** ppValue)
{
    // If we're creating an ICDValue for a field added with EnC, the local address will be null, since the field
    // will not be included with the local cached copy of the ICDObjectValue to which the field belongs.
    // This means we need to compute the size for the type of the field, and then determine whether this
    // should also be the size for the localValue we pass to CreateValueByType. The only way we can tell if this
    // is an EnC added field is if the local address is NULL.
    ULONG32 localSize = localAddress != NULL ? size : 0;

    CordbAppDomain * pAppdomain = pType->GetAppDomain();

    CordbValue::CreateValueByType(pAppdomain,
                                  pType,
                                  kUnboxed,
                                  TargetBuffer(m_remoteValue.pAddress + offset, size),
                                  MemoryRange(localAddress, localSize),
                                  NULL, // remote reg
                                  ppValue);  // throws
} // RemoteValueHome::CreateInternalValue

// Gets the value of a field or element of an existing ICDValue instance and returns it in dest
void RemoteValueHome::GetInternalValue(MemoryRange dest, SIZE_T offset)
{
    _ASSERTE((!m_remoteValue.IsEmpty()) && (dest.StartAddress() != NULL));

    m_pProcess->SafeReadBuffer(TargetBuffer(m_remoteValue.pAddress + offset, (ULONG)dest.Size()),
                               (BYTE *)dest.StartAddress());
} // RemoteValueHome::GetInternalValue

// copies the register information from this to a RemoteAddress instance
// virtual
void RemoteValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_NONE;
} // RegisterValueHome::CopyToIPCEType


// ============================================================================
// RegisterValueHome implementation
// ============================================================================

// constructor
// Arguments:
//     input:  pProcess        - process for this value
//             ppRemoteRegAddr - enregistered value information
//
RegisterValueHome::RegisterValueHome(CordbProcess *                pProcess,
                                     EnregisteredValueHomeHolder * ppRemoteRegAddr):
    ValueHome(pProcess)
{
    EnregisteredValueHome * pRemoteRegAddr = ppRemoteRegAddr == NULL ? NULL : ppRemoteRegAddr->GetValue();
    // in the general case, we should have either a remote address or a register address, but FuncEval makes
    // empty GenericValues for literals, so it's possible that we have neither address

    if (pRemoteRegAddr != NULL)
    {
        m_pRemoteRegAddr = pRemoteRegAddr;
        // be sure not to delete the remote register information on exit
        ppRemoteRegAddr->SuppressRelease();
    }
    else
    {
        m_pRemoteRegAddr = NULL;
    }
 } // RegisterValueHome::RegisterValueHome

// clean up resources as necessary
void RegisterValueHome::Clear()
{
    if (m_pRemoteRegAddr != NULL)
    {
        delete m_pRemoteRegAddr;
        m_pRemoteRegAddr = NULL;
    }
} // RegisterValueHome::Clear

// Gets a value and returns it in dest
// virtual
void RegisterValueHome::GetValue(MemoryRange dest)
{
    // FuncEval makes empty CordbGenericValue instances for literals, which will have a RegisterValueHome,
    // but we should not be calling this in that case; we should be able to assert that the register
    // address isn't NULL
    _ASSERTE(m_pRemoteRegAddr != NULL);
    m_pRemoteRegAddr->GetEnregisteredValue(dest); // throws
} // RegisterValueHome::GetValue

// Sets a location to the value provided in src
void RegisterValueHome::SetValue(MemoryRange src, CordbType * pType)
{
    SetEnregisteredValue(src, IsSigned(pType->m_elementType)); // throws
} // RegisterValueHome::SetValue

// creates an ICDValue for a field or array element or for the value type of a boxed object
// virtual
void RegisterValueHome::CreateInternalValue(CordbType *       pType,
                                            CORDB_ADDRESS     offset,
                                            void *            localAddress,
                                            ULONG32           size,
                                            ICorDebugValue ** ppValue)
{
    TargetBuffer remoteValue(PTR_TO_CORDB_ADDRESS((void *)NULL),0);
    EnregisteredValueHomeHolder pRemoteReg(NULL);

    _ASSERTE(m_pRemoteRegAddr != NULL);
    // Remote register address is the same as the parent....
    /*
     * <TODO>
     * nickbe 10/17/2002 07:31:53
     * If this object consists of two register-sized fields, e.g.
     * struct Point
     * {
     *      int x;
     *      int y;
     * };
     * then the variable home of this object is not necessarily the variable
     * home for member data within this object. For example, if we have
     * Point p;
     * and p.x is in a register, while p.y is in memory, then clearly the
     * home of p (RAK_REGMEM) is not the same as the home of p.x (RAK_MEM).
     *
     * Currently the JIT does not split compound objects in this way. It
     * will only split an object that has exactly one field that is twice
     * the size of the register
     * </TODO>
     */
    _ASSERTE(offset == 0);
    pRemoteReg.Assign(m_pRemoteRegAddr->Clone());

    EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

    // create a value for the member field.
    CordbValue::CreateValueByType(pType->GetAppDomain(),
                                  pType,
                                  kUnboxed,
                                  EMPTY_BUFFER,  // remote address
                                  MemoryRange(localAddress, size),
                                  pRegHolder,
                                  ppValue);  // throws
} // RegisterValueHome::CreateInternalValue

// Gets the value of a field or element of an existing ICDValue instance and returns it in dest
// virtual
void RegisterValueHome::GetInternalValue(MemoryRange dest, SIZE_T offset)
{
    // currently, we can't have an enregistered value that has more than one field or element, so
    // there's nothing to do here but ASSERT. If the JIT changes what can be enregistered, we'll have
    // work to do here
    _ASSERTE(!"Compound types are not enregistered--we shouldn't be here");
    ThrowHR(E_INVALIDARG);
} // RegisterValueHome::GetInternalValue


// copies the register information from this to a RemoteAddress instance
// virtual
void RegisterValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    if(m_pRemoteRegAddr != NULL)
    {
        m_pRemoteRegAddr->CopyToIPCEType(pRegAddr);
    }
    else
    {
        pRegAddr->kind = RAK_NONE;
    }
} // RegisterValueHome::CopyToIPCEType

// sets a remote enregistered location to a new value
// Arguments:
//     input: src -       contains the new value
//            fIsSigned - indicates whether the new value is signed (needed for proper extension
// Return value: S_OK on success or CORDBG_E_SET_VALUE_NOT_ALLOWED_ON_NONLEAF_FRAME, CORDBG_E_CONTEXT_UNVAILABLE,
// or HRESULT values from writing process memory
void RegisterValueHome::SetEnregisteredValue(MemoryRange src, bool fIsSigned)
{
    _ASSERTE(m_pRemoteRegAddr != NULL);
    const CordbNativeFrame * frame = m_pRemoteRegAddr->GetFrame();

    // Can't set an enregistered value unless the frame the value was
    // from is also the current leaf frame. This is because we don't
    // track where we get the registers from every frame from.

    if (!frame->IsLeafFrame())
    {
        ThrowHR(CORDBG_E_SET_VALUE_NOT_ALLOWED_ON_NONLEAF_FRAME);
    }

    ULONG32 cbCtx = frame->GetProcess()->GetTargetContextSize();

    // Get the thread's context so we can update it.
    BYTE * cTemp = NULL;
    HRESULT hr = S_OK;
    EX_TRY
    {
        // This may throw, in which case we want to return our own HRESULT.
        hr = frame->m_pThread->GetManagedContext(&cTemp);
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
    {
        // If we failed to get the context, then we must not be in a leaf frame.
        ThrowHR(CORDBG_E_SET_VALUE_NOT_ALLOWED_ON_NONLEAF_FRAME);
    }

    // Work on a local copy so failures in the chain below don't corrupt the
    // thread's cached context. The subclass mutates this buffer to apply the
    // new register value, and SetManagedContext then ships it to the LS and
    // updates the cache.
    NewArrayHolder<BYTE> ctxBuf(new BYTE[cbCtx]);
    memcpy(ctxBuf, cTemp, cbCtx);

    m_pRemoteRegAddr->SetEnregisteredValue(src, ctxBuf, fIsSigned);

    // Set the thread's modified context.
    IfFailThrow(frame->m_pThread->SetManagedContext(ctxBuf, cbCtx));
} // RegisterValueHome::SetEnregisteredValue


// Get an enregistered value from the register display of the native frame
// Arguments:
//     output: dest - buffer will hold the register value
// Note: Throws E_NOTIMPL for attempts to get an enregistered value for a float register
//       or for 64-bit platforms
void RegisterValueHome::GetEnregisteredValue(MemoryRange dest)
{
#if !defined(TARGET_X86)
    _ASSERTE(!"@TODO IA64/AMD64 -- Not Yet Implemented");
    ThrowHR(E_NOTIMPL);
#else // TARGET_X86
    _ASSERTE(m_pRemoteRegAddr != NULL);

    m_pRemoteRegAddr->GetEnregisteredValue(dest); // throws
#endif // !TARGET_X86
} // RegisterValueHome::GetEnregisteredValue

// Is this a signed type or unsigned type?
// Useful to known when we need to sign-extend.
bool RegisterValueHome::IsSigned(CorElementType elementType)
{
    switch (elementType)
    {
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_I:
        return true;

    default:
        return false;
    }
} // RegisterValueHome::IsSigned

// ============================================================================
// HandleValueHome implementation
// ============================================================================

//
CORDB_ADDRESS HandleValueHome::GetAddress()
{
    _ASSERTE((m_pProcess != NULL) && !m_vmObjectHandle.IsNull());
    CORDB_ADDRESS handle = PTR_TO_CORDB_ADDRESS((void *)NULL);
    EX_TRY
    {
        IfFailThrow(m_pProcess->GetDAC()->GetHandleAddressFromVmHandle(m_vmObjectHandle, &handle));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH
    return handle;
}

// Gets a value and returns it in dest
// virtual
void HandleValueHome::GetValue(MemoryRange dest)
{
    _ASSERTE((m_pProcess != NULL) && !m_vmObjectHandle.IsNull());
    CORDB_ADDRESS objPtr = PTR_TO_CORDB_ADDRESS((void *)NULL);
    IfFailThrow(m_pProcess->GetDAC()->GetHandleAddressFromVmHandle(m_vmObjectHandle, &objPtr));

    _ASSERTE(dest.Size() <= sizeof(void *));
    _ASSERTE(dest.StartAddress() != NULL);
    _ASSERTE(objPtr != (CORDB_ADDRESS)NULL);
    m_pProcess->SafeReadBuffer(TargetBuffer(objPtr, sizeof(void *)), (BYTE *)dest.StartAddress());
} // HandleValueHome::GetValue

// Sets a location to the value provided in src
// virtual
void HandleValueHome::SetValue(MemoryRange src, CordbType * pType)
{
    _ASSERTE(!m_vmObjectHandle.IsNull());

    DebuggerIPCEvent event;

    m_pProcess->InitIPCEvent(&event, DB_IPCE_SET_REFERENCE, true, VMPTR_AppDomain::NullPtr());

    event.SetReference.objectRefAddress = (CORDB_ADDRESS)0;
    event.SetReference.vmObjectHandle = m_vmObjectHandle;
    event.SetReference.newReference = PTR_TO_CORDB_ADDRESS(*((void **)src.StartAddress()));

    // Note: two-way event here...
    IfFailThrow(m_pProcess->SendIPCEvent(&event, sizeof(DebuggerIPCEvent)));

    _ASSERTE(event.type == DB_IPCE_SET_REFERENCE_RESULT);

    IfFailThrow(event.hr);
} // HandleValueHome::SetValue

// creates an ICDValue for a field or array element or for the value type of a boxed object
// virtual
void HandleValueHome::CreateInternalValue(CordbType *       pType,
                                          CORDB_ADDRESS     offset,
                                          void *            localAddress,
                                          ULONG32           size,
                                          ICorDebugValue ** ppValue)
{
    _ASSERTE(!"References don't have sub-objects--we shouldn't be here");
    ThrowHR(E_INVALIDARG);

} // HandleValueHome::CreateInternalValue

// Gets the value of a field or element of an existing ICDValue instance and returns it in dest
// virtual
void HandleValueHome::GetInternalValue(MemoryRange dest, SIZE_T offset)
{
    _ASSERTE(!"References don't have sub-objects--we shouldn't be here");
    ThrowHR(E_INVALIDARG);
} // HandleValueHome::GetInternalValue

// copies the register information from this to a RemoteAddress instance
// virtual
void HandleValueHome::CopyToIPCEType(RemoteAddress * pRegAddr)
{
    pRegAddr->kind = RAK_NONE;
} // HandleValueHome::CopyToIPCEType

// ============================================================================
// VCRemoteValueHome implementation
// ============================================================================

// Sets a location to the value provided in src
// Arguments:
//     input: src   - buffer containing the new value to be set--memory for this buffer is owned by the caller
//            pType - type information for the value
//     output: none, but sets the value on success
// Note: Throws CORDBG_E_CLASS_NOT_LOADED or errors from WriteProcessMemory or
//       GetRemoteBuffer on failure
void VCRemoteValueHome::SetValue(MemoryRange src, CordbType * pType)
{
    _ASSERTE(!m_remoteValue.IsEmpty());

 // send a Set Value Class message to the right side with the address of this value class, the address of
 // the new data, and the class of the value class that we're setting.
    DebuggerIPCEvent event;

    // First, we have to make room on the Left Side for the new data for the value class. We allocate
    // memory on the Left Side for this, then write the new data across. The Set Value Class message will
    // free the buffer when its done.
    void *buffer = NULL;
    IfFailThrow(m_pProcess->GetAndWriteRemoteBuffer(NULL,
                                                    m_remoteValue.cbSize,
                                                    CORDB_ADDRESS_TO_PTR(src.StartAddress()),
                                                    &buffer));

    // Finally, send over the Set Value Class message.
    m_pProcess->InitIPCEvent(&event, DB_IPCE_SET_VALUE_CLASS, true, VMPTR_AppDomain::NullPtr());
    event.SetValueClass.oldData = m_remoteValue.pAddress;
    event.SetValueClass.newData = PTR_TO_CORDB_ADDRESS(buffer);
    IfFailThrow(pType->TypeToBasicTypeData(&event.SetValueClass.type));

    // Note: two-way event here...
    IfFailThrow(m_pProcess->SendIPCEvent(&event, sizeof(DebuggerIPCEvent)));

    _ASSERTE(event.type == DB_IPCE_SET_VALUE_CLASS_RESULT);

    IfFailThrow(event.hr);
} // VCRemoteValueHome::SetValue


// ============================================================================
// RefRemoteValueHome implementation
// ============================================================================

// constructor
// Arguments:
//     input:  pProcess        - process for this value
//             remoteValue     - remote location information
//             vmObjHandle     - object handle
RefRemoteValueHome ::RefRemoteValueHome (CordbProcess *                 pProcess,
                                         TargetBuffer                   remoteValue):
   RemoteValueHome(pProcess, remoteValue)
{
    // caller supplies remoteValue, to work w/ Func-eval.
    _ASSERTE((!remoteValue.IsEmpty()) && (remoteValue.cbSize == sizeof (void *)));

} // RefRemoteValueHome::RefRemoteValueHome

// Sets a location to the value provided in src
// Arguments:
//     input: src   - buffer containing the new value to be set--memory for this buffer is owned by the caller
//            pType - type information for the value
//     output: none, but sets the value on success
//     Return Value: S_OK on success or CORDBG_E_CLASS_NOT_LOADED or errors from WriteProcessMemory or
//                   GetRemoteBuffer on failure
void RefRemoteValueHome::SetValue(MemoryRange src, CordbType * pType)
{
    // We had better have a remote address.
    _ASSERTE(!m_remoteValue.IsEmpty());

    // send a Set Reference message to the right side with the address of this reference and whether or not
    // the reference points to a handle.

    // If it's a reference but not a GC root then just we can just treat it like raw data (like a DWORD).
    // This would include things like "int*", and E_T_FNPTR. If it is a GC root, then we need to go over to
    // the LS to update the WriteBarrier.
    if ((pType != NULL) && !pType->IsGCRoot())
    {
            m_pProcess->SafeWriteBuffer(m_remoteValue, (BYTE *)src.StartAddress());
    }
    else
    {
        DebuggerIPCEvent event;

        m_pProcess->InitIPCEvent(&event, DB_IPCE_SET_REFERENCE, true, VMPTR_AppDomain::NullPtr());

        event.SetReference.objectRefAddress = m_remoteValue.pAddress;
        event.SetReference.vmObjectHandle = VMPTR_OBJECTHANDLE::NullPtr();
        event.SetReference.newReference = PTR_TO_CORDB_ADDRESS(*((void **)src.StartAddress()));

        // Note: two-way event here...
        IfFailThrow(m_pProcess->SendIPCEvent(&event, sizeof(DebuggerIPCEvent)));

        _ASSERTE(event.type == DB_IPCE_SET_REFERENCE_RESULT);

        IfFailThrow(event.hr);
    }
} // RefRemoteValueHome::SetValue

// ============================================================================
// RefValueHome implementation
// ============================================================================

// constructor
// Only one of the location types should be non-NULL, but we pass all of them to the
// constructor so we can instantiate m_pHome correctly.
// Arguments:
//     input:  pProcess        - process to which the value belongs
//             remoteValue     - a target location holding the object reference
//             ppRemoteRegAddr - information about the register that holds the object ref
//             vmObjHandle     - an object handle that holds the object ref
RefValueHome::RefValueHome(CordbProcess *                pProcess,
                           TargetBuffer                  remoteValue,
                           EnregisteredValueHomeHolder * ppRemoteRegAddr,
                           VMPTR_OBJECTHANDLE            vmObjHandle)
{
    if (!remoteValue.IsEmpty())
    {
        NewHolder<ValueHome> pHome(new RefRemoteValueHome(pProcess, remoteValue));
        m_fNullObjHandle = true;
    }
    else if (!vmObjHandle.IsNull())
    {
        NewHolder<ValueHome> pHome(new HandleValueHome(pProcess, vmObjHandle));
        m_fNullObjHandle = false;
    }
    else
    {
        NewHolder<ValueHome> pHome(new RegisterValueHome(pProcess, ppRemoteRegAddr));
        m_fNullObjHandle = true;
    }


} // RefValueHome::RefValueHome

