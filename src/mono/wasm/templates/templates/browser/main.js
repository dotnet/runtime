import { App } from './app-support.js'

async function main(applicationArguments) {
    App.runtime.setModuleImports("main.js", {
        window: {
            location: {
                href: () => globalThis.window.location.href
            }
        }
    });
    const exports = await App.runtime.getAssemblyExports("browser.0.dll");
    const text = exports.MyClass.Greeting();
    document.getElementById("out").innerHTML = `${text}`;

    await App.runtime.runMain("browser.0.dll", applicationArguments);
}

App.run(main);