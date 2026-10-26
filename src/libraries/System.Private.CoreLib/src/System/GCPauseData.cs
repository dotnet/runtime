// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Describes one contribution to the time spent paused for garbage collection.</summary>
/// <remarks>
/// A background collection can have several pauses. Foreground collections during
/// a background collection have their own collection indices and are not concurrent.
/// </remarks>
public readonly struct GCPauseData
{
    internal GCPauseData(long sequenceNumber, long collectionIndex, TimeSpan duration, int generation, bool concurrent)
    {
        SequenceNumber = sequenceNumber;
        CollectionIndex = collectionIndex;
        Duration = duration;
        Generation = generation;
        Concurrent = concurrent;
    }

    /// <summary>Gets the sequence number of the captured pause.</summary>
    /// <remarks>
    /// Sequence numbers increase while pause capture is enabled and include records
    /// skipped when the capture buffer is full. They are not reset on registration.
    /// </remarks>
    public long SequenceNumber { get; }

    /// <summary>Gets the index of the garbage collection associated with the pause.</summary>
    public long CollectionIndex { get; }

    /// <summary>Gets the contribution to the total time paused for garbage collection.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the generation collected by the associated garbage collection.</summary>
    public int Generation { get; }

    /// <summary>Gets a value that indicates whether the pause belongs to a background collection.</summary>
    public bool Concurrent { get; }
}

/// <summary>Represents a method that receives individual garbage collection pauses.</summary>
/// <param name="pause">The pause details.</param>
/// <param name="missedRecords">The newly observed number of records skipped because the shared capture buffer was full.</param>
/// <remarks>
/// The runtime invokes this callback on the finalizer thread. The callback must
/// return promptly and handle its own exceptions. An uncaught exception terminates
/// the process. A slow callback delays other subscribers and ordinary finalizers.
/// Loss is sampled before delivering a pause and reported independently for each
/// subscription, relative to its previous callback or registration. Loss need not
/// immediately precede the supplied pause. There are no loss-only callbacks.
/// </remarks>
public delegate void GCPauseDataCallback(GCPauseData pause, long missedRecords);
