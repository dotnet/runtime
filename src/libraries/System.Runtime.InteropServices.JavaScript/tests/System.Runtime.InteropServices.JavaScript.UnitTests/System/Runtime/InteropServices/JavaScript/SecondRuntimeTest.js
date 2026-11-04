export async function runSecondRuntimeAndTestStaticState(guid) {
    const { dotnet: dotnet2 } = await import('./_framework/dotnet.js?instance=2-' + guid);
    const builder2 = dotnet2
        .withConfig({
            forwardConsole: false,
            diagnosticTracing: false,
            appendElementOnExit: false,
            logExitCode: false,
        });
    const runtime2 = await builder2.create();
    if (runtime2.runtimeId === App.runtime.runtimeId
        || globalThis.getDotnetRuntime(runtime2.runtimeId) !== runtime2
        || globalThis.getDotnetRuntime(App.runtime.runtimeId) !== App.runtime) {
        throw new Error("Runtime registrations collided");
    }

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