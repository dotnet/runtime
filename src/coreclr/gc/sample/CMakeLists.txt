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
    ../gceewks.cpp
    ../gchandletable.cpp
    ../gcscan.cpp
    ../gcwks.cpp
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

if(CLR_CMAKE_TARGET_WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib)
endif(CLR_CMAKE_TARGET_WIN32)

add_definitions(-DVERIFY_HEAP)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../windows/gcenv.windows.cpp)
    add_definitions(-DUNICODE)
else()
    list(APPEND SOURCES
        ../gcenv.unix.cpp)
endif()

add_executable_clr(gcsample
    ${SOURCES}
)

if(CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(gcsample ${GC_LINK_LIBRARIES})
endif()
