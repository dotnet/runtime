function wasm_exit(exit_code) {
    /* Set result in a tests_done element, to be read by xharness in runonly CI test */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

function downloadData(dataURL, filename) {
    // make an `<a download="filename" href="data:..."/>` link and click on it to trigger a download with the given name
    const elt = document.createElement('a');
    elt.download = filename;
    elt.href = dataURL;

    document.body.appendChild(elt);

    elt.click();

    document.body.removeChild(elt);
}

function makeTimestamp() {
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


const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

async function doWork(startWork, stopWork, getIterationsDone) {
    const N = parseInt(document.getElementById("inputN").value);


    const workPromise = startWork(N);

    let btn = document.getElementById("startWork");
    btn.disabled = true;
    btn.innerText = "Working";
    document.getElementById("out").innerHTML = '...';

    await delay(5000); // let it run for 5 seconds

    document.getElementById("startWork").innerText = "Stopping";
    document.getElementById("out").innerHTML = '... ...';

    stopWork();

    const ret = await workPromise; // get the answer
    const iterations = getIterationsDone(); // get how many times the loop ran

    btn = document.getElementById("startWork");
    btn.disabled = false;
    btn.innerText = "Start Work";

    document.getElementById("out").innerHTML = `${ret} as computed in ${iterations} iterations`;

    console.debug(`ret: ${ret}`);

    return ret;
}

function getOnClickHandler(startWork, stopWork, getIterationsDone) {
    return async function () {
        const options = MONO.diagnostics.SessionOptionsBuilder
            .Empty
            .setRundownEnabled(false)
            .addProvider({ name: 'WasmHello', level: MONO.diagnostics.EventLevel.Verbose, args: 'EventCounterIntervalSec=1' })
            .build();
        console.log('starting providers', options.providers);

        const eventSession = MONO.diagnostics.createEventPipeSession(options);

        eventSession.start();
        const ret = await doWork(startWork, stopWork, getIterationsDone);
        eventSession.stop();

        const filename = "dotnet-wasm-" + makeTimestamp() + ".nettrace";

        const blob = eventSession.getTraceBlob();
        const uri = URL.createObjectURL(blob);
        downloadData(uri, filename);
    }
}


async function main() {
    const createDotnetRuntime = await loadRuntime();
    const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => {
        return {
            disableDotnet6Compatibility: true,
            configSrc: "./mono-config.json",
        }
    });
    globalThis.__Module = Module;
    globalThis.MONO = MONO;

    const startWork = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:StartAsyncWork");
    const stopWork = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:StopWork");
    const getIterationsDone = BINDING.bind_static_method("[Wasm.Browser.EventPipe.Sample] Sample.Test:GetIterationsDone");


    const btn = document.getElementById("startWork");

    btn.style.backgroundColor = "rgb(192,255,192)";
    btn.onclick = getOnClickHandler(startWork, stopWork, getIterationsDone);

}

main();
