project(jitinterface)

set(NATIVE_SOURCES
    jithost.cpp
    jitinterface.cpp
    jitwrapper.cpp
    corinfoexception.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
  set(JITINTERFACE_RESOURCES Native.rc)
endif()

if(CLR_CMAKE_TARGET_BROWSER)
  set(LIBRARY_TYPE STATIC)
else()
  set(LIBRARY_TYPE SHARED)
endif()

add_library_clr(jitinterface_${ARCH_HOST_NAME}
    ${LIBRARY_TYPE}
    ${NATIVE_SOURCES}
    ${JITINTERFACE_RESOURCES}
)

target_link_libraries(jitinterface_${ARCH_HOST_NAME} PRIVATE minipal)

install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT jit)
install_clr(TARGETS jitinterface_${ARCH_HOST_NAME} DESTINATIONS . COMPONENT alljits)
