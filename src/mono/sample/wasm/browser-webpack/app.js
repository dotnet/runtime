// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from '@microsoft/dotnet-runtime'
import _ from 'underscore'

async function dotnetMeaning() {
    try {
        const { getAssemblyExports } = await dotnet.create();

        const exports = await getAssemblyExports("Wasm.Browser.WebPack.Sample");
        const meaningFunction = exports.Sample.Test.Main;
        return meaningFunction();
    } catch (err) {
        console.log(err)
        throw err;
    }
}

export async function main() {

    const element = document.getElementById("out");
    element.textContent = "loading dotnet...";

    const ret = await dotnetMeaning();
    const template = _.template('<%=ret%> as computed on dotnet');
    element.textContent = template({ ret });

    document.body.appendChild(element);
}