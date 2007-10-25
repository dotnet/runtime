//
// XApiService.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections;
using System.Xml.XPath;

namespace Mono.Linker {

	public class XApiService {

		static Hashtable infos = new Hashtable ();

		public static XPathDocument GetApiInfoFromFile (string file)
		{
			XPathDocument document = infos [file] as XPathDocument;
			if (document != null)
				return document;

			document = new XPathDocument (file);
			infos [GetAssemblyName (document)] = file;
			return document;
		}

		public static XPathDocument GetApiInfoByAssemblyName (string assembly_name)
		{
			if (!infos.Contains (assembly_name))
				return null;

			foreach (string name in infos.Keys)
				if (assembly_name == name)
					return new XPathDocument ((string) infos [name]);

			return null;
		}

		static string GetAssemblyName (XPathDocument document)
		{
			XPathNavigator nav = document.CreateNavigator ();
			for (XPathNodeIterator it = nav.Select ("assemblies//assembly"); it.MoveNext (); ) {
				return it.Current.GetAttribute ("name", string.Empty);
			}

			return null;
		}
	}
}
