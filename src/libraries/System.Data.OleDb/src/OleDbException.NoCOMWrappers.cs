// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System.Data.OleDb
{
    public sealed partial class OleDbException
    {
        internal static OleDbException CreateException(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult errorCode, Exception? inner)
        {
            OleDbErrorCollection errors = new OleDbErrorCollection(errorInfo);
            string? message = null;
            string? source = null;
            OleDbHResult hr = 0;

            if (null != errorInfo)
            {
                hr = errorInfo.GetDescription(out message);

                hr = errorInfo.GetSource(out source);
            }

            int count = errors.Count;
            if (0 < errors.Count)
            {
                StringBuilder builder = new StringBuilder();

                if ((null != message) && (message != errors[0].Message))
                {
                    builder.Append(message.TrimEnd(ODB.ErrorTrimCharacters));
                    if (1 < count)
                    {
                        builder.Append(Environment.NewLine);
                    }
                }
                for (int i = 0; i < count; ++i)
                {
                    if (0 < i)
                    {
                        builder.Append(Environment.NewLine);
                    }
                    builder.Append(errors[i].Message.TrimEnd(ODB.ErrorTrimCharacters));
                }
                message = builder.ToString();
            }
            if (ADP.IsEmpty(message))
            {
                message = ODB.NoErrorMessage(errorCode);
            }
            return new OleDbException(message, inner, source, errorCode, errors);
        }
    }
}
