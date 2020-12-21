// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;
using System.Xml;

namespace System.Speech.Internal.SrgsCompiler
{
    internal static class SrgsCompiler
    {
        #region Internal Methods

        /// <summary>
        /// Loads the SRGS XML grammar and produces the binary grammar format.
        /// </summary>
        /// <param name="xmlReaders">Source SRGS XML streams</param>
        /// <param name="filename">filename to compile to</param>
        /// <param name="stream">stream to compile to</param>
        /// <param name="fOutputCfg">Compile for CFG or DLL</param>
        /// <param name="originalUri">in xmlReader.Count == 1, name of the originel file</param>
        /// <param name="referencedAssemblies">List of referenced assemblies</param>
        /// <param name="keyFile">Strong name</param>
        internal static void CompileStream(XmlReader[] xmlReaders, string filename, Stream stream, bool fOutputCfg, Uri originalUri, string[] referencedAssemblies, string keyFile)
        {
            // raft of files to compiler is only available for class library
            System.Diagnostics.Debug.Assert(!fOutputCfg || xmlReaders.Length == 1);

            int cReaders = xmlReaders.Length;
            List<CustomGrammar.CfgResource> cfgResources = new();

            CustomGrammar cgCombined = new();
            for (int iReader = 0; iReader < cReaders; iReader++)
            {
                // Set the current directory to the location where is the grammar
                string srgsPath = null;
                Uri uri = originalUri;
                if (uri == null)
                {
                    if (xmlReaders[iReader].BaseURI != null && xmlReaders[iReader].BaseURI.Length > 0)
                    {
                        uri = new Uri(xmlReaders[iReader].BaseURI);
                    }
                }
                if (uri != null && (!uri.IsAbsoluteUri || uri.IsFile))
                {
                    srgsPath = Path.GetDirectoryName(uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString);
                }

                CultureInfo culture;
                StringBuilder innerCode = new();
                ISrgsParser srgsParser = new XmlParser(xmlReaders[iReader], uri);
                object cg = CompileStream(iReader + 1, srgsParser, srgsPath, filename, stream, fOutputCfg, innerCode, cfgResources, out culture, referencedAssemblies, keyFile);
                if (!fOutputCfg)
                {
                    cgCombined.Combine((CustomGrammar)cg, innerCode.ToString());
                }
            }

            // Create the DLL if this needs to be done
            if (!fOutputCfg)
            {
                cgCombined.CreateAssembly(filename, cfgResources);
            }
        }

        /// <summary>
        /// Produces the binary grammar format.
        /// </summary>
        /// <param name="srgsGrammar">Source SRGS XML streams</param>
        /// <param name="filename">filename to compile to</param>
        /// <param name="stream">stream to compile to</param>
        /// <param name="fOutputCfg">Compile for CFG or DLL</param>
        /// <param name="referencedAssemblies">List of referenced assemblies</param>
        /// <param name="keyFile">Strong name</param>
        internal static void CompileStream(SrgsDocument srgsGrammar, string filename, Stream stream, bool fOutputCfg, string[] referencedAssemblies, string keyFile)
        {
            ISrgsParser srgsParser = new SrgsDocumentParser(srgsGrammar.Grammar);

            List<CustomGrammar.CfgResource> cfgResources = new();

            StringBuilder innerCode = new();
            CultureInfo culture;

            // Validate the grammar before compiling it. Set the tag-format and sapi flags too.
            srgsGrammar.Grammar.Validate();

            object cg = CompileStream(1, srgsParser, null, filename, stream, fOutputCfg, innerCode, cfgResources, out culture, referencedAssemblies, keyFile);

            // Create the DLL if this needs to be done
            if (!fOutputCfg)
            {
                CustomGrammar cgCombined = new();
                cgCombined.Combine((CustomGrammar)cg, innerCode.ToString());
                cgCombined.CreateAssembly(filename, cfgResources);
            }
        }

        #endregion

        private static object CompileStream(int iCfg, ISrgsParser srgsParser, string srgsPath, string filename, Stream stream, bool fOutputCfg, StringBuilder innerCode, object cfgResources, out CultureInfo culture, string[] referencedAssemblies, string keyFile)
        {
            Backend backend = new();
            CustomGrammar cg = new();
            SrgsElementCompilerFactory elementFactory = new(backend, cg);
            srgsParser.ElementFactory = elementFactory;
            srgsParser.Parse();

            // Optimize in-memory graph representation of the grammar.
            backend.Optimize();
            culture = backend.LangId == 0x540A ? new CultureInfo("es-us") : new CultureInfo(backend.LangId);

            // A grammar may contains references to other files in codebehind.
            // Set the current directory to the location where is the grammar
            if (cg._codebehind.Count > 0 && !string.IsNullOrEmpty(srgsPath))
            {
                for (int i = 0; i < cg._codebehind.Count; i++)
                {
                    if (!File.Exists(cg._codebehind[i]))
                    {
                        cg._codebehind[i] = srgsPath + "\\" + cg._codebehind[i];
                    }
                }
            }

            // Add the referenced assemblies
            if (referencedAssemblies != null)
            {
                foreach (string assembly in referencedAssemblies)
                {
                    cg._assemblyReferences.Add(assembly);
                }
            }

            // Assign the key file
            cg._keyFile = keyFile;

            // Assign the Scripts to the backend
            backend.ScriptRefs = cg._scriptRefs;

            // If the target is a dll, then create first the CFG and stuff it as an embedded resource
            if (!fOutputCfg)
            {
                // Creates a DLL with the CFG is a resource
                // Create the CFG
                CustomGrammar.CfgResource resource = new();
                resource.data = BuildCfg(backend).ToArray();
                resource.name = iCfg.ToString(CultureInfo.InvariantCulture) + ".CFG";
                ((List<CustomGrammar.CfgResource>)cfgResources).Add(resource);

                // Sanity check, create the DLL and checks that all the methods can be called
                // Combine all the scripts
                innerCode.Append(cg.CreateAssembly(iCfg, filename, culture));
            }
            else
            {
                //if semantic processing for a rule is defined, a script needs to be defined
                if (cg._scriptRefs.Count > 0 && !cg.HasScript)
                {
                    XmlParser.ThrowSrgsException(SRID.NoScriptsForRules);
                }

                // Creates a CFG with IL embedded
                CreateAssembly(backend, cg);

                // Save binary grammar to dest
                if (!string.IsNullOrEmpty(filename))
                {
                    // Create a stream if a filename was given
                    stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
                }
                try
                {
                    using (StreamMarshaler streamHelper = new(stream))
                    {
                        backend.Commit(streamHelper);
                    }
                }
                finally
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        stream.Close();
                    }
                }
            }
            return cg;
        }

        /// <summary>
        /// Build the state machine for a grammar and returns it in a Memory Stream.
        /// </summary>
        private static MemoryStream BuildCfg(Backend backend)
        {
            // Creates a DLL
            MemoryStream cfgStream = new();

            // Save binary grammar to dest
            using (StreamMarshaler streamHelper = new(cfgStream))
            {
                // no IL
                backend.IL = null;
                backend.PDB = null;
                // Generate the binary blob
                backend.Commit(streamHelper);
            }
            return cfgStream;
        }

        /// <summary>
        /// Generate the assembly code for a back. The scripts are defined in custom
        /// grammars.
        /// </summary>
        private static void CreateAssembly(Backend backend, CustomGrammar cg)
        {
            if (cg.HasScript)
            {
                // Create the IL and checks that the signature for all the methods for scriptRefs are ok
                // Pass a reference to the IL and scriptRefs to the backend.
                byte[] il;
                byte[] symbols;
                cg.CreateAssembly(out il, out symbols);
                backend.IL = il;
                backend.PDB = symbols;
            }
        }
    }

    internal enum RuleScope
    {
        PublicRule, PrivateRule
    }
}
