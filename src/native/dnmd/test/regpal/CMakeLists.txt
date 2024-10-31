set(SOURCES
    ./pal.cpp
)

add_library(regpal STATIC ${SOURCES})

target_link_libraries(regpal PUBLIC nethost dncp::dncp)

if (NOT WIN32)
    target_link_libraries(regpal PUBLIC dncp::winhdrs)
endif()

if (NOT WIN32 AND NOT APPLE)
    target_link_libraries(regpal PUBLIC dl)
endif()

target_include_directories(regpal PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})