// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Speech.Internal.SrgsParser
{
    /// <summary>
    /// Interface definition for the IGrammar
    /// </summary>
    internal interface IGrammar : IElement
    {
        IRule CreateRule (string id, RulePublic publicRule, RuleDynamic dynamic, bool hasSCript);

        string Root { set; get; }
        System.Speech.Recognition.SrgsGrammar.SrgsTagFormat TagFormat { set; get; }
        Collection<string> GlobalTags { set; get; }
        GrammarType Mode { set; }
        CultureInfo Culture { set; }
        Uri XmlBase { set; }
        AlphabetType PhoneticAlphabet { set; }

        string Language { set; get; }
        string Namespace { set; get; }
        bool Debug { set; }
        Collection<string> CodeBehind { get; set; }
        Collection<string> ImportNamespaces { get; set; }
        Collection<string> AssemblyReferences { get; set; }
    }

    internal enum GrammarType
    {
        VoiceGrammar, DtmfGrammar
    }
}
