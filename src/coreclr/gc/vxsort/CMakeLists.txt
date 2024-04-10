set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")

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
  isa_detection.cpp
  do_vxsort_avx2.cpp
  do_vxsort_avx512.cpp
  machine_traits.avx2.cpp
  smallsort/bitonic_sort.AVX2.int64_t.generated.cpp
  smallsort/bitonic_sort.AVX2.int32_t.generated.cpp
  smallsort/bitonic_sort.AVX512.int64_t.generated.cpp
  smallsort/bitonic_sort.AVX512.int32_t.generated.cpp
  smallsort/avx2_load_mask_tables.cpp
  do_vxsort.h
)

add_library(gc_vxsort STATIC ${VXSORT_SOURCES})
