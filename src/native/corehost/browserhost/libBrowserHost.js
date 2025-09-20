//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the corehost.
 */

(function (exports) {
    function browserHostLibLibFactory() {
        const BrowserHostLib = {
            $BROWSER_HOST: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        BROWSER_HOST.dotnetInternals = dotnetInternals;

                        const HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
                        const HOST_PROPERTY_ENTRY_ASSEMBLY_NAME = "ENTRY_ASSEMBLY_NAME";
                        const HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";
                        const HOST_PROPERTY_APP_PATHS = "APP_PATHS";

                        const config = dotnetInternals.config;
                        const assemblyPaths = config.resources.assembly.map(a => a.virtualPath);
                        const coreAssemblyPaths = config.resources.coreAssembly.map(a => a.virtualPath);
                        config.environmentVariables[HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES] = [...coreAssemblyPaths, assemblyPaths].join(":");
                        config.environmentVariables[HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES] = config.virtualWorkingDirectory;
                        config.environmentVariables[HOST_PROPERTY_APP_PATHS] = config.virtualWorkingDirectory;
                        config.environmentVariables[HOST_PROPERTY_ENTRY_ASSEMBLY_NAME] = config.mainAssemblyName;
                    }
                },
            },
            "$BROWSER_HOST__deps": ["$DOTNET", "browserHostInitializeCoreCLR", "browserHostExecuteAssembly", "browserHostExternalAssemblyProbe"],
            "$BROWSER_HOST__postset": "DOTNET.selfInitialize();",
        };
        autoAddDeps(BrowserHostLib, "$BROWSER_HOST");
        addToLibrary(BrowserHostLib);
    }
    browserHostLibLibFactory();
    return exports;
})({});
