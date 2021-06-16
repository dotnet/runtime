// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Thunkerator
{
    // Parse type replacement section for normal types
    // Parse type replacement section for return value types

    public static class StringExtensions
    {
        public static string Canonicalize(this string current)
        {
            string untrimmed = "";
            while (untrimmed != current)
            {
                untrimmed = current;
                current = current.Replace(" *", "*");
                current = current.Replace("* ", "*");
                current = current.Replace(" ,", ",");
                current = current.Replace(", ", ",");
                current = current.Replace("  ", " ");
                current = current.Replace("\t", " ");
            }

            return current.Trim();
        }
    }

    class TypeReplacement
    {
        public TypeReplacement(string line)
        {
            string[] typenames = line.Split(',');
            if ((typenames.Length < 1) || (typenames.Length > 4))
            {
                throw new Exception("Wrong number of type name entries");
            }
            ThunkTypeName = typenames[0].Canonicalize();

            if (typenames.Length > 1 && !string.IsNullOrWhiteSpace(typenames[1]))
            {
                ManagedTypeName = typenames[1].Canonicalize();
            }
            else
            {
                ManagedTypeName = ThunkTypeName;
            }

            if (typenames.Length > 2)
            {
                NativeTypeName = typenames[2].Canonicalize();
            }
            else
            {
                NativeTypeName = ThunkTypeName;
            }

            if (typenames.Length > 3)
            {
                NativeTypeName2 = typenames[3].Canonicalize();
            }
            else
            {
                NativeTypeName2 = ThunkTypeName;
            }
        }
        public readonly string ThunkTypeName;
        public readonly string NativeTypeName;
        public readonly string NativeTypeName2;
        public readonly string ManagedTypeName;

        public bool IsByRef => ManagedTypeName.Contains("ref ");
        public bool IsBoolean => ManagedTypeName == "[MarshalAs(UnmanagedType.I1)]bool";
        public bool IsBOOL => ManagedTypeName == "[MarshalAs(UnmanagedType.Bool)]bool";

        public string UnmanagedTypeName
        {
            get
            {
                if (IsBoolean)
                    return "byte";

                if (IsBOOL)
                    return "int";

                if (IsByRef)
                    return ManagedTypeName.Replace("ref ", "") + "*";

                // No special marshaling rules
                return ManagedTypeName;
            }
        }
    }

    class Parameter
    {
        public Parameter(string name, TypeReplacement type)
        {
            Type = type;
            Name = name;
            if (name.StartsWith("*"))
                throw new Exception("Names not allowed to start with *");
        }

        public readonly string Name;
        public readonly TypeReplacement Type;
    }

    class FunctionDecl
    {
        public FunctionDecl(string line, Dictionary<string, TypeReplacement> ThunkReturnTypes, Dictionary<string, TypeReplacement> ThunkTypes)
        {
            if (line.Contains("[ManualNativeWrapper]"))
            {
                ManualNativeWrapper = true;
                line = line.Replace("[ManualNativeWrapper]", string.Empty);
            }

            int indexOfOpenParen = line.IndexOf('(');
            int indexOfCloseParen = line.IndexOf(')');
            string returnTypeAndFunctionName = line.Substring(0, indexOfOpenParen).Canonicalize();
            int indexOfLastWhitespaceInReturnTypeAndFunctionName = returnTypeAndFunctionName.LastIndexOfAny(new char[] { ' ', '*' });
            FunctionName = returnTypeAndFunctionName.Substring(indexOfLastWhitespaceInReturnTypeAndFunctionName + 1).Canonicalize();
            if (FunctionName.StartsWith("*"))
                throw new Exception("Names not allowed to start with *");
            string returnType = returnTypeAndFunctionName.Substring(0, indexOfLastWhitespaceInReturnTypeAndFunctionName + 1).Canonicalize();

            if (!ThunkReturnTypes.TryGetValue(returnType, out ReturnType))
            {
                throw new Exception(String.Format("Type {0} unknown", returnType));
            }

            string parameterList = line.Substring(indexOfOpenParen + 1, indexOfCloseParen - indexOfOpenParen - 1).Canonicalize();
            string[] parametersString = parameterList.Length == 0 ? new string[0] : parameterList.Split(',');
            List<Parameter> parameters = new List<Parameter>();

            foreach (string parameterString in parametersString)
            {
                int indexOfLastWhitespaceInParameter = parameterString.LastIndexOfAny(new char[] { ' ', '*' });
                string paramName = parameterString.Substring(indexOfLastWhitespaceInParameter + 1).Canonicalize();
                string paramType = parameterString.Substring(0, indexOfLastWhitespaceInParameter + 1).Canonicalize();
                TypeReplacement tr;
                if (!ThunkTypes.TryGetValue(paramType, out tr))
                {
                    throw new Exception(String.Format("Type {0} unknown", paramType));
                }
                parameters.Add(new Parameter(paramName, tr));
            }

            Parameters = parameters.ToArray();
        }

        public readonly string FunctionName;
        public readonly TypeReplacement ReturnType;
        public readonly Parameter[] Parameters;
        public readonly bool ManualNativeWrapper = false;
    }

    class Program
    {
        enum ParseMode
        {
            RETURNTYPES,
            NORMALTYPES,
            FUNCTIONS,
            IFDEFING
        }
        static IEnumerable<FunctionDecl> ParseInput(TextReader tr)
        {
            Dictionary<string, TypeReplacement> ThunkReturnTypes = new Dictionary<string, TypeReplacement>();
            Dictionary<string, TypeReplacement> ThunkTypes = new Dictionary<string, TypeReplacement>();
            ParseMode currentParseMode = ParseMode.FUNCTIONS;
            ParseMode oldParseMode = ParseMode.FUNCTIONS;
            List<FunctionDecl> functions = new List<FunctionDecl>();
            int currentLineIndex = 1;
            for (string currentLine = tr.ReadLine(); currentLine != null; currentLine = tr.ReadLine(), currentLineIndex++)
            {
                try
                {
                    if (currentLine.Length == 0)
                    {
                        continue; // Its an empty line, ignore
                    }

                    if (currentLine[0] == ';')
                    {
                        continue; // Its a comment
                    }

                    if (currentLine == "RETURNTYPES")
                    {
                        currentParseMode = ParseMode.RETURNTYPES;
                        continue;
                    }
                    if (currentLine == "NORMALTYPES")
                    {
                        currentParseMode = ParseMode.NORMALTYPES;
                        continue;
                    }
                    if (currentLine == "FUNCTIONS")
                    {
                        currentParseMode = ParseMode.FUNCTIONS;
                        continue;
                    }

                    if (currentLine == "#endif")
                    {
                        currentParseMode = oldParseMode;
                        continue;
                    }

                    if (currentLine.StartsWith("#if"))
                    {
                        oldParseMode = currentParseMode;
                        currentParseMode = ParseMode.IFDEFING;
                    }

                    if (currentParseMode == ParseMode.IFDEFING)
                    {
                        continue;
                    }

                    switch (currentParseMode)
                    {
                        case ParseMode.NORMALTYPES:
                        case ParseMode.RETURNTYPES:
                            TypeReplacement t = new TypeReplacement(currentLine);
                            if (currentParseMode == ParseMode.NORMALTYPES)
                            {
                                ThunkTypes.Add(t.ThunkTypeName, t);
                                ThunkReturnTypes.Add(t.ThunkTypeName, t);
                            }
                            if (currentParseMode == ParseMode.RETURNTYPES)
                            {
                                ThunkReturnTypes[t.ThunkTypeName] = t;
                            }
                            break;

                        case ParseMode.FUNCTIONS:
                            functions.Add(new FunctionDecl(currentLine, ThunkReturnTypes, ThunkTypes));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error parsing line {0} : {1}", currentLineIndex, e.Message);
                }
            }

            return functions.AsReadOnly();
        }

        static void WriteAutogeneratedHeader(TextWriter tw)
        {
            // Write header
            tw.Write(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// To regenerate run the gen script in src/coreclr/tools/Common/JitInterface/ThunkGenerator
// and follow the instructions in docs/project/updating-jitinterface.md
");
        }

        static void WriteManagedThunkInterface(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(@"
using System;
using System.Runtime.InteropServices;

namespace Internal.JitInterface
{
    unsafe partial class CorInfoImpl
    {
");

            foreach (FunctionDecl decl in functionData)
            {
                tw.WriteLine("        [UnmanagedCallersOnly]");
                tw.Write($"        static {decl.ReturnType.UnmanagedTypeName} _{decl.FunctionName}(IntPtr thisHandle, IntPtr* ppException");
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write($", {param.Type.UnmanagedTypeName} {param.Name}");
                }
                tw.Write(@")
        {
            var _this = GetThis(thisHandle);
            try
            {
");
                bool isVoid = decl.ReturnType.ManagedTypeName == "void";
                tw.Write($"                {(isVoid ? "" : "return ")}_this.{decl.FunctionName}(");
                bool isFirst = true;
                foreach (Parameter param in decl.Parameters)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        tw.Write(", ");
                    }

                    if (param.Type.IsByRef)
                    {
                        tw.Write("ref *");
                    }
                    tw.Write(param.Name);
                    if (param.Type.IsBoolean || param.Type.IsBOOL)
                    {
                        tw.Write(" != 0");
                    }
                }
                tw.Write(")");
                if (decl.ReturnType.IsBOOL || decl.ReturnType.IsBoolean)
                {
                    tw.Write($" ? ({decl.ReturnType.UnmanagedTypeName})1 : ({decl.ReturnType.UnmanagedTypeName})0");
                }
                tw.Write(";");
                tw.Write(@"
            }
            catch (Exception ex)
            {
                *ppException = _this.AllocException(ex);
");
                if (!isVoid)
                {
                    tw.WriteLine("                return default;");
                }
                tw.WriteLine(@"            }");
                tw.WriteLine("        }");
                tw.WriteLine();
            }

            int total = functionData.Count();
            tw.WriteLine(@"
        static IntPtr GetUnmanagedCallbacks()
        {
            void** callbacks = (void**)Marshal.AllocCoTaskMem(sizeof(IntPtr) * " + total + @");
");

            int index = 0;
            foreach (FunctionDecl decl in functionData)
            {
                tw.Write($"            callbacks[{index}] = (delegate* unmanaged<IntPtr, IntPtr*");
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write($", {param.Type.UnmanagedTypeName}");
                }
                tw.WriteLine($", {decl.ReturnType.UnmanagedTypeName}>)&_{decl.FunctionName};");
                index++;
            }

            tw.WriteLine(@"
            return (IntPtr)callbacks;
        }
    }
}
");
        }

        static void WriteNativeWrapperInterface(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(@"

#include ""corinfoexception.h""
#include ""../../../inc/corjit.h""

struct JitInterfaceCallbacks
{
");

            foreach (FunctionDecl decl in functionData)
            {
                tw.Write($"    {decl.ReturnType.NativeTypeName} (* {decl.FunctionName})(void * thisHandle, CorInfoExceptionClass** ppException");
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write($", {param.Type.NativeTypeName} {param.Name}");
                }
                tw.WriteLine(");");
            }

            tw.Write(@"
};

class JitInterfaceWrapper : public ICorJitInfo
{
    void * _thisHandle;
    JitInterfaceCallbacks * _callbacks;

public:
    JitInterfaceWrapper(void * thisHandle, void ** callbacks)
        : _thisHandle(thisHandle), _callbacks((JitInterfaceCallbacks *)callbacks)
    {
    }

");

            API_Wrapper_Generic_Core(tw, functionData, 
                funcNameFunc: (FunctionDecl decl)=>$"{decl.FunctionName }", 
                beforeCallFunc:(FunctionDecl)=>"    CorInfoExceptionClass* pException = nullptr;", 
                afterCallFunc: (FunctionDecl decl) => "    if (pException != nullptr) throw pException;", 
                wrappedObjectName: "_callbacks", 
                useNativeType2: false, 
                addVirtualPrefix: true, 
                skipManualWrapper: true);

            tw.WriteLine("};");
        }

        static void WriteAPI_Names(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);

            foreach (FunctionDecl decl in functionData)
            {
                tw.WriteLine($"DEF_CLR_API({decl.FunctionName})");
            }

            tw.Write(@"
#undef DEF_CLR_API
");
        }

        static void API_Wrapper_Generic_Core(TextWriter tw, IEnumerable<FunctionDecl> functionData, Func<FunctionDecl, string> funcNameFunc, Func<FunctionDecl, string> beforeCallFunc, Func<FunctionDecl,string> afterCallFunc, string wrappedObjectName, bool useNativeType2, bool addVirtualPrefix, bool skipManualWrapper)
        {
            foreach (FunctionDecl decl in functionData)
            {
                tw.WriteLine("");
                if (addVirtualPrefix)
                {
                    tw.Write("    virtual ");
                }
                tw.Write($"{GetNativeType(decl.ReturnType)} {funcNameFunc(decl)}(");
                bool isFirst = true;
                foreach (Parameter param in decl.Parameters)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        tw.Write(",");
                    }
                    tw.Write(Environment.NewLine + "          " + GetNativeType(param.Type) + " " + param.Name);
                }
                tw.Write(')');
                if (skipManualWrapper && decl.ManualNativeWrapper)
                {
                    tw.WriteLine(";");
                    continue;
                }
                tw.WriteLine("");
                tw.WriteLine("{");
                string beforeCall = beforeCallFunc(decl) ?? null;
                string afterCall = afterCallFunc(decl) ?? null;
                if (beforeCall != null)
                    tw.WriteLine(beforeCall);

                tw.Write("    ");
                if (GetNativeType(decl.ReturnType) != "void")
                {
                    if (afterCall != null)
                        tw.Write($"{GetNativeType(decl.ReturnType)} temp = ");
                    else
                        tw.Write("return ");
                }
                tw.Write($"{wrappedObjectName}->{decl.FunctionName}(");
                isFirst = true;

                if (skipManualWrapper)
                {
                    tw.Write("_thisHandle, &pException");
                    isFirst = false;
                }

                foreach (Parameter param in decl.Parameters)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        tw.Write(", ");
                    }
                    tw.Write(param.Name);
                }
                tw.WriteLine(");");
                if (afterCall != null)
                    tw.WriteLine(afterCall);
                if ((GetNativeType(decl.ReturnType) != "void") && (afterCall != null))
                {
                    tw.WriteLine("    return temp;");
                }
                tw.WriteLine("}");
            }

            string GetNativeType(TypeReplacement typeReplacement)
            {
                if (useNativeType2)
                    return typeReplacement.NativeTypeName2;
                else
                    return typeReplacement.NativeTypeName;
            }
        }

        static void API_Wrapper_Generic(TextWriter tw, IEnumerable<FunctionDecl> functionData, string header, string footer, string cppType, Func<FunctionDecl, string> beforeCallFunc, Func<FunctionDecl,string> afterCallFunc, string wrappedObjectName)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(header);

            API_Wrapper_Generic_Core(tw, functionData, funcNameFunc: (FunctionDecl decl)=>$"{cppType}::{ decl.FunctionName }", beforeCallFunc:beforeCallFunc, afterCallFunc: afterCallFunc, wrappedObjectName: wrappedObjectName, useNativeType2: true, addVirtualPrefix: false, skipManualWrapper: false);

            tw.Write(footer);
        }

        static void API_Wrapper(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            API_Wrapper_Generic(tw, functionData, 
                                header:@"
#define API_ENTER(name) wrapComp->CLR_API_Enter(API_##name);
#define API_LEAVE(name) wrapComp->CLR_API_Leave(API_##name);

/**********************************************************************************/
// clang-format off
/**********************************************************************************/
", 
                                footer: @"
/**********************************************************************************/
// clang-format on
/**********************************************************************************/
",
                                cppType: "WrapICorJitInfo",
                                beforeCallFunc: (FunctionDecl decl)=> $"    API_ENTER({decl.FunctionName});",
                                afterCallFunc: (FunctionDecl decl)=> $"    API_LEAVE({decl.FunctionName});",
                                wrappedObjectName: "wrapHnd");
        }

        static void SPMI_ICorJitInfoImpl(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(@"

// ICorJitInfoImpl: declare for implementation all the members of the ICorJitInfo interface (which are
// specified as pure virtual methods). This is done once, here, and all implementations share it,
// to avoid duplicated declarations. This file is #include'd within all the ICorJitInfo implementation
// classes.
//
// NOTE: this file is in exactly the same order, with exactly the same whitespace, as the ICorJitInfo
// interface declaration (with the ""virtual"" and ""= 0"" syntax removed). This is to make it easy to compare
// against the interface declaration.

/**********************************************************************************/
// clang-format off
/**********************************************************************************/

public:
");

            foreach (FunctionDecl decl in functionData)
            {
                tw.Write($"{Environment.NewLine}{decl.ReturnType.NativeTypeName2} { decl.FunctionName}(");
                bool isFirst = true;
                foreach (Parameter param in decl.Parameters)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        tw.Write(",");
                    }
                    tw.Write(Environment.NewLine + "          " + param.Type.NativeTypeName2 + " " + param.Name);
                }
                tw.WriteLine(") override;");
            }

            tw.Write(@"
/**********************************************************************************/
// clang-format on
/**********************************************************************************/
");
        }

        static void SPMI_ShimCounter_ICorJitInfo(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            API_Wrapper_Generic(tw, functionData, 
                                header:@"
#include ""standardpch.h""
#include ""icorjitinfo.h""
#include ""superpmi-shim-counter.h""
#include ""icorjitcompiler.h""
#include ""spmiutil.h""

", 
                                footer: Environment.NewLine,
                                cppType: "interceptor_ICJI",
                                beforeCallFunc: (FunctionDecl decl)=> $"    mcs->AddCall(\"{decl.FunctionName}\");",
                                afterCallFunc: (FunctionDecl decl)=> null,
                                wrappedObjectName: "original_ICorJitInfo");
        }

        static void SPMI_ShimSimple_ICorJitInfo(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            API_Wrapper_Generic(tw, functionData, 
                                header:@"
#include ""standardpch.h""
#include ""icorjitinfo.h""
#include ""superpmi-shim-simple.h""
#include ""icorjitcompiler.h""
#include ""spmiutil.h""

", 
                                footer: Environment.NewLine,
                                cppType: "interceptor_ICJI",
                                beforeCallFunc: (FunctionDecl decl)=> null,
                                afterCallFunc: (FunctionDecl decl)=> null,
                                wrappedObjectName: "original_ICorJitInfo");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ThunkGenerator - Generate thunks for the jit interface and for defining the set of instruction sets supported by the runtime, JIT, and crossgen2. Call by using the gen scripts which are aware of the right set of files generated and command line args.");
                return;
            }
            if (args[0] == "InstructionSetGenerator")
            {
                if (args.Length != 7)
                {
                    Console.WriteLine("Incorrect number of files specified for generation");
                }
                InstructionSetGenerator generator = new InstructionSetGenerator();
                if (!generator.ParseInput(new StreamReader(args[1])))
                    return;

                using (TextWriter tw = new StreamWriter(args[2]))
                {
                    Console.WriteLine("Generating {0}", args[2]);
                    generator.WriteManagedReadyToRunInstructionSet(tw);
                }

                using (TextWriter tw = new StreamWriter(args[3]))
                {
                    Console.WriteLine("Generating {0}", args[3]);
                    generator.WriteManagedReadyToRunInstructionSetHelper(tw);
                }

                using (TextWriter tw = new StreamWriter(args[4]))
                {
                    Console.WriteLine("Generating {0}", args[4]);
                    generator.WriteManagedJitInstructionSet(tw);
                }

                using (TextWriter tw = new StreamWriter(args[5]))
                {
                    Console.WriteLine("Generating {0}", args[5]);
                    generator.WriteNativeCorInfoInstructionSet(tw);
                }

                using (TextWriter tw = new StreamWriter(args[6]))
                {
                    Console.WriteLine("Generating {0}", args[6]);
                    generator.WriteNativeReadyToRunInstructionSet(tw);
                }
            }
            else
            {
                if (args.Length != 8)
                {
                    Console.WriteLine("Incorrect number of files specified for generation");
                }

                IEnumerable<FunctionDecl> functions = ParseInput(new StreamReader(args[0]));

                EmitStuff(1, WriteManagedThunkInterface);
                EmitStuff(2, WriteNativeWrapperInterface);
                EmitStuff(3, WriteAPI_Names);
                EmitStuff(4, API_Wrapper);
                EmitStuff(5, SPMI_ICorJitInfoImpl);
                EmitStuff(6, SPMI_ShimCounter_ICorJitInfo);
                EmitStuff(7, SPMI_ShimSimple_ICorJitInfo);

                void EmitStuff(int index, Action<TextWriter, IEnumerable<FunctionDecl>> printer)
                {
                    using (TextWriter tw = new StreamWriter(args[index]))
                    {
                        Console.WriteLine("Generating {0}", args[index]);
                        printer(tw, functions);
                    }
                }
            }
        }
    }
}
