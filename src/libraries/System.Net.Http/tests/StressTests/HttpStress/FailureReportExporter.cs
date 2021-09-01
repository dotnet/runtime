// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public static void Test()
        {
            StressFailureType a = TestGetFailureFingerpint1("a")!;
            StressFailureType b = TestGetFailureFingerpint1("b")!;
            StressFailureType c = TestGetFailureFingerpint2()!;
            StressFailureType d = TestGetFailureFingerpint3()!;
            XDocument doc = ExportFailures(new Configuration() { HttpVersion = new Version(1, 1) }, new[] { a, b, c, d });
            Console.WriteLine(doc);
        }

        internal static XDocument ExportFailures(Configuration configuration, IEnumerable<StressFailureType> failures)
        {
            XElement root = new XElement("FailureReport",
                new XAttribute("Timestamp", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("HttpVersion", configuration.HttpVersion),
                new XAttribute("OSDescription", RuntimeInformation.OSDescription),
                failures.Select(FailureToXml));
            return new XDocument(root);

            static XElement FailureToXml(StressFailureType f) =>
                new XElement("Failure",
                new XAttribute("FailureTypeFingerprint", f.Fingerprint),
                new XAttribute("FailureCount", f.FailureCount),
                new XElement("FailureText", new XCData(f.ErrorText)));
        }

        private static StressFailureType MakeFailure(Exception ex)
        {
            var key = ClassifyFailure(ex);
            string fingerprint = GetFailureFingerprint(key);
            return new StressFailureType(ex.ToString(), fingerprint);
        }

        internal static StressFailureType? TestGetFailureFingerpint1(string haha)
        {
            try
            {
                Foo(haha);
                return null;
            }
            catch (Exception ex)
            {
                return MakeFailure(ex);
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

        internal static StressFailureType? TestGetFailureFingerpint2()
        {
            try
            {
                Foo();
                return null;
            }
            catch (Exception ex)
            {
                return MakeFailure(ex);
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

        internal static StressFailureType? TestGetFailureFingerpint3()
        {
            try
            {
                Foo();
                return null;
            }
            catch (Exception ex)
            {
                return MakeFailure(ex);
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
