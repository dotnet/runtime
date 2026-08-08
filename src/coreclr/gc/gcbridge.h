// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCBRIDGE_H_
#define _GCBRIDGE_H_

#ifdef FEATURE_JAVAMARSHAL

#include "common.h"
#include "gcinterface.h"

void BridgeResetData();
MarkCrossReferencesArgs* ProcessBridgeObjects();

// Decides whether this collection should hand a fresh set of cross references to the client.
// Returns false when the client is still processing a previous set (the new one would just be
// discarded) or when the request would arrive too soon after the previous one. Only gen0
// collections are ever throttled, so a deferred object is guaranteed to be reconsidered by the
// next gen1 or gen2 collection.
bool ShouldProcessBridgeObjects(uint32_t condemned);

void RegisterBridgeObject(Object *object, uintptr_t context);
uint8_t** GetRegisteredBridges(size_t *pNumBridges);

#endif // FEATURE_JAVAMARSHAL

#endif // _GCBRIDGE_H_
