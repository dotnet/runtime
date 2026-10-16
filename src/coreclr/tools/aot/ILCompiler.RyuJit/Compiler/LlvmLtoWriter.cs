// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using ILCompiler.DependencyAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal sealed class LlvmLtoWriter
    {
        private readonly RyuJitCompilation _compilation;
        private readonly Dictionary<MethodDesc, MethodCodeNode> _nodes;
        private readonly Dictionary<MethodDesc, Export> _exports = new();
        private readonly HashSet<MethodDesc> _pending = new();
        private readonly HashSet<MethodDesc> _rejected = new();
        private readonly Dictionary<MethodDesc, string> _addresses = new();

        internal readonly SortedDictionary<string, string> Intrinsics = new(StringComparer.Ordinal);

        internal sealed record Export(MethodCodeNode Node, ScalarType ReturnType, ScalarType[] Parameters, string Body)
        {
            public string Symbol;
        }

        internal sealed class UnsupportedException(string reason) : Exception(reason);

        private LlvmLtoWriter(RyuJitCompilation compilation, IEnumerable<MethodCodeNode> methods)
        {
            _compilation = compilation;
            _nodes = methods.ToDictionary(node => node.Method);
        }

        public static void Write(string path, RyuJitCompilation compilation, IEnumerable<MethodCodeNode> methods)
        {
            string targetTriple = GetTargetTriple(compilation.TypeSystemContext.Target);
            var exporter = new LlvmLtoWriter(compilation, methods);
            foreach (MethodDesc method in exporter._nodes.Keys)
                if (method is EcmaMethod ecma && compilation.TypeSystemContext.InputFilePaths.ContainsKey(ecma.Module.Assembly.GetName().Name))
                    exporter.TryExport(method);

            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("; NativeAOT optimization IR. Link with RyuJIT object.");
                writer.WriteLine($"target triple = \"{targetTriple}\"");

                foreach (string declaration in exporter.Intrinsics.Values)
                    writer.WriteLine(declaration);

                foreach (var address in exporter._addresses.OrderBy(pair => pair.Value, StringComparer.Ordinal))
                {
                    if (!exporter._exports.ContainsKey(address.Key))
                    {
                        MethodSignature signature = address.Key.Signature;
                        bool managed = !address.Key.IsUnmanagedCallersOnly;
                        string types = string.Join(", ", Enumerable.Range(0, signature.Length).Select(i =>
                            (managed ? ScalarType.From(signature[i]).AbiType : ScalarType.From(signature[i])).IR));
                        ScalarType returnType = ScalarType.From(signature.ReturnType, allowVoid: true);
                        writer.WriteLine($"declare {(managed ? returnType.AbiType : returnType).IR} {Quote(address.Value)}({types})");
                    }
                }

                foreach (Export export in exporter._exports.Values.OrderBy(e => e.Symbol, StringComparer.Ordinal))
                {
                    string parameters = string.Join(", ", export.Parameters.Select((type, index) => $"{type.AbiType.IR} %arg{index}"));
                    writer.WriteLine($"define available_externally {export.ReturnType.AbiType.IR} {Quote(export.Symbol)}({parameters}) {{\n{export.Body}}}\n");
                }
            }

            if (compilation.Logger.IsVerbose)
                compilation.Logger.LogMessage($"Wrote {exporter._exports.Count} methods to LLVM LTO IR '{path}'.");
        }

        private static string GetTargetTriple(TargetDetails target)
        {
            string architecture = target.Architecture switch
            {
                TargetArchitecture.X64 => "x86_64",
                TargetArchitecture.ARM64 => "aarch64",
                _ => throw new NotSupportedException("LLVM LTO exports currently support x64 and arm64 calling conventions."),
            };
            string platform = target.OperatingSystem switch
            {
                TargetOS.Windows => "pc-windows-msvc",
                TargetOS.Linux => "unknown-linux-gnu",
                TargetOS.OSX => "apple-macosx",
                TargetOS.FreeBSD => "unknown-freebsd",
                TargetOS.NetBSD => "unknown-netbsd",
                TargetOS.OpenBSD => "unknown-openbsd",
                TargetOS.SunOS => "pc-solaris",
                TargetOS.iOS => "apple-ios",
                TargetOS.iOSSimulator => "apple-ios-simulator",
                TargetOS.tvOS => "apple-tvos",
                TargetOS.tvOSSimulator => "apple-tvos-simulator",
                TargetOS.MacCatalyst => "apple-ios-macabi",
                _ => throw new NotSupportedException($"Unsupported LLVM LTO target OS: {target.OperatingSystem}"),
            };
            return $"{architecture}-{platform}";
        }

        internal Export GetCallee(MethodDesc method) => TryExport(method) ?? throw new UnsupportedException($"Call target is not a verified scalar method: {method}");

        internal string GetAddress(MethodDesc method)
        {
            if (method.HasInstantiation || method.OwningType.HasInstantiation || !method.Signature.IsStatic || !_nodes.TryGetValue(method, out MethodCodeNode node))
                throw new UnsupportedException("Function pointer needs an emitted, non-generic static target");

            ScalarType.From(method.Signature.ReturnType, allowVoid: true);
            for (int i = 0; i < method.Signature.Length; i++)
                ScalarType.From(method.Signature[i]);

            // Use one IR identity for each function. Runtime exports have a native
            // alias at the same address; prefer it for both calls and ldftn. LLD's
            // prevailing-native-symbol handling does not import an available_externally
            // alias body, and describing both names as independent functions is unsound.
            var alias = _compilation.NodeFactory.GetSymbolAlternateName(node, out bool hidden);
            string symbol = !alias.IsNull && !hidden ? alias.ToString() : node.GetMangledName(_compilation.NameMangler);
            _addresses[method] = symbol;
            return symbol;
        }

        private Export TryExport(MethodDesc method)
        {
            if (_exports.TryGetValue(method, out Export existing))
                return existing;

            if (_rejected.Contains(method) || _pending.Contains(method))
                return null;

            _pending.Add(method);

            try
            {
                if (!_nodes.TryGetValue(method, out MethodCodeNode node) || method is not EcmaMethod ||
                    method.HasInstantiation || method.OwningType.HasInstantiation ||
                    method.IsUnmanagedCallersOnly || method.IsPInvoke || method.IsInternalCall || method.IsIntrinsic ||
                    method.IsNoInlining || method.IsNoOptimization || method.IsSynchronized || method.IsAsync || method.RequireSecObject ||
                    method.OwningType.HasStaticConstructor || method.Signature.Flags != MethodSignatureFlags.Static ||
                    node.ShouldSkipEmittingObjectNode(_compilation.NodeFactory) || node.EHInfo != null)
                    throw new UnsupportedException("Unsupported calling, initialization, or optimization semantics");

                MethodIL il = _compilation.GetMethodIL(method);
                if (il == null || il.GetExceptionRegions().Length != 0)
                    throw new UnsupportedException("Missing IL or exception regions");

                var translator = new LlvmLtoILTranslator(this, method, il);
                string body = translator.Translate();

                ObjectNode.ObjectData code = node.GetData(_compilation.NodeFactory, relocsOnly: false);
                if (code == null)
                    throw new UnsupportedException("Missing RyuJIT body");

                foreach (Relocation relocation in code.Relocs ?? [])
                {
                    // Switch dispatch can use relocations to offsets in this body.
                    // Recursive IL calls have already failed the dependency check.
                    if (ReferenceEquals(relocation.Target, node))
                        continue;
                    if (relocation.Target is IMethodNode target && translator.References.Contains(target.Method))
                        continue;
                    if (translator.NativeMathHelpers.Contains(relocation.Target.GetMangledName(_compilation.NameMangler)))
                        continue;
                    if (relocation.Target is MethodReadOnlyDataNode data)
                    {
                        var contents = data.GetData(_compilation.NodeFactory, relocsOnly: false);
                        if ((contents.Relocs ?? []).All(r => ReferenceEquals(r.Target, node)))
                            continue;
                    }

                    // Polls, throw helpers, statics, and instrumentation must keep
                    // their runtime semantics, even if the source IL looks scalar.
                    throw new UnsupportedException($"Unmodelled native relocation: {relocation.Target.GetMangledName(_compilation.NameMangler)}");
                }

                var export = new Export(node, translator.ReturnType, translator.Parameters, body) { Symbol = GetAddress(method) };
                _exports.Add(method, export);
                return export;
            }
            catch (Exception ex) when (ex is UnsupportedException or TypeSystemException)
            {
                _rejected.Add(method);
                if (_compilation.Logger.IsVerbose)
                    _compilation.Logger.LogMessage($"LLVM LTO IR skipped '{method}': {ex.Message}");
                return null;
            }
            finally
            {
                _pending.Remove(method);
            }
        }

        internal static string Quote(string symbol)
        {
            var result = new StringBuilder("@\"");
            foreach (byte value in Encoding.UTF8.GetBytes(symbol))
                if (value >= 0x20 && value <= 0x7e && value != '"' && value != '\\')
                    result.Append((char)value);
                else
                    result.Append('\\').Append(value.ToString("X2", CultureInfo.InvariantCulture));
            return result.Append('"').ToString();
        }
    }

    internal enum ScalarKind { Void, Integer, NativeInt, Float, Pointer }

    internal readonly record struct ScalarType(ScalarKind Kind, int Bits, bool Unsigned = false)
    {
        public string IR => Kind switch
        {
            ScalarKind.Void => "void",
            ScalarKind.Pointer => "ptr",
            ScalarKind.Float => Bits == 32 ? "float" : "double",
            _ => $"i{Bits}",
        };

        public bool IsInteger => Kind is ScalarKind.Integer or ScalarKind.NativeInt;

        // Managed calls normalize small integers in 32-bit registers. The C ABI
        // for a byte/short return alone does not promise those upper bits.
        public ScalarType AbiType => Kind == ScalarKind.Integer && Bits < 32 ? new(ScalarKind.Integer, 32, Unsigned) : this;

        public ScalarType StackType => Kind switch
        {
            ScalarKind.Pointer or ScalarKind.NativeInt => new(ScalarKind.NativeInt, Bits),
            ScalarKind.Integer => new(ScalarKind.Integer, Bits < 32 ? 32 : Bits),
            _ => this,
        };

        public static ScalarType From(TypeDesc type, bool allowVoid = false, bool allowByRef = false)
        {
            type = type.UnderlyingType;
            return type.Category switch
            {
                TypeFlags.Void when allowVoid => new(ScalarKind.Void, 0),
                TypeFlags.Boolean or TypeFlags.Byte => new(ScalarKind.Integer, 8, true),
                TypeFlags.SByte => new(ScalarKind.Integer, 8),
                TypeFlags.Char or TypeFlags.UInt16 => new(ScalarKind.Integer, 16, true),
                TypeFlags.Int16 => new(ScalarKind.Integer, 16),
                TypeFlags.Int32 => new(ScalarKind.Integer, 32),
                TypeFlags.UInt32 => new(ScalarKind.Integer, 32, true),
                TypeFlags.Int64 => new(ScalarKind.Integer, 64),
                TypeFlags.UInt64 => new(ScalarKind.Integer, 64, true),
                TypeFlags.IntPtr => new(ScalarKind.NativeInt, type.Context.Target.PointerSize * 8),
                TypeFlags.UIntPtr => new(ScalarKind.NativeInt, type.Context.Target.PointerSize * 8, true),
                TypeFlags.Single => new(ScalarKind.Float, 32),
                TypeFlags.Double => new(ScalarKind.Float, 64),
                TypeFlags.Pointer or TypeFlags.FunctionPointer => new(ScalarKind.Pointer, type.Context.Target.PointerSize * 8),
                TypeFlags.ByRef when allowByRef => new(ScalarKind.Pointer, type.Context.Target.PointerSize * 8),
                _ => throw new LlvmLtoWriter.UnsupportedException($"Unsupported scalar type: {type}"),
            };
        }
    }
}
