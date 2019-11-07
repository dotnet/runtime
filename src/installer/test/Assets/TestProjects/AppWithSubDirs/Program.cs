using System;
using System.IO;
using System.Reflection;

namespace AppWithSubDirs
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string baseDir =
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Sentence");

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
                Part("This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation");
                      
            // This should print "Wow! We now say hello to the big world and you."
            Console.WriteLine(message);
        }
    }
}
