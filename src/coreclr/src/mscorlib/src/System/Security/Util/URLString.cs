// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  URLString
//
//
//  Implementation of membership condition for zones
//

namespace System.Security.Util {
    
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Diagnostics.Contracts;

#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    internal sealed class URLString : SiteString
    {
        private String m_protocol;
        [OptionalField(VersionAdded = 2)]
        private String m_userpass;
        private SiteString m_siteString;
        private int m_port;
#if !PLATFORM_UNIX
        private LocalSiteString m_localSite;
#endif // !PLATFORM_UNIX
        private DirectoryString m_directory;
        
        private const String m_defaultProtocol = "file";

        [OptionalField(VersionAdded = 2)]
        private bool m_parseDeferred;
        [OptionalField(VersionAdded = 2)]
        private String m_urlOriginal;
        [OptionalField(VersionAdded = 2)]
        private bool m_parsedOriginal;

        [OptionalField(VersionAdded = 3)]
        private bool m_isUncShare;

        // legacy field from v1.x, not used in v2 and beyond. Retained purely for serialization compatibility.
        private String m_fullurl;


        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {

            if (m_urlOriginal == null)
            {
                // pre-v2 deserialization. Need to fix-up fields here
                m_parseDeferred = false;
                m_parsedOriginal = false; // Dont care what this value is - never used
                m_userpass = "";
                m_urlOriginal = m_fullurl;
                m_fullurl = null;
            }
        }
        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {

            if ((ctx.State & ~(StreamingContextStates.Clone|StreamingContextStates.CrossAppDomain)) != 0)
            {
                DoDeferredParse();
                m_fullurl = m_urlOriginal;
            }
        }   
        [OnSerialized]
        private void OnSerialized(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone|StreamingContextStates.CrossAppDomain)) != 0)
            {
                m_fullurl = null;
            }
        }
        
        public URLString()
        {
            m_protocol = "";
            m_userpass = "";
            m_siteString = new SiteString();
            m_port = -1;
#if !PLATFORM_UNIX
            m_localSite = null;
#endif // !PLATFORM_UNIX
            m_directory = new DirectoryString();
            m_parseDeferred = false;
        }

        private void DoDeferredParse()
        {
            if (m_parseDeferred)
            {
                ParseString(m_urlOriginal, m_parsedOriginal);
                m_parseDeferred = false;
            }
        }

        public URLString(string url) : this(url, false, false) {}
        public URLString(string url, bool parsed) : this(url, parsed, false) {}

        internal URLString(string url, bool parsed, bool doDeferredParsing)
        {
            m_port = -1;
            m_userpass = "";
            DoFastChecks(url);
            m_urlOriginal = url;
            m_parsedOriginal = parsed;
            m_parseDeferred = true;
            if (doDeferredParsing)
                DoDeferredParse();
        }

        // Converts %XX and %uYYYY to the actual characters (I.e. Unesacpes any escape characters present in the URL)
        private String UnescapeURL(String url)
        {
            StringBuilder intermediate = StringBuilderCache.Acquire(url.Length);
            int Rindex = 0; // index into temp that gives the rest of the string to be processed
            int index;
            int braIndex = -1;
            int ketIndex = -1;
            braIndex = url.IndexOf('[',Rindex);
            if (braIndex != -1)
                ketIndex = url.IndexOf(']', braIndex);
            
            do
                {
                    index = url.IndexOf( '%', Rindex);

                    if (index == -1)
                    {
                        intermediate = intermediate.Append(url, Rindex, (url.Length - Rindex));
                        break;
                    }
                    // if we hit a '%' in the middle of an IPv6 address, dont process that
                    if (index > braIndex && index < ketIndex)
                    {
                        intermediate = intermediate.Append(url, Rindex, (ketIndex - Rindex+1));
                        Rindex = ketIndex+1;
                        continue;
                    }

                    if (url.Length - index < 2) // Check that there is at least 1 char after the '%'
                        throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );

                    if (url[index+1] == 'u' || url[index+1] == 'U')
                    {
                        if (url.Length - index < 6) // example: "%u004d" is 6 chars long
                            throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );

                        // We have a unicode character specified in hex

                        try
                        {
                            char c = (char)(Hex.ConvertHexDigit( url[index+2] ) << 12 |
                                            Hex.ConvertHexDigit( url[index+3] ) << 8  |
                                            Hex.ConvertHexDigit( url[index+4] ) << 4  |
                                            Hex.ConvertHexDigit( url[index+5] ));
                            intermediate = intermediate.Append(url, Rindex, index - Rindex);
                            intermediate = intermediate.Append(c);
                        }
                        catch(ArgumentException) // Hex.ConvertHexDigit can throw an "out of range" ArgumentException
                        {
                            throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
                        }
                                        
                        Rindex = index + 6 ; //update the 'seen' length
                    }
                    else
                    {
                        // we have a hex character.
                                         
                        if (url.Length - index < 3) // example: "%4d" is 3 chars long
                             throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );

                        try 
                        {
                            char c = (char)(Hex.ConvertHexDigit( url[index+1] ) << 4 | Hex.ConvertHexDigit( url[index+2] ));

                            intermediate = intermediate.Append(url, Rindex, index - Rindex);
                            intermediate = intermediate.Append(c);
                        }
                        catch(ArgumentException) // Hex.ConvertHexDigit can throw an "out of range" ArgumentException
                        {
                            throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
                        }
                        
                        Rindex = index + 3; // update the 'seen' length
                    }  

                } 
            while (true);
            return StringBuilderCache.GetStringAndRelease(intermediate);
        }

        // Helper Function for ParseString: 
        // Search for the end of the protocol info and grab the actual protocol string
        // ex. http://www.microsoft.com/complus would have a protocol string of http
        private String ParseProtocol(String url)
        {
            String temp;
            int index = url.IndexOf( ':' );
                
            if (index == 0)
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
            }
            else if (index == -1)
            {
                m_protocol = m_defaultProtocol;
                temp = url;
            }
            else if (url.Length > index + 1)
            {
                if (index == m_defaultProtocol.Length && 
                    String.Compare(url, 0, m_defaultProtocol, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    m_protocol = m_defaultProtocol;
                    temp = url.Substring( index + 1 );

                    // Since an explicit file:// URL could be immediately followed by a host name, we will be
                    // conservative and assume that it is on a share rather than a potentally relative local
                    // URL.
                    m_isUncShare = true;
                }
                else if (url[index+1] != '\\')
                {
#if !PLATFORM_UNIX
                    if (url.Length > index + 2 &&
                        url[index+1] == '/' &&
                        url[index+2] == '/')
#else
                    if (url.Length > index + 1 &&
                        url[index+1] == '/' ) // UNIX style "file:/home/me" is allowed, so account for that
#endif  // !PLATFORM_UNIX
                    {
                        m_protocol = url.Substring( 0, index );

                        for (int i = 0; i < m_protocol.Length; ++i)
                        {
                            char c = m_protocol[i];

                            if ((c >= 'a' && c <= 'z') ||
                                (c >= 'A' && c <= 'Z') ||
                                (c >= '0' && c <= '9') ||
                                (c == '+') ||
                                (c == '.') ||
                                (c == '-'))
                            {
                                continue;
                            }
                            else
                            {
                                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
                            }
                        }
#if !PLATFORM_UNIX
                        temp = url.Substring( index + 3 );
#else
                        // In UNIX, we don't know how many characters we'll have to skip past.
                        // Skip past \, /, and :
                        //
                        for ( int j=index ; j<url.Length ; j++ )
                        {
                            if ( url[j] != '\\' && url[j] != '/' && url[j] != ':' )
                            {
                                index = j;
                                break;
                            }
                        }

                        temp = url.Substring( index );
#endif  // !PLATFORM_UNIX
                     }
                    else
                    {
                        throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
                    }
                }
                else
                {
                    m_protocol = m_defaultProtocol;
                    temp = url;
                }
            }
            else
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
            }

            return temp;
        }

        private String ParsePort(String url)
        {
            String temp = url;
            char[] separators = new char[] { ':', '/' };
            int Rindex = 0;
            int userpassIndex = temp.IndexOf('@');
            if (userpassIndex != -1) {
                if (temp.IndexOf('/',0,userpassIndex) == -1) {
                    // this is a user:pass type of string 
                    m_userpass = temp.Substring(0,userpassIndex);
                    Rindex = userpassIndex + 1;
                }
            }

            int braIndex = -1;
            int ketIndex = -1;
            int portIndex = -1;
            braIndex = url.IndexOf('[',Rindex);
            if (braIndex != -1)
                ketIndex = url.IndexOf(']', braIndex);
            if (ketIndex != -1)
            {
                // IPv6 address...ignore the IPv6 block when searching for the port
                portIndex = temp.IndexOfAny(separators,ketIndex);
            }
            else
            {
                portIndex = temp.IndexOfAny(separators,Rindex);
            }

            

            if (portIndex != -1 && temp[portIndex] == ':')
            {
                // make sure it really is a port, and has a number after the :
                if ( temp[portIndex+1] >= '0' && temp[portIndex+1] <= '9' )
                {
                    int tempIndex = temp.IndexOf( '/', Rindex);

                    if (tempIndex == -1)
                    {
                        m_port = Int32.Parse( temp.Substring(portIndex + 1), CultureInfo.InvariantCulture );

                        if (m_port < 0)
                            throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );

                        temp = temp.Substring( Rindex, portIndex - Rindex );
                    }
                    else if (tempIndex > portIndex)
                    {
                        m_port = Int32.Parse( temp.Substring(portIndex + 1, tempIndex - portIndex - 1), CultureInfo.InvariantCulture );
                        temp = temp.Substring( Rindex, portIndex - Rindex ) + temp.Substring( tempIndex );
                    }
                    else 
                        throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
                }
                else
                    throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );
            }
            else {
                // Chop of the user/pass portion if any
                temp = temp.Substring(Rindex);
            }
                
            return temp;
        }

        // This does three things:
        // 1. It makes the following modifications to the start of the string:
        //      a. \\?\ and \\?/ => <empty>
        //      b. \\.\ and \\./ => <empty>
        // 2. If isFileUrl is true, converts all slashes to front slashes and strips leading
        //    front slashes. See comment by code.
        // 3. Throws a PathTooLongException if the length of the resulting URL is >= MAX_PATH.
        //    This is done to prevent security issues due to canonicalization truncations.
        // Remove this method when the Path class supports "\\?\"
        internal static string PreProcessForExtendedPathRemoval(string url, bool isFileUrl)
        {
            return PreProcessForExtendedPathRemoval(checkPathLength: true, url: url, isFileUrl: isFileUrl);
        }

        internal static string PreProcessForExtendedPathRemoval(bool checkPathLength, string url, bool isFileUrl)
        {
            bool isUncShare = false;
            return PreProcessForExtendedPathRemoval(checkPathLength: checkPathLength, url: url, isFileUrl: isFileUrl, isUncShare: ref isUncShare);
        }

        // Keeping this signature to avoid reflection breaks
        private static string PreProcessForExtendedPathRemoval(string url, bool isFileUrl, ref bool isUncShare)
        {
            return PreProcessForExtendedPathRemoval(checkPathLength: true, url: url, isFileUrl: isFileUrl, isUncShare: ref isUncShare);
        }

        private static string PreProcessForExtendedPathRemoval(bool checkPathLength, string url, bool isFileUrl, ref bool isUncShare)
        {
            // This is the modified URL that we will return
            StringBuilder modifiedUrl = new StringBuilder(url);

            // ITEM 1 - remove extended path characters.
            {
                // Keep track of where we are in both the comparison and altered strings.
                int curCmpIdx = 0;
                int curModIdx = 0;

                // If all the '\' have already been converted to '/', just check for //?/ or //./
                if ((url.Length - curCmpIdx) >= 4 &&
                    (String.Compare(url, curCmpIdx, "//?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                     String.Compare(url, curCmpIdx, "//./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    modifiedUrl.Remove(curModIdx, 4);
                    curCmpIdx += 4;
                }
                else
                {
                    if (isFileUrl) {
                        // We need to handle an indefinite number of leading front slashes for file URLs since we could
                        // get something like:
                        //      file://\\?\
                        //      file:/\\?\
                        //      file:\\?\
                        //      etc...
                        while (url[curCmpIdx] == '/')
                        {
                            curCmpIdx++;
                            curModIdx++;
                        }
                    }

                    // Remove the extended path characters
                    if ((url.Length - curCmpIdx) >= 4 &&
                        (String.Compare(url, curCmpIdx, "\\\\?\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\.\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        modifiedUrl.Remove(curModIdx, 4);
                        curCmpIdx += 4;
                    }
                }
            }

            // ITEM 2 - convert all slashes to forward slashes, and strip leading slashes.
            if (isFileUrl)
            {
                int slashCount = 0;
                bool seenFirstBackslash = false;
                
                while (slashCount < modifiedUrl.Length && (modifiedUrl[slashCount] == '/' || modifiedUrl[slashCount] == '\\'))
                {
                    // Look for sets of consecutive backslashes. We can't just look for these at the start
                    // of the string, since file:// might come first.  Instead, once we see the first \, look
                    // for a second one following it.
                    if (!seenFirstBackslash && modifiedUrl[slashCount] == '\\')
                {
                        seenFirstBackslash = true;
                        if (slashCount + 1 < modifiedUrl.Length && modifiedUrl[slashCount + 1] == '\\')
                            isUncShare = true;
                    }

                    slashCount++;
                }

                modifiedUrl.Remove(0, slashCount);
                modifiedUrl.Replace('\\', '/');
            }

            // ITEM 3 - If the path is greater than or equal (due to terminating NULL in windows) MAX_PATH, we throw.
            if (checkPathLength)
            {
                // This needs to be a separate method to avoid hitting the static constructor on AppContextSwitches
                CheckPathTooLong(modifiedUrl);
            }

            // Create the result string from the StringBuilder
            return modifiedUrl.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckPathTooLong(StringBuilder path)
        {
            if (path.Length >= (
#if FEATURE_PATHCOMPAT
                AppContextSwitches.BlockLongPaths ? PathInternal.MaxShortPath :
#endif
                PathInternal.MaxLongPath))
            {
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            }
        }

        // Do any misc massaging of data in the URL
        private String PreProcessURL(String url, bool isFileURL)
        {

#if !PLATFORM_UNIX
            if (isFileURL) {
                // Remove when the Path class supports "\\?\"
                url = PreProcessForExtendedPathRemoval(url, true, ref m_isUncShare);
            }
            else {
                url = url.Replace('\\', '/');
            }
            return url;
#else
            // Remove superfluous '/'
            // For UNIX, the file path would look something like:
            //      file:///home/johndoe/here
            //      file:/home/johndoe/here
            //      file:../johndoe/here
            //      file:~/johndoe/here
            String temp = url;            
            int  nbSlashes = 0;
            while(nbSlashes<temp.Length && '/'==temp[nbSlashes])
                nbSlashes++;  
            
            // if we get a path like file:///directory/name we need to convert 
            // this to /directory/name.
            if(nbSlashes > 2)
               temp = temp.Substring(nbSlashes-1, temp.Length - (nbSlashes-1));
            else if (2 == nbSlashes) /* it's a relative path */
               temp = temp.Substring(nbSlashes, temp.Length - nbSlashes);
            return temp;
#endif // !PLATFORM_UNIX

        }

        private void ParseFileURL(String url)
        {

            String temp = url;
#if !PLATFORM_UNIX            
            int index = temp.IndexOf( '/');

            if (index != -1 &&
                ((index == 2 &&
                  temp[index-1] != ':' &&
                  temp[index-1] != '|') ||
                 index != 2) &&
                index != temp.Length - 1)
            {
                // Also, if it is a UNC share, we want m_localSite to
                // be of the form "computername/share", so if the first
                // fileEnd character found is a slash, do some more parsing
                // to find the proper end character.

                int tempIndex = temp.IndexOf( '/', index+1);

                if (tempIndex != -1)
                    index = tempIndex;
                else
                    index = -1;
            }

            String localSite;
            if (index == -1)
                localSite = temp;
            else
                localSite = temp.Substring(0,index);

            if (localSite.Length == 0)
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidUrl" ) );

            int i;
            bool spacesAllowed;

            if (localSite[0] == '\\' && localSite[1] == '\\')
            {
                spacesAllowed = true;
                i = 2;
            }
            else
            {
                i = 0;
                spacesAllowed = false;
            }

            bool useSmallCharToUpper = true;

            for (; i < localSite.Length; ++i)
            {
                char c = localSite[i];

                if ((c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    (c == '-') || (c == '/') ||
                    (c == ':') || (c == '|') ||
                    (c == '.') || (c == '*') ||
                    (c == '$') || (spacesAllowed && c == ' '))
                {
                    continue;
                }
                else
                {
                    useSmallCharToUpper = false;
                    break;
                }
            }

            if (useSmallCharToUpper)
                localSite = String.SmallCharToUpper( localSite );
            else
                localSite = localSite.ToUpper(CultureInfo.InvariantCulture);

            m_localSite = new LocalSiteString( localSite );

            if (index == -1)
            {
                if (localSite[localSite.Length-1] == '*')
                    m_directory = new DirectoryString( "*", false );
                else 
                    m_directory = new DirectoryString();
            }
            else
            {
                String directoryString = temp.Substring( index + 1 );
                if (directoryString.Length == 0)
                {
                    m_directory = new DirectoryString();
                }
                else
                {
                    m_directory = new DirectoryString( directoryString, true);
                }
            }
#else // !PLATFORM_UNIX
            m_directory = new DirectoryString( temp, true);
#endif // !PLATFORM_UNIX

            m_siteString = null;
            return;
        }

        private void ParseNonFileURL(String url)
        {
            String temp = url;
            int index = temp.IndexOf('/');

            if (index == -1)
            {
#if !PLATFORM_UNIX
                m_localSite = null;    // for drive letter
#endif // !PLATFORM_UNIX
                m_siteString = new SiteString( temp );
                m_directory = new DirectoryString();
            }
            else
            {
#if !PLATFORM_UNIX 
                String site = temp.Substring( 0, index );
                m_localSite = null;
                m_siteString = new SiteString( site );

                String directoryString = temp.Substring( index + 1 );

                if (directoryString.Length == 0)
                {
                    m_directory = new DirectoryString();
                }
                else
                {
                    m_directory = new DirectoryString( directoryString, false );
                }
#else
                String directoryString = temp.Substring( index + 1 );
                String site = temp.Substring( 0, index );
                m_directory = new DirectoryString( directoryString, false );
                m_siteString = new SiteString( site );
#endif //!PLATFORM_UNIX
            }
            return;
        }

        void DoFastChecks( String url )
        {
            if (url == null)
            {
                throw new ArgumentNullException( "url" );
            }
            Contract.EndContractBlock();
            
            if (url.Length == 0)
            {
                throw new FormatException(Environment.GetResourceString("Format_StringZeroLength"));
            }
        }

        // NOTE:
        // 1. We support URLs that follow the common Internet scheme syntax
        //     (<scheme>://user:pass@<host>:<port>/<url-path>) and all windows file URLs.
        // 2.  In the general case we parse of the site and create a SiteString out of it
        //      (which supports our wildcarding scheme).  In the case of files we don't support
        //      wildcarding and furthermore SiteString doesn't like ':' and '|' which can appear
        //      in file urls so we just keep that info in a separate string and set the
        //      SiteString to null.
        //
        // ex. http://www.microsoft.com/complus  -> m_siteString = "www.microsoft.com" m_localSite = null
        // ex. file:///c:/complus/mscorlib.dll  -> m_siteString = null m_localSite = "c:"
        // ex. file:///c|/complus/mscorlib.dll  -> m_siteString = null m_localSite = "c:"
        void ParseString( String url, bool parsed )
        {
            // If there are any escaped hex or unicode characters in the url, translate those
            // into the proper character.

            if (!parsed)
            {
                url = UnescapeURL(url);
            }

            // Identify the protocol and strip the protocol info from the string, if present.
            String temp = ParseProtocol(url); 

            bool fileProtocol = (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0);
            
            // handle any special  preocessing...removing extra characters, etc.
            temp = PreProcessURL(temp, fileProtocol);
            
            if (fileProtocol)
            {
                ParseFileURL(temp);
            }
            else 
            {
                // Check if there is a port number and parse that out.
                temp = ParsePort(temp);
                ParseNonFileURL(temp);
                // Note: that we allow DNS and Netbios names for non-file protocols (since sitestring will check
                // that the hostname satisfies these two protocols. DNS-only checking can theoretically be added
                // here but that would break all the programs that use '_' (which is fairly common, yet illegal).
                // If this needs to be done at any point, add a call to m_siteString.IsLegalDNSName().
            }


        }

        public String Scheme
        {
            get
            {
                DoDeferredParse();

                return m_protocol;
            }
        }

        public String Host
        {
            get
            {
                DoDeferredParse();

                if (m_siteString != null)
                {
                    return m_siteString.ToString();
                }
                else
                {
#if !PLATFORM_UNIX
                    return m_localSite.ToString();
#else
                    return "";
#endif // !PLATFORM_UNIX
                }
            }
        }

        public String Port 
        {
            get 
            {
                DoDeferredParse();

                if (m_port == -1)
                    return null;
                else
                    return m_port.ToString(CultureInfo.InvariantCulture);
            }
        }

        public String Directory
        {
            get
            {
                DoDeferredParse();

                return m_directory.ToString();
            }
        }

        /// <summary>
        ///     Make a best guess at determining if this is URL refers to a file with a relative path. Since
        ///     this is a guess to help out users of UrlMembershipCondition who may accidentally supply a
        ///     relative URL, we'd rather err on the side of absolute than relative. (We'd rather accept some
        ///     meaningless membership conditions rather than reject meaningful ones).
        /// 
        ///     In order to be a relative file URL, the URL needs to have a protocol of file, and not be on a
        ///     UNC share.
        /// 
        ///     If both of the above are true, then the heuristics we'll use to detect an absolute URL are:
        ///         1. A host name which is:
        ///              a. greater than one character and ends in a colon (representing the drive letter) OR
        ///              b. ends with a * (so we match any file with the given prefix if any)
        ///         2. Has a directory name (cannot be simply file://c:)
        /// </summary>
        public bool IsRelativeFileUrl
        {
            get
            {
                DoDeferredParse();

                if (String.Equals(m_protocol, "file", StringComparison.OrdinalIgnoreCase) && !m_isUncShare)
                {
#if !PLATFORM_UNIX
                    string host = m_localSite != null ? m_localSite.ToString() : null;
                    // If the host name ends with the * character, treat this as an absolute URL since the *
                    // could represent the rest of the full path.
                    if (host.EndsWith('*'))
                        return false;
#endif // !PLATFORM_UNIX
                    string directory = m_directory != null ? m_directory.ToString() : null;

#if !PLATFORM_UNIX
                    return host == null || host.Length < 2 || !host.EndsWith(':') ||
                           String.IsNullOrEmpty(directory);
#else
                    return String.IsNullOrEmpty(directory);
#endif // !PLATFORM_UNIX

                }

                // Since this is not a local URL, it cannot be relative
                return false;
            }
        }

        public String GetFileName()
        {
            DoDeferredParse();

#if !PLATFORM_UNIX
            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
                return null;
         
            String intermediateDirectory = this.Directory.Replace( '/', '\\' );

            String directory = this.Host.Replace( '/', '\\' );

            int directorySlashIndex = directory.IndexOf( '\\' );
            if (directorySlashIndex == -1)
            {
                if (directory.Length != 2 ||
                    !(directory[1] == ':' || directory[1] == '|'))
                {
                    directory = "\\\\" + directory;
                }
            }
            else if (directorySlashIndex != 2 ||
                     (directorySlashIndex == 2 && directory[1] != ':' && directory[1] != '|'))
            {
                directory = "\\\\" + directory;
            }

            directory += "\\" + intermediateDirectory;
            
            return directory;
#else
            // In Unix, directory contains the full pathname
            // (this is what we get in Win32)
            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase ) != 0)
                return null;

            return this.Directory;
#endif    // !PLATFORM_UNIX
    }


        public String GetDirectoryName()
        {
            DoDeferredParse();

#if !PLATFORM_UNIX
            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase ) != 0)
                return null;

            String intermediateDirectory = this.Directory.Replace( '/', '\\' );

            int slashIndex = 0;
            for (int i = intermediateDirectory.Length; i > 0; i--)
            {
               if (intermediateDirectory[i-1] == '\\')
               {
                   slashIndex = i;
                   break;
               }
            }

            String directory = this.Host.Replace( '/', '\\' );

            int directorySlashIndex = directory.IndexOf( '\\' );
            if (directorySlashIndex == -1)
            {
                if (directory.Length != 2 ||
                    !(directory[1] == ':' || directory[1] == '|'))
                {
                    directory = "\\\\" + directory;
                }
            }
            else if (directorySlashIndex > 2 ||
                    (directorySlashIndex == 2 && directory[1] != ':' && directory[1] != '|'))
            {
                directory = "\\\\" + directory;
            }

            directory += "\\";
            
            if (slashIndex > 0)
            {
                directory += intermediateDirectory.Substring( 0, slashIndex );
            }

            return directory;
#else
            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
                return null;
            
            String directory = this.Directory.ToString();
            int slashIndex = 0;
            for (int i = directory.Length; i > 0; i--)
            {
               if (directory[i-1] == '/')
               {
                   slashIndex = i;
                   break;
               }
            }
            
            if (slashIndex > 0)
            {
                directory = directory.Substring( 0, slashIndex );
            }

            return directory;
#endif // !PLATFORM_UNIX            
        }

        public override SiteString Copy()
        {
            return new URLString( m_urlOriginal, m_parsedOriginal );
        }            
        
        public override bool IsSubsetOf( SiteString site )
        {
            if (site == null)
            {
                return false;
            }
            
            URLString url = site as URLString;
            
            if (url == null)
            {
                return false;
            }

            DoDeferredParse();
            url.DoDeferredParse();

            URLString normalUrl1 = this.SpecialNormalizeUrl();
            URLString normalUrl2 = url.SpecialNormalizeUrl();
            
            if (String.Compare( normalUrl1.m_protocol, normalUrl2.m_protocol, StringComparison.OrdinalIgnoreCase) == 0 &&
                normalUrl1.m_directory.IsSubsetOf( normalUrl2.m_directory ))
            {
#if !PLATFORM_UNIX
                if (normalUrl1.m_localSite != null)
                {
                    // We do a little extra processing in here for local files since we allow
                    // both <drive_letter>: and <drive_letter>| forms of urls.
                    
                    return normalUrl1.m_localSite.IsSubsetOf( normalUrl2.m_localSite );
                }
                else
#endif // !PLATFORM_UNIX
                {
                    if (normalUrl1.m_port != normalUrl2.m_port)
                        return false;

                    return normalUrl2.m_siteString != null && normalUrl1.m_siteString.IsSubsetOf( normalUrl2.m_siteString );
                }
            }
            else
            {
                return false;
            }
        }
        
        public override String ToString()
        {
            return m_urlOriginal;
        }
        
        public override bool Equals(Object o)
        {
            DoDeferredParse();

            if (o == null || !(o is URLString))
                return false;
            else
                return this.Equals( (URLString)o );
        }

        public override int GetHashCode()
        {
            DoDeferredParse();

            TextInfo info = CultureInfo.InvariantCulture.TextInfo;
            int accumulator = 0;

            if (this.m_protocol != null)
                accumulator = info.GetCaseInsensitiveHashCode( this.m_protocol );

#if !PLATFORM_UNIX
            if (this.m_localSite != null)
            {
                accumulator = accumulator ^ this.m_localSite.GetHashCode();
            }
            else
            {
                accumulator = accumulator ^ this.m_siteString.GetHashCode();
            }
            accumulator = accumulator ^ this.m_directory.GetHashCode();
#else
            accumulator = accumulator ^ info.GetCaseInsensitiveHashCode(this.m_urlOriginal);
#endif // !PLATFORM_UNIX
            


            return accumulator;
        }    
        
        public bool Equals( URLString url )
        {
            return CompareUrls( this, url );
        }

        public static bool CompareUrls( URLString url1, URLString url2 )
        {
            if (url1 == null && url2 == null)
                return true;

            if (url1 == null || url2 == null)
                return false;

            url1.DoDeferredParse();
            url2.DoDeferredParse();

            URLString normalUrl1 = url1.SpecialNormalizeUrl();
            URLString normalUrl2 = url2.SpecialNormalizeUrl();

            // Compare protocol (case insensitive)

            if (String.Compare( normalUrl1.m_protocol, normalUrl2.m_protocol, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            // Do special processing for file urls

            if (String.Compare( normalUrl1.m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0)
            {
#if !PLATFORM_UNIX
                if (!normalUrl1.m_localSite.IsSubsetOf( normalUrl2.m_localSite ) ||
                    !normalUrl2.m_localSite.IsSubsetOf( normalUrl1.m_localSite ))
                     return false;
#else
                return url1.IsSubsetOf( url2 ) &&
                       url2.IsSubsetOf( url1 );
#endif // !PLATFORM_UNIX
            }
            else
            {
                if (String.Compare( normalUrl1.m_userpass, normalUrl2.m_userpass, StringComparison.Ordinal) != 0)
                    return false;
                
                if (!normalUrl1.m_siteString.IsSubsetOf( normalUrl2.m_siteString ) ||
                    !normalUrl2.m_siteString.IsSubsetOf( normalUrl1.m_siteString ))
                    return false;

                if (url1.m_port != url2.m_port)
                    return false;
            }

            if (!normalUrl1.m_directory.IsSubsetOf( normalUrl2.m_directory ) ||
                !normalUrl2.m_directory.IsSubsetOf( normalUrl1.m_directory ))
                return false;

            return true;
        }

        internal String NormalizeUrl()
        {
            DoDeferredParse();
            StringBuilder builtUrl = StringBuilderCache.Acquire();

            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0)
            {
#if !PLATFORM_UNIX
                builtUrl = builtUrl.AppendFormat("FILE:///{0}/{1}", m_localSite.ToString(), m_directory.ToString());
#else
                builtUrl = builtUrl.AppendFormat("FILE:///{0}", m_directory.ToString());
#endif // !PLATFORM_UNIX
            }
            else
            {
                builtUrl = builtUrl.AppendFormat("{0}://{1}{2}", m_protocol, m_userpass, m_siteString.ToString());

                if (m_port != -1)
                    builtUrl = builtUrl.AppendFormat("{0}",m_port);

                builtUrl = builtUrl.AppendFormat("/{0}", m_directory.ToString());
            }

            return StringBuilderCache.GetStringAndRelease(builtUrl).ToUpper(CultureInfo.InvariantCulture);
        }
        
#if !PLATFORM_UNIX
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal URLString SpecialNormalizeUrl()
        {
            // Under WinXP, file protocol urls can be mapped to
            // drives that aren't actually file protocol underneath
            // due to drive mounting.  This code attempts to figure
            // out what a drive is mounted to and create the
            // url is maps to.

            DoDeferredParse();
            if (String.Compare( m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return this;
            }
            else
            {
                String localSite = m_localSite.ToString();

                if (localSite.Length == 2 &&
                    (localSite[1] == '|' ||
                     localSite[1] == ':'))
                {
                    String deviceName = null;
                    GetDeviceName(localSite, JitHelpers.GetStringHandleOnStack(ref deviceName));

                    if (deviceName != null)
                    {
                        if (deviceName.IndexOf( "://", StringComparison.Ordinal ) != -1)
                        {
                            URLString u = new URLString( deviceName + "/" + this.m_directory.ToString() );
                            u.DoDeferredParse(); // Presumably the caller of SpecialNormalizeUrl wants a fully parsed URL
                            return u;
                        }
                        else
                        {
                            URLString u = new URLString( "file://" + deviceName + "/" + this.m_directory.ToString() );
                            u.DoDeferredParse();// Presumably the caller of SpecialNormalizeUrl wants a fully parsed URL
                            return u;
                        }
                    }
                    else
                        return this;
                }
                else
                {
                    return this;
                }
            }
        }
                
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetDeviceName( String driveLetter, StringHandleOnStack retDeviceName );

#else
        internal URLString SpecialNormalizeUrl()
        {
            return this;
        }
#endif // !PLATFORM_UNIX

    }

    
    [Serializable]
    internal class DirectoryString : SiteString
    {
        private bool m_checkForIllegalChars;

        private new static char[] m_separators = { '/' };

        // From KB #Q177506, file/folder illegal characters are \ / : * ? " < > | 
        protected static char[] m_illegalDirectoryCharacters = { '\\', ':', '*', '?', '"', '<', '>', '|' };
        
        public DirectoryString()
        {
            m_site = "";
            m_separatedSite = new ArrayList();
        }
        
        public DirectoryString( String directory, bool checkForIllegalChars )
        {
            m_site = directory;
            m_checkForIllegalChars = checkForIllegalChars;
            m_separatedSite = CreateSeparatedString(directory);
        }

        private ArrayList CreateSeparatedString(String directory)
        {
            if (directory == null || directory.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
            }
            Contract.EndContractBlock();

            ArrayList list = new ArrayList();
            String[] separatedArray = directory.Split(m_separators);
            
            for (int index = 0; index < separatedArray.Length; ++index)
            {
                if (separatedArray[index] == null || separatedArray[index].Equals( "" ))
                {
                    // this case is fine, we just ignore it the extra separators.
                }
                else if (separatedArray[index].Equals( "*" ))
                {
                    if (index != separatedArray.Length-1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }
                    list.Add( separatedArray[index] );
                }
                else if (m_checkForIllegalChars && separatedArray[index].IndexOfAny( m_illegalDirectoryCharacters ) != -1)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                }
                else
                {
                    list.Add( separatedArray[index] );
                }
            }
            
            return list;
        }
        
        public virtual bool IsSubsetOf( DirectoryString operand )
        {
            return this.IsSubsetOf( operand, true );
        }

        public virtual bool IsSubsetOf( DirectoryString operand, bool ignoreCase )
        {
            if (operand == null)
            {
                return false;
            }
            else if (operand.m_separatedSite.Count == 0)
            {
                return this.m_separatedSite.Count == 0 || this.m_separatedSite.Count > 0 && String.Compare((String)this.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else if (this.m_separatedSite.Count == 0)
            {
                return String.Compare((String)operand.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else
            {
                return base.IsSubsetOf( operand, ignoreCase );
            }
        }
    }

#if !PLATFORM_UNIX
    [Serializable]
    internal class LocalSiteString : SiteString
    {
        private new static char[] m_separators = { '/' };

        public LocalSiteString( String site )
        {
            m_site = site.Replace( '|', ':');

            if (m_site.Length > 2 && m_site.IndexOf( ':' ) != -1)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));

            m_separatedSite = CreateSeparatedString(m_site);
        }

        private ArrayList CreateSeparatedString(String directory)
        {
            if (directory == null || directory.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
            }
            Contract.EndContractBlock();

            ArrayList list = new ArrayList();
            String[] separatedArray = directory.Split(m_separators);
            
            for (int index = 0; index < separatedArray.Length; ++index)
            {
                if (separatedArray[index] == null || separatedArray[index].Equals( "" ))
                {
                    if (index < 2 &&
                        directory[index] == '/')
                    {
                        list.Add( "//" );
                    }
                    else if (index != separatedArray.Length-1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }
                }
                else if (separatedArray[index].Equals( "*" ))
                {
                    if (index != separatedArray.Length-1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }
                    list.Add( separatedArray[index] );
                }
                else
                {
                    list.Add( separatedArray[index] );
                }
            }
            
            return list;
        }
        
        public virtual bool IsSubsetOf( LocalSiteString operand )
        {
            return this.IsSubsetOf( operand, true );
        }

        public virtual bool IsSubsetOf( LocalSiteString operand, bool ignoreCase )
        {
            if (operand == null)
            {
                return false;
            }
            else if (operand.m_separatedSite.Count == 0)
            {
                return this.m_separatedSite.Count == 0 || this.m_separatedSite.Count > 0 && String.Compare((String)this.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else if (this.m_separatedSite.Count == 0)
            {
                return String.Compare((String)operand.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else
            {
                return base.IsSubsetOf( operand, ignoreCase );
            }
        }
    }
#endif // !PLATFORM_UNIX
}
