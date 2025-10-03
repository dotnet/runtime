project(Common.JavaScript C)

set(NATIVE_JAVASCRIPT_SRC ${CMAKE_CURRENT_SOURCE_DIR}/../../../)
set(NATIVE_JAVASCRIPT_BIN ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_TYPE}-wasm/)

file(GLOB_RECURSE ROLLUP_TS_SOURCES 
    "${NATIVE_JAVASCRIPT_SRC}/libs/Common/JavaScript/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/Common/JavaScript/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Native.Browser/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Native.Browser/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Runtime.InteropServices.JavaScript.Native/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/libs/System.Runtime.InteropServices.JavaScript.Native/*.js"
    "${NATIVE_JAVASCRIPT_SRC}/corehost/browserhost/*.ts"
    "${NATIVE_JAVASCRIPT_SRC}/corehost/browserhost/*.js"
    "${NATIVE_JAVASCRIPT_SRC}package.json"
    "${NATIVE_JAVASCRIPT_SRC}tsconfig.json"
    "${NATIVE_JAVASCRIPT_SRC}node_modules/.npm-stamp"
)

set(ROLLUP_OUTPUTS
    "${NATIVE_JAVASCRIPT_BIN}/dotnet.js"
    "${NATIVE_JAVASCRIPT_BIN}/dotnet.js.map"
    "${NATIVE_JAVASCRIPT_BIN}/dotnet.d.ts"
    "${NATIVE_JAVASCRIPT_BIN}/libBrowserHost.js"
    "${NATIVE_JAVASCRIPT_BIN}/libBrowserHost.js.map"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Native.Browser.js"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Native.Browser.js.map"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Browser.Utils.js"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Browser.Utils.js.map"
    "${NATIVE_JAVASCRIPT_BIN}/dotnet.runtime.js"
    "${NATIVE_JAVASCRIPT_BIN}/dotnet.runtime.js.map"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Runtime.InteropServices.JavaScript.Native.js"
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Runtime.InteropServices.JavaScript.Native.js.map"
)

add_custom_command(
    OUTPUT ${ROLLUP_OUTPUTS}
    COMMAND npm run rollup:cmake -- "Configuration:${CMAKE_BUILD_TYPE},ProductVersion:${CMAKE_PRODUCT_VERSION},ContinuousIntegrationBuild:${CMAKE_CONTINUOUS_INTEGRATION_BUILD},NetCoreAppCurrentVersion:${CMAKE_NET_CORE_APP_CURRENT_VERSION}"
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
