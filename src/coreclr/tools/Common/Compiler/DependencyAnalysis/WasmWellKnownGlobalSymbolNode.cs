// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    // Fixed wasm global indices, matching the ABI shared with the object writer
    // (see WasmAbiConstants / WasmObjectWriter and the JIT's emitwasm.cpp, as well as the WebCIL spec).
    public enum WasmWellKnownGlobal
    {
        StackPointer = 0,
        ImageBase = 1,
        TableBase = 2,
    }

    public static class WasmWellKnownGlobalExtensions
    {
        private static Utf8String StackPointerUtf8String = new Utf8String("__stack_pointer"u8);
        private static Utf8String MemoryBaseUtf8String = new Utf8String("__memory_base"u8);
        private static Utf8String TableBaseUtf8String = new Utf8String("__table_base"u8);

        extension(WasmWellKnownGlobal global)
        {
            public string ToSymbolString()
            {
                return global switch
                {
                    WasmWellKnownGlobal.StackPointer => "__stack_pointer",
                    WasmWellKnownGlobal.ImageBase => "__memory_base",
                    WasmWellKnownGlobal.TableBase => "__table_base",
                    _ => throw new UnreachableException()
                };
            }

            public Utf8String ToSymbolName()
            {
                return global switch
                {
                    WasmWellKnownGlobal.StackPointer => StackPointerUtf8String,
                    WasmWellKnownGlobal.ImageBase => MemoryBaseUtf8String,
                    WasmWellKnownGlobal.TableBase => TableBaseUtf8String,
                    _ => throw new UnreachableException()
                };
            }

            public static WasmWellKnownGlobal FromSymbolName(string symbolName)
            {
                return symbolName switch
                {
                    "__stack_pointer" => WasmWellKnownGlobal.StackPointer,
                    "__memory_base" => WasmWellKnownGlobal.ImageBase,
                    "__table_base" => WasmWellKnownGlobal.TableBase,
                    _ => throw new UnreachableException()
                };
            }
            public static WasmWellKnownGlobal FromSymbolName(Utf8String symbolName) => FromSymbolName(symbolName.ToString());
        }
    }

    /// <summary>
    /// Represents one of the well-known wasm globals referenced by JIT-generated code.
    /// These are imported globals whose final index is assigned by the ObjectWriter / wasm linker.
    /// crossgen2/R2R resolves it back to the fixed index defined in the WebCIL format, while a relocatable
    /// NativeAOT object emits it as an undefined imported global for wasm-ld to resolve.
    /// </summary>
    public class WasmWellKnownGlobalSymbolNode(WasmWellKnownGlobal _global) : ExternSymbolNode(_global.ToSymbolName())
    {
        public override int ClassCode => -767379803;

        protected override string GetName(NodeFactory factory) => $"WasmWellKnownGlobal {this.ToString()}";
    }
}
