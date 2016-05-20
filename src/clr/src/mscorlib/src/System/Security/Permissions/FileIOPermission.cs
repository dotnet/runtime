// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
#if FEATURE_CAS_POLICY
    using SecurityElement = System.Security.SecurityElement;
#endif // FEATURE_CAS_POLICY
    using System.Security.AccessControl;
    using System.Security.Util;
    using System.IO;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

[Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FileIOPermissionAccess
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
    sealed public class FileIOPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
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
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOPermission( FileIOPermissionAccess access, String path )
        {
            VerifyAccess( access );
        
            String[] pathList = new String[] { path };
            AddPathList( access, pathList, false, true, false );
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOPermission( FileIOPermissionAccess access, String[] pathList )
        {
            VerifyAccess( access );
        
            AddPathList( access, pathList, false, true, false );
        }

#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOPermission( FileIOPermissionAccess access, AccessControlActions control, String path )
        {
            VerifyAccess( access );
        
            String[] pathList = new String[] { path };
            AddPathList( access, control, pathList, false, true, false );
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOPermission( FileIOPermissionAccess access, AccessControlActions control, String[] pathList )
            : this( access, control, pathList, true, true )
        {
        }
#endif

        [System.Security.SecurityCritical]  // auto-generated
        internal FileIOPermission( FileIOPermissionAccess access, String[] pathList, bool checkForDuplicates, bool needFullPath )
        {
            VerifyAccess( access );
        
            AddPathList( access, pathList, checkForDuplicates, needFullPath, true );
        }

#if FEATURE_MACL
        [System.Security.SecurityCritical]  // auto-generated
        internal FileIOPermission( FileIOPermissionAccess access, AccessControlActions control, String[] pathList, bool checkForDuplicates, bool needFullPath )
        {
            VerifyAccess( access );
        
            AddPathList( access, control, pathList, checkForDuplicates, needFullPath, true );
        }
#endif

        public void SetPathList( FileIOPermissionAccess access, String path )
        {
            String[] pathList;
            if(path == null)
                pathList = new String[] {};
            else
                pathList = new String[] { path };
            SetPathList( access, pathList, false );
        }
            
        public void SetPathList( FileIOPermissionAccess access, String[] pathList )
        {
            SetPathList( access, pathList, true );
        }

        internal void SetPathList( FileIOPermissionAccess access, 
            String[] pathList, bool checkForDuplicates )
        {
            SetPathList( access, AccessControlActions.None, pathList, checkForDuplicates );
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal void SetPathList( FileIOPermissionAccess access, AccessControlActions control, String[] pathList, bool checkForDuplicates )
        {
            VerifyAccess( access );
            
            if ((access & FileIOPermissionAccess.Read) != 0)
                m_read = null;
            
            if ((access & FileIOPermissionAccess.Write) != 0)
                m_write = null;
    
            if ((access & FileIOPermissionAccess.Append) != 0)
                m_append = null;

            if ((access & FileIOPermissionAccess.PathDiscovery) != 0)
                m_pathDiscovery = null;

#if FEATURE_MACL
            if ((control & AccessControlActions.View) != 0)
                m_viewAcl = null;

            if ((control & AccessControlActions.Change) != 0)
                m_changeAcl = null;
#else
            m_viewAcl = null;
            m_changeAcl = null;
#endif
            
            m_unrestricted = false;
#if FEATURE_MACL
            AddPathList( access, control, pathList, checkForDuplicates, true, true );
#else
            AddPathList( access, pathList, checkForDuplicates, true, true );
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddPathList( FileIOPermissionAccess access, String path )
        {
            String[] pathList;
            if(path == null)
                pathList = new String[] {};
            else
                pathList = new String[] { path };
            AddPathList( access, pathList, false, true, false );
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddPathList( FileIOPermissionAccess access, String[] pathList )
        {
            AddPathList( access, pathList, true, true, true );
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void AddPathList( FileIOPermissionAccess access, String[] pathListOrig, bool checkForDuplicates, bool needFullPath, bool copyPathList )
        {
            AddPathList( access, AccessControlActions.None, pathListOrig, checkForDuplicates, needFullPath, copyPathList );
        }

        [System.Security.SecurityCritical]  // auto-generated
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

#if FEATURE_MACL
            if ((control & AccessControlActions.View) != 0)
            {
                if (m_viewAcl == null)
                {
                    m_viewAcl = new FileIOAccess();
                }
                m_viewAcl.AddExpressions( pathArrayList, checkForDuplicates);
            }

            if ((control & AccessControlActions.Change) != 0)
            {
                if (m_changeAcl == null)
                {
                    m_changeAcl = new FileIOAccess();
                }
                m_changeAcl.AddExpressions( pathArrayList, checkForDuplicates);
            }
#endif
        }
        
        [SecuritySafeCritical]
        public String[] GetPathList( FileIOPermissionAccess access )
        {
            VerifyAccess( access );
            ExclusiveAccess( access );
    
            if (AccessIsSet( access, FileIOPermissionAccess.Read ))
            {
                if (m_read == null)
                {
                    return null;
                }
                return m_read.ToStringArray();
            }
            
            if (AccessIsSet( access, FileIOPermissionAccess.Write ))
            {
                if (m_write == null)
                {
                    return null;
                }
                return m_write.ToStringArray();
            }
    
            if (AccessIsSet( access, FileIOPermissionAccess.Append ))
            {
                if (m_append == null)
                {
                    return null;
                }
                return m_append.ToStringArray();
            }
            
            if (AccessIsSet( access, FileIOPermissionAccess.PathDiscovery ))
            {
                if (m_pathDiscovery == null)
                {
                    return null;
                }
                return m_pathDiscovery.ToStringArray();
            }

            // not reached
            
            return null;
        }
        

        public FileIOPermissionAccess AllLocalFiles
        {
            get
            {
                if (m_unrestricted)
                    return FileIOPermissionAccess.AllAccess;
            
                FileIOPermissionAccess access = FileIOPermissionAccess.NoAccess;
                
                if (m_read != null && m_read.AllLocalFiles)
                {
                    access |= FileIOPermissionAccess.Read;
                }
                
                if (m_write != null && m_write.AllLocalFiles)
                {
                    access |= FileIOPermissionAccess.Write;
                }
                
                if (m_append != null && m_append.AllLocalFiles)
                {
                    access |= FileIOPermissionAccess.Append;
                }

                if (m_pathDiscovery != null && m_pathDiscovery.AllLocalFiles)
                {
                    access |= FileIOPermissionAccess.PathDiscovery;
                }
                
                return access;
            }
            
            set
            {
                if ((value & FileIOPermissionAccess.Read) != 0)
                {
                    if (m_read == null)
                        m_read = new FileIOAccess();
                        
                    m_read.AllLocalFiles = true;
                }
                else
                {
                    if (m_read != null)
                        m_read.AllLocalFiles = false;
                }
                
                if ((value & FileIOPermissionAccess.Write) != 0)
                {
                    if (m_write == null)
                        m_write = new FileIOAccess();
                        
                    m_write.AllLocalFiles = true;
                }
                else
                {
                    if (m_write != null)
                        m_write.AllLocalFiles = false;
                }
                
                if ((value & FileIOPermissionAccess.Append) != 0)
                {
                    if (m_append == null)
                        m_append = new FileIOAccess();
                        
                    m_append.AllLocalFiles = true;
                }
                else
                {
                    if (m_append != null)
                        m_append.AllLocalFiles = false;
                }

                if ((value & FileIOPermissionAccess.PathDiscovery) != 0)
                {
                    if (m_pathDiscovery == null)
                        m_pathDiscovery = new FileIOAccess( true );
                        
                    m_pathDiscovery.AllLocalFiles = true;
                }
                else
                {
                    if (m_pathDiscovery != null)
                        m_pathDiscovery.AllLocalFiles = false;
                }

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
        
        [Pure]
        private static void ExclusiveAccess( FileIOPermissionAccess access )
        {
            if (access == FileIOPermissionAccess.NoAccess)
            {
                throw new ArgumentException( Environment.GetResourceString("Arg_EnumNotSingleFlag") ); 
            }
    
            if (((int) access & ((int)access-1)) != 0)
            {
                throw new ArgumentException( Environment.GetResourceString("Arg_EnumNotSingleFlag") ); 
            }
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
                    Path.CheckInvalidPathChars(str[i]);
            }
#else
            // There are no "extras" on Unix
            if (onlyCheckExtras)
                return;

            for (int i = 0; i < str.Length; ++i)
            {
                Path.CheckInvalidPathChars(str[i]);
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

        private static bool AccessIsSet( FileIOPermissionAccess access, FileIOPermissionAccess question )
        {
            return (access & question) != 0;
        }
        
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
   
#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.FileIOPermission" );
            if (!IsUnrestricted())
            {
                if (this.m_read != null && !this.m_read.IsEmpty())
                {
                    esd.AddAttribute( "Read", SecurityElement.Escape( m_read.ToString() ) );
                }
                if (this.m_write != null && !this.m_write.IsEmpty())
                {
                    esd.AddAttribute( "Write", SecurityElement.Escape( m_write.ToString() ) );
                }
                if (this.m_append != null && !this.m_append.IsEmpty())
                {
                    esd.AddAttribute( "Append", SecurityElement.Escape( m_append.ToString() ) );
                }
                if (this.m_pathDiscovery != null && !this.m_pathDiscovery.IsEmpty())
                {
                    esd.AddAttribute( "PathDiscovery", SecurityElement.Escape( m_pathDiscovery.ToString() ) );
                }
                if (this.m_viewAcl != null && !this.m_viewAcl.IsEmpty())
                {
                    esd.AddAttribute( "ViewAcl", SecurityElement.Escape( m_viewAcl.ToString() ) );
                }
                if (this.m_changeAcl != null && !this.m_changeAcl.IsEmpty())
                {
                    esd.AddAttribute( "ChangeAcl", SecurityElement.Escape( m_changeAcl.ToString() ) );
                }

            }
            else
            {
                esd.AddAttribute( "Unrestricted", "true" );
            }
            return esd;
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void FromXml(SecurityElement esd)
        {
            CodeAccessPermission.ValidateElement( esd, this );
            String et;
            
            if (XMLUtil.IsUnrestricted(esd))
            {
                m_unrestricted = true;
                return;
            }
    
            
            m_unrestricted = false;
            
            et = esd.Attribute( "Read" );
            if (et != null)
            {
                m_read = new FileIOAccess( et );
            }
            else
            {
                m_read = null;
            }
            
            et = esd.Attribute( "Write" );
            if (et != null)
            {
                m_write = new FileIOAccess( et );
            }
            else
            {
                m_write = null;
            }
    
            et = esd.Attribute( "Append" );
            if (et != null)
            {
                m_append = new FileIOAccess( et );
            }
            else
            {
                m_append = null;
            }

            et = esd.Attribute( "PathDiscovery" );
            if (et != null)
            {
                m_pathDiscovery = new FileIOAccess( et );
                m_pathDiscovery.PathDiscovery = true;
            }
            else
            {
                m_pathDiscovery = null;
            }

            et = esd.Attribute( "ViewAcl" );
            if (et != null)
            {
                m_viewAcl = new FileIOAccess( et );
            }
            else
            {
                m_viewAcl = null;
            }

            et = esd.Attribute( "ChangeAcl" );
            if (et != null)
            {
                m_changeAcl = new FileIOAccess( et );
            }
            else
            {
                m_changeAcl = null;
            }
        }
#endif // FEATURE_CAS_POLICY

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

        /// <summary>
        /// Call this method if you don't need a the FileIOPermission for anything other than calling Demand() once.
        /// 
        /// This method tries to verify full access before allocating a FileIOPermission object.
        /// If full access is there, then we still have to emulate the checks that creating the 
        /// FileIOPermission object would have performed.
        /// 
        /// IMPORTANT: This method should only be used after calling GetFullPath on the path to verify
        /// </summary>
        [System.Security.SecuritySafeCritical]
        internal static void QuickDemand(FileIOPermissionAccess access, string fullPath, bool checkForDuplicates = false, bool needFullPath = false)
        {
#if FEATURE_CAS_POLICY
            if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                new FileIOPermission(access, new string[] { fullPath }, checkForDuplicates, needFullPath).Demand();
            }
            else
#endif
            {
                EmulateFileIOPermissionChecks(fullPath);
            }
        }

        /// <summary>
        /// Call this method if you don't need a the FileIOPermission for anything other than calling Demand() once.
        /// 
        /// This method tries to verify full access before allocating a FileIOPermission object.
        /// If full access is there, then we still have to emulate the checks that creating the 
        /// FileIOPermission object would have performed.
        /// 
        /// IMPORTANT: This method should only be used after calling GetFullPath on the path to verify
        /// 
        /// </summary>
        [System.Security.SecuritySafeCritical]
        internal static void QuickDemand(FileIOPermissionAccess access, string[] fullPathList, bool checkForDuplicates = false, bool needFullPath = true)
        {
#if FEATURE_CAS_POLICY
            if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                new FileIOPermission(access, fullPathList, checkForDuplicates, needFullPath).Demand();
            }
            else
#endif
            {
                foreach (string fullPath in fullPathList)
                {
                    EmulateFileIOPermissionChecks(fullPath);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        internal static void QuickDemand(PermissionState state)
        {
            // Should be a no-op without CAS
#if FEATURE_CAS_POLICY
            if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                new FileIOPermission(state).Demand();
            }
#endif
        }

#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]
        internal static void QuickDemand(FileIOPermissionAccess access, AccessControlActions control, string fullPath, bool checkForDuplicates = false, bool needFullPath = true)
        {
            if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                new FileIOPermission(access, control, new string[] { fullPath }, checkForDuplicates, needFullPath).Demand();
            }
            else
            {
                EmulateFileIOPermissionChecks(fullPath);
            }
        }

        [System.Security.SecuritySafeCritical]
        internal static void QuickDemand(FileIOPermissionAccess access, AccessControlActions control, string[] fullPathList, bool checkForDuplicates = true, bool needFullPath = true)
        {
            if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                new FileIOPermission(access, control, fullPathList, checkForDuplicates, needFullPath).Demand();
            }
            else
            {
                foreach (string fullPath in fullPathList)
                {
                    EmulateFileIOPermissionChecks(fullPath);
                }
            }
        }
#endif

        /// <summary>
        /// Perform the additional path checks that would normally happen when creating a FileIOPermission object.
        /// </summary>
        /// <param name="fullPath">A path that has already gone through GetFullPath or Normalize</param>
        internal static void EmulateFileIOPermissionChecks(string fullPath)
        {
            // Callers should have already made checks for invalid path format via normalization. This method will only make the
            // additional checks needed to throw the same exceptions that would normally throw when using FileIOPermission.
            // These checks are done via CheckIllegalCharacters() and StringExpressionSet in AddPathList() above.
            //
            // We have to check the beginning as some paths may be passed in as path + @"\.", which will be normalized away.
            BCLDebug.Assert(
                fullPath.StartsWith(Path.NormalizePath(fullPath, fullCheck: false), StringComparison.OrdinalIgnoreCase),
                string.Format("path isn't normalized: {0}", fullPath));

            // Checking for colon / invalid characters on device paths blocks legitimate access to objects such as named pipes.
            if (
#if FEATURE_PATHCOMPAT
                AppContextSwitches.UseLegacyPathHandling ||
#endif
                !PathInternal.IsDevice(fullPath))
            {
                // GetFullPath already checks normal invalid path characters. We need to just check additional (wildcard) characters here.
                // (By calling the standard helper we can allow extended paths \\?\ through when the support is enabled.)
                if (PathInternal.HasWildCardCharacters(fullPath))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
                }

                if (PathInternal.HasInvalidVolumeSeparator(fullPath))
                {
                    throw new NotSupportedException(Environment.GetResourceString("Argument_PathFormatNotSupported"));
                }
            }
        }
    }

    [Serializable]
    internal sealed class FileIOAccess
    {
#if !FEATURE_CASE_SENSITIVE_FILESYSTEM
        private bool m_ignoreCase = true;
#else
        private bool m_ignoreCase = false;
#endif // !FEATURE_CASE_SENSITIVE_FILESYSTEM
        
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

        [System.Security.SecurityCritical]  // auto-generated
        public FileIOAccess( String value )
        {
            if (value == null)
            {
                m_set = new StringExpressionSet( m_ignoreCase, true );
                m_allFiles = false;
                m_allLocalFiles = false;
            }
            else if (value.Length >= m_strAllFiles.Length && String.Compare( m_strAllFiles, value, StringComparison.Ordinal) == 0)
            {
                m_set = new StringExpressionSet( m_ignoreCase, true );
                m_allFiles = true;
                m_allLocalFiles = false;
            }
            else if (value.Length >= m_strAllLocalFiles.Length && String.Compare( m_strAllLocalFiles, 0, value, 0, m_strAllLocalFiles.Length, StringComparison.Ordinal) == 0)
            {
                m_set = new StringExpressionSet( m_ignoreCase, value.Substring( m_strAllLocalFiles.Length ), true );
                m_allFiles = false;
                m_allLocalFiles = true;
            }
            else
            {
                m_set = new StringExpressionSet( m_ignoreCase, value, true );
                m_allFiles = false;
                m_allLocalFiles = false;
            }
            m_pathDiscovery = false;
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

        [System.Security.SecurityCritical]  // auto-generated
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

        public bool AllLocalFiles
        {
            get
            {
                return m_allLocalFiles;
            }
            
            set
            {
                m_allLocalFiles = value;
            }
        }

        public bool PathDiscovery
        {
            set
            {
                m_pathDiscovery = value;
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
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOAccess Union( FileIOAccess operand )
        {
            if (operand == null)
            {
                return this.IsEmpty() ? null : this.Copy();
            }
            
            Contract.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

            if (this.m_allFiles || operand.m_allFiles)
            {
                return new FileIOAccess( true, false, this.m_pathDiscovery );
            }

            return new FileIOAccess( this.m_set.Union( operand.m_set ), false, this.m_allLocalFiles || operand.m_allLocalFiles, this.m_pathDiscovery );
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileIOAccess Intersect( FileIOAccess operand )
        {
            if (operand == null)
            {
                return null;
            }
            
            Contract.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

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
    
        [System.Security.SecuritySafeCritical]  // auto-generated
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
            
            Contract.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );

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
        
        [SecuritySafeCritical]
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

        [SecuritySafeCritical]
        public String[] ToStringArray()
        {
            // SafeCritical: all string expression sets are constructed with the throwOnRelative bit set, so
            // we're only exposing out the same paths that we took as input.
            return m_set.UnsafeToStringArray();
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool IsLocalDrive(String path);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool Equals(Object obj)
        {
            FileIOAccess operand = obj as FileIOAccess;
            if(operand == null)
                return (IsEmpty() && obj == null);
            Contract.Assert( this.m_pathDiscovery == operand.m_pathDiscovery, "Path discovery settings must match" );
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
