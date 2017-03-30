// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.FeatureExtAck
{
    /// <summary>
    /// A single option of the feature extension acknowledgement block
    /// </summary>
    public abstract class TDSFeatureExtAckOption : IDeflatable, IInflatable
    {
        /// <summary>
        /// Feature identifier
        /// </summary>
        public virtual TDSFeatureID FeatureID { get; protected set; }

        /// <summary>
        /// Initialization Constructor.
        /// </summary>
        public TDSFeatureExtAckOption()
        {
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        public abstract bool Inflate(Stream source);

        /// <summary>
        /// Deflate the token.
        /// </summary>
        public abstract void Deflate(Stream destination);
    }
}
