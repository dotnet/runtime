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

function add(a, b) {
    return a + b;
}

function sub(a, b) {
    return a - b;
}

try {
    const { MONO, RuntimeBuildInfo, IMPORTS } = await createDotnetRuntime(() => {
        console.log('user code in createDotnetRuntime');
        return {
            configSrc: "./mono-config.json",
            preInit: () => { console.log('user code Module.preInit'); },
            preRun: () => { console.log('user code Module.preRun'); },
            onRuntimeInitialized: () => { console.log('user code Module.onRuntimeInitialized'); },
            postRun: () => { console.log('user code Module.postRun'); },
        }
    });
    console.log('after createDotnetRuntime');
    IMPORTS.Sample = {
        Test: {
            add,
            sub
        }
    };

    const exports = await MONO.mono_wasm_get_assembly_exports("Wasm.Browser.ES6.Sample.dll");
    const meaning = exports.Sample.Test.TestMeaning();
    console.debug(`meaning: ${meaning}`);
    if (!exports.Sample.Test.IsPrime(meaning)) {
        document.getElementById("out").innerHTML = `${meaning} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;
        console.debug(`ret: ${meaning}`);
    }

    let exit_code = await MONO.mono_run_main("Wasm.Browser.ES6.Sample.dll", []);
    wasm_exit(exit_code);
} catch (err) {
    wasm_exit(2, err);
}
