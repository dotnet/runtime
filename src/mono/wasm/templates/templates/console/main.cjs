const { App } = require("./app-support.cjs");

App.init = async function () {
    await App.MONO.mono_run_main_and_exit("console.0.dll", App.processedArguments.applicationArgs);
}
