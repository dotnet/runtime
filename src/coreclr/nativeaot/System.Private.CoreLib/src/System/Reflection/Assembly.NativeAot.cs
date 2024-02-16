// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.IO;
using System.Runtime.Serialization;

using Internal.Reflection.Augments;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        private static Assembly? GetEntryAssemblyInternal() => Internal.Runtime.CompilerHelpers.StartupCodeHelpers.GetEntryAssembly();

        [System.Runtime.CompilerServices.Intrinsic]
        public static Assembly GetExecutingAssembly() { throw NotImplemented.ByDesign; } //Implemented by toolchain.

        public static Assembly GetCallingAssembly()
        {
            if (AppContext.TryGetSwitch("Switch.System.Reflection.Assembly.SimulatedCallingAssembly", out bool isSimulated) && isSimulated)
                return GetEntryAssembly();

            throw new PlatformNotSupportedException();
        }

        public static Assembly Load(AssemblyName assemblyRef) => ReflectionAugments.ReflectionCoreCallbacks.Load(assemblyRef, throwOnFileNotFound: true);

        public static Assembly Load(string assemblyString)
        {
            ArgumentNullException.ThrowIfNull(assemblyString);

            AssemblyName name = new AssemblyName(assemblyString);
            return Load(name);
        }

        // Performance metric to count the number of assemblies
        // Caching since in NativeAOT, the number will be the same
        private static uint s_assemblyCount;
        internal static uint GetAssemblyCount()
        {
            if (s_assemblyCount == 0)
                s_assemblyCount = (uint)Internal.Reflection.Core.Execution.ReflectionCoreExecution.ExecutionEnvironment.AssemblyBinder.GetLoadedAssemblies().Count;
            return s_assemblyCount;
        }

        [Obsolete("Assembly.LoadWithPartialName has been deprecated. Use Assembly.Load() instead.")]
        public static Assembly LoadWithPartialName(string partialName)
        {
            ArgumentNullException.ThrowIfNull(partialName);

            if ((partialName.Length == 0) || (partialName[0] == '\0'))
                throw new ArgumentException(SR.Format_StringZeroLength, nameof(partialName));

            try
            {
                return Load(partialName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
