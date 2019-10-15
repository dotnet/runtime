// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IReceivableSourceBlock.cs
//
//
// The base interface for all source blocks.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Represents a dataflow block that supports receiving of messages without linking.</summary>
    /// <typeparam name="TOutput">Specifies the type of data supplied by the <see cref="IReceivableSourceBlock{TOutput}"/>.</typeparam>
    public interface IReceivableSourceBlock<TOutput> : ISourceBlock<TOutput>
    {
        // IMPLEMENT IMPLICITLY

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        bool TryReceive(Predicate<TOutput> filter, out TOutput item);

        // IMPLEMENT IMPLICITLY IF BLOCK SUPPORTS RECEIVING MORE THAN ONE ITEM, OTHERWISE EXPLICITLY

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        bool TryReceiveAll(out IList<TOutput> items);
    }
}
