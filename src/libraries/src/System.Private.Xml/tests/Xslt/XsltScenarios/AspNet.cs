// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Net;

namespace XsltScenarios
{
    [TestCase(id = 1, Name = "AspNet using XslTransform", Desc = "Testing XslTransform functionality under ASP.NET Environment", Pri = 2, Param = "http://dpxmldtap1/security/xmlscenarios/xslt/")]
    [TestCase(id = 2, Name = "AspNet using XslCompiledTransform", Desc = "Testing XslCompiledTransform functionality under ASP.NET Environment", Pri = 2, Param = "http://dpxmldtap1/security/xmlscenarios/xslt/")]
    internal class AspNet : CTestCase
    {
        public AspNet()
        {
        }

        private string ExecuteUrl(string url)
        {
            //When id is 1, ver parameter is sent as v1 and in the ASP.NET Environment XslTransform is used
            //When id is 2, ver parameter is sent as v2 and in the ASP.NET Environment XslCompiledTransform is used
            if (id == 1)
                url = Param.ToString() + url + "&ver=v1";
            else if (id == 2)
                url = Param.ToString() + url + "&ver=v2";

            //Special if the url doesn't have any parameters other than ver then use ? instead of &
            if (url.IndexOf("?") == -1)
                url = url.Replace("&", "?");

            // Create a request for the URL.
            CError.WriteLine("Executing the URL " + url);
            WebRequest request = WebRequest.Create(url);
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Display the status.
            // Console.WriteLine(response.StatusDescription);
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        [Variation(id = 1, Desc = "Basic ASP.NET Test, send params and pass them to Transform", Pri = 0, Param = "test1.aspx?firstname=foo&lastname=bar&age=20&dob=2/2/1985")]
        public int Test1()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result><firstname>foo</firstname><lastname>bar</lastname><age>20</age><dob>2/2/1985</dob></result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 2, Desc = "Pass an extension object to Transform in ASP.NET", Pri = 0, Param = "test2.aspx")]
        public int Test2()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result>10</result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 3, Desc = "Store compiled XSLT in Session state and use it from Session", Pri = 1, Param = "test3.aspx")]
        public int Test3()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result>Xslt in Session</result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 4, Desc = "Use the Xslt stored in Session state in the previous test", Pri = 2, Param = "test4.aspx")]
        public int Test4()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result>Nothing is stored in Session</result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 5, Desc = "Store the XSLT Object in Application state", Pri = 1, Param = "test5.aspx")]
        public int Test5()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result>Xslt in Application</result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 6, Desc = "Use the Application XSLT Object stored in previous test", Pri = 2, Param = "test6.aspx")]
        public int Test6()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result>Xslt in Application</result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 7, Desc = "Store the ArgumentList in the Application state", Pri = 2, Param = "test7.aspx?firstname=foo&lastname=bar&age=20&dob=2/2/1985")]
        public int Test7()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result><firstname>foo</firstname><lastname>bar</lastname><age>20</age><dob>2/2/1985</dob></result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 8, Desc = "Use the Application XSLT and ArgList Objects stored in previous test", Pri = 2, Param = "test8.aspx")]
        public int Test8()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result><firstname>foo</firstname><lastname>bar</lastname><age>20</age><dob>2/2/1985</dob></result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 9, Desc = "Add more parameters to the existing Argumentlist in Application state", Pri = 2, Param = "test9.aspx?gender=male")]
        public int Test9()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result><firstname>foo</firstname><lastname>bar</lastname><age>20</age><dob>2/2/1985</dob><gender>male</gender><state>WA</state></result>";

            return Utils.VerifyTest(actResult, expResult);
        }

        [Variation(id = 10, Desc = "Transform output to TextWriter(Response.Output)", Pri = 2, Param = "test10.aspx")]
        [Variation(id = 11, Desc = "Transform output to Stream(Response.OutputStream)", Pri = 2, Param = "test11.aspx")]
        public int Test10()
        {
            string url = CurVariation.Param.ToString();
            string actResult = ExecuteUrl(url);
            string expResult = "<result><firstname>foo</firstname><lastname>bar</lastname><age>20</age><dob>2/2/1985</dob></result>";

            return Utils.VerifyTest(actResult, expResult);
        }
    }
}