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
        internal MetadataTable<EcmaDefinitionType, EcmaModule> TypeDefTable
        {
            get
            {
                return _lazyTypeDefTable ??
                    Interlocked.CompareExchange(ref _lazyTypeDefTable, CreateTable<EcmaDefinitionType>(TableIndex.TypeDef), null) ??
                    _lazyTypeDefTable;
            }
        }
        private volatile MetadataTable<EcmaDefinitionType, EcmaModule>? _lazyTypeDefTable;

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

        internal MetadataTable<RoDefinitionType, EcmaModule> TypeRefTable =>
            field ??
            Interlocked.CompareExchange(ref field, CreateTable<RoDefinitionType>(TableIndex.TypeRef), null) ??
            field;

        internal MetadataTable<EcmaGenericParameterType, EcmaModule> GenericParamTable =>
            field ??
            Interlocked.CompareExchange(ref field, CreateTable<EcmaGenericParameterType>(TableIndex.GenericParam), null) ??
            field;

        internal MetadataTable<RoAssembly, EcmaModule> AssemblyRefTable =>
            field ??
            Interlocked.CompareExchange(ref field, CreateTable<RoAssembly>(TableIndex.AssemblyRef), null) ??
            field;

        private MetadataTable<T, EcmaModule> CreateTable<T>(TableIndex tableIndex) where T : class
        {
            return new MetadataTable<T, EcmaModule>(Reader.GetTableRowCount(tableIndex));
        }
    }
}
