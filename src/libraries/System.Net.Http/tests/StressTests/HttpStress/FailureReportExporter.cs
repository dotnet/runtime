// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HttpStress
{
    public static class FailureReportExporter
    {
        public static string GetFailureFingerprint((Type, string, string)[] key)
        {
            using BinaryWriter bw = new BinaryWriter(new MemoryStream());
            foreach ((Type ex, string message, string callSite) in key)
            {
                bw.Write(ex.GetHashCode());
                bw.Write(message);
                bw.Write(callSite);
            }
            bw.Seek(0, SeekOrigin.Begin);
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(bw.BaseStream);
            StringBuilder sb = new StringBuilder();
            foreach (byte x in hash)
            {
                sb.Append(x.ToString("X2"));
            }
            return sb.ToString();
        }

        public static void TestGetFailureFingerpint1(string haha)
        {
            try
            {
                Foo(haha);
            }
            catch (Exception ex)
            {
                var key = ClassifyFailure(ex);
                Console.WriteLine(GetFailureFingerprint(key));
            }

            void Foo(string haha)
            {
                try
                {
                    Bar();
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception(haha, ex);
                }
            }

            void Bar()
            {
                throw new InvalidOperationException("lol");
            }

        }

        public static void TestGetFailureFingerpint2()
        {
            try
            {
                Foo();
            }
            catch (Exception ex)
            {
                var key = ClassifyFailure(ex);
                Console.WriteLine(GetFailureFingerprint(key));
            }

            void Foo()
            {
                try
                {
                    Bar();
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception("boo", ex);
                }
            }

            void Bar()
            {
                throw new InvalidOperationException("lol");
            }

        }

        public static void TestGetFailureFingerpint3()
        {
            try
            {
                Foo();
            }
            catch (Exception ex)
            {
                var key = ClassifyFailure(ex);
                Console.WriteLine(GetFailureFingerprint(key));
            }

            void Foo()
            {
                try
                {
                    Bar();
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception("boo", ex);
                }
            }

            void Bar()
            {
                try
                {
                    Bz();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("lol", ex);
                }   
            }

            void Bz()
            {
                throw new OutOfMemoryException();
            }
        }

        static (Type exception, string message, string callSite)[] ClassifyFailure(Exception exn)
        {
            var acc = new List<(Type exception, string message, string callSite)>();

            for (Exception? e = exn; e != null;)
            {
                acc.Add((e.GetType(), e.Message ?? "", new StackTrace(e, true).GetFrame(0)?.ToString() ?? ""));
                e = e.InnerException;
            }

            return acc.ToArray();
        }
    }
}
