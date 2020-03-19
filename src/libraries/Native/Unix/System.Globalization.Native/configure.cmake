include(CheckCSourceCompiles)
include(CheckSymbolExists)

if(CLR_CMAKE_TARGET_ANDROID)
    string(REPLACE ";" ":" ANDROID_RPATHS "${CMAKE_C_IMPLICIT_LINK_DIRECTORIES}:${CMAKE_SYSTEM_LIBRARY_PATH}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -rpath ${ANDROID_RPATHS}")
endif()

if (CLR_CMAKE_TARGET_UNIX)
    set(CMAKE_REQUIRED_INCLUDES ${UTYPES_H} ${ICU_HOMEBREW_INC_PATH})

    CHECK_C_SOURCE_COMPILES("
        #include <unicode/udat.h>
        int main(void) { enum UDateFormatSymbolType e = UDAT_STANDALONE_SHORTER_WEEKDAYS; }
    " HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS)

    if(CLR_CMAKE_TARGET_DARWIN)
        set(CMAKE_REQUIRED_LIBRARIES ${ICUCORE})
    else()
        set(CMAKE_REQUIRED_LIBRARIES ${ICUUC} ${ICUI18N})
    endif()

    check_symbol_exists(
        ucol_setMaxVariable
        "unicode/ucol.h"
        HAVE_SET_MAX_VARIABLE)

    unset(CMAKE_REQUIRED_LIBRARIES)
    unset(CMAKE_REQUIRED_INCLUDES)
endif()

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
