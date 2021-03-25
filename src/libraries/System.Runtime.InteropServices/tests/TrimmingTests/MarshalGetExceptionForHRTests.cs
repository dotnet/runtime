// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

class Program
{
    static int Main(string[] args)
    {
        Dictionary<uint, string> expectedExceptions = new()
        {
            { 0x80000000, "System.Runtime.InteropServices.COMException" },
            { 0x8000000F, "System.TypeLoadException" },
            { 0x80000013, "System.ObjectDisposedException" },
            { 0x8000211D, "System.Reflection.AmbiguousMatchException" },
            { 0x80004001, "System.NotImplementedException" },
            { 0x80004002, "System.InvalidCastException" },
            { 0x80004003, "System.NullReferenceException" },
            { 0x8002000E, "System.Reflection.TargetParameterCountException" },
            { 0x80020012, "System.DivideByZeroException" },
            { 0x80030003, "System.IO.DirectoryNotFoundException" },
            { 0x80070002, "System.IO.FileNotFoundException" },
            { 0x80070004, "System.IO.FileLoadException" },
            { 0x80070005, "System.UnauthorizedAccessException" },
            { 0x8007000B, "System.BadImageFormatException" },
            { 0x8007000E, "System.OutOfMemoryException" },
            { 0x80070026, "System.IO.EndOfStreamException" },
            { 0x80070057, "System.ArgumentException" },
            { 0x800700CE, "System.IO.PathTooLongException" },
            { 0x80070216, "System.ArithmeticException" },
            { 0x800703E9, "System.StackOverflowException" },
            { 0x80070459, "System.ArgumentOutOfRangeException" },
            { 0x800A0006, "System.OverflowException" },
            { 0x800A0009, "System.IndexOutOfRangeException" },
            { 0x800A0039, "System.IO.IOException" },
            { 0x800A0046, "System.Security.SecurityException" },
            { 0x800A01B6, "System.NotSupportedException" },
            { 0x800A01CD, "System.MissingMemberException" },
            { 0x80131013, "System.TypeUnloadedException" },
            { 0x8013106A, "System.Runtime.AmbiguousImplementationException" },
            { 0x801311E6, "System.MethodAccessException" },
            { 0x80131430, "System.Security.Cryptography.CryptographicException" },
            { 0x80131500, "System.Exception" },
            { 0x80131501, "System.SystemException" },
            { 0x80131503, "System.ArrayTypeMismatchException" },
            { 0x80131506, "System.ExecutionEngineException" },
            { 0x80131507, "System.FieldAccessException" },
            { 0x80131509, "System.InvalidOperationException" },
            { 0x8013150C, "System.Runtime.Serialization.SerializationException" },
            { 0x8013150D, "System.Security.VerificationException" },
            { 0x80131511, "System.MissingFieldException" },
            { 0x80131513, "System.MissingMethodException" },
            { 0x80131514, "System.MulticastNotSupportedException" },
            { 0x80131517, "System.RankException" },
            { 0x80131518, "System.Threading.SynchronizationLockException" },
            { 0x80131519, "System.Threading.ThreadInterruptedException" },
            { 0x8013151A, "System.MemberAccessException" },
            { 0x80131520, "System.Threading.ThreadStateException" },
            { 0x80131523, "System.EntryPointNotFoundException" },
            { 0x80131524, "System.DllNotFoundException" },
            { 0x80131527, "System.Runtime.InteropServices.InvalidComObjectException" },
            { 0x80131528, "System.NotFiniteNumberException" },
            { 0x80131529, "System.DuplicateWaitObjectException" },
            { 0x80131531, "System.Runtime.InteropServices.InvalidOleVariantTypeException" },
            { 0x80131532, "System.Resources.MissingManifestResourceException" },
            { 0x80131533, "System.Runtime.InteropServices.SafeArrayTypeMismatchException" },
            { 0x80131535, "System.Runtime.InteropServices.MarshalDirectiveException" },
            { 0x80131537, "System.FormatException" },
            { 0x80131538, "System.Runtime.InteropServices.SafeArrayRankMismatchException" },
            { 0x80131539, "System.PlatformNotSupportedException" },
            { 0x8013153A, "System.InvalidProgramException" },
            { 0x8013153B, "System.OperationCanceledException" },
            { 0x80131541, "System.DataMisalignedException" },
            { 0x80131543, "System.TypeAccessException" },
            { 0x80131578, "System.InsufficientExecutionStackException" },
            { 0x80131600, "System.ApplicationException" },
            { 0x80131601, "System.Reflection.InvalidFilterCriteriaException" },
            { 0x80131603, "System.Reflection.TargetException" },
            { 0x80131605, "System.Reflection.CustomAttributeFormatException" }
        };

        foreach (var expectedKv in expectedExceptions)
        {
            Exception e = Marshal.GetExceptionForHR((int)expectedKv.Key);
            Type exceptionType = e.GetType();
            ConstructorInfo ctor = exceptionType.GetConstructor(new Type[] { typeof(string) });
            if (ctor != null)
            {
                e = (Exception)ctor.Invoke(new object[] { "test" });
            }
            else
            {
                Console.WriteLine($"Type `{exceptionType.FullName}` does not have constructor");
                return -1;
            }

            if (e == null)
            {
                Console.WriteLine($"Expected exception `{expectedKv.Value}` for HR {expectedKv.Key:X8} but got `{exceptionType.FullName}.`");
                return -1;
            }
        }

        return 100;
    }
}
