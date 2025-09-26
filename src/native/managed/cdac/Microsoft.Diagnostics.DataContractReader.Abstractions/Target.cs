// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Representation of the target under inspection
/// </summary>
/// <remarks>
/// This class provides APIs used by contracts for reading from the target and getting type and globals
/// information. Like the contracts themselves in the cdac, these are throwing APIs. Any callers at the boundaries
/// (for example, unmanaged entry points, COM) should handle any exceptions.
/// </remarks>
public abstract class Target
{
    /// <summary>
    /// Pointer size of the target
    /// </summary>
    public abstract int PointerSize { get; }
    /// <summary>
    ///  Endianness of the target
    /// </summary>
    public abstract bool IsLittleEndian { get; }

    /// <summary>
    /// Fills a buffer with the context of the given thread
    /// </summary>
    /// <param name="threadId">The identifier of the thread whose context is to be retrieved. The identifier is defined by the operating system.</param>
    /// <param name="contextFlags">A bitwise combination of platform-dependent flags that indicate which portions of the context should be read.</param>
    /// <param name="buffer">Buffer to be filled with thread context.</param>
    /// <returns>true if successful, false otherwise</returns>
    public abstract bool TryGetThreadContext(ulong threadId, uint contextFlags, Span<byte> buffer);

    /// <summary>
    /// Reads a well-known global pointer value from the target process
    /// </summary>
    /// <param name="global">The name of the global</param>
    /// <returns>The value of the global</returns>
    public abstract TargetPointer ReadGlobalPointer(string global);

    /// <summary>
    /// Reads a well-known global pointer value from the target process
    /// </summary>
    /// <param name="global">The name of the global</param>
    /// <param name="value">The value of the global, if found.</param>
    /// <returns>True if the global is found, false otherwise.</returns>
    public abstract bool TryReadGlobalPointer(string name, [NotNullWhen(true)] out TargetPointer? value);

    /// <summary>
    /// Read a pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Pointer read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract TargetPointer ReadPointer(ulong address);

    /// <summary>
    /// Read a pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <param name="value">Pointer read from the target</param>
    /// <returns>True if read succeeds, false otherwise.</returns>
    public abstract bool TryReadPointer(ulong address, out TargetPointer value);

    /// <summary>
    /// Read a code pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Pointer read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract TargetCodePointer ReadCodePointer(ulong address);

    /// <summary>
    /// Read a code pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <param name="value">Pointer read from the target</param>
    /// <returns>True if read succeeds, false otherwise.</returns>
    public abstract bool TryReadCodePointer(ulong address, out TargetCodePointer value);

    /// <summary>
    /// Read some bytes from the target
    /// </summary>
    /// <param name="address">The address where to start reading</param>
    /// <param name="buffer">Destination to copy the bytes, the number of bytes to read is the span length</param>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract void ReadBuffer(ulong address, Span<byte> buffer);

    /// <summary>
    /// Write some bytes to the target
    /// </summary>
    /// <param name="address">The address where to start writing</param>
    /// <param name="buffer">Source of the bytes to write, the number of bytes to write is the span length</param>
    public abstract void WriteBuffer(ulong address, Span<byte> buffer);

    /// <summary>
    /// Read a null-terminated UTF-8 string from the target
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract string ReadUtf8String(ulong address);

    /// <summary>
    /// Read a null-terminated UTF-16 string from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract string ReadUtf16String(ulong address);

    /// <summary>
    /// Read a native unsigned integer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract TargetNUInt ReadNUInt(ulong address);

    /// <summary>
    /// Read a well known global from the target process as a string
    /// </summary>
    /// <param name="name">The name of the global</param>
    /// <param name="value">The value of the global if found</param>
    /// <returns>True if a global is found, false otherwise</returns>
    public abstract bool TryReadGlobalString(string name, [NotNullWhen(true)] out string? value);

    /// <summary>
    /// Read a well known global from the target process as a string
    /// </summary>
    /// <param name="name">The name of the global</param>
    /// <returns>A string value</returns>
    public abstract string ReadGlobalString(string name);

    /// <summary>
    /// Read a well known global from the target process as a number in the target endianness
    /// </summary>
    /// <typeparam name="T">The numeric type to be read</typeparam>
    /// <param name="name">The name of the global</param>
    /// <returns>A numeric value</returns>
    public abstract T ReadGlobal<T>(string name) where T : struct, INumber<T>;

    /// <summary>
    /// Read a well known global from the target process as a number in the target endianness
    /// </summary>
    /// <typeparam name="T">The numeric type to be read</typeparam>
    /// <param name="name">The name of the global</param>
    /// <param name="value">The numeric value read.</param>
    /// <returns>True if a global is found, false otherwise.</returns>
    public abstract bool TryReadGlobal<T>(string name, [NotNullWhen(true)] out T? value) where T : struct, INumber<T>;

    /// <summary>
    /// Read a value from the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public abstract T Read<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

    /// <summary>
    /// Read a value from the target in little endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    public abstract T ReadLittleEndian<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

    /// <summary>
    /// Read a value from the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>True if read succeeds, false otherwise.</returns>
    public abstract bool TryRead<T>(ulong address, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

    /// <summary>
    /// Write a value to the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to write</typeparam>
    /// <param name="address">Address to start writing to</param>
    /// <param name="value">Value to write</param>
    public abstract void Write<T>(ulong address, T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

    /// <summary>
    /// Read a target pointer from a span of bytes
    /// </summary>
    /// <param name="bytes">The span of bytes to read from</param>
    /// <returns>The target pointer read from the span</returns>
    public abstract TargetPointer ReadPointerFromSpan(ReadOnlySpan<byte> bytes);


    /// <summary>
    /// Returns true if the given pointer is aligned to the pointer size of the target
    /// </summary>
    /// <param name="pointer">A target pointer value</param>
    /// <returns></returns>
    public abstract bool IsAlignedToPointerSize(TargetPointer pointer);

    /// <summary>
    /// Returns the information about the given well-known data type in the target process
    /// </summary>
    /// <param name="type">The name of the well known type</param>
    /// <returns>The information about the given type in the target process</returns>
    public abstract TypeInfo GetTypeInfo(DataType type);

    /// <summary>
    /// Get the data cache for the target
    /// </summary>
    public abstract IDataCache ProcessedData { get; }

    /// <summary>
    /// Holds a snapshot of the target's structured data
    /// </summary>
    public interface IDataCache
    {
        /// <summary>
        /// Read a value from the target and cache it, or return the currently cached value
        /// </summary>
        /// <typeparam name="T">The type  of data to be read</typeparam>
        /// <param name="address">The address in the target where the data resides</param>
        /// <returns>The value that has been read from the target</returns>
        T GetOrAdd<T>(TargetPointer address) where T : Data.IData<T>;
        /// <summary>
        /// Return the cached value for the given address, or null if no value is cached
        /// </summary>
        /// <typeparam name="T">The type of the data to be read</typeparam>
        /// <param name="address">The address in the target where the data resides</param>
        /// <param name="data">On return, set to the cached data value, or null if the data hasn't been cached yet.</param>
        /// <returns>True if a copy of the data is cached, or false otherwise</returns>
        bool TryGet<T>(ulong address, [NotNullWhen(true)] out T? data);
        /// <summary>
        /// Clear all cached data
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Information about a well-known data type in the target process
    /// </summary>
    public readonly record struct TypeInfo
    {
        /// <summary>
        /// The stride of the type in the target process.
        /// This is a value that can be used to calculate the byte offset of an element in an array of this type.
        /// </summary>
        public uint? Size { get; init; }
        /// <summary>
        /// Information about the well-known fields of the type in the target process
        /// </summary>
        public readonly IReadOnlyDictionary<string, FieldInfo> Fields
        {
            get;
            init;
        }

        public TypeInfo()
        {
            Fields = new Dictionary<string, FieldInfo>();
        }
    }

    /// <summary>
    /// Information about a well-known field of a well-known data type in the target process
    /// </summary>
    public readonly record struct FieldInfo
    {
        /// <summary>
        /// The byte offset of the field in an instance of the type in the target process
        /// </summary>
        public int Offset { get; init; }
        /// <summary>
        /// The well known data type of the field in the target process
        /// </summary>
        public readonly DataType Type { get; init; }
        /// <summary>
        /// The name of the well known data type of the field in the target process, or null
        /// if the target data descriptor did not record a name
        /// </summary>
        public readonly string? TypeName { get; init; }
    }

    /// <summary>
    /// A cache of the contracts for the target process
    /// </summary>
    public abstract ContractRegistry Contracts { get; }
}
