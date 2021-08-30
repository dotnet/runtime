// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Module objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaModule
    {
        internal MetadataTable<EcmaDefinitionType> TypeDefTable
        {
            get
            {
                return _lazyTypeDefTable ??
                    Interlocked.CompareExchange(ref _lazyTypeDefTable, CreateTable<EcmaDefinitionType>(TableIndex.TypeDef), null) ??
                    _lazyTypeDefTable;
            }
        }
        private volatile MetadataTable<EcmaDefinitionType>? _lazyTypeDefTable;

        private void EnsureTypeDefTableFullyFilled()
        {
            if (!_typeDefTableFullyFilled)
            {
                foreach (TypeDefinitionHandle h in Reader.TypeDefinitions)
                {
                    h.ResolveTypeDef(this);
                }
                _typeDefTableFullyFilled = true;
            }
        }
        private bool _typeDefTableFullyFilled; // Only gets set true if EnsureTypeDefTableFullyFilled() fills the table. False negative just means some unnecessary work is done.

        internal MetadataTable<RoDefinitionType> TypeRefTable
        {
            get
            {
                return _lazyTypeRefTable ??
                    Interlocked.CompareExchange(ref _lazyTypeRefTable, CreateTable<RoDefinitionType>(TableIndex.TypeRef), null) ??
                    _lazyTypeRefTable;
            }
        }
        private volatile MetadataTable<RoDefinitionType>? _lazyTypeRefTable;

        internal MetadataTable<EcmaGenericParameterType> GenericParamTable
        {
            get
            {
                return _lazyGenericParamTable ??
                    Interlocked.CompareExchange(ref _lazyGenericParamTable, CreateTable<EcmaGenericParameterType>(TableIndex.GenericParam), null) ??
                    _lazyGenericParamTable;
            }
        }
        private volatile MetadataTable<EcmaGenericParameterType>? _lazyGenericParamTable;

        internal MetadataTable<RoAssembly> AssemblyRefTable
        {
            get
            {
                return _lazyAssemblyRefTable ??
                    Interlocked.CompareExchange(ref _lazyAssemblyRefTable, CreateTable<RoAssembly>(TableIndex.AssemblyRef), null) ??
                    _lazyAssemblyRefTable;
            }
        }
        private volatile MetadataTable<RoAssembly>? _lazyAssemblyRefTable;

        private MetadataTable<T> CreateTable<T>(TableIndex tableIndex) where T : class
        {
            int rowCount = tableIndex switch
            {
                // Windows Metadata assemblies contain additional "virtual" AssemblyRefs we need to account for.
                // This is the simplest way to get the total AssemblyRefs count:
                TableIndex.AssemblyRef => Reader.AssemblyReferences.Count,
                _ => Reader.GetTableRowCount(tableIndex)
            };
            return new MetadataTable<T>(rowCount);
        }
    }
}
