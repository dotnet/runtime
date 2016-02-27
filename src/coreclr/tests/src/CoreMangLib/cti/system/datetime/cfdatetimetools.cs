// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using TestLibrary;
using System.Runtime.InteropServices;

// utility functions for accessing DateTime functions in CoreFoundation for validation purposes

// Important Notes:
//    CF reference types are passed as IntPtrs. In the declarations, the actual type name is given within C-style comments:
//                IntPtr /* CFTypeRef */ tr
//    many function signatures contain an allocator of type CFAllocatorRef. To use the default allocator pass in IntPtr.Zero.

public class CFDateTimeTools
{

    // Basic tools (should be factored out eventually, as not DateTime-specific)
    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    protected static extern string CFStringGetCStringPtr(IntPtr /* CFStringRef */ theString, CFStringEncoding encoding);

    public enum CFStringEncoding : uint
    {
        kCFStringEncodingMacRoman = 0,
        kCFStringEncodingWindowsLatin1 = 0x0500,
        kCFStringEncodingISOLatin1 = 0x0201,
        kCFStringEncodingNextStepLatin = 0x0B01,
        kCFStringEncodingASCII = 0x0600,
        kCFStringEncodingUnicode = 0x0100,
        kCFStringEncodingUTF8 = 0x08000100,
        kCFStringEncodingNonLossyASCII = 0x0BFF,

        // The following constants are available
        // only on Mac OS X v10.4 and later,
        kCFStringEncodingUTF16 = 0x0100,
        kCFStringEncodingUTF16BE = 0x10000100,
        kCFStringEncodingUTF16LE = 0x14000100,
        kCFStringEncodingUTF32 = 0x0c000100,
        kCFStringEncodingUTF32BE = 0x18000100,
        kCFStringEncodingUTF32LE = 0x1c000100
    }
    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    protected static extern IntPtr /* CFStringRef */ CFStringCreateWithCString(IntPtr /* CFAllocatorRef */ alloc,
                                string cStr,
                                CFStringEncoding encoding);


    [System.Security.SecuritySafeCritical]
    public static string CFStringToSystemString(IntPtr /* CFStringRef */ theString)
    {
        return CFStringGetCStringPtr(theString, CFStringEncoding.kCFStringEncodingMacRoman);
    }

    [System.Security.SecuritySafeCritical]
    public static IntPtr /* CFStringRef */ SystemStringToCFString(string theString)
    {
        return CFStringCreateWithCString(IntPtr.Zero, theString, CFStringEncoding.kCFStringEncodingMacRoman);
    }

    [System.Security.SecuritySafeCritical]
    public static string GetOSFormattedDate(DateTime d, bool getLocale)
    {
        IntPtr cfDateRef = CFDateTimeTools.DateTimeToCFDateRef(d);
        IntPtr cfLocaleRef = (getLocale ? CFDateTimeTools.CFLocaleCopyCurrent() : IntPtr.Zero);
        IntPtr cfDateFmtr = CFDateFormatterCreate(IntPtr.Zero, cfLocaleRef,
        CFDateFormatterStyle.kCFDateFormatterLongStyle,
        CFDateFormatterStyle.kCFDateFormatterLongStyle); // 3 for long style

        IntPtr cfStr = CFDateFormatterCreateStringWithDate(IntPtr.Zero, cfDateFmtr, cfDateRef);
        return CFStringToSystemString(cfStr);
    }

    /// CFLocale
    /// --------

    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public static extern IntPtr /* CFLocaleRef */ CFLocaleCopyCurrent();

    /// CFDate tools
    /// ------------

    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public static extern /* CFDateRef */ IntPtr CFDateCreate(IntPtr /* CFAllocatorRef */ allocator,
                        double /* CFAbsoluteTime */ at);

    // convert between CLR DateTime and CoreFoundation CFAbsoluteTime (== double) and CFDateRef types
    private static DateTime CFAbsoluteTimeZero = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static double DateTimeToCFAbsoluteTime(DateTime d)
    {
        return d.Subtract(CFAbsoluteTimeZero).TotalSeconds;
    }

    [System.Security.SecuritySafeCritical]
    public static IntPtr /* CFDateRef */ DateTimeToCFDateRef(DateTime d)
    {
        return CFDateCreate(IntPtr.Zero,
            DateTimeToCFAbsoluteTime(d));
    }


    /// CFDateTimeFormatter
    /// -------------------

    public enum CFDateFormatterStyle : int
    {
        kCFDateFormatterNoStyle = 0,
        kCFDateFormatterShortStyle = 1,
        kCFDateFormatterMediumStyle = 2,
        kCFDateFormatterLongStyle = 3,
        kCFDateFormatterFullStyle = 4
    };


    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public static extern IntPtr /* CFDateFormatterRef */ CFDateFormatterCreate(IntPtr /* CFAllocatorRef */ alloc,
          IntPtr /* CFLocaleRef */ locale, CFDateFormatterStyle dateStyle, CFDateFormatterStyle timeStyle);


    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public static extern /* CFStringRef */ IntPtr CFDateFormatterCreateStringWithDate(IntPtr /* CFAllocatorRef */ alloc,
          IntPtr /* CFDateFormatterRef */ formatter, IntPtr /* CFDateRef */ date);

    /// CFTimeZone 
    /// -----------

    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public extern static IntPtr /* CFTimeZoneRef */ CFTimeZoneCopySystem();

    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public extern static IntPtr /* CFStringRef */ CFTimeZoneGetName(IntPtr /* CFTimeZoneRef */ tz);

    [System.Security.SecuritySafeCritical]
    public static string GetOSTimeZoneName(IntPtr /* CFTimeZoneRef */ tz)
    {
        return CFStringToSystemString(CFTimeZoneGetName(tz));
    }

    [DllImport("/Developer/SDKs/MacOSX10.4u.sdk/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
    public extern static bool CFTimeZoneIsDaylightSavingTime(IntPtr /* CFTimeZoneRef */ tz,
                                double /* CFAbsoluteTime */ at);
}