// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    public sealed partial class MetadataLoadContext
    {
        private static readonly string[] s_CoreNames = { "mscorlib", "System.Runtime", "netstandard" };

        // Cache loaded coreAssembly and core types.
        internal RoAssembly TryGetCoreAssembly(string coreAssemblyName, out Exception e)
        {
            e = null;
            Debug.Assert(_coreAssembly == null);
            if (coreAssemblyName == null)
            {
                _coreAssembly = TryGetDefaultCoreAssembly(out e);
            }
            else
            {
                RoAssemblyName roAssemblyName = new AssemblyName(coreAssemblyName).ToRoAssemblyName();
                _coreAssembly = TryResolveAssembly(roAssemblyName, out e);
            }

            return _coreAssembly;
        }

        private RoAssembly TryGetDefaultCoreAssembly(out Exception e)
        {
            foreach (string coreName in s_CoreNames)
            {
                RoAssemblyName roAssemblyName = new AssemblyName(coreName).ToRoAssemblyName();
                RoAssembly roAssembly = TryResolveAssembly(roAssemblyName, out e);

                // Stop on the first core assembly we find
                if (roAssembly != null)
                {
                    e = null;
                    return roAssembly;
                }
            }

            e = new FileNotFoundException(SR.UnableToDetermineCoreAssembly);
            return null;
        }

        private RoAssembly _coreAssembly = null;

        /// <summary>
        /// Returns a lazily created and cached Type instance corresponding to the indicated core type. This method throws 
        /// if the core assembly name wasn't supplied, the core assembly could not be loaded for some reason or if the specified
        /// type does not exist in the core assembly.
        /// </summary>
        internal RoType GetCoreType(CoreType coreType)
        {
            CoreTypes coreTypes = GetAllFoundCoreTypes();
            RoType t = TryGetCoreType(coreType);
            return t ?? throw coreTypes.GetException(coreType);
        }

        /// <summary>
        /// Returns a lazily created and cached Type instance corresponding to the indicated core type. This method returns null
        /// if the core assembly name wasn't supplied, the core assembly could not be loaded for some reason or if the specified
        /// type does not exist in the core assembly.
        /// </summary>
        internal RoType TryGetCoreType(CoreType coreType)
        {
            CoreTypes coreTypes = GetAllFoundCoreTypes();
            return coreTypes[coreType];
        }

        /// <summary>
        /// Returns a cached array containing the resolved CoreTypes, indexed by the CoreType enum cast to an int.
        /// If the core assembly was not specified, not locatable or if one or more core types aren't present in the core assembly,
        /// the corresponding elements will be null.
        /// </summary>
        internal CoreTypes GetAllFoundCoreTypes() => _coreTypes;
        private CoreTypes _coreTypes;

        //
        // Seriously, ugh - the default binder for Reflection has a dependency on checking types for equality with System.Object - for that
        // one reason, we have to instance it per MetadataLoadContext.
        //
        internal Binder GetDefaultBinder() => _lazyDefaultBinder ?? (_lazyDefaultBinder = new DefaultBinder(this));
        private volatile Binder _lazyDefaultBinder;
    }
}
