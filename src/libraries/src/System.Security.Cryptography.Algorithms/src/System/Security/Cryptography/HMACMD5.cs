// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    //
    // If you change anything in this class, you must make the same change in the other HMAC* classes. This is a pain but given that the 
    // preexisting contract from the desktop locks all of these into deriving directly from HMAC, it can't be helped.
    //

    public class HMACMD5 : HMAC
    {
        public HMACMD5()
            : this(Helpers.GenerateRandom(BlockSize))
        {
        }

        public HMACMD5(byte[] key)
        {
            this.HashName = HashAlgorithmNames.MD5;
            _hMacCommon = new HMACCommon(HashAlgorithmNames.MD5, key, BlockSize);
            base.Key = _hMacCommon.ActualKey;
            // this not really needed as it'll initialize BlockSizeValue with same value it has which is 64.
            // we just want to be explicit in all HMAC extended classes   
            BlockSizeValue = BlockSize; 
        }

        public override int HashSize
        {
            get
            {
                return _hMacCommon.HashSizeInBits;
            }
        }

        public override byte[] Key
        {
            get
            {
                return base.Key;
            }
            set
            {
                _hMacCommon.ChangeKey(value);
                base.Key = _hMacCommon.ActualKey;
            }
        }

        protected override void HashCore(byte[] rgb, int ib, int cb)
        {
            _hMacCommon.AppendHashData(rgb, ib, cb);
        }

        protected override byte[] HashFinal()
        {
            return _hMacCommon.FinalizeHashAndReset();
        }

        public override void Initialize()
        {
            // Nothing to do here. We expect HashAlgorithm to invoke HashFinal() and Initialize() as a pair. This reflects the 
            // reality that our native crypto providers (e.g. CNG) expose hash finalization and object reinitialization as an atomic operation.
            return;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HMACCommon hMacCommon = _hMacCommon;
                _hMacCommon = null;
                if (hMacCommon != null)
                    hMacCommon.Dispose(disposing);
            }
            base.Dispose(disposing);
        }

        private HMACCommon _hMacCommon;
        private const int BlockSize = 64;
    }
}
