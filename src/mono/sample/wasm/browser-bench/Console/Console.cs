// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Mono.Options;

namespace Sample
{
    public partial class Test
    {
        static string tasksArg;

        static List<string> ProcessArguments(string[] args)
        {
            var help = false;
            var options = new OptionSet {
                "Simple mono wasm benchmark",
                "",
                "Copyright 2021 Microsoft Corporation",
                "",
                "Options:",
                { "h|help|?",
                    "Show this message and exit",
                    v => help = v != null },
                { "j|json-results",
                    "Print full results in JSON format",
                    v => JsonResults = v != null },
                { "t|tasks=",
                    "Filter comma separated tasks and its measurements matching, TASK[:REGEX][,TASK[:REGEX],...]. Example: -t Json:non,Exceptions:Inline",
                    v => tasksArg = v },
            };

            var remaining = options.Parse(args);

            if (help || remaining.Count > 0)
            {
                options.WriteOptionDescriptions(Console.Out);

                Environment.Exit(0);
            }

            return remaining;
        }

        public static async Task<int> Main(string[] args)
        {
            ProcessArguments(args);

            if (tasksArg != null)
                SetTasks(tasksArg);

            string output;

            instance.formatter = new PlainFormatter();
            instance.tasks.RemoveAll(t => t.BrowserOnly);

            if (instance.tasks.Count < 1)
            {
                Console.WriteLine("No task(s) to run");
                Environment.Exit(0);
            }

            do
            {
                output = await instance.RunTasks();
                Console.Write(output);
            } while (!string.IsNullOrEmpty(output));

            return 0;
        }
    }
}
