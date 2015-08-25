
project(System.Globalization.Native)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC=1)
add_definitions(-DBIT64=1)

find_library(ICUUC NAMES icuuc)
if(ICUUC STREQUAL ICUUC-NOTFOUND)
    message(WARNING "Cannot find libicuuc, skipping build for System.Globalization.Native. .NET globalization is not expected to function. Try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

add_compile_options(-fPIC)

set(NATIVEGLOBALIZATION_SOURCES
    calendarData.cpp
    casing.cpp
    idna.cpp
    locale.cpp
    localeNumberData.cpp
    localeStringData.cpp
    normalization.cpp
)

add_library(System.Globalization.Native
    SHARED
    ${NATIVEGLOBALIZATION_SOURCES}
)

# Disable the "lib" prefix.
set_target_properties(System.Globalization.Native PROPERTIES PREFIX "")

target_link_libraries(System.Globalization.Native
  ${ICUUC}
  ${ICUI18N}
)

install (TARGETS System.Globalization.Native DESTINATION .)
