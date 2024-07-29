project(jitinterface)

set(NATIVE_SOURCES
    jithost.cpp
    jitinterface.cpp
    jitwrapper.cpp
    corinfoexception.cpp

    ${CLR_SRC_NATIVE_DIR}/minipal/cpufeatures.c
)

if(CLR_CMAKE_TARGET_WIN32)
  set(JITINTERFACE_RESOURCES Native.rc)
else()
  include(${CLR_SRC_NATIVE_DIR}/minipal/configure.cmake)
  include_directories(${CMAKE_CURRENT_BINARY_DIR})
endif()

add_library_clr(jitinterface_${ARCH_HOST_NAME}
    SHARED
    ${NATIVE_SOURCES}
    ${JITINTERFACE_RESOURCES}
)

install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT jit)
install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT alljits)
