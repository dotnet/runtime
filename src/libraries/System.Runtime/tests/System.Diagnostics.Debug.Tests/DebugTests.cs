// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#define DEBUG
using System.Reflection;
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

        protected abstract bool DebugUsesTraceListeners { get; }
        protected static readonly object _debugOnlyProvider;
        protected static readonly object _debugTraceProvider;

        static DebugTests()
        {
            FieldInfo fieldInfo = typeof(Debug).GetField("s_provider", BindingFlags.Static | BindingFlags.NonPublic);
            _debugOnlyProvider = GetProvider(null);
            // Triggers code to wire up TraceListeners with Debug
            Assert.Equal(1, Trace.Listeners.Count);
            _debugTraceProvider = GetProvider(null);
            Assert.NotEqual(_debugOnlyProvider.GetType(), _debugTraceProvider.GetType());
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
                test();
                for (int i = 0; i < expectedOutputStrings.Length; i++)
                {
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.s_instance.LoggedOutput);
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.s_instance.AssertUIOutput);
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
