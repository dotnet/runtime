function wasm_exit(exit_code) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

function downloadData(dataURL,filename)
{
    // make an `<a download="filename" href="data:..."/>` link and click on it to trigger a download with the given name
    const elt = document.createElement('a');
    elt.download = filename;
    elt.href = dataURL;

    document.body.appendChild(elt);

    elt.click();

    document.body.removeChild(elt);
}

function makeTimestamp()
{
    // ISO date string, but with : and . replaced by -
    const t = new Date();
    const s = t.toISOString();
    return s.replace(/[:.]/g, '-');
}

async function loadRuntime() {
    globalThis.exports = {};
    await import("./dotnet.js");
    return globalThis.exports.createDotnetRuntime;
}


const delay = (ms) => new Promise((resolve) => setTimeout (resolve, ms))

const saveUsingBlob = true;

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

    const startWork = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:StartAsyncWork");
    const stopWork = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:StopWork");
    const getIterationsDone = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:GetIterationsDone");
    const eventSession = MONO.diagnostics.createEventPipeSession();
    eventSession.start();
    const workPromise = startWork();

    document.getElementById("out").innerHTML = '&lt;&lt;running&gt;&gt;';
    await delay(5000); // let it run for 5 seconds

    stopWork();

    document.getElementById("out").innerHTML = '&lt;&lt;stopping&gt;&gt;';

    const ret = await workPromise; // get the answer
    const iterations = getIterationsDone(); // get how many times the loop ran

    eventSession.stop();

    document.getElementById("out").innerHTML = `${ret} as computed in ${iterations} iterations on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;

    console.debug(`ret: ${ret}`);

    const filename = "dotnet-wasm-" + makeTimestamp() + ".nettrace";

    if (saveUsingBlob) {
        const blob = eventSession.getTraceBlob();
        const uri = URL.createObjectURL(blob);
        downloadData(uri, filename);
        URL.revokeObjectURL(uri);
    } else {
        const dataUri = eventSession.getTraceDataURI();

        downloadData(dataUri, filename);
    }
    const exit_code = ret == 42 ? 0 : 1;

    wasm_exit(exit_code);
}

console.log("Waiting 10s for curious human before starting the program");
setTimeout(main, 10000);
