//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "unicode/ucal.h"
#include "unicode/uenum.h"

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
    IcuHolder(const IcuHolder&);
    IcuHolder operator=(const IcuHolder&);
};

struct UCalendarCloser
{
  public:
    void operator()(UCalendar* pCal) const
    {
        ucal_close(pCal);
    }
};

struct UEnumerationCloser
{
  public:
    void operator()(UEnumeration* pEnum) const
    {
        uenum_close(pEnum);
    }
};

typedef IcuHolder<UCalendar, UCalendarCloser> UCalendarHolder;
typedef IcuHolder<UEnumeration, UEnumerationCloser> UEnumerationHolder;
