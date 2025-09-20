var dotnetInternals = {
    runtimeApi: {
        Module: Module,
    },
    updates: [],
};
Module.preRun = () => {
    // copy all node/shell env variables to emscripten env
    if (globalThis.process && globalThis.process.env) {
        for (const [key, value] of Object.entries(process.env)) {
            ENV[key] = value;
        }
    }
};