// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents a reference to an object in the JavaScript host environment and enables interaction with it as a proxy.
    /// </summary>
    /// <remarks>JSObject instances are expensive, so use <see cref="Dispose()"/> to release instances once you no longer need to retain a reference to the target object.</remarks>
    [SupportedOSPlatform("browser")]
    public partial class JSObject : IDisposable
    {
        /// <summary>
        /// Returns true if the proxy was already disposed.
        /// </summary>
        public bool IsDisposed { get => _isDisposed; }

        /// <summary>
        /// Checks whether the target object or one of its prototypes has a property with the specified name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasProperty(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.HasProperty(this, propertyName);
        }

        /// <summary>
        /// Returns <code>typeof()</code> of the property.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetTypeOfProperty(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetTypeOfProperty(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="bool"/> if the property exists, otherwise false.
        /// </summary>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a bool.</remarks>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetPropertyAsBoolean(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsBoolean(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="int"/> if the property exists, otherwise 0.
        /// </summary>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an integer.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPropertyAsInt32(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsInt32(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="double"/> if the property exists, otherwise 0.
        /// </summary>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a number.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetPropertyAsDouble(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsDouble(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="string"/> if the property exists, otherwise null.
        /// </summary>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a string.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetPropertyAsString(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsString(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="JSObject"/> proxy if the property exists, otherwise null.
        /// </summary>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an object.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSObject? GetPropertyAsJSObject(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsJSObject(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="byte"/> array if the property exists, otherwise null.
        /// </summary>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an array.</remarks>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? GetPropertyAsByteArray(string propertyName)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return JavaScriptImports.GetPropertyAsByteArray(this, propertyName);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, bool value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyBool(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, int value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyInt(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, double value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyDouble(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, string? value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyString(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, JSObject? value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyJSObject(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, byte[]? value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            JavaScriptImports.SetPropertyBytes(this, propertyName, value);
        }
    }
}
