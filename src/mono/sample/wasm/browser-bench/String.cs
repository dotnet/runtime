﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Globalization;

namespace Sample
{
    class StringTask : BenchTask
    {
        public override string Name => "String";
        Measurement[] measurements;

        public StringTask()
        {
            measurements = new Measurement[] {
                new NormalizeMeasurement(),
                new IsNormalizedMeasurement(),
                new NormalizeMeasurementASCII(),
                new TextInfoToLower(),
                new TextInfoToUpper(),
                new TextInfoToTitleCase(),
                new StringCompareMeasurement(),
                new StringEqualsMeasurement(),
                new CompareInfoCompareMeasurement(),
                new CompareInfoStartsWithMeasurement(),
                new CompareInfoEndsWithMeasurement(),
                new StringStartsWithMeasurement(),
                new StringEndsWithMeasurement(),
                new StringIndexOfMeasurement(),
                new StringLastIndexOfMeasurement(),
            };
        }

        public override Measurement[] Measurements
        {
            get
            {
                return measurements;
            }
        }

        public abstract class StringMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 30;
            protected Random random;
            protected char[] data;
            protected int len = 64 * 1024;
            protected string str;

            public void InitializeString()
            {
                data = new char[len];
                random = new(123456);
                for (int i = 0; i < len; i++)
                {
                    data[i] = (char)random.Next(0xd800);
                }
                str = new string(data);
            }

            public override Task BeforeBatch()
            {
                InitializeString();
                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;
                return Task.CompletedTask;
            }
        }

        public class NormalizeMeasurement : StringMeasurement
        {
            public override string Name => "Normalize";
            public override void RunStep() => str.Normalize();
        }

        public class IsNormalizedMeasurement : StringMeasurement
        {
            public override string Name => "IsNormalized";
            public override void RunStep() => str.IsNormalized();
        }

        public abstract class ASCIIStringMeasurement : StringMeasurement
        {
            public override Task BeforeBatch()
            {
                data = new char[len];
                random = new(123456);
                for (int i = 0; i < len; i++)
                    data[i] = (char)random.Next(0x80);

                str = new string(data);
                return Task.CompletedTask;
            }
        }

        public class NormalizeMeasurementASCII : ASCIIStringMeasurement
        {
            public override string Name => "Normalize ASCII";
            public override void RunStep() => str.Normalize();
        }

        public class TextInfoMeasurement : StringMeasurement
        {
            protected TextInfo textInfo;

            public override Task BeforeBatch()
            {
                textInfo = new CultureInfo("de-DE").TextInfo;
                InitializeString();
                return Task.CompletedTask;
            }
            public override string Name => "TextInfo";
        }

        public class TextInfoToLower : TextInfoMeasurement
        {
            public override string Name => "TextInfo ToLower";
            public override void RunStep() => textInfo.ToLower(str);
        }

        public class TextInfoToUpper : TextInfoMeasurement
        {
            public override string Name => "TextInfo ToUpper";
            public override void RunStep() => textInfo.ToUpper(str);
        }

        public class TextInfoToTitleCase : TextInfoMeasurement
        {
            public override string Name => "TextInfo ToTileCase";
            public override void RunStep() => textInfo.ToTitleCase(str);
        }

        public abstract class StringsCompare : StringMeasurement
        {
            protected string strAsciiSuffix;
            protected string strAsciiPrefix;
            protected string needleSameAsStrEnd;
            protected string needleSameAsStrStart;

            public void InitializeStringsForComparison()
            {
                InitializeString();
                needleSameAsStrEnd = new string(new ArraySegment<char>(data, len - 10, 10));
                needleSameAsStrStart = new string(new ArraySegment<char>(data, 0, 10));
                // worst case: strings may differ only with the last/first char
                char originalLastChar = data[len-1];
                data[len-1] = (char)random.Next(0x80);
                strAsciiSuffix = new string(data);
                int middleIdx = (int)(len/2);
                data[len-1] = originalLastChar;
                data[0] = (char)random.Next(0x80);
                strAsciiPrefix = new string(data);
            }
            public override string Name => "Strings Compare Base";
        }

        public class StringCompareMeasurement :  StringsCompare
        {
            protected CultureInfo cultureInfo;

            public override Task BeforeBatch()
            {
                cultureInfo = new CultureInfo("sk-SK");
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String Compare";
            public override void RunStep() => string.Compare(str, strAsciiSuffix, cultureInfo, CompareOptions.None);
        }

        public class StringEqualsMeasurement : StringsCompare
        {
            public override Task BeforeBatch()
            {
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String Equals";
            public override void RunStep() => string.Equals(str, strAsciiSuffix, StringComparison.InvariantCulture);
        }

        public class CompareInfoCompareMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("tr-TR").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "CompareInfo Compare";
            public override void RunStep() => compareInfo.Compare(str, strAsciiSuffix);
        }

        public class CompareInfoStartsWithMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("hy-AM").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "CompareInfo IsPrefix";
            public override void RunStep() => compareInfo.IsPrefix(str, strAsciiSuffix);
        }

        public class CompareInfoEndsWithMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("it-IT").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "CompareInfo IsSuffix";
            public override void RunStep() => compareInfo.IsSuffix(str, strAsciiPrefix);
        }

        public class StringStartsWithMeasurement : StringsCompare
        {
            protected CultureInfo cultureInfo;

            public override Task BeforeBatch()
            {
                cultureInfo = new CultureInfo("bs-BA");
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String StartsWith";
            public override void RunStep() => str.StartsWith(strAsciiSuffix, false, cultureInfo);
        }

        public class StringEndsWithMeasurement : StringsCompare
        {
            protected CultureInfo cultureInfo;

            public override Task BeforeBatch()
            {
                cultureInfo = new CultureInfo("nb-NO");
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String EndsWith";
            public override void RunStep() => str.EndsWith(strAsciiPrefix, false, cultureInfo);
        }

        public class StringIndexOfMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("nb-NO").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String IndexOf";
            public override void RunStep() => compareInfo.IndexOf(str, needleSameAsStrEnd, CompareOptions.None);
        }

        public class StringLastIndexOfMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("nb-NO").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String LastIndexOf";
            public override void RunStep() => compareInfo.LastIndexOf(str, needleSameAsStrStart, CompareOptions.None);
        }
    }
}
