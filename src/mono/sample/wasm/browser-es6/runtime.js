import createRuntime from './dotnet.js'

export const { MONO, INTERNAL, BINDING, Module } = await createRuntime(({ MONO, INTERNAL, BINDING, Module }) => ({
    disableDotNet6Compatibility: true,
    configSrc: "./mono-config.json",
    onAbort: function () {
        test_exit(1);
    },
}));

export function test_exit(exit_code) {
    console.log(`test_exit: ${exit_code}`);

    /* Set result in a tests_done element, to be read by xharness */
    var tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}