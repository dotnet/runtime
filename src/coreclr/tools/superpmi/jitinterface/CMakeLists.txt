project(superpmi-jitinterface)

# The JIT-EE interface wrapper and the JIT host are shared with crossgen2's jitinterface library.
include_directories(../../aot/jitinterface)

# Linked into superpmi (a NativeAOT executable).
add_library_clr(superpmi-jitinterface
    STATIC
    jitinterface.cpp
    threadtime.cpp
    ../../aot/jitinterface/jithost.cpp
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

foreach(target superpmi-jitinterface superpmi-shim-jitinterface)
    set_target_properties(${target} PROPERTIES POSITION_INDEPENDENT_CODE ON)

    if (MSVC)
        # NativeAOT links with the release C runtime; the C++ headers also tell the C runtime from _DEBUG.
        set_target_properties(${target} PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded)
        target_compile_options(${target} PRIVATE /U_DEBUG)
    endif()

    install_static_library(${target} superpmi-lib spmi)
endforeach()
