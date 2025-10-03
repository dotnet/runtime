//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements interop between JS and .NET
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
        libInteropJavaScriptNative(exports);

        let commonDeps = ["$DOTNET"];
        const lib = {
            $DOTNET_INTEROP: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        DOTNET_INTEROP.dotnetInternals = dotnetInternals;
                        DOTNET_INTEROP.dotnetInitializeModule(dotnetInternals);
                    }
                },
                dotnetInitializeModule: exports.dotnetInitializeModule,
            },
            $DOTNET_INTEROP__postset: "DOTNET_INTEROP.selfInitialize()",
            $DOTNET_INTEROP__deps: commonDeps,
        };

        for (const exportName of Reflect.ownKeys(exports)) {
            const name = String(exportName);
            if (name === "dotnetInitializeModule") continue;
            lib[name] = exports[name];
        }

        autoAddDeps(lib, "$DOTNET_INTEROP");
        addToLibrary(lib);
    }
    libFactory();
})();
