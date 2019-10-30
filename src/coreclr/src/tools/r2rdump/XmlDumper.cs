using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace R2RDump
{
    public class XmlDumper : Dumper
    {
        public XmlDocument XmlDocument { get; }
        private XmlNode _rootNode;
        private bool _ignoreSensitive;
        private XmlAttributeOverrides _ignoredProperties;

        public XmlDumper(bool ignoreSensitive, R2RReader r2r, TextWriter writer, Disassembler disassembler, DumpOptions options)
            : base(r2r, writer, disassembler, options)
        {
            _ignoreSensitive = ignoreSensitive;
            XmlDocument = new XmlDocument();

            _ignoredProperties = new XmlAttributeOverrides();
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlIgnore = _ignoreSensitive;
            _ignoredProperties.Add(typeof(R2RHeader), "RelativeVirtualAddress", attrs);
            _ignoredProperties.Add(typeof(R2RHeader), "Size", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection), "SectionRVA", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection), "SectionSize", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection), "EntrySize", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection), "SignatureRVA", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection), "AuxiliaryDataRVA", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection.ImportSectionEntry), "SignatureSample", attrs);
            _ignoredProperties.Add(typeof(R2RImportSection.ImportSectionEntry), "SignatureRVA", attrs);
            _ignoredProperties.Add(typeof(RuntimeFunction), "StartAddress", attrs);
            _ignoredProperties.Add(typeof(RuntimeFunction), "UnwindRVA", attrs);
            _ignoredProperties.Add(typeof(R2RSection), "RelativeVirtualAddress", attrs);
            _ignoredProperties.Add(typeof(R2RSection), "Size", attrs);

            XmlAttributes ignoreAlways = new XmlAttributes();
            ignoreAlways.XmlIgnore = true;
            _ignoredProperties.Add(typeof(R2RReader), "ImportCellNames", ignoreAlways);
        }

        internal override void Begin()
        {
            _rootNode = XmlDocument.CreateNode("element", "R2RDump", "");
            XmlDocument.AppendChild(_rootNode);
            Serialize(_r2r, _rootNode);
        }

        internal override void End() {
            if (_writer != null)
            {
                XmlDocument.Save(_writer);
            }
        }

        internal override void WriteDivider(string title)
        {
        }

        internal override void WriteSubDivider()
        {
        }

        internal override void SkipLine()
        {
        }

        /// <summary>
        /// Dumps the R2RHeader and all the sections in the header
        /// </summary>
        internal override void DumpHeader(bool dumpSections)
        {
            XmlNode headerNode = XmlDocument.CreateNode("element", "Header", "");
            _rootNode.AppendChild(headerNode);
            Serialize(_r2r.R2RHeader, headerNode);

            if (_options.Raw)
            {
                DumpBytes(_r2r.R2RHeader.RelativeVirtualAddress, (uint)_r2r.R2RHeader.Size, headerNode);
            }

            if (dumpSections)
            {
                XmlNode sectionsNode = XmlDocument.CreateNode("element", "Sections", "");
                _rootNode.AppendChild(sectionsNode);
                AddXMLNode("Count", _r2r.R2RHeader.Sections.Count.ToString(), sectionsNode);

                foreach (R2RSection section in NormalizedSections())
                {
                    DumpSection(section, sectionsNode);
                }
            }
        }

        /// <summary>
        /// Dumps one R2RSection
        /// </summary>
        internal override void DumpSection(R2RSection section, XmlNode parentNode)
        {
            XmlNode sectionNode = XmlDocument.CreateNode("element", "Section", "");
            AddXMLAttribute(sectionNode, "Index", $"{section.Type}");

            parentNode.AppendChild(sectionNode);
            Serialize(section, sectionNode);

            if (_options.Raw)
            {
                DumpBytes(section.RelativeVirtualAddress, (uint)section.Size, sectionNode);
            }
            if (_options.SectionContents)
            {
                DumpSectionContents(section, sectionNode);
            }
        }

        internal override void DumpEntryPoints()
        {
            XmlNode entryPointsNode = XmlDocument.CreateNode("element", "EntryPoints", "");
            _rootNode.AppendChild(entryPointsNode);
            AddXMLAttribute(entryPointsNode, "Count", _r2r.R2RMethods.Count.ToString());
            foreach (R2RMethod method in NormalizedMethods())
            {
                DumpMethod(method, entryPointsNode);
            }
        }

        internal override void DumpAllMethods()
        {
            XmlNode methodsNode = XmlDocument.CreateNode("element", "Methods", "");
            _rootNode.AppendChild(methodsNode);
            AddXMLAttribute(methodsNode, "Count", _r2r.R2RMethods.Count.ToString());
            foreach (R2RMethod method in NormalizedMethods())
            {
                DumpMethod(method, methodsNode);
            }
        }

        /// <summary>
        /// Dumps one R2RMethod. 
        /// </summary>
        internal override void DumpMethod(R2RMethod method, XmlNode parentNode)
        {
            XmlNode methodNode = XmlDocument.CreateNode("element", "Method", "");
            AddXMLAttribute(methodNode, "Index", $"{method.Index}");
            parentNode.AppendChild(methodNode);
            Serialize(method, methodNode);

            if (_options.GC && method.GcInfo != null)
            {
                XmlNode gcNode = XmlDocument.CreateNode("element", "GcInfo", "");
                methodNode.AppendChild(gcNode);
                Serialize(method.GcInfo, gcNode);

                foreach (List<BaseGcTransition> transitionList in method.GcInfo.Transitions.Values)
                {
                    foreach (BaseGcTransition transition in transitionList)
                    {
                        Serialize(transition, gcNode);
                    }
                }

                if (_options.Raw)
                {
                    DumpBytes(method.GcInfo.Offset, (uint)method.GcInfo.Size, gcNode, "Raw", false);
                }
            }

            XmlNode rtfsNode = null;
            rtfsNode = XmlDocument.CreateNode("element", "RuntimeFunctions", "");
            methodNode.AppendChild(rtfsNode);

            foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
            {
                DumpRuntimeFunction(runtimeFunction, rtfsNode);
            }
        }

        /// <summary>
        /// Dumps one runtime function. 
        /// </summary>
        internal override void DumpRuntimeFunction(RuntimeFunction rtf, XmlNode parentNode)
        {
            XmlNode rtfNode = XmlDocument.CreateNode("element", "RuntimeFunction", "");
            AddXMLAttribute(rtfNode, "Index", $"{rtf.Id}");
            parentNode.AppendChild(rtfNode);
            AddXMLNode("MethodRid", rtf.Method.Rid.ToString(), rtfNode);
            Serialize(rtf, rtfNode);

            if (_options.Disasm)
            {
                DumpDisasm(rtf, _r2r.GetOffset(rtf.StartAddress), rtfNode);
            }

            if (_options.Raw)
            {
                DumpBytes(rtf.StartAddress, (uint)rtf.Size, rtfNode);
            }
            if (_options.Unwind && rtf.UnwindInfo != null)
            {
                XmlNode unwindNode = null;
                unwindNode = XmlDocument.CreateNode("element", "UnwindInfo", "");
                rtfNode.AppendChild(unwindNode);
                Serialize(rtf.UnwindInfo, unwindNode);

                if (_options.Raw)
                {
                    DumpBytes(rtf.UnwindRVA, (uint)((Amd64.UnwindInfo)rtf.UnwindInfo).Size, unwindNode);
                }
            }
        }

        /// <summary>
        /// Dumps disassembly and register liveness
        /// </summary>
        internal override void DumpDisasm(RuntimeFunction rtf, int imageOffset, XmlNode parentNode)
        {
            int rtfOffset = 0;
            int codeOffset = rtf.CodeOffset;

            while (rtfOffset < rtf.Size)
            {
                string instr;
                int instrSize = _disassembler.GetInstruction(rtf, imageOffset, rtfOffset, out instr);

                AddXMLNode("offset" + codeOffset, instr, parentNode, $"{codeOffset}");

                if (rtf.Method.GcInfo != null && rtf.Method.GcInfo.Transitions.ContainsKey(codeOffset))
                {
                    foreach (BaseGcTransition transition in rtf.Method.GcInfo.Transitions[codeOffset])
                    {
                        AddXMLNode("Transition", transition.ToString(), parentNode, $"{codeOffset}");
                    }
                }

                CoreDisTools.ClearOutputBuffer();
                rtfOffset += instrSize;
                codeOffset += instrSize;
            }
        }

        /// <summary>
        /// Prints a formatted string containing a block of bytes from the relative virtual address and size
        /// </summary>
        internal override void DumpBytes(int rva, uint size, XmlNode parentNode, string name = "Raw", bool convertToOffset = true)
        {
            int start = rva;
            if (convertToOffset)
                start = _r2r.GetOffset(rva);
            if (start > _r2r.Image.Length || start + size > _r2r.Image.Length)
            {
                throw new IndexOutOfRangeException();
            }

            if (parentNode != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{_r2r.Image[start]:X2}");
                for (uint i = 1; i < size; i++)
                {
                    sb.Append($" {_r2r.Image[start + i]:X2}");
                }
                AddXMLNode(name, sb.ToString(), parentNode, $"{start}");
                return;
            }
        }

        internal override void DumpSectionContents(R2RSection section, XmlNode parentNode)
        {
            XmlNode contentsNode = XmlDocument.CreateNode("element", "Contents", "");
            parentNode.AppendChild(contentsNode);

            switch (section.Type)
            {
                case R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES:
                    int availableTypesId = 0;
                    foreach (string name in _r2r.AvailableTypes)
                    {
                        AddXMLNode("AvailableType", name, contentsNode, $"{availableTypesId++}");
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS:
                    if (_ignoreSensitive)
                        break;
                    int rtfOffset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    int rtfEndOffset = rtfOffset + section.Size;
                    int rtfIndex = 0;
                    while (rtfOffset < rtfEndOffset)
                    {
                        uint rva = NativeReader.ReadUInt32(_r2r.Image, ref rtfOffset);
                        AddXMLNode($"id{rtfIndex}", $"0x{rva:X8}", contentsNode, $"{rtfIndex}");
                        rtfIndex++;
                    }
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_COMPILER_IDENTIFIER:
                    AddXMLNode("CompilerIdentifier", _r2r.CompilerIdentifier, contentsNode);
                    break;
                case R2RSection.SectionType.READYTORUN_SECTION_IMPORT_SECTIONS:
                    foreach (R2RImportSection importSection in _r2r.ImportSections)
                    {
                        XmlNode importSectionsNode = XmlDocument.CreateNode("element", "ImportSection", "");
                        AddXMLAttribute(importSectionsNode, "Index", $"{importSection.Index}");
                        contentsNode.AppendChild(importSectionsNode);

                        Serialize(importSection, importSectionsNode);
                        if (_options.Raw && importSection.Entries.Count != 0)
                        {
                            if (importSection.SectionRVA != 0)
                            {
                                DumpBytes(importSection.SectionRVA, (uint)importSection.SectionSize, importSectionsNode, "SectionBytes");
                            }
                            if (importSection.SignatureRVA != 0)
                            {
                                DumpBytes(importSection.SignatureRVA, (uint)importSection.Entries.Count * sizeof(int), importSectionsNode, "SignatureBytes");
                            }
                            if (importSection.AuxiliaryDataRVA != 0)
                            {
                                DumpBytes(importSection.AuxiliaryDataRVA, (uint)importSection.AuxiliaryDataSize, importSectionsNode, "AuxiliaryDataBytes");
                            }
                        }
                        foreach (R2RImportSection.ImportSectionEntry entry in importSection.Entries)
                        {
                            Serialize(entry, importSectionsNode);
                        }
                    }
                    break;
            }
        }

        internal override XmlNode DumpQueryCount(string q, string title, int count)
        {
            XmlNode queryNode = XmlDocument.CreateNode("element", title, "");
            _rootNode.AppendChild(queryNode);
            AddXMLAttribute(queryNode, "Query", q);
            AddXMLAttribute(queryNode, "Count", count.ToString());
            return queryNode;
        }

        private void Serialize(object obj, XmlNode node)
        {
            using (XmlWriter xmlWriter = node.CreateNavigator().AppendChild())
            {
                xmlWriter.WriteWhitespace("");
                XmlSerializer Serializer = new XmlSerializer(obj.GetType(), _ignoredProperties);
                Serializer.Serialize(xmlWriter, obj);
            }
        }

        private XmlNode AddXMLNode(String name, String contents, XmlNode parentNode, string index = "")
        {
            XmlNode node = XmlDocument.CreateNode("element", name, "");
            if (!index.Equals(""))
            {
                AddXMLAttribute(node, "Index", index);
            }
            parentNode.AppendChild(node);
            node.InnerText = contents;
            return node;
        }

        private void AddXMLAttribute(XmlNode node, string name, string value)
        {
            XmlAttribute attr = XmlDocument.CreateAttribute(name);
            attr.Value = value;
            node.Attributes.SetNamedItem(attr);
        }
    }
}
