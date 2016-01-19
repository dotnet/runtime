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

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_precompiled_header(stdafx.h stdafx.cpp CEEFILEGEN_SOURCES)

add_library_clr(ceefgen
    STATIC
    ${CEEFILEGEN_SOURCES}
)
