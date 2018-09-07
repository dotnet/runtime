// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace R2RDump
{
    public abstract class Dumper
    {
        internal R2RReader _r2r;
        internal TextWriter _writer;

        internal bool _raw;
        internal bool _header;
        internal bool _disasm;
        internal Disassembler _disassembler;
        internal bool _unwind;
        internal bool _gc;
        internal bool _sectionContents;

        /// <summary>
        /// Run right before printing output
        /// </summary>
        abstract internal void Begin();

        /// <summary>
        /// Run right after printing output
        /// </summary>
        abstract internal void End();
        abstract internal void WriteDivider(string title);
        abstract internal void WriteSubDivider();
        abstract internal void SkipLine();
        abstract internal void DumpHeader(bool dumpSections);
        abstract internal void DumpSection(R2RSection section, XmlNode parentNode = null);
        abstract internal void DumpAllMethods();
        abstract internal void DumpMethod(R2RMethod method, XmlNode parentNode = null);
        abstract internal void DumpRuntimeFunction(RuntimeFunction rtf, XmlNode parentNode = null);
        abstract internal void DumpDisasm(RuntimeFunction rtf, int imageOffset, XmlNode parentNode = null);
        abstract internal void DumpBytes(int rva, uint size, XmlNode parentNode = null, string name = "Raw", bool convertToOffset = true);
        abstract internal void DumpSectionContents(R2RSection section, XmlNode parentNode = null);
        abstract internal XmlNode DumpQueryCount(string q, string title, int count);
    }

    class R2RDump
    {
        // Options set by user specifying what to dump
        private bool _help;
        private IReadOnlyList<string> _inputFilenames = Array.Empty<string>();
        private string _outputFilename = null;
        private bool _xml;
        private bool _raw;
        private bool _header;
        private bool _disasm;
        private IReadOnlyList<string> _queries = Array.Empty<string>();
        private IReadOnlyList<string> _keywords = Array.Empty<string>();
        private IReadOnlyList<int> _runtimeFunctions = Array.Empty<int>();
        private IReadOnlyList<string> _sections = Array.Empty<string>();
        private bool _diff;
        private bool _unwind;
        private bool _gc;
        private bool _sectionContents;
        private TextWriter _writer;
        private Dictionary<R2RSection.SectionType, bool> _selectedSections = new Dictionary<R2RSection.SectionType, bool>();
        private Dumper _dumper;
        private bool _ignoreSensitive;

        private R2RDump()
        {
        }

        /// <summary>
        /// Parse commandline options
        /// </summary>
        private ArgumentSyntax ParseCommandLine(string[] args)
        {
            bool verbose = false;
            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = "R2RDump";
                syntax.HandleHelp = false;
                syntax.HandleErrors = true;

                syntax.DefineOption("h|help", ref _help, "Help message for R2RDump");
                syntax.DefineOptionList("i|in", ref _inputFilenames, "Input file(s) to dump. Expects them to by ReadyToRun images");
                syntax.DefineOption("o|out", ref _outputFilename, "Output file path. Dumps everything to the specified file except help message and exception messages");
                syntax.DefineOption("x|xml", ref _xml, "Output in XML format");
                syntax.DefineOption("raw", ref _raw, "Dump the raw bytes of each section or runtime function");
                syntax.DefineOption("header", ref _header, "Dump R2R header");
                syntax.DefineOption("d|disasm", ref _disasm, "Show disassembly of methods or runtime functions");
                syntax.DefineOptionList("q|query", ref _queries, "Query method by exact name, signature, row id or token");
                syntax.DefineOptionList("k|keyword", ref _keywords, "Search method by keyword");
                syntax.DefineOptionList("r|runtimefunction", ref _runtimeFunctions, ArgStringToInt, "Get one runtime function by id or relative virtual address");
                syntax.DefineOptionList("s|section", ref _sections, "Get section by keyword");
                syntax.DefineOption("unwind", ref _unwind, "Dump unwindInfo");
                syntax.DefineOption("gc", ref _gc, "Dump gcInfo and slot table");
                syntax.DefineOption("sc", ref _sectionContents, "Dump section contents");
                syntax.DefineOption("v|verbose", ref verbose, "Dump disassembly, unwindInfo, gcInfo and section contents");
                syntax.DefineOption("diff", ref _diff, "Compare two R2R images");
                syntax.DefineOption("ignoreSensitive", ref _ignoreSensitive, "Ignores sensitive properties in xml dump to avoid failing tests");
            });

            if (verbose)
            {
                _disasm = true;
                _unwind = true;
                _gc = true;
                _sectionContents = true;
            }

            return argSyntax;
        }

        private int ArgStringToInt(string arg)
        {
            int n;
            if (!ArgStringToInt(arg, out n))
            {
                throw new ArgumentException("Can't parse argument to int");
            }
            return n;
        }

        /// <summary>
        /// Converts string passed as cmd line args into int, works for hexidecimal with 0x as prefix
        /// </summary>
        /// <param name="arg">The argument string to convert</param>
        /// <param name="n">The integer representation</param>
        private bool ArgStringToInt(string arg, out int n)
        {
			arg = arg.Trim();
            if (arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(arg.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out n);
            }
            return int.TryParse(arg, out n);
        }

        /// <summary>
        /// Outputs a warning message
        /// </summary>
        /// <param name="warning">The warning message to output</param>
        public static void WriteWarning(string warning)
        {
            Console.WriteLine($"Warning: {warning}");
        }

        // <summary>
        /// For each query in the list of queries, dump all methods matching the query by name, signature or id
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="title">The title to print, "R2R Methods by Query" or "R2R Methods by Keyword"</param>
        /// <param name="queries">The keywords/ids to search for</param>
        /// <param name="exact">Specifies whether to look for methods with names/signatures/ids matching the method exactly or partially</param>
        private void QueryMethod(R2RReader r2r, string title, IReadOnlyList<string> queries, bool exact)
        {
            if (queries.Count > 0)
            {
                _dumper.WriteDivider(title);
            }
            foreach (string q in queries)
            {
                IList<R2RMethod> res = FindMethod(r2r, q, exact);
                XmlNode queryNode = _dumper.DumpQueryCount(q, "Methods", res.Count);
                foreach (R2RMethod method in res)
                {
                    _dumper.DumpMethod(method, queryNode);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, dump all sections by the name or value of the ReadyToRunSectionType enum
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The names/values to search for</param>
        private void QuerySection(R2RReader r2r, IReadOnlyList<string> queries)
        {
            if (queries.Count > 0)
            {
                _dumper.WriteDivider("R2R Section");
            }
            foreach (string q in queries)
            {
                IList<R2RSection> res = FindSection(r2r, q);
                XmlNode queryNode = _dumper.DumpQueryCount(q, "Sections", res.Count);
                foreach (R2RSection section in res)
                {
                    _dumper.DumpSection(section, queryNode);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, dump a runtime function by id. 
        /// The method containing the runtime function gets outputted, along with the single runtime function that was searched
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The ids to search for</param>
        private void QueryRuntimeFunction(R2RReader r2r, IReadOnlyList<int> queries)
        {
            if (queries.Count > 0)
            {
                _dumper.WriteDivider("Runtime Functions");
            }
            foreach (int q in queries)
            {
                RuntimeFunction rtf = FindRuntimeFunction(r2r, q);

                if (rtf == null)
                {
                    WriteWarning("Unable to find by id " + q);
                    continue;
                }
                XmlNode queryNode = _dumper.DumpQueryCount(q.ToString(), "Runtime Function", 1);
                _dumper.DumpRuntimeFunction(rtf, queryNode);
            }
        }

        /// <summary>
        /// Outputs specified headers, sections, methods or runtime functions for one ReadyToRun image
        /// </summary>
        /// <param name="r2r">The structure containing the info of the ReadyToRun image</param>
        public void Dump(R2RReader r2r)
        {

            _dumper.Begin();

            if (_queries.Count == 0 && _keywords.Count == 0 && _runtimeFunctions.Count == 0 && _sections.Count == 0) //dump all sections and methods if no queries specified
            {
                _dumper.WriteDivider("R2R Header");
                _dumper.DumpHeader(true);
                
                if (!_header)
                {
                    _dumper.DumpAllMethods();
                }
            }
            else //dump queried sections, methods and runtimeFunctions
            {
                if (_header)
                {
                    _dumper.DumpHeader(false);
                }

                QuerySection(r2r, _sections);
                QueryRuntimeFunction(r2r, _runtimeFunctions);
                QueryMethod(r2r, "R2R Methods by Query", _queries, true);
                QueryMethod(r2r, "R2R Methods by Keyword", _keywords, false);
            }

            _dumper.End();
        }

        /// <summary>
        /// Returns true if the name, signature or id of <param>method</param> matches <param>query</param>
        /// </summary>
        /// <param name="exact">Specifies exact or partial match</param>
        /// <remarks>Case-insensitive and ignores whitespace</remarks>
        private bool Match(R2RMethod method, string query, bool exact)
        {
            int id;
            bool isNum = ArgStringToInt(query, out id);
            bool idMatch = isNum && (method.Rid == id || method.Token == id);

            bool sigMatch = false;
            if (exact)
            {
                sigMatch = method.Name.Equals(query, StringComparison.OrdinalIgnoreCase);
                if (!sigMatch)
                {
                    string sig = method.SignatureString.Replace(" ", "");
                    string q = query.Replace(" ", "");
                    int iMatch = sig.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                    sigMatch = (iMatch == 0 || (iMatch > 0 && iMatch == (sig.Length - q.Length) && sig[iMatch - 1] == '.'));
                }
            }
            else
            {
                string sig = method.Signature.ReturnType + method.SignatureString.Replace(" ", "");
                sigMatch = (sig.IndexOf(query.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return idMatch || sigMatch;
        }

        /// <summary>
        /// Returns true if the name or value of the ReadyToRunSectionType of <param>section</param> matches <param>query</param>
        /// </summary>
        /// <remarks>Case-insensitive</remarks>
        private bool Match(R2RSection section, string query)
        {
            int queryInt;
            bool isNum = ArgStringToInt(query, out queryInt);
            string typeName = Enum.GetName(typeof(R2RSection.SectionType), section.Type);

            return (isNum && (int)section.Type == queryInt) || typeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Finds all R2RMethods by name/signature/id matching <param>query</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="query">The name/signature/id to search for</param>
        /// <param name="exact">Specifies exact or partial match</param>
        /// <out name="res">List of all matching methods</out>
        /// <remarks>Case-insensitive and ignores whitespace</remarks>
        public IList<R2RMethod> FindMethod(R2RReader r2r, string query, bool exact)
        {
            List<R2RMethod> res = new List<R2RMethod>();
            foreach (R2RMethod method in r2r.R2RMethods)
            {
                if (Match(method, query, exact))
                {
                    res.Add(method);
                }
            }
            return res;
        }

        /// <summary>
        /// Finds all R2RSections by name or value of the ReadyToRunSectionType matching <param>query</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="query">The name or value to search for</param>
        /// <out name="res">List of all matching sections</out>
        /// <remarks>Case-insensitive</remarks>
        public IList<R2RSection> FindSection(R2RReader r2r, string query)
        {
            List<R2RSection> res = new List<R2RSection>();
            foreach (R2RSection section in r2r.R2RHeader.Sections.Values)
            {
                if (Match(section, query))
                {
                    res.Add(section);
                }
            }
            return res;
        }

        /// <summary>
        /// Returns the runtime function with id matching <param>rtfQuery</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="rtfQuery">The name or value to search for</param>
        public RuntimeFunction FindRuntimeFunction(R2RReader r2r, int rtfQuery)
        {
            foreach (R2RMethod m in r2r.R2RMethods)
            {
                foreach (RuntimeFunction rtf in m.RuntimeFunctions)
                {
                    if (rtf.Id == rtfQuery || (rtf.StartAddress >= rtfQuery && rtf.StartAddress + rtf.Size < rtfQuery))
                    {
                        return rtf;
                    }
                }
            }
            return null;
        }

        private int Run(string[] args)
        {
            ArgumentSyntax syntax = ParseCommandLine(args);

            // open output stream
            if (_outputFilename != null)
            {
                _writer = File.CreateText(_outputFilename);
            }
            else
            {
                _writer = Console.Out;
            }

            if (_help)
            {
                _writer.WriteLine(syntax.GetHelpText());
                return 0;
            }

            Disassembler disassembler = null;

            try
            {
                if (_inputFilenames.Count == 0)
                    throw new ArgumentException("Input filename must be specified (--in <file>)");

                if (_diff && _inputFilenames.Count < 2)
                    throw new ArgumentException("Need at least 2 input files in diff mode");

                R2RReader previousReader = null;

                foreach (string filename in _inputFilenames)
                {
                    // parse the ReadyToRun image
                    R2RReader r2r = new R2RReader(filename);

                    if (_disasm)
                    {
                        if (r2r.InputArchitectureSupported() && r2r.DisassemblerArchitectureSupported())
                        {
                            disassembler = new Disassembler(r2r);
                        }
                        else
                        {
                            throw new ArgumentException($"The architecture of input file {filename} ({r2r.Machine.ToString()}) or the architecture of the disassembler tools ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()}) is not supported.");
                        }
                    }

                    if (_xml)
                    {
                        _dumper = new XmlDumper(_ignoreSensitive, r2r, _writer, _raw, _header, _disasm, disassembler, _unwind, _gc, _sectionContents);
                    }
                    else
                    {
                        _dumper = new TextDumper(r2r, _writer, _raw, _header, _disasm, disassembler, _unwind, _gc, _sectionContents);
                    }

                    if (!_diff)
                    {
                        // output the ReadyToRun info
                        Dump(r2r);
                    }
                    else if (previousReader != null)
                    {
                        new R2RDiff(previousReader, r2r, _writer).Run();
                    }

                    previousReader = r2r;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                if (e is ArgumentException)
                {
                    Console.WriteLine();
                    Console.WriteLine(syntax.GetHelpText());
                }
                if (_xml)
                {
                    XmlDocument document = new XmlDocument();
                    XmlNode node = document.CreateNode("element", "Error", "");
                    node.InnerText = e.Message;
                    document.AppendChild(node);
                    if (_writer != null)
                    {
                        document.Save(_writer);
                    }
                }
                return 1;
            }
            finally
            {
                if (disassembler != null)
                {
                    disassembler.Dispose();
                }
                // close output stream
                _writer.Close();
            }

            return 0;
        }

        public static int Main(string[] args)
        {
            try
            {
                return new R2RDump().Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                return 1;
            }
        }
    }
}
