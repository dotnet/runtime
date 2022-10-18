// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppAst.CodeGen.CSharp;

namespace LibObjectFile.CodeGen
{
    partial class Program
    {
        private static void GenerateDwarf()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "DwarfNative",
                DefaultNamespace = "LibObjectFile.Dwarf",
                DefaultOutputFilePath = "/LibObjectFile.Dwarf.generated.cs",
                DefaultDllImportNameAndArguments = "NotUsed",
                MappingRules =
                {
                    map => map.MapMacroToConst("^DW_TAG_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_FORM_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_AT_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_LN[ES]_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_IDX_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_LANG_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_ID_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_CC_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_ISA_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_CHILDREN_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_OP_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_ACCESS_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_VIS_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_VIRTUALITY_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_INL_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_ORD_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_DSC_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_UT_.*", "unsigned char"),
                }
            };

            cppOptions.GenerateEnumItemAsFields = false;
            cppOptions.IncludeFolders.Add(Environment.CurrentDirectory);

            var csCompilation = CSharpConverter.Convert(@"#include ""dwarf.h""", cppOptions);

            AssertCompilation(csCompilation);

            // Add pragma
            var csFile = csCompilation.Members.OfType<CSharpGeneratedFile>().First();
            var ns = csFile.Members.OfType<CSharpNamespace>().First();
            csFile.Members.Insert(csFile.Members.IndexOf(ns), new CSharpLineElement("#pragma warning disable 1591") );

            ProcessEnum(cppOptions, csCompilation, "DW_AT_", "DwarfAttributeKind");
            ProcessEnum(cppOptions, csCompilation, "DW_FORM_", "DwarfAttributeForm");
            ProcessEnum(cppOptions, csCompilation, "DW_TAG_", "DwarfTag");
            ProcessEnum(cppOptions, csCompilation, "DW_OP_", "DwarfOperationKind");
            ProcessEnum(cppOptions, csCompilation, "DW_LANG_", "DwarfLanguageKind");
            ProcessEnum(cppOptions, csCompilation, "DW_CC_", "DwarfCallingConvention");
            ProcessEnum(cppOptions, csCompilation, "DW_UT_", "DwarfUnitKind");

            GenerateDwarfAttributes(ns);
            GenerateDwarfDIE(ns);

            csCompilation.DumpTo(GetCodeWriter(Path.Combine("LibObjectFile", "generated")));
        }

        private static Dictionary<string, AttributeMapping> MapAttributeCompactNameToType = new Dictionary<string, AttributeMapping>();

        private static void GenerateDwarfAttributes(CSharpNamespace ns)
        {
            var alreadyDone = new HashSet<string>();

            var csHelper = new CSharpClass("DwarfHelper")
            {
                Modifiers = CSharpModifiers.Static | CSharpModifiers.Partial,
                Visibility = CSharpVisibility.Public
            };
            ns.Members.Add(csHelper);

            var csField = new CSharpField("AttributeToEncoding")
            {
                Modifiers = CSharpModifiers.Static | CSharpModifiers.ReadOnly,
                Visibility = CSharpVisibility.Private,
                FieldType = new CSharpArrayType(new CSharpFreeType("DwarfAttributeEncoding"))
            };
            csHelper.Members.Add(csField);

            var fieldArrayBuilder = new StringBuilder();
            fieldArrayBuilder.AppendLine("new DwarfAttributeEncoding[] {");

            int currentAttributeIndex = 0;

            foreach (var attrEncoding in MapAttributeToEncoding)
            {
                var attrEncodingParts = attrEncoding.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var attributeName = attrEncodingParts[0];
                var attributeIndex = int.Parse(attrEncodingParts[1].Substring(2), System.Globalization.NumberStyles.HexNumber);
                var rawName = attributeName.Substring("DW_AT_".Length);
                //var csharpName = CSharpifyName(rawName);

                string attrType = "object";
                var kind = AttributeKind.Managed;

                if (attributeName == "DW_AT_accessibility")
                {
                    attrType = "DwarfAccessibility";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_visibility")
                {
                    attrType = "DwarfVisibility";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_virtuality")
                {
                    attrType = "DwarfVirtuality";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_language")
                {
                    attrType = "DwarfLanguageKind";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_identifier_case")
                {
                    attrType = "DwarfIdentifierCaseKind";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_calling_convention")
                {
                    attrType = "DwarfCallingConvention";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_inline")
                {
                    attrType = "DwarfInlineKind";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_ordering")
                {
                    attrType = "DwarfArrayOrderingKind";
                    kind = AttributeKind.ValueType;
                }
                else if (attributeName == "DW_AT_discr_list")
                {
                    attrType = "DwarfDiscriminantListKind";
                    kind = AttributeKind.ValueType;
                }
                else if (attrEncodingParts.Length == 3)
                {
                    switch (attrEncodingParts[2])
                    {
                        case "string":
                            attrType = "string";
                            break;

                        case "flag":
                            attrType = "bool";
                            kind = AttributeKind.ValueType;
                            break;

                        case "reference":
                            attrType = "DwarfDIE";
                            break;

                        case "address":
                            attrType = "ulong";
                            kind = AttributeKind.ValueType;
                            break;

                        case "constant":
                            attrType = "DwarfConstant";
                            kind = AttributeKind.ValueType;
                            break;

                        case "lineptr":
                            attrType = "DwarfLineProgramTable";
                            break;
                        
                        case "exprloc":
                            attrType = "DwarfExpression";
                            break;

                        case "loclist":
                        case "loclistptr":
                            attrType = "DwarfLocation";
                            break;

                        case "addrptr":
                        case "macptr":
                        case "rnglist":
                        case "rangelistptr":
                        case "rnglistsptr":
                        case "stroffsetsptr":
                            attrType = "ulong";
                            kind = AttributeKind.ValueType;
                            break;
                    }
                }
                else if (attrEncodingParts.Length > 3)
                {
                    var key = string.Join(" ", attrEncodingParts.Skip(2).ToArray());
                    alreadyDone.Add(key);

                    Console.WriteLine(attrEncoding);

                    bool hasConstant = false;
                    for (int i = 2; i < attrEncodingParts.Length; i++)
                    {
                        switch (attrEncodingParts[i])
                        {
                            case "loclist":
                            case "loclistptr":
                                attrType = "DwarfLocation";
                                kind = AttributeKind.ValueType;
                                goto next;
                            case "constant":
                                hasConstant = true;
                                break;
                        }
                    }

                    if (hasConstant)
                    {
                        attrType = "DwarfConstant";
                        kind = AttributeKind.ValueType;
                    }
                }

                next:

                MapAttributeCompactNameToType.Add(attributeName.Replace("_", string.Empty), new AttributeMapping(rawName, attrType, kind));

                const int PaddingEncodingName = 50;

                for (; currentAttributeIndex < attributeIndex; currentAttributeIndex++)
                {
                    fieldArrayBuilder.AppendLine($"        {"DwarfAttributeEncoding.None",-PaddingEncodingName}, // 0x{currentAttributeIndex:x2} (undefined)");
                }

                for (int i = 2; i < attrEncodingParts.Length; i++)
                {
                    string name;
                    switch (attrEncodingParts[i])
                    {
                        case "string":
                            name = "String";
                            break;

                        case "flag":
                            name = "Flag";
                            break;

                        case "block":
                            name = "Block";
                            break;

                        case "reference":
                            name = "Reference";
                            break;

                        case "address":
                            name = "Address";
                            break;

                        case "constant":
                            name = "Constant";
                            break;

                        case "lineptr":
                            name = "LinePointer";
                            break;

                        case "exprloc":
                            name = "ExpressionLocation";
                            break;

                        case "loclist":
                            name = "LocationList";
                            break;

                        case "loclistptr":
                            name = "LocationListPointer";
                            break;

                        case "loclistsptr":
                            name = "LocationListsPointer";
                            break;

                        case "addrptr":
                            name = "AddressPointer";
                            break;

                        case "macptr":
                            name = "MacroPointer";
                            break;

                        case "rnglist":
                            name = "RangeList";
                            break;

                        case "rangelistptr":
                            name = "RangeListPointer";
                            break;

                        case "rnglistsptr":
                            name = "RangeListsPointer";
                            break;

                        case "stroffsetsptr":
                            name = "StringOffsetPointer";
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown encoding {attrEncodingParts[i]}");
                    }

                    bool isLast = i + 1 == attrEncodingParts.Length;

                    fieldArrayBuilder.Append($"        {"DwarfAttributeEncoding." + name + (isLast ? "" : " | "),-PaddingEncodingName}");

                    if (isLast)
                    {
                        fieldArrayBuilder.Append($", // 0x{currentAttributeIndex:x2} {attributeName} ");
                    }
                    fieldArrayBuilder.AppendLine();

                }

                currentAttributeIndex++;
            }

            fieldArrayBuilder.Append("    }");
            csField.InitValue = fieldArrayBuilder.ToString();

            Console.WriteLine();
            foreach (var key in alreadyDone.ToArray().OrderBy(x => x))
            {
                Console.WriteLine(key);
            }
        }

        struct AttributeMapping
        {
            public AttributeMapping(string rawName, string attributeType, AttributeKind kind)
            {
                RawName = rawName;
                AttributeType = attributeType;
                Kind = kind;
            }

            public string RawName { get; set; }

            public string AttributeType { get; set; }

            public AttributeKind Kind { get; set; }

            public override string ToString()
            {
                return $"{nameof(RawName)}: {RawName}, {nameof(AttributeType)}: {AttributeType}, {nameof(Kind)}: {Kind}";
            }
        }

        enum AttributeKind
        {
            Managed,

            ValueType,

            Link,
        }

        private static void GenerateDwarfDIE(CSharpNamespace ns)
        {
            var file = File.ReadAllLines(@"C:\code\LibObjectFile\ext\dwarf-specs\attributesbytag.tex");

            var regexDWTag = new Regex(@"^\\(DWTAG\w+)");
            var regexDWAT = new Regex(@"^&\\(DWAT\w+)");

            int state = 0;

            string currentCompactTagName = null;
            CSharpClass currentDIE = null;

            var dieClasses = new List<CSharpClass>();
            var dieTags = new List<string>();

            foreach (var line in file)
            {
                if (state == 0)
                {
                    if (line.StartsWith(@"\begin{longtable}"))
                    {
                        continue;
                    }
                    else
                    {
                        state = 1;
                    }
                }
                var match = regexDWTag.Match(line);
                if (match.Success)
                {
                    var compactTagName = match.Groups[1].Value;
                    if (compactTagName == currentCompactTagName)
                    {
                        continue;
                    }
                    currentCompactTagName = compactTagName;
                    var fullTagName = MapTagCompactNameToFullName[compactTagName];
                    dieTags.Add(fullTagName);
                    var csDIEName = fullTagName.Substring("DW_TAG_".Length);
                    csDIEName = CSharpifyName(csDIEName);
                    currentDIE = new CSharpClass($"DwarfDIE{csDIEName}");
                    currentDIE.BaseTypes.Add(new CSharpFreeType("DwarfDIE"));
                    ns.Members.Add(currentDIE);

                    var csConstructor = new CSharpMethod();
                    csConstructor.IsConstructor = true;
                    csConstructor.Body = (writer, element) => writer.WriteLine($"this.Tag = (DwarfTag)DwarfNative.{fullTagName};");
                    currentDIE.Members.Add(csConstructor);

                    dieClasses.Add(currentDIE);
                }
                else
                {
                    match = regexDWAT.Match(line);
                    if (match.Success)
                    {
                        var compactAttrName = match.Groups[1].Value;
                        var csProperty = CreatePropertyFromDwarfAttributeName(compactAttrName);
                        currentDIE.Members.Add(csProperty);

                        // The DW_AT_description attribute can be used on any debugging information
                        // entry that may have a DW_AT_name attribute. For simplicity, this attribute is
                        // not explicitly shown.
                        if (compactAttrName == "DWATname")
                        {
                            csProperty = CreatePropertyFromDwarfAttributeName("DWATdescription");
                            currentDIE.Members.Add(csProperty);
                        }


                    }
                    else if (currentDIE != null && line.Contains("{DECL}"))
                    {
                        currentDIE.BaseTypes[0] = new CSharpFreeType("DwarfDIEDeclaration");
                    }
                }
            }

            // Generate the DIEHelper class
            var dieHelperClass = new CSharpClass("DIEHelper")
            {
                Modifiers = CSharpModifiers.Partial | CSharpModifiers.Static,
                Visibility = CSharpVisibility.Internal
            };
            ns.Members.Add(dieHelperClass);
            var dieHelperMethod = new CSharpMethod()
            {
                Name = "ConvertTagToDwarfDIE",
                Modifiers =  CSharpModifiers.Static,
                Visibility =  CSharpVisibility.Public
            };
            dieHelperClass.Members.Add(dieHelperMethod);

            dieHelperMethod.Parameters.Add(new CSharpParameter("tag") { ParameterType = CSharpPrimitiveType.UShort() });
            dieHelperMethod.ReturnType = new CSharpFreeType("DwarfDIE");

            dieHelperMethod.Body = (writer, element) => { 
                
                writer.WriteLine("switch (tag)"); 
                writer.OpenBraceBlock();

                for (var i = 0; i < dieClasses.Count; i++)
                {
                    var dieCls = dieClasses[i];
                    var dieTag = dieTags[i];
                    writer.WriteLine($"case DwarfNative.{dieTag}:");
                    writer.Indent();
                    writer.WriteLine($"return new {dieCls.Name}();");
                    writer.UnIndent();
                }

                writer.CloseBraceBlock();
                writer.WriteLine("return new DwarfDIE();");
            };
        }

        private static CSharpProperty CreatePropertyFromDwarfAttributeName(string compactAttrName)
        {
            if (compactAttrName == "DWATuseUTFeight")
            {
                compactAttrName = "DWATuseUTF8";
            }

            var map = MapAttributeCompactNameToType[compactAttrName];
            var rawAttrName = map.RawName;
            var attrType = map.AttributeType;

            var propertyName = CSharpifyName(map.RawName);

            var csProperty = new CSharpProperty(propertyName)
            {
                Visibility = CSharpVisibility.Public,
                ReturnType = new CSharpFreeType(map.Kind == AttributeKind.Managed ? attrType : $"{attrType}?"),
            };

            var attrName = CSharpifyName(rawAttrName);

            switch (map.Kind)
            {
                case AttributeKind.Managed:
                    csProperty.GetBody = (writer, element) => writer.WriteLine($"return GetAttributeValue<{attrType}>(DwarfAttributeKind.{attrName});");
                    csProperty.SetBody = (writer, element) => writer.WriteLine($"SetAttributeValue<{attrType}>(DwarfAttributeKind.{attrName}, value);");
                    break;
                case AttributeKind.ValueType:
                    if (map.AttributeType == "DwarfConstant")
                    {
                        csProperty.GetBody = (writer, element) => writer.WriteLine($"return GetAttributeConstantOpt(DwarfAttributeKind.{attrName});");
                        csProperty.SetBody = (writer, element) => writer.WriteLine($"SetAttributeConstantOpt(DwarfAttributeKind.{attrName}, value);");
                    }
                    else if (map.AttributeType == "DwarfLocation")
                    {
                        csProperty.GetBody = (writer, element) => writer.WriteLine($"return GetAttributeLocationOpt(DwarfAttributeKind.{attrName});");
                        csProperty.SetBody = (writer, element) => writer.WriteLine($"SetAttributeLocationOpt(DwarfAttributeKind.{attrName}, value);");
                    }
                    else
                    {
                        csProperty.GetBody = (writer, element) => writer.WriteLine($"return GetAttributeValueOpt<{attrType}>(DwarfAttributeKind.{attrName});");
                        csProperty.SetBody = (writer, element) => writer.WriteLine($"SetAttributeValueOpt<{attrType}>(DwarfAttributeKind.{attrName}, value);");
                    }
                    break;
                case AttributeKind.Link:
                    csProperty.GetBody = (writer, element) =>
                    {
                        writer.WriteLine($"var attr = FindAttributeByKey(DwarfAttributeKind.{attrName});");
                        writer.WriteLine($"return attr == null ? null : new {attrType}(attr.ValueAsU64, attr.ValueAsObject);");
                    };
                    csProperty.SetBody = (writer, element) => { writer.WriteLine($"SetAttributeLinkValue(DwarfAttributeKind.{attrName}, value);"); };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return csProperty;
        }

        private static string CSharpifyName(string rawName)
        {
            if (rawName.EndsWith("_pc"))
            {
                rawName = rawName.Replace("_pc", "_PC");
            }

            var newName = new StringBuilder();
            bool upperCase = true;
            for (var i = 0; i < rawName.Length; i++)
            {
                var c = rawName[i];
                if (c == '_')
                {
                    upperCase = true;
                    continue;
                }

                if (upperCase)
                {
                    newName.Append(char.ToUpperInvariant(c));
                    upperCase = false;
                }
                else
                {
                    newName.Append(c);
                }
            }
            return newName.ToString();
        }


        private static Dictionary<string, string> MapTagCompactNameToFullName = new Dictionary<string, string>()
        {
            {"DWTAGaccessdeclaration", "DW_TAG_access_declaration"},
            {"DWTAGarraytype", "DW_TAG_array_type"},
            {"DWTAGatomictype", "DW_TAG_atomic_type"},
            {"DWTAGbasetype", "DW_TAG_base_type"},
            {"DWTAGcallsite", "DW_TAG_call_site"},
            {"DWTAGcallsiteparameter", "DW_TAG_call_site_parameter"},
            {"DWTAGcatchblock", "DW_TAG_catch_block"},
            {"DWTAGclasstype", "DW_TAG_class_type"},
            {"DWTAGcoarraytype", "DW_TAG_coarray_type"},
            {"DWTAGcommonblock", "DW_TAG_common_block"},
            {"DWTAGcommoninclusion", "DW_TAG_common_inclusion"},
            {"DWTAGcompileunit", "DW_TAG_compile_unit"},
            {"DWTAGcondition", "DW_TAG_condition"},
            {"DWTAGconsttype", "DW_TAG_const_type"},
            {"DWTAGconstant", "DW_TAG_constant"},
            {"DWTAGdescriptortype", "DW_TAG_descriptor_type"},
            {"DWTAGdwarfprocedure", "DW_TAG_dwarf_procedure"},
            {"DWTAGdynamictype", "DW_TAG_dynamic_type"},
            {"DWTAGentrypoint", "DW_TAG_entry_point"},
            {"DWTAGenumerationtype", "DW_TAG_enumeration_type"},
            {"DWTAGenumerator", "DW_TAG_enumerator"},
            {"DWTAGfiletype", "DW_TAG_file_type"},
            {"DWTAGformalparameter", "DW_TAG_formal_parameter"},
            {"DWTAGfriend", "DW_TAG_friend"},
            {"DWTAGgenericsubrange", "DW_TAG_generic_subrange"},
            {"DWTAGhiuser", "DW_TAG_hi_user"},
            {"DWTAGimmutabletype", "DW_TAG_immutable_type"},
            {"DWTAGimporteddeclaration", "DW_TAG_imported_declaration"},
            {"DWTAGimportedmodule", "DW_TAG_imported_module"},
            {"DWTAGimportedunit", "DW_TAG_imported_unit"},
            {"DWTAGinheritance", "DW_TAG_inheritance"},
            {"DWTAGinlinedsubroutine", "DW_TAG_inlined_subroutine"},
            {"DWTAGinterfacetype", "DW_TAG_interface_type"},
            {"DWTAGlabel", "DW_TAG_label"},
            {"DWTAGlexicalblock", "DW_TAG_lexical_block"},
            {"DWTAGlouser", "DW_TAG_lo_user"},
            {"DWTAGmember", "DW_TAG_member"},
            {"DWTAGmodule", "DW_TAG_module"},
            {"DWTAGnamelist", "DW_TAG_namelist"},
            {"DWTAGnamelistitem", "DW_TAG_namelist_item"},
            {"DWTAGnamespace", "DW_TAG_namespace"},
            {"DWTAGpackedtype", "DW_TAG_packed_type"},
            {"DWTAGpartialunit", "DW_TAG_partial_unit"},
            {"DWTAGpointertype", "DW_TAG_pointer_type"},
            {"DWTAGptrtomembertype", "DW_TAG_ptr_to_member_type"},
            {"DWTAGreferencetype", "DW_TAG_reference_type"},
            {"DWTAGrestricttype", "DW_TAG_restrict_type"},
            {"DWTAGrvaluereferencetype", "DW_TAG_rvalue_reference_type"},
            {"DWTAGsettype", "DW_TAG_set_type"},
            {"DWTAGsharedtype", "DW_TAG_shared_type"},
            {"DWTAGskeletonunit", "DW_TAG_skeleton_unit"},
            {"DWTAGstringtype", "DW_TAG_string_type"},
            {"DWTAGstructuretype", "DW_TAG_structure_type"},
            {"DWTAGsubprogram", "DW_TAG_subprogram"},
            {"DWTAGsubrangetype", "DW_TAG_subrange_type"},
            {"DWTAGsubroutinetype", "DW_TAG_subroutine_type"},
            {"DWTAGtemplatealias", "DW_TAG_template_alias"},
            {"DWTAGtemplatetypeparameter", "DW_TAG_template_type_parameter"},
            {"DWTAGtemplatevalueparameter", "DW_TAG_template_value_parameter"},
            {"DWTAGthrowntype", "DW_TAG_thrown_type"},
            {"DWTAGtryblock", "DW_TAG_try_block"},
            {"DWTAGtypedef", "DW_TAG_typedef"},
            {"DWTAGtypeunit", "DW_TAG_type_unit"},
            {"DWTAGuniontype", "DW_TAG_union_type"},
            {"DWTAGunspecifiedparameters", "DW_TAG_unspecified_parameters"},
            {"DWTAGunspecifiedtype", "DW_TAG_unspecified_type"},
            {"DWTAGvariable", "DW_TAG_variable"},
            {"DWTAGvariant", "DW_TAG_variant"},
            {"DWTAGvariantpart", "DW_TAG_variant_part"},
            {"DWTAGvolatiletype", "DW_TAG_volatile_type"},
            {"DWTAGwithstmt", "DW_TAG_with_stmt"},
        };

        // Extract from Dwarf 5 specs - Figure 20, Attribute encodings
        private static readonly string[] MapAttributeToEncoding = new string[]
        {
            "DW_AT_sibling 0x01 reference",
            "DW_AT_location 0x02 exprloc loclist",
            "DW_AT_name 0x03 string",
            "DW_AT_ordering 0x09 constant",
            "DW_AT_byte_size 0x0b constant exprloc reference",
            "DW_AT_bit_offset 0x0c constant exprloc reference",
            "DW_AT_bit_size 0x0d constant exprloc reference",
            "DW_AT_stmt_list 0x10 lineptr",
            "DW_AT_low_pc 0x11 address",
            "DW_AT_high_pc 0x12 address constant",
            "DW_AT_language 0x13 constant",
            "DW_AT_discr 0x15 reference",
            "DW_AT_discr_value 0x16 constant",
            "DW_AT_visibility 0x17 constant",
            "DW_AT_import 0x18 reference",
            "DW_AT_string_length 0x19 exprloc loclistptr",
            "DW_AT_common_reference 0x1a reference",
            "DW_AT_comp_dir 0x1b string",
            "DW_AT_const_value 0x1c block constant string ",
            "DW_AT_containing_type 0x1d reference",
            "DW_AT_default_value 0x1e reference",
            "DW_AT_inline 0x20 constant",
            "DW_AT_is_optional 0x21 flag",
            "DW_AT_lower_bound 0x22 constant exprloc reference",
            "DW_AT_producer 0x25 string",
            "DW_AT_prototyped 0x27 flag",
            "DW_AT_return_addr 0x2a exprloc loclistptr",
            "DW_AT_start_scope 0x2c constant rangelistptr",
            "DW_AT_bit_stride 0x2e constant exprloc reference",
            "DW_AT_upper_bound 0x2f constant exprloc reference",
            "DW_AT_abstract_origin 0x31 reference",
            "DW_AT_accessibility 0x32 constant",
            "DW_AT_address_class 0x33 constant",
            "DW_AT_artificial 0x34 flag",
            "DW_AT_base_types 0x35 reference",
            "DW_AT_calling_convention 0x36 constant",
            "DW_AT_count 0x37 constant exprloc reference",
            "DW_AT_data_member_location 0x38 constant exprloc loclistptr",
            "DW_AT_decl_column 0x39 constant",
            "DW_AT_decl_file 0x3a constant",
            "DW_AT_decl_line 0x3b constant",
            "DW_AT_declaration 0x3c flag",
            "DW_AT_discr_list 0x3d block",
            "DW_AT_encoding 0x3e constant",
            "DW_AT_external 0x3f flag",
            "DW_AT_frame_base 0x40 exprloc loclistptr",
            "DW_AT_friend 0x41 reference",
            "DW_AT_identifier_case 0x42 constant",
            "DW_AT_macro_info 0x43 macptr",
            "DW_AT_namelist_item 0x44 reference",
            "DW_AT_priority 0x45 reference",
            "DW_AT_segment 0x46 exprloc loclistptr",
            "DW_AT_specification 0x47 reference",
            "DW_AT_static_link 0x48 exprloc loclistptr",
            "DW_AT_type 0x49 reference",
            "DW_AT_use_location 0x4a exprloc loclistptr",
            "DW_AT_variable_parameter 0x4b flag",
            "DW_AT_virtuality 0x4c constant",
            "DW_AT_vtable_elem_location 0x4d exprloc loclistptr ",
            "DW_AT_allocated 0x4e constant exprloc reference",
            "DW_AT_associated 0x4f constant exprloc reference",
            "DW_AT_data_location 0x50 exprloc",
            "DW_AT_byte_stride 0x51 constant exprloc reference",
            "DW_AT_entry_pc 0x52 address",
            "DW_AT_use_UTF8 0x53 flag",
            "DW_AT_extension 0x54 reference",
            "DW_AT_ranges 0x55 rangelistptr",
            "DW_AT_trampoline 0x56 address flag reference string",
            "DW_AT_call_column 0x57 constant",
            "DW_AT_call_file 0x58 constant",
            "DW_AT_call_line 0x59 constant",
            "DW_AT_description 0x5a string",
            "DW_AT_binary_scale 0x5b constant",
            "DW_AT_decimal_scale 0x5c constant",
            "DW_AT_small 0x5d reference",
            "DW_AT_decimal_sign 0x5e constant",
            "DW_AT_digit_count 0x5f constant",
            "DW_AT_picture_string 0x60 string",
            "DW_AT_mutable 0x61 flag ",
            "DW_AT_threads_scaled 0x62 flag",
            "DW_AT_explicit 0x63 flag",
            "DW_AT_object_pointer 0x64 reference",
            "DW_AT_endianity 0x65 constant",
            "DW_AT_elemental 0x66 flag",
            "DW_AT_pure 0x67 flag",
            "DW_AT_recursive 0x68 flag",
            "DW_AT_signature 0x69 reference",
            "DW_AT_main_subprogram 0x6a flag",
            "DW_AT_data_bit_offset 0x6b constant",
            "DW_AT_const_expr 0x6c flag",
            "DW_AT_enum_class 0x6d flag",
            "DW_AT_linkage_name 0x6e string ",
            "DW_AT_string_length_bit_size 0x6f constant",
            "DW_AT_string_length_byte_size 0x70 constant",
            "DW_AT_rank 0x71 constant exprloc",
            "DW_AT_str_offsets_base 0x72 stroffsetsptr",
            "DW_AT_addr_base 0x73 addrptr",
            "DW_AT_rnglists_base 0x74 rnglistsptr",
            "DW_AT_dwo_name 0x76 string",
            "DW_AT_reference 0x77 flag",
            "DW_AT_rvalue_reference 0x78 flag",
            "DW_AT_macros 0x79 macptr",
            "DW_AT_call_all_calls 0x7a flag",
            "DW_AT_call_all_source_calls 0x7b flag",
            "DW_AT_call_all_tail_calls 0x7c flag",
            "DW_AT_call_return_pc 0x7d address",
            "DW_AT_call_value 0x7e exprloc",
            "DW_AT_call_origin 0x7f exprloc",
            "DW_AT_call_parameter 0x80 reference",
            "DW_AT_call_pc 0x81 address",
            "DW_AT_call_tail_call 0x82 flag",
            "DW_AT_call_target 0x83 exprloc",
            "DW_AT_call_target_clobbered 0x84 exprloc",
            "DW_AT_call_data_location 0x85 exprloc",
            "DW_AT_call_data_value 0x86 exprloc",
            "DW_AT_noreturn 0x87 flag",
            "DW_AT_alignment 0x88 constant",
            "DW_AT_export_symbols 0x89 flag",
            "DW_AT_deleted 0x8a flag",
            "DW_AT_defaulted 0x8b constant",
            "DW_AT_loclists_base 0x8c loclistsptr",
        };
    }
}