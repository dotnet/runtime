// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace System.CodeDom.Compiler
{
    public abstract class CodeCompiler : CodeGenerator, ICodeCompiler
    {
        CompilerResults ICodeCompiler.CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit e)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                return FromDom(options, e);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        CompilerResults ICodeCompiler.CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                return FromFile(options, fileName);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        CompilerResults ICodeCompiler.CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                return FromSource(options, source);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        CompilerResults ICodeCompiler.CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                return FromSourceBatch(options, sources);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        CompilerResults ICodeCompiler.CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (fileNames == null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            try
            {
                // Try opening the files to make sure they exists.  This will throw an exception if it doesn't
                foreach (string fileName in fileNames)
                {
                    File.OpenRead(fileName).Dispose();
                }

                return FromFileBatch(options, fileNames);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        CompilerResults ICodeCompiler.CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] ea)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                return FromDomBatch(options, ea);
            }
            finally
            {
                options.TempFiles.SafeDelete();
            }
        }

        protected abstract string FileExtension { get; }

        protected abstract string CompilerName { get; }

        protected virtual CompilerResults FromDom(CompilerParameters options, CodeCompileUnit e)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return FromDomBatch(options, new CodeCompileUnit[1] { e });
        }

        protected virtual CompilerResults FromFile(CompilerParameters options, string fileName)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            // Try opening the file to make sure it exists.  This will throw an exception if it doesn't
            File.OpenRead(fileName).Dispose();

            return FromFileBatch(options, new string[1] { fileName });
        }

        protected virtual CompilerResults FromSource(CompilerParameters options, string source)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return FromSourceBatch(options, new string[1] { source });
        }

        protected virtual CompilerResults FromDomBatch(CompilerParameters options, CodeCompileUnit[] ea)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (ea == null)
            {
                throw new ArgumentNullException(nameof(ea));
            }

            var filenames = new string[ea.Length];

            for (int i = 0; i < ea.Length; i++)
            {
                if (ea[i] == null)
                {
                    continue; // the other two batch methods just work if one element is null, so we'll match that. 
                }

                ResolveReferencedAssemblies(options, ea[i]);
                filenames[i] = options.TempFiles.AddExtension(i + FileExtension);
                using (var fs = new FileStream(filenames[i], FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    ((ICodeGenerator)this).GenerateCodeFromCompileUnit(ea[i], sw, Options);
                    sw.Flush();
                }
            }

            return FromFileBatch(options, filenames);
        }

        private void ResolveReferencedAssemblies(CompilerParameters options, CodeCompileUnit e)
        {
            if (e.ReferencedAssemblies.Count > 0)
            {
                foreach (string assemblyName in e.ReferencedAssemblies)
                {
                    if (!options.ReferencedAssemblies.Contains(assemblyName))
                    {
                        options.ReferencedAssemblies.Add(assemblyName);
                    }
                }
            }
        }

        protected virtual CompilerResults FromFileBatch(CompilerParameters options, string[] fileNames)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (fileNames == null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            throw new PlatformNotSupportedException();
        }

        protected abstract void ProcessCompilerOutputLine(CompilerResults results, string line);

        protected abstract string CmdArgsFromParameters(CompilerParameters options);

        protected virtual string GetResponseFileCmdArgs(CompilerParameters options, string cmdArgs)
        {
            string responseFileName = options.TempFiles.AddExtension("cmdline");

            using (var fs = new FileStream(responseFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.Write(cmdArgs);
                sw.Flush();
            }

            return "@\"" + responseFileName + "\"";
        }

        protected virtual CompilerResults FromSourceBatch(CompilerParameters options, string[] sources)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var filenames = new string[sources.Length];

            CompilerResults results = null;
            for (int i = 0; i < sources.Length; i++)
            {
                string name = options.TempFiles.AddExtension(i + FileExtension);
                using (var fs = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(sources[i]);
                    sw.Flush();
                }
                filenames[i] = name;
            }
            return results = FromFileBatch(options, filenames);
        }

        protected static string JoinStringArray(string[] sa, string separator)
        {
            if (sa == null || sa.Length == 0)
            {
                return string.Empty;
            }

            if (sa.Length == 1)
            {
                return "\"" + sa[0] + "\"";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < sa.Length - 1; i++)
            {
                sb.Append('\"');
                sb.Append(sa[i]);
                sb.Append('\"');
                sb.Append(separator);
            }
            sb.Append('\"');
            sb.Append(sa[sa.Length - 1]);
            sb.Append('\"');

            return sb.ToString();
        }
    }
}
