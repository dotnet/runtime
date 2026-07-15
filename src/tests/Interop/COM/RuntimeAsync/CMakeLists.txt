include_directories( ${INC_PLATFORM_DIR} )
include_directories("../ServerContracts" )
include_directories("../NativeServer" )
include_directories("../NativeClients")
set(SOURCES
    RuntimeAsyncNative.cpp)

# add the executable
add_library (RuntimeAsyncNative SHARED ${SOURCES})
target_link_libraries(RuntimeAsyncNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
