// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef GCHANDLETABLE_H_
#define GCHANDLETABLE_H_

#include "gcinterface.h"

class GCHandleTable : public IGCHandleTable
{
public:
    virtual bool Initialize();

    virtual void Shutdown();
};

#endif  // GCHANDLETABLE_H_
