if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST)
    set(HAVE_SET_MAX_VARIABLE 1)
    set(HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS 1)
else()
    include(CheckCSourceCompiles)
    include(CheckSymbolExists)

    if (CLR_CMAKE_TARGET_UNIX)
        set(CMAKE_REQUIRED_INCLUDES ${UTYPES_H} ${ICU_HOMEBREW_INC_PATH})

        CHECK_C_SOURCE_COMPILES("
            #include <unicode/udat.h>
            int main(void) { enum UDateFormatSymbolType e = UDAT_STANDALONE_SHORTER_WEEKDAYS; }
        " HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS)

        set(CMAKE_REQUIRED_LIBRARIES ${ICUUC} ${ICUI18N})

        check_symbol_exists(
            ucol_setMaxVariable
            "unicode/ucol.h"
            HAVE_SET_MAX_VARIABLE)

        unset(CMAKE_REQUIRED_LIBRARIES)
        unset(CMAKE_REQUIRED_INCLUDES)
    endif()
endif()

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
