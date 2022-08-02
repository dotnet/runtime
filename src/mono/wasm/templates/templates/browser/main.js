import { App } from './app-support.js'

App.main = async function (applicationArguments) {
    App.API.setModuleImports("main.js", {
        window: {
            location: {
                href: () => globalThis.window.location.href
            }
        }
    });
    const exports = await App.API.getAssemblyExports("browser.0.dll");
    const text = exports.MyClass.Greeting();
    document.getElementById("out").innerHTML = `${text}`;

    await App.API.runMain("browser.0.dll", applicationArguments);
}
