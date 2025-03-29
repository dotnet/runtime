// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Nrbf;
using System.Runtime.Serialization;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

#pragma warning disable SYSLIB0050 // Type or member is obsolete

/// <summary>
///  Interface for deserialization used to define the coupling between the main <see cref="Deserializer"/>
///  and its <see cref="ObjectRecordDeserializer"/>s.
/// </summary>
internal interface IDeserializer
{
    /// <summary>
    ///  The current deserialization options.
    /// </summary>
    BinaryFormattedObject.Options Options { get; }

    /// <summary>
    ///  The set of object record ids that are not considered "complete" yet.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Objects are considered incomplete if they contain references to value or <see cref="IObjectReference"/> types
    ///   that need completed or if they have not yet finished evaluating all of their member data. They are also
    ///   considered incomplete if they implement <see cref="ISerializable"/> or have a surrogate and the
    ///   <see cref="SerializationInfo"/> has not yet been applied.
    ///  </para>
    /// </remarks>
    HashSet<SerializationRecordId> IncompleteObjects { get; }

    /// <summary>
    ///  The set of objects that have been deserialized, indexed by record id.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Objects may not be fully filled out. If they are not in <see cref="IncompleteObjects"/>, they are
    ///   guaranteed to have their reference type members created (this is not transitive- their members may not
    ///   be ready if there are cycles in the object graph).
    ///  </para>
    /// </remarks>
    IDictionary<SerializationRecordId, object> DeserializedObjects { get; }

    /// <summary>
    ///  Resolver for types.
    /// </summary>
    BinaryFormattedObject.ITypeResolver TypeResolver { get; }

    /// <summary>
    ///  Pend the given value updater to be run when it's value type dependency is complete.
    /// </summary>
    void PendValueUpdater(ValueUpdater updater);

    /// <summary>
    ///  Pend a <see cref="SerializationInfo"/> to be applied when the graph is fully parsed.
    /// </summary>
    void PendSerializationInfo(PendingSerializationInfo pending);

    /// <summary>
    ///  Mark the object id as complete. This will check dependencies and resolve relevant <see cref="ValueUpdater"/>s.
    /// </summary>
    void CompleteObject(SerializationRecordId id);

    /// <summary>
    ///  Check for a surrogate for the given type. If none exists, returns <see langword="null"/>.
    /// </summary>
    ISerializationSurrogate? GetSurrogate(Type type);
}

#pragma warning restore SYSLIB0050 // Type or member is obsolete
