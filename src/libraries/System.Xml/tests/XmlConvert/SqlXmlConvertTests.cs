// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase: XmlConvert 
    //
    ////////////////////////////////////////////////////////////////
    internal abstract class SqlXmlConvertTests : CTestCase
    {
        #region Static Fields

        public static string[] strEncode = { "-Var-", ".Var.", "_Var_", "Var var2", " Var ", "var#var3", "#var#", "&var&", "\"var\"", "<var<", ">var>", "1var2", ".var.", "-var-", "var-1", "var 2", "var_var2", "_var_3", "_Var_", "4Var5", "^Var^", "*Var*", "/Var/", "\\Var\\", "$Var$", "@var@", "(Var(", ")Var)", "(Var(3))", "+Var+", "{Var{", "}var}", "Var{3}", ";Var;", "?Var?", "%Var%", "Var,Var", ",Var,", "'Var'", "!Var!", "`Var`", "Var`Var", "~Var~", "|Var|", "=Var=", "Var=Var2", "Order_Details", "Order_x0020_", null, String.Empty, "1Var ()+,/=?;!*#@$%{}~`^&\"><|\\", "\uDE34\uD9A2" };

        public static string[] strEncodeLocal = { ":Var:", ":Var", "Var:Var2", "6:7" };

        public static string[] strExpEncode = { "_x002D_Var-", "_x002E_Var.", "_Var_", "Var_x0020_var2", "_x0020_Var_x0020_", "var_x0023_var3", "_x0023_var_x0023_", "_x0026_var_x0026_", "_x0022_var_x0022_", "_x003C_var_x003C_", "_x003E_var_x003E_", "_x0031_var2", "_x002E_var.", "_x002D_var-", "var-1", "var_x0020_2", "var_var2", "_var_3", "_Var_", "_x0034_Var5", "_x005E_Var_x005E_", "_x002A_Var_x002A_", "_x002F_Var_x002F_", "_x005C_Var_x005C_", "_x0024_Var_x0024_", "_x0040_var_x0040_", "_x0028_Var_x0028_", "_x0029_Var_x0029_", "_x0028_Var_x0028_3_x0029__x0029_", "_x002B_Var_x002B_", "_x007B_Var_x007B_", "_x007D_var_x007D_", "Var_x007B_3_x007D_", "_x003B_Var_x003B_", "_x003F_Var_x003F_", "_x0025_Var_x0025_", "Var_x002C_Var", "_x002C_Var_x002C_", "_x0027_Var_x0027_", "_x0021_Var_x0021_", "_x0060_Var_x0060_", "Var_x0060_Var", "_x007E_Var_x007E_", "_x007C_Var_x007C_", "_x003D_Var_x003D_", "Var_x003D_Var2", "Order_Details", "Order_x005F_x0020_", null, String.Empty, "_x0031_Var_x0020__x0028__x0029__x002B__x002C__x002F__x003D__x003F__x003B__x0021__x002A__x0023__x0040__x0024__x0025__x007B__x007D__x007E__x0060__x005E__x0026__x0022__x003E__x003C__x007C__x005C_", "_xDE34__xD9A2_" };

        public static string[] strExpEncodeLocal = { "_x003A_Var_x003A_", "_x003A_Var", "Var_x003A_Var2", "6_x003A_7" };

        public static string[] strExpEncodeNmToken = { "-Var-", ".Var.", "_Var_", "Var_x0020_var2", "_x0020_Var_x0020_", "var_x0023_var3", "_x0023_var_x0023_", "_x0026_var_x0026_", "_x0022_var_x0022_", "_x003C_var_x003C_", "_x003E_var_x003E_", "1var2", ".var.", "-var-", "var-1", "var_x0020_2", "var_var2", "_var_3", "_Var_", "4Var5", "_x005E_Var_x005E_", "_x002A_Var_x002A_", "_x002F_Var_x002F_", "_x005C_Var_x005C_", "_x0024_Var_x0024_", "_x0040_var_x0040_", "_x0028_Var_x0028_", "_x0029_Var_x0029_", "_x0028_Var_x0028_3_x0029__x0029_", "_x002B_Var_x002B_", "_x007B_Var_x007B_", "_x007D_var_x007D_", "Var_x007B_3_x007D_", "_x003B_Var_x003B_", "_x003F_Var_x003F_", "_x0025_Var_x0025_", "Var_x002C_Var", "_x002C_Var_x002C_", "_x0027_Var_x0027_", "_x0021_Var_x0021_", "_x0060_Var_x0060_", "Var_x0060_Var", "_x007E_Var_x007E_", "_x007C_Var_x007C_", "_x003D_Var_x003D_", "Var_x003D_Var2", "Order_Details", "Order_x005F_x0020_", null, String.Empty, "1Var_x0020__x0028__x0029__x002B__x002C__x002F__x003D__x003F__x003B__x0021__x002A__x0023__x0040__x0024__x0025__x007B__x007D__x007E__x0060__x005E__x0026__x0022__x003E__x003C__x007C__x005C_", "_xDE34__xD9A2_" };
        #endregion

        // Common variations

        //[TestCase(Name="1. XmlConvert (SQL-XML EncodeName) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (SQL-XML EncodeName) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (SQL-XML EncodeName) EncodeName-DecodeName", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (SQL-XML EncodeName) EncodeLocalName only", Desc="XmlConvert")]

        //[TestCase(Name="EncodeName/DecodeName", Desc="XmlConvert")]

        //[TestCase(Name="1. XmlConvert (Boundary Base Char) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (Boundary Base Char) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (Boundary Base Char) EncodeName-DecodeName ", Desc="XmlConvert")]

        //[TestCase(Name="1. XmlConvert (Boundary Ideographic Char) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert  (Boundary Ideographic Char) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (Boundary Ideographic Char) EncodeName-DecodeName", Desc="XmlConvert")]

        //[TestCase(Name="1. XmlConvert (Boundary Combining Char) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (Boundary Combining Char) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (Boundary Combining Char) EncodeName-DecodeName", Desc="XmlConvert")]

        //[TestCase(Name="1. XmlConvert (Boundary Digit Char) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (Boundary Digit Char) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (Boundary Digit Char) EncodeName-DecodeName", Desc="XmlConvert")]

        //[TestCase(Name="1. XmlConvert (EmbeddedNull Char) EncodeName-EncodeLocalName", Desc="XmlConvert")]

        //[TestCase(Name="2. XmlConvert (EmbeddedNull Char) EncodeNmToken-EncodeLocalNmToken", Desc="XmlConvert")]

        //[TestCase(Name="3. XmlConvert (EmbeddedNull Char) EncodeName-DecodeName", Desc="XmlConvert")]
    }
}
