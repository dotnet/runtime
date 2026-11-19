include(configure.cmake)
include_directories(${CMAKE_CURRENT_BINARY_DIR})

add_subdirectory(superpmi)
add_subdirectory(mcs)
add_subdirectory(superpmi-shim-collector)
add_subdirectory(superpmi-shim-counter)
add_subdirectory(superpmi-shim-simple)

set_target_properties(superpmi mcs superpmi-shim-collector superpmi-shim-counter superpmi-shim-simple
  PROPERTIES IGNORE_FEATURE_INTERPRETER_DIRECT_CALLS TRUE)
