// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace AppWithSubDirs
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string baseDir = Path.Combine(AppContext.BaseDirectory, "Sentence");

            // singlefile app redirects a number of known internal PInvokes to their implementations
            // which are statically linked into the host.
            // Use of File IO, Console APIs and the like uses and tests those mechanisms extensively.
            //
            // There are also two PInvokes in the hostpolicy itself. Test them here.
            // We are not looking for a success, just that we can get to these methods -
            // we should not see errors like "Unable to find an entry point".
            try
            {
                new System.Runtime.Loader.AssemblyDependencyResolver("qwerty");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to locate managed application"))
            {
            }

            string Part(string dir="", string subdir="", string subsubdir="")
            {
                return File.ReadAllText(Path.Combine(baseDir, dir, subdir, subsubdir, "word"));
            }

            string message =
                Part("Interjection") +
                Part("Noun", "Pronoun") +
                Part("Verb", "Adverb") +
                Part("Verb") +
                Part() +
                Part("Noun", "Adjective", "Preposition") +
                Part("Noun", "Adjective", "Article") +
                Part("Noun", "Adjective") +
                Part("Noun") +
                Part("Conjunction") +
                Part("Noun", "Pronoun", "Another") + 
                // The following part with a really long name is generated while running the test.
                Part("This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation");
                      
            // This should print "Wow! We now say hello to the big world and you."
            Console.WriteLine(message);
        }
    }
}
