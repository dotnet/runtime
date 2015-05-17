// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ITargetBlock.cs
//
//
// The base interface for all target blocks.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Represents a dataflow block that is a target for data.</summary>
    /// <typeparam name="TInput">Specifies the type of data accepted by the <see cref="ITargetBlock{TInput}"/>.</typeparam>
    public interface ITargetBlock<in TInput> : IDataflowBlock
    {
        // IMPLEMENT EXPLICITLY

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, Boolean consumeToAccept);
    }
}
