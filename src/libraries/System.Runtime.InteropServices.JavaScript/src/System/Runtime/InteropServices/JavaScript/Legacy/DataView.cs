// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// The DataView view provides a low-level interface for reading and writing multiple number types in a
    /// binary ArrayBuffer, without having to care about the platform's endianness.
    /// </summary>
    [Obsolete]
    public class DataView : JSObject
    {
        /// <summary>
        /// Initializes a new instance of the DataView class.
        /// </summary>
        /// <param name="buffer">ArrayBuffer to use as the storage backing the new DataView object.</param>
        public DataView(ArrayBuffer buffer)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(DataView), new object[] { buffer }))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

        /// <summary>
        /// Initializes a new instance of the DataView class.
        /// </summary>
        /// <param name="buffer">ArrayBuffer to use as the storage backing the new DataView object.</param>
        /// <param name="byteOffset">The offset, in bytes, to the first byte in the above buffer for the new view to reference. If unspecified, the buffer view starts with the first byte.</param>
        public DataView(ArrayBuffer buffer, int byteOffset)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(DataView), new object[] { buffer, byteOffset }))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

        /// <summary>
        /// Initializes a new instance of the DataView class.
        /// </summary>
        /// <param name="buffer">ArrayBuffer to use as the storage backing the new DataView object.</param>
        /// <param name="byteOffset">The offset, in bytes, to the first byte in the above buffer for the new view to reference. If unspecified, the buffer view starts with the first byte.</param>
        /// <param name="byteLength">The number of elements in the byte array. If unspecified, the view's length will match the buffer's length.</param>
        public DataView(ArrayBuffer buffer, int byteOffset, int byteLength)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(DataView), new object[] { buffer, byteOffset, byteLength }))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

        /// <summary>
        /// Initializes a new instance of the DataView class.
        /// </summary>
        /// <param name="jsHandle">Js handle.</param>
        internal DataView(IntPtr jsHandle) : base(jsHandle)
        { }

        /// <summary>
        /// Gets the length (in bytes) of this view from the start of its ArrayBuffer. Fixed at construction time and thus read only.
        /// </summary>
        /// <value>The length (in bytes) of this view.</value>
        public int ByteLength => (int)this.GetObjectProperty("byteLength");

        /// <summary>
        /// Gets the offset (in bytes) of this view from the start of its ArrayBuffer. Fixed at construction time and thus read only.
        /// </summary>
        /// <value>The offset (in bytes) of this view.</value>
        public int ByteOffset => (int)this.GetObjectProperty("byteOffset");

        /// <summary>
        /// Gets the ArrayBuffer referenced by this view. Fixed at construction time and thus read only.
        /// </summary>
        /// <value>The ArrayBuffer.</value>
        public ArrayBuffer Buffer => (ArrayBuffer)this.GetObjectProperty("buffer");

        /// <summary>
        /// Gets the signed 32-bit float (float) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A signed 32-bit float number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public float GetFloat32(int byteOffset, bool littleEndian = false) => UnBoxValue<float>(this.Invoke("getFloat32", byteOffset, littleEndian));

        /// <summary>
        /// Gets the signed 64-bit double (double) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A signed 64-bit coulbe number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 64-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public double GetFloat64(int byteOffset, bool littleEndian = false) => UnBoxValue<double>(this.Invoke("getFloat64", byteOffset, littleEndian));

        /// <summary>
        /// Gets the signed 16-bit integer (short) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A signed 16-bit ineger (short) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 16-bit integer (short) is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public short GetInt16(int byteOffset, bool littleEndian = false) => UnBoxValue<short>(this.Invoke("getInt16", byteOffset, littleEndian));

        /// <summary>
        /// Gets the signed 32-bit integer (int) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A signed 32-bit ineger (int) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit integer (int) is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public int GetInt32(int byteOffset, bool littleEndian = false) => UnBoxValue<int>(this.Invoke("getInt32", byteOffset, littleEndian));

        /// <summary>
        /// Gets the signed 8-bit byte (sbyte) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A signed 8-bit byte (sbyte) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 8-bit byte is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public sbyte GetInt8(int byteOffset, bool littleEndian = false) => UnBoxValue<sbyte>(this.Invoke("getInt8", byteOffset, littleEndian));

        /// <summary>
        /// Gets the unsigned 16-bit integer (short) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A unsigned 16-bit integer (ushort) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the unsigned 16-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public ushort GetUint16(int byteOffset, bool littleEndian = false) => UnBoxValue<ushort>(this.Invoke("getUint16", byteOffset, littleEndian));

        /// <summary>
        /// Gets the usigned 32-bit integer (uint) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A usigned 32-bit ineger (uint) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public uint GetUint32(int byteOffset, bool littleEndian = false) => UnBoxValue<uint>(this.Invoke("getUint32", byteOffset, littleEndian));

        /// <summary>
        /// Gets the unsigned 8-bit byte (byte) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <returns>A unsigned 8-bit byte (byte) number.</returns>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public byte GetUint8(int byteOffset, bool littleEndian = false) => UnBoxValue<byte>(this.Invoke("getUint8", byteOffset, littleEndian));

        /// <summary>
        /// Sets the signed 32-bit float (float) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">float value.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public void SetFloat32(int byteOffset, float value, bool littleEndian = false) => this.Invoke("setFloat32", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the signed 64-bit double (double) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">double value.</param>
        /// <param name="littleEndian">Indicates whether the 64-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public void SetFloat64(int byteOffset, double value, bool littleEndian = false) => this.Invoke("setFloat64", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the signed 16-bit integer (short) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">short value.</param>
        /// <param name="littleEndian">Indicates whether the 16-bit integer (short) is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public void SetInt16(int byteOffset, short value, bool littleEndian = false) => this.Invoke("setInt16", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the signed 32-bit integer (int) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">int value.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit integer (int) is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public void SetInt32(int byteOffset, int value, bool littleEndian = false) => this.Invoke("setInt32", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the signed 8-bit byte (sbyte) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">sbyte value.</param>
        /// <param name="littleEndian">Indicates whether the 8-bit byte is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public void SetInt8(int byteOffset, sbyte value, bool littleEndian = false) => this.Invoke("setInt8", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the unsigned 16-bit integer (short) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">ushort value.</param>
        /// <param name="littleEndian">Indicates whether the unsigned 16-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public void SetUint16(int byteOffset, ushort value, bool littleEndian = false) => this.Invoke("setUint16", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the usigned 32-bit integer (uint) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">uint value.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        [CLSCompliant(false)]
        public void SetUint32(int byteOffset, uint value, bool littleEndian = false) => this.Invoke("setUint32", byteOffset, value, littleEndian);

        /// <summary>
        /// Sets the unsigned 8-bit byte (sbyte) at the specified byte offset from the start of the DataView.
        /// </summary>
        /// <param name="byteOffset">Byte offset.</param>
        /// <param name="value">byte value.</param>
        /// <param name="littleEndian">Indicates whether the 32-bit float is stored in little- or big-endian format. If <c>false</c>, a big-endian value is read.</param>
        public void SetUint8(int byteOffset, byte value, bool littleEndian = false) => this.Invoke("setUint8", byteOffset, value, littleEndian);

        private static U UnBoxValue<U>(object jsValue) where U : struct
        {
            if (jsValue == null)
            {
                throw new InvalidCastException(SR.Format(SR.UnableCastNullToType, typeof(U)));
            }

            var type = jsValue.GetType();
            if (type.IsPrimitive)
            {
                return (U)Convert.ChangeType(jsValue, typeof(U));
            }
            else
            {
                throw new InvalidCastException(SR.Format(SR.UnableCastObjectToType, type, typeof(U)));
            }
        }

    }
}
