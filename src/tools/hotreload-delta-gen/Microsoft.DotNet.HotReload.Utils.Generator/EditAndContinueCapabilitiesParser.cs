using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

public static class EditAndContinueCapabilitiesParser {

    public readonly struct Token {
        public readonly string Value {get; init;}
    }
    static public readonly Regex capabilitiesTokenizer = new (@"^\s*(?:(\S+)\s+)*(\S+)?$", RegexOptions.CultureInvariant);
    public static IEnumerable<Token> Tokenize (string capabilities)
    {
        Match match = capabilitiesTokenizer.Match (capabilities);
        if (!match.Success)
            yield break;
        foreach (Capture c in match.Groups[1].Captures) {
                    yield return new Token { Value = c.Value };
        }
        foreach (Capture c in match.Groups[2].Captures) {
            yield return new Token { Value = c.Value };
        }
    }

    public static IEnumerable<Token> Tokenize (IEnumerable<string> capabilities)
    {
        foreach (var cap in capabilities) {
            foreach (var token in Tokenize(cap))
                yield return token;
        }
    }

    internal static (IEnumerable<EnC.EditAndContinueCapabilities> capabilities, IEnumerable<string> unknowns) Parse (IEnumerable<Token> tokens)
    {
        List<string> unknowns = new();
        List<EnC.EditAndContinueCapabilities> capabilities = new();
        foreach (var tok in tokens)
        {
            if (ParseToken (tok, out var cap)) {
                capabilities.Add (cap);
            } else {
                unknowns.Add (tok.Value);
            }
        }
        return (capabilities, unknowns);
    }

    internal static (IEnumerable<EnC.EditAndContinueCapabilities> capabilities, IEnumerable<string> unknowns) Parse (string capabilities)
    {
        return Parse(Tokenize(capabilities));
    }

    internal static (IEnumerable<EnC.EditAndContinueCapabilities> capabilities, IEnumerable<string> unknowns) Parse (IEnumerable<string> capabilities)
    {
        return Parse(Tokenize(capabilities));
    }

    internal static bool ParseToken (Token token, out EnC.EditAndContinueCapabilities res) =>
        Enum.TryParse<EnC.EditAndContinueCapabilities>(token.Value, ignoreCase: true, out res);

}

