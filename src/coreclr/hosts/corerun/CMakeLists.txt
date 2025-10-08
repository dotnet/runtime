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

target_link_libraries(corerun PRIVATE minipal)

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
    # Enable CET-compatibility
    if (CLR_CMAKE_HOST_ARCH_AMD64)
        target_link_options(corerun PRIVATE "/CETCOMPAT")
    endif()
else(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(corerun PRIVATE ${CMAKE_DL_LIBS})
    # Required to expose symbols for global symbol discovery
    target_link_libraries(corerun PRIVATE -rdynamic)

    # Android implements pthread natively
    if(NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_ARCH_WASM)
        target_link_libraries(corerun PRIVATE pthread)
    endif()
    # Static linking
    if (CLR_CMAKE_TARGET_ARCH_WASM)
        target_sources(corerun PRIVATE corerun.wasm.cpp)
        target_link_libraries(corerun PRIVATE
        coreclr_static
        System.Native-Static
        System.Native.TimeZoneData)
        # linker options for NodeJs, link in JavaScript helper, access to local filesystem
        if (CLR_CMAKE_TARGET_BROWSER)
            target_compile_options(corerun PRIVATE 
                -fwasm-exceptions
                -msimd128
                )
            target_link_libraries(corerun PRIVATE 
                System.Native.Browser-Static)
            set(JS_SYSTEM_NATIVE_BROWSER
                "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js")
            set(JS_SYSTEM_BROWSER_UTILS
                "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Browser.Utils.js")
            set(JS_CORE_RUN_PRE
                "${CMAKE_CURRENT_SOURCE_DIR}/libCorerun.pre.js")
            set_target_properties(corerun PROPERTIES
                LINK_DEPENDS "${JS_CORE_RUN_PRE};${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};"
                LINK_FLAGS "--pre-js ${JS_CORE_RUN_PRE} --js-library ${JS_SYSTEM_NATIVE_BROWSER} --js-library ${JS_SYSTEM_BROWSER_UTILS}"
                RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}")
            target_link_options(corerun PRIVATE
                -fwasm-exceptions
                -sEXIT_RUNTIME=1
                -sINITIAL_MEMORY=134217728
                -sENVIRONMENT=node,shell
                -sSTACK_SIZE=5MB
                -lnoderawfs.js
                -lnodefs.js
                -Wl,-error-limit=0)
        endif()
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
