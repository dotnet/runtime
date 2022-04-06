project(bootstrapperdll)

add_definitions(-DCORERT_DLL)

set(SOURCES
    ../main.cpp
)

add_library(bootstrapperdll STATIC ${SOURCES})

install_static_library(bootstrapperdll aotsdk nativeaot)

if (CLR_CMAKE_TARGET_WIN32)
  add_library(bootstrapperdll.GuardCF STATIC ${SOURCES})
  install_static_library(bootstrapperdll.GuardCF aotsdk nativeaot)
  target_compile_options(bootstrapperdll.GuardCF PRIVATE $<$<OR:$<COMPILE_LANGUAGE:C>,$<COMPILE_LANGUAGE:CXX>>:/guard:cf>)
endif()
