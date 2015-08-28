project(System.Net.Http.Native)

find_library(CURL NAMES curl)
if(CURL STREQUAL CURL-NOTFOUND)
    message(WARNING "Cannot find libcurl, skipping build for System.Net.Http.Native. .NET HttpClient is not expected to function. Try installing libcurl4-dev (or the appropriate package for your platform)")
    return()
endif()

set(NATIVEHTTP_SOURCES
    pal_curlinit.cpp
)

add_library(System.Net.Http.Native
    SHARED
    ${NATIVEHTTP_SOURCES}
)

target_link_libraries(System.Net.Http.Native
  ${CURL}
)

install (TARGETS System.Net.Http.Native DESTINATION .)
