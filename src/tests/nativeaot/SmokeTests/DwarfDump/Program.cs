// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Program
{
    public static int Main(string[] args)
    {
        UseTypesUnderTest();

        var llvmDwarfDumpPath = Path.Combine(
            Environment.GetEnvironmentVariable("CORE_ROOT"),
            "SuperFileCheck",
            "runtimes",
            RuntimeInformation.RuntimeIdentifier,
            "native",
            "llvm-dwarfdump");

        if (!File.Exists(llvmDwarfDumpPath))
        {
            // Linux-only test
            Console.WriteLine("Could not find llvm-dwarfdump at path: " + llvmDwarfDumpPath);
            return 1;
        }

        Console.WriteLine("Running llvm-dwarfdump");
        ProcessExitStatus status = Process.Run(new ProcessStartInfo(llvmDwarfDumpPath, ["--version"])
        {
            InheritedHandles = [],
        });
        if (status.ExitCode != 0)
        {
            return 3;
        }

        ProcessTextOutput listsResult = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--debug-info", "--recurse-depth=0", "--debug-loc", "--debug-ranges", "--debug-loclists", "--debug-rnglists", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (listsResult.ExitStatus.ExitCode != 0)
        {
            Console.Error.WriteLine(listsResult.StandardError);
            return 4;
        }

        ProcessTextOutput result = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--verify", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        // A verifier that was killed or crashed reports nothing, which would look like a clean il.cpp.
        // It always ends with a summary line, and fails normally because of the native units.
        string output = result.StandardOutput.TrimEnd();
        if (result.ExitStatus.Signal is not null || result.ExitStatus.Canceled ||
            !(output.EndsWith("Errors detected.") || output.EndsWith("No errors.")))
        {
            Console.Error.WriteLine($"llvm-dwarfdump --verify did not complete (exit code {result.ExitStatus.ExitCode}, signal {result.ExitStatus.Signal}).");
            Console.Error.WriteLine(result.StandardError);
            return 5;
        }

        int count = 0;
        bool foundIlCpp = false;
        bool insideIlCpp = false;
        foreach (string rawLine in result.StandardOutput.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.StartsWith("Verifying unit:"))
            {
                if (line.EndsWith("\"il.cpp\""))
                {
                    foundIlCpp = true;
                    insideIlCpp = true;
                }
                else
                {
                    insideIlCpp = false;
                }
            }
            if ((line.Contains("warning:") || line.Contains("error:")) && insideIlCpp)
            {
                count++;
            }
        }

        if (!foundIlCpp)
        {
            Console.Error.WriteLine($"llvm-dwarfdump failed. Cound not find unit named \"il.cpp\".");
            return 10;
        }

        Console.WriteLine($"Found {count} warnings and errors");
        if (count != 0)
        {
            Console.WriteLine("This is likely a result of debug info changes. To see the new output, run the following command:");
            Console.WriteLine("\tllvm-dwarfdump --verify " + Environment.ProcessPath);
            return 10;
        }

        // References to classes are resolved to their complete type, there's no separate declaration. Tools
        // like dsymutil only keep the referenced types, and would drop a complete type that is only found by name.
        ProcessTextOutput typeResult = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--debug-info", "--regex", $"--name=.*_{nameof(Program)}$", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (typeResult.ExitStatus.ExitCode != 0 ||
            !typeResult.StandardOutput.Contains("DW_TAG_class_type") ||
            typeResult.StandardOutput.Contains("DW_AT_declaration"))
        {
            Console.Error.WriteLine($"Unexpected debug info for {nameof(Program)}:");
            Console.Error.WriteLine(typeResult.StandardOutput);
            Console.Error.WriteLine(typeResult.StandardError);
            return 11;
        }

        // Non-GC statics are stored directly in a data section, GC statics are stored in an object referenced
        // by the static base, so their location dereferences the static base once.
        string gcStaticLocation = GetStaticFieldLocation(llvmDwarfDumpPath, nameof(StaticFields.GcStaticDeclaredFirst));
        string nonGcStaticLocation = GetStaticFieldLocation(llvmDwarfDumpPath, nameof(StaticFields.NonGcStaticDeclaredSecond));
        if (gcStaticLocation is null || gcStaticLocation.Split("DW_OP_deref").Length - 1 != 1 ||
            nonGcStaticLocation is null || nonGcStaticLocation.Contains("DW_OP_deref"))
        {
            Console.Error.WriteLine($"Unexpected static field locations. GC static: {gcStaticLocation}, non-GC static: {nonGcStaticLocation}.");
            return 12;
        }

        // The storage of thread statics can't be described by a location expression, they are only declared.
        ProcessTextOutput threadStaticResult = RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", $"--name={nameof(ThreadStaticHolder<string>.ThreadStaticField)}");
        if (!threadStaticResult.StandardOutput.Contains("DW_TAG_member") ||
            GetStaticFieldLocation(llvmDwarfDumpPath, nameof(ThreadStaticHolder<string>.ThreadStaticField)) is not null)
        {
            Console.Error.WriteLine($"Unexpected debug info for {nameof(ThreadStaticHolder<string>.ThreadStaticField)}:");
            Console.Error.WriteLine(threadStaticResult.StandardOutput);
            return 13;
        }

        // The System.Array base class describes the number of elements, the members of array types follow it.
        (ulong arrayBaseSize, List<(string Name, ulong Offset)> arrayBaseMembers) = GetClassLayout(llvmDwarfDumpPath, "S_P_CoreLib_System_Array");
        (_, List<(string Name, ulong Offset)> intArrayMembers) = GetClassLayout(llvmDwarfDumpPath, "__Array<Int32>");
        if (!arrayBaseMembers.Exists(m => m.Name == "_numComponents") ||
            intArrayMembers.Count == 0 ||
            intArrayMembers.Exists(m => m.Offset < arrayBaseSize))
        {
            Console.Error.WriteLine($"Unexpected array layout. System.Array: size {arrayBaseSize}, members {string.Join(", ", arrayBaseMembers)}. int[]: members {string.Join(", ", intArrayMembers)}.");
            return 14;
        }

        // Interfaces are described as classes deriving from Object, so they can't be smaller than Object.
        (ulong objectSize, _) = GetClassLayout(llvmDwarfDumpPath, "Object");
        (ulong interfaceSize, _) = GetClassLayout(llvmDwarfDumpPath, $".*_{nameof(ITestInterface)}$", isRegex: true);
        if (objectSize == 0 || interfaceSize < objectSize)
        {
            Console.Error.WriteLine($"Unexpected size of {nameof(ITestInterface)}: {interfaceSize}, Object: {objectSize}.");
            return 15;
        }

        // Enums, including boxed ones, are only described by an enumeration type.
        ProcessTextOutput enumResult = RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", "--regex", $"--name=.*_{nameof(TestEnum)}$");
        if (!enumResult.StandardOutput.Contains("DW_TAG_enumeration_type") ||
            enumResult.StandardOutput.Contains("DW_TAG_class_type"))
        {
            Console.Error.WriteLine($"Unexpected debug info for {nameof(TestEnum)}:");
            Console.Error.WriteLine(enumResult.StandardOutput);
            return 16;
        }

        // The this parameter is a pointer, like the object pointer of the member function declaration.
        if (!CheckThisParameterIsPointer(llvmDwarfDumpPath, nameof(ThisTestClass.GetValueFromClass)) ||
            !CheckThisParameterIsPointer(llvmDwarfDumpPath, nameof(ThisTestStruct.GetValueFromStruct)))
        {
            return 17;
        }

        return 100;
    }

    // NativeAOT only describes the types and methods that the program uses,
    // this uses the ones inspected by the checks of Main.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseTypesUnderTest()
    {
        StaticFields.Initialize();
        new ThreadStaticHolder<string>().Set(42);
        GC.KeepAlive(new int[] { 1, 2, 3 });
        GetValue(new TestInterfaceImplementation());
        GC.KeepAlive((object)TestEnum.Second);
        new ThisTestClass { Value = 21 }.GetValueFromClass();
        new ThisTestStruct { Value = 21 }.GetValueFromStruct();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetValue(ITestInterface value) => value.GetValue();

    private static ProcessTextOutput RunLlvmDwarfDump(string llvmDwarfDumpPath, params string[] arguments)
    {
        ProcessTextOutput result = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, [.. arguments, Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (result.ExitStatus.ExitCode != 0)
        {
            Console.Error.WriteLine(result.StandardError);
        }

        return result;
    }

    // Static fields are matched both by their member declaration and by their variable definition, which has the location.
    private static string GetStaticFieldLocation(string llvmDwarfDumpPath, string fieldName)
    {
        ProcessTextOutput result = RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", $"--name={fieldName}");

        bool insideVariable = false;
        foreach (string rawLine in result.StandardOutput.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Contains("DW_TAG_"))
            {
                insideVariable = line.EndsWith("DW_TAG_variable");
            }
            else if (insideVariable && line.StartsWith("DW_AT_location"))
            {
                return line;
            }
        }

        return null;
    }

    // Returns the size and the instance fields of the complete class types with the given name.
    private static (ulong Size, List<(string Name, ulong Offset)> Members) GetClassLayout(string llvmDwarfDumpPath, string className, bool isRegex = false)
    {
        ProcessTextOutput result = isRegex ?
            RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", "--show-children", "--regex", $"--name={className}") :
            RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", "--show-children", $"--name={className}");

        ulong size = 0;
        List<(string Name, ulong Offset)> members = new();
        string tag = null;
        string memberName = null;
        foreach (string rawLine in result.StandardOutput.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Contains("DW_TAG_") || line.EndsWith("NULL"))
            {
                tag = line.Substring(line.IndexOf(' ') + 1).Trim();
                memberName = null;
            }
            else if (tag == "DW_TAG_class_type" && line.StartsWith("DW_AT_byte_size"))
            {
                // Forward declarations have no size, the complete type has.
                size = Math.Max(size, ParseAttributeValue(line));
            }
            else if (tag == "DW_TAG_member" && line.StartsWith("DW_AT_name"))
            {
                memberName = line.Substring(line.IndexOf('"') + 1).TrimEnd(')', '"');
            }
            else if (tag == "DW_TAG_member" && line.StartsWith("DW_AT_data_member_location"))
            {
                members.Add((memberName, ParseAttributeValue(line)));
            }
        }

        return (size, members);

        static ulong ParseAttributeValue(string line)
        {
            string value = line.Substring(line.IndexOf('(') + 1).TrimEnd(')');
            return Convert.ToUInt64(value, 16);
        }
    }

    private static bool CheckThisParameterIsPointer(string llvmDwarfDumpPath, string methodName)
    {
        ProcessTextOutput result = RunLlvmDwarfDump(llvmDwarfDumpPath, "--debug-info", "--show-children", $"--name={methodName}");

        // The declaration of the method has an unnamed this parameter, its definition has a parameter named this.
        bool insideParameter = false;
        bool isThis = false;
        foreach (string rawLine in result.StandardOutput.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Contains("DW_TAG_"))
            {
                insideParameter = line.EndsWith("DW_TAG_formal_parameter");
                isThis = false;
            }
            else if (insideParameter && line.StartsWith("DW_AT_name"))
            {
                isThis = line.EndsWith("(\"this\")");
            }
            else if (isThis && line.StartsWith("DW_AT_type"))
            {
                if (!line.EndsWith("*\")"))
                {
                    Console.Error.WriteLine($"Unexpected type for the this parameter of {methodName}: {line}");
                    return false;
                }

                return true;
            }
        }

        Console.Error.WriteLine($"Could not find the this parameter of {methodName}.");
        return false;
    }
}

static class StaticFields
{
    // Declared in the opposite order of the storage regions, which are emitted non-GC statics first.
    public static string GcStaticDeclaredFirst;
    public static int NonGcStaticDeclaredSecond;

    // Types are only described in the debug info when something refers to them, like their methods.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        GcStaticDeclaredFirst = "gc";
        NonGcStaticDeclaredSecond = 42;
    }
}

// Thread statics of types sharing code with other instantiations are not inlined in the thread local storage
// of the module, they are reached through the thread static base of the type.
class ThreadStaticHolder<T>
{
    [ThreadStatic]
    public static int ThreadStaticField;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Set(int value) => ThreadStaticField = value;
}

interface ITestInterface
{
    int GetValue();
}

class TestInterfaceImplementation : ITestInterface
{
    public int GetValue() => 42;
}

enum TestEnum
{
    First,
    Second,
}

class ThisTestClass
{
    public int Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetValueFromClass() => Value;
}

struct ThisTestStruct
{
    public int Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetValueFromStruct() => Value;
}
