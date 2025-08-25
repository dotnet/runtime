// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.
//
// This file is compiled using the target architecture.  Preprocessor defines for the target
// platform will be available.  It is ok to use `#ifdef`.


CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()


#ifdef SERVER_GC
#define GC_TYPE server
#else // SERVER_GC
#define GC_TYPE workstation
#endif // SERVER_GC

#ifdef USE_REGIONS
#define HEAP_TYPE regions
#else // USE_REGIONS
#define HEAP_TYPE segments
#endif // USE_REGIONS

// CDAC_GLOBAL_STRING takes a single value argument.
// To avoid issues with commas in the string we wrap the input string in a macro.
#define GC_IDENTIFIER(...) __VA_ARGS__ // GC_IDENTIFIER(gc, heap) expands to: gc, heap
CDAC_GLOBAL_STRING(GCIdentifiers, GC_IDENTIFIER(GC_TYPE, HEAP_TYPE))

CDAC_GLOBAL_POINTER(MaxGeneration, &::g_max_generation)
CDAC_GLOBAL_POINTER(StructureInvalidCount, &GCScan::m_GcStructuresInvalidCnt)

#ifdef SERVER_GC
CDAC_GLOBAL_POINTER(NumHeaps, &GC_NAMESPACE::gc_heap::n_heaps)
CDAC_GLOBAL_POINTER(Heaps, cdac_data<GC_NAMESPACE::gc_heap>::Heaps)
#endif // SERVER_GC

CDAC_GLOBALS_END()
