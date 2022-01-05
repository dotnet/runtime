import createDotnetRuntime from '@microsoft/dotnet-runtime'

let dotnetRuntime = undefined;
let meaningFunction = undefined;

async function dotnetMeaning() {
    if (!dotnetRuntime) {
        try {
            const { BINDING } = await createDotnetRuntime({
                configSrc: "./mono-config.json",
                scriptDirectory: "./"
            });
            meaningFunction = BINDING.bind_static_method("[Wasm.Browser.WebPack.Sample] Sample.Test:TestMeaning");
        } catch (err) {
            console.log(err)
            throw err;
        }
    }
    return meaningFunction();
}

async function main() {
    const element = document.createElement('div');

    const ret = await dotnetMeaning();
    element.textContent = `${ret} as computed on dotnet`;

    document.body.appendChild(element);
}

main()