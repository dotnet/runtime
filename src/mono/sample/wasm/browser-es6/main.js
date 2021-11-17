import createDotnetRuntime from './dotnet.js'

const { MONO, BINDING, Module } = await createDotnetRuntime(() => ({
    disableDotNet6Compatibility: true,
    configSrc: "./mono-config.json",
    onAbort: () => {
        wasm_exit(1);
    },
}));

const App = {
    init: function () {
        const testMeaning = BINDING.bind_static_method("[Wasm.Browser.Sample] Sample.Test:TestMeaning");
        const ret = testMeaning();
        document.getElementById("out").innerHTML = ret;

        console.debug(`ret: ${ret}`);
        let exit_code = ret == 42 ? 0 : 1;
        wasm_exit(exit_code);
    },
};

App.init();

function wasm_exit(exit_code) {
    console.log(`WASM EXIT ${exit_code}`);
}