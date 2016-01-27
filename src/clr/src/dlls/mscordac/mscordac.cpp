// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifdef FEATURE_PAL

#include <clrdata.h>

//
// This dummy reference to CLRDataCreateInstance prevents the LLVM toolchain from optimizing this important export out.
//
#ifdef __llvm__
__attribute__((used))
#endif // __llvm__
void
DummyReferenceToExportedAPI()
{
    CLRDataCreateInstance(IID_ICLRDataTarget, NULL, NULL);
}

#endif // FEATURE_PAL
