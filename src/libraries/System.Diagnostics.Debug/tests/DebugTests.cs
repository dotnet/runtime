// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
            writeCoreHook.SetValue(null, new Action<string>(WriteLogger.s_instance.MockWrite));

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
            s_defaultProvider = Debug.SetProvider(WriteLogger.s_instance);
            WriteLogger.s_instance.OriginalProvider = s_defaultProvider;
            
            var originalWriteCoreHook = writeCoreHook.GetValue(null);
            writeCoreHook.SetValue(null, new Action<string>(WriteLogger.s_instance.MockWrite));

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
                Debug.SetProvider(s_defaultProvider);
            }
        }

        private static DebugProvider s_defaultProvider;
        private class WriteLogger : DebugProvider
        {
            public static readonly WriteLogger s_instance = new WriteLogger();

            private WriteLogger() { }

            public DebugProvider OriginalProvider { get; set; }

            public string LoggedOutput { get; private set; }

            public string AssertUIOutput { get; private set; }

            public void Clear()
            {
                LoggedOutput = string.Empty;
                AssertUIOutput = string.Empty;
            }

            public override void ShowDialog(string stackTrace, string message, string detailMessage, string errorSource)
            {
                AssertUIOutput += stackTrace + message + detailMessage + errorSource;
            }

            public override void OnIndentLevelChanged(int indentLevel)
            {
                OriginalProvider.OnIndentLevelChanged(indentLevel);
            }

            public override void OnIndentSizeChanged(int indentSize)
            {
                OriginalProvider.OnIndentLevelChanged(indentSize);
            }
            public override void Write(string message) { OriginalProvider.Write(message); }
            public override void WriteLine(string message) { OriginalProvider.WriteLine(message); }

            public void MockWrite(string message)
            {
                Assert.NotNull(message);
                LoggedOutput += message;
            }
        }
    }
}
