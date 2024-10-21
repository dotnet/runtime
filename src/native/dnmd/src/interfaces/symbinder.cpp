#ifdef DNMD_BUILD_SHARED
#ifdef _MSC_VER
#define DNMD_EXPORT __declspec(dllexport)
#else
#define DNMD_EXPORT __attribute__((__visibility__("default")))
#endif // !_MSC_VER
#endif // DNMD_BUILD_SHARED

#include <internal/dnmd_platform.hpp>
#include "dnmd_interfaces.hpp"

#include <cor.h>
#include <corhdr.h>
#include <corsym.h>

EXTERN_GUID(IID_ISymUnmanagedBinder, 0xaa544d42, 0x28cb, 0x11d3, 0xbd, 0x22, 0x00, 0x00, 0xf8, 0x08, 0x49, 0xbd);

namespace
{
    class SymUnmanagedBinderStateless final : ISymUnmanagedBinder
    {
    public: // ISymUnmanagedBinder
        STDMETHOD(GetReaderForFile)(
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in WCHAR const *fileName,
            /* [in] */ __RPC__in WCHAR const *searchPath,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal)
        {
            UNREFERENCED_PARAMETER(importer);
            UNREFERENCED_PARAMETER(fileName);
            UNREFERENCED_PARAMETER(searchPath);
            UNREFERENCED_PARAMETER(pRetVal);
            return E_NOTIMPL;
        }

        STDMETHOD(GetReaderFromStream)(
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in_opt IStream *pstream,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal)
        {
            UNREFERENCED_PARAMETER(importer);
            UNREFERENCED_PARAMETER(pstream);
            UNREFERENCED_PARAMETER(pRetVal);
            return E_NOTIMPL;
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
            else if (riid == IID_ISymUnmanagedBinder)
            {
                *ppvObject = static_cast<ISymUnmanagedBinder*>(this);
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

    // The only available binder is stateless and
    // statically allocated. There is no lifetime management
    // needed.
    SymUnmanagedBinderStateless g_binder;
}

extern "C" DNMD_EXPORT
HRESULT GetSymBinder(
    REFGUID riid,
    void** ppObj)
{
    if (riid != IID_ISymUnmanagedBinder)
        return E_INVALIDARG;

    if (ppObj == nullptr)
        return E_INVALIDARG;

    return g_binder.QueryInterface(riid, (void**)ppObj);
}