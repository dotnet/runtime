project(clrgcsample)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(..)
include_directories(../env)

set(SOURCES
    GCSample.cpp
    gcenv.ee.cpp
    ../gceventstatus.cpp
    ../gcconfig.cpp
    ../gccommon.cpp
    ../gcee.cpp
    ../gchandletable.cpp
    ../gcscan.cpp
    ../gc.cpp
    ../init.cpp
    ../no_gc.cpp
    ../finalization.cpp
    ../dynamic_tuning.cpp
    ../region_free_list.cpp
    ../region_allocator.cpp
    ../memory.cpp
    ../sweep.cpp
    ../collect.cpp
    ../diagnostics.cpp
    ../dynamic_heap_count.cpp
    ../card_table.cpp
    ../relocate_compact.cpp
    ../mark_phase.cpp
    ../background.cpp
    ../interface.cpp
    ../allocation.cpp
    ../plan_phase.cpp
    ../regions_segments.cpp
    ../gcload.cpp
    ../handletable.cpp
    ../handletablecache.cpp
    ../handletablecore.cpp
    ../handletablescan.cpp
    ../objecthandle.cpp
    ../softwarewritewatch.cpp
)

if (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)
  set ( SOURCES
    ${SOURCES}
    ../vxsort/isa_detection.cpp
    ../vxsort/do_vxsort_avx2.cpp
    ../vxsort/do_vxsort_avx512.cpp
    ../vxsort/machine_traits.avx2.cpp
    ../vxsort/smallsort/bitonic_sort.AVX2.int64_t.generated.cpp
    ../vxsort/smallsort/bitonic_sort.AVX2.int32_t.generated.cpp
    ../vxsort/smallsort/bitonic_sort.AVX512.int64_t.generated.cpp
    ../vxsort/smallsort/bitonic_sort.AVX512.int32_t.generated.cpp
    ../vxsort/smallsort/avx2_load_mask_tables.cpp
)
endif (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_ARCH_ARM64 AND CLR_CMAKE_TARGET_WIN32)
  set ( SOURCES
    ${SOURCES}
    ../vxsort/isa_detection.cpp
    ../vxsort/do_vxsort_neon.cpp
    ../vxsort/machine_traits.neon.cpp
    ../vxsort/smallsort/bitonic_sort.NEON.uint32_t.generated.cpp
    ../vxsort/smallsort/bitonic_sort.scalar.uint64_t.generated.cpp
)
endif(CLR_CMAKE_TARGET_ARCH_ARM64 AND CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib
    minipal
  )
endif(CLR_CMAKE_TARGET_WIN32)

add_definitions(-DVERIFY_HEAP)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../windows/gcenv.windows.cpp)
    add_definitions(-DUNICODE)
    add_compile_definitions(NOMINMAX)
else()
    list(APPEND SOURCES
        ../gcenv.unix.cpp)
endif()

add_executable_clr(gcsample
    ${SOURCES}
)

if(CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(gcsample PRIVATE ${GC_LINK_LIBRARIES})
endif()
