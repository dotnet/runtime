project(unixcorerun)

include_directories("${CLR_SRC_NATIVE_DIR}/common")
include(configure.cmake)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

_add_executable(corerun corerun.cpp)

target_link_libraries(corerun ${CMAKE_DL_LIBS})

# Android implements pthread natively
if(NOT CLR_CMAKE_TARGET_ANDROID)
  target_link_libraries(corerun pthread)
endif()

install_clr(TARGETS corerun)
