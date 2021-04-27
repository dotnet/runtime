include(CheckStructHasMember)
include(CheckSymbolExists)

check_struct_has_member("struct dirent" d_type dirent.h HAVE_DIRENT_D_TYPE)

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)
