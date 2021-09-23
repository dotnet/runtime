// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Obsolete("Assembly.LoadWithPartialName has been deprecated. Use Assembly.Load() instead.")]
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
            return RuntimeAssembly.InternalLoad(assemblyRef, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExecutingAssemblyNative(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly? retAssembly = null;
            GetExecutingAssemblyNative(new StackCrawlMarkHandle(ref stackMark), ObjectHandleOnStack.Create(ref retAssembly));
            return retAssembly!;
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEntryAssemblyNative(ObjectHandleOnStack retAssembly);

        private static Assembly? GetEntryAssemblyInternal()
        {
            RuntimeAssembly? entryAssembly = null;
            GetEntryAssemblyNative(ObjectHandleOnStack.Create(ref entryAssembly));
            return entryAssembly;
        }

        // Exists to faciliate code sharing between CoreCLR and CoreRT.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRuntimeImplemented() => this is RuntimeAssembly;

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint GetAssemblyCount();
    }
}
