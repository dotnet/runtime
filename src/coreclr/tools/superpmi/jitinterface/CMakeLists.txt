project(superpmi-jitinterface)

# The JIT-EE interface wrapper and the JIT host are shared with crossgen2's jitinterface library.
include_directories(../../aot/jitinterface)

set(SUPERPMI_JITINTERFACE_SOURCES
    jitinterface.cpp
    threadtime.cpp
    ../../aot/jitinterface/jithost.cpp
)

# Loaded by superpmi when it is published as a single-file app, which gets a copy of it next to it.
add_library_clr(superpmi-jitinterface
    SHARED
    ${SUPERPMI_JITINTERFACE_SOURCES}
)

if (CLR_CMAKE_HOST_WIN32)
    target_link_libraries(superpmi-jitinterface
        PRIVATE
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        ${STATIC_MT_CPP_LIB}
        kernel32.lib
    )
endif()

install_clr(TARGETS superpmi-jitinterface DESTINATIONS superpmi-lib COMPONENT spmi)

# Linked into superpmi when it is published with NativeAOT.
add_library_clr(superpmi-jitinterface-static
    STATIC
    ${SUPERPMI_JITINTERFACE_SOURCES}
)

# Linked into the managed shims (NativeAOT shared libraries), which the runtime loads as its JIT.
add_library_clr(superpmi-shim-jitinterface
    STATIC
    shim.cpp
)

if (MSVC)
    # The EE's exceptions are structured exceptions, which the shim catches with catch (...).
    target_compile_options(superpmi-shim-jitinterface PRIVATE /EHa)
endif()

foreach(target superpmi-jitinterface-static superpmi-shim-jitinterface)
    set_target_properties(${target} PROPERTIES POSITION_INDEPENDENT_CODE ON)

    if (MSVC)
        # NativeAOT links with the release C runtime; the C++ headers also tell the C runtime from _DEBUG.
        set_target_properties(${target} PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded)
        target_compile_options(${target} PRIVATE /U_DEBUG)
    endif()

    install_static_library(${target} superpmi-lib spmi)
endforeach()
