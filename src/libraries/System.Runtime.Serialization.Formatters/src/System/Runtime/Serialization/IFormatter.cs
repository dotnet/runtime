// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Runtime.Serialization
{
    public interface IFormatter
    {
        internal const string RequiresUnreferencedCodeMessage = "BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.";

        [Obsolete(Obsoletions.BinaryFormatterMessage, DiagnosticId = Obsoletions.BinaryFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        object Deserialize(Stream serializationStream);
        [Obsolete(Obsoletions.BinaryFormatterMessage, DiagnosticId = Obsoletions.BinaryFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        void Serialize(Stream serializationStream, object graph);
        ISurrogateSelector? SurrogateSelector { get; set; }
        SerializationBinder? Binder { get; set; }
        StreamingContext Context { get; set; }
    }
}
