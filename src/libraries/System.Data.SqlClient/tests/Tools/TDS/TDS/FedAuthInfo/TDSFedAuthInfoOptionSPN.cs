// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.SqlServer.TDS.Authentication
{
    /// <summary>
    /// TDS FedAuth Info Option for SPN
    /// </summary>
    public class TDSFedAuthInfoOptionSPN : TDSFedAuthInfoOption
    {
        /// <summary>
        /// FedAuth Information ID
        /// </summary>
        private TDSFedAuthInfoId _fedAuthInfoId;

        /// <summary>
        /// Information Data Length
        /// </summary>
        private uint _infoDataLength;

        /// <summary>
        /// STS URL
        /// </summary>
        private byte[] _spn;

        /// <summary>
        /// Return the SPN as a unicode string.
        /// </summary>
        public string SPN
        {
            get
            {
                if (_spn != null)
                {
                    return Encoding.Unicode.GetString(_spn);
                }

                return null;
            }
        }

        /// <summary>
        /// Return the FedAuthInfo Id.
        /// </summary>
        public override TDSFedAuthInfoId FedAuthInfoId
        {
            get
            {
                return _fedAuthInfoId;
            }
        }

        /// <summary>
        /// Default public constructor
        /// </summary>
        public TDSFedAuthInfoOptionSPN()
        {
            _fedAuthInfoId = TDSFedAuthInfoId.SPN;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="infoDataLength">Info Data Length</param>
        public TDSFedAuthInfoOptionSPN(uint infoDataLength)
            : this()
        {
            _infoDataLength = infoDataLength;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spn">SPN string</param>
        public TDSFedAuthInfoOptionSPN(string spn)
            : this()
        {
            _spn = Encoding.Unicode.GetBytes(spn);
            _infoDataLength = (uint)_spn.Length;
        }

        /// <summary>
        /// Inflate the data from the stream, when receiving this token.
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // Read the information data
            // 
            if (_infoDataLength > 0)
            {
                _spn = new byte[_infoDataLength];
                source.Read(_spn, 0, _spn.Length);
            }

            return true;
        }

        /// <summary>
        /// Deflate the data to the stream, when writing this token.
        /// </summary>
        /// <param name="source"></param>
        public override void Deflate(Stream source)
        {
            if (_infoDataLength > 0)
            {
                source.Write(_spn, 0, _spn.Length);
            }
        }
    }
}
