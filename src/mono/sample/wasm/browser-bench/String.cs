// Licensed to the .NET Foundation under one or more agreements.
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
                new NormalizeMeasurementASCII(),
                new TextInfoToLower(),
                new TextInfoToUpper(),
                new TextInfoToTitleCase(),
                new StringCompareMeasurement(),
                new StringEqualsMeasurement(),
                new CompareInfoMeasurement(),
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

        public class StringsCompare : StringMeasurement
        {
            protected string str2;

            public void InitializeStringsForComparison()
            {
                InitializeString();
                // worst case: strings may differ only with the last char
                data[len-1] = (char)random.Next(0x80);
                str2 = new string(data);
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
            public override void RunStep() => string.Compare(str, str2, cultureInfo, CompareOptions.None);
        }

        public class StringEqualsMeasurement : StringsCompare
        {
            public override Task BeforeBatch()
            {
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "String Equals";
            public override void RunStep() => string.Equals(str, str2, StringComparison.InvariantCulture);
        }

        public class CompareInfoMeasurement : StringsCompare
        {
            protected CompareInfo compareInfo;

            public override Task BeforeBatch()
            {
                compareInfo = new CultureInfo("tr-TR").CompareInfo;
                InitializeStringsForComparison();
                return Task.CompletedTask;
            }
            public override string Name => "CompareInfo Compare";
            public override void RunStep() => compareInfo.Compare(str, str2);
        }
    }
}
