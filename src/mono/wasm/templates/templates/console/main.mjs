import { App } from './app-support.mjs'

App.main = async function (applicationArguments) {

    App.IMPORTS.node = {
        process : {
            version: () => globalThis.process.version
        }
    };

    const exports = await App.MONO.mono_wasm_get_assembly_exports("console.0.dll");
    const text = exports.MyClass.Greeting();
    console.log(text);

    await App.MONO.mono_run_main("console.0.dll", applicationArguments);
}
