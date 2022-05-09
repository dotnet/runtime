// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Diagnostics.CodeAnalysis;

//
// This class generates the icall_trampoline_dispatch () function used by the interpreter to call native code on WASM.
// It should be kept in sync with mono_wasm_interp_to_native_trampoline () in the runtime.
//

public class InterpToNativeGenerator : Task
{
    [Required, NotNull]
    public string? OutputPath { get; set; }

    [Output]
    public string? FileWrites { get; private set; } = "";

    // Default set of signatures
    private static readonly string[] cookies = new string[] {
        "V",
        "VI",
        "VII",
        "VIII",
        "VIIII",
        "VIIIII",
        "VIIIIII",
        "VIIIIIII",
        "VIIIIIIII",
        "VIIIIIIIII",
        "VIIIIIIIIII",
        "VIIIIIIIIIII",
        "VIIIIIIIIIIII",
        "VIIIIIIIIIIIII",
        "VIIIIIIIIIIIIII",
        "VIIIIIIIIIIIIIII",
        "I",
        "II",
        "III",
        "IIII",
        "IIIII",
        "IIIIIDII",
        "IIIIII",
        "IIIIIII",
        "IIIIIIII",
        "IIIIIIIII",
        "IIIIIIIIII",
        "IIIIIIIIIII",
        "IIIIIIIIIIII",
        "IIIIIIIIIIIII",
        "IIIIIIIIIIIIII",
        "IILIIII",
        "IIIL",
        "IF",
        "ID",
        "IIF",
        "IIFI",
        "IIFF",
        "IFFII",
        "IIFII",
        "IIFFI",
        "IIFFF",
        "IIFFFF",
        "IIFFFI",
        "IIFFII",
        "IIFIII",
        "IIFFFFI",
        "IIFFFFII",
        "IIIF",
        "IIIFI",
        "IIIFII",
        "IIIFIII",
        "IIIIF",
        "IIIIFI",
        "IIIIFII",
        "IIIIFIII",
        "IIIFFFF",
        "IIIFFFFF",
        "IIFFFFFF",
        "IIIFFFFFF",
        "IIIIIIIF",
        "IIIIIIIFF",
        "IIFFFFFFFF",
        "IIIFFFFFFFF",
        "IIIIIIFII",
        "IIIFFFFFFFFIII",
        "IIIIIFFFFIIII",
        "IFFFFFFI",
        "IIFFIII",
        "ILI",
        "IILLI",
        "L",
        "LL",
        "LI",
        "LIL",
        "LILI",
        "LILII",
        "DD",
        "DDI",
        "DDD",
        "DDDD",
        "VF",
        "VFF",
        "VFFF",
        "VFFFF",
        "VFFFFF",
        "VFFFFFF",
        "VFFFFFFF",
        "VFFFFFFFF",
        "VFI",
        "VIF",
        "VIFF",
        "VIFFFF",
        "VIFFFFF",
        "VIFFFFFF",
        "VIFFFFFI",
        "VIIFFI",
        "VIIF",
        "VIIFFF",
        "VIIFI",
        "FF",
        "FFI",
        "FFF",
        "FFFF",
        "DI",
        "FI",
        "IIL",
        "IILI",
        "IILIIIL",
        "IILLLI",
        "IDIII",
        "LII",
        "VID",
        "VILLI",
        "DID",
        "DIDD",
        "FIF",
        "FIFF",
        "LILL",
        "VL",
        "VIL",
        "VIIL",
        "FIFFF",
        "FII",
        "FIII",
        "FIIIIII",
        "IFFFFIIII",
        "IFFI",
        "IFFIF",
        "IFFIFI",
        "IFI",
        "IFIII",
        "IIFIFIIIII",
        "IIFIFIIIIII",
        "IIFIIIII",
        "IIFIIIIII",
        "IIIFFFII",
        "IIIFFIFFFII",
        "IIIFFIFFII",
        "IIIFFII",
        "IIIFFIIIII",
        "IIIIIF",
        "IIIIIFII",
        "IIIIIFIII",
        "IIIIIIFFI",
        "IIIIIIIFFI",
        "VIFFF",
        "VIFFFFI",
        "VIFFFI",
        "VIFFFIIFF",
        "VIFFI",
        "VIFI",
        "VIIFF",
        "VIIFFFF",
        "VIIFFII",
        "VIIIF",
        "VIIIFFII",
        "VIIIFFIII",
        "VIIIFI",
        "VIIIFII",
        "VIIIFIII",
        "VIIIIF",
        "IFFFFIII",
        "IFFIII",
        "VIIIIFFII",
        "IIILIIII",
        "IIILLI",
        "IL",
        "IFF",
        "IFFF",
        "IFFFF",
        "VLII",
        "IIIIL",
        "LIIII",
        "LIIIL",
        "IILL",
    };

    private static string TypeToSigType(char c)
    {
        switch (c)
        {
            case 'V': return "void";
            case 'I': return "int";
            case 'L': return "gint64";
            case 'F': return "float";
            case 'D': return "double";
            default:
                throw new Exception("Can't handle " + c);
        }
    }

    public override bool Execute()
    {
        try
        {
            string tmpFileName = Path.GetTempFileName();
            using (var w = File.CreateText(tmpFileName))
            {
                Emit(w);
            }

            if (Utils.CopyIfDifferent(tmpFileName, OutputPath, useHash: false))
                Log.LogMessage(MessageImportance.Low, $"Generating pinvoke table to '{OutputPath}'.");
            else
                Log.LogMessage(MessageImportance.Low, $"PInvoke table in {OutputPath} is unchanged.");

            FileWrites = OutputPath;

            File.Delete(tmpFileName);
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    public static void Emit(StreamWriter w)
    {
        w.WriteLine("/*");
        w.WriteLine("* GENERATED FILE, DON'T EDIT");
        w.WriteLine("* Generated by InterpToNativeGenerator");
        w.WriteLine("*/");

        var added = new HashSet<string>();

        var l = new List<string>();
        foreach (var c in cookies)
        {
            l.Add(c);
            added.Add(c);
        }
        var signatures = l.ToArray();

        foreach (var c in signatures)
        {
            w.WriteLine("static void");
            w.WriteLine($"wasm_invoke_{c.ToLower(CultureInfo.InvariantCulture)} (void *target_func, InterpMethodArguments *margs)");
            w.WriteLine("{");

            w.Write($"\ttypedef {TypeToSigType(c[0])} (*T)(");
            for (int i = 1; i < c.Length; ++i)
            {
                char p = c[i];
                if (i > 1)
                    w.Write(", ");
                w.Write($"{TypeToSigType(p)} arg_{i - 1}");
            }
            if (c.Length == 1)
                w.Write("void");

            w.WriteLine(");\n\tT func = (T)target_func;");

            var ctx = new EmitCtx();

            w.Write("\t");
            if (c[0] != 'V')
                w.Write($"{TypeToSigType(c[0])} res = ");

            w.Write("func (");
            for (int i = 1; i < c.Length; ++i)
            {
                char p = c[i];
                if (i > 1)
                    w.Write(", ");
                w.Write(ctx.Emit(p));
            }
            w.WriteLine(");");

            if (c[0] != 'V')
                w.WriteLine($"\t*({TypeToSigType(c[0])}*)margs->retval = res;");

            w.WriteLine("\n}\n");
        }

        Array.Sort(signatures);

        w.WriteLine("interp_to_native_signatures = {");
        foreach (var sig in signatures)
            w.WriteLine($"\"{sig}\",");
        w.WriteLine("};");
        w.WriteLine($"interp_to_native_signatures_count = {signatures.Length};");

        w.WriteLine("interp_to_native_invokes = {");
        foreach (var sig in signatures)
        {
            var lsig = sig.ToLower(CultureInfo.InvariantCulture);
            w.WriteLine($"wasm_invoke_{lsig},");
        }
        w.WriteLine("};");
    }

    private sealed class EmitCtx
    {
        private int iarg, farg;

        public string Emit(char c)
        {
            switch (c)
            {
                case 'I':
                    iarg += 1;
                    return $"(int)(gssize)margs->iargs [{iarg - 1}]";
                case 'F':
                    farg += 1;
                    return $"*(float*)&margs->fargs [FIDX ({farg - 1})]";
                case 'L':
                    iarg += 2;
                    return $"get_long_arg (margs, {iarg - 2})";
                case 'D':
                    farg += 1;
                    return $"margs->fargs [FIDX ({farg - 1})]";
                default:
                    throw new Exception("IDK how to handle " + c);
            }
        }
    }
}
