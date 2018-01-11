// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Xunit;

namespace System
{
    public static partial class AdminHelpers
    {
        public static bool IsProcessElevated() { throw null; }
        public static int RunAsSudo(string commandLine) { throw null; }
    }
    public static partial class AssertExtensions
    {
        public static void Contains(string value, string substring) { }
        public static void Equal(byte[] expected, byte[] actual) { }
        public static void GreaterThanOrEqualTo<T>(T actual, T greaterThanOrEqualTo, string userMessage = null) where T : System.IComparable { }
        public static void GreaterThan<T>(T actual, T greaterThan, string userMessage = null) where T : System.IComparable { }
        public static void LessThanOrEqualTo<T>(T actual, T lessThanOrEqualTo, string userMessage = null) where T : System.IComparable { }
        public static void LessThan<T>(T actual, T lessThan, string userMessage = null) where T : System.IComparable { }
        public static void ThrowsAny(System.Type firstExceptionType, System.Type secondExceptionType, System.Action action) { }
        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType>(System.Action action) where TFirstExceptionType : System.Exception where TSecondExceptionType : System.Exception { }
        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType, TThirdExceptionType>(System.Action action) where TFirstExceptionType : System.Exception where TSecondExceptionType : System.Exception where TThirdExceptionType : System.Exception { }
        public static System.Threading.Tasks.Task<T> ThrowsAsync<T>(string paramName, System.Func<System.Threading.Tasks.Task> testCode) where T : System.ArgumentException { throw null; }
        public static void ThrowsIf<T>(bool condition, System.Action action) where T : System.Exception { }
        public static void Throws<T>(System.Action action, string message) where T : System.Exception { }
        public static T Throws<T>(string paramName, System.Action action) where T : System.ArgumentException { throw null; }
        public static T Throws<T>(string paramName, System.Func<object> testCode) where T : System.ArgumentException { throw null; }
        public static void Throws<T>(string netCoreParamName, string netFxParamName, System.Action action) where T : System.ArgumentException { }
        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string paramName, System.Action action) where TNetCoreExceptionType : System.ArgumentException where TNetFxExceptionType : System.ArgumentException { }
        public static Exception Throws<TNetCoreExceptionType, TNetFxExceptionType>(Action action) where TNetCoreExceptionType : Exception where TNetFxExceptionType : Exception { throw null; }
        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string netCoreParamName, string netFxParamName, System.Action action) where TNetCoreExceptionType : System.ArgumentException where TNetFxExceptionType : System.ArgumentException { }
    }
    public static partial class PlatformDetection
    {
        public static bool IsReflectionEmitSupported;
        public static bool ClientWebSocketPartialMessagesSupported { get { throw null; } }
        public static bool HasWindowsShell { get { throw null; } }
        public static bool IsArmProcess { get { throw null; } }
        public static bool IsCentos6 { get { throw null; } }
        public static bool IsDebian { get { throw null; } }
        public static bool IsDebian8 { get { throw null; } }
        public static bool IsDrawingSupported { get { throw null; } }
        public static bool IsFedora { get { throw null; } }
        public static bool IsFullFramework { get { throw null; } }
        public static bool IsInvokingStaticConstructorsSupported { get { throw null; } }
        public static bool IsMacOsHighSierraOrHigher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static bool IsNetBSD { get { throw null; } }
        public static bool IsFreeBSD { get { throw null; } }
        public static bool IsNetCore { get { throw null; } }
        public static bool IsNetfx462OrNewer { get { throw null; } }
        public static bool IsNetfx470OrNewer { get { throw null; } }
        public static bool IsNetfx471OrNewer { get { throw null; } }
        public static bool IsNetNative { get { throw null; } }
        public static bool IsNonZeroLowerBoundArraySupported { get { throw null; } }
        public static bool IsNotArmProcess { get { throw null; } }
        public static bool IsNotFedoraOrRedHatFamily { get { throw null; } }
        public static bool IsNotMacOsHighSierraOrHigher { get { throw null; } }
        public static bool IsNotNetNativeRunningAsConsoleApp { get { throw null; } }
        public static bool IsNotOneCoreUAP { get { throw null; } }
        public static bool IsNotWindows8x { get { throw null; } }
        public static bool IsNotWindowsIoTCore { get { throw null; } }
        public static bool IsNotWindowsNanoServer { get { throw null; } }
        public static bool IsNotWindowsServerCore { get { throw null; } }
        public static bool IsNotWindowsSubsystemForLinux { get { throw null; } }
        public static bool IsNotInAppContainer { get { throw null; } }
        public static bool IsNotWinRTSupported { get { throw null; } }
        public static bool IsOpenSUSE { get { throw null; } }
        public static bool IsOSX { get { throw null; } }
        public static bool IsSuperUser { get { throw null; } }
        public static bool IsTizen { get { throw null; } }
        public static bool IsRedHatFamily { get { throw null; } }
        public static bool IsNotRedHatFamily { get { throw null; } }
        public static bool IsRedHatFamily6 { get { throw null; } }
        public static bool IsRedHatFamily7 { get { throw null; } }
        public static bool IsNotRedHatFamily6 { get { throw null; } }
        public static bool IsUap { get { throw null; } }
        public static Version ICUVersion { get { return null; } }
        public static Version OpenSslVersion { get { return null; } }
        public static bool IsUbuntu { get { throw null; } }
        public static bool IsUbuntu1404 { get { throw null; } }
        public static bool IsUbuntu1604 { get { throw null; } }
        public static bool IsUbuntu1704 { get { throw null; } }
        public static bool IsUbuntu1710 { get { throw null; } }
        public static bool IsWindows { get { throw null; } }
        public static bool IsWindows10Version1607OrGreater { get { throw null; } } // >= Windows 10 Anniversary Update
        public static bool IsWindows10Version1703OrGreater { get { throw null; } } // >= Windows 10 Creators Update
        public static bool IsWindows10Version1709OrGreater { get { throw null; } } // >= Windows 10 Fall Creators Update
        public static bool IsWindows7 { get { throw null; } }
        public static bool IsWindows8x { get { throw null; } }
        public static bool IsWindowsAndElevated { get { throw null; } }
        public static bool IsWindowsIoTCore { get { throw null; } }
        public static bool IsWindowsNanoServer { get { throw null; } }
        public static bool IsWindowsServerCore { get { throw null; } }
        public static bool IsWindowsSubsystemForLinux { get { throw null; } }
        public static bool IsInAppContainer { get { throw null; } }
        public static bool IsWinRTSupported { get { throw null; } }
        public static bool IsXmlDsigXsltTransformSupported { get { throw null; } }
        public static System.Version OSXVersion { get { throw null; } }
        public static int WindowsVersion { get { throw null; } }
        public static string GetDistroVersionString() { throw null; }
        public static bool TargetsNetFx452OrLower { get { throw null; } }
        public static bool IsDomainJoinedMachine { get { throw null; } }
    }
    public static partial class TheoryExtensions
    {
        [CLSCompliant(false)]
        public static Xunit.TheoryData ToTheoryData<T>(this System.Collections.Generic.IEnumerable<T> data) { throw null; }
    }
}
namespace System.Diagnostics
{
    public abstract partial class RemoteExecutorTestBase : System.IO.FileCleanupTestBase
    {
        public const int FailWaitTimeoutMilliseconds = 60000;
        protected static readonly string HostRunner;
        protected static readonly string HostRunnerName;
        public const int SuccessExitCode = 42;
        protected static readonly string TestConsoleApp;
        protected RemoteExecutorTestBase() { }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Action method, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<int> method, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, int> method, string arg, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, int> method, string arg1, string arg2, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, int> method, string arg1, string arg2, string arg3, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, string, int> method, string arg1, string arg2, string arg3, string arg4, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, string, string, int> method, string arg1, string arg2, string arg3, string arg4, string arg5, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<System.Threading.Tasks.Task<int>> method, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, System.Threading.Tasks.Task<int>> method, string arg, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvokeRaw(System.Delegate method, string unparsedArg, System.Diagnostics.RemoteInvokeOptions options = null) { throw null; }
        public sealed partial class RemoteInvokeHandle : System.IDisposable
        {
            public RemoteInvokeHandle(System.Diagnostics.Process process, System.Diagnostics.RemoteInvokeOptions options) { }
            public System.Diagnostics.RemoteInvokeOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public System.Diagnostics.Process Process { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public void Dispose() { }
        }
    }
    public sealed partial class RemoteInvokeOptions
    {
        public RemoteInvokeOptions() { }
        public bool CheckExitCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool EnableProfiling { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ExceptionFile { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int ExpectedExitCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Start { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.ProcessStartInfo StartInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int TimeOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace System.IO
{
    public abstract partial class FileCleanupTestBase : System.IDisposable
    {
        protected FileCleanupTestBase() { }
        protected string TestDirectory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~FileCleanupTestBase() { }
        protected string GetTestFileName(System.Nullable<int> index = default(System.Nullable<int>), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
        protected string GetTestFilePath(System.Nullable<int> index = default(System.Nullable<int>), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
    }
}
