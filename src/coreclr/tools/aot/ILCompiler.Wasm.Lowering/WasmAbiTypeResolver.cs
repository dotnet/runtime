// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// Answers "what does this look like in a wasm ABI signature?" for methods and for individual
    /// types, using the same lowering crossgen2 uses, so callers that need to agree with compiled
    /// code do not have to reimplement it.
    /// </summary>
    /// <remarks>
    /// Methods and types are identified by assembly simple name plus metadata token rather than by
    /// name. Name-based lookup would have to reproduce nested-type and generic name mangling, and
    /// would silently pick the wrong member when it got that wrong; a token cannot be ambiguous.
    /// Note that this makes constructed generics unnameable — they have no token — which is why
    /// whole signatures are queried per method rather than a token at a time.
    /// </remarks>
    public sealed class WasmAbiTypeResolver
    {
        private readonly WasmTypeSystemContext _context;

        public WasmAbiTypeResolver(string targetOS, IEnumerable<string> assemblyPaths, string systemModuleName = "System.Private.CoreLib")
        {
            _context = new WasmTypeSystemContext(ParseTargetOS(targetOS));

            foreach (string path in assemblyPaths)
            {
                _context.AddAssemblyPath(path);
            }

            _context.SetSystemModule(_context.GetModuleForSimpleName(systemModuleName));
        }

        private static TargetOS ParseTargetOS(string targetOS) => targetOS?.ToLowerInvariant() switch
        {
            "browser" => TargetOS.Browser,
            "wasi" => TargetOS.Wasi,
            _ => throw new ArgumentException($"Unsupported wasm target OS '{targetOS}'.", nameof(targetOS)),
        };

        /// <summary>
        /// Gets the signature encoding for a single type in parameter position: a primitive character
        /// (<c>i</c>, <c>l</c>, <c>f</c>, <c>d</c>, <c>V</c>) or <c>S&lt;size&gt;</c> for a struct that is
        /// passed by reference.
        /// </summary>
        /// <param name="assemblySimpleName">Simple name of the assembly defining the type.</param>
        /// <param name="metadataToken">The type's metadata token (a TypeDef, TypeRef or TypeSpec token).</param>
        public string GetAbiToken(string assemblySimpleName, int metadataToken)
        {
            var module = (EcmaModule)_context.GetModuleForSimpleName(assemblySimpleName);
            TypeDesc type = module.GetType(MetadataTokens.EntityHandle(metadataToken));

            return GetAbiToken(type);
        }

        /// <summary>
        /// Gets the full wasm signature string for a method, using the same lowering the compiler
        /// applies to the code that will implement or call it.
        /// </summary>
        /// <remarks>
        /// Preferred over per-parameter <see cref="GetAbiToken(string, int)"/> queries: the parameter
        /// types come from the method's signature blob, so generic instantiations resolve here even
        /// though they have no metadata token of their own and cannot be named over the wire.
        /// </remarks>
        /// <param name="assemblySimpleName">Simple name of the assembly defining the method.</param>
        /// <param name="methodToken">The method's MethodDef token.</param>
        /// <param name="flags">A <see cref="WasmLowering.LoweringFlags"/> value.</param>
        public string GetMethodSignature(string assemblySimpleName, int methodToken, int flags)
        {
            // The caller keeps its own copy of LoweringFlags, because a build task cannot reference
            // the type system. Reject bits this build does not define rather than letting a copy that
            // has drifted ahead silently ask for a lowering that is not the one it means.
            const int KnownFlags = (int)(WasmLowering.LoweringFlags.HasGenericContextArg
                | WasmLowering.LoweringFlags.IsAsyncCall
                | WasmLowering.LoweringFlags.IsUnmanagedCallersOnly);

            if ((flags & ~KnownFlags) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(flags), $"Unknown wasm lowering flags 0x{flags:x}; this build understands 0x{KnownFlags:x}.");
            }

            var module = (EcmaModule)_context.GetModuleForSimpleName(assemblySimpleName);
            MethodDesc method = module.GetMethod(MetadataTokens.EntityHandle(methodToken));

            return WasmLowering.GetSignature(method.Signature, (WasmLowering.LoweringFlags)flags).SignatureString;
        }

        /// <summary>
        /// Gets the signature encoding for a type. Public so tests can drive it with types they
        /// resolved themselves.
        /// </summary>
        public static string GetAbiToken(TypeDesc type)
        {
            TypeDesc loweredType = WasmLowering.LowerToAbiType(type);
            if (loweredType is null)
            {
                // Passed by reference; the size is what the callee needs to know.
                return string.Create(null, stackalloc char[16], $"S{type.GetElementSize().AsInt}");
            }

            return WasmLowering.WasmValueTypeToSigChar(WasmLowering.LowerType(loweredType)).ToString();
        }
    }
}
