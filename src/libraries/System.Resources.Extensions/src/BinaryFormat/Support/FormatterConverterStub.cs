// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Windows.Forms.BinaryFormat;

/// <summary>
///  <see cref="IFormatterConverter"/> that only returns default values.
/// </summary>
/// <remarks>
///  <para>
///   Allows creating a <see cref="SerializationInfo"/> when a <see cref="IFormatterConverter"/>
///   isn't necessary.
///  </para>
/// </remarks>
#pragma warning disable SYSLIB0050 // Type or member is obsolete (IFormatterConverter)
internal sealed class FormatterConverterStub : IFormatterConverter
{
    private FormatterConverterStub() { }

    public static IFormatterConverter Instance { get; } = new FormatterConverterStub();
#pragma warning restore SYSLIB0050 // Type or member is obsolete

    public object Convert(object value, Type type) => default!;
    public object Convert(object value, TypeCode typeCode) => default!;
    public bool ToBoolean(object value) => default;
    public byte ToByte(object value) => default;
    public char ToChar(object value) => default;
    public DateTime ToDateTime(object value) => default;
    public decimal ToDecimal(object value) => default;
    public double ToDouble(object value) => default;
    public short ToInt16(object value) => default;
    public int ToInt32(object value) => default;
    public long ToInt64(object value) => default;
    public sbyte ToSByte(object value) => default;
    public float ToSingle(object value) => default;
    public string? ToString(object value) => default;
    public ushort ToUInt16(object value) => default;
    public uint ToUInt32(object value) => default;
    public ulong ToUInt64(object value) => default;
}
