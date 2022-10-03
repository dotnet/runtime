// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class DiagnosticListener : System.Diagnostics.DiagnosticSource, System.IDisposable, System.IObservable<System.Collections.Generic.KeyValuePair<string, object?>>
    {
        public DiagnosticListener(string name) { }
        public static System.IObservable<System.Diagnostics.DiagnosticListener> AllListeners { get { throw null; } }
        public string Name { get { throw null; } }
        public virtual void Dispose() { }
        public bool IsEnabled() { throw null; }
        public override bool IsEnabled(string name) { throw null; }
        public override bool IsEnabled(string name, object? arg1, object? arg2 = null) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Func<string, object?, object?, bool>? isEnabled) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Predicate<string>? isEnabled) { throw null; }
        public override string ToString() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("DiagnosticSource may require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
        public override void Write(string name, object? value) { }
    }
    public abstract partial class DiagnosticSource
    {
        protected DiagnosticSource() { }
        public abstract bool IsEnabled(string name);
        public virtual bool IsEnabled(string name, object? arg1, object? arg2 = null) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("DiagnosticSource may require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
        public abstract void Write(string name, object? value);
    }
}
