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

# WASM-TODO *.ts files instead
set(ROLLUP_TS_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/../../corehost/browserhost/loader/dotnet.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../corehost/browserhost/loader/dotnet.d.ts"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../corehost/browserhost/libBrowserHost.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/libSystem.Native.Browser.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/libSystem.Native.Browser.extpost.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/./libSystem.Native.Browser.extpost.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/./libSystem.Native.Browser.js"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../package.json"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../tsconfig.json"
)

# WASM-TODO *.js.map files
set(ROLLUP_OUTPUTS
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js"
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.d.ts"
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/libBrowserHost.js"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/dotnet.runtime.js"
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js"
)

set(ROLLUP_STAMP
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/.rollup.stamp"
)

if(NOT PRODUCT_VERSION_JS)
    # WASM-TODO get dotnet ProductVersion from MSBuild
    set(PRODUCT_VERSION_JS "10.0.0-dev")
endif()
if(NOT CI_BUILD_JS)
    # WASM-TODO get ContinuousIntegrationBuild from MSBuild
    set(CI_BUILD_JS "false")
endif()

add_custom_command(
    OUTPUT ${ROLLUP_STAMP}
    BYPRODUCTS ${ROLLUP_OUTPUTS}
    COMMAND npm run rollup:stub -- ${CMAKE_BUILD_TYPE} ${PRODUCT_VERSION_JS} ${CI_BUILD_JS}
    WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../.."
    COMMENT "Running 'npm run rollup' to generate JavaScript bundles"
    DEPENDS ${ROLLUP_TS_SOURCES}
    VERBATIM
)

# WASM-TODO fixme: make this incremental
add_custom_target(System.Native.Browser-Rollup
    DEPENDS ${ROLLUP_STAMP}
)

add_dependencies(System.Native.Browser-Static System.Native.Browser-Rollup)

set(NPM_INSTALL_OUTPUTS
    "${CMAKE_CURRENT_SOURCE_DIR}/../../node_modules/.package-lock.json"
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
