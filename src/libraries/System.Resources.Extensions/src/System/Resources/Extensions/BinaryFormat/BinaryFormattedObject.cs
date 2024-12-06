// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.BinaryFormat;

/// <summary>
///  Object model for the binary format put out by BinaryFormatter. It parses and creates a model but does not
///  instantiate any reference types outside of string.
/// </summary>
/// <remarks>
///  <para>
///   This is useful for explicitly controlling the rehydration of binary formatted data.
///  </para>
/// </remarks>
internal sealed partial class BinaryFormattedObject
{
#pragma warning disable SYSLIB0050 // Type or member is obsolete
    internal static FormatterConverter DefaultConverter { get; } = new();
#pragma warning restore SYSLIB0050

    private static readonly Options s_defaultOptions = new();
    private static readonly PayloadOptions s_payloadOptions = new()
    {
        UndoTruncatedTypeNames = true // Required for backward compat
    };
    private readonly Options _options;

    private ITypeResolver? _typeResolver;
    private ITypeResolver TypeResolver => _typeResolver ??= new DefaultTypeResolver(_options);

    /// <summary>
    ///  Creates <see cref="BinaryFormattedObject"/> by parsing <paramref name="stream"/>.
    /// </summary>
    public BinaryFormattedObject(Stream stream, Options? options = null)
    {
        _options = options ?? s_defaultOptions;

        try
        {
            RootRecord = NrbfDecoder.Decode(stream, out var readonlyRecordMap, options: s_payloadOptions, leaveOpen: true);
            RecordMap = readonlyRecordMap;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidCastException or ArithmeticException or IOException)
        {
            // Make the exception easier to catch, but retain the original stack trace.
            throw ex.ConvertToSerializationException();
        }
        catch (TargetInvocationException ex)
        {
            throw ExceptionDispatchInfo.Capture(ex.InnerException!).SourceException.ConvertToSerializationException();
        }
    }

    /// <summary>
    ///  Deserializes the <see cref="BinaryFormattedObject"/> back to an object.
    /// </summary>
    [RequiresUnreferencedCode("Ultimately calls Assembly.GetType for type names in the data.")]
    public object Deserialize()
    {
        try
        {
            return Deserializer.Deserializer.Deserialize(RootRecord.Id, RecordMap, TypeResolver, _options);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidCastException or ArithmeticException or IOException)
        {
            // Make the exception easier to catch, but retain the original stack trace.
            throw ex.ConvertToSerializationException();
        }
        catch (TargetInvocationException ex)
        {
            throw ExceptionDispatchInfo.Capture(ex.InnerException!).SourceException.ConvertToSerializationException();
        }
    }

    /// <summary>
    ///  The Id of the root record of the object graph.
    /// </summary>
    public SerializationRecord RootRecord { get; }

    /// <summary>
    ///  Gets a record by it's identifier. Not all records have identifiers, only ones that
    ///  can be referenced by other records.
    /// </summary>
    public SerializationRecord this[SerializationRecordId id] => RecordMap[id];

    public IReadOnlyDictionary<SerializationRecordId, SerializationRecord> RecordMap { get; }
}
