// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ! this is temporary file until we setup proper build system for TypeScript

Module.preRun = () => {
    // copy all node/shell env variables to emscripten env
    for (const [key, value] of Object.entries(process.env)) {
        ENV[key] = value;
    }
    const cwd = process.cwd().replaceAll('\\', '/').replace(/[a-zA-Z]:[\\/]/, '/'); // unix vs windows, remove drive letter
    const app = process.argv[2] || "HelloWorld.dll";
    const dlls = fs.readdirSync(cwd).filter(f => f.endsWith('.dll'));
    const tpa = dlls.map(f => `${cwd}/${f}`).join(':');
    ENV["CWD"] = cwd;
    ENV["APP"] = `${cwd}/${app}`;
    ENV["TRUSTED_PLATFORM_ASSEMBLIES"] = tpa;
};

