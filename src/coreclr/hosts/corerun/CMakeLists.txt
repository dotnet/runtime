project(corerun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=corerun.exe)
else(CLR_CMAKE_HOST_WIN32)
    include(configure.cmake)
endif(CLR_CMAKE_HOST_WIN32)

#Required to expose symbols for global symbol discovery.
set(CLR_CMAKE_KEEP_NATIVE_SYMBOLS TRUE)

add_executable_clr(corerun
  corerun.cpp
  dotenv.cpp
  native.rc
)

if(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(corerun
        PRIVATE
        advapi32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(corerun PRIVATE ${CMAKE_DL_LIBS})
    # Required to expose symbols for global symbol discovery
    target_link_libraries(corerun PRIVATE -rdynamic)

    # Android implements pthread natively
    if(NOT CLR_CMAKE_TARGET_ANDROID)
        target_link_libraries(corerun PRIVATE pthread)
    endif()
endif(CLR_CMAKE_HOST_WIN32)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(corerun "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()

install_clr(TARGETS corerun DESTINATIONS . COMPONENT hosts)

# If there's a dynamic ASAN runtime, then install it in the directory where we put our executable.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    install(FILES ${ASAN_RUNTIME} DESTINATION .)
endif()
