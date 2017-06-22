// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Drawing.Printing
{
    [Serializable]
    partial class InvalidPrinterException
    {
        protected InvalidPrinterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _settings = (PrinterSettings)info.GetValue("settings", typeof(PrinterSettings));
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("settings", _settings);
            base.GetObjectData(info, context);
        }
    }
}

