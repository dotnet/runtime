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

async function loadDotnet(file) {
    const cjsExport = new Promise((resolve) => {
        globalThis.__onDotnetRuntimeLoaded = (createDotnetRuntime) => {
            delete globalThis.__onDotnetRuntimeLoaded;
            resolve(createDotnetRuntime);
        };
    });

    await import(file);
    return await cjsExport;
}


async function main() {
    try {
        const createDotnetRuntime = await loadDotnet("./dotnet.js");
        const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => ({
            disableDotnet6Compatibility: true,
            configSrc: "./mono-config.json",
        }));

        const testMeaning = BINDING.bind_static_method("[Wasm.Browser.CJS.Sample] Sample.Test:TestMeaning");
        const ret = testMeaning();
        document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;

        console.debug(`ret: ${ret}`);
        let exit_code = ret == 42 ? 0 : 1;
        wasm_exit(exit_code);
    } catch (err) {
        wasm_exit(2, err)
    }
}

main();