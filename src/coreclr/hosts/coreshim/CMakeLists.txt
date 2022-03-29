project (CoreShim)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CoreShim_SOURCES
    CoreShim.cpp
    ComActivation.cpp
    Exports.def)

add_library_clr(CoreShim
    SHARED
    ${CoreShim_SOURCES}
)

target_link_libraries(CoreShim
    utilcodestaticnohost
    advapi32.lib
    oleaut32.lib
    uuid.lib
    user32.lib
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
)

install_clr(TARGETS CoreShim DESTINATIONS . COMPONENT hosts)
