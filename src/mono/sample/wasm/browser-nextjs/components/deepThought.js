import { useState, useEffect } from 'react'
import createDotnetRuntime from '@microsoft/dotnet-runtime'

let dotnetRuntimePromise = undefined;
let meaningFunction = undefined;

async function createRuntime() {
    try {
        const response = await fetch('dotnet.wasm');
        const arrayBuffer = await response.arrayBuffer();
        return createDotnetRuntime({
            configSrc: "./mono-config.json",
            disableDotnet6Compatibility: true,
            scriptDirectory: "/",
            instantiateWasm: async (imports, successCallback) => {
                try {
                    const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, imports);
                    successCallback(arrayBufferResult.instance);
                } catch (err) {
                    console.error(err);
                    throw err;
                }
            }
        });
    } catch (err) {
        console.error(err);
        throw err;
    }
}

async function dotnetMeaning() {
    if (!dotnetRuntimePromise) {
        dotnetRuntimePromise = createRuntime();
    }
    const { BINDING } = await dotnetRuntimePromise;
    meaningFunction = BINDING.bind_static_method("[Wasm.Browser.NextJs.Sample] Sample.Test:Main");
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
