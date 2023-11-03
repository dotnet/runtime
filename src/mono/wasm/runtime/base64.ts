// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code from JSIL:
// https://github.com/sq/JSIL/blob/1d57d5427c87ab92ffa3ca4b82429cd7509796ba/JSIL.Libraries/Includes/Bootstrap/Core/Classes/System.Convert.js#L149
// Thanks to Katelyn Gadd @kg

export function toBase64StringImpl(inArray: Uint8Array, offset?: number, length?: number) : string{
    const reader = _makeByteReader(inArray, offset, length);
    let result = "";
    let ch1: number | null = 0, ch2: number | null = 0, ch3: number | null = 0;
    let bits = 0, equalsCount = 0, sum = 0;
    const mask1 = (1 << 24) - 1, mask2 = (1 << 18) - 1, mask3 = (1 << 12) - 1, mask4 = (1 << 6) - 1;
    const shift1 = 18, shift2 = 12, shift3 = 6, shift4 = 0;

    for (;;) {
        ch1 = reader.read();
        ch2 = reader.read();
        ch3 = reader.read();

        if (ch1 === null)
            break;
        if (ch2 === null) {
            ch2 = 0;
            equalsCount += 1;
        }
        if (ch3 === null) {
            ch3 = 0;
            equalsCount += 1;
        }

        // Seems backwards, but is right!
        sum = (ch1 << 16) | (ch2 << 8) | (ch3 << 0);

        bits = (sum & mask1) >> shift1;
        result += _base64Table[bits];
        bits = (sum & mask2) >> shift2;
        result += _base64Table[bits];

        if (equalsCount < 2) {
            bits = (sum & mask3) >> shift3;
            result += _base64Table[bits];
        }

        if (equalsCount === 2) {
            result += "==";
        } else if (equalsCount === 1) {
            result += "=";
        } else {
            bits = (sum & mask4) >> shift4;
            result += _base64Table[bits];
        }
    }

    return result;
}

const _base64Table = [
    "A", "B", "C", "D",
    "E", "F", "G", "H",
    "I", "J", "K", "L",
    "M", "N", "O", "P",
    "Q", "R", "S", "T",
    "U", "V", "W", "X",
    "Y", "Z",
    "a", "b", "c", "d",
    "e", "f", "g", "h",
    "i", "j", "k", "l",
    "m", "n", "o", "p",
    "q", "r", "s", "t",
    "u", "v", "w", "x",
    "y", "z",
    "0", "1", "2", "3",
    "4", "5", "6", "7",
    "8", "9",
    "+", "/"
];

function _makeByteReader(bytes: Uint8Array, index?: number, count?: number): {
    read: () => number | null
} {
    let position = (typeof (index) === "number") ? index : 0;
    let endpoint: number;

    if (typeof (count) === "number")
        endpoint = (position + count);
    else
        endpoint = (bytes.length - position);

    const result = {
        read: function () {
            if (position >= endpoint)
                return null;

            const nextByte = bytes[position];
            position += 1;
            return nextByte;
        }
    };

    Object.defineProperty(result, "eof", {
        get: function () {
            return (position >= endpoint);
        },
        configurable: true,
        enumerable: true
    });

    return result;
}
