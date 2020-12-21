// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Speech.Internal;

namespace System.Speech.Synthesis
{
    [DebuggerDisplay("{_text}")]
    public class Prompt
    {
        #region Constructors
        public Prompt(string textToSpeak)
            : this(textToSpeak, SynthesisTextFormat.Text)
        {
        }
        public Prompt(PromptBuilder promptBuilder)
        {
            Helpers.ThrowIfNull(promptBuilder, nameof(promptBuilder));

            _text = promptBuilder.ToXml();
            _media = SynthesisMediaType.Ssml;
        }

        // Disable parameter validation check for empty strings
#pragma warning disable 56507
        public Prompt(string textToSpeak, SynthesisTextFormat media)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            switch (_media = (SynthesisMediaType)media)
            {
                case SynthesisMediaType.Text:
                case SynthesisMediaType.Ssml:
                    _text = textToSpeak;
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.SynthesizerUnknownMediaType), nameof(media));
            }
        }

#pragma warning restore 56507
        internal Prompt(Uri promptFile, SynthesisMediaType media)
        {
            Helpers.ThrowIfNull(promptFile, nameof(promptFile));

            switch (_media = media)
            {
                case SynthesisMediaType.Text:
                case SynthesisMediaType.Ssml:
                    string localPath;
                    string mimeType;
                    Uri baseUri;
                    using (Stream stream = s_resourceLoader.LoadFile(promptFile, out mimeType, out baseUri, out localPath))
                    {
                        try
                        {
                            using (TextReader reader = new StreamReader(stream))
                            {
                                _text = reader.ReadToEnd();
                            }
                        }
                        finally
                        {
                            s_resourceLoader.UnloadFile(localPath);
                        }
                    }

                    break;

                case SynthesisMediaType.WaveAudio:
                    _text = promptFile.ToString();
                    _audio = promptFile;
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.SynthesizerUnknownMediaType), nameof(media));
            }
        }

        #endregion

        #region public Properties
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
                    throw new ArgumentException(SR.Get(SRID.SynthesizerPromptInUse), nameof(value));
                }

                _synthesizer = value;
            }
        }

        #endregion

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

        #region Private Fields

        /// <summary>
        /// Is this SpeakToken canceled before it was completed?
        /// </summary>
        private bool _completed;

        /// <summary>
        /// The synthesizer this prompt is played on
        /// </summary>
        private object _synthesizer;

        private static ResourceLoader s_resourceLoader = new();

        #endregion
    }

    #region Public Enums
    public enum SynthesisMediaType
    {
        Text = 0,
        Ssml = 1,
        WaveAudio
    }
    public enum SynthesisTextFormat
    {
        Text = 0,
        Ssml = 1,
    }

    #endregion

    #region Internal Types
    internal enum PromptPriority
    {
        Normal,
        High
    }

    #endregion
}
