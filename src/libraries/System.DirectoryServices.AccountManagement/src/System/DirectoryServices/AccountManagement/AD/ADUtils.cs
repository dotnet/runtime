// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Text;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    internal static class ADUtils
    {
        // We use this, rather than simply testing DirectoryEntry.SchemaClassName, because we don't
        // want to miss objects that are of a derived type.
        // Note that, since computer is a derived class of user in AD, if you don't want to confuse
        // computers with users, you must test an object for computer status before testing it for
        // user status.
        internal static bool IsOfObjectClass(DirectoryEntry de, string classToCompare)
        {
            return de.Properties["objectClass"].Contains(classToCompare);
        }

        internal static bool IsOfObjectClass(SearchResult sr, string classToCompare)
        {
            return sr.Properties["objectClass"].Contains(classToCompare);
        }

        // Retrieves the name of the actual server that the DirectoryEntry is connected to
        internal static string GetServerName(DirectoryEntry de)
        {
            UnsafeNativeMethods.IAdsObjectOptions objOptions = (UnsafeNativeMethods.IAdsObjectOptions)de.NativeObject;
            return (string)objOptions.GetOption(0 /* == ADS_OPTION_SERVERNAME */);
        }

        // This routine escapes values used in DNs, per RFC 2253 and ADSI escaping rules.
        // It treats its input as a unescaped literal and produces a LDAP string that represents that literal
        // and that is escaped according to RFC 2253 and ADSI rules for DN components.
        internal static string EscapeDNComponent(string dnComponent)
        {
            //
            //   From RFC 2254:
            //
            //      If the UTF-8 string does not have any of the following characters
            //      which need escaping, then that string can be used as the string
            //      representation of the value.
            //
            //      o   a space or "#" character occurring at the beginning of the
            //          string
            //
            //      o   a space character occurring at the end of the string
            //
            //      o   one of the characters ",", "+", """, "\", "<", ">" or ";"
            //
            //      Implementations MAY escape other characters.
            //
            //      If a character to be escaped is one of the list shown above, then it
            //      is prefixed by a backslash ('\' ASCII 92).
            //
            //      Otherwise the character to be escaped is replaced by a backslash and
            //      two hex digits, which form a single byte in the code of the
            //      character.
            //
            //   ADSI imposes the additional requirement that occurrences of '=' be escaped.
            //   For ADsPaths, ADSI also requires the '/' (forward slash) to be escaped,
            //   but not for the individual DN components that we're building here
            //   (e.g., for IADsContainer::Create).

            StringBuilder sb = new StringBuilder(dnComponent.Length);

            // If it starts with ' ' or '#', escape the first character (clause one)
            int startingIndex = 0;
            if (dnComponent[0] == ' ' || dnComponent[0] == '#')
            {
                sb.Append('\\');
                sb.Append(dnComponent[0]);
                startingIndex++;
            }

            // Handle the escaping of the remaining characters (clause three)
            for (int i = startingIndex; i < dnComponent.Length; i++)
            {
                char c = dnComponent[i];

                switch (c)
                {
                    case ',':
                        sb.Append(@"\,");
                        break;

                    case '+':
                        sb.Append(@"\+");
                        break;

                    case '\"':
                        sb.Append("\\\"");  // that's the literal sequence "backslash followed by a quotation mark"
                        break;

                    case '\\':
                        sb.Append(@"\\");
                        break;

                    case '>':
                        sb.Append(@"\>");
                        break;

                    case '<':
                        sb.Append(@"\<");
                        break;

                    case ';':
                        sb.Append(@"\;");
                        break;

                    case '=':
                        sb.Append(@"\=");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            // If it ends in a space, escape that space (clause two)
            if (sb[sb.Length - 1] == ' ')
            {
                sb.Remove(sb.Length - 1, 1);
                sb.Append(@"\ ");
            }

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADUtils",
                            "EscapeDNComponent: mapped '{0}' to '{1}'",
                            dnComponent,
                            sb.ToString());

            return sb.ToString();
        }

        // This routine escapes values used in search filters, per RFC 2254 escaping rules.
        // It treats its input as a unescaped literal and produces a LDAP string that represents that literal
        // and that is escaped according to RFC 2254 rules.
        internal static string EscapeRFC2254SpecialChars(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);

            foreach (char c in s)
            {
                switch (c)
                {
                    case '(':
                        sb.Append(@"\28");
                        break;

                    case ')':
                        sb.Append(@"\29");
                        break;

                    case '*':
                        sb.Append(@"\2a");
                        break;

                    case '\\':
                        sb.Append(@"\5c");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADUtils",
                            "EscapeRFC2254SpecialChars: mapped '{0}' to '{1}'",
                            s,
                            sb.ToString());

            return sb.ToString();
        }

        // This routine escapes PAPI string values that may contain wilcards.
        // It treats its input string as a PAPI string filter (escaped according to
        // PAPI rules, and possibly containing wildcards), and produces a string
        // escaped to RFC 2254 rules and possibly containing wildcards.
        internal static string PAPIQueryToLdapQueryString(string papiString)
        {
            //
            // Wildcard
            //   *  --> *
            //
            // Escaped Literals
            //   \* --> \2a
            //   \\ --> \5c
            //
            // Other
            //   (  --> \28
            //   )  --> \29
            //   \( --> \28
            //   \) --> \29
            //   x  --> x       (where x is anything else)
            //   \x --> x       (where x is anything else)

            StringBuilder sb = new StringBuilder(papiString.Length);

            bool escapeMode = false;

            foreach (char c in papiString)
            {
                if (escapeMode == false)
                {
                    switch (c)
                    {
                        case '(':
                            sb.Append(@"\28");          //   (  --> \28
                            break;

                        case ')':
                            sb.Append(@"\29");          //   )  --> \29
                            break;

                        case '\\':
                            escapeMode = true;
                            break;

                        default:
                            // including the '*' wildcard
                            sb.Append(c);    //   *  --> *   and   x  --> x
                            break;
                    }
                }
                else
                {
                    escapeMode = false;

                    switch (c)
                    {
                        case '(':
                            sb.Append(@"\28");          //      \( --> \28
                            break;

                        case ')':
                            sb.Append(@"\29");          //      \) --> \29
                            break;

                        case '*':
                            sb.Append(@"\2a");          //      \* --> \2a
                            break;

                        case '\\':
                            sb.Append(@"\5c");          //      \\ --> \5c
                            break;

                        default:
                            sb.Append(c);    //      \x --> x
                            break;
                    }
                }
            }

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADUtils",
                            "PAPIQueryToLdapQueryString: mapped '{0}' to '{1}'",
                            papiString,
                            sb.ToString());

            return sb.ToString();
        }

        internal static string EscapeBinaryValue(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 3);

            foreach (byte b in bytes)
            {
                sb.Append('\\');
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        internal static string DateTimeToADString(DateTime dateTime)
        {
            // DateTime --> FILETIME --> stringized FILETIME

            long fileTime = dateTime.ToFileTimeUtc();

            return fileTime.ToString(CultureInfo.InvariantCulture);
        }

        internal static DateTime ADFileTimeToDateTime(long filetime)
        {
            // int64 FILETIME --> DateTime
            return DateTime.FromFileTimeUtc(filetime);
        }

        internal static long DateTimeToADFileTime(DateTime dt)
        {
            // DateTime --> int64 FILETIME
            return dt.ToFileTimeUtc();
        }

        internal static long LargeIntToInt64(UnsafeNativeMethods.IADsLargeInteger largeInt)
        {
            uint lowPart = (uint)largeInt.LowPart;
            uint highPart = (uint)largeInt.HighPart;
            long i = (long)(((ulong)lowPart) | (((ulong)highPart) << 32));

            return i;
        }

        // Transform from hex string ("1AFF") to LDAP hex string ("\1A\FF").
        // Returns null if input string is not a valid hex string.
        internal static string HexStringToLdapHexString(string s)
        {
            Debug.Assert(s != null);

            if (s.Length % 2 != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADUtils", "HexStringToLdapHexString: string has bad length " + s.Length);
                return null;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < (s.Length) / 2; i++)
            {
                char firstChar = s[i * 2];
                char secondChar = s[(i * 2) + 1];

                if (((firstChar >= '0' && firstChar <= '9') || (firstChar >= 'A' && firstChar <= 'F') || (firstChar >= 'a' && firstChar <= 'f')) &&
                     ((secondChar >= '0' && secondChar <= '9') || (secondChar >= 'A' && secondChar <= 'F') || (secondChar >= 'a' && secondChar <= 'f')))
                {
                    sb.Append('\\');
                    sb.Append(firstChar);
                    sb.Append(secondChar);
                }
                else
                {
                    // not a hex character
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADUtils", "HexStringToLdapHexString: invalid string " + s);
                    return null;
                }
            }

            return sb.ToString();
        }

        internal static bool ArePrincipalsInSameForest(Principal p1, Principal p2)
        {
            string p1DnsForestName = ((ADStoreCtx)p1.GetStoreCtxToUse()).DnsForestName;
            string p2DnsForestName = ((ADStoreCtx)p2.GetStoreCtxToUse()).DnsForestName;

            return (string.Equals(p1DnsForestName, p2DnsForestName, StringComparison.OrdinalIgnoreCase));
        }

        ///
        /// <summary>
        /// Returns true if the specified SIDs are from the same domain.
        /// Otherwise return false.
        /// </summary>
        /// <param name="sid1"></param>
        /// <param name="sid2"></param>
        /// <returns>Returns true if the specified SIDs are from the same domain.
        /// Otherwise return false
        /// </returns>
        ///
        internal static bool AreSidsInSameDomain(SecurityIdentifier sid1, SecurityIdentifier sid2)
        {
            if (sid1.IsAccountSid() && sid2.IsAccountSid())
            {
                return (sid1.AccountDomainSid.Equals(sid2.AccountDomainSid));
            }
            else
            {
                return false;
            }
        }

        internal static Principal DirectoryEntryAsPrincipal(DirectoryEntry de, ADStoreCtx storeCtx)
        {
            if (ADUtils.IsOfObjectClass(de, "computer") ||
               ADUtils.IsOfObjectClass(de, "user") ||
               ADUtils.IsOfObjectClass(de, "group"))
            {
                return storeCtx.GetAsPrincipal(de, null);
            }
            else if (ADUtils.IsOfObjectClass(de, "foreignSecurityPrincipal"))
            {
                return storeCtx.ResolveCrossStoreRefToPrincipal(de);
            }
            else
            {
                return storeCtx.GetAsPrincipal(de, null);
            }
        }

        internal static Principal SearchResultAsPrincipal(SearchResult sr, ADStoreCtx storeCtx, object discriminant)
        {
            if (ADUtils.IsOfObjectClass(sr, "computer") ||
               ADUtils.IsOfObjectClass(sr, "user") ||
               ADUtils.IsOfObjectClass(sr, "group"))
            {
                return storeCtx.GetAsPrincipal(sr, discriminant);
            }
            else if (ADUtils.IsOfObjectClass(sr, "foreignSecurityPrincipal"))
            {
                return storeCtx.ResolveCrossStoreRefToPrincipal(sr.GetDirectoryEntry());
            }
            else
            {
                return storeCtx.GetAsPrincipal(sr, discriminant);
            }
        }

        // This function is used to check if we will be able to lookup a SID from the
        // target domain by targeting the local computer.  This is done by checking for either
        // a outbound or bidirectional trust between the computers domain and the target
        // domain or the current forest and the target domain's forest.
        // target domain must be the full DNS domain name of the target domain to make the string
        // compare below work properly.
        internal static bool VerifyOutboundTrust(string targetDomain, string username, string password)
        {
            Domain currentDom = null;

            try
            {
                currentDom = Domain.GetComputerDomain();
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // The computer is not domain joined so there cannot be a trust...
                return false;
            }
            catch (System.Security.Authentication.AuthenticationException)
            {
                // The computer is domain joined but we are running with creds that can't access it.  We can't determine trust.
                return false;
            }

            // If this is the same domain then we have a trust.
            // Domain.Name always returns full dns name.
            // function is always supplied with a full DNS domain name.
            if (string.Equals(currentDom.Name, targetDomain, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                TrustRelationshipInformation TRI = currentDom.GetTrustRelationship(targetDomain);

                if (TrustDirection.Outbound == TRI.TrustDirection || TrustDirection.Bidirectional == TRI.TrustDirection)
                {
                    return true;
                }
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
            }

            // Since we were able to retrive the computer domain above we should be able to access the current forest here.
            Forest currentForest = Forest.GetCurrentForest();

            Domain targetdom = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, targetDomain, username, password));

            try
            {
                ForestTrustRelationshipInformation FTC = currentForest.GetTrustRelationship(targetdom.Forest.Name);

                if (TrustDirection.Outbound == FTC.TrustDirection || TrustDirection.Bidirectional == FTC.TrustDirection)
                {
                    return true;
                }
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
            }

            return false;
        }

        internal static string RetriveWkDn(DirectoryEntry deBase, string defaultNamingContext, string serverName, byte[] wellKnownContainerGuid)
        {
            /*
                            bool w2k3Supported  = false;
                            if ( w2k3Supported )
                            {
                                return @"LDAP://" + this.UserSuppliedServerName + @"/<WKGUID= " + Constants.GUID_FOREIGNSECURITYPRINCIPALS_CONTAINER_W + @"," + this.DefaultNamingContext + @">";
                            }
                */
            PropertyValueCollection wellKnownObjectValues = deBase.Properties["wellKnownObjects"];

            foreach (UnsafeNativeMethods.IADsDNWithBinary value in wellKnownObjectValues)
            {
                if (Utils.AreBytesEqual(wellKnownContainerGuid, (byte[])value.BinaryValue))
                {
                    return ("LDAP://" + serverName + @"/" + value.DNString);
                }
            }

            return null;
        }
    }
}
