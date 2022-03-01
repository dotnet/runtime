# Require at least version 2.8.12 of CMake
cmake_minimum_required(VERSION 2.8.12)

set(PROJECT_NAME_STR mono_test_app)
project(${PROJECT_NAME_STR})

file(GLOB PROJECT_SRC_FILES ${PROJECT_SOURCE_DIR}/*.cpp)

include_directories(${CLR_DIR}/src/inc)

add_executable(${PROJECT_NAME_STR} ${PROJECT_SRC_FILES})
target_link_libraries(${PROJECT_NAME_STR} ${CMAKE_DL_LIBS})
target_compile_features(${PROJECT_NAME_STR} PRIVATE cxx_range_for)
target_compile_definitions(${PROJECT_NAME_STR} PRIVATE ENABLE_MONO=1 PLATFORM_STANDALONE=1)
