// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Speech.Internal;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    [DebuggerDisplay ("{_text}")]
    public class Prompt
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
        /// <param name="textToSpeak"></param>
        public Prompt (string textToSpeak)
            : this (textToSpeak, SynthesisTextFormat.Text)
        {
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="promptBuilder"></param>
        /// <returns></returns>
        public Prompt (PromptBuilder promptBuilder)
        {
            Helpers.ThrowIfNull (promptBuilder, "promptBuilder");

            _text = promptBuilder.ToXml ();
            _media = SynthesisMediaType.Ssml;
        }

        // Disable parameter validation check for empty strings
#pragma warning disable 56507

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="textToSpeak"></param>
        /// <param name="media"></param>
        public Prompt (string textToSpeak, SynthesisTextFormat media)
        {
            Helpers.ThrowIfNull (textToSpeak, "textToSpeak");

            switch (_media = (SynthesisMediaType) media)
            {
                case SynthesisMediaType.Text:
                case SynthesisMediaType.Ssml:
                    _text = textToSpeak;
                    break;

                default:
                    throw new ArgumentException (SR.Get (SRID.SynthesizerUnknownMediaType), "media");
            }
        }

#pragma warning restore 56507

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="promptFile"></param>
        /// <param name="media"></param>
        internal Prompt (Uri promptFile, SynthesisMediaType media)
        {
            Helpers.ThrowIfNull (promptFile, "promptFile");

            switch (_media = media)
            {
                case SynthesisMediaType.Text:
                case SynthesisMediaType.Ssml:
                    string localPath;
                    string mimeType;
                    Uri baseUri;
                    using (Stream stream = _resourceLoader.LoadFile (promptFile, out mimeType, out baseUri, out localPath))
                    {
                        try
                        {
                            using (TextReader reader = new StreamReader (stream))
                            {
                                _text = reader.ReadToEnd ();
                            }
                        }
                        finally
                        {
                            _resourceLoader.UnloadFile (localPath);
                        }
                    }

                    break;

                case SynthesisMediaType.WaveAudio:
                    _text = promptFile.ToString ();
                    _audio = promptFile;
                    break;

                default:
                    throw new ArgumentException (SR.Get (SRID.SynthesizerUnknownMediaType), "media");
            }
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public bool IsCompleted
        {
            get
            {
                return _completed;
            }
            internal set
            {
                _completed = value;
            }
        }

        internal object Synthesizer
        {
            set
            {
                if (value != null && (_synthesizer != null || _completed))
                {
                    throw new ArgumentException (SR.Get (SRID.SynthesizerPromptInUse), "value");
                }

                _synthesizer = value;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Internal Fields
        //
        //*******************************************************************

        #region Internal Fields

        /// <summary>
        /// Could be some raw text or SSML doc or the file name (wave file)
        /// </summary>
        internal string _text;

        /// <summary>
        /// Audio data
        /// </summary>
        internal Uri _audio;

        /// <summary>
        /// Unused at this point
        /// </summary>
        internal SynthesisMediaType _media;

        /// <summary>
        /// Is this prompt played asynchrounously
        /// </summary>
        internal bool _syncSpeak;

        /// <summary>
        /// What errors occurred during this operation?
        /// </summary>
        internal Exception _exception;

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        /// <summary>
        /// Is this SpeakToken canceled before it was completed?
        /// </summary>
        private bool _completed;

        /// <summary>
        /// The synthesizer this prompt is played on
        /// </summary>
        private object _synthesizer;

        static private ResourceLoader _resourceLoader = new ResourceLoader ();

        #endregion
    }

    //*******************************************************************
    //
    // Public Enums
    //
    //*******************************************************************

    #region Public Enums

    /// <summary>
    /// TODOC
    /// </summary>
    public enum SynthesisMediaType
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Text = 0,
        /// <summary>
        /// TODOC
        /// </summary>
        Ssml = 1,
        /// <summary>
        /// TODOC
        /// </summary>
        WaveAudio
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum SynthesisTextFormat
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Text = 0,
        /// <summary>
        /// TODOC
        /// </summary>
        Ssml = 1,
    }

    #endregion

    //*******************************************************************
    //
    // Internal Types
    //
    //*******************************************************************

    #region Internal Types

    /// <summary>
    /// TODOC
    /// </summary>
    internal enum PromptPriority
    {
        Normal,
        High
    }

    #endregion

}
