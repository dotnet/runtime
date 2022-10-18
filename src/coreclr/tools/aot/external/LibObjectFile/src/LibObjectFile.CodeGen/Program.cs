// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppAst;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using Zio.FileSystems;

namespace LibObjectFile.CodeGen
{
    partial class Program
    {
        private const string SrcFolderRelative = @"..\..\..\..";

        static void Main(string[] args)
        {
            GenerateElf();
            GenerateDwarf();
        }
       
        private static CodeWriter GetCodeWriter(string subPath)
        {
            var fs = new PhysicalFileSystem();
            var destFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, SrcFolderRelative, subPath));
            var subfs = new SubFileSystem(fs, fs.ConvertPathFromInternal(destFolder));
            var codeWriter = new CodeWriter(new CodeWriterOptions(subfs));
            return codeWriter;
        }

        private static void AssertCompilation(CSharpCompilation csCompilation)
        {
            if (csCompilation.HasErrors)
            {
                foreach (var message in csCompilation.Diagnostics.Messages)
                {
                    Console.Error.WriteLine(message);
                }
                Console.Error.WriteLine("Unexpected parsing errors");
                Environment.Exit(1);
            }
        }

        private static void GenerateElf()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "ElfNative",
                DefaultNamespace = "LibObjectFile.Elf",
                DefaultOutputFilePath = "/LibObjectFile.Elf.generated.cs",
                DefaultDllImportNameAndArguments = "NotUsed",
                MappingRules =
                {
                    map => map.MapMacroToConst("^EI.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFMAG\\d", "uint8_t"),
                    map => map.MapMacroToConst("^ELFCLASS.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFDATA.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFOSABI.*", "uint8_t"),
                    map => map.MapMacroToConst("^ET_.*", "uint16_t"),
                    map => map.MapMacroToConst("^EM_.*", "uint16_t"),
                    map => map.MapMacroToConst("^EV_.*", "uint8_t"),
                    map => map.MapMacroToConst("^SHN_.*", "uint32_t"),
                    map => map.MapMacroToConst("^SHT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^SHF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^EF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^PT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^PF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^NT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^DT_.*", "int32_t"),
                    map => map.MapMacroToConst("^DF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^DTF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^VER_DEF_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_FLG_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_NDX_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_NEED_.*", "uint16_t"),
                    map => map.MapMacroToConst("^ELFCOMPRESS_.*", "int32_t"),
                    map => map.MapMacroToConst("^SYMINFO_.*", "uint16_t"),
                    map => map.MapMacroToConst("^STB_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STT_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STN_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STV_.*", "uint8_t"),
                    map => map.MapMacroToConst("^R_.*", "uint32_t"),
                    map => map.MapMacroToConst("ELF_NOTE_OS_.*", "uint32_t"),
                }
            };

            cppOptions.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);
            cppOptions.Defines.Add("_AMD64_");
            cppOptions.Defines.Add("_TARGET_AMD64_");
            cppOptions.Defines.Add("STARK_NO_ENUM_FLAG");
            cppOptions.GenerateEnumItemAsFields = false;
            cppOptions.IncludeFolders.Add(Environment.CurrentDirectory);

            var csCompilation = CSharpConverter.Convert(@"#include ""elf.h""", cppOptions);

            AssertCompilation(csCompilation);

            // Add pragma
            var csFile = csCompilation.Members.OfType<CSharpGeneratedFile>().First();
            var ns = csFile.Members.OfType<CSharpNamespace>().First();
            csFile.Members.Insert(csFile.Members.IndexOf(ns), new CSharpLineElement("#pragma warning disable 1591") ); 

            ProcessEnum(cppOptions, csCompilation, "EM_", "ElfArch");
            ProcessEnum(cppOptions, csCompilation, "ELFOSABI_", "ElfOSABI");
            ProcessEnum(cppOptions, csCompilation, "R_", "ElfRelocationType");
            ProcessEnum(cppOptions, csCompilation, "NT_", "ElfNoteType");

            csCompilation.DumpTo(GetCodeWriter(Path.Combine("LibObjectFile", "generated")));
        }

        private static readonly Dictionary<string, string> MapRelocMachineToArch = new Dictionary<string, string>()
        {
            {"R_386_", "I386"},
            {"R_X86_64_", "X86_64"},
            {"R_ARM_", "ARM"},
            {"R_AARCH64_", "AARCH64"},
        };

        private static readonly Dictionary<string, string> MapRelocMachineToMachine = new Dictionary<string, string>()
        {
            {"R_386_", "EM_386"},
            {"R_X86_64_", "EM_X86_64"},
            {"R_ARM_", "EM_ARM"},
            {"R_AARCH64_", "EM_AARCH64"},
        };

        private static void ProcessEnum(CSharpConverterOptions cppOptions, CSharpCompilation csCompilation, string enumPrefix, string enumClassName)
        {
            var ns = csCompilation.Members.OfType<CSharpGeneratedFile>().First().Members.OfType<CSharpNamespace>().First();

            var rawElfClass = ns.Members.OfType<CSharpClass>().First();

            var enumRawFields = rawElfClass.Members.OfType<CSharpField>().Where(x => (x.Modifiers & CSharpModifiers.Const) != 0 && x.Name.StartsWith(enumPrefix)).ToList();

            var enumClass = new CSharpStruct(enumClassName)
            {
                Modifiers = CSharpModifiers.Partial | CSharpModifiers.ReadOnly
            };
            ns.Members.Add(enumClass);

            CSharpEnum stdEnum = null;

            var enumItemType = enumRawFields[0].FieldType;

            bool isReloc = enumPrefix == "R_";

            if (!isReloc)
            {
                enumClass.Name = enumClass.Name + "Ex";
                stdEnum = new CSharpEnum(enumClassName)
                {
                    Visibility = CSharpVisibility.Public
                };
                ns.Members.Add(stdEnum);
                stdEnum.BaseTypes.Add(enumItemType);
            }

            var filteredFields = new List<CSharpField>();

            foreach (var enumRawField in enumRawFields)
            {
                var rawName = enumRawField.Name;

                string relocArch = null;

                if (isReloc)
                {
                    foreach (var mapReloc in MapRelocMachineToArch)
                    {
                        if (rawName.StartsWith(mapReloc.Key))
                        {
                            relocArch = mapReloc.Value;
                            break;
                        }
                    }

                    if (relocArch == null)
                    {
                        continue;
                    }
                }

                // skip lo/hi user
                if (rawName.StartsWith("DW_") && (rawName.Contains("_lo_") || rawName.Contains("_hi_")))
                {
                    continue;
                }

                // NUM fields
                if (rawName.EndsWith("_NUM")) continue;
                
                filteredFields.Add(enumRawField);

                var csFieldName = isReloc ? rawName : rawName.Substring(enumPrefix.Length); // discard EM_
                if (csFieldName.StartsWith("386"))
                {
                    csFieldName = $"I{csFieldName}";
                }
                else
                {
                    switch (csFieldName)
                    {
                        case "88K":
                            csFieldName = "M88K";
                            break;
                        case "860":
                            csFieldName = "I860";
                            break;
                        case "960":
                            csFieldName = "I960";
                            break;
                        default:
                            // assume Motorola
                            if (csFieldName.StartsWith("68"))
                            {
                                csFieldName = $"M{csFieldName}";
                            }

                            break;
                    }
                }

                if (char.IsDigit(csFieldName[0]))
                {
                    throw new InvalidOperationException($"The enum name `{rawName}` starts with a number and needs to be modified");
                }

                if (rawName.StartsWith("DW_"))
                {
                    csFieldName = CSharpifyName(csFieldName);
                }

                csFieldName = CSharpHelper.EscapeName(csFieldName);

                var enumField = new CSharpField(csFieldName)
                {
                    Modifiers = CSharpModifiers.Static | CSharpModifiers.ReadOnly,
                    FieldType = enumClass,
                    Visibility = CSharpVisibility.Public,
                    Comment = enumRawField.Comment,
                    InitValue = relocArch != null ? 
                        $"new {enumClass.Name}(ElfArch.{relocArch}, {cppOptions.DefaultClassLib}.{rawName})" :  
                        $"new {enumClass.Name}({cppOptions.DefaultClassLib}.{rawName})"
                };

                enumClass.Members.Add(enumField);

                if (!isReloc)
                {
                    var stdEnumField = new CSharpEnumItem(csFieldName, $"{cppOptions.DefaultClassLib}.{rawName}");
                    stdEnum.Members.Add(stdEnumField);
                }
            }

            var toStringInternal = new CSharpMethod()
            {
                Name = "ToStringInternal",
                Visibility = CSharpVisibility.Private,
                ReturnType = CSharpPrimitiveType.String()
            };
            enumClass.Members.Add(toStringInternal);

            toStringInternal.Body = (writer, element) =>
            {
                var values = new HashSet<object>();
                if (isReloc)
                {
                    writer.WriteLine("switch (((ulong)Value << 16) | (ulong)Arch.Value)");
                }
                else
                {
                    writer.WriteLine($"switch (({enumItemType})Value)");
                }
                writer.OpenBraceBlock();
                foreach (var rawField in filteredFields)
                {
                    var cppField = ((CppField)rawField.CppElement);
                    if (isReloc)
                    {
                        string relocMachine = null;
                        foreach (var mapReloc in MapRelocMachineToMachine)
                        {
                            if (rawField.Name.StartsWith(mapReloc.Key))
                            {
                                relocMachine = mapReloc.Value;
                                break;
                            }
                        }

                        if (relocMachine == null)
                        {
                            continue;
                        }

                        if (!values.Add(relocMachine + "$" + cppField.InitValue.Value))
                        {
                            continue;
                        }
                        
                        writer.WriteLine($"case ((ulong){cppOptions.DefaultClassLib}.{rawField.Name} << 16) | {cppOptions.DefaultClassLib}.{relocMachine} : return \"{rawField.Name}\";");
                    }
                    else
                    {
                        if (!values.Add(cppField.InitValue.Value))
                        {
                            continue;
                        }

                        string descriptionText = rawField.Name;
                        //if (cppField.Comment != null)
                        //{
                        //    descriptionText += " - " + cppField.Comment.ToString().Replace("\"", "\\\"");
                        //}
                        descriptionText = descriptionText.Replace("\r\n", "").Replace("\n", "");
                        writer.WriteLine($"case {cppOptions.DefaultClassLib}.{rawField.Name}: return \"{descriptionText}\";");
                    }
                }

                writer.WriteLine($"default: return null;");
                writer.CloseBraceBlock();
            };
        }
    }
}