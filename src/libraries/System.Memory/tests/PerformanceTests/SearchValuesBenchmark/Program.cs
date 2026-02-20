// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<SearchValuesBenchmark>();

[MemoryDiagnoser]
public class SearchValuesBenchmark
{
    // The standard Base64 alphabet (64 characters).
    private const string Base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    [Benchmark]
    public SearchValues<char> Create_Base64Alphabet() =>
        SearchValues.Create(Base64Chars);
}
