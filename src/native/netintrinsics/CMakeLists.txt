set(SOURCES math.c)

include(configure.cmake)

add_library(netintrinsics OBJECT ${SOURCES})

target_include_directories(netintrinsics PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(netintrinsics PRIVATE ${CMAKE_CURRENT_BINARY_DIR})