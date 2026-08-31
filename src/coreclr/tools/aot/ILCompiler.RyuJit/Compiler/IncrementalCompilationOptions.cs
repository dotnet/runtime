// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal readonly struct IncrementalUpdateRequest
    {
        internal IncrementalUpdateRequest(string updatedAssemblyPath, string outputObjectPath)
        {
            UpdatedAssemblyPath = updatedAssemblyPath;
            OutputObjectPath = outputObjectPath;
        }

        internal string UpdatedAssemblyPath { get; }
        internal string OutputObjectPath { get; }
    }

    internal readonly struct IncrementalUpdateResult
    {
        internal IncrementalUpdateResult(
            bool succeeded,
            string reason,
            int changedMethodCount,
            int recompiledMethodCount,
            long patchedByteCount)
        {
            Succeeded = succeeded;
            Reason = reason;
            ChangedMethodCount = changedMethodCount;
            RecompiledMethodCount = recompiledMethodCount;
            PatchedByteCount = patchedByteCount;
        }

        internal bool Succeeded { get; }
        internal bool RequiresCleanCompilation => !Succeeded;
        internal string Reason { get; }
        internal int ChangedMethodCount { get; }
        internal int RecompiledMethodCount { get; }
        internal long PatchedByteCount { get; }
    }

    internal sealed class IncrementalCompilationOptions
    {
        private IncrementalCompilationOptions(IncrementalUpdateRequest[] updates)
        {
            Updates = updates;
        }

        internal IncrementalUpdateRequest[] Updates { get; }

        internal static bool IsEnvironmentRequested =>
            Environment.GetEnvironmentVariable(IncrementalFailureContract.EnableVariable) is not null ||
            Environment.GetEnvironmentVariable(IncrementalFailureContract.UpdatedAssembliesVariable) is not null ||
            Environment.GetEnvironmentVariable(IncrementalFailureContract.OutputObjectsVariable) is not null;

        internal static IncrementalCompilationOptions ReadEnvironment()
        {
            string enabled = Environment.GetEnvironmentVariable(IncrementalFailureContract.EnableVariable);
            string updatedAssemblies = Environment.GetEnvironmentVariable(IncrementalFailureContract.UpdatedAssembliesVariable);
            string outputObjects = Environment.GetEnvironmentVariable(IncrementalFailureContract.OutputObjectsVariable);
            if (enabled is null && updatedAssemblies is null && outputObjects is null)
                return null;
            if (!string.Equals(enabled, "1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{IncrementalFailureContract.EnableVariable} must be exactly '1' when incremental experiment variables are present.");
            }

            if (string.IsNullOrWhiteSpace(updatedAssemblies) ||
                string.IsNullOrWhiteSpace(outputObjects))
            {
                throw new InvalidOperationException(
                    $"The incremental experiment requires {IncrementalFailureContract.UpdatedAssembliesVariable} and " +
                    $"{IncrementalFailureContract.OutputObjectsVariable}.");
            }

            string[] assemblies = updatedAssemblies.Split(
                Path.PathSeparator,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string[] outputs = outputObjects.Split(
                Path.PathSeparator,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (assemblies.Length == 0 || assemblies.Length != outputs.Length)
            {
                throw new InvalidOperationException(
                    "Incremental updated-assembly and output-object lists must have the same nonzero length.");
            }

            var outputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requests = new IncrementalUpdateRequest[assemblies.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                string assemblyPath;
                string outputPath;
                try
                {
                    assemblyPath = Path.GetFullPath(assemblies[i]);
                    outputPath = Path.GetFullPath(outputs[i]);
                }
                catch (Exception ex) when (ex is ArgumentException or
                    IOException or
                    UnauthorizedAccessException or
                    NotSupportedException or
                    System.Security.SecurityException)
                {
                    throw new InvalidOperationException(
                        $"An incremental input or output path is invalid: {ex.Message}",
                        ex);
                }

                if (!outputPaths.Add(outputPath))
                {
                    throw new InvalidOperationException(
                        $"The incremental output path occurs more than once: '{outputPath}'.");
                }

                requests[i] = new IncrementalUpdateRequest(assemblyPath, outputPath);
            }

            return new IncrementalCompilationOptions(requests);
        }
    }

    internal static class IncrementalCompilationFingerprint
    {
        internal static bool TryCreate(
            CompilerTypeSystemContext context,
            InstructionSetSupport instructionSetSupport,
            string configurationDescription,
            out byte[] hash,
            out string reason)
        {
            hash = null;
            reason = null;

            try
            {
                using var data = new MemoryStream();
                using (var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write("NativeAOT-incremental-fast-coff-v1");
                    TargetDetails target = context.Target;
                    writer.Write((int)target.Architecture);
                    writer.Write((int)target.OperatingSystem);
                    writer.Write((int)target.Abi);
                    writer.Write((int)target.MaximumSimdVectorLength);
                    writer.Write(configurationDescription);
                    WriteInstructionSets(writer, instructionSetSupport.SupportedFlags);
                    WriteInstructionSets(writer, instructionSetSupport.ExplicitlyUnsupportedFlags);
                    WriteInstructionSets(writer, instructionSetSupport.OptimisticFlags);
                    WriteInstructionSets(writer, instructionSetSupport.NonSpecifiableFlags);
                    WriteFiles(writer, "input", context.InputFilePaths);
                    WriteFiles(writer, "reference", context.ReferenceFilePaths);
                    WriteRelevantEnvironment(writer);
                }

                hash = SHA256.HashData(data.GetBuffer().AsSpan(0, checked((int)data.Length)));
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                reason = $"configuration-fingerprint-failed:{ex.Message}";
                return false;
            }
        }

        private static void WriteInstructionSets(BinaryWriter writer, InstructionSetFlags flags)
        {
            var values = new List<int>();
            foreach (InstructionSet instructionSet in flags)
                values.Add((int)instructionSet);

            values.Sort();
            writer.Write(values.Count);
            foreach (int value in values)
                writer.Write(value);
        }

        private static void WriteFiles(
            BinaryWriter writer,
            string role,
            IReadOnlyDictionary<string, string> files)
        {
            var entries = new List<KeyValuePair<string, string>>(files);
            entries.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
            writer.Write(role);
            writer.Write(entries.Count);
            foreach (KeyValuePair<string, string> entry in entries)
            {
                string path = Path.GetFullPath(entry.Value);
                writer.Write(entry.Key);
                writer.Write(path);
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                writer.Write(stream.Length);
                writer.Write(SHA256.HashData(stream));
            }
        }

        private static void WriteRelevantEnvironment(BinaryWriter writer)
        {
            var entries = new List<KeyValuePair<string, string>>();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                string key = (string)entry.Key;
                if ((key.StartsWith("COMPlus_", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase)) &&
                    !key.StartsWith("DOTNET_ILC_INCREMENTAL", StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add(new KeyValuePair<string, string>(key, (string)entry.Value));
                }
            }

            entries.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key));
            writer.Write(entries.Count);
            foreach (KeyValuePair<string, string> entry in entries)
            {
                writer.Write(entry.Key.ToUpperInvariant());
                writer.Write(entry.Value);
            }
        }
    }
}
