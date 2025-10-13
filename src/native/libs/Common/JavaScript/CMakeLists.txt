project(Common.JavaScript C)

set(NATIVE_JAVASCRIPT_SRC ${CMAKE_CURRENT_SOURCE_DIR}/../../..)
if (NOT STATIC_LIB_DESTINATION)
    message(FATAL_ERROR "please set STATIC_LIB_DESTINATION")
endif()

file(GLOB_RECURSE ROLLUP_TS_SOURCES 
    "${NATIVE_JAVASCRIPT_SRC}/libs/Common/JavaScript/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/Common/JavaScript/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Native.Browser/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Native.Browser/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Runtime.InteropServices.JavaScript.Native/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Runtime.InteropServices.JavaScript.Native/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/corehost/browserhost/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/corehost/browserhost/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/package.json"
    "${NATIVE_JAVASCRIPT_SRC}/tsconfig.json"
    "${NATIVE_JAVASCRIPT_SRC}/node_modules/.npm-stamp"
)

set(ROLLUP_OUTPUTS
    "${STATIC_LIB_DESTINATION}/dotnet.js"
    "${STATIC_LIB_DESTINATION}/dotnet.js.map"
    "${STATIC_LIB_DESTINATION}/dotnet.d.ts"
    "${STATIC_LIB_DESTINATION}/libBrowserHost.js"
    "${STATIC_LIB_DESTINATION}/libBrowserHost.js.map"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.js"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.js.map"
    "${STATIC_LIB_DESTINATION}/libSystem.Browser.Utils.js"
    "${STATIC_LIB_DESTINATION}/libSystem.Browser.Utils.js.map"
    "${STATIC_LIB_DESTINATION}/dotnet.runtime.js"
    "${STATIC_LIB_DESTINATION}/dotnet.runtime.js.map"
    "${STATIC_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.js"
    "${STATIC_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.js.map"
)

add_custom_command(
    OUTPUT ${ROLLUP_OUTPUTS}
    COMMAND npm run rollup:cmake -- "Configuration:${CMAKE_BUILD_TYPE},ProductVersion:${CMAKE_PRODUCT_VERSION},ContinuousIntegrationBuild:${CMAKE_CONTINUOUS_INTEGRATION_BUILD},StaticLibDestination:${STATIC_LIB_DESTINATION}"
    WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../.."
    COMMENT "Running 'npm run rollup' to generate JavaScript bundles"
    DEPENDS ${ROLLUP_TS_SOURCES}
    VERBATIM
)

add_custom_target(System.Native.Browser-Rollup
    DEPENDS ${ROLLUP_OUTPUTS}
)

set(NPM_INSTALL_OUTPUTS
    "${NATIVE_JAVASCRIPT_SRC}/node_modules/.package-lock.json"
)

set(NPM_INSTALL_SOURCES
    "${NATIVE_JAVASCRIPT_SRC}/package.json"
)

add_dependencies(System.Native.Browser-Rollup System.Native.Browser-NpmInstall)

add_custom_command(
    OUTPUT ${NPM_INSTALL_OUTPUTS}
    COMMAND npm ci
    WORKING_DIRECTORY "${NATIVE_JAVASCRIPT_SRC}"
    COMMENT "Running 'npm ci' to install npm packages"
    DEPENDS ${NPM_INSTALL_SOURCES}
    VERBATIM
)

add_custom_target(System.Native.Browser-NpmInstall
    DEPENDS ${NPM_INSTALL_OUTPUTS}
)

install(FILES ${ROLLUP_OUTPUTS} DESTINATION sharedFramework COMPONENT runtime)