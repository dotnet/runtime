// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Runtime.Serialization;
using System.IO;

using Internal.Reflection.Augments;
using Internal.Reflection.Core.NonPortable;

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
            if (assemblyString == null)
                throw new ArgumentNullException(nameof(assemblyString));

            AssemblyName name = new AssemblyName(assemblyString);
            return Load(name);
        }

        [Obsolete("Assembly.LoadWithPartialName has been deprecated. Use Assembly.Load() instead.")]
        public static Assembly LoadWithPartialName(string partialName)
        {
            if (partialName == null)
                throw new ArgumentNullException(nameof(partialName));

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
