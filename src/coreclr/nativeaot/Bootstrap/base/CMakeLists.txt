project(bootstrapper)

set(SOURCES
    ../main.cpp
)

add_library(bootstrapper OBJECT ${SOURCES})
set_target_properties(bootstrapper PROPERTIES OUTPUT_NAME bootstrapper)

install_bootstrapper_object(bootstrapper aotsdk)

if (CLR_CMAKE_HOST_UNIX)
  add_library(stdc++compat STATIC ../stdcppshim.cpp)
  install_static_library(stdc++compat aotsdk nativeaot)
endif()
