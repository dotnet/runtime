project(Common.JavaScript C)

if (NOT STATIC_LIB_DESTINATION)
    message(FATAL_ERROR "please set STATIC_LIB_DESTINATION")
endif()

set(ROLLUP_TS_SOURCES
    "${CLR_SRC_NATIVE_DIR}/package.json"
    "${CLR_SRC_NATIVE_DIR}/tsconfig.json"
    "${CLR_SRC_NATIVE_DIR}/node_modules/.package-lock.json"
    "${CLR_SRC_NATIVE_DIR}/rollup.config.js"
    "${CLR_SRC_NATIVE_DIR}/rollup.config.defines.js"
    "${CLR_SRC_NATIVE_DIR}/rollup.config.plugins.js"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/libBrowserHost.footer.js"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/libSystem.Native.Browser.Utils.footer.js"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/libSystem.Native.Browser.extpost.js"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/libSystem.Native.Browser.footer.js"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.footer.js"

    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/assets.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/cross-module.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/host.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/index.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/host/types.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/assets.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/bootstrap.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/config.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/cross-module.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/dotnet.d.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/dotnet.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/exit.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/host-builder.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/icu.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/index.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/lib-initializers.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/logging.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/polyfills.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/promise-completion-source.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/run.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/loader/types.ts"
    "${CLR_SRC_NATIVE_DIR}/corehost/browserhost/types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/ems-ambient/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/cross-module/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/per-module/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/consts.d.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/ems-ambient.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/emscripten.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/emsdk.d.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/exchange.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/export-api.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/internal.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/node.d.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/public-api.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/Common/JavaScript/types/v8.d.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/cross-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/console-proxy.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/exit.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/symbolicate.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/diagnostics/types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/crypto.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/globalization-locale.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/main.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/native/scheduling.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/cdac.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/cross-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/host.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/memory.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/polyfills.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/runtime-list.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/strings.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Native.Browser/utils/types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/cancelable-promise.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/cross-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/gc-handles.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/http.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/invoke-cs.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/invoke-js.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/lazy.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/managed-exports.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-cs.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-js.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshaled-types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/per-module.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/queue.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/scheduling.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/types.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/utils.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/weak-ref.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/interop/web-socket.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/native/index.ts"
    "${CLR_SRC_NATIVE_DIR}/libs/System.Runtime.InteropServices.JavaScript.Native/types.ts"
)

set(ROLLUP_OUTPUTS
    "${STATIC_LIB_DESTINATION}/dotnet.js"
    "${STATIC_LIB_DESTINATION}/dotnet.js.map"
    "${STATIC_LIB_DESTINATION}/dotnet.d.ts"
    "${STATIC_LIB_DESTINATION}/libBrowserHost.js"
    "${STATIC_LIB_DESTINATION}/libBrowserHost.js.map"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.js"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.js.map"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.Utils.js"
    "${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.Utils.js.map"
    "${STATIC_LIB_DESTINATION}/dotnet.runtime.js"
    "${STATIC_LIB_DESTINATION}/dotnet.runtime.js.map"
    "${STATIC_LIB_DESTINATION}/dotnet.diagnostics.js"
    "${STATIC_LIB_DESTINATION}/dotnet.diagnostics.js.map"
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
    "${CLR_SRC_NATIVE_DIR}/node_modules/.package-lock.json"
)

set(NPM_INSTALL_SOURCES
    "${CLR_SRC_NATIVE_DIR}/package.json"
)

add_dependencies(System.Native.Browser-Rollup System.Native.Browser-NpmInstall)

add_custom_command(
    OUTPUT ${NPM_INSTALL_OUTPUTS}
    COMMAND npm ci
    WORKING_DIRECTORY "${CLR_SRC_NATIVE_DIR}"
    COMMENT "Running 'npm ci' to install npm packages"
    DEPENDS ${NPM_INSTALL_SOURCES}
    VERBATIM
)

add_custom_target(System.Native.Browser-NpmInstall
    DEPENDS ${NPM_INSTALL_OUTPUTS}
)

if (NOT CORERUN_LIBS_ONLY)
    install(FILES ${ROLLUP_OUTPUTS} DESTINATION sharedFramework COMPONENT runtime)
    install(FILES "${CLR_SRC_NATIVE_DIR}/package.json" DESTINATION sharedFramework COMPONENT runtime)
endif()
