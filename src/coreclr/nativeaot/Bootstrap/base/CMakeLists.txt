project(bootstrapper)

set(SOURCES
    ../main.cpp
)

add_library(bootstrapper STATIC ${SOURCES})

install_static_library(bootstrapper aotsdk nativeaot)

if (CLR_CMAKE_TARGET_WIN32)
  add_library(bootstrapper.GuardCF STATIC ${SOURCES})
  install_static_library(bootstrapper.GuardCF aotsdk nativeaot)
  target_compile_options(bootstrapper.GuardCF PRIVATE $<$<OR:$<COMPILE_LANGUAGE:C>,$<COMPILE_LANGUAGE:CXX>>:/guard:cf>)
else()
  add_library(stdc++compat STATIC ../stdcppshim.cpp)
  install_static_library(stdc++compat aotsdk nativeaot)
endif()
