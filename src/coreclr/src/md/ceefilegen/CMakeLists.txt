set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-D__TODO_PORT_TO_WRAPPERS__)
include_directories("../inc")

set(CEEFILEGEN_SOURCES
    blobfetcher.cpp
    cceegen.cpp
    ceegentokenmapper.cpp
    ceesectionstring.cpp
    pesectionman.cpp
)

set(CEEFILEGEN_HEADERS
    ../../inc/corpriv.h
    ../../inc/blobfetcher.h
    ../../inc/ceegen.h
    ../../inc/ceegentokenmapper.h
    ../../inc/ceesectionstring.h
    ../../inc/pesectionman.h
    ../../inc/utilcode.h
)

if (WIN32)
    list(APPEND CEEFILEGEN_SOURCES ${CEEFILEGEN_HEADERS})
endif (WIN32)

add_library_clr(ceefgen
    STATIC
    ${CEEFILEGEN_SOURCES}
)
target_precompile_header(TARGET ceefgen HEADER stdafx.h)
