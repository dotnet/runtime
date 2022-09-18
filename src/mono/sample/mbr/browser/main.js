import { dotnet } from './dotnet.js'

try {
    const { getAssemblyExports } = await dotnet
        .withEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES", "debug")
        .create();

    const exports = await getAssemblyExports("WasmDelta.dll");
    const update = exports.Sample.Test.Update;
    const testMeaning = exports.Sample.Test.TestMeaning;
    
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
