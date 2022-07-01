// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Holds proxy of JavaScript object.
    /// </summary>
    /// <remarks>Proxies are relatively expensive object. Developers could manualy <see cref="Dispose()"/> them to save runtime resources.</remarks>
    [SupportedOSPlatform("browser")]
    public partial class JSObject : IDisposable
    {
        /// <summary>
        /// Returns true if the proxy was already disposed.
        /// </summary>
        public bool IsDisposed { get => _isDisposed; }

        /// <summary>
        /// Returns true when the object contains the property.
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetPropertyAsBoolean(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns value of the property as <see cref="int"/> if the property exists, otherwise 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPropertyAsInt32(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns value of the property as <see cref="string"/> if the property exists, otherwise 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetPropertyAsDouble(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns value of the property as <see cref="string"/> if the property exists, otherwise null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetPropertyAsString(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns value of the property as <see cref="JSObject"/> proxy if the property exists, otherwise null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSObject? GetPropertyAsJSObject(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns value of the property as <see cref="byte"/> array if the property exists, otherwise null.
        /// </summary>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? GetPropertyAsByteArray(string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, bool value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, int value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, double value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, string? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, JSObject? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        /// <remarks>The method will copy the bytes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(string propertyName, byte[]? value)
        {
            throw new NotImplementedException();
        }
    }
}
