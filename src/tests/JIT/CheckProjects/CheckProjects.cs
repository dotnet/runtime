// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml.Linq;

// Verify that jit test project files specify DebugType properly.
// Returns error status (-1) if any project files are in error.

internal class ScanProjectFiles
{
    private static bool s_showNeedsFixOnly = true;
    private static bool s_tryAndFix = true;
    private static int s_projCount = 0;
    private static int s_needsFixCount = 0;
    private static int s_fixedCount = 0;

    private static int Main(string[] args)
    {
        // If invoked w/o args, locate jit test project dir from
        // CORE_ROOT, and scan only.
        //
        // If invoked w args, locate and try to fix project files.
        string projectRoot = null;

        if (args.Length == 0)
        {
            s_tryAndFix = false;

            // CORE_ROOT should be something like
            //  c:\repos\coreclr\artifacts\tests\coreclr\Windows_NT.x64.Checked\Tests\Core_Root
            // or
            //  D:\j\workspace\x64_release_w---0575cb46\artifacts\tests\Windows_NT.x64.Release\Tests\Core_Root
            // We want
            //  c:\repos\coreclr\tests\src\JIT
            string coreRoot = System.Environment.GetEnvironmentVariable("CORE_ROOT");

            if (coreRoot == null)
            {
                Console.WriteLine("CORE_ROOT must be set");
                return -1;
            }

            int binIndex = coreRoot.IndexOf("bin");

            if (binIndex < 0)
            {
                Console.WriteLine("No bin directory found in CORE_ROOT path `{0}`," + 
                    " so no checking will be performed.", coreRoot);
                return 100;
            }

            string repoRoot = coreRoot.Substring(0, binIndex);

            projectRoot = Path.Combine(repoRoot, "tests", "src", "JIT");
        }
        else if (args.Length != 1)
        {
            Console.WriteLine("Usage: CheckProjects [<dir>]");
            Console.WriteLine("If optional <dir> is specified,"
                + " all project files under <dir> will be scanned and updates will be attempted.");
            return -1;
        }
        else
        {
            projectRoot = args[0];
        }

        Console.WriteLine("Scanning{0}projects under {1}",
            s_tryAndFix ? " and attempting to update " : " ", projectRoot);

        if (!Directory.Exists(projectRoot))
        {
            Console.WriteLine("Project directory does not exist, so no checking will be performed.");
            return 100;
        }

        DirectoryInfo projectRootDir = new DirectoryInfo(projectRoot);

        foreach (FileInfo f in projectRootDir.GetFiles("*.*proj", SearchOption.AllDirectories))
        {
            if (!f.FullName.Contains("JIT\\config") && !f.FullName.Contains("JIT/config"))
            {
                ParseAndUpdateProj(f.FullName, s_tryAndFix);
            }

        }

        Console.WriteLine("{0} projects, {1} needed fixes, {2} were fixed",
            s_projCount, s_needsFixCount, s_fixedCount);

        // Return error status if there are unfixed projects
        return (s_needsFixCount == 0 ? 100 : -1);
    }

    // Load up a project file and look for key attributes.
    // Optionally try and update. Return true if modified.
    private static bool ParseAndUpdateProj(string projFile, bool tryUpdate)
    {
        s_projCount++;
        // Guess at expected settings by looking for suffixes...
        string projFileBase = Path.GetFileNameWithoutExtension(projFile);

        bool isDebugTypeTest = projFileBase.EndsWith("_d") || projFileBase.EndsWith("_do") || projFileBase.EndsWith("_dbg");
        bool isRelTypeTest = projFileBase.EndsWith("_r") || projFileBase.EndsWith("_ro") || projFileBase.EndsWith("_rel");
        bool isNotOptTypeTest = projFileBase.EndsWith("_r") || projFileBase.EndsWith("_d");
        bool isOptTypeTest = projFileBase.EndsWith("_ro") || projFileBase.EndsWith("_do") || projFileBase.EndsWith("_opt");
        bool isSpecificTest = isDebugTypeTest || isRelTypeTest;
        bool updated = false;

        try
        {
            XElement root = XElement.Load(projFile);
            string nn = "{" + root.Name.NamespaceName + "}";
            IEnumerable<XElement> props = from el in root.Descendants(nn + "PropertyGroup") select el;
            bool hasReleaseCondition = false;
            bool hasDebugCondition = false;
            string oddness = null;
            string optimizeOddness = null;
            string debugVal = null;
            string optimizeVal = null;
            bool needsFix = false;
            XElement bestPropertyGroupNode = null;
            XElement lastPropertyGroupNode = null;
            List<XElement> debugTypePropertyGroupNodes = new List<XElement>();
            List<XElement> optimizePropertyGroupNodes = new List<XElement>();

            foreach (XElement prop in props)
            {
                lastPropertyGroupNode = prop;
                XAttribute condition = prop.Attribute("Condition");
                bool isReleaseCondition = false;
                bool isDebugCondition = false;
                if (condition != null)
                {
                    isReleaseCondition = condition.Value.Contains("Release");
                    isDebugCondition = condition.Value.Contains("Debug");

                    if (isReleaseCondition || isDebugCondition)
                    {
                        bestPropertyGroupNode = prop;
                    }
                }

                XElement debugType = prop.Element(nn + "DebugType");

                if (debugType != null)
                {
                    debugTypePropertyGroupNodes.Add(prop);

                    // If <DebugType> appears multiple times, all should agree.
                    string newDebugVal = debugType.Value;
                    if (newDebugVal.Equals(""))
                    {
                        newDebugVal = "blank";
                    }

                    if (debugVal != null)
                    {
                        if (!debugType.Value.Equals(newDebugVal))
                        {
                            oddness = "ConflictingDebugType";
                        }
                    }

                    debugVal = newDebugVal;

                    if (condition != null)
                    {
                        if (isReleaseCondition == isDebugCondition)
                        {
                            oddness = "RelDebugDisagree";
                        }

                        hasReleaseCondition |= isReleaseCondition;
                        hasDebugCondition |= isDebugCondition;
                    }
                    else
                    {
                        if (hasReleaseCondition || hasDebugCondition)
                        {
                            oddness = "CondAndUncond";
                        }
                    }
                }

                XElement optimize = prop.Element(nn + "Optimize");
                if (optimize != null)
                {
                    optimizePropertyGroupNodes.Add(optimize);
                    string newOptimizeVal = optimize.Value;
                    if (string.IsNullOrWhiteSpace(newOptimizeVal))
                    {
                        newOptimizeVal = "False";
                    }

                    if (optimizeVal != null && !optimizeVal.Equals(newOptimizeVal, StringComparison.InvariantCultureIgnoreCase))
                    {
                        optimizeOddness = "MultipleConflictValues";
                    }

                    optimizeVal = newOptimizeVal;
                }
            }

            if (oddness == null)
            {
                if (hasReleaseCondition && !hasDebugCondition)
                {
                    oddness = "RelButNotDbg";
                }
                else if (!hasReleaseCondition && hasDebugCondition)
                {
                    oddness = "DbgButNotRel";
                }
            }

            bool hasDebugType = debugTypePropertyGroupNodes.Count > 0;
            bool hasOptimize = optimizePropertyGroupNodes.Count > 0;

            // Analyze suffix convention mismatches
            string suffixNote = "SuffixNone";

            if (isSpecificTest)
            {
                if (!hasDebugType || oddness != null || hasReleaseCondition || hasDebugCondition)
                {
                    suffixNote = "SuffixProblem";
                    needsFix = true;
                }
                else
                {
                    if (isRelTypeTest)
                    {
                        if (debugVal.Equals("pdbonly", StringComparison.OrdinalIgnoreCase)
                            || debugVal.Equals("none", StringComparison.OrdinalIgnoreCase)
                            || debugVal.Equals("blank", StringComparison.OrdinalIgnoreCase)
                            || debugVal.Equals("embedded", StringComparison.OrdinalIgnoreCase))
                        {
                            suffixNote = "SuffixRelOk";
                        }
                        else
                        {
                            suffixNote = "SuffixRelTestNot";
                            needsFix = true;
                        }
                    }
                    else if (isDebugTypeTest)
                    {
                        if (debugVal.Equals("full", StringComparison.OrdinalIgnoreCase))
                        {
                            suffixNote = "SuffixDbgOk";
                        }
                        else
                        {
                            suffixNote = "SuffixDbgTestNot";
                            needsFix = true;
                        }
                    }
                }
            }

            if (!hasDebugType)
            {
                needsFix = true;
            }

            if (oddness != null)
            {
                needsFix = true;
            }

            if (needsFix || !s_showNeedsFixOnly)
            {
                if (!hasDebugType)
                {
                    Console.WriteLine("{0} DebugType-n/a-{1}", projFile, suffixNote);
                }
                else if (oddness != null)
                {
                    Console.WriteLine("{0} DebugType-Odd-{1}-{2}", projFile, oddness, suffixNote);
                }
                else if (hasReleaseCondition || hasDebugCondition)
                {
                    Console.WriteLine("{0} DebugType-{1}-Conditional-{2}", projFile, debugVal, suffixNote);
                }
                else
                {
                    Console.WriteLine("{0} DebugType-{1}-Unconditional-{2}", projFile, debugVal, suffixNote);
                }
            }

            if (optimizeOddness != null)
            {
                needsFix = true;
            }

            if (!needsFix)
            {
                if (isOptTypeTest)
                {
                    needsFix = DetermineIfOptimizeSettingNeedsFix(true, optimizeVal);
                }
                else if (isNotOptTypeTest)
                {
                    needsFix = DetermineIfOptimizeSettingNeedsFix(false, optimizeVal);
                }
            }

            if (needsFix || !s_showNeedsFixOnly)
            {
                if (!hasOptimize)
                {
                    Console.WriteLine("{0} Optimize-n/a", projFile);
                }
                else if (optimizeOddness != null)
                {
                    Console.WriteLine("{0} Optimize-Odd-{1}", projFile, optimizeOddness);
                }
                else
                {
                    Console.WriteLine("{0} Optimize-{1}-Conflict", projFile, optimizeVal);
                }
            }

            if (needsFix)
            {
                s_needsFixCount++;
            }

            // If a fix is needed, give it a shot!
            if (!needsFix || !tryUpdate)
            {
                return false;
            }

            // Add new elements just after the conditional rel/debug
            // property group entries, if possible.
            if (bestPropertyGroupNode == null)
            {
                bestPropertyGroupNode = lastPropertyGroupNode;
            }

            if (bestPropertyGroupNode == null)
            {
                Console.WriteLine(".... no prop group, can't fix");
                return false;
            }

            if (debugTypePropertyGroupNodes.Count == 0)
            {
                // Fix projects that don't mention debug type at all.
                Console.WriteLine(".... no DebugType, attempting fix ....");

                XElement newPropGroup = new XElement(nn + "PropertyGroup",
                    new XElement(nn + "DebugType", isDebugTypeTest ? "Full" : "PdbOnly"),
                    new XElement(nn + "Optimize", isNotOptTypeTest ? "False" : "True"));

                bestPropertyGroupNode.AddAfterSelf(newPropGroup);

                // Write out updated project file
                using (StreamWriter outFile = File.CreateText(projFile))
                {
                    root.Save(outFile);
                    updated = true;
                    s_fixedCount++;
                }
            }
            else if (debugTypePropertyGroupNodes.Count == 1)
            {
                // Fix projects with just one mention of debug type.
                Console.WriteLine(".... one DebugType, attempting fix ....");

                XElement prop = debugTypePropertyGroupNodes.First();
                XAttribute condition = prop.Attribute("Condition");

                // If there is no condition then this is likely a suffix mismatch
                if ((condition == null) && suffixNote.Equals("SuffixDbgTestNot"))
                {
                    Console.WriteLine("Unconditional debug test w/ suffix issue");

                    // Do case analysis of suffix and debugType/Opt, then update.
                    XElement debugType = prop.Element(nn + "DebugType");
                    XElement optimize = prop.Element(nn + "Optimize");

                    // We know DebugType is set, but Optimize may not be.
                    if (optimize == null)
                    {
                        optimize = new XElement(nn + "Optimize");
                        prop.Add(optimize);
                    }

                    bool modified = false;

                    if (isDebugTypeTest && !isOptTypeTest)
                    {
                        // "d" suffix --
                        debugType.Value = "full";
                        optimize.Value = "False";
                        modified = true;
                    }
                    else if (isDebugTypeTest && isOptTypeTest)
                    {
                        // "do" suffix --
                        debugType.Value = "full";
                        optimize.Value = "True";
                        modified = true;
                    }

                    if (modified)
                    {
                        // Write out updated project file
                        using (StreamWriter outFile = File.CreateText(projFile))
                        {
                            root.Save(outFile);
                            updated = true;
                            s_fixedCount++;
                        }
                    }
                }
                else
                {
                    XElement newPropGroup = new XElement(prop);
                    newPropGroup.RemoveAttributes();
                    prop.RemoveNodes();
                    bestPropertyGroupNode.AddAfterSelf(newPropGroup);

                    // Write out updated project file
                    using (StreamWriter outFile = File.CreateText(projFile))
                    {
                        root.Save(outFile);
                        updated = true;
                        s_fixedCount++;
                    }
                }
            }
            else
            {
                // Multiple property groups specifying DebugType. Remove any that are conditional.
                Console.WriteLine(".... multiple DebugTypes, attempting fix ....");
                bool modified = false;
                foreach (XElement prop in debugTypePropertyGroupNodes)
                {
                    XAttribute condition = prop.Attribute("Condition");

                    if (condition != null)
                    {
                        prop.RemoveNodes();
                        modified = true;
                    }
                }

                if (modified)
                {
                    // Write out updated project file
                    using (StreamWriter outFile = File.CreateText(projFile))
                    {
                        root.Save(outFile);
                        updated = true;
                        s_fixedCount++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("{0} DebugType-fail {1}", projFile, e.Message);
        }

        return updated;
    }

    /// <summary>
    /// Determines if optimize setting needs to fix.
    /// </summary>
    /// <param name="isOptType">Whether a optimization is specified. This is the baseline for checking.</param>
    /// <param name="optimizeVal">The optimize value in the project file's <Optimize /> property.</param>
    /// <returns>True if a fix is needed. Otherwise false.</returns>
    private static bool DetermineIfOptimizeSettingNeedsFix(bool isOptType, string optimizeVal)
    {
        if (isOptType && optimizeVal == null)
        {
            return true;
        }

        if (optimizeVal != null)
        {
            string expectedOptimizeValue = isOptType.ToString();
            if (!optimizeVal.Equals(expectedOptimizeValue, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
