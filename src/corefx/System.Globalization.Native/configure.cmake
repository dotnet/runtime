include(CheckCSourceCompiles)
include(CheckSymbolExists)

set(CMAKE_REQUIRED_INCLUDES ${UTYPES_H} ${ICU_HOMEBREW_INC_PATH})

CHECK_C_SOURCE_COMPILES("
    #include <unicode/udat.h>
    int main(void) { enum UDateFormatSymbolType e = UDAT_STANDALONE_SHORTER_WEEKDAYS; }
" HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS)

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_REQUIRED_LIBRARIES ${ICUUC} ${ICUI18N})
else()
    set(CMAKE_REQUIRED_LIBRARIES ${ICUCORE})
endif()

check_symbol_exists(
    ucol_setMaxVariable
    "unicode/ucol.h"
    HAVE_SET_MAX_VARIABLE)

unset(CMAKE_REQUIRED_LIBRARIES)
unset(CMAKE_REQUIRED_INCLUDES)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
