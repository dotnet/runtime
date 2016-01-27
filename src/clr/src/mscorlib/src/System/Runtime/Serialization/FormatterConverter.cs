// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: A base implementation of the IFormatterConverter
**          interface that uses the Convert class and the 
**          IConvertible interface.
**
**
============================================================*/
namespace System.Runtime.Serialization {
    using System;
    using System.Globalization;
    using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
    public class FormatterConverter : IFormatterConverter {

        public FormatterConverter() {
        }

        public Object Convert(Object value, Type type) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        public Object Convert(Object value, TypeCode typeCode) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
        }

        public bool ToBoolean(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        public char   ToChar(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToChar(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public sbyte  ToSByte(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToSByte(value, CultureInfo.InvariantCulture);
        }

        public byte   ToByte(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        public short  ToInt16(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToInt16(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ushort ToUInt16(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }

        public int    ToInt32(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public uint   ToUInt32(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        public long   ToInt64(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ulong  ToUInt64(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        } 

        public float  ToSingle(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        public double ToDouble(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public Decimal ToDecimal(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public DateTime ToDateTime(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        public String   ToString(Object value) {
            if (value==null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return System.Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
        
