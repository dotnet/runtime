using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace R2RDump
{
    public class XmlDumper : Dumper
    {
        public XmlDocument XmlDocument { get; }
        private XmlNode _rootNode;

        public XmlDumper(R2RReader r2r, TextWriter writer, bool raw, bool header, bool disasm, IntPtr disassembler, bool unwind, bool gc, bool sectionContents)
        {
            _r2r = r2r;
            _writer = writer;
            XmlDocument = new XmlDocument();

            _raw = raw;
            _header = header;
            _disasm = disasm;
            _disassembler = disassembler;
            _unwind = unwind;
            _gc = gc;
            _sectionContents = sectionContents;
        }

        public XmlDocument GetXmlDocument()
        {
            Begin();
            DumpHeader(true);
            DumpAllMethods();
            return XmlDocument;
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

            if (_raw)
            {
                DumpBytes(_r2r.R2RHeader.RelativeVirtualAddress, (uint)_r2r.R2RHeader.Size, headerNode);
            }

            if (dumpSections)
            {
                XmlNode sectionsNode = XmlDocument.CreateNode("element", "Sections", "");
                _rootNode.AppendChild(sectionsNode);
                AddXMLNode("Count", _r2r.R2RHeader.Sections.Count.ToString(), sectionsNode);

                foreach (R2RSection section in _r2r.R2RHeader.Sections.Values)
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
            AddIndexAttribute(sectionNode, $"{section.Type}");
            parentNode.AppendChild(sectionNode);
            Serialize(section, sectionNode);

            if (_raw)
            {
                DumpBytes(section.RelativeVirtualAddress, (uint)section.Size, sectionNode);
            }
            if (_sectionContents)
            {
                DumpSectionContents(section, sectionNode);
            }
        }

        internal override void DumpAllMethods()
        {
            XmlNode methodsNode = XmlDocument.CreateNode("element", "Methods", "");
            _rootNode.AppendChild(methodsNode);
            AddXMLNode("Count", _r2r.R2RMethods.Count.ToString(), methodsNode);
            foreach (R2RMethod method in _r2r.R2RMethods)
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
            AddIndexAttribute(methodNode, $"{method.Rid}");
            parentNode.AppendChild(methodNode);
            Serialize(method, methodNode);

            if (_gc && method.GcInfo != null)
            {
                XmlNode gcNode = XmlDocument.CreateNode("element", "GcInfo", "");
                methodNode.AppendChild(gcNode);
                Serialize(method.GcInfo, gcNode);

                foreach (GcInfo.GcTransition transition in method.GcInfo.Transitions.Values)
                {
                    Serialize(transition, gcNode);
                }

                if (_raw)
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
            parentNode.AppendChild(rtfNode);
            AddXMLNode("MethodRid", rtf.Method.Rid.ToString(), rtfNode);
            Serialize(rtf, rtfNode);

            if (_disasm)
            {
                DumpDisasm(_disassembler, rtf, _r2r.GetOffset(rtf.StartAddress), _r2r.Image, rtfNode);
            }

            if (_raw)
            {
                DumpBytes(rtf.StartAddress, (uint)rtf.Size, rtfNode);
            }
            if (_unwind && rtf.UnwindInfo != null)
            {
                XmlNode unwindNode = null;
                unwindNode = XmlDocument.CreateNode("element", "UnwindInfo", "");
                rtfNode.AppendChild(unwindNode);
                Serialize(rtf.UnwindInfo, unwindNode);

                if (_raw)
                {
                    DumpBytes(rtf.UnwindRVA, (uint)((Amd64.UnwindInfo)rtf.UnwindInfo).Size, unwindNode);
                }
            }
        }

        internal unsafe override void DumpDisasm(IntPtr Disasm, RuntimeFunction rtf, int imageOffset, byte[] image, XmlNode parentNode)
        {
            int rtfOffset = 0;
            int codeOffset = rtf.CodeOffset;
            Dictionary<int, GcInfo.GcTransition> transitions = rtf.Method.GcInfo.Transitions;
            GcSlotTable slotTable = rtf.Method.GcInfo.SlotTable;
            while (rtfOffset < rtf.Size)
            {
                string instr;
                int instrSize = CoreDisTools.GetInstruction(Disasm, rtf, imageOffset, rtfOffset, image, out instr);

                AddXMLNode("offset"+codeOffset, instr, parentNode, $"{codeOffset}");
                if (transitions.ContainsKey(codeOffset))
                {
                    AddXMLNode("Transition", transitions[codeOffset].GetSlotState(slotTable), parentNode, $"{codeOffset}");
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
                        Serialize(importSection, contentsNode);
                        if (_raw && importSection.Entries.Count != 0)
                        {
                            if (importSection.SectionRVA != 0)
                            {
                                DumpBytes(importSection.SectionRVA, (uint)importSection.SectionSize, contentsNode, "SectionBytes");
                            }
                            if (importSection.SignatureRVA != 0)
                            {
                                DumpBytes(importSection.SignatureRVA, (uint)importSection.Entries.Count * sizeof(int), contentsNode, "SignatureBytes");
                            }
                            if (importSection.AuxiliaryDataRVA != 0)
                            {
                                DumpBytes(importSection.AuxiliaryDataRVA, (uint)importSection.AuxiliaryData.Size, contentsNode, "AuxiliaryDataBytes");
                            }
                        }
                        foreach (R2RImportSection.ImportSectionEntry entry in importSection.Entries)
                        {
                            Serialize(entry, contentsNode);
                        }
                    }
                    break;
            }
        }

        internal override XmlNode DumpQueryCount(string q, string title, int count)
        {
            XmlNode queryNode = XmlDocument.CreateNode("element", title, "");
            _rootNode.AppendChild(queryNode);
            AddXMLNode("Query", q, queryNode);
            AddXMLNode("Count", count.ToString(), queryNode);
            return queryNode;
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

        private XmlNode AddXMLNode(String name, String contents, XmlNode parentNode, string index = "")
        {
            XmlNode node = XmlDocument.CreateNode("element", name, "");
            if (!index.Equals(""))
            {
                AddIndexAttribute(node, index);
            }
            parentNode.AppendChild(node);
            node.InnerText = contents;
            return node;
        }

        private void AddIndexAttribute(XmlNode node, string index)
        {
            XmlAttribute attr = XmlDocument.CreateAttribute("Index");
            attr.Value = index;
            node.Attributes.SetNamedItem(attr);
        }
    }
}
