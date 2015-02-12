//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifdef FEATURE_PAL

#include <clrdata.h>

void
DummyReferenceToExportedAPI()
#ifdef __llvm__
__attribute__((used))
#endif // __llvm__
{
    CLRDataCreateInstance(IID_ICLRDataTarget, NULL, NULL);
}

#endif // FEATURE_PAL
