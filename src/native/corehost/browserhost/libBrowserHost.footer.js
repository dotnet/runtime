//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the corehost and JS related to runtime hosting.
 */

/* eslint-disable no-undef */
/* eslint-disable space-before-function-paren */
(function () {
    function libFactory() {
        // this executes the function at link time in order to capture exports
        // this is what Emscripten does for linking JS libraries
        // https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#javascript-limits-in-library-files
        // it would execute the code at link time and call .toString() on functions to move it to the final output
        // this process would loose any closure references, unless they are passed to `__deps` and also explicitly given to the linker
        // JS name mangling and minification also applies, see src\native\rollup.config.defines.js and `reserved` there
        const exports = {};
        libBrowserHost(exports);

        let commonDeps = ["$libBrowserHostFn", "$DOTNET", "$DOTNET_INTEROP", "$ENV"];
        const lib = {
            $BROWSER_HOST: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        BROWSER_HOST.dotnetInternals = dotnetInternals;

                        const exports = {};
                        libBrowserHostFn(exports);
                        exports.dotnetInitializeModule(dotnetInternals);
                        BROWSER_HOST.assignExports(exports, BROWSER_HOST);

                        const HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
                        const HOST_PROPERTY_ENTRY_ASSEMBLY_NAME = "ENTRY_ASSEMBLY_NAME";
                        const HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";
                        const HOST_PROPERTY_APP_PATHS = "APP_PATHS";

                        const config = dotnetInternals[2/*InternalExchangeIndex.LoaderConfig*/];
                        if (!config.resources.assembly ||
                            !config.resources.coreAssembly ||
                            config.resources.coreAssembly.length === 0 ||
                            !config.mainAssemblyName ||
                            !config.virtualWorkingDirectory ||
                            !config.environmentVariables) {
                            throw new Error("Invalid runtime config, cannot initialize the runtime.");
                        }
                        // Build TPA, APP_PATHS, and NATIVE_DLL_SEARCH_DIRECTORIES values and store them in config
                        // These will be passed as arguments to BrowserHost_InitializeCoreCLR instead of using ENV variables
                        const assemblyPaths = config.resources.assembly.map(a => a.virtualPath);
                        const coreAssemblyPaths = config.resources.coreAssembly.map(a => a.virtualPath);
                        config.environmentVariables[HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES] = [...coreAssemblyPaths, ...assemblyPaths].join(":");
                        config.environmentVariables[HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES] = config.virtualWorkingDirectory;
                        config.environmentVariables[HOST_PROPERTY_APP_PATHS] = config.virtualWorkingDirectory;
                        ENV[HOST_PROPERTY_ENTRY_ASSEMBLY_NAME] = config.environmentVariables[HOST_PROPERTY_ENTRY_ASSEMBLY_NAME] = config.mainAssemblyName;

                        // WASM-TODO: remove once globalization is loaded via ICU
                        ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
                    }
                },
            },
            $libBrowserHostFn: libBrowserHost,
            $BROWSER_HOST__postset: "BROWSER_HOST.selfInitialize()",
            $BROWSER_HOST__deps: commonDeps,
        };

        let assignExportsBuilder = "";
        for (const exportName of Reflect.ownKeys(exports)) {
            const name = String(exportName);
            if (name === "dotnetInitializeModule") continue;
            lib[name] = () => "dummy";
            assignExportsBuilder += `_${String(name)} = exports.${String(name)};\n`;
        }
        lib.$BROWSER_HOST.assignExports = new Function("exports", assignExportsBuilder);

        autoAddDeps(lib, "$BROWSER_HOST");
        addToLibrary(lib);
    }
    libFactory();
})();
