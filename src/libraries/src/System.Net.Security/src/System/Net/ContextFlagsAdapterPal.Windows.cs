// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net
{
    internal static class ContextFlagsAdapterPal
    {
        private struct ContextFlagMapping
        {
            public readonly Interop.SspiCli.ContextFlags Win32Flag;
            public readonly ContextFlagsPal ContextFlag;

            public ContextFlagMapping(Interop.SspiCli.ContextFlags win32Flag, ContextFlagsPal contextFlag)
            {
                Win32Flag = win32Flag;
                ContextFlag = contextFlag;
            }
        }

        private static readonly ContextFlagMapping[] s_contextFlagMapping = new[]
        {
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AcceptExtendedError, ContextFlagsPal.AcceptExtendedError),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AcceptIdentify, ContextFlagsPal.AcceptIdentify),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AcceptIntegrity, ContextFlagsPal.AcceptIntegrity),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AcceptStream, ContextFlagsPal.AcceptStream),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AllocateMemory, ContextFlagsPal.AllocateMemory),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.AllowMissingBindings, ContextFlagsPal.AllowMissingBindings),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.Confidentiality, ContextFlagsPal.Confidentiality),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.Connection, ContextFlagsPal.Connection),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.Delegate, ContextFlagsPal.Delegate),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitExtendedError, ContextFlagsPal.InitExtendedError),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitIdentify, ContextFlagsPal.InitIdentify),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitManualCredValidation, ContextFlagsPal.InitManualCredValidation),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitIntegrity, ContextFlagsPal.InitIntegrity),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitStream, ContextFlagsPal.InitStream),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.InitUseSuppliedCreds, ContextFlagsPal.InitUseSuppliedCreds),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.MutualAuth, ContextFlagsPal.MutualAuth),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.ProxyBindings, ContextFlagsPal.ProxyBindings),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.ReplayDetect, ContextFlagsPal.ReplayDetect),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.SequenceDetect, ContextFlagsPal.SequenceDetect),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.UnverifiedTargetName, ContextFlagsPal.UnverifiedTargetName),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.UseSessionKey, ContextFlagsPal.UseSessionKey),
            new ContextFlagMapping(Interop.SspiCli.ContextFlags.Zero, ContextFlagsPal.None),
        };

        internal static ContextFlagsPal GetContextFlagsPalFromInterop(Interop.SspiCli.ContextFlags win32Flags)
        {
            ContextFlagsPal flags = ContextFlagsPal.None;
            foreach (ContextFlagMapping mapping in s_contextFlagMapping)
            {
                if ((win32Flags & mapping.Win32Flag) ==  mapping.Win32Flag)
                {
                    flags |= mapping.ContextFlag;
                }
            }

            return flags;
        }

        internal static Interop.SspiCli.ContextFlags GetInteropFromContextFlagsPal(ContextFlagsPal flags)
        {
            Interop.SspiCli.ContextFlags win32Flags = Interop.SspiCli.ContextFlags.Zero;
            foreach (ContextFlagMapping mapping in s_contextFlagMapping)
            {
                if ((flags & mapping.ContextFlag) ==  mapping.ContextFlag)
                {
                    win32Flags |= mapping.Win32Flag;
                }
            }

            return win32Flags;
        }
    }
}
