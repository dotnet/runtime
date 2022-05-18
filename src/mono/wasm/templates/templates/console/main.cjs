const createDotnetRuntime = require("./dotnet.js");

async function main() {
    const { MONO } = await createDotnetRuntime();
    const app_args = process.argv.slice(2);
    await MONO.mono_run_main_and_exit("console.0.dll", app_args);
};
main();
