// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.SqlClient
{
	// this structure is used for transporting packet handle references between the TdsParserStateObject
	//  base class and Managed or Native implementations. 
	// It carries type information so that assertions about the type of handle can be made in the 
	//  implemented abstract methods 
	// it is a ref struct so that it can only be used to transport the handles and not store them

	// N.B. If you change this type you must also change the version for the other platform

	internal readonly ref struct SessionHandle
    {
        public const int NativeHandleType = 1;
        public const int ManagedHandleType = 2;

        public readonly SNI.SNIHandle ManagedHandle;
        public readonly int Type;

        public SessionHandle(SNI.SNIHandle managedHandle, int type)
        {
            Type = type;
            ManagedHandle = managedHandle;
        }

        public bool IsNull => ManagedHandle is null;

		public static SessionHandle FromManagedSession(SNI.SNIHandle managedSessionHandle) => new SessionHandle(managedSessionHandle, ManagedHandleType);
	}
}
