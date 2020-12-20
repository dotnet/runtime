// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Speech.Internal;
using System.Speech.Internal.SrgsCompiler;

namespace System.Speech.Recognition.SrgsGrammar
{

    /// <summary>
    /// Compiles Xml Srgs data into a CFG
    /// </summary>

    public static class SrgsGrammarCompiler
    {
        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        /// <summary>
        /// Compiles a grammar to a file
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputStream"></param>
        static public void Compile (string inputPath, Stream outputStream)
        {
            Helpers.ThrowIfEmptyOrNull (inputPath, "inputPath");
            Helpers.ThrowIfNull (outputStream, "outputStream");

            using (XmlTextReader reader = new XmlTextReader (new Uri (inputPath, UriKind.RelativeOrAbsolute).ToString ()))
            {
                SrgsCompiler.CompileStream (new XmlReader [] { reader }, null, outputStream, true, null, null, null);
            }
        }

        /// <summary>
        /// Compiles an Srgs documentto a file
        /// </summary>
        /// <param name="srgsGrammar"></param>
        /// <param name="outputStream"></param>
        static public void Compile (SrgsDocument srgsGrammar, Stream outputStream)
        {
            Helpers.ThrowIfNull (srgsGrammar, "srgsGrammar");
            Helpers.ThrowIfNull (outputStream, "outputStream");

            SrgsCompiler.CompileStream (srgsGrammar, null, outputStream, true, null, null);
        }

        /// <summary>
        /// Compiles a grammar to a file
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="outputStream"></param>
        static public void Compile (XmlReader reader, Stream outputStream)
        {
            Helpers.ThrowIfNull (reader, "reader");
            Helpers.ThrowIfNull (outputStream, "outputStream");

            SrgsCompiler.CompileStream (new XmlReader [] { reader }, null, outputStream, true, null, null, null);
        }


        /// <summary>
        /// Compiles a grammar to a file
        /// </summary>
        /// <param name="inputPaths"></param>
        /// <param name="outputPath"></param>
        /// <param name="referencedAssemblies"></param>
        /// <param name="keyFile"></param>
        static public void CompileClassLibrary (string [] inputPaths, string outputPath, string [] referencedAssemblies, string keyFile)
        {
            Helpers.ThrowIfNull (inputPaths, "inputPaths");
            Helpers.ThrowIfEmptyOrNull (outputPath, "outputPath");

            XmlTextReader [] readers = new XmlTextReader [inputPaths.Length];
            try
            {
                for (int iFile = 0; iFile < inputPaths.Length; iFile++)
                {
                    if (inputPaths [iFile] == null)
                    {
                        throw new ArgumentException (SR.Get (SRID.ArrayOfNullIllegal), "inputPaths");
                    }
                    readers [iFile] = new XmlTextReader (new Uri (inputPaths [iFile], UriKind.RelativeOrAbsolute).ToString ());
                }
                SrgsCompiler.CompileStream (readers, outputPath, null, false, null, referencedAssemblies, keyFile);
            }
            finally
            {
                for (int iReader = 0; iReader < readers.Length; iReader++)
                {
                    XmlTextReader srgsGrammar = readers [iReader];
                    if (srgsGrammar != null)
                    {
                        ((IDisposable) srgsGrammar).Dispose ();
                    }
                }
            }
        }

        /// <summary>
        /// Compiles an Srgs documentto a file
        /// </summary>
        /// <param name="srgsGrammar"></param>
        /// <param name="outputPath"></param>
        /// <param name="referencedAssemblies"></param>
        /// <param name="keyFile"></param>
        static public void CompileClassLibrary (SrgsDocument srgsGrammar, string outputPath, string [] referencedAssemblies, string keyFile)
        {
            Helpers.ThrowIfNull (srgsGrammar, "srgsGrammar");
            Helpers.ThrowIfEmptyOrNull (outputPath, "outputPath");

            SrgsCompiler.CompileStream (srgsGrammar, outputPath, null, false, referencedAssemblies, keyFile);
        }

        /// <summary>
        /// Compiles a grammar to a file
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="outputPath"></param>
        /// <param name="referencedAssemblies"></param>
        /// <param name="keyFile"></param>
        static public void CompileClassLibrary (XmlReader reader, string outputPath, string [] referencedAssemblies, string keyFile)
        {
            Helpers.ThrowIfNull (reader, "reader");
            Helpers.ThrowIfEmptyOrNull (outputPath, "outputPath");

            SrgsCompiler.CompileStream (new XmlReader [] { reader }, outputPath, null, false, null, referencedAssemblies, keyFile);
        }


        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        // Decide if the input stream is a cfg.
        // If not assume it's an xml grammar.
        // The stream parameter points to the start of the data on entry and is reset to that point on exit.
        static private bool CheckIfCfg (Stream stream, out int cfgLength)
        {
            long initialPosition = stream.Position;

            bool isCfg = CfgGrammar.CfgSerializedHeader.IsCfg (stream, out cfgLength);

            // Reset stream position:
            stream.Position = initialPosition;
            return isCfg;
        }

        static internal void CompileXmlOrCopyCfg (
            Stream inputStream,
            Stream outputStream,
            Uri orginalUri)
        {

            // Wrap stream in case Seek is not supported:
            SeekableReadStream seekableInputStream = new SeekableReadStream (inputStream);

            // See if CFG or XML document:
            int cfgLength;
            bool isCFG = CheckIfCfg (seekableInputStream, out cfgLength);

            seekableInputStream.CacheDataForSeeking = false; // Stop buffering data

            if (isCFG)
            {
                // Just copy the input to the output:
                // {We later check the header on the output stream - we could do it on the input stream but it may not be seekable}.
                Helpers.CopyStream (seekableInputStream, outputStream, cfgLength);
            }
            else
            {
                // Else compile the Xml:
                SrgsCompiler.CompileStream (new XmlReader [] { new XmlTextReader (seekableInputStream) }, null, outputStream, true, orginalUri, null, null);
            }
        }

        #endregion

    }
}
