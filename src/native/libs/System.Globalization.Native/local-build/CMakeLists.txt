cmake_minimum_required(VERSION 3.6)

project(System.Globalization.Native.Local.Build C)

if(CMAKE_SYSTEM_NAME STREQUAL "Linux") # Only Linux for now

configure_file(../config.h.in config.h)

set(NATIVEGLOBALIZATION_SOURCES
  ../pal_calendarData.c
  ../pal_casing.c
  ../pal_collation.c
  ../pal_idna.c
  ../pal_locale.c
  ../pal_localeNumberData.c
  ../pal_localeStringData.c
  ../pal_normalization.c
  ../pal_icushim_static.c
  ../pal_timeZoneInfo.c
)

add_library(System.Globalization.Native
    STATIC
    ${NATIVEGLOBALIZATION_SOURCES}
)

add_definitions(-DPALEXPORT=EXTERN_C -DTARGET_UNIX -DSTATIC_ICU -DSTATIC_SHIM_COMPILE)

target_compile_options(System.Globalization.Native PUBLIC -std=gnu99 -fPIC -fno-omit-frame-pointer -fstack-protector-strong)

target_include_directories (System.Globalization.Native PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/../../../ ${CMAKE_CURRENT_SOURCE_DIR}/../../../libs/Common ${CMAKE_CURRENT_BINARY_DIR})

else()
  message( FATAL_ERROR "Static builds are supported only on Linux" )
endif()
