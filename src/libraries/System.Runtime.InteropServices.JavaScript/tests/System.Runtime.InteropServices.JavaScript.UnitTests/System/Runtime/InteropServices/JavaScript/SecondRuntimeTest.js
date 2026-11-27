export async function runSecondRuntimeAndTestStaticState(guid) {
    const { dotnet: dotnet2 } = await import('./_framework/dotnet.js?instance=2-' + guid);
    const runtime2 = await dotnet2
        .withConfig({
            forwardConsole: false,
            diagnosticTracing: false,
            appendElementOnExit: false,
            logExitCode: false,
        })
        .create();

    const increment1 = await getIncrementStateFunction(App.runtime);
    const increment2 = await getIncrementStateFunction(runtime2);

    increment1();
    increment1();
    increment2();
    increment2();
    const state2 = increment2();
    return state2;
}

async function getIncrementStateFunction(runtime) {
    const exports = await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.dll");
    return exports.System.Runtime.InteropServices.JavaScript.Tests.SecondRuntimeTest.Interop.IncrementState;
}

export async function capturedExportCannotRunAfterDisposal(guid, bindBeforeExit) {
    const module = await import('./_framework/dotnet.js?instance=dispose-' + guid);
    const runtime = await module.dotnet.withConfig({
        forwardConsole: false,
        appendElementOnExit: false,
        logExitCode: false,
    }).create();
    const increment = await getIncrementStateFunction(runtime);
    if (bindBeforeExit && increment() !== 1)
        throw new Error("The second runtime did not have independent export state");
    try {
        module.exit(0);
    } catch (error) {
        if (error?.status !== 0) throw error;
    }
    try {
        increment();
    } catch (error) {
        return error.message.includes("The runtime is not running.") ||
            error.message.includes(".NET runtime already exited with 0");
    }
    return false;
}