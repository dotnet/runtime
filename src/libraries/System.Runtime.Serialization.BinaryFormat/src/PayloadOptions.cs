// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

public sealed class PayloadOptions
{
    public PayloadOptions() { }

    public TypeNameParseOptions? TypeNameParseOptions { get; set; }
}
