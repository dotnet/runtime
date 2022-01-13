const path = require('path');

module.exports = (env) => {
    const wasmAppDir = path.resolve(env.WasmAppDir ?? "bin/Debug/AppBundle");
    const mode = env.Configuration === "Release" ? "production" : "development";
    return {
        mode,
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