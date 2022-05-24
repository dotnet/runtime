// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// See regex_base.h for more information.
//
// This header creates some concrete instantiations of RegExBase for commonly used scenarios
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
