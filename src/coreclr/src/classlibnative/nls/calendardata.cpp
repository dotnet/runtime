// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  Class:    CalendarData
//

//
//  Purpose:  This module implements the methods of the CalendarData
//            class.  These methods are the helper functions for the
//            Locale class.
//
//  Date:     July 4, 2007
//
////////////////////////////////////////////////////////////////////////////

#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "interoputil.h"
#include "corhost.h"

#include <winnls.h>

#include "calendardata.h"
#include "nlsinfo.h"
#include "newapis.h"

////////////////////////////////////////////////////////////////////////
//
// Call the Win32 GetCalendarInfoEx() using the specified calendar and LCTYPE.
// The return value can be INT32 or an allocated managed string object, depending on
// which version's called.
//
// Parameters:
//      OUT pOutputInt32    The output int32 value.
//      OUT pOutputRef      The output string value.
//
////////////////////////////////////////////////////////////////////////
BOOL CalendarData::CallGetCalendarInfoEx(LPCWSTR localeName, int calendar, int calType, INT32* pOutputInt32)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    int result = 0;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return result; )

    // Just stick it right into the output int
    _ASSERT((calType & CAL_RETURN_NUMBER) != 0);
    result = NewApis::GetCalendarInfoEx(localeName, calendar, NULL, calType, NULL, 0, (LPDWORD)pOutputInt32);

    END_SO_INTOLERANT_CODE

    return (result != 0);
}

BOOL CalendarData::CallGetCalendarInfoEx(LPCWSTR localeName, int calendar, int calType, STRINGREF* pOutputStrRef)
{
    CONTRACTL
    {
        THROWS;                 // We can throw since we are allocating managed string.
        DISABLED(GC_TRIGGERS); // Disabled 'cause it don't work right now
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    // The maximum size for values returned from GetLocaleInfo is 80 characters.
    WCHAR buffer[80];
    int result = 0;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return result; )

    _ASSERT((calType & CAL_RETURN_NUMBER) == 0);
    result = NewApis::GetCalendarInfoEx(localeName, calendar, NULL, calType, buffer, 80, NULL);

    if (result != 0)
    {
        _ASSERTE(pOutputStrRef != NULL);
        *pOutputStrRef = StringObject::NewString(buffer, result - 1);
    }

    END_SO_INTOLERANT_CODE

    return (result != 0);
}

////////////////////////////////////////////////////////////////////////
//
// Get the native day names
//
// NOTE: There's a disparity between .Net & windows day orders, the input day should
//           start with Sunday
//
// Parameters:
//      OUT pOutputStrings      The output string[] value.
//
////////////////////////////////////////////////////////////////////////
BOOL CalendarData::GetCalendarDayInfo(LPCWSTR localeName, int calendar, int calType, PTRARRAYREF* pOutputStrings)
{
    CONTRACTL
    {
        THROWS;                 // We can throw since we are allocating managed string.
        INJECT_FAULT(COMPlusThrowOM());
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    // The maximum size for values returned from GetLocaleInfo is 80 characters.
    WCHAR buffer[80];
    int result = 0;

    _ASSERT((calType & CAL_RETURN_NUMBER) == 0);

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return result;)

    //
    // We'll need a new array of 7 items
    //
    // Allocate the array of STRINGREFs.  We don't need to check for null because the GC will throw
    // an OutOfMemoryException if there's not enough memory.
    //
    PTRARRAYREF ResultArray = (PTRARRAYREF)AllocateObjectArray(7, g_pStringClass);
    
    GCPROTECT_BEGIN(ResultArray);

    // Get each one of them
    for (int i = 0; i < 7; i++, calType++)
    {
        result = NewApis::GetCalendarInfoEx(localeName, calendar, NULL, calType, buffer, 80, NULL);

        // Note that the returned string is null terminated, so even an empty string will be 1
        if (result != 0)
        {
            // Make a string for this entry
            STRINGREF stringResult = StringObject::NewString(buffer, result - 1);
            ResultArray->SetAt(i, (OBJECTREF)stringResult);
        }

        // On the first iteration we need to go from CAL_SDAYNAME7 to CAL_SDAYNAME1, so subtract 7 before the ++ happens
        // This is because the framework starts on sunday and windows starts on monday when counting days
        if (i == 0) calType -= 7;
    }
    GCPROTECT_END();

    _ASSERTE(pOutputStrings != NULL);
    *pOutputStrings = ResultArray;

    END_SO_INTOLERANT_CODE

    return (result != 0);
}



////////////////////////////////////////////////////////////////////////
//
// Get the native month names
//
// Parameters:
//      OUT pOutputStrings      The output string[] value.
//
////////////////////////////////////////////////////////////////////////
BOOL CalendarData::GetCalendarMonthInfo(LPCWSTR localeName, int calendar, int calType, PTRARRAYREF* pOutputStrings)
{
    CONTRACTL
    {
        THROWS;                 // We can throw since we are allocating managed string.
        INJECT_FAULT(COMPlusThrowOM());
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    // The maximum size for values returned from GetLocaleInfo is 80 characters.
    WCHAR buffer[80];
    int result = 0;

    _ASSERT((calType & CAL_RETURN_NUMBER) == 0);

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return result;)

    //
    // We'll need a new array of 13 items
    //
    // Allocate the array of STRINGREFs.  We don't need to check for null because the GC will throw
    // an OutOfMemoryException if there's not enough memory.
    //
    PTRARRAYREF ResultArray = (PTRARRAYREF)AllocateObjectArray(13, g_pStringClass);
    
    GCPROTECT_BEGIN(ResultArray);

    // Get each one of them
    for (int i = 0; i < 13; i++, calType++)
    {
        result = NewApis::GetCalendarInfoEx(localeName, calendar, NULL, calType, buffer, 80, NULL);

        // If we still have failure, then mark as empty string
        if (result == 0)
        {
            buffer[0] = W('0');
            result = 1;


        }

        // Note that the returned string is null terminated, so even an empty string will be 1
        // Make a string for this entry
        STRINGREF stringResult = StringObject::NewString(buffer, result - 1);
        ResultArray->SetAt(i, (OBJECTREF)stringResult);
    }
    GCPROTECT_END();

    _ASSERTE(pOutputStrings != NULL);
    *pOutputStrings = ResultArray;

    END_SO_INTOLERANT_CODE

    return (result != 0);
}

//
// struct to help our calendar data enumaration callback
//
struct enumData
{
    int     count;          // # of strings found so far
    LPWSTR  userOverride;   // pointer to user override string if used
    LPWSTR  stringsBuffer;  // pointer to a buffer to use for the strings.
    LPWSTR  endOfBuffer;    // pointer to the end of the stringsBuffer ( must be < this to write)
};

//
// callback itself
//
BOOL CALLBACK EnumCalendarInfoCallback(__in_z LPWSTR lpCalendarInfoString, __in CALID Calendar, __in_opt LPWSTR pReserved, __in LPARAM lParam)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(lpCalendarInfoString));
        PRECONDITION(CheckPointer((LPVOID)lParam));
    } CONTRACTL_END;

    // Cast our data to the right type
    enumData* pData = (enumData*)lParam;

    // If we had a user override, check to make sure this differs
    if (pData->userOverride == NULL ||
        wcscmp(pData->userOverride, lpCalendarInfoString) != 0)
    {
        // They're different, add it to our buffer
        LPWSTR pStart = pData->stringsBuffer;
        LPCWSTR pEnd = pData->endOfBuffer;
        while (pStart < pEnd && *lpCalendarInfoString != 0)
        {
            *(pStart++) = *(lpCalendarInfoString++);
        }

        // Add a \0
        if (pStart < pEnd)
        {
            *(pStart++) = 0;

            // Did it finish?
            if (pStart <= pEnd)
            {
                // It finished, use it
                pData->count++;
                pData->stringsBuffer = pStart;
            }
        }
    }

    return TRUE;
}

//
// CallEnumCalendarInfo
//
// Get the list of whichever calendar property from the OS.  If user override is passed in, then check GetLocaleInfo as well
// to see if a user override is set.
//
// We build a list of strings, first calling getlocaleinfo if necessary, and then the enums.  The strings are null terminated,
// with a double null ending the list.  Once we have the list we can allocate our COMStrings and arrays from the count.
//
// We need a helper structure to pass as an lParam
//
BOOL CalendarData::CallEnumCalendarInfo(__in_z LPCWSTR localeName, __in int calendar, __in int calType,
                                        __in int lcType, __inout PTRARRAYREF* pOutputStrings)
{
    CONTRACTL
    {
        THROWS;                 // We can throw since we are allocating managed string.
        INJECT_FAULT(COMPlusThrowOM());
        DISABLED(GC_TRIGGERS); // Disabled 'cause it don't work right now
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    BOOL result = TRUE;

    // Our longest string in culture.xml is shorter than this and it has lots of \x type characters, so this should be long enough by far.
    WCHAR   stringBuffer[512];

    struct enumData data;
    data.count = 0;
    data.userOverride = NULL;
    data.stringsBuffer = stringBuffer;
    data.endOfBuffer = stringBuffer + 512;   // We're adding WCHAR sizes

    // First call GetLocaleInfo if necessary
    if ((lcType && ((lcType & LOCALE_NOUSEROVERRIDE) == 0)) &&
        // Get user locale, see if it matches localeName.
        // Note that they should match exactly, including letter case
        NewApis::GetUserDefaultLocaleName(stringBuffer, 512) && wcscmp(localeName, stringBuffer) == 0)
    {
        // They want user overrides, see if the user calendar matches the input calendar
        CALID userCalendar = 0;
        NewApis::GetLocaleInfoEx(localeName, LOCALE_ICALENDARTYPE | LOCALE_RETURN_NUMBER, (LPWSTR)&userCalendar,
                                            sizeof(userCalendar) / sizeof(WCHAR) );

        // If the calendars were the same, see if the locales were the same
        if ((int)userCalendar == calendar) // todo: cast to compile on MAC
        {
            // They matched, get the user override since locale & calendar match
            int i = NewApis::GetLocaleInfoEx(localeName, lcType, stringBuffer, 512);

            // if it succeeded, advance the pointer and remember the override for the later callers
            if (i > 0)
            {
                // Remember this was the override (so we can look for duplicates later in the enum function)
                data.userOverride = data.stringsBuffer;

                // Advance to the next free spot (i includes counting the \0)
                data.stringsBuffer += i;

                // And our count...
                data.count++;
            }
        }
    }

    // Now call the enumeration API. Work is done by our callback function
    NewApis::EnumCalendarInfoExEx(EnumCalendarInfoCallback, localeName, calendar, calType, (LPARAM)&data);

    // Now we have a list of data, fail if we didn't find anything.
    if (data.count == 0) return FALSE;

    // Now we need to allocate our stringarray and populate it
    STATIC_CONTRACT_SO_TOLERANT;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return (result); )

    // Get our array object (will throw, don't have to check it)
    PTRARRAYREF dataArray = (PTRARRAYREF) AllocateObjectArray(data.count, g_pStringClass);

    GCPROTECT_BEGIN(dataArray);
    LPCWSTR buffer = stringBuffer;   // Restart @ buffer beginning
    for(DWORD i = 0; i < (DWORD)data.count; i++)
    {
        OBJECTREF o = (OBJECTREF) StringObject::NewString(buffer);

        if (calType == CAL_SABBREVERASTRING || calType == CAL_SERASTRING)
        {
            // Eras are enumerated backwards.  (oldest era name first, but
            // Japanese calendar has newest era first in array, and is only
            // calendar with multiple eras)
            dataArray->SetAt((DWORD)data.count - i - 1, o);
        }
        else
        {
            dataArray->SetAt(i, o);
        }

        buffer += (lstrlenW(buffer) + 1);
    }
    GCPROTECT_END();

    _ASSERTE(pOutputStrings != NULL);
    *pOutputStrings = dataArray;

    END_SO_INTOLERANT_CODE

    return result;
}

////////////////////////////////////////////////////////////////////////
//
// For calendars like Gregorain US/Taiwan/UmAlQura, they are not available
// in all OS or all localized versions of OS.
// If OS does not support these calendars, we will fallback by using the
// appropriate fallback calendar and locale combination to retrieve data from OS.
//
// Parameters:
//  __deref_inout pCalendarInt:
//    Pointer to the calendar ID. This will be updated to new fallback calendar ID if needed.
//  __in_out pLocaleNameStackBuffer
//    Pointer to the StackSString object which holds the locale name to be checked.
//    This will be updated to new fallback locale name if needed.
//
////////////////////////////////////////////////////////////////////////

void CalendarData::CheckSpecialCalendar(INT32* pCalendarInt, StackSString* pLocaleNameStackBuffer)
{
    // Gregorian-US isn't always available in the OS, however it is the same for all locales
    switch (*pCalendarInt)
    {
        case CAL_GREGORIAN_US:
            // See if this works
            if (0 == NewApis::GetCalendarInfoEx(*pLocaleNameStackBuffer, *pCalendarInt, NULL, CAL_SCALNAME, NULL, 0, NULL))
            {
                // Failed, set it to a locale (fa-IR) that's alway has Gregorian US available in the OS
                pLocaleNameStackBuffer->Set(W("fa-IR"), 5);
            }
            // See if that works
            if (0 == NewApis::GetCalendarInfoEx(*pLocaleNameStackBuffer, *pCalendarInt, NULL, CAL_SCALNAME, NULL, 0, NULL))
            {
                // Failed again, just use en-US with the gregorian calendar
                pLocaleNameStackBuffer->Set(W("en-US"), 5);
                *pCalendarInt = CAL_GREGORIAN;
            }
            break;
        case CAL_TAIWAN:
            // Taiwan calendar data is not always in all language version of OS due to Geopolical reasons.
            // It is only available in zh-TW localized versions of Windows.
            // Let's check if OS supports it.  If not, fallback to Greogrian localized for Taiwan calendar.
            if (0 == NewApis::GetCalendarInfoEx(*pLocaleNameStackBuffer, *pCalendarInt, NULL, CAL_SCALNAME, NULL, 0, NULL))
            {
                *pCalendarInt = CAL_GREGORIAN;
            }
            break;
        case CAL_UMALQURA:
            // UmAlQura is only available in Vista and above, so we will need to fallback to Hijri if it is not available in the OS.
            if (0 == NewApis::GetCalendarInfoEx(*pLocaleNameStackBuffer, *pCalendarInt, NULL, CAL_SCALNAME, NULL, 0, NULL))
            {
                // There are no differences in DATA between UmAlQura and Hijri, and
                // UmAlQura isn't available before Vista, so just use Hijri..
                *pCalendarInt = CAL_HIJRI;
            }
            break;
    }
}

////////////////////////////////////////////////////////////////////////
//
//  Implementation for CalendarInfo.nativeGetCalendarData
//
//  Retrieve calendar properties from the native side
//
//  Parameters:
//      pCalendarData: This is passed from a managed structure CalendarData.cs
//      pLocaleNameUNSAFE: Locale name associated with the locale for this calendar
//      calendar: Calendar ID
//
//  NOTE: Calendars depend on the locale name that creates it.  Only a few
//            properties are available without locales using CalendarData.GetCalendar(int)
//
////////////////////////////////////////////////////////////////////////

FCIMPL3(FC_BOOL_RET, CalendarData::nativeGetCalendarData, CalendarData* calendarDataUNSAFE, StringObject* pLocaleNameUNSAFE, INT32 calendar)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pLocaleNameUNSAFE));
    } CONTRACTL_END;


    // The maximum allowed string length in GetLocaleInfo is 80 WCHARs.
    BOOL ret = TRUE;

    struct _gc
    {
        STRINGREF       localeName;
        CALENDARDATAREF calendarData;
    } gc;

    // Dereference our gc objects
    gc.localeName = (STRINGREF)pLocaleNameUNSAFE;
    gc.calendarData = (CALENDARDATAREF)calendarDataUNSAFE;

    // Need to set up the frame since we will be allocating managed strings.
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // create a local copy of the string in order to pass it to helper methods that trigger GCs like GetCalendarDayInfo and GetCalendarMonthInfo
    StackSString localeNameStackBuffer( gc.localeName->GetBuffer() );

    // Conveniently this is the same as LOCALE_NOUSEROVERRIDE, so we can use this for both
    int useOverrides = (gc.calendarData->bUseUserOverrides) ? 0 : CAL_NOUSEROVERRIDE;

    // Helper string
    STRINGREF stringResult = NULL;

    //
    // Windows doesn't support some calendars right now, so remap those.
    //
    if (calendar >= 13 && calendar < 23)
    {
        switch (calendar)
        {
            case RESERVED_CAL_PERSIAN: // don't change if we have Persian calendar
                break;

            case RESERVED_CAL_JAPANESELUNISOLAR:    // Data looks like Japanese
                calendar=CAL_JAPAN;
                break;
            case RESERVED_CAL_JULIAN:               // Data looks like gregorian US
            case RESERVED_CAL_CHINESELUNISOLAR:     // Algorithmic, so actual data is irrelevent
            case RESERVED_CAL_SAKA:                 // reserved to match Office but not implemented in our code, so data is irrelevent
            case RESERVED_CAL_LUNAR_ETO_CHN:        // reserved to match Office but not implemented in our code, so data is irrelevent
            case RESERVED_CAL_LUNAR_ETO_KOR:        // reserved to match Office but not implemented in our code, so data is irrelevent
            case RESERVED_CAL_LUNAR_ETO_ROKUYOU:    // reserved to match Office but not implemented in our code, so data is irrelevent
            case RESERVED_CAL_KOREANLUNISOLAR:      // Algorithmic, so actual data is irrelevent
            case RESERVED_CAL_TAIWANLUNISOLAR:      // Algorithmic, so actual data is irrelevent
            default:
                calendar = CAL_GREGORIAN_US;
                break;
        }
    }

    //
    // Speical handling for some special calendar due to OS limitation.
    // This includes calendar like Taiwan calendar, UmAlQura calendar, etc.
    //
    CheckSpecialCalendar(&calendar, &localeNameStackBuffer);


    // Numbers
    ret &= CallGetCalendarInfoEx(localeNameStackBuffer, calendar,
                                 CAL_ITWODIGITYEARMAX | CAL_RETURN_NUMBER | useOverrides,
                                 &(gc.calendarData->iTwoDigitYearMax));

    if (ret == FALSE) // failed call
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_ITWODIGITYEARMAX");
    }

    _ASSERTE(ret == TRUE);

    // Strings
    if (CallGetCalendarInfoEx(localeNameStackBuffer, calendar, CAL_SCALNAME , &stringResult))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->sNativeName), stringResult, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SCALNAME");
        ret = FALSE;
    }
    if (CallGetCalendarInfoEx(localeNameStackBuffer, calendar, CAL_SMONTHDAY | useOverrides, &stringResult))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->sMonthDay), stringResult, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read RESERVED_CAL_SMONTHDAY");
        ret = FALSE;
    }

    // String Arrays
    // Formats
    PTRARRAYREF array = NULL;
    if (CallEnumCalendarInfo(localeNameStackBuffer, calendar, CAL_SSHORTDATE, LOCALE_SSHORTDATE | useOverrides, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saShortDates), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SSHORTDATE");
        ret = FALSE;
    }
    if (CallEnumCalendarInfo(localeNameStackBuffer, calendar, CAL_SLONGDATE, LOCALE_SLONGDATE | useOverrides, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saLongDates), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SLONGDATE");
        ret = FALSE;
    }

    // Get the YearMonth pattern.
    // Before Windows Vista, NLS would not write the Year/Month pattern into the reg key.
    // This causes GetLocaleInfo() to retrieve the Gregorian localized calendar pattern even when the calendar is not Gregorian localized.
    // So we will call GetLocaleInfo() only when the reg key for sYearMonth is there.
    //
    // If the key does not exist, leave yearMonthPattern to be null, so that we will pick up the default table value.

    int useOverridesForYearMonthPattern = useOverrides;
    if (useOverridesForYearMonthPattern == 0)
    {
        HKEY hkey = NULL;
        useOverridesForYearMonthPattern = CAL_NOUSEROVERRIDE;
        if (WszRegOpenKeyEx(HKEY_CURRENT_USER, W("Control Panel\\International"), 0, KEY_READ, &hkey) == ERROR_SUCCESS)
        {
            if (WszRegQueryValueEx(hkey, W("sYearMonth"), 0, NULL, NULL, NULL) == ERROR_SUCCESS)
            {
                // The sYearMonth key exists.  Call GetLocaleInfo() to read it.
                useOverridesForYearMonthPattern = 0; // now we can use the overrides
            }
            RegCloseKey(hkey);
        }
    }

    if (CallEnumCalendarInfo(localeNameStackBuffer, calendar, CAL_SYEARMONTH, LOCALE_SYEARMONTH | useOverridesForYearMonthPattern, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saYearMonths), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SYEARMONTH");
        ret = FALSE;
    }

    // Day & Month Names
    // These are all single calType entries, 1 per day, so we have to make 7 or 13 calls to collect all the names

    // Day
    // Note that we're off-by-one since managed starts on sunday and windows starts on monday
    if (GetCalendarDayInfo(localeNameStackBuffer, calendar, CAL_SDAYNAME7, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saDayNames), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SDAYNAME7");
        ret = FALSE;
    }
    if (GetCalendarDayInfo(localeNameStackBuffer, calendar, CAL_SABBREVDAYNAME7, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saAbbrevDayNames), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SABBREVDAYNAME7");
        ret = FALSE;
    }

    // Month names
    if (GetCalendarMonthInfo(localeNameStackBuffer, calendar, CAL_SMONTHNAME1, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saMonthNames), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SMONTHNAME1");
        ret = FALSE;
    }

    if (GetCalendarMonthInfo(localeNameStackBuffer, calendar, CAL_SABBREVMONTHNAME1, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saAbbrevMonthNames), array, NULL);
    else
    {
        _ASSERTE(!"nativeGetCalendarData could not read CAL_SABBREVMONTHNAME1");
        ret = FALSE;
    }

    //
    // The following LCTYPE are not supported in some platforms.  If the call fails,
    // don't return a failure.
    //
    if (GetCalendarDayInfo(localeNameStackBuffer, calendar, CAL_SSHORTESTDAYNAME7, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saSuperShortDayNames), array, NULL);


    // Gregorian may have genitive month names
    if (calendar == CAL_GREGORIAN)
    {
        if (GetCalendarMonthInfo(localeNameStackBuffer, calendar, CAL_SMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES, &array))
            SetObjectReference((OBJECTREF*)&(gc.calendarData->saMonthGenitiveNames), array, NULL);
        // else we ignore the error and let managed side copy the normal month names
        if (GetCalendarMonthInfo(localeNameStackBuffer, calendar, CAL_SABBREVMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES, &array))
            SetObjectReference((OBJECTREF*)&(gc.calendarData->saAbbrevMonthGenitiveNames), array, NULL);
        // else we ignore the error and let managed side copy the normal month names
    }

//  leap year names are only different for month 6 in Hebrew calendar
//    PTRARRAYREF saLeapYearMonthNames      ; // Multiple strings for the month names in a leap year. (Hebrew's the only one that has these)

    // Calendar Parts Names
    if (CallEnumCalendarInfo(localeNameStackBuffer, calendar, CAL_SERASTRING, NULL, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saEraNames), array, NULL);
    // else we set the era in managed code
    if (CallEnumCalendarInfo(localeNameStackBuffer, calendar, CAL_SABBREVERASTRING, NULL, &array))
        SetObjectReference((OBJECTREF*)&(gc.calendarData->saAbbrevEraNames), array, NULL);
    // else we set the era in managed code

    //    PTRARRAYREF saAbbrevEnglishEraNames   ; // Abbreviated Era Names in English

    //
    // Calendar Era Info
    // Note that calendar era data (offsets, etc) is hard coded for each calendar since this
    // data is implementation specific and not dynamic (except perhaps Japanese)
    //

    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

//
// Get the system two digit year max value for the specified calendar
//
FCIMPL1(INT32, CalendarData::nativeGetTwoDigitYearMax, INT32 calendar)
{
    FCALL_CONTRACT;

    DWORD dwTwoDigitYearMax = (DWORD) -1;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    WCHAR strName[LOCALE_NAME_MAX_LENGTH];

    // Really we should just be able to pass NULL for the locale name since that
    // causes the OS to look up the user default locale name.  The downlevel APIS could
    // emulate this as necessary
    if (NewApis::GetUserDefaultLocaleName(strName,NumItems(strName)) == 0 ||
        NewApis::GetCalendarInfoEx(strName, calendar, NULL, CAL_ITWODIGITYEARMAX | CAL_RETURN_NUMBER, NULL, 0,&dwTwoDigitYearMax) == 0)
    {
        dwTwoDigitYearMax = (DWORD) -1;
        goto lExit;
    }

lExit: ;
    HELPER_METHOD_FRAME_END();

    return (dwTwoDigitYearMax);
}
FCIMPLEND

//
// nativeGetCalendars
//
// Get the list of acceptable calendars for this user/locale
//
// Might be a better way to marshal the int[] for calendars
// We expect the input array to be 23 ints long.  We then fill up the first "count" ints and return the count.
// The caller should then make it a smaller array.
//

// Perhaps we could do something more like this...
//U1ARRAYREF rgbOut = (U1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
//memcpyNoGCRefs(rgbOut->GetDirectPointerToNonObjectElements(), rgbKey, cb * sizeof(BYTE));
//
//refRetVal = rgbOut;
//
//HELPER_METHOD_FRAME_END();
//return (U1Array*) OBJECTREFToObject(refRetVal);

//
// struct to help our calendar data enumaration callback
//
struct enumCalendarsData
{
    int     count;          // # of strings found so far
    CALID   userOverride;   // user override value (if found)
    INT32*  calendarList;   // list of calendars found so far
};

//
// callback itself
//
BOOL CALLBACK EnumCalendarsCallback(__in_z LPWSTR lpCalendarInfoString, __in CALID Calendar, __in_opt LPWSTR pReserved, __in LPARAM lParam)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(lpCalendarInfoString));
        PRECONDITION(CheckPointer((LPVOID)lParam));
    } CONTRACTL_END;

    // Cast our data to the right type
    enumCalendarsData* pData = (enumCalendarsData*)lParam;

    // If we had a user override, check to make sure this differs
    if (pData->userOverride == Calendar)
    {
        // Its the same, just return
        return TRUE;
    }

    // They're different, add it to our buffer, check we have room
    if (pData->count < 23)
    {
        pData->calendarList[pData->count++] = Calendar;
    }

    return TRUE;
}

FCIMPL3(INT32, CalendarData::nativeGetCalendars, StringObject* pLocaleNameUNSAFE, CLR_BOOL useOverrides, I4Array* calendarsUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pLocaleNameUNSAFE));
    } CONTRACTL_END;

    int ret = 0;

    struct _gc
    {
        STRINGREF                localeName;
        I4ARRAYREF               calendarsRef;
    } gc;

    // Dereference our string
    gc.localeName   = (STRINGREF)pLocaleNameUNSAFE;
    gc.calendarsRef = (I4ARRAYREF)calendarsUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    int    calendarBuffer[23];
    struct enumCalendarsData data;
    data.count = 0;
    data.userOverride = 0;
    data.calendarList = calendarBuffer;

    // First call GetLocaleInfo if necessary
    if (useOverrides)
    {
        // They want user overrides, see if the user calendar matches the input calendar

        CALID userCalendar = 0;
        NewApis::GetLocaleInfoEx( gc.localeName->GetBuffer(), LOCALE_ICALENDARTYPE | LOCALE_RETURN_NUMBER,
                                            (LPWSTR)&userCalendar, sizeof(userCalendar) / sizeof(WCHAR) );

        // If we got a default, then use it as the first calendar
        if (userCalendar != 0)
        {
            data.userOverride = userCalendar;
            data.calendarList[data.count++] = userCalendar;
        }
    }

    // Now call the enumeration API. Work is done by our callback function
    NewApis::EnumCalendarInfoExEx(EnumCalendarsCallback, gc.localeName->GetBuffer(), ENUM_ALL_CALENDARS, CAL_ICALINTVALUE, (LPARAM)&(data));

    // Copy to the output array
    for (int i = 0; i < data.count; i++)
    {
        (gc.calendarsRef->GetDirectPointerToNonObjectElements())[i] = calendarBuffer[i];
    }

    ret = data.count;
    HELPER_METHOD_FRAME_END();

    // Now we have a list of data, return the count
    return ret;
}
FCIMPLEND

//
// nativeEnumTimeFormats
//
// Enumerate all of the time formats (long times) on the system.
// Windows only has 1 time format so there's nothing like an LCTYPE here.
//
// Note that if the locale is the user default locale windows ALWAYS returns the user override value first.
// (ie: there's no no-user-override option for this API)
//
// We reuse the enumData structure since it works for us.
//

//
// callback itself
//
BOOL CALLBACK EnumTimeFormatsCallback(__in_z LPCWSTR lpTimeFormatString, __in LPARAM lParam)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(lpTimeFormatString));
        PRECONDITION(CheckPointer((LPVOID)lParam));
    } CONTRACTL_END;

    // Cast our data to the right type
    enumData* pData = (enumData*)lParam;

    // Don't have to worry about user overrides (the enum adds them)
    // add it to our buffer
    LPWSTR pStart = pData->stringsBuffer;
    LPCWSTR pEnd = pData->endOfBuffer;
    while (pStart < pEnd && *lpTimeFormatString != 0)
    {
        *(pStart++) = *(lpTimeFormatString++);
    }

    // Add a \0
    if (pStart < pEnd)
    {
        *(pStart++) = 0;

        // Did it finish?
        if (pStart <= pEnd)
        {
            // It finished, use it
            pData->count++;
            pData->stringsBuffer = pStart;
        }
    }

    return TRUE;
}

//
// nativeEnumTimeFormats that calls the callback above
//
FCIMPL3(Object*, CalendarData::nativeEnumTimeFormats,
        StringObject* pLocaleNameUNSAFE, INT32 dwFlags, CLR_BOOL useUserOverride)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pLocaleNameUNSAFE));
    } CONTRACTL_END;


    struct _gc
    {
        STRINGREF       localeName;
        PTRARRAYREF     timeFormatsArray;
    } gc;

    // Dereference our gc objects
    gc.localeName = (STRINGREF)pLocaleNameUNSAFE;
    gc.timeFormatsArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // Our longest string in culture.xml is shorter than this and it has lots of \x type characters, so this should be long enough by far.
    WCHAR   stringBuffer[512];
    struct enumData data;
    data.count = 0;
    data.userOverride = NULL;
    data.stringsBuffer = stringBuffer;
    data.endOfBuffer = stringBuffer + 512; // We're adding WCHAR sizes

    // Now call the enumeration API. Work is done by our callback function
    NewApis::EnumTimeFormatsEx((TIMEFMT_ENUMPROCEX)EnumTimeFormatsCallback, gc.localeName->GetBuffer(), dwFlags, (LPARAM)&data);

    if (data.count > 0)
    {
        // Now we need to allocate our stringarray and populate it
        // Get our array object (will throw, don't have to check it)
        gc.timeFormatsArray = (PTRARRAYREF) AllocateObjectArray(data.count, g_pStringClass);

        LPCWSTR buffer = stringBuffer;   // Restart @ buffer beginning
        for(DWORD i = 0; i < (DWORD)data.count; i++) // todo: cast to compile on Mac
        {
            OBJECTREF o = (OBJECTREF) StringObject::NewString(buffer);
            gc.timeFormatsArray->SetAt(i, o);

            buffer += (lstrlenW(buffer) + 1);
        }

        if(!useUserOverride && data.count > 1)
        {
            // Since there is no "NoUserOverride" aware EnumTimeFormatsEx, we always get an override
            // The override is the first entry if it is overriden.
            // We can check if we have overrides by checking the GetLocaleInfo with no override
            // If we do have an override, we don't know if it is a user defined override or if the
            // user has just selected one of the predefined formats so we can't just remove it
            // but we can move it down.
            WCHAR timeFormatNoUserOverride[LOCALE_NAME_MAX_LENGTH];
            DWORD lcType = (dwFlags == TIME_NOSECONDS) ? LOCALE_SSHORTTIME : LOCALE_STIMEFORMAT;
            lcType |= LOCALE_NOUSEROVERRIDE;
            int result = NewApis::GetLocaleInfoEx(gc.localeName->GetBuffer(), lcType, timeFormatNoUserOverride, LOCALE_NAME_MAX_LENGTH);
            if(result != 0)
            {
                STRINGREF firstTimeFormat = (STRINGREF)gc.timeFormatsArray->GetAt(0);
                if(wcscmp(timeFormatNoUserOverride, firstTimeFormat->GetBuffer())!=0)
                {
                    gc.timeFormatsArray->SetAt(0, gc.timeFormatsArray->GetAt(1));
                    gc.timeFormatsArray->SetAt(1, firstTimeFormat);
                }
            }
        }

    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.timeFormatsArray);
}
FCIMPLEND


