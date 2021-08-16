project(jitinterface)

set(NATIVE_SOURCES
    jithost.cpp
    jitinterface.cpp
    jitwrapper.cpp
    corinfoexception.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
  set(JITINTERFACE_RESOURCES Native.rc)
endif(CLR_CMAKE_TARGET_WIN32)

add_library_clr(jitinterface_${ARCH_HOST_NAME}
    SHARED
    ${NATIVE_SOURCES}
    ${JITINTERFACE_RESOURCES}
)

install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT jit)
install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT alljits)
