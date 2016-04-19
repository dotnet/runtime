//--------------------------------------------------------------------------
//
//		FileSysIntLayer.cs
//
//		Define concrete classes to provide testing entry point for abstract classes:
//				AccessRule
//				AuditRule
//				NativeObjectSecurity
//				CommonObjectSecurity
//				ObjectSecurity
//				
//
//		Copyright (C) Microsoft Corporation, 2003
//
//--------------------------------------------------------------------------


using System;
using System.Collections;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
//using System.Runtime.Handles;

namespace System.Security.AccessControl.Test
{

	class MyException:Exception
	{
		const string myMessage = "My Exception for Intelligent Exception Handling";
		public MyException():base(myMessage)
		{}			
	}

    [Flags]
    public enum FileSystemRights
    {
        None                        = 0x000000,
        ReadData                    = 0x000001,
        ListFolder                  = ReadData,
        WriteData                   = 0x000002,
        CreateFiles                 = WriteData,
        AppendData                  = 0x000004,
        CreateFolders               = AppendData,
        ReadExtendedAttributes      = 0x000008,
        WriteExtendedAttributes     = 0x000010,
        ExecuteFile                 = 0x000020,
        Traverse                    = ExecuteFile,
        ReadAttributes              = 0x000080,
        WriteAttributes             = 0x000100,
        Delete                      = 0x010000,
        ReadPermissions             = 0x020000,
        ChangePermissions           = 0x040000,
        TakeOwnership               = 0x080000,
        DeleteSubfoldersAndFiles    = 0x100000,
        FullControl                 = 0x1F01BF,
    }

    sealed class FileSystemRightsTranslator
    {
        #region Access mask to rights translation
        
        internal static int AccessMaskFromRights( FileSystemRights fileSystemRights )
        {
            return ( int )fileSystemRights;
        }

        internal static FileSystemRights RightsFromAccessMask( int accessMask )
        {
            return ( FileSystemRights )accessMask;
        }

        #endregion
    }

    public sealed class FileSystemAccessRule : AccessRule
    {
        #region Constructors

        //
        // Constructor for creating access rules for file objects
        //

        public FileSystemAccessRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            AccessControlType type )
            : this(
                identity,
			    FileSystemRightsTranslator.AccessMaskFromRights( fileSystemRights ),
			    false,
                InheritanceFlags.None,
                PropagationFlags.None,
                type )
        {
        }

        //
        // Constructor for creating access rules for folder objects
        //

        public FileSystemAccessRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type )
            : this(
                identity,
                FileSystemRightsTranslator.AccessMaskFromRights( fileSystemRights ),
                false,
                inheritanceFlags,
                propagationFlags,
                type )
        {
        }

        //
        // constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //

        public FileSystemAccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type )
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type )
        {
        }

        #endregion

        #region Public properties        

        public FileSystemRights FileSystemRights
        {
            get { return FileSystemRightsTranslator.RightsFromAccessMask( base.AccessMask ); }
        }

	//directly expose base class's AccessMask for testing purpose
	public new int AccessMask
	{
		get {return base.AccessMask;}
	}

        #endregion
    }

    public sealed class FileSystemAuditRule : AuditRule
    {
        #region Constructors

        public FileSystemAuditRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            AuditFlags flags )
            : this(
                identity,
			    FileSystemRightsTranslator.AccessMaskFromRights( fileSystemRights ),
			    false,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags )
        {
        }

        public FileSystemAuditRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags )
            : this(
                identity,
                FileSystemRightsTranslator.AccessMaskFromRights( fileSystemRights ),
                false,
                inheritanceFlags,
                propagationFlags,
                flags )
        {
        }

        public FileSystemAuditRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags )
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags )
        {
        }

        #endregion

        #region Public properties

        public FileSystemRights FileSystemRights
        {
            get { return FileSystemRightsTranslator.RightsFromAccessMask( base.AccessMask ); }
        }

	//directly expose base class's AccessMask for testing purpose
	public new int AccessMask
	{
		get {return base.AccessMask;}
	}
        #endregion
    }

    public abstract class FileSystemSecurity : NativeObjectSecurity
    {
        #region Member variables        

        private const ResourceType s_ResourceType = ResourceType.FileObject;

        #endregion

	#region Constructors

        internal FileSystemSecurity( bool isContainer )
            : base( isContainer, s_ResourceType )
        {
        }

        internal FileSystemSecurity( bool isContainer , bool needIntellegentException)
            : base( isContainer, s_ResourceType, new ExceptionFromErrorCode(FileSystemSecurity.CreateMyException), new Object())
        {
        }
	internal FileSystemSecurity( bool isContainer, string name, AccessControlSections includeRules ) 
            : base( isContainer, s_ResourceType, name, includeRules) 
        {
        }

        internal FileSystemSecurity( bool isContainer, SafeHandle handle, AccessControlSections includeRules ) 
            : base( isContainer, s_ResourceType, handle, includeRules) 
        {
        }

	private static Exception CreateMyException(int errorCode, string name, SafeHandle handle, object context)
	{
		return new MyException();
			
	}
	//add constructor for intellegient error handling
	internal FileSystemSecurity( bool isContainer, string name, AccessControlSections includeRules, bool needIntellegentException)
            : base( isContainer, s_ResourceType, name, includeRules, new ExceptionFromErrorCode(FileSystemSecurity.CreateMyException), new Object()) 
        {
        }

	internal protected FileSystemSecurity( bool isContainer, SafeHandle handle, AccessControlSections includeRules, bool needIntellegentException ) 
            : base( isContainer, s_ResourceType, handle, includeRules, new ExceptionFromErrorCode(FileSystemSecurity.CreateMyException), new Object()) 
        {
        }	

	#endregion

        #region Factories
        
        public sealed override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type )
        {
            return new FileSystemAccessRule(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type );
        }
        
        public sealed override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags )
        {
            return new FileSystemAuditRule(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags );
        }
       
        #endregion

        #region Public Methods

	public void SetReadLock()
        {
		ReadLock();
        }
	public void UnsetReadLock()
	{
		ReadUnlock();
	}

	public void SetWriteLock()
	{
		WriteLock();
	}

	public void UnsetWriteLock()
	{
		WriteUnlock();
	}
        public bool GetIsContainer()
        {
            return IsContainer;
        }

        public bool GetIsDS()
        {
            return IsDS;
        }

        public bool GetOwnerModified()
        {
            return OwnerModified;
        }

        public void SetOwnerModified(bool value)
        {
            OwnerModified = value;
        }

        public bool GetGroupModified()
        {
            return GroupModified;
        }

        public void SetGroupModified(bool value)
        {
            GroupModified = value;
        }

        public bool GetAuditRulesModified()
        {
            return AuditRulesModified;
        }

        public void SetAuditRulesModified(bool value)
        {
            AuditRulesModified = value;
        }

        public bool GetAccessRulesModified()
        {
            return AccessRulesModified;
        }

        public void SetAccessRulesModified(bool value)
        {
            AccessRulesModified = value;
        }
 

        public void PersistByName(string name, AccessControlSections persistRules)
        {		
		base.Persist(name, persistRules);
        }

        public void PersistByHandle( SafeHandle handle, AccessControlSections persistRules) 
        {
    
            base.Persist( handle, persistRules ); 
        }

	//add for intellegient error handling
        public void PersistByName(string name, AccessControlSections persistRules, bool needIntellegentException)
        {		
		base.Persist(name, persistRules, new Object());
        }

        public void PersistByHandle( SafeHandle handle, AccessControlSections persistRules, bool needIntellegentException) 
        {
    
            base.Persist( handle, persistRules, new Object() ); 
        }

	//add for enable take ownership
	public void PersistByName(bool enableOwnershipPrivilege, string name, AccessControlSections persistRules)
	{
		base.Persist(enableOwnershipPrivilege, name, persistRules);
		
	}
	public bool AccessModify(AccessControlModification modification, AccessRule rule, out bool modified)
	{
		return ModifyAccess(modification, rule, out modified);
	}

	public bool AuditModify(AccessControlModification modification, AuditRule rule, out bool modified)
	{
		return ModifyAudit(modification, rule, out modified);
	}
        
        public void AddAccessRule( FileSystemAccessRule rule )
        {
            base.AddAccessRule( rule );
        }
        
        public void SetAccessRule( FileSystemAccessRule rule )
        {
            base.SetAccessRule( rule );
        }
        
        public void ResetAccessRule( FileSystemAccessRule rule )
        {
            base.ResetAccessRule( rule );
        }
        
        public bool RemoveAccessRule( FileSystemAccessRule rule )
        {
            return base.RemoveAccessRule( rule );
        }
        
        public void RemoveAccessRuleAll( FileSystemAccessRule rule )
        {
            base.RemoveAccessRuleAll( rule );
        }
        
        public void RemoveAccessRuleSpecific( FileSystemAccessRule rule )
        {
            base.RemoveAccessRuleSpecific( rule );
        }
        
        public void AddAuditRule( FileSystemAuditRule rule )
        {
            base.AddAuditRule( rule );
        }
        
        public void SetAuditRule( FileSystemAuditRule rule )
        {
            base.SetAuditRule( rule );
        }
        
        public bool RemoveAuditRule( FileSystemAuditRule rule )
        {
            return base.RemoveAuditRule( rule );
        }
        
        public void RemoveAuditRuleAll( FileSystemAuditRule rule )
        {
            base.RemoveAuditRuleAll( rule );
        }
        
        public void RemoveAuditRuleSpecific( FileSystemAuditRule rule )
        {
            base.RemoveAuditRuleSpecific( rule );
        }


        public override Type AccessRightType 
	{ 
		get{ return null;	}
	}
        public override Type AccessRuleType 
	{
		get { return typeof (System.Security.AccessControl.Test.FileSystemAccessRule); }
	}
        public override Type AuditRuleType 
	{ 
		get { return typeof(System.Security.AccessControl.Test.FileSystemAuditRule);} 
	}
        
#endregion
    }

    public class FileSecurity : FileSystemSecurity
    {
        #region Constructors

        public FileSecurity()
            : base( false )
        {
        }

        public FileSecurity(bool needIntellegentException)
            : base( false, needIntellegentException)
        {
        }
		
        public FileSecurity( string name, AccessControlSections includeRules ) 
            : base( false, name, includeRules) 
        {
        }

        public FileSecurity( SafeHandle handle, AccessControlSections includeRules ) 
            : base( false, handle, includeRules) 
        {
        }

	//to test intelligent exception handling
        internal protected FileSecurity( string name, AccessControlSections includeRules, bool needIntellegentException)
            : base( false, name, includeRules, true ) 
        {
        }

        internal protected FileSecurity( SafeHandle handle, AccessControlSections includeRules, bool needIntellegentException)
            : base( false, handle, includeRules, true ) 
        {
        }
	

        #endregion
    }
    
    public class FolderSecurity : FileSystemSecurity
    {
        #region Constructors

        public FolderSecurity()
            : base( true )
        {
        }

        public FolderSecurity(bool needIntellegentException)
            : base( true, needIntellegentException)
        {
        }
        public FolderSecurity( string name, AccessControlSections includeRules ) 
            : base( true, name, includeRules) 
        {
        }

        public FolderSecurity( SafeHandle handle, AccessControlSections includeRules ) 
            : base( true, handle, includeRules) 
        {
        }

	//to test intelligent exception handling
       internal protected FolderSecurity( string name, AccessControlSections includeRules, bool needIntellegentException)
            : base( true, name, includeRules, true  ) 
        {
        }

        internal protected FolderSecurity( SafeHandle handle, AccessControlSections includeRules, bool needIntellegentException)
            : base( true, handle, includeRules, true  ) 
        {
        }	
        #endregion
    }
}
