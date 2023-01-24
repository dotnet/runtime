const path = require('path');

module.exports = (env) => {
    const wasmAppDir = path.resolve(env.WasmAppDir ?? "bin/Debug/AppBundle");
    return {
        module: {
            parser: {
                javascript: {
                    importMeta: false // requires webpack >= 5.68.0
                }
            }
        },
        mode: "production",// int webpack 5.68.0 devtools are not good with es6 module as library
        entry: './app.js',
        experiments: {
            outputModule: true,
        },
        output: {
            filename: 'app.js',
            library: { type: "module" },
            path: wasmAppDir,
        }
    }
};