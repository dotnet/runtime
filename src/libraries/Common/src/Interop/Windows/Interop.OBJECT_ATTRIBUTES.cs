// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    /// <summary>
    /// <a href="https://msdn.microsoft.com/en-us/library/windows/hardware/ff557749.aspx">OBJECT_ATTRIBUTES</a> structure.
    /// The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects or object handles by routines 
    /// that create objects and/or return handles to objects.
    /// </summary>
    internal unsafe struct OBJECT_ATTRIBUTES
    {
        public uint Length;

        /// <summary>
        /// Optional handle to root object directory for the given ObjectName.
        /// Can be a file system directory or object manager directory.
        /// </summary>
        public IntPtr RootDirectory;

        /// <summary>
        /// Name of the object. Must be fully qualified if RootDirectory isn't set.
        /// Otherwise is relative to RootDirectory.
        /// </summary>
        public UNICODE_STRING* ObjectName;

        public ObjectAttributes Attributes;

        /// <summary>
        /// If null, object will receive default security settings.
        /// </summary>
        public void* SecurityDescriptor;

        /// <summary>
        /// Optional quality of service to be applied to the object. Used to indicate
        /// security impersonation level and context tracking mode (dynamic or static).
        /// </summary>
        public void* SecurityQualityOfService;

        /// <summary>
        /// Equivalent of InitializeObjectAttributes macro with the exception that you can directly set SQOS.
        /// </summary>
        public unsafe OBJECT_ATTRIBUTES(UNICODE_STRING* objectName, ObjectAttributes attributes, IntPtr rootDirectory)
        {
            Length = (uint)sizeof(OBJECT_ATTRIBUTES);
            RootDirectory = rootDirectory;
            ObjectName = objectName;
            Attributes = attributes;
            SecurityDescriptor = null;
            SecurityQualityOfService = null;
        }
    }

    [Flags]
    public enum ObjectAttributes : uint
    {
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff564586.aspx
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff547804.aspx

        /// <summary>
        /// This handle can be inherited by child processes of the current process.
        /// </summary>
        OBJ_INHERIT = 0x00000002,

        /// <summary>
        /// This flag only applies to objects that are named within the object manager.
        /// By default, such objects are deleted when all open handles to them are closed.
        /// If this flag is specified, the object is not deleted when all open handles are closed.
        /// </summary>
        OBJ_PERMANENT = 0x00000010,

        /// <summary>
        /// Only a single handle can be open for this object.
        /// </summary>
        OBJ_EXCLUSIVE = 0x00000020,

        /// <summary>
        /// Lookups for this object should be case insensitive.
        /// </summary>
        OBJ_CASE_INSENSITIVE = 0x00000040,

        /// <summary>
        /// Create on existing object should open, not fail with STATUS_OBJECT_NAME_COLLISION.
        /// </summary>
        OBJ_OPENIF = 0x00000080,

        /// <summary>
        /// Open the symbolic link, not its target.
        /// </summary>
        OBJ_OPENLINK = 0x00000100,

        // Only accessible from kernel mode
        // OBJ_KERNEL_HANDLE

        // Access checks enforced, even in kernel mode
        // OBJ_FORCE_ACCESS_CHECK
        // OBJ_VALID_ATTRIBUTES = 0x000001F2
    }
}
