// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
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
            var date = objc_msgSend(NSDate, alloc);
            date = objc_msgSend(date, initWithTimeIntervalSince1970_, time.ToUnixTimeSeconds());
            var fileAttributes = objc_msgSend(NSDictionary, dictionaryWithObject_forKey_, date, isModificationDate ? NSFileModificationDate : NSFileCreationDate);
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
            }
        }

        private static IntPtr NSDate;
        private static IntPtr NSDictionary;
        private static IntPtr NSFileManager;
        private static IntPtr NSString;
        private static IntPtr alloc;
        private static IntPtr init;
        private static IntPtr initWithUTF8String_;
        private static IntPtr initWithTimeIntervalSince1970_;
        private static IntPtr dictionaryWithObject_forKey_;
        private static IntPtr defaultManager;
        private static IntPtr setAttributes_ofItemAtPath_error_;
        private static IntPtr release;
        private static IntPtr NSFileCreationDate;
        private static IntPtr NSFileModificationDate;
        private static IntPtr DefaultNSFileManager;

        static libobjc()
        {
            NSDate = objc_getClass("NSDate");
            NSDictionary = objc_getClass("NSDictionary");
            NSFileManager = objc_getClass("NSFileManager");
            NSString = objc_getClass("NSString");

            alloc = sel_getUid("alloc");
            init = sel_getUid("init");
            initWithUTF8String_ = sel_getUid("initWithUTF8String:");
            initWithTimeIntervalSince1970_ = sel_getUid("initWithTimeIntervalSince1970:");
            dictionaryWithObject_forKey_ = sel_getUid("dictionaryWithObject:forKey:");
            defaultManager = sel_getUid("defaultManager");
            setAttributes_ofItemAtPath_error_ = sel_getUid("setAttributes:ofItemAtPath:error:");
            release = sel_getUid("release");

            NSFileCreationDate = objc_msgSend(NSString, alloc);
            NSFileCreationDate = objc_msgSend(NSFileCreationDate, initWithUTF8String_, "NSFileCreationDate");

            NSFileModificationDate = objc_msgSend(NSString, alloc);
            NSFileModificationDate = objc_msgSend(NSFileModificationDate, initWithUTF8String_, "NSFileModificationDate");

            DefaultNSFileManager = objc_msgSend(NSFileManager, defaultManager);
        }
    }
}
