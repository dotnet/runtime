// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// A convenience class that holds the palette of core types that were successfully loaded (or the reason they were not.)
    /// </summary>
    internal sealed class CoreTypes
    {
        private readonly RoType[] _coreTypes;
        private readonly Exception[] _exceptions;

        internal CoreTypes(MetadataLoadContext loader, string coreAssemblyName)
        {
            int numCoreTypes = (int)CoreType.NumCoreTypes;
            RoType[] coreTypes = new RoType[numCoreTypes];
            Exception[] exceptions = new Exception[numCoreTypes];
            RoAssembly coreAssembly = loader.TryGetCoreAssembly(coreAssemblyName, out Exception e);
            if (coreAssembly == null)
            {
                // If the core assembly was not found, don't continue.
                throw e;
            }
            else
            {
                for (int i = 0; i < numCoreTypes; i++)
                {
                    ((CoreType)i).GetFullName(out byte[] ns, out byte[] name);
                    RoType type = coreAssembly.GetTypeCore(ns, name, ignoreCase: false, out e);
                    coreTypes[i] = type;
                    if (type == null)
                    {
                        exceptions[i] = e;
                    }
                }
            }
            _coreTypes = coreTypes;
            _exceptions = exceptions;
        }

        /// <summary>
        /// Returns null if the specific core type did not exist or could not be loaded. Call GetException(coreType) to get detailed info.
        /// </summary>
        public RoType this[CoreType coreType] => _coreTypes[(int)coreType];
        public Exception GetException(CoreType coreType) => _exceptions[(int)coreType];
    }
}
