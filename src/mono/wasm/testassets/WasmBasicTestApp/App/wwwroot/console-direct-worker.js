// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const events = [];
try {
    if (typeof window !== "undefined" || typeof importScripts !== "function") {
        throw new Error("Expected a web worker host");
    }
    const dotnetUrl = new URL(import.meta.url).searchParams.get("dotnet");
    if (dotnetUrl === null) {
        throw new Error("Missing .NET loader URL");
    }
    const { dotnet } = await import(dotnetUrl);
    const runtime = await dotnet.withConfig({ appendElementOnExit: false })
        .withModuleConfig({
            out: value => events.push(["out", value]),
            err: value => events.push(["err", value]),
            stdout: value => events.push(["stdout-byte", value]),
            stderr: value => events.push(["stderr-byte", value]),
        }).create();
    const config = runtime.getConfig();
    const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
    exports.ConsoleTest.RunInWorker();
    postMessage({ events });
} catch (error) {
    postMessage({ error: `${error}\n${error?.stack ?? ""}` });
}
