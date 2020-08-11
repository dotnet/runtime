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
    public sealed class OleDbException : System.Data.Common.DbException
    {
        private readonly OleDbErrorCollection oledbErrors;

        internal OleDbException(string? message, OleDbHResult errorCode, Exception? inner) : base(message, inner)
        {
            HResult = (int)errorCode;
            this.oledbErrors = new OleDbErrorCollection(null);
        }

        internal OleDbException(OleDbException previous, Exception? inner) : base(previous.Message, inner)
        {
            HResult = previous.ErrorCode;
            this.oledbErrors = previous.oledbErrors;
        }

        private OleDbException(string? message, Exception? inner, string? source, OleDbHResult errorCode, OleDbErrorCollection errors) : base(message, inner)
        {
            Debug.Assert(errors != null, "OleDbException without OleDbErrorCollection");
            Source = source;
            HResult = (int)errorCode;
            this.oledbErrors = errors;
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (si == null)
            {
                throw new ArgumentNullException(nameof(si));
            }
            si.AddValue("oledbErrors", oledbErrors, typeof(OleDbErrorCollection));
            base.GetObjectData(si, context);
        }

        [TypeConverter(typeof(ErrorCodeConverter))]
        public override int ErrorCode
        {
            get
            {
                return base.ErrorCode;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public OleDbErrorCollection Errors
        {
            get
            {
                OleDbErrorCollection errors = this.oledbErrors;
                return ((errors != null) ? errors : new OleDbErrorCollection(null));
            }
        }

        internal static OleDbException CreateException(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult errorCode, Exception? inner)
        {
            OleDbErrorCollection errors = new OleDbErrorCollection(errorInfo);
            string? message = null;
            string? source = null;
            OleDbHResult hr = 0;

            if (errorInfo != null)
            {
                hr = errorInfo.GetDescription(out message);

                hr = errorInfo.GetSource(out source);
            }

            int count = errors.Count;
            if (errors.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                if ((message != null) && (message != errors[0].Message))
                {
                    builder.Append(message.TrimEnd(ODB.ErrorTrimCharacters));
                    if (count > 1)
                    {
                        builder.Append(Environment.NewLine);
                    }
                }
                for (int i = 0; i < count; ++i)
                {
                    if (i > 0)
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

        internal static OleDbException CombineExceptions(List<OleDbException> exceptions)
        {
            Debug.Assert(exceptions.Count > 0, "missing exceptions");
            if (exceptions.Count > 1)
            {
                OleDbErrorCollection errors = new OleDbErrorCollection(null);
                StringBuilder builder = new StringBuilder();

                foreach (OleDbException exception in exceptions)
                {
                    errors.AddRange(exception.Errors);
                    builder.Append(exception.Message);
                    builder.Append(Environment.NewLine);
                }
                return new OleDbException(builder.ToString(), null, exceptions[0].Source, (OleDbHResult)exceptions[0].ErrorCode, errors);
            }
            else
            {
                return exceptions[0];
            }
        }

        internal sealed class ErrorCodeConverter : Int32Converter
        {
            // converter classes should have public ctor
            public ErrorCodeConverter()
            {
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == null)
                {
                    throw ADP.ArgumentNull("destinationType");
                }
                if ((destinationType == typeof(string)) && (value != null) && (value is int))
                {
                    return ODB.ELookup((OleDbHResult)value);
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
