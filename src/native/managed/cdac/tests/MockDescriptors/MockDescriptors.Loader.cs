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
        internal (string Name, ulong Value)[] Globals { get; private set; }

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
            Globals = [];
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

        internal TargetPointer AddModule(string? path = null, string? fileName = null, string? simpleName = null, TargetPointer? domainAssembly = null)
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

            if (simpleName != null)
            {
                // SimpleName is UTF-8
                byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(simpleName);
                ulong simpleNameSize = (ulong)utf8Bytes.Length + 1; // null terminator
                MockMemorySpace.HeapFragment simpleNameFragment = _allocator.Allocate(simpleNameSize, $"Module simple name = {simpleName}");
                utf8Bytes.CopyTo(simpleNameFragment.Data.AsSpan());
                simpleNameFragment.Data[utf8Bytes.Length] = 0; // null terminator
                _builder.AddHeapFragment(simpleNameFragment);

                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.SimpleName)].Offset, helpers.PointerSize),
                    simpleNameFragment.Address);
            }

            if (domainAssembly.HasValue)
            {
                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.DomainAssembly)].Offset, helpers.PointerSize),
                    domainAssembly.Value);
            }

            // add an assembly without any fields set (ie: not collectible)
            MockMemorySpace.HeapFragment assembly = _allocator.Allocate((ulong)helpers.SizeOfTypeInfo(Types[DataType.Assembly]), "Assembly");
            _builder.AddHeapFragment(assembly);
            helpers.WritePointer(module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.Assembly)].Offset, helpers.PointerSize), assembly.Address);

            return module.Address;
        }

        internal void SetAppDomainGlobal(TargetPointer appDomainAddress)
        {
            TargetTestHelpers helpers = _builder.TargetTestHelpers;

            // Create a global pointer that points to a cell containing the AppDomain address
            MockMemorySpace.HeapFragment appDomainGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] AppDomain");
            helpers.WritePointer(appDomainGlobal.Data, appDomainAddress);
            _builder.AddHeapFragment(appDomainGlobal);

            // Add to globals array
            var newGlobals = new (string Name, ulong Value)[Globals.Length + 1];
            Globals.CopyTo(newGlobals, 0);
            newGlobals[Globals.Length] = (nameof(Constants.Globals.AppDomain), appDomainGlobal.Address);
            Globals = newGlobals;
        }
    }
}
