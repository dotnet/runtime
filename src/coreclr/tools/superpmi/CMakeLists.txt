if (CLR_CMAKE_HOST_UNIX)
    add_compile_options(-Wno-format)
endif(CLR_CMAKE_HOST_UNIX)

add_subdirectory(superpmi)
add_subdirectory(mcs)
add_subdirectory(superpmi-shim-collector)
add_subdirectory(superpmi-shim-counter)
add_subdirectory(superpmi-shim-simple)
