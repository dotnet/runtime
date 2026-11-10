if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_APPLE OR CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    set(HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS 1)

    # Android uses its own system ICU
    if(CLR_CMAKE_TARGET_ANDROID)
        set(HAVE_UCOL_CLONE 0)
    else()
        set(HAVE_UCOL_CLONE 1)
    endif()
else()
    include(CheckCSourceCompiles)
    include(CheckSymbolExists)

    if (CLR_CMAKE_TARGET_UNIX)
        set(CMAKE_REQUIRED_INCLUDES ${UCURR_H} ${ICU_HOMEBREW_INC_PATH})
        if (DEFINED CMAKE_ICU_DIR)
            list(APPEND CMAKE_REQUIRED_INCLUDES ${CMAKE_ICU_DIR}/include)
        endif()

        CHECK_C_SOURCE_COMPILES("
            #include <unicode/udat.h>
            int main(void) { enum UDateFormatSymbolType e = UDAT_STANDALONE_SHORTER_WEEKDAYS; }
        " HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS)

        set(CMAKE_REQUIRED_LIBRARIES ${ICUUC} ${ICUI18N})
        if (DEFINED CMAKE_ICU_DIR)
            # With ICU symbol renaming disabled, ucol_clone is a function, so the check also links,
            # but this path sets no ICU libraries: compile only.
            if (DEFINED CMAKE_TRY_COMPILE_TARGET_TYPE)
                set(SAVED_CMAKE_TRY_COMPILE_TARGET_TYPE "${CMAKE_TRY_COMPILE_TARGET_TYPE}")
            endif()
            set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)
        endif()
        check_symbol_exists(
            ucol_clone
            "unicode/ucol.h"
            HAVE_UCOL_CLONE)

        if (DEFINED CMAKE_ICU_DIR)
            if (DEFINED SAVED_CMAKE_TRY_COMPILE_TARGET_TYPE)
                set(CMAKE_TRY_COMPILE_TARGET_TYPE "${SAVED_CMAKE_TRY_COMPILE_TARGET_TYPE}")
                unset(SAVED_CMAKE_TRY_COMPILE_TARGET_TYPE)
            else()
                unset(CMAKE_TRY_COMPILE_TARGET_TYPE)
            endif()
        endif()
        unset(CMAKE_REQUIRED_LIBRARIES)
        unset(CMAKE_REQUIRED_INCLUDES)
    endif()
endif()

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
