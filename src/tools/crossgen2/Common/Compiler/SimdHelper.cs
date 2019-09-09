// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.TypeSystem;

using AssemblyName = System.Reflection.AssemblyName;

namespace ILCompiler
{
    /// <summary>
    /// Helper type that deals with System.Numerics.Vectors intrinsics.
    /// </summary>
    public struct SimdHelper
    {
        private ModuleDesc[] _simdModulesCached;

        public bool IsSimdType(TypeDesc type)
        {
            if (type is MetadataType metadataType)
            {
                if (_simdModulesCached == null)
                {
                    InitializeSimdModules(type);
                }

                ModuleDesc typeModule = metadataType.Module;
                foreach (ModuleDesc simdModule in _simdModulesCached)
                    if (typeModule == simdModule)
                        return true;

                if (metadataType.IsIntrinsic)
                {
                    string name = metadataType.Name;
                    if ((name == "Vector`1" || name == "Vector") &&
                        metadataType.Namespace == "System.Numerics")
                        return true;
                }
            }

            return false;
        }

        private void InitializeSimdModules(TypeDesc type)
        {
            TypeSystemContext context = type.Context;

            ArrayBuilder<ModuleDesc> simdModules = new ArrayBuilder<ModuleDesc>();

            ModuleDesc module = context.ResolveAssembly(new AssemblyName("System.Numerics"), false);
            if (module != null)
                simdModules.Add(module);

            module = context.ResolveAssembly(new AssemblyName("System.Numerics.Vectors"), false);
            if (module != null)
                simdModules.Add(module);

            _simdModulesCached = simdModules.ToArray();
        }

        public bool IsVectorOfT(TypeDesc type)
        {
            return IsSimdType(type)
                && ((MetadataType)type).Name == "Vector`1"
                && ((MetadataType)type).Namespace == "System.Numerics";
        }
    }
}
