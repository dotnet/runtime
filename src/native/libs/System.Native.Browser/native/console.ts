// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr } from "../types";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export function SystemJS_ConsoleWriteJS(buffer: number, bufferLength: number, isError: number): number | CharPtr {
    try {
        const start = buffer >>> 0;
        const bytes = _ems_.dotnetApi.localHeapViewU8().subarray(start, start + bufferLength);
        _ems_.dotnetApi.INTERNAL.consoleWriteBytes(bytes, isError !== 0);
        return 0;
    } catch (error) {
        return _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(_ems_.dotnetLoaderExports.normalizeException(error));
    }
}
