#include "regnative.hpp"

namespace
{
    void const* g_data;
    uint32_t g_dataLen;

    IMetaDataDispenser* g_baselineDisp;
    IMetaDataDispenser* g_currentDisp;

    IMetaDataImport* g_baselineImport;
    IMetaDataImport* g_currentImport;

    HRESULT CreateImport(IMetaDataDispenser* disp, IMetaDataImport** import)
    {
        assert(disp != nullptr && import != nullptr);
        return disp->OpenScopeOnMemory(
            g_data,
            g_dataLen,
            CorOpenFlags::ofReadOnly,
            IID_IMetaDataImport,
            reinterpret_cast<IUnknown**>(import));
    }

    HRESULT EnumTypeDefs(IMetaDataImport* import)
    {
        assert(import != nullptr);
        HRESULT hr;
        HCORENUM hcorenum = {};
        uint32_t buffer[1];
        uint32_t count;
        while (S_OK == (hr = import->EnumTypeDefs(&hcorenum, buffer, ARRAYSIZE(buffer), (ULONG*)&count))
            && count != 0)
        {
        }
        return hr;
    }

    HRESULT GetScopeProps(IMetaDataImport* import)
    {
        assert(import != nullptr);
        WCHAR name[512];
        ULONG nameLen;
        GUID mvid;
        return import->GetScopeProps(name, ARRAYSIZE(name), &nameLen, &mvid);
    }

    HRESULT EnumUserStrings(IMetaDataImport* import)
    {
        assert(import != nullptr);
        HRESULT hr;
        HCORENUM hcorenum = {};
        uint32_t buffer[1];
        uint32_t count;
        while (S_OK == (hr = import->EnumUserStrings(&hcorenum, buffer, ARRAYSIZE(buffer), (ULONG*)&count))
            && count != 0)
        {
        }
        return hr;
    }

    HRESULT GetCustomAttributeByName(IMetaDataImport* import)
    {
        assert(import != nullptr);
        mdToken tk = TokenFromRid(2, mdtTypeDef);
        void const* data;
        uint32_t dataLen;
        return import->GetCustomAttributeByName(tk, W("NotAnAttribute"), &data, (ULONG*)&dataLen);
    }
}

EXPORT
HRESULT PerfInitialize(
    void const* data,
    uint32_t dataLen,
    IMetaDataDispenser* baseline)
{
    if (data == nullptr || baseline == nullptr)
        return E_INVALIDARG;

    g_data = data;
    g_dataLen = dataLen;

    (void)baseline->AddRef();
    g_baselineDisp = baseline;

    HRESULT hr;
    if (FAILED(hr = CreateImport(g_baselineDisp, &g_baselineImport)))
        return hr;

    if (FAILED(hr = GetDispenser(IID_IMetaDataDispenser, reinterpret_cast<void**>(&g_currentDisp))))
        return hr;

    if (FAILED(hr = CreateImport(g_currentDisp, &g_currentImport)))
        return hr;

    return S_OK;
}

EXPORT
HRESULT PerfBaselineCreateImport(int iter)
{
    HRESULT hr;
    IMetaDataImport* import;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = CreateImport(g_baselineDisp, &import)))
            return hr;
        (void)import->Release();
    }
    return S_OK;
}

EXPORT
HRESULT PerfCurrentCreateImport(int iter)
{
    HRESULT hr;
    IMetaDataImport* import;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = CreateImport(g_currentDisp, &import)))
            return hr;
        (void)import->Release();
    }
    return S_OK;
}

EXPORT
HRESULT PerfBaselineEnumTypeDefs(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = EnumTypeDefs(g_baselineImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfCurrentEnumTypeDefs(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = EnumTypeDefs(g_currentImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfBaselineGetScopeProps(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = GetScopeProps(g_baselineImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfCurrentGetScopeProps(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = GetScopeProps(g_currentImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfBaselineEnumUserStrings(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = EnumUserStrings(g_baselineImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfCurrentEnumUserStrings(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = EnumUserStrings(g_currentImport)))
            return hr;
    }
    return S_OK;
}

EXPORT
HRESULT PerfBaselineGetCustomAttributeByName(int iter)
{
    for (int i = 0; i < iter; ++i)
    {
        if (S_FALSE != GetCustomAttributeByName(g_baselineImport))
            return E_FAIL;
    }
    return S_OK;
}

EXPORT
HRESULT PerfCurrentGetCustomAttributeByName(int iter)
{
    for (int i = 0; i < iter; ++i)
    {
        if (S_FALSE != GetCustomAttributeByName(g_currentImport))
            return E_FAIL;
    }
    return S_OK;
}
