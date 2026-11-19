// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetBrowserUtilsExports, dotnetApi, dotnetAssert, Module } from "./cross-module";

import type { GCHandle, JSFunctionSignature, JSHandle, JSMarshalerType, JSMarshalerArgument, JSMarshalerArguments, MarshalerToCs, MarshalerToJs, VoidPtr, PThreadPtr } from "./types";
import { JavaScriptMarshalerArgSize, JavaScriptMarshalerArgSize64, JSBindingHeaderOffsets, JSBindingTypeOffsets, JSMarshalerArgumentOffsets, JSMarshalerSignatureHeaderSize, JSMarshalerTypeSize, MarshalerType } from "./types";
import { fixupPointer } from "./utils";

export function marshalerArgSize(): number {
    return Module.dotnetWasm64 ? JavaScriptMarshalerArgSize64 : JavaScriptMarshalerArgSize;
}

function slotOffset(offset: JSMarshalerArgumentOffsets, offset64: number): number {
    return Module.dotnetWasm64 ? offset64 : offset;
}

function getHandle(offset: any): number {
    return Module.dotnetWasm64 ? Number(dotnetApi.getHeapI64Big(offset)) : dotnetApi.getHeapI32(offset);
}

function setHandle(offset: any, handle: number): void {
    if (Module.dotnetWasm64) {
        dotnetApi.setHeapI64Big(offset, BigInt(handle));
    } else {
        dotnetApi.setHeapI32(offset, handle);
    }
}

export const jsInteropState = {
    isPendingSynchronousCall: false,
    proxyGCHandle: undefined as GCHandle | undefined,
    cspPolicy: false,
    isInitialized: false,
    isChromium: false,
    isFirefox: false,
    enablePerfMeasure: false,
    managedThreadTID: 0 as any as PThreadPtr,
};

export const csToJsMarshalers = new Map<MarshalerType, MarshalerToJs>();
export const jsToCsMarshalers = new Map<MarshalerType, MarshalerToCs>();
export const jsinteropDoc = "For more information see https://aka.ms/dotnet-wasm-jsinterop";

export function getMarshalerToCsByType(marshalerType: MarshalerType): MarshalerToCs | undefined {
    if (marshalerType === MarshalerType.None || marshalerType === MarshalerType.Void) {
        return undefined;
    }
    const converter = jsToCsMarshalers.get(marshalerType);
    dotnetAssert.fastCheck(converter && typeof converter === "function", () => `ERR30: Unknown converter for type ${marshalerType}`);
    return converter;
}

export function getMarshalerToJsByType(marshalerType: MarshalerType): MarshalerToJs | undefined {
    if (marshalerType === MarshalerType.None || marshalerType === MarshalerType.Void) {
        return undefined;
    }
    const converter = csToJsMarshalers.get(marshalerType);
    dotnetAssert.fastCheck(converter && typeof converter === "function", () => `ERR41: Unknown converter for type ${marshalerType}. ${jsinteropDoc}`);
    return converter;
}

export function allocStackFrame(size: number): JSMarshalerArguments {
    const bytes = marshalerArgSize() * size;
    const args = Module.stackAlloc(bytes) as any;
    dotnetBrowserUtilsExports.zeroRegion(args, bytes);
    setArgsContext(args);
    return args;
}

export function getArg(args: JSMarshalerArguments, index: number): JSMarshalerArgument {
    dotnetAssert.check(args, "Null args");
    return <any>args + (index * marshalerArgSize());
}

export function isArgsException(args: JSMarshalerArguments): boolean {
    dotnetAssert.check(args, "Null args");
    const exceptionType = getArgType(<any>args);
    return exceptionType !== MarshalerType.None;
}

export function isReceiverShouldFree(args: JSMarshalerArguments): boolean {
    dotnetAssert.check(args, "Null args");
    return dotnetApi.getHeapB8(<any>args + slotOffset(JSMarshalerArgumentOffsets.ReceiverShouldFree, 32));
}

export function getSyncDoneSemaphorePtr(args: JSMarshalerArguments): VoidPtr {
    dotnetAssert.check(args, "Null args");
    return getArgIntptr(<any>args + slotOffset(JSMarshalerArgumentOffsets.SyncDoneSemaphorePtr, 48)) as any;
}

export function getCallerNativeTid(args: JSMarshalerArguments): PThreadPtr {
    dotnetAssert.check(args, "Null args");
    return getHandle(<any>args + slotOffset(JSMarshalerArgumentOffsets.CallerNativeTID, 40)) as any;
}

// TODO-MT: unused until threads are enabled, the cross-thread post paths set this in the Mono tree
export function setReceiverShouldFree(args: JSMarshalerArguments): void {
    dotnetApi.setHeapB8(<any>args + slotOffset(JSMarshalerArgumentOffsets.ReceiverShouldFree, 32), true);
}

export function setArgsContext(args: JSMarshalerArguments): void {
    dotnetAssert.check(args, "Null args");
    const exc = getArg(args, 0);
    const res = getArg(args, 1);
    setArgProxyContext(exc);
    setArgProxyContext(res);
}

export function getSig(signature: JSFunctionSignature, index: number): JSMarshalerType {
    dotnetAssert.check(signature, "Null signatures");
    return <any>signature + (index * JSMarshalerTypeSize) + JSMarshalerSignatureHeaderSize;
}

export function getSignatureType(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null sig");
    return <any>dotnetApi.getHeapU8(<any>sig + JSBindingTypeOffsets.Type);
}

export function getSignatureResType(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null sig");
    return <any>dotnetApi.getHeapU8(<any>sig + JSBindingTypeOffsets.ResultMarshalerType);
}

export function getSignatureArg1Type(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null sig");
    return <any>dotnetApi.getHeapU8(<any>sig + JSBindingTypeOffsets.Arg1MarshalerType);
}

export function getSignatureArg2Type(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null sig");
    return <any>dotnetApi.getHeapU8(<any>sig + JSBindingTypeOffsets.Arg2MarshalerType);
}

export function getSignatureArg3Type(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null sig");
    return <any>dotnetApi.getHeapU8(<any>sig + JSBindingTypeOffsets.Arg3MarshalerType);
}

export function getSignatureArgumentCount(signature: JSFunctionSignature): number {
    dotnetAssert.check(signature, "Null signatures");
    return <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.ArgumentCount);
}

export function getSignatureVersion(signature: JSFunctionSignature): number {
    dotnetAssert.check(signature, "Null signatures");
    return <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.Version);
}

export function getSignatureHandle(signature: JSFunctionSignature): number {
    dotnetAssert.check(signature, "Null signatures");
    return <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.ImportHandle);
}

export function getSignatureFunctionName(signature: JSFunctionSignature): string | null {
    dotnetAssert.check(signature, "Null signatures");
    const functionNameOffset = <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.FunctionNameOffset);
    if (functionNameOffset === 0) return null;
    const functionNameLength = <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.FunctionNameLength);
    dotnetAssert.check(functionNameOffset, "Null name");
    return dotnetBrowserUtilsExports.utf16ToString(<any>signature + functionNameOffset, <any>signature + functionNameOffset + functionNameLength);
}

export function getSignatureModuleName(signature: JSFunctionSignature): string | null {
    dotnetAssert.check(signature, "Null signatures");
    const moduleNameOffset = <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.ModuleNameOffset);
    if (moduleNameOffset === 0) return null;
    const moduleNameLength = <any>dotnetApi.getHeapI32(<any>signature + JSBindingHeaderOffsets.ModuleNameLength);
    return dotnetBrowserUtilsExports.utf16ToString(<any>signature + moduleNameOffset, <any>signature + moduleNameOffset + moduleNameLength);
}

export function getSigType(sig: JSMarshalerType): MarshalerType {
    dotnetAssert.check(sig, "Null signatures");
    return <any>dotnetApi.getHeapU8(<any>sig);
}

export function getArgType(arg: JSMarshalerArgument): MarshalerType {
    dotnetAssert.check(arg, "Null arg");
    const type = dotnetApi.getHeapU8(<any>arg + slotOffset(JSMarshalerArgumentOffsets.Type, 20));
    return <any>type;
}

export function getArgElementType(arg: JSMarshalerArgument): MarshalerType {
    dotnetAssert.check(arg, "Null arg");
    const type = dotnetApi.getHeapU8(<any>arg + slotOffset(JSMarshalerArgumentOffsets.ElementType, 21));
    return <any>type;
}

export function setArgType(arg: JSMarshalerArgument, type: MarshalerType): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapU8(<any>arg + slotOffset(JSMarshalerArgumentOffsets.Type, 20), type);
}

export function setArgElementType(arg: JSMarshalerArgument, type: MarshalerType): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapU8(<any>arg + slotOffset(JSMarshalerArgumentOffsets.ElementType, 21), type);
}

export function getArgBool(arg: JSMarshalerArgument): boolean {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapB8(<any>arg);
}

export function getArgU8(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapU8(<any>arg);
}

export function getArgU16(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapU16(<any>arg);
}

export function getArgI16(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapI16(<any>arg);
}

export function getArgI32(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapI32(<any>arg);
}

export function getArgIntptr(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return Module.dotnetWasm64 ? fixupPointer(BigInt.asUintN(64, dotnetApi.getHeapI64Big(<any>arg)), 0) : dotnetApi.getHeapU32(<any>arg);
}

export function getArgI52(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    // we know that the range check and conversion from Int64 was be done on C# side
    return dotnetApi.getHeapF64(<any>arg);
}

export function getArgI64Big(arg: JSMarshalerArgument): bigint {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapI64Big(<any>arg);
}

export function getArgDate(arg: JSMarshalerArgument): Date {
    dotnetAssert.check(arg, "Null arg");
    const unixTime = dotnetApi.getHeapF64(<any>arg);
    const date = new Date(unixTime);
    return date;
}

export function getArgF32(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapF32(<any>arg);
}

export function getArgF64(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapF64(<any>arg);
}

export function setArgBool(arg: JSMarshalerArgument, value: boolean): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetAssert.fastCheck(typeof value === "boolean", () => `Value is not a Boolean: ${value} (${typeof (value)})`);
    dotnetApi.setHeapB8(<any>arg, value);
}

export function setArgU8(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapU8(<any>arg, value);
}

export function setArgU16(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapU16(<any>arg, value);
}

export function setArgI16(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapI16(<any>arg, value);
}

export function setArgI32(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapI32(<any>arg, value);
}

export function setArgIntptr(arg: JSMarshalerArgument, value: VoidPtr): void {
    dotnetAssert.check(arg, "Null arg");
    if (Module.dotnetWasm64) {
        dotnetApi.setHeapI64Big(<any>arg, BigInt(fixupPointer(value, 0)));
    } else {
        dotnetApi.setHeapU32(<any>arg, <any>value);
    }
}

export function setArgI52(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetAssert.fastCheck(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    // we know that conversion to Int64 would be done on C# side
    dotnetApi.setHeapF64(<any>arg, value);
}

export function setArgI64Big(arg: JSMarshalerArgument, value: bigint): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapI64Big(<any>arg, value);
}

const minDateUnixTime = -0x3883122CD800;
const maxDateUnixTime = 0xE677D21FDBFF;
export function setArgDate(arg: JSMarshalerArgument, value: Date): void {
    dotnetAssert.check(arg, "Null arg");
    // getTime() is always UTC
    const unixTime = value.getTime();
    dotnetAssert.check(unixTime >= minDateUnixTime && unixTime <= maxDateUnixTime, `Overflow: value ${value.toISOString()} is out of ${new Date(minDateUnixTime).toISOString()} ${new Date(maxDateUnixTime).toISOString()} range`);
    dotnetApi.setHeapF64(<any>arg, unixTime);
}

export function setArgF64(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapF64(<any>arg, value);
}

export function setArgF32(arg: JSMarshalerArgument, value: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapF32(<any>arg, value);
}

export function getArgJsHandle(arg: JSMarshalerArgument): JSHandle {
    dotnetAssert.check(arg, "Null arg");
    return <any>getHandle(<any>arg + slotOffset(JSMarshalerArgumentOffsets.JSHandle, 8));
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setArgProxyContext(arg: JSMarshalerArgument): void {
    /*TODO-WASM threads only
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapI32(<any>arg + JSMarshalerArgumentOffsets.ContextHandle, <any>jsInteropState.proxyGCHandle);
    */
}

export function setJsHandle(arg: JSMarshalerArgument, jsHandle: JSHandle): void {
    dotnetAssert.check(arg, "Null arg");
    setHandle(<any>arg + slotOffset(JSMarshalerArgumentOffsets.JSHandle, 8), <any>jsHandle);
    setArgProxyContext(arg);
}

export function getArgGcHandle(arg: JSMarshalerArgument): GCHandle {
    dotnetAssert.check(arg, "Null arg");
    return <any>getHandle(<any>arg + slotOffset(JSMarshalerArgumentOffsets.GCHandle, 8));
}

export function setGcHandle(arg: JSMarshalerArgument, gcHandle: GCHandle): void {
    dotnetAssert.check(arg, "Null arg");
    setHandle(<any>arg + slotOffset(JSMarshalerArgumentOffsets.GCHandle, 8), <any>gcHandle);
    setArgProxyContext(arg);
}

export function getArgLength(arg: JSMarshalerArgument): number {
    dotnetAssert.check(arg, "Null arg");
    return dotnetApi.getHeapI32(<any>arg + slotOffset(JSMarshalerArgumentOffsets.Length, 16));
}

export function setArgLength(arg: JSMarshalerArgument, size: number): void {
    dotnetAssert.check(arg, "Null arg");
    dotnetApi.setHeapI32(<any>arg + slotOffset(JSMarshalerArgumentOffsets.Length, 16), size);
}

export function getSignatureMarshaler(signature: JSFunctionSignature, index: number): JSHandle {
    dotnetAssert.check(signature, "Null signatures");
    const sig = getSig(signature, index);
    return <any>dotnetApi.getHeapU32(<any>sig + JSBindingHeaderOffsets.ImportHandle);
}

export function arrayElementSize(elementType: MarshalerType): number {
    if (elementType == MarshalerType.Byte) return 1;
    if (elementType == MarshalerType.Int32) return 4;
    if (elementType == MarshalerType.Int52) return 8;
    if (elementType == MarshalerType.Single) return 4;
    if (elementType == MarshalerType.Double) return 8;
    if (elementType == MarshalerType.String) return marshalerArgSize();
    if (elementType == MarshalerType.Object) return marshalerArgSize();
    if (elementType == MarshalerType.JSObject) return marshalerArgSize();
    return -1;
}

