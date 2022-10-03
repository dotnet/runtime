import createDotnetRuntime from './dotnet.js'

try {
    const { BINDING } = await createDotnetRuntime(({ MONO }) => ({
        configSrc: "./mono-config.json",
        onConfigLoaded: (config) => {
            config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
        },
    }));
    const update = BINDING.bind_static_method("[WasmDelta] Sample.Test:Update");
    const testMeaning = BINDING.bind_static_method("[WasmDelta] Sample.Test:TestMeaning");
    const outElement = document.getElementById("out");
    document.getElementById("update").addEventListener("click", function () {
        update();
        console.log("applied update");
        outElement.innerHTML = testMeaning();
    })
    outElement.innerHTML = testMeaning();
    console.log("ready");
} catch (err) {
    console.log(`WASM ERROR ${err}`);
}
