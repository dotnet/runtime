// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

    internal sealed class TypeReplacement
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

    internal sealed class Parameter
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

    internal sealed class FunctionDecl
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
                throw new Exception(string.Format("Type {0} unknown", returnType));
            }

            string parameterList = line.Substring(indexOfOpenParen + 1, indexOfCloseParen - indexOfOpenParen - 1).Canonicalize();
            string[] parametersString = parameterList.Length == 0 ? Array.Empty<string>() : parameterList.Split(',');
            List<Parameter> parameters = new List<Parameter>();

            foreach (string parameterString in parametersString)
            {
                int indexOfLastWhitespaceInParameter = parameterString.LastIndexOfAny(new char[] { ' ', '*' });
                string paramName = parameterString.Substring(indexOfLastWhitespaceInParameter + 1).Canonicalize();
                string paramType = parameterString.Substring(0, indexOfLastWhitespaceInParameter + 1).Canonicalize();
                TypeReplacement tr;
                if (!ThunkTypes.TryGetValue(paramType, out tr))
                {
                    throw new Exception(string.Format("Type {0} unknown", paramType));
                }
                parameters.Add(new Parameter(paramName, tr));
            }

            Parameters = parameters.ToArray();
        }

        public readonly string FunctionName;
        public readonly TypeReplacement ReturnType;
        public readonly Parameter[] Parameters;
        public readonly bool ManualNativeWrapper;
    }

    internal static class Program
    {
        private enum ParseMode
        {
            RETURNTYPES,
            NORMALTYPES,
            FUNCTIONS,
            IFDEFING
        }

        private static ReadOnlyCollection<FunctionDecl> ParseInput(StreamReader tr)
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

        private static void WriteAutogeneratedHeader(TextWriter tw)
        {
            // Write header
            tw.Write(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// To regenerate run the gen script in src/coreclr/tools/Common/JitInterface/ThunkGenerator
// and follow the instructions in docs/project/updating-jitinterface.md
");
        }

        private static void WriteManagedThunkInterface(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.JitInterface
{
    internal unsafe partial class CorInfoImpl
    {
        private struct ICorJitInfoCallbacks
        {
            [FixedAddressValueType]
            internal static readonly ICorJitInfoCallbacks s_callbacks;

            static ICorJitInfoCallbacks()
            {
");
            foreach (FunctionDecl decl in functionData)
            {
                tw.WriteLine($"                s_callbacks.{decl.FunctionName} = &_{decl.FunctionName};");
            }
            tw.WriteLine("            }");
            tw.WriteLine();

            foreach (FunctionDecl decl in functionData)
            {
                tw.Write("            public delegate* unmanaged<IntPtr, IntPtr*");
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write($", {param.Type.UnmanagedTypeName}");
                }
                tw.WriteLine($", {decl.ReturnType.UnmanagedTypeName}> {decl.FunctionName};");
            }
            tw.Write(@"        }

        private static IntPtr GetUnmanagedCallbacks()
        {
            return (IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(in ICorJitInfoCallbacks.s_callbacks));
        }

");

            foreach (FunctionDecl decl in functionData)
            {
                tw.WriteLine("        [UnmanagedCallersOnly]");
                tw.Write($"        private static {decl.ReturnType.UnmanagedTypeName} _{decl.FunctionName}(IntPtr thisHandle, IntPtr* ppException");
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

            tw.WriteLine(@"    }
}");
        }

        private static void WriteNativeWrapperInterface(TextWriter tw, IEnumerable<FunctionDecl> functionData)
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
                funcNameFunc: (FunctionDecl decl) => $"{decl.FunctionName}",
                beforeCallFunc: (FunctionDecl) => "    CorInfoExceptionClass* pException = nullptr;",
                afterCallFunc: (FunctionDecl decl) => "    if (pException != nullptr) throw pException;",
                wrappedObjectName: "_callbacks",
                useNativeType2: false,
                addVirtualPrefix: true,
                skipManualWrapper: true);

            tw.WriteLine("};");
        }

        private static void WriteAPI_Names(TextWriter tw, IEnumerable<FunctionDecl> functionData)
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

        private static void API_Wrapper_Generic_Core(TextWriter tw, IEnumerable<FunctionDecl> functionData, Func<FunctionDecl, string> funcNameFunc, Func<FunctionDecl, string> beforeCallFunc, Func<FunctionDecl, string> afterCallFunc, string wrappedObjectName, bool useNativeType2, bool addVirtualPrefix, bool skipManualWrapper)
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

        private static void API_Wrapper_Generic(TextWriter tw, IEnumerable<FunctionDecl> functionData, string header, string footer, string cppType, Func<FunctionDecl, string> beforeCallFunc, Func<FunctionDecl, string> afterCallFunc, string wrappedObjectName)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(header);

            API_Wrapper_Generic_Core(tw, functionData, funcNameFunc: (FunctionDecl decl) => $"{cppType}::{decl.FunctionName}", beforeCallFunc: beforeCallFunc, afterCallFunc: afterCallFunc, wrappedObjectName: wrappedObjectName, useNativeType2: true, addVirtualPrefix: false, skipManualWrapper: false);

            tw.Write(footer);
        }

        private static void API_Wrapper(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            API_Wrapper_Generic(tw, functionData,
                                header: @"
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
                                beforeCallFunc: (FunctionDecl decl) => $"    API_ENTER({decl.FunctionName});",
                                afterCallFunc: (FunctionDecl decl) => $"    API_LEAVE({decl.FunctionName});",
                                wrappedObjectName: "wrapHnd");
        }

        private static void SPMI_ICorJitInfoImpl(TextWriter tw, IEnumerable<FunctionDecl> functionData)
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
                tw.Write($"{Environment.NewLine}{decl.ReturnType.NativeTypeName2} {decl.FunctionName}(");
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

        private static string ManagedParameterType(TypeReplacement type)
            => type.IsBoolean || type.IsBOOL ? "bool" : type.ManagedTypeName;

        // The forwarders that the managed collector calls the EE through, for the queries it makes itself. Each
        // catches the exception the call throws, and returns its code, which the collector records.
        private static void SPMI_Shim_Forwarders(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.WriteLine();
            tw.WriteLine("// Included by shim.cpp, which defines RunCapturingException and the ManualNativeWrapper forwarders.");

            foreach (FunctionDecl decl in functionData.Where(d => !d.ManualNativeWrapper))
            {
                string returnType = decl.ReturnType.NativeTypeName;
                bool isVoid = returnType == "void";
                tw.WriteLine();
                tw.Write($"static {returnType} Forward_{decl.FunctionName}(ICorJitInfo* jitInfo, CorInfoExceptionClass** ppException");
                foreach (Parameter param in decl.Parameters)
                    tw.Write($", {param.Type.NativeTypeName} {param.Name}");
                tw.WriteLine(")");
                tw.WriteLine("{");
                if (!isVoid)
                    tw.WriteLine($"    {returnType} result{{}};");
                tw.Write($"    RunCapturingException(ppException, [&]() {{ {(isVoid ? "" : "result = ")}jitInfo->{decl.FunctionName}(");
                tw.Write(string.Join(", ", decl.Parameters.Select(p => p.Name)));
                tw.WriteLine("); });");
                if (!isVoid)
                    tw.WriteLine("    return result;");
                tw.WriteLine("}");
            }

            tw.WriteLine();
            tw.WriteLine("static void* const s_forwarders[] =");
            tw.WriteLine("{");
            foreach (FunctionDecl decl in functionData)
                tw.WriteLine($"    (void*)&Forward_{decl.FunctionName},");
            tw.WriteLine("};");
        }

        // The managed client for the forwarders: calls the EE's ICorJitInfo, and throws EEException if the call threw.
        private static void SPMI_Shim_OriginalJitInfo(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.Write(@"
using System;
using SuperPmi.Shim;

namespace Internal.JitInterface;

// The ICorJitInfo of the EE, called through the forwarders of the native shim library.
//
// With a pending call, the native side has already made the call that the JIT made (and the managed shim records):
// the method for it returns the result of that call, or throws if the call threw, instead of calling the EE.
internal readonly unsafe struct OriginalJitInfo
{
    private readonly IntPtr _target;
    private readonly Forwarders* _forwarders;
    private readonly ShimInterop.PendingCall* _pending;

    public OriginalJitInfo(IntPtr target, IntPtr forwarders, ShimInterop.PendingCall* pending = null)
    {
        _target = target;
        _forwarders = (Forwarders*)forwarders;
        _pending = pending != null ? pending : ShimInterop.PendingCall.None;
    }

    public IntPtr Target => _target;

#pragma warning disable CS0649 // The native shim fills in the table.
    private struct Forwarders
    {
");
            foreach (FunctionDecl decl in functionData)
            {
                tw.Write("        public delegate* unmanaged<IntPtr, IntPtr*");
                foreach (Parameter param in decl.Parameters)
                    tw.Write($", {param.Type.UnmanagedTypeName}");
                tw.WriteLine($", {decl.ReturnType.UnmanagedTypeName}> {decl.FunctionName};");
            }
            tw.WriteLine("    }");
            tw.WriteLine("#pragma warning restore CS0649");

            int index = -1;
            foreach (FunctionDecl decl in functionData)
            {
                index++;
                bool isVoid = decl.ReturnType.ManagedTypeName == "void";
                tw.WriteLine();
                tw.Write($"    public {ManagedParameterType(decl.ReturnType)} {decl.FunctionName}(");
                tw.Write(string.Join(", ", decl.Parameters.Select(p => $"{ManagedParameterType(p.Type)} {p.Name}")));
                tw.WriteLine(")");
                tw.WriteLine("    {");
                tw.WriteLine($"        if (_pending->TryTake({index}))");
                if (isVoid)
                    tw.WriteLine("            return;");
                else
                    tw.WriteLine($"            return *({decl.ReturnType.UnmanagedTypeName}*)_pending->Result{(decl.ReturnType.IsBoolean || decl.ReturnType.IsBOOL ? " != 0" : "")};");
                tw.WriteLine();
                tw.WriteLine("        IntPtr exception = 0;");
                if (!isVoid)
                    tw.WriteLine($"        {decl.ReturnType.UnmanagedTypeName} result;");

                // The by-ref parameters refer to the JIT's memory, but are pinned to get their addresses.
                string indent = "        ";
                foreach (Parameter param in decl.Parameters.Where(p => p.Type.IsByRef))
                {
                    tw.WriteLine($"        fixed ({param.Type.UnmanagedTypeName} p_{param.Name} = &{param.Name})");
                    indent = "            ";
                }

                tw.Write($"{indent}{(isVoid ? "" : "result = ")}_forwarders->{decl.FunctionName}(_target, &exception");
                foreach (Parameter param in decl.Parameters)
                {
                    if (param.Type.IsByRef)
                        tw.Write($", p_{param.Name}");
                    else if (param.Type.IsBoolean)
                        tw.Write($", {param.Name} ? (byte)1 : (byte)0");
                    else if (param.Type.IsBOOL)
                        tw.Write($", {param.Name} ? 1 : 0");
                    else
                        tw.Write($", {param.Name}");
                }
                tw.WriteLine(");");
                tw.WriteLine("        if (exception != 0)");
                tw.WriteLine("            throw new EEException(exception);");
                if (!isVoid)
                    tw.WriteLine($"        return result{(decl.ReturnType.IsBoolean || decl.ReturnType.IsBOOL ? " != 0" : "")};");
                tw.WriteLine("    }");
            }

            tw.WriteLine();
            tw.WriteLine("    // The name of the method of a pending call.");
            tw.WriteLine("    public static string GetMethodName(int index) => s_methodNames[index];");
            tw.WriteLine();
            tw.WriteLine("    private static readonly string[] s_methodNames =");
            tw.WriteLine("    [");
            foreach (FunctionDecl decl in functionData)
                tw.WriteLine($"        \"{decl.FunctionName}\",");
            tw.WriteLine("    ];");
            tw.WriteLine("}");
        }

        // The methods of ShimJitInfo (in shim.cpp) that shim.cpp implements itself.
        private static readonly HashSet<string> s_shimManualMethods = new HashSet<string>
        {
            // The JIT may pass null for the results, which the collector records regardless.
            "getNewHelper",
            "isEnum",
            "getHelperFtn",
            // The collector's own error trap, which doesn't call the EE.
            "runWithSPMIErrorTrap",
            // The collector records the entry point from before the call too, with a callback of its own.
            "updateEntryPointForTailCall",
        };

        // The methods that the collector records before the EE gets the call: the EE frees the arrays of the
        // setters, and doAssert may not return.
        private static readonly HashSet<string> s_recordingBeforeMethods = new HashSet<string>
        {
            "setBoundaries",
            "setVars",
            "setPatchpointInfo",
            "doAssert",
        };

        // The ICorJitInfo that the JIT gets from the native shims: each method calls the EE's, and the shim's
        // CallThenRecord (or RecordBefore) either counts the call (superpmi-shim-counter), or has the managed collector
        // record it (through the callbacks of CorInfoImpl_generated.cs). The managed collector gets the result of the
        // call from OriginalJitInfo's pending call, rather than calling the EE itself, so that an exception of the EE
        // propagates to the JIT without going through managed code.
        private static void SPMI_Shim_JitInfo(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            WriteAutogeneratedHeader(tw);
            tw.WriteLine();
            tw.WriteLine("// Included in the class body of ShimJitInfo by shim.cpp, which defines CallThenRecord, RecordBefore and the");
            tw.WriteLine("// manual methods.");

            int index = -1;
            foreach (FunctionDecl decl in functionData)
            {
                index++;
                if (s_shimManualMethods.Contains(decl.FunctionName))
                {
                    tw.WriteLine();
                    tw.WriteLine($"    static const int Index_{decl.FunctionName} = {index};");
                    continue;
                }

                string returnType = decl.ReturnType.NativeTypeName;
                bool isVoid = returnType == "void";
                string args = string.Join(", ", decl.Parameters.Select(p => p.Name));
                string callbackArgs = string.Concat(decl.Parameters.Select(p => ", " + p.Name));
                string originalCall = $"_original->{decl.FunctionName}({args})";
                string recordCallback = $"[&](CorInfoExceptionClass** ppException) {{ _callbacks->{decl.FunctionName}(_thisHandle, ppException{callbackArgs}); }}";

                tw.WriteLine();
                tw.Write($"    {returnType} {decl.FunctionName}(");
                tw.Write(string.Join(", ", decl.Parameters.Select(p => $"{p.Type.NativeTypeName} {p.Name}")));
                tw.WriteLine(") override");
                tw.WriteLine("    {");
                if (!isVoid)
                    tw.WriteLine($"        {returnType} result{{}};");
                string resultPointer = isVoid ? "nullptr" : "&result";
                if (s_recordingBeforeMethods.Contains(decl.FunctionName))
                {
                    tw.WriteLine($"        RecordBefore({index}, \"{decl.FunctionName}\", {resultPointer}, {recordCallback});");
                    tw.WriteLine($"        {(isVoid ? "" : "result = ")}{originalCall};");
                }
                else
                {
                    tw.WriteLine($"        CallThenRecord({index}, \"{decl.FunctionName}\", {resultPointer},");
                    tw.WriteLine($"            [&]() {{ {(isVoid ? "" : "result = ")}{originalCall}; }},");
                    tw.WriteLine($"            {recordCallback});");
                }
                if (!isVoid)
                    tw.WriteLine("        return result;");
                tw.WriteLine("    }");
            }
        }

        private static void GenerateNewJitEEVersion(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            Guid newGuid = Guid.NewGuid();
            string[] hex = newGuid.ToString("X").Replace("{", "").Replace("}", "").Split(',');

            string newGuidStr = $$"""
constexpr GUID JITEEVersionIdentifier = { /* {{newGuid:D}} */
    {{hex[0]}},
    {{hex[1]}},
    {{hex[2]}},
    {{{hex[3]}}, {{hex[4]}}, {{hex[5]}}, {{hex[6]}}, {{hex[7]}}, {{hex[8]}}, {{hex[9]}}, {{hex[10]}}}
  };
""";
            string pattern = @"constexpr GUID JITEEVersionIdentifier = .*\n.*\n.*\n.*\n.*\n.*;";
            string output = Regex.Replace(File.ReadAllText(file), pattern, newGuidStr);
            File.WriteAllText(file, output);
            Console.WriteLine($"Updated to {newGuid}");
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ThunkGenerator - Generate thunks for the jit interface and for defining the set of instruction sets supported by the runtime, JIT, and crossgen2. Call by using the gen scripts which are aware of the right set of files generated and command line args.");
                return;
            }

            if (args[0] == "NewJITEEVersion")
            {
                if (args.Length != 2)
                {
                    Console.WriteLine("Incorrect number of arguments specified for NewJITEEVersion");
                    return;
                }
                GenerateNewJitEEVersion(args[1]);
            }
            else if (args[0] == "InstructionSetGenerator")
            {
                if (args.Length != 8)
                {
                    Console.WriteLine("Incorrect number of files specified for generation");
                    return;
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

                using (TextWriter tw = new StreamWriter(args[7]))
                {
                    Console.WriteLine("Generating {0}", args[7]);
                    generator.WriteManagedJitInstructionSetParser(tw);
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
                if (args.Length != 9)
                {
                    Console.WriteLine("Incorrect number of files specified for generation");
                    return;
                }

                IEnumerable<FunctionDecl> functions = ParseInput(new StreamReader(args[0]));

                EmitStuff(1, WriteManagedThunkInterface);
                EmitStuff(2, WriteNativeWrapperInterface);
                EmitStuff(3, WriteAPI_Names);
                EmitStuff(4, API_Wrapper);
                EmitStuff(5, SPMI_ICorJitInfoImpl);
                EmitStuff(6, SPMI_Shim_Forwarders);
                EmitStuff(7, SPMI_Shim_OriginalJitInfo);
                EmitStuff(8, SPMI_Shim_JitInfo);

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
