// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

using Microsoft.SqlServer.TDS.FeatureExtAck;
using Microsoft.SqlServer.TDS.PreLogin;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Class that defines a feature option which is delivered in the login packet FeatureExt block
    /// </summary>
    public abstract class TDSLogin7FeatureOptionToken : IInflatable, IDeflatable
    {
        /// <summary>
        /// Size of the data read during inflation operation. It is needed to properly parse the option stream.
        /// </summary>
        internal uint InflationSize { get; set; }

        /// <summary>
        /// Feature type
        /// </summary>
        public virtual TDSFeatureID FeatureID { get; protected set; }

        /// <summary>
        /// Inflate the Feature option
        /// </summary>
        public abstract bool Inflate(Stream source);

        /// <summary>
        /// Deflate the token
        /// </summary>
        public abstract void Deflate(Stream destination);
    }
}
