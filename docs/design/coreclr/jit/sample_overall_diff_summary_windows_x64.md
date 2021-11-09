# ASM diffs generated on windows x64

## Linux arm64

<details>

<summary>Linux arm64 details</summary>

Summary file: `superpmi_diff_summary_Linux_arm64.md`

To reproduce these diffs on windows x64:
```
superpmi.py asmdiffs -target_os Linux -target_arch arm64 -arch x64
```

## coreclr_tests.pmi.Linux.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 165241292 (overridden on cmd)
Total bytes of diff: 165175900 (overridden on cmd)
Total bytes of delta: -65392 (-0.04 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          48 : 235822.dasm (10.62 % of base)
          16 : 244146.dasm (4.17 % of base)
          12 : 211757.dasm (4.69 % of base)
          12 : 195484.dasm (1.69 % of base)
          12 : 195584.dasm (4.17 % of base)
           8 : 247924.dasm (0.92 % of base)
           8 : 238249.dasm (0.85 % of base)
           4 : 181331.dasm (0.01 % of base)
           4 : 211759.dasm (1.35 % of base)
           4 : 241762.dasm (0.13 % of base)
           4 : 243728.dasm (0.13 % of base)

Top file improvements (bytes):
       -3060 : 250410.dasm (-68.73 % of base)
       -2520 : 249425.dasm (-52.20 % of base)
       -2404 : 207855.dasm (-69.40 % of base)
       -1692 : 253377.dasm (-64.09 % of base)
       -1484 : 207786.dasm (-58.24 % of base)
       -1476 : 207776.dasm (-60.59 % of base)
       -1064 : 252987.dasm (-58.98 % of base)
        -948 : 219431.dasm (-26.57 % of base)
        -948 : 222136.dasm (-26.33 % of base)
        -944 : 207756.dasm (-39.07 % of base)
        -852 : 250717.dasm (-45.13 % of base)
        -852 : 250485.dasm (-45.13 % of base)
        -848 : 253287.dasm (-47.86 % of base)
        -664 : 207853.dasm (-57.84 % of base)
        -632 : 207854.dasm (-45.66 % of base)
        -628 : 252307.dasm (-51.48 % of base)
        -560 : 248556.dasm (-49.47 % of base)
        -544 : 247293.dasm (-21.28 % of base)
        -536 : 245551.dasm (-21.41 % of base)
        -536 : 246455.dasm (-21.41 % of base)

328 total files with Code Size differences (317 improved, 11 regressed), 24 unchanged.

Top method regressions (bytes):
          48 (10.62 % of base) : 235822.dasm - Dynamo.Dynamo:Compare():bool:this
          16 (4.17 % of base) : 244146.dasm - RuntimeEventListener:Verify():bool:this
          12 (1.69 % of base) : 195484.dasm - Internal.TypeSystem.MetadataRuntimeInterfacesAlgorithm:ComputeRuntimeInterfacesForNonInstantiatedMetadataType(Internal.TypeSystem.MetadataType):Internal.TypeSystem.DefType[]:this
          12 (4.17 % of base) : 195584.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          12 (4.69 % of base) : 211757.dasm - TestLibrary.Utilities:ByteArrayToString(System.Byte[]):System.String
           8 (0.92 % of base) : 247924.dasm - BenchmarksGame.NBodySystem:.ctor():this
           8 (0.85 % of base) : 238249.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
           4 (0.13 % of base) : 241762.dasm - intmm:Main():int
           4 (0.13 % of base) : 243728.dasm - intmm:Main():int
           4 (0.01 % of base) : 181331.dasm - Program:Main(System.String[]):int
           4 (1.35 % of base) : 211759.dasm - TestLibrary.Utilities:FormatHexStringFromUnicodeString(System.String,bool):System.String

Top method improvements (bytes):
       -3060 (-68.73 % of base) : 250410.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -2520 (-52.20 % of base) : 249425.dasm - jaggedarr:gaussj(System.Double[,][],int,System.Double[,][],int)
       -2404 (-69.40 % of base) : 207855.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1692 (-64.09 % of base) : 253377.dasm - Complex_Array_Test:Main(System.String[]):int
       -1484 (-58.24 % of base) : 207786.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
       -1476 (-60.59 % of base) : 207776.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1064 (-58.98 % of base) : 252987.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -948 (-26.57 % of base) : 219431.dasm - VectorTest:Main():int
        -948 (-26.33 % of base) : 222136.dasm - VectorTest:Main():int
        -944 (-39.07 % of base) : 207756.dasm - Huffman:DoHuffIteration(System.Byte[],System.Byte[],System.Byte[],int,int,huff_node[]):long
        -852 (-45.13 % of base) : 250717.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -852 (-45.13 % of base) : 250485.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -848 (-47.86 % of base) : 253287.dasm - Simple_Array_Test:Main(System.String[]):int
        -664 (-57.84 % of base) : 207853.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -632 (-45.66 % of base) : 207854.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])
        -628 (-51.48 % of base) : 252307.dasm - CTest:TestArrays1(int,double)
        -560 (-49.47 % of base) : 248556.dasm - SimpleArray_01.Test:BadMatrixMul2()
        -544 (-21.28 % of base) : 247293.dasm - classarr:gaussj(MatrixCls,int,MatrixCls,int)
        -536 (-21.41 % of base) : 245551.dasm - plainarr:gaussj(System.Double[,],int,System.Double[,],int)
        -536 (-43.79 % of base) : 232296.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int

Top method regressions (percentages):
          48 (10.62 % of base) : 235822.dasm - Dynamo.Dynamo:Compare():bool:this
          12 (4.69 % of base) : 211757.dasm - TestLibrary.Utilities:ByteArrayToString(System.Byte[]):System.String
          12 (4.17 % of base) : 195584.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          16 (4.17 % of base) : 244146.dasm - RuntimeEventListener:Verify():bool:this
          12 (1.69 % of base) : 195484.dasm - Internal.TypeSystem.MetadataRuntimeInterfacesAlgorithm:ComputeRuntimeInterfacesForNonInstantiatedMetadataType(Internal.TypeSystem.MetadataType):Internal.TypeSystem.DefType[]:this
           4 (1.35 % of base) : 211759.dasm - TestLibrary.Utilities:FormatHexStringFromUnicodeString(System.String,bool):System.String
           8 (0.92 % of base) : 247924.dasm - BenchmarksGame.NBodySystem:.ctor():this
           8 (0.85 % of base) : 238249.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
           4 (0.13 % of base) : 241762.dasm - intmm:Main():int
           4 (0.13 % of base) : 243728.dasm - intmm:Main():int
           4 (0.01 % of base) : 181331.dasm - Program:Main(System.String[]):int

Top method improvements (percentages):
       -2404 (-69.40 % of base) : 207855.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -520 (-69.15 % of base) : 251803.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -3060 (-68.73 % of base) : 250410.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -1692 (-64.09 % of base) : 253377.dasm - Complex_Array_Test:Main(System.String[]):int
        -328 (-63.57 % of base) : 252849.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
        -276 (-61.61 % of base) : 253037.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
        -504 (-61.17 % of base) : 249946.dasm - Benchstone.BenchF.InProd:Bench():bool
        -348 (-60.84 % of base) : 253038.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
       -1476 (-60.59 % of base) : 207776.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -276 (-59.48 % of base) : 250451.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -276 (-59.48 % of base) : 250450.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
       -1064 (-58.98 % of base) : 252987.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -176 (-58.67 % of base) : 252531.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -280 (-58.33 % of base) : 252292.dasm - Benchstone.BenchI.AddArray2:BenchInner1(System.Int32[][],byref)
       -1484 (-58.24 % of base) : 207786.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -664 (-57.84 % of base) : 207853.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -532 (-56.12 % of base) : 250718.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 250486.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 250719.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 250487.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this

328 total methods with Code Size differences (317 improved, 11 regressed), 24 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.crossgen2.Linux.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 48372880 (overridden on cmd)
Total bytes of diff: 48236216 (overridden on cmd)
Total bytes of delta: -136664 (-0.28 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          96 : 146443.dasm (2.33 % of base)
          88 : 1660.dasm (3.17 % of base)
          60 : 172973.dasm (1.28 % of base)
          40 : 178412.dasm (3.05 % of base)
          40 : 16721.dasm (4.42 % of base)
          36 : 12176.dasm (9.00 % of base)
          36 : 157383.dasm (0.89 % of base)
          28 : 1359.dasm (0.42 % of base)
          24 : 41093.dasm (0.12 % of base)
          20 : 13613.dasm (3.57 % of base)
          20 : 179464.dasm (4.24 % of base)
          20 : 33447.dasm (1.69 % of base)
          20 : 77592.dasm (1.87 % of base)
          20 : 78533.dasm (2.07 % of base)
          20 : 89800.dasm (7.04 % of base)
          20 : 89889.dasm (6.85 % of base)
          20 : 21601.dasm (2.49 % of base)
          16 : 168121.dasm (6.45 % of base)
          16 : 178775.dasm (13.79 % of base)
          16 : 70967.dasm (6.45 % of base)

Top file improvements (bytes):
      -12260 : 82412.dasm (-44.63 % of base)
       -5404 : 17038.dasm (-42.74 % of base)
       -2384 : 82399.dasm (-52.74 % of base)
       -1856 : 82410.dasm (-36.54 % of base)
       -1652 : 82494.dasm (-45.94 % of base)
       -1532 : 82497.dasm (-45.11 % of base)
       -1060 : 196484.dasm (-35.81 % of base)
       -1060 : 82406.dasm (-46.99 % of base)
        -824 : 121011.dasm (-38.50 % of base)
        -812 : 82792.dasm (-24.82 % of base)
        -804 : 196483.dasm (-29.30 % of base)
        -748 : 82404.dasm (-43.90 % of base)
        -736 : 36174.dasm (-35.25 % of base)
        -732 : 140062.dasm (-42.46 % of base)
        -724 : 97541.dasm (-31.42 % of base)
        -724 : 93525.dasm (-45.59 % of base)
        -712 : 173222.dasm (-34.56 % of base)
        -704 : 17035.dasm (-27.80 % of base)
        -692 : 17036.dasm (-30.35 % of base)
        -692 : 131380.dasm (-39.41 % of base)

853 total files with Code Size differences (777 improved, 76 regressed), 144 unchanged.

Top method regressions (bytes):
          96 (2.33 % of base) : 146443.dasm - Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.DirectiveParser:ParsePragmaDirective(Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken,Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken,bool):Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.DirectiveTriviaSyntax:this
          88 (3.17 % of base) : 1660.dasm - System.Diagnostics.Tracing.EventPipeMetadataGenerator:GenerateMetadata(int,System.String,long,int,int,int,System.Diagnostics.Tracing.EventParameterInfo[]):System.Byte[]:this
          60 (1.28 % of base) : 172973.dasm - System.DirectoryServices.Protocols.LdapConnection:SendRequestHelper(System.DirectoryServices.Protocols.DirectoryRequest,byref):int:this
          40 (3.05 % of base) : 178412.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          40 (4.42 % of base) : 16721.dasm - System.DateTimeParse:MatchEraName(byref,System.Globalization.DateTimeFormatInfo,byref):bool
          36 (0.89 % of base) : 157383.dasm - CriticalHelper:WriteCollection(System.Runtime.Serialization.CollectionDataContract):this
          36 (9.00 % of base) : 12176.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          28 (0.42 % of base) : 1359.dasm - System.Diagnostics.Tracing.ManifestBuilder:CreateManifestString():System.String:this
          24 (0.12 % of base) : 41093.dasm - Microsoft.Diagnostics.Tracing.Parsers.AspNet.AspNetTraceEventParser:EnumerateTemplates(System.Func`3[System.String, System.String, Microsoft.Diagnostics.Tracing.EventFilterResponse],System.Action`1[Microsoft.Diagnostics.Tracing.TraceEvent]):this
          20 (1.69 % of base) : 33447.dasm - Microsoft.CodeAnalysis.TypeNameDecoder`2:GetTypeSymbol(Microsoft.CodeAnalysis.MetadataHelpers+AssemblyQualifiedTypeName,byref):System.__Canon:this
          20 (2.07 % of base) : 78533.dasm - System.ComponentModel.EnumConverter:ConvertFrom(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object):System.Object:this
          20 (1.87 % of base) : 77592.dasm - System.ComponentModel.MaskedTextProvider:.ctor(System.String,System.Globalization.CultureInfo,bool,ushort,ushort,bool):this
          20 (2.49 % of base) : 21601.dasm - System.Data.Common.SqlBooleanStorage:Aggregate(System.Int32[],int):System.Object:this
          20 (4.24 % of base) : 179464.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          20 (3.57 % of base) : 13613.dasm - System.TimeZoneInfo:TryConvertIanaIdToWindowsId(System.String,bool,byref):bool
          20 (6.85 % of base) : 89889.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderCodeGen+Member,System.String):this
          20 (7.04 % of base) : 89800.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderILGen+Member,System.String):this
          16 (0.40 % of base) : 21743.dasm - System.Data.Common.SqlDecimalStorage:Aggregate(System.Int32[],int):System.Object:this
          16 (7.41 % of base) : 26072.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          16 (4.21 % of base) : 2447.dasm - System.IO.PathInternal:NormalizeDirectorySeparators(System.String):System.String

Top method improvements (bytes):
      -12260 (-44.63 % of base) : 82412.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -5404 (-42.74 % of base) : 17038.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2384 (-52.74 % of base) : 82399.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
       -1856 (-36.54 % of base) : 82410.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1652 (-45.94 % of base) : 82494.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
       -1532 (-45.11 % of base) : 82497.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
       -1060 (-46.99 % of base) : 82406.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
       -1060 (-35.81 % of base) : 196484.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -824 (-38.50 % of base) : 121011.dasm - Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter:UseTwiceLateInvocation(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.VisualBasic.BoundLateInvocation,Microsoft.CodeAnalysis.ArrayBuilder`1[Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedLocal]):Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter+Result
        -812 (-24.82 % of base) : 82792.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:CanMatchArguments(Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],System.Type[],bool,System.Collections.Generic.List`1[System.String]):bool
        -804 (-29.30 % of base) : 196483.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -748 (-43.90 % of base) : 82404.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -736 (-35.25 % of base) : 36174.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
        -732 (-42.46 % of base) : 140062.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -724 (-31.42 % of base) : 97541.dasm - System.Xml.Schema.ParticleContentValidator:BuildTransitionTable(System.Xml.Schema.BitSet,System.Xml.Schema.BitSet[],int):System.Int32[][]:this
        -724 (-45.59 % of base) : 93525.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -712 (-34.56 % of base) : 173222.dasm - System.DirectoryServices.Protocols.DirectoryAttribute:GetValues(System.Type):System.Object[]:this
        -704 (-27.80 % of base) : 17035.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -692 (-39.41 % of base) : 131380.dasm - Internal.Cryptography.Pal.OpenSslX509ChainProcessor:BuildChainElements(Internal.Cryptography.Pal.OpenSslX509ChainProcessor+WorkingChain,byref):System.Security.Cryptography.X509Certificates.X509ChainElement[]:this
        -692 (-30.35 % of base) : 17036.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this

Top method regressions (percentages):
          16 (13.79 % of base) : 178775.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          36 (9.00 % of base) : 12176.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          12 (7.50 % of base) : 65345.dasm - System.Reflection.Internal.ObjectPool`1:Allocate():System.__Canon:this
          16 (7.41 % of base) : 26072.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          20 (7.04 % of base) : 89800.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderILGen+Member,System.String):this
          20 (6.85 % of base) : 89889.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderCodeGen+Member,System.String):this
          16 (6.45 % of base) : 168121.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int
          16 (6.45 % of base) : 70967.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int
          16 (6.45 % of base) : 170232.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int
          12 (6.25 % of base) : 33199.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[Microsoft.CodeAnalysis.CommonReferenceManager`2+AssemblyReferenceBinding[System.__Canon, System.__Canon][]]):bool
          12 (4.69 % of base) : 56385.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          12 (4.55 % of base) : 11319.dasm - System.Globalization.JapaneseLunisolarCalendar:TrimEras(System.Globalization.EraInfo[]):System.Globalization.EraInfo[]
          40 (4.42 % of base) : 16721.dasm - System.DateTimeParse:MatchEraName(byref,System.Globalization.DateTimeFormatInfo,byref):bool
           8 (4.26 % of base) : 126533.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CRC32:Crc32Update(int,System.Byte[]):int
          20 (4.24 % of base) : 179464.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          12 (4.23 % of base) : 967.dasm - System.Diagnostics.Tracing.XplatEventLogger:minimalJsonserializer(System.String,System.Text.StringBuilder)
          16 (4.21 % of base) : 2447.dasm - System.IO.PathInternal:NormalizeDirectorySeparators(System.String):System.String
          12 (3.66 % of base) : 185756.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          20 (3.57 % of base) : 13613.dasm - System.TimeZoneInfo:TryConvertIanaIdToWindowsId(System.String,bool,byref):bool
          12 (3.26 % of base) : 33698.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String

Top method improvements (percentages):
       -2384 (-52.74 % of base) : 82399.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
        -432 (-50.23 % of base) : 26299.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this
        -532 (-49.81 % of base) : 26084.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -408 (-48.57 % of base) : 24914.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
        -280 (-47.95 % of base) : 155760.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,Microsoft.CodeAnalysis.CSharp.OverloadResolution+ParameterMap):System.Nullable`1[System.Int32]
        -292 (-47.71 % of base) : 81326.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
       -1060 (-46.99 % of base) : 82406.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -412 (-46.61 % of base) : 83129.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
       -1652 (-45.94 % of base) : 82494.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
        -332 (-45.60 % of base) : 115168.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
        -724 (-45.59 % of base) : 93525.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -504 (-45.32 % of base) : 209019.dasm - System.Web.Util.HttpEncoder:UrlEncodeUnicode(System.String):System.String
       -1532 (-45.11 % of base) : 82497.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
      -12260 (-44.63 % of base) : 82412.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
        -596 (-44.48 % of base) : 185608.dasm - Internal.TypeSystem.RuntimeDeterminedTypeUtilities:ConvertInstantiationToSharedRuntimeForm(Internal.TypeSystem.Instantiation,Internal.TypeSystem.Instantiation,byref):Internal.TypeSystem.Instantiation
        -160 (-44.44 % of base) : 73831.dasm - Newtonsoft.Json.Utilities.ConvertUtils:TryHexTextToInt(System.Char[],int,int,byref):bool
        -748 (-43.90 % of base) : 82404.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -644 (-43.63 % of base) : 131408.dasm - <>c:<get_Extensions>b__65_0(Microsoft.Win32.SafeHandles.SafeX509Handle):System.Security.Cryptography.X509Certificates.X509Extension[]:this
        -400 (-43.10 % of base) : 82810.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:IsExactSignatureMatch(System.Reflection.ParameterInfo[],int,System.Reflection.ParameterInfo[],int):bool
        -160 (-43.01 % of base) : 21123.dasm - System.Collections.Generic.NullableEqualityComparer`1:IndexOf(System.Nullable`1[System.Int32][],System.Nullable`1[System.Int32],int,int):int:this

853 total methods with Code Size differences (777 improved, 76 regressed), 144 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.pmi.Linux.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 47200124 (overridden on cmd)
Total bytes of diff: 47047060 (overridden on cmd)
Total bytes of delta: -153064 (-0.32 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          44 : 121990.dasm (1.04 % of base)
          40 : 160274.dasm (8.26 % of base)
          40 : 543.dasm (12.82 % of base)
          40 : 184623.dasm (4.03 % of base)
          36 : 161387.dasm (5.14 % of base)
          36 : 187814.dasm (9.38 % of base)
          36 : 24087.dasm (1.42 % of base)
          32 : 105319.dasm (6.67 % of base)
          32 : 172485.dasm (2.66 % of base)
          32 : 192117.dasm (8.60 % of base)
          32 : 192281.dasm (8.60 % of base)
          28 : 1436.dasm (10.45 % of base)
          28 : 161671.dasm (3.65 % of base)
          28 : 28845.dasm (3.95 % of base)
          28 : 93909.dasm (2.83 % of base)
          28 : 153664.dasm (1.87 % of base)
          28 : 170142.dasm (8.64 % of base)
          28 : 183604.dasm (3.65 % of base)
          24 : 123287.dasm (7.41 % of base)
          24 : 179966.dasm (2.14 % of base)

Top file improvements (bytes):
      -10124 : 59416.dasm (-42.67 % of base)
       -2368 : 31221.dasm (-44.68 % of base)
       -1852 : 34058.dasm (-46.63 % of base)
       -1748 : 59418.dasm (-37.29 % of base)
       -1708 : 128258.dasm (-45.57 % of base)
       -1708 : 36505.dasm (-45.96 % of base)
       -1548 : 38282.dasm (-46.07 % of base)
       -1400 : 34060.dasm (-48.41 % of base)
       -1344 : 38208.dasm (-33.01 % of base)
       -1332 : 36502.dasm (-49.12 % of base)
       -1320 : 38155.dasm (-22.15 % of base)
       -1284 : 55330.dasm (-44.28 % of base)
       -1232 : 54193.dasm (-26.19 % of base)
       -1200 : 33778.dasm (-40.98 % of base)
       -1124 : 37171.dasm (-28.07 % of base)
       -1100 : 54243.dasm (-24.60 % of base)
       -1092 : 37558.dasm (-42.00 % of base)
       -1092 : 31414.dasm (-13.67 % of base)
       -1076 : 145452.dasm (-48.21 % of base)
       -1056 : 188442.dasm (-39.17 % of base)

973 total files with Code Size differences (855 improved, 118 regressed), 184 unchanged.

Top method regressions (bytes):
          44 (1.04 % of base) : 121990.dasm - System.Xml.Serialization.SchemaGraph:Depends(System.Xml.Schema.XmlSchemaObject,System.Collections.ArrayList):this
          40 (12.82 % of base) : 543.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          40 (4.03 % of base) : 184623.dasm - System.Net.WebClient:GetStringUsingEncoding(System.Net.WebRequest,System.Byte[]):System.String:this
          40 (8.26 % of base) : 160274.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          36 (9.38 % of base) : 187814.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          36 (1.42 % of base) : 24087.dasm - System.Data.DataColumnCollection:CanRemove(System.Data.DataColumn,bool):bool:this
          36 (5.14 % of base) : 161387.dasm - Xunit.StackFrameInfo:FromFailure(Xunit.Abstractions.IFailureInformation):Xunit.StackFrameInfo
          32 (6.67 % of base) : 105319.dasm - <OrderBy>d__3`1[Byte][System.Byte]:MoveNext():bool:this
          32 (8.60 % of base) : 192281.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (8.60 % of base) : 192117.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (2.66 % of base) : 172485.dasm - System.Uri:GetLocalPath():System.String:this
          28 (1.87 % of base) : 153664.dasm - DebugViewPrinter:Analyze():this
          28 (2.83 % of base) : 93909.dasm - Microsoft.CodeAnalysis.TypeNameDecoder`2[__Canon,__Canon][System.__Canon,System.__Canon]:GetTypeSymbol(AssemblyQualifiedTypeName,byref):System.__Canon:this
          28 (10.45 % of base) : 1436.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          28 (8.64 % of base) : 170142.dasm - System.Collections.HashHelpers:GetPrime(int):int
          28 (3.95 % of base) : 28845.dasm - System.Data.Common.SqlBooleanStorage:Aggregate(System.Int32[],int):System.Object:this
          28 (3.65 % of base) : 161671.dasm - Xunit.Serialization.XunitSerializationInfo:CanSerializeObject(System.Object):bool
          28 (3.65 % of base) : 183604.dasm - Xunit.Serialization.XunitSerializationInfo:CanSerializeObject(System.Object):bool
          24 (1.01 % of base) : 216451.dasm - R2RTest.BuildFolder:FromDirectory(System.String,System.Collections.Generic.IEnumerable`1[[R2RTest.CompilerRunner, R2RTest, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null]],System.String,R2RTest.BuildOptions):R2RTest.BuildFolder
          24 (2.14 % of base) : 179966.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this

Top method improvements (bytes):
      -10124 (-42.67 % of base) : 59416.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2368 (-44.68 % of base) : 31221.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportUnspecificProcedures(Microsoft.CodeAnalysis.Location,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.DiagnosticBag,bool):this
       -1852 (-46.63 % of base) : 34058.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
       -1748 (-37.29 % of base) : 59418.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1708 (-45.57 % of base) : 128258.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:IsApplicable(Microsoft.CodeAnalysis.CSharp.Symbol,EffectiveParameters,Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,System.Collections.Immutable.ImmutableArray`1[Int32],bool,bool,bool,byref):Microsoft.CodeAnalysis.CSharp.MemberAnalysisResult:this
       -1708 (-45.96 % of base) : 36505.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1548 (-46.07 % of base) : 38282.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
       -1400 (-48.41 % of base) : 34060.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1344 (-33.01 % of base) : 38208.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateMakeArgumentArrayArgument(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1332 (-49.12 % of base) : 36502.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
       -1320 (-22.15 % of base) : 38155.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateCallOrGet(Microsoft.CodeAnalysis.VisualBasic.BoundLateMemberAccess,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1284 (-44.28 % of base) : 55330.dasm - AsyncMethodToClassRewriter:RewriteSpillSequenceIntoBlock(Microsoft.CodeAnalysis.VisualBasic.BoundSpillSequence,bool,Microsoft.CodeAnalysis.VisualBasic.BoundStatement[]):Microsoft.CodeAnalysis.VisualBasic.BoundBlock:this
       -1232 (-26.19 % of base) : 54193.dasm - AnonymousDelegatePublicSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1200 (-40.98 % of base) : 33778.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceAttributeData:GetTargetAttributeSignatureIndex(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.AttributeDescription):int:this
       -1124 (-28.07 % of base) : 37171.dasm - Microsoft.CodeAnalysis.VisualBasic.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol,System.Predicate`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1100 (-24.60 % of base) : 54243.dasm - AnonymousDelegateTemplateSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1092 (-13.67 % of base) : 31414.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:MakeVarianceConversionSuggestion(int,Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol,Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol,Microsoft.CodeAnalysis.DiagnosticBag,bool):bool:this
       -1092 (-42.00 % of base) : 37558.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbol:GetAttributesToBind(Roslyn.Utilities.OneOrMany`1[[Microsoft.CodeAnalysis.SyntaxList`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], Microsoft.CodeAnalysis, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],int,Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation,byref):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]:this
       -1076 (-48.21 % of base) : 145452.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
       -1056 (-39.17 % of base) : 188442.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException

Top method regressions (percentages):
          24 (20.69 % of base) : 184622.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          40 (12.82 % of base) : 543.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          28 (10.45 % of base) : 1436.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          36 (9.38 % of base) : 187814.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          16 (9.30 % of base) : 24045.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          28 (8.64 % of base) : 170142.dasm - System.Collections.HashHelpers:GetPrime(int):int
          32 (8.60 % of base) : 192281.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (8.60 % of base) : 192117.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          12 (8.57 % of base) : 105460.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          40 (8.26 % of base) : 160274.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          16 (8.00 % of base) : 202109.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          16 (7.84 % of base) : 148633.dasm - QueryTranslationState:RangeVariableMap(Microsoft.CodeAnalysis.CSharp.Symbols.RangeVariableSymbol[]):RangeVariableMap
          16 (7.69 % of base) : 32929.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CRC32:ComputeCRC32(System.String[]):int
          12 (7.69 % of base) : 207241.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          20 (7.69 % of base) : 123372.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
          24 (7.41 % of base) : 123287.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(Member,System.String):this
          12 (7.32 % of base) : 195812.dasm - System.IO.IsolatedStorage.IsolatedStorageFile:GetFullPath(System.String):System.String:this
          32 (6.67 % of base) : 105319.dasm - <OrderBy>d__3`1[Byte][System.Byte]:MoveNext():bool:this
          16 (6.06 % of base) : 171527.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int
          16 (6.06 % of base) : 105834.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int

Top method improvements (percentages):
        -672 (-54.19 % of base) : 128240.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,ParameterMap):System.Nullable`1[Int32]
        -300 (-51.02 % of base) : 56999.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
        -432 (-50.70 % of base) : 24084.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -376 (-50.27 % of base) : 23867.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this
        -352 (-49.16 % of base) : 25310.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
       -1332 (-49.12 % of base) : 36502.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
        -900 (-48.49 % of base) : 132848.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1400 (-48.41 % of base) : 34060.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
        -380 (-48.22 % of base) : 58694.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
        -324 (-48.21 % of base) : 44583.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
       -1076 (-48.21 % of base) : 145452.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
        -964 (-48.10 % of base) : 132856.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:RewriteArgumentsForComCall(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.Symbols.LocalSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -896 (-47.16 % of base) : 59423.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -452 (-47.08 % of base) : 92647.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArray(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -636 (-46.90 % of base) : 178130.dasm - Internal.Cryptography.Pal.UnixPkcs12Reader:FindMatchingKey(System.Security.Cryptography.Asn1.Pkcs12.SafeBagAsn[],int,System.ReadOnlySpan`1[Byte]):int
       -1852 (-46.63 % of base) : 34058.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
        -800 (-46.40 % of base) : 145874.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.CustomModifierUtils:CopyParameterCustomModifiers(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
       -1548 (-46.07 % of base) : 38282.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
        -160 (-45.98 % of base) : 68917.dasm - Newtonsoft.Json.Utilities.ConvertUtils:TryHexTextToInt(System.Char[],int,int,byref):bool
       -1708 (-45.96 % of base) : 36505.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this

973 total methods with Code Size differences (855 improved, 118 regressed), 184 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries_tests.pmi.Linux.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 111306288 (overridden on cmd)
Total bytes of diff: 111209824 (overridden on cmd)
Total bytes of delta: -96464 (-0.09 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         152 : 162617.dasm (3.83 % of base)
          80 : 252944.dasm (6.83 % of base)
          60 : 260364.dasm (1.75 % of base)
          52 : 242774.dasm (1.14 % of base)
          48 : 263281.dasm (3.23 % of base)
          40 : 58666.dasm (0.21 % of base)
          40 : 322747.dasm (2.14 % of base)
          32 : 208332.dasm (3.64 % of base)
          32 : 239169.dasm (1.40 % of base)
          32 : 247763.dasm (2.45 % of base)
          28 : 206506.dasm (0.33 % of base)
          28 : 308717.dasm (0.44 % of base)
          28 : 308727.dasm (0.42 % of base)
          28 : 323245.dasm (8.75 % of base)
          28 : 97762.dasm (7.61 % of base)
          24 : 126205.dasm (0.54 % of base)
          24 : 192129.dasm (2.14 % of base)
          24 : 161162.dasm (1.66 % of base)
          24 : 152147.dasm (2.16 % of base)
          24 : 42015.dasm (2.58 % of base)

Top file improvements (bytes):
       -1136 : 162430.dasm (-45.51 % of base)
        -768 : 61390.dasm (-24.37 % of base)
        -700 : 98878.dasm (-35.43 % of base)
        -684 : 229491.dasm (-24.15 % of base)
        -524 : 95834.dasm (-49.06 % of base)
        -428 : 61394.dasm (-15.95 % of base)
        -384 : 154159.dasm (-16.49 % of base)
        -384 : 194106.dasm (-16.49 % of base)
        -372 : 244439.dasm (-12.76 % of base)
        -340 : 336167.dasm (-22.08 % of base)
        -332 : 336168.dasm (-21.23 % of base)
        -320 : 260444.dasm (-33.90 % of base)
        -316 : 306649.dasm (-23.51 % of base)
        -304 : 205881.dasm (-41.08 % of base)
        -300 : 115887.dasm (-43.10 % of base)
        -300 : 305815.dasm (-7.38 % of base)
        -300 : 310902.dasm (-43.10 % of base)
        -296 : 20561.dasm (-23.95 % of base)
        -288 : 20332.dasm (-35.64 % of base)
        -288 : 20316.dasm (-29.88 % of base)

1047 total files with Code Size differences (855 improved, 192 regressed), 200 unchanged.

Top method regressions (bytes):
         152 (3.83 % of base) : 162617.dasm - Microsoft.Build.Tasks.ResolveAssemblyReference:LogInputs():this
          80 (6.83 % of base) : 252944.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          60 (1.75 % of base) : 260364.dasm - System.Threading.Tests.MonitorTests:Enter_HasToWait()
          52 (1.14 % of base) : 242774.dasm - System.IO.Tests.BinaryWriterTests:BinaryWriter_SeekTests():this
          48 (3.23 % of base) : 263281.dasm - <MultipleProcesses_StartAllKillAllWaitAllAsync>d__1:MoveNext():this
          40 (0.21 % of base) : 58666.dasm - <ArrayAsRootObject>d__7:MoveNext():this
          40 (2.14 % of base) : 322747.dasm - NuGet.ProjectModel.PackageSpec:GetHashCode():int:this
          32 (3.64 % of base) : 208332.dasm - <>c__DisplayClass1_0:<GetTags>b__2():System.UInt32[]:this
          32 (2.45 % of base) : 247763.dasm - Lamar.Scanning.Conventions.GenericConnectionScanner:ScanTypes(Lamar.Scanning.TypeSet,Lamar.ServiceRegistry):this
          32 (1.40 % of base) : 239169.dasm - System.Collections.Concurrent.Tests.BlockingCollectionTests:AddAnyTakeAny(int,int,int,System.Collections.Concurrent.BlockingCollection`1[Int32],System.Collections.Concurrent.BlockingCollection`1[System.Int32][],int)
          28 (0.33 % of base) : 206506.dasm - <>c__DisplayClass40_0:<UseInstance>b__0(Registry):Registry:this
          28 (7.61 % of base) : 97762.dasm - Microsoft.CodeAnalysis.Collections.Internal.HashHelpers:GetPrime(int):int
          28 (0.44 % of base) : 308717.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscovery:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          28 (0.42 % of base) : 308727.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscoveryV1:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          28 (8.75 % of base) : 323245.dasm - Unity.Utility.Prime:GetPrime(int):int
          24 (2.58 % of base) : 42015.dasm - <<GetAsync_SetCookieContainerMultipleCookies_CookiesSent>b__0>d:MoveNext():this
          24 (0.54 % of base) : 126205.dasm - Microsoft.Build.Construction.SolutionProjectGenerator:CreateTraversalInstance(System.String,bool,System.Collections.Generic.List`1[[Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]):Microsoft.Build.Execution.ProjectInstance:this
          24 (2.14 % of base) : 192129.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          24 (2.16 % of base) : 152147.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          24 (1.66 % of base) : 161162.dasm - System.Numerics.Tests.ToStringTest:ConvertDecimalToHex(System.String,bool,System.Globalization.NumberFormatInfo):System.String

Top method improvements (bytes):
       -1136 (-45.51 % of base) : 162430.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -768 (-24.37 % of base) : 61390.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddAfter_LLNode():this
        -700 (-35.43 % of base) : 98878.dasm - Microsoft.CodeAnalysis.Shared.Extensions.IMethodSymbolExtensions:RenameTypeParameters(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.Shared.Extensions.ITypeGenerator):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -684 (-24.15 % of base) : 229491.dasm - Expander:VisitParenthesizedLambdaExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax):Microsoft.CodeAnalysis.SyntaxNode:this
        -524 (-49.06 % of base) : 95834.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -428 (-15.95 % of base) : 61394.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddBefore_LLNode():this
        -384 (-16.49 % of base) : 154159.dasm - System.Data.SqlClient.TdsParser:WriteSessionRecoveryFeatureRequest(System.Data.SqlClient.SessionData,bool):int:this
        -384 (-16.49 % of base) : 194106.dasm - System.Data.SqlClient.TdsParser:WriteSessionRecoveryFeatureRequest(System.Data.SqlClient.SessionData,bool):int:this
        -372 (-12.76 % of base) : 244439.dasm - System.Diagnostics.Tests.DiagnosticSourceEventSourceBridgeTests:<TestEnableAllActivitySourcesWithOneEvent>b__1_0(System.String):this
        -340 (-22.08 % of base) : 336167.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[__Canon,__Canon,__Canon,__Canon]
        -332 (-21.23 % of base) : 336168.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60],[System.IServiceProvider, System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a],[System.Threading.Tasks.Task, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        -320 (-33.90 % of base) : 260444.dasm - System.Threading.Tests.SpinLockTests:RunSpinLockTest2_TryEnter(int,bool)
        -316 (-23.51 % of base) : 306649.dasm - System.TypeExtensions:SatisfiesGenericConstraintsOf(System.Type,System.Reflection.TypeInfo):bool
        -304 (-41.08 % of base) : 205881.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool
        -300 (-43.10 % of base) : 115887.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-43.10 % of base) : 310902.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-7.38 % of base) : 305815.dasm - System.Transactions.Tests.NonMsdtcPromoterTests:TestCase_PSPENonMsdtc(bool,bool,int,int,int,int,int,int,int)
        -296 (-23.95 % of base) : 20561.dasm - System.Linq.Expressions.Tests.NullableNewArrayListTests:CheckNullableDecimalArrayListTest(bool)
        -288 (-29.88 % of base) : 20316.dasm - System.Linq.Expressions.Tests.NewArrayListTests:CheckDecimalArrayListTest(bool)
        -288 (-35.64 % of base) : 20332.dasm - System.Linq.Expressions.Tests.NewArrayListTests:CheckStructWithStringAndFieldArrayListTest(bool)

Top method regressions (percentages):
          16 (11.11 % of base) : 17400.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          16 (10.26 % of base) : 93869.dasm - Roslyn.Utilities.PathUtilities:PathHashCode(System.String):int
          20 (8.93 % of base) : 164407.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          20 (8.93 % of base) : 131562.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          20 (8.93 % of base) : 333528.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          28 (8.75 % of base) : 323245.dasm - Unity.Utility.Prime:GetPrime(int):int
          16 (8.70 % of base) : 17402.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          28 (7.61 % of base) : 97762.dasm - Microsoft.CodeAnalysis.Collections.Internal.HashHelpers:GetPrime(int):int
          80 (6.83 % of base) : 252944.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          20 (6.76 % of base) : 206382.dasm - EmittingVisitor:TryEmitLabel(FastExpressionCompiler.LightExpression.LabelExpression,System.Collections.Generic.IReadOnlyList`1[[FastExpressionCompiler.LightExpression.ParameterExpression, DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]],System.Reflection.Emit.ILGenerator,byref,int):bool
          16 (6.06 % of base) : 241171.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int
           8 (5.41 % of base) : 206240.dasm - <>c__45`1[Byte][System.Byte]:<Visit>b__45_0(ImTools.ImMapEntry`1[KValue`1],bool,System.Action`1[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]]):bool:this
           8 (5.00 % of base) : 206236.dasm - <>c__44`2[Byte,Nullable`1][System.Byte,System.Nullable`1[System.Int32]]:<Visit>b__44_0(ImTools.ImMapEntry`1[KValue`1],System.Nullable`1[Int32],System.Action`2[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]):System.Nullable`1[Int32]:this
           8 (4.88 % of base) : 161512.dasm - System.Numerics.Tests.MyBigIntImp:PrintFormatX2(System.Byte[]):System.String
           8 (4.76 % of base) : 206232.dasm - <>c__43`2[Byte,Nullable`1][System.Byte,System.Nullable`1[System.Int32]]:<Fold>b__43_0(ImTools.ImMapEntry`1[KValue`1],System.Nullable`1[Int32],System.Func`3[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]):System.Nullable`1[Int32]:this
          16 (4.55 % of base) : 328835.dasm - LamarCompiler.Util.TypeExtensions:Closes(System.Type,System.Type):bool
          16 (4.49 % of base) : 171703.dasm - CatalogListener:ContainsChanges():bool:this
          12 (4.11 % of base) : 305573.dasm - Parser:DeterminePropNameIdentifier(System.String):System.String
           8 (3.92 % of base) : 20817.dasm - System.Linq.Expressions.Tests.BinaryNullableAddTests:CheckNullableUIntAddTest(bool)
           8 (3.92 % of base) : 20815.dasm - System.Linq.Expressions.Tests.BinaryNullableAddTests:CheckNullableUShortAddTest(bool)

Top method improvements (percentages):
        -524 (-49.06 % of base) : 95834.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
       -1136 (-45.51 % of base) : 162430.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
         -80 (-44.44 % of base) : 93264.dasm - Roslyn.Utilities.Hash:GetFNVHashCode(System.Char[],int,int):int
        -284 (-43.83 % of base) : 205880.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
        -168 (-43.75 % of base) : 457.dasm - System.Runtime.Serialization.Formatters.Tests.EqualityHelpers:ArraysAreEqual(System.Byte[][],System.Byte[][]):bool
        -300 (-43.10 % of base) : 115887.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-43.10 % of base) : 310902.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
         -72 (-41.86 % of base) : 198989.dasm - Microsoft.CodeAnalysis.VisualBasic.Extensions.SyntaxKindExtensions:IndexOf(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind[],ushort,int):int
        -112 (-41.79 % of base) : 99298.dasm - Microsoft.CodeAnalysis.Shared.Extensions.StringExtensions:ConvertTabToSpace(System.String,int,int,int):int
        -304 (-41.08 % of base) : 205881.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool
        -160 (-40.82 % of base) : 94176.dasm - Roslyn.Utilities.EditDistance:ConvertToLowercaseArray(System.String):System.Char[]
         -68 (-40.48 % of base) : 213150.dasm - ChecksumWriter:Write(System.Char[]):this
         -68 (-40.48 % of base) : 141300.dasm - ChecksumWriter:Write(System.Char[]):this
         -68 (-40.48 % of base) : 213149.dasm - ChecksumWriter:Write(System.String):this
         -68 (-40.48 % of base) : 141299.dasm - ChecksumWriter:Write(System.String):this
        -132 (-40.24 % of base) : 325744.dasm - System.Numerics.Tests.Util:GenerateRandomLongs(int):System.Int64[]
        -280 (-40.00 % of base) : 307631.dasm - <>c__DisplayClass36_0:<GetMethodByArguments>b__0(System.Reflection.MethodBase):bool:this
        -204 (-39.84 % of base) : 284428.dasm - Castle.DynamicProxy.Generators.Emitters.ArgumentsUtil:ConvertToArgumentReferenceExpression(System.Reflection.ParameterInfo[]):Castle.DynamicProxy.Generators.Emitters.SimpleAST.ReferenceExpression[]
        -100 (-39.68 % of base) : 230625.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -100 (-39.68 % of base) : 290187.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this

1047 total methods with Code Size differences (855 improved, 192 regressed), 200 unchanged.

```

</details>

--------------------------------------------------------------------------------


</details>


## Linux x64

<details>

<summary>Linux x64 details</summary>

Summary file: `superpmi_diff_summary_Linux_x64.md`

To reproduce these diffs on windows x64:
```
superpmi.py asmdiffs -target_os Linux -target_arch x64 -arch x64
```

## benchmarks.run.Linux.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 9083170 (overridden on cmd)
Total bytes of diff: 9040541 (overridden on cmd)
Total bytes of delta: -42629 (-0.47 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         169 : 8102.dasm (13.51 % of base)
          70 : 7687.dasm (4.65 % of base)
          67 : 7462.dasm (4.22 % of base)
          27 : 1958.dasm (1.59 % of base)
          20 : 4846.dasm (0.27 % of base)
          18 : 11330.dasm (7.53 % of base)
          18 : 9025.dasm (1.51 % of base)
          18 : 6.dasm (7.53 % of base)
          15 : 10460.dasm (7.11 % of base)
          15 : 18672.dasm (3.75 % of base)
          14 : 19742.dasm (0.45 % of base)
          14 : 5434.dasm (0.73 % of base)
          14 : 7655.dasm (6.48 % of base)
          14 : 8013.dasm (0.72 % of base)
          13 : 19091.dasm (5.10 % of base)
          11 : 33.dasm (1.14 % of base)
          10 : 18899.dasm (7.35 % of base)
          10 : 21466.dasm (10.31 % of base)
           9 : 1340.dasm (1.82 % of base)
           9 : 1866.dasm (2.97 % of base)

Top file improvements (bytes):
       -4619 : 1970.dasm (-42.03 % of base)
       -2295 : 26379.dasm (-66.93 % of base)
       -2066 : 17716.dasm (-66.07 % of base)
       -1068 : 14424.dasm (-58.62 % of base)
       -1066 : 14307.dasm (-62.23 % of base)
        -887 : 26836.dasm (-59.41 % of base)
        -819 : 3002.dasm (-21.59 % of base)
        -802 : 9003.dasm (-58.12 % of base)
        -713 : 3873.dasm (-48.01 % of base)
        -681 : 4305.dasm (-38.15 % of base)
        -658 : 2549.dasm (-33.42 % of base)
        -625 : 3034.dasm (-32.05 % of base)
        -622 : 17715.dasm (-58.96 % of base)
        -601 : 20225.dasm (-48.20 % of base)
        -579 : 7103.dasm (-42.76 % of base)
        -550 : 13512.dasm (-42.24 % of base)
        -527 : 15594.dasm (-45.59 % of base)
        -518 : 26722.dasm (-64.75 % of base)
        -506 : 15477.dasm (-69.51 % of base)
        -496 : 17714.dasm (-46.75 % of base)

279 total files with Code Size differences (241 improved, 38 regressed), 20 unchanged.

Top method regressions (bytes):
         169 (13.51 % of base) : 8102.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
          70 (4.65 % of base) : 7687.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          67 (4.22 % of base) : 7462.dasm - System.Xml.Serialization.XmlAttributes:.ctor(System.Reflection.ICustomAttributeProvider):this
          27 (1.59 % of base) : 1958.dasm - MemberInfoCache`1[__Canon][System.__Canon]:PopulateInterfaces(Filter):System.RuntimeType[]:this
          20 (0.27 % of base) : 4846.dasm - ProtoBuf.Meta.MetaType:ApplyDefaultBehaviourImpl(int):this
          18 (1.51 % of base) : 9025.dasm - System.Collections.Concurrent.ConcurrentDictionary`2[Int32,Int32][System.Int32,System.Int32]:GrowTable(Tables[Int32,Int32]):this
          18 (7.53 % of base) : 11330.dasm - System.Collections.HashHelpers:GetPrime(int):int
          18 (7.53 % of base) : 6.dasm - System.Collections.HashHelpers:GetPrime(int):int
          15 (3.75 % of base) : 18672.dasm - Microsoft.CodeAnalysis.Compilation:ValidateReferences(System.Collections.Generic.IEnumerable`1[[Microsoft.CodeAnalysis.MetadataReference, Microsoft.CodeAnalysis, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.MetadataReference, Microsoft.CodeAnalysis, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
          15 (7.11 % of base) : 10460.dasm - Perfolizer.Horology.TimeUnit:GetBestTimeUnit(System.Double[]):Perfolizer.Horology.TimeUnit
          14 (0.45 % of base) : 19742.dasm - Microsoft.CodeAnalysis.CSharp.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol):this
          14 (0.72 % of base) : 8013.dasm - Sigil.Emit`1[__Canon][System.__Canon]:Switch(Sigil.Label[]):Sigil.Emit`1[__Canon]:this
          14 (0.73 % of base) : 5434.dasm - System.Reflection.RuntimeCustomAttributeData:.ctor(System.Reflection.RuntimeModule,System.Reflection.MetadataToken,byref):this
          14 (6.48 % of base) : 7655.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
          13 (5.10 % of base) : 19091.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          11 (1.14 % of base) : 33.dasm - System.StartupHookProvider:ProcessStartupHooks()
          10 (7.35 % of base) : 18899.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[__Canon]):bool
          10 (10.31 % of base) : 21466.dasm - System.ByteArrayHelpers:EqualsOrdinalAsciiIgnoreCase(System.String,System.ReadOnlySpan`1[Byte]):bool
           9 (1.82 % of base) : 1340.dasm - Microsoft.Extensions.Options.OptionsFactory`1[__Canon][System.__Canon]:Create(System.String):System.__Canon:this
           9 (2.97 % of base) : 1866.dasm - System.IO.PathInternal:NormalizeDirectorySeparators(System.String):System.String

Top method improvements (bytes):
       -4619 (-42.03 % of base) : 1970.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2295 (-66.93 % of base) : 26379.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -2066 (-66.07 % of base) : 17716.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1068 (-58.62 % of base) : 14424.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
       -1066 (-62.23 % of base) : 14307.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -887 (-59.41 % of base) : 26836.dasm - Benchstone.BenchF.InvMt:Test():bool:this
        -819 (-21.59 % of base) : 3002.dasm - AutomataNode:EmitSearchNextCore(System.Reflection.Emit.ILGenerator,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Action`1[[System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],System.Action,Utf8Json.Internal.AutomataDictionary+AutomataNode[],int)
        -802 (-58.12 % of base) : 9003.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -713 (-48.01 % of base) : 3873.dasm - Internal.Cryptography.Pal.UnixPkcs12Reader:FindMatchingKey(System.Security.Cryptography.Asn1.Pkcs12.SafeBagAsn[],int,System.ReadOnlySpan`1[Byte]):int
        -681 (-38.15 % of base) : 4305.dasm - Internal.Cryptography.Pal.OpenSslX509ChainProcessor:BuildChainElements(WorkingChain,byref):System.Security.Cryptography.X509Certificates.X509ChainElement[]:this
        -658 (-33.42 % of base) : 2549.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -625 (-32.05 % of base) : 3034.dasm - System.Reflection.Emit.MethodBuilder:CreateMethodBodyHelper(System.Reflection.Emit.ILGenerator):this
        -622 (-58.96 % of base) : 17715.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -601 (-48.20 % of base) : 20225.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool,Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -579 (-42.76 % of base) : 7103.dasm - BilinearTest:BilinearInterpol(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]
        -550 (-42.24 % of base) : 13512.dasm - Fourier:DoFPUTransIteration(System.Double[],System.Double[],int):long
        -527 (-45.59 % of base) : 15594.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int
        -518 (-64.75 % of base) : 26722.dasm - Benchstone.BenchF.InProd:Test():bool:this
        -506 (-69.51 % of base) : 15477.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
        -496 (-46.75 % of base) : 17714.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])

Top method regressions (percentages):
         169 (13.51 % of base) : 8102.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
          10 (10.31 % of base) : 21466.dasm - System.ByteArrayHelpers:EqualsOrdinalAsciiIgnoreCase(System.String,System.ReadOnlySpan`1[Byte]):bool
          18 (7.53 % of base) : 11330.dasm - System.Collections.HashHelpers:GetPrime(int):int
          18 (7.53 % of base) : 6.dasm - System.Collections.HashHelpers:GetPrime(int):int
          10 (7.35 % of base) : 18899.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[__Canon]):bool
           8 (7.21 % of base) : 21598.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          15 (7.11 % of base) : 10460.dasm - Perfolizer.Horology.TimeUnit:GetBestTimeUnit(System.Double[]):Perfolizer.Horology.TimeUnit
          14 (6.48 % of base) : 7655.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
           3 (5.17 % of base) : 14340.dasm - BenchmarksGame.Fasta_1:MakeCumulative(BenchmarksGame.Fasta_1+Frequency[])
          13 (5.10 % of base) : 19091.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          70 (4.65 % of base) : 7687.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          67 (4.22 % of base) : 7462.dasm - System.Xml.Serialization.XmlAttributes:.ctor(System.Reflection.ICustomAttributeProvider):this
          15 (3.75 % of base) : 18672.dasm - Microsoft.CodeAnalysis.Compilation:ValidateReferences(System.Collections.Generic.IEnumerable`1[[Microsoft.CodeAnalysis.MetadataReference, Microsoft.CodeAnalysis, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.MetadataReference, Microsoft.CodeAnalysis, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
           9 (2.97 % of base) : 1866.dasm - System.IO.PathInternal:NormalizeDirectorySeparators(System.String):System.String
           9 (2.97 % of base) : 4021.dasm - System.TimeZoneInfo:TryConvertIanaIdToWindowsId(System.String,bool,byref):bool
           7 (2.58 % of base) : 5661.dasm - Sigil.Impl.LinqAlternative:Each(System.Collections.Generic.IEnumerable`1[__Canon],System.Action`1[__Canon])
           2 (2.56 % of base) : 21459.dasm - System.Net.Http.HttpConnection:EqualsOrdinal(System.String,System.ReadOnlySpan`1[Byte]):bool
           7 (2.40 % of base) : 2553.dasm - System.Reflection.SignatureConstructedGenericType:.ctor(System.Type,System.Type[]):this
           9 (1.82 % of base) : 1340.dasm - Microsoft.Extensions.Options.OptionsFactory`1[__Canon][System.__Canon]:Create(System.String):System.__Canon:this
          27 (1.59 % of base) : 1958.dasm - MemberInfoCache`1[__Canon][System.__Canon]:PopulateInterfaces(Filter):System.RuntimeType[]:this

Top method improvements (percentages):
        -331 (-69.83 % of base) : 23429.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
        -506 (-69.51 % of base) : 15477.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -2295 (-66.93 % of base) : 26379.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
        -253 (-66.23 % of base) : 12837.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
       -2066 (-66.07 % of base) : 17716.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -166 (-64.84 % of base) : 24144.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -518 (-64.75 % of base) : 26722.dasm - Benchstone.BenchF.InProd:Test():bool:this
        -202 (-64.54 % of base) : 23428.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
       -1066 (-62.23 % of base) : 14307.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -243 (-60.90 % of base) : 25026.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -291 (-60.75 % of base) : 15262.dasm - SciMark2.SparseCompRow:matmult(System.Double[],System.Double[],System.Int32[],System.Int32[],System.Double[],int)
        -235 (-60.10 % of base) : 25025.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
        -887 (-59.41 % of base) : 26836.dasm - Benchstone.BenchF.InvMt:Test():bool:this
        -622 (-58.96 % of base) : 17715.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
       -1068 (-58.62 % of base) : 14424.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -802 (-58.12 % of base) : 9003.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -204 (-57.14 % of base) : 26723.dasm - Benchstone.BenchF.InProd:Inner(System.Double[][],System.Double[][],System.Double[][])
        -220 (-56.12 % of base) : 22532.dasm - Benchstone.BenchI.AddArray2:BenchInner1(System.Int32[][],byref)
        -304 (-55.88 % of base) : 23426.dasm - Benchstone.BenchI.Array2:Bench(int):bool
        -372 (-54.23 % of base) : 14285.dasm - SciMark2.SOR:execute(double,System.Double[][],int)

279 total methods with Code Size differences (241 improved, 38 regressed), 20 unchanged.

```

</details>

--------------------------------------------------------------------------------

## coreclr_tests.pmi.Linux.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 123163091 (overridden on cmd)
Total bytes of diff: 123112407 (overridden on cmd)
Total bytes of delta: -50684 (-0.04 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         646 : 218146.dasm (2.50 % of base)
         538 : 218847.dasm (1.83 % of base)
         313 : 216267.dasm (1.87 % of base)
         307 : 221904.dasm (1.62 % of base)
         258 : 215380.dasm (2.89 % of base)
         169 : 241440.dasm (13.51 % of base)
         140 : 232009.dasm (2.59 % of base)
         139 : 226163.dasm (3.19 % of base)
         122 : 226729.dasm (2.58 % of base)
         116 : 224537.dasm (2.48 % of base)
         116 : 225254.dasm (2.50 % of base)
          78 : 227390.dasm (1.86 % of base)
          78 : 229797.dasm (1.82 % of base)
          77 : 228349.dasm (3.47 % of base)
          65 : 225762.dasm (2.84 % of base)
          38 : 223232.dasm (1.76 % of base)
           8 : 244788.dasm (2.02 % of base)
           8 : 141810.dasm (1.56 % of base)
           7 : 180432.dasm (2.79 % of base)
           6 : 231795.dasm (11.54 % of base)

Top file improvements (bytes):
       -2422 : 246758.dasm (-53.13 % of base)
       -2295 : 253000.dasm (-66.93 % of base)
       -2066 : 200405.dasm (-66.07 % of base)
       -1438 : 253238.dasm (-62.66 % of base)
       -1068 : 200336.dasm (-58.62 % of base)
       -1066 : 200326.dasm (-62.23 % of base)
        -887 : 82927.dasm (-59.41 % of base)
        -872 : 250151.dasm (-46.56 % of base)
        -872 : 250189.dasm (-46.56 % of base)
        -722 : 251541.dasm (-48.78 % of base)
        -622 : 200403.dasm (-58.96 % of base)
        -606 : 250152.dasm (-64.13 % of base)
        -606 : 250153.dasm (-64.13 % of base)
        -606 : 250190.dasm (-64.13 % of base)
        -606 : 250191.dasm (-64.13 % of base)
        -560 : 247097.dasm (-23.61 % of base)
        -557 : 245531.dasm (-24.30 % of base)
        -557 : 248031.dasm (-24.30 % of base)
        -550 : 200386.dasm (-42.24 % of base)
        -545 : 200306.dasm (-29.72 % of base)

360 total files with Code Size differences (318 improved, 42 regressed), 11 unchanged.

Top method regressions (bytes):
         646 (2.50 % of base) : 218146.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         538 (1.83 % of base) : 218847.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         313 (1.87 % of base) : 216267.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         307 (1.62 % of base) : 221904.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         258 (2.89 % of base) : 215380.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         169 (13.51 % of base) : 241440.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
         140 (2.59 % of base) : 232009.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         139 (3.19 % of base) : 226163.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         122 (2.58 % of base) : 226729.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         116 (2.48 % of base) : 224537.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         116 (2.50 % of base) : 225254.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          78 (1.86 % of base) : 227390.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          78 (1.82 % of base) : 229797.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          77 (3.47 % of base) : 228349.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          65 (2.84 % of base) : 225762.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          38 (1.76 % of base) : 223232.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
           8 (2.02 % of base) : 244788.dasm - Dynamo.Dynamo:Compare():bool:this
           8 (1.56 % of base) : 141810.dasm - testout1:Main():int
           7 (2.79 % of base) : 180432.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
           6 (10.53 % of base) : 252951.dasm - SimpleArray_01.Test:Test2()

Top method improvements (bytes):
       -2422 (-53.13 % of base) : 246758.dasm - jaggedarr:gaussj(System.Double[,][],int,System.Double[,][],int)
       -2295 (-66.93 % of base) : 253000.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -2066 (-66.07 % of base) : 200405.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1438 (-62.66 % of base) : 253238.dasm - Complex_Array_Test:Main(System.String[]):int
       -1068 (-58.62 % of base) : 200336.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
       -1066 (-62.23 % of base) : 200326.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -887 (-59.41 % of base) : 82927.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -872 (-46.56 % of base) : 250151.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -872 (-46.56 % of base) : 250189.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -722 (-48.78 % of base) : 251541.dasm - Simple_Array_Test:Main(System.String[]):int
        -622 (-58.96 % of base) : 200403.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -606 (-64.13 % of base) : 250152.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250190.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250153.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250191.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -560 (-23.61 % of base) : 247097.dasm - classarr:gaussj(MatrixCls,int,MatrixCls,int)
        -557 (-24.30 % of base) : 248031.dasm - plainarr:gaussj(System.Double[,],int,System.Double[,],int)
        -557 (-24.30 % of base) : 245531.dasm - structarr:gaussj(MatrixStruct,int,MatrixStruct,int)
        -550 (-42.24 % of base) : 200386.dasm - Fourier:DoFPUTransIteration(System.Double[],System.Double[],int):long
        -545 (-29.72 % of base) : 200306.dasm - Huffman:DoHuffIteration(System.Byte[],System.Byte[],System.Byte[],int,int,huff_node[]):long

Top method regressions (percentages):
         169 (13.51 % of base) : 241440.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
           6 (11.54 % of base) : 231793.dasm - test:test_001b()
           6 (11.54 % of base) : 231795.dasm - test:test_002b()
           6 (11.54 % of base) : 231771.dasm - test:test_021b()
           6 (11.54 % of base) : 231773.dasm - test:test_022b()
           6 (10.53 % of base) : 252951.dasm - SimpleArray_01.Test:Test2()
           3 (5.17 % of base) : 240279.dasm - BenchmarksGame.Fasta_1:MakeCumulative(BenchmarksGame.Fasta_1+Frequency[])
          77 (3.47 % of base) : 228349.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         139 (3.19 % of base) : 226163.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         258 (2.89 % of base) : 215380.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          65 (2.84 % of base) : 225762.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
           7 (2.79 % of base) : 180432.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
         140 (2.59 % of base) : 232009.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         122 (2.58 % of base) : 226729.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         116 (2.50 % of base) : 225254.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         646 (2.50 % of base) : 218146.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         116 (2.48 % of base) : 224537.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
           8 (2.02 % of base) : 244788.dasm - Dynamo.Dynamo:Compare():bool:this
         313 (1.87 % of base) : 216267.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          78 (1.86 % of base) : 227390.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int

Top method improvements (percentages):
        -331 (-69.83 % of base) : 84717.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
        -506 (-69.51 % of base) : 84486.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -2295 (-66.93 % of base) : 253000.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
        -253 (-66.23 % of base) : 82407.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
       -2066 (-66.07 % of base) : 200405.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -202 (-64.54 % of base) : 84716.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
        -606 (-64.13 % of base) : 250152.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250190.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250153.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -606 (-64.13 % of base) : 250191.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -481 (-63.71 % of base) : 252781.dasm - Benchstone.BenchF.InProd:Bench():bool
       -1438 (-62.66 % of base) : 253238.dasm - Complex_Array_Test:Main(System.String[]):int
       -1066 (-62.23 % of base) : 200326.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -243 (-60.90 % of base) : 253223.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -291 (-60.75 % of base) : 229619.dasm - SciMark2.SparseCompRow:matmult(System.Double[],System.Double[],System.Int32[],System.Int32[],System.Double[],int)
        -126 (-60.58 % of base) : 82602.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -235 (-60.10 % of base) : 253222.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
        -887 (-59.41 % of base) : 82927.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -622 (-58.96 % of base) : 200403.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -194 (-58.79 % of base) : 229597.dasm - SciMark2.kernel:matvec(System.Double[][],System.Double[],System.Double[])

360 total methods with Code Size differences (318 improved, 42 regressed), 11 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.crossgen2.Linux.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 33071116 (overridden on cmd)
Total bytes of diff: 32965188 (overridden on cmd)
Total bytes of delta: -105928 (-0.32 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          67 : 154999.dasm (4.60 % of base)
          45 : 192678.dasm (1.28 % of base)
          44 : 158036.dasm (3.26 % of base)
          43 : 52970.dasm (17.55 % of base)
          35 : 205700.dasm (1.60 % of base)
          29 : 42235.dasm (0.60 % of base)
          28 : 172247.dasm (0.96 % of base)
          27 : 120642.dasm (11.64 % of base)
          27 : 192564.dasm (1.67 % of base)
          25 : 188284.dasm (1.03 % of base)
          25 : 203576.dasm (1.03 % of base)
          24 : 13762.dasm (9.80 % of base)
          23 : 112909.dasm (9.43 % of base)
          23 : 204235.dasm (8.36 % of base)
          22 : 76803.dasm (4.48 % of base)
          22 : 45642.dasm (7.77 % of base)
          21 : 197124.dasm (14.09 % of base)
          21 : 152682.dasm (6.46 % of base)
          21 : 41548.dasm (10.61 % of base)
          21 : 195886.dasm (10.61 % of base)

Top file improvements (bytes):
      -11458 : 62916.dasm (-45.71 % of base)
       -4389 : 57751.dasm (-42.51 % of base)
       -2046 : 62903.dasm (-54.47 % of base)
       -1941 : 62914.dasm (-41.50 % of base)
       -1242 : 62998.dasm (-47.31 % of base)
       -1064 : 63001.dasm (-44.78 % of base)
       -1004 : 130926.dasm (-42.29 % of base)
        -995 : 62910.dasm (-49.04 % of base)
        -872 : 152949.dasm (-37.70 % of base)
        -800 : 31256.dasm (-41.43 % of base)
        -727 : 62908.dasm (-46.63 % of base)
        -674 : 63296.dasm (-25.10 % of base)
        -621 : 45107.dasm (-33.35 % of base)
        -613 : 152948.dasm (-27.98 % of base)
        -602 : 57749.dasm (-33.33 % of base)
        -589 : 158716.dasm (-46.52 % of base)
        -587 : 63288.dasm (-27.05 % of base)
        -578 : 89844.dasm (-44.74 % of base)
        -567 : 171321.dasm (-46.74 % of base)
        -549 : 2560.dasm (-40.19 % of base)

880 total files with Code Size differences (767 improved, 113 regressed), 86 unchanged.

Top method regressions (bytes):
          67 (4.60 % of base) : 154999.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          45 (1.28 % of base) : 192678.dasm - System.DirectoryServices.Protocols.LdapConnection:SendRequestHelper(System.DirectoryServices.Protocols.DirectoryRequest,byref):int:this
          44 (3.26 % of base) : 158036.dasm - System.Xml.Xsl.Runtime.XmlQueryStaticData:GetObjectData(byref,byref):this
          43 (17.55 % of base) : 52970.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          35 (1.60 % of base) : 205700.dasm - R2RTest.BuildFolder:FromDirectory(System.String,System.Collections.Generic.IEnumerable`1[R2RTest.CompilerRunner],System.String,R2RTest.BuildOptions):R2RTest.BuildFolder
          29 (0.60 % of base) : 42235.dasm - System.Diagnostics.Tracing.ManifestBuilder:CreateManifestString():System.String:this
          28 (0.96 % of base) : 172247.dasm - Internal.IL.MethodILDebugView:get_Disassembly():System.String:this
          27 (11.64 % of base) : 120642.dasm - Scope:IsAllowedType(System.Type):bool:this
          27 (1.67 % of base) : 192564.dasm - System.DirectoryServices.Protocols.LdapSessionOptions:StartTransportLayerSecurity(System.DirectoryServices.Protocols.DirectoryControlCollection):this
          25 (1.03 % of base) : 203576.dasm - System.Resources.Extensions.PreserializedResourceWriter:Generate():this
          25 (1.03 % of base) : 188284.dasm - System.Resources.ResourceWriter:Generate():this
          24 (9.80 % of base) : 13762.dasm - System.Data.Common.DBConnectionString:IsSupersetOf(System.Data.Common.DBConnectionString):bool:this
          23 (9.43 % of base) : 112909.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          23 (8.36 % of base) : 204235.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          22 (4.48 % of base) : 76803.dasm - System.Drawing.Pen:set_DashPattern(System.Single[]):this
          22 (7.77 % of base) : 45642.dasm - System.Reflection.SignatureConstructedGenericType:.ctor(System.Type,System.Type[]):this
          21 (14.09 % of base) : 197124.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          21 (10.61 % of base) : 41548.dasm - System.Collections.HashHelpers:GetPrime(int):int
          21 (10.61 % of base) : 195886.dasm - System.Collections.HashHelpers:GetPrime(int):int
          21 (6.46 % of base) : 152682.dasm - System.Reflection.TypeLoading.Assignability:ProvablyAGcReferenceTypeHelper(System.Type,System.Reflection.TypeLoading.CoreTypes):bool

Top method improvements (bytes):
      -11458 (-45.71 % of base) : 62916.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -4389 (-42.51 % of base) : 57751.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2046 (-54.47 % of base) : 62903.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
       -1941 (-41.50 % of base) : 62914.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1242 (-47.31 % of base) : 62998.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
       -1064 (-44.78 % of base) : 63001.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
       -1004 (-42.29 % of base) : 130926.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
        -995 (-49.04 % of base) : 62910.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -872 (-37.70 % of base) : 152949.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -800 (-41.43 % of base) : 31256.dasm - Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter:UseTwiceLateInvocation(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.VisualBasic.BoundLateInvocation,Microsoft.CodeAnalysis.ArrayBuilder`1[Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedLocal]):Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter+Result
        -727 (-46.63 % of base) : 62908.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -674 (-25.10 % of base) : 63296.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:CanMatchArguments(Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],System.Type[],bool,System.Collections.Generic.List`1[System.String]):bool
        -621 (-33.35 % of base) : 45107.dasm - System.Reflection.Emit.MethodBuilder:CreateMethodBodyHelper(System.Reflection.Emit.ILGenerator):this
        -613 (-27.98 % of base) : 152948.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -602 (-33.33 % of base) : 57749.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -589 (-46.52 % of base) : 158716.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -587 (-27.05 % of base) : 63288.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:MoreSpecificProcedure(Microsoft.VisualBasic.CompilerServices.Symbols+Method,Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],int,byref,bool):Microsoft.VisualBasic.CompilerServices.Symbols+Method
        -578 (-44.74 % of base) : 89844.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -567 (-46.74 % of base) : 171321.dasm - Internal.TypeSystem.RuntimeDeterminedTypeUtilities:ConvertInstantiationToSharedRuntimeForm(Internal.TypeSystem.Instantiation,Internal.TypeSystem.Instantiation,byref):Internal.TypeSystem.Instantiation
        -549 (-40.19 % of base) : 2560.dasm - Internal.Cryptography.Pal.OpenSslX509ChainProcessor:BuildChainElements(Internal.Cryptography.Pal.OpenSslX509ChainProcessor+WorkingChain,byref):System.Security.Cryptography.X509Certificates.X509ChainElement[]:this

Top method regressions (percentages):
          17 (26.56 % of base) : 209143.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          43 (17.55 % of base) : 52970.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          21 (14.09 % of base) : 197124.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          19 (12.03 % of base) : 150639.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          27 (11.64 % of base) : 120642.dasm - Scope:IsAllowedType(System.Type):bool:this
          15 (11.45 % of base) : 112427.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[Microsoft.CodeAnalysis.CommonReferenceManager`2+AssemblyReferenceBinding[System.__Canon, System.__Canon][]]):bool
          21 (10.61 % of base) : 41548.dasm - System.Collections.HashHelpers:GetPrime(int):int
          21 (10.61 % of base) : 195886.dasm - System.Collections.HashHelpers:GetPrime(int):int
          24 (9.80 % of base) : 13762.dasm - System.Data.Common.DBConnectionString:IsSupersetOf(System.Data.Common.DBConnectionString):bool:this
          15 (9.43 % of base) : 192878.dasm - System.DirectoryServices.Protocols.DirectoryAttributeModificationCollection:AddRange(System.DirectoryServices.Protocols.DirectoryAttributeModification[]):this
          23 (9.43 % of base) : 112909.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          13 (9.29 % of base) : 9360.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          15 (8.88 % of base) : 192780.dasm - System.DirectoryServices.Protocols.DirectoryControlCollection:AddRange(System.DirectoryServices.Protocols.DirectoryControl[]):this
          23 (8.36 % of base) : 204235.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          18 (8.11 % of base) : 171469.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          22 (7.77 % of base) : 45642.dasm - System.Reflection.SignatureConstructedGenericType:.ctor(System.Type,System.Type[]):this
          16 (7.37 % of base) : 155005.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderILGen+Member,System.String):this
           8 (7.34 % of base) : 36861.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CRC32:Crc32Update(int,System.Byte[]):int
          14 (7.29 % of base) : 169635.dasm - System.Net.HttpListenerResponse:set_StatusDescription(System.String):this
          13 (7.26 % of base) : 189485.dasm - System.Net.ContextFlagsAdapterPal:GetContextFlagsPalFromInterop(int,bool):int

Top method improvements (percentages):
       -2046 (-54.47 % of base) : 62903.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
        -223 (-52.35 % of base) : 105792.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,Microsoft.CodeAnalysis.CSharp.OverloadResolution+ParameterMap):System.Nullable`1[System.Int32]
        -218 (-51.54 % of base) : 129561.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
        -382 (-50.93 % of base) : 63633.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
        -389 (-50.06 % of base) : 53080.dasm - System.Net.WebUtility:GetEncodedBytes(System.Byte[],int,int,System.Byte[])
        -995 (-49.04 % of base) : 62910.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -359 (-48.45 % of base) : 122442.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -278 (-47.68 % of base) : 121307.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
       -1242 (-47.31 % of base) : 62998.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
        -242 (-47.27 % of base) : 25144.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
        -567 (-46.74 % of base) : 171321.dasm - Internal.TypeSystem.RuntimeDeterminedTypeUtilities:ConvertInstantiationToSharedRuntimeForm(Internal.TypeSystem.Instantiation,Internal.TypeSystem.Instantiation,byref):Internal.TypeSystem.Instantiation
        -727 (-46.63 % of base) : 62908.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -589 (-46.52 % of base) : 158716.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -279 (-46.50 % of base) : 122637.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this
         -53 (-46.09 % of base) : 64038.dasm - ReplaceEscapeSequenceRule:HexToInt32(System.Char[]):int
      -11458 (-45.71 % of base) : 62916.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
        -384 (-45.12 % of base) : 2590.dasm - <>c:<get_Extensions>b__65_0(Microsoft.Win32.SafeHandles.SafeX509Handle):System.Security.Cryptography.X509Certificates.X509Extension[]:this
       -1064 (-44.78 % of base) : 63001.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
        -578 (-44.74 % of base) : 89844.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -372 (-44.71 % of base) : 196440.dasm - System.Web.Util.HttpEncoder:UrlEncodeUnicode(System.String):System.String

880 total methods with Code Size differences (767 improved, 113 regressed), 86 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.pmi.Linux.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 43371880 (overridden on cmd)
Total bytes of diff: 43214381 (overridden on cmd)
Total bytes of delta: -157499 (-0.36 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         126 : 203808.dasm (3.53 % of base)
          80 : 145225.dasm (6.49 % of base)
          79 : 149996.dasm (2.81 % of base)
          70 : 51308.dasm (4.62 % of base)
          67 : 50668.dasm (4.12 % of base)
          55 : 150093.dasm (2.28 % of base)
          52 : 49913.dasm (1.21 % of base)
          46 : 48286.dasm (2.43 % of base)
          45 : 137798.dasm (3.09 % of base)
          40 : 97990.dasm (2.85 % of base)
          37 : 203578.dasm (9.74 % of base)
          30 : 3606.dasm (7.87 % of base)
          29 : 55860.dasm (10.36 % of base)
          28 : 25994.dasm (1.45 % of base)
          28 : 55853.dasm (10.69 % of base)
          27 : 178204.dasm (2.47 % of base)
          26 : 39785.dasm (1.66 % of base)
          25 : 221261.dasm (2.74 % of base)
          24 : 51608.dasm (0.59 % of base)
          24 : 178497.dasm (9.38 % of base)

Top file improvements (bytes):
      -10227 : 138784.dasm (-41.78 % of base)
       -2427 : 61465.dasm (-48.46 % of base)
       -2371 : 58682.dasm (-43.34 % of base)
       -2055 : 63886.dasm (-51.11 % of base)
       -1851 : 138786.dasm (-38.50 % of base)
       -1808 : 4985.dasm (-47.00 % of base)
       -1782 : 61467.dasm (-52.24 % of base)
       -1777 : 82445.dasm (-47.45 % of base)
       -1728 : 65655.dasm (-48.12 % of base)
       -1682 : 65528.dasm (-23.45 % of base)
       -1649 : 65581.dasm (-33.49 % of base)
       -1586 : 63883.dasm (-52.05 % of base)
       -1531 : 56528.dasm (-42.16 % of base)
       -1493 : 64936.dasm (-46.60 % of base)
       -1416 : 81316.dasm (-24.93 % of base)
       -1389 : 81366.dasm (-24.68 % of base)
       -1351 : 9707.dasm (-50.94 % of base)
       -1298 : 22926.dasm (-50.86 % of base)
       -1283 : 63484.dasm (-41.16 % of base)
       -1256 : 64551.dasm (-27.15 % of base)

1040 total files with Code Size differences (907 improved, 133 regressed), 89 unchanged.

Top method regressions (bytes):
         126 (3.53 % of base) : 203808.dasm - System.DirectoryServices.Protocols.LdapConnection:SendRequestHelper(System.DirectoryServices.Protocols.DirectoryRequest,byref):int:this
          80 (6.49 % of base) : 145225.dasm - System.Data.DataColumn:HandleDependentColumnList(System.Data.DataExpression,System.Data.DataExpression):this
          79 (2.81 % of base) : 149996.dasm - System.Data.Common.SqlInt64Storage:Aggregate(System.Int32[],int):System.Object:this
          70 (4.62 % of base) : 51308.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          67 (4.12 % of base) : 50668.dasm - System.Xml.Serialization.XmlAttributes:.ctor(System.Reflection.ICustomAttributeProvider):this
          55 (2.28 % of base) : 150093.dasm - System.Data.Common.SqlByteStorage:Aggregate(System.Int32[],int):System.Object:this
          52 (1.21 % of base) : 49913.dasm - System.Xml.Serialization.SchemaGraph:Depends(System.Xml.Schema.XmlSchemaObject,System.Collections.ArrayList):this
          46 (2.43 % of base) : 48286.dasm - System.Xml.Xsl.Runtime.XmlQueryStaticData:GetObjectData(byref,byref):this
          45 (3.09 % of base) : 137798.dasm - Microsoft.VisualBasic.CompilerServices.ConversionResolution:ClassifyPredefinedCLRConversion(System.Type,System.Type):byte
          40 (2.85 % of base) : 97990.dasm - DebugViewPrinter:Analyze():this
          37 (9.74 % of base) : 203578.dasm - System.DirectoryServices.Protocols.DirectoryAttribute:AddRange(System.Object[]):this
          30 (7.87 % of base) : 3606.dasm - <OrderBy>d__3`1[Byte][System.Byte]:MoveNext():bool:this
          29 (10.36 % of base) : 55860.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          28 (1.45 % of base) : 25994.dasm - Microsoft.CodeAnalysis.CSharp.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.CSharp.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
          28 (10.69 % of base) : 55853.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:LowerBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.CType,Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType):bool:this
          27 (2.47 % of base) : 178204.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          26 (1.66 % of base) : 39785.dasm - System.Xml.XmlEventCache:EventsToWriter(System.Xml.XmlWriter):this
          25 (2.74 % of base) : 221261.dasm - System.Diagnostics.XmlWriterTraceListener:WriteEscaped(System.String):this
          24 (9.38 % of base) : 178497.dasm - System.Data.Common.DBConnectionString:IsSupersetOf(System.Data.Common.DBConnectionString):bool:this
          24 (0.59 % of base) : 51608.dasm - System.Xml.Serialization.XmlSerializationWriterILGen:WriteEnumMethod(System.Xml.Serialization.EnumMapping):this

Top method improvements (bytes):
      -10227 (-41.78 % of base) : 138784.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2427 (-48.46 % of base) : 61465.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
       -2371 (-43.34 % of base) : 58682.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportUnspecificProcedures(Microsoft.CodeAnalysis.Location,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.DiagnosticBag,bool):this
       -2055 (-51.11 % of base) : 63886.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1851 (-38.50 % of base) : 138786.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1808 (-47.00 % of base) : 4985.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:IsApplicable(Microsoft.CodeAnalysis.CSharp.Symbol,EffectiveParameters,Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,System.Collections.Immutable.ImmutableArray`1[Int32],bool,bool,bool,byref):Microsoft.CodeAnalysis.CSharp.MemberAnalysisResult:this
       -1782 (-52.24 % of base) : 61467.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1777 (-47.45 % of base) : 82445.dasm - AsyncMethodToClassRewriter:RewriteSpillSequenceIntoBlock(Microsoft.CodeAnalysis.VisualBasic.BoundSpillSequence,bool,Microsoft.CodeAnalysis.VisualBasic.BoundStatement[]):Microsoft.CodeAnalysis.VisualBasic.BoundBlock:this
       -1728 (-48.12 % of base) : 65655.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
       -1682 (-23.45 % of base) : 65528.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateCallOrGet(Microsoft.CodeAnalysis.VisualBasic.BoundLateMemberAccess,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1649 (-33.49 % of base) : 65581.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateMakeArgumentArrayArgument(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1586 (-52.05 % of base) : 63883.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
       -1531 (-42.16 % of base) : 56528.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
       -1493 (-46.60 % of base) : 64936.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbol:GetAttributesToBind(Roslyn.Utilities.OneOrMany`1[[Microsoft.CodeAnalysis.SyntaxList`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], Microsoft.CodeAnalysis, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],int,Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation,byref):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]:this
       -1416 (-24.93 % of base) : 81316.dasm - AnonymousDelegatePublicSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1389 (-24.68 % of base) : 81366.dasm - AnonymousDelegateTemplateSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1351 (-50.94 % of base) : 9707.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:RewriteArgumentsForComCall(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.Symbols.LocalSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1298 (-50.86 % of base) : 22926.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
       -1283 (-41.16 % of base) : 63484.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution:PrivateAdjustForConstruct(Microsoft.CodeAnalysis.ArrayBuilder`1[[System.Collections.Generic.KeyValuePair`2[[Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35],[Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeWithModifiers, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution):bool
       -1256 (-27.15 % of base) : 64551.dasm - Microsoft.CodeAnalysis.VisualBasic.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol,System.Predicate`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this

Top method regressions (percentages):
          17 (26.56 % of base) : 166015.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          12 (17.65 % of base) : 2623.dasm - System.Reflection.Metadata.Ecma335.MetadataBuilder:ChooseSeparator(System.String):ushort
          21 (13.46 % of base) : 214039.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          12 (11.21 % of base) : 25440.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.MetadataDecoder:GetIndexOfReferencedAssembly(Microsoft.CodeAnalysis.AssemblyIdentity):int:this
          13 (10.74 % of base) : 136605.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          28 (10.69 % of base) : 55853.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:LowerBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.CType,Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType):bool:this
          29 (10.36 % of base) : 55860.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          10 (10.31 % of base) : 141095.dasm - System.ByteArrayHelpers:EqualsOrdinalAsciiIgnoreCase(System.String,System.ReadOnlySpan`1[Byte]):bool
          37 (9.74 % of base) : 203578.dasm - System.DirectoryServices.Protocols.DirectoryAttribute:AddRange(System.Object[]):this
          24 (9.38 % of base) : 178497.dasm - System.Data.Common.DBConnectionString:IsSupersetOf(System.Data.Common.DBConnectionString):bool:this
          12 (9.30 % of base) : 206096.dasm - System.IO.IsolatedStorage.IsolatedStorageFile:GetFullPath(System.String):System.String:this
          23 (8.98 % of base) : 51217.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(Member,System.String):this
          30 (7.87 % of base) : 3606.dasm - <OrderBy>d__3`1[Byte][System.Byte]:MoveNext():bool:this
          18 (7.53 % of base) : 164912.dasm - System.Collections.HashHelpers:GetPrime(int):int
          12 (7.45 % of base) : 26142.dasm - QueryTranslationState:RangeVariableMap(Microsoft.CodeAnalysis.CSharp.Symbols.RangeVariableSymbol[]):RangeVariableMap
           8 (7.21 % of base) : 3749.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          23 (6.65 % of base) : 82692.dasm - TriviaChecker:IsInvalidTrivia(Microsoft.CodeAnalysis.GreenNode):bool
          80 (6.49 % of base) : 145225.dasm - System.Data.DataColumn:HandleDependentColumnList(System.Data.DataExpression,System.Data.DataExpression):this
          14 (6.48 % of base) : 51302.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
           8 (6.30 % of base) : 60368.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CRC32:Crc32Update(int,System.Byte[]):int

Top method improvements (percentages):
        -560 (-54.32 % of base) : 4967.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,ParameterMap):System.Nullable`1[Int32]
       -1782 (-52.24 % of base) : 61467.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1586 (-52.05 % of base) : 63883.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
        -885 (-51.39 % of base) : 83533.dasm - InferenceGraph:PopulateGraph():this
       -2055 (-51.11 % of base) : 63886.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1096 (-51.00 % of base) : 9699.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1351 (-50.94 % of base) : 9707.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:RewriteArgumentsForComCall(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.Symbols.LocalSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1298 (-50.86 % of base) : 22926.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
        -368 (-50.69 % of base) : 138063.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
       -1056 (-50.67 % of base) : 23361.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.CustomModifierUtils:CopyParameterCustomModifiers(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -511 (-50.25 % of base) : 26114.dasm - Microsoft.CodeAnalysis.CSharp.CodeGen.StackOptimizerPass1:VisitArguments(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]:this
       -1054 (-50.05 % of base) : 138791.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -210 (-49.65 % of base) : 151670.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
         -54 (-49.54 % of base) : 206240.dasm - System.Numerics.Tensors.ArrayUtilities:GetIndex(System.Int32[],System.ReadOnlySpan`1[Int32],int):int
        -438 (-48.99 % of base) : 4428.dasm - Microsoft.CodeAnalysis.CSharp.Binder:GetAttributes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Binder, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.CSharpAttributeData[],Microsoft.CodeAnalysis.DiagnosticBag)
       -2427 (-48.46 % of base) : 61465.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
        -689 (-48.35 % of base) : 150759.dasm - Internal.Cryptography.Pal.UnixPkcs12Reader:FindMatchingKey(System.Security.Cryptography.Asn1.Pkcs12.SafeBagAsn[],int,System.ReadOnlySpan`1[Byte]):int
       -1728 (-48.12 % of base) : 65655.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
        -327 (-47.74 % of base) : 145388.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -455 (-47.49 % of base) : 61798.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CustomModifierUtils:CopyParameterCustomModifiers(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]

1040 total methods with Code Size differences (907 improved, 133 regressed), 89 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries_tests.pmi.Linux.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 110408827 (overridden on cmd)
Total bytes of diff: 110317752 (overridden on cmd)
Total bytes of delta: -91075 (-0.08 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         105 : 112622.dasm (2.32 % of base)
          69 : 28149.dasm (0.36 % of base)
          54 : 334122.dasm (5.04 % of base)
          50 : 285198.dasm (0.66 % of base)
          45 : 285208.dasm (0.53 % of base)
          40 : 193314.dasm (1.65 % of base)
          40 : 139805.dasm (2.64 % of base)
          40 : 35545.dasm (0.79 % of base)
          39 : 213854.dasm (2.39 % of base)
          37 : 29379.dasm (5.85 % of base)
          37 : 108284.dasm (5.67 % of base)
          30 : 212837.dasm (1.53 % of base)
          29 : 192046.dasm (0.29 % of base)
          29 : 176599.dasm (2.86 % of base)
          28 : 150780.dasm (0.58 % of base)
          27 : 127952.dasm (2.47 % of base)
          27 : 217381.dasm (2.47 % of base)
          27 : 318393.dasm (0.64 % of base)
          27 : 135280.dasm (3.56 % of base)
          25 : 300174.dasm (9.73 % of base)

Top file improvements (bytes):
       -1429 : 112433.dasm (-46.71 % of base)
        -779 : 197239.dasm (-25.25 % of base)
        -775 : 52394.dasm (-37.05 % of base)
        -592 : 49335.dasm (-49.01 % of base)
        -566 : 82783.dasm (-18.87 % of base)
        -434 : 245429.dasm (-12.52 % of base)
        -371 : 201964.dasm (-18.86 % of base)
        -358 : 319924.dasm (-23.46 % of base)
        -352 : 319925.dasm (-23.11 % of base)
        -336 : 82787.dasm (-12.96 % of base)
        -336 : 334731.dasm (-35.74 % of base)
        -331 : 201141.dasm (-24.36 % of base)
        -331 : 201713.dasm (-23.33 % of base)
        -326 : 191415.dasm (-47.25 % of base)
        -321 : 314432.dasm (-45.15 % of base)
        -321 : 158926.dasm (-45.15 % of base)
        -313 : 191416.dasm (-44.15 % of base)
        -304 : 153799.dasm (-8.42 % of base)
        -299 : 47864.dasm (-24.65 % of base)
        -292 : 201682.dasm (-14.94 % of base)

1249 total files with Code Size differences (1056 improved, 193 regressed), 74 unchanged.

Top method regressions (bytes):
         105 (2.32 % of base) : 112622.dasm - Microsoft.Build.Tasks.ResolveAssemblyReference:LogInputs():this
          69 (0.36 % of base) : 28149.dasm - <ArrayAsRootObject>d__7:MoveNext():this
          54 (5.04 % of base) : 334122.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          50 (0.66 % of base) : 285198.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscovery:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          45 (0.53 % of base) : 285208.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscoveryV1:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          40 (2.64 % of base) : 139805.dasm - Microsoft.VisualBasic.FileIO.Tests.FileSystemTests:CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse():this
          40 (1.65 % of base) : 193314.dasm - System.Collections.Concurrent.Tests.BlockingCollectionTests:AddAnyTakeAny(int,int,int,System.Collections.Concurrent.BlockingCollection`1[Int32],System.Collections.Concurrent.BlockingCollection`1[System.Int32][],int)
          40 (0.79 % of base) : 35545.dasm - System.Reflection.Tests.SignatureTypeTests:TestSignatureTypeInvariants(System.Type)
          39 (2.39 % of base) : 213854.dasm - System.Data.Tests.DataTableTest2:Compute():this
          37 (5.85 % of base) : 29379.dasm - DataContractSerializerTests:DCS_InvalidDataContract_Read_Invalid_Types_Throws()
          37 (5.67 % of base) : 108284.dasm - DataContractSerializerTests:DCS_InvalidDataContract_Read_Invalid_Types_Throws()
          30 (1.53 % of base) : 212837.dasm - System.Data.Tests.DataProvider:GetDSSchema(System.Data.DataSet):System.String
          29 (0.29 % of base) : 192046.dasm - <>c__DisplayClass40_0:<UseInstance>b__0(Registry):Registry:this
          29 (2.86 % of base) : 176599.dasm - System.Threading.Tasks.Dataflow.Tests.ActionBlockTests:TestPost():this
          28 (0.58 % of base) : 150780.dasm - Microsoft.Build.Construction.SolutionProjectGenerator:CreateTraversalInstance(System.String,bool,System.Collections.Generic.List`1[[Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]):Microsoft.Build.Execution.ProjectInstance:this
          27 (3.56 % of base) : 135280.dasm - <>c__DisplayClass1_0:<GetTags>b__2():System.UInt32[]:this
          27 (2.47 % of base) : 127952.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          27 (2.47 % of base) : 217381.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          27 (0.64 % of base) : 318393.dasm - System.IO.Tests.BinaryWriterTests:BinaryWriter_SeekTests():this
          25 (9.73 % of base) : 300174.dasm - LightInject.ServiceContainer:Create(System.Type,LightInject.Scope):System.Object:this

Top method improvements (bytes):
       -1429 (-46.71 % of base) : 112433.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -779 (-25.25 % of base) : 197239.dasm - Expander:VisitParenthesizedLambdaExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax):Microsoft.CodeAnalysis.SyntaxNode:this
        -775 (-37.05 % of base) : 52394.dasm - Microsoft.CodeAnalysis.Shared.Extensions.IMethodSymbolExtensions:RenameTypeParameters(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.Shared.Extensions.ITypeGenerator):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -592 (-49.01 % of base) : 49335.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -566 (-18.87 % of base) : 82783.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddAfter_LLNode():this
        -434 (-12.52 % of base) : 245429.dasm - System.Diagnostics.Tests.DiagnosticSourceEventSourceBridgeTests:<TestEnableAllActivitySourcesWithOneEvent>b__1_0(System.String):this
        -371 (-18.86 % of base) : 201964.dasm - System.SpanTests.SpanTests:TestMatchIndexOfAny_ManyString()
        -358 (-23.46 % of base) : 319924.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[__Canon,__Canon,__Canon,__Canon]
        -352 (-23.11 % of base) : 319925.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60],[System.IServiceProvider, System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a],[System.Threading.Tasks.Task, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        -336 (-12.96 % of base) : 82787.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddBefore_LLNode():this
        -336 (-35.74 % of base) : 334731.dasm - System.Threading.Tests.SpinLockTests:RunSpinLockTest2_TryEnter(int,bool)
        -331 (-24.36 % of base) : 201141.dasm - System.SpanTests.ReadOnlySpanTests:SequenceCompareToNoMatch()
        -331 (-23.33 % of base) : 201713.dasm - System.SpanTests.SpanTests:SequenceCompareToNoMatch()
        -326 (-47.25 % of base) : 191415.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
        -321 (-45.15 % of base) : 314432.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -321 (-45.15 % of base) : 158926.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -313 (-44.15 % of base) : 191416.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool
        -304 (-8.42 % of base) : 153799.dasm - Microsoft.Build.BackEnd.Scheduler:WriteNodeUtilizationGraph(Microsoft.Build.BackEnd.Logging.ILoggingService,Microsoft.Build.Framework.BuildEventContext,bool):this
        -299 (-24.65 % of base) : 47864.dasm - Roslyn.Utilities.SpecializedTasks:WhenAll(System.Collections.Generic.IEnumerable`1[ValueTask`1]):System.Threading.Tasks.ValueTask`1[[System.Byte[], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        -292 (-14.87 % of base) : 201104.dasm - System.SpanTests.ReadOnlySpanTests:OnSequenceEqualOfEqualSpansMakeSureEveryElementIsCompared(int)

Top method regressions (percentages):
          14 (12.50 % of base) : 47357.dasm - Roslyn.Utilities.PathUtilities:PathHashCode(System.String):int
          14 (11.20 % of base) : 32147.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          18 (10.34 % of base) : 156003.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          18 (10.34 % of base) : 114595.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          18 (10.34 % of base) : 283603.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          10 (10.31 % of base) : 157965.dasm - System.ByteArrayHelpers:EqualsOrdinalAsciiIgnoreCase(System.String,System.ReadOnlySpan`1[Byte]):bool
          25 (9.73 % of base) : 300174.dasm - LightInject.ServiceContainer:Create(System.Type,LightInject.Scope):System.Object:this
          18 (8.82 % of base) : 124218.dasm - System.Threading.Tasks.Tests.ContinueWithAllAny.TaskContinueWithAllAnyTest:VerifyAll(System.Threading.Tasks.Task[]):this
          18 (8.82 % of base) : 124219.dasm - System.Threading.Tasks.Tests.ContinueWithAllAny.TaskContinueWithAllAnyTest:VerifyAllT(System.Threading.Tasks.Task`1[System.Double][]):this
           9 (8.41 % of base) : 191778.dasm - <>c__45`1[Byte][System.Byte]:<Visit>b__45_0(ImTools.ImMapEntry`1[KValue`1],bool,System.Action`1[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]]):bool:this
          14 (8.28 % of base) : 32149.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          18 (7.79 % of base) : 336762.dasm - Unity.Utility.Prime:GetPrime(int):int
          22 (7.77 % of base) : 300816.dasm - <>c__DisplayClass3_0`1[Int32][System.Int32]:<CreateScopedLazy>b__0():int:this
          22 (7.77 % of base) : 300802.dasm - <>c__DisplayClass5_0`1[Int32][System.Int32]:<CreateScopedGenericFunc>b__0():int:this
          22 (7.75 % of base) : 300814.dasm - <>c__DisplayClass3_0`1[Byte][System.Byte]:<CreateScopedLazy>b__0():ubyte:this
          22 (7.75 % of base) : 300800.dasm - <>c__DisplayClass5_0`1[Byte][System.Byte]:<CreateScopedGenericFunc>b__0():ubyte:this
          22 (7.64 % of base) : 300817.dasm - <>c__DisplayClass3_0`1[Double][System.Double]:<CreateScopedLazy>b__0():double:this
          22 (7.64 % of base) : 300803.dasm - <>c__DisplayClass5_0`1[Double][System.Double]:<CreateScopedGenericFunc>b__0():double:this
          21 (7.39 % of base) : 300819.dasm - <>c__DisplayClass3_0`1[Int64][System.Int64]:<CreateScopedLazy>b__0():long:this
          21 (7.39 % of base) : 300805.dasm - <>c__DisplayClass5_0`1[Int64][System.Int64]:<CreateScopedGenericFunc>b__0():long:this

Top method improvements (percentages):
        -592 (-49.01 % of base) : 49335.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -326 (-47.25 % of base) : 191415.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
       -1429 (-46.71 % of base) : 112433.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -264 (-45.60 % of base) : 45748.dasm - System.Runtime.Serialization.Formatters.Tests.EqualityHelpers:ArraysAreEqual(System.__Canon[],System.__Canon[]):bool
        -321 (-45.15 % of base) : 314432.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -321 (-45.15 % of base) : 158926.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -313 (-44.15 % of base) : 191416.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool
        -141 (-43.38 % of base) : 47664.dasm - Roslyn.Utilities.EditDistance:ConvertToLowercaseArray(System.String):System.Char[]
         -62 (-43.06 % of base) : 168391.dasm - ChecksumWriter:Write(System.Char[],int,int):this
         -62 (-43.06 % of base) : 173218.dasm - ChecksumWriter:Write(System.Char[],int,int):this
        -201 (-43.04 % of base) : 282946.dasm - System.Xml.Tests.CXmlDriverEngine:FindElementAndRemoveIt(System.String,int,System.Xml.Linq.XElement[],int):System.Xml.Linq.XElement
         -51 (-42.86 % of base) : 305920.dasm - System.IO.Compression.Tests.ZipFileTestBase:ArraysEqual(System.Byte[],System.Byte[],int):bool
         -51 (-42.86 % of base) : 331759.dasm - System.IO.Compression.Tests.ZipFileTestBase:ArraysEqual(System.Byte[],System.Byte[],int):bool
         -69 (-42.86 % of base) : 236545.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 263732.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 293352.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 86410.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 250095.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 314258.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -69 (-42.86 % of base) : 318118.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this

1249 total methods with Code Size differences (1056 improved, 193 regressed), 74 unchanged.

```

</details>

--------------------------------------------------------------------------------


</details>


## windows arm64

<details>

<summary>windows arm64 details</summary>

Summary file: `superpmi_diff_summary_windows_arm64.md`

To reproduce these diffs on windows x64:
```
superpmi.py asmdiffs -target_os windows -target_arch arm64 -arch x64
```

## benchmarks.run.windows.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 7551784 (overridden on cmd)
Total bytes of diff: 7504724 (overridden on cmd)
Total bytes of delta: -47060 (-0.62 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          80 : 3483.dasm (7.27 % of base)
          80 : 7809.dasm (6.94 % of base)
          28 : 6.dasm (8.64 % of base)
          28 : 6510.dasm (8.64 % of base)
          24 : 1666.dasm (1.20 % of base)
          24 : 7212.dasm (2.93 % of base)
          24 : 10334.dasm (0.35 % of base)
          24 : 22001.dasm (3.82 % of base)
          24 : 3569.dasm (1.68 % of base)
          24 : 3772.dasm (4.17 % of base)
          20 : 2263.dasm (5.95 % of base)
          20 : 3533.dasm (7.69 % of base)
          16 : 16164.dasm (0.48 % of base)
          16 : 17005.dasm (0.56 % of base)
          16 : 3229.dasm (3.01 % of base)
          16 : 2857.dasm (5.19 % of base)
          12 : 15306.dasm (1.94 % of base)
          12 : 2655.dasm (0.67 % of base)
          12 : 12961.dasm (0.33 % of base)
          12 : 16148.dasm (6.25 % of base)

Top file improvements (bytes):
       -5056 : 1915.dasm (-43.51 % of base)
       -3060 : 22244.dasm (-68.73 % of base)
       -2436 : 14922.dasm (-69.68 % of base)
       -1484 : 12911.dasm (-58.24 % of base)
       -1476 : 12526.dasm (-60.59 % of base)
       -1064 : 22901.dasm (-58.59 % of base)
        -904 : 6935.dasm (-23.04 % of base)
        -744 : 1003.dasm (-53.76 % of base)
        -672 : 17486.dasm (-46.28 % of base)
        -664 : 14921.dasm (-57.84 % of base)
        -636 : 1283.dasm (-32.32 % of base)
        -632 : 14920.dasm (-45.66 % of base)
        -620 : 962.dasm (-43.30 % of base)
        -540 : 13669.dasm (-43.83 % of base)
        -520 : 13961.dasm (-69.15 % of base)
        -508 : 1745.dasm (-29.88 % of base)
        -504 : 22581.dasm (-61.17 % of base)
        -452 : 16765.dasm (-48.09 % of base)
        -444 : 2104.dasm (-38.41 % of base)
        -440 : 16189.dasm (-28.95 % of base)

260 total files with Code Size differences (225 improved, 35 regressed), 62 unchanged.

Top method regressions (bytes):
          80 (6.94 % of base) : 7809.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          80 (7.27 % of base) : 3483.dasm - System.Xml.Serialization.TypeScope:PopulateMemberInfos(System.Xml.Serialization.StructMapping,System.Xml.Serialization.MemberMapping[],System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Reflection.MemberInfo, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]])
          28 (8.64 % of base) : 6.dasm - System.Collections.HashHelpers:GetPrime(int):int
          28 (8.64 % of base) : 6510.dasm - System.Collections.HashHelpers:GetPrime(int):int
          24 (1.20 % of base) : 1666.dasm - MemberInfoCache`1[__Canon][System.__Canon]:PopulateInterfaces(Filter):System.RuntimeType[]:this
          24 (2.93 % of base) : 7212.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_AbsoluteExpiration():this
          24 (3.82 % of base) : 22001.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_ExpirationTokens():this
          24 (4.17 % of base) : 3772.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_NoExpiration():this
          24 (0.35 % of base) : 10334.dasm - ProtoBuf.Meta.MetaType:ApplyDefaultBehaviourImpl(int):this
          24 (1.68 % of base) : 3569.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          20 (5.95 % of base) : 2263.dasm - Sigil.Impl.LinqAlternative:Each(System.Collections.Generic.IEnumerable`1[__Canon],System.Action`1[__Canon])
          20 (7.69 % of base) : 3533.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
          16 (0.56 % of base) : 17005.dasm - Microsoft.CodeAnalysis.CSharp.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol):this
          16 (0.48 % of base) : 16164.dasm - Microsoft.CodeAnalysis.PEModule:GetTargetAttributeSignatureIndex(System.Reflection.Metadata.MetadataReader,System.Reflection.Metadata.CustomAttributeHandle,Microsoft.CodeAnalysis.AttributeDescription):int
          16 (5.19 % of base) : 2857.dasm - Newtonsoft.Json.Utilities.TypeExtensions:AssignableToTypeName(System.Type,System.String,bool,byref):bool
          16 (3.01 % of base) : 3229.dasm - System.Xml.Serialization.TypeScope:TypeName(System.Type):System.String
          12 (0.33 % of base) : 12961.dasm - CriticalHelper:WriteCollection(System.Runtime.Serialization.CollectionDataContract):this
          12 (6.25 % of base) : 16148.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[__Canon]):bool
          12 (1.94 % of base) : 15306.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_RelativeExpiration():this
          12 (1.94 % of base) : 21715.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_SlidingExpiration():this

Top method improvements (bytes):
       -5056 (-43.51 % of base) : 1915.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -3060 (-68.73 % of base) : 22244.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -2436 (-69.68 % of base) : 14922.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1484 (-58.24 % of base) : 12911.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
       -1476 (-60.59 % of base) : 12526.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1064 (-58.59 % of base) : 22901.dasm - Benchstone.BenchF.InvMt:Test():bool:this
        -904 (-23.04 % of base) : 6935.dasm - AutomataNode:EmitSearchNextCore(System.Reflection.Emit.ILGenerator,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Action`1[[System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],System.Action,Utf8Json.Internal.AutomataDictionary+AutomataNode[],int)
        -744 (-53.76 % of base) : 1003.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -672 (-46.28 % of base) : 17486.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool,Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -664 (-57.84 % of base) : 14921.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -636 (-32.32 % of base) : 1283.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -632 (-45.66 % of base) : 14920.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])
        -620 (-43.30 % of base) : 962.dasm - BilinearTest:BilinearInterpol(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]
        -540 (-43.83 % of base) : 13669.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int
        -520 (-69.15 % of base) : 13961.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
        -508 (-29.88 % of base) : 1745.dasm - System.Reflection.Emit.MethodBuilder:CreateMethodBodyHelper(System.Reflection.Emit.ILGenerator):this
        -504 (-61.17 % of base) : 22581.dasm - Benchstone.BenchF.InProd:Test():bool:this
        -452 (-48.09 % of base) : 16765.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArray(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -444 (-38.41 % of base) : 2104.dasm - System.DefaultBinder:FindMostSpecific(System.Reflection.ParameterInfo[],System.Int32[],System.Type,System.Reflection.ParameterInfo[],System.Int32[],System.Type,System.Type[],System.Object[]):int
        -440 (-28.95 % of base) : 16189.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:ReuseAssemblySymbolsWithNoPiaLocalTypes(Microsoft.CodeAnalysis.CommonReferenceManager`2+BoundInputAssembly[System.__Canon,System.__Canon][],System.__Canon[],System.Collections.Immutable.ImmutableArray`1[__Canon],int):bool:this

Top method regressions (percentages):
          28 (8.64 % of base) : 6.dasm - System.Collections.HashHelpers:GetPrime(int):int
          28 (8.64 % of base) : 6510.dasm - System.Collections.HashHelpers:GetPrime(int):int
          12 (8.57 % of base) : 9933.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          20 (7.69 % of base) : 3533.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
          80 (7.27 % of base) : 3483.dasm - System.Xml.Serialization.TypeScope:PopulateMemberInfos(System.Xml.Serialization.StructMapping,System.Xml.Serialization.MemberMapping[],System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Reflection.MemberInfo, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]])
          80 (6.94 % of base) : 7809.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          12 (6.25 % of base) : 16148.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[__Canon]):bool
          20 (5.95 % of base) : 2263.dasm - Sigil.Impl.LinqAlternative:Each(System.Collections.Generic.IEnumerable`1[__Canon],System.Action`1[__Canon])
          16 (5.19 % of base) : 2857.dasm - Newtonsoft.Json.Utilities.TypeExtensions:AssignableToTypeName(System.Type,System.String,bool,byref):bool
          24 (4.17 % of base) : 3772.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_NoExpiration():this
          24 (3.82 % of base) : 22001.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_ExpirationTokens():this
           8 (3.70 % of base) : 23018.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:Stack():System.Collections.Generic.Stack`1[__Canon]:this
           8 (3.39 % of base) : 21769.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:Dictionary():System.Collections.Generic.Dictionary`2[__Canon,__Canon]:this
           4 (3.33 % of base) : 15741.dasm - Roslyn.Utilities.StringTable:TextEqualsCore(System.String,System.Char[],int):bool
           8 (3.33 % of base) : 20197.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Dictionary():System.Collections.Generic.Dictionary`2[Int32,Int32]:this
           8 (3.17 % of base) : 9664.dasm - System.Net.Http.HttpConnection:WriteStringAsync(System.String,bool):System.Threading.Tasks.Task:this
           8 (3.08 % of base) : 21257.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Stack():System.Collections.Generic.Stack`1[Int32]:this
          16 (3.01 % of base) : 3229.dasm - System.Xml.Serialization.TypeScope:TypeName(System.Type):System.String
          24 (2.93 % of base) : 7212.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_AbsoluteExpiration():this
           8 (2.90 % of base) : 4276.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:List():System.Collections.Generic.List`1[__Canon]:this

Top method improvements (percentages):
       -2436 (-69.68 % of base) : 14922.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -520 (-69.15 % of base) : 13961.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -3060 (-68.73 % of base) : 22244.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
        -328 (-63.57 % of base) : 11982.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
        -276 (-61.61 % of base) : 20117.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
        -504 (-61.17 % of base) : 22581.dasm - Benchstone.BenchF.InProd:Test():bool:this
        -348 (-60.84 % of base) : 20118.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
       -1476 (-60.59 % of base) : 12526.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -200 (-59.52 % of base) : 20905.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -276 (-59.48 % of base) : 21180.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -276 (-59.48 % of base) : 21179.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
       -1064 (-58.59 % of base) : 22901.dasm - Benchstone.BenchF.InvMt:Test():bool:this
        -280 (-58.33 % of base) : 19350.dasm - Benchstone.BenchI.AddArray2:BenchInner1(System.Int32[][],byref)
       -1484 (-58.24 % of base) : 12911.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -664 (-57.84 % of base) : 14921.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -312 (-56.12 % of base) : 13171.dasm - SciMark2.SparseCompRow:matmult(System.Double[],System.Double[],System.Int32[],System.Int32[],System.Double[],int)
        -404 (-55.49 % of base) : 12065.dasm - SciMark2.SOR:execute(double,System.Double[][],int)
        -744 (-53.76 % of base) : 1003.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -344 (-53.42 % of base) : 20115.dasm - Benchstone.BenchI.Array2:Bench(int):bool
        -220 (-51.40 % of base) : 19349.dasm - Benchstone.BenchI.AddArray2:Bench(System.Int32[][]):bool

260 total methods with Code Size differences (225 improved, 35 regressed), 62 unchanged.

```

</details>

--------------------------------------------------------------------------------

## coreclr_tests.pmi.windows.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 164977868 (overridden on cmd)
Total bytes of diff: 164912456 (overridden on cmd)
Total bytes of delta: -65412 (-0.04 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          48 : 232858.dasm (10.62 % of base)
          16 : 245519.dasm (4.17 % of base)
          12 : 211916.dasm (4.69 % of base)
          12 : 209090.dasm (1.69 % of base)
          12 : 209190.dasm (4.17 % of base)
           8 : 248267.dasm (0.93 % of base)
           8 : 238847.dasm (0.85 % of base)
           4 : 211918.dasm (1.35 % of base)
           4 : 236846.dasm (0.13 % of base)
           4 : 223231.dasm (0.09 % of base)
           4 : 236844.dasm (0.13 % of base)
           4 : 197245.dasm (0.01 % of base)

Top file improvements (bytes):
       -3060 : 250614.dasm (-68.73 % of base)
       -2520 : 247530.dasm (-52.20 % of base)
       -2436 : 191461.dasm (-69.68 % of base)
       -1692 : 251749.dasm (-64.09 % of base)
       -1484 : 191392.dasm (-58.24 % of base)
       -1476 : 191382.dasm (-60.59 % of base)
       -1064 : 252527.dasm (-58.98 % of base)
        -944 : 219476.dasm (-26.28 % of base)
        -944 : 191362.dasm (-39.07 % of base)
        -944 : 219526.dasm (-26.05 % of base)
        -852 : 246015.dasm (-45.13 % of base)
        -852 : 246056.dasm (-45.13 % of base)
        -836 : 251750.dasm (-48.27 % of base)
        -664 : 191459.dasm (-57.84 % of base)
        -632 : 191460.dasm (-45.66 % of base)
        -628 : 253440.dasm (-51.48 % of base)
        -560 : 248878.dasm (-49.47 % of base)
        -544 : 247521.dasm (-21.28 % of base)
        -536 : 228275.dasm (-43.79 % of base)
        -536 : 247538.dasm (-21.41 % of base)

328 total files with Code Size differences (316 improved, 12 regressed), 24 unchanged.

Top method regressions (bytes):
          48 (10.62 % of base) : 232858.dasm - Dynamo.Dynamo:Compare():bool:this
          16 (4.17 % of base) : 245519.dasm - RuntimeEventListener:Verify():bool:this
          12 (1.69 % of base) : 209090.dasm - Internal.TypeSystem.MetadataRuntimeInterfacesAlgorithm:ComputeRuntimeInterfacesForNonInstantiatedMetadataType(Internal.TypeSystem.MetadataType):Internal.TypeSystem.DefType[]:this
          12 (4.17 % of base) : 209190.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          12 (4.69 % of base) : 211916.dasm - TestLibrary.Utilities:ByteArrayToString(System.Byte[]):System.String
           8 (0.93 % of base) : 248267.dasm - BenchmarksGame.NBodySystem:.ctor():this
           8 (0.85 % of base) : 238847.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
           4 (0.09 % of base) : 223231.dasm - GCSimulator.ClientSimulator:RunTest()
           4 (0.13 % of base) : 236846.dasm - intmm:Main():int
           4 (0.13 % of base) : 236844.dasm - intmm:Main():int
           4 (0.01 % of base) : 197245.dasm - Program:Main(System.String[]):int
           4 (1.35 % of base) : 211918.dasm - TestLibrary.Utilities:FormatHexStringFromUnicodeString(System.String,bool):System.String

Top method improvements (bytes):
       -3060 (-68.73 % of base) : 250614.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -2520 (-52.20 % of base) : 247530.dasm - jaggedarr:gaussj(System.Double[,][],int,System.Double[,][],int)
       -2436 (-69.68 % of base) : 191461.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1692 (-64.09 % of base) : 251749.dasm - Complex_Array_Test:Main(System.String[]):int
       -1484 (-58.24 % of base) : 191392.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
       -1476 (-60.59 % of base) : 191382.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1064 (-58.98 % of base) : 252527.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -944 (-39.07 % of base) : 191362.dasm - Huffman:DoHuffIteration(System.Byte[],System.Byte[],System.Byte[],int,int,huff_node[]):long
        -944 (-26.28 % of base) : 219476.dasm - VectorTest:Main():int
        -944 (-26.05 % of base) : 219526.dasm - VectorTest:Main():int
        -852 (-45.13 % of base) : 246015.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -852 (-45.13 % of base) : 246056.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -836 (-48.27 % of base) : 251750.dasm - Simple_Array_Test:Main(System.String[]):int
        -664 (-57.84 % of base) : 191459.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -632 (-45.66 % of base) : 191460.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])
        -628 (-51.48 % of base) : 253440.dasm - CTest:TestArrays1(int,double)
        -560 (-49.47 % of base) : 248878.dasm - SimpleArray_01.Test:BadMatrixMul2()
        -544 (-21.28 % of base) : 247521.dasm - classarr:gaussj(MatrixCls,int,MatrixCls,int)
        -536 (-21.41 % of base) : 247538.dasm - plainarr:gaussj(System.Double[,],int,System.Double[,],int)
        -536 (-43.79 % of base) : 228275.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int

Top method regressions (percentages):
          48 (10.62 % of base) : 232858.dasm - Dynamo.Dynamo:Compare():bool:this
          12 (4.69 % of base) : 211916.dasm - TestLibrary.Utilities:ByteArrayToString(System.Byte[]):System.String
          12 (4.17 % of base) : 209190.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          16 (4.17 % of base) : 245519.dasm - RuntimeEventListener:Verify():bool:this
          12 (1.69 % of base) : 209090.dasm - Internal.TypeSystem.MetadataRuntimeInterfacesAlgorithm:ComputeRuntimeInterfacesForNonInstantiatedMetadataType(Internal.TypeSystem.MetadataType):Internal.TypeSystem.DefType[]:this
           4 (1.35 % of base) : 211918.dasm - TestLibrary.Utilities:FormatHexStringFromUnicodeString(System.String,bool):System.String
           8 (0.93 % of base) : 248267.dasm - BenchmarksGame.NBodySystem:.ctor():this
           8 (0.85 % of base) : 238847.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
           4 (0.13 % of base) : 236846.dasm - intmm:Main():int
           4 (0.13 % of base) : 236844.dasm - intmm:Main():int
           4 (0.09 % of base) : 223231.dasm - GCSimulator.ClientSimulator:RunTest()
           4 (0.01 % of base) : 197245.dasm - Program:Main(System.String[]):int

Top method improvements (percentages):
       -2436 (-69.68 % of base) : 191461.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -520 (-69.15 % of base) : 252536.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -3060 (-68.73 % of base) : 250614.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -1692 (-64.09 % of base) : 251749.dasm - Complex_Array_Test:Main(System.String[]):int
        -328 (-63.57 % of base) : 252565.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
        -276 (-61.61 % of base) : 252553.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
        -504 (-61.17 % of base) : 250589.dasm - Benchstone.BenchF.InProd:Bench():bool
        -348 (-60.84 % of base) : 252554.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
       -1476 (-60.59 % of base) : 191382.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -276 (-59.48 % of base) : 250578.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -276 (-59.48 % of base) : 250577.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
       -1064 (-58.98 % of base) : 252527.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -176 (-58.67 % of base) : 252582.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -280 (-58.33 % of base) : 252584.dasm - Benchstone.BenchI.AddArray2:BenchInner1(System.Int32[][],byref)
       -1484 (-58.24 % of base) : 191392.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -664 (-57.84 % of base) : 191459.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -532 (-56.12 % of base) : 246016.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 246057.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 246017.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -532 (-56.12 % of base) : 246058.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this

328 total methods with Code Size differences (316 improved, 12 regressed), 24 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.crossgen2.windows.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 51675792 (overridden on cmd)
Total bytes of diff: 51529572 (overridden on cmd)
Total bytes of delta: -146220 (-0.28 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          96 : 11418.dasm (2.33 % of base)
          88 : 29765.dasm (3.17 % of base)
          60 : 207708.dasm (1.28 % of base)
          40 : 44959.dasm (4.42 % of base)
          40 : 197019.dasm (2.78 % of base)
          40 : 160232.dasm (3.05 % of base)
          36 : 22614.dasm (0.89 % of base)
          36 : 40429.dasm (9.00 % of base)
          28 : 29461.dasm (0.42 % of base)
          24 : 53095.dasm (0.12 % of base)
          20 : 83869.dasm (7.04 % of base)
          20 : 83958.dasm (6.85 % of base)
          20 : 166069.dasm (2.07 % of base)
          20 : 142140.dasm (1.69 % of base)
          20 : 165163.dasm (1.87 % of base)
          20 : 41837.dasm (3.52 % of base)
          20 : 220003.dasm (4.24 % of base)
          20 : 77456.dasm (2.49 % of base)
          16 : 192642.dasm (6.78 % of base)
          16 : 83599.dasm (0.26 % of base)

Top file improvements (bytes):
      -12260 : 194028.dasm (-44.63 % of base)
       -5404 : 45275.dasm (-42.74 % of base)
       -2384 : 194015.dasm (-52.74 % of base)
       -1856 : 194026.dasm (-36.54 % of base)
       -1652 : 194110.dasm (-45.94 % of base)
       -1532 : 194113.dasm (-45.11 % of base)
       -1060 : 182192.dasm (-35.81 % of base)
       -1060 : 194022.dasm (-46.99 % of base)
        -928 : 76313.dasm (-50.11 % of base)
        -824 : 127383.dasm (-38.50 % of base)
        -812 : 194406.dasm (-24.82 % of base)
        -804 : 182191.dasm (-29.30 % of base)
        -748 : 194020.dasm (-43.90 % of base)
        -736 : 177591.dasm (-35.25 % of base)
        -732 : 4491.dasm (-42.46 % of base)
        -724 : 87593.dasm (-45.59 % of base)
        -724 : 91558.dasm (-31.42 % of base)
        -712 : 207915.dasm (-34.56 % of base)
        -704 : 45272.dasm (-27.80 % of base)
        -704 : 75592.dasm (-42.41 % of base)

922 total files with Code Size differences (840 improved, 82 regressed), 167 unchanged.

Top method regressions (bytes):
          96 (2.33 % of base) : 11418.dasm - Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.DirectiveParser:ParsePragmaDirective(Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken,Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken,bool):Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.DirectiveTriviaSyntax:this
          88 (3.17 % of base) : 29765.dasm - System.Diagnostics.Tracing.EventPipeMetadataGenerator:GenerateMetadata(int,System.String,long,int,int,int,System.Diagnostics.Tracing.EventParameterInfo[]):System.Byte[]:this
          60 (1.28 % of base) : 207708.dasm - System.DirectoryServices.Protocols.LdapConnection:SendRequestHelper(System.DirectoryServices.Protocols.DirectoryRequest,byref):int:this
          40 (2.78 % of base) : 197019.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          40 (3.05 % of base) : 160232.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          40 (4.42 % of base) : 44959.dasm - System.DateTimeParse:MatchEraName(byref,System.Globalization.DateTimeFormatInfo,byref):bool
          36 (0.89 % of base) : 22614.dasm - CriticalHelper:WriteCollection(System.Runtime.Serialization.CollectionDataContract):this
          36 (9.00 % of base) : 40429.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          28 (0.42 % of base) : 29461.dasm - System.Diagnostics.Tracing.ManifestBuilder:CreateManifestString():System.String:this
          24 (0.12 % of base) : 53095.dasm - Microsoft.Diagnostics.Tracing.Parsers.AspNet.AspNetTraceEventParser:EnumerateTemplates(System.Func`3[System.String, System.String, Microsoft.Diagnostics.Tracing.EventFilterResponse],System.Action`1[Microsoft.Diagnostics.Tracing.TraceEvent]):this
          20 (1.69 % of base) : 142140.dasm - Microsoft.CodeAnalysis.TypeNameDecoder`2:GetTypeSymbol(Microsoft.CodeAnalysis.MetadataHelpers+AssemblyQualifiedTypeName,byref):System.__Canon:this
          20 (2.07 % of base) : 166069.dasm - System.ComponentModel.EnumConverter:ConvertFrom(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object):System.Object:this
          20 (1.87 % of base) : 165163.dasm - System.ComponentModel.MaskedTextProvider:.ctor(System.String,System.Globalization.CultureInfo,bool,ushort,ushort,bool):this
          20 (2.49 % of base) : 77456.dasm - System.Data.Common.SqlBooleanStorage:Aggregate(System.Int32[],int):System.Object:this
          20 (4.24 % of base) : 220003.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          20 (3.52 % of base) : 41837.dasm - System.TimeZoneInfo:TryConvertIanaIdToWindowsId(System.String,bool,byref):bool
          20 (6.85 % of base) : 83958.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderCodeGen+Member,System.String):this
          20 (7.04 % of base) : 83869.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderILGen+Member,System.String):this
          16 (0.40 % of base) : 77598.dasm - System.Data.Common.SqlDecimalStorage:Aggregate(System.Int32[],int):System.Object:this
          16 (7.41 % of base) : 81887.dasm - System.Data.DataColumnCollection:FinishInitCollection():this

Top method improvements (bytes):
      -12260 (-44.63 % of base) : 194028.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -5404 (-42.74 % of base) : 45275.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2384 (-52.74 % of base) : 194015.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
       -1856 (-36.54 % of base) : 194026.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1652 (-45.94 % of base) : 194110.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
       -1532 (-45.11 % of base) : 194113.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
       -1060 (-46.99 % of base) : 194022.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
       -1060 (-35.81 % of base) : 182192.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -928 (-50.11 % of base) : 76313.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -824 (-38.50 % of base) : 127383.dasm - Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter:UseTwiceLateInvocation(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.VisualBasic.BoundLateInvocation,Microsoft.CodeAnalysis.ArrayBuilder`1[Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedLocal]):Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter+Result
        -812 (-24.82 % of base) : 194406.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:CanMatchArguments(Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],System.Type[],bool,System.Collections.Generic.List`1[System.String]):bool
        -804 (-29.30 % of base) : 182191.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -748 (-43.90 % of base) : 194020.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -736 (-35.25 % of base) : 177591.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
        -732 (-42.46 % of base) : 4491.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -724 (-31.42 % of base) : 91558.dasm - System.Xml.Schema.ParticleContentValidator:BuildTransitionTable(System.Xml.Schema.BitSet,System.Xml.Schema.BitSet[],int):System.Int32[][]:this
        -724 (-45.59 % of base) : 87593.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -712 (-34.56 % of base) : 207915.dasm - System.DirectoryServices.Protocols.DirectoryAttribute:GetValues(System.Type):System.Object[]:this
        -704 (-27.80 % of base) : 45272.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -704 (-42.41 % of base) : 75592.dasm - System.Speech.Internal.SrgsCompiler.SrgsCompiler:CompileStream(System.Xml.XmlReader[],System.String,System.IO.Stream,bool,System.Uri,System.String[],System.String)

Top method regressions (percentages):
          16 (13.79 % of base) : 218949.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          36 (9.00 % of base) : 40429.dasm - System.Globalization.CalendricalCalculationsHelper:EphemerisCorrection(double):double
          12 (7.50 % of base) : 198986.dasm - System.Reflection.Internal.ObjectPool`1:Allocate():System.__Canon:this
          16 (7.41 % of base) : 81887.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          20 (7.04 % of base) : 83869.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderILGen+Member,System.String):this
          20 (6.85 % of base) : 83958.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(System.Xml.Serialization.XmlSerializationReaderCodeGen+Member,System.String):this
          16 (6.78 % of base) : 192642.dasm - System.Diagnostics.ProcessManager:GetProcessInfo(int,System.String):System.Diagnostics.ProcessInfo
          12 (6.25 % of base) : 141908.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[Microsoft.CodeAnalysis.CommonReferenceManager`2+AssemblyReferenceBinding[System.__Canon, System.__Canon][]]):bool
          12 (6.12 % of base) : 184191.dasm - System.Drawing.Printing.PageSettings:PaperSourceFromMode(Interop+Gdi32+DEVMODE):System.Drawing.Printing.PaperSource:this
          12 (4.69 % of base) : 68360.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          12 (4.55 % of base) : 39519.dasm - System.Globalization.JapaneseLunisolarCalendar:TrimEras(System.Globalization.EraInfo[]):System.Globalization.EraInfo[]
          40 (4.42 % of base) : 44959.dasm - System.DateTimeParse:MatchEraName(byref,System.Globalization.DateTimeFormatInfo,byref):bool
           8 (4.26 % of base) : 132878.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.CRC32:Crc32Update(int,System.Byte[]):int
          20 (4.24 % of base) : 220003.dasm - System.DomainNameHelper:IdnEquivalent(System.String):System.String
          12 (3.66 % of base) : 187691.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          20 (3.52 % of base) : 41837.dasm - System.TimeZoneInfo:TryConvertIanaIdToWindowsId(System.String,bool,byref):bool
          12 (3.26 % of base) : 142388.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          88 (3.17 % of base) : 29765.dasm - System.Diagnostics.Tracing.EventPipeMetadataGenerator:GenerateMetadata(int,System.String,long,int,int,int,System.Diagnostics.Tracing.EventParameterInfo[]):System.Byte[]:this
          40 (3.05 % of base) : 160232.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          40 (2.78 % of base) : 197019.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this

Top method improvements (percentages):
       -2384 (-52.74 % of base) : 194015.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
        -432 (-50.23 % of base) : 82106.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this
        -928 (-50.11 % of base) : 76313.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -532 (-49.81 % of base) : 81899.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -408 (-48.57 % of base) : 80740.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
        -280 (-47.95 % of base) : 20966.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,Microsoft.CodeAnalysis.CSharp.OverloadResolution+ParameterMap):System.Nullable`1[System.Int32]
        -292 (-47.71 % of base) : 168334.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
       -1060 (-46.99 % of base) : 194022.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -412 (-46.61 % of base) : 194743.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
       -1652 (-45.94 % of base) : 194110.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
        -332 (-45.60 % of base) : 121724.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
        -724 (-45.59 % of base) : 87593.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -504 (-45.32 % of base) : 211131.dasm - System.Web.Util.HttpEncoder:UrlEncodeUnicode(System.String):System.String
       -1532 (-45.11 % of base) : 194113.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
      -12260 (-44.63 % of base) : 194028.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
        -596 (-44.48 % of base) : 187542.dasm - Internal.TypeSystem.RuntimeDeterminedTypeUtilities:ConvertInstantiationToSharedRuntimeForm(Internal.TypeSystem.Instantiation,Internal.TypeSystem.Instantiation,byref):Internal.TypeSystem.Instantiation
        -160 (-44.44 % of base) : 147218.dasm - Newtonsoft.Json.Utilities.ConvertUtils:TryHexTextToInt(System.Char[],int,int,byref):bool
        -748 (-43.90 % of base) : 194020.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -400 (-43.10 % of base) : 194424.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:IsExactSignatureMatch(System.Reflection.ParameterInfo[],int,System.Reflection.ParameterInfo[],int):bool
        -160 (-43.01 % of base) : 49450.dasm - System.Collections.Generic.NullableEqualityComparer`1:IndexOf(System.Nullable`1[System.Int32][],System.Nullable`1[System.Int32],int,int):int:this

922 total methods with Code Size differences (840 improved, 82 regressed), 167 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.pmi.windows.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 51123456 (overridden on cmd)
Total bytes of diff: 50948096 (overridden on cmd)
Total bytes of delta: -175360 (-0.34 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          80 : 200614.dasm (6.83 % of base)
          44 : 134075.dasm (1.05 % of base)
          40 : 82789.dasm (12.82 % of base)
          40 : 226697.dasm (8.26 % of base)
          36 : 107911.dasm (1.42 % of base)
          36 : 144182.dasm (9.38 % of base)
          36 : 236119.dasm (5.14 % of base)
          32 : 209598.dasm (6.67 % of base)
          32 : 154132.dasm (8.60 % of base)
          32 : 154297.dasm (8.60 % of base)
          32 : 203525.dasm (2.66 % of base)
          28 : 187026.dasm (1.87 % of base)
          28 : 83627.dasm (10.45 % of base)
          28 : 112616.dasm (3.95 % of base)
          28 : 236344.dasm (3.65 % of base)
          28 : 210871.dasm (8.64 % of base)
          28 : 234564.dasm (3.65 % of base)
          28 : 76580.dasm (2.83 % of base)
          24 : 175168.dasm (1.20 % of base)
          24 : 106152.dasm (0.99 % of base)

Top file improvements (bytes):
      -10092 : 150026.dasm (-42.54 % of base)
       -7012 : 48434.dasm (-64.85 % of base)
       -5572 : 48409.dasm (-27.10 % of base)
       -2368 : 48406.dasm (-44.68 % of base)
       -1852 : 51215.dasm (-46.63 % of base)
       -1768 : 48373.dasm (-30.32 % of base)
       -1748 : 150028.dasm (-37.29 % of base)
       -1708 : 22787.dasm (-45.57 % of base)
       -1708 : 53660.dasm (-45.96 % of base)
       -1548 : 55427.dasm (-46.07 % of base)
       -1400 : 51217.dasm (-48.41 % of base)
       -1372 : 72265.dasm (-45.19 % of base)
       -1332 : 53657.dasm (-49.12 % of base)
       -1320 : 55300.dasm (-22.15 % of base)
       -1316 : 55353.dasm (-32.54 % of base)
       -1228 : 71132.dasm (-26.58 % of base)
       -1200 : 50942.dasm (-40.98 % of base)
       -1124 : 54324.dasm (-28.07 % of base)
       -1100 : 71182.dasm (-24.60 % of base)
       -1096 : 54709.dasm (-42.09 % of base)

1082 total files with Code Size differences (936 improved, 146 regressed), 204 unchanged.

Top method regressions (bytes):
          80 (6.83 % of base) : 200614.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          44 (1.05 % of base) : 134075.dasm - System.Xml.Serialization.SchemaGraph:Depends(System.Xml.Schema.XmlSchemaObject,System.Collections.ArrayList):this
          40 (12.82 % of base) : 82789.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          40 (8.26 % of base) : 226697.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          36 (9.38 % of base) : 144182.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          36 (1.42 % of base) : 107911.dasm - System.Data.DataColumnCollection:CanRemove(System.Data.DataColumn,bool):bool:this
          36 (5.14 % of base) : 236119.dasm - Xunit.StackFrameInfo:FromFailure(Xunit.Abstractions.IFailureInformation):Xunit.StackFrameInfo
          32 (6.67 % of base) : 209598.dasm - <OrderBy>d__3`1[Byte][System.Byte]:MoveNext():bool:this
          32 (8.60 % of base) : 154297.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (8.60 % of base) : 154132.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (2.66 % of base) : 203525.dasm - System.Uri:GetLocalPath():System.String:this
          28 (1.87 % of base) : 187026.dasm - DebugViewPrinter:Analyze():this
          28 (2.83 % of base) : 76580.dasm - Microsoft.CodeAnalysis.TypeNameDecoder`2[__Canon,__Canon][System.__Canon,System.__Canon]:GetTypeSymbol(AssemblyQualifiedTypeName,byref):System.__Canon:this
          28 (10.45 % of base) : 83627.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          28 (8.64 % of base) : 210871.dasm - System.Collections.HashHelpers:GetPrime(int):int
          28 (3.95 % of base) : 112616.dasm - System.Data.Common.SqlBooleanStorage:Aggregate(System.Int32[],int):System.Object:this
          28 (3.65 % of base) : 236344.dasm - Xunit.Serialization.XunitSerializationInfo:CanSerializeObject(System.Object):bool
          28 (3.65 % of base) : 234564.dasm - Xunit.Serialization.XunitSerializationInfo:CanSerializeObject(System.Object):bool
          24 (4.51 % of base) : 151018.dasm - Microsoft.Win32.RegistryKey:DeleteSubKeyTree(System.String,bool):this
          24 (0.99 % of base) : 106152.dasm - R2RTest.BuildFolder:FromDirectory(System.String,System.Collections.Generic.IEnumerable`1[[R2RTest.CompilerRunner, R2RTest, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null]],System.String,R2RTest.BuildOptions):R2RTest.BuildFolder

Top method improvements (bytes):
      -10092 (-42.54 % of base) : 150026.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -7012 (-64.85 % of base) : 48434.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindFieldAndPropertyInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceMemberContainerTypeSymbol,System.Collections.Immutable.ImmutableArray`1[ImmutableArray`1],Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedInteractiveInitializerMethod,Microsoft.CodeAnalysis.DiagnosticBag):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundInitializer, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
       -5572 (-27.10 % of base) : 48409.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportOverloadResolutionFailureForASingleCandidate(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.Location,int,byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool,bool,bool,bool,Microsoft.CodeAnalysis.DiagnosticBag,Microsoft.CodeAnalysis.VisualBasic.Symbol,bool,Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.Symbol):this
       -2368 (-44.68 % of base) : 48406.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportUnspecificProcedures(Microsoft.CodeAnalysis.Location,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.DiagnosticBag,bool):this
       -1852 (-46.63 % of base) : 51215.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
       -1768 (-30.32 % of base) : 48373.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindLateBoundInvocation(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.BoundMethodOrPropertyGroup,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.DiagnosticBag,bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1748 (-37.29 % of base) : 150028.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1708 (-45.57 % of base) : 22787.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:IsApplicable(Microsoft.CodeAnalysis.CSharp.Symbol,EffectiveParameters,Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,System.Collections.Immutable.ImmutableArray`1[Int32],bool,bool,bool,byref):Microsoft.CodeAnalysis.CSharp.MemberAnalysisResult:this
       -1708 (-45.96 % of base) : 53660.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1548 (-46.07 % of base) : 55427.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
       -1400 (-48.41 % of base) : 51217.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1372 (-45.19 % of base) : 72265.dasm - AsyncMethodToClassRewriter:RewriteSpillSequenceIntoBlock(Microsoft.CodeAnalysis.VisualBasic.BoundSpillSequence,bool,Microsoft.CodeAnalysis.VisualBasic.BoundStatement[]):Microsoft.CodeAnalysis.VisualBasic.BoundBlock:this
       -1332 (-49.12 % of base) : 53657.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
       -1320 (-22.15 % of base) : 55300.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateCallOrGet(Microsoft.CodeAnalysis.VisualBasic.BoundLateMemberAccess,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1316 (-32.54 % of base) : 55353.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateMakeArgumentArrayArgument(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1228 (-26.58 % of base) : 71132.dasm - AnonymousDelegatePublicSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1200 (-40.98 % of base) : 50942.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceAttributeData:GetTargetAttributeSignatureIndex(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.AttributeDescription):int:this
       -1124 (-28.07 % of base) : 54324.dasm - Microsoft.CodeAnalysis.VisualBasic.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol,System.Predicate`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1100 (-24.60 % of base) : 71182.dasm - AnonymousDelegateTemplateSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1096 (-42.09 % of base) : 54709.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbol:GetAttributesToBind(Roslyn.Utilities.OneOrMany`1[[Microsoft.CodeAnalysis.SyntaxList`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], Microsoft.CodeAnalysis, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],int,Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation,byref):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]:this

Top method regressions (percentages):
          24 (20.69 % of base) : 202020.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          40 (12.82 % of base) : 82789.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          20 (11.63 % of base) : 179545.dasm - System.Drawing.Printing.PageSettings:PaperSourceFromMode(DEVMODE):System.Drawing.Printing.PaperSource:this
          28 (10.45 % of base) : 83627.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          36 (9.38 % of base) : 144182.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          16 (9.30 % of base) : 107870.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          28 (8.64 % of base) : 210871.dasm - System.Collections.HashHelpers:GetPrime(int):int
          32 (8.60 % of base) : 154297.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          32 (8.60 % of base) : 154132.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          12 (8.57 % of base) : 209727.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          40 (8.26 % of base) : 226697.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          16 (8.00 % of base) : 148939.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          16 (8.00 % of base) : 173690.dasm - System.Diagnostics.ProcessManager:GetProcessInfo(int,System.String):System.Diagnostics.ProcessInfo
          16 (7.84 % of base) : 43947.dasm - QueryTranslationState:RangeVariableMap(Microsoft.CodeAnalysis.CSharp.Symbols.RangeVariableSymbol[]):RangeVariableMap
          12 (7.69 % of base) : 164228.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          20 (7.69 % of base) : 135457.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberElementsElse(Member,System.String):this
          24 (7.41 % of base) : 135372.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:WriteMemberElementsElse(Member,System.String):this
          12 (7.32 % of base) : 182457.dasm - System.IO.IsolatedStorage.IsolatedStorageFile:GetFullPath(System.String):System.String:this
          20 (7.14 % of base) : 179544.dasm - System.Drawing.Printing.PageSettings:PaperSizeFromMode(DEVMODE):System.Drawing.Printing.PaperSize:this
          80 (6.83 % of base) : 200614.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this

Top method improvements (percentages):
       -7012 (-64.85 % of base) : 48434.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindFieldAndPropertyInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceMemberContainerTypeSymbol,System.Collections.Immutable.ImmutableArray`1[ImmutableArray`1],Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedInteractiveInitializerMethod,Microsoft.CodeAnalysis.DiagnosticBag):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundInitializer, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -672 (-54.19 % of base) : 22769.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,ParameterMap):System.Nullable`1[Int32]
        -300 (-51.02 % of base) : 223230.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
        -432 (-50.70 % of base) : 107908.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
        -376 (-50.27 % of base) : 107701.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this
        -860 (-50.00 % of base) : 137059.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -352 (-49.16 % of base) : 109116.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
       -1332 (-49.12 % of base) : 53657.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
        -900 (-48.49 % of base) : 27505.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
       -1400 (-48.41 % of base) : 51217.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
        -380 (-48.22 % of base) : 149305.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
        -324 (-48.21 % of base) : 61527.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
       -1076 (-48.21 % of base) : 40731.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
        -964 (-48.10 % of base) : 27513.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:RewriteArgumentsForComCall(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.Symbols.LocalSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -896 (-47.16 % of base) : 150033.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -452 (-47.08 % of base) : 75254.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArray(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean]):System.Collections.Immutable.ImmutableArray`1[__Canon]
       -1852 (-46.63 % of base) : 51215.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
        -800 (-46.40 % of base) : 41166.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.CustomModifierUtils:CopyParameterCustomModifiers(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
       -1548 (-46.07 % of base) : 55427.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
        -160 (-45.98 % of base) : 103298.dasm - Newtonsoft.Json.Utilities.ConvertUtils:TryHexTextToInt(System.Char[],int,int,byref):bool

1082 total methods with Code Size differences (936 improved, 146 regressed), 204 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries_tests.pmi.windows.arm64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 116322196 (overridden on cmd)
Total bytes of diff: 116219208 (overridden on cmd)
Total bytes of delta: -102988 (-0.09 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         152 : 113856.dasm (3.83 % of base)
          80 : 304036.dasm (6.83 % of base)
          60 : 336758.dasm (1.74 % of base)
          48 : 291148.dasm (1.14 % of base)
          40 : 256534.dasm (2.14 % of base)
          40 : 106533.dasm (0.21 % of base)
          36 : 180646.dasm (1.24 % of base)
          32 : 234802.dasm (2.45 % of base)
          32 : 315681.dasm (3.64 % of base)
          32 : 264301.dasm (1.42 % of base)
          28 : 250147.dasm (0.45 % of base)
          28 : 250157.dasm (0.43 % of base)
          28 : 348895.dasm (8.75 % of base)
          28 : 5556.dasm (7.61 % of base)
          28 : 226766.dasm (0.33 % of base)
          24 : 153080.dasm (1.12 % of base)
          24 : 181214.dasm (2.51 % of base)
          24 : 215126.dasm (2.16 % of base)
          24 : 323230.dasm (1.74 % of base)
          24 : 116936.dasm (0.53 % of base)

Top file improvements (bytes):
       -1308 : 220031.dasm (-32.25 % of base)
       -1136 : 113658.dasm (-45.51 % of base)
        -768 : 19779.dasm (-24.37 % of base)
        -700 : 6702.dasm (-35.43 % of base)
        -664 : 126770.dasm (-23.51 % of base)
        -524 : 3615.dasm (-49.06 % of base)
        -428 : 19783.dasm (-15.95 % of base)
        -392 : 277282.dasm (-13.19 % of base)
        -384 : 212822.dasm (-16.49 % of base)
        -384 : 217082.dasm (-16.49 % of base)
        -340 : 236697.dasm (-22.08 % of base)
        -332 : 236698.dasm (-21.23 % of base)
        -320 : 336837.dasm (-33.90 % of base)
        -316 : 258996.dasm (-23.51 % of base)
        -304 : 226142.dasm (-41.08 % of base)
        -300 : 338151.dasm (-7.37 % of base)
        -300 : 175833.dasm (-43.10 % of base)
        -300 : 222258.dasm (-43.10 % of base)
        -296 : 44162.dasm (-23.95 % of base)
        -288 : 43933.dasm (-35.64 % of base)

1113 total files with Code Size differences (919 improved, 194 regressed), 212 unchanged.

Top method regressions (bytes):
         152 (3.83 % of base) : 113856.dasm - Microsoft.Build.Tasks.ResolveAssemblyReference:LogInputs():this
          80 (6.83 % of base) : 304036.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          60 (1.74 % of base) : 336758.dasm - System.Threading.Tests.MonitorTests:Enter_HasToWait()
          48 (1.14 % of base) : 291148.dasm - System.IO.Tests.BinaryWriterTests:BinaryWriter_SeekTests():this
          40 (0.21 % of base) : 106533.dasm - <ArrayAsRootObject>d__7:MoveNext():this
          40 (2.14 % of base) : 256534.dasm - NuGet.ProjectModel.PackageSpec:GetHashCode():int:this
          36 (1.24 % of base) : 180646.dasm - <GetAsync_SetCookieContainerMultipleCookies_CookiesSent>d__8:MoveNext():this
          32 (3.64 % of base) : 315681.dasm - <>c__DisplayClass1_0:<GetTags>b__2():System.UInt32[]:this
          32 (2.45 % of base) : 234802.dasm - Lamar.Scanning.Conventions.GenericConnectionScanner:ScanTypes(Lamar.Scanning.TypeSet,Lamar.ServiceRegistry):this
          32 (1.42 % of base) : 264301.dasm - System.Collections.Concurrent.Tests.BlockingCollectionTests:AddAnyTakeAny(int,int,int,System.Collections.Concurrent.BlockingCollection`1[Int32],System.Collections.Concurrent.BlockingCollection`1[System.Int32][],int)
          28 (0.33 % of base) : 226766.dasm - <>c__DisplayClass40_0:<UseInstance>b__0(Registry):Registry:this
          28 (7.61 % of base) : 5556.dasm - Microsoft.CodeAnalysis.Collections.Internal.HashHelpers:GetPrime(int):int
          28 (0.45 % of base) : 250147.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscovery:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          28 (0.43 % of base) : 250157.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscoveryV1:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          28 (8.75 % of base) : 348895.dasm - Unity.Utility.Prime:GetPrime(int):int
          24 (2.51 % of base) : 181214.dasm - <<GetAsync_SetCookieContainerMultipleCookies_CookiesSent>b__0>d:MoveNext():this
          24 (2.58 % of base) : 64508.dasm - <<GetAsync_SetCookieContainerMultipleCookies_CookiesSent>b__0>d:MoveNext():this
          24 (0.53 % of base) : 116936.dasm - Microsoft.Build.Construction.SolutionProjectGenerator:CreateTraversalInstance(System.String,bool,System.Collections.Generic.List`1[[Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]):Microsoft.Build.Execution.ProjectInstance:this
          24 (2.16 % of base) : 215126.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          24 (2.16 % of base) : 210827.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this

Top method improvements (bytes):
       -1308 (-32.25 % of base) : 220031.dasm - Castle.DynamicProxy.Generators.InvocationTypeGenerator:ImplementInvokeMethodOnTarget(Castle.DynamicProxy.Generators.Emitters.AbstractTypeEmitter,System.Reflection.ParameterInfo[],Castle.DynamicProxy.Generators.Emitters.MethodEmitter,Castle.DynamicProxy.Generators.Emitters.SimpleAST.Reference):this
       -1136 (-45.51 % of base) : 113658.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -768 (-24.37 % of base) : 19779.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddAfter_LLNode():this
        -700 (-35.43 % of base) : 6702.dasm - Microsoft.CodeAnalysis.Shared.Extensions.IMethodSymbolExtensions:RenameTypeParameters(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.Shared.Extensions.ITypeGenerator):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -664 (-23.51 % of base) : 126770.dasm - Expander:VisitParenthesizedLambdaExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax):Microsoft.CodeAnalysis.SyntaxNode:this
        -524 (-49.06 % of base) : 3615.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -428 (-15.95 % of base) : 19783.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddBefore_LLNode():this
        -392 (-13.19 % of base) : 277282.dasm - System.Diagnostics.Tests.DiagnosticSourceEventSourceBridgeTests:<TestEnableAllActivitySourcesWithOneEvent>b__1_0(System.String):this
        -384 (-16.49 % of base) : 212822.dasm - System.Data.SqlClient.TdsParser:WriteSessionRecoveryFeatureRequest(System.Data.SqlClient.SessionData,bool):int:this
        -384 (-16.49 % of base) : 217082.dasm - System.Data.SqlClient.TdsParser:WriteSessionRecoveryFeatureRequest(System.Data.SqlClient.SessionData,bool):int:this
        -340 (-22.08 % of base) : 236697.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[__Canon,__Canon,__Canon,__Canon]
        -332 (-21.23 % of base) : 236698.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60],[System.IServiceProvider, System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a],[System.Threading.Tasks.Task, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        -320 (-33.90 % of base) : 336837.dasm - System.Threading.Tests.SpinLockTests:RunSpinLockTest2_TryEnter(int,bool)
        -316 (-23.51 % of base) : 258996.dasm - System.TypeExtensions:SatisfiesGenericConstraintsOf(System.Type,System.Reflection.TypeInfo):bool
        -304 (-41.08 % of base) : 226142.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool
        -300 (-43.10 % of base) : 175833.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-43.10 % of base) : 222258.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-7.37 % of base) : 338151.dasm - System.Transactions.Tests.NonMsdtcPromoterTests:TestCase_PSPENonMsdtc(bool,bool,int,int,int,int,int,int,int)
        -296 (-23.95 % of base) : 44162.dasm - System.Linq.Expressions.Tests.NullableNewArrayListTests:CheckNullableDecimalArrayListTest(bool)
        -288 (-29.88 % of base) : 43917.dasm - System.Linq.Expressions.Tests.NewArrayListTests:CheckDecimalArrayListTest(bool)

Top method regressions (percentages):
          16 (11.11 % of base) : 39384.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          16 (11.11 % of base) : 41140.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          16 (10.26 % of base) : 1597.dasm - Roslyn.Utilities.PathUtilities:PathHashCode(System.String):int
          20 (8.93 % of base) : 115904.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          20 (8.93 % of base) : 237970.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          20 (8.93 % of base) : 122241.dasm - Microsoft.Build.Shared.FileUtilities:HasExtension(System.String,System.String[]):bool
          28 (8.75 % of base) : 348895.dasm - Unity.Utility.Prime:GetPrime(int):int
          16 (8.70 % of base) : 39386.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          16 (8.70 % of base) : 41141.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          28 (7.61 % of base) : 5556.dasm - Microsoft.CodeAnalysis.Collections.Internal.HashHelpers:GetPrime(int):int
          80 (6.83 % of base) : 304036.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          20 (6.76 % of base) : 226642.dasm - EmittingVisitor:TryEmitLabel(FastExpressionCompiler.LightExpression.LabelExpression,System.Collections.Generic.IReadOnlyList`1[[FastExpressionCompiler.LightExpression.ParameterExpression, DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]],System.Reflection.Emit.ILGenerator,byref,int):bool
           8 (5.41 % of base) : 226500.dasm - <>c__45`1[Byte][System.Byte]:<Visit>b__45_0(ImTools.ImMapEntry`1[KValue`1],bool,System.Action`1[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]]):bool:this
           8 (5.00 % of base) : 226496.dasm - <>c__44`2[Byte,Nullable`1][System.Byte,System.Nullable`1[System.Int32]]:<Visit>b__44_0(ImTools.ImMapEntry`1[KValue`1],System.Nullable`1[Int32],System.Action`2[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]):System.Nullable`1[Int32]:this
           8 (4.76 % of base) : 226492.dasm - <>c__43`2[Byte,Nullable`1][System.Byte,System.Nullable`1[System.Int32]]:<Fold>b__43_0(ImTools.ImMapEntry`1[KValue`1],System.Nullable`1[Int32],System.Func`3[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]):System.Nullable`1[Int32]:this
          16 (4.55 % of base) : 233944.dasm - LamarCompiler.Util.TypeExtensions:Closes(System.Type,System.Type):bool
          16 (4.49 % of base) : 149229.dasm - CatalogListener:ContainsChanges():bool:this
          12 (4.11 % of base) : 331430.dasm - Parser:DeterminePropNameIdentifier(System.String):System.String
           8 (3.92 % of base) : 44418.dasm - System.Linq.Expressions.Tests.BinaryNullableAddTests:CheckNullableUIntAddTest(bool)
           8 (3.92 % of base) : 44416.dasm - System.Linq.Expressions.Tests.BinaryNullableAddTests:CheckNullableUShortAddTest(bool)

Top method improvements (percentages):
        -524 (-49.06 % of base) : 3615.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
       -1136 (-45.51 % of base) : 113658.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
         -80 (-44.44 % of base) : 990.dasm - Roslyn.Utilities.Hash:GetFNVHashCode(System.Char[],int,int):int
        -284 (-43.83 % of base) : 226141.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
        -168 (-43.75 % of base) : 188973.dasm - System.Runtime.Serialization.Formatters.Tests.EqualityHelpers:ArraysAreEqual(System.Byte[][],System.Byte[][]):bool
        -300 (-43.10 % of base) : 175833.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -300 (-43.10 % of base) : 222258.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -136 (-43.04 % of base) : 283729.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 284068.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 290869.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 222084.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 260203.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 300500.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 302374.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 327911.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 58488.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -136 (-43.04 % of base) : 329856.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -72 (-41.86 % of base) : 128801.dasm - Microsoft.CodeAnalysis.VisualBasic.Extensions.SyntaxKindExtensions:IndexOf(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind[],ushort,int):int
        -112 (-41.79 % of base) : 7124.dasm - Microsoft.CodeAnalysis.Shared.Extensions.StringExtensions:ConvertTabToSpace(System.String,int,int,int):int
        -304 (-41.08 % of base) : 226142.dasm - DryIoc.ReflectionFactory:MatchServiceWithImplementedTypeParams(System.Type[],System.Type[],System.Type[],System.Type[],int):bool

1113 total methods with Code Size differences (919 improved, 194 regressed), 212 unchanged.

```

</details>

--------------------------------------------------------------------------------


</details>


## windows x64

<details>

<summary>windows x64 details</summary>

Summary file: `superpmi_diff_summary_windows_x64.md`

To reproduce these diffs on windows x64:
```
superpmi.py asmdiffs -target_os windows -target_arch x64 -arch x64
```

## aspnet.run.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 13670517 (overridden on cmd)
Total bytes of diff: 13666207 (overridden on cmd)
Total bytes of delta: -4310 (-0.03 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          82 : 20119.dasm (3.52 % of base)
          40 : 33820.dasm (10.18 % of base)
          34 : 33752.dasm (5.51 % of base)
          34 : 21004.dasm (5.51 % of base)
          34 : 30125.dasm (5.51 % of base)
          34 : 38598.dasm (5.59 % of base)
          34 : 28332.dasm (5.59 % of base)
          26 : 14570.dasm (2.50 % of base)
          22 : 14337.dasm (6.09 % of base)
          21 : 14336.dasm (0.59 % of base)
          18 : 21414.dasm (0.71 % of base)
          17 : 16351.dasm (2.13 % of base)
          17 : 23144.dasm (2.13 % of base)
          16 : 41866.dasm (0.66 % of base)
          12 : 10110.dasm (0.88 % of base)
          12 : 10092.dasm (0.81 % of base)
          11 : 43044.dasm (0.30 % of base)
           9 : 29672.dasm (0.23 % of base)
           7 : 25230.dasm (2.95 % of base)
           4 : 14370.dasm (2.60 % of base)

Top file improvements (bytes):
        -425 : 42346.dasm (-26.28 % of base)
        -285 : 25474.dasm (-13.02 % of base)
        -284 : 19707.dasm (-13.43 % of base)
        -197 : 19706.dasm (-7.82 % of base)
        -197 : 25473.dasm (-7.59 % of base)
        -112 : 21032.dasm (-20.86 % of base)
        -112 : 28629.dasm (-20.86 % of base)
        -112 : 38632.dasm (-20.86 % of base)
        -111 : 16958.dasm (-29.76 % of base)
        -111 : 23446.dasm (-29.76 % of base)
        -111 : 39629.dasm (-30.41 % of base)
        -111 : 10119.dasm (-30.00 % of base)
        -111 : 33911.dasm (-30.41 % of base)
        -107 : 16741.dasm (-35.43 % of base)
        -106 : 15142.dasm (-1.81 % of base)
        -102 : 15068.dasm (-18.18 % of base)
         -98 : 38702.dasm (-4.64 % of base)
         -95 : 23391.dasm (-26.32 % of base)
         -95 : 16498.dasm (-26.32 % of base)
         -93 : 23629.dasm (-32.40 % of base)

129 total files with Code Size differences (99 improved, 30 regressed), 9 unchanged.

Top method regressions (bytes):
          82 (3.52 % of base) : 20119.dasm - <MultiplexingWriteLoop>d__19:MoveNext():this
          40 (10.18 % of base) : 33820.dasm - ConcurrentDictionary`2:get_IsEmpty():bool:this
          34 (5.51 % of base) : 33752.dasm - RegexRunner:InitializeForGo():this
          34 (5.51 % of base) : 21004.dasm - RegexRunner:InitializeForGo():this
          34 (5.51 % of base) : 30125.dasm - RegexRunner:InitializeForGo():this
          34 (5.59 % of base) : 38598.dasm - RegexRunner:InitializeForGo():this
          34 (5.59 % of base) : 28332.dasm - RegexRunner:InitializeForGo():this
          26 (2.50 % of base) : 14570.dasm - <<Invoke>g__AwaitRequestTask|6_0>d:MoveNext():this
          22 (6.09 % of base) : 14337.dasm - HeaderDescriptor:TryGet(ReadOnlySpan`1,byref):bool
          21 (0.59 % of base) : 14336.dasm - HttpConnection:ParseHeaderNameValue(HttpConnection,ReadOnlySpan`1,HttpResponseMessage,bool)
          18 (0.71 % of base) : 21414.dasm - SecureChannel:VerifyRemoteCertificate(RemoteCertificateValidationCallback,SslCertificateTrust,byref,byref,byref):bool:this
          17 (2.13 % of base) : 16351.dasm - ConsoleHostScenariosConfiguration:ConfigureScenarios(Scenarios):this
          17 (2.13 % of base) : 23144.dasm - ConsoleHostScenariosConfiguration:ConfigureScenarios(Scenarios):this
          16 (0.66 % of base) : 41866.dasm - JsonMiddleware:Invoke(HttpContext):Task:this
          12 (0.81 % of base) : 10092.dasm - RequestHeaderOriginalHostTransform:ApplyAsync(RequestTransformContext):ValueTask:this
          12 (0.88 % of base) : 10110.dasm - RequestHeaderXForwardedHostTransform:ApplyAsync(RequestTransformContext):ValueTask:this
          11 (0.30 % of base) : 43044.dasm - PlainTextActionResult:ExecuteResultAsync(ActionContext):Task:this
           9 (0.23 % of base) : 29672.dasm - MiddlewareHelpers:RenderFortunesHtml(IEnumerable`1,HttpContext,HtmlEncoder):Task
           7 (2.95 % of base) : 25230.dasm - CompiledQueryBase`2:ExecuteCore(__Canon,CancellationToken,ref):__Canon:this
           4 (2.60 % of base) : 14370.dasm - HttpConnectionBase:<GetResponseHeaderValueWithCaching>g__GetOrAddCachedValue|2_0(byref,HeaderDescriptor,ReadOnlySpan`1,Encoding):String

Top method improvements (bytes):
        -425 (-26.28 % of base) : 42346.dasm - DefaultTypeMap:FindConstructor(ref,ref):ConstructorInfo:this
        -285 (-13.02 % of base) : 25474.dasm - EntityShaperExpression:GenerateMaterializationCondition(IEntityType,bool):LambdaExpression:this
        -284 (-13.43 % of base) : 19707.dasm - EntityShaperExpression:GenerateMaterializationCondition(IEntityType,bool):LambdaExpression:this
        -197 (-7.82 % of base) : 19706.dasm - RelationalEntityShaperExpression:GenerateMaterializationCondition(IEntityType,bool):LambdaExpression:this
        -197 (-7.59 % of base) : 25473.dasm - RelationalEntityShaperExpression:GenerateMaterializationCondition(IEntityType,bool):LambdaExpression:this
        -112 (-20.86 % of base) : 21032.dasm - Dictionary`2:Resize(int,bool):this
        -112 (-20.86 % of base) : 28629.dasm - Dictionary`2:Resize(int,bool):this
        -112 (-20.86 % of base) : 38632.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-29.76 % of base) : 16958.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-29.76 % of base) : 23446.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-30.41 % of base) : 39629.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-30.00 % of base) : 10119.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-30.41 % of base) : 33911.dasm - Dictionary`2:Resize(int,bool):this
        -107 (-35.43 % of base) : 16741.dasm - ControllerActionInvoker:PrepareArguments(IDictionary`2,ObjectMethodExecutor):ref
        -106 (-1.81 % of base) : 15142.dasm - <WriteHeadersAsync>d__56:MoveNext():this
        -102 (-18.18 % of base) : 15068.dasm - Dictionary`2:Resize(int,bool):this
         -98 (-4.64 % of base) : 38702.dasm - Associates:AssignAssociates(MetadataImport,int,RuntimeType,RuntimeType,byref,byref,byref,byref,byref,byref,byref,byref)
         -95 (-26.32 % of base) : 23391.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this
         -95 (-26.32 % of base) : 16498.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this
         -93 (-32.40 % of base) : 23629.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this

Top method regressions (percentages):
          40 (10.18 % of base) : 33820.dasm - ConcurrentDictionary`2:get_IsEmpty():bool:this
           3 (7.89 % of base) : 21715.dasm - DynamicResolver:CalculateNumberOfExceptions(ref):int
          22 (6.09 % of base) : 14337.dasm - HeaderDescriptor:TryGet(ReadOnlySpan`1,byref):bool
          34 (5.59 % of base) : 38598.dasm - RegexRunner:InitializeForGo():this
          34 (5.59 % of base) : 28332.dasm - RegexRunner:InitializeForGo():this
          34 (5.51 % of base) : 33752.dasm - RegexRunner:InitializeForGo():this
          34 (5.51 % of base) : 21004.dasm - RegexRunner:InitializeForGo():this
          34 (5.51 % of base) : 30125.dasm - RegexRunner:InitializeForGo():this
          82 (3.52 % of base) : 20119.dasm - <MultiplexingWriteLoop>d__19:MoveNext():this
           7 (2.95 % of base) : 25230.dasm - CompiledQueryBase`2:ExecuteCore(__Canon,CancellationToken,ref):__Canon:this
           4 (2.60 % of base) : 14370.dasm - HttpConnectionBase:<GetResponseHeaderValueWithCaching>g__GetOrAddCachedValue|2_0(byref,HeaderDescriptor,ReadOnlySpan`1,Encoding):String
          26 (2.50 % of base) : 14570.dasm - <<Invoke>g__AwaitRequestTask|6_0>d:MoveNext():this
           1 (2.33 % of base) : 22371.dasm - ConcurrentDictionary`2:AreAllBucketsEmpty():bool:this
          17 (2.13 % of base) : 16351.dasm - ConsoleHostScenariosConfiguration:ConfigureScenarios(Scenarios):this
          17 (2.13 % of base) : 23144.dasm - ConsoleHostScenariosConfiguration:ConfigureScenarios(Scenarios):this
          12 (0.88 % of base) : 10110.dasm - RequestHeaderXForwardedHostTransform:ApplyAsync(RequestTransformContext):ValueTask:this
          12 (0.81 % of base) : 10092.dasm - RequestHeaderOriginalHostTransform:ApplyAsync(RequestTransformContext):ValueTask:this
          18 (0.71 % of base) : 21414.dasm - SecureChannel:VerifyRemoteCertificate(RemoteCertificateValidationCallback,SslCertificateTrust,byref,byref,byref):bool:this
          16 (0.66 % of base) : 41866.dasm - JsonMiddleware:Invoke(HttpContext):Task:this
          21 (0.59 % of base) : 14336.dasm - HttpConnection:ParseHeaderNameValue(HttpConnection,ReadOnlySpan`1,HttpResponseMessage,bool)

Top method improvements (percentages):
        -107 (-35.43 % of base) : 16741.dasm - ControllerActionInvoker:PrepareArguments(IDictionary`2,ObjectMethodExecutor):ref
         -93 (-32.40 % of base) : 23629.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this
         -93 (-32.40 % of base) : 17189.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this
         -67 (-30.73 % of base) : 40462.dasm - RouteValueDictionary:TryFindItem(String,byref):bool:this
        -111 (-30.41 % of base) : 39629.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-30.41 % of base) : 33911.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-30.00 % of base) : 10119.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-29.76 % of base) : 16958.dasm - Dictionary`2:Resize(int,bool):this
        -111 (-29.76 % of base) : 23446.dasm - Dictionary`2:Resize(int,bool):this
         -77 (-29.39 % of base) : 40461.dasm - RouteValueDictionary:TryGetValue(String,byref):bool:this
         -77 (-29.39 % of base) : 22507.dasm - RouteValueDictionary:TryGetValue(String,byref):bool:this
         -40 (-28.99 % of base) : 8525.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -35 (-27.13 % of base) : 14973.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -35 (-27.13 % of base) : 39695.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -35 (-27.13 % of base) : 42839.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -35 (-27.13 % of base) : 41808.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -35 (-27.13 % of base) : 29065.dasm - HttpUtilities:IsHostPortValid(String,int):bool
         -62 (-26.84 % of base) : 20426.dasm - ValueCollection:CopyTo(ref,int):this
         -62 (-26.84 % of base) : 25939.dasm - ValueCollection:CopyTo(ref,int):this
         -95 (-26.32 % of base) : 23391.dasm - EnumerableSorter`2:ComputeKeys(ref,int):this

129 total methods with Code Size differences (99 improved, 30 regressed), 9 unchanged.

```

</details>

--------------------------------------------------------------------------------

## benchmarks.run.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 7151027 (overridden on cmd)
Total bytes of diff: 7111713 (overridden on cmd)
Total bytes of delta: -39314 (-0.55 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         302 : 6070.dasm (22.50 % of base)
          71 : 13447.dasm (6.66 % of base)
          62 : 7365.dasm (4.16 % of base)
          51 : 20239.dasm (4.99 % of base)
          37 : 1466.dasm (2.02 % of base)
          25 : 2779.dasm (9.69 % of base)
          25 : 7294.dasm (2.13 % of base)
          22 : 1489.dasm (0.31 % of base)
          19 : 17855.dasm (8.26 % of base)
          16 : 18507.dasm (0.50 % of base)
          16 : 21241.dasm (7.37 % of base)
          16 : 7134.dasm (1.04 % of base)
          15 : 7481.dasm (3.33 % of base)
          15 : 7602.dasm (8.72 % of base)
          14 : 6647.dasm (0.80 % of base)
          13 : 24266.dasm (7.07 % of base)
          12 : 14364.dasm (0.31 % of base)
          12 : 17667.dasm (0.35 % of base)
          12 : 22659.dasm (6.94 % of base)
          11 : 24958.dasm (6.40 % of base)

Top file improvements (bytes):
       -4631 : 1640.dasm (-42.64 % of base)
       -2237 : 25275.dasm (-66.82 % of base)
       -1925 : 16078.dasm (-64.86 % of base)
       -1142 : 12863.dasm (-63.27 % of base)
       -1110 : 13824.dasm (-57.72 % of base)
        -912 : 26056.dasm (-57.90 % of base)
        -775 : 6098.dasm (-58.36 % of base)
        -702 : 18989.dasm (-47.79 % of base)
        -694 : 2482.dasm (-34.46 % of base)
        -629 : 1141.dasm (-32.86 % of base)
        -604 : 16077.dasm (-58.87 % of base)
        -570 : 944.dasm (-38.85 % of base)
        -535 : 15079.dasm (-43.96 % of base)
        -524 : 25585.dasm (-65.58 % of base)
        -517 : 18268.dasm (-51.14 % of base)
        -516 : 16076.dasm (-49.24 % of base)
        -502 : 14889.dasm (-69.92 % of base)
        -469 : 12333.dasm (-44.45 % of base)
        -461 : 2450.dasm (-12.81 % of base)
        -406 : 20677.dasm (-45.93 % of base)

298 total files with Code Size differences (249 improved, 49 regressed), 26 unchanged.

Top method regressions (bytes):
         302 (22.50 % of base) : 6070.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
          71 (6.66 % of base) : 13447.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          62 (4.16 % of base) : 7365.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          51 (4.99 % of base) : 20239.dasm - BenchmarksGame.NBodySystem:.ctor():this
          37 (2.02 % of base) : 1466.dasm - MemberInfoCache`1[__Canon][System.__Canon]:PopulateInterfaces(Filter):System.RuntimeType[]:this
          25 (9.69 % of base) : 2779.dasm - Sigil.Impl.LinqAlternative:Each(System.Collections.Generic.IEnumerable`1[__Canon],System.Action`1[__Canon])
          25 (2.13 % of base) : 7294.dasm - System.Xml.Serialization.TypeScope:PopulateMemberInfos(System.Xml.Serialization.StructMapping,System.Xml.Serialization.MemberMapping[],System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Reflection.MemberInfo, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]])
          22 (0.31 % of base) : 1489.dasm - ProtoBuf.Meta.MetaType:ApplyDefaultBehaviourImpl(int):this
          19 (8.26 % of base) : 17855.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          16 (0.50 % of base) : 18507.dasm - Microsoft.CodeAnalysis.CSharp.MethodCompiler:CompileNamedType(Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol):this
          16 (7.37 % of base) : 21241.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:List():System.Collections.Generic.List`1[Int32]:this
          16 (1.04 % of base) : 7134.dasm - System.Xml.Serialization.XmlAttributes:.ctor(System.Reflection.ICustomAttributeProvider):this
          15 (3.33 % of base) : 7481.dasm - Microsoft.Extensions.Caching.Memory.Tests.MemoryCacheTests:AddThenRemove_NoExpiration():this
          15 (8.72 % of base) : 7602.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:List():System.Collections.Generic.List`1[__Canon]:this
          14 (0.80 % of base) : 6647.dasm - Sigil.Emit`1[__Canon][System.__Canon]:Switch(Sigil.Label[]):Sigil.Emit`1[__Canon]:this
          13 (7.07 % of base) : 24266.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Stack():System.Collections.Generic.Stack`1[Int32]:this
          12 (0.31 % of base) : 14364.dasm - CriticalHelper:WriteCollection(System.Runtime.Serialization.CollectionDataContract):this
          12 (0.35 % of base) : 17667.dasm - Microsoft.CodeAnalysis.PEModule:GetTargetAttributeSignatureIndex(System.Reflection.Metadata.MetadataReader,System.Reflection.Metadata.CustomAttributeHandle,Microsoft.CodeAnalysis.AttributeDescription):int
          12 (6.94 % of base) : 22659.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Dictionary():System.Collections.Generic.Dictionary`2[Int32,Int32]:this
          11 (4.47 % of base) : 6822.dasm - Newtonsoft.Json.Utilities.TypeExtensions:AssignableToTypeName(System.Type,System.String,bool,byref):bool

Top method improvements (bytes):
       -4631 (-42.64 % of base) : 1640.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2237 (-66.82 % of base) : 25275.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -1925 (-64.86 % of base) : 16078.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1142 (-63.27 % of base) : 12863.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1110 (-57.72 % of base) : 13824.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -912 (-57.90 % of base) : 26056.dasm - Benchstone.BenchF.InvMt:Test():bool:this
        -775 (-58.36 % of base) : 6098.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -702 (-47.79 % of base) : 18989.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool,Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=2.10.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -694 (-34.46 % of base) : 2482.dasm - System.Reflection.Emit.MethodBuilder:CreateMethodBodyHelper(System.Reflection.Emit.ILGenerator):this
        -629 (-32.86 % of base) : 1141.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -604 (-58.87 % of base) : 16077.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -570 (-38.85 % of base) : 944.dasm - BilinearTest:BilinearInterpol(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]
        -535 (-43.96 % of base) : 15079.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int
        -524 (-65.58 % of base) : 25585.dasm - Benchstone.BenchF.InProd:Test():bool:this
        -517 (-51.14 % of base) : 18268.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArray(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -516 (-49.24 % of base) : 16076.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])
        -502 (-69.92 % of base) : 14889.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
        -469 (-44.45 % of base) : 12333.dasm - Fourier:DoFPUTransIteration(System.Double[],System.Double[],int):long
        -461 (-12.81 % of base) : 2450.dasm - AutomataNode:EmitSearchNextCore(System.Reflection.Emit.ILGenerator,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Reflection.Emit.LocalBuilder,System.Action`1[[System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],System.Action,Utf8Json.Internal.AutomataDictionary+AutomataNode[],int)
        -406 (-45.93 % of base) : 20677.dasm - System.Xml.Serialization.AccessorMapping:ElementsMatch(System.Xml.Serialization.ElementAccessor[],System.Xml.Serialization.ElementAccessor[]):bool

Top method regressions (percentages):
         302 (22.50 % of base) : 6070.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
          25 (9.69 % of base) : 2779.dasm - Sigil.Impl.LinqAlternative:Each(System.Collections.Generic.IEnumerable`1[__Canon],System.Action`1[__Canon])
          15 (8.72 % of base) : 7602.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:List():System.Collections.Generic.List`1[__Canon]:this
           8 (8.51 % of base) : 9976.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          19 (8.26 % of base) : 17855.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
           6 (8.22 % of base) : 7316.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:IsSequence(System.Xml.Serialization.XmlSerializationReaderILGen+Member[]):bool:this
          16 (7.37 % of base) : 21241.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:List():System.Collections.Generic.List`1[Int32]:this
          13 (7.07 % of base) : 24266.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Stack():System.Collections.Generic.Stack`1[Int32]:this
          12 (6.94 % of base) : 22659.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Dictionary():System.Collections.Generic.Dictionary`2[Int32,Int32]:this
          71 (6.66 % of base) : 13447.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          11 (6.40 % of base) : 24958.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:Dictionary():System.Collections.Generic.Dictionary`2[__Canon,__Canon]:this
           9 (5.77 % of base) : 26625.dasm - System.Collections.CreateAddAndRemove`1[__Canon][System.__Canon]:Stack():System.Collections.Generic.Stack`1[__Canon]:this
           3 (5.00 % of base) : 13765.dasm - BenchmarksGame.Fasta_1:MakeCumulative(BenchmarksGame.Fasta_1+Frequency[])
          51 (4.99 % of base) : 20239.dasm - BenchmarksGame.NBodySystem:.ctor():this
           6 (4.80 % of base) : 16870.dasm - System.FixedBufferExtensions:FixedBufferEqualsString(System.ReadOnlySpan`1[Char],System.String):bool
           6 (4.69 % of base) : 17651.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2[__Canon,__Canon][System.__Canon,System.__Canon]:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[__Canon]):bool
          11 (4.47 % of base) : 6822.dasm - Newtonsoft.Json.Utilities.TypeExtensions:AssignableToTypeName(System.Type,System.String,bool,byref):bool
          62 (4.16 % of base) : 7365.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
           9 (4.09 % of base) : 3958.dasm - System.Collections.CreateAddAndRemove`1[Int32][System.Int32]:Queue():System.Collections.Generic.Queue`1[Int32]:this
           7 (3.57 % of base) : 10495.dasm - System.Runtime.Serialization.FormatterServices:GetSerializableFields(System.Type):System.Reflection.FieldInfo[]

Top method improvements (percentages):
        -502 (-69.92 % of base) : 14889.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -2237 (-66.82 % of base) : 25275.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
        -320 (-66.81 % of base) : 22083.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
        -265 (-66.08 % of base) : 12328.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
        -524 (-65.58 % of base) : 25585.dasm - Benchstone.BenchF.InProd:Test():bool:this
       -1925 (-64.86 % of base) : 16078.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -207 (-64.49 % of base) : 22082.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
       -1142 (-63.27 % of base) : 12863.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
        -241 (-62.60 % of base) : 23829.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -239 (-62.57 % of base) : 23828.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
        -168 (-61.99 % of base) : 23436.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -604 (-58.87 % of base) : 16077.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -775 (-58.36 % of base) : 6098.dasm - JetStream.Statistics:findOptimalSegmentationInternal(System.Single[][],System.Int32[][],System.Double[],JetStream.SampleVarianceUpperTriangularMatrix,int)
        -912 (-57.90 % of base) : 26056.dasm - Benchstone.BenchF.InvMt:Test():bool:this
       -1110 (-57.72 % of base) : 13824.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -223 (-56.31 % of base) : 21099.dasm - Benchstone.BenchI.AddArray2:BenchInner1(System.Int32[][],byref)
        -292 (-55.94 % of base) : 22080.dasm - Benchstone.BenchI.Array2:Bench(int):bool
        -181 (-54.19 % of base) : 25586.dasm - Benchstone.BenchF.InProd:Inner(System.Double[][],System.Double[][],System.Double[][])
        -244 (-53.98 % of base) : 14514.dasm - SciMark2.SparseCompRow:matmult(System.Double[],System.Double[],System.Int32[],System.Int32[],System.Double[],int)
        -344 (-52.76 % of base) : 12969.dasm - SciMark2.SOR:execute(double,System.Double[][],int)

298 total methods with Code Size differences (249 improved, 49 regressed), 26 unchanged.

```

</details>

--------------------------------------------------------------------------------

## coreclr_tests.pmi.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 124162882 (overridden on cmd)
Total bytes of diff: 124114294 (overridden on cmd)
Total bytes of delta: -48588 (-0.04 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         631 : 215136.dasm (2.33 % of base)
         514 : 215102.dasm (1.45 % of base)
         349 : 215169.dasm (2.01 % of base)
         302 : 239006.dasm (22.50 % of base)
         300 : 215114.dasm (1.48 % of base)
         233 : 216118.dasm (2.66 % of base)
         124 : 215205.dasm (2.23 % of base)
         113 : 225369.dasm (5.26 % of base)
         107 : 225083.dasm (2.46 % of base)
         107 : 225189.dasm (2.28 % of base)
         107 : 225228.dasm (2.43 % of base)
          91 : 224684.dasm (1.51 % of base)
          60 : 224789.dasm (1.36 % of base)
          60 : 225212.dasm (1.44 % of base)
          51 : 248581.dasm (4.99 % of base)
          47 : 224741.dasm (1.54 % of base)
          32 : 224644.dasm (1.42 % of base)
          29 : 245833.dasm (9.51 % of base)
          25 : 209222.dasm (11.11 % of base)
          14 : 233009.dasm (3.56 % of base)

Top file improvements (bytes):
       -2380 : 247844.dasm (-52.76 % of base)
       -2237 : 250920.dasm (-66.82 % of base)
       -1925 : 191492.dasm (-64.86 % of base)
       -1356 : 252063.dasm (-63.07 % of base)
       -1142 : 191413.dasm (-63.27 % of base)
       -1110 : 191423.dasm (-57.72 % of base)
        -912 : 252834.dasm (-57.90 % of base)
        -772 : 246329.dasm (-45.39 % of base)
        -772 : 246391.dasm (-45.39 % of base)
        -703 : 252064.dasm (-49.54 % of base)
        -625 : 191393.dasm (-32.57 % of base)
        -604 : 191490.dasm (-58.87 % of base)
        -555 : 253759.dasm (-54.84 % of base)
        -527 : 247835.dasm (-22.16 % of base)
        -524 : 228426.dasm (-44.03 % of base)
        -516 : 191491.dasm (-49.24 % of base)
        -506 : 250911.dasm (-66.14 % of base)
        -502 : 252854.dasm (-69.92 % of base)
        -498 : 246330.dasm (-60.51 % of base)
        -498 : 246331.dasm (-60.51 % of base)

358 total files with Code Size differences (301 improved, 57 regressed), 10 unchanged.

Top method regressions (bytes):
         631 (2.33 % of base) : 215136.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         514 (1.45 % of base) : 215102.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         349 (2.01 % of base) : 215169.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         302 (22.50 % of base) : 239006.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
         300 (1.48 % of base) : 215114.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         233 (2.66 % of base) : 216118.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         124 (2.23 % of base) : 215205.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         113 (5.26 % of base) : 225369.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         107 (2.46 % of base) : 225083.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         107 (2.28 % of base) : 225189.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
         107 (2.43 % of base) : 225228.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          91 (1.51 % of base) : 224684.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          60 (1.36 % of base) : 224789.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          60 (1.44 % of base) : 225212.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          51 (4.99 % of base) : 248581.dasm - BenchmarksGame.NBodySystem:.ctor():this
          47 (1.54 % of base) : 224741.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          32 (1.42 % of base) : 224644.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
          29 (9.51 % of base) : 245833.dasm - RuntimeEventListener:Verify():bool:this
          25 (11.11 % of base) : 209222.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          14 (3.56 % of base) : 233009.dasm - Dynamo.Dynamo:Compare():bool:this

Top method improvements (bytes):
       -2380 (-52.76 % of base) : 247844.dasm - jaggedarr:gaussj(System.Double[,][],int,System.Double[,][],int)
       -2237 (-66.82 % of base) : 250920.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
       -1925 (-64.86 % of base) : 191492.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
       -1356 (-63.07 % of base) : 252063.dasm - Complex_Array_Test:Main(System.String[]):int
       -1142 (-63.27 % of base) : 191413.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1110 (-57.72 % of base) : 191423.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])
        -912 (-57.90 % of base) : 252834.dasm - Benchstone.BenchF.InvMt:Bench():bool
        -772 (-45.39 % of base) : 246329.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -772 (-45.39 % of base) : 246391.dasm - DefaultNamespace.MulDimJagAry:Main(System.String[]):int
        -703 (-49.54 % of base) : 252064.dasm - Simple_Array_Test:Main(System.String[]):int
        -625 (-32.57 % of base) : 191393.dasm - Huffman:DoHuffIteration(System.Byte[],System.Byte[],System.Byte[],int,int,huff_node[]):long
        -604 (-58.87 % of base) : 191490.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -555 (-54.84 % of base) : 253759.dasm - CTest:TestArrays1(int,double)
        -527 (-22.16 % of base) : 247835.dasm - classarr:gaussj(MatrixCls,int,MatrixCls,int)
        -524 (-44.03 % of base) : 228426.dasm - SciMark2.LU:factor(System.Double[][],System.Int32[]):int
        -516 (-49.24 % of base) : 191491.dasm - LUDecomp:build_problem(System.Double[][],int,System.Double[])
        -506 (-66.14 % of base) : 250911.dasm - Benchstone.BenchF.InProd:Bench():bool
        -502 (-69.92 % of base) : 252854.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
        -498 (-60.51 % of base) : 246330.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -498 (-60.51 % of base) : 246392.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this

Top method regressions (percentages):
         302 (22.50 % of base) : 239006.dasm - BilinearTest:BilinearInterpol_Vector(System.Double[],System.Double[],double,double,System.Double[],double,double,double):System.Double[]:this
          25 (11.11 % of base) : 209222.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
           6 (10.91 % of base) : 229099.dasm - test:test_001b()
           6 (10.91 % of base) : 229101.dasm - test:test_002b()
           6 (10.91 % of base) : 229077.dasm - test:test_021b()
           6 (10.91 % of base) : 229079.dasm - test:test_022b()
           9 (10.47 % of base) : 251434.dasm - N.C:FalseTrueAnd(System.Int32[]):bool
           9 (10.47 % of base) : 251433.dasm - N.C:FalseTrueOr(System.Int32[]):bool
           9 (10.47 % of base) : 251432.dasm - N.C:TrueFalseAnd(System.Int32[]):bool
           9 (10.47 % of base) : 251431.dasm - N.C:TrueFalseOr(System.Int32[]):bool
           6 (9.52 % of base) : 251294.dasm - SimpleArray_01.Test:Test2()
          29 (9.51 % of base) : 245833.dasm - RuntimeEventListener:Verify():bool:this
         113 (5.26 % of base) : 225369.dasm - IntelHardwareIntrinsicTest.Program:Main(System.String[]):int
           3 (5.00 % of base) : 238914.dasm - BenchmarksGame.Fasta_1:MakeCumulative(BenchmarksGame.Fasta_1+Frequency[])
          51 (4.99 % of base) : 248581.dasm - BenchmarksGame.NBodySystem:.ctor():this
          14 (3.56 % of base) : 233009.dasm - Dynamo.Dynamo:Compare():bool:this
           3 (2.86 % of base) : 164139.dasm - Helper:NewInnerArraySequential(int,float,System.String):InnerArraySequential
           3 (2.86 % of base) : 163955.dasm - Helper:NewInnerArraySequential(int,float,System.String):InnerArraySequential
           3 (2.86 % of base) : 164057.dasm - Helper:NewInnerArraySequential(int,float,System.String):InnerArraySequential
           3 (2.86 % of base) : 164254.dasm - Helper:NewInnerArraySequential(int,float,System.String):InnerArraySequential

Top method improvements (percentages):
        -502 (-69.92 % of base) : 252854.dasm - Benchstone.BenchF.SqMtx:Inner(System.Double[][],System.Double[][],int)
       -2237 (-66.82 % of base) : 250920.dasm - Benchstone.BenchI.MulMatrix:Inner(System.Int32[][],System.Int32[][],System.Int32[][])
        -320 (-66.81 % of base) : 252905.dasm - Benchstone.BenchI.Array2:VerifyCopy(System.Int32[][][],System.Int32[][][]):bool
        -506 (-66.14 % of base) : 250911.dasm - Benchstone.BenchF.InProd:Bench():bool
        -265 (-66.08 % of base) : 252861.dasm - Benchstone.BenchI.XposMatrix:Inner(System.Int32[][],int)
       -1925 (-64.86 % of base) : 191492.dasm - LUDecomp:ludcmp(System.Double[][],int,System.Int32[],byref):int
        -207 (-64.49 % of base) : 252904.dasm - Benchstone.BenchI.Array2:Initialize(System.Int32[][][])
       -1142 (-63.27 % of base) : 191413.dasm - AssignJagged:second_assignments(System.Int32[][],System.Int16[][])
       -1356 (-63.07 % of base) : 252063.dasm - Complex_Array_Test:Main(System.String[]):int
        -152 (-62.81 % of base) : 252894.dasm - Benchstone.BenchI.BubbleSort2:Inner(System.Int32[])
        -241 (-62.60 % of base) : 250892.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAtv(int,System.Double[],System.Double[]):this
        -239 (-62.57 % of base) : 250891.dasm - BenchmarksGame.SpectralNorm_1:MultiplyAv(int,System.Double[],System.Double[]):this
        -498 (-60.51 % of base) : 246330.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -498 (-60.51 % of base) : 246392.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagAry(System.Object[][][],int,int):this
        -498 (-60.51 % of base) : 246331.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -498 (-60.51 % of base) : 246393.dasm - DefaultNamespace.MulDimJagAry:SetThreeDimJagVarAry(System.Object[][][],int,int):this
        -189 (-60.00 % of base) : 228419.dasm - SciMark2.kernel:matvec(System.Double[][],System.Double[],System.Double[])
        -604 (-58.87 % of base) : 191490.dasm - LUDecomp:DoLUIteration(System.Double[][],System.Double[],System.Double[][][],System.Double[][],int):long
        -912 (-57.90 % of base) : 252834.dasm - Benchstone.BenchF.InvMt:Bench():bool
       -1110 (-57.72 % of base) : 191423.dasm - AssignRect:second_assignments(System.Int32[,],System.Int16[,])

358 total methods with Code Size differences (301 improved, 57 regressed), 10 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.crossgen2.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 34283537 (overridden on cmd)
Total bytes of diff: 34176334 (overridden on cmd)
Total bytes of delta: -107203 (-0.31 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
          75 : 36267.dasm (7.35 % of base)
          71 : 166367.dasm (7.79 % of base)
          59 : 199705.dasm (5.34 % of base)
          58 : 175381.dasm (2.67 % of base)
          48 : 31754.dasm (4.58 % of base)
          47 : 136088.dasm (3.46 % of base)
          44 : 139124.dasm (3.62 % of base)
          37 : 187935.dasm (13.91 % of base)
          37 : 188104.dasm (13.91 % of base)
          37 : 161259.dasm (3.94 % of base)
          33 : 55796.dasm (13.25 % of base)
          32 : 170999.dasm (51.61 % of base)
          30 : 208006.dasm (8.82 % of base)
          28 : 107124.dasm (1.15 % of base)
          28 : 168717.dasm (4.38 % of base)
          26 : 140823.dasm (9.12 % of base)
          26 : 110119.dasm (7.83 % of base)
          24 : 119062.dasm (6.49 % of base)
          21 : 176697.dasm (17.95 % of base)
          21 : 199371.dasm (3.98 % of base)

Top file improvements (bytes):
      -11621 : 96498.dasm (-46.83 % of base)
       -4491 : 122474.dasm (-43.64 % of base)
       -2128 : 96496.dasm (-41.01 % of base)
       -1939 : 96485.dasm (-53.65 % of base)
       -1201 : 96580.dasm (-47.21 % of base)
       -1009 : 96492.dasm (-47.96 % of base)
       -1003 : 96583.dasm (-45.97 % of base)
        -847 : 199956.dasm (-37.30 % of base)
        -829 : 84611.dasm (-39.03 % of base)
        -766 : 67007.dasm (-52.07 % of base)
        -759 : 56127.dasm (-38.18 % of base)
        -690 : 109735.dasm (-35.83 % of base)
        -684 : 96490.dasm (-46.03 % of base)
        -663 : 96871.dasm (-27.87 % of base)
        -646 : 199955.dasm (-29.69 % of base)
        -612 : 96879.dasm (-23.42 % of base)
        -600 : 143715.dasm (-34.19 % of base)
        -575 : 4491.dasm (-45.45 % of base)
        -558 : 139806.dasm (-46.31 % of base)
        -558 : 66294.dasm (-43.26 % of base)

1068 total files with Code Size differences (869 improved, 199 regressed), 129 unchanged.

Top method regressions (bytes):
          75 (7.35 % of base) : 36267.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          71 (7.79 % of base) : 166367.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          59 (5.34 % of base) : 199705.dasm - System.Reflection.TypeLoading.Assignability:CanCastTo(System.Type,System.Type,System.Reflection.TypeLoading.CoreTypes):bool
          58 (2.67 % of base) : 175381.dasm - R2RTest.BuildFolder:FromDirectory(System.String,System.Collections.Generic.IEnumerable`1[R2RTest.CompilerRunner],System.String,R2RTest.BuildOptions):R2RTest.BuildFolder
          48 (4.58 % of base) : 31754.dasm - DebugViewPrinter:Analyze():this
          47 (3.46 % of base) : 136088.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          44 (3.62 % of base) : 139124.dasm - System.Xml.Xsl.Runtime.XmlQueryStaticData:GetObjectData(byref,byref):this
          37 (3.94 % of base) : 161259.dasm - Microsoft.CodeAnalysis.TypeNameDecoder`2:GetTypeSymbol(Microsoft.CodeAnalysis.MetadataHelpers+AssemblyQualifiedTypeName,byref):System.__Canon:this
          37 (13.91 % of base) : 187935.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          37 (13.91 % of base) : 188104.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          33 (13.25 % of base) : 55796.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          32 (51.61 % of base) : 170999.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          30 (8.82 % of base) : 208006.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          28 (1.15 % of base) : 107124.dasm - System.Diagnostics.Tracing.EventPipeMetadataGenerator:GenerateMetadata(int,System.String,long,int,int,int,System.Diagnostics.Tracing.EventParameterInfo[]):System.Byte[]:this
          28 (4.38 % of base) : 168717.dasm - System.ServiceProcess.ServiceBase:Run(System.ServiceProcess.ServiceBase[])
          26 (7.83 % of base) : 110119.dasm - System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler:ClearCache(System.Type[])
          26 (9.12 % of base) : 140823.dasm - System.Xml.Xsl.Xslt.XslAstAnalyzer:AddImportDependencies(System.Xml.Xsl.Xslt.Stylesheet,System.Xml.Xsl.Xslt.Template):this
          24 (6.49 % of base) : 119062.dasm - System.TimeZoneInfo:PopulateAllSystemTimeZones(System.TimeZoneInfo+CachedData)
          21 (17.95 % of base) : 176697.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          21 (3.98 % of base) : 199371.dasm - System.Reflection.TypeLoading.RoType:ComputeInterfaceClosure():System.Reflection.TypeLoading.RoType[]:this

Top method improvements (bytes):
      -11621 (-46.83 % of base) : 96498.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -4491 (-43.64 % of base) : 122474.dasm - System.DefaultBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -2128 (-41.01 % of base) : 96496.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -1939 (-53.65 % of base) : 96485.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
       -1201 (-47.21 % of base) : 96580.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
       -1009 (-47.96 % of base) : 96492.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
       -1003 (-45.97 % of base) : 96583.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
        -847 (-37.30 % of base) : 199956.dasm - System.DefaultBinder:SelectMethod(int,System.Reflection.MethodBase[],System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.MethodBase:this
        -829 (-39.03 % of base) : 84611.dasm - Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter:UseTwiceLateInvocation(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.VisualBasic.BoundLateInvocation,Microsoft.CodeAnalysis.ArrayBuilder`1[Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedLocal]):Microsoft.CodeAnalysis.VisualBasic.UseTwiceRewriter+Result
        -766 (-52.07 % of base) : 67007.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -759 (-38.18 % of base) : 56127.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
        -690 (-35.83 % of base) : 109735.dasm - System.Reflection.Emit.MethodBuilder:CreateMethodBodyHelper(System.Reflection.Emit.ILGenerator):this
        -684 (-46.03 % of base) : 96490.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
        -663 (-27.87 % of base) : 96871.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:MoreSpecificProcedure(Microsoft.VisualBasic.CompilerServices.Symbols+Method,Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],int,byref,bool):Microsoft.VisualBasic.CompilerServices.Symbols+Method
        -646 (-29.69 % of base) : 199955.dasm - System.DefaultBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
        -612 (-23.42 % of base) : 96879.dasm - Microsoft.VisualBasic.CompilerServices.OverloadResolution:CanMatchArguments(Microsoft.VisualBasic.CompilerServices.Symbols+Method,System.Object[],System.String[],System.Type[],bool,System.Collections.Generic.List`1[System.String]):bool
        -600 (-34.19 % of base) : 143715.dasm - System.Xml.Schema.ParticleContentValidator:BuildTransitionTable(System.Xml.Schema.BitSet,System.Xml.Schema.BitSet[],int):System.Int32[][]:this
        -575 (-45.45 % of base) : 4491.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -558 (-43.26 % of base) : 66294.dasm - System.Speech.Internal.SrgsCompiler.SrgsCompiler:CompileStream(System.Xml.XmlReader[],System.String,System.IO.Stream,bool,System.Uri,System.String[],System.String)
        -558 (-46.31 % of base) : 139806.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this

Top method regressions (percentages):
          32 (51.61 % of base) : 170999.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          21 (17.95 % of base) : 176697.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          37 (13.91 % of base) : 187935.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          37 (13.91 % of base) : 188104.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          33 (13.25 % of base) : 55796.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          18 (13.14 % of base) : 104000.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
          26 (9.12 % of base) : 140823.dasm - System.Xml.Xsl.Xslt.XslAstAnalyzer:AddImportDependencies(System.Xml.Xsl.Xslt.Stylesheet,System.Xml.Xsl.Xslt.Template):this
          30 (8.82 % of base) : 208006.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          11 (7.91 % of base) : 211008.dasm - Microsoft.Extensions.Primitives.StringValues:IndexOf(System.String):int:this
          26 (7.83 % of base) : 110119.dasm - System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler:ClearCache(System.Type[])
          71 (7.79 % of base) : 166367.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
           9 (7.76 % of base) : 161030.dasm - Microsoft.CodeAnalysis.CommonReferenceManager`2:CheckCircularReference(System.Collections.Generic.IReadOnlyList`1[Microsoft.CodeAnalysis.CommonReferenceManager`2+AssemblyReferenceBinding[System.__Canon, System.__Canon][]]):bool
           8 (7.69 % of base) : 132513.dasm - System.Reflection.Internal.ObjectPool`1:Allocate():System.__Canon:this
          75 (7.35 % of base) : 36267.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          17 (7.23 % of base) : 161505.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
          17 (6.85 % of base) : 123759.dasm - System.Type:FindInterfaces(System.Reflection.TypeFilter,System.Object):System.Type[]:this
          24 (6.49 % of base) : 119062.dasm - System.TimeZoneInfo:PopulateAllSystemTimeZones(System.TimeZoneInfo+CachedData)
           9 (6.21 % of base) : 55029.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          13 (5.88 % of base) : 102201.dasm - Scope:IsAllowedType(System.Type):bool:this
          13 (5.58 % of base) : 181895.dasm - Internal.Cryptography.Pal.FindPal:LaxParseDecimalBigInteger(System.String):System.Numerics.BigInteger

Top method improvements (percentages):
        -213 (-54.20 % of base) : 20966.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,Microsoft.CodeAnalysis.CSharp.OverloadResolution+ParameterMap):System.Nullable`1[System.Int32]
       -1939 (-53.65 % of base) : 96485.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMethodsByName(System.Type,System.Reflection.IReflect,System.String,int):System.Reflection.MethodBase[]:this
        -766 (-52.07 % of base) : 67007.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -208 (-50.73 % of base) : 23515.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
        -422 (-49.01 % of base) : 212460.dasm - System.Web.Util.HttpEncoder:UrlEncodeUnicode(System.String):System.String
        -314 (-48.83 % of base) : 97216.dasm - Microsoft.VisualBasic.CompilerServices.LateBinding:MemberIsField(System.Reflection.MemberInfo[]):bool
        -342 (-48.44 % of base) : 117837.dasm - System.Net.WebUtility:GetEncodedBytes(System.Byte[],int,int,System.Byte[])
        -246 (-48.43 % of base) : 78905.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
       -1009 (-47.96 % of base) : 96492.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:SelectProperty(int,System.Reflection.PropertyInfo[],System.Type,System.Type[],System.Reflection.ParameterModifier[]):System.Reflection.PropertyInfo:this
       -1201 (-47.21 % of base) : 96580.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:InternalWriteHelper(System.Object[]):this
        -315 (-47.01 % of base) : 104012.dasm - System.Data.DataColumnCollection:BaseGroupSwitch(System.Data.DataColumn[],int,System.Data.DataColumn[],int):this
      -11621 (-46.83 % of base) : 96498.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
         -60 (-46.51 % of base) : 182979.dasm - ReplaceEscapeSequenceRule:HexToInt32(System.Char[]):int
        -558 (-46.31 % of base) : 139806.dasm - System.Xml.Xsl.XsltOld.XsltCompileContext:FindBestMethod(System.Reflection.MethodInfo[],bool,bool,System.String,System.Xml.XPath.XPathResultType[]):System.Reflection.MethodInfo:this
        -684 (-46.03 % of base) : 96490.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindingScore(System.Reflection.ParameterInfo[],System.Int32[],System.Type[],bool,int):int:this
       -1003 (-45.97 % of base) : 96583.dasm - Microsoft.VisualBasic.CompilerServices.VB6File:Print(System.Object[]):this
        -575 (-45.45 % of base) : 4491.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.SourceMemberContainerTypeSymbol:CheckInterfaceUnification(Microsoft.CodeAnalysis.DiagnosticBag):this
        -120 (-45.45 % of base) : 137256.dasm - System.Xml.Serialization.ReflectionXmlSerializationReader:InitializeValueTypes(System.Object[],System.Xml.Serialization.MemberMapping[]):this
        -243 (-45.00 % of base) : 102874.dasm - System.Data.DataTableCollection:BaseGroupSwitch(System.Data.DataTable[],int,System.Data.DataTable[],int):this
        -243 (-44.75 % of base) : 104208.dasm - System.Data.ConstraintCollection:BaseGroupSwitch(System.Data.Constraint[],int,System.Data.Constraint[],int):this

1068 total methods with Code Size differences (869 improved, 199 regressed), 129 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries.pmi.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 45344459 (overridden on cmd)
Total bytes of diff: 45172320 (overridden on cmd)
Total bytes of delta: -172139 (-0.38 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         183 : 45627.dasm (9.80 % of base)
         116 : 114009.dasm (3.41 % of base)
         110 : 169614.dasm (13.50 % of base)
         107 : 114023.dasm (3.40 % of base)
         100 : 190972.dasm (2.76 % of base)
          85 : 113967.dasm (3.05 % of base)
          71 : 209601.dasm (6.54 % of base)
          64 : 144146.dasm (4.26 % of base)
          50 : 142758.dasm (1.18 % of base)
          48 : 167989.dasm (1.91 % of base)
          42 : 109334.dasm (1.94 % of base)
          41 : 166681.dasm (1.89 % of base)
          40 : 136298.dasm (3.42 % of base)
          40 : 141134.dasm (2.29 % of base)
          38 : 183911.dasm (3.65 % of base)
          36 : 210989.dasm (56.25 % of base)
          36 : 164421.dasm (7.27 % of base)
          33 : 84604.dasm (13.15 % of base)
          30 : 227984.dasm (8.09 % of base)
          30 : 189014.dasm (3.37 % of base)

Top file improvements (bytes):
      -10397 : 164698.dasm (-42.96 % of base)
       -8009 : 50246.dasm (-67.80 % of base)
       -6533 : 50221.dasm (-27.71 % of base)
       -2630 : 50218.dasm (-45.72 % of base)
       -2367 : 53021.dasm (-47.51 % of base)
       -2147 : 164700.dasm (-37.27 % of base)
       -2108 : 50185.dasm (-32.87 % of base)
       -2042 : 55467.dasm (-49.80 % of base)
       -1880 : 57234.dasm (-48.30 % of base)
       -1872 : 24631.dasm (-48.25 % of base)
       -1786 : 74072.dasm (-47.93 % of base)
       -1774 : 57160.dasm (-34.39 % of base)
       -1744 : 53023.dasm (-49.76 % of base)
       -1684 : 57107.dasm (-24.42 % of base)
       -1654 : 72939.dasm (-27.58 % of base)
       -1556 : 23388.dasm (-42.76 % of base)
       -1552 : 50411.dasm (-16.98 % of base)
       -1523 : 72989.dasm (-28.71 % of base)
       -1502 : 52752.dasm (-49.44 % of base)
       -1408 : 55464.dasm (-50.00 % of base)

1188 total files with Code Size differences (996 improved, 192 regressed), 98 unchanged.

Top method regressions (bytes):
         183 (9.80 % of base) : 45627.dasm - Microsoft.CodeAnalysis.CSharp.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.CSharp.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
         116 (3.41 % of base) : 114009.dasm - System.Data.Common.SqlMoneyStorage:Aggregate(System.Int32[],int):System.Object:this
         110 (13.50 % of base) : 169614.dasm - System.Collections.Concurrent.BlockingCollection`1[Vector`1][System.Numerics.Vector`1[System.Single]]:TryAddToAnyCore(System.Collections.Concurrent.BlockingCollection`1[System.Numerics.Vector`1[System.Single]][],System.Numerics.Vector`1[Single],int,System.Threading.CancellationToken):int
         107 (3.40 % of base) : 114023.dasm - System.Data.Common.SqlSingleStorage:Aggregate(System.Int32[],int):System.Object:this
         100 (2.76 % of base) : 190972.dasm - System.DirectoryServices.Protocols.LdapConnection:SendRequestHelper(System.DirectoryServices.Protocols.DirectoryRequest,byref):int:this
          85 (3.05 % of base) : 113967.dasm - System.Data.Common.SqlInt16Storage:Aggregate(System.Int32[],int):System.Object:this
          71 (6.54 % of base) : 209601.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          64 (4.26 % of base) : 144146.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:WriteMemberEnd(System.Xml.Serialization.XmlSerializationReaderILGen+Member[],bool):this
          50 (1.18 % of base) : 142758.dasm - System.Xml.Serialization.SchemaGraph:Depends(System.Xml.Schema.XmlSchemaObject,System.Collections.ArrayList):this
          48 (1.91 % of base) : 167989.dasm - R2RTest.BuildFolder:FromDirectory(System.String,System.Collections.Generic.IEnumerable`1[[R2RTest.CompilerRunner, R2RTest, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null]],System.String,R2RTest.BuildOptions):R2RTest.BuildFolder
          42 (1.94 % of base) : 109334.dasm - System.Data.DataColumnCollection:CanRemove(System.Data.DataColumn,bool):bool:this
          41 (1.89 % of base) : 166681.dasm - R2RDump.TextDumper:DumpDisasm(ILCompiler.Reflection.ReadyToRun.RuntimeFunction,int):this
          40 (3.42 % of base) : 136298.dasm - System.Xml.Schema.Datatype_List:TryParseValue(System.String,System.Xml.XmlNameTable,System.Xml.IXmlNamespaceResolver,byref):System.Exception:this
          40 (2.29 % of base) : 141134.dasm - System.Xml.Xsl.Runtime.XmlQueryStaticData:GetObjectData(byref,byref):this
          38 (3.65 % of base) : 183911.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          36 (7.27 % of base) : 164421.dasm - Microsoft.VisualBasic.CompilerServices.Symbols:IsOrInheritsFrom(System.Type,System.Type):bool
          36 (56.25 % of base) : 210989.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          33 (13.15 % of base) : 84604.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          30 (3.37 % of base) : 189014.dasm - System.Diagnostics.XmlWriterTraceListener:WriteEscaped(System.String):this
          30 (8.09 % of base) : 227984.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this

Top method improvements (bytes):
      -10397 (-42.96 % of base) : 164698.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:BindToMethod(int,System.Reflection.MethodBase[],byref,System.Reflection.ParameterModifier[],System.Globalization.CultureInfo,System.String[],byref):System.Reflection.MethodBase:this
       -8009 (-67.80 % of base) : 50246.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindFieldAndPropertyInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceMemberContainerTypeSymbol,System.Collections.Immutable.ImmutableArray`1[ImmutableArray`1],Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedInteractiveInitializerMethod,Microsoft.CodeAnalysis.DiagnosticBag):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundInitializer, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
       -6533 (-27.71 % of base) : 50221.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportOverloadResolutionFailureForASingleCandidate(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.Location,int,byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool,bool,bool,bool,Microsoft.CodeAnalysis.DiagnosticBag,Microsoft.CodeAnalysis.VisualBasic.Symbol,bool,Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.Symbol):this
       -2630 (-45.72 % of base) : 50218.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:ReportUnspecificProcedures(Microsoft.CodeAnalysis.Location,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.DiagnosticBag,bool):this
       -2367 (-47.51 % of base) : 53021.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:DetailedParameterCompare(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],byref,int,int):int
       -2147 (-37.27 % of base) : 164700.dasm - Microsoft.VisualBasic.CompilerServices.VBBinder:GetMostSpecific(System.Reflection.MethodBase,System.Reflection.MethodBase,System.Int32[],System.Object[],bool,int,int,System.Object[]):int:this
       -2108 (-32.87 % of base) : 50185.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindLateBoundInvocation(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.BoundMethodOrPropertyGroup,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.DiagnosticBag,bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -2042 (-49.80 % of base) : 55467.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1880 (-48.30 % of base) : 57234.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this
       -1872 (-48.25 % of base) : 24631.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:IsApplicable(Microsoft.CodeAnalysis.CSharp.Symbol,EffectiveParameters,Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,System.Collections.Immutable.ImmutableArray`1[Int32],bool,bool,bool,byref):Microsoft.CodeAnalysis.CSharp.MemberAnalysisResult:this
       -1786 (-47.93 % of base) : 74072.dasm - AsyncMethodToClassRewriter:RewriteSpillSequenceIntoBlock(Microsoft.CodeAnalysis.VisualBasic.BoundSpillSequence,bool,Microsoft.CodeAnalysis.VisualBasic.BoundStatement[]):Microsoft.CodeAnalysis.VisualBasic.BoundBlock:this
       -1774 (-34.39 % of base) : 57160.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateMakeArgumentArrayArgument(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1744 (-49.76 % of base) : 53023.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1684 (-24.42 % of base) : 57107.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:LateCallOrGet(Microsoft.CodeAnalysis.VisualBasic.BoundLateMemberAccess,Microsoft.CodeAnalysis.VisualBasic.BoundExpression,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],bool):Microsoft.CodeAnalysis.VisualBasic.BoundExpression:this
       -1654 (-27.58 % of base) : 72939.dasm - AnonymousDelegatePublicSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1556 (-42.76 % of base) : 23388.dasm - Microsoft.CSharp.RuntimeBinder.Errors.ErrorHandling:Error(int,Microsoft.CSharp.RuntimeBinder.Errors.ErrArg[]):Microsoft.CSharp.RuntimeBinder.RuntimeBinderException
       -1552 (-16.98 % of base) : 50411.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:MakeVarianceConversionSuggestion(int,Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode,Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol,Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol,Microsoft.CodeAnalysis.DiagnosticBag,bool):bool:this
       -1523 (-28.71 % of base) : 72989.dasm - AnonymousDelegateTemplateSymbol:.ctor(Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager,Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeDescriptor):this
       -1502 (-49.44 % of base) : 52752.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceAttributeData:GetTargetAttributeSignatureIndex(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.AttributeDescription):int:this
       -1408 (-50.00 % of base) : 55464.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this

Top method regressions (percentages):
          36 (56.25 % of base) : 210989.dasm - System.Net.WebClient:ByteArrayHasPrefix(System.Byte[],System.Byte[]):bool
          21 (21.43 % of base) : 179333.dasm - System.ComponentModel.ReflectionCachesUpdateHandler:ClearCache(System.Type[])
          26 (14.53 % of base) : 85442.dasm - Microsoft.Diagnostics.Tracing.Utilities.FastStream:ReadAsciiStringUpToAny(System.String,System.Text.StringBuilder):this
          18 (14.29 % of base) : 109293.dasm - System.Data.DataColumnCollection:FinishInitCollection():this
         110 (13.50 % of base) : 169614.dasm - System.Collections.Concurrent.BlockingCollection`1[Vector`1][System.Numerics.Vector`1[System.Single]]:TryAddToAnyCore(System.Collections.Concurrent.BlockingCollection`1[System.Numerics.Vector`1[System.Single]][],System.Numerics.Vector`1[Single],int,System.Threading.CancellationToken):int
          33 (13.15 % of base) : 84604.dasm - Microsoft.Diagnostics.Utilities.DirectoryUtilities:Clean(System.String):int
          25 (11.11 % of base) : 158830.dasm - Internal.TypeSystem.TypeSystemHelpers:RequiresSlotUnification(Internal.TypeSystem.MethodDesc):bool
          29 (10.36 % of base) : 22680.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.MethodTypeInferrer:UpperBoundInterfaceInference(Microsoft.CSharp.RuntimeBinder.Semantics.AggregateType,Microsoft.CSharp.RuntimeBinder.Semantics.CType):bool:this
          29 (10.25 % of base) : 169360.dasm - Microsoft.CSharp.CSharpModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
          29 (10.25 % of base) : 169196.dasm - Microsoft.VisualBasic.VBModifierAttributeConverter:ConvertTo(System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo,System.Object,System.Type):System.Object:this
         183 (9.80 % of base) : 45627.dasm - Microsoft.CodeAnalysis.CSharp.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.CSharp.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
          26 (8.81 % of base) : 109148.dasm - System.Data.DataColumn:IsInRelation():bool:this
           8 (8.51 % of base) : 147987.dasm - System.Reflection.Internal.ObjectPool`1[__Canon][System.__Canon]:Allocate():System.__Canon:this
          25 (8.45 % of base) : 191717.dasm - System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule:SetSchedule(System.DayOfWeek[],int,int,int,int):this
          19 (8.26 % of base) : 78134.dasm - Microsoft.CodeAnalysis.MetadataHelpers:SplitQualifiedName(System.String,byref):System.String
           6 (8.22 % of base) : 144056.dasm - System.Xml.Serialization.XmlSerializationReaderCodeGen:IsSequence(System.Xml.Serialization.XmlSerializationReaderCodeGen+Member[]):bool:this
           6 (8.22 % of base) : 144141.dasm - System.Xml.Serialization.XmlSerializationReaderILGen:IsSequence(System.Xml.Serialization.XmlSerializationReaderILGen+Member[]):bool:this
          11 (8.21 % of base) : 115943.dasm - System.Drawing.Printing.PageSettings:PaperSourceFromMode(DEVMODE):System.Drawing.Printing.PaperSource:this
          30 (8.09 % of base) : 227984.dasm - System.Threading.Tasks.Dataflow.Internal.JoinBlockTargetSharedResources:RetrievePostponedItemsNonGreedy():bool:this
          25 (7.65 % of base) : 141268.dasm - System.Xml.Xsl.Runtime.XsltFunctions:MSStringCompare(System.String,System.String,System.String,System.String):double

Top method improvements (percentages):
       -8009 (-67.80 % of base) : 50246.dasm - Microsoft.CodeAnalysis.VisualBasic.Binder:BindFieldAndPropertyInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceMemberContainerTypeSymbol,System.Collections.Immutable.ImmutableArray`1[ImmutableArray`1],Microsoft.CodeAnalysis.VisualBasic.Symbols.SynthesizedInteractiveInitializerMethod,Microsoft.CodeAnalysis.DiagnosticBag):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundInitializer, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -576 (-55.71 % of base) : 24613.dasm - Microsoft.CodeAnalysis.CSharp.OverloadResolution:NameUsedForPositional(Microsoft.CodeAnalysis.CSharp.AnalyzedArguments,ParameterMap):System.Nullable`1[Int32]
       -1165 (-52.91 % of base) : 29338.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:BuildStoresToTemps(bool,System.Collections.Immutable.ImmutableArray`1[Int32],System.Collections.Immutable.ImmutableArray`1[RefKind],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.BoundExpression, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.BoundAssignmentOperator, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -807 (-51.37 % of base) : 149060.dasm - System.Speech.Internal.PhonemeConverter:DecompressPhoneMaps(System.Speech.Internal.PhonemeConverter+PhoneMapCompressed[]):System.Speech.Internal.PhonemeConverter+PhoneMap[]
        -208 (-50.86 % of base) : 153942.dasm - System.Text.RegularExpressions.Match:TidyBalancing():this
       -1285 (-50.67 % of base) : 29346.dasm - Microsoft.CodeAnalysis.CSharp.LocalRewriter:RewriteArgumentsForComCall(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.BoundExpression[],Microsoft.CodeAnalysis.ArrayBuilder`1[RefKind],Microsoft.CodeAnalysis.ArrayBuilder`1[[Microsoft.CodeAnalysis.CSharp.Symbols.LocalSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):this
        -517 (-50.34 % of base) : 77060.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArray(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -474 (-50.27 % of base) : 73751.dasm - Analyzer:VisitArguments(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]:this
       -1408 (-50.00 % of base) : 55464.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitOnedimensionalElementInitializers(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.BoundExpression, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],bool):this
         -67 (-50.00 % of base) : 211619.dasm - System.Numerics.Tensors.ArrayUtilities:GetIndex(System.Int32[],System.ReadOnlySpan`1[Int32],int):int
       -2042 (-49.80 % of base) : 55467.dasm - Microsoft.CodeAnalysis.VisualBasic.CodeGen.CodeGenerator:EmitAllElementInitializersRecursive(Microsoft.CodeAnalysis.VisualBasic.Symbols.ArrayTypeSymbol,Microsoft.CodeAnalysis.ArrayBuilder`1[IndexDesc],bool):this
       -1744 (-49.76 % of base) : 53023.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.VisualBasic, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSubstitution,bool,bool):bool
       -1502 (-49.44 % of base) : 52752.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceAttributeData:GetTargetAttributeSignatureIndex(Microsoft.CodeAnalysis.VisualBasic.Symbol,Microsoft.CodeAnalysis.AttributeDescription):int:this
         -67 (-49.26 % of base) : 233463.dasm - ReplaceEscapeSequenceRule:HexToInt32(System.Char[]):int
        -265 (-48.71 % of base) : 63335.dasm - Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable:EnsureHalfWidth(System.String):System.String
        -137 (-48.58 % of base) : 20687.dasm - System.Collections.Generic.ObjectEqualityComparer`1[Vector`1][System.Numerics.Vector`1[System.Single]]:IndexOf(System.Numerics.Vector`1[System.Single][],System.Numerics.Vector`1[Single],int,int):int:this
       -1345 (-48.50 % of base) : 42559.dasm - Microsoft.CodeAnalysis.CSharp.Symbols.MemberSignatureComparer:HaveSameParameterTypes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.TypeMap,bool,bool,bool):bool
        -321 (-48.34 % of base) : 21819.dasm - Microsoft.CSharp.RuntimeBinder.RuntimeBinder:PopulateLocalScope(Microsoft.CSharp.RuntimeBinder.ICSharpBinder,Microsoft.CSharp.RuntimeBinder.Semantics.Scope,Microsoft.CSharp.RuntimeBinder.ArgumentObject[],System.Linq.Expressions.Expression[]):Microsoft.CSharp.RuntimeBinder.Semantics.LocalVariableSymbol[]
        -415 (-48.31 % of base) : 24074.dasm - Microsoft.CodeAnalysis.CSharp.Binder:GetAttributes(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Binder, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol, Microsoft.CodeAnalysis.CSharp, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],Microsoft.CodeAnalysis.CSharp.Symbols.CSharpAttributeData[],Microsoft.CodeAnalysis.DiagnosticBag)
       -1880 (-48.30 % of base) : 57234.dasm - Microsoft.CodeAnalysis.VisualBasic.LocalRewriter:VisitAsNewLocalDeclarations(Microsoft.CodeAnalysis.VisualBasic.BoundAsNewLocalDeclarations):Microsoft.CodeAnalysis.VisualBasic.BoundNode:this

1188 total methods with Code Size differences (996 improved, 192 regressed), 98 unchanged.

```

</details>

--------------------------------------------------------------------------------

## libraries_tests.pmi.windows.x64.checked.mch:

```

Summary of Code Size diffs:
(Lower is better)

Total bytes of base: 114254056 (overridden on cmd)
Total bytes of diff: 114162813 (overridden on cmd)
Total bytes of delta: -91243 (-0.08 % of base)
    diff is an improvement.
    relative diff is an improvement.
```
<details>

<summary>Detail diffs</summary>

```


Top file regressions (bytes):
         132 : 114575.dasm (3.06 % of base)
          91 : 107243.dasm (0.52 % of base)
          71 : 308619.dasm (6.54 % of base)
          53 : 230758.dasm (25.24 % of base)
          43 : 261041.dasm (2.74 % of base)
          38 : 219094.dasm (3.65 % of base)
          38 : 214792.dasm (3.65 % of base)
          37 : 268836.dasm (1.55 % of base)
          34 : 319347.dasm (1.77 % of base)
          34 : 254556.dasm (0.41 % of base)
          33 : 217265.dasm (4.43 % of base)
          33 : 221611.dasm (4.43 % of base)
          33 : 323258.dasm (5.96 % of base)
          33 : 71387.dasm (5.96 % of base)
          29 : 150000.dasm (11.28 % of base)
          27 : 134596.dasm (0.54 % of base)
          27 : 184589.dasm (0.95 % of base)
          26 : 117659.dasm (0.55 % of base)
          25 : 132850.dasm (5.83 % of base)
          24 : 336058.dasm (0.43 % of base)

Top file improvements (bytes):
       -1642 : 224017.dasm (-35.73 % of base)
       -1532 : 114377.dasm (-46.79 % of base)
        -755 : 96389.dasm (-6.16 % of base)
        -747 : 6835.dasm (-37.88 % of base)
        -709 : 127506.dasm (-24.71 % of base)
        -582 : 20004.dasm (-20.61 % of base)
        -566 : 3653.dasm (-51.31 % of base)
        -424 : 281849.dasm (-12.70 % of base)
        -400 : 176479.dasm (-24.98 % of base)
        -355 : 240958.dasm (-24.32 % of base)
        -347 : 240959.dasm (-23.41 % of base)
        -337 : 226247.dasm (-47.07 % of base)
        -337 : 179779.dasm (-47.07 % of base)
        -318 : 20008.dasm (-13.05 % of base)
        -307 : 175870.dasm (-26.37 % of base)
        -307 : 176398.dasm (-26.37 % of base)
        -297 : 175901.dasm (-21.23 % of base)
        -291 : 176760.dasm (-17.41 % of base)
        -289 : 230252.dasm (-46.39 % of base)
        -288 : 114367.dasm (-11.70 % of base)

1365 total files with Code Size differences (1119 improved, 246 regressed), 92 unchanged.

Top method regressions (bytes):
         132 (3.06 % of base) : 114575.dasm - Microsoft.Build.Tasks.ResolveAssemblyReference:LogInputs():this
          91 (0.52 % of base) : 107243.dasm - <ArrayAsRootObject>d__7:MoveNext():this
          71 (6.54 % of base) : 308619.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          53 (25.24 % of base) : 230758.dasm - EmittingVisitor:TryEmitLabel(FastExpressionCompiler.LightExpression.LabelExpression,System.Collections.Generic.IReadOnlyList`1[[FastExpressionCompiler.LightExpression.ParameterExpression, DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]],System.Reflection.Emit.ILGenerator,byref,int):bool
          43 (2.74 % of base) : 261041.dasm - NuGet.ProjectModel.PackageSpec:GetHashCode():int:this
          38 (3.65 % of base) : 219094.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          38 (3.65 % of base) : 214792.dasm - System.Data.ProviderBase.DbConnectionFactory:PruneConnectionPoolGroups(System.Object):this
          37 (1.55 % of base) : 268836.dasm - System.Collections.Concurrent.Tests.BlockingCollectionTests:AddAnyTakeAny(int,int,int,System.Collections.Concurrent.BlockingCollection`1[Int32],System.Collections.Concurrent.BlockingCollection`1[System.Int32][],int)
          34 (0.41 % of base) : 254556.dasm - Microsoft.VisualStudio.Composition.AttributedPartDiscoveryV1:CreatePart(System.Type,bool):Microsoft.VisualStudio.Composition.ComposablePartDefinition:this
          34 (1.77 % of base) : 319347.dasm - System.Reflection.Metadata.Tests.MetadataReaderTests:ObfuscateWithExtraData(System.Byte[],bool):System.Byte[]
          33 (4.43 % of base) : 217265.dasm - System.Data.SqlClient.SNI.SNITCPHandle:Connect(System.String,int,System.TimeSpan):System.Net.Sockets.Socket
          33 (4.43 % of base) : 221611.dasm - System.Data.SqlClient.SNI.SNITCPHandle:Connect(System.String,int,System.TimeSpan):System.Net.Sockets.Socket
          33 (5.96 % of base) : 323258.dasm - System.Tests.CharTests:IsSurrogatePair_Char()
          33 (5.96 % of base) : 71387.dasm - System.Tests.CharTests:IsSurrogatePair_Char()
          29 (11.28 % of base) : 150000.dasm - CatalogListener:ContainsChanges():bool:this
          27 (0.54 % of base) : 134596.dasm - <EnumerateMemoryRegions>d__62:MoveNext():bool:this
          27 (0.95 % of base) : 184589.dasm - <GetAsync_SetCookieContainerMultipleCookies_CookiesSent>d__8:MoveNext():this
          26 (0.55 % of base) : 117659.dasm - Microsoft.Build.Construction.SolutionProjectGenerator:CreateTraversalInstance(System.String,bool,System.Collections.Generic.List`1[[Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]):Microsoft.Build.Execution.ProjectInstance:this
          25 (5.83 % of base) : 132850.dasm - Microsoft.Diagnostics.Runtime.Desktop.DesktopRuntimeBase:GetILMap(long):Microsoft.Diagnostics.Runtime.ILToNativeMap[]:this
          24 (1.10 % of base) : 254845.dasm - Microsoft.VisualStudio.Composition.ImportMetadataViewConstraint:IsSatisfiedBy(Microsoft.VisualStudio.Composition.ExportDefinition):bool:this

Top method improvements (bytes):
       -1642 (-35.73 % of base) : 224017.dasm - Castle.DynamicProxy.Generators.InvocationTypeGenerator:ImplementInvokeMethodOnTarget(Castle.DynamicProxy.Generators.Emitters.AbstractTypeEmitter,System.Reflection.ParameterInfo[],Castle.DynamicProxy.Generators.Emitters.MethodEmitter,Castle.DynamicProxy.Generators.Emitters.SimpleAST.Reference):this
       -1532 (-46.79 % of base) : 114377.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -755 (-6.16 % of base) : 96389.dasm - System.Text.Json.Tests.Utf8JsonWriterTests:WriteNumbers(bool,bool,System.String):this
        -747 (-37.88 % of base) : 6835.dasm - Microsoft.CodeAnalysis.Shared.Extensions.IMethodSymbolExtensions:RenameTypeParameters(System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Collections.Immutable.ImmutableArray`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]],Microsoft.CodeAnalysis.Shared.Extensions.ITypeGenerator):System.Collections.Immutable.ImmutableArray`1[[Microsoft.CodeAnalysis.ITypeParameterSymbol, Microsoft.CodeAnalysis, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]
        -709 (-24.71 % of base) : 127506.dasm - Expander:VisitParenthesizedLambdaExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax):Microsoft.CodeAnalysis.SyntaxNode:this
        -582 (-20.61 % of base) : 20004.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddAfter_LLNode():this
        -566 (-51.31 % of base) : 3653.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -424 (-12.70 % of base) : 281849.dasm - System.Diagnostics.Tests.DiagnosticSourceEventSourceBridgeTests:<TestEnableAllActivitySourcesWithOneEvent>b__1_0(System.String):this
        -400 (-24.98 % of base) : 176479.dasm - System.SpanTests.SpanTests:SequenceEqualNoMatch(int)
        -355 (-24.32 % of base) : 240958.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[__Canon,__Canon,__Canon,__Canon]
        -347 (-23.41 % of base) : 240959.dasm - Microsoft.AspNetCore.Builder.UseMiddlewareExtensions:Compile(System.Reflection.MethodInfo,System.Reflection.ParameterInfo[]):System.Func`4[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60],[System.IServiceProvider, System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a],[System.Threading.Tasks.Task, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        -337 (-47.07 % of base) : 226247.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -337 (-47.07 % of base) : 179779.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -318 (-13.05 % of base) : 20008.dasm - System.Collections.Tests.LinkedList_Generic_Tests`1[Byte][System.Byte]:AddBefore_LLNode():this
        -307 (-26.37 % of base) : 175870.dasm - System.SpanTests.ReadOnlySpanTests:StartsWithNoMatch()
        -307 (-26.37 % of base) : 176398.dasm - System.SpanTests.SpanTests:StartsWithNoMatch()
        -297 (-21.23 % of base) : 175901.dasm - System.SpanTests.ReadOnlySpanTests:SequenceEqualNoMatch(int)
        -291 (-17.41 % of base) : 176760.dasm - System.SpanTests.SpanTests:TestMatchIndexOfAny_ManyString()
        -289 (-46.39 % of base) : 230252.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
        -288 (-11.70 % of base) : 114367.dasm - Microsoft.Build.Tasks.ManagedRuntimeVersionReader:GetRuntimeVersion(System.String):System.String

Top method regressions (percentages):
          53 (25.24 % of base) : 230758.dasm - EmittingVisitor:TryEmitLabel(FastExpressionCompiler.LightExpression.LabelExpression,System.Collections.Generic.IReadOnlyList`1[[FastExpressionCompiler.LightExpression.ParameterExpression, DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]],System.Reflection.Emit.ILGenerator,byref,int):bool
          29 (11.28 % of base) : 150000.dasm - CatalogListener:ContainsChanges():bool:this
          12 (10.26 % of base) : 39935.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          12 (10.26 % of base) : 41692.dasm - System.Globalization.Tests.CultureInfoAll:GetMonthNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          10 (10.00 % of base) : 1618.dasm - Roslyn.Utilities.PathUtilities:PathHashCode(System.String):int
           9 (9.38 % of base) : 132768.dasm - Microsoft.Diagnostics.Runtime.Desktop.DesktopMethod:GetILOffset(long):int:this
          22 (8.98 % of base) : 240302.dasm - LightInject.ServiceContainer:GetAllInstances(System.Type):System.Collections.Generic.IEnumerable`1[[System.Object, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]:this
          19 (8.48 % of base) : 206973.dasm - System.Threading.Tasks.Tests.ContinueWithAllAny.TaskContinueWithAllAnyTest:VerifyAll(System.Threading.Tasks.Task[]):this
          19 (8.48 % of base) : 206974.dasm - System.Threading.Tasks.Tests.ContinueWithAllAny.TaskContinueWithAllAnyTest:VerifyAllT(System.Threading.Tasks.Task`1[System.Double][]):this
          12 (7.55 % of base) : 39937.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
          12 (7.55 % of base) : 41693.dasm - System.Globalization.Tests.CultureInfoAll:GetDayNames(System.Globalization.CultureInfo,int,int):System.String[]:this
           7 (7.29 % of base) : 230616.dasm - <>c__45`1[Byte][System.Byte]:<Visit>b__45_0(ImTools.ImMapEntry`1[KValue`1],bool,System.Action`1[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]]):bool:this
          22 (7.28 % of base) : 240902.dasm - <>c__DisplayClass3_0`1[Vector`1][System.Numerics.Vector`1[System.Single]]:<CreateScopedLazy>b__0():System.Numerics.Vector`1[Single]:this
          22 (7.28 % of base) : 240888.dasm - <>c__DisplayClass5_0`1[Vector`1][System.Numerics.Vector`1[System.Single]]:<CreateScopedGenericFunc>b__0():System.Numerics.Vector`1[Single]:this
           8 (6.90 % of base) : 139369.dasm - NuGet.Packaging.Signing.Rfc3161TimestampUtils:IsLegalOid(System.String):bool
           7 (6.73 % of base) : 230614.dasm - <>c__45`1[__Canon][System.__Canon]:<Visit>b__45_0(ImTools.ImMapEntry`1[KValue`1],bool,System.Action`1[__Canon]):bool:this
          71 (6.54 % of base) : 308619.dasm - System.Net.Security.SslStreamCertificateContext:.ctor(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Certificate2[],System.Net.Security.SslCertificateTrust):this
          10 (6.41 % of base) : 114894.dasm - Microsoft.Build.Tasks.CreateItem:GetUniqueItems(Microsoft.Build.Framework.ITaskItem[]):System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
           7 (6.14 % of base) : 230612.dasm - <>c__44`2[Byte,Nullable`1][System.Byte,System.Nullable`1[System.Int32]]:<Visit>b__44_0(ImTools.ImMapEntry`1[KValue`1],System.Nullable`1[Int32],System.Action`2[[ImTools.ImMapEntry`1[[ImTools.ImMap+KValue`1[[System.Byte, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768]], DryIoc, Version=4.1.4.0, Culture=neutral, PublicKeyToken=dfbf2bd50fcf7768],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]):System.Nullable`1[Int32]:this
          33 (5.96 % of base) : 323258.dasm - System.Tests.CharTests:IsSurrogatePair_Char()

Top method improvements (percentages):
        -566 (-51.31 % of base) : 3653.dasm - Microsoft.CodeAnalysis.ImmutableArrayExtensions:WhereAsArrayImpl(System.Collections.Immutable.ImmutableArray`1[__Canon],System.Func`2[__Canon,Boolean],System.Func`3[__Canon,Nullable`1,Boolean],System.Nullable`1[Int32]):System.Collections.Immutable.ImmutableArray`1[__Canon]
        -337 (-47.07 % of base) : 226247.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
        -337 (-47.07 % of base) : 179779.dasm - System.Net.Http.HPack.Huffman:GenerateDecodingLookupTree():System.UInt16[]
       -1532 (-46.79 % of base) : 114377.dasm - Microsoft.Build.Tasks.AssemblyResolution:CompileSearchPaths(Microsoft.Build.Framework.IBuildEngine,System.String[],System.String[],int,System.String[],Microsoft.Build.Shared.FileExists,Microsoft.Build.Tasks.GetAssemblyName,Microsoft.Build.Tasks.InstalledAssemblies,Microsoft.Build.Tasks.GetAssemblyRuntimeVersion,System.Version,Microsoft.Build.Tasks.GetAssemblyPathInGac,Microsoft.Build.Utilities.TaskLoggingHelper):Microsoft.Build.Tasks.Resolver[]
        -289 (-46.39 % of base) : 230252.dasm - DryIoc.ReflectionFactory:MatchOpenGenericConstraints(System.Type[],System.Type[])
        -168 (-45.78 % of base) : 192935.dasm - System.Runtime.Serialization.Formatters.Tests.EqualityHelpers:ArraysAreEqual(System.Byte[][],System.Byte[][]):bool
         -66 (-45.21 % of base) : 288871.dasm - System.IO.Compression.Tests.ZipFileTestBase:ArraysEqual(System.Byte[],System.Byte[],int):bool
         -66 (-45.21 % of base) : 289339.dasm - System.IO.Compression.Tests.ZipFileTestBase:ArraysEqual(System.Byte[],System.Byte[],int):bool
        -143 (-44.41 % of base) : 1926.dasm - Roslyn.Utilities.EditDistance:ConvertToLowercaseArray(System.String):System.Char[]
         -87 (-43.50 % of base) : 264734.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 305081.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 306956.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 226073.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 295443.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 332553.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 334498.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 288297.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 288636.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
         -87 (-43.50 % of base) : 59057.dasm - System.Net.MultiArrayBuffer:FreeBlocks(int,int):this
        -178 (-43.31 % of base) : 346525.dasm - System.Xml.Tests.CXmlDriverEngine:FindElementAndRemoveIt(System.String,int,System.Xml.Linq.XElement[],int):System.Xml.Linq.XElement

1365 total methods with Code Size differences (1119 improved, 246 regressed), 92 unchanged.

```

</details>

--------------------------------------------------------------------------------


</details>

