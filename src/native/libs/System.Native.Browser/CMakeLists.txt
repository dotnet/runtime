project(System.Native.Browser C)

set(BROWSER_SOURCES)

if (NOT GEN_PINVOKE)
    set (BROWSER_SOURCES ${BROWSER_SOURCES} entrypoints.c)
endif()

add_library(System.Native.Browser-Static
    STATIC
    ${BROWSER_SOURCES}
)
set_target_properties(System.Native.Browser-Static PROPERTIES OUTPUT_NAME System.Native.Browser CLEAN_DIRECT_OUTPUT 1)
install(TARGETS System.Native.Browser-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)


file(GLOB_RECURSE ROLLUP_TS_SOURCES 
    "${CMAKE_CURRENT_SOURCE_DIR}/*.ts"
    "${CMAKE_CURRENT_SOURCE_DIR}/*.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../System.Runtime.InteropServices.JavaScript.Native/*.ts"
    "${CMAKE_CURRENT_SOURCE_DIR}/../System.Runtime.InteropServices.JavaScript.Native/*.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../Common/JavaScript/*.ts"
    "${CMAKE_CURRENT_SOURCE_DIR}/../Common/JavaScript/*.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../corehost/browserhost/*.ts"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../corehost/browserhost/*.js"
    "${CMAKE_CURRENT_SOURCE_DIR}../../package.json"
    "${CMAKE_CURRENT_SOURCE_DIR}../../tsconfig.json"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.npm-stamp"
)

set(ROLLUP_OUTPUTS
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js"
    # TODOWASM "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js.map"
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.d.ts"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js"
    # TODOWASM "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js.map"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/dotnet.runtime.js"
    # TODOWASM "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/dotnet.runtime.js.map"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js"
    # TODOWASM "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js.map"
)
if(NOT PRODUCT_VERSION_JS)
    # TODOWASM get dotnet ProductVersion from MSBuild
    set(PRODUCT_VERSION_JS "10.0.0-dev")
endif()
if(NOT CI_BUILD_JS)
    # TODOWASM get ContinuousIntegrationBuild from MSBuild
    set(CI_BUILD_JS "false")
endif()

add_custom_command(
    OUTPUT ${ROLLUP_OUTPUTS}
    COMMAND npm run rollup ${CMAKE_BUILD_TYPE} ${PRODUCT_VERSION_JS} ${CI_BUILD_JS}
    WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../.."
    COMMENT "Running 'npm run rollup' to generate JavaScript bundles"
    DEPENDS ${ROLLUP_TS_SOURCES}
    VERBATIM
)

add_custom_target(System.Native.Browser-Rollup
    DEPENDS ${ROLLUP_OUTPUTS}
)

add_dependencies(System.Native.Browser-Static System.Native.Browser-Rollup)

set(NPM_INSTALL_OUTPUTS
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.bin/rollup"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.bin/terser"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.bin/tsc"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.bin/eslint"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.npm-stamp"
)

set(NPM_INSTALL_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/../../package.json"
)

add_dependencies(System.Native.Browser-Rollup System.Native.Browser-NpmInstall)

add_custom_command(
    OUTPUT ${NPM_INSTALL_OUTPUTS}
    COMMAND npm ci
    WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../.."
    COMMENT "Running 'npm ci' to install npm packages"
    DEPENDS ${NPM_INSTALL_SOURCES}
    VERBATIM
)

add_custom_target(System.Native.Browser-NpmInstall
    DEPENDS ${NPM_INSTALL_OUTPUTS}
)
