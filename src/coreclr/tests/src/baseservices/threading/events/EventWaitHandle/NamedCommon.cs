using System;
using System.Threading;

class Common
{
    public static string GenerateUnicodeString(int iNumChars)
    {
        Random rand = new Random();
        string semName = string.Empty;
        string semNameNum = string.Empty;
        for (int i = 0; i < iNumChars; i++)
        {
            char c = '\\';
	     while (c == '\\')
	     {
	     	c = (char)rand.Next(Char.MinValue, Char.MaxValue);
	     }
            semNameNum += ((int)c).ToString() + ";";
            semName += c;
        }
        // write to output
        Console.WriteLine("Unicode string: " + semNameNum);
        return semName;
    }

    public static string GetUniqueName()
    {
        string sName = Guid.NewGuid().ToString();
        Console.WriteLine("Name created: " + sName);
        return sName;
    }
}