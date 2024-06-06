// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace DotnetFuzzing.Fuzzers;

internal sealed class TextEncodingNetFramework : IFuzzer
{
    const string TestSubDirectory = @"src\libraries\Fuzzing\DotnetFuzzing\Fuzzers\TextEncoding.NetFramework\bin\Release\TextEncodingNetFramework.exe";

    string[] IFuzzer.TargetAssemblies => ["mscorlib"]; // Not sure if this does anything.
    string[] IFuzzer.TargetCoreLibPrefixes { get; } = [];

    void IFuzzer.FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        string path = GetRepoRootDirectory();
        path = Path.Combine(path, TestSubDirectory);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Cannot find the .NET Framework test executable at {path}");
        }

        string encoded = Convert.ToBase64String(bytes);
        Process.Start(path, encoded);
    }

    private static string GetRepoRootDirectory()
    {
        string? currentDirectory = Directory.GetCurrentDirectory();

        while (currentDirectory != null)
        {
            string gitDirOrFile = Path.Combine(currentDirectory, ".git");
            if (Directory.Exists(gitDirOrFile) || File.Exists(gitDirOrFile))
            {
                break;
            }
            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        if (currentDirectory == null)
        {
            throw new Exception("Cannot find the git repository root");
        }

        return currentDirectory;
    }
}
