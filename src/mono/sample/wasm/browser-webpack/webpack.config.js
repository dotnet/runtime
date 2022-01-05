const path = require('path');

module.exports = (env) => {
    const wasmAppDir = path.resolve(env.WasmAppDir);
    const mode = env.Configuration === "Release" ? "production" : "development";
    return {
        mode,
        entry: './index.js',
        output: {
            filename: 'main.js',
            path: wasmAppDir,
        }
    }
};