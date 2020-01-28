/**
 * \file
 */

#ifndef __LINUX_MAGIC_H
#define __LINUX_MAGIC_H

#if __linux__
#if HAVE_LINUX_MAGIC_H
#include <linux/magic.h>
#endif

#ifndef ADFS_SUPER_MAGIC
#define ADFS_SUPER_MAGIC        0xadf5
#endif

#ifndef AFFS_SUPER_MAGIC
#define AFFS_SUPER_MAGIC        0xadff
#endif

#ifndef AFS_SUPER_MAGIC
#define AFS_SUPER_MAGIC         0x5346414F
#endif

#ifndef AUTOFS_SUPER_MAGIC
#define AUTOFS_SUPER_MAGIC      0x0187
#endif

#ifndef AUTOFS_SBI_MAGIC
#define AUTOFS_SBI_MAGIC        0x6d4a556d
#endif

#ifndef CODA_SUPER_MAGIC
#define CODA_SUPER_MAGIC        0x73757245
#endif

#ifndef CRAMFS_MAGIC
#define CRAMFS_MAGIC            0x28cd3d45
#endif

#ifndef CRAMFS_MAGIC_WEND
#define CRAMFS_MAGIC_WEND       0x453dcd28
#endif

#ifndef DEBUGFS_MAGIC
#define DEBUGFS_MAGIC          0x64626720
#endif

#ifndef SYSFS_MAGIC
#define SYSFS_MAGIC             0x62656572
#endif

#ifndef SECURITYFS_MAGIC
#define SECURITYFS_MAGIC        0x73636673
#endif

#ifndef SELINUX_MAGIC
#define SELINUX_MAGIC           0xf97cff8c
#endif

#ifndef RAMFS_MAGIC
#define RAMFS_MAGIC             0x858458f6
#endif

#ifndef TMPFS_MAGIC
#define TMPFS_MAGIC             0x01021994
#endif

#ifndef HUGETLBFS_MAGIC
#define HUGETLBFS_MAGIC         0x958458f6
#endif

#ifndef SQUASHFS_MAGIC
#define SQUASHFS_MAGIC          0x73717368
#endif

#ifndef EFS_SUPER_MAGIC
#define EFS_SUPER_MAGIC         0x414A53
#endif

#ifndef EXT2_SUPER_MAGIC
#define EXT2_SUPER_MAGIC        0xEF53
#endif

#ifndef EXT3_SUPER_MAGIC
#define EXT3_SUPER_MAGIC        0xEF53
#endif

#ifndef XENFS_SUPER_MAGIC
#define XENFS_SUPER_MAGIC       0xabba1974
#endif

#ifndef EXT4_SUPER_MAGIC
#define EXT4_SUPER_MAGIC        0xEF53
#endif

#ifndef BTRFS_SUPER_MAGIC
#define BTRFS_SUPER_MAGIC       0x9123683E
#endif

#ifndef HPFS_SUPER_MAGIC
#define HPFS_SUPER_MAGIC        0xf995e849
#endif

#ifndef ISOFS_SUPER_MAGIC
#define ISOFS_SUPER_MAGIC       0x9660
#endif

#ifndef JFFS2_SUPER_MAGIC
#define JFFS2_SUPER_MAGIC       0x72b6
#endif

#ifndef JFS_SUPER_MAGIC
#define JFS_SUPER_MAGIC         0x3153464a
#endif

#ifndef ANON_INODE_FS_MAGIC
#define ANON_INODE_FS_MAGIC     0x09041934
#endif

#ifndef MINIX_SUPER_MAGIC
#define MINIX_SUPER_MAGIC       0x137F
#endif

#ifndef MINIX_SUPER_MAGIC2
#define MINIX_SUPER_MAGIC2      0x138F
#endif

#ifndef MINIX2_SUPER_MAGIC
#define MINIX2_SUPER_MAGIC      0x2468
#endif

#ifndef MINIX2_SUPER_MAGIC2
#define MINIX2_SUPER_MAGIC2     0x2478
#endif

#ifndef MINIX3_SUPER_MAGIC
#define MINIX3_SUPER_MAGIC      0x4d5a
#endif

#ifndef MSDOS_SUPER_MAGIC
#define MSDOS_SUPER_MAGIC       0x4d44
#endif

#ifndef NCP_SUPER_MAGIC
#define NCP_SUPER_MAGIC         0x564c
#endif

#ifndef NFS_SUPER_MAGIC
#define NFS_SUPER_MAGIC         0x6969
#endif

#ifndef OPENPROM_SUPER_MAGIC
#define OPENPROM_SUPER_MAGIC    0x9fa1
#endif

#ifndef PROC_SUPER_MAGIC
#define PROC_SUPER_MAGIC        0x9fa0
#endif

#ifndef QNX4_SUPER_MAGIC
#define QNX4_SUPER_MAGIC        0x002f
#endif

#ifndef REISERFS_SUPER_MAGIC
#define REISERFS_SUPER_MAGIC    0x52654973
#endif

#ifndef SMB_SUPER_MAGIC
#define SMB_SUPER_MAGIC         0x517B
#endif

#ifndef USBDEVICE_SUPER_MAGIC
#define USBDEVICE_SUPER_MAGIC   0x9fa2
#endif

#ifndef CGROUP_SUPER_MAGIC
#define CGROUP_SUPER_MAGIC      0x27e0eb
#endif

#ifndef FUTEXFS_SUPER_MAGIC
#define FUTEXFS_SUPER_MAGIC     0xBAD1DEA
#endif

#ifndef DEVPTS_SUPER_MAGIC
#define DEVPTS_SUPER_MAGIC      0x1cd1
#endif

#ifndef CIFS_MAGIC_NUMBER
#define CIFS_MAGIC_NUMBER       0xFF534D42
#endif

#ifndef BEFS_SUPER_MAGIC1
#define BEFS_SUPER_MAGIC1       0x42465331
#endif

#ifndef BEFS_SUPER_MAGIC2
#define BEFS_SUPER_MAGIC2       0xdd121031
#endif

#ifndef BEFS_SUPER_MAGIC3
#define BEFS_SUPER_MAGIC3       0x15b6830e
#endif

#ifndef BFS_MAGIC
#define BFS_MAGIC               0x1BADFACE
#endif

#ifndef NTFS_SB_MAGIC
#define NTFS_SB_MAGIC           0x5346544e
#endif

enum {
        MONO_SYSV_FSTYPE_NONE = 0,
        MONO_SYSV_FSTYPE_XENIX,
        MONO_SYSV_FSTYPE_SYSV4,
        MONO_SYSV_FSTYPE_SYSV2,
        MONO_SYSV_FSTYPE_COH,
};

#ifndef SYSV_MAGIC_BASE
#define SYSV_MAGIC_BASE         0x012FF7B3
#endif

#ifndef XENIX_SUPER_MAGIC
#define XENIX_SUPER_MAGIC       (SYSV_MAGIC_BASE+MONO_SYSV_FSTYPE_XENIX)
#endif

#ifndef SYSV4_SUPER_MAGIC
#define SYSV4_SUPER_MAGIC       (SYSV_MAGIC_BASE+MONO_SYSV_FSTYPE_SYSV4)
#endif

#ifndef SYSV2_SUPER_MAGIC
#define SYSV2_SUPER_MAGIC       (SYSV_MAGIC_BASE+MONO_SYSV_FSTYPE_SYSV2)
#endif

#ifndef COH_SUPER_MAGIC
#define COH_SUPER_MAGIC         (SYSV_MAGIC_BASE+MONO_SYSV_FSTYPE_COH)
#endif

#ifndef UFS_MAGIC
#define UFS_MAGIC               0x00011954
#endif

#ifndef UFS_MAGIC_BW
#define UFS_MAGIC_BW            0x0f242697
#endif

#ifndef UFS2_MAGIC
#define UFS2_MAGIC              0x19540119
#endif

#ifndef UFS_CIGAM
#define UFS_CIGAM               0x54190100
#endif

#ifndef UDF_SUPER_MAGIC
#define UDF_SUPER_MAGIC         0x15013346
#endif

#ifndef XFS_SB_MAGIC
#define XFS_SB_MAGIC            0x58465342
#endif

#ifndef FUSE_SUPER_MAGIC
#define FUSE_SUPER_MAGIC        0x65735546
#endif

#ifndef V9FS_MAGIC
#define V9FS_MAGIC              0x01021997
#endif

#ifndef CEPH_SUPER_MAGIC
#define CEPH_SUPER_MAGIC        0x00c36400
#endif

#ifndef CONFIGFS_MAGIC
#define CONFIGFS_MAGIC          0x62656570
#endif

#ifndef ECRYPTFS_SUPER_MAGIC
#define ECRYPTFS_SUPER_MAGIC    0xf15f
#endif

#ifndef EXOFS_SUPER_MAGIC
#define EXOFS_SUPER_MAGIC       0x5df5
#endif

#ifndef VXFS_SUPER_MAGIC
#define VXFS_SUPER_MAGIC        0xa501fcf5
#endif

#ifndef VXFS_OLT_MAGIC
#define VXFS_OLT_MAGIC          0xa504fcf5
#endif

#ifndef GFS2_MAGIC
#define GFS2_MAGIC              0x01161970
#endif

#ifndef HFS_SUPER_MAGIC
#define HFS_SUPER_MAGIC         0x4244
#endif

#ifndef HFSPLUS_SUPER_MAGIC
#define HFSPLUS_SUPER_MAGIC     0x482b
#endif

#ifndef LOGFS_MAGIC_U32
#define LOGFS_MAGIC_U32         0xc97e8168
#endif

#ifndef OCFS2_SUPER_MAGIC
#define OCFS2_SUPER_MAGIC       0x7461636f
#endif

#ifndef OMFS_MAGIC
#define OMFS_MAGIC              0xc2993d87
#endif

#ifndef UBIFS_SUPER_MAGIC
#define UBIFS_SUPER_MAGIC       0x24051905
#endif

#ifndef ROMFS_MAGIC
#define ROMFS_MAGIC             0x7275
#endif

#endif
#endif
