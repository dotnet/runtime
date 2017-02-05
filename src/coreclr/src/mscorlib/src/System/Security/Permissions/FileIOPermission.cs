// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Util;
    using System.IO;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    internal enum FileIOPermissionAccess
    {
        NoAccess = 0x00,
        Read = 0x01,
        Write = 0x02,
        Append = 0x04,
        PathDiscovery = 0x08,
        AllAccess = 0x0F,
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    internal sealed class FileIOPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        private FileIOAccess m_read;
        private FileIOAccess m_write;
        private FileIOAccess m_append;
        private FileIOAccess m_pathDiscovery;
        [OptionalField(VersionAdded = 2)]
        private FileIOAccess m_viewAcl;
        [OptionalField(VersionAdded = 2)]
        private FileIOAccess m_changeAcl;
        private bool m_unrestricted;
        
        public FileIOPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_unrestricted = true;
            }
            else if (state == PermissionState.None)
            {
                m_unrestricted = false;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }
        
        public FileIOPermission( FileIOPermissionAccess access, String path )
        {
            VerifyAccess( access );
        
            String[] pathList = new String[] { path };
            AddPathList( access, pathList, false, true, false );
        }

        internal void AddPathList( FileIOPermissionAccess access, String[] pathListOrig, bool checkForDuplicates, bool needFullPath, bool copyPathList )
        {
            AddPathList( access, AccessControlActions.None, pathListOrig, checkForDuplicates, needFullPath, copyPathList );
        }

        internal void AddPathList(FileIOPermissionAccess access, AccessControlActions control, String[] pathListOrig, bool checkForDuplicates, bool needFullPath, bool copyPathList)
        {
            if (pathListOrig == null)
            {
                throw new ArgumentNullException( "pathList" );    
            }
            if (pathListOrig.Length == 0)
            {
                throw new ArgumentException( Environment.GetResourceString("Argument_EmptyPath" ));    
            }
            Contract.EndContractBlock();

            VerifyAccess(access);
                
            if (m_unrestricted)
                return;

            String[] pathList = pathListOrig;
            if(copyPathList)
            {
                // Make a copy of pathList (in case its value changes after we check for illegal chars)
                pathList = new String[pathListOrig.Length];
                Array.Copy(pathListOrig, pathList, pathListOrig.Length);
            }

            ArrayList pathArrayList = StringExpressionSet.CreateListFromExpressions(pathList, needFullPath);

            // If we need the full path the standard illegal characters will be checked in StringExpressionSet.
            CheckIllegalCharacters(pathList, onlyCheckExtras: needFullPath);

            // StringExpressionSet will do minor normalization, trimming spaces and replacing alternate
            // directory separators. It will make an attemt to expand short file names and will check
            // for standard colon placement.
            //
            // If needFullPath is true it will call NormalizePath- which performs short name expansion
            // and does the normal validity checks.

            if ((access & FileIOPermissionAccess.Read) != 0)
            {
                if (m_read == null)
                {
                    m_read = new FileIOAccess();
                }
                m_read.AddExpressions( pathArrayList, checkForDuplicates);
            }
            
            if ((access & FileIOPermissionAccess.Write) != 0)
            {
                if (m_write == null)
                {
                    m_write = new FileIOAccess();
                }
                m_write.AddExpressions( pathArrayList, checkForDuplicates);
            }
    
            if ((access & FileIOPermissionAccess.Append) != 0)
            {
                if (m_append == null)
                {
                    m_append = new FileIOAccess();
                }
                m_append.AddExpressions( pathArrayList, checkForDuplicates);
            }

            if ((access & FileIOPermissionAccess.PathDiscovery) != 0)
            {
                if (m_pathDiscovery == null)
                {
                    m_pathDiscovery = new FileIOAccess( true );
                }
                m_pathDiscovery.AddExpressions( pathArrayList, checkForDuplicates);
            }
        }
        
        public FileIOPermissionAccess AllFiles
        {
            get
            {
                if (m_unrestricted)
                    return FileIOPermissionAccess.AllAccess;
            
                FileIOPermissionAccess access = FileIOPermissionAccess.NoAccess;
                
                if (m_read != null && m_read.AllFiles)
                {
                    access |= FileIOPermissionAccess.Read;
                }
                
                if (m_write != null && m_write.AllFiles)
                {
                    access |= FileIOPermissionAccess.Write;
                }
                
                if (m_append != null && m_append.AllFiles)
                {
                    access |= FileIOPermissionAccess.Append;
                }
                
                if (m_pathDiscovery != null && m_pathDiscovery.AllFiles)
                {
                    access |= FileIOPermissionAccess.PathDiscovery;
                }

                return access;
            }
            
            set
            {
                if (value == FileIOPermissionAccess.AllAccess)
                {
                    m_unrestricted = true;
                    return;
                }
            
                if ((value & FileIOPermissionAccess.Read) != 0)
                {
                    if (m_read == null)
                        m_read = new FileIOAccess();
                        
                    m_read.AllFiles = true;
                }
                else
                {
                    if (m_read != null)
                        m_read.AllFiles = false;
                }
                
                if ((value & FileIOPermissionAccess.Write) != 0)
                {
                    if (m_write == null)
                        m_write = new FileIOAccess();
                        
                    m_write.AllFiles = true;
                }
                else
                {
                    if (m_write != null)
                        m_write.AllFiles = false;
                }
                
                if ((value & FileIOPermissionAccess.Append) != 0)
                {
                    if (m_append == null)
                        m_append = new FileIOAccess();
                        
                    m_append.AllFiles = true;
                }
                else
                {
                    if (m_append != null)
                        m_append.AllFiles = false;
                }

                if ((value & FileIOPermissionAccess.PathDiscovery) != 0)
                {
                    if (m_pathDiscovery == null)
                        m_pathDiscovery = new FileIOAccess( true );
                        
                    m_pathDiscovery.AllFiles = true;
                }
                else
                {
                    if (m_pathDiscovery != null)
                        m_pathDiscovery.AllFiles = false;
                }

            }
        }        
        
        [Pure]
        private static void VerifyAccess( FileIOPermissionAccess access )
        {
            if ((access & ~FileIOPermissionAccess.AllAccess) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access));
        }

        private static void CheckIllegalCharacters(String[] str, bool onlyCheckExtras)
        {
#if !PLATFORM_UNIX
            for (int i = 0; i < str.Length; ++i)
            {
                // FileIOPermission doesn't allow for normalizing across various volume names. This means "C:\" and
                // "\\?\C:\" won't be considered correctly. In addition there are many other aliases for the volume
                // besides "C:" such as (in one concrete example) "\\?\Harddisk0Partition2\", "\\?\HarddiskVolume6\",
                // "\\?\Volume{d1655348-0000-0000-0000-f01500000000}\", etc.
                //
                // We'll continue to explicitly block extended syntax here by disallowing wildcards no matter where
                // they occur in the string (e.g. \\?\ isn't ok)
                if (CheckExtraPathCharacters(str[i]))
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));

                if (!onlyCheckExtras)
                    PathInternal.CheckInvalidPathChars(str[i]);
            }
#else
            // There are no "extras" on Unix
            if (onlyCheckExtras)
                return;

            for (int i = 0; i < str.Length; ++i)
            {
                PathInternal.CheckInvalidPathChars(str[i]);
            }
#endif
        }

#if !PLATFORM_UNIX
        /// <summary>
        /// Check for ?,* and null, ignoring extended syntax.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static bool CheckExtraPathCharacters(string path)
        {
            char currentChar;
            for (int i = 0; i < path.Length; i++)
            {
                currentChar = path[i];

                // We also check for null here as StringExpressionSet will trim it out. (Ensuring we still throw as we always have.)
                if (currentChar == '*' || currentChar == '?' || currentChar == '\0') return true;
            }
            return false;
        }

#endif
        
        private bool IsEmpty()
        {
            return (!m_unrestricted &&
                    (this.m_read == null || this.m_read.IsEmpty()) &&
                    (this.m_write == null || this.m_write.IsEmpty()) &&
                    (this.m_append == null || this.m_append.IsEmpty()) &&
                    (this.m_pathDiscovery == null || this.m_pathDiscovery.IsEmpty()) &&
                    (this.m_viewAcl == null || this.m_viewAcl.IsEmpty()) &&
                    (this.m_changeAcl == null || this.m_changeAcl.IsEmpty()));
        }
        
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsUnrestricted()
        {
            return m_unrestricted;
        }
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return this.IsEmpty();
            }

            FileIOPermission operand = target as FileIOPermission;
            if (operand == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));

            if (operand.IsUnrestricted())
                return true;
            else if (this.IsUnrestricted())
                return false;
            else
                return ((this.m_read == null || this.m_read.IsSubsetOf( operand.m_read )) &&
                        (this.m_write == null || this.m_write.IsSubsetOf( operand.m_write )) &&
                        (this.m_append == null || this.m_append.IsSubsetOf( operand.m_append )) &&
                        (this.m_pathDiscovery == null || this.m_pathDiscovery.IsSubsetOf( operand.m_pathDiscovery )) &&
                        (this.m_viewAcl == null || this.m_viewAcl.IsSubsetOf( operand.m_viewAcl )) &&
                        (this.m_changeAcl == null || this.m_changeAcl.IsSubsetOf( operand.m_changeAcl )));
        }
      
        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }

            FileIOPermission operand = target as FileIOPermission;

            if (operand == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
            else if (this.IsUnrestricted())
            {
                return target.Copy();
            }
    
            if (operand.IsUnrestricted())
            {
                return this.Copy();
            }
            
            FileIOAccess intersectRead = this.m_read == null ? null : this.m_read.Intersect( operand.m_read );
            FileIOAccess intersectWrite = this.m_write == null ? null : this.m_write.Intersect( operand.m_write );
            FileIOAccess intersectAppend = this.m_append == null ? null : this.m_append.Intersect( operand.m_append );
            FileIOAccess intersectPathDiscovery = this.m_pathDiscovery == null ? null : this.m_pathDiscovery.Intersect( operand.m_pathDiscovery );
            FileIOAccess intersectViewAcl = this.m_viewAcl == null ? null : this.m_viewAcl.Intersect( operand.m_viewAcl );
            FileIOAccess intersectChangeAcl = this.m_changeAcl == null ? null : this.m_changeAcl.Intersect( operand.m_changeAcl );

            if ((intersectRead == null || intersectRead.IsEmpty()) &&
                (intersectWrite == null || intersectWrite.IsEmpty()) &&
                (intersectAppend == null || intersectAppend.IsEmpty()) &&
                (intersectPathDiscovery == null || intersectPathDiscovery.IsEmpty()) &&
                (intersectViewAcl == null || intersectViewAcl.IsEmpty()) &&
                (intersectChangeAcl == null || intersectChangeAcl.IsEmpty()))
            {
                return null;
            }
            
            FileIOPermission intersectPermission = new FileIOPermission(PermissionState.None);
            intersectPermission.m_unrestricted = false;
            intersectPermission.m_read = intersectRead;
            intersectPermission.m_write = intersectWrite;
            intersectPermission.m_append = intersectAppend;
            intersectPermission.m_pathDiscovery = intersectPathDiscovery;
            intersectPermission.m_viewAcl = intersectViewAcl;
            intersectPermission.m_changeAcl = intersectChangeAcl;
            
            return intersectPermission;
        }
        
        public override IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return this.Copy();
            }

            FileIOPermission operand = other as FileIOPermission;

            if (operand == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
    
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new FileIOPermission( PermissionState.Unrestricted );
            }
    
            FileIOAccess unionRead = this.m_read == null ? operand.m_read : this.m_read.Union( operand.m_read );
            FileIOAccess unionWrite = this.m_write == null ? operand.m_write : this.m_write.Union( operand.m_write );
            FileIOAccess unionAppend = this.m_append == null ? operand.m_append : this.m_append.Union( operand.m_append );
            FileIOAccess unionPathDiscovery = this.m_pathDiscovery == null ? operand.m_pathDiscovery : this.m_pathDiscovery.Union( operand.m_pathDiscovery );
            FileIOAccess unionViewAcl = this.m_viewAcl == null ? operand.m_viewAcl : this.m_viewAcl.Union( operand.m_viewAcl );
            FileIOAccess unionChangeAcl = this.m_changeAcl == null ? operand.m_changeAcl : this.m_changeAcl.Union( operand.m_changeAcl );
            
            if ((unionRead == null || unionRead.IsEmpty()) &&
                (unionWrite == null || unionWrite.IsEmpty()) &&
                (unionAppend == null || unionAppend.IsEmpty()) &&
                (unionPathDiscovery == null || unionPathDiscovery.IsEmpty()) &&
                (unionViewAcl == null || unionViewAcl.IsEmpty()) &&
                (unionChangeAcl == null || unionChangeAcl.IsEmpty()))
            {
                return null;
            }
            
            FileIOPermission unionPermission = new FileIOPermission(PermissionState.None);
            unionPermission.m_unrestricted = false;
            unionPermission.m_read = unionRead;
            unionPermission.m_write = unionWrite;
            unionPermission.m_append = unionAppend;
            unionPermission.m_pathDiscovery = unionPathDiscovery;
            unionPermission.m_viewAcl = unionViewAcl;
            unionPermission.m_changeAcl = unionChangeAcl;

            return unionPermission;    
        }
        
        public override IPermission Copy()
        {
            FileIOPermission copy = new FileIOPermission(PermissionState.None);
            if (this.m_unrestricted)
            {
                copy.m_unrestricted = true;
            }
            else
            {
                copy.m_unrestricted = false;
                if (this.m_read != null)
                {
                    copy.m_read = this.m_read.Copy();
                }
                if (this.m_write != null)
                {
                    copy.m_write = this.m_write.Copy();
                }
                if (this.m_append != null)
                {
                    copy.m_append = this.m_append.Copy();
                }
                if (this.m_pathDiscovery != null)
                {
                    copy.m_pathDiscovery = this.m_pathDiscovery.Copy();
                }
                if (this.m_viewAcl != null)
                {
                    copy.m_viewAcl = this.m_viewAcl.Copy();
                }
                if (this.m_changeAcl != null)
                {
                    copy.m_changeAcl = this.m_changeAcl.Copy();
                }
            }
            return copy;   
        }

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return FileIOPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.FileIOPermissionIndex;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals(Object obj)
        {
            FileIOPermission perm = obj as FileIOPermission;
            if(perm == null)
                return false;

            if(m_unrestricted && perm.m_unrestricted)
                return true;
            if(m_unrestricted != perm.m_unrestricted)
                return false;

            if(m_read == null)
            {
                if(perm.m_read != null && !perm.m_read.IsEmpty())
                    return false;
            }
            else if(!m_read.Equals(perm.m_read))
                return false;

            if(m_write == null)
            {
                if(perm.m_write != null && !perm.m_write.IsEmpty())
                    return false; 
            }
            else if(!m_write.Equals(perm.m_write))
                return false;

            if(m_append == null)
            {
                if(perm.m_append != null && !perm.m_append.IsEmpty())
                    return false; 
            }
            else if(!m_append.Equals(perm.m_append))
                return false;

            if(m_pathDiscovery == null)
            {
                if(perm.m_pathDiscovery != null && !perm.m_pathDiscovery.IsEmpty())
                    return false; 
            }
            else if(!m_pathDiscovery.Equals(perm.m_pathDiscovery))
                return false;

            if(m_viewAcl == null)
            {
                if(perm.m_viewAcl != null && !perm.m_viewAcl.IsEmpty())
                    return false; 
            }
            else if(!m_viewAcl.Equals(perm.m_viewAcl))
                return false;

            if(m_changeAcl == null)
            {
                if(perm.m_changeAcl != null && !perm.m_changeAcl.IsEmpty())
                    return false; 
            }
            else if(!m_changeAcl.Equals(perm.m_changeAcl))
                return false;

            return true;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            // This implementation is only to silence a compiler warning.
            return base.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class FileIOAccess
    {
        private bool m_ignoreCase = true;
        
        private StringExpressionSet m_set;
        private bool m_allFiles;
        private bool m_allLocalFiles;
        private bool m_pathDiscovery;

        private const String m_strAllFiles = "*AllFiles*";
        private const String m_strAllLocalFiles = "*AllLocalFiles*";

        public FileIOAccess()
        {
            m_set = new StringExpressionSet( m_ignoreCase, true );
            m_allFiles = false;
            m_allLocalFiles = false;
            m_pathDiscovery = false;
        }

        public FileIOAccess( bool pathDiscovery )
        {
            m_set = new StringExpressionSet( m_ignoreCase, true );
            m_allFiles = false;
            m_allLocalFiles = false;
            m_pathDiscovery = pathDiscovery;
        }

        public FileIOAccess( bool allFiles, bool allLocalFiles, bool pathDiscovery )
        {
            m_set = new StringExpressionSet( m_ignoreCase, true );
            m_allFiles = allFiles;
            m_allLocalFiles = allLocalFiles;
            m_pathDiscovery = pathDiscovery;
        }

        public FileIOAccess( StringExpressionSet set, bool allFiles, bool allLocalFiles, bool pathDiscovery )
        {
            m_set = set;
            m_set.SetThrowOnRelative( true );
            m_allFiles = allFiles;
            m_allLocalFiles = allLocalFiles;
            m_pathDiscovery = pathDiscovery;
        }

        private FileIOAccess( FileIOAccess operand )
        {
            m_set = operand.m_set.Copy();
            m_allFiles = operand.m_allFiles;
            m_allLocalFiles = operand.m_allLocalFiles;
            m_pathDiscovery = operand.m_pathDiscovery;
        }

        public void AddExpressions(ArrayList values, bool checkForDuplicates)
        {
            m_allFiles = false;
            m_set.AddExpressions(values, checkForDuplicates);
        }

        public bool AllFiles
        {
            get
            {
                return m_allFiles;
            }

            set
            {
                m_allFiles = value;
            }
        }
        
        public bool IsEmpty()
        {
            return !m_allFiles && !m_allLocalFiles && (m_set == null || m_set.IsEmpty());
        }
        
        public FileIOAccess Copy()
        {
            return new FileIOAccess( this );
        }
        
        public FileIOAccess Union( FileIOAccess operand )
        {
            if (operand == null)
            {
                return this.IsEmpty() ? null : this.Copy();
            }
            
            Debug.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

            if (this.m_allFiles || operand.m_allFiles)
            {
                return new FileIOAccess( true, false, this.m_pathDiscovery );
            }

            return new FileIOAccess( this.m_set.Union( operand.m_set ), false, this.m_allLocalFiles || operand.m_allLocalFiles, this.m_pathDiscovery );
        }
        
        public FileIOAccess Intersect( FileIOAccess operand )
        {
            if (operand == null)
            {
                return null;
            }
            
            Debug.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

            if (this.m_allFiles)
            {
                if (operand.m_allFiles)
                {
                    return new FileIOAccess( true, false, this.m_pathDiscovery );
                }
                else
                {
                    return new FileIOAccess( operand.m_set.Copy(), false, operand.m_allLocalFiles, this.m_pathDiscovery );
                }
            }
            else if (operand.m_allFiles)
            {
                return new FileIOAccess( this.m_set.Copy(), false, this.m_allLocalFiles, this.m_pathDiscovery );
            }

            StringExpressionSet intersectionSet = new StringExpressionSet( m_ignoreCase, true );

            if (this.m_allLocalFiles)
            {
                String[] expressions = operand.m_set.UnsafeToStringArray();
                
                if (expressions != null)
                {
                    for (int i = 0; i < expressions.Length; ++i)
                    {
                        String root = GetRoot( expressions[i] );
                        if (root != null && IsLocalDrive( GetRoot( root ) ) )
                        {
                            intersectionSet.AddExpressions( new String[] { expressions[i] }, true, false );
                        }
                    }
                }
            }

            if (operand.m_allLocalFiles)
            {
                String[] expressions = this.m_set.UnsafeToStringArray();

                if (expressions != null)
                {
                    for (int i = 0; i < expressions.Length; ++i)
                    {
                        String root = GetRoot( expressions[i] );
                        if (root != null && IsLocalDrive(GetRoot(root)))
                        {
                            intersectionSet.AddExpressions( new String[] { expressions[i] }, true, false );
                        }
                    }
                }
            }

            String[] regularIntersection = this.m_set.Intersect( operand.m_set ).UnsafeToStringArray();

            if (regularIntersection != null)
                intersectionSet.AddExpressions( regularIntersection, !intersectionSet.IsEmpty(), false );

            return new FileIOAccess( intersectionSet, false, this.m_allLocalFiles && operand.m_allLocalFiles, this.m_pathDiscovery );
        }
    
        public bool IsSubsetOf( FileIOAccess operand )
        {
            if (operand == null)
            {
                return this.IsEmpty();
            }
            
            if (operand.m_allFiles)
            {
                return true;
            }
            
            Debug.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

            if (!((m_pathDiscovery && this.m_set.IsSubsetOfPathDiscovery( operand.m_set )) || this.m_set.IsSubsetOf( operand.m_set )))
            {
                if (operand.m_allLocalFiles)
                {
                    String[] expressions = m_set.UnsafeToStringArray();
                
                    for (int i = 0; i < expressions.Length; ++i)
                    {
                        String root = GetRoot( expressions[i] );
                        if (root == null || !IsLocalDrive(GetRoot(root)))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            
            return true;
        }

        private static String GetRoot( String path )
        {
#if !PLATFORM_UNIX
            String str = path.Substring( 0, 3 );
            if (str.EndsWith( ":\\", StringComparison.Ordinal))
#else
            String str = path.Substring( 0, 1 );
            if(str ==  "/")
#endif // !PLATFORM_UNIX
            {
                return str;
            }
            else
            {
                return null;
            }
        }
        
        public override String ToString()
        {
            // SafeCritical: all string expression sets are constructed with the throwOnRelative bit set, so
            // we're only exposing out the same paths that we took as input.
            if (m_allFiles)
            {
                return m_strAllFiles;
            }
            else
            {
                if (m_allLocalFiles)
                {
                    String retstr = m_strAllLocalFiles;

                    String tempStr = m_set.UnsafeToString();

                    if (tempStr != null && tempStr.Length > 0)
                        retstr += ";" + tempStr;

                    return retstr;
                }
                else
                {
                    return m_set.UnsafeToString();
                }
            }
        }
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool IsLocalDrive(String path);

        public override bool Equals(Object obj)
        {
            FileIOAccess operand = obj as FileIOAccess;
            if(operand == null)
                return (IsEmpty() && obj == null);
            Debug.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );
            if(m_pathDiscovery)
            {
                if(this.m_allFiles && operand.m_allFiles)
                    return true;
                if(this.m_allLocalFiles == operand.m_allLocalFiles &&
                    m_set.IsSubsetOf(operand.m_set) &&
                    operand.m_set.IsSubsetOf(m_set)) // Watch Out: This calls StringExpressionSet.IsSubsetOf, unlike below
                    return true;
                return false;
            }
            else
            {
                if(!this.IsSubsetOf(operand)) // Watch Out: This calls FileIOAccess.IsSubsetOf, unlike above
                    return false;
                if(!operand.IsSubsetOf(this))
                    return false;
                return true;
            }
        }

        public override int GetHashCode()
        {
            // This implementation is only to silence a compiler warning.
            return base.GetHashCode();
        }
    }
}
