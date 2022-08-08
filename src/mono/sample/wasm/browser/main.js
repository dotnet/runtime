import createDotnetRuntime from './dotnet.js'

function wasm_exit(exit_code, reason) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    if (exit_code) tests_done_elem.style.background = "red";
    document.body.appendChild(tests_done_elem);

    if (reason) console.error(reason);
    console.log(`WASM EXIT ${exit_code}`);
}

/**
 * @type {import('../../../wasm/runtime/dotnet').CreateDotnetRuntimeType}
 */
const createDotnetRuntimeTyped = createDotnetRuntime;

function add(a, b) {
    return a + b;
}

function sub(a, b) {
    return a - b;
}
try {
    const { runtimeBuildInfo, setModuleImports, getAssemblyExports, runMain } = await createDotnetRuntimeTyped(() => {
        // this callback usually needs no statements, the API objects are only empty shells here and are populated later
        return {
            configSrc: "./mono-config.json",
            onConfigLoaded: (config) => {
                // This is called during emscripten `dotnet.wasm` instantiation, after we fetched config.
                console.log('user code Module.onConfigLoaded');
                // config is loaded and could be tweaked before the rest of the runtime startup sequence
                config.environmentVariables["MONO_LOG_LEVEL"] = "debug"
            },
            preInit: () => { console.log('user code Module.preInit'); },
            preRun: () => { console.log('user code Module.preRun'); },
            onRuntimeInitialized: () => {
                console.log('user code Module.onRuntimeInitialized');
                // here we could use API passed into this callback
                // Module.FS.chdir("/");
            },
            onDotnetReady: () => {
                // This is called after all assets are loaded.
                console.log('user code Module.onDotnetReady');
            },
            postRun: () => { console.log('user code Module.postRun'); },
        }
    });
    // at this point both emscripten and monoVM are fully initialized.
    // we could use the APIs returned and resolved from createDotnetRuntime promise
    // both exports are receiving the same object instances
    console.log('user code after createDotnetRuntime()');
    setModuleImports("main.js", {
        Sample: {
            Test: {
                add,
                sub
            }
        }
    });

    const exports = await getAssemblyExports("Wasm.Browser.Sample.dll");
    const meaning = exports.Sample.Test.TestMeaning();
    console.debug(`meaning: ${meaning}`);
    if (!exports.Sample.Test.IsPrime(meaning)) {
        document.getElementById("out").innerHTML = `${meaning} as computed on dotnet ver ${runtimeBuildInfo.productVersion}`;
        console.debug(`ret: ${meaning}`);
    }

    let exit_code = await runMain("Wasm.Browser.Sample.dll", []);
    wasm_exit(exit_code);
}
catch (err) {
    wasm_exit(2, err);
}