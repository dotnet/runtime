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
