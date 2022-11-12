#include <cassert>
#include <platform.h>
#include <external/cor.h>
#include <dnmd_interfaces.hpp>

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER

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
}

EXPORT
HRESULT Initialize(
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
HRESULT BaselineCreateImport(int iter)
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
HRESULT CurrentCreateImport(int iter)
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
HRESULT BaselineEnumTypeDefs(int iter)
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
HRESULT CurrentEnumTypeDefs(int iter)
{
    HRESULT hr;
    for (int i = 0; i < iter; ++i)
    {
        if (FAILED(hr = EnumTypeDefs(g_currentImport)))
            return hr;
    }
    return S_OK;
}
