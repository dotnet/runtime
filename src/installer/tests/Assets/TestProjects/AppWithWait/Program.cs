// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Reflection;

namespace AppWithSubDirs
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Write("Hello ");

            // If the caller wants the app to start and wait, it provides the names of two files.
            // In this case, this test app creates the waitFile, and waits until resumeFile is created
            if (args.Length == 2)
            {
                string waitFile = args[0];
                string resumeFile = args[1];

                // Once this app creates the waitFile and yields control, the test-harness renames this single-file app bundle.
                // Therefore, any assemblies loaded directly from the bundle, should be loaded before creating the waitFile.
                Assembly.Load("System.Memory");

                File.Create(waitFile).Close();

                Thread.Sleep(200);

                while (!File.Exists(resumeFile))
                {
                    Thread.Sleep(100);
                }
            }

            Console.WriteLine("World!");
        }
    }
}
