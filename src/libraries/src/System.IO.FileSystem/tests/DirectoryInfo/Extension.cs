// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using Xunit;

public class DirectoryInfo_Extension
{
    public static String s_strClassMethod = "DirectoryInfo.Extension";
    public static String s_strTFName = "Extension.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            String fileName = "Testing";
            DirectoryInfo dir;

            strLoc = "Err_0001";
            // With no extension
            iCountTestcases++;
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != "")
            {
                iCountErrors++;
                printerr("Error_0002! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_0003";
            // With an extension
            iCountTestcases++;
            fileName = "foo.bar";
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != ".bar")
            {
                iCountErrors++;
                printerr("Error_0004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_1003";
            // With valid extenstion but file name with special symbols.
            iCountTestcases++;
            fileName = "foo.bar.fkl;fkds92-509450-4359.$#%()#%().%#(%)_#(%_).cool";
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != ".cool")
            {
                iCountErrors++;
                printerr("Error_1004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_2003";
            // With a long extension	    
            iCountTestcases++;
            String extension = ".bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            fileName = "AAAAAAAAAAAAAAAAAAAAAA" + extension;
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != extension)
            {
                iCountErrors++;
                printerr("Error_2004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_3003";
            // No characters after the extension
            //INFO: This should return basically ".". But that really doesn't make sense. 
            iCountTestcases++;
            fileName = "foo.";
            dir = new DirectoryInfo(fileName);
            String test = Exten(fileName);
            if (dir.Extension != "")
            {
                iCountErrors++;
                printerr("Error_3004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_4003";
            // Symbol and special characters in extension	    
            iCountTestcases++;
            extension = ".$#@$_)+_)!@@!!@##&_$)#_";
            fileName = "foo" + extension;
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != extension)
            {
                iCountErrors++;
                printerr("Error_4004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_5003";
            // Lots of dots at end of the directory name  
            //INFO: This should return basically ".". But that really doesn't make sense.                               
            iCountTestcases++;
            extension = "..............";
            fileName = "foo" + extension;
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != "")
            {
                iCountErrors++;
                printerr("Error_5004! Incorrect extension , dir==" + dir.Extension);
            }

            strLoc = "Err_6003";
            // Extension with exactly one character. 
            iCountTestcases++;
            extension = "..............";
            fileName = "foo . z" + extension;
            dir = new DirectoryInfo(fileName);
            if (dir.Extension != ". z")
            {
                iCountErrors++;
                printerr("Error_6004! Incorrect extension , dir==" + dir.Extension);
            }

            if (File.Exists(fileName))
                File.Delete(fileName);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + iCountErrors.ToString());
        }

        Assert.Equal(0, iCountErrors);
    }

    private static string RemoveIfDotExists(String str)
    {
        while (str.IndexOf(".") > -1)
            str = str.Remove(str.IndexOf("."), 1);
        return str;
    }

    public static String Exten(String str)
    {
        int length = str.Length;

        for (int i = length; --i >= 0;)
        {
            char ch = str[i];
            Console.WriteLine(ch);
            if (ch == '.')
                return str.Substring(i, length - i);
            if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar)
                break;
        }
        return String.Empty;
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}
