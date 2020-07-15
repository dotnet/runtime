// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libobjc
    {
#if TARGET_ARM64
        private const string MessageSendStructReturnEntryPoint = "objc_msgSend";
#else
        private const string MessageSendStructReturnEntryPoint = "objc_msgSend_stret";
#endif

        [StructLayout(LayoutKind.Sequential)]
        private struct NSOperatingSystemVersion
        {
            public nint majorVersion;
            public nint minorVersion;
            public nint patchVersion;
        }

        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_getClass(string className);
        [DllImport(Libraries.libobjc)]
        private static extern IntPtr sel_getUid(string selector);
        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector);

        internal static Version GetOperatingSystemVersion()
        {
            int major = 0;
            int minor = 0;
            int patch = 0;

            IntPtr processInfo = objc_msgSend(objc_getClass("NSProcessInfo"), sel_getUid("processInfo"));

            if (processInfo != IntPtr.Zero)
            {
                NSOperatingSystemVersion osVersion = get_operatingSystemVersion(processInfo, sel_getUid("operatingSystemVersion"));

                checked
                {
                    major = (int)osVersion.majorVersion;
                    minor = (int)osVersion.minorVersion;
                    patch = (int)osVersion.patchVersion;
                }
            }

            return new Version(major, minor, patch);
        }

        [DllImport(Libraries.libobjc, EntryPoint = MessageSendStructReturnEntryPoint)]
        private static extern NSOperatingSystemVersion get_operatingSystemVersion(IntPtr basePtr, IntPtr selector);

        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector, double secs); //used for 'initWithTimeIntervalSince1970:'
        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string nullTerminatedCString); //used for 'initWithUTF8String:'
        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector, IntPtr @object, IntPtr key); //used for 'dictionaryWithObject:forKey:'
        [DllImport(Libraries.libobjc)]
        private static extern byte objc_msgSend(IntPtr basePtr, IntPtr selector, IntPtr attributes, IntPtr path, IntPtr error); //used for 'setAttributes:ofItemAtPath:error:'

        internal static void SetCreationOrModificationTimeOfFileInternal(string path, bool isModificationDate, DateTimeOffset time)
        {
            var NSDate = objc_getClass("NSDate");
            var NSDictionary = objc_getClass("NSDictionary");
            var NSFileManager = objc_getClass("NSFileManager");
            var NSString = objc_getClass("NSString");
            var alloc = sel_getUid("alloc");
            var initWithUTF8String_ = sel_getUid("initWithUTF8String:");
            var initWithTimeIntervalSince1970_ = sel_getUid("initWithTimeIntervalSince1970:");
            var dictionaryWithObject_forKey_ = sel_getUid("dictionaryWithObject:forKey:");
            var defaultManager = sel_getUid("defaultManager");
            var setAttributes_ofItemAtPath_error_ = sel_getUid("setAttributes:ofItemAtPath:error:");
            var release = sel_getUid("release");
            var NSFileCreationOrModificationDate = objc_msgSend(objc_msgSend(NSString, alloc), initWithUTF8String_, isModificationDate ? "NSFileModificationDate" : "NSFileCreationDate");
            var DefaultNSFileManager = objc_msgSend(NSFileManager, defaultManager);

            var date = objc_msgSend(NSDate, alloc);
            date = objc_msgSend(date, initWithTimeIntervalSince1970_, (time - DateTimeOffset.UnixEpoch).TotalSeconds);
            var fileAttributes = objc_msgSend(NSDictionary, dictionaryWithObject_forKey_, date, NSFileCreationOrModificationDate);
            var native_filePath = objc_msgSend(NSString, alloc);
            native_filePath = objc_msgSend(native_filePath, initWithUTF8String_, path);
            try
            {
                if (objc_msgSend(DefaultNSFileManager, setAttributes_ofItemAtPath_error_, fileAttributes, native_filePath, IntPtr.Zero) != 1)
                {
                    //throw an error of some sort - need to change
                    throw new IOException("Could not set the creation date of the file.");
                }
            }
            finally
            {
                objc_msgSend(date, release);
                objc_msgSend(fileAttributes, release);
                objc_msgSend(native_filePath, release);
                objc_msgSend(NSFileCreationOrModificationDate, release);
            }
        }
    }
}
