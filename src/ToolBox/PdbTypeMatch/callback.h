#include "dia2.h"

#pragma warning ( disable : 4100)

class CCallback : public IDiaLoadCallback2{
    int m_nRefCount;
public:
    CCallback() { m_nRefCount = 0; }

    //IUnknown
    ULONG STDMETHODCALLTYPE AddRef() {
        m_nRefCount++;
        return m_nRefCount;
    }
    ULONG STDMETHODCALLTYPE Release() {
        if ( (--m_nRefCount) == 0 )
            delete this;
        return m_nRefCount;
    }
    HRESULT STDMETHODCALLTYPE QueryInterface( REFIID rid, void **ppUnk ) {
        if ( ppUnk == NULL ) {
            return E_INVALIDARG;
        }
        if (rid == __uuidof( IDiaLoadCallback2 ) )
            *ppUnk = (IDiaLoadCallback2 *)this;
        else if (rid == __uuidof( IDiaLoadCallback ) )
            *ppUnk = (IDiaLoadCallback *)this;
        else if (rid == __uuidof( IUnknown ) )
            *ppUnk = (IUnknown *)this;
        else
            *ppUnk = NULL;
        if ( *ppUnk != NULL ) {
            AddRef();
            return S_OK;
        }
        return E_NOINTERFACE;
    }

    HRESULT STDMETHODCALLTYPE NotifyDebugDir(
                BOOL fExecutable, 
                DWORD cbData,
                BYTE data[]) // really a const struct _IMAGE_DEBUG_DIRECTORY *
    {
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE NotifyOpenDBG(
                LPCOLESTR dbgPath, 
                HRESULT resultCode)
    {
        // wprintf(L"opening %s...\n", dbgPath);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE NotifyOpenPDB(
                LPCOLESTR pdbPath, 
                HRESULT resultCode)
    {
        // wprintf(L"opening %s...\n", pdbPath);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictRegistryAccess()         
    {
        // return hr != S_OK to prevent querying the registry for symbol search paths
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictSymbolServerAccess()
    {
      // return hr != S_OK to prevent accessing a symbol server
      return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictOriginalPathAccess()     
    {
        // return hr != S_OK to prevent querying the registry for symbol search paths
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictReferencePathAccess()
    {
        // return hr != S_OK to prevent accessing a symbol server
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictDBGAccess()
    {
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE RestrictSystemRootAccess()
    {
        return S_OK;
    }
};

#pragma warning ( default : 4100 )
