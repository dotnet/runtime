// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { RuntimeAPI } from "./types";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export function registerCDAC(runtimeApi: RuntimeAPI): void {
    runtimeApi.INTERNAL.GetDotNetRuntimeContractDescriptor = () => _ems_._GetDotNetRuntimeContractDescriptor();
    runtimeApi.INTERNAL.GetDotNetRuntimeHeap = (ptr: number, length: number) => _ems_.HEAPU8.subarray(ptr >>> 0, (ptr >>> 0) + length);
}
