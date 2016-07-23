// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection 
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // This is not serializable because it is a reflection command.
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Missing : ISerializable
    {
        public static readonly Missing Value = new Missing();

        #region Constructor
        private Missing() { }
        #endregion

        #region ISerializable
        [System.Security.SecurityCritical]  // auto-generated_required
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            UnitySerializationHolder.GetUnitySerializationInfo(info, this);
        }
        #endregion
    }
}
