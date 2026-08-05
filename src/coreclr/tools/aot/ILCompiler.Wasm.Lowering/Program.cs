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
    /// Each stdin line is "&lt;assemblySimpleName&gt; &lt;metadataToken&gt;", where the token is the decimal or
    /// 0x-prefixed hexadecimal metadata token of a type. Each reply line is either the ABI encoding
    /// ('i', 'l', 'f', 'd', 'V' or "S&lt;size&gt;") or '!' followed by an error message.
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
                    int separator = line.LastIndexOf(' ');
                    if (separator < 0)
                        throw new FormatException($"Malformed query '{line}'; expected '<assembly> <token>'.");

                    string assemblyName = line.Substring(0, separator);
                    string tokenText = line.Substring(separator + 1);
                    int metadataToken = tokenText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? int.Parse(tokenText.Substring(2), System.Globalization.NumberStyles.HexNumber)
                        : int.Parse(tokenText, System.Globalization.CultureInfo.InvariantCulture);

                    reply = resolver.GetAbiToken(assemblyName, metadataToken);
                }
                catch (Exception ex)
                {
                    reply = "!" + ex.Message.Replace('\r', ' ').Replace('\n', ' ');
                }

                Console.Out.WriteLine(reply);
                Console.Out.Flush();
            }
        }
    }
}
