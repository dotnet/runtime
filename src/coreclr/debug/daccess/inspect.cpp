// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: inspect.cpp
//

//
// ClrData object inspection.
//
//*****************************************************************************

#include "stdafx.h"


HRESULT
InitFieldIter(DeepFieldDescIterator* fieldIter,
              TypeHandle typeHandle,
              bool canHaveFields,
              ULONG32 flags,
              IXCLRDataTypeInstance* fromType)
{
    // Currently we can't filter on kinds so
    // require them all.
    if ((flags & ~CLRDATA_FIELD_ALL_FIELDS) != 0 ||
        (flags & CLRDATA_TYPE_ALL_KINDS) != CLRDATA_TYPE_ALL_KINDS ||
        (flags & CLRDATA_FIELD_ALL_LOCATIONS) == 0)
    {
        return E_INVALIDARG;
    }

    if (!canHaveFields)
    {
        // Leave default empty initialization.
        return S_OK;
    }

    int fieldIterFlags = ApproxFieldDescIterator::ALL_FIELDS;

    if ((flags & CLRDATA_FIELD_FROM_INSTANCE) == 0)
    {
        fieldIterFlags &= ~ApproxFieldDescIterator::INSTANCE_FIELDS;
    }
    if ((flags & CLRDATA_FIELD_FROM_STATIC) == 0)
    {
        fieldIterFlags &= ~ApproxFieldDescIterator::STATIC_FIELDS;
    }

    bool includeParents;

    if ((flags & CLRDATA_FIELD_IS_INHERITED) == 0)
    {
        if (fromType)
        {
            typeHandle = ((ClrDataTypeInstance*)fromType)->GetTypeHandle();
        }
        includeParents = false;
    }
    else if (fromType)
    {
        return E_INVALIDARG;
    }
    else
    {
        includeParents = true;
    }

    if (typeHandle.IsNull() ||
        !typeHandle.GetMethodTable() ||
        !typeHandle.IsRestored())
    {
        return E_INVALIDARG;
    }

    fieldIter->Init(typeHandle.GetMethodTable(), fieldIterFlags, includeParents);

    return S_OK;
}

ULONG32
GetTypeFieldValueFlags(TypeHandle typeHandle,
                       FieldDesc* fieldDesc,
                       ULONG32 otherFlags,
                       bool isDeref)
{
    otherFlags &= ~(CLRDATA_VALUE_IS_PRIMITIVE |
                    CLRDATA_VALUE_IS_VALUE_TYPE |
                    CLRDATA_VALUE_IS_STRING |
                    CLRDATA_VALUE_IS_ARRAY |
                    CLRDATA_VALUE_IS_REFERENCE |
                    CLRDATA_VALUE_IS_POINTER |
                    CLRDATA_VALUE_IS_ENUM);

    CorElementType eltType;

    if (fieldDesc)
    {
        eltType = fieldDesc->GetFieldType();
    }
    else
    {
        _ASSERTE(!typeHandle.IsNull());
        eltType = typeHandle.GetInternalCorElementType();
    }

    if (!isDeref && CorTypeInfo::IsObjRef_NoThrow(eltType))
    {
        otherFlags |= CLRDATA_VALUE_IS_REFERENCE;
    }
    else if (typeHandle.IsEnum())
    {
        otherFlags |= CLRDATA_VALUE_IS_ENUM;
    }
    else if (eltType == ELEMENT_TYPE_STRING)
    {
        otherFlags |= CLRDATA_VALUE_IS_STRING;
    }
    else if (eltType == ELEMENT_TYPE_PTR)
    {
        otherFlags |= CLRDATA_VALUE_IS_POINTER;
    }
    else if (CorTypeInfo::IsPrimitiveType_NoThrow(eltType))
    {
        otherFlags |= CLRDATA_VALUE_IS_PRIMITIVE;
    }
    else if (typeHandle.IsArray())
    {
        otherFlags |= CLRDATA_VALUE_IS_ARRAY;
    }
    else if (typeHandle.IsValueType())
    {
        otherFlags |= CLRDATA_VALUE_IS_VALUE_TYPE;
    }
    else if (eltType == ELEMENT_TYPE_CLASS)
    {
        //
        // Perform extra checks to identify well-known classes.
        //

        if ((&g_CoreLib)->IsClass(typeHandle.GetMethodTable(), CLASS__STRING))
        {
            otherFlags |= CLRDATA_VALUE_IS_STRING;
        }
    }

    if (fieldDesc)
    {
        otherFlags &= ~(CLRDATA_VALUE_IS_LITERAL |
                        CLRDATA_VALUE_FROM_INSTANCE |
                        CLRDATA_VALUE_FROM_TASK_LOCAL |
                        CLRDATA_VALUE_FROM_STATIC);

        if ((isDeref ||
             (otherFlags & CLRDATA_VALUE_IS_REFERENCE) == 0) &&
            IsFdLiteral(fieldDesc->GetAttributes()))
        {
            otherFlags |= CLRDATA_VALUE_IS_LITERAL;
        }

        if (fieldDesc->IsStatic())
        {
            otherFlags |= CLRDATA_VALUE_FROM_STATIC;
        }
        else if (fieldDesc->IsThreadStatic())
        {
            otherFlags |= CLRDATA_VALUE_FROM_TASK_LOCAL;
        }
        else
        {
            otherFlags |= CLRDATA_VALUE_FROM_INSTANCE;
        }
    }

    return otherFlags;
}

//----------------------------------------------------------------------------
//
// ClrDataValue.
//
//----------------------------------------------------------------------------

ClrDataValue::ClrDataValue(ClrDataAccess* dac,
                           AppDomain* appDomain,
                           Thread* thread,
                           ULONG32 flags,
                           TypeHandle typeHandle,
                           ULONG64 baseAddr,
                           ULONG32 numLocs,
                           NativeVarLocation* locs)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_appDomain = appDomain;
    m_thread = thread;
    m_flags = flags;
    m_typeHandle = typeHandle;
    m_baseAddr = baseAddr;
    m_numLocs = numLocs;
    if (numLocs)
    {
        memcpy(m_locs, locs, numLocs * sizeof(m_locs[0]));
    }

    if (numLocs && (m_flags & CLRDATA_VALUE_IS_REFERENCE) != 0)
    {
        m_totalSize = sizeof(TADDR);
    }
    else
    {
        m_totalSize = 0;
        for (ULONG32 i = 0; i < m_numLocs; i++)
        {
            m_totalSize += m_locs[i].size;
        }
    }
}

ClrDataValue::~ClrDataValue(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataValue::QueryInterface(THIS_
                             IN REFIID interfaceId,
                             OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataValue)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataValue*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataValue::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataValue::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = m_flags;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetAddress(
    /* [out] */ CLRDATA_ADDRESS *address)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // This query can only be answered if there's a
        // single non-register address.
        if (m_numLocs == 1 &&
            !m_locs[0].contextReg)
        {
            *address = TO_CDADDR(m_locs[0].addr);
            status = S_OK;
        }
        else
        {
            status = E_NOINTERFACE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetSize(
    /* [out] */ ULONG64 *size)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_totalSize)
        {
            *size = m_totalSize;
            status = S_OK;
        }
        else
        {
            status = E_NOINTERFACE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataValue::IntGetBytes(
    /* [in] */ ULONG32 bufLen,
    /* [size_is][out] */ BYTE buffer[  ])
{
    HRESULT status;

    NativeVarLocation* loc = m_locs;
    for (ULONG32 i = 0; i < m_numLocs; i++)
    {
        if (loc->contextReg)
        {
            memcpy(buffer, (void*)(ULONG_PTR)loc->addr, loc->size);
            buffer += loc->size;
        }
        else
        {
            ULONG32 done;

            _ASSERTE(FitsIn<ULONG32>(loc->size));
            status = m_dac->m_pTarget->
                ReadVirtual(loc->addr, buffer, static_cast<ULONG32>(loc->size),
                            &done);
            if (status != S_OK)
            {
                return CORDBG_E_READVIRTUAL_FAILURE;
            }
            if (done != loc->size)
            {
                return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
            }

            buffer += loc->size;
        }

        loc++;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetBytes(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *dataSize,
    /* [size_is][out] */ BYTE buffer[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_totalSize)
        {
            status = E_NOINTERFACE;
            goto Exit;
        }

        if (dataSize)
        {
            _ASSERTE(FitsIn<ULONG32>(m_totalSize));
            *dataSize = static_cast<ULONG32>(m_totalSize);
        }

        if (bufLen < m_totalSize)
        {
            status = HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW);
            goto Exit;
        }

        status = IntGetBytes(bufLen, buffer);

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::SetBytes(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *dataSize,
    /* [size_is][in] */ BYTE buffer[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        NativeVarLocation* loc = NULL;

        if (!m_totalSize)
        {
            status = E_NOINTERFACE;
            goto Exit;
        }

        if (dataSize)
        {
            _ASSERTE(FitsIn<ULONG32>(m_totalSize));
            *dataSize = static_cast<ULONG32>(m_totalSize);
        }

        if (bufLen < m_totalSize)
        {
            status = HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW);
            goto Exit;
        }

        loc = m_locs;
        for (ULONG32 i = 0; i < m_numLocs; i++)
        {
            if (loc->contextReg)
            {
                // XXX Microsoft - Context update?
                // memcpy(buffer, (void*)(ULONG_PTR)loc->addr, loc->size);
                // buffer += loc->size;
                // until drew decides, return notimpl
                status = E_NOTIMPL;
                goto Exit;
            }
            else
            {
                _ASSERT(FitsIn<ULONG32>(loc->size));
                status = m_dac->m_pMutableTarget->
                    WriteVirtual(loc->addr, buffer, static_cast<ULONG32>(loc->size));
                if (status != S_OK)
                {
                    goto Exit;
                }

                buffer += loc->size;
            }

            loc++;
        }

        status = S_OK;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetType(
    /* [out] */ IXCLRDataTypeInstance **typeInstance)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if ((m_flags & CLRDATA_VALUE_IS_REFERENCE) != 0)
        {
            *typeInstance = NULL;
            status = S_FALSE;
        }
        else if (!m_appDomain ||
                 m_typeHandle.IsNull())
        {
            status = E_NOTIMPL;
        }
        else
        {
            *typeInstance = new (nothrow)
                ClrDataTypeInstance(m_dac, m_appDomain, m_typeHandle);
            status = *typeInstance ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetNumFields(
    /* [out] */ ULONG32 *numFields)
{
    // XXX Microsoft - Obsolete method, never implemented.
    return E_UNEXPECTED;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetFieldByIndex(
    /* [in] */ ULONG32 index,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ mdFieldDef *token)
{
    // XXX Microsoft - Obsolete method, never implemented.
    return E_UNEXPECTED;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetNumFields2(
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataTypeInstance *fromType,
    /* [out] */ ULONG32 *numFields)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, CanHaveFields(),
                                    flags, fromType)) == S_OK)
        {
            *numFields = fieldIter.Count();
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::StartEnumFields(
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataTypeInstance *fromType,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::
            CdStartField(NULL,
                         0,
                         flags,
                         fromType,
                         m_typeHandle,
                         NULL,
                         mdTypeDefNil,
                         m_baseAddr,
                         m_thread,
                         NULL,
                         m_appDomain,
                         NULL,
                         NULL,
                         handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EnumField(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ mdFieldDef *token)
{
    return EnumField2(handle, field, nameBufLen, nameLen, nameBuf,
                      NULL, token);
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EnumField2(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, NULL, NULL, field,
                                        nameBufLen, nameLen, nameBuf,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EndEnumFields(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::StartEnumFieldsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 nameFlags,
    /* [in] */ ULONG32 fieldFlags,
    /* [in] */ IXCLRDataTypeInstance *fromType,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::
            CdStartField(name,
                         nameFlags,
                         fieldFlags,
                         fromType,
                         m_typeHandle,
                         NULL,
                         mdTypeDefNil,
                         m_baseAddr,
                         m_thread,
                         NULL,
                         m_appDomain,
                         NULL,
                         NULL,
                         handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EnumFieldByName(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataValue **field,
    /* [out] */ mdFieldDef *token)
{
    return EnumFieldByName2(handle, field, NULL, token);
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EnumFieldByName2(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataValue **field,
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, NULL, NULL, field,
                                        0, NULL, NULL,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::EndEnumFieldsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetFieldByToken(
    /* [in] */ mdFieldDef token,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    return GetFieldByToken2(NULL, token, field, bufLen, nameLen, nameBuf);
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetFieldByToken2(
    /* [in] */ IXCLRDataModule* tokenScope,
    /* [in] */ mdFieldDef token,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, CanHaveFields(),
                                    CLRDATA_VALUE_ALL_FIELDS, NULL)) == S_OK)
        {
            FieldDesc* fieldDesc;

            status = E_INVALIDARG;
            while ((fieldDesc = fieldIter.Next()))
            {
                if ((!tokenScope ||
                     PTR_HOST_TO_TADDR(((ClrDataModule*)tokenScope)->
                                       GetModule()) ==
                     PTR_HOST_TO_TADDR(fieldDesc->GetModule())) &&
                    fieldDesc->GetMemberDef() == token)
                {
                    status = NewFromSubField(fieldDesc,
                                             fieldIter.
                                             IsFieldFromParentClass() ?
                                             CLRDATA_VALUE_IS_INHERITED : 0,
                                             NULL, field,
                                             bufLen, nameLen, nameBuf,
                                             NULL, NULL);
                    break;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataValue::GetRefAssociatedValue(IXCLRDataValue** assocValue)
{
    HRESULT status;

    if (m_typeHandle.IsNull())
    {
        return E_NOINTERFACE;
    }

    TADDR refAddr;

    _ASSERTE(m_totalSize == sizeof(refAddr));

    if ((status = IntGetBytes(sizeof(refAddr),
                              (PBYTE)&refAddr)) != S_OK)
    {
        return status;
    }

    // We assume that objrefs always refer
    // to objects so there is no ref chain.
    ULONG32 valueFlags =
        GetTypeFieldValueFlags(m_typeHandle, NULL,
                               m_flags & CLRDATA_VALUE_ALL_LOCATIONS, true);

    NativeVarLocation loc;

    loc.addr = TO_CDADDR(refAddr);
    // XXX Microsoft - Good way to get the right size for everything?
    loc.size = (m_typeHandle.GetMethodTable())->GetBaseSize();
    loc.contextReg = false;

    *assocValue = new (nothrow)
        ClrDataValue(m_dac,
                     m_appDomain,
                     m_thread,
                     valueFlags,
                     m_typeHandle,
                     loc.addr,
                     1,
                     &loc);
    return *assocValue ? S_OK : E_OUTOFMEMORY;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetAssociatedValue(
    /* [out] */ IXCLRDataValue **assocValue)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_numLocs && (m_flags & CLRDATA_VALUE_IS_REFERENCE) != 0)
        {
            status = GetRefAssociatedValue(assocValue);
        }
        else
        {
            status = E_NOINTERFACE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetAssociatedType(
    /* [out] */ IXCLRDataTypeInstance **assocType)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        TypeHandle dacType;

        if ((m_flags & CLRDATA_VALUE_IS_REFERENCE) != 0)
        {
            dacType = m_typeHandle;
        }
        else if ((m_flags & CLRDATA_VALUE_IS_ARRAY) != 0)
        {
            ArrayBase* arrayBase = PTR_ArrayBase(CLRDATA_ADDRESS_TO_TADDR(m_baseAddr));
            dacType = arrayBase->GetArrayElementTypeHandle();
        }

        if (dacType.IsNull())
        {
            status = E_NOINTERFACE;
        }
        else
        {
            *assocType = new (nothrow)
                ClrDataTypeInstance(m_dac,
                                    m_appDomain,
                                    dacType);
            status = *assocType ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetString(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *strLen,
    /* [size_is][out] */ __out_ecount_part(bufLen, *strLen) WCHAR str[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if ((m_flags & CLRDATA_VALUE_IS_STRING) != 0)
        {
            STRINGREF message = STRINGREF(TO_TADDR(m_baseAddr));

            PWSTR msgStr = DacInstantiateStringW((TADDR)message->GetBuffer(),
                                                 message->GetStringLength(),
                                                 true);

            if (strLen)
            {
                *strLen = static_cast<ULONG32>(wcslen(msgStr) + 1);
            }
            status = StringCchCopy(str, bufLen, msgStr) == S_OK ?
                S_OK : S_FALSE;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetArrayProperties(
    /* [out] */ ULONG32 *rank,
    /* [out] */ ULONG32 *totalElements,
    /* [in] */ ULONG32 numDim,
    /* [size_is][out] */ ULONG32 dims[  ],
    /* [in] */ ULONG32 numBases,
    /* [size_is][out] */ LONG32 bases[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if ((m_flags & CLRDATA_VALUE_IS_ARRAY) != 0)
        {
            ArrayBase* arrayBase = PTR_ArrayBase(CLRDATA_ADDRESS_TO_TADDR(m_baseAddr));
            unsigned baseRank = arrayBase->GetRank();
            unsigned i;

            if (rank)
            {
                *rank = baseRank;
            }

            if (totalElements)
            {
                *totalElements = arrayBase->GetNumComponents();
            }

            if (numDim)
            {
                PTR_INT32 bounds = arrayBase->GetBoundsPtr();

                for (i = 0; i < baseRank; i++)
                {
                    if (i < numDim)
                    {
                        dims[i] = bounds[i];
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (numBases)
            {
                PTR_INT32 lowBounds = arrayBase->GetLowerBoundsPtr();

                for (i = 0; i < baseRank; i++)
                {
                    if (i < numBases)
                    {
                        bases[i] = lowBounds[i];
                    }
                    else
                    {
                        break;
                    }
                }
            }

            status = S_OK;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetArrayElement(
    /* [in] */ ULONG32 numInd,
    /* [size_is][in] */ LONG32 indices[  ],
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        PTR_INT32 bounds, lowBounds;
        TypeHandle eltType;

        if ((m_flags & CLRDATA_VALUE_IS_ARRAY) == 0)
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        ArrayBase* arrayBase;
        unsigned baseRank;

        arrayBase = PTR_ArrayBase(CLRDATA_ADDRESS_TO_TADDR(m_baseAddr));
        baseRank = arrayBase->GetRank();
        if (numInd != baseRank)
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        eltType = arrayBase->GetArrayElementTypeHandle();
        if (eltType.IsNull())
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        unsigned dim;
        ULONG64 offs;
        SIZE_T dimSize;

        dim = baseRank;
        offs = TO_CDADDR(PTR_TO_TADDR(arrayBase->GetDataPtr()));
        dimSize = arrayBase->GetComponentSize();
        bounds = arrayBase->GetBoundsPtr();
        lowBounds = arrayBase->GetLowerBoundsPtr();

        while (dim-- > 0)
        {
            if (indices[dim] < lowBounds[dim])
            {
                status = E_INVALIDARG;
                goto Exit;
            }

            UINT32 uindex = (UINT32)(indices[dim] - lowBounds[dim]);
            if (uindex >= (UINT32)bounds[dim])
            {
                status = E_INVALIDARG;
                goto Exit;
            }

            offs += dimSize * uindex;

            dimSize *= (UINT64)bounds[dim];
        }

        NativeVarLocation loc;

        loc.addr = offs;
        loc.size = eltType.GetSize();
        loc.contextReg = false;

        *value = new (nothrow)
            ClrDataValue(m_dac, m_appDomain, m_thread,
                         GetTypeFieldValueFlags(eltType, NULL, 0, false),
                         eltType, offs, 1, &loc);
        status = *value ? S_OK : E_OUTOFMEMORY;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetNumLocations(
    /* [out] */ ULONG32* numLocs)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *numLocs = m_numLocs;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::GetLocationByIndex(
    /* [in] */ ULONG32 loc,
    /* [out] */ ULONG32* flags,
    /* [out] */ CLRDATA_ADDRESS* arg)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (loc < m_numLocs)
        {
            if (m_locs[loc].contextReg)
            {
                *flags = CLRDATA_VLOC_REGISTER;
                *arg = 0;
            }
            else
            {
                *flags = CLRDATA_VLOC_MEMORY;
                *arg = TO_CDADDR(m_locs[loc].addr);
            }

            status = S_OK;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataValue::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 3;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataValue::NewFromFieldDesc(ClrDataAccess* dac,
                               AppDomain* appDomain,
                               ULONG32 flags,
                               FieldDesc* fieldDesc,
                               ULONG64 objBase,
                               Thread* tlsThread,
                               ClrDataValue** value,
                               IXCLRDataValue** pubValue,
                               ULONG32 nameBufRetLen,
                               ULONG32 *nameLenRet,
                               __out_ecount_part_opt(nameBufRetLen, *nameLenRet) WCHAR nameBufRet[  ],
                               IXCLRDataModule** tokenScopeRet,
                               mdFieldDef *tokenRet)
{
    HRESULT status;
    ClrDataValue* field;
    ULONG numLocs = 1;
    NativeVarLocation varLoc, *locs = &varLoc;
    ULONG64 baseAddr;
    LPCUTF8 szFieldName;

    status = fieldDesc->GetName_NoThrow(&szFieldName);
    if (status != S_OK)
    {
        return status;
    }

    status = ConvertUtf8(
        szFieldName,
        nameBufRetLen,
        nameLenRet,
        nameBufRet);
    if (status != S_OK)
    {
        return status;
    }

    if (tokenRet != NULL)
    {
        *tokenRet = fieldDesc->GetMemberDef();
    }

    if (fieldDesc->GetEnclosingMethodTable()->ContainsGenericVariables())
    {
        // This field is for a generic type definition and
        // so doesn't have a real location.  Produce
        // a placeholder no-data value.
        numLocs = 0;
        locs = NULL;
        baseAddr = 0;
    }
    else if (fieldDesc->IsThreadStatic())
    {
        if (!tlsThread)
        {
            return E_INVALIDARG;
        }

        baseAddr =
            TO_CDADDR(tlsThread->GetStaticFieldAddrNoCreate(fieldDesc));
    }
    else if (fieldDesc->IsStatic())
    {
        baseAddr = TO_CDADDR
            (PTR_TO_TADDR(fieldDesc->GetStaticAddressHandle
             (fieldDesc->GetBase())));
    }
    else
    {
        // objBase is basically a CLRDATA_ADDRESS, which is a pointer-sized target address sign-extened to
        // 64-bit.  We need to get a TADDR here, which is a pointer-size unsigned value.
        baseAddr = TO_CDADDR(PTR_TO_TADDR(fieldDesc->GetAddress(PTR_VOID(CLRDATA_ADDRESS_TO_TADDR(objBase)))));
    }

    if (locs)
    {
        locs->addr = baseAddr;
        locs->size = fieldDesc->GetSize();
        locs->contextReg = false;
    }

    TypeHandle typeHandle = fieldDesc->LookupFieldTypeHandle();

    // We allow no-type situations for reference fields
    // as they can still be useful even though they cannot
    // be expanded.  This is also a common case where the
    // referred-to type may not be loaded if the field
    // is holding null.
    if (typeHandle.IsNull() && !fieldDesc->IsObjRef())
    {
        return E_INVALIDARG;
    }

    flags = GetTypeFieldValueFlags(typeHandle, fieldDesc, flags, false);

    if (tokenScopeRet)
    {
        *tokenScopeRet = new (nothrow)
            ClrDataModule(dac, fieldDesc->GetModule());
        if (!*tokenScopeRet)
        {
            return E_OUTOFMEMORY;
        }
    }

    field = new (nothrow) ClrDataValue(dac,
                                       appDomain,
                                       tlsThread,
                                       flags,
                                       typeHandle,
                                       baseAddr,
                                       numLocs,
                                       locs);
    if (value)
    {
        *value = field;
    }
    if (pubValue)
    {
        *pubValue = field;
    }

    if (!field)
    {
        if (tokenScopeRet)
        {
            delete (ClrDataModule*)*tokenScopeRet;
        }
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

//----------------------------------------------------------------------------
//
// ClrDataTypeDefinition.
//
//----------------------------------------------------------------------------

ClrDataTypeDefinition::ClrDataTypeDefinition(ClrDataAccess* dac,
                                             Module* module,
                                             mdTypeDef token,
                                             TypeHandle typeHandle)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_module = module;
    m_token = token;
    m_typeHandle = typeHandle;
}

ClrDataTypeDefinition::~ClrDataTypeDefinition(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataTypeDefinition::QueryInterface(THIS_
                                      IN REFIID interfaceId,
                                      OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataTypeDefinition)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataTypeDefinition*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataTypeDefinition::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataTypeDefinition::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetModule(
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *mod = new (nothrow)
            ClrDataModule(m_dac, m_module);
        status = *mod ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::StartEnumMethodDefinitions(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::New(m_module,
                               mdtMethodDef,
                                       m_token,
                               NULL,
                               NULL,
                               handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumMethodDefinition(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodDefinition **methodDefinition)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdMethodDef token;

        if ((status = MetaEnum::CdNextToken(handle, &token)) == S_OK)
        {
            status = ClrDataMethodDefinition::
                NewFromModule(m_dac,
                              m_module,
                              token,
                              NULL,
                              methodDefinition);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EndEnumMethodDefinitions(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::StartEnumMethodDefinitionsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartMethod(name,
                                          flags,
                                          m_module,
                                          m_token,
                                          NULL,
                                          NULL,
                                          NULL,
                                          handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumMethodDefinitionByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodDefinition **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdMethodDef token;

        if ((status = SplitName::CdNextMethod(handle, &token)) == S_OK)
        {
            status = ClrDataMethodDefinition::
                NewFromModule(m_dac,
                              m_module,
                              token,
                              NULL,
                              method);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EndEnumMethodDefinitionsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetMethodDefinitionByToken(
    /* [in] */ mdMethodDef token,
    /* [out] */ IXCLRDataMethodDefinition **methodDefinition)
{
    HRESULT status;

    // This isn't critically necessary but it prevents
    // an assert in the metadata code.
    if (TypeFromToken(token) != mdtMethodDef)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ClrDataMethodDefinition::
            NewFromModule(m_dac,
                          m_module,
                          token,
                          NULL,
                          methodDefinition);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::StartEnumInstances(
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumInstance(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataTypeInstance **instance)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EndEnumInstances(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetNumFields(
    /* [in] */ ULONG32 flags,
    /* [out] */ ULONG32 *numFields)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            status = E_NOTIMPL;
        }
        else
        {
            DeepFieldDescIterator fieldIter;

            if ((status = InitFieldIter(&fieldIter, m_typeHandle, true,
                                        flags, NULL)) == S_OK)
            {
                *numFields = fieldIter.Count();
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::StartEnumFields(
    /* [in] */ ULONG32 flags,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            *handle = 0;
            status = E_NOTIMPL;
        }
        else
        {
            status = SplitName::
                CdStartField(NULL,
                             0,
                             flags,
                             NULL,
                             m_typeHandle,
                             NULL,
                             mdTypeDefNil,
                             0,
                             NULL,
                             NULL,
                             NULL,
                             NULL,
                             NULL,
                             handle);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumField(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags,
    /* [out] */ mdFieldDef *token)
{
    return EnumField2(handle, nameBufLen, nameLen, nameBuf,
                      type, flags, NULL, token);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumField2(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags,
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, type, flags, NULL,
                                        nameBufLen, nameLen, nameBuf,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EndEnumFields(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::StartEnumFieldsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 nameFlags,
    /* [in] */ ULONG32 fieldFlags,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            *handle = 0;
            status = E_NOTIMPL;
        }
        else
        {
            status = SplitName::
                CdStartField(name,
                             nameFlags,
                             fieldFlags,
                             NULL,
                             m_typeHandle,
                             NULL,
                             mdTypeDefNil,
                             0,
                             NULL,
                             NULL,
                             NULL,
                             NULL,
                             NULL,
                             handle);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumFieldByName(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags,
    /* [out] */ mdFieldDef *token)
{
    return EnumFieldByName2(handle, type, flags, NULL, token);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EnumFieldByName2(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags,
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, type, flags, NULL,
                                        0, NULL, NULL,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::EndEnumFieldsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetFieldByToken(
    /* [in] */ mdFieldDef token,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags)
{
    return GetFieldByToken2(NULL, token, nameBufLen, nameLen, nameBuf,
                            type, flags);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetFieldByToken2(
    /* [in] */ IXCLRDataModule* tokenScope,
    /* [in] */ mdFieldDef token,
    /* [in] */ ULONG32 nameBufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(nameBufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataTypeDefinition **type,
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if (m_typeHandle.IsNull())
        {
            status = E_NOTIMPL;
            goto Exit;
        }

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, true,
                                    CLRDATA_VALUE_ALL_FIELDS, NULL)) == S_OK)
        {
            FieldDesc* fieldDesc;

            status = E_INVALIDARG;
            while ((fieldDesc = fieldIter.Next()))
            {
                if ((!tokenScope ||
                     PTR_HOST_TO_TADDR(((ClrDataModule*)tokenScope)->
                                       GetModule()) ==
                     PTR_HOST_TO_TADDR(fieldDesc->GetModule())) &&
                    fieldDesc->GetMemberDef() == token)
                {
                    if (flags)
                    {
                        *flags = GetTypeFieldValueFlags(m_typeHandle,
                                     fieldDesc,
                                     fieldIter.IsFieldFromParentClass() ?
                                     CLRDATA_VALUE_IS_INHERITED : 0,
                                     false);
                    }

                    status = ConvertUtf8(fieldDesc->GetName(),
                                         nameBufLen, nameLen, nameBuf);

                    if (SUCCEEDED(status) && type)
                    {
                        TypeHandle fieldTypeHandle =
                            fieldDesc->LookupFieldTypeHandle();
                        *type = new (nothrow)
                            ClrDataTypeDefinition(m_dac,
                                                  fieldTypeHandle.GetModule(),
                                                  fieldTypeHandle.
                                                  GetMethodTable()->GetCl(),
                                                  fieldTypeHandle);
                        status = *type ? S_OK : E_OUTOFMEMORY;
                    }

                    break;
                }
            }
        }

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetName(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    HRESULT status = S_OK;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        char classNameBuf[MAX_CLASSNAME_LENGTH];

        if (m_typeHandle.IsNull())
        {
            if ((status =
                 GetFullClassNameFromMetadata(m_module->GetMDImport(),
                                              m_token,
                                              NumItems(classNameBuf),
                                              classNameBuf)) == S_OK)
            {
                status = ConvertUtf8(classNameBuf, bufLen, nameLen, nameBuf);
            }
        }
        else
        {
            StackSString ssClassNameBuf;
            m_typeHandle.GetName(ssClassNameBuf);
            if (wcsncpy_s(nameBuf, bufLen, ssClassNameBuf.GetUnicode(), _TRUNCATE) == STRUNCATE)
            {
                status = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            }
            if (nameLen != NULL)
            {
                size_t cchName = ssClassNameBuf.GetCount() + 1;
                if (FitsIn<ULONG32>(cchName))
                {
                    *nameLen = (ULONG32) cchName;
                }
                else
                {
                    status = COR_E_OVERFLOW;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetTokenAndScope(
    /* [out] */ mdTypeDef *token,
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = S_OK;

        if (token)
        {
            *token = m_token;
        }

        if (mod)
        {
            *mod = new (nothrow)
                ClrDataModule(m_dac, m_module);
            status = *mod ? S_OK : E_OUTOFMEMORY;
        }

    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetCorElementType(
    /* [out] */ CorElementType *type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            status = E_NOTIMPL;
        }
        else
        {
            *type = m_typeHandle.GetInternalCorElementType();
            status = S_OK;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = CLRDATA_TYPE_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetBase(
    /* [out] */ IXCLRDataTypeDefinition **base)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdTypeDef token;
        TypeHandle typeHandle;

        if (m_typeHandle.IsNull())
        {
            ULONG attr;

            status = m_module->GetMDImport()->GetTypeDefProps(m_token, &attr, &token);
            if (FAILED(status))
            {
                goto Exit;
            }
        }
        else
        {
            typeHandle = m_typeHandle.GetParent();
            if (typeHandle.IsNull() ||
                !typeHandle.GetMethodTable())
            {
                status = E_NOINTERFACE;
                goto Exit;
            }

            token = typeHandle.GetMethodTable()->GetCl();
        }

        *base = new (nothrow)
            ClrDataTypeDefinition(m_dac, m_module, token, typeHandle);
        status = *base ? S_OK : E_OUTOFMEMORY;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetArrayRank(
    /* [out] */ ULONG32* rank)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            status = E_NOTIMPL;
        }
        else
        {
            MethodTable* pMT = m_typeHandle.GetMethodTable();

            if (!m_typeHandle.IsArray() ||
                (pMT == NULL))
            {
                status = E_NOINTERFACE;
            }
            else
            {
                *rank = pMT->GetRank();
                status = S_OK;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::IsSameObject(
    /* [in] */ IXCLRDataTypeDefinition* type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_typeHandle.IsNull())
        {
            status = (PTR_HOST_TO_TADDR(m_module) ==
                      PTR_HOST_TO_TADDR(((ClrDataTypeDefinition*)type)->
                                        m_module) &&
                      m_token == ((ClrDataTypeDefinition*)type)->m_token) ?
                S_OK : S_FALSE;
        }
        else
        {
            status = (m_typeHandle.AsTAddr() ==
                      ((ClrDataTypeDefinition*)type)->m_typeHandle.AsTAddr()) ?
                S_OK : S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::GetTypeNotification(
    /* [out] */ ULONG32* flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::SetTypeNotification(
    /* [in] */ ULONG32 flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeDefinition::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 2;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataTypeDefinition::NewFromModule(ClrDataAccess* dac,
                                     Module* module,
                                     mdTypeDef token,
                                     ClrDataTypeDefinition** typeDef,
                                     IXCLRDataTypeDefinition** pubTypeDef)
{
    // The type may not be loaded yet so the
    // absence of a TypeHandle is not fatal.
    // If the type isn't loaded a metadata-query
    // TypeDefinition is produced.
    TypeHandle typeHandle = module->LookupTypeDef(token);
    if (!typeHandle.IsNull() &&
        !typeHandle.IsRestored())
    {
        // The type isn't fully usable so just go with metadata.
        typeHandle = TypeHandle();
    }

    ClrDataTypeDefinition* def = new (nothrow)
        ClrDataTypeDefinition(dac, module, token, typeHandle);
    if (!def)
    {
        return E_OUTOFMEMORY;
    }

    PREFIX_ASSUME(typeDef || pubTypeDef);

    if (typeDef)
    {
        *typeDef = def;
    }
    if (pubTypeDef)
    {
        *pubTypeDef = def;
    }

    return S_OK;
}

//----------------------------------------------------------------------------
//
// ClrDataTypeInstance.
//
//----------------------------------------------------------------------------

ClrDataTypeInstance::ClrDataTypeInstance(ClrDataAccess* dac,
                                         AppDomain* appDomain,
                                         TypeHandle typeHandle)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_appDomain = appDomain;
    m_typeHandle = typeHandle;
}

ClrDataTypeInstance::~ClrDataTypeInstance(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataTypeInstance::QueryInterface(THIS_
                                    IN REFIID interfaceId,
                                    OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataTypeInstance)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataTypeInstance*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataTypeInstance::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataTypeInstance::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::StartEnumMethodInstances(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_typeHandle.GetMethodTable())
        {
            *handle = 0;
            status = S_FALSE;
            goto Exit;
        }

        status = MetaEnum::New(m_typeHandle.GetModule(),
                               mdtMethodDef,
                               m_typeHandle.GetCl(),
                               NULL,
                               NULL,
                               handle);

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumMethodInstance(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodInstance **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        for (;;)
        {
            mdMethodDef token;

            if ((status = MetaEnum::CdNextToken(handle, &token)) != S_OK)
            {
                break;
            }

            // If the method doesn't have a MethodDesc or hasn't
            // been JIT'ed yet it's not an instance and should
            // just be skipped.
            if ((status = ClrDataMethodInstance::
                 NewFromModule(m_dac,
                               m_appDomain,
                               m_typeHandle.GetModule(),
                               token,
                               NULL,
                               method)) != E_INVALIDARG)
            {
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EndEnumMethodInstances(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::StartEnumMethodInstancesByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_typeHandle.GetMethodTable())
        {
            *handle = 0;
            status = S_FALSE;
            goto Exit;
        }

        status = SplitName::CdStartMethod(name,
                                          flags,
                                          m_typeHandle.GetModule(),
                                          m_typeHandle.GetCl(),
                                          m_appDomain,
                                          NULL,
                                          NULL,
                                          handle);

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumMethodInstanceByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodInstance **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        for (;;)
        {
            mdMethodDef token;

            if ((status = SplitName::CdNextMethod(handle, &token)) != S_OK)
            {
                break;
            }

            // If the method doesn't have a MethodDesc or hasn't
            // been JIT'ed yet it's not an instance and should
            // just be skipped.
            if ((status = ClrDataMethodInstance::
                 NewFromModule(m_dac,
                               m_appDomain,
                               m_typeHandle.GetModule(),
                               token,
                               NULL,
                               method)) != E_INVALIDARG)
            {
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EndEnumMethodInstancesByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetNumStaticFields(
    /* [out] */ ULONG32 *numFields)
{
    return GetNumStaticFields2(INH_STATIC, numFields);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetStaticFieldByIndex(
    /* [in] */ ULONG32 index,
    /* [in] */ IXCLRDataTask *tlsTask,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, true,
                                    INH_STATIC, NULL)) == S_OK)
        {
            ULONG32 count = 0;
            FieldDesc* fieldDesc;

            status = E_INVALIDARG;
            while ((fieldDesc = fieldIter.Next()))
            {
                if (count++ == index)
                {
                    Thread* tlsThread = tlsTask ?
                        ((ClrDataTask*)tlsTask)->GetThread() : NULL;

                    status = ClrDataValue::
                        NewFromFieldDesc(m_dac,
                                         m_appDomain,
                                         fieldIter.IsFieldFromParentClass() ?
                                         CLRDATA_VALUE_IS_INHERITED : 0,
                                         fieldDesc,
                                         0,
                                         tlsThread,
                                         NULL,
                                         field,
                                         bufLen,
                                         nameLen,
                                         nameBuf,
                                         NULL,
                                         token);
                    break;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::StartEnumStaticFieldsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [out] */ CLRDATA_ENUM* handle)
{
    return StartEnumStaticFieldsByName2(name, flags, INH_STATIC, tlsTask,
                                        handle);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumStaticFieldByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value)
{
    return EnumStaticFieldByName2(handle, value);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EndEnumStaticFieldsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    return EndEnumStaticFieldsByName2(handle);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetNumStaticFields2(
    /* [in] */ ULONG32 flags,
    /* [out] */ ULONG32 *numFields)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, true,
                                    flags, NULL)) == S_OK)
        {
            *numFields = fieldIter.Count();
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::StartEnumStaticFields(
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::
            CdStartField(NULL,
                         0,
                         flags,
                         NULL,
                         m_typeHandle,
                         NULL,
                         mdTypeDefNil,
                         0,
                         NULL,
                         tlsTask,
                         m_appDomain,
                         NULL,
                         NULL,
                         handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumStaticField(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value)
{
    return EnumStaticField2(handle, value,  0, NULL, NULL,
                            NULL, NULL);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumStaticField2(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, NULL, NULL, value,
                                        bufLen, nameLen, nameBuf,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EndEnumStaticFields(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::StartEnumStaticFieldsByName2(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 nameFlags,
    /* [in] */ ULONG32 fieldFlags,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::
            CdStartField(name,
                         nameFlags,
                         fieldFlags,
                         NULL,
                         m_typeHandle,
                         NULL,
                         mdTypeDefNil,
                         0,
                         NULL,
                         tlsTask,
                         m_appDomain,
                         NULL,
                         NULL,
                         handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumStaticFieldByName2(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value)
{
    return EnumStaticFieldByName3(handle, value, NULL, NULL);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EnumStaticFieldByName3(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value,
    /* [out] */ IXCLRDataModule** tokenScope,
    /* [out] */ mdFieldDef *token)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextField(m_dac, handle, NULL, NULL, value,
                                        0, NULL, NULL,
                                        tokenScope, token);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::EndEnumStaticFieldsByName2(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetStaticFieldByToken(
    /* [in] */ mdFieldDef token,
    /* [in] */ IXCLRDataTask *tlsTask,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    return GetStaticFieldByToken2(NULL, token, tlsTask, field,
                                  bufLen, nameLen, nameBuf);
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetStaticFieldByToken2(
    /* [in] */ IXCLRDataModule* tokenScope,
    /* [in] */ mdFieldDef token,
    /* [in] */ IXCLRDataTask *tlsTask,
    /* [out] */ IXCLRDataValue **field,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        DeepFieldDescIterator fieldIter;

        if ((status = InitFieldIter(&fieldIter, m_typeHandle, true,
                                    INH_STATIC, NULL)) == S_OK)
        {
            FieldDesc* fieldDesc;

            status = E_INVALIDARG;
            while ((fieldDesc = fieldIter.Next()))
            {
                if ((!tokenScope ||
                     PTR_HOST_TO_TADDR(((ClrDataModule*)tokenScope)->
                                       GetModule()) ==
                     PTR_HOST_TO_TADDR(fieldDesc->GetModule())) &&
                    fieldDesc->GetMemberDef() == token)
                {
                    Thread* tlsThread = tlsTask ?
                        ((ClrDataTask*)tlsTask)->GetThread() : NULL;

                    status = ClrDataValue::
                        NewFromFieldDesc(m_dac,
                                         m_appDomain,
                                         fieldIter.IsFieldFromParentClass() ?
                                         CLRDATA_VALUE_IS_INHERITED : 0,
                                         fieldDesc,
                                         0,
                                         tlsThread,
                                         NULL,
                                         field,
                                         bufLen,
                                         nameLen,
                                         nameBuf,
                                         NULL,
                                         NULL);
                    break;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetName(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR nameBuf[  ])
{
    HRESULT status = S_OK;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        StackSString ssClassNameBuf;

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
        PAL_CPP_TRY
        {
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

        m_typeHandle.GetName(ssClassNameBuf);

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
        }
        PAL_CPP_CATCH_ALL
        {
            // if metadata is unavailable try the DAC's MdCache
            ssClassNameBuf.Clear();
            PTR_MethodTable pMT = m_typeHandle.AsMethodTable();
            if (pMT != NULL)
            {
                if (!DacMdCacheGetEEName(dac_cast<TADDR>(pMT), ssClassNameBuf))
                {
                    ssClassNameBuf.Clear();
                }
            }
            if (ssClassNameBuf.IsEmpty())
            {
                PAL_CPP_RETHROW;
            }
        }
        PAL_CPP_ENDTRY
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

        if (wcsncpy_s(nameBuf, bufLen, ssClassNameBuf.GetUnicode(), _TRUNCATE) == STRUNCATE)
        {
            status = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        if (nameLen != NULL)
        {
            size_t cchName = ssClassNameBuf.GetCount() + 1;
            if (FitsIn<ULONG32>(cchName))
            {
                *nameLen = (ULONG32) cchName;
            }
            else
            {
                status = COR_E_OVERFLOW;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetModule(
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *mod = new (nothrow)
            ClrDataModule(m_dac, m_typeHandle.GetModule());
        status = *mod ? S_OK : E_OUTOFMEMORY;

    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetDefinition(
    /* [out] */ IXCLRDataTypeDefinition **typeDefinition)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        TypeHandle defType;

        if (m_typeHandle.IsArray() || m_typeHandle.IsFnPtrType())
        {
            // Arrays don't necessarily have metadata so
            // we can't rely on being able to look up
            // a definition that way.

            // Also ByRef and FUNC_PTR does not have typedef token backing it.

            // Instead, just use the same type handle.
            // XXX Microsoft - Generics issues?

            // Question - what does the GetCl return return here? The underlying element type?
            // If so, we are lossing informaiton.
            //
            defType = m_typeHandle;
            *typeDefinition = new (nothrow)
                ClrDataTypeDefinition(m_dac,
                                      defType.GetModule(),
                                      defType.GetCl(),
                                      defType);
        }

        else if (m_typeHandle.IsTypeDesc() && m_typeHandle.AsTypeDesc()->HasTypeParam())
        {
            // HasTypeParam is true for - ParamTypeDesc (BYREF, PTR)
            defType = m_typeHandle.AsTypeDesc()->GetTypeParam();

            // The DefinitionType won't contain ByRef, PTR.
            *typeDefinition = new (nothrow)
                ClrDataTypeDefinition(m_dac,
                                      defType.GetModule(),
                                      defType.GetCl(),
                                      defType);
        }
        else
        {
            // @TODO:: Should this be only for generic?
            //
            defType = m_typeHandle.GetModule()->
                LookupTypeDef(m_typeHandle.GetCl());
            *typeDefinition = new (nothrow)
                ClrDataTypeDefinition(m_dac,
                                      m_typeHandle.GetModule(),
                                      m_typeHandle.GetCl(),
                                      defType);
        }

        status = *typeDefinition ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = CLRDATA_TYPE_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetBase(
    /* [out] */ IXCLRDataTypeInstance **base)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *base = new (nothrow)
            ClrDataTypeInstance(m_dac, m_appDomain, m_typeHandle.GetParent());
        status = *base ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::IsSameObject(
    /* [in] */ IXCLRDataTypeInstance* type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = (PTR_HOST_TO_TADDR(m_appDomain) ==
                  PTR_HOST_TO_TADDR(((ClrDataTypeInstance*)type)->
                                    m_appDomain) &&
                  m_typeHandle == ((ClrDataTypeInstance*)type)->
                  m_typeHandle) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetNumTypeArguments(
    /* [out] */ ULONG32 *numTypeArgs)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::GetTypeArgumentByIndex(
    /* [in] */ ULONG32 index,
    /* [out] */ IXCLRDataTypeInstance **typeArg)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTypeInstance::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 2;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataTypeInstance::NewFromModule(ClrDataAccess* dac,
                                   AppDomain* appDomain,
                                   Module* module,
                                   mdTypeDef token,
                                   ClrDataTypeInstance** typeInst,
                                   IXCLRDataTypeInstance** pubTypeInst)
{
    TypeHandle typeHandle = module->LookupTypeDef(token);
    if (typeHandle.IsNull() ||
        !typeHandle.IsRestored())
    {
        return E_INVALIDARG;
    }

    ClrDataTypeInstance* inst = new (nothrow)
        ClrDataTypeInstance(dac, appDomain, typeHandle);
    if (!inst)
    {
        return E_OUTOFMEMORY;
    }

    PREFIX_ASSUME(typeInst || pubTypeInst);

    if (typeInst)
    {
        *typeInst = inst;
    }
    if (pubTypeInst)
    {
        *pubTypeInst = inst;
    }

    return S_OK;
}


