// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks;

public abstract class EmitWasmBundleBase : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private CancellationTokenSource BuildTaskCancelled { get; } = new();

    /// Must have DestinationFile metadata, which is the output filename
    /// Could have RegisteredName, otherwise it would be the filename.
    /// RegisteredName should be prefixed with namespace in form of unix like path. For example: "/usr/share/zoneinfo/"
    [Required]
    public ITaskItem[] FilesToBundle { get; set; } = default!;

    [Required]
    public string BundleName { get; set; } = default!;

    [Required]
    public string BundleFile { get; set; } = default!;

    [Required]
    public string RegistrationCallbackFunctionName { get; set; } = default!;

    public override bool Execute()
    {
        // The DestinationFile (output filename) already includes a content hash. Grouping by this filename therefore
        // produces one group per file-content. We only want to emit one copy of each file-content, and one symbol for it.
        var filesToBundleByDestinationFileName = FilesToBundle.GroupBy(f => f.GetMetadata("DestinationFile")).ToList();

        // We're handling the incrementalism within this task, because it needs to be based on file content hashes
        // and not on timetamps. The output filenames contain a content hash, so if any such file already exists on
        // disk with that name, we know it must be up-to-date.
        var remainingDestinationFilesToBundle = filesToBundleByDestinationFileName.Where(g => !File.Exists(g.Key)).ToArray();

        // If you're only touching the leaf project, we don't really need to tell you that.
        // But if there's more work to do it's valuable to show progress.
        var verbose = remainingDestinationFilesToBundle.Length > 1;
        var verboseCount = 0;

        var filesToBundleByRegisteredName = FilesToBundle.GroupBy(file => {
            var registeredName = file.GetMetadata("RegisteredName");
            if(string.IsNullOrEmpty(registeredName))
            {
                registeredName = Path.GetFileName(file.ItemSpec);
            }
            return registeredName;
        }).ToList();

        var files = filesToBundleByRegisteredName.Select(group => {
            var registeredFile = group.First();
            var outputFile = registeredFile.GetMetadata("DestinationFile");
            var registeredName = group.Key;
            var symbolName = ToSafeSymbolName(outputFile);
            return (registeredName, symbolName);
        }).ToList();

        Log.LogMessage(MessageImportance.Low, $"Bundling {files.Count} files for {BundleName}");

        if (remainingDestinationFilesToBundle.Length > 0)
        {
            int allowedParallelism = Math.Max(Math.Min(remainingDestinationFilesToBundle.Length, Environment.ProcessorCount), 1);
            if (BuildEngine is IBuildEngine9 be9)
                allowedParallelism = be9.RequestCores(allowedParallelism);

            Parallel.For(0, remainingDestinationFilesToBundle.Length, new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism, CancellationToken = BuildTaskCancelled.Token }, (i, state) =>
            {
                var group = remainingDestinationFilesToBundle[i];

                // Since the object filenames include a content hash, we can pick an arbitrary ITaskItem from each group,
                // since we know each group's ITaskItems all contain the same binary data
                var contentSourceFile = group.First();

                var outputFile = group.Key;
                var inputFile = contentSourceFile.ItemSpec;
                if (verbose)
                {
                    var registeredName = contentSourceFile.GetMetadata("RegisteredName");
                    if(string.IsNullOrEmpty(registeredName))
                    {
                        registeredName = Path.GetFileName(inputFile);
                    }
                    var count = Interlocked.Increment(ref verboseCount);
                    Log.LogMessage(MessageImportance.Low, "{0}/{1} Bundling {2} ...", count, remainingDestinationFilesToBundle.Length, registeredName);
                }

                Log.LogMessage(MessageImportance.Low, "Bundling {0} as {1}", inputFile, outputFile);
                var symbolName = ToSafeSymbolName(outputFile);
                if (!Emit(outputFile, (codeStream) => {
                    using var inputStream = File.OpenRead(inputFile);
                    BundleFileToCSource(symbolName, inputStream, codeStream);
                }))
                {
                    state.Stop();
                }
            });
        }

        return Emit(BundleFile, (inputStream) =>
        {
            using var outputUtf8Writer = new StreamWriter(inputStream, Utf8NoBom);
            GenerateRegisteredBundledObjects($"mono_wasm_register_{BundleName}_bundle", RegistrationCallbackFunctionName, files, outputUtf8Writer);
        }) && !Log.HasLoggedErrors;
    }

    public void Cancel()
    {
        BuildTaskCancelled.Cancel();
    }

    #region Helpers

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly byte[] HexToUtf8Lookup = InitLookupTable();
    private static readonly byte[] NewLineAndIndentation = new[] { (byte)0x0a, (byte)0x20, (byte)0x20 };

    private static byte[] InitLookupTable()
    {
        // Every 6 bytes in this array represents the output for a different input byte value.
        // For example, the input byte 0x1a (26 decimal) corresponds to bytes 156-161 (26*6=156),
        // whose values will be ['0', 'x', '1', 'a', ',', ' '], which is the UTF-8 representation
        // for "0x1a, ". This is just a faster alternative to calling .ToString("x2") on every
        // byte of the input file and then pushing that string through UTF8Encoding.
        var lookup = new byte[256 * 6];
        for (int i = 0; i < 256; i++)
        {
            string byteAsHex = i.ToString("x2");
            char highOrderChar = BitConverter.IsLittleEndian ? byteAsHex[0] : byteAsHex[1];
            char lowOrderChar = BitConverter.IsLittleEndian ? byteAsHex[1] : byteAsHex[0];
            lookup[i * 6 + 0] = (byte)'0';
            lookup[i * 6 + 1] = (byte)'x';
            lookup[i * 6 + 2] = (byte)highOrderChar;
            lookup[i * 6 + 3] = (byte)lowOrderChar;
            lookup[i * 6 + 4] = (byte)',';
            lookup[i * 6 + 5] = (byte)' ';
        }

        return lookup;
    }

    public abstract bool Emit(string destinationFile, Action<Stream> inputProvider);

    public static void GenerateRegisteredBundledObjects(string newFunctionName, string callbackFunctionName, ICollection<(string registeredName, string symbol)> files, StreamWriter outputUtf8Writer)
    {
        outputUtf8Writer.WriteLine($"int {callbackFunctionName}(const char* name, const unsigned char* data, unsigned int size);");
        outputUtf8Writer.WriteLine();

        foreach (var tuple in files)
        {
            outputUtf8Writer.WriteLine($"extern const unsigned char {tuple.symbol}[];");
            outputUtf8Writer.WriteLine($"extern const int {tuple.symbol}_len;");
        }

        outputUtf8Writer.WriteLine();
        outputUtf8Writer.WriteLine($"void {newFunctionName}() {{");

        foreach (var tuple in files)
        {
            outputUtf8Writer.WriteLine($"  {callbackFunctionName} (\"{tuple.registeredName}\", {tuple.symbol}, {tuple.symbol}_len);");
        }

        outputUtf8Writer.WriteLine("}");
    }

    private static void BundleFileToCSource(string symbolName, FileStream inputStream, Stream outputStream)
    {
        // Emits a C source file in the same format as "xxd --include". Example:
        //
        // unsigned char Some_File_dll[] = {
        //   0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x0a
        // };
        // unsigned int Some_File_dll_len = 6;

        var buf = new byte[4096];
        int bytesRead;
        var generatedArrayLength = 0;
        var bytesEmitted = 0;

        using var outputUtf8Writer = new StreamWriter(outputStream, Utf8NoBom);

        outputUtf8Writer.Write($"unsigned char {symbolName}[] = {{");
        outputUtf8Writer.Flush();
        while ((bytesRead = inputStream.Read(buf, 0, buf.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
            {
                if (bytesEmitted++ % 12 == 0)
                {
                    outputStream.Write(NewLineAndIndentation, 0, NewLineAndIndentation.Length);
                }

                var byteValue = buf[i];
                outputStream.Write(HexToUtf8Lookup, byteValue * 6, 6);
            }

            generatedArrayLength += bytesRead;
        }

        outputUtf8Writer.WriteLine("0\n};");
        outputUtf8Writer.WriteLine($"unsigned int {symbolName}_len = {generatedArrayLength};");
        outputUtf8Writer.Flush();
        outputStream.Flush();
    }

    private static string ToSafeSymbolName(string destinationFileName)
    {
        // Since destinationFileName includes a content hash, we can safely strip off the directory name
        // as the filename is always unique enough. This avoid disclosing information about the build
        // file structure in the resulting symbols.
        var filename = Path.GetFileName(destinationFileName);

        // Equivalent to the logic from "xxd --include"
        var sb = new StringBuilder();
        foreach (var c in filename)
        {
            sb.Append(IsAlphanumeric(c) ? c : '_');
        }

        return sb.ToString();
    }

    // Equivalent to "isalnum"
    private static bool IsAlphanumeric(char c) => c
        is (>= 'a' and <= 'z')
        or (>= 'A' and <= 'Z')
        or (>= '0' and <= '9');

    #endregion

}
