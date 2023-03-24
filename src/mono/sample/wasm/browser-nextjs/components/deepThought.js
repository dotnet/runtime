// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useState, useEffect } from 'react'
import { dotnet } from '@microsoft/dotnet-runtime'

let dotnetRuntimePromise = undefined;

async function createRuntime() {
    try {
        return dotnet.
            withModuleConfig({
                locateFile: (path, prefix) => {
                    return '/' + path;
                }
            })
            .create();
    } catch (err) {
        console.error(err);
        throw err;
    }
}

async function dotnetMeaning() {
    if (!dotnetRuntimePromise) {
        dotnetRuntimePromise = createRuntime();
    }
    const { getAssemblyExports } = await dotnetRuntimePromise;
    const exports = await getAssemblyExports("Wasm.Browser.NextJs.Sample.dll");
    return exports.Sample.Test.Main();
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
