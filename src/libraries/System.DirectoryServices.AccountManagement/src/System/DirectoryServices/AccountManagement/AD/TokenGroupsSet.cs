
using System;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class TokenGroupSet : ResultSet
    {

       internal TokenGroupSet(
                            string userDN,
                            ADStoreCtx storeCtx,
                            bool readDomainGroups )
        {

            this.principalDN = userDN;
            this.storeCtx = storeCtx;
            this.attributeToQuery = readDomainGroups ? "tokenGroups" : "tokenGroupsGlobalAndUniversal";
            
            GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                    "TokenGroupSet", 
                                    "TokenGroupSet: userDN={0}",
                                    userDN);

        }

        string  principalDN;
        ADStoreCtx storeCtx;

        bool atBeginning = true;
        DirectoryEntry current = null; // current member of the group (or current group of the user)

        IEnumerator tokenGroupsEnum;

        SecurityIdentifier currentSID;
        bool disposed = false;

        string attributeToQuery;
        
        
        // Return the principal we're positioned at as a Principal object.
    	// Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
    	override internal object CurrentAsPrincipal
    	{
    	    get
    	    {
                if (this.currentSID != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "CurrentAsPrincipal: using current");

                    StringBuilder SidBindingString = new StringBuilder();

                    SidBindingString.Append("<SID=");
                    SidBindingString.Append(Utils.SecurityIdentifierToLdapHexBindingString(this.currentSID));
                    SidBindingString.Append(">");

                    DirectoryEntry currentDE = SDSUtils.BuildDirectoryEntry(
                                                BuildPathFromDN(SidBindingString.ToString()),
                                                this.storeCtx.Credentials,
                                                this.storeCtx.AuthTypes);

                    this.storeCtx.InitializeNewDirectoryOptions( currentDE );
                    this.storeCtx.LoadDirectoryEntryAttributes( currentDE );
                                                            
                    return ADUtils.DirectoryEntryAsPrincipal(currentDE, this.storeCtx);
                }
                
                return null;
    	    }
	}

    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results (or ranges of attribute values) as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
       {   

            if ( atBeginning )
            {
                Debug.Assert(this.principalDN != null);
    
                this.current = SDSUtils.BuildDirectoryEntry(
                                            BuildPathFromDN(this.principalDN),
                                            this.storeCtx.Credentials,
                                            this.storeCtx.AuthTypes);

                this.current.RefreshCache(new string [] {this.attributeToQuery});
                
                this.tokenGroupsEnum = this.current.Properties[this.attributeToQuery].GetEnumerator();

                atBeginning = false;
             }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "MoveNextLocal: returning primary group {0}", this.current.Path);


            if (this.tokenGroupsEnum.MoveNext())
            {
                // Got a member from this group (or, got a group of which we're a member).
                // Create a DirectoryEntry for it.

                byte[] sid = (byte[]) this.tokenGroupsEnum.Current;                   
                currentSID = new SecurityIdentifier(sid, 0);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "MoveNextLocal: got a value from the enumerator: {0}", currentSID.ToString());
                                       
                return true;
            }
                
            return false;

       }

    	// Resets the enumerator to before the first result in the set.  This potentially can be an expensive
    	// operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
    	// As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
    	// a no-op.
    	override internal void Reset()
       {   
            if ( atBeginning )
                return;

           this.tokenGroupsEnum.Reset();            
       }

    	// IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if ( !disposed )
                {
                    if ( current != null )
                        current.Dispose();

                    disposed = true;
                }
            }
            finally
            {            
                 base.Dispose();
            }             
        
        }

        string BuildPathFromDN(string dn)
        {
            string userSuppliedServername = this.storeCtx.UserSuppliedServerName;

            UnsafeNativeMethods.Pathname pathCracker = new UnsafeNativeMethods.Pathname();
            UnsafeNativeMethods.IADsPathname pathName = (UnsafeNativeMethods.IADsPathname) pathCracker;
            pathName.EscapedMode = 2 /* ADS_ESCAPEDMODE_ON */;
            pathName.Set(dn, 4 /* ADS_SETTYPE_DN */);
            string escapedDn = pathName.Retrieve(7 /* ADS_FORMAT_X500_DN */);
            
            if (userSuppliedServername.Length > 0)            
                return "LDAP://" + this.storeCtx.UserSuppliedServerName + "/" + escapedDn;
            else
                return "LDAP://" + escapedDn;
        }        
       

    }
}
