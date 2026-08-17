import { dotnet } from './_framework/dotnet.js'

function wasm_exit(exit_code) {
    var tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

try {
    const { getAssemblyExports } = await dotnet.create();
    const exports = await getAssemblyExports("WebAssembly.Browser.Aot_131537.Test.dll");
    const ret = exports.Sample.Test.TestMeaning();
    document.getElementById("out").innerHTML = `${ret}`;
    console.debug(`ret: ${ret}`);
    wasm_exit(ret);
} catch (err) {
    console.log(`WASM ERROR ${err}`);
    wasm_exit(1);
}
