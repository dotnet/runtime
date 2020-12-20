// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Speech.Internal;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    [DebuggerDisplay("{_text}")]
    public class FilePrompt : Prompt
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="path"></param>
        /// <param name="media"></param>
        /// <returns></returns>
        public FilePrompt(string path, SynthesisMediaType media)
            : this(new Uri(path, UriKind.Relative), media)
        {
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="promptFile"></param>
        /// <param name="media"></param>
        public FilePrompt(Uri promptFile, SynthesisMediaType media)
            : base(promptFile, media)
        {
        }
        #endregion
    }
}
