export async function runSecondRuntimeAndTestStaticState() {
    const { dotnet: dotnet2 } = await import('./dotnet.js?instance=2');
    const runtime2 = await dotnet2
        .withStartupMemoryCache(false)
        .withConfig({
            assetUniqueQuery: "?instance=2",
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