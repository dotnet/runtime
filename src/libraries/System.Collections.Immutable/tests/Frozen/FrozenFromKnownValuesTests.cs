// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Collections.Frozen.Tests
{
    public class FrozenFromKnownValuesTests
    {
        public static IEnumerable<object[]> Int32StringData() =>
            from keys in new int[][]
            {
                new int[] { 0 },
                new int[] { 0, 1 },
                new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                new int[] { 0, 2, 4, 6, 8, 10 },
                new int[] { -1, 0, 2, 4, 6, 8, 10 },
                Enumerable.Range(42, 100).ToArray(),
            }
            select new object[] { keys.ToDictionary(i => i, i => i.ToString()) };

        public static IEnumerable<object[]> StringStringData() =>
            from comparer in new[] { StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase }
            from keys in new string[][]
            {
                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/Common/src/Interop/Unix/System.Native/Interop.MountPoints.FormatInfo.cs#L84-L327
                new[]
                {
                    "cddafs", "cd9660", "iso", "isofs", "iso9660", "fuseiso", "fuseiso9660", "udf", "umview-mod-umfuseiso9660", "aafs", "adfs", "affs", "anoninode", "anon-inode FS", "apfs", "balloon-kvm-fs", "bdevfs", "befs", "bfs", "bootfs", "bpf_fs",
                    "btrfs", "btrfs_test", "coh", "daxfs", "drvfs", "efivarfs", "efs", "exfat", "exofs", "ext", "ext2", "ext2_old", "ext3", "ext2/ext3", "ext4", "ext4dev", "f2fs", "fat", "fuseext2", "fusefat", "hfs", "hfs+", "hfsplus", "hfsx", "hostfs",
                    "hpfs", "inodefs", "inotifyfs", "jbd", "jbd2", "jffs", "jffs2", "jfs", "lofs", "logfs", "lxfs", "minix (30 char.)", "minix v2 (30 char.)", "minix v2", "minix", "minix_old", "minix2", "minix2v2", "minix2 v2", "minix3", "mlfs", "msdos",
                    "nilfs", "nsfs", "ntfs", "ntfs-3g", "ocfs2", "omfs", "overlay", "overlayfs", "pstorefs", "qnx4", "qnx6", "reiserfs", "rpc_pipefs", "sffs", "smackfs", "squashfs", "swap", "sysv", "sysv2", "sysv4", "tracefs", "ubifs", "ufs", "ufscigam",
                    "ufs2", "umsdos", "umview-mod-umfuseext2", "v9fs", "vagrant", "vboxfs", "vxfs", "vxfs_olt", "vzfs", "wslfs", "xenix", "xfs", "xia", "xiafs", "xmount", "zfs", "zfs-fuse", "zsmallocfs", "9p", "acfs", "afp", "afpfs", "afs", "aufs", "autofs",
                    "autofs4", "beaglefs", "ceph", "cifs", "coda", "coherent", "curlftpfs", "davfs2", "dlm", "eCryptfs", "fhgfs", "flickrfs", "ftp", "fuse", "fuseblk", "fusedav", "fusesmb", "gfsgfs2", "gfs/gfs2", "gfs2", "glusterfs-client",
                    "gmailfs", "gpfs", "ibrix", "k-afs", "kafs", "kbfuse", "ltspfs", "lustre", "ncp", "ncpfs", "nfs", "nfs4", "nfsd", "novell", "obexfs", "panfs", "prl_fs", "s3ql", "samba", "smb", "smb2", "smbfs", "snfs", "sshfs", "vmhgfs", "webdav", "wikipediafs",
                    "xenfs", "anon_inode", "anon_inodefs", "aptfs", "avfs", "bdev", "binfmt_misc", "cgroup", "cgroupfs", "cgroup2fs", "configfs", "cpuset", "cramfs", "cramfs-wend", "cryptkeeper", "ctfs", "debugfs", "dev", "devfs", "devpts", "devtmpfs", "encfs", "fd",
                    "fdesc", "fuse.gvfsd-fuse", "fusectl", "futexfs", "hugetlbfs", "libpam-encfs", "ibpam-mount", "mntfs", "mqueue", "mtpfs", "mythtvfs", "objfs", "openprom", "openpromfs", "pipefs", "plptools", "proc", "pstore", "pytagsfs", "ramfs", "rofs", "romfs",
                    "rootfs", "securityfs", "selinux", "selinuxfs", "sharefs", "sockfs", "sysfs", "tmpfs", "udev", "usbdev", "usbdevfs", "gphotofs", "sdcardfs", "usbfs", "usbdevice", "vfat",
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Formats.Asn1/src/System/Formats/Asn1/WellKnownOids.cs#L317-L419
                new[]
                {
                    "1.2.840.10040.4.1", "1.2.840.10040.4.3", "1.2.840.10045.2.1", "1.2.840.10045.1.1", "1.2.840.10045.1.2", "1.2.840.10045.3.1.7", "1.2.840.10045.4.1", "1.2.840.10045.4.3.2", "1.2.840.10045.4.3.3", "1.2.840.10045.4.3.4",
                    "1.2.840.113549.1.1.1", "1.2.840.113549.1.1.5", "1.2.840.113549.1.1.7", "1.2.840.113549.1.1.8", "1.2.840.113549.1.1.9", "1.2.840.113549.1.1.10", "1.2.840.113549.1.1.11", "1.2.840.113549.1.1.12", "1.2.840.113549.1.1.13",
                    "1.2.840.113549.1.5.3", "1.2.840.113549.1.5.10", "1.2.840.113549.1.5.11", "1.2.840.113549.1.5.12", "1.2.840.113549.1.5.13", "1.2.840.113549.1.7.1", "1.2.840.113549.1.7.2", "1.2.840.113549.1.7.3", "1.2.840.113549.1.7.6",
                    "1.2.840.113549.1.9.1", "1.2.840.113549.1.9.3", "1.2.840.113549.1.9.4", "1.2.840.113549.1.9.5", "1.2.840.113549.1.9.6", "1.2.840.113549.1.9.7", "1.2.840.113549.1.9.14", "1.2.840.113549.1.9.15", "1.2.840.113549.1.9.16.1.4",
                    "1.2.840.113549.1.9.16.2.12", "1.2.840.113549.1.9.16.2.14", "1.2.840.113549.1.9.16.2.47", "1.2.840.113549.1.9.20", "1.2.840.113549.1.9.21", "1.2.840.113549.1.9.22.1", "1.2.840.113549.1.12.1.3", "1.2.840.113549.1.12.1.5",
                    "1.2.840.113549.1.12.1.6", "1.2.840.113549.1.12.10.1.1", "1.2.840.113549.1.12.10.1.2", "1.2.840.113549.1.12.10.1.3", "1.2.840.113549.1.12.10.1.5", "1.2.840.113549.1.12.10.1.6", "1.2.840.113549.2.5", "1.2.840.113549.2.7",
                    "1.2.840.113549.2.9", "1.2.840.113549.2.10", "1.2.840.113549.2.11", "1.2.840.113549.3.2", "1.2.840.113549.3.7", "1.3.6.1.4.1.311.17.1", "1.3.6.1.4.1.311.17.3.20", "1.3.6.1.4.1.311.20.2.3", "1.3.6.1.4.1.311.88.2.1",
                    "1.3.6.1.4.1.311.88.2.2", "1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.3", "1.3.6.1.5.5.7.3.4", "1.3.6.1.5.5.7.3.8", "1.3.6.1.5.5.7.3.9", "1.3.6.1.5.5.7.6.2", "1.3.6.1.5.5.7.48.1", "1.3.6.1.5.5.7.48.1.2",
                    "1.3.6.1.5.5.7.48.2", "1.3.14.3.2.26", "1.3.14.3.2.7", "1.3.132.0.34", "1.3.132.0.35", "2.5.4.3", "2.5.4.5", "2.5.4.6", "2.5.4.7", "2.5.4.8", "2.5.4.10", "2.5.4.11", "2.5.4.97", "2.5.29.14", "2.5.29.15", "2.5.29.17", "2.5.29.19",
                    "2.5.29.20", "2.5.29.35", "2.16.840.1.101.3.4.1.2", "2.16.840.1.101.3.4.1.22", "2.16.840.1.101.3.4.1.42", "2.16.840.1.101.3.4.2.1", "2.16.840.1.101.3.4.2.2", "2.16.840.1.101.3.4.2.3", "2.23.140.1.2.1", "2.23.140.1.2.2",
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/Common/src/Interop/Linux/procfs/Interop.ProcFsStat.TryReadStatusFile.cs#L66-L102
                new[] { "Pid", "VmHWM", "VmRSS", "VmData", "VmSwap", "VmSize", "VmPeak", "VmStk", },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Private.Xml/src/System/Xml/Xsl/XsltOld/XsltCompileContext.cs#L451-L485
                new[]
                {
                    "last", "position", "name", "namespace-uri", "local-name", "count", "id", "string", "concat", "starts-with", "contains", "substring-before", "substring-after", "substring", "string-length",
                    "normalize-space", "translate", "boolean", "not", "true", "false", "lang", "number", "sum", "floor", "ceiling", "round",
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.ServiceModel.Syndication/src/System/ServiceModel/Syndication/DateTimeHelper.cs#L146-L212
                new[] { "UT", "Z", "GMT", "A", "B", "C", "D", "EDT", "E", "EST", "CDT", "F", "CST", "MDT", "G", "MST", "PDT", "H", "PST", "I", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y" },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCompiler.cs#L5810-L5879
                new[]
                {
                    "\0\0\0\u03ff\ufffe\u07ff\ufffe\u07ff",
                    "\0\0\0\u03FF\0\0\0\0",
                    "\0\0\0\0\ufffe\u07FF\ufffe\u07ff",
                    "\0\0\0\0\0\0\ufffe\u07ff",
                    "\0\0\0\0\ufffe\u07FF\0\0",
                    "\0\0\0\u03FF\u007E\0\u007E\0",
                    "\0\0\0\u03FF\0\0\u007E\0",
                    "\0\0\0\u03FF\u007E\0\0\0",
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/mono/wasm/debugger/BrowserDebugProxy/MonoProxy.cs#L274-L572
                new[]
                {
                    "Target.attachToTarget", "Debugger.enable", "Debugger.getScriptSource", "Runtime.compileScript", "Debugger.getPossibleBreakpoints",
                    "Debugger.setBreakpoint", "Debugger.setBreakpointByUrl", "Debugger.removeBreakpoint", "Debugger.resume", "Debugger.stepInto",
                    "Debugger.setVariableValue", "Debugger.stepOut", "Debugger.stepOver", "Runtime.evaluate", "Debugger.evaluateOnCallFrame",
                    "Runtime.getProperties", "Runtime.releaseObject", "Debugger.setPauseOnExceptions", "DotnetDebugger.setDebuggerProperty",
                    "DotnetDebugger.setNextIP", "DotnetDebugger.applyUpdates", "DotnetDebugger.addSymbolServerUrl", "DotnetDebugger.getMethodLocation",
                    "Runtime.callFunctionOn"
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/tools/illink/src/linker/Linker.Steps/DiscoverCustomOperatorsHandler.cs#L156-L221
                new[]
                {
                    "UnaryPlus", "UnaryNegation", "LogicalNot", "OnesComplement", "Increment", "Decrement", "True", "False", "Addition", "Subtraction", "Multiply", "Division", "Modulus",
                    "BitwiseAnd", "BitwiseOr", "ExclusiveOr", "LeftShift", "RightShift", "Equality", "Inequality", "LessThan", "GreaterThan", "LessThanOrEqual", "GreaterThanOrEqual",
                    "Implicit", "Explicit",
                },

                // from https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/coreclr/tools/Common/TypeSystem/IL/Stubs/UnsafeIntrinsics.cs#L21-L94
                new[]
                {
                    "AsPointer", "As", "AsRef", "Add", "AddByteOffset", "Copy", "CopyBlock", "CopyBlockUnaligned", "InitBlock", "InitBlockUnaligned", "Read", "Write",
                    "ReadUnaligned", "WriteUnaligned", "AreSame", "IsAddressGreaterThan", "IsAddressLessThan", "ByteOffset", "NullRef",  "IsNullRef", "SkipInit",
                    "Subtract", "SubtractByteOffset", "Unbox",
                },

                // from https://raw.githubusercontent.com/dotnet/roslyn/0456b4adc6939e366e7c509318b3ac6a85cda496/src/Compilers/CSharp/Test/Emit2/CodeGen/CodeGenLengthBasedSwitchTests.cs
                new[] { "", "a", "b", "c", "no", "yes", "four", "alice", "blurb", "hello", "lamps", "lambs", "lower", "names", "slurp", "towed", "words" },
                new[] { "", "a", "b", "c", "no", "yes", "four", "alice", "blurb", "hello", "lamps", "lambs", "lower", "names", "slurp", "towed", "words", "\u03BB" }, // plus a non-ASCII char
                new[] { "", "a", "b", "c", "no", "yes", "four", "alice", "blurb", "hello" },
                new[] { "abcdefgh", "abcdefg", "abcdef", "abcde", "abcd", "abc", "ab", "a" },
                Enumerable.Range(0, 100).Select(i => i.ToString("D2")).ToArray(),
                new[] { "00100000", "00100001", "00010000", "00010001", "00001000", "00001001", "00000100", "00000101" },

                // from https://github.com/dotnet/runtime/blob/2b8514b02f6d0e87f4645aca0be38f16864004a7/src/libraries/System.Net.Http/src/System/Net/Http/Headers/KnownHeaders.cs#L14-L108
                new[]
                {
                    ":status", "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Accept-Patch", "Accept-Ranges", "Access-Control-Allow-Credentials", "Access-Control-Allow-Headers",
                    "Access-Control-Allow-Methods", "Access-Control-Allow-Origin", "Access-Control-Expose-Headers", "Access-Control-Max-Age", "Age", "Allow", "Alt-Svc", "Alt-Used", "Authorization",
                    "Cache-Control", "Connection", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Security-Policy",
                    "Content-Type", "Cookie", "Cookie2", "Date", "ETag", "Expect", "Expect-CT", "Expires", "From", "grpc-encoding", "grpc-message", "grpc-status", "Host", "If-Match", "If-Modified-Since",
                    "If-None-Match", "If-Range", "If-Unmodified-Since", "Keep-Alive", "Last-Modified", "Link", "Location", "Max-Forwards", "Origin", "P3P", "Pragma", "Proxy-Authenticate", "Proxy-Authorization",
                    "Proxy-Connection", "Proxy-Support", "Public-Key-Pins", "Range", "Referer", "Referrer-Policy", "Refresh", "Retry-After", "Sec-WebSocket-Accept", "Sec-WebSocket-Extensions", "Sec-WebSocket-Key",
                    "Sec-WebSocket-Protocol", "Sec-WebSocket-Version", "Server", "Server-Timing", "Set-Cookie", "Set-Cookie2", "Strict-Transport-Security", "TE", "TSV", "Trailer", "Transfer-Encoding", "Upgrade",
                    "Upgrade-Insecure-Requests", "User-Agent", "Vary", "Via", "WWW-Authenticate", "Warning", "X-AspNet-Version", "X-Cache", "X-Content-Duration", "X-Content-Type-Options", "X-Frame-Options",
                    "X-MSEdge-Ref", "X-Powered-By", "X-Request-ID", "X-UA-Compatible", "X-XSS-Protection",
                },

                // exercise left/right justified ordinal comparers
                Enumerable.Range(0, 10).Select(i => $"{i}ABCDEFGH").ToArray(), // left justified single char ascii
                Enumerable.Range(0, 10).Select(i => $"ABCDEFGH{i}").ToArray(), // right justified single char ascii
                Enumerable.Range(0, 100).Select(i => $"{i:D2}ABCDEFGH").ToArray(), // left justified substring ascii
                Enumerable.Range(0, 100).Select(i => $"ABCDEFGH{i:D2}").ToArray(), // right justified substring ascii
                Enumerable.Range(0, 10).Select(i => $"{i}ABCDEFGH\U0001F600").ToArray(), // left justified single char non-ascii
                Enumerable.Range(0, 10).Select(i => $"ABCDEFGH\U0001F600{i}").ToArray(), // right justified single char non-ascii
                Enumerable.Range(0, 100).Select(i => $"{i:D2}ABCDEFGH\U0001F600").ToArray(), // left justified substring non-ascii
                Enumerable.Range(0, 100).Select(i => $"ABCDEFGH\U0001F600{i:D2}").ToArray(), // right justified substring non-ascii
                Enumerable.Range(0, 20).Select(i => i.ToString("D2")).Select(s => (char)(s[0] + 128) + "" + (char)(s[1] + 128)).ToArray(), // left-justified non-ascii
                
                Enumerable.Range(0, 10).Select(i => $"{i}ABCDefgh").ToArray(), // left justified single char ascii, mixed casing
                Enumerable.Range(0, 10).Select(i => $"ABCDefgh{i}").ToArray(), // right justified single char ascii, mixed casing
                Enumerable.Range(0, 100).Select(i => $"{i:D2}ABCDefgh").ToArray(), // left justified substring ascii, mixed casing
                Enumerable.Range(0, 100).Select(i => $"ABCDefgh{i:D2}").ToArray(), // right justified substring ascii, mixed casing
                Enumerable.Range(0, 10).Select(i => $"{i}ABCDefgh\U0001F600").ToArray(), // left justified single char non-ascii, mixed casing
                Enumerable.Range(0, 10).Select(i => $"ABCDefgh\U0001F600{i}").ToArray(), // right justified single char non-ascii, mixed casing
                Enumerable.Range(0, 100).Select(i => $"{i:D2}ABCDefgh\U0001F600").ToArray(), // left justified substring non-ascii, mixed casing
                Enumerable.Range(0, 100).Select(i => $"ABCDefgh\U0001F600{i:D2}").ToArray(), // right justified substring non-ascii, mixed casing
            }
            select new object[] { keys.ToDictionary(i => i, i => i, comparer) };

        [Theory]
        [MemberData(nameof(StringStringData))]
        public void FrozenDictionary_StringString(Dictionary<string, string> source) =>
            FrozenDictionaryWorker(source);

        [Theory]
        [MemberData(nameof(Int32StringData))]
        public void FrozenDictionary_Int32String(Dictionary<int, string> source)
        {
            FrozenDictionaryWorker(source);
            FrozenDictionaryWorker(source.ToDictionary(i => (sbyte)i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => (short)i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => (long)i.Key, i => i.Value));

            FrozenDictionaryWorker(source.ToDictionary(i => (byte)i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => (ushort)i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => (uint)i.Key, i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => (ulong)i.Key, i => i.Value));

            FrozenDictionaryWorker(source.ToDictionary(i => TimeSpan.FromTicks(i.Key), i => i.Value));
            FrozenDictionaryWorker(source.ToDictionary(i => new Guid((ushort)i.Key, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), i => i.Value));
        }

        private static void FrozenDictionaryWorker<TKey, TValue>(Dictionary<TKey, TValue> source)
        {
            FrozenDictionary<TKey, TValue> frozen = source.ToFrozenDictionary(source.Comparer);

            Assert.NotNull(frozen);

            try
            {
                Assert.Equal(source.Count, frozen.Count);
                Assert.Equal(source.Keys.Count, frozen.Keys.Length);
                Assert.Equal(source.Values.Count, frozen.Values.Length);
                Assert.Equal(source.Count, new HashSet<TKey>(frozen.Keys, frozen.Comparer).Count);

                Assert.Same(source.Comparer, frozen.Comparer);

                foreach (KeyValuePair<TKey, TValue> pair in source)
                {
                    Assert.Equal(pair.Value, frozen.GetValueRefOrNullRef(pair.Key));
                    Assert.Equal(pair.Value, frozen[pair.Key]);
                    Assert.True(frozen.TryGetValue(pair.Key, out TValue value));
                    Assert.Equal(pair.Value, value);
                }

                if (typeof(TKey) == typeof(string) && ReferenceEquals(frozen.Comparer, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (KeyValuePair<TKey, TValue> pair in source)
                    {
                        TKey keyUpper = (TKey)(object)((string)(object)pair.Key).ToUpper();
                        bool isValidTest = frozen.Comparer.Equals(pair.Key, keyUpper);
                        if (isValidTest)
                        {
                            Assert.Equal(pair.Value, frozen.GetValueRefOrNullRef(keyUpper));
                            Assert.Equal(pair.Value, frozen[keyUpper]);
                            Assert.True(frozen.TryGetValue(keyUpper, out TValue value));
                            Assert.Equal(pair.Value, value);
                        }
                    }
                }

                foreach (KeyValuePair<TKey, TValue> pair in frozen)
                {
                    Assert.True(source.TryGetValue(pair.Key, out TValue value));
                    Assert.Equal(pair.Value, value);
                }

                foreach (TKey key in source.Keys)
                {
                    Assert.True(frozen.ContainsKey(key));
                }
                foreach (TKey key in frozen.Keys)
                {
                    Assert.True(source.ContainsKey(key));
                }

                Assert.Equal(new HashSet<TValue>(source.Values), new HashSet<TValue>(frozen.Values));

                if (source.Count > 1)
                {
                    KeyValuePair<TKey, TValue> first = source.First();
                    source.Remove(first.Key);

                    frozen = source.ToFrozenDictionary(source.Comparer);

                    Assert.False(frozen.TryGetValue(first.Key, out TValue value));
                    Assert.Equal(default, value);
                    Assert.True(Unsafe.IsNullRef(ref Unsafe.AsRef(in frozen.GetValueRefOrNullRef(first.Key))));
                    Assert.Throws<KeyNotFoundException>(() => frozen[first.Key]);
                }
            }
            catch (Exception e)
            {
                throw new XunitException(e.Message + Environment.NewLine + frozen.GetType(), e);
            }
        }

        [Theory]
        [MemberData(nameof(StringStringData))]
        public void FrozenSet_StringString(Dictionary<string, string> source) =>
            FrozenSetWorker(source);

        [Theory]
        [MemberData(nameof(Int32StringData))]
        public void FrozenSet_Int32String(Dictionary<int, string> source)
        {
            FrozenSetWorker(source);
            FrozenSetWorker(source.ToDictionary(i => (sbyte)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (byte)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (short)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (ushort)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (int)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (uint)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (long)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => (ulong)i.Key, i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => TimeSpan.FromTicks(i.Key), i => i.Value));
            FrozenSetWorker(source.ToDictionary(i => new Guid((ushort)i.Key, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), i => i.Value));
        }

        private void FrozenSetWorker<TKey, TValue>(Dictionary<TKey, TValue> source)
        {
            FrozenSet<TKey> frozen = source.Select(p => p.Key).ToFrozenSet(source.Comparer);

            Assert.NotNull(frozen);

            try
            {
                Assert.Equal(source.Count, frozen.Count);
                Assert.Equal(source.Keys.Count, frozen.Items.Length);
                Assert.Equal(source.Count, new HashSet<TKey>(frozen.Items, frozen.Comparer).Count);

                Assert.Same(source.Comparer, frozen.Comparer);

                foreach (KeyValuePair<TKey, TValue> pair in source)
                {
                    Assert.True(frozen.Contains(pair.Key));
                    Assert.True(frozen.TryGetValue(pair.Key, out TKey actualKey));
                    Assert.Equal(pair.Key, actualKey);
                }

                if (typeof(TKey) == typeof(string) && ReferenceEquals(frozen.Comparer, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (KeyValuePair<TKey, TValue> pair in source)
                    {
                        TKey keyUpper = (TKey)(object)((string)(object)pair.Key).ToUpper();
                        bool isValidTest = frozen.Comparer.Equals(pair.Key, keyUpper);
                        if (isValidTest)
                        {
                            Assert.True(frozen.Contains(keyUpper));
                            Assert.True(frozen.TryGetValue(keyUpper, out TKey actualKey));
                            Assert.Equal(pair.Key, actualKey);
                        }
                    }
                }

                foreach (TKey key in frozen)
                {
                    Assert.True(source.TryGetValue(key, out _));
                }

                foreach (TKey key in source.Keys)
                {
                    Assert.True(frozen.Contains(key));
                }
                foreach (TKey item in frozen.Items)
                {
                    Assert.True(source.ContainsKey(item));
                }

                var keysSet = new HashSet<TKey>(source.Keys, source.Comparer);

                Assert.True(frozen.IsSubsetOf(keysSet));
                Assert.True(frozen.IsSupersetOf(keysSet));
                Assert.False(frozen.IsProperSubsetOf(keysSet));
                Assert.False(frozen.IsProperSupersetOf(keysSet));
                Assert.True(frozen.SetEquals(keysSet));
                Assert.True(frozen.Overlaps(keysSet));

                Assert.True(frozen.IsSubsetOf(source.Keys));
                Assert.True(frozen.IsSupersetOf(source.Keys));
                Assert.False(frozen.IsProperSubsetOf(source.Keys));
                Assert.False(frozen.IsProperSupersetOf(source.Keys));
                Assert.True(frozen.SetEquals(source.Keys));
                Assert.True(frozen.Overlaps(source.Keys));

                if (source.Count > 1)
                {
                    TKey[] originalKeys = source.Keys.ToArray();

                    KeyValuePair<TKey, TValue> first = source.First();
                    source.Remove(first.Key);

                    frozen = source.Select(p => p.Key).ToFrozenSet(source.Comparer);

                    Assert.False(frozen.Contains(first.Key));

                    Assert.True(frozen.IsProperSubsetOf(new HashSet<TKey>(originalKeys, source.Comparer)));
                }
            }
            catch (Exception e)
            {
                throw new XunitException(e.Message + Environment.NewLine + frozen.GetType(), e);
            }
        }
    }
}
