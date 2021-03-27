# Define all the individually buildable components of the CoreCLR build and their respective targets
add_component(jit)
add_component(alljits)
add_component(runtime)
add_component(paltests paltests_install)
add_component(iltools)

# Define coreclr_all as the fallback component and make every component depend on this component.
# iltools should be a minimal subset, so don't add a dependency on coreclr_misc
set(CMAKE_INSTALL_DEFAULT_COMPONENT_NAME coreclr_misc)
add_component(coreclr_misc)
add_dependencies(jit coreclr_misc)
add_dependencies(alljits coreclr_misc)
add_dependencies(runtime coreclr_misc)
add_dependencies(paltests_install coreclr_misc)

# The runtime build requires the clrjit and iltools builds
add_dependencies(runtime jit iltools)

# The cross-components build is separate, so we don't need to add a dependency on coreclr_misc
add_component(crosscomponents)
