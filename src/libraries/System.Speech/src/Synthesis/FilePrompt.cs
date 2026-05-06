// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Speech.Synthesis
{
    [DebuggerDisplay("{_text}")]
    public class FilePrompt : Prompt
    {
        #region Constructors
        public FilePrompt(string path, SynthesisMediaType media)
            : this(new Uri(path, UriKind.Relative), media)
        {
        }
        public FilePrompt(Uri promptFile, SynthesisMediaType media)
            : base(promptFile, media)
        {
        }
        #endregion
    }
}
