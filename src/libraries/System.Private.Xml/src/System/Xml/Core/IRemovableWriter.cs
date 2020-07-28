// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml
{
    /// <summary>
    /// Event which is used when a raw writer in a processing pipeline wishes to remove itself from the pipeline and
    /// replace itself with another writer.
    /// </summary>
    internal delegate void OnRemoveWriter(XmlRawWriter writer);

    /// <summary>
    /// This interface is implemented by writers which wish to remove themselves from the processing pipeline once they
    /// have accomplished some work.  An example would be the auto-detect writer, which removes itself from the pipeline
    /// once it has determined whether to use the Xml or the Html output mode.
    /// </summary>
    internal interface IRemovableWriter
    {
        OnRemoveWriter? OnRemoveWriterEvent { get; set; }
    }
}
