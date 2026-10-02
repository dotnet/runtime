# Include the dnmd project into the CoreCLR build
include(FetchContent)
FetchContent_Declare(
  dnmd
  SOURCE_DIR "${CLR_SRC_NATIVE_DIR}/dnmd"
)

set(DNMD_BUILD_TESTS OFF)
set(DNMD_INSTALL OFF)
set(DNMD_ENABLE_LOADED_MODULES_CACHE ON CACHE BOOL "Enable DNMD cross-scope TypeRef resolution")
set(DNMD_ENABLE_INTERNAL_INTERFACES ON CACHE BOOL "Enable CoreCLR-specific DNMD metadata interfaces")
FetchContent_MakeAvailable(dnmd)

set_property(DIRECTORY ${CLR_SRC_NATIVE_DIR}/dnmd PROPERTY CLR_CONTROL_FLOW_GUARD ON)

# Install dnmd for usage by the cdac.
install_static_library(dnmd cdac cdac)
install_static_library(dnmd_interfaces_static cdac cdac)
install(TARGETS dnmd_interfaces DESTINATION cdac COMPONENT cdac)
add_dependencies(cdac dnmd_interfaces)
