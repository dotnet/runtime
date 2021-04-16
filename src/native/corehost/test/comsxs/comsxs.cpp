// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <thread>
#include <windows.h>
#include <pal.h>

#define IfFailRet(F) if (FAILED(hr = (F))) return hr

struct __declspec(uuid("27293cc8-7933-4fdf-9fde-653cbf9b55df"))
IServer : public IDispatch
{
    virtual HRESULT MethodCall() = 0;
};

// comsxs.exe clsid
int __cdecl wmain(const int argc, const pal::char_t *argv[])
{
    HRESULT hr;
    IfFailRet(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED));
    std::cout << "Activated main thread in STA" << std::endl;
    if (argc < 2)
    {
        return E_INVALIDARG;
    }
    const pal::char_t* clsidStr = argv[1];
    CLSID clsid;
    IfFailRet(CLSIDFromString(clsidStr, &clsid));

    std::wcout << _X("Parsed class id ") << clsidStr << _X(" from the command line.") << std::endl;

    IServer* pServer = nullptr;
    IfFailRet(CoCreateInstance(clsid, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IServer), (void**)&pServer));

    IGlobalInterfaceTable* pGit;
    IfFailRet(CoCreateInstance(CLSID_StdGlobalInterfaceTable,
                 NULL,
                 CLSCTX_INPROC_SERVER,
                 IID_IGlobalInterfaceTable,
                 (void **)&pGit));

    DWORD gitCookie;
    pGit->RegisterInterfaceInGlobal(pServer, __uuidof(IServer), &gitCookie);

    std::thread mtaThread([gitCookie, pGit, &hr](){
        IfFailRet(CoInitializeEx(NULL, COINIT_MULTITHREADED));
        IServer* pServerProxy = nullptr;
        IfFailRet(pGit->GetInterfaceFromGlobal(gitCookie, __uuidof(IServer), (void**)&pServerProxy));
        std::cout << "Retrieved IServer object from the global interface table." << std::endl;
        IfFailRet(pServerProxy->MethodCall());
        std::cout << "Successfully called method on proxy" << std::endl;
        pServerProxy->Release();
        CoUninitialize();
        return hr = S_OK;
    });

    mtaThread.join();

    IfFailRet(hr);

    pGit->RevokeInterfaceFromGlobal(gitCookie);
    pGit->Release();
    pServer->Release();
    CoUninitialize();
    return S_OK;
}
