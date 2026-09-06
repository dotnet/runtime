set(CMAKE_INCLUDE_CURRENT_DIR ON)

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

add_library_clr(ceefgen OBJECT ${CEEFILEGEN_SOURCES})
target_precompile_headers(ceefgen PRIVATE stdafx.h)

# SELF_NO_HOST disables the hosted contract machinery
# so these tools do not require the contract implementation at link time.
add_library_clr(ceefgen_nohost OBJECT ${CEEFILEGEN_SOURCES})
target_compile_definitions(ceefgen_nohost PRIVATE SELF_NO_HOST)
target_precompile_headers(ceefgen_nohost PRIVATE stdafx.h)
