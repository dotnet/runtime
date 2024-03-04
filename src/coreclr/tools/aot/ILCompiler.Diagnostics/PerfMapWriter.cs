// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Internal.ReadyToRunDiagnosticsConstants;
using Internal.TypeSystem;

namespace ILCompiler.Diagnostics
{
    public class PerfMapWriter
    {
        public const int LegacyCrossgen1FormatVersion = 0;

        public const int CurrentFormatVersion = 1;

        const int HeaderEntriesPseudoLength = 0;

        private TextWriter _writer;

        private PerfMapWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public static void Write(string perfMapFileName, int perfMapFormatVersion, IEnumerable<MethodInfo> methods, IEnumerable<AssemblyInfo> inputAssemblies, TargetDetails details)
        {
            if (perfMapFormatVersion > CurrentFormatVersion)
            {
                throw new NotSupportedException(perfMapFormatVersion.ToString());
            }

            using (TextWriter writer = new StreamWriter(perfMapFileName))
            {

                PerfMapWriter perfMapWriter = new PerfMapWriter(writer);
                byte[] signature = PerfMapV1SignatureHelper(inputAssemblies, details);
                WritePerfMapV1Header(inputAssemblies, details, perfMapWriter);

                foreach (MethodInfo methodInfo in methods)
                {
                    if (methodInfo.HotRVA != 0 && methodInfo.HotLength != 0)
                    {
                        perfMapWriter.WriteLine(methodInfo.Name, methodInfo.HotRVA, methodInfo.HotLength);
                    }
                    if (methodInfo.ColdRVA != 0 && methodInfo.ColdLength != 0)
                    {
                        perfMapWriter.WriteLine(methodInfo.Name, methodInfo.ColdRVA, methodInfo.ColdLength);
                    }
                }
            }
        }

        private static void WritePerfMapV1Header(IEnumerable<AssemblyInfo> inputAssemblies, TargetDetails details, PerfMapWriter perfMapWriter)
        {
            byte[] signature = PerfMapV1SignatureHelper(inputAssemblies, details);

            // Make sure these get emitted in this order, other tools in the ecosystem like the symbol uploader and PerfView rely on this.
            // In particular, the order of it. Append only.
            string signatureFormatted = Convert.ToHexString(signature);

            PerfmapTokensForTarget targetTokens = TranslateTargetDetailsToPerfmapConstants(details);

            perfMapWriter.WriteLine(signatureFormatted, (uint)PerfMapPseudoRVAToken.OutputSignature, HeaderEntriesPseudoLength);
            perfMapWriter.WriteLine(CurrentFormatVersion.ToString(), (uint)PerfMapPseudoRVAToken.FormatVersion, HeaderEntriesPseudoLength);
            perfMapWriter.WriteLine(((uint)targetTokens.OperatingSystem).ToString(), (uint)PerfMapPseudoRVAToken.TargetOS, HeaderEntriesPseudoLength);
            perfMapWriter.WriteLine(((uint)targetTokens.Architecture).ToString(), (uint)PerfMapPseudoRVAToken.TargetArchitecture, HeaderEntriesPseudoLength);
            perfMapWriter.WriteLine(((uint)targetTokens.Abi).ToString(), (uint)PerfMapPseudoRVAToken.TargetABI, HeaderEntriesPseudoLength);
        }

        public static byte[] PerfMapV1SignatureHelper(IEnumerable<AssemblyInfo> inputAssemblies, TargetDetails details)
        {
            IEnumerable<AssemblyInfo> orderedInputs = inputAssemblies.OrderBy(asm => asm.Name, StringComparer.OrdinalIgnoreCase);
            List<byte> inputHash = new List<byte>();
            foreach (AssemblyInfo inputAssembly in orderedInputs)
            {
                inputHash.AddRange(inputAssembly.Mvid.ToByteArray());
            }

            PerfmapTokensForTarget targetTokens = TranslateTargetDetailsToPerfmapConstants(details);

            byte[] buffer = new byte[12];
            if (!BitConverter.TryWriteBytes(buffer.AsSpan(0, sizeof(uint)), (uint)targetTokens.OperatingSystem)
                || !BitConverter.TryWriteBytes(buffer.AsSpan(4, sizeof(uint)), (uint)targetTokens.Architecture)
                || !BitConverter.TryWriteBytes(buffer.AsSpan(8, sizeof(uint)), (uint)targetTokens.Abi))
            {
                throw new InvalidOperationException();
            }

            if (!BitConverter.IsLittleEndian)
            {
                buffer.AsSpan(0, sizeof(uint)).Reverse();
                buffer.AsSpan(4, sizeof(uint)).Reverse();
                buffer.AsSpan(8, sizeof(uint)).Reverse();
            }

            inputHash.AddRange(buffer);
            byte[] hash = MD5.HashData(inputHash.ToArray());

            return hash;
        }

        internal record struct PerfmapTokensForTarget(PerfMapOSToken OperatingSystem, PerfMapArchitectureToken Architecture, PerfMapAbiToken Abi);

        private static PerfmapTokensForTarget TranslateTargetDetailsToPerfmapConstants(TargetDetails details)
        {
            PerfMapOSToken osToken = details.OperatingSystem switch
            {
                TargetOS.Unknown => PerfMapOSToken.Unknown,
                TargetOS.Windows => PerfMapOSToken.Windows,
                TargetOS.Linux => PerfMapOSToken.Linux,
                TargetOS.OSX => PerfMapOSToken.OSX,
                TargetOS.FreeBSD => PerfMapOSToken.FreeBSD,
                TargetOS.NetBSD => PerfMapOSToken.NetBSD,
                TargetOS.SunOS => PerfMapOSToken.SunOS,
                _ => throw new NotImplementedException(details.OperatingSystem.ToString())
            };

            PerfMapAbiToken abiToken = details.Abi switch
            {
                TargetAbi.Unknown => PerfMapAbiToken.Unknown,
                TargetAbi.NativeAot => PerfMapAbiToken.Default,
                TargetAbi.NativeAotArmel => PerfMapAbiToken.Armel,
                _ => throw new NotImplementedException(details.Abi.ToString())
            };

            PerfMapArchitectureToken archToken = details.Architecture switch
            {
                TargetArchitecture.Unknown => PerfMapArchitectureToken.Unknown,
                TargetArchitecture.ARM => PerfMapArchitectureToken.ARM,
                TargetArchitecture.ARM64 => PerfMapArchitectureToken.ARM64,
                TargetArchitecture.X64 => PerfMapArchitectureToken.X64,
                TargetArchitecture.X86 => PerfMapArchitectureToken.X86,
                TargetArchitecture.RiscV64 => PerfMapArchitectureToken.RiscV64,
                _ => throw new NotImplementedException(details.Architecture.ToString())
            };

            return new PerfmapTokensForTarget(osToken, archToken, abiToken);
        }

        private void WriteLine(string methodName, uint rva, uint length)
        {
            _writer.WriteLine($@"{rva:X8} {length:X2} {methodName}");
        }
    }
}
