import { App } from './app-support.mjs'

async function main(applicationArguments) {
    App.runtime.setModuleImports("main.mjs", {
        node: {
            process: {
                version: () => globalThis.process.version
            }
        }
    });

    const exports = await App.runtime.getAssemblyExports("console.0.dll");
    const text = exports.MyClass.Greeting();
    console.log(text);

    return await App.runtime.runMain("console.0.dll", applicationArguments);
}

App.run(main);