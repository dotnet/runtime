include(../../md_dbi.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDDATASOURCE_SOURCES)

if (WIN32)
    list(APPEND MDDATASOURCE_SOURCES ${MDDATASOURCE_HEADERS})
endif (WIN32)

add_library_clr(mddatasource_dbi STATIC ${MDDATASOURCE_SOURCES})
