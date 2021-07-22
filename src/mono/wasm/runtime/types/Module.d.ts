// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces and types that we use. Specifically, this file is for the EMSDK API

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/
declare namespace Module {
    let config: MonoConfig;
    let HEAP8: Int8Array;
    let HEAP16: Int16Array;
    let HEAP32: Int32Array;
    let HEAPU8: Uint8Array;
    let HEAPU16: Uint16Array;
    let HEAPU32: Uint32Array;
    let HEAPF32: Float32Array;
    let HEAPF64: Float64Array;

    let arguments: string[];
    let loadReadFiles: boolean;
    let printWithColors: boolean;
    let noExitRuntime: boolean;
    let noInitialRun: boolean;

    type preInit = Function | Function[];
    type preRun = Function | Function[];

    function locateFile (path: string, prefix: string): string;
    function onAbort (): void;
    function onRuntimeInitialized (): void;
    function getPreloadedPackage (remotePackageName: string, remotePackageSize: number): void;
    function _malloc (amnt: number): number;
    function _free (amn: number): void;
    function addRunDependency(id: string): void;
    function removeRunDependency(id: string): void;
    function mono_method_get_call_signature (method: number, mono_obj?: number): ArgsMarshalString;
    function print (message: string): void;

    function ccall <T> (ident: string, returnType?: string, argTypes?: string[], args?: any[] , opts?: any): T;
    function cwrap <T extends Function> (ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    function cwrap <T extends Function> (ident: string, ...args: any[]): T;

    function FS_createPath (parent: string, path: string, canRead?: boolean /* unused */, canWrite?: boolean /* unused */): string;
    function FS_createDataFile (parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    function setValue (ptr: number, value: number, type: string, noSafe?: number | boolean): void;
    function getValue (ptr: number, type: string, noSafe?: number | boolean ): number;
    function UTF8ToString (ptr: number, maxBytesToRead?: number): string;
    function UTF8ToString (arg: string): string;
    function UTF8ArrayToString (str: TypedArray, heap: number[] | number, outIdx: number, maxBytesToWrite?: number): string;
    function addFunction (func: Function, sig: string): void;
}

type MonoConfig = {
    assembly_root: string,
    debug_level: number,
    assets: (AssetEntry | AssetEntry | SatelliteAssemblyEntry | VfsEntry | IcuData)[],
    remote_sources: string[],
    [index: string]: any, // overflow for the "Extra" sections
} | {message: string, error: any};

// Types of assets that can be in the mono-config.js/mono-config.json file (taken from /src/tasks/WasmAppBuilder/WasmAppBuilder.cs)
type AssetEntry = {
    behavior: AssetBehaviours,
    name: string,
    virtual_path?: string,
    culture?: Culture,
    load_remote?: boolean,
    is_optional?: boolean
}

interface AssemblyEntry extends AssetEntry {
    name: "assembly"
}

interface SatelliteAssemblyEntry extends AssetEntry {
    name: "resource",
    culture: Culture
}

interface VfsEntry extends AssetEntry {
    name: "vfs",
    virtual_path: string
}

interface IcuData extends AssetEntry {
    name: "icu",
    load_remote: boolean
}

// Note that since these are annoated as `declare const enum` they are replaces by tsc with their raw value during compilation
declare const enum AssetBehaviours {
    Resource = "resource",
    Assembly = "assembly",
    Heap = "heap",
    ICU = "icu",
    VFS = "vfs",
}

type Culture = "ar_SA" | "am_ET" | "bg_BG" | "bn_BD" | "bn_IN" | "ca_AD" | "ca_ES" | "cs_CZ" | "da_DK" |
"de_AT" | "de_BE" | "de_CH" | "de_DE" | "de_IT" | "de_LI" | "de_LU" | "el_CY" | "el_GR" | "en_AE" |
"en_AG" | "en_AI" | "en_AS" | "en_AT" | "en_AU" | "en_BB" | "en_BE" | "en_BI" | "en_BM" | "en_BS" |
"en_BW" | "en_BZ" | "en_CA" | "en_CC" | "en_CH" | "en_CK" | "en_CM" | "en_CX" | "en_CY" | "en_DE" |
"en_DK" | "en_DM" | "en_ER" | "en_FI" | "en_FJ" | "en_FK" | "en_FM" | "en_GB" | "en_GD" | "en_GG" |
"en_GH" | "en_GI" | "en_GM" | "en_GU" | "en_GY" | "en_HK" | "en_IE" | "en_IL" | "en_IM" | "en_IN" |
"en_IO" | "en_JE" | "en_JM" | "en_KE" | "en_KI" | "en_KN" | "en_KY" | "en_LC" | "en_LR" | "en_LS" |
"en_MG" | "en_MH" | "en_MO" | "en_MP" | "en_MS" | "en_MT" | "en_MU" | "en_MW" | "en_MY" | "en_NA" |
"en_NF" | "en_NG" | "en_NL" | "en_NR" | "en_NU" | "en_NZ" | "en_PG" | "en_PH" | "en_PK" | "en_PN" |
"en_PR" | "en_PW" | "en_RW" | "en_SB" | "en_SC" | "en_SD" | "en_SE" | "en_SG" | "en_SH" | "en_SI" |
"en_SL" | "en_SS" | "en_SX" | "en_SZ" | "en_TC" | "en_TK" | "en_TO" | "en_TT" | "en_TV" | "en_TZ" |
"en_UG" | "en_UM" | "en_US" | "en_VC" | "en_VG" | "en_VI" | "en_VU" | "en_WS" | "en_ZA" | "en_ZM" |
"en_ZW" | "en_US" | "es_419" | "es_ES" | "es_MX" | "et_EE" | "fa_IR" | "fi_FI" | "fil_PH" | "fr_BE" |
"fr_CA" | "fr_CH" | "fr_FR" | "gu_IN" | "he_IL" | "hi_IN" | "hr_BA" | "hr_HR" | "hu_HU" | "id_ID" |
"it_CH" | "it_IT" | "ja_JP" | "kn_IN" | "ko_KR" | "lt_LT" | "lv_LV" | "ml_IN" | "mr_IN" | "ms_BN" |
"ms_MY" | "ms_SG" | "nl_AW" | "nl_BE" | "nl_NL" | "pl_PL" | "pt_BR" | "pt_PT" | "ro_RO" | "ru_RU" |
"sk_SK" | "sl_SI" | "sr_Cyrl_RS" | "sr_Latn_RS" | "sv_AX" | "sv_SE" | "sw_CD" | "sw_KE" | "sw_TZ" |
"sw_UG" | "ta_IN" | "ta_LK" | "ta_MY" | "ta_SG" | "te_IN" | "th_TH" | "tr_CY" | "tr_TR" | "uk_UA" |
"vi_VN" | "zh_CN" | "zh_Hans_HK" | "zh_SG" | "zh_HK" | "zh_TW";

type Context = {
    tracing: boolean,
    pending_count: number,
    num_icu_assets_loaded_successfully?: number,
    mono_wasm_add_assembly: (a: string, b: number, c:number) => number,
    mono_wasm_add_satellite_assembly: (a: string, b:string, c:number, d:number) => void,
    loaded_files: {url: string, file: string}[],
    loaded_assets: string[],
    createPath: Function,
    createDataFile: Function
}
