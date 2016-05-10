// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Util {    
    using System.Text;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.IO;
    using System.Diagnostics.Contracts;

    [Serializable]
    internal class StringExpressionSet
    {
        // This field, as well as the expressions fields below are critical since they may contain
        // canonicalized full path data potentially built out of relative data passed as input to the
        // StringExpressionSet.  Full trust code using the string expression set needs to ensure that before
        // exposing this data out to partial trust, they protect against this.  Possibilities include:
        //
        //  1. Using the throwOnRelative flag
        //  2. Ensuring that the partial trust code has permission to see full path data
        //  3. Not using this set for paths (eg EnvironmentStringExpressionSet)
        //
        [SecurityCritical]
        protected ArrayList m_list;
        protected bool m_ignoreCase;
        [SecurityCritical]
        protected String m_expressions;
        [SecurityCritical]
        protected String[] m_expressionsArray;

        protected bool m_throwOnRelative;
        
        protected static readonly char[] m_separators = { ';' };
        protected static readonly char[] m_trimChars = { ' ' };
#if !PLATFORM_UNIX
        protected static readonly char m_directorySeparator = '\\';
        protected static readonly char m_alternateDirectorySeparator = '/';
#else
        protected static readonly char m_directorySeparator = '/';
        protected static readonly char m_alternateDirectorySeparator = '\\';
#endif // !PLATFORM_UNIX
        
        public StringExpressionSet()
            : this( true, null, false )
        {
        }
        
        public StringExpressionSet( String str )
            : this( true, str, false )
        {
        }
        
        public StringExpressionSet( bool ignoreCase, bool throwOnRelative )
            : this( ignoreCase, null, throwOnRelative )
        {
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringExpressionSet( bool ignoreCase, String str, bool throwOnRelative )
        {
            m_list = null;
            m_ignoreCase = ignoreCase;
            m_throwOnRelative = throwOnRelative;
            if (str == null)
                m_expressions = null;
            else
            AddExpressions( str );
        }

        protected virtual StringExpressionSet CreateNewEmpty()
        {
            return new StringExpressionSet();
        }
        
        [SecuritySafeCritical]
        public virtual StringExpressionSet Copy()
        {
            // SafeCritical: just copying this value around, not leaking it

            StringExpressionSet copy = CreateNewEmpty();
            if (this.m_list != null)
                copy.m_list = new ArrayList(this.m_list);

            copy.m_expressions = this.m_expressions;
            copy.m_ignoreCase = this.m_ignoreCase;
            copy.m_throwOnRelative = this.m_throwOnRelative;
            return copy;
        }
        
        public void SetThrowOnRelative( bool throwOnRelative )
        {
            this.m_throwOnRelative = throwOnRelative;
        }

        private static String StaticProcessWholeString( String str )
        {
            return str.Replace( m_alternateDirectorySeparator, m_directorySeparator );
        }

        private static String StaticProcessSingleString( String str )
        {
            return str.Trim( m_trimChars );
        }

        protected virtual String ProcessWholeString( String str )
        {
            return StaticProcessWholeString(str);
        }

        protected virtual String ProcessSingleString( String str )
        {
            return StaticProcessSingleString(str);
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        public void AddExpressions( String str )
        {
            if (str == null)
                throw new ArgumentNullException( "str" );
            Contract.EndContractBlock();
            if (str.Length == 0)
                return;

            str = ProcessWholeString( str );

            if (m_expressions == null)
                m_expressions = str;
            else
                m_expressions = m_expressions + m_separators[0] + str;

            m_expressionsArray = null;

            // We have to parse the string and compute the list here.
            // The logic in this class tries to delay this parsing but
            // since operations like IsSubsetOf are called during 
            // demand evaluation, it is not safe to delay this step
            // as that would cause concurring threads to update the object
            // at the same time. The CheckList operation should ideally be
            // removed from this class, but for the sake of keeping the 
            // changes to a minimum here, we simply make sure m_list 
            // cannot be null by parsing m_expressions eagerly.

            String[] arystr = Split( str );

            if (m_list == null)
                m_list = new ArrayList();

            for (int index = 0; index < arystr.Length; ++index)
            {
                if (arystr[index] != null && !arystr[index].Equals( "" ))
                {
                    String temp = ProcessSingleString( arystr[index] );
                    int indexOfNull = temp.IndexOf( '\0' );

                    if (indexOfNull != -1)
                        temp = temp.Substring( 0, indexOfNull );

                    if (temp != null && !temp.Equals( "" ))
                    {
                        if (m_throwOnRelative)
                        {
                            if (Path.IsRelative(temp))
                            {
                                throw new ArgumentException( Environment.GetResourceString( "Argument_AbsolutePathRequired" ) );
                            }

                            temp = CanonicalizePath( temp );
                        }

                        m_list.Add( temp );
                    }
                }
            }

            Reduce();
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void AddExpressions( String[] str, bool checkForDuplicates, bool needFullPath )
        {
            AddExpressions(CreateListFromExpressions(str, needFullPath), checkForDuplicates);
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void AddExpressions( ArrayList exprArrayList, bool checkForDuplicates)
        {
            Contract.Assert( m_throwOnRelative, "This should only be called when throw on relative is set" );

            m_expressionsArray = null;
            m_expressions = null;

            if (m_list != null)
                m_list.AddRange(exprArrayList);
            else
                m_list = new ArrayList(exprArrayList);

            if (checkForDuplicates)
                Reduce();
        }


        [System.Security.SecurityCritical]  // auto-generated
        internal static ArrayList CreateListFromExpressions(String[] str, bool needFullPath)
        {
            if (str == null)
            {
                throw new ArgumentNullException( "str" );
            }
            Contract.EndContractBlock();
            ArrayList retArrayList = new ArrayList();
            for (int index = 0; index < str.Length; ++index)
            {
                if (str[index] == null)
                    throw new ArgumentNullException( "str" );

                // Replace alternate directory separators
                String oneString = StaticProcessWholeString( str[index] );

                if (oneString != null && oneString.Length != 0)
                {
                    // Trim leading and trailing spaces
                    String temp = StaticProcessSingleString( oneString);

                    int indexOfNull = temp.IndexOf( '\0' );

                    if (indexOfNull != -1)
                        temp = temp.Substring( 0, indexOfNull );

                    if (temp != null && temp.Length != 0)
                    {
                        if (PathInternal.IsPartiallyQualified(temp))
                        {
                            throw new ArgumentException(Environment.GetResourceString( "Argument_AbsolutePathRequired" ) );
                        }

                        temp = CanonicalizePath( temp, needFullPath );

                        retArrayList.Add( temp );
                    }
                }
            }

            return retArrayList;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        protected void CheckList()
        {
            if (m_list == null && m_expressions != null)
            {
                CreateList();
            }
        }
        
        protected String[] Split( String expressions )
        {
            if (m_throwOnRelative)
            {
                List<String> tempList = new List<String>();

                String[] quoteSplit = expressions.Split( '\"' );

                for (int i = 0; i < quoteSplit.Length; ++i)
                {
                    if (i % 2 == 0)
                    {
                        String[] semiSplit = quoteSplit[i].Split( ';' );

                        for (int j = 0; j < semiSplit.Length; ++j)
                        {
                            if (semiSplit[j] != null && !semiSplit[j].Equals( "" ))
                                tempList.Add( semiSplit[j] );
                        }
                    }
                    else
                    {
                        tempList.Add( quoteSplit[i] );
                    }
                }

                String[] finalArray = new String[tempList.Count];

                IEnumerator enumerator = tempList.GetEnumerator();

                int index = 0;
                while (enumerator.MoveNext())
                {
                    finalArray[index++] = (String)enumerator.Current;
                }

                return finalArray;
            }
            else
            {
                return expressions.Split( m_separators );
            }
        }

        
        [System.Security.SecurityCritical]  // auto-generated
        protected void CreateList()
        {
            String[] expressionsArray = Split( m_expressions );

            m_list = new ArrayList();
            
            for (int index = 0; index < expressionsArray.Length; ++index)
            {
                if (expressionsArray[index] != null && !expressionsArray[index].Equals( "" ))
                {
                    String temp = ProcessSingleString( expressionsArray[index] );

                    int indexOfNull = temp.IndexOf( '\0' );

                    if (indexOfNull != -1)
                        temp = temp.Substring( 0, indexOfNull );

                    if (temp != null && !temp.Equals( "" ))
                    {
                        if (m_throwOnRelative)
                        {
                            if (Path.IsRelative(temp))
                            {
                                throw new ArgumentException( Environment.GetResourceString( "Argument_AbsolutePathRequired" ) );
                            }

                            temp = CanonicalizePath( temp );
                        }
                        
                        m_list.Add( temp );
                    }
                }
            }
        }
        
        [SecuritySafeCritical]
        public bool IsEmpty()
        {
            // SafeCritical: we're just showing that the expressions are empty, the sensitive portion is their
            // contents - not the existence of the contents
            if (m_list == null)
            {
                return m_expressions == null;
            }
            else
            {
                return m_list.Count == 0;
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        public bool IsSubsetOf( StringExpressionSet ses )
        {
            if (this.IsEmpty())
                return true;
            
            if (ses == null || ses.IsEmpty())
                return false;
            
            CheckList();
            ses.CheckList();
            
            for (int index = 0; index < this.m_list.Count; ++index)
            {
                if (!StringSubsetStringExpression( (String)this.m_list[index], ses, m_ignoreCase ))
                {
                    return false;
                }
            }
            return true;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        public bool IsSubsetOfPathDiscovery( StringExpressionSet ses )
        {
            if (this.IsEmpty())
                return true;
            
            if (ses == null || ses.IsEmpty())
                return false;
            
            CheckList();
            ses.CheckList();
            
            for (int index = 0; index < this.m_list.Count; ++index)
            {
                if (!StringSubsetStringExpressionPathDiscovery( (String)this.m_list[index], ses, m_ignoreCase ))
                {
                    return false;
                }
            }
            return true;
        }

        
        [System.Security.SecurityCritical]  // auto-generated
        public StringExpressionSet Union( StringExpressionSet ses )
        {
            // If either set is empty, the union represents a copy of the other.
            
            if (ses == null || ses.IsEmpty())
                return this.Copy();
    
            if (this.IsEmpty())
                return ses.Copy();
            
            CheckList();
            ses.CheckList();
            
            // Perform the union
            // note: insert smaller set into bigger set to reduce needed comparisons
            
            StringExpressionSet bigger = ses.m_list.Count > this.m_list.Count ? ses : this;
            StringExpressionSet smaller = ses.m_list.Count <= this.m_list.Count ? ses : this;
    
            StringExpressionSet unionSet = bigger.Copy();
            
            unionSet.Reduce();
            
            for (int index = 0; index < smaller.m_list.Count; ++index)
            {
                unionSet.AddSingleExpressionNoDuplicates( (String)smaller.m_list[index] );
            }
            
            unionSet.GenerateString();
            
            return unionSet;
        }
            
        
        [System.Security.SecurityCritical]  // auto-generated
        public StringExpressionSet Intersect( StringExpressionSet ses )
        {
            // If either set is empty, the intersection is empty
            
            if (this.IsEmpty() || ses == null || ses.IsEmpty())
                return CreateNewEmpty();
            
            CheckList();
            ses.CheckList();
            
            // Do the intersection for real
            
            StringExpressionSet intersectSet = CreateNewEmpty();
            
            for (int this_index = 0; this_index < this.m_list.Count; ++this_index)
            {
                for (int ses_index = 0; ses_index < ses.m_list.Count; ++ses_index)
                {
                    if (StringSubsetString( (String)this.m_list[this_index], (String)ses.m_list[ses_index], m_ignoreCase ))
                    {
                        if (intersectSet.m_list == null)
                        {
                            intersectSet.m_list = new ArrayList();
                        }
                        intersectSet.AddSingleExpressionNoDuplicates( (String)this.m_list[this_index] );
                    }
                    else if (StringSubsetString( (String)ses.m_list[ses_index], (String)this.m_list[this_index], m_ignoreCase ))
                    {
                        if (intersectSet.m_list == null)
                        {
                            intersectSet.m_list = new ArrayList();
                        }
                        intersectSet.AddSingleExpressionNoDuplicates( (String)ses.m_list[ses_index] );
                    }
                }
            }
            
            intersectSet.GenerateString();
            
            return intersectSet;
        }
        
        [SecuritySafeCritical]
        protected void GenerateString()
        {
            // SafeCritical - moves critical data around, but doesn't expose it out
            if (m_list != null)
            {
                StringBuilder sb = new StringBuilder();
            
                IEnumerator enumerator = this.m_list.GetEnumerator();
                bool first = true;
            
                while (enumerator.MoveNext())
                {
                    if (!first)
                        sb.Append( m_separators[0] );
                    else
                        first = false;
                            
                    String currentString = (String)enumerator.Current;
                    if (currentString != null)
                    {
                        int indexOfSeparator = currentString.IndexOf( m_separators[0] );

                        if (indexOfSeparator != -1)
                            sb.Append( '\"' );

                        sb.Append( currentString );

                        if (indexOfSeparator != -1)
                            sb.Append( '\"' );
                    }
                }
            
                m_expressions = sb.ToString();
            }
            else
            {
                m_expressions = null;
            }
        }            
        
        // We don't override ToString since that API must be either transparent or safe citical.  If the
        // expressions contain paths that were canonicalized and expanded from the input that would cause
        // information disclosure, so we instead only expose this out to trusted code that can ensure they
        // either don't leak the information or required full path information.
        [SecurityCritical]
        public string UnsafeToString()
        {
            CheckList();
        
            Reduce();
        
            GenerateString();
                            
            return m_expressions;
        }

        [SecurityCritical]
        public String[] UnsafeToStringArray()
        {
            if (m_expressionsArray == null && m_list != null)
            {
                m_expressionsArray = (String[])m_list.ToArray(typeof(String));
            }

            return m_expressionsArray;
        }
                
        
        //-------------------------------
        // protected static helper functions
        //-------------------------------
        
        [SecurityCritical]
        private bool StringSubsetStringExpression( String left, StringExpressionSet right, bool ignoreCase )
        {
            for (int index = 0; index < right.m_list.Count; ++index)
            {
                if (StringSubsetString( left, (String)right.m_list[index], ignoreCase ))
                {
                    return true;
                }
            }
            return false;
        }
        
        [SecurityCritical]
        private static bool StringSubsetStringExpressionPathDiscovery( String left, StringExpressionSet right, bool ignoreCase )
        {
            for (int index = 0; index < right.m_list.Count; ++index)
            {
                if (StringSubsetStringPathDiscovery( left, (String)right.m_list[index], ignoreCase ))
                {
                    return true;
                }
            }
            return false;
        }

        
        protected virtual bool StringSubsetString( String left, String right, bool ignoreCase )
        {
            StringComparison strComp = (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (right == null || left == null || right.Length == 0 || left.Length == 0 ||
                right.Length > left.Length)
            {
                return false;
            }
            else if (right.Length == left.Length)
            {
                // if they are equal in length, just do a normal compare
                return String.Compare( right, left, strComp) == 0;
            }
            else if (left.Length - right.Length == 1 && left[left.Length-1] == m_directorySeparator)
            {
                return String.Compare( left, 0, right, 0, right.Length, strComp) == 0;
            }
            else if (right[right.Length-1] == m_directorySeparator)
            {
                // right is definitely a directory, just do a substring compare
                return String.Compare( right, 0, left, 0, right.Length, strComp) == 0;
            }
            else if (left[right.Length] == m_directorySeparator)
            {
                // left is hinting at being a subdirectory on right, do substring compare to make find out
                return String.Compare( right, 0, left, 0, right.Length, strComp) == 0;
            }
            else
            {
                return false;
            }
        }

        protected static bool StringSubsetStringPathDiscovery( String left, String right, bool ignoreCase )
        {
            StringComparison strComp = (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (right == null || left == null || right.Length == 0 || left.Length == 0)
            {
                return false;
            }
            else if (right.Length == left.Length)
            {
                // if they are equal in length, just do a normal compare
                return String.Compare( right, left, strComp) == 0;
            }
            else
            {
                String shortString, longString;

                if (right.Length < left.Length)
                {
                    shortString = right;
                    longString = left;
                }
                else
                {
                    shortString = left;
                    longString = right;
                }

                if (String.Compare( shortString, 0, longString, 0, shortString.Length, strComp) != 0)
                {
                    return false;
                }

#if !PLATFORM_UNIX
                if (shortString.Length == 3 &&
                    shortString.EndsWith( ":\\", StringComparison.Ordinal ) &&
                    ((shortString[0] >= 'A' && shortString[0] <= 'Z') ||
                    (shortString[0] >= 'a' && shortString[0] <= 'z')))
#else
                if (shortString.Length == 1 && shortString[0]== m_directorySeparator)
#endif // !PLATFORM_UNIX
                     return true;

                return longString[shortString.Length] == m_directorySeparator;
            }
        }

        
        //-------------------------------
        // protected helper functions
        //-------------------------------
        
        [SecuritySafeCritical]
        protected void AddSingleExpressionNoDuplicates( String expression )
        {
            // SafeCritical: We're not exposing out the string sets, just allowing modification of them
            int index = 0;
            
            m_expressionsArray = null;
            m_expressions = null;

            if (this.m_list == null)
                this.m_list = new ArrayList();

            while (index < this.m_list.Count)
            {
                if (StringSubsetString( (String)this.m_list[index], expression, m_ignoreCase ))
                {
                    this.m_list.RemoveAt( index );
                }
                else if (StringSubsetString( expression, (String)this.m_list[index], m_ignoreCase ))
                {
                    return;
                }
                else
                {
                    index++;
                }
            }
            this.m_list.Add( expression );
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        protected void Reduce()
        {
            CheckList();
            
            if (this.m_list == null)
                return;
            
            int j;

            for (int i = 0; i < this.m_list.Count - 1; i++)
            {
                j = i + 1;
                
                while (j < this.m_list.Count)
                {
                    if (StringSubsetString( (String)this.m_list[j], (String)this.m_list[i], m_ignoreCase ))
                    {
                        this.m_list.RemoveAt( j );
                    }
                    else if (StringSubsetString( (String)this.m_list[i], (String)this.m_list[j], m_ignoreCase ))
                    {
                        // write the value at j into position i, delete the value at position j and keep going.
                        this.m_list[i] = this.m_list[j];
                        this.m_list.RemoveAt( j );
                        j = i + 1;
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void GetLongPathName( String path, StringHandleOnStack retLongPath );

        [System.Security.SecurityCritical]  // auto-generated
        internal static String CanonicalizePath( String path )
        {
            return CanonicalizePath( path, true );
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static string CanonicalizePath(string path, bool needFullPath)
        {
            if (needFullPath)
            {
                string newPath = Path.GetFullPathInternal(path);
                if (path.EndsWith(m_directorySeparator + ".", StringComparison.Ordinal))
                {
                    if (newPath.EndsWith(m_directorySeparator))
                    {
                        newPath += ".";
                    }
                    else
                    {
                        newPath += m_directorySeparator + ".";
                    }
                }
                path = newPath;
            }
#if !PLATFORM_UNIX
            else if (path.IndexOf('~') != -1)
            {
                // GetFullPathInternal() will expand 8.3 file names
                string longPath = null;
                GetLongPathName(path, JitHelpers.GetStringHandleOnStack(ref longPath));
                path = (longPath != null) ? longPath : path;
            }

            // This blocks usage of alternate data streams and some extended syntax paths (\\?\C:\). Checking after
            // normalization allows valid paths such as " C:\" to be considered ok (as it will become "C:\").
            if (path.IndexOf(':', 2) != -1)
                throw new NotSupportedException(Environment.GetResourceString("Argument_PathFormatNotSupported"));
#endif // !PLATFORM_UNIX

            return path;
        }
    }
}
