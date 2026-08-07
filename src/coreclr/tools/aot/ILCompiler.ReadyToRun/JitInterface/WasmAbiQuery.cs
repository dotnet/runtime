// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Answers wasm ABI signature questions on stdin, for build tasks that need to agree with what
    /// the compiler will emit but cannot reference the type system themselves.
    /// </summary>
    /// <remarks>
    /// The WebAssembly build tasks compute the signature strings that describe P/Invokes to the
    /// interpreter. Those strings encode struct sizes, which cannot be derived from metadata alone,
    /// so the task asks the compiler rather than keeping a second implementation of the layout rules
    /// that would be free to drift.
    ///
    /// This runs as a mode of crossgen2 rather than as its own tool so that there is exactly one
    /// wasm lowering implementation and one type system configuration. Loading the assembly closure
    /// is the expensive part, so the process stays up for the whole build and answers queries on
    /// stdin instead of being spawned per method.
    ///
    /// Usage:
    ///   crossgen2 --wasm-abi-query --targetos &lt;browser|wasi&gt; --targetarch wasm &lt;assembly&gt;...
    ///
    /// Each stdin line is one query, and each reply line is either the answer or '!' followed by an
    /// error message. Two query forms are supported:
    ///
    ///   t &lt;assemblySimpleName&gt; &lt;typeToken&gt;
    ///     Replies with the ABI encoding of a type in parameter position ('i', 'l', 'f', 'd', 'V' or
    ///     "S&lt;size&gt;").
    ///
    ///   m &lt;assemblySimpleName&gt; &lt;methodToken&gt; &lt;loweringFlags&gt;
    ///     Replies with the full signature string of a method. Preferred over per-parameter type
    ///     queries: the parameter types come from the method's signature blob, so generic
    ///     instantiations resolve even though they have no metadata token of their own and so cannot
    ///     be named over the wire.
    ///
    /// Tokens are decimal or 0x-prefixed hexadecimal; flags are a decimal LoweringFlags value.
    /// </remarks>
    public static class WasmAbiQuery
    {
        public static int Run(ReadyToRunCompilerContext context, TextReader input, TextWriter output)
        {
            ConfigureCompilationGroup(context);

            // Tells the caller the closure loaded, so a startup failure is not mistaken for a
            // failure of the first query.
            output.WriteLine("ready");
            output.Flush();

            string line;
            while ((line = input.ReadLine()) is not null)
            {
                if (line.Length == 0)
                    continue;

                string reply;
                try
                {
                    reply = Answer(context, line);
                }
                catch (Exception ex)
                {
                    // A malformed query is the caller's fault and the message says everything; anything
                    // else is a bug in here, and whoever reads the build log needs the stack to act on it.
                    string detail = ex is FormatException or ArgumentException ? ex.Message : ex.ToString();
                    reply = "!" + detail.Replace('\r', ' ').Replace('\n', ' ');
                }

                output.WriteLine(reply);
                output.Flush();
            }

            return 0;
        }

        /// <summary>
        /// The ReadyToRun field layout algorithm asks the compilation group whether a derived type
        /// needs its base offset aligned, so a context without a group throws before computing any
        /// layout.
        /// </summary>
        /// <remarks>
        /// Every input goes into a single version bubble. The alignment the group would otherwise
        /// introduce exists to keep offsets baked into precompiled code valid across a version
        /// boundary, and there is no precompiled code here: the interpreter loads these assemblies
        /// and computes their layout itself. One bubble is what reports that layout.
        /// </remarks>
        private static void ConfigureCompilationGroup(ReadyToRunCompilerContext context)
        {
            List<EcmaModule> modules = new();
            foreach (string simpleName in context.InputFilePaths.Keys)
            {
                modules.Add(context.GetModuleForSimpleName(simpleName));
            }

            context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
            {
                Context = context,
                IsInputBubble = true,
                CompilationModuleSet = modules,
                VersionBubbleModuleSet = modules,
                CrossModuleInlineable = Array.Empty<ModuleDesc>(),
                InstructionSetSupport = context.InstructionSetSupport,
            }));
        }

        private static string Answer(CompilerTypeSystemContext context, string query)
        {
            if (query.Length < 2 || query[1] != ' ')
                throw new FormatException($"Malformed query '{query}'; expected a 't' or 'm' verb.");

            string rest = query.Substring(2);

            // Parsed right to left so that the assembly name, which is whatever is left over, is not
            // assumed to be free of spaces.
            switch (query[0])
            {
                case 't':
                {
                    (string assemblyName, int typeToken) = SplitToken(rest, query);
                    TypeDesc type = GetModule(context, assemblyName).GetType(MetadataTokens.EntityHandle(typeToken));

                    return GetAbiToken(type);
                }

                case 'm':
                {
                    (string head, int flags) = SplitToken(rest, query);
                    (string assemblyName, int methodToken) = SplitToken(head, query);

                    return GetMethodSignature(context, assemblyName, methodToken, flags);
                }

                default:
                    throw new FormatException($"Unrecognized query verb '{query[0]}'.");
            }
        }

        private static string GetMethodSignature(CompilerTypeSystemContext context, string assemblySimpleName, int methodToken, int flags)
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

            MethodDesc method = GetModule(context, assemblySimpleName).GetMethod(MetadataTokens.EntityHandle(methodToken));

            return WasmLowering.GetSignature(method.Signature, (WasmLowering.LoweringFlags)flags).SignatureString;
        }

        /// <summary>
        /// Gets the signature encoding for a type in parameter position: a primitive character
        /// (<c>i</c>, <c>l</c>, <c>f</c>, <c>d</c>, <c>V</c>) or <c>S&lt;size&gt;</c> for a struct that
        /// is passed by reference.
        /// </summary>
        private static string GetAbiToken(TypeDesc type)
        {
            TypeDesc loweredType = WasmLowering.LowerToAbiType(type);
            if (loweredType is null)
            {
                // Passed by reference; the size is what the callee needs to know.
                return string.Create(null, stackalloc char[16], $"S{type.GetElementSize().AsInt}");
            }

            return WasmLowering.WasmValueTypeToSigChar(WasmLowering.LowerType(loweredType)).ToString();
        }

        private static EcmaModule GetModule(CompilerTypeSystemContext context, string assemblySimpleName)
        {
            // Resolved by simple name plus metadata token rather than by name: name-based lookup
            // would have to reproduce nested-type and generic name mangling, and would silently pick
            // the wrong member when it got that wrong, whereas a token cannot be ambiguous.
            return context.GetModuleForSimpleName(assemblySimpleName);
        }

        private static (string Head, int Value) SplitToken(string text, string query)
        {
            int separator = text.LastIndexOf(' ');
            if (separator < 0)
                throw new FormatException($"Malformed query '{query}'; not enough fields.");

            return (text.Substring(0, separator), ParseToken(text.Substring(separator + 1)));
        }

        private static int ParseToken(string text)
        {
            return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? int.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : int.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
