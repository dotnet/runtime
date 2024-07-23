#ifdef DNMD_BUILD_SHARED
#ifdef _MSC_VER
#define DNMD_EXPORT __declspec(dllexport)
#else
#define DNMD_EXPORT __attribute__((__visibility__("default")))
#endif // !_MSC_VER
#endif // DNMD_BUILD_SHARED

#include <internal/dnmd_platform.hpp>
#include "dnmd_interfaces.hpp"
#include "controllingiunknown.hpp"
#include "metadataimportro.hpp"
#include "metadataemit.hpp"
#include "threadsafe.hpp"

#include <cstring>

namespace
{
    class MDDispenser final : public TearOffBase<IMetaDataDispenserEx>
    {
        bool _threadSafe;
    private:
        dncp::com_ptr<ControllingIUnknown> CreateExposedObject(dncp::com_ptr<ControllingIUnknown> unknown, DNMDOwner* owner)
        {
            mdhandle_view handle_view{ owner };
            MetadataEmit* emit = unknown->CreateAndAddTearOff<MetadataEmit>(handle_view);
            MetadataImportRO* import = unknown->CreateAndAddTearOff<MetadataImportRO>(std::move(handle_view));
            if (!_threadSafe)
            {
                return unknown;
            }
            dncp::com_ptr<ControllingIUnknown> threadSafeUnknown;
            threadSafeUnknown.Attach(new ControllingIUnknown());
            
            // Define an IDNMDOwner* tear-off here so the thread-safe object can be identified as a DNMD object.
            (void)threadSafeUnknown->CreateAndAddTearOff<DelegatingDNMDOwner>(handle_view);
            (void)threadSafeUnknown->CreateAndAddTearOff<ThreadSafeImportEmit<MetadataImportRO, MetadataEmit>>(std::move(unknown), import, emit);
            // ThreadSafeImportEmit took ownership of owner through unknown.
            return threadSafeUnknown;
        }

    protected:
        virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
        {
            if (riid == IID_IMetaDataDispenserEx || riid == IID_IMetaDataDispenser)
            {
                *ppvObject = static_cast<IMetaDataDispenserEx*>(this);
                return true;
            }
            return false;
        }

    public: // IMetaDataDispenser
        using TearOffBase<IMetaDataDispenserEx>::TearOffBase;

        STDMETHOD(DefineScope)(
            REFCLSID    rclsid,
            DWORD       dwCreateFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            if (rclsid != CLSID_CLR_v2_MetaData)
            {
                // DNMD::Interfaces only creating v2 metadata images.
                return CLDB_E_FILE_OLDVER;
            }

            if (dwCreateFlags != 0)
            {
                return E_INVALIDARG;
            }

            mdhandle_ptr md_ptr { md_create_new_handle() };
            if (md_ptr == nullptr)
                return E_OUTOFMEMORY;
            
            // Initialize the MVID of the new image.
            mdcursor_t moduleCursor;
            if (!md_token_to_cursor(md_ptr.get(), TokenFromRid(1, mdtModule), &moduleCursor))
                return E_FAIL;
            
            mdguid_t mvid;
            HRESULT hr = PAL_CoCreateGuid(reinterpret_cast<GUID*>(&mvid));
            if (FAILED(hr))
                return hr;
            
            if (1 != md_set_column_value_as_guid(moduleCursor, mdtModule_Mvid, 1, &mvid))
                return E_OUTOFMEMORY;
            
            dncp::com_ptr<ControllingIUnknown> obj;
            obj.Attach(new (std::nothrow) ControllingIUnknown());
            if (obj == nullptr)
                return E_OUTOFMEMORY;

            try
            {
                DNMDOwner* owner = obj->CreateAndAddTearOff<DNMDOwner>(std::move(md_ptr));
                return CreateExposedObject(std::move(obj), owner)->QueryInterface(riid, (void**)ppIUnk);
            }
            catch(std::bad_alloc const&)
            {
                return E_OUTOFMEMORY;
            }
        }

        STDMETHOD(OpenScope)(
            LPCWSTR     szScope,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            UNREFERENCED_PARAMETER(szScope);
            UNREFERENCED_PARAMETER(dwOpenFlags);
            UNREFERENCED_PARAMETER(riid);
            UNREFERENCED_PARAMETER(ppIUnk);
            return E_NOTIMPL;
        }

        STDMETHOD(OpenScopeOnMemory)(
            LPCVOID     pData,
            ULONG       cbData,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            if (ppIUnk == nullptr)
                return E_INVALIDARG;

            dncp::cotaskmem_ptr<void> nowOwned;
            if (dwOpenFlags & ofTakeOwnership)
                nowOwned.reset((void*)pData);

            malloc_ptr<void> copiedMem;
            if (dwOpenFlags & ofCopyMemory)
            {
                copiedMem.reset(::malloc(cbData));
                if (copiedMem == nullptr)
                    return E_OUTOFMEMORY;

                // Reassign the newly allocated memory to the param variable.
                pData = ::memcpy(copiedMem.get(), pData, cbData);
            }

            mdhandle_t mdhandle;
            if (!md_create_handle(pData, cbData, &mdhandle))
                return CLDB_E_FILE_CORRUPT;

            mdhandle_ptr md_ptr{ mdhandle };

            dncp::com_ptr<ControllingIUnknown> obj;
            obj.Attach(new (std::nothrow) ControllingIUnknown());
            if (obj == nullptr)
                return E_OUTOFMEMORY;

            try
            {
                DNMDOwner* owner = obj->CreateAndAddTearOff<DNMDOwner>(std::move(md_ptr), std::move(copiedMem), std::move(nowOwned));
                mdhandle_view handle_view{ owner };

                if (dwOpenFlags & ofReadOnly)
                {
                    // If we're read-only, then we don't need to deal with thread safety.
                    (void)obj->CreateAndAddTearOff<MetadataImportRO>(std::move(handle_view));
                    return obj->QueryInterface(riid, (void**)ppIUnk);
                }
                
                // If we're read-write, go through our helper to create an object that respects all of the options
                // (as the various options affect writing operations only).
                return CreateExposedObject(std::move(obj), owner)->QueryInterface(riid, (void**)ppIUnk);
            }
            catch(std::bad_alloc const&)
            {
                return E_OUTOFMEMORY;
            }
        }

        public: // IMetaDataDispenserEx
            STDMETHOD(SetOption)(
            REFGUID     optionid,
            VARIANT const *value) override
        {
            if (optionid == MetaDataThreadSafetyOptions)
            {
                _threadSafe = V_UI4(value) == CorThreadSafetyOptions::MDThreadSafetyOn;
                return S_OK;
            }
            return E_INVALIDARG;
        }

        STDMETHOD(GetOption)(
            REFGUID     optionid,
            VARIANT *pvalue) override
        {
            if (optionid == MetaDataThreadSafetyOptions)
            {
                V_UI4(pvalue) = _threadSafe ? CorThreadSafetyOptions::MDThreadSafetyOn : CorThreadSafetyOptions::MDThreadSafetyOff;
                return S_OK;
            }
            return E_INVALIDARG;
        }

        STDMETHOD(OpenScopeOnITypeInfo)(
            ITypeInfo   *pITI,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown    **ppIUnk) override
        {
            UNREFERENCED_PARAMETER(pITI);
            UNREFERENCED_PARAMETER(dwOpenFlags);
            UNREFERENCED_PARAMETER(riid);
            UNREFERENCED_PARAMETER(ppIUnk);
            return E_NOTIMPL;
        }

        STDMETHOD(GetCORSystemDirectory)(
        _Out_writes_to_opt_(cchBuffer, *pchBuffer)
            LPWSTR      szBuffer,
            DWORD       cchBuffer,
            DWORD*      pchBuffer) override
        {
            UNREFERENCED_PARAMETER(szBuffer);
            UNREFERENCED_PARAMETER(cchBuffer);
            UNREFERENCED_PARAMETER(pchBuffer);
            return E_NOTIMPL;
        }

        STDMETHOD(FindAssembly)(
            LPCWSTR  szAppBase,
            LPCWSTR  szPrivateBin,
            LPCWSTR  szGlobalBin,
            LPCWSTR  szAssemblyName,
            LPCWSTR  szName,
            ULONG    cchName,
            ULONG    *pcName) override
        {
            UNREFERENCED_PARAMETER(szAppBase);
            UNREFERENCED_PARAMETER(szPrivateBin);
            UNREFERENCED_PARAMETER(szGlobalBin);
            UNREFERENCED_PARAMETER(szAssemblyName);
            UNREFERENCED_PARAMETER(szName);
            UNREFERENCED_PARAMETER(cchName);
            UNREFERENCED_PARAMETER(pcName);
            return E_NOTIMPL;
        }

        STDMETHOD(FindAssemblyModule)(
            LPCWSTR  szAppBase,
            LPCWSTR  szPrivateBin,
            LPCWSTR  szGlobalBin,
            LPCWSTR  szAssemblyName,
            LPCWSTR  szModuleName,
        _Out_writes_to_opt_(cchName, *pcName)
            LPWSTR   szName,
            ULONG    cchName,
            ULONG    *pcName) override
        {
            UNREFERENCED_PARAMETER(szAppBase);
            UNREFERENCED_PARAMETER(szPrivateBin);
            UNREFERENCED_PARAMETER(szGlobalBin);
            UNREFERENCED_PARAMETER(szAssemblyName);
            UNREFERENCED_PARAMETER(szModuleName);
            UNREFERENCED_PARAMETER(szName);
            UNREFERENCED_PARAMETER(cchName);
            UNREFERENCED_PARAMETER(pcName);
            return E_NOTIMPL;
        }
    };
}

extern "C" DNMD_EXPORT
HRESULT GetDispenser(
    REFGUID riid,
    void** ppObj)
{
    if (riid != IID_IMetaDataDispenser
        && riid != IID_IMetaDataDispenserEx)
    {
        return E_INVALIDARG;
    }

    if (ppObj == nullptr)
        return E_INVALIDARG;

    try
    {
        dncp::com_ptr<ControllingIUnknown> obj;
        obj.Attach(new ControllingIUnknown());
        (void)obj->CreateAndAddTearOff<MDDispenser>();
        return obj->QueryInterface(riid, (void**)ppObj);
    }
    catch(std::bad_alloc const&)
    {
        return E_OUTOFMEMORY;
    }
}
