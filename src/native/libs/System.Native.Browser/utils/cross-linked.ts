// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


import { } from "../../Common/JavaScript/cross-linked";
declare global {
    export let ABORT: boolean;
    export let EXITSTATUS: number;
    export function ExitStatus(exitCode: number): number;
}
