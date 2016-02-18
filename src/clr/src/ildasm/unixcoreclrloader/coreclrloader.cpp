// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <assert.h>
#include "dlfcn.h"
#include "coreclrloader.h"
#include "coreruncommon.h"

using namespace std;
void *CoreCLRLoader::LoadFunction(const char *funcName)
{
    void *func = nullptr;
    if (coreclrLib == nullptr)
    {
        fprintf(stderr, "Error: coreclr should be loaded before loading a function: %s\n", funcName);
    }
    else
    {
        func = dlsym(coreclrLib, funcName);
        if (func == nullptr)
        {
            fprintf(stderr, "Error: cannot find %s in coreclr\n", funcName);
        }
    }
    return func;
}

CoreCLRLoader* CoreCLRLoader::Create(const char *exePath)
{
    string absolutePath, coreClrPath;
    bool success = GetAbsolutePath(exePath, absolutePath);
    if (!success)
    {
        success = GetEntrypointExecutableAbsolutePath(absolutePath);
        assert(success);
    }
    GetDirectory(absolutePath.c_str(), coreClrPath);
    coreClrPath.append("/");
    coreClrPath.append(coreClrDll);

    CoreCLRLoader *loader = new CoreCLRLoader();
    loader->coreclrLib = dlopen(coreClrPath.c_str(), RTLD_NOW | RTLD_LOCAL);
    if (loader->coreclrLib == nullptr)
    {
        fprintf(stderr, "Error: Fail to load %s\n", coreClrPath.c_str());
        delete loader;
        return nullptr;
    }
    else
    {
        loader->initializeCoreCLR = (InitializeCoreCLRFunction)loader->LoadFunction("coreclr_initialize");
        loader->shutdownCoreCLR = (ShutdownCoreCLRFunction)loader->LoadFunction("coreclr_shutdown");
        int ret = loader->initializeCoreCLR(
                        exePath,
                        "coreclrloader",
                        0,
                        0,
                        0,
                        &loader->hostHandle,
                        &loader->domainId);
        if (ret != 0)
        {
            fprintf(stderr, "Error: Fail to initialize CoreCLR\n");
            delete loader;
            return nullptr;
        }
    }
    return loader;
}

int CoreCLRLoader::Finish()
{
  if (hostHandle != 0)
  {
      shutdownCoreCLR(hostHandle, domainId);
      delete this;
  }
  return 0;
}
