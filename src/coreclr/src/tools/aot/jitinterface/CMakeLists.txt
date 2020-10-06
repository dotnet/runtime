project(jitinterface)

set(NATIVE_SOURCES
    jithost.cpp
    jitinterface.cpp
    jitwrapper.cpp
    corinfoexception.cpp
)

add_library_clr(jitinterface_${ARCH_HOST_NAME}
    SHARED
    ${NATIVE_SOURCES}
)

install_clr(TARGETS jitinterface_${ARCH_HOST_NAME})
