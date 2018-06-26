// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace R2RDump
{
    class R2RDump
    {
        private bool _help;
        private IReadOnlyList<string> _inputFilenames = Array.Empty<string>();
        private string _outputFilename = null;
        private bool _raw;
        private bool _header;
        private bool _disasm;
        private IReadOnlyList<string> _queries = Array.Empty<string>();
        private IReadOnlyList<string> _keywords = Array.Empty<string>();
        private IReadOnlyList<int> _runtimeFunctions = Array.Empty<int>();
        private IReadOnlyList<string> _sections = Array.Empty<string>();
        private bool _diff;
        private IntPtr _disassembler;
        private bool _unwind;
        private bool _gc;
        private bool _sectionContents;
        private TextWriter _writer;
        private Dictionary<R2RSection.SectionType, bool> _selectedSections = new Dictionary<R2RSection.SectionType, bool>();

        private R2RDump()
        {
        }

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
                syntax.DefineOption("v|verbose", ref verbose, "Dump raw bytes, disassembly, unwindInfo, gcInfo and section contents");
                syntax.DefineOption("diff", ref _diff, "Compare two R2R images (not yet implemented)");
            });

            if (verbose)
            {
                _raw = true;
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

        public static void WriteWarning(string warning)
        {
            Console.WriteLine($"Warning: {warning}");
        }

        private void WriteDivider(string title)
        {
            int len = 61 - title.Length - 2;
            _writer.WriteLine(new String('=', len/2) + " " + title + " " + new String('=', (int)Math.Ceiling(len/2.0)));
            _writer.WriteLine();
        }

        private void WriteSubDivider()
        {
            _writer.WriteLine("_______________________________________________");
            _writer.WriteLine();
        }

        /// <summary>
        /// Dumps the R2RHeader and all the sections in the header
        /// </summary>
        private void DumpHeader(R2RReader r2r, bool dumpSections)
        {
            _writer.WriteLine(r2r.R2RHeader.ToString());
            if (_raw)
            {
                DumpBytes(r2r, r2r.R2RHeader.RelativeVirtualAddress, (uint)r2r.R2RHeader.Size);
            }
            _writer.WriteLine();
            if (dumpSections)
            {
                WriteDivider("R2R Sections");
                _writer.WriteLine($"{r2r.R2RHeader.Sections.Count} sections");
                _writer.WriteLine();
                foreach (R2RSection section in r2r.R2RHeader.Sections.Values)
                {
                    DumpSection(r2r, section);
                }
            }
            _writer.WriteLine();
        }

        /// <summary>
        /// Dumps one R2RSection
        /// </summary>
        private void DumpSection(R2RReader r2r, R2RSection section)
        {
            WriteSubDivider();
            _writer.WriteLine(section.ToString());
            if (_raw)
            {
                DumpBytes(r2r, section.RelativeVirtualAddress, (uint)section.Size);
                _writer.WriteLine();
            }
            if (_sectionContents)
            {
                DumpSectionContents(r2r, section);
                _writer.WriteLine();
            }
        }

        /// <summary>
        /// Dumps one R2RMethod. 
        /// </summary>
        private void DumpMethod(R2RReader r2r, R2RMethod method)
        {
            WriteSubDivider();
            _writer.WriteLine(method.ToString());
            if (_gc)
            {
                _writer.WriteLine("GcInfo:");
                _writer.Write(method.GcInfo);
                if (_raw)
                {
                    DumpBytes(r2r, method.GcInfo.Offset, (uint)method.GcInfo.Size, false);
                }
            }
            _writer.WriteLine();

            foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
            {
                DumpRuntimeFunction(r2r, runtimeFunction);
            }
        }

        /// <summary>
        /// Dumps one runtime function. 
        /// </summary>
        private void DumpRuntimeFunction(R2RReader r2r, RuntimeFunction rtf)
        {
            _writer.Write($"{rtf}");
            if (_disasm)
            {
                _writer.Write(CoreDisTools.GetCodeBlock(_disassembler, rtf, r2r.GetOffset(rtf.StartAddress), r2r.Image));
            }

            if (_raw)
            {
                _writer.WriteLine("Raw Bytes:");
                DumpBytes(r2r, rtf.StartAddress, (uint)rtf.Size);
            }
            if (_unwind)
            {
                _writer.WriteLine("UnwindInfo:");
                _writer.Write(rtf.UnwindInfo);
                if (_raw)
                {
                    DumpBytes(r2r, rtf.UnwindRVA, (uint)((Amd64.UnwindInfo)rtf.UnwindInfo).Size);
                }
            }
            _writer.WriteLine();
        }

        /// <summary>
        /// Prints a formatted string containing a block of bytes from the relative virtual address and size
        /// </summary>
        public void DumpBytes(R2RReader r2r, int rva, uint size, bool convertToOffset = true)
        {
            int start = rva;
            if (convertToOffset)
                start = r2r.GetOffset(rva);
            if (start > r2r.Image.Length || start + size > r2r.Image.Length)
            {
                throw new IndexOutOfRangeException();
            }
            _writer.Write("    ");
            if (rva % 16 != 0)
            {
                int floor = rva / 16 * 16;
                _writer.Write($"{floor:X8}:");
                _writer.Write(new String(' ', (rva - floor) * 3));
            }
            for (uint i = 0; i < size; i++)
            {
                if ((rva + i) % 16 == 0)
                {
                    _writer.Write($"{rva + i:X8}:");
                }
                _writer.Write($" {r2r.Image[start + i]:X2}");
                if ((rva + i) % 16 == 15 && i != size - 1)
                {
                    _writer.WriteLine();
                    _writer.Write("    ");
                }
            }
            _writer.WriteLine();
        }

        private void DumpSectionContents(R2RReader r2r, R2RSection section)
        {
            switch (section.Type)
            {
                case R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES:
                    uint availableTypesSectionOffset = (uint)r2r.GetOffset(section.RelativeVirtualAddress);
                    NativeParser availableTypesParser = new NativeParser(r2r.Image, availableTypesSectionOffset);
                    NativeHashtable availableTypes = new NativeHashtable(r2r.Image, availableTypesParser, (uint)(availableTypesSectionOffset + section.Size));
                    _writer.WriteLine(availableTypes.ToString());

                    foreach (string name in r2r.AvailableTypes)
                    {
                        _writer.WriteLine(name);
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_METHODDEF_ENTRYPOINTS:
                    NativeArray methodEntryPoints = new NativeArray(r2r.Image, (uint)r2r.GetOffset(section.RelativeVirtualAddress));
                    _writer.Write(methodEntryPoints.ToString());
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS:
                    uint instanceSectionOffset = (uint)r2r.GetOffset(section.RelativeVirtualAddress);
                    NativeParser instanceParser = new NativeParser(r2r.Image, instanceSectionOffset);
                    NativeHashtable instMethodEntryPoints = new NativeHashtable(r2r.Image, instanceParser, (uint)(instanceSectionOffset + section.Size));
                    _writer.Write(instMethodEntryPoints.ToString());
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS:
                    int rtfOffset = r2r.GetOffset(section.RelativeVirtualAddress);
                    int rtfEndOffset = rtfOffset + section.Size;
                    int rtfIndex = 0;
                    while (rtfOffset < rtfEndOffset)
                    {
                        uint rva = NativeReader.ReadUInt32(r2r.Image, ref rtfOffset);
                        _writer.WriteLine($"{rtfIndex}: 0x{rva:X8}");
                        rtfIndex++;
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_COMPILER_IDENTIFIER:
                    _writer.WriteLine(r2r.CompileIdentifier);
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_IMPORT_SECTIONS:
                    foreach (R2RImportSection importSection in r2r.ImportSections)
                    {
                        _writer.Write(importSection.ToString());
                        if (_raw && importSection.Entries.Count != 0)
                        {
                            if (importSection.SectionRVA != 0)
                            {
                                _writer.WriteLine("Section Bytes:");
                                DumpBytes(r2r, importSection.SectionRVA, (uint)importSection.SectionSize);
                            }
                            if (importSection.SignatureRVA != 0)
                            {
                                _writer.WriteLine("Signature Bytes:");
                                DumpBytes(r2r, importSection.SignatureRVA, (uint)importSection.Entries.Count * sizeof(int));
                            }
                            if (importSection.AuxiliaryDataRVA != 0)
                            {
                                _writer.WriteLine("AuxiliaryData Bytes:");
                                DumpBytes(r2r, importSection.AuxiliaryDataRVA, (uint)importSection.AuxiliaryData.Size);
                            }
                        }
                        foreach (R2RImportSection.ImportSectionEntry entry in importSection.Entries)
                        {
                            _writer.WriteLine();
                            _writer.WriteLine(entry.ToString());
                        }
                        _writer.WriteLine();
                    }
                    break;
            }
        }

        // <summary>
        /// For each query in the list of queries, search for all methods matching the query by name, signature or id
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="title">The title to print, "R2R Methods by Query" or "R2R Methods by Keyword"</param>
        /// <param name="queries">The keywords/ids to search for</param>
        /// <param name="exact">Specifies whether to look for methods with names/signatures/ids matching the method exactly or partially</param>
        private void QueryMethod(R2RReader r2r, string title, IReadOnlyList<string> queries, bool exact)
        {
            if (queries.Count > 0)
            {
                WriteDivider(title);
            }
            foreach (string q in queries)
            {
                IList<R2RMethod> res = FindMethod(r2r, q, exact);

                _writer.WriteLine(res.Count + " result(s) for \"" + q + "\"");
                _writer.WriteLine();
                foreach (R2RMethod method in res)
                {
                    DumpMethod(r2r, method);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, search for all sections by the name or value of the ReadyToRunSectionType enum
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The names/values to search for</param>
        private void QuerySection(R2RReader r2r, IReadOnlyList<string> queries)
        {
            if (queries.Count > 0)
            {
                WriteDivider("R2R Section");
            }
            foreach (string q in queries)
            {
                IList<R2RSection> res = FindSection(r2r, q);

                _writer.WriteLine(res.Count + " result(s) for \"" + q + "\"");
                _writer.WriteLine();
                foreach (R2RSection section in res)
                {
                    DumpSection(r2r, section);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, search for a runtime function by id. 
        /// The method containing the runtime function gets outputted, along with the single runtime function that was searched
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The ids to search for</param>
        private void QueryRuntimeFunction(R2RReader r2r, IReadOnlyList<int> queries)
        {
            if (queries.Count > 0)
            {
                WriteDivider("Runtime Functions");
            }
            foreach (int q in queries)
            {
                RuntimeFunction rtf = FindRuntimeFunction(r2r, q);

                if (rtf == null)
                {
                    WriteWarning("Unable to find by id " + q);
                    continue;
                }
                _writer.WriteLine(rtf.Method.SignatureString);
                DumpRuntimeFunction(r2r, rtf);
            }
        }

        /// <summary>
        /// Outputs specified headers, sections, methods or runtime functions for one ReadyToRun image
        /// </summary>
        /// <param name="r2r">The structure containing the info of the ReadyToRun image</param>
        public void Dump(R2RReader r2r)
        {
            _writer.WriteLine($"Filename: {r2r.Filename}");
            _writer.WriteLine($"Machine: {r2r.Machine}");
            _writer.WriteLine($"ImageBase: 0x{r2r.ImageBase:X8}");
            _writer.WriteLine();

            if (_queries.Count == 0 && _keywords.Count == 0 && _runtimeFunctions.Count == 0 && _sections.Count == 0) //dump all sections and methods
            {
                WriteDivider("R2R Header");
                DumpHeader(r2r, true);
                
                if (!_header)
                {
                    WriteDivider("R2R Methods");
                    _writer.WriteLine($"{r2r.R2RMethods.Count} methods");
                    _writer.WriteLine();
                    foreach (R2RMethod method in r2r.R2RMethods)
                    {
                        DumpMethod(r2r, method);
                    }
                }
            }
            else //dump queried sections/methods/runtimeFunctions
            {
                if (_header)
                {
                    DumpHeader(r2r, false);
                }

                QuerySection(r2r, _sections);
                QueryRuntimeFunction(r2r, _runtimeFunctions);
                QueryMethod(r2r, "R2R Methods by Query", _queries, true);
                QueryMethod(r2r, "R2R Methods by Keyword", _keywords, false);
            }

            _writer.WriteLine("=============================================================");
            _writer.WriteLine();
        }

        /// <summary>
        /// Returns true if the name/signature/id of <param>method</param> matches <param>query</param>
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
            
            if (_help)
            {
                _writer.WriteLine(syntax.GetHelpText());
                return 0;
            }

            if (_inputFilenames.Count == 0)
                throw new ArgumentException("Input filename must be specified (--in <file>)");

            // open output stream
            if (_outputFilename != null)
            {
                _writer = File.CreateText(_outputFilename);
            }
            else
            {
                _writer = Console.Out;
            }

            try
            {
                foreach (string filename in _inputFilenames)
                {
                    R2RReader r2r = new R2RReader(filename);
                    if (_disasm)
                    {
                        _disassembler = CoreDisTools.GetDisasm(r2r.Machine);
                    }

                    Dump(r2r);

                    if (_disasm)
                    {
                        CoreDisTools.FinishDisasm(_disassembler);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                return 1;
            }
            finally
            {
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
