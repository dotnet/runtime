// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#define DEBUG
using System.Reflection;
using Xunit;

namespace System.Diagnostics.Tests
{
    public abstract class DebugTests
    {
        protected abstract bool DebugUsesTraceListeners { get; }
        protected static readonly DebugProvider _debugOnlyProvider;
        protected static readonly DebugProvider _debugTraceProvider;

        static DebugTests()
        {
            FieldInfo fieldInfo = typeof(Debug).GetField("s_provider", BindingFlags.Static | BindingFlags.NonPublic);
            _debugOnlyProvider = (DebugProvider)fieldInfo.GetValue(null);
            // Triggers code to wire up TraceListeners with Debug
            Assert.Equal(1, Trace.Listeners.Count);
            _debugTraceProvider = (DebugProvider)fieldInfo.GetValue(null);
            Assert.NotEqual(_debugOnlyProvider.GetType(), _debugTraceProvider.GetType());
        }

        public DebugTests()
        {
            if (DebugUsesTraceListeners)
            {
                Debug.SetProvider(_debugTraceProvider);
            }
            else
            {
                Debug.SetProvider(_debugOnlyProvider);
            }
        }

        protected void VerifyLogged(Action test, string expectedOutput)
        {
            FieldInfo writeCoreHook = typeof(DebugProvider).GetField("s_WriteCore", BindingFlags.Static | BindingFlags.NonPublic);

            // First use our test logger to verify the output
            var originalWriteCoreHook = writeCoreHook.GetValue(null);
            writeCoreHook.SetValue(null, new Action<string>(WriteLogger.s_instance.WriteCore));

            try
            {
                WriteLogger.s_instance.Clear();
                test();
                Assert.Equal(expectedOutput, WriteLogger.s_instance.LoggedOutput);
            }
            finally
            {
                writeCoreHook.SetValue(null, originalWriteCoreHook);
            }

            // Then also use the actual logger for this platform, just to verify
            // that nothing fails.
            test();
        }

        protected void VerifyAssert(Action test, params string[] expectedOutputStrings)
        {
            FieldInfo writeCoreHook = typeof(DebugProvider).GetField("s_WriteCore", BindingFlags.Static | BindingFlags.NonPublic);
            var originalWriteCoreHook = writeCoreHook.GetValue(null);
            writeCoreHook.SetValue(null, new Action<string>(WriteLogger.s_instance.WriteCore));

            FieldInfo failCoreHook = typeof(DebugProvider).GetField("s_FailCore", BindingFlags.Static | BindingFlags.NonPublic);
            var originalFailCoreHook = failCoreHook.GetValue(null);
            failCoreHook.SetValue(null, new Action<string, string, string, string>(WriteLogger.s_instance.FailCore));

            try
            {
                WriteLogger.s_instance.Clear();
                test();
                for (int i = 0; i < expectedOutputStrings.Length; i++)
                {
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.s_instance.LoggedOutput);
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.s_instance.AssertUIOutput);
                }

            }
            finally
            {
                writeCoreHook.SetValue(null, originalWriteCoreHook);
                failCoreHook.SetValue(null, originalFailCoreHook);
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
