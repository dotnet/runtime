function wasm_exit(exit_code) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

async function loadRuntime() {
    globalThis.exports = {};
    await import("./dotnet.js");
    return globalThis.exports.createDotnetRuntime;
}

async function main() {
    try {
        const createDotnetRuntime = await loadRuntime();
        const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => {
            return {
                disableDotnet6Compatibility: true,
                configSrc: "./mono-config.json"
            }
        });

        document.querySelector("button").addEventListener("click", async () => {
            const testMeaning = BINDING.bind_static_method("[Wasm.Browser.JsArrayCopy] Sample.Test:TestMeaning");
            const ret = await testMeaning();
            document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;
    
            console.debug(`ret: ${ret}`);
            // let exit_code = ret == 42 ? 0 : 1;
            // wasm_exit(exit_code);
        })

    } catch (err) {
        console.log(`WASM ERROR ${err}`);
        wasm_exit(2)
    }
}

main();