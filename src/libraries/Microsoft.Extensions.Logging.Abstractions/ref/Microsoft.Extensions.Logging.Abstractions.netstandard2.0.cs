// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventId
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public EventId(int id, string name = null) { throw null; }
        public int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Equals(Microsoft.Extensions.Logging.EventId other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Microsoft.Extensions.Logging.EventId left, Microsoft.Extensions.Logging.EventId right) { throw null; }
        public static implicit operator Microsoft.Extensions.Logging.EventId (int i) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Logging.EventId left, Microsoft.Extensions.Logging.EventId right) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial interface IExternalScopeProvider
    {
        void ForEachScope<TState>(System.Action<object, TState> callback, TState state);
        System.IDisposable Push(object state);
    }
    public partial interface ILogger
    {
        System.IDisposable BeginScope<TState>(TState state);
        bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel);
        void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter);
    }
    public partial interface ILoggerFactory : System.IDisposable
    {
        void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider);
        Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName);
    }
    public partial interface ILoggerProvider : System.IDisposable
    {
        Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName);
    }
    public partial interface ILogger<out TCategoryName> : Microsoft.Extensions.Logging.ILogger
    {
    }
    public partial interface ISupportExternalScope
    {
        void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider);
    }
    public static partial class LoggerExtensions
    {
        public static System.IDisposable BeginScope(this Microsoft.Extensions.Logging.ILogger logger, string messageFormat, params object[] args) { throw null; }
        public static void Log(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void Log(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void Log(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel logLevel, System.Exception exception, string message, params object[] args) { }
        public static void Log(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel logLevel, string message, params object[] args) { }
        public static void LogCritical(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogCritical(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogCritical(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogCritical(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
        public static void LogDebug(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogDebug(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogDebug(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogDebug(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
        public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
        public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
        public static void LogTrace(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogTrace(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogTrace(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogTrace(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
        public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, System.Exception exception, string message, params object[] args) { }
        public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.EventId eventId, string message, params object[] args) { }
        public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string message, params object[] args) { }
        public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args) { }
    }
    public partial class LoggerExternalScopeProvider : Microsoft.Extensions.Logging.IExternalScopeProvider
    {
        public LoggerExternalScopeProvider() { }
        public void ForEachScope<TState>(System.Action<object, TState> callback, TState state) { }
        public System.IDisposable Push(object state) { throw null; }
    }
    public static partial class LoggerFactoryExtensions
    {
        public static Microsoft.Extensions.Logging.ILogger CreateLogger(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Type type) { throw null; }
        public static Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>(this Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
    }
    public static partial class LoggerMessage
    {
        public static System.Action<Microsoft.Extensions.Logging.ILogger, System.Exception> Define(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Func<Microsoft.Extensions.Logging.ILogger, System.IDisposable> DefineScope(string formatString) { throw null; }
        public static System.Func<Microsoft.Extensions.Logging.ILogger, T1, System.IDisposable> DefineScope<T1>(string formatString) { throw null; }
        public static System.Func<Microsoft.Extensions.Logging.ILogger, T1, T2, System.IDisposable> DefineScope<T1, T2>(string formatString) { throw null; }
        public static System.Func<Microsoft.Extensions.Logging.ILogger, T1, T2, T3, System.IDisposable> DefineScope<T1, T2, T3>(string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, System.Exception> Define<T1>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, T2, System.Exception> Define<T1, T2>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, T2, T3, System.Exception> Define<T1, T2, T3>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, T2, T3, T4, System.Exception> Define<T1, T2, T3, T4>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, T2, T3, T4, T5, System.Exception> Define<T1, T2, T3, T4, T5>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
        public static System.Action<Microsoft.Extensions.Logging.ILogger, T1, T2, T3, T4, T5, T6, System.Exception> Define<T1, T2, T3, T4, T5, T6>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, string formatString) { throw null; }
    }
    public partial class Logger<T> : Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILogger<T>
    {
        public Logger(Microsoft.Extensions.Logging.ILoggerFactory factory) { }
        System.IDisposable Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state) { throw null; }
        bool Microsoft.Extensions.Logging.ILogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        void Microsoft.Extensions.Logging.ILogger.Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
    }
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6,
    }
}
namespace Microsoft.Extensions.Logging.Abstractions
{
    public partial class NullLogger : Microsoft.Extensions.Logging.ILogger
    {
        internal NullLogger() { }
        public static Microsoft.Extensions.Logging.Abstractions.NullLogger Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IDisposable BeginScope<TState>(TState state) { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
    }
    public partial class NullLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory, System.IDisposable
    {
        public static readonly Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory Instance;
        public NullLoggerFactory() { }
        public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
    }
    public partial class NullLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        internal NullLoggerProvider() { }
        public static Microsoft.Extensions.Logging.Abstractions.NullLoggerProvider Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) { throw null; }
        public void Dispose() { }
    }
    public partial class NullLogger<T> : Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILogger<T>
    {
        public static readonly Microsoft.Extensions.Logging.Abstractions.NullLogger<T> Instance;
        public NullLogger() { }
        public System.IDisposable BeginScope<TState>(TState state) { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
    }
}
