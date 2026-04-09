// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        /// <summary>
        ///     Get the value of an arbitrary property
        /// </summary>
        public CngProperty GetProperty(string name, CngPropertyOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);

            byte[]? value = _keyHandle.GetProperty(name, options);
            if (value == null)
                throw ErrorCode.NTE_NOT_FOUND.ToCryptographicException();

            if (value.Length == 0)
                value = null;   // .NET Framework compat: For some reason, CngKey.GetProperty() morphs zero length property values to null.

            return new CngProperty(name, value, options);
        }

        /// <summary>
        ///     Determine if a property exists on the key
        /// </summary>
        public bool HasProperty(string name, CngPropertyOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);

            unsafe
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(_keyHandle, name, null, 0, out _, options);
                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                    return false;
                if (errorCode != ErrorCode.ERROR_SUCCESS)
                    throw errorCode.ToCryptographicException();
                return true;
            }
        }

        /// <summary>
        ///     Set an arbitrary property on the key
        /// </summary>
        public void SetProperty(CngProperty property)
        {
            unsafe
            {
                byte[]? propertyValue = property.GetValueWithoutCopying();

                // .NET Framework compat. It would have nicer to throw an ArgumentNull exception or something...
                if (propertyValue == null)
                    throw ErrorCode.NTE_INVALID_PARAMETER.ToCryptographicException();

                fixed (byte* pinnedPropertyValue = MapZeroLengthArrayToNonNullPointer(propertyValue))
                {
                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(_keyHandle, property.Name, pinnedPropertyValue, propertyValue.Length, property.Options);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                }
            }
        }
    }
}
