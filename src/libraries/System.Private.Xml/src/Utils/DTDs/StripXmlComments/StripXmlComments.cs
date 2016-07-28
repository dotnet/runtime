// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace StripComments {
    class Program {

        static bool inComment = false;

        const bool StripEmptyLines = false;
        const bool UseUnixNewLine = true;

        static string FilterComments(string line) {
            int indexStart, indexEnd;
            if (inComment) {
                if ((indexEnd = line.IndexOf("-->")) != -1) {
                    line = line.Substring(indexEnd + 3);
                    inComment = false;
                    return line;
                }
                else {
                    return string.Empty;
                }
            }
            else {
                if ((indexStart = line.IndexOf("<!--")) != -1) {
                    if ((indexEnd = line.IndexOf("-->")) != -1) {
                        if (indexEnd + 3 == line.Length) {
                            line = line.Substring(0, indexStart);
                            line = line.TrimEnd();
                        }
                        else {
                            line = string.Concat(line.Substring(0, indexStart), line.Substring(indexEnd + 3));
                            return FilterComments(line);
                        }
                    }
                    else {
                        line = line.Substring(0, indexStart);
                        inComment = true;
                    }
                }
                return line;
            }
        }

        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("USAGE: StripSpaces input_file output_file");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];

            using (StreamReader input = new StreamReader(inputFile)) {
                using (StreamWriter output = new StreamWriter(outputFile)) {
                    if (UseUnixNewLine) {
                        output.NewLine = "\n";
                    }
                    string line;
                    while ((line = input.ReadLine()) != null) {
                        line = FilterComments(line);
                        if (line != null) {
                            if (!StripEmptyLines || line.Length > 0) {
                                output.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }
    }
}
