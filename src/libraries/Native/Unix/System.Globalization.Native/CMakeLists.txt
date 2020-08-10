project(System.Globalization.Native C)

if(CLR_CMAKE_TARGET_UNIX)
    add_compile_options(-Wno-switch-enum)
    add_compile_options(-Wno-covered-switch-default)

    # Workaround for warnings produced by ICU headers
    add_compile_options(-Wno-reserved-id-macro)
    add_compile_options(-Wno-documentation)
    add_compile_options(-Wno-documentation-unknown-command)

    # Workaround for https://unicode-org.atlassian.net/browse/ICU-20601
    add_compile_options(-Wno-extra-semi-stmt)
    add_compile_options(-Wno-unknown-warning-option)

    if (NOT CLR_CMAKE_TARGET_ANDROID)
        set(ICU_HOMEBREW_INC_PATH "/usr/local/opt/icu4c/include")

        find_path(UTYPES_H "unicode/utypes.h" PATHS ${ICU_HOMEBREW_INC_PATH})
        if(UTYPES_H STREQUAL UTYPES_H-NOTFOUND)
            message(FATAL_ERROR "Cannot find utypes.h, try installing libicu-dev (or the appropriate package for your platform)")
            return()
        endif()

        if(CLR_CMAKE_TARGET_OSX)
            add_definitions(-DOSX_ICU_LIBRARY_PATH="/usr/lib/libicucore.dylib")
            add_definitions(-DU_DISABLE_RENAMING)
        else()
            find_library(ICUUC icuuc)
            if(ICUUC STREQUAL ICUUC-NOTFOUND)
                message(FATAL_ERROR "Cannot find libicuuc, try installing libicu-dev (or the appropriate package for your platform)")
                return()
            endif()

            find_library(ICUI18N icui18n)
            if(ICUI18N STREQUAL ICUI18N-NOTFOUND)
                message(FATAL_ERROR "Cannot find libicui18n, try installing libicu-dev (or the appropriate package for your platform)")
                return()
            endif()
        endif()

        include_directories(${UTYPES_H})
    endif()
endif()

include(configure.cmake)

set(NATIVEGLOBALIZATION_SOURCES
    pal_calendarData.c
    pal_casing.c
    pal_collation.c
    pal_idna.c
    pal_locale.c
    pal_localeNumberData.c
    pal_localeStringData.c
    pal_normalization.c
    pal_timeZoneInfo.c
    pal_icushim.c
    entrypoints.c
)

if (MSVC)
    set_source_files_properties(${NATIVEGLOBALIZATION_SOURCES} PROPERTIES LANGUAGE CXX)
endif()

include_directories("../Common")

if (GEN_SHARED_LIB)
    add_library(System.Globalization.Native
        SHARED
        ${NATIVEGLOBALIZATION_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.Globalization.Native
        dl
    )

    install_with_stripped_symbols (System.Globalization.Native PROGRAMS .)
endif()

add_library(System.Globalization.Native_obj
    OBJECT
    ${NATIVEGLOBALIZATION_SOURCES}
)

set_target_properties(System.Globalization.Native_obj PROPERTIES OUTPUT_NAME System.Globalization.Native  CLEAN_DIRECT_OUTPUT 1)

add_library(System.Globalization.Native-static INTERFACE)
target_sources(System.Globalization.Native-static INTERFACE $<TARGET_OBJECTS:System.Globalization.Native_obj>)

if(NOT CLR_CMAKE_TARGET_OSX AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID)
    if (GEN_SHARED_LIB)
        add_custom_command(TARGET System.Globalization.Native POST_BUILD
            COMMENT "Verifying System.Globalization.Native.so dependencies"
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-so.sh
                $<TARGET_FILE:System.Globalization.Native>
                "Verification failed. System.Globalization.Native.so has undefined dependencies. These are likely ICU APIs that need to be added to icushim.h."
            VERBATIM
        )
    endif()
endif()
