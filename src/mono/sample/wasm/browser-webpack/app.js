import createDotnetRuntime from '@microsoft/dotnet-runtime'
import _ from 'underscore'

async function dotnetMeaning() {
    try {
        const { BINDING } = await createDotnetRuntime({
            configSrc: "./mono-config.json",
            scriptDirectory: "./",
        });
        const meaningFunction = BINDING.bind_static_method("[Wasm.Browser.WebPack.Sample] Sample.Test:Main");
        return meaningFunction();
    } catch (err) {
        console.log(err)
        throw err;
    }
}

export async function main() {

    const element = document.getElementById("out")

    const ret = await dotnetMeaning();
    const template = _.template('<%=ret%> as computed on dotnet');
    element.textContent = template({ ret });

    document.body.appendChild(element);
}