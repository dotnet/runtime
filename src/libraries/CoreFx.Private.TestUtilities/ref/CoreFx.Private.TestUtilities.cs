// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

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
        public static void Equal<T>(System.Collections.Generic.HashSet<T> expected, System.Collections.Generic.HashSet<T> actual) { }
        public static void GreaterThanOrEqualTo<T>(T actual, T greaterThanOrEqualTo, string userMessage = null) where T : System.IComparable { }
        public static void GreaterThan<T>(T actual, T greaterThan, string userMessage = null) where T : System.IComparable { }
        public static void LessThanOrEqualTo<T>(T actual, T lessThanOrEqualTo, string userMessage = null) where T : System.IComparable { }
        public static void LessThan<T>(T actual, T lessThan, string userMessage = null) where T : System.IComparable { }
        public static System.Exception Throws(System.Type netCoreExceptionType, System.Type netFxExceptionType, System.Action action) { throw null; }
        public static void ThrowsAny(System.Type firstExceptionType, System.Type secondExceptionType, System.Action action) { }
        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType>(System.Action action) where TFirstExceptionType : System.Exception where TSecondExceptionType : System.Exception { }
        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType, TThirdExceptionType>(System.Action action) where TFirstExceptionType : System.Exception where TSecondExceptionType : System.Exception where TThirdExceptionType : System.Exception { }
        public static System.Threading.Tasks.Task<T> ThrowsAsync<T>(string paramName, System.Func<System.Threading.Tasks.Task> testCode) where T : System.ArgumentException { throw null; }
        public static void ThrowsIf<T>(bool condition, System.Action action) where T : System.Exception { }
        public static T Throws<T>(System.Action action) where T : System.Exception { throw null; }
        public static void Throws<T>(System.Action action, string message) where T : System.Exception { }
        public static T Throws<T>(string paramName, System.Action action) where T : System.ArgumentException { throw null; }
        public static T Throws<T>(string paramName, System.Func<object> testCode) where T : System.ArgumentException { throw null; }
        public static void Throws<T>(string netCoreParamName, string netFxParamName, System.Action action) where T : System.ArgumentException { }
        public static void Throws<T>(string netCoreParamName, string netFxParamName, System.Func<object> testCode) where T : System.ArgumentException { }
        public static System.Exception Throws<TNetCoreExceptionType, TNetFxExceptionType>(System.Action action) where TNetCoreExceptionType : System.Exception where TNetFxExceptionType : System.Exception { throw null; }
        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string paramName, System.Action action) where TNetCoreExceptionType : System.ArgumentException where TNetFxExceptionType : System.Exception { }
        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string netCoreParamName, string netFxParamName, System.Action action) where TNetCoreExceptionType : System.ArgumentException where TNetFxExceptionType : System.ArgumentException { }
    }
    public static partial class PlatformDetection
    {
        public static bool IsReflectionEmitSupported;
        public static bool ClientWebSocketPartialMessagesSupported { get { throw null; } }
        public static bool HasWindowsShell { get { throw null; } }
        public static System.Version ICUVersion { get { throw null; } }
        public static bool Is32BitProcess { get { throw null; } }
        public static bool IsAlpine { get { throw null; } }
        public static bool IsArgIteratorNotSupported { get { throw null; } }
        public static bool IsArgIteratorSupported { get { throw null; } }
        public static bool IsArm64Process { get { throw null; } }
        public static bool IsArmOrArm64Process { get { throw null; } }
        public static bool IsArmProcess { get { throw null; } }
        public static bool IsCentos6 { get { throw null; } }
        public static bool IsDebian { get { throw null; } }
        public static bool IsDebian8 { get { throw null; } }
        public static bool IsDomainJoinedMachine { get { throw null; } }
        public static bool IsDrawingSupported { get { throw null; } }
        public static bool IsFedora { get { throw null; } }
        public static bool IsFreeBSD { get { throw null; } }
        public static bool IsFullFramework { get { throw null; } }
        public static bool IsInAppContainer { get { throw null; } }
        public static bool IsInContainer { get { throw null; } }
        public static bool IsInvokingStaticConstructorsSupported { get { throw null; } }
        public static bool IsMacOsHighSierraOrHigher { get { throw null; } }
        public static bool IsMacOsMojaveOrHigher { get { throw null; } }
        public static bool IsMonoRuntime { get { throw null; } }
        public static bool IsNetBSD { get { throw null; } }
        public static bool IsNetCore { get { throw null; } }
        public static bool IsNetfx462OrNewer { get { throw null; } }
        public static bool IsNetfx470OrNewer { get { throw null; } }
        public static bool IsNetfx471OrNewer { get { throw null; } }
        public static bool IsNetfx472OrNewer { get { throw null; } }
        public static bool IsNetNative { get { throw null; } }
        public static bool IsNonZeroLowerBoundArraySupported { get { throw null; } }
        public static bool IsNotArm64Process { get { throw null; } }
        public static bool IsNotArmNorArm64Process { get { throw null; } }
        public static bool IsNotArmProcess { get { throw null; } }
        public static bool IsNotFedoraOrRedHatFamily { get { throw null; } }
        public static bool IsNotInAppContainer { get { throw null; } }
        public static bool IsNotIntMaxValueArrayIndexSupported { get { throw null; } }
        public static bool IsNotMacOsHighSierraOrHigher { get { throw null; } }
        public static bool IsNotNetNative { get { throw null; } }
        public static bool IsNotNetNativeRunningAsConsoleApp { get { throw null; } }
        public static bool IsNotOneCoreUAP { get { throw null; } }
        public static bool IsNotOSX { get { throw null; } }
        public static bool IsNotRedHatFamily { get { throw null; } }
        public static bool IsNotRedHatFamily6 { get { throw null; } }
        public static bool IsNotWindows8x { get { throw null; } }
        public static bool IsNotWindowsHomeEdition { get { throw null; } }
        public static bool IsNotWindowsIoTCore { get { throw null; } }
        public static bool IsNotWindowsNanoServer { get { throw null; } }
        public static bool IsNotWindowsServerCore { get { throw null; } }
        public static bool IsNotWindowsSubsystemForLinux { get { throw null; } }
        public static bool IsNotWinRTSupported { get { throw null; } }
        public static bool IsOpenSUSE { get { throw null; } }
        public static bool IsOSX { get { throw null; } }
        public static bool IsPreciseGcSupported { get { throw null; } }
        public static bool IsRedHatFamily { get { throw null; } }
        public static bool IsRedHatFamily6 { get { throw null; } }
        public static bool IsRedHatFamily7 { get { throw null; } }
        public static bool IsSoundPlaySupported { get { throw null; } }
        public static bool IsSuperUser { get { throw null; } }
        public static bool IsTizen { get { throw null; } }
        public static bool IsUap { get { throw null; } }
        public static bool IsUbuntu { get { throw null; } }
        public static bool IsUbuntu1404 { get { throw null; } }
        public static bool IsUbuntu1604 { get { throw null; } }
        public static bool IsUbuntu1704 { get { throw null; } }
        public static bool IsUbuntu1710 { get { throw null; } }
        public static bool IsUbuntu1710OrHigher { get { throw null; } }
        public static bool IsUbuntu1804 { get { throw null; } }
        public static bool IsUbuntu1810OrHigher { get { throw null; } }
        public static bool IsWindows { get { throw null; } }
        public static bool IsWindows10Version1607OrGreater { get { throw null; } }
        public static bool IsWindows10Version1703OrGreater { get { throw null; } }
        public static bool IsWindows10Version1709OrGreater { get { throw null; } }
        public static bool IsWindows10Version1803OrGreater { get { throw null; } }
        public static bool IsWindows10Version1903OrGreater { get { throw null; } }
        public static bool IsWindows7 { get { throw null; } }
        public static bool IsWindows8x { get { throw null; } }
        public static bool IsWindows8xOrLater { get { throw null; } }
        public static bool IsWindowsAndElevated { get { throw null; } }
        public static bool IsWindowsHomeEdition { get { throw null; } }
        public static bool IsWindowsIoTCore { get { throw null; } }
        public static bool IsWindowsNanoServer { get { throw null; } }
        public static bool IsWindowsServerCore { get { throw null; } }
        public static bool IsWindowsSubsystemForLinux { get { throw null; } }
        public static bool IsWinRTSupported { get { throw null; } }
        public static bool IsXmlDsigXsltTransformSupported { get { throw null; } }
        public static string LibcRelease { get { throw null; } }
        public static string LibcVersion { get { throw null; } }
        public static System.Version OpenSslVersion { get { throw null; } }
        public static System.Version OSXVersion { get { throw null; } }
        public static bool SupportsAlpn { get { throw null; } }
        public static bool SupportsClientAlpn { get { throw null; } }
        public static bool SupportsSsl3 { get { throw null; } }
        public static bool SupportsTls13 { get { throw null; } }
        public static bool TargetsNetFx452OrLower { get { throw null; } }
        public static int WindowsVersion { get { throw null; } }
        public static string GetDistroVersionString() { throw null; }
    }
    public static partial class RetryHelper
    {
        public static void Execute(System.Action test, int maxAttempts = 5, System.Func<int, int> backoffFunc = null) { }
        public static System.Threading.Tasks.Task ExecuteAsync(Func<System.Threading.Tasks.Task> test, int maxAttempts = 5, System.Func<int, int> backoffFunc = null) { throw null; }
    }
    public static partial class TestEnvironment
    {
        public static bool IsStressModeEnabled { get { throw null; } }
    }
    public static partial class TheoryExtensions
    {
        [System.CLSCompliantAttribute(false)]
        public static Xunit.TheoryData ToTheoryData<T>(this System.Collections.Generic.IEnumerable<T> data) { throw null; }
    }
}
namespace System.IO
{
    public abstract partial class FileCleanupTestBase : System.IDisposable
    {
        protected FileCleanupTestBase() { }
        protected static bool IsProcessElevated { get { throw null; } }
        protected string TestDirectory { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~FileCleanupTestBase() { }
        protected string GetTestFileName(int? index = default(int?), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
        protected string GetTestFilePath(int? index = default(int?), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
    }
}
