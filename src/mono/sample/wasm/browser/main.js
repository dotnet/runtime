// @ts-check
// @ts-ignore
import createDotnetRuntime from './dotnet.js'

/**
 * @type {import('../../../wasm/runtime/dotnet').CreateDotnetRuntimeType}
 */
const createDotnetRuntimeTyped = createDotnetRuntime;

function add(a, b) {
    return a + b;
}

function sub(a, b) {
    return a - b;
}

const { API, RuntimeBuildInfo, IMPORTS } = await createDotnetRuntimeTyped(() => {
    console.log('user code in createDotnetRuntime callback');
    return {
        configSrc: "./mono-config.json",
        preInit: () => { console.log('user code Module.preInit'); },
        preRun: () => { console.log('user code Module.preRun'); },
        onRuntimeInitialized: () => { console.log('user code Module.onRuntimeInitialized'); },
        postRun: () => { console.log('user code Module.postRun'); },
    }
});
console.log('user code after createDotnetRuntime()');
IMPORTS.Sample = {
    Test: {
        add,
        sub
    }
};

const exports = await API.getAssemblyExports("Wasm.Browser.Sample.dll");
const meaning = exports.Sample.Test.TestMeaning();
console.debug(`meaning: ${meaning}`);
if (!exports.Sample.Test.IsPrime(meaning)) {
    // @ts-ignore
    document.getElementById("out").innerHTML = `${meaning} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;
    console.debug(`ret: ${meaning}`);
}

await API.runMainAndExit("Wasm.Browser.Sample.dll", []);