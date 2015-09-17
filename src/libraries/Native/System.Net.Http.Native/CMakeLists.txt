project(System.Net.Http.Native)

find_library(CURL NAMES curl)
if(CURL STREQUAL CURL-NOTFOUND)
    message(SEND_ERROR "!!! Cannot find libcurl and System.Net.Http.Native cannot build without it. Try installing libcurl4-openssl-dev (or the appropriate package for your platform) !!!")
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
