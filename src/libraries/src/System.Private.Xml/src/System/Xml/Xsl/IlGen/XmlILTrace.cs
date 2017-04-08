// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security;
using System.Xml;
using System.Globalization;
using System.Xml.Xsl.Qil;
using System.Runtime.Versioning;

// This class is only for debug purposes so there is no need to have it in Retail builds
#if DEBUG
namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// Helper class that facilitates tracing of ILGen.
    /// </summary>
    internal static class XmlILTrace
    {
        private const int MAX_REWRITES = 200;

        /// <summary>
        /// Check environment variable in order to determine whether to write out trace files.  This really should be a
        /// check of the configuration file, but System.Xml does not yet have a good tracing story.
        /// </summary>
        private static volatile string s_dirName = null;
        private static volatile bool s_alreadyCheckedEnabled = false;

        /// <summary>
        /// True if tracing has been enabled (environment variable set).
        /// </summary>
        public static bool IsEnabled
        {
            // SxS: This property poses potential SxS issue. However the class is used only in debug builds (it won't 
            // get compiled into ret build) so it's OK to suppress the SxS warning.
            get
            {
                // If environment variable has not yet been checked, do so now
                if (!s_alreadyCheckedEnabled)
                {
                    try
                    {
                        s_dirName = Environment.GetEnvironmentVariable("XmlILTrace");
                    }
                    catch (SecurityException)
                    {
                        // If user does not have access to environment variables, tracing will remain disabled
                    }

                    s_alreadyCheckedEnabled = true;
                }

                return (s_dirName != null);
            }
        }

        /// <summary>
        /// If tracing is enabled, this method will delete the contents of "filename" in preparation for append
        /// operations.
        /// </summary>
        public static void PrepareTraceWriter(string fileName)
        {
            if (!IsEnabled)
                return;

            File.Delete(s_dirName + "\\" + fileName);
        }

        /// <summary>
        /// If tracing is enabled, this method will open a TextWriter over "fileName" and return it.  Otherwise,
        /// null will be returned.
        /// </summary>
        public static TextWriter GetTraceWriter(string fileName)
        {
            if (!IsEnabled)
                return null;

            return new StreamWriter(s_dirName + "\\" + fileName, true);
        }

        /// <summary>
        /// Serialize Qil tree to "fileName", in the directory identified by "dirName".
        /// </summary>
        public static void WriteQil(QilExpression qil, string fileName)
        {
            if (!IsEnabled)
                return;

            XmlWriter w = XmlWriter.Create(s_dirName + "\\" + fileName);
            try
            {
                WriteQil(qil, w);
            }
            finally
            {
                w.Close();
            }
        }

        /// <summary>
        /// Trace ILGen optimizations and log them to "fileName".
        /// </summary>
        public static void TraceOptimizations(QilExpression qil, string fileName)
        {
            if (!IsEnabled)
                return;

            XmlWriter w = XmlWriter.Create(s_dirName + "\\" + fileName);

            w.WriteStartDocument();
            w.WriteProcessingInstruction("xml-stylesheet", "href='qilo.xslt' type='text/xsl'");
            w.WriteStartElement("QilOptimizer");
            w.WriteAttributeString("timestamp", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            WriteQilRewrite(qil, w, null);

            try
            {
                // Then, rewrite the graph until "done" or some max value is reached.
                for (int i = 1; i < MAX_REWRITES; i++)
                {
                    QilExpression qilTemp = (QilExpression)(new QilCloneVisitor(qil.Factory).Clone(qil));

                    XmlILOptimizerVisitor visitor = new XmlILOptimizerVisitor(qilTemp, !qilTemp.IsDebug);
                    visitor.Threshold = i;
                    qilTemp = visitor.Optimize();

                    // In debug code, ensure that QIL after N steps is correct
                    QilValidationVisitor.Validate(qilTemp);

                    // Trace the rewrite
                    WriteQilRewrite(qilTemp, w, OptimizationToString(visitor.LastReplacement));

                    if (visitor.ReplacementCount < i)
                        break;
                }
            }
            catch (Exception e)
            {
                if (!XmlException.IsCatchableException(e))
                {
                    throw;
                }
                w.WriteElementString("Exception", null, e.ToString());
                throw;
            }
            finally
            {
                w.WriteEndElement();
                w.WriteEndDocument();
                w.Flush();
                w.Close();
            }
        }

        /// <summary>
        /// Serialize Qil tree to writer "w".
        /// </summary>
        private static void WriteQil(QilExpression qil, XmlWriter w)
        {
            QilXmlWriter qw = new QilXmlWriter(w);
            qw.ToXml(qil);
        }

        /// <summary>
        /// Serialize rewritten Qil tree to writer "w".
        /// </summary>
        private static void WriteQilRewrite(QilExpression qil, XmlWriter w, string rewriteName)
        {
            w.WriteStartElement("Diff");
            if (rewriteName != null)
                w.WriteAttributeString("rewrite", rewriteName);
            WriteQil(qil, w);
            w.WriteEndElement();
        }

        /// <summary>
        /// Get friendly string description of an ILGen optimization.
        /// </summary>
        private static string OptimizationToString(int opt)
        {
            string s = Enum.GetName(typeof(XmlILOptimization), opt);
            if (s.StartsWith("Introduce", StringComparison.Ordinal))
            {
                return s.Substring(9) + " introduction";
            }
            else if (s.StartsWith("Eliminate", StringComparison.Ordinal))
            {
                return s.Substring(9) + " elimination";
            }
            else if (s.StartsWith("Commute", StringComparison.Ordinal))
            {
                return s.Substring(7) + " commutation";
            }
            else if (s.StartsWith("Fold", StringComparison.Ordinal))
            {
                return s.Substring(4) + " folding";
            }
            else if (s.StartsWith("Misc", StringComparison.Ordinal))
            {
                return s.Substring(4);
            }
            return s;
        }
    }
}
#endif
