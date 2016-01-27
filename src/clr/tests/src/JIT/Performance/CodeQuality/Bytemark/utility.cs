// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.IO;

public class Utility
{
    static public int sscanf(String stream, String format, Object[] results)
    {
        int fieldsRead = 0;
        int resultsIndex = 0;
        int formatIndex = 0;
        char fieldType = '\0';
        char charRead = '\0';
        bool readingField = false;
        bool eatWhiteSpace = false;
        StringReader srStream = new StringReader(stream);

        while (formatIndex < format.Length)
        {
            if (Char.IsWhiteSpace((char)format[formatIndex]))
            {
                eatWhiteSpace = true;
                formatIndex++;
                continue;
            }
            while (eatWhiteSpace)
            {
                if (!Char.IsWhiteSpace((char)srStream.Peek()))
                {
                    eatWhiteSpace = false;
                    break;
                }
                srStream.Read();
            }
            if ('%' == format[formatIndex])  //If we found a scan field type
            {
                StringBuilder sb = new StringBuilder();
                ++formatIndex;
                fieldType = format[formatIndex++];
                readingField = true;
                charRead = (char)srStream.Read();

                while (readingField)
                {
                    if (-1 == (short)charRead)
                    {
                        readingField = false;
                    }

                    sb.Append(charRead);

                    int intCharRead = srStream.Peek();
                    unchecked
                    {
                        charRead = (char)intCharRead;
                    }
                    if (Char.IsWhiteSpace(charRead) || ('c' == fieldType) || (-1 == intCharRead))
                    {
                        readingField = false;
                        fieldsRead++;

                        switch (fieldType)
                        {
                            case 'c':
                                results[resultsIndex++] = sb.ToString()[0];
                                break;
                            case 'd':
                            case 'i':
                                int parsedInt;
                                parsedInt = int.Parse(sb.ToString());
                                results[resultsIndex++] = parsedInt;
                                break;
                            case 'f':
                                double parsedDouble;
                                parsedDouble = double.Parse(sb.ToString());
                                results[resultsIndex++] = parsedDouble;
                                break;
                            case 's':
                                results[resultsIndex++] = sb.ToString();
                                break;
                        }
                        continue;
                    }
                    charRead = (char)srStream.Read();
                }
            }
        }

        return fieldsRead;
    }

    static public int fscanf(TextReader stream, String format, Object[] results)
    {
        String s = stream.ReadLine();
        if (null == s)
            return 0;
        return sscanf(s, format, results);
    }
}
