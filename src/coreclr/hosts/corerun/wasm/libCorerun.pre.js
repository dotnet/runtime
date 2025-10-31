var dotnetInternals = [
    {
        Module: Module,
    },
    [],
];
var basePreRun = () => {
    // copy all node/shell env variables to emscripten env
    if (globalThis.process && globalThis.process.env) {
        for (const [key, value] of Object.entries(process.env)) {
            ENV[key] = value;
        }
    }

    ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
};

// Append to or set the preRun array
Module.preRun = Module.preRun || [];
Module.preRun.push(basePreRun);