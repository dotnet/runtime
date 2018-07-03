// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ISourceBlock.cs
//
//
// The base interface for all source blocks.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Represents a dataflow block that is a source of data.</summary>
    /// <typeparam name="TOutput">Specifies the type of data supplied by the <see cref="ISourceBlock{TOutput}"/>.</typeparam>
    public interface ISourceBlock<out TOutput> : IDataflowBlock
    {
        // IMPLEMENT IMPLICITLY

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions);

        // IMPLEMENT EXPLICITLY

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        TOutput ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed);

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
        bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target);

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
        void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target);
    }
}
