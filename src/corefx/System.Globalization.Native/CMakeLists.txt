
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

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
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

include_directories(${UTYPES_H})

_add_library(System.Globalization.Native
    SHARED
    ${NATIVEGLOBALIZATION_SOURCES}
)

# Disable the "lib" prefix.
set_target_properties(System.Globalization.Native PROPERTIES PREFIX "")

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    target_link_libraries(System.Globalization.Native
        ${ICUUC}
        ${ICUI18N}
    )
else()
    target_link_libraries(System.Globalization.Native
        ${ICUCORE}
    )

    add_definitions(-DU_DISABLE_RENAMING=1)
endif()

# add the install targets
install_clr(System.Globalization.Native)