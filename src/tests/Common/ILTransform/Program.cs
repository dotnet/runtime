// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class ILTransform
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                throw new Exception("Usage: ILTransform <test source root, e.g. d:\\git\\runtime\\src\\tests>");
            }
            string testRoot = args[0];
            string wrapperRoot = Path.Combine(testRoot, "wrappers");
            string logPath = Path.Combine(wrapperRoot, "wrapper.log");
            Directory.CreateDirectory(wrapperRoot);
            foreach (string file in Directory.GetFiles(wrapperRoot))
            {
                File.Delete(file);
            }

            TestProjectStore testStore = new TestProjectStore();
            testStore.ScanTree(testRoot);

            using (StreamWriter log = new StreamWriter(logPath))
            {
                testStore.DumpFolderStatistics(log);
                testStore.DumpDebugOptimizeStatistics(log);
                testStore.DumpDuplicateEntrypointClasses(log);
            }

            testStore.RewriteAllTests();
            testStore.GenerateAllWrappers(wrapperRoot);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: {0}", ex.ToString());
            return 1;
        }
    }
}
