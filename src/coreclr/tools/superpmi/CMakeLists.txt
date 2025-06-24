# SuperPMI is development-only toolset.
# Enable C++ 17 to get more handy stuffs from STL.
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_subdirectory(superpmi)
add_subdirectory(mcs)
add_subdirectory(superpmi-shim-collector)
add_subdirectory(superpmi-shim-counter)
add_subdirectory(superpmi-shim-simple)
