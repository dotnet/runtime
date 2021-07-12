// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <summary>
    /// Represents the exception thrown when a component cannot be granted a license.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class LicenseException : SystemException
    {
        private const int LicenseHResult = unchecked((int)0x80131901);

        private readonly object? _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the
        /// specified type.
        /// </summary>
        public LicenseException(Type? type) : this(type, null, SR.Format(SR.LicExceptionTypeOnly, type?.FullName))
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the
        /// specified type and instance.
        /// </summary>
        public LicenseException(Type? type, object? instance) : this(type, null, SR.Format(SR.LicExceptionTypeAndInstance, type?.FullName, instance?.GetType().FullName))
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the
        /// specified type and instance with the specified message.
        /// </summary>
        public LicenseException(Type? type, object? instance, string? message) : base(message)
        {
            LicensedType = type;
            _instance = instance;
            HResult = LicenseHResult;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the
        /// specified innerException, type and instance with the specified message.
        /// </summary>
        public LicenseException(Type? type, object? instance, string? message, Exception? innerException) : base(message, innerException)
        {
            LicensedType = type;
            _instance = instance;
            HResult = LicenseHResult;
        }

        /// <summary>
        /// Need this constructor since Exception implements ISerializable.
        /// </summary>
        protected LicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Gets the type of the component that was not granted a license.
        /// </summary>
        public Type? LicensedType { get; }

        /// <summary>
        /// Need this since Exception implements ISerializable.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("type", null); // Type is not serializable.
            info.AddValue("instance", _instance);
        }
    }
}
