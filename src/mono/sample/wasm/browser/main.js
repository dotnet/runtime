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

try {
    const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => {
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

    const testMeaning = BINDING.bind_static_method("[Wasm.Browser.ES6.Sample] Sample.Test:TestMeaning");
    const ret = testMeaning();
    document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;
    console.debug(`ret: ${ret}`);

    let exit_code = await MONO.mono_run_main("Wasm.Browser.ES6.Sample.dll", []);
    wasm_exit(exit_code);
} catch (err) {
    wasm_exit(2, err);
}
