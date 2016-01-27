// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// This tool exists to transform a high level description of Crst dependencies (i.e. which Crst type may be
// acquired before or after other Crst types) into a header file that defines a enum to describe each Crst
// type and tables that map type to numerical ranking and a string based name.
//
// To use the tool, run "csc.exe CrstTypeTool.cs" and run the resulting executable.
// 
// The Crst type definition file is written in a very simple language. Comments begin with '//' and continue
// to the end of the line. All remaining tokens after comment removal are simply sequences of non-whitespace
// characters separated by whitespace. Keywords are case-insensitive and identifiers (which are always Crst
// type names) are case sensitive. The language grammar is given below in EBNF-like form:
//
//      TopLevel        ::= CrstDefinition*
//
//      CrstDefinition  ::= 'Crst' <Crst type name> CrstDependency* 'End'
//
//      CrstDependency  ::= 'AcquiredBefore' <Crst type name>*
//                      |   'AcquiredAfter' <Crst type name>*
//                      |   'SameLevelAs' <Crst type name>*
//                      |   'Unordered'
//
// Crst type names match the CrstType enums used in the source code minus the 'Crst' prefix. For example
// CrstAppDomainCache is written as 'AppDomainCache' in the .def file.
//
// The dependency "A 'AcquiredBefore' B" indicates that CrstA may be legally held while CrstB is acquired.
// Similarly "A 'AcquiredAfter' B" indicates that CrstA may be legally acquired while CrstB is held. "A
// 'AcquiredBefore' B" is logically equivalent to "B 'AcquiredAfter' A" and authors may enter the dependency
// is whichever seems to make the most sense to them (or add both rules if they so desire).
//
// 'Unordered' indicates that the Crst type does not participate in ranking (there should be very few Crsts
// like this and those that are know how to avoid or deal with deadlocks manually).
//
// 'SameLevelAs' indicates the given Crst type may be acquired alongside any number of instances of the Crst
// types indicated. "A 'SameLevel' B" automatically implies "B 'SameLevel' A" so it's not necessary to specify
// the dependency both ways though authors can do so if they wish.
//
// Simple validation of the .def file (over and above syntax checking) is performed by this tool prior to
// emitting the header file. This will catch logic errors such as referencing a Crst type that is not
// defined or using the 'Unordered' attribute along with any other attribute within a single definition. It
// will also catch cycles in the dependency graph (i.e. definitions that logically describe a system where the
// Crst types can't be ranked).
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

// The main application class containing the program entry point.
class CrstTypeTool
{
    // A hash containing every Crst type defined by the input .def file along with its attributes. Keyed by
    // Crst type name (which is case sensitive and doesn't include the 'Crst' enum prefix).
    Dictionary<string, CrstType> m_crsts = new Dictionary<string, CrstType>();

    // The program entry point.
    public static int Main()
    {
        try
        {
            // Calculate the filenames of the input and output files.
            string inputFile = "CrstTypes.def";
            string outputFile = "CrstTypes.h";

            // A common error is to forget to check out the CrstTypes.h file first. Handle this case specially
            // so we can give a good error message.
            if (File.Exists(outputFile) && (File.GetAttributes(outputFile) & FileAttributes.ReadOnly) != 0)
            {
                Console.WriteLine(outputFile + " is read-only, you must check it out of TFS/SD first");
                return 2;
            }

            // Create an instance of our application class to store state in (specifically the collection of
            // Crst type definitions).
            CrstTypeTool app = new CrstTypeTool();

            // Create a parser for the CrstTypes.def file and run it over the input file (errors are signalled
            // via exception, in common with all the following steps except validation).
            new TypeFileParser().ParseFile(inputFile, app.m_crsts);

            // Validate the collection of Crst type definitions we built up during parsing for common logic
            // errors and the presence of dependency cycles. False is returned from ValidateCrsts if an error
            // was detected (an error message will have already been output to the console at this point).
            if (!app.ValidateCrsts())
                return 3;

            // Perform a topological sort to map each Crst type to a numeric ranking.
            app.LevelCrsts();

            // Emit the new header file containing Crst type definitions and ranking information.
            app.WriteHeaderFile(outputFile);

            // If we get here the transformation was successful; inform the user and we're done.
            Console.WriteLine(outputFile + " successfully updated");
            return 0;
        }
        catch (TypeFileParser.ParseError pe)
        {
            // Syntax errors specific to parsing the input file.
            Console.WriteLine("ParseError: " + pe.Message);
            return 4;
        }
        catch (Exception e)
        {
            // Any other general errors (file I/O problems, out of memory etc.).
            Console.WriteLine("Unexpected exception:");
            Console.WriteLine(e);
            return 5;
        }
    }

    // Emit the CrstTypes.h output file.
    void WriteHeaderFile(string fileName)
    {
        FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        StreamWriter writer = new StreamWriter(stream);

        // Create a collection based on all the Crst types we've stored in the hash. We do this so we can sort
        // the Crst types we emit (lexically, based on type name).
        Dictionary<string, CrstType>.ValueCollection crstCollection = m_crsts.Values;
        CrstType[] crsts = new CrstType[crstCollection.Count];
        crstCollection.CopyTo(crsts, 0);
        Array.Sort(crsts);

        // Emit the header. Contains copyright information, the usual goop to avoid multiple inclusion and a
        // header comment to discourage direct editing and point the user at the CrstTypes.def file instead
        // (where all will be explained in greater detail).
        writer.WriteLine("//");
        writer.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
        writer.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
        writer.WriteLine("// See the LICENSE file in the project root for more information.");
        writer.WriteLine("//");
        writer.WriteLine();
        writer.WriteLine("#ifndef __CRST_TYPES_INCLUDED");
        writer.WriteLine("#define __CRST_TYPES_INCLUDED");
        writer.WriteLine();
        writer.WriteLine("// **** THIS IS AN AUTOMATICALLY GENERATED HEADER FILE -- DO NOT EDIT!!! ****");
        writer.WriteLine();
        writer.WriteLine("// This file describes the range of Crst types available and their mapping to a numeric level (used by the");
        writer.WriteLine("// runtime in debug mode to validate we're deadlock free). To modify these settings edit the");
        writer.WriteLine("// file:CrstTypes.def file and run the clr\\bin\\CrstTypeTool utility to generate a new version of this file.");
        writer.WriteLine();

        // Emit the CrstType enum to define a value for each crst type (along with the kNumberOfCrstTypes
        // constant).
        writer.WriteLine("// Each Crst type is declared as a value in the following CrstType enum.");
        writer.WriteLine("enum CrstType");
        writer.WriteLine("{");
        for (int i = 0; i < crsts.Length; i++)
            writer.WriteLine("    Crst" + crsts[i].Name + " = " + i.ToString() + ",");
        writer.WriteLine("    kNumberOfCrstTypes = " + crsts.Length.ToString());
        writer.WriteLine("};");
        writer.WriteLine();

        // This is the end of the regular part of the header included by most files.
        writer.WriteLine("#endif // __CRST_TYPES_INCLUDED");
        writer.WriteLine();

        // There is a second section of the header intended for inclusion only by vm\Crst.cpp. This contains
        // some data tables used to map crst type to rank or name. We could instead define two separate
        // headers, but on the whole it seems simpler to do it this way.
        writer.WriteLine("// Define some debug data in one module only -- vm\\crst.cpp.");
        writer.WriteLine("#if defined(__IN_CRST_CPP) && defined(_DEBUG)");
        writer.WriteLine();

        // Emit the crst type to rank mapping table.
        writer.WriteLine("// An array mapping CrstType to level.");
        writer.WriteLine("int g_rgCrstLevelMap[] =");
        writer.WriteLine("{");
        foreach (CrstType crst in crsts)
            writer.WriteLine("    " + crst.Level + ",\t\t\t// Crst" + crst.Name);
        writer.WriteLine("};");
        writer.WriteLine();

        // Emit the crst type to name mapping table.
        writer.WriteLine("// An array mapping CrstType to a stringized name.");
        writer.WriteLine("LPCSTR g_rgCrstNameMap[] =");
        writer.WriteLine("{");
        foreach (CrstType crst in crsts)
            writer.WriteLine("    \"Crst" + crst.Name + "\",");
        writer.WriteLine("};");
        writer.WriteLine();

        // Emit the constant Crst.cpp uses to record an unordered rank.
        writer.WriteLine("// Define a special level constant for unordered locks.");
        writer.WriteLine("#define CRSTUNORDERED (-1)");
        writer.WriteLine();

        // Emit a couple of inline helpers to map type to rank or name (and validate the type while they're at
        // it).
        writer.WriteLine("// Define inline helpers to map Crst types to names and levels.");
        writer.WriteLine("inline static int GetCrstLevel(CrstType crstType)");
        writer.WriteLine("{");
        writer.WriteLine("    LIMITED_METHOD_CONTRACT;");
        writer.WriteLine("    _ASSERTE(crstType >= 0 && crstType < kNumberOfCrstTypes);");
        writer.WriteLine("    return g_rgCrstLevelMap[crstType];");
        writer.WriteLine("}");
        writer.WriteLine("inline static LPCSTR GetCrstName(CrstType crstType)");
        writer.WriteLine("{");
        writer.WriteLine("    LIMITED_METHOD_CONTRACT;");
        writer.WriteLine("    _ASSERTE(crstType >= 0 && crstType < kNumberOfCrstTypes);");
        writer.WriteLine("    return g_rgCrstNameMap[crstType];");
        writer.WriteLine("}");
        writer.WriteLine();

        // And that's the end of the second section of the header file.
        writer.WriteLine("#endif // defined(__IN_CRST_CPP) && defined(_DEBUG)");

        writer.Close();
        stream.Close();
    }

    // Peform checking of the Crst type definitions we've read just read. Various forms of logic error are
    // scanned for including cycles in the dependency graph. Returns true if no errors are found. If false is
    // returned a descriptive error message will have already been written to the console.
    bool ValidateCrsts()
    {
        // Look at each Crst type definition in turn.
        foreach (CrstType crst in m_crsts.Values)
        {
            // Catch Crst types that are referenced but never defined.
            if (!crst.Defined)
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is referenced without being defined",
                                                crst.Name));
                return false;
            }

            // Catch the use of the 'Unordered' attribute alongside the 'AcquiredBefore' attribute (which
            // indicates an ordering).
            if (crst.Level == CrstType.CrstUnordered && (crst.AcquiredBeforeList.Count > 0 ||
                                                         crst.Group != null))
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is declared as both unordered and acquired before 'Crst{1}'",
                                                crst.Name, crst.AcquiredBeforeList[0].Name));
                return false;
            }

            // Catch the use of the 'Unordered' attribute alongside the 'SameLevelAs' attribute (which
            // indicates an ordering).
            if (crst.Level == CrstType.CrstUnordered && crst.Group != null)
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is declared as both unordered and in the same level as another CrstType",
                                                crst.Name));
                return false;
            }

            // Catch the simple cycle where the Crst type depends on itself.
            if (crst.AcquiredBeforeList.Contains(crst))
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is declared as being acquired before itself",
                                                crst.Name));
                return false;
            }

            // Look for deeper cycles using a recursive algorithm in 'FindCycle()'.
            List<CrstType> cycleList = new List<CrstType>();
            if (FindCycle(crst, crst, cycleList))
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is involved in a dependency cycle with the following CrstTypes:",
                                                crst.Name));
                foreach (CrstType cycleCrst in cycleList)
                    Console.WriteLine(String.Format("    Crst{0}", cycleCrst.Name));
                return false;
            }
        }

        // Perform normalization of each set of Crst types that are included in the same group (i.e. have a
        // 'SameLevelAs' relationship). Normalization means that each Crst type in a group will have exactly
        // the same set of dependency rules as all the others.
        CrstTypeGroup.NormalizeAllRules();

        // The normalization process could have introduced cycles in the dependency graph so run the cycle
        // detection pass again. We do separate passes like this since normalizing can lead to less intuitive
        // error messages if a cycle is found: so if the cycle exists before normalization takes place we want
        // to generate an error message then.
        foreach (CrstType crst in m_crsts.Values)
        {
            List<CrstType> cycleList = new List<CrstType>();
            if (FindCycle(crst, crst, cycleList))
            {
                Console.WriteLine(String.Format("Error: CrstType 'Crst{0}' is involved in a dependency cycle with the following CrstTypes:",
                                                crst.Name));
                foreach (CrstType cycleCrst in cycleList)
                    Console.WriteLine(String.Format("    Crst{0}", cycleCrst));
                Console.WriteLine("Note that the cycle was detected only after 'SameLevelAs' processing was performed so some CrstType dependencies are implied by peer CrstTypes");
                return false;
            }
        }

        return true;
    }

    // Recursively determine if a cycle exists in the Crst type dependency graph rooted at the 'rootCrst'
    // type. The 'currCrst' indicates the next dependency to be examined (it will be the same as the
    // 'rootCrst' when we're first called). The 'cycleList' argument contains a list of Crst types we've
    // already examined in this branch of the algorithm and serves both to avoid checking the same node twice
    // and to provide a list of the involved Crst types should a cycle be detected.
    // Note that this algorithm is not designed to detect general cycles in the graph, only those that involve
    // the 'rootCrst' directly. This is somewhat inefficient but gives us a simple way to generate clear error
    // messages.
    bool FindCycle(CrstType rootCrst, CrstType currCrst, List<CrstType> cycleList)
    {
        // Add the current Crst type to the list of those we've seen.
        cycleList.Add(currCrst);

        // Look through all the dependencies of the current Crst type.
        foreach (CrstType childCrst in currCrst.AcquiredBeforeList)
        {
            // If we find a reference back to the root Crst type then we've found a cycle. Start backing out
            // from the recursion (keeping the list of nodes we visited intact) by returning true.
            if (childCrst == rootCrst)
                return true;

            // Otherwise iterate over the dependencies of the current node and for each one that we haven't
            // already seen and recursively extend the search.
            if (!cycleList.Contains(childCrst))
                if (FindCycle(rootCrst, childCrst, cycleList))
                    return true;
        }

        // Didn't find any cycles involving the root and this node; remove this node from the potential cycle
        // list and return up to our caller indicating such.
        cycleList.RemoveAt(cycleList.Count - 1);

        return false;
    }

    // Topologically sort all the Crsts so we can assign a total ordering to them (in the form of a numeric
    // ranking). Ranks start from 0 (Crst types that may be acquired at any time) and increment from there
    // (Crst types that may only be acquired if a lower type is not already held).
    // **** NOTE: The leveling process is destructive in that we will lose all dependency information from the
    // Crst type definitions during the course of the algorithm.
    void LevelCrsts()
    {
        // Note that Crst type dependency rules have been normalized (by the input parser) so that all
        // AcquiredBefore/AcquiredAfter relationships have been reduced to AcquiredBefore relationships (i.e.
        // any rule of the form "A AcquiredAfter B" has been converted to "B AcquiredBefore A". Any
        // normalization makes the algorithm easier to program, but a normaliztion to AcquiredBefore
        // relationships was chosen since it makes it particularly easy to implement an algorithm that assigns
        // ranks beginning with zero and moving up to an arbitrary level. Any type that doesn't have any
        // AcquiredBefore dependencies can always be ranked at a lower level than any remaining unranked types
        // by definition and from this we can derive a simple iterative process to rank all the crst types.

        // Calculate how many Crst types we have left to rank (some are not included in this step because
        // they've been marked as 'Unordered' in the input file).
        int unsorted = 0;
        foreach (CrstType crst in m_crsts.Values)
            if (crst.Level == CrstType.CrstUnassigned)
                unsorted++;

        // The ranking level we're going to assign to Crst types on the next pass of the algorithm.
        int currLevel = 0;

        // Iterate while we still have Crst types left to rank. On each pass we'll assign a rank to those
        // types that no longer have any dependencies forcing them to have a higher rank and then remove
        // dependency rules involving those newly ranked types from the remaining types.
        while (unsorted > 0)
        {
            // Record a flag indicating whether we manage to assign a rank to at least one Crst type on this
            // pass. If we ever fail to do this we've hit a cycle (this is just paranoia, the Crst declaration
            // validation performed in ValidateCrsts() should have detected such a cycle first).
            bool madeProgress = false;

            // If we spot any types that are in a group (SameLevelAs relationship) then we defer assigning a
            // rank till we've dealt with any non-group types (we wish to always place type groups in their
            // very own rank else the Crst rank violation detection code won't detect violations between
            // members of the group and singleton types that happened to be assigned rank on the same pass).
            List<CrstTypeGroup> deferredGroups = new List<CrstTypeGroup>();

            // Scan through all the Crst types.
            foreach (CrstType crst in m_crsts.Values)
            {
                // Skip those types that already have a rank assigned.
                if (crst.Level != CrstType.CrstUnassigned)
                    continue;

                // We're looking for Crst types that no longer have any types that can be acquired while they
                // are already held. This indicates that it's safe to assign the current rank to them (since
                // there are no remaining dependencies that need to be ranked first (i.e. with a lower rank
                // value than this type).
                if (crst.AcquiredBeforeList.Count == 0)
                {
                    if (crst.Group == null)
                    {
                        // If this type is not part of the group we can go and assign the rank right away.
                        crst.Level = currLevel;
                        madeProgress = true;
                        unsorted--;
                    }
                    else if (!deferredGroups.Contains(crst.Group))
                        // Otherwise we'll defer ranking this group member until all the singletons are
                        // processed.
                        deferredGroups.Add(crst.Group);
                }
            }

            // We've gone through the entire collection of Crst types and assigned the current rank level to
            // any singleton Crst types that qualify. Now deal with any group members we detected (it's
            // possible that more than one group qualifies for ranking at this level but we need to be careful
            // to assign distinct rank values to each group to avoid hiding lock rank violations (since group
            // members are always allowed to be acquired alongside any other type with the same rank value).
            // Iterate over each distinct group that we found in this pass.
            foreach (CrstTypeGroup group in deferredGroups)
            {
                // Look at our progress flag here. If it is false then we didn't have any singleton Crst types
                // ranked at this level and we haven't processed any other groups at this level either. Thus
                // we can rank this group at the current level. Otherwise at least one type was already ranked
                // with this level so we need to increment to a new, distinct level to avoid ranking
                // ambiguity.
                if (madeProgress)
                    currLevel++;

                // Iterate through each Crst type that is a member of this group assigning them the (same)
                // current rank.
                foreach (CrstType crst in group.Members)
                {
                    // Double check that each member has the same dependencies (i.e. they should all be empty
                    // by now). There should be no way that this error should ever occur, it's just paranoia
                    // on my part.
                    if (crst.AcquiredBeforeList.Count != 0)
                        throw new Exception("Internal error: SameLevel CrstTypes with differing rulesets");

                    crst.Level = currLevel;
                    unsorted--;
                }

                // Once we've processed at least one group we've made progress this iteration.
                madeProgress = true;
            }

            // If we didn't manage to assign rank to at least one Crst type then we're not going to do any
            // better next iteration either (because no state was updated in this iteration). This should only
            // occur in the presence of a dependency cycle and we shouldn't get that here after a successful
            // call to ValidateCrsts(), so this check is pure paranoia.
            if (!madeProgress)
            {
                Console.WriteLine(String.Format("{0} unsorted remain", unsorted));
                throw new Exception("Cycle detected trying to assign level " + currLevel.ToString());
            }

            // Loop through all the unranked Crsts types and remove any AcquiredBefore relationships that
            // involve types we've already leveled (since those types, by definition, have already been
            // assigned a lower rank).
            foreach (CrstType crst in m_crsts.Values)
            {
                if (crst.Level != CrstType.CrstUnassigned)
                    continue;
                List<CrstType> prunedCrsts = crst.AcquiredBeforeList.FindAll(Unleveled);
                crst.AcquiredBeforeList = prunedCrsts;
            }

            // Done with this rank level, move to the next.
            currLevel++;
        }
    }

    // Predicate method used with List<T>.FindAll() to locate Crst types that haven't had their rank assigned
    // yet.
    static bool Unleveled(CrstType crst)
    {
        return crst.Level == CrstType.CrstUnassigned;
    }
}

// Class used to parse a CrstTypes.def file into a dictionary of Crst type definitions. It uses a simple lexer
// that removes comments then forms tokens out of any consecutive non-whitespace characters. An equally simple
// recursive descent parser forms Crst instances by parsing the token stream.
class TypeFileParser
{
    // Remember the input file name and the dictionary we're meant to populate.
    string                          m_typeFileName;
    Dictionary<string, CrstType>    m_crsts;

    // Compile regular expressions for detecting comments and tokens in the parser input.
    Regex                           m_commentRegex = new Regex(@"//.*");
    Regex                           m_tokenRegex = new Regex(@"^(\s*(\S+)\s*)*");

    // Input is lexed into an array of tokens. We record the index of the token being currently parsed.
    Token[]                         m_tokens;
    int                             m_currToken;

    // Parse the given file into Crst type definitions and place these definitions in the dictionary provided.
    // Syntax errors are signalled via ParseError derived exceptions.
    public void ParseFile(string typeFileName, Dictionary<string, CrstType> crsts)
    {
        m_typeFileName = typeFileName;
        m_crsts = crsts;

        // Lex the file into tokens.
        InitTokenStream();

        // Parse the tokens according to the grammar set out at the top of this file.
        // Loop until we have no further tokens to process.
        while (!IsEof())
        {
            // Grab the next token.
            Token token = NextToken();

            // We're at the top level, so the token had better be 'Crst'.
            if (token.Id != KeywordId.Crst)
                throw new UnexpectedTokenError(token, KeywordId.Crst);

            // OK, parse the rest of this single Crst type definition.
            ParseCrst();
        }
    }

    // Parse a single Crst type definition.
    void ParseCrst()
    {
        // The next token had better be an identifier (the Crst type name).
        Token token = NextToken();
        if (token.Id != KeywordId.Id)
            throw new UnexpectedTokenError(token, KeywordId.Id);

        // The Crst instance might already exist in the dictionary (forward references to a Crst type cause
        // these entries to auto-vivify). But in that case the entry better not be marked as 'Defined' which
        // would indicate a double declaration.
        CrstType crst;
        if (m_crsts.ContainsKey(token.Text))
        {
            crst = m_crsts[token.Text];
            if (crst.Defined)
                throw new ParseError(String.Format("Duplicate definition for CrstType '{0}'", token.Text), token);
        }
        else
        {
            // Otherwise this Crst type hasn't been seen thus far so we allocate a new instance and add it to
            // the dictionary.
            crst = new CrstType(token.Text);
            m_crsts.Add(crst.Name, crst);
        }

        // We're defining, not just referencing this type.
        crst.Defined = true;

        // Parse any attributes inside this definition (until we see an 'End' token).
        bool parsingCrst = true;
        while (parsingCrst)
        {
            // Get the next token. Either some attribute keyword or 'End'.
            token = NextToken();
            List<CrstType> list;

            switch (token.Id)
            {

            case KeywordId.AcquiredBefore:
                // Simply parse the following list of Crst types into the current type's AcquiredBefore list.
                ParseList(crst.AcquiredBeforeList);
                break;

            case KeywordId.AcquiredAfter:
                // AcquiredAfter is trickier. To make the ranking algorithm's life easier we actually
                // normalize all rules to the AcquiredBefore form (see LevelCrsts() for the reasoning). So we
                // capture the list of Crst types that follow the AcquiredAfter keyword and then append the
                // current type to the AcquiredBefore list of each type found.
                list = new List<CrstType>();
                ParseList(list);
                foreach (CrstType priorCrst in list)
                    priorCrst.AcquiredBeforeList.Add(crst);
                break;

            case KeywordId.SameLevelAs:
                // Parse the following list of Crst types them let the CrstTypeGroup class handle the
                // resulting updates to the type groups we're currently maintaining. See the comments for the
                // CrstTypeGroup class for more details.
                list = new List<CrstType>();
                ParseList(list);
                foreach (CrstType sameLevelCrst in list)
                    CrstTypeGroup.Join(crst, sameLevelCrst);
                break;

            case KeywordId.Unordered:
                crst.Level = CrstType.CrstUnordered;
                break;

            case KeywordId.End:
                parsingCrst = false;
                break;

            default:
                throw new UnexpectedTokenError(token,
                                               KeywordId.AcquiredBefore,
                                               KeywordId.AcquiredAfter,
                                               KeywordId.SameLevelAs,
                                               KeywordId.Unordered);
            }
        }
    }

    // Parse a list of Crst type names. Any other token terminates the list (without error and without
    // consuming that token from the stream). The list of tokens is returned as a list of corresponding
    // CrstTypes (which are auto-vivified in the output dictionary if they haven't been declared yet).
    void ParseList(List<CrstType> list)
    {
        // Parse tokens until we find a non-indentifier.
        while (true)
        {
            Token token = NextToken();
            if (token.Id != KeywordId.Id)
            {
                // We found the list terminator. Push the non-identifier token back into the stream for our
                // caller to parse correctly.
                UnwindToken();
                return;
            }

            // Look up or add a new CrstType corresponding to this type name.
            CrstType crst;
            if (m_crsts.ContainsKey(token.Text))
                crst = m_crsts[token.Text];
            else
            {
                crst = new CrstType(token.Text);
                m_crsts[crst.Name] = crst;
            }

            // Add the type to the output list we're building.
            list.Add(crst);
        }
    }

    // Lex the input file into an array of tokens.
    void InitTokenStream()
    {
        StreamReader    file = new StreamReader(m_typeFileName);
        int             lineNumber = 1;
        List<Token>     tokenList = new List<Token>();

        // Read the file a line at a time.
        string line;
        while ((line = file.ReadLine()) != null)
        {
            // Remove comments from the current line.
            line = m_commentRegex.Replace(line, "");

            // Match all contiguous non-whitespace characters as individual tokens.
            Match match = m_tokenRegex.Match(line);
            if (match.Success)
            {
                // For each token captured build a token instance and record the token text and the file, line
                // and column at which it was encountered (these latter in order to produce useful syntax
                // error messages).
                CaptureCollection cap = match.Groups[2].Captures;
                for (int i = 0; i < cap.Count; i++)
                    tokenList.Add(new Token(m_typeFileName, cap[i].Value, lineNumber, cap[i].Index));
            }

            lineNumber++;
        }

        // Record the list of tokens we captured as an array and reset the index of the next token to be
        // handled by the parser.
        m_tokens = tokenList.ToArray();
        m_currToken = 0;
    }

    // Have we run out of tokens to parse?
    bool IsEof()
    {
        return m_currToken >= m_tokens.Length;
    }

    // Get the next token and throw an exception if we ran out.
    Token NextToken()
    {
        if (m_currToken >= m_tokens.Length)
            throw new UnexpectedEofError();
        return m_tokens[m_currToken++];
    }

    // Push the last token parsed back into the stream.
    void UnwindToken()
    {
        if (m_currToken <= 0)
            throw new InvalidOperationException();
        m_currToken--;
    }

    // The various keywords we can encounter (plus Id for identifiers, which are currently always Crst type
    // names).
    internal enum KeywordId
    {
        Id,
        Crst,
        End,
        AcquiredBefore,
        AcquiredAfter,
        Unordered,
        SameLevelAs,
    }

    // Class encapsulating a single token captured from the input file.
    internal class Token
    {
        // Hash of keyword text to enum values.
        static Dictionary<string, KeywordId> s_keywords;

        // The characters comprising the text of the token from the input file.
        string      m_text;

        // Where the token was found (for error messages).
        string      m_file;
        int         m_line;
        int         m_column;

        // The ID of the keyword this token represents (or KeywordId.Id).
        KeywordId   m_id;

        // Static class initialization.
        static Token()
        {
            // Populate the keyword hash. No sense building complex finite state machines to improve the
            // efficiency of keyword lexing here since the input file (and keyword set) is never going to be
            // big enough to justify the extra work.
            s_keywords = new Dictionary<string, KeywordId>();
            s_keywords.Add("crst", KeywordId.Crst);
            s_keywords.Add("end", KeywordId.End);
            s_keywords.Add("acquiredbefore", KeywordId.AcquiredBefore);
            s_keywords.Add("acquiredafter", KeywordId.AcquiredAfter);
            s_keywords.Add("unordered", KeywordId.Unordered);
            s_keywords.Add("samelevelas", KeywordId.SameLevelAs);
        }

        public Token(string file, string text, int line, int column)
        {
            m_file = file;
            m_text = text;
            m_line = line;
            m_column = column;

            // Map token text to keyword ID. True keywords (not identifiers) are case insensitive so normalize
            // the text to lower case before performing the keyword hash lookup.
            string canonName = m_text.ToLower();
            if (s_keywords.ContainsKey(canonName))
                m_id = s_keywords[canonName];
            else
                m_id = KeywordId.Id;
        }

        public string Text {get { return m_text; }}
        public string Location {get { return String.Format("{0} line {1}, column {2}", m_file, m_line, m_column); }}
        public KeywordId Id {get { return m_id; }}
    }

    // Base class for all syntax errors reported by the parser.
    internal class ParseError : Exception
    {
        // A raw error message.
        public ParseError(string message)
            : base(message)
        {}

        // An error message tagged with a file, line and column (coming from an error token).
        public ParseError(string message, Token errorToken)
            : base(String.Format("{0}: {1}", errorToken.Location, message))
        {}

        // Produce a textual name for the given keyword type.
        protected static string IdToName(KeywordId id)
        {
            if (id == KeywordId.Id)
                return "a CrstType name";
            return String.Format("'{0}'", id.ToString());
        }
    }

    // Syntax error used when an unexpected token is encountered which further lists the valid tokens that
    // would otherwise have been accepted.
    internal class UnexpectedTokenError : ParseError
    {
        // Produce an unexpected token message with a file, line and column coming from an error token and
        // optionally the names of zero or more tokens that would have been accepted.
        public UnexpectedTokenError(Token errorToken, params KeywordId[] expected)
            : base(FormatErrorMessage(errorToken, expected))
        {}

        static string FormatErrorMessage(Token errorToken, KeywordId[] expected)
        {
            StringBuilder message = new StringBuilder(String.Format("Unexpected token '{0}' at {1}",
                                                                    errorToken.Text, errorToken.Location));
            if (expected.Length == 0)
            {
            }
            else if (expected.Length == 1)
            {
                message.Append(String.Format("; expected {0}", IdToName(expected[0])));
            }
            else
            {
                message.Append("; expected one of ");
                for (int i = 0; i < expected.Length - 1; i++)
                    message.Append(String.Format("{0}, ", IdToName(expected[i])));
                message.Append(IdToName(expected[expected.Length - 1]));
                    
            }

            return message.ToString();
        }
    }

    // Syntax error used when we unexpectedly ran out of tokens.
    internal class UnexpectedEofError : ParseError
    {
        public UnexpectedEofError()
            : base("Unexpected end of file")
        {}
    }
}

// This class represents an instance of a Crst type. These are unqiuely identified by case-sensitive name (the
// same as the enum name used in vm code, minus the 'Crst' prefix).
class CrstType : IComparable
{
    // Special level constants used to indicate unordered Crst types or those types we haven't gotten around
    // to ranking yet.
    public static readonly int CrstUnordered = -1;
    public static readonly int CrstUnassigned = -2;

    // Name of the type, e.g. "AppDomainCache" for the CrstAppDomainCache type.
    string          m_name;

    // The numeric ranking assigned to this type. Starts as CrstUnassigned and then becomes either
    // CrstUnordered (while parsing the input file) or a number >= 0 (during LevelCrsts()).
    int             m_level;

    // List of Crst types that can be legally acquired while this one is held. (AcquiredAfter relationships
    // are by switching the terms and adding to the second type's AcquiredBefore list).
    List<CrstType>  m_acquiredBeforeCrsts;

    // Either null if this Crst type is not in (or has not yet been determined to be in) a SameLevelAs
    // relationship or points to a CrstTypeGroup that records all the sibling types at the same level (that
    // have been discovered thus far during parsing).
    CrstTypeGroup   m_group;

    // Set once a definition for this type has been discovered. Used to detect double definitions and types
    // referenced without definitions.
    bool            m_defined;

    public CrstType(string name)
    {
        m_name = name;
        m_level = CrstUnassigned;
        m_acquiredBeforeCrsts = new List<CrstType>();
        m_group = null;
        m_defined = false;
    }

    public string Name {get { return m_name; }}
    public int Level {get { return m_level; } set { m_level = value; }}
    public List<CrstType> AcquiredBeforeList {get { return m_acquiredBeforeCrsts; } set { m_acquiredBeforeCrsts = value; }}
    public CrstTypeGroup Group {get { return m_group; } set { m_group = value; }}
    public bool Defined {get {return m_defined; } set { m_defined = value; }}

    // Helper used to sort CrstTypes. The sort order is lexical based on the type name.
    public int CompareTo(object other)
    {
        return m_name.CompareTo(((CrstType)other).m_name);
    }
}

// Every time a SameLevelAs relationship is used we need to be careful to keep track of the transitive closure
// of all types bound in the relationship. That's because such a relationship impacts the other dependency
// rules (each member of a SameLevelAs group must behave as though it has exactly the same dependency rules as
// all the others). Identifying all the members is tricky because "A SameLevelAs B" and "B SameLevelAs C"
// implies "A SameLevelAs C". So we use a separate tracking structure, instances of the CrstTypeGroup type, to
// do the bookkeeping for us. Each Crst type belongs to either zero or one CrstTypeGroups. As we find new
// SameLevelAs relationships we create new groups, add types to existing groups or merge groups (as previous
// distinct groups are merged by the discovery of a SameLevelAs relationship that links them). By the time
// parsing has finished we are guaranteed to have discovered all the distinct, disjoint groups and to have
// fully populated them with the transitive closure of all related types. We can them normalize all groups
// members so they share the same AcquiredBefore relationships.
class CrstTypeGroup
{
    // We record every group that has been formed so far. This makes normalizing all groups easier.
    static List<CrstTypeGroup>  s_groups = new List<CrstTypeGroup>();

    // Crst types that are members of the current group. There are no duplicates in this list.
    List<CrstType>              m_members = new List<CrstType>();

    // Declare a SameLevelAs relationship between the two Crst types given. Groups will be assigned, created
    // or merged as required to maintain our guarantees (each CrstType is a member of at most one group and
    // all CrstTypes involved in the same transitive closure of a SameLevelAs relationship are members of one
    // group).
    public static void Join(CrstType crst1, CrstType crst2)
    {
        CrstTypeGroup group;

        if (crst1 == crst2)
        {
            // In this case the type refers to itself. Create a singleton group for this type if it doesn't
            // already exist.
            if (crst1.Group == null)
            {
                group = new CrstTypeGroup();
                group.m_members.Add(crst1);

                s_groups.Add(group);

                crst1.Group = group;
            }
        }
        else if (crst1.Group == null && crst2.Group == null)
        {
            // Neither types belong to a group already. So we can create a new one and add both types to it.
            group = new CrstTypeGroup();
            group.m_members.Add(crst1);
            group.m_members.Add(crst2);

            s_groups.Add(group);

            crst1.Group = group;
            crst2.Group = group;
        }
        else if (crst1.Group == null)
        {
            // The first type doesn't belong to a group yet but the second does. So we can simply add the
            // first type to the second group.
            group = crst2.Group;
            group.m_members.Add(crst1);

            crst1.Group = group;
        }
        else if (crst2.Group == null)
        {
            // As for the case above but the group/no-group positions are reversed.
            group = crst1.Group;
            group.m_members.Add(crst2);

            crst2.Group = group;
        }
        else if (crst1.Group != crst2.Group)
        {
            // Both types belong to different groups so we'll have to merge them. Add the members of group 2
            // to group 1 and throw away group 2.
            group = crst1.Group;
            CrstTypeGroup absorbGroup = crst2.Group;
            foreach (CrstType crst in absorbGroup.m_members)
            {
                group.m_members.Add(crst);
                crst.Group = group;
            }

            s_groups.Remove(absorbGroup);
        }

        // The only case left is when both types are already in the same group and there's no work needed in
        // this case.
    }

    // Normalize all the groups we created during parsing. See below for the definition of normalization.
    public static void NormalizeAllRules()
    {
        foreach (CrstTypeGroup group in s_groups)
            group.NormalizeRules();
    }

    // Normalize this group. This involves adjusting the AcquiredBefore list of each member to be the union of
    // all such rules within the group. This step allows us to detect cycles in the dependency graph that
    // would otherwise remain hidden if we only examined the unnormalized AcquiredBefore rules.
    void NormalizeRules()
    {
        // This local will contain the union of all AcquiredBefore rules.
        List<CrstType> acquiredBeforeList = new List<CrstType>();

        // Iterate through each member of the group.
        foreach (CrstType crst in m_members)
        {
            // Add each AcquiredBefore rule we haven't already seen to the union.
            foreach (CrstType afterCrst in crst.AcquiredBeforeList)
                if (!acquiredBeforeList.Contains(afterCrst))
                    acquiredBeforeList.Add(afterCrst);
        }

        // Reset each member's AcquiredBefore list to a copy of the union we calculated. Note it's important
        // to make a (shallow) copy because the ranking process modifies this list and so a shared copy would
        // cause unexpected results.
        foreach (CrstType crst in m_members)
            crst.AcquiredBeforeList = acquiredBeforeList.GetRange(0, acquiredBeforeList.Count);
    }

    public List<CrstType> Members {get { return m_members; }}
}
