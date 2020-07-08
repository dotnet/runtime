// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapWrapper.cpp
//

//
// ZapNode that wraps EE datastructure for zapping
//
// ======================================================================================

#include "common.h"

#include "zapwrapper.h"

void ZapWrapperTable::Resolve()
{
    for (WrapperTable::Iterator i = m_entries.Begin(), end = m_entries.End(); i != end; i++)
    {
        (*i)->Resolve(m_pImage);
    }
}

// ======================================================================================
// Actual placeholders

class ZapMethodHandle : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapMethodHandle(CORINFO_METHOD_HANDLE(GetHandle())));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_MethodHandle;
    }
};

ZapNode * ZapWrapperTable::GetMethodHandle(CORINFO_METHOD_HANDLE handle)
{
    return GetPlaceHolder<ZapMethodHandle, ZapNodeType_MethodHandle>(handle);
}

class ZapClassHandle : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapClassHandle(CORINFO_CLASS_HANDLE(GetHandle())));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ClassHandle;
    }
};

ZapNode * ZapWrapperTable::GetClassHandle(CORINFO_CLASS_HANDLE handle)
{
    return GetPlaceHolder<ZapClassHandle, ZapNodeType_ClassHandle>(handle);
}

class ZapFieldHandle : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapFieldHandle(CORINFO_FIELD_HANDLE(GetHandle())));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_FieldHandle;
    }
};

ZapNode * ZapWrapperTable::GetFieldHandle(CORINFO_FIELD_HANDLE handle)
{
    return GetPlaceHolder<ZapFieldHandle, ZapNodeType_FieldHandle>(handle);
}

class ZapAddrOfPInvokeFixup : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE((BYTE *)GetHandle() - 1)));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_AddrOfPInvokeFixup;
    }
};

ZapNode * ZapWrapperTable::GetAddrOfPInvokeFixup(CORINFO_METHOD_HANDLE handle)
{
    // Disambiguate the normal method handle and address of P/Invoke fixup by adding 1
    return GetPlaceHolder<ZapAddrOfPInvokeFixup, ZapNodeType_AddrOfPInvokeFixup>((BYTE *)handle + 1);
}

class ZapGenericHandle : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapGenericHandle(CORINFO_GENERIC_HANDLE(GetHandle())));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_GenericHandle;
    }
};

ZapNode * ZapWrapperTable::GetGenericHandle(CORINFO_GENERIC_HANDLE handle)
{
    return GetPlaceHolder<ZapGenericHandle, ZapNodeType_GenericHandle>(handle);
}

class ZapModuleIDHandle : public ZapWrapper
{
public:
    virtual void Resolve(ZapImage * pImage)
    {
        SetRVA(pImage->m_pPreloader->MapModuleIDHandle(CORINFO_MODULE_HANDLE(GetHandle())));
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ModuleIDHandle;
    }
};

ZapNode * ZapWrapperTable::GetModuleIDHandle(CORINFO_MODULE_HANDLE handle)
{
    return GetPlaceHolder<ZapModuleIDHandle, ZapNodeType_ModuleIDHandle>(handle);
}

class ZapStub : public ZapWrapper
{
    DWORD m_dwStubSize;

public:
    ZapStub(PVOID pStubData, DWORD dwStubSize)
        : ZapWrapper(pStubData), m_dwStubSize(dwStubSize)
    {
    }

    virtual DWORD GetSize()
    {
        return m_dwStubSize;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Stub;
    }

    virtual UINT GetAlignment()
    {
        return DEFAULT_CODE_ALIGN;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        DWORD dwSize = GetSize();
        PVOID pStub = GetHandle();

        SBuffer stubClone(dwSize);

        ICorCompileInfo *pCompileInfo = ZapImage::GetImage(pZapWriter)->GetCompileInfo();
        IfFailThrow(pCompileInfo->GetStubClone(pStub,
            const_cast<BYTE *>(static_cast<const BYTE *>(stubClone)), dwSize));

        pZapWriter->Write(const_cast<BYTE *>(static_cast<const BYTE *>(stubClone)), dwSize);
    }
};

ZapNode * ZapWrapperTable::GetStub(void * pStub)
{
    DWORD dwStubSize = 0;
    void * pStubData = m_pImage->GetCompileInfo()->GetStubSize(pStub, &dwStubSize);
    _ASSERTE(pStubData < pStub && pStub < (BYTE*)pStubData + dwStubSize);

    ZapStub * pZapStub = (ZapStub *)m_entries.Lookup(pStubData);
    if (pZapStub == NULL)
    {
        // did not find the delegate stub, need to emit the stub in the native image
        pZapStub = new (m_pImage->GetHeap()) ZapStub(pStubData, dwStubSize);

        m_entries.Add(pZapStub);
    }

    // Return inner ptr for the entrypoint
    _ASSERTE(pZapStub->GetType() == ZapNodeType_Stub);
    return m_pImage->GetInnerPtr(pZapStub, (PBYTE)pStub - (PBYTE)pStubData);
}
