set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

include_directories(${CLR_SRC_NATIVE_DIR})

if(NOT TARGET dn-containers)
    add_subdirectory(${CLR_SRC_NATIVE_DIR}/containers containers-library)
endif()

add_executable(NativeContainerTestsDriver
    ../common/driver.c
    ../common/test.c
    native-container-tests-driver.c
    dn-fwd-list-tests.c
    dn-list-tests.c
    dn-queue-tests.c
    dn-umap-tests.c
    dn-vector-ptr-tests.c
    dn-vector-tests.c)

target_include_directories(NativeContainerTestsDriver PRIVATE ../common)
target_link_libraries(NativeContainerTestsDriver PRIVATE dn-containers)
install(TARGETS NativeContainerTestsDriver DESTINATION bin)
