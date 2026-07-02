set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_library_clr(cdacgc STATIC
    gc.cpp
    thread.cpp
    loader.cpp
    loaderheaps.cpp
    handles.cpp
    jit.cpp
    statics.cpp
    syncblock.cpp
    stresslog.cpp
    interop.cpp
    stackscan.cpp
)

target_include_directories(cdacgc PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(cdacgc PUBLIC cdacdata)
