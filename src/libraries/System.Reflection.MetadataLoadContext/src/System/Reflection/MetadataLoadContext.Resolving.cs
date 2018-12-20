// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    public sealed partial class MetadataLoadContext
    {
        //
        // List of ref AssemblyNames to successfully bound assemblies. This is not the same as _loadedAssemblies.
        // _loadedAssemblies keeps track of canonical Assembly instances.
        //
        // _binds keeps the resolve event from being called to resolve the same refName over and over again.
        // If the resolve logic allows variations on the ref name, it is possible and common for the same assembly instance
        // to appear multiple times (once for each variation that was used to bind to it.) 
        //
        // We also latch failures. That is, _binds can bind a RuntimeAssemblyName to a RoFailedBindAssembly.
        //
        private readonly ConcurrentDictionary<RoAssemblyName, RoAssembly> _binds = new ConcurrentDictionary<RoAssemblyName, RoAssembly>();

        internal RoAssembly ResolveAssembly(RoAssemblyName refName)
        {
            Debug.Assert(refName != null);

            RoAssembly assembly = TryResolveAssembly(refName, out Exception e);
            return assembly ?? throw e;
        }

        internal RoAssembly TryResolveAssembly(RoAssemblyName refName, out Exception e)
        {
            e = null;

            RoAssembly result = ResolveToAssemblyOrExceptionAssembly(refName);
            if (result is RoExceptionAssembly exceptionAssembly)
            {
                e = exceptionAssembly.Exception;
                return null;
            }
            return result;
        }

        internal RoAssembly ResolveToAssemblyOrExceptionAssembly(RoAssemblyName refName)
        {
            Debug.Assert(refName != null);

            if (_binds.TryGetValue(refName, out RoAssembly prior))
                return prior;

            RoAssembly assembly = TryFindAssemblyByCallingResolveHandler(refName);
            return _binds.GetOrAdd(refName, assembly);
        }

        private RoAssembly TryFindAssemblyByCallingResolveHandler(RoAssemblyName refName)
        {
            Debug.Assert(refName != null);

            Assembly assembly = resolver?.Resolve(this, refName.ToAssemblyName());

            if (assembly == null)
                return new RoExceptionAssembly(new FileNotFoundException(SR.Format(SR.FileNotFoundAssembly, refName.FullName)));

            if (!(assembly is RoAssembly roAssembly && roAssembly.Loader == this))
                throw new FileLoadException(SR.ExternalAssemblyReturnedByMetadataAssemblyResolver);

            return roAssembly;
        }
    }
}
