// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 169
#pragma warning disable 282 // There is no defined ordering between fields in multiple declarations of partial class or struct
#pragma warning disable CA1066 // IEquatable<T> implementations aren't used

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat
{
    // This Enum matches CorMethodSemanticsAttr defined in CorHdr.h
    [Flags]
    public enum MethodSemanticsAttributes
    {
        Setter = 0x0001,
        Getter = 0x0002,
        Other = 0x0004,
        AddOn = 0x0008,
        RemoveOn = 0x0010,
        Fire = 0x0020,
    }

    // This Enum matches CorPInvokeMap defined in CorHdr.h
    [Flags]
    public enum PInvokeAttributes
    {
        NoMangle = 0x0001,

        CharSetMask = 0x0006,
        CharSetNotSpec = 0x0000,
        CharSetAnsi = 0x0002,
        CharSetUnicode = 0x0004,
        CharSetAuto = 0x0006,

        BestFitUseAssem = 0x0000,
        BestFitEnabled = 0x0010,
        BestFitDisabled = 0x0020,
        BestFitMask = 0x0030,

        ThrowOnUnmappableCharUseAssem = 0x0000,
        ThrowOnUnmappableCharEnabled = 0x1000,
        ThrowOnUnmappableCharDisabled = 0x2000,
        ThrowOnUnmappableCharMask = 0x3000,

        SupportsLastError = 0x0040,

        CallConvMask = 0x0700,
        CallConvWinapi = 0x0100,
        CallConvCdecl = 0x0200,
        CallConvStdcall = 0x0300,
        CallConvThiscall = 0x0400,
        CallConvFastcall = 0x0500,

        MaxValue = 0xFFFF,
    }

    public partial struct Handle
    {
        public override bool Equals(object obj)
        {
            if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        }

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        }

        public override int GetHashCode()
        {
            return (int)_value;
        }

        internal Handle(int value)
        {
            _value = value;
        }

        internal void Validate(params HandleType[] permittedTypes)
        {
            var myHandleType = (HandleType)(_value >> 24);
            foreach (var hType in permittedTypes)
            {
                if (myHandleType == hType)
                {
                    return;
                }
            }
            if (myHandleType == HandleType.Null)
            {
                return;
            }
            throw new ArgumentException("Invalid handle type");
        }

        public Handle(HandleType type, int offset)
        {
            _value = (int)type << 24 | (int)offset;
        }

        public HandleType HandleType
        {
            get
            {
                return (HandleType)(_value >> 24);
            }
        }

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        }

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        }

        public int ToIntToken()
        {
            return _value;
        }

        public static Handle FromIntToken(int value)
        {
            return new Handle(value);
        }

        internal int _value;

#if DEBUG
        public override string ToString()
        {
            return string.Format("{1} : {0,8:X8}", _value, Enum.GetName(typeof(HandleType), this.HandleType));
        }
#endif
    }

    public static class NativeFormatReaderExtensions
    {
        public static string GetString(this MetadataReader reader, ConstantStringValueHandle handle)
        {
            return reader.GetConstantStringValue(handle).Value;
        }
    }

    /// <summary>
    /// ConstantReferenceValue can only be used to encapsulate null reference values,
    /// and therefore does not actually store the value.
    /// </summary>
    public partial struct ConstantReferenceValue
    {
        /// Always returns null value.
        public object Value
        { get { return null; } }
    } // ConstantReferenceValue

    public partial struct ConstantStringValueHandle
    {
        public bool StringEquals(string value, MetadataReader reader)
        {
            return reader.StringEquals(this, value);
        }
    }

    public sealed partial class MetadataReader
    {
        private MetadataHeader _header;

        internal NativeReader _streamReader;

        // Creates a metadata reader on a memory-mapped file block
        public unsafe MetadataReader(IntPtr pBuffer, int cbBuffer)
        {
            _streamReader = new NativeReader((byte*)pBuffer, (uint)cbBuffer);

            _header = new MetadataHeader();
            _header.Decode(_streamReader);
        }

        /// <summary>
        /// Used as the root entrypoint for metadata, this is where all top-down
        /// structural walks of metadata must start.
        /// </summary>
        public ScopeDefinitionHandleCollection ScopeDefinitions
        {
            get
            {
                return _header.ScopeDefinitions;
            }
        }

        /// <summary>
        /// Returns a Handle value representing the null value. Can be used
        /// to test handle values of all types for null.
        /// </summary>
        public Handle NullHandle
        {
            get
            {
                return new Handle() { _value = ((int)HandleType.Null) << 24 };
            }
        }

        /// <summary>
        /// Returns true if handle is null.
        /// </summary>
        public bool IsNull(Handle handle)
        {
            return handle._value == NullHandle._value;
        }

        /// <summary>
        /// Idempotent - simply returns the provided handle value. Exists for
        /// consistency so that generated code does not need to handle this
        /// as a special case.
        /// </summary>
        public Handle ToHandle(Handle handle)
        {
            return handle;
        }

        internal bool StringEquals(ConstantStringValueHandle handle, string value)
        {
            return _streamReader.StringEquals((uint)handle.Offset, value);
        }
    }

    internal partial class MetadataHeader
    {
        /// <todo>
        /// Signature should be updated every time the metadata schema changes.
        /// </todo>
        public const uint Signature = 0xDEADDFFD;

        /// <summary>
        /// The set of ScopeDefinitions contained within this metadata resource.
        /// </summary>
        public ScopeDefinitionHandleCollection ScopeDefinitions;

        public void Decode(NativeReader reader)
        {
            if (reader.ReadUInt32(0) != Signature)
                NativeReader.ThrowBadImageFormatException();
            reader.Read(4, out ScopeDefinitions);
        }
    }
}
