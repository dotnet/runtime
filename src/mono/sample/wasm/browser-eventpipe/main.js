import createDotnetRuntime from "./dotnet.js";

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
        let sessions = MONO.diagnostics.getStartupSessions();

        if (typeof (sessions) !== "object" || sessions.length === "undefined")
            console.error("expected an array of sessions, got ", sessions);
        let eventSession = null;
        if (sessions.length !== 0) {
            if (sessions.length != 1)
                console.error("expected one startup session, got ", sessions);
            eventSession = sessions[0];
            console.debug("eventSession state is ", eventSession._state); // ooh protected member access
        }

        const ret = await doWork(startWork, stopWork, getIterationsDone);

        if (eventSession !== null) {
            eventSession.stop();

            const filename = "dotnet-wasm-" + makeTimestamp() + ".nettrace";

            const blob = eventSession.getTraceBlob();
            const uri = URL.createObjectURL(blob);
            downloadData(uri, filename);
        }

        console.debug("sample onclick handler done");
    }
}

async function main() {
    const { MONO, BINDING, Module } = await createDotnetRuntime(() => {
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
