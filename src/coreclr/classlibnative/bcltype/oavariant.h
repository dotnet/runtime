// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OAVariant.h
//

#ifndef _OAVARIANT_H_
#define _OAVARIANT_H_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "olevariant.h"

extern "C" void QCALLTYPE OAVariant_ChangeType(VariantData* result, VariantData* source, LCID lcid, void* targetType, int cvType, INT16 flags);

#endif  // _OAVARIANT_H_
