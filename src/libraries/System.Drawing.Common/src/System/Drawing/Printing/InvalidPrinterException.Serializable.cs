// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Drawing.Printing
{
    partial class InvalidPrinterException
    {
        protected InvalidPrinterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _settings = (PrinterSettings)info.GetValue("settings", typeof(PrinterSettings));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("settings", _settings);
        }
    }
}

