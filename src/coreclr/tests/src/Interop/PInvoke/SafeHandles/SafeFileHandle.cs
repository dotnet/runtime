// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;

//	special subclass for out/ref ChildSFH params that are changed in unmanaged code 
//	(see comments above ReleaseHandle for details)
namespace SafeHandlesTests{
    public class ChildSFH_NoCloseHandle : SafeFileHandle
    {
        ///////////////////////////////////////////////////////////
        private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

        //0 or -1 considered invalid
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
        }

        //each SafeHandle subclass will expose a static method for instance creation
        [DllImport("api-ms-win-core-file-l1-2-1", EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern ChildSFH_NoCloseHandle CreateChildSafeFileHandle(String lpFileName,
            DesiredAccess dwDesiredAccess, ShareMode dwShareMode,
            IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition,
            FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        //default constructor which just calls the base class constructor
        public ChildSFH_NoCloseHandle()
            : base()
        {
        }

        //	this method will not actually call any resource releasing API method
        //	since the out/ref ChildSFH param is not actually initialized to an OS allocated
        //	HANDLE---instead the unmanaged side just initializes/changes it to some integer;
        //	If a resource releasing API method like CloseHandle were called then
        //	it would return false and an unhandled exception would be thrown by the
        //	runtime indicating that the release method failed
        override protected bool ReleaseHandle()
        {
            return true;
        }

    } //end fo ChildSFH_NoCloseHandle

    public class ChildSafeFileHandle : SafeFileHandle
    {
        //each SafeHandle subclass will expose a static method for instance creation
        [DllImport("api-ms-win-core-file-l1-2-1", EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern ChildSafeFileHandle CreateChildSafeFileHandle(String lpFileName,
            DesiredAccess dwDesiredAccess, ShareMode dwShareMode,
            IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition,
            FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        //default constructor which just calls the base class constructor
        public ChildSafeFileHandle()
            : base()
        {
        }

    } //end fo ChildSafeFileHandle

    //	special subclass for out/ref SFH params that are changed in unmanaged code 
    //	(see comments above ReleaseHandle for details)
    public class SFH_NoCloseHandle : SafeHandle //SafeHandle subclass
    {
        ///////////////////////////////////////////////////////////
        private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

        //0 or -1 considered invalid
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
        }

        //each SafeHandle subclass will expose a static method for instance creation
        [DllImport("api-ms-win-core-file-l1-2-1", EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern SFH_NoCloseHandle CreateFile(String lpFileName,
                                                DesiredAccess dwDesiredAccess, ShareMode dwShareMode,
                                                IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition,
                                                FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        //default constructor which just calls the base class constructor
        public SFH_NoCloseHandle()
            : base(IntPtr.Zero, true)
        {
        }

        //	this method will not actually call any resource releasing API method
        //	since the out/ref SFH param is not actually initialized to an OS allocated
        //	HANDLE---instead the unmanaged side just initializes/changes it to some integer;
        //	If a resource releasing API method like CloseHandle were called then
        //	it would return false and an unhandled exception would be thrown by the
        //	runtime indicating that the release method failed
        override protected bool ReleaseHandle()
        {
            return true;
        }

    } //end of SFH_NoCloseHandle class

    public class SafeFileHandle : SafeHandle //SafeHandle subclass
    {
        //public fields and properties
        public SafeHandle shfld1;

        public SafeFileHandle shfld2;
        public SafeFileHandle shfld2_prop
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            get { return shfld2; }
            [param: MarshalAs(UnmanagedType.Interface)]
            set { shfld2 = value; }
        }

        ///////////////////////////////////////////////////////////
        private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

        //0 or -1 considered invalid
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("api-ms-win-core-handle-l1-1-0", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);


        //each SafeHandle subclass will expose a static method for instance creation
        [DllImport("api-ms-win-core-file-l1-2-1", EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern SafeFileHandle CreateFile(String lpFileName,
                                                DesiredAccess dwDesiredAccess, ShareMode dwShareMode,
                                                IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition,
                                                FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        //default constructor which just calls the base class constructor
        public SafeFileHandle()
            : base(IntPtr.Zero, true)
        {
        }

        override protected bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

    } //end of SafeFileHandle class

    /// <summary>
    /// The following public enums are for use in creating the 
    /// new file i.e. setting attributes etc. on the file
    /// </summary>

    // This enumeration defines the level of desired access. The
    // enumeration contains a special member for querying the
    // device without accessing it.
    public enum DesiredAccess : uint
    {
        QueryDeviceOnly = 0,
        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
        STANDARD_RIGHTS_REQUIRED = 0x000F0000,
        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
        STANDARD_RIGHTS_ALL = 0x001F0000,
        SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
        ACCESS_SYSTEM_SECURITY = 0x01000000,
        MAXIMUM_ALLOWED = 0x02000000
    }

    // This enumeration defines the type of sharing to support. It
    // includes a special member for no sharing at all.
    public enum ShareMode
    {
        NotShared = 0,
        FILE_SHARE_READ = 0x00000001,
        FILE_SHARE_WRITE = 0x00000002,
        FILE_SHARE_DELETE = 0x00000004
    }

    // This enumeration defines how the call will treat files or
    // other objects that already exist. You must provide one of
    // these values as input.
    public enum CreationDisposition
    {
        CREATE_NEW = 1,
        CREATE_ALWAYS = 2,
        OPEN_EXISTING = 3,
        OPEN_ALWAYS = 4,
        TRUNCATE_EXISTING = 5
    }

    // This enumeration defines additional flags and attributes the
    // call will use when opening an object. This enumeration contains
    // as special value for no flags or attributes.
    public enum FlagsAndAttributes : uint
    {
        None = 0,
        FILE_ATTRIBUTE_READONLY = 0x00000001,
        FILE_ATTRIBUTE_HIDDEN = 0x00000002,
        FILE_ATTRIBUTE_SYSTEM = 0x00000004,
        FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
        FILE_ATTRIBUTE_NORMAL = 0x00000080,
        FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
        FILE_ATTRIBUTE_OFFLINE = 0x00001000,
        FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
        FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
        FILE_FLAG_WRITE_THROUGH = 0x80000000,
        FILE_FLAG_OVERLAPPED = 0x40000000,
        FILE_FLAG_NO_BUFFERING = 0x20000000,
        FILE_FLAG_RANDOM_ACCESS = 0x10000000,
        FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
        FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
        FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
        FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
        FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
        FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
        SECURITY_ANONYMOUS = 0x00000000,
        SECURITY_IDENTIFICATION = 0x00010000,
        SECURITY_IMPERSONATION = 0x00020000,
        SECURITY_DELEGATION = 0x00030000,
        SECURITY_CONTEXT_TRACKING = 0x00040000,
        SECURITY_EFFECTIVE_ONLY = 0x00080000
    }
}
