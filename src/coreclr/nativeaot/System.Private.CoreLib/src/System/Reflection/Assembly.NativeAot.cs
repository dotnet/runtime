// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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

            // We're interested in the frame that called the method that calls GetCallingAssembly
            // (skipFrames: 2), but we walk the stack so that we can skip any compiler-generated
            // thunks that don't have method info, as well as the static constructor invocation
            // machinery (matching what CoreCLR skips when looking for the caller).
            // The for loop typically only runs one iteration.
            DiagnosticMethodInfo? dmi = null;
            for (int i = 2; ; i++)
            {
                var frame = new StackFrame(i);

                // A zero IP address means we walked off the top of the stack without finding anything.
                if (frame.GetNativeIPAddress() == IntPtr.Zero)
                    break;

                dmi = DiagnosticMethodInfo.Create(frame);
                if (dmi == null)
                    continue;

                // Skip the static constructor invocation machinery so that a GetCallingAssembly
                // called from a class constructor sees the assembly that triggered the cctor.
                if (dmi.DeclaringTypeName == $"System.Runtime.CompilerServices.{nameof(ClassConstructorRunner)}"
                    && dmi.DeclaringAssemblyName?.StartsWith(CoreLib.Name, StringComparison.Ordinal) == true)
                {
                    dmi = null;
                    continue;
                }

                break;
            }

            // If we haven't found anything, fall back to the method that called GetCallingAssembly.
            // This simulates what CoreCLR would do if GetCallingAssembly is called from e.g. Main.
            dmi ??= DiagnosticMethodInfo.Create(new StackFrame(1));

            return dmi?.DeclaringAssemblyName is string asmName ? Load(asmName) : null;
        }

        public static Assembly Load(AssemblyName assemblyRef) => ReflectionAugments.Load(assemblyRef, throwOnFileNotFound: true);

        public static Assembly Load(string assemblyString)
        {
            ArgumentNullException.ThrowIfNull(assemblyString);

            AssemblyName name = new AssemblyName(assemblyString);
            return Load(name);
        }

        // Performance metric to count the number of assemblies
        internal static uint GetAssemblyCount()
        {
            return (uint)Internal.Reflection.Core.Execution.ReflectionCoreExecution.ExecutionEnvironment.AssemblyBinder.GetLoadedAssembliesCount();
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
