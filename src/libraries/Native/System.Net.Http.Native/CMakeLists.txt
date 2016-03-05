project(System.Net.Http.Native)

find_package(CURL)
if(NOT CURL_FOUND)
    message(FATAL_ERROR "!!! Cannot find libcurl and System.Net.Http.Native cannot build without it. Try installing libcurl4-openssl-dev (or the appropriate package for your platform) !!!")
endif(NOT CURL_FOUND)

set(NATIVEHTTP_SOURCES
    pal_curlinit.cpp
    pal_easy.cpp
    pal_multi.cpp
    pal_slist.cpp
    pal_versioninfo.cpp
)

include_directories(SYSTEM ${CURL_INCLUDE_DIR})

add_library(System.Net.Http.Native
    SHARED
    ${NATIVEHTTP_SOURCES}
)

target_link_libraries(System.Net.Http.Native
  ${CURL_LIBRARIES}
)

install (TARGETS System.Net.Http.Native DESTINATION .)
