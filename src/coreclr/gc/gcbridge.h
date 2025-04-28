// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _GCBRIDGE_H_
#define _GCBRIDGE_H_

#include "common.h"
#include "gcinterface.h"

struct BridgeProcessorResult
{
    int sccsLen;
    StronglyConnectedComponent *sccs;

    int ccrsLen;
    ComponentCrossReference *ccrs;
};

void BridgeResetData();
BridgeProcessorResult ProcessBridgeObjects();

void RegisterBridgeObject(Object *object, uintptr_t context);
uint8_t** GetRegisteredBridges(size_t *pNumBridges);

#endif // _GCBRIDGE_H_
