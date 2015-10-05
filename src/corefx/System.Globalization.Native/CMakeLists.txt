
project(System.Globalization.Native)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DPIC=1)
add_definitions(-DBIT64=1)

set(ICU_HOMEBREW_LIB_PATH "/usr/local/opt/icu4c/lib")
set(ICU_HOMEBREW_INC_PATH "/usr/local/opt/icu4c/include")

find_library(ICUUC NAMES icuuc PATHS ${ICU_HOMEBREW_LIB_PATH})
if(ICUUC STREQUAL ICUUC-NOTFOUND)
    message(FATAL_ERROR "Cannot find libicuuc, try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

find_library(ICUI18N NAMES icui18n PATHS ${ICU_HOMEBREW_LIB_PATH})
if(ICUI18N STREQUAL ICUI18N-NOTFOUND)
    message(FATAL_ERROR "Cannot find libicui18n, try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

find_path(UTYPES_H "unicode/utypes.h" PATHS ${ICU_HOMEBREW_INC_PATH})
if(UTYPES_H STREQUAL UTYPES_H-NOTFOUND)
    message(FATAL_ERROR "Cannont find utypes.h, try installing libicu-dev (or the appropriate package for your platform)")
    return()
endif()

set(CMAKE_REQUIRED_INCLUDES ${ICU_HOMEBREW_INC_PATH})
CHECK_CXX_SOURCE_COMPILES("
 #include <unicode/dtfmtsym.h>
 int main() { DateFormatSymbols::DtWidthType e = DateFormatSymbols::DtWidthType::SHORT; }
" HAVE_DTWIDTHTYPE_SHORT) 

if(HAVE_DTWIDTHTYPE_SHORT)
    add_definitions(-DHAVE_DTWIDTHTYPE_SHORT=1)
endif(HAVE_DTWIDTHTYPE_SHORT)

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
