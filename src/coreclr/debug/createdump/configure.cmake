include(CheckFunctionExists)
include(CheckSymbolExists)

check_function_exists(process_vm_readv HAVE_PROCESS_VM_READV)

if(CLR_CMAKE_HOST_APPLE OR NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  # The bundled libunwind has both.
  set(HAVE_UNW_GET_ACCESSORS 1)
  set(HAVE_GET_PROC_INFO_IN_RANGE 1)
else()
  check_symbol_exists(unw_get_accessors libunwind.h HAVE_UNW_GET_ACCESSORS)
  check_symbol_exists(unw_get_proc_info_in_range libunwind.h HAVE_GET_PROC_INFO_IN_RANGE)
endif()

configure_file(${CMAKE_CURRENT_SOURCE_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
