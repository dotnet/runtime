import { App } from './app-support.js'

App.init = async function () {
    await App.MONO.mono_run_main("browser.0.dll", App.runArgs.applicationArguments);
}
