// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Internal.TypeSystem
{
    // Api surface for TypeDesc that relates to interfaces

    public partial class TypeDesc
    {
        private DefType[] _runtimeInterfaces;

        /// <summary>
        /// The interfaces implemented by this type at runtime. There may be duplicates in this list.
        /// </summary>
        ///
        public DefType[] RuntimeInterfaces
        {
            get
            {
                if (_runtimeInterfaces == null)
                {
                    return InitializeRuntimeInterfaces();
                }

                return _runtimeInterfaces;
            }
        }

        private DefType[] InitializeRuntimeInterfaces()
        {
            RuntimeInterfacesAlgorithm algorithm = this.Context.GetRuntimeInterfacesAlgorithmForType(this);
            DefType[] computedInterfaces = algorithm != null ? algorithm.ComputeRuntimeInterfaces(this) : Array.Empty<DefType>();
            Interlocked.CompareExchange(ref _runtimeInterfaces, computedInterfaces, null);
            return _runtimeInterfaces;
        }
    }
}
