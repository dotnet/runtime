//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ngenoptout.h
//

//
// 
// Contains functionality to reject native images at runtime


#ifndef NGENOPTOUT_H
#define NGENOPTOUT_H

#include "assemblynamesconfigfactory.h"

// throwing
BOOL IsNativeImageOptedOut(IAssemblyName* pName);
void AddNativeImageOptOut(IAssemblyName* pName);

// HRESULT
HRESULT RuntimeIsNativeImageOptedOut(IAssemblyName* pName);


class NativeImageOptOutConfigFactory : public AssemblyNamesConfigFactory
{
    virtual void AddAssemblyName(IAssemblyName* pName) 
    {
        WRAPPER_NO_CONTRACT;
        AddNativeImageOptOut(pName);
    }
};

#endif // NGENOPTOUT_H
