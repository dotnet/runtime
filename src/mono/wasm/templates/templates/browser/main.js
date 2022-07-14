import { App } from './app-support.js'

App.main = async function (applicationArguments) {
    App.IMPORTS.window = {
        location: {
            href: () => globalThis.window.location.href
        }
    };

    const exports = await App.MONO.mono_wasm_get_assembly_exports("browser.0.dll");
    const text = exports.MyClass.Greeting();
    document.getElementById("out").innerHTML = `${text}`;

    await App.MONO.mono_run_main("browser.0.dll", applicationArguments);
}
