// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.InteropServices.JavaScript;

[System.AttributeUsageAttribute(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[Versioning.SupportedOSPlatformAttribute("browser")]
public sealed class JSImportAttribute : Attribute
{
    public JSImportAttribute(string functionName) { throw null; }
    public JSImportAttribute(string functionName, string moduleName) { throw null; }
    public string FunctionName { get { throw null; } }
    public string? ModuleName { get { throw null; } }
}

[System.AttributeUsageAttribute(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[Versioning.SupportedOSPlatformAttribute("browser")]
public sealed class JSExportAttribute : Attribute
{
    public JSExportAttribute() { throw null; }
}

[System.AttributeUsageAttribute(AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = false)]
[Versioning.SupportedOSPlatformAttribute("browser")]
public sealed class JSMarshalAsAttribute<T> : Attribute where T : JSType
{
    public JSMarshalAsAttribute() { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
public abstract class JSType
{
    internal JSType() { throw null; }
    public sealed class Void : JSType
    {
        internal Void() { throw null; }
    }
    public sealed class Discard : JSType
    {
        internal Discard() { throw null; }
    }
    public sealed class Boolean : JSType
    {
        internal Boolean() { throw null; }
    }
    public sealed class Number : JSType
    {
        internal Number() { throw null; }
    }
    public sealed class BigInt : JSType
    {
        internal BigInt() { throw null; }
    }
    public sealed class Date : JSType
    {
        internal Date() { throw null; }
    }
    public sealed class String : JSType
    {
        internal String() { throw null; }
    }
    public sealed class Object : JSType
    {
        internal Object() { throw null; }
    }
    public sealed class Error : JSType
    {
        internal Error() { throw null; }
    }
    public sealed class MemoryView : JSType
    {
        internal MemoryView() { throw null; }
    }
    public sealed class Array<T> : JSType where T : JSType
    {
        internal Array() { throw null; }
    }
    public sealed class Promise<T> : JSType where T : JSType
    {
        internal Promise() { throw null; }
    }
    public sealed class Function : JSType
    {
        internal Function() { throw null; }
    }
    public sealed class Function<T> : JSType where T : JSType
    {
        internal Function() { throw null; }
    }
    public sealed class Function<T1, T2> : JSType where T1 : JSType where T2 : JSType
    {
        internal Function() { throw null; }
    }
    public sealed class Function<T1, T2, T3> : JSType where T1 : JSType where T2 : JSType where T3 : JSType
    {
        internal Function() { throw null; }
    }
    public sealed class Function<T1, T2, T3, T4> : JSType where T1 : JSType where T2 : JSType where T3 : JSType where T4 : JSType
    {
        internal Function() { throw null; }
    }
    public sealed class Any : JSType
    {
        internal Any() { throw null; }
    }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
public class JSObject : IDisposable
{
    internal JSObject() { throw null; }
    public bool IsDisposed { get { throw null; } }
    public void Dispose() { throw null; }

    public bool HasProperty(string propertyName) { throw null; }
    public string GetTypeOfProperty(string propertyName) { throw null; }

    public bool GetPropertyAsBoolean(string propertyName) { throw null; }
    public int GetPropertyAsInt32(string propertyName) { throw null; }
    public double GetPropertyAsDouble(string propertyName) { throw null; }
    public string? GetPropertyAsString(string propertyName) { throw null; }
    public JSObject? GetPropertyAsJSObject(string propertyName) { throw null; }
    public byte[]? GetPropertyAsByteArray(string propertyName) { throw null; }

    public void SetProperty(string propertyName, bool value) { throw null; }
    public void SetProperty(string propertyName, int value) { throw null; }
    public void SetProperty(string propertyName, double value) { throw null; }
    public void SetProperty(string propertyName, string? value) { throw null; }
    public void SetProperty(string propertyName, JSObject? value) { throw null; }
    public void SetProperty(string propertyName, byte[]? value) { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
public sealed class JSException : Exception
{
    public JSException(string msg) { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
public static class JSHost
{
    public static JSObject GlobalThis { get { throw null; } }
    public static JSObject DotnetInstance { get { throw null; } }
    public static System.Threading.Tasks.Task<JSObject> ImportAsync(string moduleName, string moduleUrl, System.Threading.CancellationToken cancellationToken = default) { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
[CLSCompliant(false)]
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class JSFunctionBinding
{
    public static void InvokeJS(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments) { throw null; }
    public static JSFunctionBinding BindJSFunction(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures) { throw null; }
    public static JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures) { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class JSMarshalerType
{
    private JSMarshalerType() { throw null; }
    public static JSMarshalerType Void { get { throw null; } }
    public static JSMarshalerType Discard { get { throw null; } }
    public static JSMarshalerType Boolean { get { throw null; } }
    public static JSMarshalerType Byte { get { throw null; } }
    public static JSMarshalerType Char { get { throw null; } }
    public static JSMarshalerType Int16 { get { throw null; } }
    public static JSMarshalerType Int32 { get { throw null; } }
    public static JSMarshalerType Int52 { get { throw null; } }
    public static JSMarshalerType BigInt64 { get { throw null; } }
    public static JSMarshalerType Double { get { throw null; } }
    public static JSMarshalerType Single { get { throw null; } }
    public static JSMarshalerType IntPtr { get { throw null; } }
    public static JSMarshalerType JSObject { get { throw null; } }
    public static JSMarshalerType Object { get { throw null; } }
    public static JSMarshalerType String { get { throw null; } }
    public static JSMarshalerType Exception { get { throw null; } }
    public static JSMarshalerType DateTime { get { throw null; } }
    public static JSMarshalerType DateTimeOffset { get { throw null; } }
    public static JSMarshalerType Nullable(JSMarshalerType primitive) { throw null; }
    public static JSMarshalerType Task() { throw null; }
    public static JSMarshalerType Task(JSMarshalerType result) { throw null; }
    public static JSMarshalerType Array(JSMarshalerType element) { throw null; }
    public static JSMarshalerType ArraySegment(JSMarshalerType element) { throw null; }
    public static JSMarshalerType Span(JSMarshalerType element) { throw null; }
    public static JSMarshalerType Action() { throw null; }
    public static JSMarshalerType Action(JSMarshalerType arg1) { throw null; }
    public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2) { throw null; }
    public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3) { throw null; }
    public static JSMarshalerType Function(JSMarshalerType result) { throw null; }
    public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType result) { throw null; }
    public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType result) { throw null; }
    public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3, JSMarshalerType result) { throw null; }
}

[Versioning.SupportedOSPlatformAttribute("browser")]
[CLSCompliant(false)]
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public struct JSMarshalerArgument
{
    public delegate void ArgumentToManagedCallback<T>(ref JSMarshalerArgument arg, out T value);
    public delegate void ArgumentToJSCallback<T>(ref JSMarshalerArgument arg, T value);
    public void Initialize() { throw null; }
    public void ToManaged(out bool value) { throw null; }
    public void ToJS(bool value) { throw null; }
    public void ToManaged(out bool? value) { throw null; }
    public void ToJS(bool? value) { throw null; }
    public void ToManaged(out byte value) { throw null; }
    public void ToJS(byte value) { throw null; }
    public void ToManaged(out byte? value) { throw null; }
    public void ToJS(byte? value) { throw null; }
    public void ToManaged(out byte[]? value) { throw null; }
    public void ToJS(byte[]? value) { throw null; }
    public void ToManaged(out char value) { throw null; }
    public void ToJS(char value) { throw null; }
    public void ToManaged(out char? value) { throw null; }
    public void ToJS(char? value) { throw null; }
    public void ToManaged(out short value) { throw null; }
    public void ToJS(short value) { throw null; }
    public void ToManaged(out short? value) { throw null; }
    public void ToJS(short? value) { throw null; }
    public void ToManaged(out int value) { throw null; }
    public void ToJS(int value) { throw null; }
    public void ToManaged(out int? value) { throw null; }
    public void ToJS(int? value) { throw null; }
    public void ToManaged(out int[]? value) { throw null; }
    public void ToJS(int[]? value) { throw null; }
    public void ToManaged(out long value) { throw null; }
    public void ToJS(long value) { throw null; }
    public void ToManaged(out long? value) { throw null; }
    public void ToJS(long? value) { throw null; }
    public void ToManagedBig(out long value) { throw null; }
    public void ToJSBig(long value) { throw null; }
    public void ToManagedBig(out long? value) { throw null; }
    public void ToJSBig(long? value) { throw null; }
    public void ToManaged(out float value) { throw null; }
    public void ToJS(float value) { throw null; }
    public void ToManaged(out float? value) { throw null; }
    public void ToJS(float? value) { throw null; }
    public void ToManaged(out double value) { throw null; }
    public void ToJS(double value) { throw null; }
    public void ToManaged(out double? value) { throw null; }
    public void ToJS(double? value) { throw null; }
    public void ToManaged(out double[]? value) { throw null; }
    public void ToJS(double[]? value) { throw null; }
    public void ToManaged(out IntPtr value) { throw null; }
    public void ToJS(IntPtr value) { throw null; }
    public void ToManaged(out IntPtr? value) { throw null; }
    public void ToJS(IntPtr? value) { throw null; }
    public void ToManaged(out DateTimeOffset value) { throw null; }
    public void ToJS(DateTimeOffset value) { throw null; }
    public void ToManaged(out DateTimeOffset? value) { throw null; }
    public void ToJS(DateTimeOffset? value) { throw null; }
    public void ToManaged(out DateTime value) { throw null; }
    public void ToJS(DateTime value) { throw null; }
    public void ToManaged(out DateTime? value) { throw null; }
    public void ToJS(DateTime? value) { throw null; }
    public void ToManaged(out string? value) { throw null; }
    public void ToJS(string? value) { throw null; }
    public void ToManaged(out string?[]? value) { throw null; }
    public void ToJS(string?[]? value) { throw null; }
    public void ToManaged(out Exception? value) { throw null; }
    public void ToJS(Exception? value) { throw null; }
    public void ToManaged(out object? value) { throw null; }
    public void ToJS(object? value) { throw null; }
    public void ToManaged(out object?[]? value) { throw null; }
    public void ToJS(object?[]? value) { throw null; }
    public void ToManaged(out JSObject? value) { throw null; }
    public void ToJS(JSObject? value) { throw null; }
    public void ToManaged(out JSObject?[]? value) { throw null; }
    public void ToJS(JSObject?[]? value) { throw null; }
    public void ToManaged(out System.Threading.Tasks.Task? value) { throw null; }
    public void ToJS(System.Threading.Tasks.Task? value) { throw null; }
    public void ToManaged<T>(out System.Threading.Tasks.Task<T>? value, ArgumentToManagedCallback<T> marshaler) { throw null; }
    public void ToJS<T>(System.Threading.Tasks.Task<T>? value, ArgumentToJSCallback<T> marshaler) { throw null; }
    public void ToManaged(out Action? value) { throw null; }
    public void ToJS(Action? value) { throw null; }
    public void ToManaged<T>(out Action<T>? value, ArgumentToJSCallback<T> arg1Marshaler) { throw null; }
    public void ToJS<T>(Action<T>? value, ArgumentToManagedCallback<T> arg1Marshaler) { throw null; }
    public void ToManaged<T1, T2>(out Action<T1, T2>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler) { throw null; }
    public void ToJS<T1, T2>(Action<T1, T2>? value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler) { throw null; }
    public void ToManaged<T1, T2, T3>(out Action<T1, T2, T3>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler) { throw null; }
    public void ToJS<T1, T2, T3>(Action<T1, T2, T3>? value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler) { throw null; }
    public void ToManaged<TResult>(out Func<TResult>? value, ArgumentToManagedCallback<TResult> resMarshaler) { throw null; }
    public void ToJS<TResult>(Func<TResult>? value, ArgumentToJSCallback<TResult> resMarshaler) { throw null; }
    public void ToManaged<T, TResult>(out Func<T, TResult>? value, ArgumentToJSCallback<T> arg1Marshaler, ArgumentToManagedCallback<TResult> resMarshaler) { throw null; }
    public void ToJS<T, TResult>(Func<T, TResult>? value, ArgumentToManagedCallback<T> arg1Marshaler, ArgumentToJSCallback<TResult> resMarshaler) { throw null; }
    public void ToManaged<T1, T2, TResult>(out Func<T1, T2, TResult>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToManagedCallback<TResult> resMarshaler) { throw null; }
    public void ToJS<T1, T2, TResult>(Func<T1, T2, TResult>? value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToJSCallback<TResult> resMarshaler) { throw null; }
    public void ToManaged<T1, T2, T3, TResult>(out Func<T1, T2, T3, TResult>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler, ArgumentToManagedCallback<TResult> resMarshaler) { throw null; }
    public void ToJS<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult>? value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler, ArgumentToJSCallback<TResult> resMarshaler) { throw null; }
    public unsafe void ToManaged(out void* value) { throw null; }
    public unsafe void ToJS(void* value) { throw null; }
    public void ToManaged(out Span<byte> value) { throw null; }
    public void ToJS(Span<byte> value) { throw null; }
    public void ToManaged(out ArraySegment<byte> value) { throw null; }
    public void ToJS(ArraySegment<byte> value) { throw null; }
    public void ToManaged(out Span<int> value) { throw null; }
    public void ToJS(Span<int> value) { throw null; }
    public void ToManaged(out Span<double> value) { throw null; }
    public void ToJS(Span<double> value) { throw null; }
    public void ToManaged(out ArraySegment<int> value) { throw null; }
    public void ToJS(ArraySegment<int> value) { throw null; }
    public void ToManaged(out ArraySegment<double> value) { throw null; }
    public void ToJS(ArraySegment<double> value) { throw null; }
}
