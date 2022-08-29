// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Any global function that should be tracked in the DAC should be provided here

#ifndef DEFINE_DACGFN
#define DEFINE_DACGFN(func)
#endif
#ifndef DEFINE_DACGFN_STATIC
#define DEFINE_DACGFN_STATIC(class, func)
#endif

DEFINE_DACGFN(DACNotifyCompilationFinished)
DEFINE_DACGFN(ThePreStub)

#ifdef TARGET_ARM
DEFINE_DACGFN(ThePreStubCompactARM)
#endif

DEFINE_DACGFN(ThePreStubPatchLabel)
#ifdef FEATURE_COMINTEROP
DEFINE_DACGFN(Unknown_AddRef)
DEFINE_DACGFN(Unknown_AddRefSpecial)
DEFINE_DACGFN(Unknown_AddRefInner)
#endif
#ifdef FEATURE_COMWRAPPERS
DEFINE_DACGFN(ManagedObjectWrapper_QueryInterface)
DEFINE_DACGFN(TrackerTarget_QueryInterface)
#endif
