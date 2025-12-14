//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements utilities and a public JS API related to memory and runtime hosting.
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
        libBrowserUtils(exports);

        let commonDeps = ["$libBrowserUtilsFn", "$DOTNET"];
        const lib = {
            $BROWSER_UTILS: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        BROWSER_UTILS.dotnetInternals = dotnetInternals;

                        const exports = {};
                        libBrowserUtilsFn(exports);
                        exports.dotnetInitializeModule(dotnetInternals);
                    }
                },
            },
            $libBrowserUtilsFn: libBrowserUtils,
            $BROWSER_UTILS__postset: "BROWSER_UTILS.selfInitialize()",
            $BROWSER_UTILS__deps: commonDeps,
        };

        // keep in sync with `reserved`+`keep_fnames` in src\native\rollup.config.defines.js
        for (const exportName of Reflect.ownKeys(exports.cross)) {
            const name = String(exportName);
            if (name === "dotnetInternals") continue;
            if (name === "Module") continue;
            const emName = "$" + name;
            lib[emName] = exports.cross[exportName];
            commonDeps.push(emName);
        }

        autoAddDeps(lib, "$BROWSER_UTILS");
        addToLibrary(lib);
    }
    libFactory();
})();
