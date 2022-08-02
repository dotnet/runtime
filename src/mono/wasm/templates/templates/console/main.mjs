import { App } from './app-support.mjs'

App.main = async function (applicationArguments) {
    App.API.setModuleImports("main.mjs", {
        node: {
            process: {
                version: () => globalThis.process.version
            }
        }
    });

    const exports = await App.API.getAssemblyExports("console.0.dll");
    const text = exports.MyClass.Greeting();
    console.log(text);

    return await App.API.runMain("console.0.dll", applicationArguments);
}
