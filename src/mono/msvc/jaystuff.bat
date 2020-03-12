.\mono\mcs\jay\jay.exe -cv < .\mono\mcs\jay\skeleton.cs .\mono\mcs\mcs\cs-parser.jay > .\mono\mcs\mcs\cs-parser.cs

.\mono\mcs\jay\jay.exe -ct < .\mono\mcs\jay\skeleton.cs .\mono\mcs\class\System.XML\System.Xml.XPath\Parser.jay > .\mono\mcs\class\System.XML\System.Xml.XPath\Parser.cs

.\mono\mcs\jay\jay.exe -ct < .\mono\mcs\jay\skeleton.cs .\mono\mcs\class\System.XML\Mono.Xml.Xsl\PatternParser.jay > .\mono\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.cs

echo #define XSLT_PATTERN > .\mono\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt

type .\mono\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt .\mono\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.cs > .\mono\mcs\class\System.XML\Mono.Xml.Xsl\PatternParser.cs

type .\mono\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt .\mono\mcs\class\System.XML\System.Xml.XPath\Tokenizer.cs > .\mono\mcs\class\System.XML\Mono.Xml.Xsl\PatternTokenizer.cs

.\mono\mcs\jay\jay.exe -ct < .\mono\mcs\jay\skeleton.cs .\mono\mcs\class\System.Data\Mono.Data.SqlExpressions\Parser.jay > .\mono\mcs\class\System.Data\Mono.Data.SqlExpressions\Parser.cs

.\mono\mcs\jay\jay.exe -ct < .\mono\mcs\jay\skeleton.cs .\mono\mcs\class\Commons.Xml.Relaxng\Commons.Xml.Relaxng.Rnc\RncParser.jay > .\mono\mcs\class\Commons.Xml.Relaxng\Commons.Xml.Relaxng.Rnc\RncParser.cs

.\mono\mcs\jay\jay.exe -ct < .\mono\mcs\jay\skeleton.cs .\mono\mcs\ilasm\parser\ILParser.jay > .\mono\mcs\ilasm\ILParser.cs

