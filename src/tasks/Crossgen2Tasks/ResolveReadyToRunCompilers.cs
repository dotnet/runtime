// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace Microsoft.NET.Build.Tasks
{
    public class ResolveReadyToRunCompilers : TaskBase
    {
        public bool EmitSymbols { get; set; }
        public bool ReadyToRunUseCrossgen2 { get; set; }
        public string PerfmapFormatVersion { get; set; }

        [Required]
        public ITaskItem[] RuntimePacks { get; set; }
        public ITaskItem[] Crossgen2Packs { get; set; }
        [Required]
        public ITaskItem[] TargetingPacks { get; set; }
        [Required]
        public string RuntimeGraphPath { get; set; }
        [Required]
        public string NETCoreSdkRuntimeIdentifier { get; set; }

        [Output]
        public ITaskItem CrossgenTool { get; set; }
        [Output]
        public ITaskItem Crossgen2Tool { get; set; }

        internal struct CrossgenToolInfo
        {
            public string ToolPath;
            public string PackagePath;
            public string ClrJitPath;
            public string DiaSymReaderPath;
        }

        private ITaskItem _runtimePack;
        private ITaskItem _crossgen2Pack;
        private string _targetRuntimeIdentifier;
        private string _targetPlatform;
        private string _hostRuntimeIdentifier;

        private CrossgenToolInfo _crossgenTool;
        private CrossgenToolInfo _crossgen2Tool;

        private Architecture _targetArchitecture;
        private bool _crossgen2IsVersion5;

        protected override void ExecuteCore()
        {
            _runtimePack = GetNETCoreAppRuntimePack();
            _crossgen2Pack = Crossgen2Packs?.FirstOrDefault();
            _targetRuntimeIdentifier = _runtimePack?.GetMetadata(MetadataKeys.RuntimeIdentifier);

            // Get the list of runtime identifiers that we support and can target
            ITaskItem targetingPack = GetNETCoreAppTargetingPack();
            string supportedRuntimeIdentifiers = targetingPack?.GetMetadata(MetadataKeys.RuntimePackRuntimeIdentifiers);

            var runtimeGraph = new RuntimeGraphCache(this).GetRuntimeGraph(RuntimeGraphPath);
            var supportedRIDsList = supportedRuntimeIdentifiers == null ? Array.Empty<string>() : supportedRuntimeIdentifiers.Split(';');

            // Get the best RID for the host machine, which will be used to validate that we can run crossgen for the target platform and architecture
            _hostRuntimeIdentifier = NuGetUtils.GetBestMatchingRid(
                runtimeGraph,
                NETCoreSdkRuntimeIdentifier,
                supportedRIDsList,
                out _);

            if (_hostRuntimeIdentifier == null || _targetRuntimeIdentifier == null)
            {
                Log.LogError(Strings.ReadyToRunNoValidRuntimePackageError);
                return;
            }

            if (ReadyToRunUseCrossgen2)
            {
                if (!ValidateCrossgen2Support())
                {
                    return;
                }

                // In .NET 5 Crossgen2 did not support emitting native symbols, so we use Crossgen to emit them
                if (_crossgen2IsVersion5 && EmitSymbols && !ValidateCrossgenSupport())
                {
                    return;
                }
            }
            else
            {
                if (!ValidateCrossgenSupport())
                {
                    return;
                }
            }
        }

        private bool ValidateCrossgenSupport()
        {
            _crossgenTool.PackagePath = _runtimePack?.GetMetadata(MetadataKeys.PackageDirectory);
            if (_crossgenTool.PackagePath == null)
            {
                Log.LogError(Strings.ReadyToRunNoValidRuntimePackageError);
                return false;
            }

            if (!ExtractTargetPlatformAndArchitecture(_targetRuntimeIdentifier, out _targetPlatform, out _targetArchitecture) ||
                !ExtractTargetPlatformAndArchitecture(_hostRuntimeIdentifier, out string hostPlatform, out _) ||
                _targetPlatform != hostPlatform ||
                !GetCrossgenComponentsPaths())
            {
                Log.LogError(Strings.ReadyToRunTargetNotSupportedError);
                return false;
            }

            // Create tool task item
            CrossgenTool = new TaskItem(_crossgenTool.ToolPath);
            CrossgenTool.SetMetadata(MetadataKeys.JitPath, _crossgenTool.ClrJitPath);
            if (!string.IsNullOrEmpty(_crossgenTool.DiaSymReaderPath))
            {
                CrossgenTool.SetMetadata(MetadataKeys.DiaSymReader, _crossgenTool.DiaSymReaderPath);
            }

            return true;
        }

        private bool ValidateCrossgen2Support()
        {
            _crossgen2Tool.PackagePath = _crossgen2Pack?.GetMetadata(MetadataKeys.PackageDirectory);
            if (_crossgen2Tool.PackagePath == null ||
                !NuGetVersion.TryParse(_crossgen2Pack.GetMetadata(MetadataKeys.NuGetPackageVersion), out NuGetVersion crossgen2PackVersion))
            {
                Log.LogError(Strings.ReadyToRunNoValidRuntimePackageError);
                return false;
            }

            bool version5 = crossgen2PackVersion.Major < 6;
            bool isSupportedTarget = ExtractTargetPlatformAndArchitecture(_targetRuntimeIdentifier, out _targetPlatform, out _targetArchitecture);

            // Normalize target OS for crossgen invocation
            string targetOS = (_targetPlatform == "win") ? "windows" :
                // Map linux-{ musl,bionic,etc.} to linux
                _targetPlatform.StartsWith("linux-", StringComparison.Ordinal) ? "linux" :
                _targetPlatform;

            // In .NET 5 Crossgen2 supported only the following host->target compilation scenarios:
            //      win-x64 -> win-x64
            //      linux-x64 -> linux-x64
            //      linux-musl-x64 -> linux-musl-x64
            isSupportedTarget = isSupportedTarget &&
                (!version5 || _targetRuntimeIdentifier == _hostRuntimeIdentifier) &&
                GetCrossgen2ComponentsPaths(version5);

            if (!isSupportedTarget)
            {
                Log.LogError(Strings.ReadyToRunTargetNotSupportedError);
                return false;
            }

            // Create tool task item
            Crossgen2Tool = new TaskItem(_crossgen2Tool.ToolPath);
            Crossgen2Tool.SetMetadata(MetadataKeys.IsVersion5, version5.ToString());
            if (version5)
            {
                Crossgen2Tool.SetMetadata(MetadataKeys.JitPath, _crossgen2Tool.ClrJitPath);
            }
            else
            {
                Crossgen2Tool.SetMetadata(MetadataKeys.TargetOS, targetOS);
                Crossgen2Tool.SetMetadata(MetadataKeys.TargetArch, ArchitectureToString(_targetArchitecture));
                if (!string.IsNullOrEmpty(PerfmapFormatVersion))
                {
                    Crossgen2Tool.SetMetadata(MetadataKeys.PerfmapFormatVersion, PerfmapFormatVersion);
                }
            }

            _crossgen2IsVersion5 = version5;
            return true;
        }

        private ITaskItem GetNETCoreAppRuntimePack()
        {
            return GetNETCoreAppPack(RuntimePacks, MetadataKeys.FrameworkName);
        }

        private ITaskItem GetNETCoreAppTargetingPack()
        {
            return GetNETCoreAppPack(TargetingPacks, MetadataKeys.RuntimeFrameworkName);
        }

        private static ITaskItem GetNETCoreAppPack(ITaskItem[] packs, string metadataKey)
        {
            return packs.SingleOrDefault(
                pack => pack.GetMetadata(metadataKey)
                            .Equals("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ExtractTargetPlatformAndArchitecture(string runtimeIdentifier, out string platform, out Architecture architecture)
        {
            platform = null;
            architecture = default;

            // This will split RID like "linux-musl-arm64" into "linux-musl" and "arm64" components
            int separator = runtimeIdentifier.LastIndexOf('-');
            if (separator < 0)
            {
                return false;
            }

            string architectureStr = runtimeIdentifier.Substring(separator + 1).ToLowerInvariant();

            switch (architectureStr)
            {
                case "arm":
                    architecture = Architecture.Arm;
                    break;
                case "arm64":
                    architecture = Architecture.Arm64;
                    break;
                case "x64":
                    architecture = Architecture.X64;
                    break;
                case "x86":
                    architecture = Architecture.X86;
                    break;
                case "riscv64":
                    architecture = Architecture.RiscV64;
                    break;
                default:
                    return false;
            }

            platform = runtimeIdentifier.Substring(0, separator).ToLowerInvariant();
            return true;
        }

        private bool GetCrossgenComponentsPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_targetArchitecture == Architecture.Arm)
                {
                    if (RuntimeInformation.OSArchitecture == _targetArchitecture)
                    {
                        // We can run native arm32 bits on an arm64 host in WOW mode
                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen.exe");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "clrjit.dll");
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "Microsoft.DiaSymReader.Native.arm.dll");
                    }
                    else
                    {
                        // We can use the x86-hosted crossgen compiler to target ARM
                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "x86_arm", "crossgen.exe");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", "x86_arm", "native", "clrjit.dll");
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", "x86_arm", "native", "Microsoft.DiaSymReader.Native.x86.dll");
                    }
                }
                else if (_targetArchitecture == Architecture.Arm64)
                {
                    if (RuntimeInformation.OSArchitecture == _targetArchitecture)
                    {
                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen.exe");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "clrjit.dll");
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "Microsoft.DiaSymReader.Native.arm64.dll");
                    }
                    else
                    {
                        // We only have 64-bit hosted compilers for ARM64.
                        if (RuntimeInformation.OSArchitecture != Architecture.X64)
                        {
                            return false;
                        }

                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "x64_arm64", "crossgen.exe");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", "x64_arm64", "native", "clrjit.dll");
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", "x64_arm64", "native", "Microsoft.DiaSymReader.Native.amd64.dll");
                    }
                }
                else
                {
                    _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen.exe");
                    _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "clrjit.dll");
                    if (_targetArchitecture == Architecture.X64)
                    {
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "Microsoft.DiaSymReader.Native.amd64.dll");
                    }
                    else
                    {
                        _crossgenTool.DiaSymReaderPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "Microsoft.DiaSymReader.Native.x86.dll");
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Only x64 supported for OSX
                if (_targetArchitecture != Architecture.X64 || RuntimeInformation.OSArchitecture != Architecture.X64)
                {
                    return false;
                }

                _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen");
                _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "libclrjit.dylib");
            }
            else
            {
                // Generic Unix, including Linux and FreeBSD
                if (_targetArchitecture == Architecture.Arm || _targetArchitecture == Architecture.Arm64)
                {
                    if (RuntimeInformation.OSArchitecture == _targetArchitecture)
                    {
                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "libclrjit.so");
                    }
                    else if (RuntimeInformation.OSArchitecture == Architecture.X64)
                    {
                        string xarchPath = (_targetArchitecture == Architecture.Arm ? "x64_arm" : "x64_arm64");
                        _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", xarchPath, "crossgen");
                        _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", xarchPath, "native", "libclrjit.so");
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _crossgenTool.ToolPath = Path.Combine(_crossgenTool.PackagePath, "tools", "crossgen");
                    _crossgenTool.ClrJitPath = Path.Combine(_crossgenTool.PackagePath, "runtimes", _targetRuntimeIdentifier, "native", "libclrjit.so");
                }
            }

            return File.Exists(_crossgenTool.ToolPath) && File.Exists(_crossgenTool.ClrJitPath);
        }

        private bool GetCrossgen2ComponentsPaths(bool version5)
        {
            string toolFileName, v5_clrJitFileNamePattern;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                toolFileName = "crossgen2.exe";
                v5_clrJitFileNamePattern = "clrjit-{0}.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                toolFileName = "crossgen2";
                v5_clrJitFileNamePattern = "libclrjit-{0}.dylib";
            }
            else
            {
                // Generic Unix, including Linux and FreeBSD
                toolFileName = "crossgen2";
                v5_clrJitFileNamePattern = "libclrjit-{0}.so";
            }

            if (version5)
            {
                string clrJitFileName = string.Format(v5_clrJitFileNamePattern, GetTargetSpecForVersion5());
                _crossgen2Tool.ClrJitPath = Path.Combine(_crossgen2Tool.PackagePath, "tools", clrJitFileName);
                if (!File.Exists(_crossgen2Tool.ClrJitPath))
                {
                    return false;
                }
            }

            _crossgen2Tool.ToolPath = Path.Combine(_crossgen2Tool.PackagePath, "tools", toolFileName);
            return File.Exists(_crossgen2Tool.ToolPath);
        }

        // Keep in sync with JitConfigProvider.GetTargetSpec in .NET 5
        private string GetTargetSpecForVersion5()
        {
            string targetOSComponent = (_targetPlatform == "win" ? "win" : "unix");
            string targetArchComponent = ArchitectureToString(_targetArchitecture);
            return targetOSComponent + '-' + targetArchComponent;
        }

        private static string ArchitectureToString(Architecture architecture)
        {
            return architecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                Architecture.RiscV64 => "riscv64",
                _ => null
            };
        }
    }
}
