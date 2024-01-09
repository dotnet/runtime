// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let magic_buf: Uint8Array = null!;
const Magic = {
    get DOTNET_IPC_V1(): Uint8Array {
        if (magic_buf === null) {
            const magic = "DOTNET_IPC_V1";
            const magic_len = magic.length + 1; // nul terminated
            magic_buf = new Uint8Array(magic_len);
            for (let i = 0; i < magic_len; i++) {
                magic_buf[i] = magic.charCodeAt(i);
            }
            magic_buf[magic_len - 1] = 0;
        }
        return magic_buf;
    },
    get MinimalHeaderSize(): number {
        // we just need to see the magic and the size
        const sizeOfSize = 2;
        return Magic.DOTNET_IPC_V1.byteLength + sizeOfSize;
    },
};

export default Magic;
