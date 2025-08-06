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
CDAC_GLOBAL_STRING(GCType, server)
#else // SERVER_GC
CDAC_GLOBAL_STRING(GCType, workstation)
#endif // SERVER_GC

#ifdef USE_REGIONS
CDAC_GLOBAL_STRING(HeapType, regions)
#else // USE_REGIONS
CDAC_GLOBAL_STRING(HeapType, segments)
#endif // USE_REGIONS


CDAC_GLOBAL_POINTER(MaxGeneration, &::g_max_generation)
CDAC_GLOBAL_POINTER(StructureInvalidCount, (&GCScan::m_GcStructuresInvalidCnt))

#ifdef SERVER_GC
CDAC_GLOBAL_POINTER(NumHeaps, &GC_NAMESPACE::gc_heap::n_heaps)
CDAC_GLOBAL_POINTER(Heaps, cdac_data<GC_NAMESPACE::gc_heap>::Heaps)
#endif // SERVER_GC

CDAC_GLOBALS_END()

#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef CDAC_TYPES_END
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL_STRING
#undef CDAC_GLOBAL_SUB_DESCRIPTOR
#undef CDAC_GLOBALS_END
