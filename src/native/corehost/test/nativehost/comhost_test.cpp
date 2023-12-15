// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comhost_test.h"
#include <error_codes.h>
#include <iostream>
#include <future>
#include <pal.h>
#include <oleauto.h>

namespace
{
    class comhost_exports
    {
    public:
        comhost_exports(const pal::string_t &comhost_path)
        {
            if (!pal::load_library(&comhost_path, &_dll))
            {
                std::cout << "Load library of comhost failed" << std::endl;
                throw StatusCode::CoreHostLibLoadFailure;
            }

            get_class_obj_fn = (decltype(get_class_obj_fn))pal::get_symbol(_dll, "DllGetClassObject");
            if (get_class_obj_fn == nullptr)
            {
                std::cout << "Failed to get DllGetClassObject export from comhost" << std::endl;
                throw StatusCode::CoreHostEntryPointFailure;
            }
        }

        ~comhost_exports()
        {
            pal::unload_library(_dll);
        }

        decltype(&DllGetClassObject) get_class_obj_fn;

    private:
        pal::dll_t _dll;
    };

    HRESULT activate_class(const comhost_exports &comhost, REFCLSID clsid)
    {
        IClassFactory *classFactory;
        HRESULT hr = comhost.get_class_obj_fn(clsid, __uuidof(IClassFactory), (void**)&classFactory);
        if (FAILED(hr))
            return hr;

        IUnknown *instance;
        hr = classFactory->CreateInstance(nullptr, __uuidof(instance), (void**)&instance);
        classFactory->Release();
        if (FAILED(hr))
            return hr;

        instance->Release();
        return S_OK;
    }

    bool get_clsid(const pal::string_t &clsid_str, CLSID *clsid, std::vector<char> &clsidVect)
    {
        if (FAILED(::CLSIDFromString(clsid_str.c_str(), clsid)))
        {
            std::wcout << _X("Invalid CLSID: ") << clsid_str.c_str() << std::endl;
            return false;
        }

        return pal::pal_utf8string(clsid_str, &clsidVect);
    }

    void log_hresult(HRESULT hr, std::ostream &ss)
    {
        if (FAILED(hr))
            ss << "(" << std::hex << std::showbase << hr << ")";
    }

    void log_activation(const char *clsid, int activationNumber, int total, HRESULT hr, std::ostream &ss)
    {
        ss << "Activation of " << clsid << (FAILED(hr) ? " failed. " : " succeeded. ") << activationNumber << " of " << total;
        log_hresult(hr, ss);
        ss << std::endl;
    }

    HRESULT load_typelib(const pal::string_t &typelib_path)
    {
        HRESULT hr;
        ITypeLib* typelib = nullptr;
        hr = LoadTypeLibEx(typelib_path.c_str(), REGKIND_NONE, &typelib);
        if (FAILED(hr))
            return hr;

        typelib->Release();
        return hr;
    }

    void log_typelib_load(int typelib_id, HRESULT hr, std::ostream &ss)
    {
        ss << "Loading type library " << typelib_id << (FAILED(hr) ? " failed. " : " succeeded. ");
        log_hresult(hr, ss);
        ss << std::endl;
    }

    void log_default_typelib_load(HRESULT hr, std::ostream &ss)
    {
        ss << "Loading default type library" << (FAILED(hr) ? " failed. " : " succeeded. ");
        log_hresult(hr, ss);
        ss << std::endl;
    }
}

bool comhost_test::synchronous(const pal::string_t &comhost_path, const pal::string_t &clsid_str, int count)
{
    CLSID clsid;
    std::vector<char> clsidVect;
    if (!get_clsid(clsid_str, &clsid, clsidVect))
        return false;

    comhost_exports comhost(comhost_path);

    for (int i = 0; i < count; ++i)
    {
        HRESULT hr = activate_class(comhost, clsid);
        log_activation(clsidVect.data(), i + 1, count, hr, std::cout);
        if (FAILED(hr))
            return false;
    }

    return true;
}

bool comhost_test::concurrent(const pal::string_t &comhost_path, const pal::string_t &clsid_str, int count)
{
    CLSID clsid;
    std::vector<char> clsidVect;
    if (!get_clsid(clsid_str, &clsid, clsidVect))
        return false;

    comhost_exports comhost(comhost_path);

    std::vector<std::future<HRESULT>> activations;
    activations.reserve(count);
    for (int i = 0; i < count; ++i)
        activations.push_back(std::async(std::launch::async, activate_class, comhost, clsid));

    std::stringstream ss;
    bool succeeded = true;
    for (int i = 0; i < count; ++i)
    {
        HRESULT hr = activations[i].get();
        log_activation(clsidVect.data(), i + 1, count, hr, ss);
        if (FAILED(hr))
            succeeded = false;
    }

    std::cout << ss.str();
    return succeeded;
}

bool comhost_test::errorinfo(const pal::string_t &comhost_path, const pal::string_t &clsid_str, int count)
{
    CLSID clsid;
    std::vector<char> clsidVect;
    if (!get_clsid(clsid_str, &clsid, clsidVect))
        return false;

    comhost_exports comhost(comhost_path);

    for (int i = 0; i < count; ++i)
    {
        HRESULT hr = activate_class(comhost, clsid);
        if (SUCCEEDED(hr))
            return false;

        IErrorInfo *ei = nullptr;
        hr = ::GetErrorInfo(0, &ei);
        if (FAILED(hr) || ei == nullptr)
        {
            std::cout << "IErrorInfo not set" << std::endl;
            return false;
        }

        BSTR errorDesc = nullptr;
        hr = ei->GetDescription(&errorDesc);
        if (FAILED(hr) || errorDesc == nullptr)
        {
            ei->Release();
            std::cout << "IErrorInfo description not set" << std::endl;
            return false;
        }

        std::wcout << errorDesc << std::endl;

        ::SysFreeString(errorDesc);
        ei->Release();
    }

    return true;
}

bool comhost_test::typelib(const pal::string_t &comhost_path, int count)
{
    HRESULT hr;

    hr = load_typelib(comhost_path);
    log_default_typelib_load(hr, std::cout);
    if (FAILED(hr))
        return false;

    for (int i = 1; i < count + 1; i++)
    {
        // The path format for a non-default embedded TLB is 'C:\file\path\to.exe\\2' where '2' is the resource name of the tlb to load.
        // See https://docs.microsoft.com/windows/win32/api/oleauto/nf-oleauto-loadtypelib#remarks for documentation on the path format.
        pal::stringstream_t tlb_path;
        tlb_path << comhost_path << '\\' << i;
        hr = load_typelib(tlb_path.str());
        log_typelib_load(i, hr, std::cout);
        if (FAILED(hr))
            return false;
    }
    return true;
}
