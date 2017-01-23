// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    using System.Runtime.InteropServices;

    /// <summary>
    ///     The Evidence class keeps track of information that can be used to make security decisions about
    ///     an assembly or an AppDomain.  There are two types of evidence, one is supplied by the CLR or a
    ///     host, the other supplied by the assembly itself.
    ///     
    ///     We keep a dictionary that maps each type of possbile evidence to an EvidenceTypeDescriptor which
    ///     contains the evidence objects themselves if they exist as well as some extra metadata about that
    ///     type of evidence.  This dictionary is fully populated with keys for host evidence at all times and
    ///     for assembly evidence the first time the application evidence is touched.  This means that if a
    ///     Type key does not exist in the dictionary, then that particular type of evidence will never be
    ///     given to the assembly or AppDomain in question as host evidence.  The only exception is if the
    ///     user later manually adds host evidence via the AddHostEvidence API.
    ///     
    ///     Assembly supplied evidence is created up front, however host supplied evidence may be lazily
    ///     created.  In the lazy creation case, the Type will map to either an EvidenceTypeDescriptor that does
    ///     not contain any evidence data or null.  As requests come in for that evidence, we'll populate the
    ///     EvidenceTypeDescriptor appropriately.
    /// </summary>
    [ComVisible(true)]
    public sealed class Evidence
    {
    }
}
