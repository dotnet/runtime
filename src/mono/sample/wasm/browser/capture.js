async function exportMemory(memory) { // NodeJS
    const require = await import('module').then(mod => mod.createRequire(import.meta.url));
    const fs = require("fs");
    fs.promises.writeFile("./memory.dat", memory);
}

async function runtime1() {
    console.log("Runtime 1");
    const dotnet = (await import("./dotnet.js?1")).dotnet;
    const { setModuleImports, getAssemblyExports, getConfig, Module } = await dotnet.create();

    setModuleImports("main.js", { location: { href: () => "window.location.href" } });

    const exports = getAssemblyExports(getConfig().mainAssemblyName);

    await exportMemory(Module.HEAP8);
}

runtime1();