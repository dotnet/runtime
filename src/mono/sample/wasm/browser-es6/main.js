import createDotnetRuntime from './dotnet.js'

function wasm_exit(exit_code) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

try {
    const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime();
    const testMeaning = BINDING.bind_static_method("[Wasm.Browser.ES6.Sample] Sample.Test:TestMeaning");
    const ret = testMeaning();
    document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;
    console.debug(`ret: ${ret}`);

    let exit_code = await MONO.mono_run_main("Wasm.Browser.ES6.Sample.dll", []);
    wasm_exit(exit_code);
} catch (err) {
    console.log(`WASM ERROR ${err}`);
    wasm_exit(2);
}
