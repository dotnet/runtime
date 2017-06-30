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
    public static class TheoryExtensions
    {
        [CLSCompliant(false)]
        public static TheoryData ToTheoryData<T>(this IEnumerable<T> data) { throw null; }
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
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<int> method, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, int> method, string arg, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, int> method, string arg1, string arg2, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, int> method, string arg1, string arg2, string arg3, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, string, int> method, string arg1, string arg2, string arg3, string arg4, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<string, string, string, string, string, int> method, string arg1, string arg2, string arg3, string arg4, string arg5, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvoke(System.Func<System.Threading.Tasks.Task<int>> method, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public static System.Diagnostics.RemoteExecutorTestBase.RemoteInvokeHandle RemoteInvokeRaw(System.Delegate method, string unparsedArg, System.Diagnostics.RemoteInvokeOptions options=null) { throw null; }
        public sealed partial class RemoteInvokeHandle : System.IDisposable
        {
            public RemoteInvokeHandle(System.Diagnostics.Process process, System.Diagnostics.RemoteInvokeOptions options) { }
            public System.Diagnostics.RemoteInvokeOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public System.Diagnostics.Process Process { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
            public void Dispose() { }
        }
    }
    public sealed partial class RemoteInvokeOptions
    {
        public RemoteInvokeOptions() { }
        public bool CheckExitCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool EnableProfiling { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        protected string GetTestFileName(System.Nullable<int> index=default(System.Nullable<int>), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName=null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber=0) { throw null; }
        protected string GetTestFilePath(System.Nullable<int> index=default(System.Nullable<int>), [System.Runtime.CompilerServices.CallerMemberNameAttribute]string memberName=null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber=0) { throw null; }
    }
}