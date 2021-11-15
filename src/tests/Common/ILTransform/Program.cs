// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class ILTransform
{
    public static int Main(string[] args)
    {
        try
        {
            string testRoot = "";
            bool deduplicateClassNames = false;
            string classToDeduplicate = "";
            bool fixImplicitSharedLibraries = false;
            foreach (string arg in args)
            {
                if (arg[0] == '-')
                {
                    if (arg.StartsWith("-d"))
                    {
                        deduplicateClassNames = true;
                        int index = 2;
                        while (index < arg.Length && !TestProject.IsIdentifier(arg[index]))
                        {
                            index++;
                        }
                        if (index < arg.Length)
                        {
                            classToDeduplicate = arg.Substring(index);
                        }
                    }
                    else if (arg.StartsWith("-i"))
                    {
                        fixImplicitSharedLibraries = true;
                    }
                    else
                    {
                        throw new Exception(string.Format("Unsupported option '{0}'", arg));
                    }
                }
                else
                {
                    testRoot = arg;
                }
            }


            if (testRoot == "")
            {
                throw new Exception("Usage: ILTransform <test source root, e.g. d:\\git\\runtime\\src\\tests> [-d]");
            }

            string wrapperRoot = Path.Combine(testRoot, "generated", "wrappers");
            string logPath = Path.Combine(wrapperRoot, "wrapper.log");
            Directory.CreateDirectory(wrapperRoot);
            foreach (string file in Directory.GetFiles(wrapperRoot))
            {
                File.Delete(file);
            }

            TestProjectStore testStore = new TestProjectStore();
            testStore.ScanTree(testRoot);
            testStore.GenerateExternAliases();

            using (StreamWriter log = new StreamWriter(logPath))
            {
                testStore.DumpFolderStatistics(log);
                testStore.DumpDebugOptimizeStatistics(log);
                testStore.DumpImplicitSharedLibraries(log);
                testStore.DumpDuplicateEntrypointClasses(log);
            }

            if (fixImplicitSharedLibraries)
            {
                // TODO
            }
            else
            {
                testStore.RewriteAllTests(deduplicateClassNames, classToDeduplicate);
                if (!deduplicateClassNames)
                {
                    testStore.GenerateAllWrappers(wrapperRoot);
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: {0}", ex.ToString());
            return 1;
        }
    }
}
