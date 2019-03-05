// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Configuration.Assemblies;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Loader;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        // Locate an assembly by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(string assemblyString)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, ref stackMark);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            if (assemblyRef == null)
                throw new ArgumentNullException(nameof(assemblyRef));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Load(assemblyRef, ref stackMark, IntPtr.Zero);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        internal static Assembly Load(AssemblyName assemblyRef, ref StackCrawlMark stackMark, IntPtr ptrLoadContextBinder)
        {
            AssemblyName modifiedAssemblyRef = null;
            if (assemblyRef.CodeBase != null)
            {
                modifiedAssemblyRef = (AssemblyName)assemblyRef.Clone();
                modifiedAssemblyRef.CodeBase = null;
            }
            else
            {
                modifiedAssemblyRef = assemblyRef;
            }

            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, ref stackMark, ptrLoadContextBinder);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }

        // Get the assembly that the current code is running from.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod 
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetExecutingAssembly(ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly GetCallingAssembly()
        {
            // LookForMyCallersCaller is not guaranteed to return the correct stack frame
            // because of inlining, tail calls, etc. As a result GetCallingAssembly is not 
            // guaranteed to return the correct result. It's also documented as such.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return GetExecutingAssembly(ref stackMark);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEntryAssembly(ObjectHandleOnStack retAssembly);

        // internal test hook
        private static bool s_forceNullEntryPoint = false;

        public static Assembly GetEntryAssembly()
        {
            if (s_forceNullEntryPoint)
                return null;

            RuntimeAssembly entryAssembly = null;
            GetEntryAssembly(JitHelpers.GetObjectHandleOnStack(ref entryAssembly));
            return entryAssembly;
        }

        // Exists to faciliate code sharing between CoreCLR and CoreRT.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRuntimeImplemented() => this is RuntimeAssembly;
    }
}
