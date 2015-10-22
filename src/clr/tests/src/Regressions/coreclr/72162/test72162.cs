using System;
using System.Globalization;
public class Test72162
{
    // Bug 72162 dealt with the number of significant digits being incorrectly done on the Mac
    public static int Main()
    {
        int iRetVal = 100;
        Double[] dblTestValues = { -1000.54,
                                   -100.999,
                                   -10.999,
                                   10.999,
                                   100.999,
                                   1000.999,
                                 };
        String[] strExpectedValues = { "-1001",
                                   "-101",
                                   "-11",
                                   "11",
                                   "101",
                                   "1001"
                                 };

        for (int i = 0; i < dblTestValues.Length; i++)
        {
            String strOut = dblTestValues[i].ToString("G4");
            if (!strOut.Equals(strExpectedValues[i]))
            {
                TestLibrary.Logging.WriteLine("Error: Formatting number '"+dblTestValues[i].ToString()+"', with G4 formatting should generate '"+strExpectedValues[i]+"' but instead generated '"+strOut+"'");
                iRetVal = 0;
            }
        }        
     
        return iRetVal;
    }

}
