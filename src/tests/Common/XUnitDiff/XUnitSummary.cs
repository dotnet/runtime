// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

public class XUnitResult
{
    public readonly string Key;
    public readonly string File;
    public readonly string Assembly;
    public readonly string Collection;
    public readonly string Name;
    public readonly string Type;
    public readonly string Method;
    public readonly string Result;
    public readonly string Duration;

    public XUnitResult(
        string file,
        string assembly,
        string collection,
        string name,
        string type,
        string method,
        string result,
        string duration)
    {
        Key = SanitizeTestName(name);
        File = file;
        Assembly = assembly;
        Collection = collection;
        Name = name;
        Type = type;
        Method = method;
        Result = result;
        Duration = duration;
    }

    public override bool Equals(object? obj)
    {
        return obj is XUnitResult other && Key == other.Key;
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

    public override string ToString()
    {
        return Key;
    }

    private static string SanitizeTestName(string file)
    {
        string sanitized = Path.ChangeExtension(file, null).Replace("\\\\", "\\");
        if (sanitized.EndsWith("_il_do") || sanitized.EndsWith("_il_ro"))
        {
            sanitized = sanitized.Substring(0, sanitized.Length - 6);
        }
        else
        if (sanitized.EndsWith("_il_d") || sanitized.EndsWith("_il_r"))
        {
            sanitized = sanitized.Substring(0, sanitized.Length - 5);
        }
        return sanitized;
    }
}

public class XUnitResultSummary
{
    private readonly Dictionary<string, XUnitResult> _results;

    public XUnitResultSummary()
    {
        _results = new Dictionary<string, XUnitResult>();
    }

    public void AppendXmlResults(IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            AppendXmlResults(file);
        }
    }

    public void AppendXmlResults(string file)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(file);
        foreach (XmlNode assemblyNode in xmlDoc.GetElementsByTagName("assembly"))
        {
            string assemblyName = assemblyNode.Attributes?["name"]?.Value ?? "";
            foreach (XmlNode collectionNode in assemblyNode.ChildNodes)
            {
                if (collectionNode.Name == "errors")
                {
                    continue;
                }
                if (collectionNode.Name != "collection")
                {
                    Console.WriteLine("Unexpected node in xUnit summary {0}: {1} (collection expected)", file, collectionNode.Name);
                    continue;
                }
                string collectionName = collectionNode.Attributes?["name"]?.Value ?? "";
                foreach (XmlNode testNode in collectionNode.ChildNodes)
                {
                    if (testNode.Name != "test")
                    {
                        Console.WriteLine("Unexpected node in xUnit summary {0}: {1} (test expected)", file, testNode.Name);
                        continue;
                    }
                    string testName = testNode.Attributes?["name"]?.Value ?? "";
                    string result = testNode.Attributes?["result"]?.Value ?? "";
                    string type = testNode.Attributes?["type"]?.Value ?? "";
                    string method = testNode.Attributes?["method"]?.Value ?? "";
                    string duration = testNode.Attributes?["time"]?.Value ?? "";
                    XUnitResult testResult = new XUnitResult(
                        file: file,
                        assembly: assemblyName,
                        collection: collectionName,
                        name: testName,
                        type: type,
                        method: method,
                        result: result,
                        duration: duration);
                    if (_results.TryGetValue(testResult.Key, out XUnitResult? previousResult))
                    {
                        Console.WriteLine("Duplicate test result '{0}' in {1} / {2}", testResult.Key, previousResult.File, file);
                    }
                    else
                    {
                        _results.Add(testResult.Key, testResult);
                    }
                }
            }
        }
    }

    public void DumpStatistics(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));

        int passed = 0;
        int failed = 0;
        int skipped = 0;
        int total = 0;
        foreach (XUnitResult result in _results.Values)
        {
            switch (result.Result.ToLower())
            {
                case "pass":
                    passed++;
                    break;

                case "fail":
                    failed++;
                    break;

                case "skipped":
                    skipped++;
                    break;

                default:
                    throw new NotImplementedException(result.Result);
            }
            total++;
        }
        Console.WriteLine("Total tests: {0}", total);
        Console.WriteLine("    Passed:  {0}", passed);
        Console.WriteLine("    Failed:  {0}", failed);
        Console.WriteLine("    Skipped: {0}", skipped);
        Console.WriteLine();
    }

    public static void DiffTestSet(XUnitResultSummary left, XUnitResultSummary right)
    {
        HashSet<string> leftTests = new HashSet<string>();
        leftTests.UnionWith(left._results.Keys);
        leftTests.ExceptWith(right._results.Keys);

        HashSet<string> rightTests = new HashSet<string>();
        rightTests.UnionWith(right._results.Keys);
        rightTests.ExceptWith(left._results.Keys);

        HashSet<string> commonTests = new HashSet<string>();
        commonTests.UnionWith(left._results.Keys);
        commonTests.IntersectWith(right._results.Keys);

        Console.WriteLine("LEFT-ONLY TESTS ({0} total)", leftTests.Count);
        Console.WriteLine("---------------");
        foreach (string leftTest in leftTests.OrderBy(t => t))
        {
            Console.WriteLine(leftTest);
        }

        Console.WriteLine("RIGHT-ONLY TESTS ({0} total)", rightTests.Count);
        Console.WriteLine("----------------");
        foreach (string rightTest in rightTests.OrderBy(t => t))
        {
            Console.WriteLine(rightTest);
        }

        Console.WriteLine("COMMON TESTS WITH DIFFERENT RESULTS ({0} common tests total)", commonTests.Count);
        Console.WriteLine("-----------------------------------");
        Console.WriteLine("LEFT     | RIGHT    | TEST");
        Console.WriteLine("--------------------------");
        foreach (string commonTest in commonTests.OrderBy(t => t))
        {
            XUnitResult leftResult = left._results[commonTest];
            XUnitResult rightResult = right._results[commonTest];
            if (leftResult.Result != rightResult.Result)
            {
                Console.WriteLine("{0,-8} | {1,-8} | {2}", leftResult.Result, rightResult.Result, commonTest);
            }
        }

        Console.WriteLine();
    }
}
