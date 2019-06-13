// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices.WindowsRuntime
{
    internal static class ActivationFactoryLoader
    {
        // Collection of all ALCs used for WinRT activation.
        // Since each of the assemblies that act as the "key" here are WinRT assemblies
        // we don't need to share this dictionary with the COM activation dictionary
        // since there will be no overlap.
        private static readonly Dictionary<string, AssemblyLoadContext> s_assemblyLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.InvariantCultureIgnoreCase);
        
        private static AssemblyLoadContext GetALC(string assemblyPath)
        {
            AssemblyLoadContext? alc;

            lock (s_assemblyLoadContexts)
            {
                if (!s_assemblyLoadContexts.TryGetValue(assemblyPath, out alc))
                {
                    alc = new IsolatedComponentLoadContext(assemblyPath);
                    s_assemblyLoadContexts.Add(assemblyPath, alc);
                }
            }

            return alc;
        }

        /// <summary>Get a WinRT activation factory for a given type name.</summary>
        /// <param name="componentPath">The path to the WinRT component that the type is expected to be defined in.</param>
        /// <param name="typeName">The name of the component type to activate</param>
        /// <param name="activationFactory">The activation factory</param>
        public unsafe static int GetActivationFactory(
            char* componentPath,
            [MarshalAs(UnmanagedType.HString)] string typeName,
            [MarshalAs(UnmanagedType.Interface)] out IActivationFactory? activationFactory)
        {
            activationFactory = null;
            try
            {
                if (typeName is null)
                {
                    throw new ArgumentNullException(nameof(typeName));
                }

                AssemblyLoadContext context = GetALC(Marshal.PtrToStringUni((IntPtr)componentPath)!);
                
                Type winRTType = context.LoadTypeForWinRTTypeNameInContext(typeName);

                if (winRTType is null || !winRTType.IsExportedToWindowsRuntime)
                {
                    throw new TypeLoadException(typeName);
                }
                activationFactory = WindowsRuntimeMarshal.GetManagedActivationFactory(winRTType);
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
            return 0;
        }
    }
}
