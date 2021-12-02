
if (typeof globalThis.Module === "object") {
    createDotnetRuntime(() => { return globalThis.Module; }).then((exports) => exports);
}