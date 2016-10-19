// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;			//Encoding
using System.Diagnostics;	//TraceListener

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CLTMConsole
    //
    ////////////////////////////////////////////////////////////////
    public class CLTMConsole : TextWriter
    {
        //Data

        //Constructor
        public CLTMConsole()
        {
        }

        //Overloads - A subclass must minimally implement the Write(Char) method. 
        public override void Write(char ch)
        {
            CError.Write(ch.ToString());
        }

        //Overloads - We also implement "string" since its much more efficient and TextWriter will call this instead
        public override void Write(string strText)
        {
            CError.Write(strText);
        }

        //Overloads - We also implement "string" since its much more efficient and TextWriter will call this instead
        public override void Write(char[] ch)
        {
            //Note: This is a workaround the TextWriter::Write(char[]) that incorrectly 
            //writes 1 char at a time, which means \r\n is written separately and then gets fixed
            //up to be two carriage returns!
            if (ch != null)
            {
                Write(new string(ch));
            }
        }

        public override void WriteLine(string strText)
        {
            Write(strText + this.NewLine);
        }

        //Overloads
        //Writes a line terminator to the text stream. 
        //The default line terminator is a carriage return followed by a line feed ("\r\n"), 
        //but this value can be changed using the NewLine property.
        public override void WriteLine()
        {
            Write(this.NewLine);
        }

        //Overloads
        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }


    ////////////////////////////////////////////////////////////////
    // CLTMTraceListener
    //
    ////////////////////////////////////////////////////////////////
    public class CLTMTraceListener //: TraceListener
    {
        //Data

        //Constructor
        public CLTMTraceListener()
        {
        }
    }
}
