// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaModule
    {
        public int CompareTo(EcmaModule other)
        {
            if (this == other)
                return 0;

            IAssemblyDesc thisAssembly = Assembly;
            IAssemblyDesc otherAssembly = other.Assembly;
            if (thisAssembly != otherAssembly)
            {
                // Each module comes from a different assembly: compare the assemblies
                AssemblyName thisAssemblyName = thisAssembly.GetName();
                AssemblyName otherAssemblyName = otherAssembly.GetName();

                int compare = StringComparer.Ordinal.Compare(thisAssemblyName.Name, otherAssemblyName.Name);
                if (compare != 0)
                    return compare;

                compare = StringComparer.Ordinal.Compare(thisAssemblyName.CultureName, otherAssemblyName.CultureName);
                Debug.Assert(compare != 0);
                return compare;
            }
            else
            {
                // Multi-module assembly: compare two modules that are part of same assembly
                string thisName = _metadataReader.GetString(_metadataReader.GetModuleDefinition().Name);
                string otherName = other._metadataReader.GetString(other._metadataReader.GetModuleDefinition().Name);
                Debug.Assert(StringComparer.Ordinal.Compare(thisName, otherName) != 0);
                return StringComparer.Ordinal.Compare(thisName, otherName);
            }
        }
    }
}
