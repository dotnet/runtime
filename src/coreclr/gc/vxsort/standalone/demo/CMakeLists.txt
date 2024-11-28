set(TARGET_NAME ${CMAKE_PROJECT_NAME}_demo)
set(CMAKE_BUILD_TYPE Release)

include_directories("../..")

set(demo_SOURCES
    demo.cpp)

add_executable(${CMAKE_PROJECT_NAME}_demo ${demo_SOURCES})

target_link_libraries(${TARGET_NAME} -L..)
target_link_libraries(${TARGET_NAME} gc_vxsort)
