/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    ADStoreKey.cs

Abstract:

    Implements the ADStoreKey class.

    This class generates a unique key for each stored object.
    This key can then be used to determine if two principals are equal.
    Algo for key building is:

    IF ( NOT Well Known Sid )
        Hash Code from Object GUID
    ELSE    
        DomainName.HashCode ^ Sid.HashCode

History:

    05-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    class ADStoreKey : StoreKey
    {
        // For regular ADStoreKeys
        System.Guid objectGuid;

        // For ADStoreKeys corresponding to well-known SIDs
        bool wellKnownSid;
        string domainName;
        byte[] sid;

        public ADStoreKey(Guid guid)
        {
            Debug.Assert(guid != Guid.Empty);
            
            this.objectGuid = guid;
            this.wellKnownSid = false;

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADStoreKey",
                            "creating GUID key for GUID={0}",
                            guid);
            
        }

        public ADStoreKey(string domainName, byte[] sid)
        {
            Debug.Assert(domainName != null && domainName.Length > 0);
            Debug.Assert(sid != null && sid.Length != 0);

            // Make a copy of the SID, since a byte[] is mutable
            this.sid = new byte[sid.Length];
            Array.Copy(sid, this.sid, sid.Length);

            this.domainName = domainName;            
            this.wellKnownSid = true;

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADStoreKey",
                            "creating SID key for domainName={0}, sid={1}",
                            domainName,
                            Utils.ByteArrayToString(sid));
                                    
        }

        override public bool Equals(object o)
        {
            if (!(o is ADStoreKey))
                return false;

            ADStoreKey that = (ADStoreKey) o;
    
            if (this.wellKnownSid != that.wellKnownSid)
                return false;           

            if (!this.wellKnownSid)
            {
                if (this.objectGuid == that.objectGuid)
                    return true;
            }
            else
            {
                if ( (String.Compare(this.domainName, that.domainName, StringComparison.OrdinalIgnoreCase) == 0) &&
                     (Utils.AreBytesEqual(this.sid, that.sid)) )
                    return true;
            }
            
            return false;
        }

        override public int GetHashCode()
        {
            return (this.wellKnownSid == false) ? 
                        this.objectGuid.GetHashCode() :
                        (this.domainName.GetHashCode() ^ this.sid.GetHashCode());
        }
    }
}
