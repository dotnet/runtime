project(eglib C)

set(eglib_win32_sources
    gdate-win32.c
    gfile-win32.c
    gmisc-win32.c
    gmodule-win32.c
    gtimer-win32.c)

set(eglib_unix_sources
    gdate-unix.c
    gfile-unix.c
    gmisc-unix.c
    gmodule-unix.c
    gtimer-unix.c)

if(HOST_WIN32)
set(eglib_platform_sources ${eglib_win32_sources})
else()
set(eglib_platform_sources ${eglib_unix_sources})
endif()

set(eglib_common_sources
    sort.frag.h
    garray.c
    gbytearray.c
    gerror.c
    ghashtable.c
    giconv.c
    gmem.c
    goutput.c
    gstr.c
    gslist.c
    gstring.c
    gptrarray.c
    glist.c
    gqueue.c
    gpath.c
    gfile.c
    gfile-posix.c
    gutf8.c
    ${CLR_SRC_NATIVE_DIR}/minipal/utf8.c)

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
  set_source_files_properties("${CLR_SRC_NATIVE_DIR}/minipal/utf8.c" PROPERTIES COMPILE_FLAGS "-DBIGENDIAN=1")
endif()

set(eglib_headers
  glib.h
  eglib-remap.h
  gmodule.h)

if(HAVE_CLOCK_NANOSLEEP)
  list(APPEND eglib_common_sources gclock-nanosleep.c)
endif()

set(eglib_sources "${eglib_platform_sources};${eglib_common_sources}")

add_library(eglib_api INTERFACE)
target_sources(eglib_api INTERFACE ${eglib_headers})
# to pick up eglib-config.h and glib.h, respectively
target_include_directories(eglib_api INTERFACE ${PROJECT_BINARY_DIR} ${PROJECT_SOURCE_DIR})

add_library(eglib_objects OBJECT "${eglib_sources}")
# to pick up config.h, and the eglib headers, respectively
target_include_directories(eglib_objects PRIVATE
  ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_SOURCE_DIR})
target_link_libraries (eglib_objects PUBLIC eglib_api)
