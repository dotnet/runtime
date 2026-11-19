// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    public class StackTraceSymbolsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst | TestPlatforms.Android | TestPlatforms.Wasi, "Symbols are in a different location on mobile platforms and WASI")]
        public void StackTraceSymbolsDoNotLockFile()
        {
            var asmPath = AssemblyPathHelper.GetAssemblyLocation(typeof(StackTraceSymbolsTests).Assembly);
            var pdbPath = Path.ChangeExtension(asmPath, ".pdb");

            bool fileBackedSymbols = !PlatformDetection.IsBrowser;
            Assert.Equal(fileBackedSymbols, File.Exists(pdbPath));
            new StackTrace(true).GetFrames();
            if (fileBackedSymbols)
            {
                File.Move(pdbPath, pdbPath);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser))]
        public void StackTraceSymbolsFromMemory()
        {
            string assemblyPath = AssemblyPathHelper.GetAssemblyLocation(typeof(StackTraceSymbolsTests).Assembly);
            Assert.False(File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")));

            StackFrame frameWithoutSymbols = CaptureFrame(needFileInfo: false);
            Assert.Null(frameWithoutSymbols.GetFileName());
            Assert.Equal(0, frameWithoutSymbols.GetFileLineNumber());

            for (int i = 0; i < 2; i++)
            {
                StackFrame frame = CaptureFrame(needFileInfo: true);
                Assert.Equal(nameof(CaptureFrame), frame.GetMethod().Name);
                Assert.True(frame.GetILOffset() >= 0, $"Expected an IL offset for {frame}.");
                Assert.Equal("InMemorySymbols.cs", Path.GetFileName(frame.GetFileName()?.Replace('\\', '/')));
                Assert.Equal(123, frame.GetFileLineNumber());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StackFrame CaptureFrame(bool needFileInfo)
        {
#line 123 "InMemorySymbols.cs"
            return new StackFrame(needFileInfo);
#line default
        }
    }
}
