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
    /// TDS FedAuth Info Option for STS URL
    /// </summary>
    public class TDSFedAuthInfoOptionSTSURL : TDSFedAuthInfoOption
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
        private byte[] _stsUrl;

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
        /// Return the STSURL as a unicode string.
        /// </summary>
        public string STSURL
        {
            get
            {
                if (_stsUrl != null)
                {
                    return Encoding.Unicode.GetString(_stsUrl);
                }

                return null;
            }
        }

        /// <summary>
        /// Default public constructor
        /// </summary>
        public TDSFedAuthInfoOptionSTSURL()
        {
            _fedAuthInfoId = TDSFedAuthInfoId.STSURL;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="infoDataLength">Info Data Length</param>
        public TDSFedAuthInfoOptionSTSURL(uint infoDataLength) : this()
        {
            _infoDataLength = infoDataLength;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stsurl">STSURL string</param>
        public TDSFedAuthInfoOptionSTSURL(string stsurl) : this()
        {
            _stsUrl = Encoding.Unicode.GetBytes(stsurl);
            _infoDataLength = (uint)_stsUrl.Length;
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
                _stsUrl = new byte[_infoDataLength];
                source.Read(_stsUrl, 0, _stsUrl.Length);
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
                source.Write(_stsUrl, 0, _stsUrl.Length);
            }
        }
    }
}
