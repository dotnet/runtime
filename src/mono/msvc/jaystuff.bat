.\mcs\jay\jay.exe -cv < .\mcs\jay\skeleton.cs .\mcs\mcs\cs-parser.jay > .\mcs\mcs\cs-parser.cs

.\mcs\jay\jay.exe -ct < .\mcs\jay\skeleton.cs .\mcs\class\System.XML\System.Xml.XPath\Parser.jay > .\mcs\class\System.XML\System.Xml.XPath\Parser.cs

.\mcs\jay\jay.exe -ct < .\mcs\jay\skeleton.cs .\mcs\class\System.XML\Mono.Xml.Xsl\PatternParser.jay > .\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.cs

echo #define XSLT_PATTERN > .\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt

type .\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt .\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.cs > .\mcs\class\System.XML\Mono.Xml.Xsl\PatternParser.cs

type .\mcs\class\System.XML\Mono.Xml.Xsl\Pattern.txt .\mcs\class\System.XML\System.Xml.XPath\Tokenizer.cs > .\mcs\class\System.XML\Mono.Xml.Xsl\PatternTokenizer.cs

.\mcs\jay\jay.exe -ct < .\mcs\jay\skeleton.cs .\mcs\class\System.Data\Mono.Data.SqlExpressions\Parser.jay > .\mcs\class\System.Data\Mono.Data.SqlExpressions\Parser.cs

.\mcs\jay\jay.exe -ct < .\mcs\jay\skeleton.cs .\mcs\class\Commons.Xml.Relaxng\Commons.Xml.Relaxng.Rnc\RncParser.jay > .\mcs\class\Commons.Xml.Relaxng\Commons.Xml.Relaxng.Rnc\RncParser.cs

.\mcs\jay\jay.exe -ct < .\mcs\jay\skeleton.cs .\mcs\ilasm\parser\ILParser.jay > .\mcs\ilasm\ILParser.cs

