#ifdef DNMD_BUILD_SHARED
#ifdef _MSC_VER
#define DNMD_EXPORT __declspec(dllexport)
#else
#define DNMD_EXPORT __attribute__((__visibility__("default")))
#endif // !_MSC_VER
#endif // DNMD_BUILD_SHARED

#include <platform.h>
#include <dnmd_interfaces.hpp>

#include "impl.hpp"

extern "C" DNMD_EXPORT
HRESULT ReadMetadata(
    void* data,
    size_t dataLen,
    REFGUID riid,
    void** ppObj)
{
    if (dataLen == 0 || data == nullptr)
        return E_INVALIDARG;

    if (riid != IID_IMetaDataImport && riid != IID_IMetaDataImport2)
        return E_INVALIDARG;

    if (ppObj == nullptr)
        return E_INVALIDARG;

    mdhandle_t mdhandle;
    if (!md_create_handle(data, dataLen, &mdhandle))
        return CLDB_E_FILE_CORRUPT;

    mdhandle_ptr md_ptr{ mdhandle };
    MetadataImportRO* md = new (std::nothrow) MetadataImportRO(std::move(md_ptr));
    if (md == nullptr)
        return E_OUTOFMEMORY;

    HRESULT hr = md->QueryInterface(riid, (void**)ppObj);
    (void)md->Release();
    return hr;
}
