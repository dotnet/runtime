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

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND CEEFILEGEN_SOURCES ${CEEFILEGEN_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(ceefgen_obj
    OBJECT
    ${CEEFILEGEN_SOURCES}
)
target_precompile_headers(ceefgen_obj PRIVATE stdafx.h)

add_library(ceefgen INTERFACE)
target_sources(ceefgen INTERFACE $<TARGET_OBJECTS:ceefgen_obj>)
