Module.preRun = () => {
    // copy all node/shell env variables to emscripten env
    for (const [key, value] of Object.entries(process.env)) {
        ENV[key] = value;
    }
    console.log("corerun.js preRun: " + Module.FS.cwd());
};