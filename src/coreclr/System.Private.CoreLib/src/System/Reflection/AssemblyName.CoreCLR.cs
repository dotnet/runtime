// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed partial class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
    {
        internal AssemblyName(string? name,
            byte[]? publicKey,
            byte[]? publicKeyToken,
            Version? version,
            CultureInfo? cultureInfo,
            AssemblyNameFlags flags)
            : this()
        {
            _name = name;
            _publicKey = publicKey;
            _publicKeyToken = publicKeyToken;
            _version = version;
            _cultureInfo = cultureInfo;
            _flags = flags;
        }

        internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
        {
#pragma warning disable SYSLIB0037 // AssemblyName.ProcessorArchitecture is obsolete
            ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _flags);
#pragma warning restore SYSLIB0037
        }

        internal static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
        {
            if (((uint)flags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;

            if ((pek & PortableExecutableKinds.PE32Plus) == PortableExecutableKinds.PE32Plus)
            {
                switch (ifm)
                {
                    case ImageFileMachine.IA64:
                        return ProcessorArchitecture.IA64;
                    case ImageFileMachine.AMD64:
                        return ProcessorArchitecture.Amd64;
                    case ImageFileMachine.I386:
                        if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
                            return ProcessorArchitecture.MSIL;
                        break;
                }
            }
            else
            {
                if (ifm == ImageFileMachine.I386)
                {
                    if ((pek & PortableExecutableKinds.Required32Bit) == PortableExecutableKinds.Required32Bit)
                        return ProcessorArchitecture.X86;

                    if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
                        return ProcessorArchitecture.MSIL;

                    return ProcessorArchitecture.X86;
                }
                if (ifm == ImageFileMachine.ARM)
                {
                    return ProcessorArchitecture.Arm;
                }
            }
            return ProcessorArchitecture.None;
        }
    }
}
