// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetLogger } from "./cross-module";

const symbol_map = new Map<number, string>();
let symbolTable: string | undefined;
const regexes: RegExp[] = [];

export function installNativeSymbols(table: string) {
    symbolTable = table;
}

export function symbolicateStackTrace(message: string): string {
    const origMessage = message;
    initSymbolMap();

    if (symbol_map.size === 0)
        return message;

    try {

        for (let i = 0; i < regexes.length; i++) {
            const newRaw = message.replace(regexes[i], (substring, ...args) => {
                const groups = args.find(arg => {
                    return typeof (arg) == "object" && arg.replaceSection !== undefined;
                });

                if (groups === undefined)
                    return substring;

                const funcNum = groups.funcNum;
                const replaceSection = groups.replaceSection;
                const name = symbol_map.get(Number(funcNum));

                if (name === undefined)
                    return substring;

                return substring.replace(replaceSection, `${name} (${replaceSection})`);
            });

            if (newRaw !== origMessage)
                return newRaw;
        }

        return origMessage;
    } catch (error) {
        dotnetLogger.debug(`failed to symbolicate: ${error}`);
        return message;
    }
}

function initSymbolMap() {
    if (!symbolTable)
        return;

    // V8
    //   at <anonymous>:wasm-function[1900]:0x83f63
    //   at dlfree (<anonymous>:wasm-function[18739]:0x2328ef)
    regexes.push(/at (?<replaceSection>[^:()]+:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)((?![^)a-fA-F\d])|$)/g);

    //# 5: WASM [009712b2], function #111 (''), pc=0x7c16595c973 (+0x53), pos=38740 (+11)
    regexes.push(/(?:WASM \[[\da-zA-Z]+\], (?<replaceSection>function #(?<funcNum>[\d]+) \(''\)))/g);

    //# chrome
    //# at http://127.0.0.1:63817/dotnet.wasm:wasm-function[8963]:0x1e23f4
    regexes.push(/(?<replaceSection>[a-z]+:\/\/[a-zA-Z0-9.:/_~-]*:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)/g);

    //# <?>.wasm-function[8962]
    regexes.push(/(?<replaceSection><[^ >]+>[.:]wasm-function\[(?<funcNum>[0-9]+)\])/g);

    const text = symbolTable;
    symbolTable = undefined;
    try {
        text.split(/\r?\n|\r/).forEach((line: string) => {
            const parts: string[] = line.split(/:/);
            if (parts.length < 2)
                return;

            parts[1] = parts.splice(1).join(":").replace(/\\([0-9a-fA-F]{2})/g, (_, hex) => String.fromCharCode(parseInt(hex, 16)));
            symbol_map.set(Number(parts[0]), parts[1]);
        });
    } catch (exc) {
        dotnetLogger.debug(`failed to parse symbol table: ${exc}`);
    }
}
