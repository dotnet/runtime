function wasm_exit(exit_code) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

function Uint8ToString(u8a){
    var CHUNK_SZ = 0x8000;
    var c = [];
    for (var i=0; i < u8a.length; i+=CHUNK_SZ) {
        c.push(String.fromCharCode.apply(null, u8a.subarray(i, i+CHUNK_SZ)));
    }
    return c.join("");
}

async function loadRuntime() {
    globalThis.exports = {};
    await import("./dotnet.js");
    return globalThis.exports.createDotnetRuntime;
}

async function main() {
    const createDotnetRuntime = await loadRuntime();
        const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => {
            console.log('user code in createDotnetRuntime')
            return {
                disableDotnet6Compatibility: true,
                configSrc: "./mono-config.json",
                preInit: () => { console.log('user code Module.preInit') },
                preRun: () => { console.log('user code Module.preRun') },
                onRuntimeInitialized: () => { console.log('user code Module.onRuntimeInitialized') },
                postRun: () => { console.log('user code Module.postRun') },
            }
        });
    globalThis.__Module = Module;
    globalThis.MONO = MONO;
    console.log('after createDotnetRuntime')

    try {
        const testMeaning = BINDING.bind_static_method("[Wasm.Browser.ThreadsEP.Sample] Sample.Test:TestMeaning");
        const ret = testMeaning();
        document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;

        console.debug(`ret: ${ret}`);

        let exit_code = ret == 42 ? 0 : 1;
        Module._mono_wasm_exit(exit_code);

        wasm_exit(exit_code);
    } catch (err) {
        console.log(`WASM ERROR ${err}`);

        var b = Module.FS.readFile('/trace.nettrace');
        var bits = btoa((Uint8ToString(b)));

        window.open("data:application/octet-stream;base64," + bits);
        
        wasm_exit(2);
    }
}

setTimeout(main, 10000);
