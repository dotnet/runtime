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
    class R2RDump
    {
        private bool _help;
        private IReadOnlyList<string> _inputFilenames = Array.Empty<string>();
        private string _outputFilename = null;
        private bool _xml;
        private XmlDocument _xmlDocument;
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
            if (_xml)
                return;
            int len = 61 - title.Length - 2;
            _writer.WriteLine(new String('=', len/2) + " " + title + " " + new String('=', (int)Math.Ceiling(len/2.0)));
            SkipLine();
        }

        private void WriteSubDivider()
        {
            if(_xml)
                return;
            _writer.WriteLine("_______________________________________________");
            SkipLine();
        }

        private void SkipLine()
        {
            if (_xml)
                return;
            _writer.WriteLine();
        }

        /// <summary>
        /// Dumps the R2RHeader and all the sections in the header
        /// </summary>
        private void DumpHeader(R2RReader r2r, bool dumpSections, XmlNode parentNode)
        {
            XmlNode headerNode = null;
            if (_xml)
            {
                headerNode = _xmlDocument.CreateNode("element", "Header", "");
                parentNode.AppendChild(headerNode);
                Serialize(r2r.R2RHeader, headerNode);
            }
            else
            {
                _writer.WriteLine(r2r.R2RHeader.ToString());
            }
            if (_raw)
            {
                DumpBytes(r2r, r2r.R2RHeader.RelativeVirtualAddress, (uint)r2r.R2RHeader.Size, headerNode);
            }
            SkipLine();
            if (dumpSections)
            {
                XmlNode sectionsNode = null;
                if (_xml)
                {
                    sectionsNode = _xmlDocument.CreateNode("element", "Sections", "");
                    parentNode.AppendChild(sectionsNode);
                    AddXMLNode("Count", r2r.R2RHeader.Sections.Count.ToString(), sectionsNode);
                }
                else
                {
                    WriteDivider("R2R Sections");
                    _writer.WriteLine($"{r2r.R2RHeader.Sections.Count} sections");
                    SkipLine();
                }
                foreach (R2RSection section in r2r.R2RHeader.Sections.Values)
                {
                    DumpSection(r2r, section, sectionsNode);
                }
            }
            SkipLine();
        }

        /// <summary>
        /// Dumps one R2RSection
        /// </summary>
        private void DumpSection(R2RReader r2r, R2RSection section, XmlNode parentNode)
        {
            XmlNode sectionNode = null;
            if (_xml)
            {
                sectionNode = _xmlDocument.CreateNode("element", "Section", "");
                parentNode.AppendChild(sectionNode);
                Serialize(section, sectionNode);
            }
            else
            {
                WriteSubDivider();
                _writer.WriteLine(section.ToString());
            }

            if (_raw)
            {
                DumpBytes(r2r, section.RelativeVirtualAddress, (uint)section.Size, sectionNode);
                SkipLine();
            }
            if (_sectionContents)
            {
                DumpSectionContents(r2r, section, sectionNode);
                SkipLine();
            }
        }

        /// <summary>
        /// Dumps one R2RMethod. 
        /// </summary>
        private void DumpMethod(R2RReader r2r, R2RMethod method, XmlNode parentNode)
        {
            XmlNode methodNode = null;
            if (_xml)
            {
                methodNode = _xmlDocument.CreateNode("element", "Method", "");
                parentNode.AppendChild(methodNode);
                Serialize(method, methodNode);
            }
            else
            {
                WriteSubDivider();
                _writer.WriteLine(method.ToString());
            }
            if (_gc)
            {
                if (_xml)
                {
                    XmlNode gcNode = _xmlDocument.CreateNode("element", "GcInfo", "");
                    methodNode.AppendChild(gcNode);
                    Serialize(method.GcInfo, gcNode);

                    Serialize(new List<GcInfo.GcTransition>(method.GcInfo.Transitions.Values), gcNode);
                }
                else
                {
                    _writer.WriteLine("GcInfo:");
                    _writer.Write(method.GcInfo);
                }

                if (_raw)
                {
                    DumpBytes(r2r, method.GcInfo.Offset, (uint)method.GcInfo.Size, methodNode, false);
                }
            }
            SkipLine();

            XmlNode rtfsNode = null;
            if (_xml)
            {
                rtfsNode = _xmlDocument.CreateNode("element", "RuntimeFunctions", "");
                methodNode.AppendChild(rtfsNode);
            }
            foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
            {
                DumpRuntimeFunction(r2r, runtimeFunction, rtfsNode);
            }
        }

        /// <summary>
        /// Dumps one runtime function. 
        /// </summary>
        private void DumpRuntimeFunction(R2RReader r2r, RuntimeFunction rtf, XmlNode parentNode)
        {
            XmlNode rtfNode = null;
            if (_xml)
            {
                rtfNode = _xmlDocument.CreateNode("element", "RuntimeFunction", "");
                parentNode.AppendChild(rtfNode);
                AddXMLNode("MethodRid", rtf.Method.Rid.ToString(), rtfNode);
                Serialize(rtf, rtfNode);
            }

            if (_disasm)
            {
                string disassembly = CoreDisTools.GetCodeBlock(_disassembler, rtf, r2r.GetOffset(rtf.StartAddress), r2r.Image);
                if (_xml)
                {
                    AddXMLNode("Disassembly", disassembly, rtfNode);
                }
                else
                {
                    _writer.WriteLine($"Id: {rtf.Id}");
                    _writer.Write(disassembly);
                }
            }
            else if (!_xml)
            {
                _writer.Write($"{rtf}");
            }

            if (_raw)
            {
                if (!_xml)
                    _writer.WriteLine("Raw Bytes:");
                DumpBytes(r2r, rtf.StartAddress, (uint)rtf.Size, rtfNode);
            }
            if (_unwind)
            {
                XmlNode unwindNode = null;
                if (_xml)
                {
                    unwindNode = _xmlDocument.CreateNode("element", "UnwindInfo", "");
                    rtfNode.AppendChild(unwindNode);
                    Serialize(rtf.UnwindInfo, unwindNode);
                }
                else
                {
                    _writer.WriteLine("UnwindInfo:");
                    _writer.Write(rtf.UnwindInfo);
                }
                if (_raw)
                {
                    DumpBytes(r2r, rtf.UnwindRVA, (uint)((Amd64.UnwindInfo)rtf.UnwindInfo).Size, unwindNode);
                }
            }
            SkipLine();
        }

        /// <summary>
        /// Prints a formatted string containing a block of bytes from the relative virtual address and size
        /// </summary>
        public void DumpBytes(R2RReader r2r, int rva, uint size, XmlNode parentNode, bool convertToOffset = true)
        {
            int start = rva;
            if (convertToOffset)
                start = r2r.GetOffset(rva);
            if (start > r2r.Image.Length || start + size > r2r.Image.Length)
            {
                throw new IndexOutOfRangeException();
            }

            if (_xml && parentNode != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{r2r.Image[start]:X2}");
                for (uint i = 1; i < size; i++)
                {
                    sb.Append($" {r2r.Image[start + i]:X2}");
                }
                AddXMLNode("Raw", sb.ToString(), parentNode);
                return;
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
                    SkipLine();
                    _writer.Write("    ");
                }
            }
            SkipLine();
        }

        private void DumpSectionContents(R2RReader r2r, R2RSection section, XmlNode contentsNode)
        {
            switch (section.Type)
            {
                case R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES:
                    if(!_xml)
                    {
                        uint availableTypesSectionOffset = (uint)r2r.GetOffset(section.RelativeVirtualAddress);
                        NativeParser availableTypesParser = new NativeParser(r2r.Image, availableTypesSectionOffset);
                        NativeHashtable availableTypes = new NativeHashtable(r2r.Image, availableTypesParser, (uint)(availableTypesSectionOffset + section.Size));
                        _writer.WriteLine(availableTypes.ToString());
                    }

                    if (_xml)
                    {
                        Serialize(r2r.AvailableTypes, contentsNode);
                    }
                    else
                    {
                        foreach (string name in r2r.AvailableTypes)
                        {
                            _writer.WriteLine(name);
                        }
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_METHODDEF_ENTRYPOINTS:
                    if (!_xml)
                    {
                        NativeArray methodEntryPoints = new NativeArray(r2r.Image, (uint)r2r.GetOffset(section.RelativeVirtualAddress));
                        _writer.Write(methodEntryPoints.ToString());
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS:
                    if (!_xml)
                    {
                        uint instanceSectionOffset = (uint)r2r.GetOffset(section.RelativeVirtualAddress);
                        NativeParser instanceParser = new NativeParser(r2r.Image, instanceSectionOffset);
                        NativeHashtable instMethodEntryPoints = new NativeHashtable(r2r.Image, instanceParser, (uint)(instanceSectionOffset + section.Size));
                        _writer.Write(instMethodEntryPoints.ToString());
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS:
                    if (!_xml)
                    {
                        int rtfOffset = r2r.GetOffset(section.RelativeVirtualAddress);
                        int rtfEndOffset = rtfOffset + section.Size;
                        int rtfIndex = 0;
                        while (rtfOffset < rtfEndOffset)
                        {
                            uint rva = NativeReader.ReadUInt32(r2r.Image, ref rtfOffset);
                            _writer.WriteLine($"{rtfIndex}: 0x{rva:X8}");
                            rtfIndex++;
                        }
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_COMPILER_IDENTIFIER:
                    if (!_xml)
                    {
                        _writer.WriteLine(r2r.CompileIdentifier);
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_IMPORT_SECTIONS:
                    foreach (R2RImportSection importSection in r2r.ImportSections)
                    {
                        if (_xml)
                        {
                            Serialize(importSection, contentsNode);
                        }
                        else
                        {
                            _writer.Write(importSection.ToString());
                        }
                        if (_raw && importSection.Entries.Count != 0)
                        {
                            if (importSection.SectionRVA != 0)
                            {
                                XmlNode bytesNode = null;
                                if (_xml)
                                {
                                    bytesNode = _xmlDocument.CreateNode("element", "SectionBytes", "");
                                    contentsNode.AppendChild(bytesNode);
                                }
                                else
                                {
                                    _writer.WriteLine("Section Bytes:");
                                }
                                DumpBytes(r2r, importSection.SectionRVA, (uint)importSection.SectionSize, bytesNode);
                            }
                            if (importSection.SignatureRVA != 0)
                            {
                                XmlNode bytesNode = null;
                                if (_xml)
                                {
                                    bytesNode = _xmlDocument.CreateNode("element", "SignatureBytes", "");
                                    contentsNode.AppendChild(bytesNode);
                                }
                                else
                                {
                                    _writer.WriteLine("Signature Bytes:");
                                }
                                DumpBytes(r2r, importSection.SignatureRVA, (uint)importSection.Entries.Count * sizeof(int), bytesNode);
                            }
                            if (importSection.AuxiliaryDataRVA != 0)
                            {
                                XmlNode bytesNode = null;
                                if (_xml)
                                {
                                    bytesNode = _xmlDocument.CreateNode("element", "AuxiliaryDataBytes", "");
                                    contentsNode.AppendChild(bytesNode);
                                }
                                else
                                {
                                    _writer.WriteLine("AuxiliaryData Bytes:");
                                }
                                DumpBytes(r2r, importSection.AuxiliaryDataRVA, (uint)importSection.AuxiliaryData.Size, bytesNode);
                            }
                        }
                        if (!_xml)
                        {
                            foreach (R2RImportSection.ImportSectionEntry entry in importSection.Entries)
                            {
                                _writer.WriteLine();
                                _writer.WriteLine(entry.ToString());
                            }
                            _writer.WriteLine();
                        }
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
        private void QueryMethod(R2RReader r2r, string title, IReadOnlyList<string> queries, bool exact, XmlNode parentNode)
        {
            if (queries.Count > 0)
            {
                WriteDivider(title);
            }
            foreach (string q in queries)
            {
                IList<R2RMethod> res = FindMethod(r2r, q, exact);
                XmlNode queryNode = null;
                if (_xml)
                {
                    queryNode = _xmlDocument.CreateNode("element", "Methods", "");
                    parentNode.AppendChild(queryNode);
                    AddXMLNode("Query", q, queryNode);
                    AddXMLNode("Count", res.Count.ToString(), queryNode);
                }
                else
                {
                    _writer.WriteLine(res.Count + " result(s) for \"" + q + "\"");
                    SkipLine();
                }
                foreach (R2RMethod method in res)
                {
                    DumpMethod(r2r, method, queryNode);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, search for all sections by the name or value of the ReadyToRunSectionType enum
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The names/values to search for</param>
        private void QuerySection(R2RReader r2r, IReadOnlyList<string> queries, XmlNode parentNode)
        {
            if (queries.Count > 0)
            {
                WriteDivider("R2R Section");
            }
            foreach (string q in queries)
            {
                IList<R2RSection> res = FindSection(r2r, q);
                XmlNode queryNode = null;
                if (_xml)
                {
                    queryNode = _xmlDocument.CreateNode("element", "Sections", "");
                    parentNode.AppendChild(queryNode);
                    AddXMLNode("Query", q, queryNode);
                    AddXMLNode("Count", res.Count.ToString(), queryNode);
                }
                else
                {
                    _writer.WriteLine(res.Count + " result(s) for \"" + q + "\"");
                    SkipLine();
                }
                foreach (R2RSection section in res)
                {
                    DumpSection(r2r, section, queryNode);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, search for a runtime function by id. 
        /// The method containing the runtime function gets outputted, along with the single runtime function that was searched
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The ids to search for</param>
        private void QueryRuntimeFunction(R2RReader r2r, IReadOnlyList<int> queries, XmlNode parentNode)
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
                XmlNode queryNode = null;
                if (_xml)
                {
                    queryNode = _xmlDocument.CreateNode("element", "RuntimeFunctions", "");
                    parentNode.AppendChild(queryNode);
                    AddXMLNode("Query", q.ToString(), queryNode);
                    AddXMLNode("Count", "1", queryNode);
                }
                else
                {
                    _writer.WriteLine(rtf.Method.SignatureString);
                }
                DumpRuntimeFunction(r2r, rtf, queryNode);
            }
        }

        /// <summary>
        /// Outputs specified headers, sections, methods or runtime functions for one ReadyToRun image
        /// </summary>
        /// <param name="r2r">The structure containing the info of the ReadyToRun image</param>
        public void Dump(R2RReader r2r)
        {
            XmlNode rootNode = null;
            if (_xml)
            {
                rootNode = _xmlDocument.CreateNode("element", "R2RDump", "");
                _xmlDocument.AppendChild(rootNode);
                Serialize(r2r, rootNode);
            }
            else
            {
                _writer.WriteLine($"Filename: {r2r.Filename}");
                _writer.WriteLine($"Machine: {r2r.Machine}");
                _writer.WriteLine($"ImageBase: 0x{r2r.ImageBase:X8}");
                SkipLine();
            }

            if (_queries.Count == 0 && _keywords.Count == 0 && _runtimeFunctions.Count == 0 && _sections.Count == 0) //dump all sections and methods
            {
                WriteDivider("R2R Header");
                DumpHeader(r2r, true, rootNode);
                
                if (!_header)
                {
                    XmlNode methodsNode = null;
                    if (_xml)
                    {
                        methodsNode = _xmlDocument.CreateNode("element", "Methods", "");
                        rootNode.AppendChild(methodsNode);
                        AddXMLNode("Count", r2r.R2RMethods.Count.ToString(), methodsNode);
                    }
                    else
                    {
                        WriteDivider("R2R Methods");
                        _writer.WriteLine($"{r2r.R2RMethods.Count} methods");
                        SkipLine();
                    }
                    foreach (R2RMethod method in r2r.R2RMethods)
                    {
                        DumpMethod(r2r, method, methodsNode);
                    }
                }
            }
            else //dump queried sections/methods/runtimeFunctions
            {
                if (_header)
                {
                    DumpHeader(r2r, false, rootNode);
                }

                QuerySection(r2r, _sections, rootNode);
                QueryRuntimeFunction(r2r, _runtimeFunctions, rootNode);
                QueryMethod(r2r, "R2R Methods by Query", _queries, true, rootNode);
                QueryMethod(r2r, "R2R Methods by Keyword", _keywords, false, rootNode);
            }
            if (!_xml)
            {
                _writer.WriteLine("=============================================================");
                SkipLine();
            }
        }

        private void Serialize(object obj, XmlNode node)
        {
            using (XmlWriter xmlWriter = node.CreateNavigator().AppendChild())
            {
                xmlWriter.WriteWhitespace("");
                XmlSerializer Serializer = new XmlSerializer(obj.GetType());
                Serializer.Serialize(xmlWriter, obj);
            }
        }

        private XmlNode AddXMLNode(String name, String contents, XmlNode parentNode)
        {
            XmlNode node = _xmlDocument.CreateNode("element", name, "");
            parentNode.AppendChild(node);
            node.InnerText = contents;
            return node;
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

            if (_inputFilenames.Count == 0)
                throw new ArgumentException("Input filename must be specified (--in <file>)");

            try
            {
                foreach (string filename in _inputFilenames)
                {
                    R2RReader r2r = new R2RReader(filename);
                    if (_xml)
                    {
                        _xmlDocument = new XmlDocument();
                    }

                    if (_disasm)
                    {
                        _disassembler = CoreDisTools.GetDisasm(r2r.Machine);
                    }

                    Dump(r2r);

                    if (_disasm)
                    {
                        CoreDisTools.FinishDisasm(_disassembler);
                    }

                    if (_xml)
                    {
                        _xmlDocument.Save(_writer);
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
