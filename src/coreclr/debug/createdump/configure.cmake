include(CheckFunctionExists)
include(CheckSymbolExists)
include(CMakePushCheckState)

check_function_exists(process_vm_readv HAVE_PROCESS_VM_READV)

cmake_push_check_state()
if(NOT CLR_CMAKE_HOST_APPLE AND NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  list(APPEND CMAKE_REQUIRED_INCLUDES ${CLR_SRC_NATIVE_DIR}/external/libunwind/include ${CLR_ARTIFACTS_OBJ_DIR}/external/libunwind/include)
endif()
check_symbol_exists(unw_get_accessors libunwind.h HAVE_UNW_GET_ACCESSORS)
check_symbol_exists(unw_get_proc_info_in_range libunwind.h HAVE_GET_PROC_INFO_IN_RANGE)
cmake_pop_check_state()

configure_file(${CMAKE_CURRENT_SOURCE_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
