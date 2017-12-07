project(System.Net.Http.Native)

find_package(CURL)
if(NOT CURL_FOUND)
    message(FATAL_ERROR "!!! Cannot find libcurl and System.Net.Http.Native cannot build without it. Try installing libcurl4-openssl-dev (or the appropriate package for your platform) !!!")
endif(NOT CURL_FOUND)

if(CMAKE_STATIC_LIB_LINK)
    find_library(CURL_STATIC_LIB NAMES libcurl.a)
    if(NOT CURL_STATIC_LIB)
        message(FATAL_ERROR "!!! Cannot find libcurl static lib and System.Net.Http.Native cannot build without it. Try installing libcurl4-openssl-dev (or the appropriate package for your platform) !!!")
    endif(NOT CURL_STATIC_LIB)
    set(CURL_LIBRARIES ${CURL_STATIC_LIB})
    add_compile_options(-DCURL_STATICLIB)
endif(CMAKE_STATIC_LIB_LINK)


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
    ${VERSION_FILE_PATH}
)

add_library(System.Net.Http.Native-Static
    STATIC
    ${NATIVEHTTP_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.Net.Http.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.Net.Http.Native-Static PROPERTIES OUTPUT_NAME System.Net.Http.Native CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.Net.Http.Native
  ${CURL_LIBRARIES}
)

install_library_and_symbols (System.Net.Http.Native)
install (TARGETS System.Net.Http.Native-Static DESTINATION .)
