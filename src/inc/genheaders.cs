using System;
using System.Xml;
using System.Xml.Schema;
using System.IO;

public class GenerateHeaders {

    public static void Main(string[] args) {

        if (args.Length != 3) {
            Console.WriteLine("Usage:genheaders XML-file header-file resorce-file");
            return;
        }
    
        ValidateXML(args[0]);
        String Message=null;
        String SymbolicName=null;
        String NumericValue=null;
        String tempheaderfile = "temp.h";
        String temprcfile = "temp.rc";

        StreamWriter HSW=File.CreateText(tempheaderfile);
        StreamWriter RSW=File.CreateText(temprcfile);

	int FaciltyUrt=0x13;
	int SeveritySuccess=0;
	int SeverityError=1;

	int minSR = MakeHresult(SeveritySuccess,FaciltyUrt,0);		
        int maxSR = MakeHresult(SeveritySuccess,FaciltyUrt,0xffff);	
	int minHR = MakeHresult(SeverityError,FaciltyUrt,0);		
	int maxHR = MakeHresult(SeverityError,FaciltyUrt,0xffff);		


        PrintHeader(HSW);  
        PrintResourceHeader(RSW);
        
        XmlTextReader rdr = new XmlTextReader(args[0]);
        rdr.WhitespaceHandling = WhitespaceHandling.None;
        
        while (rdr.Read()) {
            
            switch (rdr.NodeType) {           
                
                case XmlNodeType.Element:

                    if (rdr.Name.ToString() == "HRESULT") {
                        NumericValue=rdr.GetAttribute("NumericValue"); 			
                    }
                    if (rdr.Name.ToString() == "Message") {
                        Message = rdr.ReadString();
                    }
                    if (rdr.Name.ToString() == "SymbolicName") {    
                        SymbolicName = rdr.ReadString();
                    }
                
                    break;

                case XmlNodeType.EndElement:
                    if(rdr.Name.ToString() == "HRESULT"){

			// For CLR Hresult's we take the last 4 digits as the resource strings.

			if ( (NumericValue.StartsWith("0x")) || (NumericValue.StartsWith("0X")) ) {

			    String HexResult = NumericValue.Substring(2);
			    int num = int.Parse(HexResult, System.Globalization.NumberStyles.HexNumber);			    	

			    if ((num>minSR) && (num <= maxSR)) {
				num = num & 0xffff;
				HSW.WriteLine("#define " + SymbolicName + " SMAKEHR(0x" + num.ToString("x") + ")");
			    } else if ((num>minHR) && (num <= maxHR)) {
				num = num & 0xffff;
			        HSW.WriteLine("#define " + SymbolicName + " EMAKEHR(0x" + num.ToString("x") + ")");
			    } else {
              		        HSW.WriteLine("#define " + SymbolicName + " " + NumericValue );		
                            }  	 	


			    	
			} else {
	                    HSW.WriteLine("#define " + SymbolicName + " " + NumericValue );
			}

                        if (Message != null) {   
                            RSW.Write("\tMSG_FOR_URT_HR(" + SymbolicName + ") ");
                            RSW.WriteLine(Message);
                        } 

                        SymbolicName = null;
                        NumericValue = null;
                        Message = null;
                    }
                    break;

            }
        }

        PrintFooter(HSW);
        PrintResourceFooter(RSW);

        HSW.Close();
        RSW.Close();

        bool AreFilesEqual = false;

        if (File.Exists(args[1])) {
            StreamReader sr1 = new StreamReader(tempheaderfile);        
            StreamReader sr2 = new StreamReader(args[1]);
            AreFilesEqual = CompareFiles(sr1, sr2);
            sr1.Close();
            sr2.Close();
        }

        if (!AreFilesEqual) {
            File.Copy(tempheaderfile, args[1], true);
            File.Copy(temprcfile, args[2], true);            
        }

        if (!File.Exists(args[2])) {
            File.Copy(temprcfile, args[2], true);            
        }

        File.Delete(tempheaderfile);            
        File.Delete(temprcfile);                      
    }

    private static void ValidateXML (String XMLFile) {

        // Set the validation settings on the XmlReaderSettings object.
        XmlReaderSettings settings = new XmlReaderSettings();

        settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;

        settings.ValidationEventHandler += new ValidationEventHandler (ValidationCallBack);
 
        // Create the XmlReader object.
        XmlReader reader = XmlReader.Create(XMLFile, settings);

        // Parse the file. 

        while (reader.Read()) {
        }
    }     

    // Display any validation errors.
    private static void ValidationCallBack(object sender, ValidationEventArgs e) {
    Console.WriteLine("Validation Error: {0}", e.Message);
    Environment.Exit(-1);
    }   


    private static void PrintHeader(StreamWriter SW) {

        SW.WriteLine("#ifndef __COMMON_LANGUAGE_RUNTIME_HRESULTS__");
        SW.WriteLine("#define __COMMON_LANGUAGE_RUNTIME_HRESULTS__");
        SW.WriteLine();
        SW.WriteLine("#include <winerror.h>");
        SW.WriteLine();
        SW.WriteLine();
        SW.WriteLine("//");
        SW.WriteLine("//This file is AutoGenerated -- Do Not Edit by hand!!!");
        SW.WriteLine("//");
        SW.WriteLine("//Add new HRESULTS along with their corresponding error messages to");
        SW.WriteLine("//corerror.xml");
        SW.WriteLine("//");
        SW.WriteLine();
        SW.WriteLine("#ifndef FACILITY_URT");    
        SW.WriteLine("#define FACILITY_URT            0x13");
        SW.WriteLine("#endif");
        SW.WriteLine("#ifndef EMAKEHR");
        SW.WriteLine("#define SMAKEHR(val) MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_URT, val)");
        SW.WriteLine("#define EMAKEHR(val) MAKE_HRESULT(SEVERITY_ERROR, FACILITY_URT, val)");
        SW.WriteLine("#endif");
        SW.WriteLine();
    }

    private static void PrintFooter(StreamWriter SW) {
        SW.WriteLine();
        SW.WriteLine();
        SW.WriteLine("#endif // __COMMON_LANGUAGE_RUNTIME_HRESULTS__");
    }

    private static void PrintResourceHeader(StreamWriter SW) {
        SW.WriteLine("STRINGTABLE DISCARDABLE");
        SW.WriteLine("BEGIN");
    }

    private static void PrintResourceFooter(StreamWriter SW) {
        SW.WriteLine("END");
    }

    private static bool CompareFiles(StreamReader sr1, StreamReader sr2) {
        String line1,line2;

        while (true) {
            line1 = sr1.ReadLine();
            line2 = sr2.ReadLine();

            if ( (line1 == null) && (line2 == null) ) {
                return true;
            }

            if (line1 != line2) {
                return false;
            }
          
        }   

    } 

   private static int MakeHresult(int sev, int fac, int code) {	
	 return ((sev<<31) | (fac<<16) | (code));
   }	
}






