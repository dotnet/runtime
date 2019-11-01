// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if ((typenames.Length < 1) || (typenames.Length > 3))
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
        }
        public readonly string ThunkTypeName;
        public readonly string NativeTypeName;
        public readonly string ManagedTypeName;
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

            if (line.Contains("[ReturnAsParm]"))
            {
                ReturnAsParm = true;
                line = line.Replace("[ReturnAsParm]", string.Empty);
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
        public readonly bool ReturnAsParm = false;
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

        static void WriteManagedThunkInterface(TextWriter tr, IEnumerable<FunctionDecl> functionData)
        {
            // Write header
            tr.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! It IS AUTOGENERATED
using System;
using System.Runtime.InteropServices;

namespace Internal.JitInterface
{
    unsafe partial class CorInfoImpl
    {
");

#if false
            foreach (FunctionDecl decl in functionData)
            {
                string returnType = decl.ReturnType.ManagedTypeName;

                tr.Write("        " + returnType + " " + decl.FunctionName + "(");
                tr.Write("IntPtr thisHandle");
                foreach (Parameter param in decl.Parameters)
                {
                    tr.Write(", ");
                    tr.Write(param.Type.ManagedTypeName + " " + param.Name);
                }
                tr.WriteLine(")");
                tr.WriteLine("            { throw new NotImplementedException(); }");
            }
            tr.WriteLine();
#endif

            foreach (FunctionDecl decl in functionData)
            {
                tr.WriteLine("        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]");

                string returnType = decl.ReturnAsParm ? "void" : decl.ReturnType.ManagedTypeName;
                int marshalAs = returnType.LastIndexOf(']');
                string returnTypeWithDelegate = returnType.Insert((marshalAs != -1) ? (marshalAs + 1) : 0, "delegate ");

                tr.Write("        " + returnTypeWithDelegate + " " + "__" + decl.FunctionName + "(");
                tr.Write("IntPtr _this");
                tr.Write(", IntPtr* ppException");
                if (decl.ReturnAsParm)
                {
                    tr.Write(", out " + decl.ReturnType.ManagedTypeName + " _return");
                }
                foreach (Parameter param in decl.Parameters)
                {
                    tr.Write(", ");
                    tr.Write(param.Type.ManagedTypeName + " " + param.Name);
                }
                tr.WriteLine(");");
            }
            tr.WriteLine();

            foreach (FunctionDecl decl in functionData)
            {
                string returnType = decl.ReturnAsParm ? "void" : decl.ReturnType.ManagedTypeName;
                int marshalAs = returnType.LastIndexOf(']');
                string returnTypeWithStatic = returnType.Insert((marshalAs != -1) ? (marshalAs + 1) : 0, "static ");

                tr.Write("        " + returnTypeWithStatic + " " + "_" + decl.FunctionName + "(");
                tr.Write("IntPtr thisHandle");
                tr.Write(", IntPtr* ppException");
                if (decl.ReturnAsParm)
                {
                    tr.Write(", out " + decl.ReturnType.ManagedTypeName + " _return");
                }
                foreach (Parameter param in decl.Parameters)
                {
                    tr.Write(", ");
                    tr.Write(param.Type.ManagedTypeName + " " + param.Name);
                }
                tr.Write(@")
        {
            var _this = GetThis(thisHandle);
            try
            {
");
                bool isVoid = decl.ReturnAsParm || decl.ReturnType.ManagedTypeName == "void";
                tr.Write("                " + (isVoid ? "" : "return ") + "_this." + decl.FunctionName + "(");
                bool isFirst = true;
                if (decl.ReturnAsParm)
                {
                    tr.Write("out _return");
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
                        tr.Write(", ");
                    }

                    if (param.Type.ManagedTypeName.Contains("ref "))
                    {
                        tr.Write("ref ");
                    }
                    tr.Write(param.Name);
                }
                tr.Write(");");
                tr.Write(@"
            }
            catch (Exception ex)
            {
                *ppException = _this.AllocException(ex);
");
                if (!isVoid)
                {
                    string retunTypeWithoutMarshalAs = marshalAs == -1 ? returnType : returnType.Substring(marshalAs + 1);
                    tr.WriteLine("                return default(" + retunTypeWithoutMarshalAs + ");");
                }
                else if (decl.ReturnAsParm)
                {
                    tr.WriteLine("                _return = default(" + decl.ReturnType.ManagedTypeName + ");");
                }
                tr.WriteLine(@"            }");
                tr.WriteLine("        }");
                tr.WriteLine();
            }

            int total = functionData.Count();
            tr.WriteLine(@"
        static IntPtr GetUnmanagedCallbacks(out Object keepAlive)
        {
            IntPtr * callbacks = (IntPtr *)Marshal.AllocCoTaskMem(sizeof(IntPtr) * " + total + @");
            Object[] delegates = new Object[" + total + @"];
");

            int index = 0;
            foreach (FunctionDecl decl in functionData)
            {
                tr.WriteLine("            var d" + index + " = new " + "__" + decl.FunctionName + "(" + "_" + decl.FunctionName + ");");
                tr.WriteLine("            callbacks[" + index + "] = Marshal.GetFunctionPointerForDelegate(d" + index + ");");
                tr.WriteLine("            delegates[" + index + "] = d" + index + ";");
                index++;
            }

            tr.WriteLine(@"
            keepAlive = delegates;
            return (IntPtr)callbacks;
        }
    }
}
");
        }

        static void WriteNativeWrapperInterface(TextWriter tw, IEnumerable<FunctionDecl> functionData)
        {
            tw.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! It IS AUTOGENERATED
#include ""corinfoexception.h""

struct CORINFO_LOOKUP_KIND;

struct JitInterfaceCallbacks
{
");

            foreach (FunctionDecl decl in functionData)
            {
                string returnType = decl.ReturnAsParm ? "void" : decl.ReturnType.NativeTypeName;
                tw.Write("    " + returnType + " (* " + decl.FunctionName + ")(");
                tw.Write("void * thisHandle");
                tw.Write(", CorInfoException** ppException");
                if (decl.ReturnAsParm)
                {
                    tw.Write(", " + decl.ReturnType.NativeTypeName + "* _return");
                }
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write(", ");
                    tw.Write(param.Type.NativeTypeName + " " + param.Name);
                }
                tw.WriteLine(");");
            }

            tw.Write(@"
};

class JitInterfaceWrapper
{
    void * _thisHandle;
    JitInterfaceCallbacks * _callbacks;

public:
    JitInterfaceWrapper(void * thisHandle, void ** callbacks)
        : _thisHandle(thisHandle), _callbacks((JitInterfaceCallbacks *)callbacks)
    {
    }

");

            foreach (FunctionDecl decl in functionData)
            {
                tw.Write("    virtual " + decl.ReturnType.NativeTypeName + " " + decl.FunctionName + "(");
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
                    tw.Write(param.Type.NativeTypeName + " " + param.Name);
                }
                tw.Write(')');

                if (decl.ManualNativeWrapper)
                {
                    tw.WriteLine(';');
                    continue;
                }
                tw.Write(@"
    {
        CorInfoException* pException = nullptr;
        ");
                if (decl.ReturnType.NativeTypeName != "void")
                {
                    tw.Write(decl.ReturnType.NativeTypeName + " _ret = ");
                }
                tw.Write("_callbacks->" + decl.FunctionName + "(_thisHandle, &pException");
                foreach (Parameter param in decl.Parameters)
                {
                    tw.Write(", " + param.Name);
                }
                tw.Write(@");
        if (pException != nullptr)
            throw pException;
");
                if (decl.ReturnType.NativeTypeName != "void")
                {
                    tw.WriteLine("        return _ret;");
                }
                tw.WriteLine("    }");
                tw.WriteLine();
            }

            tw.WriteLine("};");
        }

        static void Main(string[] args)
        {
            IEnumerable<FunctionDecl> functions = ParseInput(new StreamReader(args[0]));
            using (TextWriter tw = new StreamWriter(args[1]))
            {
                Console.WriteLine("Generating {0}", args[1]);
                WriteManagedThunkInterface(tw, functions);
            }
            using (TextWriter tw = new StreamWriter(args[2]))
            {
                Console.WriteLine("Generating {0}", args[2]);
                WriteNativeWrapperInterface(tw, functions);
            }
        }
    }
}
