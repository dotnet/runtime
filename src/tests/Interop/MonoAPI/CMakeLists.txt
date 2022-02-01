cmake_minimum_required(VERSION 3.13.0)
project (MonoEmbeddingApiTest)
include_directories(${INC_PLATFORM_DIR})


# FIXME: rename DISABLE_LIBS to something more descriptive and negate, for example MONOAPI_NO_INSTALL
set (DISABLE_LIBS 1)
add_subdirectory(${CLR_SRC_NATIVE_DIR}/include/mono-2.0 mono-2.0)

# we need eglib objects

set(COMPILE_APITEST 1)

include(${PROJECT_SOURCE_DIR}/../../../mono/mono/eglib/CMakeLists.txt)

addprefix(eglib_apitest_sources "${PROJECT_SOURCE_DIR}/../../../mono/mono/eglib/" "${eglib_platform_sources};${eglib_common_sources}")

add_library(eglib_apitest_objects OBJECT "${eglib_apitest_sources}")
# eglib/gutf8.c:80:2: error: unannotated fall-through between switch labels [-Werror,-Wimplicit-fallthrough]
target_compile_options(eglib_apitest_objects PRIVATE "-Wno-implicit-fallthrough")

# add the library
add_library (mono-embedding-api-test SHARED mono-embedding-api-test.cpp)
target_link_libraries(mono-embedding-api-test monoapi eglib_apitest_objects ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS mono-embedding-api-test DESTINATION bin)

# from artifacts/tests/coreclr/obj/OSX.x64.Release/Native/Interop/MonoAPI/
# for config.h
include_directories(${PROJECT_BINARY_DIR}/../../../../../../../obj/mono/OSX.x64.Release/)

# from src/tests/Interop/MonoAPI/
include_directories(${PROJECT_SOURCE_DIR}/../../../mono/mono/eglib/
  ${PROJECT_SOURCE_DIR}/../../../mono/)

