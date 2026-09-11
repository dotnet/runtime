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
// Returns false when the client is still processing a previous set, since the new one would
// just be discarded.
bool ShouldProcessBridgeObjects();

void RegisterBridgeObject(Object *object, uintptr_t context);
uint8_t** GetRegisteredBridges(size_t *pNumBridges);

#endif // FEATURE_JAVAMARSHAL

#endif // _GCBRIDGE_H_
