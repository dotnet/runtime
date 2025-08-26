// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stddef.h>

#include "common.h"
#include "gcenv.h"
#include "gc.h"
#include "gcscan.h"
#include "gchandletableimpl.h"
#include "gceventstatus.h"

#ifdef SERVER_GC
#define GC_NAMESPACE SVR
#else // SERVER_GC
#define GC_NAMESPACE WKS
#endif // SERVER_GC

// These files are designed to be used inside of the GC namespace.
// Without the namespace (WKS/SVR) there are naming conflicts.
namespace GC_NAMESPACE {

#include "gcimpl.h"
#include "gcpriv.h"

}

// On non-MSVC builds explicit specializations must be declared in the namespace the template was defined.
// Due to the gc being built into coreclr, cdac_data must be defined in the global scope.
template<>
struct cdac_data<GC_NAMESPACE::gc_heap>
{
#ifdef MULTIPLE_HEAPS
    static constexpr GC_NAMESPACE::gc_heap*** Heaps = &GC_NAMESPACE::gc_heap::g_heaps;
#endif // MULTIPLE_HEAPS
};
