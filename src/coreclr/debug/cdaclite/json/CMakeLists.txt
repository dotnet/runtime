set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_library_clr(cdacjson STATIC json.cpp)

target_include_directories(cdacjson PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
