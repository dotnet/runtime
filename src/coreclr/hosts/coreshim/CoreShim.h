// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _CORESHIM_H_
#define _CORESHIM_H_

// Platform
#define NOMINMAX
#include <Windows.h>
#include <combaseapi.h>

// Standard library
#include <utility>
#include <string>
#include <cstdint>
#include <cassert>

// CoreCLR
#include <palclr.h>
#include <coreclrhost.h>
#include <minipal/utils.h>

#define WCHAR wchar_t

#define RETURN_IF_FAILED(exp) { hr = (exp); if (FAILED(hr)) { assert(false && #exp); return hr; } }

template
<
    typename T,
    T DEFAULT,
    void(*RELEASE)(T)
>
struct AutoClass
{
    T c;

    AutoClass() : c{ DEFAULT }
    { }

    AutoClass(_Inout_ T t) : c{ t }
    { }

    AutoClass(_In_ const AutoClass&) = delete;
    AutoClass& operator=(_In_ const AutoClass&) = delete;

    AutoClass(_Inout_ AutoClass &&other)
        : c{ other.Detach() }
    { }

    AutoClass& operator=(_Inout_ AutoClass &&other)
    {
        Attach(other.Detach());
    }

    ~AutoClass()
    {
        Attach(DEFAULT);
    }

    operator T()
    {
        return c;
    }

    T* operator &()
    {
        return &c;
    }

    void Attach(_In_opt_ T cm)
    {
        RELEASE(c);
        c = cm;
    }

    T Detach()
    {
        T tmp = c;
        c = DEFAULT;
        return tmp;
    }
};

inline void ReleaseHandle(_In_ HANDLE h)
{
    if (h != nullptr && h != INVALID_HANDLE_VALUE)
        ::CloseHandle(h);
}

using AutoHandle = AutoClass<HANDLE, nullptr, &ReleaseHandle>;

inline void ReleaseFindFile(_In_ HANDLE h)
{
    if (h != nullptr)
        ::FindClose(h);
}

using AutoFindFile = AutoClass<HANDLE, nullptr, &ReleaseFindFile>;

inline void ReleaseModule(_In_ HMODULE m)
{
    if (m != nullptr)
        ::FreeLibrary(m);
}

using AutoModule = AutoClass<HMODULE, nullptr, &ReleaseModule>;

namespace Utility
{
    /// <summary>
    /// Get the supplied environment variable.
    /// </summary>
    HRESULT TryGetEnvVar(_In_z_ const WCHAR *env, _Inout_ std::wstring &envVar);

    /// <summary>
    /// Get the CoreShim directory.
    /// </summary>
    HRESULT GetCoreShimDirectory(_Inout_ std::string &dir);
    HRESULT GetCoreShimDirectory(_Inout_ std::wstring &dir);
}

// CoreShim environment variables used to indicate what assembly/type tuple
// to load during COM activation.
#define COMACT_ASSEMBLYNAME_ENVVAR W("CORESHIM_COMACT_ASSEMBLYNAME")
#define COMACT_TYPENAME_ENVVAR W("CORESHIM_COMACT_TYPENAME")

// CoreCLR class to handle lifetime and provide a simpler API surface
class coreclr
{
public: // static
    /// <summary>
    /// Get a CoreCLR instance
    /// </summary>
    /// <returns>S_OK if newly created and needs initialization, S_FALSE if already exists and no initialization needed, otherwise an error code</returns>
    /// <remarks>
    /// If a CoreCLR instance has already been created, the existing instance is returned.
    /// If the <paramref name="path"/> is not supplied, the 'CORE_ROOT' environment variable is used.
    /// </remarks>
    static HRESULT GetCoreClrInstance(_Outptr_ coreclr **instance, _In_opt_z_ const WCHAR *path = nullptr);

    /// <summary>
    /// Populate the supplied string with a delimited string of TPA assemblies in from the supplied directory path.
    /// </summary>
    /// <remarks>
    /// If <paramref name="dir"/> is not supplied, the 'CORE_ROOT' environment variable is used.
    /// </remarks>
    static HRESULT CreateTpaList(_Inout_ std::string &tpaList, _In_opt_z_ const WCHAR *dir = nullptr);

public:
    coreclr(_Inout_ AutoModule hmod);

    coreclr(_In_ const coreclr &) = delete;
    coreclr& operator=(_In_ const coreclr &) = delete;

    coreclr(_Inout_ coreclr &&) = delete;
    coreclr& operator=(_Inout_ coreclr &&) = delete;

    ~coreclr();

    // See exported function 'coreclr_initialize' from coreclr library
    HRESULT Initialize(
        _In_ int propertyCount,
        _In_reads_(propertyCount) const char **keys,
        _In_reads_(propertyCount) const char **values,
        _In_opt_z_ const char *appDomainName = nullptr);

    // See exported function 'coreclr_create_delegate' from coreclr library
    HRESULT CreateDelegate(
        _In_z_ const char *assembly,
        _In_z_ const char *type,
        _In_z_ const char *method,
        _Out_ void **del);

private:
    AutoModule _hmod;

    bool _attached;
    void *_clrInst;
    uint32_t _appDomainId;

    coreclr_initialize_ptr _initialize;
    coreclr_create_delegate_ptr _create_delegate;
    coreclr_shutdown_ptr _shutdown;
};

#endif /* _CORESHIM_H_ */
