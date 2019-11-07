// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace DPStressHarness
{
    /// <summary>
    /// Use SqlScript.Execute to run sql scripts prior to test execution.
    /// Recommended practice is to store the sql scripts as a resource in your dll (if it's complicated),
    /// or just as a string in your assembly.
    /// </summary>
    public static class SqlScript
    {
        public static void Execute(string script, string connectionString)
        {
            //script = Regex.Replace(script, @"/\*((?!\*/)(.|\n)*?)\*/", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            //Regex re = new Regex(@"(?<GO>^GO)|(?<COMMAND>((?!^GO)(.|\n))+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            //MatchCollection matches = re.Matches(script);

            //foreach (Match m in matches)
            //{
            //    Console.WriteLine(m.Groups["GO"]);
            //    Console.WriteLine(m.Groups["COMMAND"]);
            //}

            //Console.WriteLine();


            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            try
            {
                script = Regex.Replace(script, @"/\*((?!\*/)(.|\n)*?)\*/", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                Regex re = new Regex(@"(?<GO>^GO)|(?<COMMAND>((?!^GO)(.|\n))+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                MatchCollection matches = re.Matches(script);

                foreach (Match m in matches)
                {
                    string cmdText = m.Groups["COMMAND"].Value.Trim();

                    if (cmdText == string.Empty)
                        continue;

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.CommandTimeout = 300;
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                conn.Close();
            }
        }
    }
}