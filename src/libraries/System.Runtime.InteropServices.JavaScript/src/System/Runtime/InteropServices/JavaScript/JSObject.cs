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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns <code>typeof()</code> of the property.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetTypeOfProperty(string propertyName)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, bool value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, int value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, double value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, string? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, JSObject? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defines a new property on the target object, or modifies an existing property to have the specified value.
        /// </summary>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, byte[]? value)
        {
            throw new NotImplementedException();
        }
    }
}
