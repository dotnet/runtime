cmake_minimum_required (VERSION 3.14)

project(CorProfiler)

if(NOT WIN32)
    set(BASE_SOURCES )
    add_compile_options(-Wno-invalid-noreturn -Wno-pragma-pack -Wno-int-to-pointer-cast -fPIC -fms-extensions -DBIT64 -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DHOST_64BIT -std=c++11)
    add_link_options(--no-undefined -pthread)

    include_directories($ENV{CORECLR_PATH}/src/pal/inc/rt $ENV{CORECLR_PATH}/src/pal/inc $ENV{CORECLR_PATH}/src/inc)
endif(NOT WIN32)

if (WIN32)
    set(BASE_SOURCES src/CorProfiler.def)
endif(WIN32)

include_directories($CORECLR_BIN/inc $ENV{CORECLR_PATH}/src/pal/prebuilt/inc)

set(SOURCES ${BASE_SOURCES} src/ClassFactory.cpp src/CorProfiler.cpp src/dllmain.cpp src/sampler.cpp $ENV{CORECLR_PATH}/src/pal/prebuilt/idl/corprof_i.cpp)

add_library(CorProfiler SHARED ${SOURCES})