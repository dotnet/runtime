project(corerun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=corerun.exe)
else(CLR_CMAKE_HOST_WIN32)
    include_directories("${CLR_SRC_NATIVE_DIR}/common")
    include(configure.cmake)
endif(CLR_CMAKE_HOST_WIN32)

add_executable_clr(corerun
  corerun.cpp
  native.rc
)

if(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(corerun
        advapi32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(corerun ${CMAKE_DL_LIBS})

    # Android implements pthread natively
    if(NOT CLR_CMAKE_TARGET_ANDROID)
        target_link_libraries(corerun pthread)
    endif()
endif(CLR_CMAKE_HOST_WIN32)

install_clr(TARGETS corerun DESTINATIONS . COMPONENT runtime)
