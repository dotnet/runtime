// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer.Tests
{
	sealed class XmlText : AdditionalText
	{
		public override string Path { get; }

		readonly Stream Doc;
		public XmlText (string path, Stream data)
		{
			Path = path;
			Doc = data;
		}

		public override SourceText? GetText (CancellationToken token = default)
		{
			return SourceText.From (Doc);
		}
	}
}
