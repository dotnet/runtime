set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../../env")
include_directories("../../../../native")

EXECUTE_PROCESS( COMMAND uname -m COMMAND tr -d '\n' OUTPUT_VARIABLE ARCHITECTURE )
if( ${ARCHITECTURE} STREQUAL "aarch64" )
    add_compile_definitions(TARGET_ARM64)
    add_compile_definitions(CPU_FEATURES_ARCH_AARCH64)
endif()
if( ${ARCHITECTURE} STREQUAL "x86_64" )
    add_compile_definitions(TARGET_AMD64)
    add_compile_definitions(CPU_FEATURES_ARCH_X86)
endif()
add_compile_definitions(TARGET_UNIX)
add_compile_definitions(CLR_CMAKE_HOST_UNIX)

if(CLR_CMAKE_HOST_UNIX)
  set_source_files_properties(isa_detection.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(do_vxsort_avx2.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(do_vxsort_avx512.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(machine_traits.avx2.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(smallsort/bitonic_sort.AVX2.int64_t.generated.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(smallsort/bitonic_sort.AVX2.int32_t.generated.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(smallsort/bitonic_sort.AVX512.int64_t.generated.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(smallsort/bitonic_sort.AVX512.int32_t.generated.cpp PROPERTIES COMPILE_FLAGS -mavx2)
  set_source_files_properties(smallsort/avx2_load_mask_tables.cpp PROPERTIES COMPILE_FLAGS -mavx2)
endif(CLR_CMAKE_HOST_UNIX)

set (VXSORT_SOURCES
  ../isa_detection.cpp
  ../do_vxsort.h
)

if( ${ARCHITECTURE} STREQUAL "x86_64" )
set (VXSORT_TARGET_SOURCES
  ../do_vxsort_avx2.cpp
  ../do_vxsort_avx512.cpp
  ../machine_traits.avx2.cpp
  ../smallsort/bitonic_sort.AVX2.int64_t.generated.cpp
  ../smallsort/bitonic_sort.AVX2.int32_t.generated.cpp
  ../smallsort/bitonic_sort.AVX512.int64_t.generated.cpp
  ../smallsort/bitonic_sort.AVX512.int32_t.generated.cpp
  ../smallsort/avx2_load_mask_tables.cpp
)
endif()
if( ${ARCHITECTURE} STREQUAL "aarch64" )
set (VXSORT_TARGET_SOURCES
  ../do_vxsort_neon.cpp
  ../machine_traits.neon.cpp
  ../smallsort/bitonic_sort.NEON.uint32_t.generated.cpp
  ../smallsort/bitonic_sort.scalar.uint64_t.generated.cpp
)
endif()


add_library(gc_vxsort STATIC ${VXSORT_SOURCES} ${VXSORT_TARGET_SOURCES})

add_subdirectory(${PROJECT_SOURCE_DIR}/demo/)
add_subdirectory(${PROJECT_SOURCE_DIR}/simple_bench/)
