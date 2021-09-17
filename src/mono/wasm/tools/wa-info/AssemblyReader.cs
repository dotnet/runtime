using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace WebAssemblyInfo
{
    internal class AssemblyReader
    {
        BinaryReader binaryReader;
        PEReader peReader;
        MetadataReader reader;

        public AssemblyReader(string path)
        {
            var fileStream = File.OpenRead(path);
            binaryReader = new BinaryReader(fileStream);
            peReader = new PEReader(binaryReader.BaseStream);
            reader = peReader.GetMetadataReader();
        }

        public List<MethodDefinition> GetAllMethods()
        {
            List<MethodDefinition> methods = new();
            foreach (var type in reader.TypeDefinitions)
            {
                var td = reader.GetTypeDefinition(type);
                var typeName = GetTypeFullname(reader, td);
                //Console.WriteLine($" type {typeName}");

                foreach (var mh in td.GetMethods())
                {
                    var md = reader.GetMethodDefinition(mh);
                    //Console.WriteLine($"   method {GetMethodString(reader, td, md)}");
                    methods.Add(md);
                }
            }

            return methods;
        }

        string GetTypeFullname(MetadataReader reader, TypeDefinition td)
        {
            StringBuilder sb = new StringBuilder();
            var ns = reader.GetString(td.Namespace);
            if (ns.Length > 0)
            {
                sb.Append(ns);
                sb.Append(".");
            }

            sb.Append(reader.GetString(td.Name));

            var gps = td.GetGenericParameters();
            if (gps.Count > 0)
            {
                sb.Append('<');

                for (int i = 0; i < 1; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var gp = reader.GetGenericParameter(gps[i]);
                    sb.Append(reader.GetString(gp.Name));
                }

                sb.Append('>');
            }

            return sb.ToString();
        }

        bool warnedAboutSigErr = false;

        string GetMethodString(MetadataReader reader, TypeDefinition td, MethodDefinition md)
        {
            StringBuilder sb = new StringBuilder();

            if ((md.Attributes & System.Reflection.MethodAttributes.Public) == System.Reflection.MethodAttributes.Public)
                sb.Append("public ");
            if ((md.Attributes & System.Reflection.MethodAttributes.Static) == System.Reflection.MethodAttributes.Static)
                sb.Append("static ");

            var context = new GenericContext(md.GetGenericParameters(), td.GetGenericParameters(), reader);

            MethodSignature<string> signature = new MethodSignature<string>();
            bool sigErr = false;
            try
            {
                signature = md.DecodeSignature<string, GenericContext>(new SignatureDecoder(), context);
            }
            catch (BadImageFormatException)
            {
                sigErr = true;
                if (!warnedAboutSigErr)
                {
                    Console.WriteLine("Exception in signature decoder. Some differences might be missing.");
                    warnedAboutSigErr = true;
                }
            }

            sb.Append(sigErr ? "SIGERR" : signature.ReturnType);
            sb.Append(' ');
            sb.Append(reader.GetString(md.Name));

            sb.Append(" (");
            var first = true;

            if (sigErr)
            {
                sb.Append("SIGERR");
            }
            else
            {
                foreach (var p in signature.ParameterTypes)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append(", ");

                    sb.Append(p);
                }
            }

            sb.Append(')');

            return sb.ToString();
        }
    }
}
