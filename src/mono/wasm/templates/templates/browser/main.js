import { App } from './app-support.js'

App.main = async function (applicationArguments) {
    await App.MONO.mono_run_main("browser.0.dll", applicationArguments);
}
