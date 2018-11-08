// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private volatile MetadataTable<EcmaDefinitionType, EcmaModule> _lazyTypeDefTable;

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

        internal MetadataTable<RoDefinitionType, EcmaModule> TypeRefTable
        {
            get
            {
                return _lazyTypeRefTable ?? 
                    Interlocked.CompareExchange(ref _lazyTypeRefTable, CreateTable<RoDefinitionType>(TableIndex.TypeRef), null) ??
                    _lazyTypeRefTable;
            }
        }
        private volatile MetadataTable<RoDefinitionType, EcmaModule> _lazyTypeRefTable;

        internal MetadataTable<EcmaGenericParameterType, EcmaModule> GenericParamTable
        {
            get
            {
                return _lazyGenericParamTable ?? 
                    Interlocked.CompareExchange(ref _lazyGenericParamTable, CreateTable<EcmaGenericParameterType>(TableIndex.GenericParam), null) ?? 
                    _lazyGenericParamTable;
            }
        }
        private volatile MetadataTable<EcmaGenericParameterType, EcmaModule> _lazyGenericParamTable;

        internal MetadataTable<RoAssembly, EcmaModule> AssemblyRefTable
        {
            get
            {
                return _lazyAssemblyRefTable ?? 
                    Interlocked.CompareExchange(ref _lazyAssemblyRefTable, CreateTable<RoAssembly>(TableIndex.AssemblyRef), null) ?? 
                    _lazyAssemblyRefTable;
            }
        }
        private volatile MetadataTable<RoAssembly, EcmaModule> _lazyAssemblyRefTable;

        private MetadataTable<T, EcmaModule> CreateTable<T>(TableIndex tableIndex) where T : class
        {
            return new MetadataTable<T, EcmaModule>(Reader.GetTableRowCount(tableIndex));
        }
    }
}
