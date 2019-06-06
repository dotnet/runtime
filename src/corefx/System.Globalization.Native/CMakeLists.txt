project(System.Globalization.Native C)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC)
add_definitions(-DBIT64)

set(ICU_HOMEBREW_INC_PATH "/usr/local/opt/icu4c/include")

find_path(UTYPES_H "unicode/utypes.h" PATHS ${ICU_HOMEBREW_INC_PATH})
if(UTYPES_H STREQUAL UTYPES_H-NOTFOUND)
    message(FATAL_ERROR "Cannot find utypes.h, try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

if (NOT CLR_CMAKE_PLATFORM_DARWIN)
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
else()
    find_library(ICUCORE icucore)
    if(ICUI18N STREQUAL ICUCORE-NOTFOUND)
        message(FATAL_ERROR "Cannot find libicucore, skipping build for System.Globalization.Native. .NET globalization is not expected to function.")
        return()
    endif()

    add_definitions(-DOSX_ICU_LIBRARY_PATH=\"${ICUCORE}\")
endif()

include(configure.cmake)

add_compile_options(-fPIC)

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
)

include_directories(${UTYPES_H})

_add_library(System.Globalization.Native
    SHARED
    ${NATIVEGLOBALIZATION_SOURCES}
)

_add_library(System.Globalization.Native_Static
    STATIC
    ${NATIVEGLOBALIZATION_SOURCES}
)

# Disable the "lib" prefix.
set_target_properties(System.Globalization.Native PROPERTIES PREFIX "")

# Disable the "lib" prefix and override default name
set_target_properties(System.Globalization.Native_Static PROPERTIES PREFIX "")
set_target_properties(System.Globalization.Native_Static PROPERTIES OUTPUT_NAME System.Globalization.Native)

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    if (NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
        target_link_libraries(System.Globalization.Native
            dl
        )
        target_link_libraries(System.Globalization.Native_Static
            dl
        )
    endif()
else()
    target_link_libraries(System.Globalization.Native
        dl
    )

    add_definitions(-DU_DISABLE_RENAMING)
endif()

verify_dependencies(
    System.Globalization.Native
    "Verification failed. System.Globalization.Native.so has undefined dependencies. These are likely ICU APIs that need to be added to icushim.h."
)

# add the install targets
install_clr(System.Globalization.Native)
install(TARGETS System.Globalization.Native_Static DESTINATION .)
