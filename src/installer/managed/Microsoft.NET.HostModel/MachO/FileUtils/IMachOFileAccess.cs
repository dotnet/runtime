// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// An abstraction for reading and writing Mach-O files.
/// </summary>
public interface IMachOFileAccess : IMachOFileReader, IMachOFileWriter
{
}
