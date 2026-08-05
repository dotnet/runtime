// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// A long-lived query server that answers wasm ABI questions about types, for build tasks that
    /// cannot reference the type system directly.
    /// </summary>
    /// <remarks>
    /// This exists because the WebAssembly build tasks also run on .NET Framework MSBuild, where a
    /// netcoreapp type system assembly cannot be loaded at all. Running it as a separate process keeps
    /// one implementation of the ABI rules instead of a second, drifting one in the task.
    ///
    /// Loading the assembly closure is the expensive part, so the process stays up for the whole build
    /// and answers queries on stdin rather than being spawned per type.
    ///
    /// Usage:
    ///   ILCompiler.Wasm.Lowering --targetos &lt;browser|wasi&gt; [--assembly &lt;path&gt;]... [@responsefile]
    ///
    /// Each stdin line is one query, and each reply line is either the answer or '!' followed by an
    /// error message. Two query forms are supported:
    ///
    ///   t &lt;assemblySimpleName&gt; &lt;typeToken&gt;
    ///     Replies with the ABI encoding of a type in parameter position ('i', 'l', 'f', 'd', 'V' or
    ///     "S&lt;size&gt;").
    ///
    ///   m &lt;assemblySimpleName&gt; &lt;methodToken&gt; &lt;loweringFlags&gt;
    ///     Replies with the full signature string of a method.
    ///
    /// Tokens are decimal or 0x-prefixed hexadecimal; flags are a decimal LoweringFlags value.
    /// </remarks>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string targetOS = null;
            string systemModule = "System.Private.CoreLib";
            var assemblies = new List<string>();

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--targetos":
                            targetOS = args[++i];
                            break;
                        case "--assembly":
                            assemblies.Add(args[++i]);
                            break;
                        case "--systemmodule":
                            systemModule = args[++i];
                            break;
                        default:
                            if (args[i].StartsWith('@'))
                            {
                                assemblies.AddRange(File.ReadAllLines(args[i].Substring(1)));
                                break;
                            }

                            Console.Error.WriteLine($"Unrecognized argument '{args[i]}'.");
                            return 1;
                    }
                }

                if (targetOS is null)
                {
                    Console.Error.WriteLine("Missing required argument --targetos.");
                    return 1;
                }

                assemblies.RemoveAll(string.IsNullOrWhiteSpace);
                var resolver = new WasmAbiTypeResolver(targetOS, assemblies, systemModule);

                // Tells the caller the closure loaded, so a startup failure is not mistaken for a
                // failure of the first query.
                Console.Out.WriteLine("ready");
                Console.Out.Flush();

                Serve(resolver);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static void Serve(WasmAbiTypeResolver resolver)
        {
            string line;
            while ((line = Console.In.ReadLine()) is not null)
            {
                if (line.Length == 0)
                    continue;

                string reply;
                try
                {
                    reply = Answer(resolver, line);
                }
                catch (Exception ex)
                {
                    reply = "!" + ex.Message.Replace('\r', ' ').Replace('\n', ' ');
                }

                Console.Out.WriteLine(reply);
                Console.Out.Flush();
            }
        }

        private static string Answer(WasmAbiTypeResolver resolver, string query)
        {
            if (query.Length < 2 || query[1] != ' ')
                throw new FormatException($"Malformed query '{query}'; expected a 't' or 'm' verb.");

            string rest = query.Substring(2);

            // Parsed right to left so that the assembly name, which is whatever is left over, is not
            // assumed to be free of spaces.
            switch (query[0])
            {
                case 't':
                {
                    (string assemblyName, int typeToken) = SplitToken(rest, query);
                    return resolver.GetAbiToken(assemblyName, typeToken);
                }

                case 'm':
                {
                    (string head, int flags) = SplitToken(rest, query);
                    (string assemblyName, int methodToken) = SplitToken(head, query);
                    return resolver.GetMethodSignature(assemblyName, methodToken, flags);
                }

                default:
                    throw new FormatException($"Unrecognized query verb '{query[0]}'.");
            }
        }

        private static (string Head, int Value) SplitToken(string text, string query)
        {
            int separator = text.LastIndexOf(' ');
            if (separator < 0)
                throw new FormatException($"Malformed query '{query}'; not enough fields.");

            return (text.Substring(0, separator), ParseToken(text.Substring(separator + 1)));
        }

        private static int ParseToken(string text)
        {
            return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? int.Parse(text.Substring(2), System.Globalization.NumberStyles.HexNumber)
                : int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
