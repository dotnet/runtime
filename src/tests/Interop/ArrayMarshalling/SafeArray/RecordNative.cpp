// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <oleauto.h>
#include <algorithm>
#include <platformdefines.h>

struct BlittableRecord
{
    int a;
};

class BlittableRecordInfo : public IRecordInfo
{
public:
    HRESULT STDMETHODCALLTYPE GetField(PVOID pvData, LPCOLESTR szFieldName, VARIANT* pvarField)
    {
        if (pvData == nullptr || pvarField == nullptr)
        {
            return E_INVALIDARG;
        }

        BlittableRecord* pData = (BlittableRecord*)pvData;

        if (TP_wcmp_s(szFieldName, W("a")) == 0)
        {
            VariantClear(pvarField);
            V_VT(pvarField) = VT_I4;
            V_I4(pvarField) = pData->a;
            return S_OK;
        }
        return E_INVALIDARG;
    }

    HRESULT STDMETHODCALLTYPE GetFieldNames(ULONG* pcNames, BSTR* rgBstrNames)
    {
        if (pcNames == nullptr)
        {
            return E_INVALIDARG;
        }
        if (rgBstrNames == nullptr)
        {
            *pcNames = 1;
            return S_OK;
        }

        if (*pcNames == 0)
        {
            return S_OK;
        }

        rgBstrNames[0] = TP_SysAllocString(W("a"));

        for(size_t i = 1; i < *pcNames; i++)
        {
            rgBstrNames[i] = nullptr;
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetFieldNoCopy(
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField,
        PVOID     *ppvDataCArray
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE GetGuid(GUID *pguid)
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE GetName(BSTR* pbstrName)
    {
        *pbstrName = TP_SysAllocString(W("BlittableRecord"));
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetSize(ULONG* pcbSize)
    {
        *pcbSize = sizeof(BlittableRecord);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetTypeInfo(ITypeInfo** ppTypeInfo)
    {
        return TYPE_E_INVALIDSTATE;
    }

    BOOL STDMETHODCALLTYPE IsMatchingType(IRecordInfo* pRecordInfo)
    {
        return pRecordInfo == this;
    }

    HRESULT STDMETHODCALLTYPE PutField(
        ULONG     wFlags,
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE PutFieldNoCopy(
        ULONG     wFlags,
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE RecordClear(PVOID pvExisting)
    {
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RecordCopy(PVOID pvExisting, PVOID pvNew)
    {
        ((BlittableRecord*)pvNew)->a = ((BlittableRecord*)pvExisting)->a;
        return S_OK;
    }

    PVOID STDMETHODCALLTYPE RecordCreate()
    {
        return CoreClrAlloc(sizeof(BlittableRecord));
    }

    HRESULT STDMETHODCALLTYPE RecordCreateCopy(
        PVOID pvSource,
        PVOID *ppvDest
    )
    {
        *ppvDest = RecordCreate();
        return RecordCopy(pvSource, *ppvDest);
    }

    HRESULT STDMETHODCALLTYPE RecordDestroy(PVOID pvRecord)
    {
        CoreClrFree(pvRecord);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RecordInit(PVOID pvNew)
    {
        ((BlittableRecord*)pvNew)->a = 0;
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return ++refCount;
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        return --refCount;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(const IID& riid, void** ppvObject)
    {
        if (riid == __uuidof(IRecordInfo))
        {
            *ppvObject = static_cast<IRecordInfo*>(this);
        }
        else if (riid == __uuidof(IUnknown))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else
        {
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

private:
    ULONG refCount;
} s_BlittableRecordInfo;

struct NonBlittableRecord
{
    BOOL b;
};


class NonBlittableRecordInfo : public IRecordInfo
{
public:
    HRESULT STDMETHODCALLTYPE GetField(PVOID pvData, LPCOLESTR szFieldName, VARIANT* pvarField)
    {
        if (pvData == nullptr || pvarField == nullptr)
        {
            return E_INVALIDARG;
        }

        NonBlittableRecord* pData = (NonBlittableRecord*)pvData;

        if (TP_wcmp_s(szFieldName, W("b")) == 0)
        {
            VariantClear(pvarField);
            V_VT(pvarField) = VT_BOOL;
            V_BOOL(pvarField) = pData->b == TRUE ? VARIANT_TRUE : VARIANT_FALSE;
            return S_OK;
        }
        return E_INVALIDARG;
    }

    HRESULT STDMETHODCALLTYPE GetFieldNames(ULONG* pcNames, BSTR* rgBstrNames)
    {
        if (pcNames == nullptr)
        {
            return E_INVALIDARG;
        }
        if (rgBstrNames == nullptr)
        {
            *pcNames = 1;
            return S_OK;
        }

        if (*pcNames == 0)
        {
            return S_OK;
        }

        rgBstrNames[0] = TP_SysAllocString(W("b"));

        for(size_t i = 1; i < *pcNames; i++)
        {
            rgBstrNames[i] = nullptr;
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetFieldNoCopy(
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField,
        PVOID     *ppvDataCArray
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE GetGuid(GUID *pguid)
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE GetName(BSTR* pbstrName)
    {
        *pbstrName = TP_SysAllocString(W("NonBlittableRecord"));
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetSize(ULONG* pcbSize)
    {
        *pcbSize = sizeof(BlittableRecord);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetTypeInfo(ITypeInfo** ppTypeInfo)
    {
        return TYPE_E_INVALIDSTATE;
    }

    BOOL STDMETHODCALLTYPE IsMatchingType(IRecordInfo* pRecordInfo)
    {
        return pRecordInfo == this;
    }

    HRESULT STDMETHODCALLTYPE PutField(
        ULONG     wFlags,
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE PutFieldNoCopy(
        ULONG     wFlags,
        PVOID     pvData,
        LPCOLESTR szFieldName,
        VARIANT   *pvarField
    )
    {
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE RecordClear(PVOID pvExisting)
    {
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RecordCopy(PVOID pvExisting, PVOID pvNew)
    {
        ((NonBlittableRecord*)pvNew)->b = ((NonBlittableRecord*)pvExisting)->b;
        return S_OK;
    }

    PVOID STDMETHODCALLTYPE RecordCreate()
    {
        return CoreClrAlloc(sizeof(NonBlittableRecord));
    }

    HRESULT STDMETHODCALLTYPE RecordCreateCopy(
        PVOID pvSource,
        PVOID *ppvDest
    )
    {
        *ppvDest = RecordCreate();
        return RecordCopy(pvSource, *ppvDest);
    }

    HRESULT STDMETHODCALLTYPE RecordDestroy(PVOID pvRecord)
    {
        CoreClrFree(pvRecord);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RecordInit(PVOID pvNew)
    {
        ((NonBlittableRecord*)pvNew)->b = FALSE;
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return ++refCount;
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        return --refCount;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(const IID& riid, void** ppvObject)
    {
        if (riid == __uuidof(IRecordInfo))
        {
            *ppvObject = static_cast<IRecordInfo*>(this);
        }
        else if (riid == __uuidof(IUnknown))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else
        {
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

private:
    uint32_t refCount;
} s_NonBlittableRecordInfo;

extern "C" DLL_EXPORT SAFEARRAY* STDMETHODCALLTYPE CreateSafeArrayOfRecords(BlittableRecord records[], int numRecords)
{
    SAFEARRAYBOUND bounds[1] = {
        {numRecords, 0}
    };

    SAFEARRAY* arr = SafeArrayCreateEx(VT_RECORD, 1, bounds, &s_BlittableRecordInfo);

    memcpy(arr->pvData, records, numRecords * sizeof(BlittableRecord));

    return arr;
}


extern "C" DLL_EXPORT SAFEARRAY* STDMETHODCALLTYPE CreateSafeArrayOfNonBlittableRecords(NonBlittableRecord records[], int numRecords)
{
    SAFEARRAYBOUND bounds[1] = {
        {numRecords, 0}
    };

    SAFEARRAY* arr = SafeArrayCreateEx(VT_RECORD, 1, bounds, &s_NonBlittableRecordInfo);

    memcpy(arr->pvData, records, numRecords * sizeof(NonBlittableRecord));

    return arr;
}
