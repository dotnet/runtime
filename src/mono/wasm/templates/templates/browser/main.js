import createDotnetRuntime from './dotnet.js'

try {
    const { MONO, BINDING, Module, RuntimeBuildInfo } = await createDotnetRuntime();
    const managedMethod = BINDING.bind_static_method("[browser.0] MyClass:CallMeFromJS");
    const text = managedMethod();
    document.getElementById("out").innerHTML = `${text}`;

    await MONO.mono_run_main("browser.0.dll", []);
} catch (err) {
    console.log(`WASM ERROR ${err}`);
    document.getElementById("out").innerHTML = `error: ${err}`;
}
