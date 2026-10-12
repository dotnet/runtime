# The IJW (C++/CLI) profilee is Windows-only. This directory is picked up
# unconditionally by the recursive add_subdirectory globber in
# src/tests/CMakeLists.txt, so it must guard itself against non-Windows targets:
# IJW.cmake only defines add_ijw_msbuild_project_properties on Windows hosts.
if (CLR_CMAKE_TARGET_WIN32)
    project (IjwProfileeDll)
    include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

    include_directories( ${INC_PLATFORM_DIR} )
    set(SOURCES IjwProfileeDll.cpp)

    # add the shared library
    add_library (IjwProfileeDll SHARED ${SOURCES})
    target_link_libraries(IjwProfileeDll PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
    add_ijw_msbuild_project_properties(IjwProfileeDll ijwhost)

    # add the install targets
    install (TARGETS IjwProfileeDll DESTINATION bin)
endif()
