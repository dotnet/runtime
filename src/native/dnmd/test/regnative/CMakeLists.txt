set(SOURCES
  perf.cpp
  unit.cpp
)

if(NOT MSVC)
  # Define for DNCP
  add_compile_definitions(
      DNCP_TYPEDEFS
      DNCP_WINHDRS
  )
endif()

set(CMAKE_INSTALL_PREFIX ./)
set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
set(CMAKE_INSTALL_NAME_DIR "@rpath")
set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(CMAKE_INSTALL_RPATH "@loader_path")

add_library(regnative
  SHARED
  ${SOURCES}
)

target_link_libraries(regnative
  dnmd_interfaces
  dncp)

if(NOT MSVC)
  target_link_libraries(regnative dncp_winhdrs)
endif()

install(TARGETS regnative
  DESTINATION bin)