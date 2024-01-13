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
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Checks whether the target object or one of its prototypes has a property with the specified name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns><see langword="true" /> when the object has the property with the specified name.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasProperty(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.HasProperty(this, propertyName);
        }

        /// <summary>
        /// Returns <code>typeof()</code> of the property.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>One of "undefined", "object", "boolean", "number", "bigint", "string", "symbol" or "function".</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetTypeOfProperty(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetTypeOfProperty(this, propertyName);
        }

        /// <summary>
        /// Returns the value of the specified property as <see cref="T:System.Boolean" /> if the property exists, otherwise <see langword="false" />.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a bool.</remarks>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetPropertyAsBoolean(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsBoolean(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="int"/> if the property exists, otherwise 0.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an integer.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPropertyAsInt32(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsInt32(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="double"/> if the property exists, otherwise 0.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a number.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetPropertyAsDouble(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsDouble(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="string"/> if the property exists, otherwise null.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not a string.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetPropertyAsString(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsString(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="JSObject"/> proxy if the property exists, otherwise null.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an object.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSObject? GetPropertyAsJSObject(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsJSObject(this, propertyName);
        }

        /// <summary>
        /// Returns value of the property as <see cref="byte"/> array if the property exists, otherwise null.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property with the specified name.</returns>
        /// <seealso cref="GetTypeOfProperty(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <remarks>Will throw <see cref="JSException"/> when the property value is not an array.</remarks>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? GetPropertyAsByteArray(string propertyName)
        {
            AssertNotDisposed();
            return JavaScriptImports.GetPropertyAsByteArray(this, propertyName);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, bool value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyBool(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, int value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyInt(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, double value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyDouble(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, string? value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyString(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, JSObject? value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyJSObject(this, propertyName, value);
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">Value of property to set.</param>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, byte[]? value)
        {
            AssertNotDisposed();
            JavaScriptImports.SetPropertyBytes(this, propertyName, value);
        }
    }
}
