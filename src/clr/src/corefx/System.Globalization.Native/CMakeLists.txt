
project(System.Globalization.Native)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC=1)
add_definitions(-DBIT64=1)

set(ICU_HOMEBREW_INC_PATH "/usr/local/opt/icu4c/include")

find_path(UTYPES_H "unicode/utypes.h" PATHS ${ICU_HOMEBREW_INC_PATH})
if(UTYPES_H STREQUAL UTYPES_H-NOTFOUND)
    message(FATAL_ERROR "Cannont find utypes.h, try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

if (FEATURE_FIXED_ICU_VERSION AND NOT CLR_CMAKE_PLATFORM_DARWIN)
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

if(CLR_CMAKE_PLATFORM_DARWIN)
    find_library(ICUCORE icucore)
    if(ICUI18N STREQUAL ICUCORE-NOTFOUND)
        message(FATAL_ERROR "Cannot find libicucore, skipping build for System.Globalization.Native. .NET globalization is not expected to function.")
        return()
    endif()
    # On Darwin, we always use the OS provided ICU
    SET(FEATURE_FIXED_ICU_VERSION 1)
endif()

include(configure.cmake)

add_compile_options(-fPIC)

set(NATIVEGLOBALIZATION_SOURCES
    calendarData.cpp
    casing.cpp
    collation.cpp
    idna.cpp
    locale.cpp
    localeNumberData.cpp
    localeStringData.cpp
    normalization.cpp
    timeZoneInfo.cpp
)

if (NOT FEATURE_FIXED_ICU_VERSION)
    list(APPEND NATIVEGLOBALIZATION_SOURCES
        icushim.cpp
    )
endif()

include_directories(${UTYPES_H})

_add_library(System.Globalization.Native
    SHARED
    ${NATIVEGLOBALIZATION_SOURCES}
)

_add_library(System.Globalization.Native_Static
    STATIC
    ${NATIVEGLOBALIZATION_SOURCES}
)

# Remove the _Static postfix from the static library name
set_target_properties(System.Globalization.Native_Static PROPERTIES OUTPUT_NAME System.Globalization.Native)

# Disable the "lib" prefix.
set_target_properties(System.Globalization.Native PROPERTIES PREFIX "")

if (FEATURE_FIXED_ICU_VERSION)
    add_definitions(-DFEATURE_FIXED_ICU_VERSION)
endif()

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    if (FEATURE_FIXED_ICU_VERSION)
        target_link_libraries(System.Globalization.Native
            ${ICUUC}
            ${ICUI18N}
        )
        target_link_libraries(System.Globalization.Native_Static
            ${ICUUC}
            ${ICUI18N}
        )
    elseif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
        target_link_libraries(System.Globalization.Native
            dl
        )
        target_link_libraries(System.Globalization.Native_Static
            dl
        )
    endif()
else()
    target_link_libraries(System.Globalization.Native
        ${ICUCORE}
    )

    add_definitions(-DU_DISABLE_RENAMING=1)
endif()

verify_dependencies(
    System.Globalization.Native
    "Verification failed. System.Globalization.Native.so has undefined dependencies. These are likely ICU APIs that need to be added to icushim.h."
)

# add the install targets
install_clr(System.Globalization.Native)
install(TARGETS System.Globalization.Native_Static DESTINATION .)

