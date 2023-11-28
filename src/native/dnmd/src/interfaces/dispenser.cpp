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

#include <cstring>

namespace
{
    class MDDispenserStateless final : IMetaDataDispenser
    {
    public: // IMetaDataDispenser
        STDMETHOD(DefineScope)(
            REFCLSID    rclsid,
            DWORD       dwCreateFlags,
            REFIID      riid,
            IUnknown** ppIUnk)
        {
            UNREFERENCED_PARAMETER(rclsid);
            UNREFERENCED_PARAMETER(dwCreateFlags);
            UNREFERENCED_PARAMETER(riid);
            UNREFERENCED_PARAMETER(ppIUnk);
            return E_NOTIMPL;
        }

        STDMETHOD(OpenScope)(
            LPCWSTR     szScope,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown** ppIUnk)
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
            IUnknown** ppIUnk)
        {
            if (ppIUnk == nullptr)
                return E_INVALIDARG;

            // Only support the read-only state
            if (!(dwOpenFlags & ofReadOnly))
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
                mdhandle_view handle_view{ obj->CreateAndAddTearOff<DNMDOwner>(std::move(md_ptr), std::move(copiedMem), std::move(nowOwned)) };
                (void)obj->CreateAndAddTearOff<MetadataImportRO>(std::move(handle_view));
            }
            catch(std::bad_alloc const&)
            {
                return E_OUTOFMEMORY;
            }

            return obj->QueryInterface(riid, (void**)ppIUnk);
        }

    public: // IUnknown
        virtual HRESULT STDMETHODCALLTYPE QueryInterface(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            if (ppvObject == nullptr)
                return E_POINTER;

            if (riid == IID_IUnknown)
            {
                *ppvObject = static_cast<IUnknown*>(this);
            }
            else if (riid == IID_IMetaDataDispenser)
            {
                *ppvObject = static_cast<IMetaDataDispenser*>(this);
            }
            else
            {
                *ppvObject = nullptr;
                return E_NOINTERFACE;
            }

            (void)AddRef();
            return S_OK;
        }

        virtual ULONG STDMETHODCALLTYPE AddRef(void)
        {
            return 1;
        }

        virtual ULONG STDMETHODCALLTYPE Release(void)
        {
            return 1;
        }
    };

    // The only available dispenser is stateless and
    // statically allocated. There is no lifetime management
    // needed.
    MDDispenserStateless g_dispenser;
}

extern "C" DNMD_EXPORT
HRESULT GetDispenser(
    REFGUID riid,
    void** ppObj)
{
    if (riid != IID_IMetaDataDispenser)
        return E_INVALIDARG;

    if (ppObj == nullptr)
        return E_INVALIDARG;

    return g_dispenser.QueryInterface(riid, (void**)ppObj);
}