// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#define DEBUG
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public abstract class DebugTests
    {
        private const string DebugTypeName = "System.Diagnostics.Debug, System.Private.CoreLib";
        private const string DebugProviderTypeName = "System.Diagnostics.DebugProvider, System.Private.CoreLib";

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        [return: UnsafeAccessorType(DebugProviderTypeName)]
        private static extern object GetProvider([UnsafeAccessorType(DebugTypeName)] object? _);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        [return: UnsafeAccessorType(DebugProviderTypeName)]
        private static extern object SetProvider([UnsafeAccessorType(DebugTypeName)] object? _, [UnsafeAccessorType(DebugProviderTypeName)] object provider);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_WriteCore")]
        private static extern ref Action<string>? GetWriteCore([UnsafeAccessorType(DebugProviderTypeName)] object _);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_FailCore")]
        private static extern ref Action<string, string?, string?, string>? GetFailCore([UnsafeAccessorType(DebugProviderTypeName)] object _);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_needIndent")]
        private static extern ref bool GetNeedIndent(TraceListener listener);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_needIndent")]
        private static extern ref bool GetProviderNeedIndent([UnsafeAccessorType(DebugProviderTypeName)] object provider);

        protected abstract bool DebugUsesTraceListeners { get; }
        protected static readonly object _debugOnlyProvider;
        protected static readonly object _debugTraceProvider;

        static DebugTests()
        {
            // In xunit v3, trace listeners may already be initialized before this
            // static constructor runs, so the current provider could already be the
            // trace-aware provider.  Create a fresh DebugProvider to serve as the
            // debug-only provider (bypasses TraceListeners entirely).
            _debugOnlyProvider = Activator.CreateInstance(Type.GetType(DebugProviderTypeName)!)!;
            // Triggers code to wire up TraceListeners with Debug
            _ = Trace.Listeners.Count;
            _debugTraceProvider = GetProvider(null);
        }

        public DebugTests()
        {
            if (DebugUsesTraceListeners)
            {
                SetProvider(null, _debugTraceProvider);
            }
            else
            {
                SetProvider(null, _debugOnlyProvider);
            }

            // Reset indent state to known defaults; xunit3's TraceListener may have
            // modified these before tests run or a previous test may have leaked state.
            Debug.IndentLevel = 0;
            Debug.IndentSize = 4;

            // Reset NeedIndent on all TraceListeners so indentation starts fresh.
            foreach (TraceListener listener in Trace.Listeners)
            {
                listener.IndentLevel = 0;
                listener.IndentSize = 4;
                GetNeedIndent(listener) = true;
            }

            // Reset the DebugProvider's _needIndent as well.
            GetProviderNeedIndent(GetProvider(null)) = true;
        }

        protected void VerifyLogged(Action test, string expectedOutput)
        {
            ref Action<string>? writeCoreHook = ref GetWriteCore(null);

            // First use our test logger to verify the output
            var originalWriteCoreHook = writeCoreHook;
            writeCoreHook = WriteLogger.s_instance.WriteCore;

            try
            {
                WriteLogger.s_instance.Clear();
                test();
                Assert.Equal(expectedOutput, WriteLogger.s_instance.LoggedOutput);
            }
            finally
            {
                writeCoreHook = originalWriteCoreHook;
            }

            // Then also use the actual logger for this platform, just to verify
            // that nothing fails.
            test();
        }

        protected void VerifyAssert(Action test, params string[] expectedOutputStrings)
        {
            ref Action<string>? writeCoreHook = ref GetWriteCore(null);
            var originalWriteCoreHook = writeCoreHook;
            writeCoreHook = WriteLogger.s_instance.WriteCore;

            ref Action<string, string?, string?, string>? failCoreHook = ref GetFailCore(null);
            var originalFailCoreHook = failCoreHook;
            failCoreHook = WriteLogger.s_instance.FailCore;

            try
            {
                WriteLogger.s_instance.Clear();
                try
                {
                    test();
                }
                catch (Exception ex)
                {
                    if (WriteLogger.s_instance.AssertUIOutput == string.Empty)
                    {
                        // When using the trace provider, Debug.Assert(false) goes through
                        // TraceInternal.Fail → TraceListener.Fail. If xunit v3's listener
                        // throws before the DefaultTraceListener can call s_FailCore,
                        // capture the message from the exception instead.
                        WriteLogger.s_instance.FailCore("", ex.Message, "", "");
                    }

                    // If AssertUIOutput is already populated, the exception was thrown
                    // by a secondary listener (e.g., xunit v3's) after the assert was
                    // already captured via s_FailCore. Swallow it.
                }
                // Prefer AssertUIOutput (populated via s_FailCore). When using the
                // trace provider the assert message may only be captured through
                // WriteAssert → WriteCore → LoggedOutput instead.
                string output = WriteLogger.s_instance.AssertUIOutput;
                if (string.IsNullOrEmpty(output))
                {
                    output = WriteLogger.s_instance.LoggedOutput;
                }

                for (int i = 0; i < expectedOutputStrings.Length; i++)
                {
                    Assert.Contains(expectedOutputStrings[i], output);
                }

            }
            finally
            {
                writeCoreHook = originalWriteCoreHook;
                failCoreHook = originalFailCoreHook;
            }
        }

        internal class WriteLogger
        {
            public static readonly WriteLogger s_instance = new WriteLogger();

            private WriteLogger() { }

            public string LoggedOutput { get; private set; }

            public string AssertUIOutput { get; private set; }

            public void Clear()
            {
                LoggedOutput = string.Empty;
                AssertUIOutput = string.Empty;
            }

            public void FailCore(string stackTrace, string message, string detailMessage, string errorSource)
            {
                AssertUIOutput += stackTrace + message + detailMessage + errorSource;
            }

            public void WriteCore(string message)
            {
                Assert.NotNull(message);
                LoggedOutput += message;
            }
        }
    }
}
