// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace System.Reflection
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class Assembly
    {
        internal bool IsRuntimeImplemented() => this is RuntimeAssembly;

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly? LoadWithPartialName(string partialName)
        {
            if (partialName == null)
                throw new ArgumentNullException(nameof(partialName));

            if (partialName.Length == 0 || partialName[0] == '\0')
                throw new ArgumentException(SR.Format_StringZeroLength, nameof(partialName));

            try
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                return InternalLoad(partialName, ref stackMark, IntPtr.Zero);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetExecutingAssembly(ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Assembly GetCallingAssembly();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Assembly GetEntryAssemblyNative();

        private static Assembly? GetEntryAssemblyInternal()
        {
            return GetEntryAssemblyNative();
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(string assemblyString)
        {
            if (assemblyString == null)
                throw new ArgumentNullException(nameof(assemblyString));

            var name = new AssemblyName(assemblyString);
            // TODO: trigger assemblyFromResolveEvent

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Load(name, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            if (assemblyRef == null)
                throw new ArgumentNullException(nameof(assemblyRef));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Load(assemblyRef, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        internal static Assembly Load(AssemblyName assemblyRef, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext)
        {
            // TODO: pass AssemblyName
            Assembly? assembly = InternalLoad(assemblyRef.FullName, ref stackMark, assemblyLoadContext != null ? assemblyLoadContext.NativeALC : IntPtr.Zero);
            if (assembly == null)
                throw new FileNotFoundException(null, assemblyRef.Name);
            return assembly;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Assembly InternalLoad(string assemblyName, ref StackCrawlMark stackMark, IntPtr ptrLoadContextBinder);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Type InternalGetType(Module? module, string name, bool throwOnError, bool ignoreCase);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalGetAssemblyName(string assemblyFile, out Mono.MonoAssemblyName aname, out string codebase);
    }
}
