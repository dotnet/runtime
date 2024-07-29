// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;

namespace System.Resources.Extensions.BinaryFormat;

#pragma warning disable SYSLIB0050 // Type or member is obsolete

internal sealed partial class BinaryFormattedObject
{
    internal sealed class Options
    {
        /// <summary>
        ///  How exactly assembly names need to match for deserialization.
        /// </summary>
        public FormatterAssemblyStyle AssemblyMatching { get; set; } = FormatterAssemblyStyle.Simple;

        /// <summary>
        ///  Type name binder.
        /// </summary>
        public SerializationBinder? Binder { get; set; }

        /// <summary>
        ///  Optional type <see cref="ISerializationSurrogate"/> provider.
        /// </summary>
        public ISurrogateSelector? SurrogateSelector { get; set; }

        /// <summary>
        ///  Streaming context.
        /// </summary>
        public StreamingContext StreamingContext { get; set; } = new(StreamingContextStates.All);
    }
}

#pragma warning restore SYSLIB0050 // Type or member is obsolete
