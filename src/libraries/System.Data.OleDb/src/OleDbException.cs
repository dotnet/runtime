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
    public sealed partial class OleDbException : System.Data.Common.DbException
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
            Debug.Assert(null != errors, "OleDbException without OleDbErrorCollection");
            Source = source;
            HResult = (int)errorCode;
            this.oledbErrors = errors;
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (null == si)
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
                return ((null != errors) ? errors : new OleDbErrorCollection(null));
            }
        }

        internal static OleDbException CombineExceptions(List<OleDbException> exceptions)
        {
            Debug.Assert(0 < exceptions.Count, "missing exceptions");
            if (1 < exceptions.Count)
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
