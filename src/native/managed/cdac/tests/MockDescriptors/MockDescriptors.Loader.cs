// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class Loader
    {
        private const ulong DefaultAllocationRangeStart = 0x0001_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0002_0000;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }

        private readonly MockMemorySpace.Builder _builder;
        private readonly MockMemorySpace.BumpAllocator _allocator;

        public Loader(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public Loader(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            _builder = builder;
            _allocator = _builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            Types = GetTypes(builder.TargetTestHelpers);
        }

        private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            return GetTypesForTypeFields(
                helpers,
                [
                    ModuleFields,
                    AssemblyFields,
                ]);
        }

        internal TargetPointer AddModule(string? path = null, string? fileName = null)
        {
            TargetTestHelpers helpers = _builder.TargetTestHelpers;
            Target.TypeInfo typeInfo = Types[DataType.Module];
            uint size = typeInfo.Size.Value;
            MockMemorySpace.HeapFragment module = _allocator.Allocate(size, "Module");
            _builder.AddHeapFragment(module);

            if (path != null)
            {
                // Path data
                Encoding encoding = helpers.Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
                ulong pathSize = (ulong)encoding.GetByteCount(path) + sizeof(char);
                MockMemorySpace.HeapFragment pathFragment = _allocator.Allocate(pathSize, $"Module path = {path}");
                helpers.WriteUtf16String(pathFragment.Data, path);
                _builder.AddHeapFragment(pathFragment);

                // Pointer to path
                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.Path)].Offset, helpers.PointerSize),
                    pathFragment.Address);
            }

            if (fileName != null)
            {
                // File name data
                Encoding encoding = helpers.Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
                ulong fileNameSize = (ulong)encoding.GetByteCount(fileName) + sizeof(char);
                MockMemorySpace.HeapFragment fileNameFragment = _allocator.Allocate(fileNameSize, $"Module file name = {fileName}");
                helpers.WriteUtf16String(fileNameFragment.Data, fileName);
                _builder.AddHeapFragment(fileNameFragment);

                // Pointer to file name
                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.FileName)].Offset, helpers.PointerSize),
                    fileNameFragment.Address);
            }

            // add an assembly without any fields set (ie: not collectible)
            MockMemorySpace.HeapFragment assembly = _allocator.Allocate((ulong)helpers.SizeOfTypeInfo(Types[DataType.Assembly]), "Assembly");
            _builder.AddHeapFragment(assembly);
            helpers.WritePointer(module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.Assembly)].Offset, helpers.PointerSize), assembly.Address);

            return module.Address;
        }
    }
}
