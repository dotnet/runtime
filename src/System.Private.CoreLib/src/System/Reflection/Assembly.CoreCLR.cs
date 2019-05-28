// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
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
            return RuntimeAssembly.InternalLoad(assemblyString, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly? LoadWithPartialName(string partialName)
        {
            if (partialName == null)
                throw new ArgumentNullException(nameof(partialName));

            if ((partialName.Length == 0) || (partialName[0] == '\0'))
                throw new ArgumentException(SR.Format_StringZeroLength, nameof(partialName));

            try
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                return RuntimeAssembly.InternalLoad(partialName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            if (assemblyRef == null)
                throw new ArgumentNullException(nameof(assemblyRef));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Load(assemblyRef, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        internal static Assembly Load(AssemblyName assemblyRef, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext)
        {
            AssemblyName modifiedAssemblyRef;
            if (assemblyRef.CodeBase != null)
            {
                modifiedAssemblyRef = (AssemblyName)assemblyRef.Clone();
                modifiedAssemblyRef.CodeBase = null;
            }
            else
            {
                modifiedAssemblyRef = assemblyRef;
            }

            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, ref stackMark, assemblyLoadContext);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExecutingAssemblyNative(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly? retAssembly = null;
            GetExecutingAssemblyNative(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly!; // TODO-NULLABLE: Remove ! when nullable attributes are respected
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
        private static extern void GetEntryAssemblyNative(ObjectHandleOnStack retAssembly);

        // internal test hook
        private static bool s_forceNullEntryPoint = false;

        public static Assembly? GetEntryAssembly()
        {
            if (s_forceNullEntryPoint)
                return null;

            RuntimeAssembly? entryAssembly = null;
            GetEntryAssemblyNative(JitHelpers.GetObjectHandleOnStack(ref entryAssembly));
            return entryAssembly;
        }

        // Exists to faciliate code sharing between CoreCLR and CoreRT.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRuntimeImplemented() => this is RuntimeAssembly;

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint GetAssemblyCount();
    }
}
