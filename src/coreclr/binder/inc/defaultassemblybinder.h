// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DEFAULT_ASSEMBLY_BINDER_H__
#define __DEFAULT_ASSEMBLY_BINDER_H__

#include "assemblybinder.h"

class PEAssembly;
class PEImage;

class DefaultAssemblyBinder final : public AssemblyBinder
{
public:

    AssemblyLoaderAllocator* GetLoaderAllocator() override
    {
        // Not supported by this binder
        return NULL;
    }

    bool IsDefault() override
    {
        return true;
    }
};

#endif // __DEFAULT_ASSEMBLY_BINDER_H__
