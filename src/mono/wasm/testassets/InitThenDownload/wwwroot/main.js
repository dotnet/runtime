import { dotnet } from './_framework/dotnet.js'

try {
    const originalFetch = globalThis.fetch;
    globalThis.fetch = (url, fetchArgs) => {
        console.log("TestOutput -> fetching " + url);
        return originalFetch(url, fetchArgs);
    };

    // optional call to download all assets
    await dotnet.download();
    console.log("TestOutput -> download finished");

    // and later it could be followed by usual
    const dotnetRuntime = await dotnet.create();

    const config = dotnetRuntime.getConfig();
    let exit_code = await dotnetRuntime.runMainAndExit(config.mainAssemblyName, []);
    dotnetRuntime.exit(exit_code); // this does not print WASM EXIT message
    console.log(`WASM EXIT ${exit_code}`);
}
catch (err) {
    exit(2, err);
}
