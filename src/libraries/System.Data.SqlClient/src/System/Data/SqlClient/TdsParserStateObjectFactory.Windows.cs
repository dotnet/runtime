// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlClient.SNI;

namespace System.Data.SqlClient
{
    internal sealed class TdsParserStateObjectFactory
    {

        private const string UseLegacyNetworkingOnWindows = "System.Data.SqlClient.UseLegacyNetworkingOnWindows";

        public static readonly TdsParserStateObjectFactory Singleton = new TdsParserStateObjectFactory();


        // Temporary disabling App Context switching for managed SNI.
        // If the appcontext switch is set then Use Managed SNI based on the value. Otherwise Managed SNI should always be used.
        //private static bool shouldUseLegacyNetorking;
        //public static bool UseManagedSNI { get; } = AppContext.TryGetSwitch(UseLegacyNetworkingOnWindows, out shouldUseLegacyNetorking) ? !shouldUseLegacyNetorking : true;

        public static bool UseManagedSNI { get; } = false;

        public EncryptionOptions EncryptionOptions
        {
            get
            {
                return UseManagedSNI ? SNI.SNILoadHandle.SingletonInstance.Options : SNILoadHandle.SingletonInstance.Options;
            }
        }

        public uint SNIStatus
        {
            get
            {
                return UseManagedSNI ? SNI.SNILoadHandle.SingletonInstance.Status : SNILoadHandle.SingletonInstance.Status;
            }
        }

        public TdsParserStateObject CreateTdsParserStateObject(TdsParser parser)
        {
            if (UseManagedSNI)
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
            if (TdsParserStateObjectFactory.UseManagedSNI)
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
