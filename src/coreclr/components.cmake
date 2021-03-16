# Define all the individually buildable components of the CoreCLR build and their respective targets
add_component(clrjit clrjit_install)
add_component(all_jits)
add_component(runtime)
add_component(paltests paltests_install)
add_component(iltools)

# Define coreclr_all as the fallback component and make every component depend on this component.
set(CMAKE_INSTALL_DEFAULT_COMPONENT_NAME coreclr_misc)
add_component(coreclr_misc)
add_dependencies(clrjit_install coreclr_misc)
add_dependencies(all_jits coreclr_misc)
add_dependencies(runtime coreclr_misc)
add_dependencies(paltests_install coreclr_misc)
add_dependencies(iltools coreclr_misc)

# The runtime build requires the clrjit and iltools builds
add_dependencies(runtime clrjit_install iltools)

# The cross-components build is separate, so we don't need to add a dependency on coreclr_misc
add_component(crosscomponents)
