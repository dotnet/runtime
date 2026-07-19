// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if HOST_MODEL
namespace Microsoft.NET.HostModel.MachO;
#else
namespace ILCompiler.Reflection.ReadyToRun.MachO;
#endif

internal enum MachFileType : uint
{
    Execute = 2,
}
