// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// IcuHolder is a template that can manage the lifetime of a raw pointer to ensure that it is cleaned up at the correct
// time.  The general usage pattern is to aquire some ICU resource via an _open call, then construct a holder using the
// pointer and UErrorCode to manage the lifetime.  When the holder goes out of scope, the coresponding close method is
// called on the pointer.
template <typename T, typename Closer>
class IcuHolder
{
  public:
    IcuHolder(T* p, UErrorCode err)
    {
        m_p = U_SUCCESS(err) ? p : nullptr;
    }

    ~IcuHolder()
    {
        if (m_p != nullptr)
        {
            Closer()(m_p);
        }
    }

  private:
    T* m_p;
    IcuHolder(const IcuHolder&) = delete;
    IcuHolder operator=(const IcuHolder&) = delete;
};

struct UCalendarCloser
{
    void operator()(UCalendar* pCal) const
    {
        ucal_close(pCal);
    }
};

struct UEnumerationCloser
{
    void operator()(UEnumeration* pEnum) const
    {
        uenum_close(pEnum);
    }
};

struct UDateTimePatternGeneratorCloser
{
    void operator()(UDateTimePatternGenerator* pGenerator) const
    {
        udatpg_close(pGenerator);
    }
};

struct UDateFormatCloser
{
    void operator()(UDateFormat* pDateFormat) const
    {
        udat_close(pDateFormat);
    }
};

struct UNumberFormatCloser
{
    void operator()(UNumberFormat* pNumberFormat) const
    {
        unum_close(pNumberFormat);
    }
};

struct ULocaleDisplayNamesCloser
{
    void operator()(ULocaleDisplayNames* pLocaleDisplayNames) const
    {
        uldn_close(pLocaleDisplayNames);
    }
};

struct UResourceBundleCloser
{
    void operator()(UResourceBundle* pResourceBundle) const
    {
        ures_close(pResourceBundle);
    }
};

typedef IcuHolder<UCalendar, UCalendarCloser> UCalendarHolder;
typedef IcuHolder<UEnumeration, UEnumerationCloser> UEnumerationHolder;
typedef IcuHolder<UDateTimePatternGenerator, UDateTimePatternGeneratorCloser> UDateTimePatternGeneratorHolder;
typedef IcuHolder<UDateFormat, UDateFormatCloser> UDateFormatHolder;
typedef IcuHolder<UNumberFormat, UNumberFormatCloser> UNumberFormatHolder;
typedef IcuHolder<ULocaleDisplayNames, ULocaleDisplayNamesCloser> ULocaleDisplayNamesHolder;
typedef IcuHolder<UResourceBundle, UResourceBundleCloser> UResourceBundleHolder;
