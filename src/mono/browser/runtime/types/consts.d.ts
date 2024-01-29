// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

declare module "consts:*" {
    //Constant that will be inlined by Rollup and rollup-plugin-consts.
    const constant: any;
    export default constant;
}

declare module "consts:monoWasmThreads" {
    const constant: boolean;
    export default constant;
}

/* if true, include mock impplementations of diagnostics sockets */
declare module "consts:monoDiagnosticsMock" {
    const constant: boolean;
    export default constant;
}

// this will throw exception if the condition is false
// see src\mono\browser\runtime\rollup.config.js
// inline this, because the lambda could allocate closure on hot path otherwise
declare function mono_check(condition: unknown, messageFactory: string | (() => string)): asserts condition;
