set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_library_clr(cdacdata STATIC
    datadescriptor.cpp
    target.cpp
)

target_include_directories(cdacdata PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(cdacdata PUBLIC cdacjson)
