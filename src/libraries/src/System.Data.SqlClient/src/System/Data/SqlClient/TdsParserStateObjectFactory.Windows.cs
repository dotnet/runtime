// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlClient.SNI;

namespace System.Data.SqlClient
{
    internal sealed class TdsParserStateObjectFactory
    {
        public static bool useManagedSni = false;

        public static readonly TdsParserStateObjectFactory Singleton = new TdsParserStateObjectFactory();

        public EncryptionOptions EncryptionOptions
        {
            get
            {
                return useManagedSni ? SNI.SNILoadHandle.SingletonInstance.Options : SNILoadHandle.SingletonInstance.Options;
            }
        }

        public uint SNIStatus
        {
            get
            {
                return useManagedSni ? SNI.SNILoadHandle.SingletonInstance.Status : SNILoadHandle.SingletonInstance.Status;
            }
        }

        public TdsParserStateObject CreateTdsParserStateObject(TdsParser parser)
        {
            if (useManagedSni)
            {
                return new TdsParserStateObjectManaged(parser);
            }
            else
            {
                return new TdsParserStateObjectNative(parser);
            }
        }

        internal TdsParserStateObject CreateSessionObject(TdsParser tdsParser, TdsParserStateObject _pMarsPhysicalConObj, bool v)
        {
            if (TdsParserStateObjectFactory.useManagedSni)
            {
                return new TdsParserStateObjectManaged(tdsParser, _pMarsPhysicalConObj, true);
            }
            else
            {
                return new TdsParserStateObjectNative(tdsParser, _pMarsPhysicalConObj, true);
            }
        }
    }
}
