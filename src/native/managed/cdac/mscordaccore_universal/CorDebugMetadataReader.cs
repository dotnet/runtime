// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader;

[GeneratedComInterface]
[Guid("7CEF8BA9-2EF7-42BF-973F-4171474F87D9")]
internal unsafe partial interface ICorDebugMetaDataLocator
{
    [PreserveSig]
    int GetMetaData(char* imagePath, uint imageTimeStamp, uint imageSize,
        uint pathBufferLength, uint* pathLength, char* pathBuffer);
}

internal sealed unsafe class CorDebugMetadataReader(Target target, ICorDebugMetaDataLocator locator)
{
    private const int InsufficientBuffer = unchecked((int)0x8007007A);
    private const int InitialPathLength = 260;
    private const int MaximumPathLength = 32768;
    private bool _readingMetadata;

    public bool TryReadMemory(ulong address, Span<byte> buffer)
    {
        // Resolving module headers can itself read missing target memory.
        // Like other target operations, calls to this reader must be serialized.
        if (_readingMetadata || buffer.IsEmpty)
            return false;

        _readingMetadata = true;
        try
        {
            ILoader loader = target.Contracts.Loader;
            IEcmaMetadata metadata = target.Contracts.EcmaMetadata;
            foreach (ModuleHandle module in loader.GetModuleHandles(loader.GetAppDomain(),
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            {
                try
                {
                    if (TryReadModuleMetadata(loader, metadata, module, address, buffer))
                        return true;
                }
                catch (VirtualReadException)
                {
                    // A dump may lack the headers of an unrelated module.
                }
            }
            return false;
        }
        catch (VirtualReadException)
        {
            return false;
        }
        finally
        {
            _readingMetadata = false;
        }
    }

    private bool TryReadModuleMetadata(ILoader loader, IEcmaMetadata metadata, ModuleHandle module,
        ulong address, Span<byte> buffer)
    {
        TargetPointer assembly = loader.GetPEAssembly(module);
        if (assembly == TargetPointer.Null || loader.IsDynamic(module) || metadata.HasReadWriteMetadata(assembly))
            return false;

        TargetSpan span = metadata.GetReadOnlyMetadataAddress(module);
        if (address < span.Address.Value)
            return false;
        ulong offset = address - span.Address.Value;
        if (offset > span.Size || (ulong)buffer.Length > span.Size - offset)
            return false;

        if (!loader.GetFileHeadersInfo(module, out uint timeStamp, out uint imageSize)
            || imageSize == 0
            || GetMetadataPath(loader.GetPath(module), timeStamp, imageSize) is not string path)
        {
            return false;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            using PEReader reader = new(stream);
            if (unchecked((uint)reader.PEHeaders.CoffHeader.TimeDateStamp) != timeStamp
                || reader.PEHeaders.PEHeader is not PEHeader header
                || (uint)header.SizeOfImage != imageSize
                || !reader.HasMetadata
                || (ulong)reader.GetMetadata().Length != span.Size)
            {
                return false;
            }

            reader.GetMetadata().GetContent().AsSpan(checked((int)offset), buffer.Length).CopyTo(buffer);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private string? GetMetadataPath(string imagePath, uint timeStamp, uint imageSize)
    {
        char[] path = new char[InitialPathLength];
        for (int attempt = 0; attempt < 2; attempt++)
        {
            uint length = 0;
            int hr;
            fixed (char* image = imagePath)
            fixed (char* destination = path)
            {
                hr = locator.GetMetaData(image, timeStamp, imageSize, (uint)path.Length, &length, destination);
            }

            if (hr >= 0)
            {
                if (length <= 1 || length > path.Length || path[length - 1] != '\0')
                    return null;
                return new string(path, 0, checked((int)length - 1));
            }
            if (hr != InsufficientBuffer || length <= path.Length || length > MaximumPathLength)
                return null;
            path = new char[length];
        }
        return null;
    }
}
