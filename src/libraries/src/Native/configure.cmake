include(CheckFunctionExists)
include(CheckStructHasMember)
include(CheckCXXSourceCompiles)

check_function_exists(
    stat64
    HAVE_STAT64)

check_struct_has_member(
    "struct stat"
    st_birthtime
    "sys/types.h;sys/stat.h"
    HAVE_STAT_BIRTHTIME)

check_cxx_source_compiles(
    "
    #include <string.h>
    int main() { char* c = strerror_r(0, 0, 0); }
    "
    HAVE_GNU_STRERROR_R)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
