import createDotnetRuntime from '@microsoft/dotnet-runtime'
import { color } from 'console-log-colors'

async function dotnetMeaning() {
    try {
        const { BINDING } = await createDotnetRuntime({
            configSrc: "./mono-config.json"
        });
        const meaningFunction = BINDING.bind_static_method("[Wasm.Node.WebPack.Sample] Sample.Test:Main");
        return meaningFunction();
    } catch (err) {
        console.log(err)
        throw err;
    }
}

export async function main() {
    const meaning = await dotnetMeaning()
    console.log(color.blue("Answer to the Ultimate Question of Life, the Universe, and Everything is: ") + color.red(`${meaning}`));
}