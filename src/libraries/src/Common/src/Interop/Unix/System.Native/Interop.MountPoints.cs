using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        private const int MountPointFormatBufferSizeInBytes = 32;

        /// <summary>
        /// Internal FileSystem names and magic numbers taken from man(2) statfs
        /// </summary>
        /// <remarks>
        /// These value names MUST be kept in sync with those in GetDriveType below
        /// </remarks>
        internal enum UnixFileSystemTypes : long
        {
            adfs = 0xadf5,
            affs = 0xADFF,
            befs = 0x42465331,
            bfs = 0x1BADFACE,
            cifs = 0xFF534D42,
            coda = 0x73757245,
            coherent = 0x012FF7B7,
            cramfs = 0x28cd3d45,
            devfs = 0x1373,
            efs = 0x00414A53,
            ext = 0x137D,
            ext2_old = 0xEF51,
            ext2 = 0xEF53,
            ext3 = 0xEF53,
            ext4 = 0xEF53,
            hfs = 0x4244,
            hpfs = 0xF995E849,
            hugetlbfs = 0x958458f6,
            isofs = 0x9660,
            jffs2 = 0x72b6,
            jfs = 0x3153464a,
            minix_old = 0x137F, /* orig. minix */
            minix = 0x138F, /* 30 char minix */
            minix2 = 0x2468, /* minix V2 */
            minix2v2 = 0x2478, /* minix V2, 30 char names */
            msdos = 0x4d44,
            ncpfs = 0x564c,
            nfs = 0x6969,
            ntfs = 0x5346544e,
            openprom = 0x9fa1,
            proc = 0x9fa0,
            qnx4 = 0x002f,
            reiserfs = 0x52654973,
            romfs = 0x7275,
            smb = 0x517B,
            sysv2 = 0x012FF7B6,
            sysv4 = 0x012FF7B5,
            tmpfs = 0x01021994,
            udf = 0x15013346,
            ufs = 0x00011954,
            usbdevice = 0x9fa2,
            vxfs = 0xa501FCF5,
            xenix = 0x012FF7B4,
            xfs = 0x58465342,
            xiafs = 0x012FD16D,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MountPointInformation
        {
            internal ulong AvailableFreeSpace;
            internal ulong TotalFreeSpace;
            internal ulong TotalSize;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void MountPointFound(byte* name);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        private static extern int GetAllMountPoints(MountPointFound mpf);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int GetSpaceInfoForMountPoint([MarshalAs(UnmanagedType.LPStr)]string name, out MountPointInformation mpi);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        private unsafe static extern int GetFormatInfoForMountPoint(
            [MarshalAs(UnmanagedType.LPStr)]string name,
            byte* formatNameBuffer,
            int bufferLength,
            long* formatType);

        internal static int GetFormatInfoForMountPoint(string name, out string format)
        {
            DriveType temp;
            return GetFormatInfoForMountPoint(name, out format, out temp);
        }

        internal static int GetFormatInfoForMountPoint(string name, out DriveType type)
        {
            string temp;
            return GetFormatInfoForMountPoint(name, out temp, out type);
        }

        private static int GetFormatInfoForMountPoint(string name, out string format, out DriveType type)
        {
            unsafe
            {
                byte* formatBuffer = stackalloc byte[MountPointFormatBufferSizeInBytes];    // format names should be small
                long numericFormat;
                int result = GetFormatInfoForMountPoint(name, formatBuffer, MountPointFormatBufferSizeInBytes, &numericFormat);
                if (result == 0)
                {
                    // Check if we have a numeric answer or string
                    format = numericFormat != -1 ?
                        Enum.GetName(typeof(UnixFileSystemTypes), numericFormat) :
                        Marshal.PtrToStringAnsi((IntPtr)formatBuffer);
                    type = GetDriveType(format);
                }
                else
                {
                    format = string.Empty;
                    type = DriveType.Unknown;
                }

                return result;
            }
        }

        internal static List<string> GetAllMountPoints()
        {
            List<string> lst = new List<string>();
            unsafe
            {
                int result = GetAllMountPoints((byte* name) =>
                {
                    lst.Add(Marshal.PtrToStringAnsi((IntPtr)name));
                });
            }

            return lst;
        }

        /// <summary>Categorizes a file system name into a drive type.</summary>
        /// <param name="fileSystemName">The name to categorize.</param>
        /// <returns>The recognized drive type.</returns>
        private static DriveType GetDriveType(string fileSystemName)
        {
            // This list is based primarily on "man fs", "man mount", "mntent.h", "/proc/filesystems",
            // and "wiki.debian.org/FileSystem". It can be extended over time as we 
            // find additional file systems that should be recognized as a particular drive type.
            switch (fileSystemName)
            {
                case "iso":
                case "isofs":
                case "iso9660":
                case "fuseiso":
                case "fuseiso9660":
                case "umview-mod-umfuseiso9660":
                    return DriveType.CDRom;

                case "adfs":
                case "affs":
                case "befs":
                case "bfs":
                case "btrfs":
                case "ecryptfs":
                case "efs":
                case "ext":
                case "ext2":
                case "ext2_old":
                case "ext3":
                case "ext4":
                case "ext4dev":
                case "fat":
                case "fuseblk":
                case "fuseext2":
                case "fusefat":
                case "hfs":
                case "hfsplus":
                case "hpfs":
                case "jbd":
                case "jbd2":
                case "jfs":
                case "jffs":
                case "jffs2":
                case "minix":
                case "minix_old":
                case "minix2":
                case "minix2v2":
                case "msdos":
                case "ocfs2":
                case "omfs":
                case "openprom":
                case "ntfs":
                case "qnx4":
                case "reiserfs":
                case "squashfs":
                case "swap":
                case "sysv":
                case "ubifs":
                case "udf":
                case "ufs":
                case "umsdos":
                case "umview-mod-umfuseext2":
                case "xenix":
                case "xfs":
                case "xiafs":
                case "xmount":
                case "zfs-fuse":
                    return DriveType.Fixed;

                case "9p":
                case "autofs":
                case "autofs4":
                case "beaglefs":
                case "cifs":
                case "coda":
                case "coherent":
                case "curlftpfs":
                case "davfs2":
                case "dlm":
                case "flickrfs":
                case "fusedav":
                case "fusesmb":
                case "gfs2":
                case "glusterfs-client":
                case "gmailfs":
                case "kafs":
                case "ltspfs":
                case "ncpfs":
                case "nfs":
                case "nfs4":
                case "obexfs":
                case "s3ql":
                case "smb":
                case "smbfs":
                case "sshfs":
                case "sysfs":
                case "sysv2":
                case "sysv4":
                case "vxfs":
                case "wikipediafs":
                    return DriveType.Network;

                case "anon_inodefs":
                case "aptfs":
                case "avfs":
                case "bdev":
                case "binfmt_misc":
                case "cgroup":
                case "configfs":
                case "cramfs":
                case "cryptkeeper":
                case "cpuset":
                case "debugfs":
                case "devfs":
                case "devpts":
                case "devtmpfs":
                case "encfs":
                case "fuse":
                case "fuse.gvfsd-fuse":
                case "fusectl":
                case "hugetlbfs":
                case "libpam-encfs":
                case "ibpam-mount":
                case "mtpfs":
                case "mythtvfs":
                case "mqueue":
                case "pipefs":
                case "plptools":
                case "proc":
                case "pstore":
                case "pytagsfs":
                case "ramfs":
                case "rofs":
                case "romfs":
                case "rootfs":
                case "securityfs":
                case "sockfs":
                case "tmpfs":
                    return DriveType.Ram;

                case "gphotofs":
                case "usbfs":
                case "usbdevice":
                case "vfat":
                    return DriveType.Removable;

                case "aufs": // marking all unions as unknown
                case "funionfs":
                case "unionfs-fuse":
                case "mhddfs":
                default:
                    return DriveType.Unknown;
            }
        }
    }
}
