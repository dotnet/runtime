// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed partial class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
    {
        private static AssemblyName GetFileInformationCore(string assemblyFile)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_AssemblyName_GetAssemblyName);
        }
    }
}
