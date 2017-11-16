// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Represents the exception thrown when a component cannot be granted a license.</para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")] // must not, a Type is required in all constructors.
    public class LicenseException : SystemException
    {
        private object _instance;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the 
        ///    specified type.</para>
        /// </summary>
        public LicenseException(Type type) : this(type, null, SR.Format(SR.LicExceptionTypeOnly, type.FullName))
        {
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the 
        ///    specified type and instance.</para>
        /// </summary>
        public LicenseException(Type type, object instance) : this(type, null, SR.Format(SR.LicExceptionTypeAndInstance, type.FullName, instance.GetType().FullName))
        {
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the 
        ///    specified type and instance with the specified message.</para>
        /// </summary>
        public LicenseException(Type type, object instance, string message) : base(message)
        {
            LicensedType = type;
            _instance = instance;
            HResult = HResults.License;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.LicenseException'/> class for the 
        ///    specified innerException, type and instance with the specified message.</para>
        /// </summary>
        public LicenseException(Type type, object instance, string message, Exception innerException) : base(message, innerException)
        {
            LicensedType = type;
            _instance = instance;
            HResult = HResults.License;
        }

        /// <summary>
        ///     Need this constructor since Exception implements ISerializable. 
        /// </summary>
        protected LicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        ///    <para>Gets the type of the component that was not granted a license.</para>
        /// </summary>
        public Type LicensedType { get; }

        /// <summary>
        ///     Need this since Exception implements ISerializable.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("type", null); // Type is not serializable.
            info.AddValue("instance", _instance);
        }
    }
}
