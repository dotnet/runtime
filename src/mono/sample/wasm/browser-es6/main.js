import createDotnetRuntime from './dotnet.js'

const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime((api) => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
    onAbort: () => {
        wasm_exit(1);
    },
}));

function wasm_exit(exit_code) {
    console.log(`WASM EXIT ${exit_code}`);
}

const testMeaning = BINDING.bind_static_method("[Wasm.Browser.ES6.Sample] Sample.Test:TestMeaning");
const ret = testMeaning();
document.getElementById("out").innerHTML = `${ret} as computed on dotnet ver ${RuntimeBuildInfo.ProductVersion}`;

console.debug(`ret: ${ret}`);
let exit_code = ret == 42 ? 0 : 1;
wasm_exit(exit_code);