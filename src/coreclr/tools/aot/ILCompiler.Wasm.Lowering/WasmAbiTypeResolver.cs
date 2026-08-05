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
    /// Answers "what does this type look like in a wasm ABI signature?" using the same lowering
    /// crossgen2 uses, so callers that need to agree with compiled code do not have to reimplement it.
    /// </summary>
    /// <remarks>
    /// Types are identified by assembly simple name plus metadata token rather than by name. Name-based
    /// lookup would have to reproduce nested-type and generic name mangling, and would silently pick
    /// the wrong type when it got that wrong; a token cannot be ambiguous.
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
