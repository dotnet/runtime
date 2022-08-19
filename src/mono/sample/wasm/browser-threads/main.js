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

let progressElement = null;

function updateProgress(status) {
    if (progressElement) {
        progressElement.innerText = status;
    } else {
        console.log("Progress: " + status);
    }
}

const assemblyName = "Wasm.Browser.Threads.Sample.dll";

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function Run(exports, N) {
    while (true) {
        await delay(50);
        const p = exports.Sample.Test.Progress();
        if (p === 0)
            break;
    }
    const answer = exports.Sample.Test.GetAnswer();
    document.getElementById("out").innerText = `Fib(${N}) =  ${answer}`;
}

async function doMathSlowly(exports) {
    progressElement = document.getElementById("progressElement");
    const N = parseInt(document.getElementById("inputN").value);
    exports.Sample.Test.Start(N);
    await Run(exports, N);
}

function setEditable(inputElement, isEditable) {
    inputElement.disabled = !isEditable;
}

function onInputValueChanged(exports, inputElement) {
    async function handler() {
        setEditable(inputElement, false);
        await doMathSlowly(exports);
        setEditable(inputElement, true);
    }
    return handler;
}

try {
    const inputElement = document.getElementById("inputN");
    const { runtimeBuildInfo, setModuleImports, getAssemblyExports, runMain } = await createDotnetRuntime(() => {
        console.log('user code in createDotnetRuntime callback');
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
    console.log('user code after createDotnetRuntime()');
    setModuleImports("main.js", {
	Sample: {
	    Test: {
		updateProgress
	    }
	}
    });

    const exports = await getAssemblyExports(assemblyName);

    await doMathSlowly(exports);
    setEditable(inputElement, true);
    inputElement.addEventListener("change", onInputValueChanged(exports, inputElement));

    let exit_code = await runMain(assemblyName, []);
    wasm_exit(exit_code);
} catch (err) {
    wasm_exit(2, err);
}
