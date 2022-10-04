// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: DIValue.cpp
//

//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

// copy from a MemoryRange to dest
// Arguments:
//     input:  source - MemoryRange describing the start address and size of the source buffer
//     output: dest   - address of the buffer to which the source buffer is copied
// Note: the buffer for dest must be allocated by the caller and must be large enough to hold the
//       bytes from the source buffer.
void localCopy(void * dest, MemoryRange source)
{
    _ASSERTE(dest != NULL);
    _ASSERTE(source.StartAddress() != NULL);

    memcpy(dest, source.StartAddress(), source.Size());
}
// for an inheritance graph of the ICDValue types, // See file:./ICorDebugValueTypes.vsd for a diagram of the types.

/* ------------------------------------------------------------------------- *
 * CordbValue class
 * ------------------------------------------------------------------------- */

CordbValue::CordbValue(CordbAppDomain *        appdomain,
                       CordbType *             type,
                       CORDB_ADDRESS           id,
                       bool                    isLiteral,
                       NeuterList *            pList)
    : CordbBase(
            ((appdomain != NULL) ? (appdomain->GetProcess()) : (type->GetProcess())),
            (UINT_PTR)id, enumCordbValue),
      m_appdomain(appdomain),
      m_type(type), // implicit InternalAddRef
      //m_sigCopied(false),
      m_size(0),
      m_isLiteral(isLiteral)
{
    HRESULT hr = S_OK;

    _ASSERTE(GetProcess() != NULL);

    // Add to a neuter list. If none is provided, use the ExitProcess list as a default.
    // The main neuter lists of interest here are:
    // - CordbProcess::GetContinueNeuterList() - Shortest. Neuter when the process continues.
    // - CordbAppDomain::GetExitNeuterList() - Middle. Neuter when the AD exits. Since most Values (except globals) are in
    //                                  a specific AD, this almost catches all; and keeps us safe in AD-unload scenarios.
    // - CordbProcess::GetExitNeuterList() - Worst. Doesn't neuter until the process exits (or we detach).
    //                                  This could be a long time.
    if (pList == NULL)
    {
        pList = GetProcess()->GetExitNeuterList();
    }


    EX_TRY
    {
        pList->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
} // CordbValue::CordbValue

CordbValue::~CordbValue()
{
    DTOR_ENTRY(this);

    _ASSERTE(this->IsNeutered());

    _ASSERTE(m_type == NULL);
} // CordbValue::~CordbValue

void CordbValue::Neuter()
{
    m_appdomain = NULL;
    m_type.Clear();

    ValueHome * pValueHome = GetValueHome();
    if (pValueHome != NULL)
    {
        pValueHome->Clear();
    }
    CordbBase::Neuter();
} // CordbValue::Neuter

// Helper for code:CordbValue::CreateValueByType. Create a new instance of CordbGenericValue
// Arguments:
//     input:  pAppdomain      - appdomain to which the value belongs
//             pType           - type of the value
//             remoteValue     - remote address and size of the value
//             localValue      - local address and size of the value
//             ppRemoteRegAddr - register address of the value
//     output: ppValue         - the newly created instance of an ICDValue
// Notes:
//     - only one of the three locations will be non-NULL
//     - Throws
/* static */
void CordbValue::CreateGenericValue(CordbAppDomain *               pAppdomain,
                                    CordbType *                    pType,
                                    TargetBuffer                   remoteValue,
                                    MemoryRange                    localValue,
                                    EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                                    ICorDebugValue**               ppValue)
{
    LOG((LF_CORDB,LL_INFO100000,"CV::CreateValueByType CreateGenericValue\n"));
    RSSmartPtr<CordbGenericValue> pGenValue;
    // A generic value
    // By using a RSSmartPtr we ensure that in both success and failure cases,
    // this object is cleaned up properly (deleted or not depending on ref counts).
    // Specifically, the object has probably been placed on a neuter list so we
    // can't delete it (but this is a detail we shouldn't rely on)
    pGenValue.Assign(new CordbGenericValue(pAppdomain,
                                           pType,
                                           remoteValue,
                                           ppRemoteRegAddr));

    pGenValue->Init(localValue); // throws

    pGenValue->AddRef();
    *ppValue = (ICorDebugValue *)(ICorDebugGenericValue *)pGenValue;
} // CordbValue::CreateGenericValue

// create a new instance of CordbVCObjectValue or CordbReferenceValue
// Arguments:
//     input:  pAppdomain      - appdomain to which the value belongs
//             pType           - type of the value
//             boxed           - indicates whether the value is boxed
//             remoteValue     - remote address and size of the value
//             localValue      - local address and size of the value
//             ppRemoteRegAddr - register address of the value
//     output: ppValue         - the newly created instance of an ICDValue
// Notes:
//     - only one of the three locations will be non-NULL
//     - Throws error codes from reading process memory
/* static */
void CordbValue::CreateVCObjOrRefValue(CordbAppDomain *               pAppdomain,
                                       CordbType *                    pType,
                                       bool                           boxed,
                                       TargetBuffer                   remoteValue,
                                       MemoryRange                    localValue,
                                       EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                                       ICorDebugValue**               ppValue)

{
    HRESULT hr = S_OK;
    LOG((LF_CORDB,LL_INFO1000000,"CV::CreateValueByType Creating ReferenceValue\n"));

    // We either have a boxed or unboxed value type, or we have a value that's not a value type.
    // For an unboxed value type, we'll create an instance of CordbVCObjectValue. Otherwise, we'll
    // create an instance of CordbReferenceValue.

	// do we have a value type?
    bool isVCObject = pType->IsValueType(); // throws

    if (!boxed && isVCObject)
    {
        RSSmartPtr<CordbVCObjectValue> pVCValue(new CordbVCObjectValue(pAppdomain,
                                                                       pType,
                                                                       remoteValue,
                                                                       ppRemoteRegAddr));

        IfFailThrow(pVCValue->Init(localValue));

        pVCValue->AddRef();
        *ppValue = (ICorDebugValue*)(ICorDebugObjectValue*)pVCValue;
    }
    else
    {
        // either the value is boxed or it's not a value type
        RSSmartPtr<CordbReferenceValue> pRef;
        hr = CordbReferenceValue::Build(pAppdomain,
                                        pType,
                                        remoteValue,
                                        localValue,
                                        VMPTR_OBJECTHANDLE::NullPtr(),
                                        ppRemoteRegAddr, // Home
                                        &pRef);
        IfFailThrow(hr);
        hr = pRef->QueryInterface(__uuidof(ICorDebugValue), (void**)ppValue);
        _ASSERTE(SUCCEEDED(hr));
    }
} // CordbValue::CreateVCObjOrRefValue

//
// Create the proper ICDValue instance based on the given element type.
// Arguments:
//     input:  pAppdomain      - appdomain to which the value belongs
//             pType           - type of the value
//             boxed           - indicates whether the value is boxed
//             remoteValue     - remote address and size of the value
//             localValue      - local address and size of the value
//             ppRemoteRegAddr - register address of the value
//     output: ppValue         - the newly created instance of an ICDValue
// Notes:
//     - Only one of the three locations, remoteValue, localValue or ppRemoteRegAddr, will be non-NULL.
//     - Throws.
/*static*/ void CordbValue::CreateValueByType(CordbAppDomain *               pAppdomain,
                                              CordbType *                    pType,
                                              bool                           boxed,
                                              TargetBuffer                   remoteValue,
                                              MemoryRange                    localValue,
                                              EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                                              ICorDebugValue**               ppValue)
{
    INTERNAL_SYNC_API_ENTRY(pAppdomain->GetProcess()); //

    // We'd really hope that our callers give us a valid appdomain, but in case
    // they don't, we'll fail gracefully.
    if ((pAppdomain != NULL) && pAppdomain->IsNeutered())
    {
        STRESS_LOG1(LF_CORDB, LL_EVERYTHING, "CVBT using neutered AP, %p\n", pAppdomain);
        ThrowHR(E_INVALIDARG);
    }

    LOG((LF_CORDB,LL_INFO100000,"CV::CreateValueByType\n"));

    *ppValue = NULL;

    switch(pType->m_elementType)
    {
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
        {
            CreateGenericValue(pAppdomain, pType, remoteValue, localValue, ppRemoteRegAddr, ppValue); // throws
            break;
        }

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_TYPEDBYREF:
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_FNPTR:
        {
            CreateVCObjOrRefValue(pAppdomain, pType, boxed, remoteValue, localValue, ppRemoteRegAddr, ppValue); // throws
            break;
        }

    default:
        _ASSERTE(!"Bad value type!");
        ThrowHR(E_FAIL);
    }
} // CordbValue::CreateValueByType

// Create the proper ICDValue instance based on the given remote heap object
// Arguments:
//   pAppDomain - the app domain the remote object is in
//   vmObj - the remote object to get an ICDValue for
ICorDebugValue* CordbValue::CreateHeapValue(CordbAppDomain* pAppDomain, VMPTR_Object vmObj)
{
    // Create a temporary reference and dereference it to construct the heap value we want.
    RSSmartPtr<CordbReferenceValue> pRefValue(CordbValue::CreateHeapReferenceValue(pAppDomain, vmObj));
    ICorDebugValue* pExtValue;
    IfFailThrow(pRefValue->Dereference(&pExtValue));
    return pExtValue;
}

CordbReferenceValue* CordbValue::CreateHeapReferenceValue(CordbAppDomain* pAppDomain, VMPTR_Object vmObj)
{
    IDacDbiInterface* pDac = pAppDomain->GetProcess()->GetDAC();

    TargetBuffer objBuffer = pDac->GetObjectContents(vmObj);
    VOID* pRemoteAddr = CORDB_ADDRESS_TO_PTR(objBuffer.pAddress);
    // This creates a local reference that has a remote address in it. Ie &pRemoteAddr is an address
    // in the host address space and pRemoteAddr is an address in the target.
    MemoryRange localReferenceDescription(&pRemoteAddr, sizeof(pRemoteAddr));
    RSSmartPtr<CordbReferenceValue> pRefValue;
    IfFailThrow(CordbReferenceValue::Build(pAppDomain,
                                           NULL,
                                           EMPTY_BUFFER,
                                           localReferenceDescription,
                                           VMPTR_OBJECTHANDLE::NullPtr(),
                                           NULL,
                                           &pRefValue));

    return pRefValue;
}

// Gets the size om bytes of a value from its type. If the value is complex, we assume it is represented as
// a reference, since this is called for values that have been found on the stack, as an element of an
// array (represented as CordbArrayValue) or field of an object (CordbObjectValue) or the result of a
// func eval. For unboxed value types, we get the size of the entire value (it is not represented as a
// reference).
// Examples:
// - int on the stack
//         => sizeof(int)
// - int as a field in an object on the heap
//         =>sizeof(int)
// - Boxed int on the heap
//         => size of a pointer
// - Class Point { int x; int y};  // class will have a method table / object header which may increase size.
//         => size of a pointer
// - Struct Point {int x; int y; };   // unboxed struct may not necessarily have the object header.
//         => 2 * sizeof(int)
// - List<int>
//         => size of a pointer
// Arguments: pType  - the type of the value
//            boxing - indicates whether the value is boxed or not
// Return Value: the size of the value
// Notes: Throws
//        In general, this returns the unboxed size of the value, but if we have a type
//        that represents a non-generic and it's not an unboxed value type, we know that
//        it will be represented as a reference, so we return the size of a pointer instead.
/* static */
ULONG32 CordbValue::GetSizeForType(CordbType * pType, BoxedValue boxing)
{
    ULONG32 size = 0;

    switch(pType->m_elementType)
    {
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:        pType->GetUnboxedObjectSize(&size);                     break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_FNPTR: {
                                    bool isUnboxedVCObject = false;

                                    if (boxing == kUnboxed)
                                    {
                                        isUnboxedVCObject = pType->IsValueType(); // throws
                                    }
                                    if (!isUnboxedVCObject)
                                    {
                                        // if it's not an unboxed value type (we're in the case
                                        // for compound types), then it's a reference
                                        // and we just want to return the size of a pointer
                                        size = sizeof(void *);
                                    }
                                    else
                                    {
                                        pType->GetUnboxedObjectSize(&size);
                                    }
                                 }                                                          break;

        default:
            _ASSERTE(!"Bad value type!");
}
    return size;
} // CordbValue::GetSizeForType


HRESULT CordbValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
{
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugValueBreakpoint **);

    return E_NOTIMPL;
} // CordbValue::CreateBreakpoint

// gets the exact type of a value
// Arguments:
//     input:  none (uses m_type field)
//     output: ppType - an instance of ICDType representing the exact type of the value
// Return Value:
HRESULT CordbValue::GetExactType(ICorDebugType **ppType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppType, ICorDebugType **);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *ppType = static_cast<ICorDebugType*> (m_type);

    if (*ppType != NULL)
        (*ppType)->AddRef();

    return S_OK;
} // CordbValue::GetExactType

// CreateHandle for a heap object.
// @todo: How to prevent this being called by non-heap object?
// Arguments:
//     input:  handleType - type of the handle to be created
//     output: ppHandle   - on success, the newly created handle
// Return Value: S_OK on success or E_INVALIDARG, E_OUTOFMEMORY, or CORDB_E_HELPER_MAY_DEADLOCK
HRESULT CordbValue::InternalCreateHandle(CorDebugHandleType      handleType,
    ICorDebugHandleValue ** ppHandle)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    LOG((LF_CORDB,LL_INFO1000,"CV::CreateHandle\n"));

    DebuggerIPCEvent      event;
    CordbProcess              *process;

    // @dbgtodo- , as part of inspection, convert this path to throwing.
    if (ppHandle == NULL)
    {
        return E_INVALIDARG;
    }

    *ppHandle = NULL;

    switch (handleType)
    {
        case HANDLE_STRONG:
        case HANDLE_WEAK_TRACK_RESURRECTION:
        case HANDLE_PINNED:
            break;
        default:
            return E_INVALIDARG;
    }


    // Create the ICorDebugHandleValue object
    RSInitHolder<CordbHandleValue> pHandle(new (nothrow) CordbHandleValue(m_appdomain, m_type, handleType) );

    if (pHandle == NULL)
    {
        return E_OUTOFMEMORY;
    }

    // Send the event to create the handle.
    process = m_appdomain->GetProcess();
    _ASSERTE(process != NULL);

    process->InitIPCEvent(&event,
                          DB_IPCE_CREATE_HANDLE,
                          true,
                          m_appdomain->GetADToken());

    CORDB_ADDRESS addr = GetValueHome() != NULL ? GetValueHome()->GetAddress() : NULL;
    event.CreateHandle.objectToken = CORDB_ADDRESS_TO_PTR(addr);
    event.CreateHandle.handleType = handleType;

    // Note: two-way event here...
    HRESULT hr = process->SendIPCEvent(&event, sizeof(DebuggerIPCEvent));
    hr = WORST_HR(hr, event.hr);

    if (SUCCEEDED(hr))
    {
        _ASSERTE(event.type == DB_IPCE_CREATE_HANDLE_RESULT);

        // Initialize the handle value object.
        hr = pHandle->Init(event.CreateHandleResult.vmObjectHandle);
    }

    if (!SUCCEEDED(hr))
    {
        // Free the handle from the left-side.
        pHandle->Dispose();

        // The RSInitHolder will neuter and delete it.
        return hr;
    }

    // Pass out the new handle value object.
    pHandle.TransferOwnershipExternal(ppHandle);

    return S_OK;
}   // CordbValue::InternalCreateHandle

/* ------------------------------------------------------------------------- *
 * Generic Value class
 * ------------------------------------------------------------------------- */

//
// CordbGenericValue constructor that builds a generic value from
// a remote address or register.
// Arguments:
//     input: pAppdomain      - the app domain to which the value belongs
//            pType           - the type of the value
//            remoteValue     - buffer (and size) of the remote location where
//                              the value resides. This may be NULL if the value
//                              is enregistered.
//            ppRemoteRegAddr - information describing the register in which the
//                              value resides. This may be NULL--only one of
//                              ppRemoteRegAddr and remoteValue will be non-NULL,
//                              depending on whether the value is in a register or
//                              memory.
CordbGenericValue::CordbGenericValue(CordbAppDomain *              pAppdomain,
                                     CordbType *                   pType,
                                     TargetBuffer                  remoteValue,
                                     EnregisteredValueHomeHolder * ppRemoteRegAddr)
    : CordbValue(pAppdomain, pType, remoteValue.pAddress, false),
      m_pValueHome(NULL)
{
    _ASSERTE(pType->m_elementType != ELEMENT_TYPE_END);
    _ASSERTE(pType->m_elementType != ELEMENT_TYPE_VOID);
    _ASSERTE(pType->m_elementType < ELEMENT_TYPE_MAX);

    // We can fill in the size now for generic values.
    ULONG32 size = 0;
    HRESULT hr;
    hr = pType->GetUnboxedObjectSize(&size);
    _ASSERTE (!FAILED(hr));
    m_size = size;

    // now instantiate the value home
    NewHolder<ValueHome> pHome(NULL);
    if (remoteValue.IsEmpty())
    {
        pHome = (new RegisterValueHome(pAppdomain->GetProcess(), ppRemoteRegAddr));
    }
    else
    {
        pHome = (new RemoteValueHome(pAppdomain->GetProcess(), remoteValue));
    }
    m_pValueHome = pHome.GetValue(); // throws
    pHome.SuppressRelease();
} // CordbGenericValue::CordbGenericValue

//
// CordbGenericValue constructor that builds an empty generic value
// from just an element type. Used for literal values for func evals
// only.
// Arguments:
//     input: pType - the type of the value
CordbGenericValue::CordbGenericValue(CordbType * pType)
    : CordbValue(NULL, pType, NULL, true),
      m_pValueHome(NULL)
{
    // The only purpose of a literal value is to hold a RS literal value.
    ULONG32 size = 0;
    HRESULT hr;
    hr = pType->GetUnboxedObjectSize(&size);
    _ASSERTE (!FAILED(hr));
    m_size = size;

    memset(m_pCopyOfData, 0, m_size);

    // there is no value home for a literal so we leave it as NULL
} // CordbGenericValue::CordbGenericValue

// destructor
CordbGenericValue::~CordbGenericValue()
{
    if (m_pValueHome != NULL)
    {
        delete m_pValueHome;
        m_pValueHome = NULL;
}
} // CordbGenericValue::~CordbGenericValue

HRESULT CordbGenericValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugGenericValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugGenericValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbGenericValue::QueryInterface

//
// initialize a generic value by copying the necessary data, either
// from the remote process or from another value in this process.
// Argument:
//     input: localValue - RS location of value to be copied. This could be NULL or it
//            could be a field from the cached copy of a CordbVCObjectValue or CordbObjectValue
//            instance or an element from the cached copy of a CordbArrayValue instance
// Note: Throws error codes from reading process memory
void CordbGenericValue::Init(MemoryRange localValue)
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess());

    if(!m_isLiteral)
    {
        // If neither localValue.StartAddress nor m_remoteValue.pAddress are set, then all that means
        // is that we've got a pre-initialized 64-bit value.
        if (localValue.StartAddress() != NULL)
        {
            // Copy the data out of the local address space.
            localCopy(m_pCopyOfData, localValue);
        }
        else
        {
            m_pValueHome->GetValue(MemoryRange(m_pCopyOfData, m_size)); // throws
        }
    }
} // CordbGenericValue::Init

// gets the value (i.e., number, boolean or pointer value) for this instance of CordbGenericValue
// Arguments:
//    output: pTo - the starting address of a buffer in which the value will be written. This buffer must
//                  be guaranteed by the caller to be large enough to hold the value. There is no way for
//                  us to check here if it is. This must be non-NULL.
// Return Value: S_OK on success or E_INVALIDARG if the pTo is NULL
HRESULT CordbGenericValue::GetValue(void *pTo)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pTo, BYTE, m_size, false, true);

    _ASSERTE(m_pCopyOfData != NULL);
    // Copy out the value
    memcpy(pTo, m_pCopyOfData, m_size);

    return S_OK;
} // CordbGenericValue::GetValue

// Sets the value of this instance of CordbGenericValue
// Arguments:
//     input: pFrom - pointer to a buffer holding the new value. We assume this is the same size as the
//                    original value; we have no way to check. This must be non-NULL.
// Return Value: S_OK on success or E_INVALIDARG if the pFrom is NULL
HRESULT CordbGenericValue::SetValue(void *pFrom)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pFrom, BYTE, m_size, true, false);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // We only need to send to the left side to update values that are
    // object references. For generic values, we can simply do a write
    // memory.

    EX_TRY
    {
        if(!m_isLiteral)
        {
            m_pValueHome->SetValue(MemoryRange(pFrom, m_size), m_type);  // throws
        }
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    // That worked, so update the copy of the value we have in
    // m_copyOfData.
    memcpy(m_pCopyOfData, pFrom, m_size);

    return hr;
} // CordbGenericValue::SetValue

// copies the value from this instance of CordbGenericValue iff the value represents a literal
// Arguments:
//     output: pBuffer - pointer to the beginning of a caller-allocated buffer.This buffer must
//                  be guaranteed by the caller to be large enough to hol
//                  d the value. There is no way for
//                  us to check here if it is. This must be non-NULL.
// Return Value: true iff this is a literal value and pBuffer is a valid writeable address
bool CordbGenericValue::CopyLiteralData(BYTE *pBuffer)
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess());
    _ASSERTE(pBuffer != NULL);

    // If this is a RS fabrication, copy the literal data into the
    // given buffer and return true.
    if (m_isLiteral)
    {
        _ASSERTE(m_size <= 8);
        memcpy(pBuffer, m_pCopyOfData, m_size);
        return true;
    }
    else
        return false;
} // CordbGenericValue::CopyLiteralData

/* ------------------------------------------------------------------------- *
 * Reference Value class
 * ------------------------------------------------------------------------- */

// constructor
// Arguments:
//     input: pAppdomain      - appdomain to which the value belongs
//            pType           - the type of the referent (the object pointed to)
//            localValue      - the RS address and size of the buffer from which the reference
//                              will be copied. This will be NULL if either remoteValue,
//                              ppRemoteRegAddr or vmObjectHandle is non-NULL. Otherwise, it will
//                              point into the local cached copy of another instance of ICDValue
//            remoteValue     - the LS address and size of the buffer from which the reference
//                              will be copied. This will be NULL if either localValue,
//                              ppRemoteRegAddr, or vmObjectHandle is non-NULL.
//            ppRemoteRegAddr - information about the register location of the buffer from which
//                              the reference will be copied. This will be NULL if either localValue,
//                              remoteValue, or vmObjectHandle is non-NULL.
//            vmObjectHandle  - a LS object handle holding the reference. This will be NULL if either
//                              localValue, remoteValue, or ppRemoteRegAddr is non-NULL.
// Note: this may throw OOM
CordbReferenceValue::CordbReferenceValue(CordbAppDomain *              pAppdomain,
                                         CordbType *                   pType,
                                         MemoryRange                   localValue,
                                         TargetBuffer                  remoteValue,
                                         EnregisteredValueHomeHolder * ppRemoteRegAddr,
                                         VMPTR_OBJECTHANDLE            vmObjectHandle)
    : CordbValue(pAppdomain, pType, remoteValue.pAddress, false,
            // We'd like to change this to be a ContinueList so it gets neutered earlier,
            // but it may be a breaking change
            pAppdomain->GetSweepableExitNeuterList()),

      m_realTypeOfTypedByref(NULL)
{
    memset(&m_info, 0, sizeof(m_info));

    LOG((LF_CORDB,LL_EVERYTHING,"CRV::CRV: this:0x%x\n",this));
    m_size = sizeof(void *);

    // now instantiate the value home
    NewHolder<ValueHome> pHome(NULL);

    if (!vmObjectHandle.IsNull())
    {
        pHome = (new HandleValueHome(pAppdomain->GetProcess(), vmObjectHandle));
        m_valueHome.SetObjHandleFlag(false);
    }

    else if (remoteValue.IsEmpty())
    {
        pHome = (new RegisterValueHome(pAppdomain->GetProcess(), ppRemoteRegAddr));
        m_valueHome.SetObjHandleFlag(true);

    }
    else
    {
        pHome = (new RefRemoteValueHome(pAppdomain->GetProcess(), remoteValue));
}
    m_valueHome.m_pHome = pHome.GetValue();  // throws
    pHome.SuppressRelease();
} // CordbReferenceValue::CordbReferenceValue

// CordbReferenceValue constructor that builds an empty reference value
// from just an element type. Used for literal values for func evals
// only.
// Arguments:
//     input: pType - the type of the value
CordbReferenceValue::CordbReferenceValue(CordbType * pType)
    : CordbValue(NULL, pType, NULL, true, pType->GetAppDomain()->GetSweepableExitNeuterList())
{
    memset(&m_info, 0, sizeof(m_info));

    // The only purpose of a literal value is to hold a RS literal value.
    m_size = sizeof(void*);

    // there is no value home for a literal
    m_valueHome.m_pHome = NULL;
} // CordbReferenceValue::CordbReferenceValue

// copies the value from this instance of CordbReferenceValue iff the value represents a literal
// Arguments:
//     output: pBuffer - pointer to the beginning of a caller-allocated buffer.This buffer must
//                  be guaranteed by the caller to be large enough to hold the value.
//                  There is no way for us to check here if it is. This must be non-NULL.
// Return Value: true iff this is a literal value and pBuffer is a valid writeable address
bool CordbReferenceValue::CopyLiteralData(BYTE *pBuffer)
{
    _ASSERTE(pBuffer != NULL);

    // If this is a RS fabrication, then its a null reference.
    if (m_isLiteral)
    {
        void *n = NULL;
        memcpy(pBuffer, &n, sizeof(n));
        return true;
    }
    else
        return false;
} // CordbReferenceValue::CopyLiteralData

// destructor
CordbReferenceValue::~CordbReferenceValue()
{
    DTOR_ENTRY(this);

    LOG((LF_CORDB,LL_EVERYTHING,"CRV::~CRV: this:0x%x\n",this));

    _ASSERTE(IsNeutered());
} // CordbReferenceValue::~CordbReferenceValue

void CordbReferenceValue::Neuter()
{
    if (m_valueHome.m_pHome != NULL)
    {
        m_valueHome.m_pHome->Clear();
        delete m_valueHome.m_pHome;
        m_valueHome.m_pHome = NULL;
    }

    m_realTypeOfTypedByref = NULL;
    CordbValue::Neuter();
} // CordbReferenceValue::Neuter


HRESULT CordbReferenceValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugReferenceValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugReferenceValue)
    {
        *pInterface = static_cast<ICorDebugReferenceValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugReferenceValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbReferenceValue::QueryInterface

// gets the type of the referent of the object ref
// Arguments:
//     output: pType - the type of the value. The caller must guarantee that pType is non-null.
// Return Value: S_OK on success, E_INVALIDARG on failure
HRESULT CordbReferenceValue::GetType(CorElementType *pType)
{
    LIMITED_METHOD_CONTRACT;

    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);

    if( m_type == NULL )
    {
        // We may not have a CordbType if we were created from a GC handle to NULL
        _ASSERTE( m_info.objTypeData.elementType == ELEMENT_TYPE_CLASS );
        _ASSERTE(!m_valueHome.ObjHandleIsNull());
        _ASSERTE( m_info.objRef == NULL );
        *pType = m_info.objTypeData.elementType;
    }
    else
    {
        // The element type stored in both places should match
        _ASSERTE( m_info.objTypeData.elementType == m_type->m_elementType );
        *pType = m_type->m_elementType;
    }

    return S_OK;
} // CordbReferenceValue::GetType

// gets the remote (LS) address of the reference. This may return NULL if the
// reference is a literal or resides in a register.
// Arguments:
//     output: pAddress - the LS location of the reference. The caller must guarantee pAddress is non-null,
//                        but the contents may be null after the call if the reference is enregistered or is
//                        the value of a field or element of some other Cordb*Value instance.
// Return Value: S_OK on success or E_INVALIDARG if pAddress is null
HRESULT CordbReferenceValue::GetAddress(CORDB_ADDRESS *pAddress)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);

    *pAddress = m_valueHome.m_pHome ? m_valueHome.m_pHome->GetAddress() : NULL;
    return (S_OK);
}

// Determines whether the reference is null
// Arguments:
//     output - pfIsNull - pointer to a BOOL that will be set to true iff this represents a
//              null reference
// Return  Value: S_OK on success or E_INVALIDARG if pfIsNull is null
HRESULT CordbReferenceValue::IsNull(BOOL * pfIsNull)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pfIsNull, BOOL *);

   if (m_isLiteral || (m_info.objRef == NULL))
        *pfIsNull = TRUE;
    else
        *pfIsNull = FALSE;

    return S_OK;
}

// gets the value (object address) of this CordbReferenceValue
// Arguments:
//     output: pTo - reference value
// Return Value: S_OK on success or E_INVALIDARG if pAddress is null
HRESULT CordbReferenceValue::GetValue(CORDB_ADDRESS *pAddress)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);
    FAIL_IF_NEUTERED(this);

    // Copy out the value, which is simply the value the object reference.
    if (m_isLiteral)
        *pAddress = NULL;
    else
        *pAddress = PTR_TO_CORDB_ADDRESS(m_info.objRef);

    return S_OK;
}

// sets the value of the reference
// Arguments:
//     input: address - the new reference--this must be a LS address
// Return Value: S_OK on success or E_INVALIDARG or  write process memory errors
// Note: We make no effort to ensure that the new reference is of the same type as the old one.
// We simply assume it is. As long as this assumption is correct, we only need to update information about
// the referent if it's a string (its length can change).

// @dbgtodo Microsoft inspection: consider whether it's worthwhile to verify that the type of the new referent is
// the same as the type of the existing one. We'd have to do most of the work for a call to InitRef to do
// this, since we need to know the type of the new referent.
HRESULT CordbReferenceValue::SetValue(CORDB_ADDRESS address)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    HRESULT hr = S_OK;

    // If this is a heap object, ideally we'd prevent violations of AppDomain isolation
    // here.  However, we have no reliable way of determining what AppDomain the address is in.

    // Can't change literal refs.
    if (m_isLiteral)
    {
        return E_INVALIDARG;
    }

    // Either we know the type, or it's a handle to a null value
    _ASSERTE((m_type != NULL) ||
             (!m_valueHome.ObjHandleIsNull() && (m_info.objRef == NULL)));

	EX_TRY
	{
        m_valueHome.m_pHome->SetValue(MemoryRange(&address, sizeof(void *)), m_type); // throws
	}
	EX_CATCH_HRESULT(hr);

    if (SUCCEEDED(hr))
    {
        // That worked, so update the copy of the value we have in
        // our local cache.
        m_info.objRef = CORDB_ADDRESS_TO_PTR(address);


        if (m_info.objTypeData.elementType == ELEMENT_TYPE_STRING)
        {
            // update information about the string
            InitRef(MemoryRange(&m_info.objRef, sizeof (void *)));
        }

        // All other data in m_info is no longer valid, and we may have invalidated other
        // ICDRVs at this address.  We have to invalidate all cached debuggee data.
        m_appdomain->GetProcess()->m_continueCounter++;
    }

    return hr;
} // CordbReferenceValue::SetValue

HRESULT CordbReferenceValue::DereferenceStrong(ICorDebugValue **ppValue)
{
    return E_NOTIMPL;
}

// Get a new ICDValue instance to represent the referent of this object ref.
// Arguments:
//     output: ppValue - the new ICDValue instance
// Return Value: S_OK on success or E_INVALIDARG
HRESULT CordbReferenceValue::Dereference(ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    // Can't dereference literal refs.
    if (m_isLiteral)
        return E_INVALIDARG;

    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    if (m_continueCounterLastSync != m_appdomain->GetProcess()->m_continueCounter)
    {
        IfFailRet(InitRef(MemoryRange(NULL, 0)));
    }

    EX_TRY
    {
        // We may know ahead of time (depending on the reference type) if
        // the reference is bad.
        if ((m_info.objRefBad) || (m_info.objRef == NULL))
        {
            ThrowHR(CORDBG_E_BAD_REFERENCE_VALUE);
        }

        hr = DereferenceCommon(m_appdomain, m_type, m_realTypeOfTypedByref, &m_info, ppValue);
    }
    EX_CATCH_HRESULT(hr);
    return hr;

}

//-----------------------------------------------------------------------------
// Common helper to dereferefence.
// Parameters:
//     pAppDomain, pType, pInfo - necessary parameters to create the value
//     pRealTypeOfTypedByref - type for a potential TypedByRef. Can be NULL if we know
//        that we're not a typed-byref (this is true if we're definitely an object handle)
//     ppValue - outparameter for newly created value. This will get an Ext AddRef.
//-----------------------------------------------------------------------------
HRESULT CordbReferenceValue::DereferenceCommon(
    CordbAppDomain * pAppDomain,
    CordbType * pType,
    CordbType * pRealTypeOfTypedByref,
    DebuggerIPCE_ObjectData * pInfo,
    ICorDebugValue **ppValue
)
{
    INTERNAL_SYNC_API_ENTRY(pAppDomain->GetProcess());

    // pCachedObject may be NULL if we're not caching.
    _ASSERTE(pType != NULL);
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(pInfo != NULL);
    _ASSERTE(ppValue != NULL);

    HRESULT hr = S_OK;
    *ppValue = NULL; // just to be safe.

    switch(pType->m_elementType)
    {
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_STRING:
        {
            LOG((LF_CORDB, LL_INFO1000, "DereferenceInternal: type class/object/string\n"));
            // An object value (possibly a string value, too.) If the class of this object is a value class,
            // then we have a reference to a boxed object. So we create a box instead of an object value.
            bool isBoxedVCObject = false;

            if ((pType->m_pClass != NULL) && (pType->m_elementType != ELEMENT_TYPE_STRING))
            {
                EX_TRY
                {
                    isBoxedVCObject = pType->m_pClass->IsValueClass();
                }
                EX_CATCH_HRESULT(hr);
                if (FAILED(hr))
                {
                    return hr;
                }
            }

            if (isBoxedVCObject)
            {
                TargetBuffer remoteValue(PTR_TO_CORDB_ADDRESS(pInfo->objRef), (ULONG)pInfo->objSize);
                EX_TRY
                {
                    RSSmartPtr<CordbBoxValue> pBoxValue(new CordbBoxValue(
                        pAppDomain,
                        pType,
                        remoteValue,
                        (ULONG32)pInfo->objSize,
                        pInfo->objOffsetToVars));
                    pBoxValue->ExternalAddRef();
                    *ppValue = (ICorDebugValue*)(ICorDebugBoxValue*)pBoxValue;
                }
                EX_CATCH_HRESULT(hr);
            }
            else
            {
                RSSmartPtr<CordbObjectValue> pObj;
                TargetBuffer remoteValue(PTR_TO_CORDB_ADDRESS(pInfo->objRef), (ULONG)pInfo->objSize);
                // Note: we call Init() by default when we create (or refresh) a reference value, so we
                // never have to do it again.
                EX_TRY
                {
                    pObj.Assign(new CordbObjectValue(pAppDomain, pType, remoteValue, pInfo));
                    IfFailThrow(pObj->Init());

                    pObj->ExternalAddRef();
                    *ppValue = static_cast<ICorDebugValue*>( static_cast<ICorDebugObjectValue*>(pObj) );
                }
                EX_CATCH_HRESULT(hr);
            } // boxed?

            break;
        }

    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
        {
            LOG((LF_CORDB, LL_INFO1000, "DereferenceInternal: type array/szarray\n"));
            TargetBuffer remoteValue(PTR_TO_CORDB_ADDRESS(pInfo->objRef), (ULONG)pInfo->objSize); // sizeof(void *)?
            EX_TRY
            {
                RSSmartPtr<CordbArrayValue> pArrayValue(new CordbArrayValue(
                    pAppDomain,
                    pType,
                    pInfo,
                    remoteValue));

                IfFailThrow(pArrayValue->Init());

                pArrayValue->ExternalAddRef();
                *ppValue = (ICorDebugValue*)(ICorDebugArrayValue*)pArrayValue;
            }
            EX_CATCH_HRESULT(hr);

            break;
        }

    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
        {
            //_ASSERTE(pInfo->objToken.IsNull()); // can't get this type w/ an object handle

            LOG((LF_CORDB, LL_INFO1000, "DereferenceInternal: type byref/ptr\n"));
            CordbType *ptrType;
            pType->DestUnaryType(&ptrType);

            CorElementType et = ptrType->m_elementType;

            if (et == ELEMENT_TYPE_VOID)
            {
                *ppValue = NULL;
                return CORDBG_S_VALUE_POINTS_TO_VOID;
            }

            TargetBuffer remoteValue(pInfo->objRef, GetSizeForType(ptrType, kUnboxed));
            // Create a value for what this reference points to. Note:
            // this could be almost any type of value.
            EX_TRY
            {
                CordbValue::CreateValueByType(
                    pAppDomain,
                    ptrType,
                    false,
                    remoteValue,
                    MemoryRange(NULL, 0), // local value
                    NULL,
                    ppValue);  // throws
            }
            EX_CATCH_HRESULT(hr);

            break;
        }

    case ELEMENT_TYPE_TYPEDBYREF:
        {
            //_ASSERTE(pInfo->objToken.IsNull()); // can't get this type w/ an object handle
            _ASSERTE(pRealTypeOfTypedByref != NULL);

            LOG((LF_CORDB, LL_INFO1000, "DereferenceInternal: type typedbyref\n"));

            TargetBuffer remoteValue(pInfo->objRef, sizeof(void *));
            // Create the value for what this reference points
            // to.
            EX_TRY
            {
                CordbValue::CreateValueByType(
                    pAppDomain,
                    pRealTypeOfTypedByref,
                    false,
                    remoteValue,
                    MemoryRange(NULL, 0), // local value
                    NULL,
                    ppValue);  // throws
            }
            EX_CATCH_HRESULT(hr);

            break;
        }

    case ELEMENT_TYPE_FNPTR:
        // Function pointers cannot be dereferenced; only the pointer value itself
        // may be inspected--not what it points to.
        *ppValue = NULL;
        return CORDBG_E_VALUE_POINTS_TO_FUNCTION;

    default:
        LOG((LF_CORDB, LL_INFO1000, "DereferenceInternal: Fail!\n"));
        _ASSERTE(!"Bad reference type!");
        hr = E_FAIL;
        break;
    }

    return hr;
}

// static helper to build a CordbReferenceValue from a general variable home.
// We can find the CordbType from the object instance.
HRESULT CordbReferenceValue::Build(CordbAppDomain *          appdomain,
                                   CordbType *               type,
                                   TargetBuffer                  remoteValue,
                                   MemoryRange                   localValue,
                                   VMPTR_OBJECTHANDLE        vmObjectHandle,
                                   EnregisteredValueHomeHolder * ppRemoteRegAddr,
                                   CordbReferenceValue**     ppValue)
{
    HRESULT hr = S_OK;

    // We can find the AD from an object handle (but not a normal object), so the AppDomain may
    // be NULL if if it's an OH.
    //_ASSERTE((appdomain != NULL) || objectRefsInHandles);

    // A reference, possibly to an object or value class
    // Weak by default
    EX_TRY
    {
        RSSmartPtr<CordbReferenceValue> pRefValue(new CordbReferenceValue(appdomain,
                                                                          type,
                                                                          localValue,
                                                                          remoteValue,
                                                                          ppRemoteRegAddr,
                                                                          vmObjectHandle));
        IfFailThrow(pRefValue->InitRef(localValue));

        pRefValue->InternalAddRef();
        *ppValue = pRefValue;
    }
    EX_CATCH_HRESULT(hr)
    return hr;
}

//-----------------------------------------------------------------------------
// Static helper to build a CordbReferenceValue from a GCHandle
// The LS can actually determine an AppDomain from an OBJECTHandles, however, the RS
// should already have this infromation too, so we pass it in.
// We also supply the AppDomain here because it provides the CordbValue with
// process affinity.
// Note that the GC handle may point to a NULL reference, in which case we should still create
// an appropriate ICorDebugReferenceValue for which IsNull returns TRUE.
//-----------------------------------------------------------------------------
HRESULT CordbReferenceValue::BuildFromGCHandle(
    CordbAppDomain *pAppDomain,
    VMPTR_OBJECTHANDLE gcHandle,
    ICorDebugReferenceValue ** pOutRef
)
{
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(pOutRef != NULL);

    CordbProcess * pProc;
    pProc = pAppDomain->GetProcess();
    INTERNAL_SYNC_API_ENTRY(pProc);

    HRESULT hr = S_OK;

    *pOutRef = NULL;

    // Make sure we even have a GC handle.
    // Also, We may have a handle, but its contents may be null.
    if (gcHandle.IsNull())
    {
        // We've seen this assert fire in the wild, but have never gotten a repro.
        // so we'll include a runtime check to avoid the AV.
        _ASSERTE(false || !"We got a bad reference value.");
        return CORDBG_E_BAD_REFERENCE_VALUE;
    }

    // Now that we've got an AppDomain, we can go ahead and create the reference value normally.

    RSSmartPtr<CordbReferenceValue> pRefValue;
    TargetBuffer remoteValue;
    EX_TRY
    {
        remoteValue.Init(pProc->GetDAC()->GetHandleAddressFromVmHandle(gcHandle), sizeof(void *));
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    hr = CordbReferenceValue::Build(
        pAppDomain,
        NULL, // unknown type
        remoteValue, // CORDB_ADDRESS remoteAddress,
        MemoryRange(NULL, 0),
        gcHandle, // objectRefsInHandles,
        NULL, // EnregisteredValueHome * pRemoteRegAddr,
        &pRefValue);

    if (SUCCEEDED(hr))
    {
        pRefValue->QueryInterface(__uuidof(ICorDebugReferenceValue), (void**)pOutRef);
    }

    return hr;
}

// Helper function for SanityCheckPointer. Make an attempt to read memory at the address which is the value
// of the reference.
// Arguments: none
// Notes:
//     - Throws
//     - m_info.objRefBad must be set to true before calling this function. If we throw, we'll
//       never end up setting m_info.objRefBad, but throwing indicates that the reference is
//       indeed bad. Only if we exit normally will we end up setting m_info.objRefBad to false.
void CordbReferenceValue::TryDereferencingTarget()
{
    _ASSERTE(!!m_info.objRefBad == true);
    // First get the referent type
    CordbType * pReferentType;
    m_type->DestUnaryType(&pReferentType);

    // Next get the size
    ULONG32 dataSize, sizeToRead;
    IfFailThrow(pReferentType->GetUnboxedObjectSize(&dataSize));
    if (dataSize <= 0)
        sizeToRead = 1; // Read at least one byte.
    else if (dataSize >= 8)
        sizeToRead = 8; // Read at most eight bytes--this is just a perf improvement. Even if we read
                        // all the bytes, we are only able to determine that we can read those bytes,
                        // we can't really tell if the data we are reading is actually the data we
                        // want.
    else sizeToRead = dataSize;

    // Now see if we can read from the address where the object is supposed to be
    BYTE dummy[8];

    // Get a target buffer with the remote address and size of the object--since we don't know if the
    // address if valid, this could throw or return a size that's complete garbage
    TargetBuffer object(m_info.objRef, sizeToRead);

    // now read target memory. This may throw ...
    GetProcess()->SafeReadBuffer(object, dummy);

} // CordbReferenceValue::TryDereferencingTarget

// Do a sanity check on the pointer which is the value of the object reference. We can't efficiently ensure that
// the pointer is really good, so we settle for a quick check just to make sure the memory at the address is
// readable. We're actually just checking that we can dereference the pointer.
// Arguments:
//     input:  type - the type of the pointer to which the object reference points.
//     output: none, but fills in m_info.objRefBad
// Note: Throws
void CordbReferenceValue::SanityCheckPointer (CorElementType type)
{
    m_info.objRefBad = TRUE;
    if (type != ELEMENT_TYPE_FNPTR)
    {
        // We should never dereference a function pointer, so all references
        // are considered "bad."
        if (m_info.objRef != NULL)
        {
            if (type == ELEMENT_TYPE_PTR)
            {
                // The only way to tell if the reference in PTR is bad or
                // not is to try to deref the thing.
                TryDereferencingTarget();
            }
        } // !m_info.m_basicData.m_vmObject.IsNull()
        // else Null refs are considered "bad".
    } // type != ELEMENT_TYPE_FNPTR

    // we made it without throwing, so we'll assume (perhaps wrongly) that the ref is good
    m_info.objRefBad = FALSE;

} // CordbReferenceValue::SanityCheckPointer

// get information about the reference when it's not an object address but another kind of pointer type:
// ELEMENT_TYPE_BYREF, ELEMENT_TYPE_PTR or ELEMENT_TYPE_FNPTR
// Arguments:
//    input: type       - type of the referent
//           localValue - starting address and length of a local buffer containing the object ref
// Notes:
//     - fills in the m_info field of "this"
//     - Throws (errors from reading process memory)
void CordbReferenceValue::GetPointerData(CorElementType type, MemoryRange localValue)
{
    HRESULT hr = S_OK;
    // Fill in the type since we will not be getting it from the DAC
    m_info.objTypeData.elementType = type;

    // First get the objRef
    if (localValue.StartAddress() != NULL)
    {
        // localValue represents a buffer containing a copy of the objectRef that exists locally. It could be a
        // component of a container type residing within a local cached copy belonging to some other
        // Cordb*Value instance representing the container type. In this case it will be a field, array
        // element, or referent of a different object reference for that other Cordb*Value instance. It
        // could also be a pointer to the value of a local register display of the frame from which this object
        // ref comes.

        // For example, if we have a value class (represented by a CordbVCObject instance) with a field
        // that is an object pointer, localValue will contain a pointer to that field in the local
        // cache of the CordbVCObjectValue instance (CordbVCObjectValue::m_pObjectCopy).

        // Note, though, that pLocalValue holds the address of a target object. We will cache
        // the contents of pLocalValue (the object ref) here for efficiency of read access, but if we
        // want to set the reference later (e.g., we want the object ref to point to NULL instead of an
        // object), we'll have to set the object ref in the target, not our local copy.
        //                     Host memory                                     Target memory
        //                                           ---------------    |
        // CordbVCObjectValue::m_copyOfObject ----> |               |
        //                                          |      ...      |   |
        //                                          |               |
        //                                          |---------------|   |             Object
        //                        localAddress ---> |  object addr  |------------->   --------------
        //                                          |---------------|   |      --->  |              |
        //                                          |      ...      |         |      |              |
        //                                           ---------------    |     |       --------------
        //                                                                    |
        // CordbReferenceValue::m_info.objRef --->   ---------------    |     |
        //                                          | object addr   |---------
        //                                           ---------------    |

        _ASSERTE(localValue.Size() == sizeof(void *));
        localCopy(&(m_info.objRef), localValue);
    }
    else
    {
        // we have a non-local location, so we'll get the value of the ref from its home

        // do some preinitialization in case we get an exception
        EX_TRY
        {
            m_valueHome.m_pHome->GetValue(MemoryRange(&(m_info.objRef), sizeof(void*)));  // throws
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            m_info.objRef = NULL;
            m_info.objRefBad = TRUE;
            ThrowHR(hr);
        }
    }

    EX_TRY
    {
        // If we made it this far, we need to sanity check the pointer--we'll just see if we can
        // read at that address
        SanityCheckPointer(type);
    }
    EX_CATCH_HRESULT(hr); // we don't need to do anything here, m_info.objRefBad will have been set to true

} // CordbReferenceValue::GetPointerData

// Helper function for CordbReferenceValue::GetObjectData: Sets default values for the fields in pObjectData
// before processing begins. Not all will necessarily be initialized during processing.
// Arguments:
//     input:  objectType  - type of the referent of the objRef being examined
//     output: pObjectData - information about the reference to be initialized
void PreInitObjectData(DebuggerIPCE_ObjectData * pObjectData, void * objAddress, CorElementType objectType)
{
    _ASSERTE(pObjectData != NULL);

    memset(pObjectData, 0, sizeof(DebuggerIPCE_ObjectData));
    pObjectData->objRef = objAddress;
    pObjectData->objTypeData.elementType = objectType;

} // PreInitObjectData

// get basic object specific data when a reference points to an object, plus extra data if the object is an
// array or string
// Arguments:
//     input:  pProcess      - process to which the object belongs
//             objectAddress - pointer to the TypedByRef object (this is the value of the object reference
//                             or handle.
//             type          - the type of the object referenced
//             vmAppDomain   - appdomain to which the object belongs
//     output: pInfo         - filled with information about the object to which the TypedByRef refers.
// Note: Throws
/* static */
void CordbReferenceValue::GetObjectData(CordbProcess *            pProcess,
                                        void *                    objectAddress,
                                        CorElementType            type,
                                        VMPTR_AppDomain           vmAppdomain,
                                        DebuggerIPCE_ObjectData * pInfo)
{
    IDacDbiInterface *pInterface = pProcess->GetDAC();
    CORDB_ADDRESS objTargetAddr = PTR_TO_CORDB_ADDRESS(objectAddress);

    // make sure we don't end up with old garbage values in case the reference is bad
    PreInitObjectData(pInfo, objectAddress, type);

    pInterface->GetBasicObjectInfo(objTargetAddr, type, vmAppdomain, pInfo);

    if (!pInfo->objRefBad)
    {
        // for certain referent types, we need a bit more information:
        if (pInfo->objTypeData.elementType == ELEMENT_TYPE_STRING)
        {
            pInterface->GetStringData(objTargetAddr, pInfo);
        }
        else if ((pInfo->objTypeData.elementType == ELEMENT_TYPE_ARRAY) ||
                 (pInfo->objTypeData.elementType == ELEMENT_TYPE_SZARRAY))
        {
            pInterface->GetArrayData(objTargetAddr, pInfo);
        }
    }

} // CordbReferenceValue::GetObjectData

// get information about a TypedByRef object when the reference is the address of a TypedByRef structure.
// Arguments:
//     input:  pProcess    - process to which the object belongs
//             pTypedByRef - pointer to the TypedByRef object (this is the value of the object reference or
//                           handle.
//             type          - the type of the object referenced
//             vmAppDomain - appdomain to which the object belongs
//     output: pInfo       - filled with information about the object to which the TypedByRef refers.
// Note: Throws
/* static */
void CordbReferenceValue::GetTypedByRefData(CordbProcess *            pProcess,
                                            CORDB_ADDRESS             pTypedByRef,
                                            CorElementType            type,
                                            VMPTR_AppDomain           vmAppDomain,
                                            DebuggerIPCE_ObjectData * pInfo)
{

    // make sure we don't end up with old garbage values since we don't set all the values for TypedByRef objects
    PreInitObjectData(pInfo, CORDB_ADDRESS_TO_PTR(pTypedByRef), type);

    // Though pTypedByRef is the value of the object ref represented by an instance of CordbReferenceValue,
    // it is not the address of an object, as we would ordinarily expect. Instead, in the special case of
    // TypedByref objects, it is actually the address of the TypedByRef struct which  contains the
    // type and the object address.

    pProcess->GetDAC()->GetTypedByRefInfo(pTypedByRef, vmAppDomain, pInfo);
} // CordbReferenceValue::GetTypedByRefData

//  get the address of the object referenced
//  Arguments: none
//  Return Value: the address of the object referenced (i.e., the value of the object ref)
//  Note: Throws
void * CordbReferenceValue::GetObjectAddress(MemoryRange localValue)
{
    void * objectAddress;
    if (localValue.StartAddress() != NULL)
    {
        // the object ref comes from a local cached copy
        _ASSERTE(localValue.Size() == sizeof(void *));
        memcpy(&objectAddress, localValue.StartAddress(), localValue.Size());
    }
    else
    {
        _ASSERTE(m_valueHome.m_pHome != NULL);
        m_valueHome.m_pHome->GetValue(MemoryRange(&objectAddress, sizeof(void *)));   // throws
    }
    return objectAddress;
} // CordbReferenceValue::GetObjectAddress

// update type information after initializing -- when we initialize, we may get more exact type information
// than we previously had
// Arguments: none--uses and updates data members
// Note: Throws
void CordbReferenceValue::UpdateTypeInfo()
{
    // If the object type that we got back is different than the one we sent, then it means that we
    // originally had a CLASS and now have something more specific, like a SDARRAY, MDARRAY, or STRING or
    // a constructed type.
    // Update our signature accordingly, which is okay since we always have a copy of our sig. This
    // ensures that the reference's signature accurately reflects what the Runtime knows it's pointing
    // to.
    //
    // GENERICS: do this for all types: for example, an array might have been discovered to be a more
    // specific kind of array (String[] where an Object[] was expected).
    CordbType *newtype;

    IfFailThrow(CordbType::TypeDataToType(m_appdomain, &m_info.objTypeData, &newtype));

    _ASSERTE(newtype->m_elementType != ELEMENT_TYPE_VALUETYPE);
    m_type.Assign(newtype); // implicit Release + AddRef

    // For typed-byref's the act of dereferencing the object also reveals to us
    // what the "real" type of the object is...
    if (m_info.objTypeData.elementType == ELEMENT_TYPE_TYPEDBYREF)
{
        IfFailThrow(CordbType::TypeDataToType(m_appdomain,
                    &m_info.typedByrefInfo.typedByrefType,
                    &m_realTypeOfTypedByref));
    }
} // CordbReferenceValue::UpdateTypeInfo

// Initialize this CordbReferenceValue. This may involve inspecting the LS to get information about the
// referent.
// Arguments:
//     input: localValue - buffer address and size of the RS location of the reference. (This may be NULL
//                         if the reference didn't come from a local cached copy. See
//                         code:CordbReferenceValue::GetPointerData for further explanation of local locations.)
// Return Value: S_OK on success or E_INVALIDARG or  write process memory errors on failure

HRESULT CordbReferenceValue::InitRef(MemoryRange localValue)
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess());

    HRESULT hr = S_OK;
    CordbProcess * pProcess = GetProcess();

    // Simple init needed for literal refs. Literals may have a null process / appdomain ptr.
    if (m_isLiteral)
    {
        _ASSERTE(m_type != NULL);
        m_info.objTypeData.elementType = m_type->m_elementType;
        return hr;
    }

    _ASSERTE((pProcess->GetShim() == NULL) || pProcess->GetSynchronized());

    // If the helper thread is dead, then pretend this is a bad reference.
    if (GetProcess()->m_helperThreadDead)
    {
        m_info.objRef = NULL;
        m_info.objRefBad = TRUE;
        return hr;
    }

    m_continueCounterLastSync = pProcess->m_continueCounter;

    // If no type provided, then it's b/c we're a class and we'll get the type when we get Created.
    CorElementType type = (m_type != NULL) ? (m_type->m_elementType) : ELEMENT_TYPE_CLASS;
    _ASSERTE (type != ELEMENT_TYPE_GENERICINST);
    _ASSERTE (type != ELEMENT_TYPE_VAR);
    _ASSERTE (type != ELEMENT_TYPE_MVAR);

    EX_TRY
    {
        if ((type == ELEMENT_TYPE_BYREF) ||
            (type == ELEMENT_TYPE_PTR) ||
            (type == ELEMENT_TYPE_FNPTR))
        {
            // we know the size is just the size of a pointer, so we can just read process memory to get the
            // information we need
            GetPointerData(type, localValue);
        }
        else // we have to get more information about the object from the DAC
        {
            if (type == ELEMENT_TYPE_TYPEDBYREF)
            {
                _ASSERTE(m_valueHome.m_pHome != NULL);
                GetTypedByRefData(pProcess,
                                  m_valueHome.m_pHome->GetAddress(),
                                  type,
                                  m_appdomain->GetADToken(),
                                  &m_info);
            }
            else
            {
                GetObjectData(pProcess, GetObjectAddress(localValue), type, m_appdomain->GetADToken(), &m_info);
            }

            // if we got (what we believe is probably) a good reference, we should update the type info
            if (!m_info.objRefBad)
            {
                // we may have gotten back a more specific type than we had previously
                UpdateTypeInfo();
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbReferenceValue::InitRef

/* ------------------------------------------------------------------------- *
 * Object Value class
 * ------------------------------------------------------------------------- */


// validate a CordbObjectValue to ensure it hasn't been neutered
#define COV_VALIDATE_OBJECT() do {         \
    BOOL bValid;                           \
    HRESULT hr;                            \
    if (FAILED(hr = IsValid(&bValid)))     \
        return hr;                         \
                                           \
        if (!bValid)                       \
        {                                  \
            return CORDBG_E_INVALID_OBJECT; \
        }                                  \
    }while(0)

// constructor
// Arguments:
//     input:  pAppDomain  - the appdomain to which the object belongs
//             pType       - the type of the object
//             remoteValue - the LS address and size of the object
//             pObjectData - other information about the object, most importantly, the offset to the
//                           fields of the object
CordbObjectValue::CordbObjectValue(CordbAppDomain *          pAppdomain,
                                   CordbType *               pType,
                                   TargetBuffer              remoteValue,
                                   DebuggerIPCE_ObjectData *pObjectData )
    : CordbValue(pAppdomain, pType, remoteValue.pAddress,
                false, pAppdomain->GetProcess()->GetContinueNeuterList()),
      m_info(*pObjectData),
      m_pObjectCopy(NULL), m_objectLocalVars(NULL), m_stringBuffer(NULL),
      m_valueHome(pAppdomain->GetProcess(), remoteValue),
      m_fIsExceptionObject(FALSE), m_fIsRcw(FALSE), m_fIsDelegate(FALSE)
{
    _ASSERTE(pAppdomain != NULL);

    m_size = m_info.objSize;

    HRESULT hr = S_FALSE;

    ALLOW_DATATARGET_MISSING_MEMORY
    (
        hr = IsExceptionObject();
    );

    if (hr == S_OK)
        m_fIsExceptionObject = TRUE;

    hr = S_FALSE;
    ALLOW_DATATARGET_MISSING_MEMORY
    (
        hr = IsRcw();
    );

    if (hr == S_OK)
        m_fIsRcw = TRUE;

    hr = S_FALSE;
    ALLOW_DATATARGET_MISSING_MEMORY
    (
        hr = IsDelegate();
    );

    if (hr == S_OK)
        m_fIsDelegate = TRUE;
} // CordbObjectValue::CordbObjectValue

// destructor
CordbObjectValue::~CordbObjectValue()
{
    DTOR_ENTRY(this);

    _ASSERTE(IsNeutered());
} // CordbObjectValue::~CordbObjectValue

void CordbObjectValue::Neuter()
{
    // Destroy the copy of the object.
    if (m_pObjectCopy != NULL)
    {
        delete [] m_pObjectCopy;
        m_pObjectCopy = NULL;
    }

    CordbValue::Neuter();
} // CordbObjectValue::Neuter

HRESULT CordbObjectValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugObjectValue)
    {
        *pInterface = static_cast<ICorDebugObjectValue*>(this);
    }
    else if (id == IID_ICorDebugObjectValue2)
    {
        *pInterface = static_cast<ICorDebugObjectValue2*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue)
    {
        *pInterface = static_cast<ICorDebugHeapValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue2)
    {
        *pInterface = static_cast<ICorDebugHeapValue2*>(this);
    }
    else if (id == IID_ICorDebugHeapValue3)
    {
        *pInterface = static_cast<ICorDebugHeapValue3*>(this);
    }
    else if (id == IID_ICorDebugHeapValue4)
    {
        *pInterface = static_cast<ICorDebugHeapValue4*>(this);
    }
    else if ((id == IID_ICorDebugStringValue) &&
             (m_info.objTypeData.elementType == ELEMENT_TYPE_STRING))
    {
        *pInterface = static_cast<ICorDebugStringValue*>(this);
    }
    else if (id == IID_ICorDebugExceptionObjectValue && m_fIsExceptionObject)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugExceptionObjectValue*>(this));
    }
    else if (id == IID_ICorDebugComObjectValue && m_fIsRcw)
    {
        *pInterface = static_cast<ICorDebugComObjectValue*>(this);
    }
    else if (id == IID_ICorDebugDelegateObjectValue && m_fIsDelegate)
    {
        *pInterface = static_cast<ICorDebugDelegateObjectValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbObjectValue::QueryInterface

// gets the type of the object
// Arguments:
//     output: pType - the type of the value. The caller must guarantee that pType is non-null.
// Return Value: S_OK on success, E_INVALIDARG on failure
HRESULT CordbObjectValue::GetType(CorElementType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return (CordbValue::GetType(pType));
} // CordbObjectValue::GetType

// gets the size of the object
// Arguments:
//     output: pSize - the size of the value. The caller must guarantee that pSize is non-null.
// Return Value: S_OK on success, E_INVALIDARG on failure
HRESULT CordbObjectValue::GetSize(ULONG32 *pSize)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return (CordbValue::GetSize(pSize));
} // CordbObjectValue::GetSize

// gets the size of the object
// Arguments:
//     output: pSize - the size of the value. The caller must guarantee that pSize is non-null.
// Return Value: S_OK on success, E_INVALIDARG on failure
HRESULT CordbObjectValue::GetSize64(ULONG64 *pSize)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return (CordbValue::GetSize64(pSize));
} // CordbObjectValue::GetSize64


// gets the remote (LS) address of the object. This may return NULL if the
// object is a literal or resides in a register.
// Arguments:
//     output: pAddress - the LS address (the contents should not be null since objects
//                        aren't enregistered nor are they fields or elements of other
//                        types). The caller must ensure that pAddress is not null.
// Return Value: S_OK on success or E_INVALIDARG if pAddress is null
HRESULT CordbObjectValue::GetAddress(CORDB_ADDRESS *pAddress)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    COV_VALIDATE_OBJECT();
    VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);

    *pAddress = m_valueHome.GetAddress();
    return (S_OK);
} // CordbObjectValue::GetAddress

HRESULT CordbObjectValue::CreateBreakpoint(ICorDebugValueBreakpoint ** ppBreakpoint)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    COV_VALIDATE_OBJECT();

    return (CordbValue::CreateBreakpoint(ppBreakpoint));
}

// determine if "this" is still valid (i.e., not neutered)
// Arguments:
//     output: pfIsValid - true iff "this" is still not neutered
// Return Value: S_OK or E_INVALIDARG
HRESULT CordbObjectValue::IsValid(BOOL * pfIsValid)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pfIsValid, BOOL *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // We're neutered on continue, so we're valid up until the time we're neutered
    (*pfIsValid) = TRUE;
    return S_OK;
}

HRESULT CordbObjectValue::CreateRelocBreakpoint(
                                      ICorDebugValueBreakpoint **ppBreakpoint)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugValueBreakpoint **);

   COV_VALIDATE_OBJECT();

   return E_NOTIMPL;
}

/*
* Creates a handle of the given type for this heap value.
*
* Not Implemented In-Proc.
*/
HRESULT CordbObjectValue::CreateHandle(
    CorDebugHandleType handleType,
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(handleType, ppHandle);
}   // CreateHandle

/*
* Creates a pinned handle for this heap value.
*
* Not Implemented In-Proc.
*/
HRESULT CordbObjectValue::CreatePinnedHandle(
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(HANDLE_PINNED, ppHandle);
}   // CreatePinnedHandle

// Get class information for this object
// Arguments:
//    output: ppClass - ICDClass instance for this object
// Return Value:  S_OK if success, CORDBG_E_CLASS_NOT_LOADED, E_INVALIDARG, OOM on failure
HRESULT CordbObjectValue::GetClass(ICorDebugClass **ppClass)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppClass, ICorDebugClass **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    if (m_type->m_pClass == NULL)
    {
        if (FAILED(hr = m_type->Init(FALSE)))
            return hr;
    }

    _ASSERTE(m_type->m_pClass);
    *ppClass = (ICorDebugClass*) m_type->m_pClass;

    if (*ppClass != NULL)
        (*ppClass)->AddRef();

    return hr;
} // CordbObjectValue::GetClass





//-----------------------------------------------------------------------------
//
// Public API to get instance field of the given type in the object and returns an ICDValue for it.
//
// Arguments:
//    pType - The type containing the field token.
//    fieldDef - The field's metadata def.
//    ppValue - OUT: the ICDValue for the field.
//
// Returns:
//   S_OK on success. E_INVALIDARG, CORDBG_E_ENC_HANGING_FIELD, CORDBG_E_FIELD_NOT_INSTANCE or OOM on
//   failure
//
// Notes:
//    This is for instance fields only.
//    Lookup on code:CordbType::GetStaticFieldValue to get static fields.
//    This is generics aware.
HRESULT CordbObjectValue::GetFieldValueForType(ICorDebugType * pType,
                                               mdFieldDef fieldDef,
                                               ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pType, ICorDebugType *);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);

    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    COV_VALIDATE_OBJECT();

    CordbType * pCordbType = NULL;
    HRESULT hr = S_OK;

    EX_TRY
    {
        BOOL fSyncBlockField = FALSE;
        SIZE_T fldOffset;

        //
        // <TODO>@todo: need to ensure that pType is really on the class
        // hierarchy of m_class!!!</TODO>
        //
        if (pType == NULL)
        {
            pCordbType = m_type;
        }
        else
        {
            pCordbType = static_cast<CordbType *>(pType);
        }

        // Validate the token.
        if (pCordbType->m_pClass == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }
        IMetaDataImport * pImport = pCordbType->m_pClass->GetModule()->GetMetaDataImporter();

        if (!pImport->IsValidToken(fieldDef))
        {
            ThrowHR(E_INVALIDARG);
        }

       FieldData * pFieldData;

    #ifdef _DEBUG
        pFieldData = NULL;
    #endif

        hr = pCordbType->GetFieldInfo(fieldDef, &pFieldData);

        // If we couldn't get field info because the field was added with EnC
        if (hr == CORDBG_E_ENC_HANGING_FIELD)
        {
            // The instance field hangs off the syncblock, get its address
            hr = pCordbType->m_pClass->GetEnCHangingField(fieldDef, &pFieldData, this);

            if (SUCCEEDED(hr))
            {
                fSyncBlockField = TRUE;
            }
        }

        if (SUCCEEDED(hr))
        {
            _ASSERTE(pFieldData != NULL);

            if (pFieldData->m_fFldIsStatic)
            {
                ThrowHR(CORDBG_E_FIELD_NOT_INSTANCE);
            }

            // Compute the remote address, too, so that SetValue will work.
            // Note that if pFieldData is a syncBlock field, fldOffset will have been cooked
            // to produce the correct result here.
            _ASSERTE(pFieldData->OkToGetOrSetInstanceOffset());
            fldOffset = pFieldData->GetInstanceOffset();

            CordbModule * pModule = pCordbType->m_pClass->GetModule();

            SigParser sigParser;
            IfFailThrow(pFieldData->GetFieldSignature(pModule, &sigParser));

            CordbType * pFieldType;
            IfFailThrow(CordbType::SigToType(pModule, &sigParser, &(pCordbType->m_inst), &pFieldType));

            ULONG32 size = GetSizeForType(pFieldType, kUnboxed);

            void * localAddr = NULL;
            if (!fSyncBlockField)
            {
                // verify that the field starts and ends before the end of m_pObjectCopy
                _ASSERTE(m_info.objOffsetToVars + fldOffset < m_size);
                _ASSERTE(m_info.objOffsetToVars + fldOffset + size <= m_size);
                localAddr = m_objectLocalVars + fldOffset;
            }

            // pass the computed local field address, but don't claim we have a local addr if the fldOffset
            // has been cooked to point us to a sync block field.
            m_valueHome.CreateInternalValue(pFieldType,
                                            m_info.objOffsetToVars + fldOffset,
                                            localAddr,
                                            size,
                                            ppValue); // throws
        }

        // If we can't get it b/c it's a constant, then say so.
        hr = CordbClass::PostProcessUnavailableHRESULT(hr, pImport, fieldDef);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbObjectValue::GetFieldValueForType

// Public implementation of ICorDebugObjectValue::GetFieldValue
// Arguments:
//     input:  pClass   - class information for this object
//             fieldDef - the field token for the requested field
//     output: ppValue  - instance of ICDValue created to represent the field
// Return Value: S_OK on success, E_INVALIDARG, CORDBG_E_ENC_HANGING_FIELD, CORDBG_E_FIELD_NOT_INSTANCE
// or OOM on failure
HRESULT CordbObjectValue::GetFieldValue(ICorDebugClass *pClass,
                                        mdFieldDef fieldDef,
                                        ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(pClass, ICorDebugClass *);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);

    COV_VALIDATE_OBJECT();

    HRESULT hr;
    _ASSERTE(m_type);

    if (m_type->m_elementType != ELEMENT_TYPE_CLASS &&
        m_type->m_elementType != ELEMENT_TYPE_VALUETYPE)
    {
        return E_INVALIDARG;
    }

    // mdFieldDef may specify a field within a base class. mdFieldDef tokens are unique throughout a module.
    // So we still need a metadata scope to resolve the mdFieldDef. We can infer the scope from pClass.
    // Beware that this Type may be derived from a type in another module, and so the incoming
    // fieldDef has to be resolved in the metadata scope of pClass.

    RSExtSmartPtr<CordbType> relevantType;

    // This object has an ICorDebugType which has the type-parameters for generics.
    // ICorDebugClass provided by the caller does not have type-parameters. So we resolve that
    // by using the provided ICDClass with the type parameters from this object's ICDType.
    if (FAILED (hr= m_type->GetParentType((CordbClass *) pClass, &relevantType)))
    {
        return hr;
    }
    // Upon exit relevantType will either be the appropriate type for the
    // class we're looking for.

    hr = GetFieldValueForType(relevantType, fieldDef, ppValue);
    // GetParentType adds one reference to relevantType., Holder dtor releases
    return hr;

} // CordbObjectValue::GetFieldValue

HRESULT CordbObjectValue::GetVirtualMethod(mdMemberRef memberRef,
                                           ICorDebugFunction **ppFunction)
{
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);
    FAIL_IF_NEUTERED(this);
    COV_VALIDATE_OBJECT();

    return E_NOTIMPL;
} // CordbObjectValue::GetVirtualMethod

HRESULT CordbObjectValue::GetVirtualMethodAndType(mdMemberRef memberRef,
                                                  ICorDebugFunction **ppFunction,
                                                  ICorDebugType **ppType)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugType **);

    COV_VALIDATE_OBJECT();

    return E_NOTIMPL;
} // CordbObjectValue::GetVirtualMethodAndType

HRESULT CordbObjectValue::GetContext(ICorDebugContext **ppContext)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppContext, ICorDebugContext **);

    COV_VALIDATE_OBJECT();

    return E_NOTIMPL;
} // CordbObjectValue::GetContext

// determines whether this represents a value class-- always returns false
// Arguments:
//     output: pfIsValueClass - always false; CordbVCObjectValue is used to represent
//                              value classes, so by definition, a CordbObjectValue instance
//                              does not represent a value class
// Return Value: S_OK
//
HRESULT CordbObjectValue::IsValueClass(BOOL * pfIsValueClass)
{
    FAIL_IF_NEUTERED(this);
    COV_VALIDATE_OBJECT();

    if (pfIsValueClass) // don't assign to a null pointer!
        *pfIsValueClass = FALSE;

    return S_OK;
} // CordbObjectValue::IsValueClass

HRESULT CordbObjectValue::GetManagedCopy(IUnknown **ppObject)
{
    // GetManagedCopy() is deprecated. In the case where the version of
    // the debugger doesn't match the version of the debuggee, the two processes
    // might have dangerously different notions of the layout of an object.

    // This function is deprecated
    return E_NOTIMPL;
} // CordbObjectValue::GetManagedCopy

HRESULT CordbObjectValue::SetFromManagedCopy(IUnknown *pObject)
{
    // Deprecated for the same reason as GetManagedCopy()
    return E_NOTIMPL;
} // CordbObjectValue::SetFromManagedCopy

// gets a copy of the value
// Arguments:
//     output: pTo - buffer to hold the object copy. The caller must guarantee that this
//                   is non-null and the buffer is large enough to hold the object
// Return Value: S_OK or CORDBG_E_INVALID_OBJECT, CORDBG_E_OBJECT_NEUTERED, or E_INVALIDARG  on failure
//
HRESULT CordbObjectValue::GetValue(void *pTo)
{
    FAIL_IF_NEUTERED(this);
    COV_VALIDATE_OBJECT();

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pTo, BYTE, m_size, false, true);

   // Copy out the value, which is the whole object.
    memcpy(pTo, m_pObjectCopy, m_size);

    return S_OK;
} // CordbObjectValue::GetValue

HRESULT CordbObjectValue::SetValue(void *pFrom)
{
    // You're not allowed to set a whole object at once.
    return E_INVALIDARG;
} // CordbObjectValue::SetValue

// If this instance of CordbObjectValue is actually a string, get its length
// Arguments:
//     output: pcchString - the count of characters in the string
// Return Value: S_OK or CORDBG_E_INVALID_OBJECT, CORDBG_E_OBJECT_NEUTERED, or E_INVALIDARG  on failure
// Note: if the object is not really a string, the value in pcchString will be garbage on exit
HRESULT CordbObjectValue::GetLength(ULONG32 *pcchString)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pcchString, SIZE_T *);
    FAIL_IF_NEUTERED(this);

    _ASSERTE(m_info.objTypeData.elementType == ELEMENT_TYPE_STRING);

    COV_VALIDATE_OBJECT();

    *pcchString = (ULONG32)m_info.stringInfo.length;
    return S_OK;
} // CordbObjectValue::GetLength

// If this instance of CordbObjectValue represents a string, extract the string and its length.
// If cchString is less than the length of the string, we'll return only the first cchString characters
// but pcchString will still hold the full length. If cchString is more than the string length, we'll
// return only string length characters.
// Arguments:
//     input:  cchString -  the maximum number of characters to return, including NULL terminator
//     output: pcchString - the actual length of the string, excluding NULL terminator (this may be greater than cchString)
//             szString   - a buffer holding the string. The memory for this must be allocated and
//                          managed by the caller and must have space for at least cchString characters
// Return Value: S_OK or CORDBG_E_INVALID_OBJECT, CORDBG_E_OBJECT_NEUTERED, or E_INVALIDARG  on failure
HRESULT CordbObjectValue::GetString(ULONG32 cchString,
                                    ULONG32 *pcchString,
                                    _Out_writes_bytes_opt_(cchString) WCHAR szString[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(szString, WCHAR, cchString, true, true);
    VALIDATE_POINTER_TO_OBJECT(pcchString, SIZE_T *);

    _ASSERTE(m_info.objTypeData.elementType == ELEMENT_TYPE_STRING);

    COV_VALIDATE_OBJECT();

    if ((szString == NULL) || (cchString == 0))
        return E_INVALIDARG;

    // Add 1 to include null terminator
    SIZE_T len = m_info.stringInfo.length + 1;

    // adjust length to the size of the buffer
    if (cchString < len)
        len = cchString;

    memcpy(szString, m_stringBuffer, len * 2);
    *pcchString = (ULONG32)m_info.stringInfo.length;

    return S_OK;
} // CordbObjectValue::GetString

// Initialize an instance of CordbObjectValue, filling in the m_pObjectCopy field and, if appropriate,
// string information.
// Arguments: none
// ReturnValue: S_OK on success or E_OUTOFMEMORY or read process memory errors on failure
HRESULT CordbObjectValue::Init()
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //
    LOG((LF_CORDB,LL_INFO1000,"Invoking COV::Init\n"));

    HRESULT hr = S_OK;

    _ASSERTE (m_info.objTypeData.elementType != ELEMENT_TYPE_GENERICINST);
    _ASSERTE (m_info.objTypeData.elementType != ELEMENT_TYPE_VAR);
    _ASSERTE (m_info.objTypeData.elementType != ELEMENT_TYPE_MVAR);

    // Copy the entire object over to this process.
    m_pObjectCopy = new (nothrow) BYTE[m_size];

    if (m_pObjectCopy == NULL)
        return E_OUTOFMEMORY;

    EX_TRY
    {
        m_valueHome.GetValue(MemoryRange(m_pObjectCopy, m_size));  // throws
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    // Compute offsets in bytes to the locals and to a string if this is a
    // string object.
    m_objectLocalVars = m_pObjectCopy + m_info.objOffsetToVars;

    if (m_info.objTypeData.elementType == ELEMENT_TYPE_STRING)
        m_stringBuffer = m_pObjectCopy + m_info.stringInfo.offsetToStringBase;

    return hr;
} // CordbObjectValue::Init

// CordbObjectValue::GetThreadOwningMonitorLock
// If a managed thread owns the monitor lock on this object then *ppThread
// will point to that thread and S_OK will be returned. The thread object is valid
// until the thread exits. *pAcquisitionCount will indicate the number of times
// this thread would need to release the lock before it returns to being
// unowned.
// If no managed thread owns the monitor lock on this object then *ppThread
// and pAcquisitionCount will be unchanged and S_FALSE returned.
// If ppThread or pAcquisitionCount is not a valid pointer the result is
// undefined.
// If any error occurs such that it cannot be determined which, if any, thread
// owns the monitor lock on this object then a failing HRESULT will be returned
HRESULT CordbObjectValue::GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetThreadOwningMonitorLock(GetProcess(),
                                                           GetValueHome()->GetAddress(),
                                                           ppThread,
                                                           pAcquisitionCount);
}

// CordbObjectValue::GetMonitorEventWaitList
// Provides an ordered list of threads which are queued on the event associated
// with a monitor lock. The first thread in the list is the first thread which
// will be released by the next call to Monitor.Pulse, the next thread in the list
// will be released on the following call, and so on.
// If this list is non-empty S_OK will be returned, if it is empty S_FALSE
// will be returned (the enumeration is still valid, just empty).
// In either case the enumeration interface is only usable for the duration
// of the current synchronized state, however the threads interfaces dispensed
// from it are valid until the thread exits.
// If ppThread is not a valid pointer the result is undefined.
// If any error occurs such that it cannot be determined which, if any, threads
// are waiting for the monitor then a failing HRESULT will be returned
HRESULT CordbObjectValue::GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetMonitorEventWaitList(GetProcess(),
                                                        GetValueHome()->GetAddress(),
                                                        ppThreadEnum);
}

HRESULT CordbObjectValue::EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum)
{
    if (!ppCallStackEnum)
        return E_INVALIDARG;

    *ppCallStackEnum = NULL;

    HRESULT hr = S_OK;
    CorDebugExceptionObjectStackFrame* pStackFrames = NULL;

    PUBLIC_API_BEGIN(this);

    CORDB_ADDRESS objAddr = m_valueHome.GetAddress();

    IDacDbiInterface* pDAC = GetProcess()->GetDAC();
    VMPTR_Object vmObj = pDAC->GetObject(objAddr);

    DacDbiArrayList<DacExceptionCallStackData> dacStackFrames;

    pDAC->GetStackFramesFromException(vmObj, dacStackFrames);
    int stackFramesLength = dacStackFrames.Count();

    if (stackFramesLength > 0)
    {
        pStackFrames = new CorDebugExceptionObjectStackFrame[stackFramesLength];
        for (int index = 0; index < stackFramesLength; ++index)
        {
            DacExceptionCallStackData& currentDacFrame = dacStackFrames[index];
            CorDebugExceptionObjectStackFrame& currentStackFrame = pStackFrames[index];

            CordbAppDomain* pAppDomain = GetProcess()->LookupOrCreateAppDomain(currentDacFrame.vmAppDomain);
            CordbModule* pModule = pAppDomain->LookupOrCreateModule(currentDacFrame.vmDomainAssembly);

            hr = pModule->QueryInterface(IID_ICorDebugModule, reinterpret_cast<void**>(&currentStackFrame.pModule));
            _ASSERTE(SUCCEEDED(hr));

            currentStackFrame.ip = currentDacFrame.ip;
            currentStackFrame.methodDef = currentDacFrame.methodDef;
            currentStackFrame.isLastForeignExceptionFrame = currentDacFrame.isLastForeignExceptionFrame;
        }
    }

    CordbExceptionObjectCallStackEnumerator* callStackEnum = new CordbExceptionObjectCallStackEnumerator(GetProcess(), pStackFrames, stackFramesLength);
    GetProcess()->GetContinueNeuterList()->Add(GetProcess(), callStackEnum);

    hr = callStackEnum->QueryInterface(IID_ICorDebugExceptionObjectCallStackEnum, reinterpret_cast<void**>(ppCallStackEnum));
    _ASSERTE(SUCCEEDED(hr));

    PUBLIC_API_END(hr);

    if (pStackFrames)
        delete[] pStackFrames;

    return hr;
}

HRESULT CordbObjectValue::IsExceptionObject()
{
    HRESULT hr = S_OK;

    if (m_info.objTypeData.elementType != ELEMENT_TYPE_CLASS)
    {
        hr = S_FALSE;
    }
    else
    {
        CORDB_ADDRESS objAddr = m_valueHome.GetAddress();

        if (objAddr == NULL)
        {
            // object is a literal
            hr = S_FALSE;
        }
        else
        {
            IDacDbiInterface* pDAC = GetProcess()->GetDAC();

            VMPTR_Object vmObj = pDAC->GetObject(objAddr);
            BOOL fIsException = pDAC->IsExceptionObject(vmObj);

            if (!fIsException)
                hr = S_FALSE;
        }
    }

    return hr;
}

HRESULT CordbObjectValue::IsRcw()
{
    HRESULT hr = S_OK;

    if (m_info.objTypeData.elementType != ELEMENT_TYPE_CLASS)
    {
        hr = S_FALSE;
    }
    else
    {
        CORDB_ADDRESS objAddr = m_valueHome.GetAddress();

        if (objAddr == NULL)
        {
            // object is a literal
            hr = S_FALSE;
        }
        else
        {
            IDacDbiInterface* pDAC = GetProcess()->GetDAC();

            VMPTR_Object vmObj = pDAC->GetObject(objAddr);
            BOOL fIsRcw = pDAC->IsRcw(vmObj);

            if (!fIsRcw)
                hr = S_FALSE;
        }
    }

    return hr;
}

HRESULT CordbObjectValue::IsDelegate()
{
    HRESULT hr = S_OK;

    if (m_info.objTypeData.elementType != ELEMENT_TYPE_CLASS)
    {
        hr = S_FALSE;
    }
    else
    {
        CORDB_ADDRESS objAddr = m_valueHome.GetAddress();

        if (objAddr == NULL)
        {
            // object is a literal
            hr = S_FALSE;
        }
        else
        {
            IDacDbiInterface *pDAC = GetProcess()->GetDAC();

            VMPTR_Object vmObj = pDAC->GetObject(objAddr);
            BOOL fIsDelegate = pDAC->IsDelegate(vmObj);

            if (!fIsDelegate)
                hr = S_FALSE;
        }
    }

    return hr;
}

HRESULT IsSupportedDelegateHelper(IDacDbiInterface::DelegateType delType)
{
    switch (delType)
    {
    case IDacDbiInterface::DelegateType::kClosedDelegate:
    case IDacDbiInterface::DelegateType::kOpenDelegate:
        return S_OK;
    default:
        return CORDBG_E_UNSUPPORTED_DELEGATE;
    }
}

HRESULT CordbObjectValue::GetTargetHelper(ICorDebugReferenceValue **ppTarget)
{
    IDacDbiInterface::DelegateType delType;
    VMPTR_Object pDelegateObj;
    VMPTR_Object pDelegateTargetObj;
    VMPTR_AppDomain pAppDomainOfTarget;

    CORDB_ADDRESS delegateAddr = m_valueHome.GetAddress();

    IDacDbiInterface *pDAC = GetProcess()->GetDAC();
    pDelegateObj = pDAC->GetObject(delegateAddr);

    HRESULT hr = pDAC->GetDelegateType(pDelegateObj, &delType);
    if (hr != S_OK)
        return hr;

    hr = IsSupportedDelegateHelper(delType);
    if (hr != S_OK)
        return hr;

    hr = pDAC->GetDelegateTargetObject(delType, pDelegateObj, &pDelegateTargetObj, &pAppDomainOfTarget);
    if (hr != S_OK || pDelegateTargetObj.IsNull())
    {
        *ppTarget = NULL;
        return hr;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    RSSmartPtr<CordbAppDomain> pCordbAppDomForTarget(GetProcess()->LookupOrCreateAppDomain(pAppDomainOfTarget));
    RSSmartPtr<CordbReferenceValue> targetObjRefVal(CordbValue::CreateHeapReferenceValue(pCordbAppDomForTarget, pDelegateTargetObj));
    *ppTarget = static_cast<ICorDebugReferenceValue*>(targetObjRefVal.GetValue());
    targetObjRefVal->ExternalAddRef();

    return S_OK;
}

HRESULT CordbObjectValue::GetFunctionHelper(ICorDebugFunction **ppFunction)
{
    IDacDbiInterface::DelegateType delType;
    VMPTR_Object pDelegateObj;

    *ppFunction = NULL;
    CORDB_ADDRESS delegateAddr = m_valueHome.GetAddress();

    IDacDbiInterface *pDAC = GetProcess()->GetDAC();
    pDelegateObj = pDAC->GetObject(delegateAddr);

    HRESULT hr = pDAC->GetDelegateType(pDelegateObj, &delType);
    if (hr != S_OK)
        return hr;

    hr = IsSupportedDelegateHelper(delType);
    if (hr != S_OK)
        return hr;

    mdMethodDef functionMethodDef = 0;
    VMPTR_DomainAssembly functionDomainAssembly;
    NativeCodeFunctionData nativeCodeForDelFunc;

    hr = pDAC->GetDelegateFunctionData(delType, pDelegateObj, &functionDomainAssembly, &functionMethodDef);
    if (hr != S_OK)
        return hr;

    // TODO: How to ensure results are sanitized?
    // Also, this is expensive. Do we really care that much about this?
    pDAC->GetNativeCodeInfo(functionDomainAssembly, functionMethodDef, &nativeCodeForDelFunc);

    RSSmartPtr<CordbModule> funcModule(GetProcess()->LookupOrCreateModule(functionDomainAssembly));
    RSSmartPtr<CordbFunction> func;
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        func.Assign(funcModule->LookupOrCreateFunction(functionMethodDef, nativeCodeForDelFunc.encVersion));
    }

    *ppFunction = static_cast<ICorDebugFunction*> (func.GetValue());
    func->ExternalAddRef();

    return S_OK;
}

HRESULT CordbObjectValue::GetTarget(ICorDebugReferenceValue **ppObject)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppObject, ICorDebugReferenceValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_fIsDelegate);

    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = GetTargetHelper(ppObject);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbObjectValue::GetFunction(ICorDebugFunction **ppFunction)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_fIsDelegate);

    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = GetFunctionHelper(ppFunction);
    }
    EX_CATCH_HRESULT(hr)
    return hr;
}

HRESULT CordbObjectValue::GetCachedInterfaceTypes(
                        BOOL bIInspectableOnly,
                        ICorDebugTypeEnum * * ppInterfacesEnum)
{
#if !defined(FEATURE_COMINTEROP)

    return E_NOTIMPL;

#else

    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(ppInterfacesEnum, ICorDebugTypeEnum **);

    _ASSERTE(m_fIsRcw);

    EX_TRY
    {
        *ppInterfacesEnum = NULL;

        NewArrayHolder<CordbType*> pItfs(NULL);

        // retrieve interface types
        DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> dacInterfaces;

        IDacDbiInterface* pDAC = GetProcess()->GetDAC();

        CORDB_ADDRESS objAddr = m_valueHome.GetAddress();
        VMPTR_Object vmObj = pDAC->GetObject(objAddr);

        // retrieve type info from LS
        pDAC->GetRcwCachedInterfaceTypes(vmObj, m_appdomain->GetADToken(),
                        bIInspectableOnly, &dacInterfaces);

        // synthesize CordbType instances
        int cItfs = dacInterfaces.Count();
        if (cItfs > 0)
        {
            pItfs = new CordbType*[cItfs];
            for (int n = 0; n < cItfs; ++n)
            {
                hr = CordbType::TypeDataToType(m_appdomain,
                                               &(dacInterfaces[n]),
                                               &pItfs[n]);
            }
        }

        // build a type enumerator
        CordbTypeEnum* pTypeEnum = CordbTypeEnum::Build(m_appdomain, GetProcess()->GetContinueNeuterList(), cItfs, pItfs);
        if ( pTypeEnum == NULL )
        {
            IfFailThrow(E_OUTOFMEMORY);
        }

        (*ppInterfacesEnum) = static_cast<ICorDebugTypeEnum*> (pTypeEnum);
        pTypeEnum->ExternalAddRef();

    }
    EX_CATCH_HRESULT(hr);

    return hr;

#endif
}

HRESULT CordbObjectValue::GetCachedInterfacePointers(
                        BOOL bIInspectableOnly,
                        ULONG32 celt,
                        ULONG32 *pcEltFetched,
                        CORDB_ADDRESS * ptrs)
{
#if !defined(FEATURE_COMINTEROP)

    return E_NOTIMPL;

#else

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_fIsRcw);

    if (pcEltFetched == NULL && (ptrs == NULL || celt == 0))
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    ULONG32 cItfs = 0;

    // retrieve interface types

    CORDB_ADDRESS objAddr = m_valueHome.GetAddress();

    DacDbiArrayList<CORDB_ADDRESS> dacItfPtrs;
    EX_TRY
    {
        IDacDbiInterface* pDAC = GetProcess()->GetDAC();
        VMPTR_Object vmObj = pDAC->GetObject(objAddr);

        // retrieve type info from LS
        pDAC->GetRcwCachedInterfacePointers(vmObj, bIInspectableOnly, &dacItfPtrs);
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    // synthesize CordbType instances
    cItfs = (ULONG32)dacItfPtrs.Count();

    if (pcEltFetched != NULL && ptrs == NULL)
    {
        *pcEltFetched = cItfs;
        return S_OK;
    }

    if (pcEltFetched != NULL)
    {
        *pcEltFetched = (cItfs <= celt ? cItfs : celt);
    }

    if (ptrs != NULL && *pcEltFetched > 0)
    {
        for (ULONG32 i = 0; i < *pcEltFetched; ++i)
            ptrs[i] = dacItfPtrs[i];
    }

    return (*pcEltFetched == celt ? S_OK : S_FALSE);

#endif
}


/* ------------------------------------------------------------------------- *
 * Value Class Object
 * ------------------------------------------------------------------------- */

// constructor
// Arguments:
//     input: pAppdomain      - app domain to which the value belongs
//            pType           - type information for the value
//            remoteValue     - buffer describing the target location of the value
//            ppRemoteRegAddr - describes the register information if the value resides in a register
// Note: May throw E_OUTOFMEMORY
CordbVCObjectValue::CordbVCObjectValue(CordbAppDomain *               pAppdomain,
                                       CordbType *                    pType,
                                       TargetBuffer                   remoteValue,
                                       EnregisteredValueHomeHolder * ppRemoteRegAddr)

    // We'd like to neuter this on Continue (not just exit), but it may be a breaking change,
    // especially for ValueTypes that don't have any GC refs in them.
    : CordbValue(pAppdomain,
                 pType,
                 remoteValue.pAddress,
                 false,
                 pAppdomain->GetSweepableExitNeuterList()),
      m_pObjectCopy(NULL),
      m_pValueHome(NULL)
{
    // instantiate the value home
    NewHolder<ValueHome> pHome(NULL);

    if (remoteValue.IsEmpty())
    {
        pHome = (new RegisterValueHome(pAppdomain->GetProcess(), ppRemoteRegAddr));
    }
    else
    {
        pHome = (new VCRemoteValueHome(pAppdomain->GetProcess(), remoteValue));
    }
    m_pValueHome = pHome.GetValue();  // throws
    pHome.SuppressRelease();
} // CordbVCObjectValue::CordbVCObjectValue

// destructor
CordbVCObjectValue::~CordbVCObjectValue()
{
    DTOR_ENTRY(this);

    _ASSERTE(IsNeutered());

    // Destroy the copy of the object.
    if (m_pObjectCopy != NULL)
    {
        delete [] m_pObjectCopy;
        m_pObjectCopy = NULL;
    }

    // destroy the value home
    if (m_pValueHome != NULL)
    {
        delete m_pValueHome;
        m_pValueHome = NULL;
}
} // CordbVCObjectValue::~CordbVCObjectValue

HRESULT CordbVCObjectValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugObjectValue)
    {
        *pInterface = static_cast<ICorDebugObjectValue*>(this);
    }
    else if (id == IID_ICorDebugObjectValue2)

    {
        *pInterface = static_cast<ICorDebugObjectValue2*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbVCObjectValue::QueryInterface

// returns the basic type of the ICDValue
// Arguments:
//     output: pType - the type of the ICDValue (always E_T_VALUETYPE)
// ReturnValue: S_OK on success or E_INVALIDARG if pType is NULL
HRESULT CordbVCObjectValue::GetType(CorElementType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);

    *pType = ELEMENT_TYPE_VALUETYPE;
    return S_OK;
} // CordbVCObjectValue::GetType

// public API to get the CordbClass field
// Arguments:
//     output: ppClass - holds a pointer to the ICDClass instance belonging to this
// Return Value: S_OK on success, CORDBG_E_OBJECT_NEUTERED or synchronization errors on failure
HRESULT CordbVCObjectValue::GetClass(ICorDebugClass **ppClass)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    *ppClass = (ICorDebugClass*) GetClass();

    if (*ppClass != NULL)
        (*ppClass)->AddRef();

    return S_OK;
} // CordbVCObjectValue::GetClass

// internal method  to get the CordbClass field
// Arguments: none
// ReturnValue: the instance of CordbClass belonging to this VC object
CordbClass *CordbVCObjectValue::GetClass()
{
    CordbClass *tycon;
    Instantiation inst;
    m_type->DestConstructedType(&tycon, &inst);
    return tycon;
} // CordbVCObjectValue::GetClass

//-----------------------------------------------------------------------------
//
// Finds the given field of the given type in the object and returns an ICDValue for it.
//
// Arguments:
//    pType - The type of the field
//    fieldDef - The field's metadata def.
//    ppValue - OUT: the ICDValue for the field.
//
// Returns:
//   S_OK on success, CORDBG_E_OBJECT_NEUTERED, E_INVALIDARG, CORDBG_E_ENC_HANGING_FIELD, or various other
//   failure codes
HRESULT CordbVCObjectValue::GetFieldValueForType(ICorDebugType * pType,
                                                 mdFieldDef fieldDef,
                                                 ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Validate the token.
        if ((m_type->m_pClass == NULL) || !m_type->m_pClass->GetModule()->GetMetaDataImporter()->IsValidToken(fieldDef))
        {
            ThrowHR(E_INVALIDARG);
        }


        CordbType * pCordbType;

        //
        // <TODO>@todo: need to ensure that pClass is really on the class
        // hierarchy of m_class!!!</TODO>
        //
        if (pType == NULL)
        {
            pCordbType = m_type;
        }
        else
        {
            pCordbType = static_cast<CordbType *> (pType);
        }

        FieldData * pFieldData;

    #ifdef _DEBUG
        pFieldData = NULL;
    #endif

        hr = pCordbType->GetFieldInfo(fieldDef, &pFieldData);
        _ASSERTE(hr != CORDBG_E_ENC_HANGING_FIELD);

        // If we get back CORDBG_E_ENC_HANGING_FIELD we'll just fail -
        // value classes should not be able to add fields once they're loaded,
        // since the new fields _can't_ be contiguous with the old fields,
        // and having all the fields contiguous is kinda the point of a V.C.
        IfFailThrow(hr);

        _ASSERTE(pFieldData != NULL);

        CordbModule * pModule = pCordbType->m_pClass->GetModule();

        SigParser sigParser;
        IfFailThrow(pFieldData->GetFieldSignature(pModule, &sigParser));

        // <TODO>
        // How can I assert that I have exactly one field?
        // </TODO>
        CordbType * pFieldType;

        IfFailThrow(CordbType::SigToType(pModule, &sigParser, &(pCordbType->m_inst), &pFieldType));

        _ASSERTE(pFieldData->OkToGetOrSetInstanceOffset());
        // Compute the address of the field contents in our local object cache
        SIZE_T fieldOffset = pFieldData->GetInstanceOffset();
        ULONG32 size = GetSizeForType(pFieldType, kUnboxed);

        // verify that the field starts before the end of m_pObjectCopy
        _ASSERTE(fieldOffset < m_size);
        _ASSERTE(fieldOffset + size <= m_size);

        m_pValueHome->CreateInternalValue(pFieldType,
                                          fieldOffset,
                                          m_pObjectCopy + fieldOffset,
                                          size,
                                          ppValue); // throws

    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbVCObjectValue::GetFieldValueForType

// gets an ICDValue to represent a field of the VC object
// Arguments:
//     input:  pClass   - the class information for this object (needed to get the parent class information)
//             fieldDef - field token for the desired field
//     output: ppValue  - on success, the ICDValue representing the desired field
// Return Value: S_OK on success, CORDBG_E_OBJECT_NEUTERED, CORDBG_E_CLASS_NOT_LOADED, E_INVALIDARG, OOM,
//               CORDBG_E_ENC_HANGING_FIELD, or various other failure codes
HRESULT CordbVCObjectValue::GetFieldValue(ICorDebugClass *pClass,
                                        mdFieldDef fieldDef,
                                        ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(pClass, ICorDebugClass *);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);

    HRESULT hr;
    _ASSERTE(m_type);

    if (m_type->m_elementType != ELEMENT_TYPE_CLASS &&
        m_type->m_elementType != ELEMENT_TYPE_VALUETYPE)
    {
        return E_INVALIDARG;
    }

    RSExtSmartPtr<CordbType> relevantType;

    if (FAILED (hr= m_type->GetParentType((CordbClass *) pClass, &relevantType)))
    {
        return hr;
    }
    // Upon exit relevantType will either be the appropriate type for the
    // class we're looking for.

    hr = GetFieldValueForType(relevantType, fieldDef, ppValue);
    // GetParentType ands one reference to relevantType, holder dtor releases that.
    return hr;

} // CordbVCObjectValue::GetFieldValue

// get a copy of the VC object
// Arguments:
//     output: pTo - a caller-allocated buffer to hold the copy
// Return Value: S_OK on success, CORDBG_E_OBJECT_NEUTERED on failure
// Note:  The caller must ensure the buffer is large enough to hold the value (by a previous call to GetSize)
//        and is responsible for allocation and deallocation.
HRESULT CordbVCObjectValue::GetValue(void *pTo)
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pTo, BYTE, m_size, false, true);
    FAIL_IF_NEUTERED(this);

    // Copy out the value, which is the whole object.
    memcpy(pTo, m_pObjectCopy, m_size);

    return S_OK;
} // CordbVCObjectValue::GetValue

// set the value of a VC object
// Arguments:
//     input: pSrc - buffer containing the new value. Allocated and managed by the caller.
// Return Value: S_OK on success, CORDBG_E_OBJECT_NEUTERED, synchronization errors, E_INVALIDARG, write
//               process memory errors, CORDBG_E_CLASS_NOT_LOADED or OOM on failure
HRESULT CordbVCObjectValue::SetValue(void * pSrc)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pSrc, BYTE, m_size, true, false);

    // Can't change literals...
    if (m_isLiteral)
        return E_INVALIDARG;

    if (m_type)
    {
        IfFailRet(m_type->Init(FALSE));
    }

    EX_TRY
    {
        m_pValueHome->SetValue(MemoryRange(pSrc, m_size), m_type); // throws
    }
    EX_CATCH_HRESULT(hr);
    if (SUCCEEDED(hr))
    {
        // That worked, so update the copy of the value we have over here.
        memcpy(m_pObjectCopy, pSrc, m_size);
    }

    return hr;
} // CordbVCObjectValue::SetValue

HRESULT CordbVCObjectValue::GetVirtualMethod(mdMemberRef memberRef,
                                           ICorDebugFunction **ppFunction)
{
    return E_NOTIMPL;
}

HRESULT CordbVCObjectValue::GetVirtualMethodAndType(mdMemberRef memberRef,
                                                    ICorDebugFunction **ppFunction,
                                                    ICorDebugType **ppType)
{
    return E_NOTIMPL;
}

HRESULT CordbVCObjectValue::GetContext(ICorDebugContext **ppContext)
{
    return E_NOTIMPL;
}

// self-identifier--always returns true as long as pbIsValueClass is non-Null
HRESULT CordbVCObjectValue::IsValueClass(BOOL *pbIsValueClass)
{
    if (pbIsValueClass)
        *pbIsValueClass = TRUE;

    return S_OK;
} // CordbVCObjectValue::IsValueClass

HRESULT CordbVCObjectValue::GetManagedCopy(IUnknown **ppObject)
{
    // This function is deprecated
    return E_NOTIMPL;
}

HRESULT CordbVCObjectValue::SetFromManagedCopy(IUnknown *pObject)
{
    // This function is deprecated
    return E_NOTIMPL;
}

    //
// CordbVCObjectValue::Init
//
// Description
//      Initializes the Right-Side's representation of a Value Class object.
// Parameters
//      input: localValue - buffer containing the value if this instance of CordbObjectValue
//                          was a field or array element of an existing value, otherwise this
//                          will have a start address equal to NULL
// Returns
//      HRESULT
//          S_OK if the function completed normally
//          failing HR otherwise
// Exceptions
//      None
//
HRESULT CordbVCObjectValue::Init(MemoryRange localValue)
{
    HRESULT hr = S_OK;

    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //

    // Get the object size from the class
    ULONG32 size;
    IfFailRet( m_type->GetUnboxedObjectSize(&size) );
    m_size = size;

    // Copy the entire object over to this process.
    m_pObjectCopy = new (nothrow) BYTE[m_size];

    if (m_pObjectCopy == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if (localValue.StartAddress() != NULL)
    {
        // The data is already in the local address space. Go ahead and copy it
        // from there.
        // localValue.StartAddress points to:
        // 1. A field from the local cached copy belonging to an instance of CordbVCObjectValue (different
        //    instance from "this") or CordbObjectValue
        // 2. An element in the locally cached subrange of an array belonging to an instance of CordbArrayValue
        // 3. The address of a particular register in the register display of an instance of CordbNativeFrame
        //    for an enregistered value type. In this case, it's possible that the size of the value is
        //    smaller than the size of a full register. For that reason, we can't just use localValue.Size()
        //    as the number of bytes to copy, because only enough space for the value has been allocated.
        _ASSERTE(localValue.Size() >= m_size);
        localCopy(m_pObjectCopy, MemoryRange(localValue.StartAddress(), m_size));
        return S_OK;
    }

    EX_TRY
    {
        m_pValueHome->GetValue(MemoryRange(m_pObjectCopy, m_size));  // throws
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbVCObjectValue::Init

/* ------------------------------------------------------------------------- *
 * Box Value class
 * ------------------------------------------------------------------------- */

// constructor
// Arguments:
//     input: appdomain    - app domain to which the value belongs
//            type         - type information for the boxed value
//            remoteValue  - buffer describing the remote location of the value
//            size         - size of the value
//            offsetToVars - offset from the beginning of the value to the first field of the value
CordbBoxValue::CordbBoxValue(CordbAppDomain *appdomain,
                             CordbType *type,
                             TargetBuffer      remoteValue,
                             ULONG32           size,
                             SIZE_T offsetToVars)
    : CordbValue(appdomain, type, remoteValue.pAddress, false, appdomain->GetProcess()->GetContinueNeuterList()),
       m_offsetToVars(offsetToVars),
       m_valueHome(appdomain->GetProcess(), remoteValue)
{
    m_size = size;
} // CordbBoxValue::CordbBoxValue

// destructor
CordbBoxValue::~CordbBoxValue()
{
    DTOR_ENTRY(this);
    _ASSERTE(IsNeutered());
} // CordbBoxValue::~CordbBoxValue

HRESULT CordbBoxValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugBoxValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugBoxValue)
    {
        *pInterface = static_cast<ICorDebugBoxValue*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue)
    {
        *pInterface = static_cast<ICorDebugHeapValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue2)
    {
        *pInterface = static_cast<ICorDebugHeapValue2*>(this);
    }
    else if (id == IID_ICorDebugHeapValue3)
    {
        *pInterface = static_cast<ICorDebugHeapValue3*>(this);
    }
    else if (id == IID_ICorDebugHeapValue4)
    {
        *pInterface = static_cast<ICorDebugHeapValue4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugBoxValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbBoxValue::QueryInterface

// returns the basic type of the ICDValue
// Arguments:
//     output: pType - the type of the ICDValue (always E_T_CLASS)
// ReturnValue: S_OK on success or E_INVALIDARG if pType is NULL
HRESULT CordbBoxValue::GetType(CorElementType *pType)
{
    VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);

    *pType = ELEMENT_TYPE_CLASS;

    return (S_OK);
} // CordbBoxValue::GetType

HRESULT CordbBoxValue::IsValid(BOOL *pbValid)
{
    VALIDATE_POINTER_TO_OBJECT(pbValid, BOOL *);

    // <TODO>@todo: implement tracking of objects across collections.</TODO>

    return E_NOTIMPL;
}

HRESULT CordbBoxValue::CreateRelocBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
{
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugValueBreakpoint **);

    return E_NOTIMPL;
}

// Creates a handle of the given type for this heap value.
// Not Implemented In-Proc.
// Create a handle for a heap object.
// @todo: How to prevent this being called by non-heap object?
// Arguments:
//     input:  handleType - type of the handle to be created
//     output: ppHandle   - on success, the newly created handle
// Return Value: S_OK on success or E_INVALIDARG, E_OUTOFMEMORY, or CORDB_E_HELPER_MAY_DEADLOCK
HRESULT CordbBoxValue::CreateHandle(
    CorDebugHandleType handleType,
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(handleType, ppHandle);
}   // CordbBoxValue::CreateHandle

// Creates a pinned handle for this heap value.
// Not Implemented In-Proc.
// Create a handle for a heap object.
// @todo: How to prevent this being called by non-heap object?
// Arguments:
//     output: ppHandle   - on success, the newly created handle
// Return Value: S_OK on success or E_INVALIDARG, E_OUTOFMEMORY, or CORDB_E_HELPER_MAY_DEADLOCK
HRESULT CordbBoxValue::CreatePinnedHandle(
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(HANDLE_PINNED, ppHandle);
}   // CreatePinnedHandle


HRESULT CordbBoxValue::GetValue(void *pTo)
{
    // Can't get a whole copy of a box.
    return E_INVALIDARG;
}

HRESULT CordbBoxValue::SetValue(void *pFrom)
{
    // You're not allowed to set a box value.
    return E_INVALIDARG;
}

// gets the unboxed value from this boxed value
// Arguments:
//     output: ppObject - pointer to an instance of ICDValue representing the unboxed value, unless ppObject
//                        is NULL
// Return Value: S_OK on success or a variety of possible failures: OOM, E_FAIL, errors from
//               ReadProcessMemory.
HRESULT CordbBoxValue::GetObject(ICorDebugObjectValue **ppObject)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppObject, ICorDebugObjectValue **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    ULONG32 size = 0;
    m_type->GetUnboxedObjectSize(&size);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_valueHome.CreateInternalValue(m_type,
                                        m_offsetToVars,
                                        NULL,
                                        size,
                                        reinterpret_cast<ICorDebugValue **>(ppObject)); // throws
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbBoxValue::GetObject

// If a managed thread owns the monitor lock on this object then *ppThread
// will point to that thread and S_OK will be returned. The thread object is valid
// until the thread exits. *pAcquisitionCount will indicate the number of times
// this thread would need to release the lock before it returns to being
// unowned.
// If no managed thread owns the monitor lock on this object then *ppThread
// and pAcquisitionCount will be unchanged and S_FALSE returned.
// If ppThread or pAcquisitionCount is not a valid pointer the result is
// undefined.
// If any error occurs such that it cannot be determined which, if any, thread
// owns the monitor lock on this object then a failing HRESULT will be returned
HRESULT CordbBoxValue::GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetThreadOwningMonitorLock(GetProcess(),
                                                           GetValueHome()->GetAddress(),
                                                           ppThread,
                                                           pAcquisitionCount);
}

// Provides an ordered list of threads which are queued on the event associated
// with a monitor lock. The first thread in the list is the first thread which
// will be released by the next call to Monitor.Pulse, the next thread in the list
// will be released on the following call, and so on.
// If this list is non-empty S_OK will be returned, if it is empty S_FALSE
// will be returned (the enumeration is still valid, just empty).
// In either case the enumeration interface is only usable for the duration
// of the current synchronized state, however the threads interfaces dispensed
// from it are valid until the thread exits.
// If ppThread is not a valid pointer the result is undefined.
// If any error occurs such that it cannot be determined which, if any, threads
// are waiting for the monitor then a failing HRESULT will be returned
HRESULT CordbBoxValue::GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetMonitorEventWaitList(GetProcess(),
                                                        GetValueHome()->GetAddress(),
                                                        ppThreadEnum);
}


/* ------------------------------------------------------------------------- *
 * Array Value class
 * ------------------------------------------------------------------------- */

// The size of the buffer we allocate to hold array elements.
// Note that since we must be able to hold at least one element, we may
// allocate larger than the cache size here.
// Also, this cache doesn't include a small header used to store the rank vectors
#ifdef _DEBUG
// For debug, use a small size to cause more churn
    #define ARRAY_CACHE_SIZE (1000)
#else
// For release, guess 4 pages should be enough. Subtract some bytes to store
// the header so that it doesn't push us onto another page. (We guess a reasonable
// header size, but it's ok if it's larger).
    #define ARRAY_CACHE_SIZE (4 * 4096 - 24)
#endif

// constructor
// Arguments:
//     input:
//         pAppDomain  - app domain to which the value belongs
//         pType       - type information for the value
//         pObjectInfo - array specific type information
//         remoteValue - buffer describing the remote location of the value
CordbArrayValue::CordbArrayValue(CordbAppDomain *          pAppdomain,
                                 CordbType *               pType,
                                 DebuggerIPCE_ObjectData * pObjectInfo,
                                 TargetBuffer              remoteValue)
    : CordbValue(pAppdomain,
                 pType,
                 remoteValue.pAddress,
                 false,
                 pAppdomain->GetProcess()->GetContinueNeuterList()),
      m_info(*pObjectInfo),
      m_pObjectCopy(NULL),
      m_valueHome(pAppdomain->GetProcess(), remoteValue)
{
    m_size = m_info.objSize;
    pType->DestUnaryType(&m_elemtype);

// Set range to illegal values to force a load on first access
    m_idxLower = m_idxUpper = (SIZE_T) -1;
} // CordbArrayValue::CordbArrayValue

// destructor
CordbArrayValue::~CordbArrayValue()
{
    DTOR_ENTRY(this);
    _ASSERTE(IsNeutered());

    // Destroy the copy of the object.
    if (m_pObjectCopy != NULL)
        delete [] m_pObjectCopy;
} // CordbArrayValue::~CordbArrayValue

HRESULT CordbArrayValue::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugArrayValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugArrayValue)
    {
        *pInterface = static_cast<ICorDebugArrayValue*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue)
    {
        *pInterface = static_cast<ICorDebugHeapValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue2)
    {
        *pInterface = static_cast<ICorDebugHeapValue2*>(this);
    }
    else if (id == IID_ICorDebugHeapValue3)
    {
        *pInterface = static_cast<ICorDebugHeapValue3*>(this);
    }
    else if (id == IID_ICorDebugHeapValue4)
    {
        *pInterface = static_cast<ICorDebugHeapValue4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugArrayValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbArrayValue::QueryInterface

// gets the type of the array elements
// Arguments:
//     output: pType - the element type unless pType is NULL
// Return Value: S_OK on success or E_INVALIDARG if pType is null
HRESULT CordbArrayValue::GetElementType(CorElementType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);

    *pType = m_elemtype->m_elementType;
    return S_OK;
} // CordbArrayValue::GetElementType


// gets the rank of the array
// Arguments:
//     output: pnRank - the rank of the array unless pnRank is null
// Return Value: S_OK on success or E_INVALIDARG if pnRank is null
HRESULT CordbArrayValue::GetRank(ULONG32 *pnRank)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnRank, SIZE_T *);

    // Rank info is duplicated for sanity checking - double check it here.
    _ASSERTE(m_info.arrayInfo.rank == m_type->m_rank);
    *pnRank = m_type->m_rank;
    return S_OK;
} // CordbArrayValue::GetRank

// gets the number of elements in the array
// Arguments:
//     output: pnCount - the number of dimensions for the array unless pnCount is null
// Return Value: S_OK on success or E_INVALIDARG if pnCount is null
HRESULT CordbArrayValue::GetCount(ULONG32 *pnCount)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnCount, ULONG32 *);

    *pnCount = (ULONG32)m_info.arrayInfo.componentCount;
    return S_OK;
} // CordbArrayValue::GetCount

// get the size of each dimension of the array
// Arguments:
//     input:  cdim - the number of dimensions about which to get dimensions--this must be the same as the rank
//     output: dims - an array to hold the sizes of the dimensions of the array--this is allocated and
//                    managed by the caller
// Return Value: S_OK on success or E_INVALIDARG
HRESULT CordbArrayValue::GetDimensions(ULONG32 cdim, ULONG32 dims[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(dims, SIZE_T, cdim, true, true);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // Rank info is duplicated for sanity checking - double check it here.
    _ASSERTE(m_info.arrayInfo.rank == m_type->m_rank);
    if (cdim != m_type->m_rank)
        return E_INVALIDARG;

    // SDArrays don't have bounds info, so return the component count.
    if (cdim == 1)
        dims[0] = (ULONG32)m_info.arrayInfo.componentCount;
    else
    {
        _ASSERTE(m_info.arrayInfo.offsetToUpperBounds != 0);
        _ASSERTE(m_arrayUpperBase != NULL);

        // The upper bounds info in the array is the true size of each
        // dimension.
        for (unsigned int i = 0; i < cdim; i++)
            dims[i] = m_arrayUpperBase[i];
    }

    return S_OK;
} // CordbArrayValue::GetDimensions

//
// indicates whether the array has base indices
// Arguments:
//     output: pbHasBaseIndices - true iff the array has more than one dimension and pbHasBaseIndices is not null
// Return Value: S_OK on success or E_INVALIDARG if pbHasBaseIndices is null
HRESULT CordbArrayValue::HasBaseIndices(BOOL *pbHasBaseIndices)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pbHasBaseIndices, BOOL *);

    *pbHasBaseIndices = m_info.arrayInfo.offsetToLowerBounds != 0;
    return S_OK;
} // CordbArrayValue::HasBaseIndices

// gets the base indices for a multidimensional array
// Arguments:
//     input: cdim - the number of dimensions (this must be the same as the actual rank of the array)
//            indices - an array to hold the base indices for the array dimensions (allocated and managed
//                      by the caller, it must have space for cdim elements)
// Return Value: S_OK on success or E_INVALIDARG if cdim is not equal to the array rank or indices is null
HRESULT CordbArrayValue::GetBaseIndices(ULONG32 cdim, ULONG32 indices[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(indices, SIZE_T, cdim, true, true);

    // Rank info is duplicated for sanity checking - double check it here.
    _ASSERTE(m_info.arrayInfo.rank == m_type->m_rank);
    if ((cdim != m_type->m_rank) ||
        (m_info.arrayInfo.offsetToLowerBounds == 0))
        return E_INVALIDARG;

    _ASSERTE(m_arrayLowerBase != NULL);

    for (unsigned int i = 0; i < cdim; i++)
        indices[i] = m_arrayLowerBase[i];

    return S_OK;
} // CordbArrayValue::GetBaseIndices

// Get an element at the position indicated by the values in indices (one index for each dimension)
// Arguments:
//     input:  cdim    - the number of dimensions and thus the number of elements in indices. This must match
//                       the actual rank of the array value.
//             indices - an array of indices to specify the position of the element. For example, to get a[2][1][0],
//                       indices would contain 2, 1, and 0 in that order.
//     output: ppValue - an ICDValue representing the element, unless an error occurs
// Return Value: S_OK on success or E_INVALIDARG if cdim != rank, indices is NULL or ppValue is NULL
//               or a variety of possible failures: OOM, E_FAIL, errors from
//               ReadProcessMemory.
HRESULT CordbArrayValue::GetElement(ULONG32           cdim,
                                    ULONG32           indices[],
                                    ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(indices, SIZE_T, cdim, true, true);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *ppValue = NULL;

    // Rank info is duplicated for sanity checking - double check it here.
    _ASSERTE(m_info.arrayInfo.rank == m_type->m_rank);
    if ((cdim != m_type->m_rank) || (indices == NULL))
        return E_INVALIDARG;

    // If the array has lower bounds, adjust the indices.
    if (m_info.arrayInfo.offsetToLowerBounds != 0)
    {
        _ASSERTE(m_arrayLowerBase != NULL);

        for (unsigned int i = 0; i < cdim; i++)
            indices[i] -= m_arrayLowerBase[i];
    }

    SIZE_T offset = 0;

    // SDArrays don't have upper bounds
    if (cdim == 1)
    {
        offset = indices[0];

        // Bounds check
        if (offset >= m_info.arrayInfo.componentCount)
            return E_INVALIDARG;
    }
    else
    {
        _ASSERTE(m_info.arrayInfo.offsetToUpperBounds != 0);
        _ASSERTE(m_arrayUpperBase != NULL);

        // Calculate the offset in bytes for all dimensions.
        SIZE_T multiplier = 1;

        for (int i = cdim - 1; i >= 0; i--)
        {
            // Bounds check
            if (indices[i] >= m_arrayUpperBase[i])
                return E_INVALIDARG;

            offset += indices[i] * multiplier;
            multiplier *= m_arrayUpperBase[i];
        }

        _ASSERTE(offset < m_info.arrayInfo.componentCount);
    }

    return GetElementAtPosition((ULONG32)offset, ppValue);
} // CordbArrayValue::GetElement

// get an ICDValue to represent the element at a given position
// Arguments:
//     input:  nPosition - the offset from the beginning of the array to the element
//     output: ppValue   - the ICDValue representing the array element on success
// Return Value: S_OK on success, E_INVALIDARG  or a variety of possible failures: OOM, E_FAIL, errors from
//               ReadProcessMemory.
HRESULT CordbArrayValue::GetElementAtPosition(ULONG32 nPosition,
                                              ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (nPosition >= m_info.arrayInfo.componentCount)
    {
        *ppValue = NULL;
        return E_INVALIDARG;
    }

    // Rank info is duplicated for sanity checking - double check it here.
    _ASSERTE(m_info.arrayInfo.rank == m_type->m_rank);

    // The header consists of two DWORDs for each dimension, representing the upper and lower bound for that dimension. A
    // vector of lower bounds comes first, followed by a vector of upper bounds. We want to copy a range of
    // elements into m_pObjectCopy following these vectors, so we need to compute the address where the
    // vectors end and the elements begin.
    const int cbHeader = 2 * m_type->m_rank * sizeof(DWORD);
    HRESULT hr = S_OK;

    // Ensure that the proper subset is in the cache. m_idxLower and m_idxUpper are initialized to -1, so the
    // first time we hit this condition check, it will evaluate to true. We will set these inside the
    // consequent to the range starting at nPosition and ending at the last available cache position. Thus,
    // after the first time we hit this, we are asking if nPosition lies outside the range we've cached.
    if (nPosition < m_idxLower || nPosition >= m_idxUpper)
    {
        const SIZE_T cbElemSize = m_info.arrayInfo.elementSize;
        SIZE_T len = 1;

        if (cbElemSize != 0)
        {
        // the element size could be bigger than the cache, but we want len to be at least 1.
            len = max(ARRAY_CACHE_SIZE / cbElemSize, len);
        }
        else  _ASSERTE(cbElemSize != 0);

        m_idxLower = nPosition;
        m_idxUpper = min(m_idxLower + len, m_info.arrayInfo.componentCount);
        _ASSERTE(m_idxLower < m_idxUpper);

        SIZE_T cbOffsetFrom = m_info.arrayInfo.offsetToArrayBase + m_idxLower * cbElemSize;

        SIZE_T cbSize = (m_idxUpper - m_idxLower) * cbElemSize; // we'll copy the largest range of ellements possible

        _ASSERTE(cbSize <= m_info.objSize);
        // Copy the proper subrange of the array over
        EX_TRY
        {
            m_valueHome.GetInternalValue(MemoryRange(m_pObjectCopy + cbHeader, cbSize), cbOffsetFrom); // throws
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);
    }

    SIZE_T size = m_info.arrayInfo.elementSize;
    _ASSERTE(size <= m_info.objSize);

    SIZE_T offset = m_info.arrayInfo.offsetToArrayBase + (nPosition * size);
    void * localAddress = m_pObjectCopy + cbHeader + ((nPosition - m_idxLower) * size);

    EX_TRY
    {
	    m_valueHome.CreateInternalValue(m_elemtype,
                                        offset,
                                        localAddress,
                                        (ULONG32)size,
                                        ppValue); // throws
    }
    EX_CATCH_HRESULT(hr);
    return hr;

} // CordbArrayValue::GetElementAtPosition

HRESULT CordbArrayValue::IsValid(BOOL *pbValid)
{
    VALIDATE_POINTER_TO_OBJECT(pbValid, BOOL *);

    // <TODO>@todo: implement tracking of objects across collections.</TODO>

    return E_NOTIMPL;
}

HRESULT CordbArrayValue::CreateRelocBreakpoint(
                                      ICorDebugValueBreakpoint **ppBreakpoint)
{
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugValueBreakpoint **);

    return E_NOTIMPL;
}

// Creates a handle of the given type for this heap value.
// Not Implemented In-Proc.
// Arguments:
//     input:  handleType - type of the handle to be created
//     output: ppHandle   - on success, the newly created handle
// Return Value: S_OK on success or E_INVALIDARG, E_OUTOFMEMORY, or CORDB_E_HELPER_MAY_DEADLOCK
HRESULT CordbArrayValue::CreateHandle(
    CorDebugHandleType handleType,
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(handleType, ppHandle);
}   // CordbArrayValue::CreateHandle

/*
* Creates a pinned handle for this heap value.
* Not Implemented In-Proc.
* Arguments:
*     output: ppHandle   - on success, the newly created handle
* Return Value: S_OK on success or E_INVALIDARG, E_OUTOFMEMORY, or CORDB_E_HELPER_MAY_DEADLOCK
*/
HRESULT CordbArrayValue::CreatePinnedHandle(
    ICorDebugHandleValue ** ppHandle)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbValue::InternalCreateHandle(HANDLE_PINNED, ppHandle);
}   // CreatePinnedHandle

// get a copy of the array
// Arguments
//     output: pTo - pointer to a caller-allocated and managed buffer to hold the copy. The caller must guarantee
//             that this is large enough to hold the entire array
// Return Value: S_OK on success, E_INVALIDARG or read process memory errors on failure
HRESULT CordbArrayValue::GetValue(void *pTo)
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pTo, void *, 1, false, true);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Copy out the value, which is the whole array.
        // There's no lazy-evaluation here, so this could be rather large
        m_valueHome.GetValue(MemoryRange(pTo, m_size));  // throws
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbArrayValue::GetValue

HRESULT CordbArrayValue::SetValue(void *pFrom)
{
    // You're not allowed to set a whole array at once.
    return E_INVALIDARG;
}

// initialize a new instance of CordbArrayValue
// Arguments: none
// Return Value: S_OK on success or E_OUTOFMEMORY or read process memory errors on failure
// Note: we are only initializing information about the array (rank, sizes, dimensions, etc) here. We will not
//       attempt to read array contents until we receive a request to do so.
HRESULT CordbArrayValue::Init()
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //
    HRESULT hr = S_OK;

    SIZE_T cbVector = m_info.arrayInfo.rank * sizeof(DWORD);
    _ASSERTE(cbVector <= m_info.objSize);

    int cbHeader = 2 * (int)cbVector;

    // Find largest data size that will fit in cache
    SIZE_T cbData = m_info.arrayInfo.componentCount * m_info.arrayInfo.elementSize;
    if (cbData > ARRAY_CACHE_SIZE)
    {
        cbData = (ARRAY_CACHE_SIZE / m_info.arrayInfo.elementSize)
            * m_info.arrayInfo.elementSize;
    }

    if (cbData < m_info.arrayInfo.elementSize)
    {
        cbData = m_info.arrayInfo.elementSize;
    }

    // Allocate memory
    m_pObjectCopy = new (nothrow) BYTE[cbHeader + cbData];
    if (m_pObjectCopy == NULL)
        return E_OUTOFMEMORY;


    m_arrayLowerBase  = NULL;
    m_arrayUpperBase  = NULL;

    // Copy base vectors into header. (Offsets are 0 if the vectors aren't used)
    if (m_info.arrayInfo.offsetToLowerBounds != 0)
    {
        m_arrayLowerBase  = (DWORD*)(m_pObjectCopy);
        EX_TRY
        {
            m_valueHome.GetInternalValue(MemoryRange(m_arrayLowerBase, cbVector),
                                                     m_info.arrayInfo.offsetToLowerBounds); // throws
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);
    }


    if (m_info.arrayInfo.offsetToUpperBounds != 0)
    {
        m_arrayUpperBase  = (DWORD*)(m_pObjectCopy + cbVector);
        EX_TRY
        {
            m_valueHome.GetInternalValue(MemoryRange(m_arrayUpperBase, cbVector),
                                                     m_info.arrayInfo.offsetToUpperBounds); // throws
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);
    }

    // That's all for now. We'll do lazy-evaluation for the array contents.

    return hr;
} // CordbArrayValue::Init

// CordbArrayValue::GetThreadOwningMonitorLock
// If a managed thread owns the monitor lock on this object then *ppThread
// will point to that thread and S_OK will be returned. The thread object is valid
// until the thread exits. *pAcquisitionCount will indicate the number of times
// this thread would need to release the lock before it returns to being
// unowned.
// If no managed thread owns the monitor lock on this object then *ppThread
// and pAcquisitionCount will be unchanged and S_FALSE returned.
// If ppThread or pAcquisitionCount is not a valid pointer the result is
// undefined.
// If any error occurs such that it cannot be determined which, if any, thread
// owns the monitor lock on this object then a failing HRESULT will be returned
HRESULT CordbArrayValue::GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetThreadOwningMonitorLock(GetProcess(),
        GetValueHome()->GetAddress(), ppThread, pAcquisitionCount);
}

// CordbArrayValue::GetMonitorEventWaitList
// Provides an ordered list of threads which are queued on the event associated
// with a monitor lock. The first thread in the list is the first thread which
// will be released by the next call to Monitor.Pulse, the next thread in the list
// will be released on the following call, and so on.
// If this list is non-empty S_OK will be returned, if it is empty S_FALSE
// will be returned (the enumeration is still valid, just empty).
// In either case the enumeration interface is only usable for the duration
// of the current synchronized state, however the threads interfaces dispensed
// from it are valid until the thread exits.
// If ppThread is not a valid pointer the result is undefined.
// If any error occurs such that it cannot be determined which, if any, threads
// are waiting for the monitor then a failing HRESULT will be returned
HRESULT CordbArrayValue::GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CordbHeapValue3Impl::GetMonitorEventWaitList(GetProcess(),
                                                        GetValueHome()->GetAddress(),
                                                        ppThreadEnum);
}

/* ------------------------------------------------------------------------- *
 * Handle Value
 * ------------------------------------------------------------------------- */
// constructor
// Arguments:
//     input:
//         pAppDomain  - app domain to which the value belongs
//         pType       - type information for the value
//         handleType  - indicates whether we are constructing a strong or weak handle
CordbHandleValue::CordbHandleValue(
    CordbAppDomain *   pAppdomain,
    CordbType *        pType,             // The type of object that we create handle on
    CorDebugHandleType handleType)         // strong or weak handle
    : CordbValue(pAppdomain, pType, NULL, false,
                    pAppdomain->GetSweepableExitNeuterList()
                )
{
    m_vmHandle = VMPTR_OBJECTHANDLE::NullPtr();
    m_fCanBeValid = TRUE;

    m_handleType = handleType;
    m_size = sizeof(void*);
} // CordbHandleValue::CordbHandleValue

//-----------------------------------------------------------------------------
// Assign internal handle to the given value, and update pertinent counters
//
// Arguments:
//    handle - non-null CLR ObjectHandle that this CordbHandleValue will represent
//
// Notes:
//    Call code:CordbHandleValue::ClearHandle to clear the handle value.
void CordbHandleValue::AssignHandle(VMPTR_OBJECTHANDLE handle)
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());
    _ASSERTE(m_vmHandle.IsNull());

    // Use code:CordbHandleValue::ClearHandle to clear the handle value.
    _ASSERTE(!handle.IsNull());

    m_vmHandle = handle;
    GetProcess()->IncrementOutstandingHandles();
}

//-----------------------------------------------------------------------------
// Clear the handle value
//
// Assumptions:
//    Caller only clears if not already cleared.
//
// Notes:
//    This is the inverse of code:CordbHandleValue::AssignHandle
void CordbHandleValue::ClearHandle()
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());
    _ASSERTE(!m_vmHandle.IsNull());

    m_vmHandle = VMPTR_OBJECTHANDLE::NullPtr();
    GetProcess()->DecrementOutstandingHandles();
}

// initialize a new instance of CordbHandleValue
// Arguments:
//     input: pHandle - non-null CLR ObjectHandle that this CordbHandleValue will represent
// Return Value: S_OK on success or CORDBG_E_TARGET_INCONSISTENT, E_INVALIDARG, read process memory errors.
HRESULT CordbHandleValue::Init(VMPTR_OBJECTHANDLE pHandle)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    HRESULT hr = S_OK;

    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        // If it is a strong handle, m_pHandle will not be NULL unless Dispose method is called.
        // If it is a weak handle, m_pHandle can be NULL when Dispose is called.
        AssignHandle(pHandle);
    }

    // This will init m_info.
    IfFailRet(RefreshHandleValue());

    // objRefBad is currently overloaded to mean that 1) the object ref is invalid, or 2) the object ref is NULL.
    // NULL is clearly not a bad object reference, but in either case we have no more type data to work with,
    // so don't attempt to assign more specific type information to the reference.
    if (!m_info.objRefBad)
    {
        // We need to get the type info from the left side.
        CordbType *newtype;

        IfFailRet(CordbType::TypeDataToType(m_appdomain, &m_info.objTypeData, &newtype));

        m_type.Assign(newtype);
    }

    return hr;
} // CordbHandleValue::Init

// destructor
CordbHandleValue::~CordbHandleValue()
{
    DTOR_ENTRY(this);

    _ASSERTE(IsNeutered());
} // CordbHandleValue::~CordbHandleValue

// Free left-side resources, mainly the GC handle keeping the object alive.
void CordbHandleValue::NeuterLeftSideResources()
{
    Dispose();

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    Neuter();
} // CordbHandleValue::NeuterLeftSideResources

// Neuter
// Notes:
//   CordbHandleValue may hold Left-Side resources via the GC handle.
//   By the time we neuter it, those resources must have been freed,
//   either explicitly by calling code:CordbHandleValue::Dispose, or
//   implicitly by the left-side process exiting.
void CordbHandleValue::Neuter()
{
    // CordbHandleValue is on the AppDomainExit neuter list.

    // We should have cleaned up our Left-side resource by now (m_vmHandle
    // should be null). If AppDomain / Process has already exited, then the LS
    // already cleaned them up for us, and so we don't worry about them.
    bool fAppDomainIsAlive = (m_appdomain != NULL && !m_appdomain->IsNeutered());
    if (fAppDomainIsAlive)
    {
        BOOL fTargetIsDead = !GetProcess()->IsSafeToSendEvents() || GetProcess()->m_exiting;
        if (!fTargetIsDead)
        {
            _ASSERTE(m_vmHandle.IsNull());
        }
    }

    CordbValue::Neuter();
} // CordbHandleValue::Neuter

// Helper: Refresh the handle value object.
// Gets information about the object to which the handle points.
// Arguments: none
// Return Value: S_OK on success, CORDBG_E_HANDLE_HAS_BEEN_DISPOSED, CORDBG_E_BAD_REFERENCE_VALUE,
//               errors from read process memory.
HRESULT CordbHandleValue::RefreshHandleValue()
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    // If Dispose has been called, don't bother to refresh handle value.
    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    // If weak handle and the object was dead, no point to refresh the handle value
    if (m_fCanBeValid == FALSE)
    {
        return CORDBG_E_BAD_REFERENCE_VALUE;
    }

    HRESULT hr = S_OK;
    CorElementType type = m_type->m_elementType;

    _ASSERTE((m_pProcess != NULL));

    _ASSERTE (type != ELEMENT_TYPE_GENERICINST);
    _ASSERTE (type != ELEMENT_TYPE_VAR);
    _ASSERTE (type != ELEMENT_TYPE_MVAR);

    CordbProcess * pProcess = GetProcess();
    void * objectAddress = NULL;
    CORDB_ADDRESS objectHandle = 0;

    EX_TRY
    {
        objectHandle = pProcess->GetDAC()->GetHandleAddressFromVmHandle(m_vmHandle);
        if (type != ELEMENT_TYPE_TYPEDBYREF)
        {
            pProcess->SafeReadBuffer(TargetBuffer(objectHandle, sizeof(void *)), (BYTE *)&objectAddress);
        }
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);
    EX_TRY
    {
        if (type == ELEMENT_TYPE_TYPEDBYREF)
        {
            CordbReferenceValue::GetTypedByRefData(pProcess,
                                                   objectHandle,
                                                   type,
                                                   m_appdomain->GetADToken(),
                                                   &m_info);
        }
        else
        {
            CordbReferenceValue::GetObjectData(pProcess,
                                               objectAddress,
                                               type,
                                               m_appdomain->GetADToken(),
                                               &m_info);
        }
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    // If reference is already gone bad or reference is NULL,
    // don't bother to refetch in the future.
    //
    if ((m_info.objRefBad) || (m_info.objRef == NULL))
    {
        m_fCanBeValid = FALSE;
    }

    return hr;
}
 // CordbHandleValue::RefreshHandleValue

HRESULT CordbHandleValue::QueryInterface(REFIID id, void **pInterface)
{
    VALIDATE_POINTER_TO_OBJECT(pInterface, void **);

    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(this);
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugReferenceValue)
    {
        *pInterface = static_cast<ICorDebugReferenceValue*>(this);
    }
    else if (id == IID_ICorDebugHandleValue)
    {
        *pInterface = static_cast<ICorDebugHandleValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugHandleValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
} // CordbHandleValue::QueryInterface


// return handle type. Currently we have strong and weak.
// Arguments:
//     output: pType - the handle type unless pType is null
// Return Value: S_OK on success or E_INVALIDARG or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED on failure
HRESULT CordbHandleValue::GetHandleType(CorDebugHandleType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pType, CorDebugHandleType *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    if (m_vmHandle.IsNull())
    {
        // handle has been disposed!
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }
    *pType = m_handleType;
    return S_OK;
} // CordbHandleValue::GetHandleType

// Dispose will cause handle to be recycled.
// Arguments: none
// Return Value: S_OK on success, CORDBG_E_HANDLE_HAS_BEEN_DISPOSED or errors from the
// DB_IPCE_DISPOSE_HANDLE event

// @dbgtodo Microsoft inspection: remove the dispose handle hresults when the IPC events are eliminated
HRESULT CordbHandleValue::Dispose()
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    HRESULT             hr = S_OK;
    DebuggerIPCEvent    event;
    CordbProcess        *process;

    process = GetProcess();

    // Process should still be alive because it would have neutered us if it became invalid.
    _ASSERTE(process != NULL);

    VMPTR_OBJECTHANDLE vmObjHandle = VMPTR_OBJECTHANDLE::NullPtr();
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        if (m_vmHandle.IsNull())
        {
            // handle has been disposed!
            return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
        }

        vmObjHandle = m_vmHandle;
        ClearHandle(); // set m_pHandle to null.

        if (process->m_exiting)
        {
            // process is exiting. Don't do anything
            return S_OK;
        }
    }

    // recycle the handle to EE
    process->InitIPCEvent(&event,
                          DB_IPCE_DISPOSE_HANDLE,
                          false,
                          m_appdomain->GetADToken());

    event.DisposeHandle.vmObjectHandle = vmObjHandle;
    event.DisposeHandle.handleType = m_handleType;

    // Note: one-way event here...
    hr = process->SendIPCEvent(&event, sizeof(DebuggerIPCEvent));

    hr = WORST_HR(hr, event.hr);

    return hr;
}   // CordbHandleValue::Dispose

// get the type of the object to which the handle points
// Arguments:
//     output: pType - the object type on success
// Return Value: S_OK on success, CORDBG_E_HANDLE_HAS_BEEN_DISPOSED, CORDBG_E_CLASS_NOT_LOADED or synchronization errors on
// failure
HRESULT CordbHandleValue::GetType(CorElementType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    HRESULT     hr = S_OK;

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    bool isBoxedVCObject = false;
    if ((m_type->m_pClass != NULL) && (m_type->m_elementType != ELEMENT_TYPE_STRING))
    {
        EX_TRY
        {
            isBoxedVCObject = m_type->m_pClass->IsValueClass();
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
            return hr;
    }

    if (isBoxedVCObject)
    {
        // if we create the handle to a boxed value type, then the type is
        // E_T_CLASS. m_type is the underlying value type. That is incorrect to
        // return.
        //
        *pType = ELEMENT_TYPE_CLASS;
        return S_OK;
    }

    return m_type->GetType(pType);
}   // CordbHandleValue::GetType

// get the size of the handle-- this will always return the size of the handle itself (just pointer size), so
// it's not particularly interesting.
// Arguments:
//     output: pSize - the size of the handle (on success). This must be non-null. Memory management belongs
//                     to the caller.
// Return Value: S_OK on success, E_INVALIDARG (if pSize is null), or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED on failure
HRESULT CordbHandleValue::GetSize(ULONG32 *pSize)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pSize, ULONG32 *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    if (m_size > UINT32_MAX)
    {
        *pSize = UINT32_MAX;
        return (COR_E_OVERFLOW);
    }

    //return the size of reference
    *pSize = (ULONG)m_size;
    return S_OK;
}   // CordbHandleValue::GetSize

// get the size of the handle-- this will always return the size of the handle itself (just pointer size), so
// it's not particularly interesting.
// Arguments:
//     output: pSize - the size of the handle (on success). This must be non-null. Memory management belongs
//                     to the caller.
// Return Value: S_OK on success, E_INVALIDARG (if pSize is null), or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED on failure
HRESULT CordbHandleValue::GetSize64(ULONG64 *pSize)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pSize, ULONG64 *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    //return the size of reference
    *pSize = m_size;
    return S_OK;
}   // CordbHandleValue::GetSize

// Get the target address of the handle
// Arguments:
//     output: pAddress - handle address on success. This must be non-null and memory is managed by the caller
// Return Value: S_OK on success or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED or E_INVALIDARG on failure
HRESULT CordbHandleValue::GetAddress(CORDB_ADDRESS *pAddress)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        *pAddress = GetProcess()->GetDAC()->GetHandleAddressFromVmHandle(m_vmHandle);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}   // CordbHandleValue::GetAddress

HRESULT CordbHandleValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
{
    return E_NOTIMPL;
}   // CreateBreakpoint

// indicates whether a handle is null
// Arguments:
//     output: pbNull - true iff the handle is null and pbNull is non-null.Memory is managed by the caller
// Return Value: S_OK on success or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED or E_INVALIDARG, CORDBG_E_BAD_REFERENCE_VALUE,
//               errors from read process memory.
HRESULT CordbHandleValue::IsNull(BOOL *pbNull)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pbNull, BOOL *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    HRESULT         hr = S_OK;

    *pbNull = FALSE;

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }


    // Only return true if handle is long weak handle and is disposed.
    if (m_handleType == HANDLE_WEAK_TRACK_RESURRECTION)
    {
        hr = RefreshHandleValue();
        if (FAILED(hr))
        {
            return hr;
        }

        if (m_info.objRef == NULL)
        {
            *pbNull = TRUE;
        }
    }
    else if (m_info.objRef == NULL)
    {
        *pbNull = TRUE;
    }

    // strong handle always return false for IsNull

    return S_OK;
}   // CordbHandleValue::IsNull

// gets a copy of the value of the handle
// Arguments:
//     output: pValue - handle { on success. This must be non-null and memory is managed by the caller
// Return Value: S_OK on success or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED or E_INVALIDARG, CORDBG_E_BAD_REFERENCE_VALUE,
//               errors from read process memory.
HRESULT CordbHandleValue::GetValue(CORDB_ADDRESS *pValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pValue, CORDB_ADDRESS *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    RefreshHandleValue();
    *pValue = PTR_TO_CORDB_ADDRESS(m_info.objRef);
    return S_OK;
}   // CordbHandleValue::GetValue

HRESULT CordbHandleValue::SetValue(CORDB_ADDRESS value)
{
    // do not support SetValue on Handle
    return E_FAIL;
}   // CordbHandleValue::GetValue

// get an ICDValue to represent the object to which the handle refers
// Arguments:
//     output: ppValue - pointer to the ICDValue for the handle referent as long as ppValue is non-null
// Return Value: S_OK on success or CORDBG_E_HANDLE_HAS_BEEN_DISPOSED or E_INVALIDARG, CORDBG_E_BAD_REFERENCE_VALUE,
//               errors from read process memory.
HRESULT CordbHandleValue::Dereference(ICorDebugValue **ppValue)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    _ASSERTE(m_appdomain != NULL);
    _ASSERTE(!m_appdomain->IsNeutered());

    *ppValue = NULL;

    if (m_vmHandle.IsNull())
    {
        return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
    }

    hr = RefreshHandleValue();
    if (FAILED(hr))
    {
        return hr;
    }

    if ((m_info.objRefBad) || (m_info.objRef == NULL))
    {
        return CORDBG_E_BAD_REFERENCE_VALUE;
    }

    EX_TRY
    {
        hr = CordbReferenceValue::DereferenceCommon(m_appdomain,
        m_type,
        NULL, // don't support typed-by-refs
        &m_info,
        ppValue);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}   // CordbHandleValue::Dereference

HRESULT CordbHandleValue::DereferenceStrong(ICorDebugValue **ppValue)
{
    return E_NOTIMPL;
}

// CordbHeapValue3Impl::GetThreadOwningMonitorLock
// If a managed thread owns the monitor lock on this object then *ppThread
// will point to that thread and S_OK will be returned. The thread object is valid
// until the thread exits. *pAcquisitionCount will indicate the number of times
// this thread would need to release the lock before it returns to being
// unowned.
// If no managed thread owns the monitor lock on this object then *ppThread
// and pAcquisitionCount will be unchanged and S_FALSE returned.
// If ppThread or pAcquisitionCount is not a valid pointer the result is
// undefined.
// If any error occurs such that it cannot be determined which, if any, thread
// owns the monitor lock on this object then a failing HRESULT will be returned
HRESULT CordbHeapValue3Impl::GetThreadOwningMonitorLock(CordbProcess* pProcess,
                                                        CORDB_ADDRESS remoteObjAddress,
                                                        ICorDebugThread **ppThread,
                                                        DWORD *pAcquisitionCount)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        IDacDbiInterface *pDac = pProcess->GetDAC();
        VMPTR_Object vmObj = pDac->GetObject(remoteObjAddress);
        MonitorLockInfo info = pDac->GetThreadOwningMonitorLock(vmObj);
        if(info.acquisitionCount == 0)
        {
            // unowned
            *ppThread = NULL;
            *pAcquisitionCount = 0;
            hr = S_FALSE;
        }
        else
        {
            RSLockHolder lockHolder(pProcess->GetProcessLock());
            CordbThread* pThread = pProcess->LookupOrCreateThread(info.lockOwner);
            pThread->QueryInterface(__uuidof(ICorDebugThread), (VOID**) ppThread);
            *pAcquisitionCount = info.acquisitionCount;
            hr = S_OK;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// A small helper for CordbHeapValue3Impl::GetMonitorEventWaitList that adds each enumerated thread to an array
// Arguments:
//   vmThread - The thread to add
//   puserData - the array to add it to
VOID ThreadEnumerationCallback(VMPTR_Thread vmThread, VOID* pUserData)
{
    CQuickArrayList<VMPTR_Thread>* pThreadList = (CQuickArrayList<VMPTR_Thread>*) pUserData;
    pThreadList->Push(vmThread);
}

// CordbHeapValue3Impl::GetMonitorEventWaitList
// Provides an ordered list of threads which are queued on the event associated
// with a monitor lock. The first thread in the list is the first thread which
// will be released by the next call to Monitor.Pulse, the next thread in the list
// will be released on the following call, and so on.
// If this list is non-empty S_OK will be returned, if it is empty S_FALSE
// will be returned (the enumeration is still valid, just empty).
// In either case the enumeration interface is only usable for the duration
// of the current synchronized state, however the threads interfaces dispensed
// from it are valid until the thread exits.
// If ppThread is not a valid pointer the result is undefined.
// If any error occurs such that it cannot be determined which, if any, threads
// are waiting for the monitor then a failing HRESULT will be returned
HRESULT CordbHeapValue3Impl::GetMonitorEventWaitList(CordbProcess* pProcess,
                                                     CORDB_ADDRESS remoteObjAddress,
                                                     ICorDebugThreadEnum **ppThreadEnum)
{
    HRESULT hr = S_OK;
    RSSmartPtr<CordbThread> *rsThreads = NULL;
    EX_TRY
    {
        IDacDbiInterface *pDac = pProcess->GetDAC();
        VMPTR_Object vmObj = pDac->GetObject(remoteObjAddress);
        CQuickArrayList<VMPTR_Thread> threads;
        pDac->EnumerateMonitorEventWaitList(vmObj,
            (IDacDbiInterface::FP_THREAD_ENUMERATION_CALLBACK)ThreadEnumerationCallback, (VOID*)&threads);

        rsThreads = new RSSmartPtr<CordbThread>[threads.Size()];
        {
            RSLockHolder lockHolder(pProcess->GetProcessLock());
            for(DWORD i = 0; i < threads.Size(); i++)
            {
                rsThreads[i].Assign(pProcess->LookupOrCreateThread(threads[i]));
            }
        }

        CordbThreadEnumerator* threadEnum =
            new CordbThreadEnumerator(pProcess, rsThreads, (DWORD)threads.Size());
        pProcess->GetContinueNeuterList()->Add(pProcess, threadEnum);
        threadEnum->QueryInterface(__uuidof(ICorDebugThreadEnum), (VOID**)ppThreadEnum);
        if(threads.Size() == 0)
        {
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);
    delete [] rsThreads;
    return hr;
}
