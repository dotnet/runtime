// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Speech.Internal.SrgsParser
{
    /// <summary>
    /// Interface definition for the IGrammar
    /// </summary>
    internal interface IGrammar : IElement
    {
        IRule CreateRule(string id, RulePublic publicRule, RuleDynamic dynamic, bool hasSCript);

        string Root { get; set; }
        System.Speech.Recognition.SrgsGrammar.SrgsTagFormat TagFormat { get; set; }
        Collection<string> GlobalTags { get; set; }
        GrammarType Mode { set; }
        CultureInfo Culture { set; }
        Uri XmlBase { set; }
        AlphabetType PhoneticAlphabet { set; }

        string Language { get; set; }
        string Namespace { get; set; }
        bool Debug { set; }
        Collection<string> CodeBehind { get; set; }
        Collection<string> ImportNamespaces { get; set; }
        Collection<string> AssemblyReferences { get; set; }
    }

    internal enum GrammarType
    {
        VoiceGrammar,
        DtmfGrammar
    }
}
