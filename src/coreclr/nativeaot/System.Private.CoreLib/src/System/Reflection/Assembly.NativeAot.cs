// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Security;

using Internal.Reflection.Augments;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        private static Assembly? GetEntryAssemblyInternal() => Internal.Runtime.CompilerHelpers.StartupCodeHelpers.GetEntryAssembly();

        [System.Runtime.CompilerServices.Intrinsic]
        public static Assembly GetExecutingAssembly() { throw NotImplemented.ByDesign; } //Implemented by toolchain.

        [DynamicSecurityMethod]
        public static Assembly GetCallingAssembly()
        {
            if (AppContext.TryGetSwitch("Switch.System.Reflection.Assembly.SimulatedCallingAssembly", out bool isSimulated) && isSimulated)
                return GetEntryAssembly();

            if (!StackTrace.IsSupported)
                throw new NotSupportedException(SR.NotSupported_StackTraceSupportDisabled);

            // We want to be able to handle GetCallingAssembly being called from Main (CoreCLR returns
            // the assembly of Main), and also GetCallingAssembly being called from things like
            // delegate invoke thunks. We do this by making the the definition of "calling assembly"
            // a bit more loose.

            // Technically we want skipFrames: 2 since we're interested in the frame that
            // called the method that calls GetCallingAssembly, but we might need the first frame
            // later in this method.
            var stackTrace = new StackTrace(skipFrames: 1);

            DiagnosticMethodInfo? dmi = null;

            // Note: starting at index 1 since we want to skip the method that called GetCallingAssembly.
            // We do a foreach so that we can skip any compiler-generated thunks that don't have method info.
            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                dmi = DiagnosticMethodInfo.Create(stackTrace.GetFrame(i));
                if (dmi != null)
                    break;
            }

            // If we haven't found anything in the entire stack trace, fall back
            // to the method that called this method. This simulates what CoreCLR would
            // do if GetCallingAssembly is called from e.g. Main.
            dmi ??= stackTrace.GetFrame(0) is StackFrame sf ? DiagnosticMethodInfo.Create(sf) : null;

            return dmi.DeclaringAssemblyName is string asmName ? Load(asmName) : null;
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
