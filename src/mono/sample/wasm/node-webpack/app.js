import { dotnet } from '@microsoft/dotnet-runtime'
import { color } from 'console-log-colors'

async function dotnetMeaning() {
    try {
        const { getAssemblyExports } = await dotnet.create();
        const exports = await getAssemblyExports("Wasm.Node.WebPack.Sample");
        const meaningFunction = exports.Sample.Test.Main;
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