// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader;

internal sealed class MetadataMemoryCache(Target target, MetadataMemoryCache.GetMetadataDelegate getMetadata)
{
    internal delegate int GetMetadataDelegate(string path, uint timestamp, uint imageSize, uint metadataRva, Span<byte> buffer, out uint dataSize);

    private sealed class Image(ModuleHandle module, TargetSpan contents)
    {
        internal ModuleHandle Module { get; } = module;
        internal TargetSpan Contents { get; } = contents;
        internal TargetSpan? Metadata { get; set; }
        internal byte[]? Data { get; set; }
    }

    private List<Image>? _images;
    private bool _reading;

    internal void Flush()
    {
        _images = null;
    }

    internal bool TryRead(ulong address, Span<byte> buffer)
    {
        // Discovering modules and metadata headers reads target memory too. Those reads
        // must use the host rather than recursively attempting metadata recovery.
        if (_reading || buffer.IsEmpty)
            return false;

        _reading = true;
        try
        {
            Image? image = FindImage(address);
            if (image is null)
                return false;
            if ((ulong)buffer.Length > image.Contents.Size - (address - image.Contents.Address.Value))
                return false;

            TargetSpan metadata = image.Metadata ??= target.Contracts.EcmaMetadata.GetReadOnlyMetadataAddress(image.Module);
            if (address < metadata.Address.Value)
                return false;

            ulong offset = address - metadata.Address.Value;
            if (offset >= metadata.Size || (ulong)buffer.Length > metadata.Size - offset)
                return false;

            if (image.Data is null)
            {
                if (metadata.Size > int.MaxValue)
                    return false;

                ILoader loader = target.Contracts.Loader;
                if (!loader.GetFileHeadersInfo(image.Module, out uint timestamp, out uint imageSize))
                    return false;

                string path;
                try
                {
                    path = loader.GetPath(image.Module);
                }
                catch (VirtualReadException ex)
                {
                    Trace.TraceWarning($"Metadata recovery could not read the module path: {ex.Message}");
                    path = string.Empty;
                }
                if (path.Length == 0)
                    path = loader.GetFileName(image.Module);

                byte[] data = new byte[(int)metadata.Size];
                // A zero RVA asks the locator to use the metadata directory in the IL image.
                // The target may instead be a flat image or a native image with different RVAs.
                int hr = getMetadata(path, timestamp, imageSize, 0, data, out uint dataSize);
                if (hr < 0)
                    return false;

                // A debugger may choose different metadata. It cannot be exposed at the
                // original target addresses unless it fits the original metadata range.
                if (dataSize != data.Length)
                    return false;

                image.Data = data;
            }

            image.Data.AsSpan(checked((int)offset), buffer.Length).CopyTo(buffer);
            return true;
        }
        catch (VirtualReadException ex)
        {
            Trace.TraceWarning($"Metadata recovery could not read module information: {ex.Message}");
            return false;
        }
        catch (BadImageFormatException ex)
        {
            Trace.TraceWarning($"Metadata recovery could not read image headers: {ex.Message}");
            return false;
        }
        finally
        {
            _reading = false;
        }
    }

    private Image? FindImage(ulong address)
    {
        if (_images is null)
        {
            List<Image> images = [];
            ILoader loader = target.Contracts.Loader;
            foreach (ModuleHandle module in loader.GetModuleHandles(loader.GetAppDomain(),
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            {
                try
                {
                    if (loader.TryGetLoadedImageContents(module, out TargetPointer imageBase, out uint size, out _)
                        && imageBase != TargetPointer.Null && size != 0)
                    {
                        images.Add(new Image(module, new TargetSpan(imageBase, size)));
                    }
                }
                catch (VirtualReadException ex)
                {
                    Trace.TraceWarning($"Metadata recovery could not read image information for module {module.Address}: {ex.Message}");
                }
            }

            images.Sort(static (left, right) => left.Contents.Address.Value.CompareTo(right.Contents.Address.Value));
            _images = images;
        }

        int low = 0;
        int high = _images.Count - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            Image image = _images[middle];
            if (address < image.Contents.Address.Value)
                high = middle - 1;
            else if (address - image.Contents.Address.Value >= image.Contents.Size)
                low = middle + 1;
            else
                return image;
        }

        return null;
    }
}
