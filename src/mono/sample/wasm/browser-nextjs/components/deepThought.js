import { useState, useEffect } from 'react'
import createDotnetRuntime from 'dotnet'

let dotnetRuntime = undefined;
let meaningFunction = undefined;

function createRuntime() {
    if (dotnetRuntime) {
        return dotnetRuntime;
    }
    try {
        dotnetRuntime = createDotnetRuntime({
            configSrc: "./mono-config.json",
            disableDotnet6Compatibility: true,
            scriptDirectory: "/",
            instantiateWasm: async (imports, successCallback) => {
                try {
                    const response = await fetch('dotnet.wasm');
                    const arrayBuffer = await response.arrayBuffer();
                    const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, imports);
                    successCallback(arrayBufferResult.instance);
                } catch (err) {
                    console.error(err);
                    throw ex;
                }
            }
        });
        return dotnetRuntime;
    } catch (err) {
        console.error(err);
        throw err;
    }
}

async function dotnetMeaning() {
    const { BINDING } = await createRuntime();
    meaningFunction = BINDING.bind_static_method("[Wasm.Browser.NextJs.Sample] Sample.Test:TestMeaning");
    return meaningFunction();
}

export default function DeepThought() {
    const [meaning, setCount] = useState(undefined);

    useEffect(async () => {
        const meaning = await dotnetMeaning();
        setCount(meaning);
    }, []);

    if (!meaning) {
        return (<div>DeepThought is thinking ....</div>);
    }
    return (<div>Answer to the Ultimate Question of Life, the Universe, and Everything is : {meaning}!</div>);
};
