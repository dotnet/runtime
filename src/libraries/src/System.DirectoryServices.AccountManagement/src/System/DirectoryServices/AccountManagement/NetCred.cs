/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    NetCred.cs

Abstract:

    Implements the NetCred class.
    This class is similar to NetworkCredential (i.e., it's a holder
    for username/domainname/password), but doesn't need the EnvironmentPermission.

History:

    24-Sept-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;

namespace System.DirectoryServices.AccountManagement
{
    class NetCred
    {
        public NetCred(string username, string password)
        {
            this.username = username;
            this.password = password;            
        }

        public string UserName
        {
            get { return this.username; }
        }

        public string Password
        {
            get { return this.password; }
        }

        public string ParsedUserName
        {
            // <SecurityKernel Critical="True" Ring="1">
            // <ReferencesCritical Name="Method: SplitUsername(String, String&, String&):Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get 
            { 
                if ( null == parsedUserName )
                {
                    SplitUsername( this.username, ref this.parsedUserName, ref this.domainname );
                }
                
                return this.parsedUserName; 
            }
        }
          
        public string Domain
        {
            // <SecurityKernel Critical="True" Ring="1">
            // <ReferencesCritical Name="Method: SplitUsername(String, String&, String&):Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get 
            { 
                if ( null == parsedUserName )
                {
                    SplitUsername( this.username, ref this.parsedUserName, ref this.domainname );
                }
                
                return this.domainname; 
            }
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="UnsafeNativeMethods.CredUIParseUserName(System.String,System.Text.StringBuilder,System.UInt32,System.Text.StringBuilder,System.UInt32):System.Int32" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        void SplitUsername( string username, ref string parsedUserName, ref string parsedDomainName )
        {

                // If the user has passed null creds then parsed components should also be null.
                if ( username == null )
                {
                    parsedDomainName = null;
                    parsedUserName = null;
                    return;                    
                }

                // Logon user expects the username in UPN or to have the username and domain split to the seperate parameters.
                // It does not work properly with NT4 style name formats.  This function will put the username in the proper format.
                StringBuilder splitUsername = new StringBuilder(UnsafeNativeMethods.CRED_MAX_USERNAME_LENGTH);
                StringBuilder splitDomain = new StringBuilder(UnsafeNativeMethods.CRED_MAX_DOMAIN_TARGET_LENGTH);
                
                int result = UnsafeNativeMethods.CredUIParseUserName(
                                                    username,
                                                     splitUsername,
                                                     (System.UInt32)splitUsername.Capacity,
                                                     splitDomain,
                                                     (System.UInt32)splitDomain.Capacity);

                // If CredUiParseUsername fails then username format must have been in a format it does not expect.
                // Just pass then entire username as the user passed it with a null domain string.
                if (result != 0)
                {
                    parsedDomainName = null;
                    parsedUserName = username;                
                }
                else
                {
                    parsedDomainName = splitDomain.ToString();
                    parsedUserName = splitUsername.ToString();
                }
    }
            

        //
        string username = null;
        string password = null;
        string domainname = null;
        string parsedUserName = null;
    }
}

