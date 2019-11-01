// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Provides basic interpreted regular expression matching. This is meant as a debugging tool,
// and if regular expressions become necessary in a non-debug scenario great care should be
// used to ensure that performance is not impaired, and a more thorough review of this could
// would also be a good thing. This file does not include any concrete instantiations but
// instead provides the basic building blocks. Some concrete instantiations can be found in
// regex_util.h.
//
// NOTE: See code:clr::regex::RegExBase (below) for description of supported regex language.
//
// NOTE: we had to forego standard options such as tr1::regex
//       (http://en.wikipedia.org/wiki/Technical_Report_1#Regular_Expressions) and Microsoft's
//       internal GRETA regular expressions (http://toolbox/sites/987/default.aspx) because they
//       both rely heavily on the STL, which can not currently be used within the CLR.
//
// NOTE: If this becomes non-debug-only, then read the comment on WCHARItemTraits for what
//       what needs fixing.
//

//

#ifndef _REGEX_BASE_H_
#define _REGEX_BASE_H_

// Forward declare namespace so that it is not debug-only (even if currently there is nothing
// but debug-only code in the namespace). This enables a "using namespace clr;" line in a
// header file without having to worry about whether or not it's in a debug-only block.
namespace clr {}

#ifdef _DEBUG

#include "utilcode.h" // for string hash functions
#include "sstring.h"

namespace clr {
namespace regex {

// Implementation details. Code contained in any "imp" namespace should never be directly used
// by clients of RegEx.
namespace imp {

    //===================================================================================================
    // Helper for clr::regex::RegExBase. See class definition for clr::regex::RegExBase below for more
    // information.
    
    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    class RegExBaseHelper : protected ITEM_TRAITS
    {
    public:
        typedef typename ITEM_TRAITS::RegexIterator RegexIterator;
        typedef typename ITEM_TRAITS::InputIterator InputIterator;

        typedef typename ITEM_TRAITS::MatchFlags MatchFlags;
        static const MatchFlags DefaultMatchFlags = ITEM_TRAITS::DefaultMatchFlags;

        // Arguments:
        //  regex       : marks the start of the regular expression string.
        //  regexEnd    : marks the end of the regular expression string.
        //  input       : marks the start of the input string against which regex will be matched.
        //  inputEnd    : marks the end of the input string.
        //  groups      : recipient of regular expression groups.
        //
        // Returns true if the regular expression was successfully matched against the input string;
        // otherwise false.

        RegExBaseHelper(const RegexIterator& regex, 
                        const RegexIterator& regexEnd,
                        const InputIterator& input,
                        const InputIterator& inputEnd,
                        GROUP_CONTAINER&     groups,
                        const MatchFlags     flags = DefaultMatchFlags);

        // The main entrypoint to RegExBaseHelper, Match will attempt to match the regular expression
        // defined by [regex,regexEnd) against the input defined by [input,inputEnd).
        bool Match()
            { WRAPPER_NO_CONTRACT; return DoMatch(m_regex, m_input); }

    protected:
        typedef typename ITEM_TRAITS::Item Item;
        typedef typename ITEM_TRAITS::ItemType ItemType;

        // Try to match regex at any point within input, starting with the first character and moving
        // along one at a time until a match is found or the end of the input is encountered, whichever
        // comes first.
        bool DoMatch(
            const RegexIterator& regex,
            InputIterator input);

        // Try to match regex starting exactly at input.
        bool DoMatchHere(
            const RegexIterator& regex,
            const InputIterator& input);

        // The function returns true if a match is found consisting of zero or more items c followed by a
        // successful match of regex on the remaining input; otherwise false is returned. This is a
        // conservative match, so it starts with trying to match zero items followed by regex,
        // and will then try to match one item followed by regex. 
        bool DoMatchStar(
            const Item& c,
            const RegexIterator& regex,
            InputIterator input);

        // The function returns true if a match is found consisting of zero or more items c followed by a
        // successful match of regex on the remaining input; otherwise false is returned. This is a
        // greedy match, so it starts with trying to match as many items as it can followed by regex,
        // and on failure will try again with one less items matched.
        bool DoMatchStarEagerly(
            const Item& c,
            const RegexIterator& regex,
            InputIterator input);

        // Convenience method.
        Item GetItem(
            const RegexIterator &regex)
            { WRAPPER_NO_CONTRACT; return ITEM_TRAITS::GetItem(regex, m_regexEnd, m_flags); }

        // Convenience method.
        bool MatchItem(
            const Item& c,
            const InputIterator& input)
            { WRAPPER_NO_CONTRACT; return ITEM_TRAITS::MatchItem(c, input, m_inputEnd, m_flags); }

        // Declared as protected to prevent direct instantiation.
        RegExBaseHelper()
            {}

        RegexIterator    m_regex;
        RegexIterator    m_regexEnd;
        InputIterator    m_input;
        InputIterator    m_inputEnd;
        GROUP_CONTAINER& m_groups;
        MatchFlags       m_flags;
    };

    //---------------------------------------------------------------------------------------------------
    // This method simply stores the end iterators for the regular expression and the input strings, as
    // well as the group collection object and flags, and forwards the call to DoMatch.

    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>::RegExBaseHelper(
        const RegexIterator& regex, 
        const RegexIterator& regexEnd,
        const InputIterator& input,
        const InputIterator& inputEnd,
        GROUP_CONTAINER&     groups,
        const MatchFlags     flags)
            : m_regex(regex),
              m_regexEnd(regexEnd),
              m_input(input),
              m_inputEnd(inputEnd),
              m_groups(groups),
              m_flags(flags)
        { WRAPPER_NO_CONTRACT; }

    //---------------------------------------------------------------------------------------------------
    // This method checks if the regular expression starts with a caret, indicating that any match must
    // be anchored at the start of the input string. If such a caret exists, one match attempt is made
    // on the input starting with the first character and the result is returned. If the regex does not
    // start with a caret, the method attempts to match against the input string, starting at the first
    // character and moving one character over for each successive attempt, until a match is found or
    // the end of the input is encountered, whichever comes first.

    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    inline bool
    RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>::DoMatch(
        const RegexIterator& regex,
        InputIterator input)
    {
        WRAPPER_NO_CONTRACT;

        if (GetItem(regex).GetType() == ITEM_TRAITS::CARET)
        {   // Match must occur from the beginning of the line
            m_groups.OpenGroup(input, m_inputEnd);
            bool res = DoMatchHere(regex+1, input);
            if (!res)
                m_groups.CancelGroup();
            return res;
        }
        else
        {   // Match can happen anywhere in the string
            do
            {   // Attempt to match against each substring [x,inputEnd) for x = 0...inputEnd

                // Begin the group that contains the entire match.
                m_groups.OpenGroup(input, m_inputEnd);

                if (DoMatchHere(regex, input))
                {   // Success. Note that the entire match group is closed elsewhere on a
                    // successful match.
                    return true;
                }

                // On failure, cancel the group so that it can be reopened on the new substring
                m_groups.CancelGroup();
            } while (input++ != m_inputEnd);
        }

        // No successful match found.
        return false;
    }

    //-------------------------------------------------------------------------------------------------------
    // This is the main loop, which handles grouping constructs, repetition directives (*, *?, +, +?), and
    // EOL matches ($), delegating all character matching to ITEM_TRAITS::MatchItem
    // The general algorithm is:
    //      1. Get the next item.
    //      2. If the item is a DOLLAR type, check to see if we're at the end of the retular expression and
    //         the input string, and if so return success.
    //      3. If the item is a grouping construct, open or close the appropriate group and continue matching.
    //         On failure, roll back the grouping change so that subsequent attemts will have correct state.
    //      4. Check to see if the item following the current is a repetition directive, and if so take the
    //         appropriate action.
    //      5. Otherwise defer to ITEM_TRAITS::MatchItem and if successful continue to match the remaining
    //         regular expression and input string; otherwise return failure.

    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    inline bool
    RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>::DoMatchHere(
        const RegexIterator& regex,
        const InputIterator& input)
    {
        WRAPPER_NO_CONTRACT;

        if (regex == m_regexEnd)
        {   // Reached the end of the regular expression without ever returning false,
            // implying a successful match. Close the overall match group and return.
            m_groups.CloseGroup(input);
            return true;
        }

        Item c0 = GetItem(regex);
        if (c0.GetType() == ITEM_TRAITS::DOLLAR && (c0.GetNext() == m_regexEnd))
        {   // Matches EOL if a '$' is encountered at the end of the input.
            m_groups.CloseGroup(input);
            // Success only if we're actually at the end of the input string.
            return input == m_inputEnd;
        }
        if (c0.GetType() == ITEM_TRAITS::PAREN_OPEN)
        {   // Encountered an open parenthesis ('('); open a new grouping.
            m_groups.OpenGroup(input, m_inputEnd);
            bool res = DoMatchHere(c0.GetNext(), input);
            if (!res)
            {   // If a match fails, there could be further attempts (such as if
                // there is an active repetition matching frame beneath us), so
                // need to cancel the group we just opened so that the grouping
                // structure remains consistent.
                m_groups.CancelGroup();
            }
            return res;
        }
        if (c0.GetType() == ITEM_TRAITS::PAREN_CLOSE)
        {   // Close the most recent open grouping.
            COUNT_T i = m_groups.CloseGroup(input);
            bool res = DoMatchHere(c0.GetNext(), input);
            if (!res)
            {   // For the same reasons as the need to cancel an opened group
                // explained above, we need to reopen the closed group if a
                // match fails.
                m_groups.ReopenGroup(i, m_inputEnd);
            }
            return res;
        }

        if (c0.GetNext() != m_regexEnd)
        {   // If there is another item in the regex string following the current one, get
            // it to see if it is a repetition matching directive.
            Item c1 = GetItem(c0.GetNext());
            if (c1.GetType() == ITEM_TRAITS::STAR)
            {   // '*' matching directive encountered
                if (c1.GetNext() != m_regexEnd)
                {
                    Item c2 = GetItem(c1.GetNext());
                    if (c2.GetType() == ITEM_TRAITS::QUESTION_MARK)
                    {   // conservative matching semantics requested
                        return DoMatchStar(c0, c2.GetNext(), input);
                    }
                }
                // Eager matching
                return DoMatchStarEagerly(c0, c1.GetNext(), input);
            }
            if (c1.GetType() == ITEM_TRAITS::PLUS)
            {   // '+' matching directive encountered.
                if (c1.GetNext() != m_regexEnd)
                {
                    Item c2 = GetItem(c1.GetNext());
                    if (c2.GetType() == ITEM_TRAITS::QUESTION_MARK)
                    {   // conservative matching semantics requested
                        return MatchItem(c0, input) && DoMatchStar(c0, c2.GetNext(), input+1);
                    }
                }
                // Eager matching
                return MatchItem(c0, input) && DoMatchStarEagerly(c0, c1.GetNext(), input+1);
            }
            if (c1.GetType() == ITEM_TRAITS::QUESTION_MARK)
            {   // '?' matching directive encountered
                return (MatchItem(c0, input) && DoMatchHere(c1.GetNext(), input+1)) || DoMatchHere(c1.GetNext(), input);
            }
        }

        // No special matching semantics encountered, delegate the matching to ITEM_TRAITS::MatchItem
        return MatchItem(c0, input) && DoMatchHere(c0.GetNext(), input+1);
    }

    //-------------------------------------------------------------------------------------------------------
    // Conservative '*' repetition matching. This attempts to match zero items c followed by a match of
    // regex. If this fails, attempt to match one item c followed by a match of regex. Repeat until item c
    // does not match or a successful match is found.

    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    inline bool
    RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>::DoMatchStar(
        const Item& c,
        const RegexIterator& regex,
        InputIterator input)
    {
        WRAPPER_NO_CONTRACT;
        CONSISTENCY_CHECK(input != m_inputEnd);

        do {
            if (DoMatchHere(regex, input))
            {   // A successful match is found!
                return true;
            }
            // No successful match, so try to match one more item and then attempt to match regex on the
            // remaining input.
        } while (input != m_inputEnd && MatchItem(c, input++));
        return false;
    }

    //-------------------------------------------------------------------------------------------------------
    // Similar to DoMatchStar above, except this algorithm matches as many items c as possible first followed
    // by regex on the remaining input, and on failure tries again with a match against one less item c
    // followed by regex on the remaining input, and repeats until there are no items c remaining to match
    // and the zero item match followed by a match of regex on the entire remaining input fails. If any of
    // the match attempts succeed, return success.

    template <typename ITEM_TRAITS, typename GROUP_CONTAINER>
    inline bool
    RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>::DoMatchStarEagerly(
        const Item& c,
        const RegexIterator& regex,
        InputIterator input)
    {
        WRAPPER_NO_CONTRACT;

        // Make sure we keep a hold of how far back we can unwind.
        InputIterator inputOrig = input;

        // First, determine the maximum number of matches against item c.
        while (input != m_inputEnd && MatchItem(c, input))
        {
            ++input;
        }

        do
        {   // Work backwards from the maximum number of matches of item c until a match is found
            // or until we have backed right up to the starting value of input (saved in inputOrig),
            // at which time we admit failure.
            if (DoMatchHere(regex, input))
                return true;
        } while (inputOrig != input--);
        return false;
    }

} // namespace imp

//=======================================================================================================
// Represents a matched group using iterators to denote the string contained by [Begin(),End()).

template<typename INPUT_ITERATOR>
class Group
{
public:
    typedef INPUT_ITERATOR InputIterator;

    //
    // Functions for accessing group properties
    //

    // Returns the iterator indicating the start of the group
    const InputIterator& Begin() const
        { LIMITED_METHOD_CONTRACT; return m_begin; }

    // Returns the iterator indicating the first non-member of the group
    const InputIterator& End() const
        { LIMITED_METHOD_CONTRACT; return m_end; }

    // It is possible that m_end - m_begin could be greater than the maximum of COUNT_T. m_end and
    // m_begin are the end and start of a string, so is entirely unlikely to overflow a COUNT_T.
    // Conbined with the fact that this is debug-only code, opting not to replace all COUNT_T
    // uses with SIZE_T.
    COUNT_T Length() const
        { WRAPPER_NO_CONTRACT; return static_cast<COUNT_T>(m_end - m_begin); }

    //
    // Functions used by RegExBaseHelper to create grouping constructs.
    //

    Group()
        : m_isClosed(false), m_begin(), m_end()
        { WRAPPER_NO_CONTRACT; }

    Group(const InputIterator& start, const InputIterator& end, bool isClosed = false)
        : m_isClosed(isClosed), m_begin(start), m_end(end)
        { WRAPPER_NO_CONTRACT; }

    void SetBegin(const InputIterator& start)
        { WRAPPER_NO_CONTRACT; m_begin = start; }

    void SetEnd(const InputIterator& end)
        { WRAPPER_NO_CONTRACT; m_end = end; }

    bool IsClosed() const
        { LIMITED_METHOD_CONTRACT; return m_isClosed; }

    void SetIsClosed(bool isClosed)
        { WRAPPER_NO_CONTRACT; m_isClosed = isClosed; }

protected:
    bool           m_isClosed;
    InputIterator  m_begin;
    InputIterator  m_end;
};

//=======================================================================================================
// Represents a generic container of groups, defaulting to using Group<INPUT_ITERATOR> as its element
// type. This container satisfies the method requrements of RegExBase. When a match is successful, the
// match groups may be accessed using the index operator or the iterators definin the matched groups
// [Begin(), End()).

template <typename INPUT_ITERATOR, typename GROUP_TYPE = Group<INPUT_ITERATOR> >
class GroupContainer
{
public:
    typedef typename SArray<GROUP_TYPE>::Iterator Iterator;

    //
    // Functions for enumerating groups
    //

    GROUP_TYPE & operator[](COUNT_T idx)
    {
        WRAPPER_NO_CONTRACT;
        CONSISTENCY_CHECK(((COUNT_T)(COUNT_T)idx) == idx);
        return m_array[idx];
    }

    // Returns an iterator to the first matched group (which is always the string for the
    // entire successfully matched string. Specific groups start at Begin()+1 and continue
    // to End()-1.
    Iterator Begin()
        { WRAPPER_NO_CONTRACT; return m_array.Begin(); }

    // Returns the first invalid iterator value.
    Iterator End()
        { WRAPPER_NO_CONTRACT; return m_array.End(); }

    // 
    COUNT_T Count() const
        { WRAPPER_NO_CONTRACT; return m_array.GetCount(); }

    //
    // Functions used by RegExBaseHelper to create grouping constructs.
    //

    // Note: OpenGroup takes an end iterator so that the group will have a valid (if possibly
    // incorrect) endpoint in the case that the regular expression has unbalanced grouping
    // parentheses.
    void OpenGroup(const INPUT_ITERATOR& start, const INPUT_ITERATOR& end)
        { WRAPPER_NO_CONTRACT; m_array.Append(GROUP_TYPE(start, end, false)); }

    COUNT_T CloseGroup(const INPUT_ITERATOR& end);

    void ReopenGroup(COUNT_T i, const INPUT_ITERATOR& end);

    void CancelGroup()
        { WRAPPER_NO_CONTRACT; m_array.Delete(m_array.End() - 1); }

private:
    SArray<GROUP_TYPE> m_array;
};

//-------------------------------------------------------------------------------------------------------
// Works backwards from the most recently created group looking for an open group to close. Returns
// the index of the group that was closed, which is used in the event that a group needs to be
// reopened.

template <typename INPUT_ITERATOR, typename GROUP_TYPE>
COUNT_T
GroupContainer<INPUT_ITERATOR, GROUP_TYPE>::CloseGroup(
    const INPUT_ITERATOR& end)
{
    WRAPPER_NO_CONTRACT;

    for (COUNT_T i = (COUNT_T)Count(); i > 0; --i)
    {
        if (!m_array[i-1].IsClosed())
        {
            m_array[i-1].SetEnd(end);
            m_array[i-1].SetIsClosed(true);
            return i-1;
        }
    }

    _ASSERTE(!"Unmatched grouping constructs!");
    return 0;
}

//-------------------------------------------------------------------------------------------------------
// Reopen a group at the given index, using 'end' to overwrite the current end.

template <typename INPUT_ITERATOR, typename GROUP_TYPE>
void 
GroupContainer<INPUT_ITERATOR, GROUP_TYPE>::ReopenGroup(
    COUNT_T i,
    const INPUT_ITERATOR& end)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(i > 0 && i < Count());

    if (i > 0 && i < Count())
    {
        m_array[i].SetEnd(end);
        m_array[i].SetIsClosed(false);
    }
}

//=======================================================================================================
// Empty group container that satisfies the method requirements of RegExBase but has empty bodies. This
// allows for non-allocating matches when grouping is not required.

template <typename INPUT_ITERATOR>
class NullGroupContainer
{
public:
    void OpenGroup(INPUT_ITERATOR, INPUT_ITERATOR) {}
    COUNT_T CloseGroup(INPUT_ITERATOR) { return 0; }
    void ReopenGroup(COUNT_T, INPUT_ITERATOR) {}
    void CancelGroup() {}
};

//=======================================================================================================
// This mini-implementation of regular expression matching supports the
// following constructs:
//     ^   matches the beginning of the input string
//     $   matches the end of the input string
//     *   matches zero or more occurrences of the previous item eagerly
//     *?  matches zero or more occurrences of the previous item conservatively
//     +   matches 1 or more occurrences of the previous item eagerly
//     +?  matches 1 or more occurrences of the previous item conservatively
//     ?   matches 0 or 1 occurences of the previous item
//     (   starts a grouping
//     )   ends a grouping
//
// IMPORTANT: These are just anchoring and grouping constructs. See the definition for ItemTraitsBase
//            below for information on the default character classes that are supported. (The intent of
//            this separation is to allow customization of the character classes where required.)

// ITEM_TRAITS provides traits for individual tokens in a regular expression, as well as a mechanism for
// matching said individual components with the target string. RegexBase derives from ITEM_TRAITS in a
// protected fashion, and is responsible for providing the following:
//     1. "RegexIterator" typedef
//          Used as an iterator into the regular expression, and used as arguments to indicate the start
//          and the end of the regular expression string.
//     2. "InputIterator" typedef
//          Used as an iterator into the input string, and used as arguments to indicate the start
//          and the end of the input string.
//        (NOTE: RegexIterator and InputIterator are often typedef'ed to be the same thing.)
//     3. "Item" typedef.
//          This will be used with methods GetItem and MatchItem (see below). Item must
//          define the the following methods:
//             ItemType GetType() : returns the type of the item. See below for explanation of ItemType
//             const RegexIterator& GetNext() : iterator pointing to the start of the next item.
//     4. "MatchFlags" typedef, and "static const DefaultMatchFlags" value.
//          Provided for calls to "Match" and "Matches", and passed on to calls "GetItem" and "MatchItem".
//     5. enum ItemType
//          Defines the following minimum values:
//             DOT
//             CARET
//             DOLLAR
//             STAR
//             QUESTION_MARK
//             PLUS
//             PAREN_OPEN
//             PAREN_CLOSE
//        ItemType may include more values, and may even choose to ignore the above enum types, all of 
//        which must be recognized by GetItem and MatchItem (see below).
//     6. static Item GetItem(const RegexIterator& regex,
//                            const RegexIterator& regexEnd,
//                            const MatchFlags& flags)
//          This method takes a regular expression iterator and returns the next regular expression
//          element (Item) pointed to by the iterator.
//     7. static bool MatchItem(const Item& c,
//                              const InputIterator& input,
//                              const InputIterator& inputEnd,
//                              const MatchFlags &flags)

// GROUP_CONTAINER provides functionality for keeping track of regular expression groups. This is a generic
// argument to Match, and the type of the object must support the following methods:
//     1. void OpenGroup(const InputIterator& start, const InputIterator& end);
//         Called when a PAREN_OPEN item is encountered.
//     2. COUNT_T CloseGroup(const InputIterator& end);
//         Called when a PAREN_CLOSE item is encountered. Returns the index of the group that was closed.
//     3. void ReopenGroup(COUNT_T i, const InputIterator& end);
//         Called when a match following a call to CloseGroup fails, essentially requesting a rollback
//         of the call to CloseGroup.
//     4. void CancelGroup();
//         Called when a match following a call to OpenGroup fails, essentially requesting a rollback
//         of the call to OpenGroup.

template <typename ITEM_TRAITS>
class RegExBase : public ITEM_TRAITS
{
public:
    typedef typename ITEM_TRAITS::RegexIterator RegexIterator;
    typedef typename ITEM_TRAITS::InputIterator InputIterator;

    // This is a convenience typedef that allows a caller to easily declare a grouping container
    // to be passed to a call to Match. An example would be (see regex_util.h for a definition of
    // SStringRegEx):
    //
    //      SString input(SL"Simmons");
    //      SStringRegEx::GroupingContainer container;
    //      if (SStringRegEx::Match(SL"(Sim+on)", input, container)) {
    //          printf("%S", container[1].GetSString(input).GetUnicode());
    //      }
    //
    typedef GroupContainer<InputIterator, Group<InputIterator> > GroupingContainer;

    // Pulls down the typedef for MatchFlags and initialized a static representing the default flags.
    typedef typename ITEM_TRAITS::MatchFlags MatchFlags;
    static const MatchFlags DefaultMatchFlags = ITEM_TRAITS::DefaultMatchFlags;

    template <typename GROUP_CONTAINER>
    static bool Match(RegexIterator    regex, 
                      RegexIterator    regexEnd,
                      InputIterator    input,
                      InputIterator    inputEnd,
                      GROUP_CONTAINER& groups,
                      MatchFlags       flags = DefaultMatchFlags)
    {
        imp::RegExBaseHelper<ITEM_TRAITS, GROUP_CONTAINER>
            re(regex, regexEnd, input, inputEnd, groups, flags);
        return re.Match();
    }

    static bool Matches(RegexIterator    regex, 
                        RegexIterator    regexEnd,
                        InputIterator    input,
                        InputIterator    inputEnd,
                        MatchFlags       flags = DefaultMatchFlags)
    {
        NullGroupContainer<InputIterator> ngc;
        return Match(regex, regexEnd, input, inputEnd, ngc, flags);
    }
};

//=======================================================================================================
// In addition to requirements specified on RegExBase, StandardItemTraits provides the following
// additinal regular expression item types.
//     c   matches any literal character c
//     .   matches any single character
//     \d  any literal digit character
//     \w  any alpha character
//     \s  any whitespace character
//
// Child types of ItemTraitsBase must implement GetItem and MatchItem (see below for full
// signature requirements). Current child type implementations permit a backslash ('\') to escape
// special characters ('.', '$', '*', etc.) and allow them to be interpreted as literal characters.
//
// This type describes a particular behaviour, but must be subtyped for the particular target character
// needed, and GetItem and MatchItem must be implemented.
//

template <typename ITERATOR_TYPE, typename CHAR_TYPE>
class ItemTraitsBase
{
public:
    typedef ITERATOR_TYPE RegexIterator;
    typedef ITERATOR_TYPE InputIterator;

    enum MatchFlags
    {
        MF_NONE                 = 0x00,
        MF_CASE_INSENSITIVE     = 0x01      // Match character literals as case insensitive.
    };

    static const MatchFlags DefaultMatchFlags = MF_NONE;

protected:
    ItemTraitsBase()
        {}

    enum ItemType
    {
        // REQUIRED, as described in RegExBase
        CARET,
        DOLLAR,
        STAR,
        QUESTION_MARK,
        PLUS,
        PAREN_OPEN,
        PAREN_CLOSE,
        // ADDITIONAL
        DOT,        // any literal character
        DIGIT,      // any digit
        ALPHA,      // any alpha character, upper or lower case
        WHITESPACE, // any whitespace character
        NON_WHITESPACE, // any non-whitespace character
        CHARACTER,  // a specific literal character
    };

    class Item
    {
    public:
        Item(ItemType type, CHAR_TYPE val, const RegexIterator& next)
            : m_type(type), m_val(val), m_next(next)
            { WRAPPER_NO_CONTRACT; }

        Item(ItemType type, const RegexIterator& next)
            : m_type(type), m_val(0), m_next(next)
            { WRAPPER_NO_CONTRACT; }

        ItemType GetType() const
            { LIMITED_METHOD_CONTRACT; return m_type; }

        const RegexIterator& GetNext() const
            { LIMITED_METHOD_CONTRACT; return m_next; }

        CHAR_TYPE GetValue() const
            { LIMITED_METHOD_CONTRACT; return m_val; }

    protected:
        ItemType         m_type;
        CHAR_TYPE        m_val;
        RegexIterator    m_next;
    };

    // All deriving types must add the following methods:
    //   static Item GetItem(const RegexIterator& regex, const RegexIterator& regexEnd);
    //   static bool MatchItem(const Item& c, const InputIterator& input, const InputIterator& inputEnd);
};

//=======================================================================================================
// Implements ItemTraitsBase, provides matching for UNICODE characters.
//
// !!!IMPORTANT!!!
// This is not a complete unicode implementation - only the equivalent of ASCII alpha characters are
// consider to be part of the alpha set, and this is also the only set on which case insensitive
// operations will correctly work. If RegEx is moved out of DEBUG ONLY, then this will have to be fixed
// to properly address these issues.
// !!!IMPORTANT!!!

template <typename ITERATOR_TYPE>
class WCHARItemTraits : public ItemTraitsBase<ITERATOR_TYPE, WCHAR>
{
public:
    typedef ItemTraitsBase<ITERATOR_TYPE, WCHAR> PARENT_TYPE;
    typedef typename PARENT_TYPE::RegexIterator RegexIterator;
    typedef typename PARENT_TYPE::InputIterator InputIterator;
    typedef typename PARENT_TYPE::Item          Item;
    typedef typename PARENT_TYPE::MatchFlags    MatchFlags;

    static Item GetItem(const RegexIterator& regex, const RegexIterator& regexEnd, MatchFlags flags);
    static bool MatchItem(const Item& c, const InputIterator& input, const InputIterator& inputEnd, MatchFlags flags);

protected:
    WCHARItemTraits()
        {}

private:
    static inline bool IS_UPPER_A_TO_Z(WCHAR x)
        { WRAPPER_NO_CONTRACT; return (((x) >= W('A')) && ((x) <= W('Z'))); }

    static inline bool IS_LOWER_A_TO_Z(WCHAR x)
        { WRAPPER_NO_CONTRACT; return (((x) >= W('a')) && ((x) <= W('z'))); }

    static inline WCHAR UPCASE(WCHAR x)
        { WRAPPER_NO_CONTRACT; return (IS_LOWER_A_TO_Z(x) ? ((x) - W('a') + W('A')) : (x)); }

    static inline WCHAR DOWNCASE(WCHAR x)
        { WRAPPER_NO_CONTRACT; return (IS_UPPER_A_TO_Z(x) ? ((x) - W('A') + W('a')) : (x)); }

    static bool MatchCharacter(WCHAR c1, WCHAR c2, MatchFlags flags)
        { WRAPPER_NO_CONTRACT; return (flags & PARENT_TYPE::MF_CASE_INSENSITIVE) ? (DOWNCASE(c1) == DOWNCASE(c2)) : (c1 == c2); }
};

//-------------------------------------------------------------------------------------------------------
// Reads the next item from regex, recognizing special characters outlined in ItemTraitsBase.

template <typename ITERATOR_TYPE>
typename WCHARItemTraits<ITERATOR_TYPE>::Item
WCHARItemTraits<ITERATOR_TYPE>::GetItem(
    const RegexIterator& regex,
    const RegexIterator& regexEnd,
    MatchFlags           flags)
{
    WRAPPER_NO_CONTRACT;

    if (*regex == W('\\'))
    {
        const RegexIterator regexNext = regex+1;
        if (regexNext == regexEnd)
            return Item(PARENT_TYPE::CHARACTER, W('\\'), regexNext);
        if (*regexNext == W('d'))
            return Item(PARENT_TYPE::DIGIT, regexNext+1);
        if (*regexNext == W('w'))
            return Item(PARENT_TYPE::ALPHA, regexNext+1);
        if (*regexNext == W('s'))
            return Item(PARENT_TYPE::WHITESPACE, regexNext+1);
        if (*regexNext == W('S'))
            return Item(PARENT_TYPE::NON_WHITESPACE, regexNext+1);
        return Item(PARENT_TYPE::CHARACTER, *regexNext, regexNext+1);
    }
    else if (*regex == W('.'))
        return Item(PARENT_TYPE::DOT, W('.'), regex+1);
    else if (*regex == W('^'))
        return Item(PARENT_TYPE::CARET, W('^'), regex+1);
    else if (*regex == W('$'))
        return Item(PARENT_TYPE::DOLLAR, W('$'), regex+1);
    else if (*regex == W('*'))
        return Item(PARENT_TYPE::STAR, W('*'), regex+1);
    else if (*regex == W('?'))
        return Item(PARENT_TYPE::QUESTION_MARK, W('?'), regex+1);
    else if (*regex == W('+'))
        return Item(PARENT_TYPE::PLUS, W('+'), regex+1);
    else if (*regex == W('('))
        return Item(PARENT_TYPE::PAREN_OPEN, W('('), regex+1);
    else if (*regex == W(')'))
        return Item(PARENT_TYPE::PAREN_CLOSE, W(')'), regex+1);
    else
        return Item(PARENT_TYPE::CHARACTER, *regex, regex + 1);
}

//-------------------------------------------------------------------------------------------------------
// Returns true if the next character point to by input matches the character class described by c.

template <typename ITERATOR_TYPE>
bool
WCHARItemTraits<ITERATOR_TYPE>::MatchItem(
    const Item& c,
    const InputIterator& input,
    const InputIterator& inputEnd,
    MatchFlags           flags)
{
    WRAPPER_NO_CONTRACT;

    if (c.GetType() == PARENT_TYPE::DIGIT)
        return *input >= W('0') && *input <= W('9');
    else if (c.GetType() == PARENT_TYPE::ALPHA)
        return (*input >= W('a') && *input <= W('z')) || (*input >= W('A') && *input <= W('Z'));
    else if (c.GetType() == PARENT_TYPE::WHITESPACE)
        return *input == W(' ') || *input == W('\t');
    else if (c.GetType() == PARENT_TYPE::NON_WHITESPACE)
        return !(*input == W(' ') || *input == W('\t'));
    else
        return c.GetType() == PARENT_TYPE::DOT || MatchCharacter(c.GetValue(), *input, flags);
}

//=======================================================================================================
// Implements ItemTraitsBase, provides matching for ASCII (*not* UTF8) characters.

template <typename ITERATOR_TYPE>
class CHARItemTraits : public ItemTraitsBase<ITERATOR_TYPE, CHAR>
{
public:
    typedef ItemTraitsBase<ITERATOR_TYPE, CHAR> PARENT_TYPE;
    typedef typename PARENT_TYPE::RegexIterator RegexIterator;
    typedef typename PARENT_TYPE::InputIterator InputIterator;
    typedef typename PARENT_TYPE::Item Item;
    typedef typename PARENT_TYPE::MatchFlags MatchFlags;

    static Item GetItem(const RegexIterator& regex, const RegexIterator& regexEnd, MatchFlags flags);
    static bool MatchItem(const Item& c, const InputIterator& input, const InputIterator& inputEnd, MatchFlags flags);

protected:
    CHARItemTraits()
        {}

private:
    static inline bool IS_UPPER_A_TO_Z(CHAR x)
        { WRAPPER_NO_CONTRACT; return (((x) >= 'A') && ((x) <= 'Z')); }

    static inline bool IS_LOWER_A_TO_Z(CHAR x)
        { WRAPPER_NO_CONTRACT; return (((x) >= 'a') && ((x) <= 'z')); }

    static inline CHAR UPCASE(CHAR x)
        { WRAPPER_NO_CONTRACT; return (IS_LOWER_A_TO_Z(x) ? ((x) - 'a' + 'A') : (x)); }

    static inline CHAR DOWNCASE(CHAR x)
        { WRAPPER_NO_CONTRACT; return (IS_UPPER_A_TO_Z(x) ? ((x) - 'A' + 'a') : (x)); }

    static bool MatchCharacter(CHAR c1, CHAR c2, MatchFlags flags)
        { WRAPPER_NO_CONTRACT; return (flags & PARENT_TYPE::MF_CASE_INSENSITIVE) ? (DOWNCASE(c1) == DOWNCASE(c2)) : (c1 == c2); }
};

//-------------------------------------------------------------------------------------------------------
// Reads the next item from regex, recognizing special characters outlined in ItemTraitsBase.

template <typename ITERATOR_TYPE>
typename CHARItemTraits<ITERATOR_TYPE>::Item
CHARItemTraits<ITERATOR_TYPE>::GetItem(
    const RegexIterator& regex,
    const RegexIterator& regexEnd,
    MatchFlags           flags)
{
    WRAPPER_NO_CONTRACT;

    if (*regex == '\\')
    {
        const RegexIterator regexNext = regex+1;
        if (regexNext == regexEnd)
            return Item(PARENT_TYPE::CHARACTER, W('\\'), regexNext);
        if (*regexNext == 'd')
            return Item(PARENT_TYPE::DIGIT, regexNext+1);
        if (*regexNext == 'w')
            return Item(PARENT_TYPE::ALPHA, regexNext+1);
        if (*regexNext == 's')
            return Item(PARENT_TYPE::WHITESPACE, regexNext+1);
        return Item(PARENT_TYPE::CHARACTER, *regexNext, regexNext+1);
    }
    else if (*regex == '.')
        return Item(PARENT_TYPE::DOT, '.', regex+1);
    else if (*regex == '^')
        return Item(PARENT_TYPE::CARET, '^', regex+1);
    else if (*regex == '$')
        return Item(PARENT_TYPE::DOLLAR, '$', regex+1);
    else if (*regex == '*')
        return Item(PARENT_TYPE::STAR, '*', regex+1);
    else if (*regex == '?')
        return Item(PARENT_TYPE::QUESTION_MARK, '?', regex+1);
    else if (*regex == '+')
        return Item(PARENT_TYPE::PLUS, '+', regex+1);
    else if (*regex == '(')
        return Item(PARENT_TYPE::PAREN_OPEN, '(', regex+1);
    else if (*regex == ')')
        return Item(PARENT_TYPE::PAREN_CLOSE, ')', regex+1);
    else
        return Item(PARENT_TYPE::CHARACTER, *regex, regex + 1);
}

//-------------------------------------------------------------------------------------------------------
// Returns true if the next character point to by input matches the character class described by c.

template <typename ITERATOR_TYPE>
bool
CHARItemTraits<ITERATOR_TYPE>::MatchItem(
    const Item& c,
    const InputIterator& input,
    const InputIterator& inputEnd,
    MatchFlags           flags)
{
    WRAPPER_NO_CONTRACT;

    if (c.GetType() == PARENT_TYPE::DIGIT)
        return *input >= W('0') && *input <= W('9');
    else if (c.GetType() == PARENT_TYPE::ALPHA)
        return (*input >= W('a') && *input <= W('z')) || (*input >= W('A') && *input <= W('Z'));
    else if (c.GetType() == PARENT_TYPE::WHITESPACE)
        return *input == W(' ') || *input == W('\t');
    else
        return c.GetType() == PARENT_TYPE::DOT || MatchCharacter(c.GetValue(), *input, flags);
}

} /* namespace regex */ 
} /* namespace clr */

#endif // _DEBUG

#endif // _REGEX_BASE_H_
