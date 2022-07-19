import { App } from './app-support.mjs'

App.main = async function (applicationArguments) {
    await App.MONO.mono_run_main("console.0.dll", applicationArguments);
}
