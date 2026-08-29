// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// Parameter layout of a generated R2R-to-interpreter thunk. Shared with the tests, which compare
// it against the wasm signature crossgen2 computes for the same key, so it must stay free of
// MSBuild dependencies.
internal static class PortableEntryPointThunkSignature
{
    internal readonly struct Parameter
    {
        public Parameter(string nativeType, string name)
        {
            NativeType = nativeType;
            Name = name;
        }

        public string NativeType { get; }
        public string Name { get; }
    }

    public static bool IsStructToken(string token) => token[0] is 'S' or 'A' && token.Length > 1;

    // A struct argument arrives as the address of its interpreter stack slot.
    public static string DeclType(string token)
        => IsStructToken(token) ? "int8_t*" : SignatureMapper.TokenToNativeType(token);

    /// <summary>
    /// The name of one slot of a multi-slot argument, which occupies several wasm parameters.
    /// </summary>
    public static string MultiSlotParameterName(int argIndex, int slot, int slotCount)
        => slotCount == 2 ? $"arg{argIndex}{(slot == 0 ? "Lo" : "Hi")}" : $"arg{argIndex}_{slot}";

    /// <summary>
    /// The thunk's declared C parameters, in the order crossgen2 passes them:
    /// (callersStackPointer, [this], retBuf, args..., portableEntrypoint). The stack pointer comes
    /// from the WASM_CALLABLE_FUNC macro and the entrypoint is appended by the caller, so neither
    /// appears here.
    /// </summary>
    public static List<Parameter> GetDeclaredParameters(IReadOnlyList<string> args, bool isStructReturn)
    {
        var parameters = new List<Parameter>(args.Count + 1);
        for (int i = 0; i < args.Count; i++)
        {
            if (SignatureMapper.TryGetMultiSlotToken(args[i], out int slotCount))
            {
                string slotType = SignatureMapper.MultiSlotElementNativeType(args[i]);
                for (int slot = 0; slot < slotCount; slot++)
                    parameters.Add(new Parameter(slotType, MultiSlotParameterName(i, slot, slotCount)));
            }
            else
            {
                parameters.Add(new Parameter(DeclType(args[i]), $"arg{i}"));
            }
        }

        if (isStructReturn)
        {
            // The hidden return buffer follows the 'this' pointer rather than preceding it.
            // See WasmR2RToInterpreterThunkNode.EmitCode: retBufLocalIndex = 1 + (hasThis ? 1 : 0).
            int retBufIndex = args.Count > 0 && args[0] == "T" ? 1 : 0;
            parameters.Insert(retBufIndex, new Parameter("int8_t*", "retBuf"));
        }

        return parameters;
    }
}
