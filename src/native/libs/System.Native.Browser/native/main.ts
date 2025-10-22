// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "./cross-linked"; // ensure ambient symbols are declared

export function SystemJS_ResolveMainPromise(exitCode:number) {
    if (dotnetLoaderExports.resolveRunMainPromise) {
        dotnetLoaderExports.resolveRunMainPromise(exitCode);
    }
}

export function SystemJS_RejectMainPromise(reason:any) {
    // todo UNMARSHAL utf16 LPWSTR
    if (dotnetLoaderExports.rejectRunMainPromise) {
        dotnetLoaderExports.rejectRunMainPromise(reason);
    }
}
