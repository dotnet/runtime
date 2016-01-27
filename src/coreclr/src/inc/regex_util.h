// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// See regex_base.h for more information.
//
// This header creates some concrete instantiations of RegExBase for commonly used scenarios. In
// particular, basic regular expression matching base on the regular expression language described in
// clr::regex::ItemTraitsBase (found in regex_base.h) is instantiated for use with SString, ASCII and
// UNICODE strings (clr::regex::SStringRegex, clr::regex::WSTRRegEx, and clr::regex::STRRegEx
// respectively). Each type definition includes an example of its use.
//

//

#ifndef _REGEX_UTIL_H_
#define _REGEX_UTIL_H_

#ifndef MODE_ANY
#define MODE_ANY
#endif

#include "regex_base.h"

#ifdef _DEBUG

namespace clr
{
namespace regex
{

//=======================================================================================================
// Derives from Group to provide two additional convenience methods (GetSString variants).

class SStringGroup : public Group<SString::CIterator>
{
public:
    SStringGroup()
        : Group<SString::CIterator>()
        { WRAPPER_NO_CONTRACT; }

    SStringGroup(const InputIterator& _start, const InputIterator& _end, bool _isClosed = false)
        : Group<SString::CIterator>(_start, _end, _isClosed)
        { WRAPPER_NO_CONTRACT; }

    // Since SStrings constructed from ranges require the original source string, this is a required
    // input argument. Returns the input substring that matches the corresponding grouping.
    SString GetSString(const SString& src)
        { WRAPPER_NO_CONTRACT; return SString(src, Begin(), End()); }

    // Since SStrings constructed from ranges require the original source string, this is a required
    // input argument. This version takes a target SString as a buffer, and also returns this buffer
    // as a reference. Returns the input substring that matches the corresponding grouping.
    SString& GetSString(const SString& src, SString& tgt)
        { WRAPPER_NO_CONTRACT; tgt.Set(src, Begin(), End()); return tgt; }
};

//=======================================================================================================
typedef WCHARItemTraits<SString::CIterator> SStringItemTraits;

//=======================================================================================================
// Regular expression matching with SStrings.
//
// Here is an example of how to use SStringRegEx with grouping enabled.
//
//      using namespace clr::regex;
//      SString input(SL"Simmons"); // usually this is derived from some variable source
//      SStringRegEx::GroupingContainer container;
//      if (SStringRegEx::Match(SL"(Sim+on)", input, container)) {
//          printf("%S", container[1].GetSString(input).GetUnicode());
//      }
//
//      This sample should result in "Simmon" being printed.


class SStringRegEx : public RegExBase<SStringItemTraits>
{
    typedef RegExBase<SStringItemTraits> PARENT_TYPE;

public:
    using PARENT_TYPE::Match;
    using PARENT_TYPE::Matches;

    typedef PARENT_TYPE::InputIterator InputIterator;

    typedef GroupContainer<InputIterator, SStringGroup > GroupingContainer;

    static bool Match(
        const SString&            regex,
        const SString&            input,
        GroupingContainer& groups,
        MatchFlags                flags = DefaultMatchFlags)
    {
        WRAPPER_NO_CONTRACT;
        return Match(regex.Begin(), regex.End(), input.Begin(), input.End(), groups, flags);
    }

    static bool Matches(
        const SString&            regex,
        const SString&            input,
        MatchFlags                flags = DefaultMatchFlags)
    {
        WRAPPER_NO_CONTRACT;
        return Matches(regex.Begin(), regex.End(), input.Begin(), input.End(), flags);
    }

};

//=======================================================================================================
// Regular expression matching with UNICODE strings.
//
// Here is an example of how to use WSTRRegEx to match against a null-terminated string without grouping.
//
//      using namespace clr::regex;
//      LPCWSTR input = L"Simmons";
//      if (WSTRRegEx::Matches(L"Sim+on", input))
//          printf("Match succeeded");
//      else
//          printf("Match failed");
//
//      This sample should result in "Match succeeded" being printed.

class WSTRRegEx : public RegExBase<WCHARItemTraits<LPCWSTR> >
{
    typedef RegExBase<WCHARItemTraits<LPCWSTR> > PARENT_TYPE;

public:
    using PARENT_TYPE::Match;
    using PARENT_TYPE::Matches;

    static bool Match(
        LPCWSTR                   regex,
        LPCWSTR                   input,
        GroupingContainer& groups,
        MatchFlags                flags = DefaultMatchFlags)
    {
        WRAPPER_NO_CONTRACT;
        return Match(regex, regex + wcslen(regex), input, input + wcslen(input), groups, flags);
    }

    static bool Matches(
        LPCWSTR                   regex,
        LPCWSTR                   input,
        MatchFlags                flags = DefaultMatchFlags)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        return Matches(regex, regex + wcslen(regex), input, input + wcslen(input), flags);
    }
};

//=======================================================================================================
// Regular expression matching with ASCII strings.
//
// Here is an example of how to use STRRegEx to match against a substring based on begin and end range
// pointers, with grouping disabled and case insensitivity enabled.
//
//      using namespace clr::regex;
//      LPCSTR input = "123Simmons456";
//      if (STRRegEx::Matches("Sim+on", input+3, input+10, STRRegEx::MF_CASE_INSENSITIVE))
//          printf("Match succeeded");
//      else
//          printf("Match failed");
//
//      This sample should result in "Match succeeded" being printed.

class STRRegEx : public RegExBase<CHARItemTraits<LPCSTR> >
{
    typedef RegExBase<CHARItemTraits<LPCSTR> > PARENT_TYPE;

public:
    using PARENT_TYPE::Match;
    using PARENT_TYPE::Matches;

    static bool Match(
        LPCSTR                    regex,
        LPCSTR                    input,
        GroupingContainer& groups,
        MatchFlags                flags = DefaultMatchFlags)
    {
        WRAPPER_NO_CONTRACT;
        return Match(regex, regex + strlen(regex), input, input + strlen(input), groups, flags);
    }

    static bool Matches(
        LPCSTR                    regex,
        LPCSTR                    input,
        MatchFlags                flags = DefaultMatchFlags)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        return Matches(regex, regex + strlen(regex), input, input + strlen(input), flags);
    }
};

} // namespace regex
} // namespace clr

#endif // _DEBUG

#endif // _REGEX_UTIL_H_
