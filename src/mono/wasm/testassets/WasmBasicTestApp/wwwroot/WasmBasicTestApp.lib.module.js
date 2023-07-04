export function onRuntimeConfigLoaded(config) {
    config.environmentVariables["LIBRARY_INITIALIZER_TEST"] = "1";
}

export async function onRuntimeReady({ getAssemblyExports, getConfig }) {
    const params = new URLSearchParams(location.search);
    const testCase = params.get("test");
    if (testCase == "LibraryInitializerTest") {
        const config = getConfig();
        const exports = await getAssemblyExports(config.mainAssemblyName);

        exports.LibraryInitializerTest.Run();
    }
}