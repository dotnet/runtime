// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOT Int32 Example in IL
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'Not-Int32'
{
}
.class public auto ansi Test_Not_Int32
{
.method public static int32  Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack  2
.locals init (int32 ReturnValue)
TestCase5000:
   ldc.i4 5100
   stloc.0
   ldc.i4 0x80000001
   conv.i4
   not
   ldc.i4 0x7ffffffe
   beq TestCase4999
   br Done

TestCase4999:
   ldc.i4 5099
   stloc.0
   ldc.i4 0x80000002
   conv.i4
   not
   ldc.i4 0x7ffffffd
   beq TestCase4998
   br Done

TestCase4998:
   ldc.i4 5098
   stloc.0
   ldc.i4 0x80000003
   conv.i4
   not
   ldc.i4 0x7ffffffc
   beq TestCase4997
   br Done

TestCase4997:
   ldc.i4 5097
   stloc.0
   ldc.i4 0x80000004
   conv.i4
   not
   ldc.i4 0x7ffffffb
   beq TestCase4996
   br Done

TestCase4996:
   ldc.i4 5096
   stloc.0
   ldc.i4 0x80000005
   conv.i4
   not
   ldc.i4 0x7ffffffa
   bne.un Done

TestCase4995:
   ldc.i4 5095
   stloc.0
   ldc.i4 0x80000006
   conv.i4
   not
   ldc.i4 0x7ffffff9
   bne.un Done

TestCase4994:
   ldc.i4 5094
   stloc.0
   ldc.i4 0x80000007
   conv.i4
   not
   ldc.i4 0x7ffffff8
   beq TestCase4993
   br Done

TestCase4993:
   ldc.i4 5093
   stloc.0
   ldc.i4 0x80000008
   conv.i4
   not
   ldc.i4 0x7ffffff7
   beq TestCase4992
   br Done

TestCase4992:
   ldc.i4 5092
   stloc.0
   ldc.i4 0x80000009
   conv.i4
   not
   ldc.i4 0x7ffffff6
   beq TestCase4991
   br Done

TestCase4991:
   ldc.i4 5091
   stloc.0
   ldc.i4 0x8000000a
   conv.i4
   not
   ldc.i4 0x7ffffff5
   bne.un Done

TestCase4990:
   ldc.i4 5090
   stloc.0
   ldc.i4 0x8000000b
   conv.i4
   not
   ldc.i4 0x7ffffff4
   bne.un Done

TestCase4989:
   ldc.i4 5089
   stloc.0
   ldc.i4 0x8000000c
   conv.i4
   not
   ldc.i4 0x7ffffff3
   bne.un Done

TestCase4988:
   ldc.i4 5088
   stloc.0
   ldc.i4 0x8000000d
   conv.i4
   not
   ldc.i4 0x7ffffff2
   bne.un Done

TestCase4987:
   ldc.i4 5087
   stloc.0
   ldc.i4 0x8000000e
   conv.i4
   not
   ldc.i4 0x7ffffff1
   bne.un Done

TestCase4986:
   ldc.i4 5086
   stloc.0
   ldc.i4 0x8000000f
   conv.i4
   not
   ldc.i4 0x7ffffff0
   bne.un Done

TestCase4985:
   ldc.i4 5085
   stloc.0
   ldc.i4 0x80000010
   conv.i4
   not
   ldc.i4 0x7fffffef
   beq TestCase4984
   br Done

TestCase4984:
   ldc.i4 5084
   stloc.0
   ldc.i4 0x80000011
   conv.i4
   not
   ldc.i4 0x7fffffee
   beq TestCase4983
   br Done

TestCase4983:
   ldc.i4 5083
   stloc.0
   ldc.i4 0x80000012
   conv.i4
   not
   ldc.i4 0x7fffffed
   beq TestCase4982
   br Done

TestCase4982:
   ldc.i4 5082
   stloc.0
   ldc.i4 0x80000013
   conv.i4
   not
   ldc.i4 0x7fffffec
   bne.un Done

TestCase4981:
   ldc.i4 5081
   stloc.0
   ldc.i4 0x80000014
   conv.i4
   not
   ldc.i4 0x7fffffeb
   beq TestCase4980
   br Done

TestCase4980:
   ldc.i4 5080
   stloc.0
   ldc.i4 0x80000015
   conv.i4
   not
   ldc.i4 0x7fffffea
   beq TestCase4979
   br Done

TestCase4979:
   ldc.i4 5079
   stloc.0
   ldc.i4 0x80000016
   conv.i4
   not
   ldc.i4 0x7fffffe9
   beq TestCase4978
   br Done

TestCase4978:
   ldc.i4 5078
   stloc.0
   ldc.i4 0x80000017
   conv.i4
   not
   ldc.i4 0x7fffffe8
   beq TestCase4977
   br Done

TestCase4977:
   ldc.i4 5077
   stloc.0
   ldc.i4 0x80000018
   conv.i4
   not
   ldc.i4 0x7fffffe7
   bne.un Done

TestCase4976:
   ldc.i4 5076
   stloc.0
   ldc.i4 0x80000019
   conv.i4
   not
   ldc.i4 0x7fffffe6
   bne.un Done

TestCase4975:
   ldc.i4 5075
   stloc.0
   ldc.i4 0x8000001a
   conv.i4
   not
   ldc.i4 0x7fffffe5
   beq TestCase4974
   br Done

TestCase4974:
   ldc.i4 5074
   stloc.0
   ldc.i4 0x8000001b
   conv.i4
   not
   ldc.i4 0x7fffffe4
   beq TestCase4973
   br Done

TestCase4973:
   ldc.i4 5073
   stloc.0
   ldc.i4 0x8000001c
   conv.i4
   not
   ldc.i4 0x7fffffe3
   beq TestCase4972
   br Done

TestCase4972:
   ldc.i4 5072
   stloc.0
   ldc.i4 0x8000001d
   conv.i4
   not
   ldc.i4 0x7fffffe2
   beq TestCase4971
   br Done

TestCase4971:
   ldc.i4 5071
   stloc.0
   ldc.i4 0x8000001e
   conv.i4
   not
   ldc.i4 0x7fffffe1
   bne.un Done

TestCase4970:
   ldc.i4 5070
   stloc.0
   ldc.i4 0x8000001f
   conv.i4
   not
   ldc.i4 0x7fffffe0
   bne.un Done

TestCase4969:
   ldc.i4 5069
   stloc.0
   ldc.i4 0x80000020
   conv.i4
   not
   ldc.i4 0x7fffffdf
   bne.un Done

TestCase4968:
   ldc.i4 5068
   stloc.0
   ldc.i4 0x80000021
   conv.i4
   not
   ldc.i4 0x7fffffde
   bne.un Done

TestCase4967:
   ldc.i4 5067
   stloc.0
   ldc.i4 0x80000022
   conv.i4
   not
   ldc.i4 0x7fffffdd
   beq TestCase4966
   br Done

TestCase4966:
   ldc.i4 5066
   stloc.0
   ldc.i4 0x80000023
   conv.i4
   not
   ldc.i4 0x7fffffdc
   beq TestCase4965
   br Done

TestCase4965:
   ldc.i4 5065
   stloc.0
   ldc.i4 0x80000024
   conv.i4
   not
   ldc.i4 0x7fffffdb
   bne.un Done

TestCase4964:
   ldc.i4 5064
   stloc.0
   ldc.i4 0x80000025
   conv.i4
   not
   ldc.i4 0x7fffffda
   beq TestCase4963
   br Done

TestCase4963:
   ldc.i4 5063
   stloc.0
   ldc.i4 0x80000026
   conv.i4
   not
   ldc.i4 0x7fffffd9
   beq TestCase4962
   br Done

TestCase4962:
   ldc.i4 5062
   stloc.0
   ldc.i4 0x80000027
   conv.i4
   not
   ldc.i4 0x7fffffd8
   beq TestCase4961
   br Done

TestCase4961:
   ldc.i4 5061
   stloc.0
   ldc.i4 0x80000028
   conv.i4
   not
   ldc.i4 0x7fffffd7
   beq TestCase4960
   br Done

TestCase4960:
   ldc.i4 5060
   stloc.0
   ldc.i4 0x80000029
   conv.i4
   not
   ldc.i4 0x7fffffd6
   beq TestCase4959
   br Done

TestCase4959:
   ldc.i4 5059
   stloc.0
   ldc.i4 0x8000002a
   conv.i4
   not
   ldc.i4 0x7fffffd5
   bne.un Done

TestCase4958:
   ldc.i4 5058
   stloc.0
   ldc.i4 0x8000002b
   conv.i4
   not
   ldc.i4 0x7fffffd4
   bne.un Done

TestCase4957:
   ldc.i4 5057
   stloc.0
   ldc.i4 0x8000002c
   conv.i4
   not
   ldc.i4 0x7fffffd3
   bne.un Done

TestCase4956:
   ldc.i4 5056
   stloc.0
   ldc.i4 0x8000002d
   conv.i4
   not
   ldc.i4 0x7fffffd2
   bne.un Done

TestCase4955:
   ldc.i4 5055
   stloc.0
   ldc.i4 0x8000002e
   conv.i4
   not
   ldc.i4 0x7fffffd1
   bne.un Done

TestCase4954:
   ldc.i4 5054
   stloc.0
   ldc.i4 0x8000002f
   conv.i4
   not
   ldc.i4 0x7fffffd0
   bne.un Done

TestCase4953:
   ldc.i4 5053
   stloc.0
   ldc.i4 0x80000030
   conv.i4
   not
   ldc.i4 0x7fffffcf
   beq TestCase4952
   br Done

TestCase4952:
   ldc.i4 5052
   stloc.0
   ldc.i4 0x80000031
   conv.i4
   not
   ldc.i4 0x7fffffce
   beq TestCase4951
   br Done

TestCase4951:
   ldc.i4 5051
   stloc.0
   ldc.i4 0x80000032
   conv.i4
   not
   ldc.i4 0x7fffffcd
   beq TestCase4950
   br Done

TestCase4950:
   ldc.i4 5050
   stloc.0
   ldc.i4 0x80000033
   conv.i4
   not
   ldc.i4 0x7fffffcc
   bne.un Done

TestCase4949:
   ldc.i4 5049
   stloc.0
   ldc.i4 0x80000034
   conv.i4
   not
   ldc.i4 0x7fffffcb
   bne.un Done

TestCase4948:
   ldc.i4 5048
   stloc.0
   ldc.i4 0x80000035
   conv.i4
   not
   ldc.i4 0x7fffffca
   bne.un Done

TestCase4947:
   ldc.i4 5047
   stloc.0
   ldc.i4 0x80000036
   conv.i4
   not
   ldc.i4 0x7fffffc9
   bne.un Done

TestCase4946:
   ldc.i4 5046
   stloc.0
   ldc.i4 0x80000037
   conv.i4
   not
   ldc.i4 0x7fffffc8
   beq TestCase4945
   br Done

TestCase4945:
   ldc.i4 5045
   stloc.0
   ldc.i4 0x80000038
   conv.i4
   not
   ldc.i4 0x7fffffc7
   beq TestCase4944
   br Done

TestCase4944:
   ldc.i4 5044
   stloc.0
   ldc.i4 0x80000039
   conv.i4
   not
   ldc.i4 0x7fffffc6
   beq TestCase4943
   br Done

TestCase4943:
   ldc.i4 5043
   stloc.0
   ldc.i4 0x8000003a
   conv.i4
   not
   ldc.i4 0x7fffffc5
   beq TestCase4942
   br Done

TestCase4942:
   ldc.i4 5042
   stloc.0
   ldc.i4 0x8000003b
   conv.i4
   not
   ldc.i4 0x7fffffc4
   bne.un Done

TestCase4941:
   ldc.i4 5041
   stloc.0
   ldc.i4 0x8000003c
   conv.i4
   not
   ldc.i4 0x7fffffc3
   beq TestCase4940
   br Done

TestCase4940:
   ldc.i4 5040
   stloc.0
   ldc.i4 0x8000003d
   conv.i4
   not
   ldc.i4 0x7fffffc2
   beq TestCase4939
   br Done

TestCase4939:
   ldc.i4 5039
   stloc.0
   ldc.i4 0x8000003e
   conv.i4
   not
   ldc.i4 0x7fffffc1
   beq TestCase4938
   br Done

TestCase4938:
   ldc.i4 5038
   stloc.0
   ldc.i4 0x8000003f
   conv.i4
   not
   ldc.i4 0x7fffffc0
   bne.un Done

TestCase4937:
   ldc.i4 5037
   stloc.0
   ldc.i4 0x80000040
   conv.i4
   not
   ldc.i4 0x7fffffbf
   bne.un Done

TestCase4936:
   ldc.i4 5036
   stloc.0
   ldc.i4 0x80000041
   conv.i4
   not
   ldc.i4 0x7fffffbe
   bne.un Done

TestCase4935:
   ldc.i4 5035
   stloc.0
   ldc.i4 0x80000042
   conv.i4
   not
   ldc.i4 0x7fffffbd
   bne.un Done

TestCase4934:
   ldc.i4 5034
   stloc.0
   ldc.i4 0x80000043
   conv.i4
   not
   ldc.i4 0x7fffffbc
   bne.un Done

TestCase4933:
   ldc.i4 5033
   stloc.0
   ldc.i4 0x80000044
   conv.i4
   not
   ldc.i4 0x7fffffbb
   bne.un Done

TestCase4932:
   ldc.i4 5032
   stloc.0
   ldc.i4 0x80000045
   conv.i4
   not
   ldc.i4 0x7fffffba
   beq TestCase4931
   br Done

TestCase4931:
   ldc.i4 5031
   stloc.0
   ldc.i4 0x80000046
   conv.i4
   not
   ldc.i4 0x7fffffb9
   beq TestCase4930
   br Done

TestCase4930:
   ldc.i4 5030
   stloc.0
   ldc.i4 0x80000047
   conv.i4
   not
   ldc.i4 0x7fffffb8
   bne.un Done

TestCase4929:
   ldc.i4 5029
   stloc.0
   ldc.i4 0x80000048
   conv.i4
   not
   ldc.i4 0x7fffffb7
   bne.un Done

TestCase4928:
   ldc.i4 5028
   stloc.0
   ldc.i4 0x80000049
   conv.i4
   not
   ldc.i4 0x7fffffb6
   bne.un Done

TestCase4927:
   ldc.i4 5027
   stloc.0
   ldc.i4 0x8000004a
   conv.i4
   not
   ldc.i4 0x7fffffb5
   bne.un Done

TestCase4926:
   ldc.i4 5026
   stloc.0
   ldc.i4 0x8000004b
   conv.i4
   not
   ldc.i4 0x7fffffb4
   bne.un Done

TestCase4925:
   ldc.i4 5025
   stloc.0
   ldc.i4 0x8000004c
   conv.i4
   not
   ldc.i4 0x7fffffb3
   bne.un Done

TestCase4924:
   ldc.i4 5024
   stloc.0
   ldc.i4 0x8000004d
   conv.i4
   not
   ldc.i4 0x7fffffb2
   bne.un Done

TestCase4923:
   ldc.i4 5023
   stloc.0
   ldc.i4 0x8000004e
   conv.i4
   not
   ldc.i4 0x7fffffb1
   beq TestCase4922
   br Done

TestCase4922:
   ldc.i4 5022
   stloc.0
   ldc.i4 0x8000004f
   conv.i4
   not
   ldc.i4 0x7fffffb0
   beq TestCase4921
   br Done

TestCase4921:
   ldc.i4 5021
   stloc.0
   ldc.i4 0x80000050
   conv.i4
   not
   ldc.i4 0x7fffffaf
   bne.un Done

TestCase4920:
   ldc.i4 5020
   stloc.0
   ldc.i4 0x80000051
   conv.i4
   not
   ldc.i4 0x7fffffae
   beq TestCase4919
   br Done

TestCase4919:
   ldc.i4 5019
   stloc.0
   ldc.i4 0x80000052
   conv.i4
   not
   ldc.i4 0x7fffffad
   bne.un Done

TestCase4918:
   ldc.i4 5018
   stloc.0
   ldc.i4 0x80000053
   conv.i4
   not
   ldc.i4 0x7fffffac
   beq TestCase4917
   br Done

TestCase4917:
   ldc.i4 5017
   stloc.0
   ldc.i4 0x80000054
   conv.i4
   not
   ldc.i4 0x7fffffab
   bne.un Done

TestCase4916:
   ldc.i4 5016
   stloc.0
   ldc.i4 0x80000055
   conv.i4
   not
   ldc.i4 0x7fffffaa
   beq TestCase4915
   br Done

TestCase4915:
   ldc.i4 5015
   stloc.0
   ldc.i4 0x80000056
   conv.i4
   not
   ldc.i4 0x7fffffa9
   bne.un Done

TestCase4914:
   ldc.i4 5014
   stloc.0
   ldc.i4 0x80000057
   conv.i4
   not
   ldc.i4 0x7fffffa8
   beq TestCase4913
   br Done

TestCase4913:
   ldc.i4 5013
   stloc.0
   ldc.i4 0x80000058
   conv.i4
   not
   ldc.i4 0x7fffffa7
   bne.un Done

TestCase4912:
   ldc.i4 5012
   stloc.0
   ldc.i4 0x80000059
   conv.i4
   not
   ldc.i4 0x7fffffa6
   beq TestCase4911
   br Done

TestCase4911:
   ldc.i4 5011
   stloc.0
   ldc.i4 0x8000005a
   conv.i4
   not
   ldc.i4 0x7fffffa5
   bne.un Done

TestCase4910:
   ldc.i4 5010
   stloc.0
   ldc.i4 0x8000005b
   conv.i4
   not
   ldc.i4 0x7fffffa4
   beq TestCase4909
   br Done

TestCase4909:
   ldc.i4 5009
   stloc.0
   ldc.i4 0x8000005c
   conv.i4
   not
   ldc.i4 0x7fffffa3
   beq TestCase4908
   br Done

TestCase4908:
   ldc.i4 5008
   stloc.0
   ldc.i4 0x8000005d
   conv.i4
   not
   ldc.i4 0x7fffffa2
   bne.un Done

TestCase4907:
   ldc.i4 5007
   stloc.0
   ldc.i4 0x8000005e
   conv.i4
   not
   ldc.i4 0x7fffffa1
   bne.un Done

TestCase4906:
   ldc.i4 5006
   stloc.0
   ldc.i4 0x8000005f
   conv.i4
   not
   ldc.i4 0x7fffffa0
   bne.un Done

TestCase4905:
   ldc.i4 5005
   stloc.0
   ldc.i4 0x80000060
   conv.i4
   not
   ldc.i4 0x7fffff9f
   beq TestCase4904
   br Done

TestCase4904:
   ldc.i4 5004
   stloc.0
   ldc.i4 0x80000061
   conv.i4
   not
   ldc.i4 0x7fffff9e
   bne.un Done

TestCase4903:
   ldc.i4 5003
   stloc.0
   ldc.i4 0x80000062
   conv.i4
   not
   ldc.i4 0x7fffff9d
   beq TestCase4902
   br Done

TestCase4902:
   ldc.i4 5002
   stloc.0
   ldc.i4 0x80000063
   conv.i4
   not
   ldc.i4 0x7fffff9c
   beq TestCase4901
   br Done

TestCase4901:
   ldc.i4 5001
   stloc.0
   ldc.i4 0x80000064
   conv.i4
   not
   ldc.i4 0x7fffff9b
   bne.un Done

TestCase4900:
   ldc.i4 5000
   stloc.0
   ldc.i4 0x80000065
   conv.i4
   not
   ldc.i4 0x7fffff9a
   bne.un Done

TestCase4899:
   ldc.i4 4999
   stloc.0
   ldc.i4 0x80000066
   conv.i4
   not
   ldc.i4 0x7fffff99
   beq TestCase4898
   br Done

TestCase4898:
   ldc.i4 4998
   stloc.0
   ldc.i4 0x80000067
   conv.i4
   not
   ldc.i4 0x7fffff98
   beq TestCase4897
   br Done

TestCase4897:
   ldc.i4 4997
   stloc.0
   ldc.i4 0x80000068
   conv.i4
   not
   ldc.i4 0x7fffff97
   bne.un Done

TestCase4896:
   ldc.i4 4996
   stloc.0
   ldc.i4 0x80000069
   conv.i4
   not
   ldc.i4 0x7fffff96
   bne.un Done

TestCase4895:
   ldc.i4 4995
   stloc.0
   ldc.i4 0x8000006a
   conv.i4
   not
   ldc.i4 0x7fffff95
   bne.un Done

TestCase4894:
   ldc.i4 4994
   stloc.0
   ldc.i4 0x8000006b
   conv.i4
   not
   ldc.i4 0x7fffff94
   beq TestCase4893
   br Done

TestCase4893:
   ldc.i4 4993
   stloc.0
   ldc.i4 0x8000006c
   conv.i4
   not
   ldc.i4 0x7fffff93
   bne.un Done

TestCase4892:
   ldc.i4 4992
   stloc.0
   ldc.i4 0x8000006d
   conv.i4
   not
   ldc.i4 0x7fffff92
   beq TestCase4891
   br Done

TestCase4891:
   ldc.i4 4991
   stloc.0
   ldc.i4 0x8000006e
   conv.i4
   not
   ldc.i4 0x7fffff91
   bne.un Done

TestCase4890:
   ldc.i4 4990
   stloc.0
   ldc.i4 0x8000006f
   conv.i4
   not
   ldc.i4 0x7fffff90
   bne.un Done

TestCase4889:
   ldc.i4 4989
   stloc.0
   ldc.i4 0x80000070
   conv.i4
   not
   ldc.i4 0x7fffff8f
   bne.un Done

TestCase4888:
   ldc.i4 4988
   stloc.0
   ldc.i4 0x80000071
   conv.i4
   not
   ldc.i4 0x7fffff8e
   bne.un Done

TestCase4887:
   ldc.i4 4987
   stloc.0
   ldc.i4 0x80000072
   conv.i4
   not
   ldc.i4 0x7fffff8d
   beq TestCase4886
   br Done

TestCase4886:
   ldc.i4 4986
   stloc.0
   ldc.i4 0x80000073
   conv.i4
   not
   ldc.i4 0x7fffff8c
   bne.un Done

TestCase4885:
   ldc.i4 4985
   stloc.0
   ldc.i4 0x80000074
   conv.i4
   not
   ldc.i4 0x7fffff8b
   bne.un Done

TestCase4884:
   ldc.i4 4984
   stloc.0
   ldc.i4 0x80000075
   conv.i4
   not
   ldc.i4 0x7fffff8a
   bne.un Done

TestCase4883:
   ldc.i4 4983
   stloc.0
   ldc.i4 0x80000076
   conv.i4
   not
   ldc.i4 0x7fffff89
   beq TestCase4882
   br Done

TestCase4882:
   ldc.i4 4982
   stloc.0
   ldc.i4 0x80000077
   conv.i4
   not
   ldc.i4 0x7fffff88
   bne.un Done

TestCase4881:
   ldc.i4 4981
   stloc.0
   ldc.i4 0x80000078
   conv.i4
   not
   ldc.i4 0x7fffff87
   beq TestCase4880
   br Done

TestCase4880:
   ldc.i4 4980
   stloc.0
   ldc.i4 0x80000079
   conv.i4
   not
   ldc.i4 0x7fffff86
   beq TestCase4879
   br Done

TestCase4879:
   ldc.i4 4979
   stloc.0
   ldc.i4 0x8000007a
   conv.i4
   not
   ldc.i4 0x7fffff85
   bne.un Done

TestCase4878:
   ldc.i4 4978
   stloc.0
   ldc.i4 0x8000007b
   conv.i4
   not
   ldc.i4 0x7fffff84
   beq TestCase4877
   br Done

TestCase4877:
   ldc.i4 4977
   stloc.0
   ldc.i4 0x8000007c
   conv.i4
   not
   ldc.i4 0x7fffff83
   bne.un Done

TestCase4876:
   ldc.i4 4976
   stloc.0
   ldc.i4 0x8000007d
   conv.i4
   not
   ldc.i4 0x7fffff82
   beq TestCase4875
   br Done

TestCase4875:
   ldc.i4 4975
   stloc.0
   ldc.i4 0x8000007e
   conv.i4
   not
   ldc.i4 0x7fffff81
   bne.un Done

TestCase4874:
   ldc.i4 4974
   stloc.0
   ldc.i4 0x8000007f
   conv.i4
   not
   ldc.i4 0x7fffff80
   bne.un Done

TestCase4873:
   ldc.i4 4973
   stloc.0
   ldc.i4 0x80000080
   conv.i4
   not
   ldc.i4 0x7fffff7f
   beq TestCase4872
   br Done

TestCase4872:
   ldc.i4 4972
   stloc.0
   ldc.i4 0x80000081
   conv.i4
   not
   ldc.i4 0x7fffff7e
   beq TestCase4871
   br Done

TestCase4871:
   ldc.i4 4971
   stloc.0
   ldc.i4 0x80000082
   conv.i4
   not
   ldc.i4 0x7fffff7d
   bne.un Done

TestCase4870:
   ldc.i4 4970
   stloc.0
   ldc.i4 0x80000083
   conv.i4
   not
   ldc.i4 0x7fffff7c
   beq TestCase4869
   br Done

TestCase4869:
   ldc.i4 4969
   stloc.0
   ldc.i4 0x80000084
   conv.i4
   not
   ldc.i4 0x7fffff7b
   bne.un Done

TestCase4868:
   ldc.i4 4968
   stloc.0
   ldc.i4 0x80000085
   conv.i4
   not
   ldc.i4 0x7fffff7a
   bne.un Done

TestCase4867:
   ldc.i4 4967
   stloc.0
   ldc.i4 0x80000086
   conv.i4
   not
   ldc.i4 0x7fffff79
   bne.un Done

TestCase4866:
   ldc.i4 4966
   stloc.0
   ldc.i4 0x80000087
   conv.i4
   not
   ldc.i4 0x7fffff78
   bne.un Done

TestCase4865:
   ldc.i4 4965
   stloc.0
   ldc.i4 0x80000088
   conv.i4
   not
   ldc.i4 0x7fffff77
   bne.un Done

TestCase4864:
   ldc.i4 4964
   stloc.0
   ldc.i4 0x80000089
   conv.i4
   not
   ldc.i4 0x7fffff76
   beq TestCase4863
   br Done

TestCase4863:
   ldc.i4 4963
   stloc.0
   ldc.i4 0x8000008a
   conv.i4
   not
   ldc.i4 0x7fffff75
   bne.un Done

TestCase4862:
   ldc.i4 4962
   stloc.0
   ldc.i4 0x8000008b
   conv.i4
   not
   ldc.i4 0x7fffff74
   bne.un Done

TestCase4861:
   ldc.i4 4961
   stloc.0
   ldc.i4 0x8000008c
   conv.i4
   not
   ldc.i4 0x7fffff73
   bne.un Done

TestCase4860:
   ldc.i4 4960
   stloc.0
   ldc.i4 0x8000008d
   conv.i4
   not
   ldc.i4 0x7fffff72
   bne.un Done

TestCase4859:
   ldc.i4 4959
   stloc.0
   ldc.i4 0x8000008e
   conv.i4
   not
   ldc.i4 0x7fffff71
   beq TestCase4858
   br Done

TestCase4858:
   ldc.i4 4958
   stloc.0
   ldc.i4 0x8000008f
   conv.i4
   not
   ldc.i4 0x7fffff70
   bne.un Done

TestCase4857:
   ldc.i4 4957
   stloc.0
   ldc.i4 0x80000090
   conv.i4
   not
   ldc.i4 0x7fffff6f
   beq TestCase4856
   br Done

TestCase4856:
   ldc.i4 4956
   stloc.0
   ldc.i4 0x80000091
   conv.i4
   not
   ldc.i4 0x7fffff6e
   bne.un Done

TestCase4855:
   ldc.i4 4955
   stloc.0
   ldc.i4 0x80000092
   conv.i4
   not
   ldc.i4 0x7fffff6d
   beq TestCase4854
   br Done

TestCase4854:
   ldc.i4 4954
   stloc.0
   ldc.i4 0x80000093
   conv.i4
   not
   ldc.i4 0x7fffff6c
   bne.un Done

TestCase4853:
   ldc.i4 4953
   stloc.0
   ldc.i4 0x80000094
   conv.i4
   not
   ldc.i4 0x7fffff6b
   bne.un Done

TestCase4852:
   ldc.i4 4952
   stloc.0
   ldc.i4 0x80000095
   conv.i4
   not
   ldc.i4 0x7fffff6a
   bne.un Done

TestCase4851:
   ldc.i4 4951
   stloc.0
   ldc.i4 0x80000096
   conv.i4
   not
   ldc.i4 0x7fffff69
   beq TestCase4850
   br Done

TestCase4850:
   ldc.i4 4950
   stloc.0
   ldc.i4 0x80000097
   conv.i4
   not
   ldc.i4 0x7fffff68
   beq TestCase4849
   br Done

TestCase4849:
   ldc.i4 4949
   stloc.0
   ldc.i4 0x80000098
   conv.i4
   not
   ldc.i4 0x7fffff67
   beq TestCase4848
   br Done

TestCase4848:
   ldc.i4 4948
   stloc.0
   ldc.i4 0x80000099
   conv.i4
   not
   ldc.i4 0x7fffff66
   bne.un Done

TestCase4847:
   ldc.i4 4947
   stloc.0
   ldc.i4 0x8000009a
   conv.i4
   not
   ldc.i4 0x7fffff65
   bne.un Done

TestCase4846:
   ldc.i4 4946
   stloc.0
   ldc.i4 0x8000009b
   conv.i4
   not
   ldc.i4 0x7fffff64
   bne.un Done

TestCase4845:
   ldc.i4 4945
   stloc.0
   ldc.i4 0x8000009c
   conv.i4
   not
   ldc.i4 0x7fffff63
   beq TestCase4844
   br Done

TestCase4844:
   ldc.i4 4944
   stloc.0
   ldc.i4 0x8000009d
   conv.i4
   not
   ldc.i4 0x7fffff62
   beq TestCase4843
   br Done

TestCase4843:
   ldc.i4 4943
   stloc.0
   ldc.i4 0x8000009e
   conv.i4
   not
   ldc.i4 0x7fffff61
   bne.un Done

TestCase4842:
   ldc.i4 4942
   stloc.0
   ldc.i4 0x8000009f
   conv.i4
   not
   ldc.i4 0x7fffff60
   beq TestCase4841
   br Done

TestCase4841:
   ldc.i4 4941
   stloc.0
   ldc.i4 0x800000a0
   conv.i4
   not
   ldc.i4 0x7fffff5f
   beq TestCase4840
   br Done

TestCase4840:
   ldc.i4 4940
   stloc.0
   ldc.i4 0x800000a1
   conv.i4
   not
   ldc.i4 0x7fffff5e
   bne.un Done

TestCase4839:
   ldc.i4 4939
   stloc.0
   ldc.i4 0x800000a2
   conv.i4
   not
   ldc.i4 0x7fffff5d
   beq TestCase4838
   br Done

TestCase4838:
   ldc.i4 4938
   stloc.0
   ldc.i4 0x800000a3
   conv.i4
   not
   ldc.i4 0x7fffff5c
   beq TestCase4837
   br Done

TestCase4837:
   ldc.i4 4937
   stloc.0
   ldc.i4 0x800000a4
   conv.i4
   not
   ldc.i4 0x7fffff5b
   bne.un Done

TestCase4836:
   ldc.i4 4936
   stloc.0
   ldc.i4 0x800000a5
   conv.i4
   not
   ldc.i4 0x7fffff5a
   beq TestCase4835
   br Done

TestCase4835:
   ldc.i4 4935
   stloc.0
   ldc.i4 0x800000a6
   conv.i4
   not
   ldc.i4 0x7fffff59
   bne.un Done

TestCase4834:
   ldc.i4 4934
   stloc.0
   ldc.i4 0x800000a7
   conv.i4
   not
   ldc.i4 0x7fffff58
   beq TestCase4833
   br Done

TestCase4833:
   ldc.i4 4933
   stloc.0
   ldc.i4 0x800000a8
   conv.i4
   not
   ldc.i4 0x7fffff57
   beq TestCase4832
   br Done

TestCase4832:
   ldc.i4 4932
   stloc.0
   ldc.i4 0x800000a9
   conv.i4
   not
   ldc.i4 0x7fffff56
   bne.un Done

TestCase4831:
   ldc.i4 4931
   stloc.0
   ldc.i4 0x800000aa
   conv.i4
   not
   ldc.i4 0x7fffff55
   bne.un Done

TestCase4830:
   ldc.i4 4930
   stloc.0
   ldc.i4 0x800000ab
   conv.i4
   not
   ldc.i4 0x7fffff54
   bne.un Done

TestCase4829:
   ldc.i4 4929
   stloc.0
   ldc.i4 0x800000ac
   conv.i4
   not
   ldc.i4 0x7fffff53
   bne.un Done

TestCase4828:
   ldc.i4 4928
   stloc.0
   ldc.i4 0x800000ad
   conv.i4
   not
   ldc.i4 0x7fffff52
   beq TestCase4827
   br Done

TestCase4827:
   ldc.i4 4927
   stloc.0
   ldc.i4 0x800000ae
   conv.i4
   not
   ldc.i4 0x7fffff51
   beq TestCase4826
   br Done

TestCase4826:
   ldc.i4 4926
   stloc.0
   ldc.i4 0x800000af
   conv.i4
   not
   ldc.i4 0x7fffff50
   beq TestCase4825
   br Done

TestCase4825:
   ldc.i4 4925
   stloc.0
   ldc.i4 0x800000b0
   conv.i4
   not
   ldc.i4 0x7fffff4f
   bne.un Done

TestCase4824:
   ldc.i4 4924
   stloc.0
   ldc.i4 0x800000b1
   conv.i4
   not
   ldc.i4 0x7fffff4e
   beq TestCase4823
   br Done

TestCase4823:
   ldc.i4 4923
   stloc.0
   ldc.i4 0x800000b2
   conv.i4
   not
   ldc.i4 0x7fffff4d
   bne.un Done

TestCase4822:
   ldc.i4 4922
   stloc.0
   ldc.i4 0x800000b3
   conv.i4
   not
   ldc.i4 0x7fffff4c
   bne.un Done

TestCase4821:
   ldc.i4 4921
   stloc.0
   ldc.i4 0x800000b4
   conv.i4
   not
   ldc.i4 0x7fffff4b
   bne.un Done

TestCase4820:
   ldc.i4 4920
   stloc.0
   ldc.i4 0x800000b5
   conv.i4
   not
   ldc.i4 0x7fffff4a
   bne.un Done

TestCase4819:
   ldc.i4 4919
   stloc.0
   ldc.i4 0x800000b6
   conv.i4
   not
   ldc.i4 0x7fffff49
   beq TestCase4818
   br Done

TestCase4818:
   ldc.i4 4918
   stloc.0
   ldc.i4 0x800000b7
   conv.i4
   not
   ldc.i4 0x7fffff48
   bne.un Done

TestCase4817:
   ldc.i4 4917
   stloc.0
   ldc.i4 0x800000b8
   conv.i4
   not
   ldc.i4 0x7fffff47
   bne.un Done

TestCase4816:
   ldc.i4 4916
   stloc.0
   ldc.i4 0x800000b9
   conv.i4
   not
   ldc.i4 0x7fffff46
   bne.un Done

TestCase4815:
   ldc.i4 4915
   stloc.0
   ldc.i4 0x800000ba
   conv.i4
   not
   ldc.i4 0x7fffff45
   bne.un Done

TestCase4814:
   ldc.i4 4914
   stloc.0
   ldc.i4 0x800000bb
   conv.i4
   not
   ldc.i4 0x7fffff44
   beq TestCase4813
   br Done

TestCase4813:
   ldc.i4 4913
   stloc.0
   ldc.i4 0x800000bc
   conv.i4
   not
   ldc.i4 0x7fffff43
   beq TestCase4812
   br Done

TestCase4812:
   ldc.i4 4912
   stloc.0
   ldc.i4 0x800000bd
   conv.i4
   not
   ldc.i4 0x7fffff42
   bne.un Done

TestCase4811:
   ldc.i4 4911
   stloc.0
   ldc.i4 0x800000be
   conv.i4
   not
   ldc.i4 0x7fffff41
   beq TestCase4810
   br Done

TestCase4810:
   ldc.i4 4910
   stloc.0
   ldc.i4 0x800000bf
   conv.i4
   not
   ldc.i4 0x7fffff40
   beq TestCase4809
   br Done

TestCase4809:
   ldc.i4 4909
   stloc.0
   ldc.i4 0x800000c0
   conv.i4
   not
   ldc.i4 0x7fffff3f
   bne.un Done

TestCase4808:
   ldc.i4 4908
   stloc.0
   ldc.i4 0x800000c1
   conv.i4
   not
   ldc.i4 0x7fffff3e
   bne.un Done

TestCase4807:
   ldc.i4 4907
   stloc.0
   ldc.i4 0x800000c2
   conv.i4
   not
   ldc.i4 0x7fffff3d
   bne.un Done

TestCase4806:
   ldc.i4 4906
   stloc.0
   ldc.i4 0x800000c3
   conv.i4
   not
   ldc.i4 0x7fffff3c
   beq TestCase4805
   br Done

TestCase4805:
   ldc.i4 4905
   stloc.0
   ldc.i4 0x800000c4
   conv.i4
   not
   ldc.i4 0x7fffff3b
   beq TestCase4804
   br Done

TestCase4804:
   ldc.i4 4904
   stloc.0
   ldc.i4 0x800000c5
   conv.i4
   not
   ldc.i4 0x7fffff3a
   beq TestCase4803
   br Done

TestCase4803:
   ldc.i4 4903
   stloc.0
   ldc.i4 0x800000c6
   conv.i4
   not
   ldc.i4 0x7fffff39
   bne.un Done

TestCase4802:
   ldc.i4 4902
   stloc.0
   ldc.i4 0x800000c7
   conv.i4
   not
   ldc.i4 0x7fffff38
   beq TestCase4801
   br Done

TestCase4801:
   ldc.i4 4901
   stloc.0
   ldc.i4 0x800000c8
   conv.i4
   not
   ldc.i4 0x7fffff37
   bne.un Done

TestCase4800:
   ldc.i4 4900
   stloc.0
   ldc.i4 0x800000c9
   conv.i4
   not
   ldc.i4 0x7fffff36
   beq TestCase4799
   br Done

TestCase4799:
   ldc.i4 4899
   stloc.0
   ldc.i4 0x800000ca
   conv.i4
   not
   ldc.i4 0x7fffff35
   bne.un Done

TestCase4798:
   ldc.i4 4898
   stloc.0
   ldc.i4 0x800000cb
   conv.i4
   not
   ldc.i4 0x7fffff34
   beq TestCase4797
   br Done

TestCase4797:
   ldc.i4 4897
   stloc.0
   ldc.i4 0x800000cc
   conv.i4
   not
   ldc.i4 0x7fffff33
   bne.un Done

TestCase4796:
   ldc.i4 4896
   stloc.0
   ldc.i4 0x800000cd
   conv.i4
   not
   ldc.i4 0x7fffff32
   bne.un Done

TestCase4795:
   ldc.i4 4895
   stloc.0
   ldc.i4 0x800000ce
   conv.i4
   not
   ldc.i4 0x7fffff31
   bne.un Done

TestCase4794:
   ldc.i4 4894
   stloc.0
   ldc.i4 0x800000cf
   conv.i4
   not
   ldc.i4 0x7fffff30
   beq TestCase4793
   br Done

TestCase4793:
   ldc.i4 4893
   stloc.0
   ldc.i4 0x800000d0
   conv.i4
   not
   ldc.i4 0x7fffff2f
   beq TestCase4792
   br Done

TestCase4792:
   ldc.i4 4892
   stloc.0
   ldc.i4 0x800000d1
   conv.i4
   not
   ldc.i4 0x7fffff2e
   beq TestCase4791
   br Done

TestCase4791:
   ldc.i4 4891
   stloc.0
   ldc.i4 0x800000d2
   conv.i4
   not
   ldc.i4 0x7fffff2d
   bne.un Done

TestCase4790:
   ldc.i4 4890
   stloc.0
   ldc.i4 0x800000d3
   conv.i4
   not
   ldc.i4 0x7fffff2c
   bne.un Done

TestCase4789:
   ldc.i4 4889
   stloc.0
   ldc.i4 0x800000d4
   conv.i4
   not
   ldc.i4 0x7fffff2b
   beq TestCase4788
   br Done

TestCase4788:
   ldc.i4 4888
   stloc.0
   ldc.i4 0x800000d5
   conv.i4
   not
   ldc.i4 0x7fffff2a
   bne.un Done

TestCase4787:
   ldc.i4 4887
   stloc.0
   ldc.i4 0x800000d6
   conv.i4
   not
   ldc.i4 0x7fffff29
   bne.un Done

TestCase4786:
   ldc.i4 4886
   stloc.0
   ldc.i4 0x800000d7
   conv.i4
   not
   ldc.i4 0x7fffff28
   beq TestCase4785
   br Done

TestCase4785:
   ldc.i4 4885
   stloc.0
   ldc.i4 0x800000d8
   conv.i4
   not
   ldc.i4 0x7fffff27
   bne.un Done

TestCase4784:
   ldc.i4 4884
   stloc.0
   ldc.i4 0x800000d9
   conv.i4
   not
   ldc.i4 0x7fffff26
   beq TestCase4783
   br Done

TestCase4783:
   ldc.i4 4883
   stloc.0
   ldc.i4 0x800000da
   conv.i4
   not
   ldc.i4 0x7fffff25
   beq TestCase4782
   br Done

TestCase4782:
   ldc.i4 4882
   stloc.0
   ldc.i4 0x800000db
   conv.i4
   not
   ldc.i4 0x7fffff24
   bne.un Done

TestCase4781:
   ldc.i4 4881
   stloc.0
   ldc.i4 0x800000dc
   conv.i4
   not
   ldc.i4 0x7fffff23
   beq TestCase4780
   br Done

TestCase4780:
   ldc.i4 4880
   stloc.0
   ldc.i4 0x800000dd
   conv.i4
   not
   ldc.i4 0x7fffff22
   beq TestCase4779
   br Done

TestCase4779:
   ldc.i4 4879
   stloc.0
   ldc.i4 0x800000de
   conv.i4
   not
   ldc.i4 0x7fffff21
   beq TestCase4778
   br Done

TestCase4778:
   ldc.i4 4878
   stloc.0
   ldc.i4 0x800000df
   conv.i4
   not
   ldc.i4 0x7fffff20
   beq TestCase4777
   br Done

TestCase4777:
   ldc.i4 4877
   stloc.0
   ldc.i4 0x800000e0
   conv.i4
   not
   ldc.i4 0x7fffff1f
   beq TestCase4776
   br Done

TestCase4776:
   ldc.i4 4876
   stloc.0
   ldc.i4 0x800000e1
   conv.i4
   not
   ldc.i4 0x7fffff1e
   beq TestCase4775
   br Done

TestCase4775:
   ldc.i4 4875
   stloc.0
   ldc.i4 0x800000e2
   conv.i4
   not
   ldc.i4 0x7fffff1d
   bne.un Done

TestCase4774:
   ldc.i4 4874
   stloc.0
   ldc.i4 0x800000e3
   conv.i4
   not
   ldc.i4 0x7fffff1c
   bne.un Done

TestCase4773:
   ldc.i4 4873
   stloc.0
   ldc.i4 0x800000e4
   conv.i4
   not
   ldc.i4 0x7fffff1b
   beq TestCase4772
   br Done

TestCase4772:
   ldc.i4 4872
   stloc.0
   ldc.i4 0x800000e5
   conv.i4
   not
   ldc.i4 0x7fffff1a
   bne.un Done

TestCase4771:
   ldc.i4 4871
   stloc.0
   ldc.i4 0x800000e6
   conv.i4
   not
   ldc.i4 0x7fffff19
   bne.un Done

TestCase4770:
   ldc.i4 4870
   stloc.0
   ldc.i4 0x800000e7
   conv.i4
   not
   ldc.i4 0x7fffff18
   bne.un Done

TestCase4769:
   ldc.i4 4869
   stloc.0
   ldc.i4 0x800000e8
   conv.i4
   not
   ldc.i4 0x7fffff17
   beq TestCase4768
   br Done

TestCase4768:
   ldc.i4 4868
   stloc.0
   ldc.i4 0x800000e9
   conv.i4
   not
   ldc.i4 0x7fffff16
   bne.un Done

TestCase4767:
   ldc.i4 4867
   stloc.0
   ldc.i4 0x800000ea
   conv.i4
   not
   ldc.i4 0x7fffff15
   beq TestCase4766
   br Done

TestCase4766:
   ldc.i4 4866
   stloc.0
   ldc.i4 0x800000eb
   conv.i4
   not
   ldc.i4 0x7fffff14
   beq TestCase4765
   br Done

TestCase4765:
   ldc.i4 4865
   stloc.0
   ldc.i4 0x800000ec
   conv.i4
   not
   ldc.i4 0x7fffff13
   beq TestCase4764
   br Done

TestCase4764:
   ldc.i4 4864
   stloc.0
   ldc.i4 0x800000ed
   conv.i4
   not
   ldc.i4 0x7fffff12
   beq TestCase4763
   br Done

TestCase4763:
   ldc.i4 4863
   stloc.0
   ldc.i4 0x800000ee
   conv.i4
   not
   ldc.i4 0x7fffff11
   bne.un Done

TestCase4762:
   ldc.i4 4862
   stloc.0
   ldc.i4 0x800000ef
   conv.i4
   not
   ldc.i4 0x7fffff10
   beq TestCase4761
   br Done

TestCase4761:
   ldc.i4 4861
   stloc.0
   ldc.i4 0x800000f0
   conv.i4
   not
   ldc.i4 0x7fffff0f
   beq TestCase4760
   br Done

TestCase4760:
   ldc.i4 4860
   stloc.0
   ldc.i4 0x800000f1
   conv.i4
   not
   ldc.i4 0x7fffff0e
   beq TestCase4759
   br Done

TestCase4759:
   ldc.i4 4859
   stloc.0
   ldc.i4 0x800000f2
   conv.i4
   not
   ldc.i4 0x7fffff0d
   beq TestCase4758
   br Done

TestCase4758:
   ldc.i4 4858
   stloc.0
   ldc.i4 0x800000f3
   conv.i4
   not
   ldc.i4 0x7fffff0c
   bne.un Done

TestCase4757:
   ldc.i4 4857
   stloc.0
   ldc.i4 0x800000f4
   conv.i4
   not
   ldc.i4 0x7fffff0b
   beq TestCase4756
   br Done

TestCase4756:
   ldc.i4 4856
   stloc.0
   ldc.i4 0x800000f5
   conv.i4
   not
   ldc.i4 0x7fffff0a
   bne.un Done

TestCase4755:
   ldc.i4 4855
   stloc.0
   ldc.i4 0x800000f6
   conv.i4
   not
   ldc.i4 0x7fffff09
   beq TestCase4754
   br Done

TestCase4754:
   ldc.i4 4854
   stloc.0
   ldc.i4 0x800000f7
   conv.i4
   not
   ldc.i4 0x7fffff08
   beq TestCase4753
   br Done

TestCase4753:
   ldc.i4 4853
   stloc.0
   ldc.i4 0x800000f8
   conv.i4
   not
   ldc.i4 0x7fffff07
   bne.un Done

TestCase4752:
   ldc.i4 4852
   stloc.0
   ldc.i4 0x800000f9
   conv.i4
   not
   ldc.i4 0x7fffff06
   bne.un Done

TestCase4751:
   ldc.i4 4851
   stloc.0
   ldc.i4 0x800000fa
   conv.i4
   not
   ldc.i4 0x7fffff05
   bne.un Done

TestCase4750:
   ldc.i4 4850
   stloc.0
   ldc.i4 0x800000fb
   conv.i4
   not
   ldc.i4 0x7fffff04
   beq TestCase4749
   br Done

TestCase4749:
   ldc.i4 4849
   stloc.0
   ldc.i4 0x800000fc
   conv.i4
   not
   ldc.i4 0x7fffff03
   bne.un Done

TestCase4748:
   ldc.i4 4848
   stloc.0
   ldc.i4 0x800000fd
   conv.i4
   not
   ldc.i4 0x7fffff02
   beq TestCase4747
   br Done

TestCase4747:
   ldc.i4 4847
   stloc.0
   ldc.i4 0x800000fe
   conv.i4
   not
   ldc.i4 0x7fffff01
   bne.un Done

TestCase4746:
   ldc.i4 4846
   stloc.0
   ldc.i4 0x800000ff
   conv.i4
   not
   ldc.i4 0x7fffff00
   bne.un Done

TestCase4745:
   ldc.i4 4845
   stloc.0
   ldc.i4 0x80000100
   conv.i4
   not
   ldc.i4 0x7ffffeff
   beq TestCase4744
   br Done

TestCase4744:
   ldc.i4 4844
   stloc.0
   ldc.i4 0x80000101
   conv.i4
   not
   ldc.i4 0x7ffffefe
   beq TestCase4743
   br Done

TestCase4743:
   ldc.i4 4843
   stloc.0
   ldc.i4 0x80000102
   conv.i4
   not
   ldc.i4 0x7ffffefd
   beq TestCase4742
   br Done

TestCase4742:
   ldc.i4 4842
   stloc.0
   ldc.i4 0x80000103
   conv.i4
   not
   ldc.i4 0x7ffffefc
   bne.un Done

TestCase4741:
   ldc.i4 4841
   stloc.0
   ldc.i4 0x80000104
   conv.i4
   not
   ldc.i4 0x7ffffefb
   bne.un Done

TestCase4740:
   ldc.i4 4840
   stloc.0
   ldc.i4 0x80000105
   conv.i4
   not
   ldc.i4 0x7ffffefa
   bne.un Done

TestCase4739:
   ldc.i4 4839
   stloc.0
   ldc.i4 0x80000106
   conv.i4
   not
   ldc.i4 0x7ffffef9
   beq TestCase4738
   br Done

TestCase4738:
   ldc.i4 4838
   stloc.0
   ldc.i4 0x80000107
   conv.i4
   not
   ldc.i4 0x7ffffef8
   bne.un Done

TestCase4737:
   ldc.i4 4837
   stloc.0
   ldc.i4 0x80000108
   conv.i4
   not
   ldc.i4 0x7ffffef7
   beq TestCase4736
   br Done

TestCase4736:
   ldc.i4 4836
   stloc.0
   ldc.i4 0x80000109
   conv.i4
   not
   ldc.i4 0x7ffffef6
   bne.un Done

TestCase4735:
   ldc.i4 4835
   stloc.0
   ldc.i4 0x8000010a
   conv.i4
   not
   ldc.i4 0x7ffffef5
   bne.un Done

TestCase4734:
   ldc.i4 4834
   stloc.0
   ldc.i4 0x8000010b
   conv.i4
   not
   ldc.i4 0x7ffffef4
   beq TestCase4733
   br Done

TestCase4733:
   ldc.i4 4833
   stloc.0
   ldc.i4 0x8000010c
   conv.i4
   not
   ldc.i4 0x7ffffef3
   beq TestCase4732
   br Done

TestCase4732:
   ldc.i4 4832
   stloc.0
   ldc.i4 0x8000010d
   conv.i4
   not
   ldc.i4 0x7ffffef2
   bne.un Done

TestCase4731:
   ldc.i4 4831
   stloc.0
   ldc.i4 0x8000010e
   conv.i4
   not
   ldc.i4 0x7ffffef1
   bne.un Done

TestCase4730:
   ldc.i4 4830
   stloc.0
   ldc.i4 0x8000010f
   conv.i4
   not
   ldc.i4 0x7ffffef0
   bne.un Done

TestCase4729:
   ldc.i4 4829
   stloc.0
   ldc.i4 0x80000110
   conv.i4
   not
   ldc.i4 0x7ffffeef
   bne.un Done

TestCase4728:
   ldc.i4 4828
   stloc.0
   ldc.i4 0x80000111
   conv.i4
   not
   ldc.i4 0x7ffffeee
   beq TestCase4727
   br Done

TestCase4727:
   ldc.i4 4827
   stloc.0
   ldc.i4 0x80000112
   conv.i4
   not
   ldc.i4 0x7ffffeed
   beq TestCase4726
   br Done

TestCase4726:
   ldc.i4 4826
   stloc.0
   ldc.i4 0x80000113
   conv.i4
   not
   ldc.i4 0x7ffffeec
   bne.un Done

TestCase4725:
   ldc.i4 4825
   stloc.0
   ldc.i4 0x80000114
   conv.i4
   not
   ldc.i4 0x7ffffeeb
   bne.un Done

TestCase4724:
   ldc.i4 4824
   stloc.0
   ldc.i4 0x80000115
   conv.i4
   not
   ldc.i4 0x7ffffeea
   beq TestCase4723
   br Done

TestCase4723:
   ldc.i4 4823
   stloc.0
   ldc.i4 0x80000116
   conv.i4
   not
   ldc.i4 0x7ffffee9
   bne.un Done

TestCase4722:
   ldc.i4 4822
   stloc.0
   ldc.i4 0x80000117
   conv.i4
   not
   ldc.i4 0x7ffffee8
   beq TestCase4721
   br Done

TestCase4721:
   ldc.i4 4821
   stloc.0
   ldc.i4 0x80000118
   conv.i4
   not
   ldc.i4 0x7ffffee7
   beq TestCase4720
   br Done

TestCase4720:
   ldc.i4 4820
   stloc.0
   ldc.i4 0x80000119
   conv.i4
   not
   ldc.i4 0x7ffffee6
   beq TestCase4719
   br Done

TestCase4719:
   ldc.i4 4819
   stloc.0
   ldc.i4 0x8000011a
   conv.i4
   not
   ldc.i4 0x7ffffee5
   bne.un Done

TestCase4718:
   ldc.i4 4818
   stloc.0
   ldc.i4 0x8000011b
   conv.i4
   not
   ldc.i4 0x7ffffee4
   bne.un Done

TestCase4717:
   ldc.i4 4817
   stloc.0
   ldc.i4 0x8000011c
   conv.i4
   not
   ldc.i4 0x7ffffee3
   beq TestCase4716
   br Done

TestCase4716:
   ldc.i4 4816
   stloc.0
   ldc.i4 0x8000011d
   conv.i4
   not
   ldc.i4 0x7ffffee2
   beq TestCase4715
   br Done

TestCase4715:
   ldc.i4 4815
   stloc.0
   ldc.i4 0x8000011e
   conv.i4
   not
   ldc.i4 0x7ffffee1
   bne.un Done

TestCase4714:
   ldc.i4 4814
   stloc.0
   ldc.i4 0x8000011f
   conv.i4
   not
   ldc.i4 0x7ffffee0
   bne.un Done

TestCase4713:
   ldc.i4 4813
   stloc.0
   ldc.i4 0x80000120
   conv.i4
   not
   ldc.i4 0x7ffffedf
   bne.un Done

TestCase4712:
   ldc.i4 4812
   stloc.0
   ldc.i4 0x80000121
   conv.i4
   not
   ldc.i4 0x7ffffede
   beq TestCase4711
   br Done

TestCase4711:
   ldc.i4 4811
   stloc.0
   ldc.i4 0x80000122
   conv.i4
   not
   ldc.i4 0x7ffffedd
   bne.un Done

TestCase4710:
   ldc.i4 4810
   stloc.0
   ldc.i4 0x80000123
   conv.i4
   not
   ldc.i4 0x7ffffedc
   beq TestCase4709
   br Done

TestCase4709:
   ldc.i4 4809
   stloc.0
   ldc.i4 0x80000124
   conv.i4
   not
   ldc.i4 0x7ffffedb
   bne.un Done

TestCase4708:
   ldc.i4 4808
   stloc.0
   ldc.i4 0x80000125
   conv.i4
   not
   ldc.i4 0x7ffffeda
   beq TestCase4707
   br Done

TestCase4707:
   ldc.i4 4807
   stloc.0
   ldc.i4 0x80000126
   conv.i4
   not
   ldc.i4 0x7ffffed9
   beq TestCase4706
   br Done

TestCase4706:
   ldc.i4 4806
   stloc.0
   ldc.i4 0x80000127
   conv.i4
   not
   ldc.i4 0x7ffffed8
   bne.un Done

TestCase4705:
   ldc.i4 4805
   stloc.0
   ldc.i4 0x80000128
   conv.i4
   not
   ldc.i4 0x7ffffed7
   bne.un Done

TestCase4704:
   ldc.i4 4804
   stloc.0
   ldc.i4 0x80000129
   conv.i4
   not
   ldc.i4 0x7ffffed6
   bne.un Done

TestCase4703:
   ldc.i4 4803
   stloc.0
   ldc.i4 0x8000012a
   conv.i4
   not
   ldc.i4 0x7ffffed5
   bne.un Done

TestCase4702:
   ldc.i4 4802
   stloc.0
   ldc.i4 0x8000012b
   conv.i4
   not
   ldc.i4 0x7ffffed4
   beq TestCase4701
   br Done

TestCase4701:
   ldc.i4 4801
   stloc.0
   ldc.i4 0x8000012c
   conv.i4
   not
   ldc.i4 0x7ffffed3
   beq TestCase4700
   br Done

TestCase4700:
   ldc.i4 4800
   stloc.0
   ldc.i4 0x8000012d
   conv.i4
   not
   ldc.i4 0x7ffffed2
   bne.un Done

TestCase4699:
   ldc.i4 4799
   stloc.0
   ldc.i4 0x8000012e
   conv.i4
   not
   ldc.i4 0x7ffffed1
   bne.un Done

TestCase4698:
   ldc.i4 4798
   stloc.0
   ldc.i4 0x8000012f
   conv.i4
   not
   ldc.i4 0x7ffffed0
   bne.un Done

TestCase4697:
   ldc.i4 4797
   stloc.0
   ldc.i4 0x80000130
   conv.i4
   not
   ldc.i4 0x7ffffecf
   beq TestCase4696
   br Done

TestCase4696:
   ldc.i4 4796
   stloc.0
   ldc.i4 0x80000131
   conv.i4
   not
   ldc.i4 0x7ffffece
   beq TestCase4695
   br Done

TestCase4695:
   ldc.i4 4795
   stloc.0
   ldc.i4 0x80000132
   conv.i4
   not
   ldc.i4 0x7ffffecd
   beq TestCase4694
   br Done

TestCase4694:
   ldc.i4 4794
   stloc.0
   ldc.i4 0x80000133
   conv.i4
   not
   ldc.i4 0x7ffffecc
   beq TestCase4693
   br Done

TestCase4693:
   ldc.i4 4793
   stloc.0
   ldc.i4 0x80000134
   conv.i4
   not
   ldc.i4 0x7ffffecb
   bne.un Done

TestCase4692:
   ldc.i4 4792
   stloc.0
   ldc.i4 0x80000135
   conv.i4
   not
   ldc.i4 0x7ffffeca
   bne.un Done

TestCase4691:
   ldc.i4 4791
   stloc.0
   ldc.i4 0x80000136
   conv.i4
   not
   ldc.i4 0x7ffffec9
   beq TestCase4690
   br Done

TestCase4690:
   ldc.i4 4790
   stloc.0
   ldc.i4 0x80000137
   conv.i4
   not
   ldc.i4 0x7ffffec8
   beq TestCase4689
   br Done

TestCase4689:
   ldc.i4 4789
   stloc.0
   ldc.i4 0x80000138
   conv.i4
   not
   ldc.i4 0x7ffffec7
   bne.un Done

TestCase4688:
   ldc.i4 4788
   stloc.0
   ldc.i4 0x80000139
   conv.i4
   not
   ldc.i4 0x7ffffec6
   bne.un Done

TestCase4687:
   ldc.i4 4787
   stloc.0
   ldc.i4 0x8000013a
   conv.i4
   not
   ldc.i4 0x7ffffec5
   beq TestCase4686
   br Done

TestCase4686:
   ldc.i4 4786
   stloc.0
   ldc.i4 0x8000013b
   conv.i4
   not
   ldc.i4 0x7ffffec4
   bne.un Done

TestCase4685:
   ldc.i4 4785
   stloc.0
   ldc.i4 0x8000013c
   conv.i4
   not
   ldc.i4 0x7ffffec3
   bne.un Done

TestCase4684:
   ldc.i4 4784
   stloc.0
   ldc.i4 0x8000013d
   conv.i4
   not
   ldc.i4 0x7ffffec2
   beq TestCase4683
   br Done

TestCase4683:
   ldc.i4 4783
   stloc.0
   ldc.i4 0x8000013e
   conv.i4
   not
   ldc.i4 0x7ffffec1
   beq TestCase4682
   br Done

TestCase4682:
   ldc.i4 4782
   stloc.0
   ldc.i4 0x8000013f
   conv.i4
   not
   ldc.i4 0x7ffffec0
   beq TestCase4681
   br Done

TestCase4681:
   ldc.i4 4781
   stloc.0
   ldc.i4 0x80000140
   conv.i4
   not
   ldc.i4 0x7ffffebf
   beq TestCase4680
   br Done

TestCase4680:
   ldc.i4 4780
   stloc.0
   ldc.i4 0x80000141
   conv.i4
   not
   ldc.i4 0x7ffffebe
   beq TestCase4679
   br Done

TestCase4679:
   ldc.i4 4779
   stloc.0
   ldc.i4 0x80000142
   conv.i4
   not
   ldc.i4 0x7ffffebd
   beq TestCase4678
   br Done

TestCase4678:
   ldc.i4 4778
   stloc.0
   ldc.i4 0x80000143
   conv.i4
   not
   ldc.i4 0x7ffffebc
   bne.un Done

TestCase4677:
   ldc.i4 4777
   stloc.0
   ldc.i4 0x80000144
   conv.i4
   not
   ldc.i4 0x7ffffebb
   beq TestCase4676
   br Done

TestCase4676:
   ldc.i4 4776
   stloc.0
   ldc.i4 0x80000145
   conv.i4
   not
   ldc.i4 0x7ffffeba
   bne.un Done

TestCase4675:
   ldc.i4 4775
   stloc.0
   ldc.i4 0x80000146
   conv.i4
   not
   ldc.i4 0x7ffffeb9
   beq TestCase4674
   br Done

TestCase4674:
   ldc.i4 4774
   stloc.0
   ldc.i4 0x80000147
   conv.i4
   not
   ldc.i4 0x7ffffeb8
   bne.un Done

TestCase4673:
   ldc.i4 4773
   stloc.0
   ldc.i4 0x80000148
   conv.i4
   not
   ldc.i4 0x7ffffeb7
   beq TestCase4672
   br Done

TestCase4672:
   ldc.i4 4772
   stloc.0
   ldc.i4 0x80000149
   conv.i4
   not
   ldc.i4 0x7ffffeb6
   beq TestCase4671
   br Done

TestCase4671:
   ldc.i4 4771
   stloc.0
   ldc.i4 0x8000014a
   conv.i4
   not
   ldc.i4 0x7ffffeb5
   bne.un Done

TestCase4670:
   ldc.i4 4770
   stloc.0
   ldc.i4 0x8000014b
   conv.i4
   not
   ldc.i4 0x7ffffeb4
   beq TestCase4669
   br Done

TestCase4669:
   ldc.i4 4769
   stloc.0
   ldc.i4 0x8000014c
   conv.i4
   not
   ldc.i4 0x7ffffeb3
   bne.un Done

TestCase4668:
   ldc.i4 4768
   stloc.0
   ldc.i4 0x8000014d
   conv.i4
   not
   ldc.i4 0x7ffffeb2
   beq TestCase4667
   br Done

TestCase4667:
   ldc.i4 4767
   stloc.0
   ldc.i4 0x8000014e
   conv.i4
   not
   ldc.i4 0x7ffffeb1
   beq TestCase4666
   br Done

TestCase4666:
   ldc.i4 4766
   stloc.0
   ldc.i4 0x8000014f
   conv.i4
   not
   ldc.i4 0x7ffffeb0
   bne.un Done

TestCase4665:
   ldc.i4 4765
   stloc.0
   ldc.i4 0x80000150
   conv.i4
   not
   ldc.i4 0x7ffffeaf
   bne.un Done

TestCase4664:
   ldc.i4 4764
   stloc.0
   ldc.i4 0x80000151
   conv.i4
   not
   ldc.i4 0x7ffffeae
   bne.un Done

TestCase4663:
   ldc.i4 4763
   stloc.0
   ldc.i4 0x80000152
   conv.i4
   not
   ldc.i4 0x7ffffead
   beq TestCase4662
   br Done

TestCase4662:
   ldc.i4 4762
   stloc.0
   ldc.i4 0x80000153
   conv.i4
   not
   ldc.i4 0x7ffffeac
   bne.un Done

TestCase4661:
   ldc.i4 4761
   stloc.0
   ldc.i4 0x80000154
   conv.i4
   not
   ldc.i4 0x7ffffeab
   beq TestCase4660
   br Done

TestCase4660:
   ldc.i4 4760
   stloc.0
   ldc.i4 0x80000155
   conv.i4
   not
   ldc.i4 0x7ffffeaa
   bne.un Done

TestCase4659:
   ldc.i4 4759
   stloc.0
   ldc.i4 0x80000156
   conv.i4
   not
   ldc.i4 0x7ffffea9
   bne.un Done

TestCase4658:
   ldc.i4 4758
   stloc.0
   ldc.i4 0x80000157
   conv.i4
   not
   ldc.i4 0x7ffffea8
   beq TestCase4657
   br Done

TestCase4657:
   ldc.i4 4757
   stloc.0
   ldc.i4 0x80000158
   conv.i4
   not
   ldc.i4 0x7ffffea7
   bne.un Done

TestCase4656:
   ldc.i4 4756
   stloc.0
   ldc.i4 0x80000159
   conv.i4
   not
   ldc.i4 0x7ffffea6
   beq TestCase4655
   br Done

TestCase4655:
   ldc.i4 4755
   stloc.0
   ldc.i4 0x8000015a
   conv.i4
   not
   ldc.i4 0x7ffffea5
   bne.un Done

TestCase4654:
   ldc.i4 4754
   stloc.0
   ldc.i4 0x8000015b
   conv.i4
   not
   ldc.i4 0x7ffffea4
   beq TestCase4653
   br Done

TestCase4653:
   ldc.i4 4753
   stloc.0
   ldc.i4 0x8000015c
   conv.i4
   not
   ldc.i4 0x7ffffea3
   bne.un Done

TestCase4652:
   ldc.i4 4752
   stloc.0
   ldc.i4 0x8000015d
   conv.i4
   not
   ldc.i4 0x7ffffea2
   beq TestCase4651
   br Done

TestCase4651:
   ldc.i4 4751
   stloc.0
   ldc.i4 0x8000015e
   conv.i4
   not
   ldc.i4 0x7ffffea1
   bne.un Done

TestCase4650:
   ldc.i4 4750
   stloc.0
   ldc.i4 0x8000015f
   conv.i4
   not
   ldc.i4 0x7ffffea0
   beq TestCase4649
   br Done

TestCase4649:
   ldc.i4 4749
   stloc.0
   ldc.i4 0x80000160
   conv.i4
   not
   ldc.i4 0x7ffffe9f
   bne.un Done

TestCase4648:
   ldc.i4 4748
   stloc.0
   ldc.i4 0x80000161
   conv.i4
   not
   ldc.i4 0x7ffffe9e
   bne.un Done

TestCase4647:
   ldc.i4 4747
   stloc.0
   ldc.i4 0x80000162
   conv.i4
   not
   ldc.i4 0x7ffffe9d
   beq TestCase4646
   br Done

TestCase4646:
   ldc.i4 4746
   stloc.0
   ldc.i4 0x80000163
   conv.i4
   not
   ldc.i4 0x7ffffe9c
   beq TestCase4645
   br Done

TestCase4645:
   ldc.i4 4745
   stloc.0
   ldc.i4 0x80000164
   conv.i4
   not
   ldc.i4 0x7ffffe9b
   bne.un Done

TestCase4644:
   ldc.i4 4744
   stloc.0
   ldc.i4 0x80000165
   conv.i4
   not
   ldc.i4 0x7ffffe9a
   bne.un Done

TestCase4643:
   ldc.i4 4743
   stloc.0
   ldc.i4 0x80000166
   conv.i4
   not
   ldc.i4 0x7ffffe99
   beq TestCase4642
   br Done

TestCase4642:
   ldc.i4 4742
   stloc.0
   ldc.i4 0x80000167
   conv.i4
   not
   ldc.i4 0x7ffffe98
   bne.un Done

TestCase4641:
   ldc.i4 4741
   stloc.0
   ldc.i4 0x80000168
   conv.i4
   not
   ldc.i4 0x7ffffe97
   beq TestCase4640
   br Done

TestCase4640:
   ldc.i4 4740
   stloc.0
   ldc.i4 0x80000169
   conv.i4
   not
   ldc.i4 0x7ffffe96
   beq TestCase4639
   br Done

TestCase4639:
   ldc.i4 4739
   stloc.0
   ldc.i4 0x8000016a
   conv.i4
   not
   ldc.i4 0x7ffffe95
   beq TestCase4638
   br Done

TestCase4638:
   ldc.i4 4738
   stloc.0
   ldc.i4 0x8000016b
   conv.i4
   not
   ldc.i4 0x7ffffe94
   beq TestCase4637
   br Done

TestCase4637:
   ldc.i4 4737
   stloc.0
   ldc.i4 0x8000016c
   conv.i4
   not
   ldc.i4 0x7ffffe93
   bne.un Done

TestCase4636:
   ldc.i4 4736
   stloc.0
   ldc.i4 0x8000016d
   conv.i4
   not
   ldc.i4 0x7ffffe92
   bne.un Done

TestCase4635:
   ldc.i4 4735
   stloc.0
   ldc.i4 0x8000016e
   conv.i4
   not
   ldc.i4 0x7ffffe91
   bne.un Done

TestCase4634:
   ldc.i4 4734
   stloc.0
   ldc.i4 0x8000016f
   conv.i4
   not
   ldc.i4 0x7ffffe90
   beq TestCase4633
   br Done

TestCase4633:
   ldc.i4 4733
   stloc.0
   ldc.i4 0x80000170
   conv.i4
   not
   ldc.i4 0x7ffffe8f
   beq TestCase4632
   br Done

TestCase4632:
   ldc.i4 4732
   stloc.0
   ldc.i4 0x80000171
   conv.i4
   not
   ldc.i4 0x7ffffe8e
   bne.un Done

TestCase4631:
   ldc.i4 4731
   stloc.0
   ldc.i4 0x80000172
   conv.i4
   not
   ldc.i4 0x7ffffe8d
   beq TestCase4630
   br Done

TestCase4630:
   ldc.i4 4730
   stloc.0
   ldc.i4 0x80000173
   conv.i4
   not
   ldc.i4 0x7ffffe8c
   bne.un Done

TestCase4629:
   ldc.i4 4729
   stloc.0
   ldc.i4 0x80000174
   conv.i4
   not
   ldc.i4 0x7ffffe8b
   bne.un Done

TestCase4628:
   ldc.i4 4728
   stloc.0
   ldc.i4 0x80000175
   conv.i4
   not
   ldc.i4 0x7ffffe8a
   beq TestCase4627
   br Done

TestCase4627:
   ldc.i4 4727
   stloc.0
   ldc.i4 0x80000176
   conv.i4
   not
   ldc.i4 0x7ffffe89
   bne.un Done

TestCase4626:
   ldc.i4 4726
   stloc.0
   ldc.i4 0x80000177
   conv.i4
   not
   ldc.i4 0x7ffffe88
   bne.un Done

TestCase4625:
   ldc.i4 4725
   stloc.0
   ldc.i4 0x80000178
   conv.i4
   not
   ldc.i4 0x7ffffe87
   beq TestCase4624
   br Done

TestCase4624:
   ldc.i4 4724
   stloc.0
   ldc.i4 0x80000179
   conv.i4
   not
   ldc.i4 0x7ffffe86
   bne.un Done

TestCase4623:
   ldc.i4 4723
   stloc.0
   ldc.i4 0x8000017a
   conv.i4
   not
   ldc.i4 0x7ffffe85
   beq TestCase4622
   br Done

TestCase4622:
   ldc.i4 4722
   stloc.0
   ldc.i4 0x8000017b
   conv.i4
   not
   ldc.i4 0x7ffffe84
   bne.un Done

TestCase4621:
   ldc.i4 4721
   stloc.0
   ldc.i4 0x8000017c
   conv.i4
   not
   ldc.i4 0x7ffffe83
   beq TestCase4620
   br Done

TestCase4620:
   ldc.i4 4720
   stloc.0
   ldc.i4 0x8000017d
   conv.i4
   not
   ldc.i4 0x7ffffe82
   bne.un Done

TestCase4619:
   ldc.i4 4719
   stloc.0
   ldc.i4 0x8000017e
   conv.i4
   not
   ldc.i4 0x7ffffe81
   bne.un Done

TestCase4618:
   ldc.i4 4718
   stloc.0
   ldc.i4 0x8000017f
   conv.i4
   not
   ldc.i4 0x7ffffe80
   bne.un Done

TestCase4617:
   ldc.i4 4717
   stloc.0
   ldc.i4 0x80000180
   conv.i4
   not
   ldc.i4 0x7ffffe7f
   beq TestCase4616
   br Done

TestCase4616:
   ldc.i4 4716
   stloc.0
   ldc.i4 0x80000181
   conv.i4
   not
   ldc.i4 0x7ffffe7e
   bne.un Done

TestCase4615:
   ldc.i4 4715
   stloc.0
   ldc.i4 0x80000182
   conv.i4
   not
   ldc.i4 0x7ffffe7d
   beq TestCase4614
   br Done

TestCase4614:
   ldc.i4 4714
   stloc.0
   ldc.i4 0x80000183
   conv.i4
   not
   ldc.i4 0x7ffffe7c
   bne.un Done

TestCase4613:
   ldc.i4 4713
   stloc.0
   ldc.i4 0x80000184
   conv.i4
   not
   ldc.i4 0x7ffffe7b
   bne.un Done

TestCase4612:
   ldc.i4 4712
   stloc.0
   ldc.i4 0x80000185
   conv.i4
   not
   ldc.i4 0x7ffffe7a
   bne.un Done

TestCase4611:
   ldc.i4 4711
   stloc.0
   ldc.i4 0x80000186
   conv.i4
   not
   ldc.i4 0x7ffffe79
   beq TestCase4610
   br Done

TestCase4610:
   ldc.i4 4710
   stloc.0
   ldc.i4 0x80000187
   conv.i4
   not
   ldc.i4 0x7ffffe78
   beq TestCase4609
   br Done

TestCase4609:
   ldc.i4 4709
   stloc.0
   ldc.i4 0x80000188
   conv.i4
   not
   ldc.i4 0x7ffffe77
   beq TestCase4608
   br Done

TestCase4608:
   ldc.i4 4708
   stloc.0
   ldc.i4 0x80000189
   conv.i4
   not
   ldc.i4 0x7ffffe76
   beq TestCase4607
   br Done

TestCase4607:
   ldc.i4 4707
   stloc.0
   ldc.i4 0x8000018a
   conv.i4
   not
   ldc.i4 0x7ffffe75
   beq TestCase4606
   br Done

TestCase4606:
   ldc.i4 4706
   stloc.0
   ldc.i4 0x8000018b
   conv.i4
   not
   ldc.i4 0x7ffffe74
   beq TestCase4605
   br Done

TestCase4605:
   ldc.i4 4705
   stloc.0
   ldc.i4 0x8000018c
   conv.i4
   not
   ldc.i4 0x7ffffe73
   bne.un Done

TestCase4604:
   ldc.i4 4704
   stloc.0
   ldc.i4 0x8000018d
   conv.i4
   not
   ldc.i4 0x7ffffe72
   beq TestCase4603
   br Done

TestCase4603:
   ldc.i4 4703
   stloc.0
   ldc.i4 0x8000018e
   conv.i4
   not
   ldc.i4 0x7ffffe71
   beq TestCase4602
   br Done

TestCase4602:
   ldc.i4 4702
   stloc.0
   ldc.i4 0x8000018f
   conv.i4
   not
   ldc.i4 0x7ffffe70
   bne.un Done

TestCase4601:
   ldc.i4 4701
   stloc.0
   ldc.i4 0x80000190
   conv.i4
   not
   ldc.i4 0x7ffffe6f
   beq TestCase4600
   br Done

TestCase4600:
   ldc.i4 4700
   stloc.0
   ldc.i4 0x80000191
   conv.i4
   not
   ldc.i4 0x7ffffe6e
   beq TestCase4599
   br Done

TestCase4599:
   ldc.i4 4699
   stloc.0
   ldc.i4 0x80000192
   conv.i4
   not
   ldc.i4 0x7ffffe6d
   bne.un Done

TestCase4598:
   ldc.i4 4698
   stloc.0
   ldc.i4 0x80000193
   conv.i4
   not
   ldc.i4 0x7ffffe6c
   bne.un Done

TestCase4597:
   ldc.i4 4697
   stloc.0
   ldc.i4 0x80000194
   conv.i4
   not
   ldc.i4 0x7ffffe6b
   bne.un Done

TestCase4596:
   ldc.i4 4696
   stloc.0
   ldc.i4 0x80000195
   conv.i4
   not
   ldc.i4 0x7ffffe6a
   beq TestCase4595
   br Done

TestCase4595:
   ldc.i4 4695
   stloc.0
   ldc.i4 0x80000196
   conv.i4
   not
   ldc.i4 0x7ffffe69
   beq TestCase4594
   br Done

TestCase4594:
   ldc.i4 4694
   stloc.0
   ldc.i4 0x80000197
   conv.i4
   not
   ldc.i4 0x7ffffe68
   bne.un Done

TestCase4593:
   ldc.i4 4693
   stloc.0
   ldc.i4 0x80000198
   conv.i4
   not
   ldc.i4 0x7ffffe67
   bne.un Done

TestCase4592:
   ldc.i4 4692
   stloc.0
   ldc.i4 0x80000199
   conv.i4
   not
   ldc.i4 0x7ffffe66
   beq TestCase4591
   br Done

TestCase4591:
   ldc.i4 4691
   stloc.0
   ldc.i4 0x8000019a
   conv.i4
   not
   ldc.i4 0x7ffffe65
   beq TestCase4590
   br Done

TestCase4590:
   ldc.i4 4690
   stloc.0
   ldc.i4 0x8000019b
   conv.i4
   not
   ldc.i4 0x7ffffe64
   bne.un Done

TestCase4589:
   ldc.i4 4689
   stloc.0
   ldc.i4 0x8000019c
   conv.i4
   not
   ldc.i4 0x7ffffe63
   bne.un Done

TestCase4588:
   ldc.i4 4688
   stloc.0
   ldc.i4 0x8000019d
   conv.i4
   not
   ldc.i4 0x7ffffe62
   bne.un Done

TestCase4587:
   ldc.i4 4687
   stloc.0
   ldc.i4 0x8000019e
   conv.i4
   not
   ldc.i4 0x7ffffe61
   beq TestCase4586
   br Done

TestCase4586:
   ldc.i4 4686
   stloc.0
   ldc.i4 0x8000019f
   conv.i4
   not
   ldc.i4 0x7ffffe60
   beq TestCase4585
   br Done

TestCase4585:
   ldc.i4 4685
   stloc.0
   ldc.i4 0x800001a0
   conv.i4
   not
   ldc.i4 0x7ffffe5f
   beq TestCase4584
   br Done

TestCase4584:
   ldc.i4 4684
   stloc.0
   ldc.i4 0x800001a1
   conv.i4
   not
   ldc.i4 0x7ffffe5e
   beq TestCase4583
   br Done

TestCase4583:
   ldc.i4 4683
   stloc.0
   ldc.i4 0x800001a2
   conv.i4
   not
   ldc.i4 0x7ffffe5d
   beq TestCase4582
   br Done

TestCase4582:
   ldc.i4 4682
   stloc.0
   ldc.i4 0x800001a3
   conv.i4
   not
   ldc.i4 0x7ffffe5c
   beq TestCase4581
   br Done

TestCase4581:
   ldc.i4 4681
   stloc.0
   ldc.i4 0x800001a4
   conv.i4
   not
   ldc.i4 0x7ffffe5b
   bne.un Done

TestCase4580:
   ldc.i4 4680
   stloc.0
   ldc.i4 0x800001a5
   conv.i4
   not
   ldc.i4 0x7ffffe5a
   bne.un Done

TestCase4579:
   ldc.i4 4679
   stloc.0
   ldc.i4 0x800001a6
   conv.i4
   not
   ldc.i4 0x7ffffe59
   bne.un Done

TestCase4578:
   ldc.i4 4678
   stloc.0
   ldc.i4 0x800001a7
   conv.i4
   not
   ldc.i4 0x7ffffe58
   beq TestCase4577
   br Done

TestCase4577:
   ldc.i4 4677
   stloc.0
   ldc.i4 0x800001a8
   conv.i4
   not
   ldc.i4 0x7ffffe57
   beq TestCase4576
   br Done

TestCase4576:
   ldc.i4 4676
   stloc.0
   ldc.i4 0x800001a9
   conv.i4
   not
   ldc.i4 0x7ffffe56
   beq TestCase4575
   br Done

TestCase4575:
   ldc.i4 4675
   stloc.0
   ldc.i4 0x800001aa
   conv.i4
   not
   ldc.i4 0x7ffffe55
   bne.un Done

TestCase4574:
   ldc.i4 4674
   stloc.0
   ldc.i4 0x800001ab
   conv.i4
   not
   ldc.i4 0x7ffffe54
   bne.un Done

TestCase4573:
   ldc.i4 4673
   stloc.0
   ldc.i4 0x800001ac
   conv.i4
   not
   ldc.i4 0x7ffffe53
   beq TestCase4572
   br Done

TestCase4572:
   ldc.i4 4672
   stloc.0
   ldc.i4 0x800001ad
   conv.i4
   not
   ldc.i4 0x7ffffe52
   bne.un Done

TestCase4571:
   ldc.i4 4671
   stloc.0
   ldc.i4 0x800001ae
   conv.i4
   not
   ldc.i4 0x7ffffe51
   beq TestCase4570
   br Done

TestCase4570:
   ldc.i4 4670
   stloc.0
   ldc.i4 0x800001af
   conv.i4
   not
   ldc.i4 0x7ffffe50
   beq TestCase4569
   br Done

TestCase4569:
   ldc.i4 4669
   stloc.0
   ldc.i4 0x800001b0
   conv.i4
   not
   ldc.i4 0x7ffffe4f
   beq TestCase4568
   br Done

TestCase4568:
   ldc.i4 4668
   stloc.0
   ldc.i4 0x800001b1
   conv.i4
   not
   ldc.i4 0x7ffffe4e
   bne.un Done

TestCase4567:
   ldc.i4 4667
   stloc.0
   ldc.i4 0x800001b2
   conv.i4
   not
   ldc.i4 0x7ffffe4d
   beq TestCase4566
   br Done

TestCase4566:
   ldc.i4 4666
   stloc.0
   ldc.i4 0x800001b3
   conv.i4
   not
   ldc.i4 0x7ffffe4c
   bne.un Done

TestCase4565:
   ldc.i4 4665
   stloc.0
   ldc.i4 0x800001b4
   conv.i4
   not
   ldc.i4 0x7ffffe4b
   bne.un Done

TestCase4564:
   ldc.i4 4664
   stloc.0
   ldc.i4 0x800001b5
   conv.i4
   not
   ldc.i4 0x7ffffe4a
   bne.un Done

TestCase4563:
   ldc.i4 4663
   stloc.0
   ldc.i4 0x800001b6
   conv.i4
   not
   ldc.i4 0x7ffffe49
   bne.un Done

TestCase4562:
   ldc.i4 4662
   stloc.0
   ldc.i4 0x800001b7
   conv.i4
   not
   ldc.i4 0x7ffffe48
   bne.un Done

TestCase4561:
   ldc.i4 4661
   stloc.0
   ldc.i4 0x800001b8
   conv.i4
   not
   ldc.i4 0x7ffffe47
   bne.un Done

TestCase4560:
   ldc.i4 4660
   stloc.0
   ldc.i4 0x800001b9
   conv.i4
   not
   ldc.i4 0x7ffffe46
   beq TestCase4559
   br Done

TestCase4559:
   ldc.i4 4659
   stloc.0
   ldc.i4 0x800001ba
   conv.i4
   not
   ldc.i4 0x7ffffe45
   beq TestCase4558
   br Done

TestCase4558:
   ldc.i4 4658
   stloc.0
   ldc.i4 0x800001bb
   conv.i4
   not
   ldc.i4 0x7ffffe44
   beq TestCase4557
   br Done

TestCase4557:
   ldc.i4 4657
   stloc.0
   ldc.i4 0x800001bc
   conv.i4
   not
   ldc.i4 0x7ffffe43
   bne.un Done

TestCase4556:
   ldc.i4 4656
   stloc.0
   ldc.i4 0x800001bd
   conv.i4
   not
   ldc.i4 0x7ffffe42
   bne.un Done

TestCase4555:
   ldc.i4 4655
   stloc.0
   ldc.i4 0x800001be
   conv.i4
   not
   ldc.i4 0x7ffffe41
   bne.un Done

TestCase4554:
   ldc.i4 4654
   stloc.0
   ldc.i4 0x800001bf
   conv.i4
   not
   ldc.i4 0x7ffffe40
   beq TestCase4553
   br Done

TestCase4553:
   ldc.i4 4653
   stloc.0
   ldc.i4 0x800001c0
   conv.i4
   not
   ldc.i4 0x7ffffe3f
   beq TestCase4552
   br Done

TestCase4552:
   ldc.i4 4652
   stloc.0
   ldc.i4 0x800001c1
   conv.i4
   not
   ldc.i4 0x7ffffe3e
   beq TestCase4551
   br Done

TestCase4551:
   ldc.i4 4651
   stloc.0
   ldc.i4 0x800001c2
   conv.i4
   not
   ldc.i4 0x7ffffe3d
   beq TestCase4550
   br Done

TestCase4550:
   ldc.i4 4650
   stloc.0
   ldc.i4 0x800001c3
   conv.i4
   not
   ldc.i4 0x7ffffe3c
   bne.un Done

TestCase4549:
   ldc.i4 4649
   stloc.0
   ldc.i4 0x800001c4
   conv.i4
   not
   ldc.i4 0x7ffffe3b
   beq TestCase4548
   br Done

TestCase4548:
   ldc.i4 4648
   stloc.0
   ldc.i4 0x800001c5
   conv.i4
   not
   ldc.i4 0x7ffffe3a
   beq TestCase4547
   br Done

TestCase4547:
   ldc.i4 4647
   stloc.0
   ldc.i4 0x800001c6
   conv.i4
   not
   ldc.i4 0x7ffffe39
   bne.un Done

TestCase4546:
   ldc.i4 4646
   stloc.0
   ldc.i4 0x800001c7
   conv.i4
   not
   ldc.i4 0x7ffffe38
   bne.un Done

TestCase4545:
   ldc.i4 4645
   stloc.0
   ldc.i4 0x800001c8
   conv.i4
   not
   ldc.i4 0x7ffffe37
   bne.un Done

TestCase4544:
   ldc.i4 4644
   stloc.0
   ldc.i4 0x800001c9
   conv.i4
   not
   ldc.i4 0x7ffffe36
   bne.un Done

TestCase4543:
   ldc.i4 4643
   stloc.0
   ldc.i4 0x800001ca
   conv.i4
   not
   ldc.i4 0x7ffffe35
   beq TestCase4542
   br Done

TestCase4542:
   ldc.i4 4642
   stloc.0
   ldc.i4 0x800001cb
   conv.i4
   not
   ldc.i4 0x7ffffe34
   bne.un Done

TestCase4541:
   ldc.i4 4641
   stloc.0
   ldc.i4 0x800001cc
   conv.i4
   not
   ldc.i4 0x7ffffe33
   beq TestCase4540
   br Done

TestCase4540:
   ldc.i4 4640
   stloc.0
   ldc.i4 0x800001cd
   conv.i4
   not
   ldc.i4 0x7ffffe32
   beq TestCase4539
   br Done

TestCase4539:
   ldc.i4 4639
   stloc.0
   ldc.i4 0x800001ce
   conv.i4
   not
   ldc.i4 0x7ffffe31
   beq TestCase4538
   br Done

TestCase4538:
   ldc.i4 4638
   stloc.0
   ldc.i4 0x800001cf
   conv.i4
   not
   ldc.i4 0x7ffffe30
   beq TestCase4537
   br Done

TestCase4537:
   ldc.i4 4637
   stloc.0
   ldc.i4 0x800001d0
   conv.i4
   not
   ldc.i4 0x7ffffe2f
   beq TestCase4536
   br Done

TestCase4536:
   ldc.i4 4636
   stloc.0
   ldc.i4 0x800001d1
   conv.i4
   not
   ldc.i4 0x7ffffe2e
   beq TestCase4535
   br Done

TestCase4535:
   ldc.i4 4635
   stloc.0
   ldc.i4 0x800001d2
   conv.i4
   not
   ldc.i4 0x7ffffe2d
   bne.un Done

TestCase4534:
   ldc.i4 4634
   stloc.0
   ldc.i4 0x800001d3
   conv.i4
   not
   ldc.i4 0x7ffffe2c
   bne.un Done

TestCase4533:
   ldc.i4 4633
   stloc.0
   ldc.i4 0x800001d4
   conv.i4
   not
   ldc.i4 0x7ffffe2b
   bne.un Done

TestCase4532:
   ldc.i4 4632
   stloc.0
   ldc.i4 0x800001d5
   conv.i4
   not
   ldc.i4 0x7ffffe2a
   beq TestCase4531
   br Done

TestCase4531:
   ldc.i4 4631
   stloc.0
   ldc.i4 0x800001d6
   conv.i4
   not
   ldc.i4 0x7ffffe29
   bne.un Done

TestCase4530:
   ldc.i4 4630
   stloc.0
   ldc.i4 0x800001d7
   conv.i4
   not
   ldc.i4 0x7ffffe28
   beq TestCase4529
   br Done

TestCase4529:
   ldc.i4 4629
   stloc.0
   ldc.i4 0x800001d8
   conv.i4
   not
   ldc.i4 0x7ffffe27
   bne.un Done

TestCase4528:
   ldc.i4 4628
   stloc.0
   ldc.i4 0x800001d9
   conv.i4
   not
   ldc.i4 0x7ffffe26
   beq TestCase4527
   br Done

TestCase4527:
   ldc.i4 4627
   stloc.0
   ldc.i4 0x800001da
   conv.i4
   not
   ldc.i4 0x7ffffe25
   beq TestCase4526
   br Done

TestCase4526:
   ldc.i4 4626
   stloc.0
   ldc.i4 0x800001db
   conv.i4
   not
   ldc.i4 0x7ffffe24
   beq TestCase4525
   br Done

TestCase4525:
   ldc.i4 4625
   stloc.0
   ldc.i4 0x800001dc
   conv.i4
   not
   ldc.i4 0x7ffffe23
   beq TestCase4524
   br Done

TestCase4524:
   ldc.i4 4624
   stloc.0
   ldc.i4 0x800001dd
   conv.i4
   not
   ldc.i4 0x7ffffe22
   beq TestCase4523
   br Done

TestCase4523:
   ldc.i4 4623
   stloc.0
   ldc.i4 0x800001de
   conv.i4
   not
   ldc.i4 0x7ffffe21
   bne.un Done

TestCase4522:
   ldc.i4 4622
   stloc.0
   ldc.i4 0x800001df
   conv.i4
   not
   ldc.i4 0x7ffffe20
   beq TestCase4521
   br Done

TestCase4521:
   ldc.i4 4621
   stloc.0
   ldc.i4 0x800001e0
   conv.i4
   not
   ldc.i4 0x7ffffe1f
   beq TestCase4520
   br Done

TestCase4520:
   ldc.i4 4620
   stloc.0
   ldc.i4 0x800001e1
   conv.i4
   not
   ldc.i4 0x7ffffe1e
   bne.un Done

TestCase4519:
   ldc.i4 4619
   stloc.0
   ldc.i4 0x800001e2
   conv.i4
   not
   ldc.i4 0x7ffffe1d
   bne.un Done

TestCase4518:
   ldc.i4 4618
   stloc.0
   ldc.i4 0x800001e3
   conv.i4
   not
   ldc.i4 0x7ffffe1c
   beq TestCase4517
   br Done

TestCase4517:
   ldc.i4 4617
   stloc.0
   ldc.i4 0x800001e4
   conv.i4
   not
   ldc.i4 0x7ffffe1b
   bne.un Done

TestCase4516:
   ldc.i4 4616
   stloc.0
   ldc.i4 0x800001e5
   conv.i4
   not
   ldc.i4 0x7ffffe1a
   beq TestCase4515
   br Done

TestCase4515:
   ldc.i4 4615
   stloc.0
   ldc.i4 0x800001e6
   conv.i4
   not
   ldc.i4 0x7ffffe19
   bne.un Done

TestCase4514:
   ldc.i4 4614
   stloc.0
   ldc.i4 0x800001e7
   conv.i4
   not
   ldc.i4 0x7ffffe18
   bne.un Done

TestCase4513:
   ldc.i4 4613
   stloc.0
   ldc.i4 0x800001e8
   conv.i4
   not
   ldc.i4 0x7ffffe17
   bne.un Done

TestCase4512:
   ldc.i4 4612
   stloc.0
   ldc.i4 0x800001e9
   conv.i4
   not
   ldc.i4 0x7ffffe16
   beq TestCase4511
   br Done

TestCase4511:
   ldc.i4 4611
   stloc.0
   ldc.i4 0x800001ea
   conv.i4
   not
   ldc.i4 0x7ffffe15
   beq TestCase4510
   br Done

TestCase4510:
   ldc.i4 4610
   stloc.0
   ldc.i4 0x800001eb
   conv.i4
   not
   ldc.i4 0x7ffffe14
   bne.un Done

TestCase4509:
   ldc.i4 4609
   stloc.0
   ldc.i4 0x800001ec
   conv.i4
   not
   ldc.i4 0x7ffffe13
   beq TestCase4508
   br Done

TestCase4508:
   ldc.i4 4608
   stloc.0
   ldc.i4 0x800001ed
   conv.i4
   not
   ldc.i4 0x7ffffe12
   bne.un Done

TestCase4507:
   ldc.i4 4607
   stloc.0
   ldc.i4 0x800001ee
   conv.i4
   not
   ldc.i4 0x7ffffe11
   bne.un Done

TestCase4506:
   ldc.i4 4606
   stloc.0
   ldc.i4 0x800001ef
   conv.i4
   not
   ldc.i4 0x7ffffe10
   bne.un Done

TestCase4505:
   ldc.i4 4605
   stloc.0
   ldc.i4 0x800001f0
   conv.i4
   not
   ldc.i4 0x7ffffe0f
   bne.un Done

TestCase4504:
   ldc.i4 4604
   stloc.0
   ldc.i4 0x800001f1
   conv.i4
   not
   ldc.i4 0x7ffffe0e
   bne.un Done

TestCase4503:
   ldc.i4 4603
   stloc.0
   ldc.i4 0x800001f2
   conv.i4
   not
   ldc.i4 0x7ffffe0d
   beq TestCase4502
   br Done

TestCase4502:
   ldc.i4 4602
   stloc.0
   ldc.i4 0x800001f3
   conv.i4
   not
   ldc.i4 0x7ffffe0c
   bne.un Done

TestCase4501:
   ldc.i4 4601
   stloc.0
   ldc.i4 0x800001f4
   conv.i4
   not
   ldc.i4 0x7ffffe0b
   bne.un Done

TestCase4500:
   ldc.i4 4600
   stloc.0
   ldc.i4 0x800001f5
   conv.i4
   not
   ldc.i4 0x7ffffe0a
   bne.un Done

TestCase4499:
   ldc.i4 4599
   stloc.0
   ldc.i4 0x800001f6
   conv.i4
   not
   ldc.i4 0x7ffffe09
   beq TestCase4498
   br Done

TestCase4498:
   ldc.i4 4598
   stloc.0
   ldc.i4 0x800001f7
   conv.i4
   not
   ldc.i4 0x7ffffe08
   bne.un Done

TestCase4497:
   ldc.i4 4597
   stloc.0
   ldc.i4 0x800001f8
   conv.i4
   not
   ldc.i4 0x7ffffe07
   bne.un Done

TestCase4496:
   ldc.i4 4596
   stloc.0
   ldc.i4 0x800001f9
   conv.i4
   not
   ldc.i4 0x7ffffe06
   bne.un Done

TestCase4495:
   ldc.i4 4595
   stloc.0
   ldc.i4 0x800001fa
   conv.i4
   not
   ldc.i4 0x7ffffe05
   bne.un Done

TestCase4494:
   ldc.i4 4594
   stloc.0
   ldc.i4 0x800001fb
   conv.i4
   not
   ldc.i4 0x7ffffe04
   beq TestCase4493
   br Done

TestCase4493:
   ldc.i4 4593
   stloc.0
   ldc.i4 0x800001fc
   conv.i4
   not
   ldc.i4 0x7ffffe03
   bne.un Done

TestCase4492:
   ldc.i4 4592
   stloc.0
   ldc.i4 0x800001fd
   conv.i4
   not
   ldc.i4 0x7ffffe02
   beq TestCase4491
   br Done

TestCase4491:
   ldc.i4 4591
   stloc.0
   ldc.i4 0x800001fe
   conv.i4
   not
   ldc.i4 0x7ffffe01
   bne.un Done

TestCase4490:
   ldc.i4 4590
   stloc.0
   ldc.i4 0x800001ff
   conv.i4
   not
   ldc.i4 0x7ffffe00
   beq TestCase4489
   br Done

TestCase4489:
   ldc.i4 4589
   stloc.0
   ldc.i4 0x80000200
   conv.i4
   not
   ldc.i4 0x7ffffdff
   bne.un Done

TestCase4488:
   ldc.i4 4588
   stloc.0
   ldc.i4 0x80000201
   conv.i4
   not
   ldc.i4 0x7ffffdfe
   bne.un Done

TestCase4487:
   ldc.i4 4587
   stloc.0
   ldc.i4 0x80000202
   conv.i4
   not
   ldc.i4 0x7ffffdfd
   bne.un Done

TestCase4486:
   ldc.i4 4586
   stloc.0
   ldc.i4 0x80000203
   conv.i4
   not
   ldc.i4 0x7ffffdfc
   bne.un Done

TestCase4485:
   ldc.i4 4585
   stloc.0
   ldc.i4 0x80000204
   conv.i4
   not
   ldc.i4 0x7ffffdfb
   beq TestCase4484
   br Done

TestCase4484:
   ldc.i4 4584
   stloc.0
   ldc.i4 0x80000205
   conv.i4
   not
   ldc.i4 0x7ffffdfa
   beq TestCase4483
   br Done

TestCase4483:
   ldc.i4 4583
   stloc.0
   ldc.i4 0x80000206
   conv.i4
   not
   ldc.i4 0x7ffffdf9
   beq TestCase4482
   br Done

TestCase4482:
   ldc.i4 4582
   stloc.0
   ldc.i4 0x80000207
   conv.i4
   not
   ldc.i4 0x7ffffdf8
   beq TestCase4481
   br Done

TestCase4481:
   ldc.i4 4581
   stloc.0
   ldc.i4 0x80000208
   conv.i4
   not
   ldc.i4 0x7ffffdf7
   beq TestCase4480
   br Done

TestCase4480:
   ldc.i4 4580
   stloc.0
   ldc.i4 0x80000209
   conv.i4
   not
   ldc.i4 0x7ffffdf6
   beq TestCase4479
   br Done

TestCase4479:
   ldc.i4 4579
   stloc.0
   ldc.i4 0x8000020a
   conv.i4
   not
   ldc.i4 0x7ffffdf5
   bne.un Done

TestCase4478:
   ldc.i4 4578
   stloc.0
   ldc.i4 0x8000020b
   conv.i4
   not
   ldc.i4 0x7ffffdf4
   bne.un Done

TestCase4477:
   ldc.i4 4577
   stloc.0
   ldc.i4 0x8000020c
   conv.i4
   not
   ldc.i4 0x7ffffdf3
   bne.un Done

TestCase4476:
   ldc.i4 4576
   stloc.0
   ldc.i4 0x8000020d
   conv.i4
   not
   ldc.i4 0x7ffffdf2
   beq TestCase4475
   br Done

TestCase4475:
   ldc.i4 4575
   stloc.0
   ldc.i4 0x8000020e
   conv.i4
   not
   ldc.i4 0x7ffffdf1
   bne.un Done

TestCase4474:
   ldc.i4 4574
   stloc.0
   ldc.i4 0x8000020f
   conv.i4
   not
   ldc.i4 0x7ffffdf0
   beq TestCase4473
   br Done

TestCase4473:
   ldc.i4 4573
   stloc.0
   ldc.i4 0x80000210
   conv.i4
   not
   ldc.i4 0x7ffffdef
   bne.un Done

TestCase4472:
   ldc.i4 4572
   stloc.0
   ldc.i4 0x80000211
   conv.i4
   not
   ldc.i4 0x7ffffdee
   bne.un Done

TestCase4471:
   ldc.i4 4571
   stloc.0
   ldc.i4 0x80000212
   conv.i4
   not
   ldc.i4 0x7ffffded
   bne.un Done

TestCase4470:
   ldc.i4 4570
   stloc.0
   ldc.i4 0x80000213
   conv.i4
   not
   ldc.i4 0x7ffffdec
   bne.un Done

TestCase4469:
   ldc.i4 4569
   stloc.0
   ldc.i4 0x80000214
   conv.i4
   not
   ldc.i4 0x7ffffdeb
   beq TestCase4468
   br Done

TestCase4468:
   ldc.i4 4568
   stloc.0
   ldc.i4 0x80000215
   conv.i4
   not
   ldc.i4 0x7ffffdea
   beq TestCase4467
   br Done

TestCase4467:
   ldc.i4 4567
   stloc.0
   ldc.i4 0x80000216
   conv.i4
   not
   ldc.i4 0x7ffffde9
   bne.un Done

TestCase4466:
   ldc.i4 4566
   stloc.0
   ldc.i4 0x80000217
   conv.i4
   not
   ldc.i4 0x7ffffde8
   beq TestCase4465
   br Done

TestCase4465:
   ldc.i4 4565
   stloc.0
   ldc.i4 0x80000218
   conv.i4
   not
   ldc.i4 0x7ffffde7
   bne.un Done

TestCase4464:
   ldc.i4 4564
   stloc.0
   ldc.i4 0x80000219
   conv.i4
   not
   ldc.i4 0x7ffffde6
   beq TestCase4463
   br Done

TestCase4463:
   ldc.i4 4563
   stloc.0
   ldc.i4 0x8000021a
   conv.i4
   not
   ldc.i4 0x7ffffde5
   beq TestCase4462
   br Done

TestCase4462:
   ldc.i4 4562
   stloc.0
   ldc.i4 0x8000021b
   conv.i4
   not
   ldc.i4 0x7ffffde4
   beq TestCase4461
   br Done

TestCase4461:
   ldc.i4 4561
   stloc.0
   ldc.i4 0x8000021c
   conv.i4
   not
   ldc.i4 0x7ffffde3
   beq TestCase4460
   br Done

TestCase4460:
   ldc.i4 4560
   stloc.0
   ldc.i4 0x8000021d
   conv.i4
   not
   ldc.i4 0x7ffffde2
   bne.un Done

TestCase4459:
   ldc.i4 4559
   stloc.0
   ldc.i4 0x8000021e
   conv.i4
   not
   ldc.i4 0x7ffffde1
   bne.un Done

TestCase4458:
   ldc.i4 4558
   stloc.0
   ldc.i4 0x8000021f
   conv.i4
   not
   ldc.i4 0x7ffffde0
   bne.un Done

TestCase4457:
   ldc.i4 4557
   stloc.0
   ldc.i4 0x80000220
   conv.i4
   not
   ldc.i4 0x7ffffddf
   beq TestCase4456
   br Done

TestCase4456:
   ldc.i4 4556
   stloc.0
   ldc.i4 0x80000221
   conv.i4
   not
   ldc.i4 0x7ffffdde
   beq TestCase4455
   br Done

TestCase4455:
   ldc.i4 4555
   stloc.0
   ldc.i4 0x80000222
   conv.i4
   not
   ldc.i4 0x7ffffddd
   bne.un Done

TestCase4454:
   ldc.i4 4554
   stloc.0
   ldc.i4 0x80000223
   conv.i4
   not
   ldc.i4 0x7ffffddc
   beq TestCase4453
   br Done

TestCase4453:
   ldc.i4 4553
   stloc.0
   ldc.i4 0x80000224
   conv.i4
   not
   ldc.i4 0x7ffffddb
   beq TestCase4452
   br Done

TestCase4452:
   ldc.i4 4552
   stloc.0
   ldc.i4 0x80000225
   conv.i4
   not
   ldc.i4 0x7ffffdda
   bne.un Done

TestCase4451:
   ldc.i4 4551
   stloc.0
   ldc.i4 0x80000226
   conv.i4
   not
   ldc.i4 0x7ffffdd9
   beq TestCase4450
   br Done

TestCase4450:
   ldc.i4 4550
   stloc.0
   ldc.i4 0x80000227
   conv.i4
   not
   ldc.i4 0x7ffffdd8
   beq TestCase4449
   br Done

TestCase4449:
   ldc.i4 4549
   stloc.0
   ldc.i4 0x80000228
   conv.i4
   not
   ldc.i4 0x7ffffdd7
   beq TestCase4448
   br Done

TestCase4448:
   ldc.i4 4548
   stloc.0
   ldc.i4 0x80000229
   conv.i4
   not
   ldc.i4 0x7ffffdd6
   beq TestCase4447
   br Done

TestCase4447:
   ldc.i4 4547
   stloc.0
   ldc.i4 0x8000022a
   conv.i4
   not
   ldc.i4 0x7ffffdd5
   bne.un Done

TestCase4446:
   ldc.i4 4546
   stloc.0
   ldc.i4 0x8000022b
   conv.i4
   not
   ldc.i4 0x7ffffdd4
   beq TestCase4445
   br Done

TestCase4445:
   ldc.i4 4545
   stloc.0
   ldc.i4 0x8000022c
   conv.i4
   not
   ldc.i4 0x7ffffdd3
   bne.un Done

TestCase4444:
   ldc.i4 4544
   stloc.0
   ldc.i4 0x8000022d
   conv.i4
   not
   ldc.i4 0x7ffffdd2
   bne.un Done

TestCase4443:
   ldc.i4 4543
   stloc.0
   ldc.i4 0x8000022e
   conv.i4
   not
   ldc.i4 0x7ffffdd1
   beq TestCase4442
   br Done

TestCase4442:
   ldc.i4 4542
   stloc.0
   ldc.i4 0x8000022f
   conv.i4
   not
   ldc.i4 0x7ffffdd0
   beq TestCase4441
   br Done

TestCase4441:
   ldc.i4 4541
   stloc.0
   ldc.i4 0x80000230
   conv.i4
   not
   ldc.i4 0x7ffffdcf
   beq TestCase4440
   br Done

TestCase4440:
   ldc.i4 4540
   stloc.0
   ldc.i4 0x80000231
   conv.i4
   not
   ldc.i4 0x7ffffdce
   bne.un Done

TestCase4439:
   ldc.i4 4539
   stloc.0
   ldc.i4 0x80000232
   conv.i4
   not
   ldc.i4 0x7ffffdcd
   beq TestCase4438
   br Done

TestCase4438:
   ldc.i4 4538
   stloc.0
   ldc.i4 0x80000233
   conv.i4
   not
   ldc.i4 0x7ffffdcc
   bne.un Done

TestCase4437:
   ldc.i4 4537
   stloc.0
   ldc.i4 0x80000234
   conv.i4
   not
   ldc.i4 0x7ffffdcb
   bne.un Done

TestCase4436:
   ldc.i4 4536
   stloc.0
   ldc.i4 0x80000235
   conv.i4
   not
   ldc.i4 0x7ffffdca
   beq TestCase4435
   br Done

TestCase4435:
   ldc.i4 4535
   stloc.0
   ldc.i4 0x80000236
   conv.i4
   not
   ldc.i4 0x7ffffdc9
   beq TestCase4434
   br Done

TestCase4434:
   ldc.i4 4534
   stloc.0
   ldc.i4 0x80000237
   conv.i4
   not
   ldc.i4 0x7ffffdc8
   bne.un Done

TestCase4433:
   ldc.i4 4533
   stloc.0
   ldc.i4 0x80000238
   conv.i4
   not
   ldc.i4 0x7ffffdc7
   bne.un Done

TestCase4432:
   ldc.i4 4532
   stloc.0
   ldc.i4 0x80000239
   conv.i4
   not
   ldc.i4 0x7ffffdc6
   beq TestCase4431
   br Done

TestCase4431:
   ldc.i4 4531
   stloc.0
   ldc.i4 0x8000023a
   conv.i4
   not
   ldc.i4 0x7ffffdc5
   bne.un Done

TestCase4430:
   ldc.i4 4530
   stloc.0
   ldc.i4 0x8000023b
   conv.i4
   not
   ldc.i4 0x7ffffdc4
   bne.un Done

TestCase4429:
   ldc.i4 4529
   stloc.0
   ldc.i4 0x8000023c
   conv.i4
   not
   ldc.i4 0x7ffffdc3
   bne.un Done

TestCase4428:
   ldc.i4 4528
   stloc.0
   ldc.i4 0x8000023d
   conv.i4
   not
   ldc.i4 0x7ffffdc2
   bne.un Done

TestCase4427:
   ldc.i4 4527
   stloc.0
   ldc.i4 0x8000023e
   conv.i4
   not
   ldc.i4 0x7ffffdc1
   beq TestCase4426
   br Done

TestCase4426:
   ldc.i4 4526
   stloc.0
   ldc.i4 0x8000023f
   conv.i4
   not
   ldc.i4 0x7ffffdc0
   bne.un Done

TestCase4425:
   ldc.i4 4525
   stloc.0
   ldc.i4 0x80000240
   conv.i4
   not
   ldc.i4 0x7ffffdbf
   beq TestCase4424
   br Done

TestCase4424:
   ldc.i4 4524
   stloc.0
   ldc.i4 0x80000241
   conv.i4
   not
   ldc.i4 0x7ffffdbe
   bne.un Done

TestCase4423:
   ldc.i4 4523
   stloc.0
   ldc.i4 0x80000242
   conv.i4
   not
   ldc.i4 0x7ffffdbd
   beq TestCase4422
   br Done

TestCase4422:
   ldc.i4 4522
   stloc.0
   ldc.i4 0x80000243
   conv.i4
   not
   ldc.i4 0x7ffffdbc
   beq TestCase4421
   br Done

TestCase4421:
   ldc.i4 4521
   stloc.0
   ldc.i4 0x80000244
   conv.i4
   not
   ldc.i4 0x7ffffdbb
   beq TestCase4420
   br Done

TestCase4420:
   ldc.i4 4520
   stloc.0
   ldc.i4 0x80000245
   conv.i4
   not
   ldc.i4 0x7ffffdba
   beq TestCase4419
   br Done

TestCase4419:
   ldc.i4 4519
   stloc.0
   ldc.i4 0x80000246
   conv.i4
   not
   ldc.i4 0x7ffffdb9
   beq TestCase4418
   br Done

TestCase4418:
   ldc.i4 4518
   stloc.0
   ldc.i4 0x80000247
   conv.i4
   not
   ldc.i4 0x7ffffdb8
   beq TestCase4417
   br Done

TestCase4417:
   ldc.i4 4517
   stloc.0
   ldc.i4 0x80000248
   conv.i4
   not
   ldc.i4 0x7ffffdb7
   bne.un Done

TestCase4416:
   ldc.i4 4516
   stloc.0
   ldc.i4 0x80000249
   conv.i4
   not
   ldc.i4 0x7ffffdb6
   bne.un Done

TestCase4415:
   ldc.i4 4515
   stloc.0
   ldc.i4 0x8000024a
   conv.i4
   not
   ldc.i4 0x7ffffdb5
   bne.un Done

TestCase4414:
   ldc.i4 4514
   stloc.0
   ldc.i4 0x8000024b
   conv.i4
   not
   ldc.i4 0x7ffffdb4
   bne.un Done

TestCase4413:
   ldc.i4 4513
   stloc.0
   ldc.i4 0x8000024c
   conv.i4
   not
   ldc.i4 0x7ffffdb3
   bne.un Done

TestCase4412:
   ldc.i4 4512
   stloc.0
   ldc.i4 0x8000024d
   conv.i4
   not
   ldc.i4 0x7ffffdb2
   beq TestCase4411
   br Done

TestCase4411:
   ldc.i4 4511
   stloc.0
   ldc.i4 0x8000024e
   conv.i4
   not
   ldc.i4 0x7ffffdb1
   beq TestCase4410
   br Done

TestCase4410:
   ldc.i4 4510
   stloc.0
   ldc.i4 0x8000024f
   conv.i4
   not
   ldc.i4 0x7ffffdb0
   beq TestCase4409
   br Done

TestCase4409:
   ldc.i4 4509
   stloc.0
   ldc.i4 0x80000250
   conv.i4
   not
   ldc.i4 0x7ffffdaf
   bne.un Done

TestCase4408:
   ldc.i4 4508
   stloc.0
   ldc.i4 0x80000251
   conv.i4
   not
   ldc.i4 0x7ffffdae
   beq TestCase4407
   br Done

TestCase4407:
   ldc.i4 4507
   stloc.0
   ldc.i4 0x80000252
   conv.i4
   not
   ldc.i4 0x7ffffdad
   bne.un Done

TestCase4406:
   ldc.i4 4506
   stloc.0
   ldc.i4 0x80000253
   conv.i4
   not
   ldc.i4 0x7ffffdac
   bne.un Done

TestCase4405:
   ldc.i4 4505
   stloc.0
   ldc.i4 0x80000254
   conv.i4
   not
   ldc.i4 0x7ffffdab
   bne.un Done

TestCase4404:
   ldc.i4 4504
   stloc.0
   ldc.i4 0x80000255
   conv.i4
   not
   ldc.i4 0x7ffffdaa
   bne.un Done

TestCase4403:
   ldc.i4 4503
   stloc.0
   ldc.i4 0x80000256
   conv.i4
   not
   ldc.i4 0x7ffffda9
   beq TestCase4402
   br Done

TestCase4402:
   ldc.i4 4502
   stloc.0
   ldc.i4 0x80000257
   conv.i4
   not
   ldc.i4 0x7ffffda8
   bne.un Done

TestCase4401:
   ldc.i4 4501
   stloc.0
   ldc.i4 0x80000258
   conv.i4
   not
   ldc.i4 0x7ffffda7
   beq TestCase4400
   br Done

TestCase4400:
   ldc.i4 4500
   stloc.0
   ldc.i4 0x80000259
   conv.i4
   not
   ldc.i4 0x7ffffda6
   bne.un Done

TestCase4399:
   ldc.i4 4499
   stloc.0
   ldc.i4 0x8000025a
   conv.i4
   not
   ldc.i4 0x7ffffda5
   bne.un Done

TestCase4398:
   ldc.i4 4498
   stloc.0
   ldc.i4 0x8000025b
   conv.i4
   not
   ldc.i4 0x7ffffda4
   bne.un Done

TestCase4397:
   ldc.i4 4497
   stloc.0
   ldc.i4 0x8000025c
   conv.i4
   not
   ldc.i4 0x7ffffda3
   bne.un Done

TestCase4396:
   ldc.i4 4496
   stloc.0
   ldc.i4 0x8000025d
   conv.i4
   not
   ldc.i4 0x7ffffda2
   beq TestCase4395
   br Done

TestCase4395:
   ldc.i4 4495
   stloc.0
   ldc.i4 0x8000025e
   conv.i4
   not
   ldc.i4 0x7ffffda1
   beq TestCase4394
   br Done

TestCase4394:
   ldc.i4 4494
   stloc.0
   ldc.i4 0x8000025f
   conv.i4
   not
   ldc.i4 0x7ffffda0
   beq TestCase4393
   br Done

TestCase4393:
   ldc.i4 4493
   stloc.0
   ldc.i4 0x80000260
   conv.i4
   not
   ldc.i4 0x7ffffd9f
   beq TestCase4392
   br Done

TestCase4392:
   ldc.i4 4492
   stloc.0
   ldc.i4 0x80000261
   conv.i4
   not
   ldc.i4 0x7ffffd9e
   beq TestCase4391
   br Done

TestCase4391:
   ldc.i4 4491
   stloc.0
   ldc.i4 0x80000262
   conv.i4
   not
   ldc.i4 0x7ffffd9d
   bne.un Done

TestCase4390:
   ldc.i4 4490
   stloc.0
   ldc.i4 0x80000263
   conv.i4
   not
   ldc.i4 0x7ffffd9c
   bne.un Done

TestCase4389:
   ldc.i4 4489
   stloc.0
   ldc.i4 0x80000264
   conv.i4
   not
   ldc.i4 0x7ffffd9b
   bne.un Done

TestCase4388:
   ldc.i4 4488
   stloc.0
   ldc.i4 0x80000265
   conv.i4
   not
   ldc.i4 0x7ffffd9a
   beq TestCase4387
   br Done

TestCase4387:
   ldc.i4 4487
   stloc.0
   ldc.i4 0x80000266
   conv.i4
   not
   ldc.i4 0x7ffffd99
   beq TestCase4386
   br Done

TestCase4386:
   ldc.i4 4486
   stloc.0
   ldc.i4 0x80000267
   conv.i4
   not
   ldc.i4 0x7ffffd98
   bne.un Done

TestCase4385:
   ldc.i4 4485
   stloc.0
   ldc.i4 0x80000268
   conv.i4
   not
   ldc.i4 0x7ffffd97
   beq TestCase4384
   br Done

TestCase4384:
   ldc.i4 4484
   stloc.0
   ldc.i4 0x80000269
   conv.i4
   not
   ldc.i4 0x7ffffd96
   bne.un Done

TestCase4383:
   ldc.i4 4483
   stloc.0
   ldc.i4 0x8000026a
   conv.i4
   not
   ldc.i4 0x7ffffd95
   bne.un Done

TestCase4382:
   ldc.i4 4482
   stloc.0
   ldc.i4 0x8000026b
   conv.i4
   not
   ldc.i4 0x7ffffd94
   bne.un Done

TestCase4381:
   ldc.i4 4481
   stloc.0
   ldc.i4 0x8000026c
   conv.i4
   not
   ldc.i4 0x7ffffd93
   beq TestCase4380
   br Done

TestCase4380:
   ldc.i4 4480
   stloc.0
   ldc.i4 0x8000026d
   conv.i4
   not
   ldc.i4 0x7ffffd92
   beq TestCase4379
   br Done

TestCase4379:
   ldc.i4 4479
   stloc.0
   ldc.i4 0x8000026e
   conv.i4
   not
   ldc.i4 0x7ffffd91
   beq TestCase4378
   br Done

TestCase4378:
   ldc.i4 4478
   stloc.0
   ldc.i4 0x8000026f
   conv.i4
   not
   ldc.i4 0x7ffffd90
   bne.un Done

TestCase4377:
   ldc.i4 4477
   stloc.0
   ldc.i4 0x80000270
   conv.i4
   not
   ldc.i4 0x7ffffd8f
   bne.un Done

TestCase4376:
   ldc.i4 4476
   stloc.0
   ldc.i4 0x80000271
   conv.i4
   not
   ldc.i4 0x7ffffd8e
   bne.un Done

TestCase4375:
   ldc.i4 4475
   stloc.0
   ldc.i4 0x80000272
   conv.i4
   not
   ldc.i4 0x7ffffd8d
   beq TestCase4374
   br Done

TestCase4374:
   ldc.i4 4474
   stloc.0
   ldc.i4 0x80000273
   conv.i4
   not
   ldc.i4 0x7ffffd8c
   beq TestCase4373
   br Done

TestCase4373:
   ldc.i4 4473
   stloc.0
   ldc.i4 0x80000274
   conv.i4
   not
   ldc.i4 0x7ffffd8b
   bne.un Done

TestCase4372:
   ldc.i4 4472
   stloc.0
   ldc.i4 0x80000275
   conv.i4
   not
   ldc.i4 0x7ffffd8a
   beq TestCase4371
   br Done

TestCase4371:
   ldc.i4 4471
   stloc.0
   ldc.i4 0x80000276
   conv.i4
   not
   ldc.i4 0x7ffffd89
   bne.un Done

TestCase4370:
   ldc.i4 4470
   stloc.0
   ldc.i4 0x80000277
   conv.i4
   not
   ldc.i4 0x7ffffd88
   beq TestCase4369
   br Done

TestCase4369:
   ldc.i4 4469
   stloc.0
   ldc.i4 0x80000278
   conv.i4
   not
   ldc.i4 0x7ffffd87
   bne.un Done

TestCase4368:
   ldc.i4 4468
   stloc.0
   ldc.i4 0x80000279
   conv.i4
   not
   ldc.i4 0x7ffffd86
   beq TestCase4367
   br Done

TestCase4367:
   ldc.i4 4467
   stloc.0
   ldc.i4 0x8000027a
   conv.i4
   not
   ldc.i4 0x7ffffd85
   bne.un Done

TestCase4366:
   ldc.i4 4466
   stloc.0
   ldc.i4 0x8000027b
   conv.i4
   not
   ldc.i4 0x7ffffd84
   beq TestCase4365
   br Done

TestCase4365:
   ldc.i4 4465
   stloc.0
   ldc.i4 0x8000027c
   conv.i4
   not
   ldc.i4 0x7ffffd83
   beq TestCase4364
   br Done

TestCase4364:
   ldc.i4 4464
   stloc.0
   ldc.i4 0x8000027d
   conv.i4
   not
   ldc.i4 0x7ffffd82
   beq TestCase4363
   br Done

TestCase4363:
   ldc.i4 4463
   stloc.0
   ldc.i4 0x8000027e
   conv.i4
   not
   ldc.i4 0x7ffffd81
   bne.un Done

TestCase4362:
   ldc.i4 4462
   stloc.0
   ldc.i4 0x8000027f
   conv.i4
   not
   ldc.i4 0x7ffffd80
   bne.un Done

TestCase4361:
   ldc.i4 4461
   stloc.0
   ldc.i4 0x80000280
   conv.i4
   not
   ldc.i4 0x7ffffd7f
   bne.un Done

TestCase4360:
   ldc.i4 4460
   stloc.0
   ldc.i4 0x80000281
   conv.i4
   not
   ldc.i4 0x7ffffd7e
   beq TestCase4359
   br Done

TestCase4359:
   ldc.i4 4459
   stloc.0
   ldc.i4 0x80000282
   conv.i4
   not
   ldc.i4 0x7ffffd7d
   bne.un Done

TestCase4358:
   ldc.i4 4458
   stloc.0
   ldc.i4 0x80000283
   conv.i4
   not
   ldc.i4 0x7ffffd7c
   bne.un Done

TestCase4357:
   ldc.i4 4457
   stloc.0
   ldc.i4 0x80000284
   conv.i4
   not
   ldc.i4 0x7ffffd7b
   beq TestCase4356
   br Done

TestCase4356:
   ldc.i4 4456
   stloc.0
   ldc.i4 0x80000285
   conv.i4
   not
   ldc.i4 0x7ffffd7a
   beq TestCase4355
   br Done

TestCase4355:
   ldc.i4 4455
   stloc.0
   ldc.i4 0x80000286
   conv.i4
   not
   ldc.i4 0x7ffffd79
   bne.un Done

TestCase4354:
   ldc.i4 4454
   stloc.0
   ldc.i4 0x80000287
   conv.i4
   not
   ldc.i4 0x7ffffd78
   beq TestCase4353
   br Done

TestCase4353:
   ldc.i4 4453
   stloc.0
   ldc.i4 0x80000288
   conv.i4
   not
   ldc.i4 0x7ffffd77
   bne.un Done

TestCase4352:
   ldc.i4 4452
   stloc.0
   ldc.i4 0x80000289
   conv.i4
   not
   ldc.i4 0x7ffffd76
   bne.un Done

TestCase4351:
   ldc.i4 4451
   stloc.0
   ldc.i4 0x8000028a
   conv.i4
   not
   ldc.i4 0x7ffffd75
   beq TestCase4350
   br Done

TestCase4350:
   ldc.i4 4450
   stloc.0
   ldc.i4 0x8000028b
   conv.i4
   not
   ldc.i4 0x7ffffd74
   bne.un Done

TestCase4349:
   ldc.i4 4449
   stloc.0
   ldc.i4 0x8000028c
   conv.i4
   not
   ldc.i4 0x7ffffd73
   beq TestCase4348
   br Done

TestCase4348:
   ldc.i4 4448
   stloc.0
   ldc.i4 0x8000028d
   conv.i4
   not
   ldc.i4 0x7ffffd72
   bne.un Done

TestCase4347:
   ldc.i4 4447
   stloc.0
   ldc.i4 0x8000028e
   conv.i4
   not
   ldc.i4 0x7ffffd71
   bne.un Done

TestCase4346:
   ldc.i4 4446
   stloc.0
   ldc.i4 0x8000028f
   conv.i4
   not
   ldc.i4 0x7ffffd70
   bne.un Done

TestCase4345:
   ldc.i4 4445
   stloc.0
   ldc.i4 0x80000290
   conv.i4
   not
   ldc.i4 0x7ffffd6f
   bne.un Done

TestCase4344:
   ldc.i4 4444
   stloc.0
   ldc.i4 0x80000291
   conv.i4
   not
   ldc.i4 0x7ffffd6e
   beq TestCase4343
   br Done

TestCase4343:
   ldc.i4 4443
   stloc.0
   ldc.i4 0x80000292
   conv.i4
   not
   ldc.i4 0x7ffffd6d
   beq TestCase4342
   br Done

TestCase4342:
   ldc.i4 4442
   stloc.0
   ldc.i4 0x80000293
   conv.i4
   not
   ldc.i4 0x7ffffd6c
   beq TestCase4341
   br Done

TestCase4341:
   ldc.i4 4441
   stloc.0
   ldc.i4 0x80000294
   conv.i4
   not
   ldc.i4 0x7ffffd6b
   bne.un Done

TestCase4340:
   ldc.i4 4440
   stloc.0
   ldc.i4 0x80000295
   conv.i4
   not
   ldc.i4 0x7ffffd6a
   bne.un Done

TestCase4339:
   ldc.i4 4439
   stloc.0
   ldc.i4 0x80000296
   conv.i4
   not
   ldc.i4 0x7ffffd69
   bne.un Done

TestCase4338:
   ldc.i4 4438
   stloc.0
   ldc.i4 0x80000297
   conv.i4
   not
   ldc.i4 0x7ffffd68
   bne.un Done

TestCase4337:
   ldc.i4 4437
   stloc.0
   ldc.i4 0x80000298
   conv.i4
   not
   ldc.i4 0x7ffffd67
   bne.un Done

TestCase4336:
   ldc.i4 4436
   stloc.0
   ldc.i4 0x80000299
   conv.i4
   not
   ldc.i4 0x7ffffd66
   bne.un Done

TestCase4335:
   ldc.i4 4435
   stloc.0
   ldc.i4 0x8000029a
   conv.i4
   not
   ldc.i4 0x7ffffd65
   bne.un Done

TestCase4334:
   ldc.i4 4434
   stloc.0
   ldc.i4 0x8000029b
   conv.i4
   not
   ldc.i4 0x7ffffd64
   beq TestCase4333
   br Done

TestCase4333:
   ldc.i4 4433
   stloc.0
   ldc.i4 0x8000029c
   conv.i4
   not
   ldc.i4 0x7ffffd63
   bne.un Done

TestCase4332:
   ldc.i4 4432
   stloc.0
   ldc.i4 0x8000029d
   conv.i4
   not
   ldc.i4 0x7ffffd62
   beq TestCase4331
   br Done

TestCase4331:
   ldc.i4 4431
   stloc.0
   ldc.i4 0x8000029e
   conv.i4
   not
   ldc.i4 0x7ffffd61
   beq TestCase4330
   br Done

TestCase4330:
   ldc.i4 4430
   stloc.0
   ldc.i4 0x8000029f
   conv.i4
   not
   ldc.i4 0x7ffffd60
   bne.un Done

TestCase4329:
   ldc.i4 4429
   stloc.0
   ldc.i4 0x800002a0
   conv.i4
   not
   ldc.i4 0x7ffffd5f
   bne.un Done

TestCase4328:
   ldc.i4 4428
   stloc.0
   ldc.i4 0x800002a1
   conv.i4
   not
   ldc.i4 0x7ffffd5e
   bne.un Done

TestCase4327:
   ldc.i4 4427
   stloc.0
   ldc.i4 0x800002a2
   conv.i4
   not
   ldc.i4 0x7ffffd5d
   bne.un Done

TestCase4326:
   ldc.i4 4426
   stloc.0
   ldc.i4 0x800002a3
   conv.i4
   not
   ldc.i4 0x7ffffd5c
   bne.un Done

TestCase4325:
   ldc.i4 4425
   stloc.0
   ldc.i4 0x800002a4
   conv.i4
   not
   ldc.i4 0x7ffffd5b
   bne.un Done

TestCase4324:
   ldc.i4 4424
   stloc.0
   ldc.i4 0x800002a5
   conv.i4
   not
   ldc.i4 0x7ffffd5a
   bne.un Done

TestCase4323:
   ldc.i4 4423
   stloc.0
   ldc.i4 0x800002a6
   conv.i4
   not
   ldc.i4 0x7ffffd59
   bne.un Done

TestCase4322:
   ldc.i4 4422
   stloc.0
   ldc.i4 0x800002a7
   conv.i4
   not
   ldc.i4 0x7ffffd58
   bne.un Done

TestCase4321:
   ldc.i4 4421
   stloc.0
   ldc.i4 0x800002a8
   conv.i4
   not
   ldc.i4 0x7ffffd57
   bne.un Done

TestCase4320:
   ldc.i4 4420
   stloc.0
   ldc.i4 0x800002a9
   conv.i4
   not
   ldc.i4 0x7ffffd56
   bne.un Done

TestCase4319:
   ldc.i4 4419
   stloc.0
   ldc.i4 0x800002aa
   conv.i4
   not
   ldc.i4 0x7ffffd55
   beq TestCase4318
   br Done

TestCase4318:
   ldc.i4 4418
   stloc.0
   ldc.i4 0x800002ab
   conv.i4
   not
   ldc.i4 0x7ffffd54
   bne.un Done

TestCase4317:
   ldc.i4 4417
   stloc.0
   ldc.i4 0x800002ac
   conv.i4
   not
   ldc.i4 0x7ffffd53
   bne.un Done

TestCase4316:
   ldc.i4 4416
   stloc.0
   ldc.i4 0x800002ad
   conv.i4
   not
   ldc.i4 0x7ffffd52
   bne.un Done

TestCase4315:
   ldc.i4 4415
   stloc.0
   ldc.i4 0x800002ae
   conv.i4
   not
   ldc.i4 0x7ffffd51
   bne.un Done

TestCase4314:
   ldc.i4 4414
   stloc.0
   ldc.i4 0x800002af
   conv.i4
   not
   ldc.i4 0x7ffffd50
   beq TestCase4313
   br Done

TestCase4313:
   ldc.i4 4413
   stloc.0
   ldc.i4 0x800002b0
   conv.i4
   not
   ldc.i4 0x7ffffd4f
   bne.un Done

TestCase4312:
   ldc.i4 4412
   stloc.0
   ldc.i4 0x800002b1
   conv.i4
   not
   ldc.i4 0x7ffffd4e
   bne.un Done

TestCase4311:
   ldc.i4 4411
   stloc.0
   ldc.i4 0x800002b2
   conv.i4
   not
   ldc.i4 0x7ffffd4d
   beq TestCase4310
   br Done

TestCase4310:
   ldc.i4 4410
   stloc.0
   ldc.i4 0x800002b3
   conv.i4
   not
   ldc.i4 0x7ffffd4c
   bne.un Done

TestCase4309:
   ldc.i4 4409
   stloc.0
   ldc.i4 0x800002b4
   conv.i4
   not
   ldc.i4 0x7ffffd4b
   beq TestCase4308
   br Done

TestCase4308:
   ldc.i4 4408
   stloc.0
   ldc.i4 0x800002b5
   conv.i4
   not
   ldc.i4 0x7ffffd4a
   beq TestCase4307
   br Done

TestCase4307:
   ldc.i4 4407
   stloc.0
   ldc.i4 0x800002b6
   conv.i4
   not
   ldc.i4 0x7ffffd49
   beq TestCase4306
   br Done

TestCase4306:
   ldc.i4 4406
   stloc.0
   ldc.i4 0x800002b7
   conv.i4
   not
   ldc.i4 0x7ffffd48
   beq TestCase4305
   br Done

TestCase4305:
   ldc.i4 4405
   stloc.0
   ldc.i4 0x800002b8
   conv.i4
   not
   ldc.i4 0x7ffffd47
   bne.un Done

TestCase4304:
   ldc.i4 4404
   stloc.0
   ldc.i4 0x800002b9
   conv.i4
   not
   ldc.i4 0x7ffffd46
   beq TestCase4303
   br Done

TestCase4303:
   ldc.i4 4403
   stloc.0
   ldc.i4 0x800002ba
   conv.i4
   not
   ldc.i4 0x7ffffd45
   beq TestCase4302
   br Done

TestCase4302:
   ldc.i4 4402
   stloc.0
   ldc.i4 0x800002bb
   conv.i4
   not
   ldc.i4 0x7ffffd44
   beq TestCase4301
   br Done

TestCase4301:
   ldc.i4 4401
   stloc.0
   ldc.i4 0x800002bc
   conv.i4
   not
   ldc.i4 0x7ffffd43
   bne.un Done

TestCase4300:
   ldc.i4 4400
   stloc.0
   ldc.i4 0x800002bd
   conv.i4
   not
   ldc.i4 0x7ffffd42
   beq TestCase4299
   br Done

TestCase4299:
   ldc.i4 4399
   stloc.0
   ldc.i4 0x800002be
   conv.i4
   not
   ldc.i4 0x7ffffd41
   bne.un Done

TestCase4298:
   ldc.i4 4398
   stloc.0
   ldc.i4 0x800002bf
   conv.i4
   not
   ldc.i4 0x7ffffd40
   beq TestCase4297
   br Done

TestCase4297:
   ldc.i4 4397
   stloc.0
   ldc.i4 0x800002c0
   conv.i4
   not
   ldc.i4 0x7ffffd3f
   bne.un Done

TestCase4296:
   ldc.i4 4396
   stloc.0
   ldc.i4 0x800002c1
   conv.i4
   not
   ldc.i4 0x7ffffd3e
   beq TestCase4295
   br Done

TestCase4295:
   ldc.i4 4395
   stloc.0
   ldc.i4 0x800002c2
   conv.i4
   not
   ldc.i4 0x7ffffd3d
   beq TestCase4294
   br Done

TestCase4294:
   ldc.i4 4394
   stloc.0
   ldc.i4 0x800002c3
   conv.i4
   not
   ldc.i4 0x7ffffd3c
   bne.un Done

TestCase4293:
   ldc.i4 4393
   stloc.0
   ldc.i4 0x800002c4
   conv.i4
   not
   ldc.i4 0x7ffffd3b
   beq TestCase4292
   br Done

TestCase4292:
   ldc.i4 4392
   stloc.0
   ldc.i4 0x800002c5
   conv.i4
   not
   ldc.i4 0x7ffffd3a
   beq TestCase4291
   br Done

TestCase4291:
   ldc.i4 4391
   stloc.0
   ldc.i4 0x800002c6
   conv.i4
   not
   ldc.i4 0x7ffffd39
   bne.un Done

TestCase4290:
   ldc.i4 4390
   stloc.0
   ldc.i4 0x800002c7
   conv.i4
   not
   ldc.i4 0x7ffffd38
   beq TestCase4289
   br Done

TestCase4289:
   ldc.i4 4389
   stloc.0
   ldc.i4 0x800002c8
   conv.i4
   not
   ldc.i4 0x7ffffd37
   beq TestCase4288
   br Done

TestCase4288:
   ldc.i4 4388
   stloc.0
   ldc.i4 0x800002c9
   conv.i4
   not
   ldc.i4 0x7ffffd36
   beq TestCase4287
   br Done

TestCase4287:
   ldc.i4 4387
   stloc.0
   ldc.i4 0x800002ca
   conv.i4
   not
   ldc.i4 0x7ffffd35
   beq TestCase4286
   br Done

TestCase4286:
   ldc.i4 4386
   stloc.0
   ldc.i4 0x800002cb
   conv.i4
   not
   ldc.i4 0x7ffffd34
   beq TestCase4285
   br Done

TestCase4285:
   ldc.i4 4385
   stloc.0
   ldc.i4 0x800002cc
   conv.i4
   not
   ldc.i4 0x7ffffd33
   beq TestCase4284
   br Done

TestCase4284:
   ldc.i4 4384
   stloc.0
   ldc.i4 0x800002cd
   conv.i4
   not
   ldc.i4 0x7ffffd32
   bne.un Done

TestCase4283:
   ldc.i4 4383
   stloc.0
   ldc.i4 0x800002ce
   conv.i4
   not
   ldc.i4 0x7ffffd31
   bne.un Done

TestCase4282:
   ldc.i4 4382
   stloc.0
   ldc.i4 0x800002cf
   conv.i4
   not
   ldc.i4 0x7ffffd30
   beq TestCase4281
   br Done

TestCase4281:
   ldc.i4 4381
   stloc.0
   ldc.i4 0x800002d0
   conv.i4
   not
   ldc.i4 0x7ffffd2f
   bne.un Done

TestCase4280:
   ldc.i4 4380
   stloc.0
   ldc.i4 0x800002d1
   conv.i4
   not
   ldc.i4 0x7ffffd2e
   bne.un Done

TestCase4279:
   ldc.i4 4379
   stloc.0
   ldc.i4 0x800002d2
   conv.i4
   not
   ldc.i4 0x7ffffd2d
   bne.un Done

TestCase4278:
   ldc.i4 4378
   stloc.0
   ldc.i4 0x800002d3
   conv.i4
   not
   ldc.i4 0x7ffffd2c
   bne.un Done

TestCase4277:
   ldc.i4 4377
   stloc.0
   ldc.i4 0x800002d4
   conv.i4
   not
   ldc.i4 0x7ffffd2b
   bne.un Done

TestCase4276:
   ldc.i4 4376
   stloc.0
   ldc.i4 0x800002d5
   conv.i4
   not
   ldc.i4 0x7ffffd2a
   bne.un Done

TestCase4275:
   ldc.i4 4375
   stloc.0
   ldc.i4 0x800002d6
   conv.i4
   not
   ldc.i4 0x7ffffd29
   bne.un Done

TestCase4274:
   ldc.i4 4374
   stloc.0
   ldc.i4 0x800002d7
   conv.i4
   not
   ldc.i4 0x7ffffd28
   bne.un Done

TestCase4273:
   ldc.i4 4373
   stloc.0
   ldc.i4 0x800002d8
   conv.i4
   not
   ldc.i4 0x7ffffd27
   beq TestCase4272
   br Done

TestCase4272:
   ldc.i4 4372
   stloc.0
   ldc.i4 0x800002d9
   conv.i4
   not
   ldc.i4 0x7ffffd26
   bne.un Done

TestCase4271:
   ldc.i4 4371
   stloc.0
   ldc.i4 0x800002da
   conv.i4
   not
   ldc.i4 0x7ffffd25
   beq TestCase4270
   br Done

TestCase4270:
   ldc.i4 4370
   stloc.0
   ldc.i4 0x800002db
   conv.i4
   not
   ldc.i4 0x7ffffd24
   bne.un Done

TestCase4269:
   ldc.i4 4369
   stloc.0
   ldc.i4 0x800002dc
   conv.i4
   not
   ldc.i4 0x7ffffd23
   bne.un Done

TestCase4268:
   ldc.i4 4368
   stloc.0
   ldc.i4 0x800002dd
   conv.i4
   not
   ldc.i4 0x7ffffd22
   beq TestCase4267
   br Done

TestCase4267:
   ldc.i4 4367
   stloc.0
   ldc.i4 0x800002de
   conv.i4
   not
   ldc.i4 0x7ffffd21
   beq TestCase4266
   br Done

TestCase4266:
   ldc.i4 4366
   stloc.0
   ldc.i4 0x800002df
   conv.i4
   not
   ldc.i4 0x7ffffd20
   beq TestCase4265
   br Done

TestCase4265:
   ldc.i4 4365
   stloc.0
   ldc.i4 0x800002e0
   conv.i4
   not
   ldc.i4 0x7ffffd1f
   bne.un Done

TestCase4264:
   ldc.i4 4364
   stloc.0
   ldc.i4 0x800002e1
   conv.i4
   not
   ldc.i4 0x7ffffd1e
   bne.un Done

TestCase4263:
   ldc.i4 4363
   stloc.0
   ldc.i4 0x800002e2
   conv.i4
   not
   ldc.i4 0x7ffffd1d
   bne.un Done

TestCase4262:
   ldc.i4 4362
   stloc.0
   ldc.i4 0x800002e3
   conv.i4
   not
   ldc.i4 0x7ffffd1c
   bne.un Done

TestCase4261:
   ldc.i4 4361
   stloc.0
   ldc.i4 0x800002e4
   conv.i4
   not
   ldc.i4 0x7ffffd1b
   bne.un Done

TestCase4260:
   ldc.i4 4360
   stloc.0
   ldc.i4 0x800002e5
   conv.i4
   not
   ldc.i4 0x7ffffd1a
   bne.un Done

TestCase4259:
   ldc.i4 4359
   stloc.0
   ldc.i4 0x800002e6
   conv.i4
   not
   ldc.i4 0x7ffffd19
   beq TestCase4258
   br Done

TestCase4258:
   ldc.i4 4358
   stloc.0
   ldc.i4 0x800002e7
   conv.i4
   not
   ldc.i4 0x7ffffd18
   beq TestCase4257
   br Done

TestCase4257:
   ldc.i4 4357
   stloc.0
   ldc.i4 0x800002e8
   conv.i4
   not
   ldc.i4 0x7ffffd17
   bne.un Done

TestCase4256:
   ldc.i4 4356
   stloc.0
   ldc.i4 0x800002e9
   conv.i4
   not
   ldc.i4 0x7ffffd16
   beq TestCase4255
   br Done

TestCase4255:
   ldc.i4 4355
   stloc.0
   ldc.i4 0x800002ea
   conv.i4
   not
   ldc.i4 0x7ffffd15
   bne.un Done

TestCase4254:
   ldc.i4 4354
   stloc.0
   ldc.i4 0x800002eb
   conv.i4
   not
   ldc.i4 0x7ffffd14
   beq TestCase4253
   br Done

TestCase4253:
   ldc.i4 4353
   stloc.0
   ldc.i4 0x800002ec
   conv.i4
   not
   ldc.i4 0x7ffffd13
   bne.un Done

TestCase4252:
   ldc.i4 4352
   stloc.0
   ldc.i4 0x800002ed
   conv.i4
   not
   ldc.i4 0x7ffffd12
   bne.un Done

TestCase4251:
   ldc.i4 4351
   stloc.0
   ldc.i4 0x800002ee
   conv.i4
   not
   ldc.i4 0x7ffffd11
   bne.un Done

TestCase4250:
   ldc.i4 4350
   stloc.0
   ldc.i4 0x800002ef
   conv.i4
   not
   ldc.i4 0x7ffffd10
   beq TestCase4249
   br Done

TestCase4249:
   ldc.i4 4349
   stloc.0
   ldc.i4 0x800002f0
   conv.i4
   not
   ldc.i4 0x7ffffd0f
   beq TestCase4248
   br Done

TestCase4248:
   ldc.i4 4348
   stloc.0
   ldc.i4 0x800002f1
   conv.i4
   not
   ldc.i4 0x7ffffd0e
   beq TestCase4247
   br Done

TestCase4247:
   ldc.i4 4347
   stloc.0
   ldc.i4 0x800002f2
   conv.i4
   not
   ldc.i4 0x7ffffd0d
   bne.un Done

TestCase4246:
   ldc.i4 4346
   stloc.0
   ldc.i4 0x800002f3
   conv.i4
   not
   ldc.i4 0x7ffffd0c
   bne.un Done

TestCase4245:
   ldc.i4 4345
   stloc.0
   ldc.i4 0x800002f4
   conv.i4
   not
   ldc.i4 0x7ffffd0b
   bne.un Done

TestCase4244:
   ldc.i4 4344
   stloc.0
   ldc.i4 0x800002f5
   conv.i4
   not
   ldc.i4 0x7ffffd0a
   bne.un Done

TestCase4243:
   ldc.i4 4343
   stloc.0
   ldc.i4 0x800002f6
   conv.i4
   not
   ldc.i4 0x7ffffd09
   beq TestCase4242
   br Done

TestCase4242:
   ldc.i4 4342
   stloc.0
   ldc.i4 0x800002f7
   conv.i4
   not
   ldc.i4 0x7ffffd08
   bne.un Done

TestCase4241:
   ldc.i4 4341
   stloc.0
   ldc.i4 0x800002f8
   conv.i4
   not
   ldc.i4 0x7ffffd07
   beq TestCase4240
   br Done

TestCase4240:
   ldc.i4 4340
   stloc.0
   ldc.i4 0x800002f9
   conv.i4
   not
   ldc.i4 0x7ffffd06
   bne.un Done

TestCase4239:
   ldc.i4 4339
   stloc.0
   ldc.i4 0x800002fa
   conv.i4
   not
   ldc.i4 0x7ffffd05
   beq TestCase4238
   br Done

TestCase4238:
   ldc.i4 4338
   stloc.0
   ldc.i4 0x800002fb
   conv.i4
   not
   ldc.i4 0x7ffffd04
   bne.un Done

TestCase4237:
   ldc.i4 4337
   stloc.0
   ldc.i4 0x800002fc
   conv.i4
   not
   ldc.i4 0x7ffffd03
   bne.un Done

TestCase4236:
   ldc.i4 4336
   stloc.0
   ldc.i4 0x800002fd
   conv.i4
   not
   ldc.i4 0x7ffffd02
   beq TestCase4235
   br Done

TestCase4235:
   ldc.i4 4335
   stloc.0
   ldc.i4 0x800002fe
   conv.i4
   not
   ldc.i4 0x7ffffd01
   beq TestCase4234
   br Done

TestCase4234:
   ldc.i4 4334
   stloc.0
   ldc.i4 0x800002ff
   conv.i4
   not
   ldc.i4 0x7ffffd00
   beq TestCase4233
   br Done

TestCase4233:
   ldc.i4 4333
   stloc.0
   ldc.i4 0x80000300
   conv.i4
   not
   ldc.i4 0x7ffffcff
   bne.un Done

TestCase4232:
   ldc.i4 4332
   stloc.0
   ldc.i4 0x80000301
   conv.i4
   not
   ldc.i4 0x7ffffcfe
   bne.un Done

TestCase4231:
   ldc.i4 4331
   stloc.0
   ldc.i4 0x80000302
   conv.i4
   not
   ldc.i4 0x7ffffcfd
   beq TestCase4230
   br Done

TestCase4230:
   ldc.i4 4330
   stloc.0
   ldc.i4 0x80000303
   conv.i4
   not
   ldc.i4 0x7ffffcfc
   bne.un Done

TestCase4229:
   ldc.i4 4329
   stloc.0
   ldc.i4 0x80000304
   conv.i4
   not
   ldc.i4 0x7ffffcfb
   beq TestCase4228
   br Done

TestCase4228:
   ldc.i4 4328
   stloc.0
   ldc.i4 0x80000305
   conv.i4
   not
   ldc.i4 0x7ffffcfa
   bne.un Done

TestCase4227:
   ldc.i4 4327
   stloc.0
   ldc.i4 0x80000306
   conv.i4
   not
   ldc.i4 0x7ffffcf9
   beq TestCase4226
   br Done

TestCase4226:
   ldc.i4 4326
   stloc.0
   ldc.i4 0x80000307
   conv.i4
   not
   ldc.i4 0x7ffffcf8
   beq TestCase4225
   br Done

TestCase4225:
   ldc.i4 4325
   stloc.0
   ldc.i4 0x80000308
   conv.i4
   not
   ldc.i4 0x7ffffcf7
   beq TestCase4224
   br Done

TestCase4224:
   ldc.i4 4324
   stloc.0
   ldc.i4 0x80000309
   conv.i4
   not
   ldc.i4 0x7ffffcf6
   bne.un Done

TestCase4223:
   ldc.i4 4323
   stloc.0
   ldc.i4 0x8000030a
   conv.i4
   not
   ldc.i4 0x7ffffcf5
   beq TestCase4222
   br Done

TestCase4222:
   ldc.i4 4322
   stloc.0
   ldc.i4 0x8000030b
   conv.i4
   not
   ldc.i4 0x7ffffcf4
   bne.un Done

TestCase4221:
   ldc.i4 4321
   stloc.0
   ldc.i4 0x8000030c
   conv.i4
   not
   ldc.i4 0x7ffffcf3
   bne.un Done

TestCase4220:
   ldc.i4 4320
   stloc.0
   ldc.i4 0x8000030d
   conv.i4
   not
   ldc.i4 0x7ffffcf2
   beq TestCase4219
   br Done

TestCase4219:
   ldc.i4 4319
   stloc.0
   ldc.i4 0x8000030e
   conv.i4
   not
   ldc.i4 0x7ffffcf1
   beq TestCase4218
   br Done

TestCase4218:
   ldc.i4 4318
   stloc.0
   ldc.i4 0x8000030f
   conv.i4
   not
   ldc.i4 0x7ffffcf0
   bne.un Done

TestCase4217:
   ldc.i4 4317
   stloc.0
   ldc.i4 0x80000310
   conv.i4
   not
   ldc.i4 0x7ffffcef
   bne.un Done

TestCase4216:
   ldc.i4 4316
   stloc.0
   ldc.i4 0x80000311
   conv.i4
   not
   ldc.i4 0x7ffffcee
   beq TestCase4215
   br Done

TestCase4215:
   ldc.i4 4315
   stloc.0
   ldc.i4 0x80000312
   conv.i4
   not
   ldc.i4 0x7ffffced
   bne.un Done

TestCase4214:
   ldc.i4 4314
   stloc.0
   ldc.i4 0x80000313
   conv.i4
   not
   ldc.i4 0x7ffffcec
   bne.un Done

TestCase4213:
   ldc.i4 4313
   stloc.0
   ldc.i4 0x80000314
   conv.i4
   not
   ldc.i4 0x7ffffceb
   bne.un Done

TestCase4212:
   ldc.i4 4312
   stloc.0
   ldc.i4 0x80000315
   conv.i4
   not
   ldc.i4 0x7ffffcea
   beq TestCase4211
   br Done

TestCase4211:
   ldc.i4 4311
   stloc.0
   ldc.i4 0x80000316
   conv.i4
   not
   ldc.i4 0x7ffffce9
   beq TestCase4210
   br Done

TestCase4210:
   ldc.i4 4310
   stloc.0
   ldc.i4 0x80000317
   conv.i4
   not
   ldc.i4 0x7ffffce8
   beq TestCase4209
   br Done

TestCase4209:
   ldc.i4 4309
   stloc.0
   ldc.i4 0x80000318
   conv.i4
   not
   ldc.i4 0x7ffffce7
   bne.un Done

TestCase4208:
   ldc.i4 4308
   stloc.0
   ldc.i4 0x80000319
   conv.i4
   not
   ldc.i4 0x7ffffce6
   bne.un Done

TestCase4207:
   ldc.i4 4307
   stloc.0
   ldc.i4 0x8000031a
   conv.i4
   not
   ldc.i4 0x7ffffce5
   bne.un Done

TestCase4206:
   ldc.i4 4306
   stloc.0
   ldc.i4 0x8000031b
   conv.i4
   not
   ldc.i4 0x7ffffce4
   beq TestCase4205
   br Done

TestCase4205:
   ldc.i4 4305
   stloc.0
   ldc.i4 0x8000031c
   conv.i4
   not
   ldc.i4 0x7ffffce3
   bne.un Done

TestCase4204:
   ldc.i4 4304
   stloc.0
   ldc.i4 0x8000031d
   conv.i4
   not
   ldc.i4 0x7ffffce2
   bne.un Done

TestCase4203:
   ldc.i4 4303
   stloc.0
   ldc.i4 0x8000031e
   conv.i4
   not
   ldc.i4 0x7ffffce1
   beq TestCase4202
   br Done

TestCase4202:
   ldc.i4 4302
   stloc.0
   ldc.i4 0x8000031f
   conv.i4
   not
   ldc.i4 0x7ffffce0
   beq TestCase4201
   br Done

TestCase4201:
   ldc.i4 4301
   stloc.0
   ldc.i4 0x80000320
   conv.i4
   not
   ldc.i4 0x7ffffcdf
   beq TestCase4200
   br Done

TestCase4200:
   ldc.i4 4300
   stloc.0
   ldc.i4 0x80000321
   conv.i4
   not
   ldc.i4 0x7ffffcde
   bne.un Done

TestCase4199:
   ldc.i4 4299
   stloc.0
   ldc.i4 0x80000322
   conv.i4
   not
   ldc.i4 0x7ffffcdd
   bne.un Done

TestCase4198:
   ldc.i4 4298
   stloc.0
   ldc.i4 0x80000323
   conv.i4
   not
   ldc.i4 0x7ffffcdc
   bne.un Done

TestCase4197:
   ldc.i4 4297
   stloc.0
   ldc.i4 0x80000324
   conv.i4
   not
   ldc.i4 0x7ffffcdb
   bne.un Done

TestCase4196:
   ldc.i4 4296
   stloc.0
   ldc.i4 0x80000325
   conv.i4
   not
   ldc.i4 0x7ffffcda
   bne.un Done

TestCase4195:
   ldc.i4 4295
   stloc.0
   ldc.i4 0x80000326
   conv.i4
   not
   ldc.i4 0x7ffffcd9
   bne.un Done

TestCase4194:
   ldc.i4 4294
   stloc.0
   ldc.i4 0x80000327
   conv.i4
   not
   ldc.i4 0x7ffffcd8
   beq TestCase4193
   br Done

TestCase4193:
   ldc.i4 4293
   stloc.0
   ldc.i4 0x80000328
   conv.i4
   not
   ldc.i4 0x7ffffcd7
   bne.un Done

TestCase4192:
   ldc.i4 4292
   stloc.0
   ldc.i4 0x80000329
   conv.i4
   not
   ldc.i4 0x7ffffcd6
   bne.un Done

TestCase4191:
   ldc.i4 4291
   stloc.0
   ldc.i4 0x8000032a
   conv.i4
   not
   ldc.i4 0x7ffffcd5
   bne.un Done

TestCase4190:
   ldc.i4 4290
   stloc.0
   ldc.i4 0x8000032b
   conv.i4
   not
   ldc.i4 0x7ffffcd4
   bne.un Done

TestCase4189:
   ldc.i4 4289
   stloc.0
   ldc.i4 0x8000032c
   conv.i4
   not
   ldc.i4 0x7ffffcd3
   beq TestCase4188
   br Done

TestCase4188:
   ldc.i4 4288
   stloc.0
   ldc.i4 0x8000032d
   conv.i4
   not
   ldc.i4 0x7ffffcd2
   beq TestCase4187
   br Done

TestCase4187:
   ldc.i4 4287
   stloc.0
   ldc.i4 0x8000032e
   conv.i4
   not
   ldc.i4 0x7ffffcd1
   beq TestCase4186
   br Done

TestCase4186:
   ldc.i4 4286
   stloc.0
   ldc.i4 0x8000032f
   conv.i4
   not
   ldc.i4 0x7ffffcd0
   beq TestCase4185
   br Done

TestCase4185:
   ldc.i4 4285
   stloc.0
   ldc.i4 0x80000330
   conv.i4
   not
   ldc.i4 0x7ffffccf
   beq TestCase4184
   br Done

TestCase4184:
   ldc.i4 4284
   stloc.0
   ldc.i4 0x80000331
   conv.i4
   not
   ldc.i4 0x7ffffcce
   beq TestCase4183
   br Done

TestCase4183:
   ldc.i4 4283
   stloc.0
   ldc.i4 0x80000332
   conv.i4
   not
   ldc.i4 0x7ffffccd
   bne.un Done

TestCase4182:
   ldc.i4 4282
   stloc.0
   ldc.i4 0x80000333
   conv.i4
   not
   ldc.i4 0x7ffffccc
   beq TestCase4181
   br Done

TestCase4181:
   ldc.i4 4281
   stloc.0
   ldc.i4 0x80000334
   conv.i4
   not
   ldc.i4 0x7ffffccb
   bne.un Done

TestCase4180:
   ldc.i4 4280
   stloc.0
   ldc.i4 0x80000335
   conv.i4
   not
   ldc.i4 0x7ffffcca
   beq TestCase4179
   br Done

TestCase4179:
   ldc.i4 4279
   stloc.0
   ldc.i4 0x80000336
   conv.i4
   not
   ldc.i4 0x7ffffcc9
   beq TestCase4178
   br Done

TestCase4178:
   ldc.i4 4278
   stloc.0
   ldc.i4 0x80000337
   conv.i4
   not
   ldc.i4 0x7ffffcc8
   bne.un Done

TestCase4177:
   ldc.i4 4277
   stloc.0
   ldc.i4 0x80000338
   conv.i4
   not
   ldc.i4 0x7ffffcc7
   beq TestCase4176
   br Done

TestCase4176:
   ldc.i4 4276
   stloc.0
   ldc.i4 0x80000339
   conv.i4
   not
   ldc.i4 0x7ffffcc6
   beq TestCase4175
   br Done

TestCase4175:
   ldc.i4 4275
   stloc.0
   ldc.i4 0x8000033a
   conv.i4
   not
   ldc.i4 0x7ffffcc5
   beq TestCase4174
   br Done

TestCase4174:
   ldc.i4 4274
   stloc.0
   ldc.i4 0x8000033b
   conv.i4
   not
   ldc.i4 0x7ffffcc4
   beq TestCase4173
   br Done

TestCase4173:
   ldc.i4 4273
   stloc.0
   ldc.i4 0x8000033c
   conv.i4
   not
   ldc.i4 0x7ffffcc3
   bne.un Done

TestCase4172:
   ldc.i4 4272
   stloc.0
   ldc.i4 0x8000033d
   conv.i4
   not
   ldc.i4 0x7ffffcc2
   beq TestCase4171
   br Done

TestCase4171:
   ldc.i4 4271
   stloc.0
   ldc.i4 0x8000033e
   conv.i4
   not
   ldc.i4 0x7ffffcc1
   beq TestCase4170
   br Done

TestCase4170:
   ldc.i4 4270
   stloc.0
   ldc.i4 0x8000033f
   conv.i4
   not
   ldc.i4 0x7ffffcc0
   beq TestCase4169
   br Done

TestCase4169:
   ldc.i4 4269
   stloc.0
   ldc.i4 0x80000340
   conv.i4
   not
   ldc.i4 0x7ffffcbf
   beq TestCase4168
   br Done

TestCase4168:
   ldc.i4 4268
   stloc.0
   ldc.i4 0x80000341
   conv.i4
   not
   ldc.i4 0x7ffffcbe
   beq TestCase4167
   br Done

TestCase4167:
   ldc.i4 4267
   stloc.0
   ldc.i4 0x80000342
   conv.i4
   not
   ldc.i4 0x7ffffcbd
   bne.un Done

TestCase4166:
   ldc.i4 4266
   stloc.0
   ldc.i4 0x80000343
   conv.i4
   not
   ldc.i4 0x7ffffcbc
   bne.un Done

TestCase4165:
   ldc.i4 4265
   stloc.0
   ldc.i4 0x80000344
   conv.i4
   not
   ldc.i4 0x7ffffcbb
   bne.un Done

TestCase4164:
   ldc.i4 4264
   stloc.0
   ldc.i4 0x80000345
   conv.i4
   not
   ldc.i4 0x7ffffcba
   beq TestCase4163
   br Done

TestCase4163:
   ldc.i4 4263
   stloc.0
   ldc.i4 0x80000346
   conv.i4
   not
   ldc.i4 0x7ffffcb9
   bne.un Done

TestCase4162:
   ldc.i4 4262
   stloc.0
   ldc.i4 0x80000347
   conv.i4
   not
   ldc.i4 0x7ffffcb8
   bne.un Done

TestCase4161:
   ldc.i4 4261
   stloc.0
   ldc.i4 0x80000348
   conv.i4
   not
   ldc.i4 0x7ffffcb7
   bne.un Done

TestCase4160:
   ldc.i4 4260
   stloc.0
   ldc.i4 0x80000349
   conv.i4
   not
   ldc.i4 0x7ffffcb6
   bne.un Done

TestCase4159:
   ldc.i4 4259
   stloc.0
   ldc.i4 0x8000034a
   conv.i4
   not
   ldc.i4 0x7ffffcb5
   bne.un Done

TestCase4158:
   ldc.i4 4258
   stloc.0
   ldc.i4 0x8000034b
   conv.i4
   not
   ldc.i4 0x7ffffcb4
   bne.un Done

TestCase4157:
   ldc.i4 4257
   stloc.0
   ldc.i4 0x8000034c
   conv.i4
   not
   ldc.i4 0x7ffffcb3
   bne.un Done

TestCase4156:
   ldc.i4 4256
   stloc.0
   ldc.i4 0x8000034d
   conv.i4
   not
   ldc.i4 0x7ffffcb2
   bne.un Done

TestCase4155:
   ldc.i4 4255
   stloc.0
   ldc.i4 0x8000034e
   conv.i4
   not
   ldc.i4 0x7ffffcb1
   bne.un Done

TestCase4154:
   ldc.i4 4254
   stloc.0
   ldc.i4 0x8000034f
   conv.i4
   not
   ldc.i4 0x7ffffcb0
   bne.un Done

TestCase4153:
   ldc.i4 4253
   stloc.0
   ldc.i4 0x80000350
   conv.i4
   not
   ldc.i4 0x7ffffcaf
   beq TestCase4152
   br Done

TestCase4152:
   ldc.i4 4252
   stloc.0
   ldc.i4 0x80000351
   conv.i4
   not
   ldc.i4 0x7ffffcae
   bne.un Done

TestCase4151:
   ldc.i4 4251
   stloc.0
   ldc.i4 0x80000352
   conv.i4
   not
   ldc.i4 0x7ffffcad
   beq TestCase4150
   br Done

TestCase4150:
   ldc.i4 4250
   stloc.0
   ldc.i4 0x80000353
   conv.i4
   not
   ldc.i4 0x7ffffcac
   beq TestCase4149
   br Done

TestCase4149:
   ldc.i4 4249
   stloc.0
   ldc.i4 0x80000354
   conv.i4
   not
   ldc.i4 0x7ffffcab
   bne.un Done

TestCase4148:
   ldc.i4 4248
   stloc.0
   ldc.i4 0x80000355
   conv.i4
   not
   ldc.i4 0x7ffffcaa
   beq TestCase4147
   br Done

TestCase4147:
   ldc.i4 4247
   stloc.0
   ldc.i4 0x80000356
   conv.i4
   not
   ldc.i4 0x7ffffca9
   beq TestCase4146
   br Done

TestCase4146:
   ldc.i4 4246
   stloc.0
   ldc.i4 0x80000357
   conv.i4
   not
   ldc.i4 0x7ffffca8
   beq TestCase4145
   br Done

TestCase4145:
   ldc.i4 4245
   stloc.0
   ldc.i4 0x80000358
   conv.i4
   not
   ldc.i4 0x7ffffca7
   beq TestCase4144
   br Done

TestCase4144:
   ldc.i4 4244
   stloc.0
   ldc.i4 0x80000359
   conv.i4
   not
   ldc.i4 0x7ffffca6
   bne.un Done

TestCase4143:
   ldc.i4 4243
   stloc.0
   ldc.i4 0x8000035a
   conv.i4
   not
   ldc.i4 0x7ffffca5
   beq TestCase4142
   br Done

TestCase4142:
   ldc.i4 4242
   stloc.0
   ldc.i4 0x8000035b
   conv.i4
   not
   ldc.i4 0x7ffffca4
   bne.un Done

TestCase4141:
   ldc.i4 4241
   stloc.0
   ldc.i4 0x8000035c
   conv.i4
   not
   ldc.i4 0x7ffffca3
   beq TestCase4140
   br Done

TestCase4140:
   ldc.i4 4240
   stloc.0
   ldc.i4 0x8000035d
   conv.i4
   not
   ldc.i4 0x7ffffca2
   bne.un Done

TestCase4139:
   ldc.i4 4239
   stloc.0
   ldc.i4 0x8000035e
   conv.i4
   not
   ldc.i4 0x7ffffca1
   bne.un Done

TestCase4138:
   ldc.i4 4238
   stloc.0
   ldc.i4 0x8000035f
   conv.i4
   not
   ldc.i4 0x7ffffca0
   beq TestCase4137
   br Done

TestCase4137:
   ldc.i4 4237
   stloc.0
   ldc.i4 0x80000360
   conv.i4
   not
   ldc.i4 0x7ffffc9f
   beq TestCase4136
   br Done

TestCase4136:
   ldc.i4 4236
   stloc.0
   ldc.i4 0x80000361
   conv.i4
   not
   ldc.i4 0x7ffffc9e
   bne.un Done

TestCase4135:
   ldc.i4 4235
   stloc.0
   ldc.i4 0x80000362
   conv.i4
   not
   ldc.i4 0x7ffffc9d
   beq TestCase4134
   br Done

TestCase4134:
   ldc.i4 4234
   stloc.0
   ldc.i4 0x80000363
   conv.i4
   not
   ldc.i4 0x7ffffc9c
   beq TestCase4133
   br Done

TestCase4133:
   ldc.i4 4233
   stloc.0
   ldc.i4 0x80000364
   conv.i4
   not
   ldc.i4 0x7ffffc9b
   beq TestCase4132
   br Done

TestCase4132:
   ldc.i4 4232
   stloc.0
   ldc.i4 0x80000365
   conv.i4
   not
   ldc.i4 0x7ffffc9a
   beq TestCase4131
   br Done

TestCase4131:
   ldc.i4 4231
   stloc.0
   ldc.i4 0x80000366
   conv.i4
   not
   ldc.i4 0x7ffffc99
   bne.un Done

TestCase4130:
   ldc.i4 4230
   stloc.0
   ldc.i4 0x80000367
   conv.i4
   not
   ldc.i4 0x7ffffc98
   bne.un Done

TestCase4129:
   ldc.i4 4229
   stloc.0
   ldc.i4 0x80000368
   conv.i4
   not
   ldc.i4 0x7ffffc97
   beq TestCase4128
   br Done

TestCase4128:
   ldc.i4 4228
   stloc.0
   ldc.i4 0x80000369
   conv.i4
   not
   ldc.i4 0x7ffffc96
   beq TestCase4127
   br Done

TestCase4127:
   ldc.i4 4227
   stloc.0
   ldc.i4 0x8000036a
   conv.i4
   not
   ldc.i4 0x7ffffc95
   beq TestCase4126
   br Done

TestCase4126:
   ldc.i4 4226
   stloc.0
   ldc.i4 0x8000036b
   conv.i4
   not
   ldc.i4 0x7ffffc94
   beq TestCase4125
   br Done

TestCase4125:
   ldc.i4 4225
   stloc.0
   ldc.i4 0x8000036c
   conv.i4
   not
   ldc.i4 0x7ffffc93
   bne.un Done

TestCase4124:
   ldc.i4 4224
   stloc.0
   ldc.i4 0x8000036d
   conv.i4
   not
   ldc.i4 0x7ffffc92
   bne.un Done

TestCase4123:
   ldc.i4 4223
   stloc.0
   ldc.i4 0x8000036e
   conv.i4
   not
   ldc.i4 0x7ffffc91
   bne.un Done

TestCase4122:
   ldc.i4 4222
   stloc.0
   ldc.i4 0x8000036f
   conv.i4
   not
   ldc.i4 0x7ffffc90
   beq TestCase4121
   br Done

TestCase4121:
   ldc.i4 4221
   stloc.0
   ldc.i4 0x80000370
   conv.i4
   not
   ldc.i4 0x7ffffc8f
   bne.un Done

TestCase4120:
   ldc.i4 4220
   stloc.0
   ldc.i4 0x80000371
   conv.i4
   not
   ldc.i4 0x7ffffc8e
   bne.un Done

TestCase4119:
   ldc.i4 4219
   stloc.0
   ldc.i4 0x80000372
   conv.i4
   not
   ldc.i4 0x7ffffc8d
   bne.un Done

TestCase4118:
   ldc.i4 4218
   stloc.0
   ldc.i4 0x80000373
   conv.i4
   not
   ldc.i4 0x7ffffc8c
   bne.un Done

TestCase4117:
   ldc.i4 4217
   stloc.0
   ldc.i4 0x80000374
   conv.i4
   not
   ldc.i4 0x7ffffc8b
   beq TestCase4116
   br Done

TestCase4116:
   ldc.i4 4216
   stloc.0
   ldc.i4 0x80000375
   conv.i4
   not
   ldc.i4 0x7ffffc8a
   beq TestCase4115
   br Done

TestCase4115:
   ldc.i4 4215
   stloc.0
   ldc.i4 0x80000376
   conv.i4
   not
   ldc.i4 0x7ffffc89
   bne.un Done

TestCase4114:
   ldc.i4 4214
   stloc.0
   ldc.i4 0x80000377
   conv.i4
   not
   ldc.i4 0x7ffffc88
   beq TestCase4113
   br Done

TestCase4113:
   ldc.i4 4213
   stloc.0
   ldc.i4 0x80000378
   conv.i4
   not
   ldc.i4 0x7ffffc87
   bne.un Done

TestCase4112:
   ldc.i4 4212
   stloc.0
   ldc.i4 0x80000379
   conv.i4
   not
   ldc.i4 0x7ffffc86
   beq TestCase4111
   br Done

TestCase4111:
   ldc.i4 4211
   stloc.0
   ldc.i4 0x8000037a
   conv.i4
   not
   ldc.i4 0x7ffffc85
   beq TestCase4110
   br Done

TestCase4110:
   ldc.i4 4210
   stloc.0
   ldc.i4 0x8000037b
   conv.i4
   not
   ldc.i4 0x7ffffc84
   bne.un Done

TestCase4109:
   ldc.i4 4209
   stloc.0
   ldc.i4 0x8000037c
   conv.i4
   not
   ldc.i4 0x7ffffc83
   bne.un Done

TestCase4108:
   ldc.i4 4208
   stloc.0
   ldc.i4 0x8000037d
   conv.i4
   not
   ldc.i4 0x7ffffc82
   bne.un Done

TestCase4107:
   ldc.i4 4207
   stloc.0
   ldc.i4 0x8000037e
   conv.i4
   not
   ldc.i4 0x7ffffc81
   bne.un Done

TestCase4106:
   ldc.i4 4206
   stloc.0
   ldc.i4 0x8000037f
   conv.i4
   not
   ldc.i4 0x7ffffc80
   bne.un Done

TestCase4105:
   ldc.i4 4205
   stloc.0
   ldc.i4 0x80000380
   conv.i4
   not
   ldc.i4 0x7ffffc7f
   beq TestCase4104
   br Done

TestCase4104:
   ldc.i4 4204
   stloc.0
   ldc.i4 0x80000381
   conv.i4
   not
   ldc.i4 0x7ffffc7e
   beq TestCase4103
   br Done

TestCase4103:
   ldc.i4 4203
   stloc.0
   ldc.i4 0x80000382
   conv.i4
   not
   ldc.i4 0x7ffffc7d
   bne.un Done

TestCase4102:
   ldc.i4 4202
   stloc.0
   ldc.i4 0x80000383
   conv.i4
   not
   ldc.i4 0x7ffffc7c
   bne.un Done

TestCase4101:
   ldc.i4 4201
   stloc.0
   ldc.i4 0x80000384
   conv.i4
   not
   ldc.i4 0x7ffffc7b
   beq TestCase4100
   br Done

TestCase4100:
   ldc.i4 4200
   stloc.0
   ldc.i4 0x80000385
   conv.i4
   not
   ldc.i4 0x7ffffc7a
   bne.un Done

TestCase4099:
   ldc.i4 4199
   stloc.0
   ldc.i4 0x80000386
   conv.i4
   not
   ldc.i4 0x7ffffc79
   bne.un Done

TestCase4098:
   ldc.i4 4198
   stloc.0
   ldc.i4 0x80000387
   conv.i4
   not
   ldc.i4 0x7ffffc78
   bne.un Done

TestCase4097:
   ldc.i4 4197
   stloc.0
   ldc.i4 0x80000388
   conv.i4
   not
   ldc.i4 0x7ffffc77
   beq TestCase4096
   br Done

TestCase4096:
   ldc.i4 4196
   stloc.0
   ldc.i4 0x80000389
   conv.i4
   not
   ldc.i4 0x7ffffc76
   bne.un Done

TestCase4095:
   ldc.i4 4195
   stloc.0
   ldc.i4 0x8000038a
   conv.i4
   not
   ldc.i4 0x7ffffc75
   bne.un Done

TestCase4094:
   ldc.i4 4194
   stloc.0
   ldc.i4 0x8000038b
   conv.i4
   not
   ldc.i4 0x7ffffc74
   beq TestCase4093
   br Done

TestCase4093:
   ldc.i4 4193
   stloc.0
   ldc.i4 0x8000038c
   conv.i4
   not
   ldc.i4 0x7ffffc73
   bne.un Done

TestCase4092:
   ldc.i4 4192
   stloc.0
   ldc.i4 0x8000038d
   conv.i4
   not
   ldc.i4 0x7ffffc72
   bne.un Done

TestCase4091:
   ldc.i4 4191
   stloc.0
   ldc.i4 0x8000038e
   conv.i4
   not
   ldc.i4 0x7ffffc71
   bne.un Done

TestCase4090:
   ldc.i4 4190
   stloc.0
   ldc.i4 0x8000038f
   conv.i4
   not
   ldc.i4 0x7ffffc70
   bne.un Done

TestCase4089:
   ldc.i4 4189
   stloc.0
   ldc.i4 0x80000390
   conv.i4
   not
   ldc.i4 0x7ffffc6f
   beq TestCase4088
   br Done

TestCase4088:
   ldc.i4 4188
   stloc.0
   ldc.i4 0x80000391
   conv.i4
   not
   ldc.i4 0x7ffffc6e
   beq TestCase4087
   br Done

TestCase4087:
   ldc.i4 4187
   stloc.0
   ldc.i4 0x80000392
   conv.i4
   not
   ldc.i4 0x7ffffc6d
   bne.un Done

TestCase4086:
   ldc.i4 4186
   stloc.0
   ldc.i4 0x80000393
   conv.i4
   not
   ldc.i4 0x7ffffc6c
   beq TestCase4085
   br Done

TestCase4085:
   ldc.i4 4185
   stloc.0
   ldc.i4 0x80000394
   conv.i4
   not
   ldc.i4 0x7ffffc6b
   beq TestCase4084
   br Done

TestCase4084:
   ldc.i4 4184
   stloc.0
   ldc.i4 0x80000395
   conv.i4
   not
   ldc.i4 0x7ffffc6a
   bne.un Done

TestCase4083:
   ldc.i4 4183
   stloc.0
   ldc.i4 0x80000396
   conv.i4
   not
   ldc.i4 0x7ffffc69
   bne.un Done

TestCase4082:
   ldc.i4 4182
   stloc.0
   ldc.i4 0x80000397
   conv.i4
   not
   ldc.i4 0x7ffffc68
   bne.un Done

TestCase4081:
   ldc.i4 4181
   stloc.0
   ldc.i4 0x80000398
   conv.i4
   not
   ldc.i4 0x7ffffc67
   bne.un Done

TestCase4080:
   ldc.i4 4180
   stloc.0
   ldc.i4 0x80000399
   conv.i4
   not
   ldc.i4 0x7ffffc66
   beq TestCase4079
   br Done

TestCase4079:
   ldc.i4 4179
   stloc.0
   ldc.i4 0x8000039a
   conv.i4
   not
   ldc.i4 0x7ffffc65
   beq TestCase4078
   br Done

TestCase4078:
   ldc.i4 4178
   stloc.0
   ldc.i4 0x8000039b
   conv.i4
   not
   ldc.i4 0x7ffffc64
   bne.un Done

TestCase4077:
   ldc.i4 4177
   stloc.0
   ldc.i4 0x8000039c
   conv.i4
   not
   ldc.i4 0x7ffffc63
   beq TestCase4076
   br Done

TestCase4076:
   ldc.i4 4176
   stloc.0
   ldc.i4 0x8000039d
   conv.i4
   not
   ldc.i4 0x7ffffc62
   bne.un Done

TestCase4075:
   ldc.i4 4175
   stloc.0
   ldc.i4 0x8000039e
   conv.i4
   not
   ldc.i4 0x7ffffc61
   bne.un Done

TestCase4074:
   ldc.i4 4174
   stloc.0
   ldc.i4 0x8000039f
   conv.i4
   not
   ldc.i4 0x7ffffc60
   beq TestCase4073
   br Done

TestCase4073:
   ldc.i4 4173
   stloc.0
   ldc.i4 0x800003a0
   conv.i4
   not
   ldc.i4 0x7ffffc5f
   beq TestCase4072
   br Done

TestCase4072:
   ldc.i4 4172
   stloc.0
   ldc.i4 0x800003a1
   conv.i4
   not
   ldc.i4 0x7ffffc5e
   beq TestCase4071
   br Done

TestCase4071:
   ldc.i4 4171
   stloc.0
   ldc.i4 0x800003a2
   conv.i4
   not
   ldc.i4 0x7ffffc5d
   beq TestCase4070
   br Done

TestCase4070:
   ldc.i4 4170
   stloc.0
   ldc.i4 0x800003a3
   conv.i4
   not
   ldc.i4 0x7ffffc5c
   beq TestCase4069
   br Done

TestCase4069:
   ldc.i4 4169
   stloc.0
   ldc.i4 0x800003a4
   conv.i4
   not
   ldc.i4 0x7ffffc5b
   beq TestCase4068
   br Done

TestCase4068:
   ldc.i4 4168
   stloc.0
   ldc.i4 0x800003a5
   conv.i4
   not
   ldc.i4 0x7ffffc5a
   beq TestCase4067
   br Done

TestCase4067:
   ldc.i4 4167
   stloc.0
   ldc.i4 0x800003a6
   conv.i4
   not
   ldc.i4 0x7ffffc59
   bne.un Done

TestCase4066:
   ldc.i4 4166
   stloc.0
   ldc.i4 0x800003a7
   conv.i4
   not
   ldc.i4 0x7ffffc58
   bne.un Done

TestCase4065:
   ldc.i4 4165
   stloc.0
   ldc.i4 0x800003a8
   conv.i4
   not
   ldc.i4 0x7ffffc57
   beq TestCase4064
   br Done

TestCase4064:
   ldc.i4 4164
   stloc.0
   ldc.i4 0x800003a9
   conv.i4
   not
   ldc.i4 0x7ffffc56
   bne.un Done

TestCase4063:
   ldc.i4 4163
   stloc.0
   ldc.i4 0x800003aa
   conv.i4
   not
   ldc.i4 0x7ffffc55
   beq TestCase4062
   br Done

TestCase4062:
   ldc.i4 4162
   stloc.0
   ldc.i4 0x800003ab
   conv.i4
   not
   ldc.i4 0x7ffffc54
   beq TestCase4061
   br Done

TestCase4061:
   ldc.i4 4161
   stloc.0
   ldc.i4 0x800003ac
   conv.i4
   not
   ldc.i4 0x7ffffc53
   bne.un Done

TestCase4060:
   ldc.i4 4160
   stloc.0
   ldc.i4 0x800003ad
   conv.i4
   not
   ldc.i4 0x7ffffc52
   beq TestCase4059
   br Done

TestCase4059:
   ldc.i4 4159
   stloc.0
   ldc.i4 0x800003ae
   conv.i4
   not
   ldc.i4 0x7ffffc51
   bne.un Done

TestCase4058:
   ldc.i4 4158
   stloc.0
   ldc.i4 0x800003af
   conv.i4
   not
   ldc.i4 0x7ffffc50
   bne.un Done

TestCase4057:
   ldc.i4 4157
   stloc.0
   ldc.i4 0x800003b0
   conv.i4
   not
   ldc.i4 0x7ffffc4f
   beq TestCase4056
   br Done

TestCase4056:
   ldc.i4 4156
   stloc.0
   ldc.i4 0x800003b1
   conv.i4
   not
   ldc.i4 0x7ffffc4e
   bne.un Done

TestCase4055:
   ldc.i4 4155
   stloc.0
   ldc.i4 0x800003b2
   conv.i4
   not
   ldc.i4 0x7ffffc4d
   beq TestCase4054
   br Done

TestCase4054:
   ldc.i4 4154
   stloc.0
   ldc.i4 0x800003b3
   conv.i4
   not
   ldc.i4 0x7ffffc4c
   beq TestCase4053
   br Done

TestCase4053:
   ldc.i4 4153
   stloc.0
   ldc.i4 0x800003b4
   conv.i4
   not
   ldc.i4 0x7ffffc4b
   beq TestCase4052
   br Done

TestCase4052:
   ldc.i4 4152
   stloc.0
   ldc.i4 0x800003b5
   conv.i4
   not
   ldc.i4 0x7ffffc4a
   beq TestCase4051
   br Done

TestCase4051:
   ldc.i4 4151
   stloc.0
   ldc.i4 0x800003b6
   conv.i4
   not
   ldc.i4 0x7ffffc49
   beq TestCase4050
   br Done

TestCase4050:
   ldc.i4 4150
   stloc.0
   ldc.i4 0x800003b7
   conv.i4
   not
   ldc.i4 0x7ffffc48
   beq TestCase4049
   br Done

TestCase4049:
   ldc.i4 4149
   stloc.0
   ldc.i4 0x800003b8
   conv.i4
   not
   ldc.i4 0x7ffffc47
   bne.un Done

TestCase4048:
   ldc.i4 4148
   stloc.0
   ldc.i4 0x800003b9
   conv.i4
   not
   ldc.i4 0x7ffffc46
   beq TestCase4047
   br Done

TestCase4047:
   ldc.i4 4147
   stloc.0
   ldc.i4 0x800003ba
   conv.i4
   not
   ldc.i4 0x7ffffc45
   beq TestCase4046
   br Done

TestCase4046:
   ldc.i4 4146
   stloc.0
   ldc.i4 0x800003bb
   conv.i4
   not
   ldc.i4 0x7ffffc44
   beq TestCase4045
   br Done

TestCase4045:
   ldc.i4 4145
   stloc.0
   ldc.i4 0x800003bc
   conv.i4
   not
   ldc.i4 0x7ffffc43
   beq TestCase4044
   br Done

TestCase4044:
   ldc.i4 4144
   stloc.0
   ldc.i4 0x800003bd
   conv.i4
   not
   ldc.i4 0x7ffffc42
   bne.un Done

TestCase4043:
   ldc.i4 4143
   stloc.0
   ldc.i4 0x800003be
   conv.i4
   not
   ldc.i4 0x7ffffc41
   bne.un Done

TestCase4042:
   ldc.i4 4142
   stloc.0
   ldc.i4 0x800003bf
   conv.i4
   not
   ldc.i4 0x7ffffc40
   beq TestCase4041
   br Done

TestCase4041:
   ldc.i4 4141
   stloc.0
   ldc.i4 0x800003c0
   conv.i4
   not
   ldc.i4 0x7ffffc3f
   bne.un Done

TestCase4040:
   ldc.i4 4140
   stloc.0
   ldc.i4 0x800003c1
   conv.i4
   not
   ldc.i4 0x7ffffc3e
   beq TestCase4039
   br Done

TestCase4039:
   ldc.i4 4139
   stloc.0
   ldc.i4 0x800003c2
   conv.i4
   not
   ldc.i4 0x7ffffc3d
   beq TestCase4038
   br Done

TestCase4038:
   ldc.i4 4138
   stloc.0
   ldc.i4 0x800003c3
   conv.i4
   not
   ldc.i4 0x7ffffc3c
   bne.un Done

TestCase4037:
   ldc.i4 4137
   stloc.0
   ldc.i4 0x800003c4
   conv.i4
   not
   ldc.i4 0x7ffffc3b
   bne.un Done

TestCase4036:
   ldc.i4 4136
   stloc.0
   ldc.i4 0x800003c5
   conv.i4
   not
   ldc.i4 0x7ffffc3a
   beq TestCase4035
   br Done

TestCase4035:
   ldc.i4 4135
   stloc.0
   ldc.i4 0x800003c6
   conv.i4
   not
   ldc.i4 0x7ffffc39
   beq TestCase4034
   br Done

TestCase4034:
   ldc.i4 4134
   stloc.0
   ldc.i4 0x800003c7
   conv.i4
   not
   ldc.i4 0x7ffffc38
   beq TestCase4033
   br Done

TestCase4033:
   ldc.i4 4133
   stloc.0
   ldc.i4 0x800003c8
   conv.i4
   not
   ldc.i4 0x7ffffc37
   bne.un Done

TestCase4032:
   ldc.i4 4132
   stloc.0
   ldc.i4 0x800003c9
   conv.i4
   not
   ldc.i4 0x7ffffc36
   bne.un Done

TestCase4031:
   ldc.i4 4131
   stloc.0
   ldc.i4 0x800003ca
   conv.i4
   not
   ldc.i4 0x7ffffc35
   bne.un Done

TestCase4030:
   ldc.i4 4130
   stloc.0
   ldc.i4 0x800003cb
   conv.i4
   not
   ldc.i4 0x7ffffc34
   bne.un Done

TestCase4029:
   ldc.i4 4129
   stloc.0
   ldc.i4 0x800003cc
   conv.i4
   not
   ldc.i4 0x7ffffc33
   bne.un Done

TestCase4028:
   ldc.i4 4128
   stloc.0
   ldc.i4 0x800003cd
   conv.i4
   not
   ldc.i4 0x7ffffc32
   beq TestCase4027
   br Done

TestCase4027:
   ldc.i4 4127
   stloc.0
   ldc.i4 0x800003ce
   conv.i4
   not
   ldc.i4 0x7ffffc31
   bne.un Done

TestCase4026:
   ldc.i4 4126
   stloc.0
   ldc.i4 0x800003cf
   conv.i4
   not
   ldc.i4 0x7ffffc30
   bne.un Done

TestCase4025:
   ldc.i4 4125
   stloc.0
   ldc.i4 0x800003d0
   conv.i4
   not
   ldc.i4 0x7ffffc2f
   bne.un Done

TestCase4024:
   ldc.i4 4124
   stloc.0
   ldc.i4 0x800003d1
   conv.i4
   not
   ldc.i4 0x7ffffc2e
   beq TestCase4023
   br Done

TestCase4023:
   ldc.i4 4123
   stloc.0
   ldc.i4 0x800003d2
   conv.i4
   not
   ldc.i4 0x7ffffc2d
   beq TestCase4022
   br Done

TestCase4022:
   ldc.i4 4122
   stloc.0
   ldc.i4 0x800003d3
   conv.i4
   not
   ldc.i4 0x7ffffc2c
   beq TestCase4021
   br Done

TestCase4021:
   ldc.i4 4121
   stloc.0
   ldc.i4 0x800003d4
   conv.i4
   not
   ldc.i4 0x7ffffc2b
   beq TestCase4020
   br Done

TestCase4020:
   ldc.i4 4120
   stloc.0
   ldc.i4 0x800003d5
   conv.i4
   not
   ldc.i4 0x7ffffc2a
   bne.un Done

TestCase4019:
   ldc.i4 4119
   stloc.0
   ldc.i4 0x800003d6
   conv.i4
   not
   ldc.i4 0x7ffffc29
   bne.un Done

TestCase4018:
   ldc.i4 4118
   stloc.0
   ldc.i4 0x800003d7
   conv.i4
   not
   ldc.i4 0x7ffffc28
   beq TestCase4017
   br Done

TestCase4017:
   ldc.i4 4117
   stloc.0
   ldc.i4 0x800003d8
   conv.i4
   not
   ldc.i4 0x7ffffc27
   bne.un Done

TestCase4016:
   ldc.i4 4116
   stloc.0
   ldc.i4 0x800003d9
   conv.i4
   not
   ldc.i4 0x7ffffc26
   beq TestCase4015
   br Done

TestCase4015:
   ldc.i4 4115
   stloc.0
   ldc.i4 0x800003da
   conv.i4
   not
   ldc.i4 0x7ffffc25
   bne.un Done

TestCase4014:
   ldc.i4 4114
   stloc.0
   ldc.i4 0x800003db
   conv.i4
   not
   ldc.i4 0x7ffffc24
   beq TestCase4013
   br Done

TestCase4013:
   ldc.i4 4113
   stloc.0
   ldc.i4 0x800003dc
   conv.i4
   not
   ldc.i4 0x7ffffc23
   bne.un Done

TestCase4012:
   ldc.i4 4112
   stloc.0
   ldc.i4 0x800003dd
   conv.i4
   not
   ldc.i4 0x7ffffc22
   bne.un Done

TestCase4011:
   ldc.i4 4111
   stloc.0
   ldc.i4 0x800003de
   conv.i4
   not
   ldc.i4 0x7ffffc21
   beq TestCase4010
   br Done

TestCase4010:
   ldc.i4 4110
   stloc.0
   ldc.i4 0x800003df
   conv.i4
   not
   ldc.i4 0x7ffffc20
   bne.un Done

TestCase4009:
   ldc.i4 4109
   stloc.0
   ldc.i4 0x800003e0
   conv.i4
   not
   ldc.i4 0x7ffffc1f
   bne.un Done

TestCase4008:
   ldc.i4 4108
   stloc.0
   ldc.i4 0x800003e1
   conv.i4
   not
   ldc.i4 0x7ffffc1e
   bne.un Done

TestCase4007:
   ldc.i4 4107
   stloc.0
   ldc.i4 0x800003e2
   conv.i4
   not
   ldc.i4 0x7ffffc1d
   bne.un Done

TestCase4006:
   ldc.i4 4106
   stloc.0
   ldc.i4 0x800003e3
   conv.i4
   not
   ldc.i4 0x7ffffc1c
   bne.un Done

TestCase4005:
   ldc.i4 4105
   stloc.0
   ldc.i4 0x800003e4
   conv.i4
   not
   ldc.i4 0x7ffffc1b
   bne.un Done

TestCase4004:
   ldc.i4 4104
   stloc.0
   ldc.i4 0x800003e5
   conv.i4
   not
   ldc.i4 0x7ffffc1a
   bne.un Done

TestCase4003:
   ldc.i4 4103
   stloc.0
   ldc.i4 0x800003e6
   conv.i4
   not
   ldc.i4 0x7ffffc19
   bne.un Done

TestCase4002:
   ldc.i4 4102
   stloc.0
   ldc.i4 0x800003e7
   conv.i4
   not
   ldc.i4 0x7ffffc18
   beq TestCase4001
   br Done

TestCase4001:
   ldc.i4 4101
   stloc.0
   ldc.i4 0x800003e8
   conv.i4
   not
   ldc.i4 0x7ffffc17
   beq TestCase4000
   br Done

TestCase4000:
   ldc.i4 4100
   stloc.0
   ldc.i4 0x7fffffff
   conv.i4
   not
   ldc.i4 0x80000000
   bne.un Done

TestCase3999:
   ldc.i4 4099
   stloc.0
   ldc.i4 0x7ffffffe
   conv.i4
   not
   ldc.i4 0x80000001
   bne.un Done

TestCase3998:
   ldc.i4 4098
   stloc.0
   ldc.i4 0x7ffffffd
   conv.i4
   not
   ldc.i4 0x80000002
   beq TestCase3997
   br Done

TestCase3997:
   ldc.i4 4097
   stloc.0
   ldc.i4 0x7ffffffc
   conv.i4
   not
   ldc.i4 0x80000003
   bne.un Done

TestCase3996:
   ldc.i4 4096
   stloc.0
   ldc.i4 0x7ffffffb
   conv.i4
   not
   ldc.i4 0x80000004
   beq TestCase3995
   br Done

TestCase3995:
   ldc.i4 4095
   stloc.0
   ldc.i4 0x7ffffffa
   conv.i4
   not
   ldc.i4 0x80000005
   beq TestCase3994
   br Done

TestCase3994:
   ldc.i4 4094
   stloc.0
   ldc.i4 0x7ffffff9
   conv.i4
   not
   ldc.i4 0x80000006
   bne.un Done

TestCase3993:
   ldc.i4 4093
   stloc.0
   ldc.i4 0x7ffffff8
   conv.i4
   not
   ldc.i4 0x80000007
   beq TestCase3992
   br Done

TestCase3992:
   ldc.i4 4092
   stloc.0
   ldc.i4 0x7ffffff7
   conv.i4
   not
   ldc.i4 0x80000008
   bne.un Done

TestCase3991:
   ldc.i4 4091
   stloc.0
   ldc.i4 0x7ffffff6
   conv.i4
   not
   ldc.i4 0x80000009
   bne.un Done

TestCase3990:
   ldc.i4 4090
   stloc.0
   ldc.i4 0x7ffffff5
   conv.i4
   not
   ldc.i4 0x8000000a
   bne.un Done

TestCase3989:
   ldc.i4 4089
   stloc.0
   ldc.i4 0x7ffffff4
   conv.i4
   not
   ldc.i4 0x8000000b
   beq TestCase3988
   br Done

TestCase3988:
   ldc.i4 4088
   stloc.0
   ldc.i4 0x7ffffff3
   conv.i4
   not
   ldc.i4 0x8000000c
   beq TestCase3987
   br Done

TestCase3987:
   ldc.i4 4087
   stloc.0
   ldc.i4 0x7ffffff2
   conv.i4
   not
   ldc.i4 0x8000000d
   bne.un Done

TestCase3986:
   ldc.i4 4086
   stloc.0
   ldc.i4 0x7ffffff1
   conv.i4
   not
   ldc.i4 0x8000000e
   beq TestCase3985
   br Done

TestCase3985:
   ldc.i4 4085
   stloc.0
   ldc.i4 0x7ffffff0
   conv.i4
   not
   ldc.i4 0x8000000f
   bne.un Done

TestCase3984:
   ldc.i4 4084
   stloc.0
   ldc.i4 0x7fffffef
   conv.i4
   not
   ldc.i4 0x80000010
   beq TestCase3983
   br Done

TestCase3983:
   ldc.i4 4083
   stloc.0
   ldc.i4 0x7fffffee
   conv.i4
   not
   ldc.i4 0x80000011
   beq TestCase3982
   br Done

TestCase3982:
   ldc.i4 4082
   stloc.0
   ldc.i4 0x7fffffed
   conv.i4
   not
   ldc.i4 0x80000012
   bne.un Done

TestCase3981:
   ldc.i4 4081
   stloc.0
   ldc.i4 0x7fffffec
   conv.i4
   not
   ldc.i4 0x80000013
   bne.un Done

TestCase3980:
   ldc.i4 4080
   stloc.0
   ldc.i4 0x7fffffeb
   conv.i4
   not
   ldc.i4 0x80000014
   beq TestCase3979
   br Done

TestCase3979:
   ldc.i4 4079
   stloc.0
   ldc.i4 0x7fffffea
   conv.i4
   not
   ldc.i4 0x80000015
   bne.un Done

TestCase3978:
   ldc.i4 4078
   stloc.0
   ldc.i4 0x7fffffe9
   conv.i4
   not
   ldc.i4 0x80000016
   bne.un Done

TestCase3977:
   ldc.i4 4077
   stloc.0
   ldc.i4 0x7fffffe8
   conv.i4
   not
   ldc.i4 0x80000017
   bne.un Done

TestCase3976:
   ldc.i4 4076
   stloc.0
   ldc.i4 0x7fffffe7
   conv.i4
   not
   ldc.i4 0x80000018
   beq TestCase3975
   br Done

TestCase3975:
   ldc.i4 4075
   stloc.0
   ldc.i4 0x7fffffe6
   conv.i4
   not
   ldc.i4 0x80000019
   bne.un Done

TestCase3974:
   ldc.i4 4074
   stloc.0
   ldc.i4 0x7fffffe5
   conv.i4
   not
   ldc.i4 0x8000001a
   beq TestCase3973
   br Done

TestCase3973:
   ldc.i4 4073
   stloc.0
   ldc.i4 0x7fffffe4
   conv.i4
   not
   ldc.i4 0x8000001b
   beq TestCase3972
   br Done

TestCase3972:
   ldc.i4 4072
   stloc.0
   ldc.i4 0x7fffffe3
   conv.i4
   not
   ldc.i4 0x8000001c
   bne.un Done

TestCase3971:
   ldc.i4 4071
   stloc.0
   ldc.i4 0x7fffffe2
   conv.i4
   not
   ldc.i4 0x8000001d
   bne.un Done

TestCase3970:
   ldc.i4 4070
   stloc.0
   ldc.i4 0x7fffffe1
   conv.i4
   not
   ldc.i4 0x8000001e
   beq TestCase3969
   br Done

TestCase3969:
   ldc.i4 4069
   stloc.0
   ldc.i4 0x7fffffe0
   conv.i4
   not
   ldc.i4 0x8000001f
   bne.un Done

TestCase3968:
   ldc.i4 4068
   stloc.0
   ldc.i4 0x7fffffdf
   conv.i4
   not
   ldc.i4 0x80000020
   beq TestCase3967
   br Done

TestCase3967:
   ldc.i4 4067
   stloc.0
   ldc.i4 0x7fffffde
   conv.i4
   not
   ldc.i4 0x80000021
   bne.un Done

TestCase3966:
   ldc.i4 4066
   stloc.0
   ldc.i4 0x7fffffdd
   conv.i4
   not
   ldc.i4 0x80000022
   beq TestCase3965
   br Done

TestCase3965:
   ldc.i4 4065
   stloc.0
   ldc.i4 0x7fffffdc
   conv.i4
   not
   ldc.i4 0x80000023
   bne.un Done

TestCase3964:
   ldc.i4 4064
   stloc.0
   ldc.i4 0x7fffffdb
   conv.i4
   not
   ldc.i4 0x80000024
   beq TestCase3963
   br Done

TestCase3963:
   ldc.i4 4063
   stloc.0
   ldc.i4 0x7fffffda
   conv.i4
   not
   ldc.i4 0x80000025
   beq TestCase3962
   br Done

TestCase3962:
   ldc.i4 4062
   stloc.0
   ldc.i4 0x7fffffd9
   conv.i4
   not
   ldc.i4 0x80000026
   beq TestCase3961
   br Done

TestCase3961:
   ldc.i4 4061
   stloc.0
   ldc.i4 0x7fffffd8
   conv.i4
   not
   ldc.i4 0x80000027
   beq TestCase3960
   br Done

TestCase3960:
   ldc.i4 4060
   stloc.0
   ldc.i4 0x7fffffd7
   conv.i4
   not
   ldc.i4 0x80000028
   bne.un Done

TestCase3959:
   ldc.i4 4059
   stloc.0
   ldc.i4 0x7fffffd6
   conv.i4
   not
   ldc.i4 0x80000029
   beq TestCase3958
   br Done

TestCase3958:
   ldc.i4 4058
   stloc.0
   ldc.i4 0x7fffffd5
   conv.i4
   not
   ldc.i4 0x8000002a
   beq TestCase3957
   br Done

TestCase3957:
   ldc.i4 4057
   stloc.0
   ldc.i4 0x7fffffd4
   conv.i4
   not
   ldc.i4 0x8000002b
   beq TestCase3956
   br Done

TestCase3956:
   ldc.i4 4056
   stloc.0
   ldc.i4 0x7fffffd3
   conv.i4
   not
   ldc.i4 0x8000002c
   bne.un Done

TestCase3955:
   ldc.i4 4055
   stloc.0
   ldc.i4 0x7fffffd2
   conv.i4
   not
   ldc.i4 0x8000002d
   beq TestCase3954
   br Done

TestCase3954:
   ldc.i4 4054
   stloc.0
   ldc.i4 0x7fffffd1
   conv.i4
   not
   ldc.i4 0x8000002e
   bne.un Done

TestCase3953:
   ldc.i4 4053
   stloc.0
   ldc.i4 0x7fffffd0
   conv.i4
   not
   ldc.i4 0x8000002f
   bne.un Done

TestCase3952:
   ldc.i4 4052
   stloc.0
   ldc.i4 0x7fffffcf
   conv.i4
   not
   ldc.i4 0x80000030
   beq TestCase3951
   br Done

TestCase3951:
   ldc.i4 4051
   stloc.0
   ldc.i4 0x7fffffce
   conv.i4
   not
   ldc.i4 0x80000031
   bne.un Done

TestCase3950:
   ldc.i4 4050
   stloc.0
   ldc.i4 0x7fffffcd
   conv.i4
   not
   ldc.i4 0x80000032
   bne.un Done

TestCase3949:
   ldc.i4 4049
   stloc.0
   ldc.i4 0x7fffffcc
   conv.i4
   not
   ldc.i4 0x80000033
   beq TestCase3948
   br Done

TestCase3948:
   ldc.i4 4048
   stloc.0
   ldc.i4 0x7fffffcb
   conv.i4
   not
   ldc.i4 0x80000034
   beq TestCase3947
   br Done

TestCase3947:
   ldc.i4 4047
   stloc.0
   ldc.i4 0x7fffffca
   conv.i4
   not
   ldc.i4 0x80000035
   bne.un Done

TestCase3946:
   ldc.i4 4046
   stloc.0
   ldc.i4 0x7fffffc9
   conv.i4
   not
   ldc.i4 0x80000036
   bne.un Done

TestCase3945:
   ldc.i4 4045
   stloc.0
   ldc.i4 0x7fffffc8
   conv.i4
   not
   ldc.i4 0x80000037
   bne.un Done

TestCase3944:
   ldc.i4 4044
   stloc.0
   ldc.i4 0x7fffffc7
   conv.i4
   not
   ldc.i4 0x80000038
   beq TestCase3943
   br Done

TestCase3943:
   ldc.i4 4043
   stloc.0
   ldc.i4 0x7fffffc6
   conv.i4
   not
   ldc.i4 0x80000039
   bne.un Done

TestCase3942:
   ldc.i4 4042
   stloc.0
   ldc.i4 0x7fffffc5
   conv.i4
   not
   ldc.i4 0x8000003a
   bne.un Done

TestCase3941:
   ldc.i4 4041
   stloc.0
   ldc.i4 0x7fffffc4
   conv.i4
   not
   ldc.i4 0x8000003b
   bne.un Done

TestCase3940:
   ldc.i4 4040
   stloc.0
   ldc.i4 0x7fffffc3
   conv.i4
   not
   ldc.i4 0x8000003c
   bne.un Done

TestCase3939:
   ldc.i4 4039
   stloc.0
   ldc.i4 0x7fffffc2
   conv.i4
   not
   ldc.i4 0x8000003d
   bne.un Done

TestCase3938:
   ldc.i4 4038
   stloc.0
   ldc.i4 0x7fffffc1
   conv.i4
   not
   ldc.i4 0x8000003e
   bne.un Done

TestCase3937:
   ldc.i4 4037
   stloc.0
   ldc.i4 0x7fffffc0
   conv.i4
   not
   ldc.i4 0x8000003f
   bne.un Done

TestCase3936:
   ldc.i4 4036
   stloc.0
   ldc.i4 0x7fffffbf
   conv.i4
   not
   ldc.i4 0x80000040
   bne.un Done

TestCase3935:
   ldc.i4 4035
   stloc.0
   ldc.i4 0x7fffffbe
   conv.i4
   not
   ldc.i4 0x80000041
   beq TestCase3934
   br Done

TestCase3934:
   ldc.i4 4034
   stloc.0
   ldc.i4 0x7fffffbd
   conv.i4
   not
   ldc.i4 0x80000042
   beq TestCase3933
   br Done

TestCase3933:
   ldc.i4 4033
   stloc.0
   ldc.i4 0x7fffffbc
   conv.i4
   not
   ldc.i4 0x80000043
   bne.un Done

TestCase3932:
   ldc.i4 4032
   stloc.0
   ldc.i4 0x7fffffbb
   conv.i4
   not
   ldc.i4 0x80000044
   bne.un Done

TestCase3931:
   ldc.i4 4031
   stloc.0
   ldc.i4 0x7fffffba
   conv.i4
   not
   ldc.i4 0x80000045
   bne.un Done

TestCase3930:
   ldc.i4 4030
   stloc.0
   ldc.i4 0x7fffffb9
   conv.i4
   not
   ldc.i4 0x80000046
   bne.un Done

TestCase3929:
   ldc.i4 4029
   stloc.0
   ldc.i4 0x7fffffb8
   conv.i4
   not
   ldc.i4 0x80000047
   bne.un Done

TestCase3928:
   ldc.i4 4028
   stloc.0
   ldc.i4 0x7fffffb7
   conv.i4
   not
   ldc.i4 0x80000048
   bne.un Done

TestCase3927:
   ldc.i4 4027
   stloc.0
   ldc.i4 0x7fffffb6
   conv.i4
   not
   ldc.i4 0x80000049
   bne.un Done

TestCase3926:
   ldc.i4 4026
   stloc.0
   ldc.i4 0x7fffffb5
   conv.i4
   not
   ldc.i4 0x8000004a
   beq TestCase3925
   br Done

TestCase3925:
   ldc.i4 4025
   stloc.0
   ldc.i4 0x7fffffb4
   conv.i4
   not
   ldc.i4 0x8000004b
   beq TestCase3924
   br Done

TestCase3924:
   ldc.i4 4024
   stloc.0
   ldc.i4 0x7fffffb3
   conv.i4
   not
   ldc.i4 0x8000004c
   beq TestCase3923
   br Done

TestCase3923:
   ldc.i4 4023
   stloc.0
   ldc.i4 0x7fffffb2
   conv.i4
   not
   ldc.i4 0x8000004d
   beq TestCase3922
   br Done

TestCase3922:
   ldc.i4 4022
   stloc.0
   ldc.i4 0x7fffffb1
   conv.i4
   not
   ldc.i4 0x8000004e
   bne.un Done

TestCase3921:
   ldc.i4 4021
   stloc.0
   ldc.i4 0x7fffffb0
   conv.i4
   not
   ldc.i4 0x8000004f
   bne.un Done

TestCase3920:
   ldc.i4 4020
   stloc.0
   ldc.i4 0x7fffffaf
   conv.i4
   not
   ldc.i4 0x80000050
   beq TestCase3919
   br Done

TestCase3919:
   ldc.i4 4019
   stloc.0
   ldc.i4 0x7fffffae
   conv.i4
   not
   ldc.i4 0x80000051
   beq TestCase3918
   br Done

TestCase3918:
   ldc.i4 4018
   stloc.0
   ldc.i4 0x7fffffad
   conv.i4
   not
   ldc.i4 0x80000052
   beq TestCase3917
   br Done

TestCase3917:
   ldc.i4 4017
   stloc.0
   ldc.i4 0x7fffffac
   conv.i4
   not
   ldc.i4 0x80000053
   beq TestCase3916
   br Done

TestCase3916:
   ldc.i4 4016
   stloc.0
   ldc.i4 0x7fffffab
   conv.i4
   not
   ldc.i4 0x80000054
   beq TestCase3915
   br Done

TestCase3915:
   ldc.i4 4015
   stloc.0
   ldc.i4 0x7fffffaa
   conv.i4
   not
   ldc.i4 0x80000055
   beq TestCase3914
   br Done

TestCase3914:
   ldc.i4 4014
   stloc.0
   ldc.i4 0x7fffffa9
   conv.i4
   not
   ldc.i4 0x80000056
   beq TestCase3913
   br Done

TestCase3913:
   ldc.i4 4013
   stloc.0
   ldc.i4 0x7fffffa8
   conv.i4
   not
   ldc.i4 0x80000057
   bne.un Done

TestCase3912:
   ldc.i4 4012
   stloc.0
   ldc.i4 0x7fffffa7
   conv.i4
   not
   ldc.i4 0x80000058
   bne.un Done

TestCase3911:
   ldc.i4 4011
   stloc.0
   ldc.i4 0x7fffffa6
   conv.i4
   not
   ldc.i4 0x80000059
   bne.un Done

TestCase3910:
   ldc.i4 4010
   stloc.0
   ldc.i4 0x7fffffa5
   conv.i4
   not
   ldc.i4 0x8000005a
   beq TestCase3909
   br Done

TestCase3909:
   ldc.i4 4009
   stloc.0
   ldc.i4 0x7fffffa4
   conv.i4
   not
   ldc.i4 0x8000005b
   beq TestCase3908
   br Done

TestCase3908:
   ldc.i4 4008
   stloc.0
   ldc.i4 0x7fffffa3
   conv.i4
   not
   ldc.i4 0x8000005c
   bne.un Done

TestCase3907:
   ldc.i4 4007
   stloc.0
   ldc.i4 0x7fffffa2
   conv.i4
   not
   ldc.i4 0x8000005d
   bne.un Done

TestCase3906:
   ldc.i4 4006
   stloc.0
   ldc.i4 0x7fffffa1
   conv.i4
   not
   ldc.i4 0x8000005e
   bne.un Done

TestCase3905:
   ldc.i4 4005
   stloc.0
   ldc.i4 0x7fffffa0
   conv.i4
   not
   ldc.i4 0x8000005f
   beq TestCase3904
   br Done

TestCase3904:
   ldc.i4 4004
   stloc.0
   ldc.i4 0x7fffff9f
   conv.i4
   not
   ldc.i4 0x80000060
   bne.un Done

TestCase3903:
   ldc.i4 4003
   stloc.0
   ldc.i4 0x7fffff9e
   conv.i4
   not
   ldc.i4 0x80000061
   beq TestCase3902
   br Done

TestCase3902:
   ldc.i4 4002
   stloc.0
   ldc.i4 0x7fffff9d
   conv.i4
   not
   ldc.i4 0x80000062
   bne.un Done

TestCase3901:
   ldc.i4 4001
   stloc.0
   ldc.i4 0x7fffff9c
   conv.i4
   not
   ldc.i4 0x80000063
   bne.un Done

TestCase3900:
   ldc.i4 4000
   stloc.0
   ldc.i4 0x7fffff9b
   conv.i4
   not
   ldc.i4 0x80000064
   beq TestCase3899
   br Done

TestCase3899:
   ldc.i4 3999
   stloc.0
   ldc.i4 0x7fffff9a
   conv.i4
   not
   ldc.i4 0x80000065
   bne.un Done

TestCase3898:
   ldc.i4 3998
   stloc.0
   ldc.i4 0x7fffff99
   conv.i4
   not
   ldc.i4 0x80000066
   beq TestCase3897
   br Done

TestCase3897:
   ldc.i4 3997
   stloc.0
   ldc.i4 0x7fffff98
   conv.i4
   not
   ldc.i4 0x80000067
   beq TestCase3896
   br Done

TestCase3896:
   ldc.i4 3996
   stloc.0
   ldc.i4 0x7fffff97
   conv.i4
   not
   ldc.i4 0x80000068
   beq TestCase3895
   br Done

TestCase3895:
   ldc.i4 3995
   stloc.0
   ldc.i4 0x7fffff96
   conv.i4
   not
   ldc.i4 0x80000069
   beq TestCase3894
   br Done

TestCase3894:
   ldc.i4 3994
   stloc.0
   ldc.i4 0x7fffff95
   conv.i4
   not
   ldc.i4 0x8000006a
   bne.un Done

TestCase3893:
   ldc.i4 3993
   stloc.0
   ldc.i4 0x7fffff94
   conv.i4
   not
   ldc.i4 0x8000006b
   bne.un Done

TestCase3892:
   ldc.i4 3992
   stloc.0
   ldc.i4 0x7fffff93
   conv.i4
   not
   ldc.i4 0x8000006c
   bne.un Done

TestCase3891:
   ldc.i4 3991
   stloc.0
   ldc.i4 0x7fffff92
   conv.i4
   not
   ldc.i4 0x8000006d
   bne.un Done

TestCase3890:
   ldc.i4 3990
   stloc.0
   ldc.i4 0x7fffff91
   conv.i4
   not
   ldc.i4 0x8000006e
   bne.un Done

TestCase3889:
   ldc.i4 3989
   stloc.0
   ldc.i4 0x7fffff90
   conv.i4
   not
   ldc.i4 0x8000006f
   bne.un Done

TestCase3888:
   ldc.i4 3988
   stloc.0
   ldc.i4 0x7fffff8f
   conv.i4
   not
   ldc.i4 0x80000070
   beq TestCase3887
   br Done

TestCase3887:
   ldc.i4 3987
   stloc.0
   ldc.i4 0x7fffff8e
   conv.i4
   not
   ldc.i4 0x80000071
   beq TestCase3886
   br Done

TestCase3886:
   ldc.i4 3986
   stloc.0
   ldc.i4 0x7fffff8d
   conv.i4
   not
   ldc.i4 0x80000072
   beq TestCase3885
   br Done

TestCase3885:
   ldc.i4 3985
   stloc.0
   ldc.i4 0x7fffff8c
   conv.i4
   not
   ldc.i4 0x80000073
   beq TestCase3884
   br Done

TestCase3884:
   ldc.i4 3984
   stloc.0
   ldc.i4 0x7fffff8b
   conv.i4
   not
   ldc.i4 0x80000074
   beq TestCase3883
   br Done

TestCase3883:
   ldc.i4 3983
   stloc.0
   ldc.i4 0x7fffff8a
   conv.i4
   not
   ldc.i4 0x80000075
   bne.un Done

TestCase3882:
   ldc.i4 3982
   stloc.0
   ldc.i4 0x7fffff89
   conv.i4
   not
   ldc.i4 0x80000076
   bne.un Done

TestCase3881:
   ldc.i4 3981
   stloc.0
   ldc.i4 0x7fffff88
   conv.i4
   not
   ldc.i4 0x80000077
   bne.un Done

TestCase3880:
   ldc.i4 3980
   stloc.0
   ldc.i4 0x7fffff87
   conv.i4
   not
   ldc.i4 0x80000078
   beq TestCase3879
   br Done

TestCase3879:
   ldc.i4 3979
   stloc.0
   ldc.i4 0x7fffff86
   conv.i4
   not
   ldc.i4 0x80000079
   beq TestCase3878
   br Done

TestCase3878:
   ldc.i4 3978
   stloc.0
   ldc.i4 0x7fffff85
   conv.i4
   not
   ldc.i4 0x8000007a
   beq TestCase3877
   br Done

TestCase3877:
   ldc.i4 3977
   stloc.0
   ldc.i4 0x7fffff84
   conv.i4
   not
   ldc.i4 0x8000007b
   bne.un Done

TestCase3876:
   ldc.i4 3976
   stloc.0
   ldc.i4 0x7fffff83
   conv.i4
   not
   ldc.i4 0x8000007c
   beq TestCase3875
   br Done

TestCase3875:
   ldc.i4 3975
   stloc.0
   ldc.i4 0x7fffff82
   conv.i4
   not
   ldc.i4 0x8000007d
   beq TestCase3874
   br Done

TestCase3874:
   ldc.i4 3974
   stloc.0
   ldc.i4 0x7fffff81
   conv.i4
   not
   ldc.i4 0x8000007e
   bne.un Done

TestCase3873:
   ldc.i4 3973
   stloc.0
   ldc.i4 0x7fffff80
   conv.i4
   not
   ldc.i4 0x8000007f
   bne.un Done

TestCase3872:
   ldc.i4 3972
   stloc.0
   ldc.i4 0x7fffff7f
   conv.i4
   not
   ldc.i4 0x80000080
   bne.un Done

TestCase3871:
   ldc.i4 3971
   stloc.0
   ldc.i4 0x7fffff7e
   conv.i4
   not
   ldc.i4 0x80000081
   beq TestCase3870
   br Done

TestCase3870:
   ldc.i4 3970
   stloc.0
   ldc.i4 0x7fffff7d
   conv.i4
   not
   ldc.i4 0x80000082
   beq TestCase3869
   br Done

TestCase3869:
   ldc.i4 3969
   stloc.0
   ldc.i4 0x7fffff7c
   conv.i4
   not
   ldc.i4 0x80000083
   bne.un Done

TestCase3868:
   ldc.i4 3968
   stloc.0
   ldc.i4 0x7fffff7b
   conv.i4
   not
   ldc.i4 0x80000084
   bne.un Done

TestCase3867:
   ldc.i4 3967
   stloc.0
   ldc.i4 0x7fffff7a
   conv.i4
   not
   ldc.i4 0x80000085
   beq TestCase3866
   br Done

TestCase3866:
   ldc.i4 3966
   stloc.0
   ldc.i4 0x7fffff79
   conv.i4
   not
   ldc.i4 0x80000086
   bne.un Done

TestCase3865:
   ldc.i4 3965
   stloc.0
   ldc.i4 0x7fffff78
   conv.i4
   not
   ldc.i4 0x80000087
   bne.un Done

TestCase3864:
   ldc.i4 3964
   stloc.0
   ldc.i4 0x7fffff77
   conv.i4
   not
   ldc.i4 0x80000088
   bne.un Done

TestCase3863:
   ldc.i4 3963
   stloc.0
   ldc.i4 0x7fffff76
   conv.i4
   not
   ldc.i4 0x80000089
   beq TestCase3862
   br Done

TestCase3862:
   ldc.i4 3962
   stloc.0
   ldc.i4 0x7fffff75
   conv.i4
   not
   ldc.i4 0x8000008a
   beq TestCase3861
   br Done

TestCase3861:
   ldc.i4 3961
   stloc.0
   ldc.i4 0x7fffff74
   conv.i4
   not
   ldc.i4 0x8000008b
   beq TestCase3860
   br Done

TestCase3860:
   ldc.i4 3960
   stloc.0
   ldc.i4 0x7fffff73
   conv.i4
   not
   ldc.i4 0x8000008c
   bne.un Done

TestCase3859:
   ldc.i4 3959
   stloc.0
   ldc.i4 0x7fffff72
   conv.i4
   not
   ldc.i4 0x8000008d
   bne.un Done

TestCase3858:
   ldc.i4 3958
   stloc.0
   ldc.i4 0x7fffff71
   conv.i4
   not
   ldc.i4 0x8000008e
   beq TestCase3857
   br Done

TestCase3857:
   ldc.i4 3957
   stloc.0
   ldc.i4 0x7fffff70
   conv.i4
   not
   ldc.i4 0x8000008f
   beq TestCase3856
   br Done

TestCase3856:
   ldc.i4 3956
   stloc.0
   ldc.i4 0x7fffff6f
   conv.i4
   not
   ldc.i4 0x80000090
   beq TestCase3855
   br Done

TestCase3855:
   ldc.i4 3955
   stloc.0
   ldc.i4 0x7fffff6e
   conv.i4
   not
   ldc.i4 0x80000091
   beq TestCase3854
   br Done

TestCase3854:
   ldc.i4 3954
   stloc.0
   ldc.i4 0x7fffff6d
   conv.i4
   not
   ldc.i4 0x80000092
   bne.un Done

TestCase3853:
   ldc.i4 3953
   stloc.0
   ldc.i4 0x7fffff6c
   conv.i4
   not
   ldc.i4 0x80000093
   beq TestCase3852
   br Done

TestCase3852:
   ldc.i4 3952
   stloc.0
   ldc.i4 0x7fffff6b
   conv.i4
   not
   ldc.i4 0x80000094
   beq TestCase3851
   br Done

TestCase3851:
   ldc.i4 3951
   stloc.0
   ldc.i4 0x7fffff6a
   conv.i4
   not
   ldc.i4 0x80000095
   bne.un Done

TestCase3850:
   ldc.i4 3950
   stloc.0
   ldc.i4 0x7fffff69
   conv.i4
   not
   ldc.i4 0x80000096
   bne.un Done

TestCase3849:
   ldc.i4 3949
   stloc.0
   ldc.i4 0x7fffff68
   conv.i4
   not
   ldc.i4 0x80000097
   bne.un Done

TestCase3848:
   ldc.i4 3948
   stloc.0
   ldc.i4 0x7fffff67
   conv.i4
   not
   ldc.i4 0x80000098
   bne.un Done

TestCase3847:
   ldc.i4 3947
   stloc.0
   ldc.i4 0x7fffff66
   conv.i4
   not
   ldc.i4 0x80000099
   beq TestCase3846
   br Done

TestCase3846:
   ldc.i4 3946
   stloc.0
   ldc.i4 0x7fffff65
   conv.i4
   not
   ldc.i4 0x8000009a
   beq TestCase3845
   br Done

TestCase3845:
   ldc.i4 3945
   stloc.0
   ldc.i4 0x7fffff64
   conv.i4
   not
   ldc.i4 0x8000009b
   bne.un Done

TestCase3844:
   ldc.i4 3944
   stloc.0
   ldc.i4 0x7fffff63
   conv.i4
   not
   ldc.i4 0x8000009c
   bne.un Done

TestCase3843:
   ldc.i4 3943
   stloc.0
   ldc.i4 0x7fffff62
   conv.i4
   not
   ldc.i4 0x8000009d
   beq TestCase3842
   br Done

TestCase3842:
   ldc.i4 3942
   stloc.0
   ldc.i4 0x7fffff61
   conv.i4
   not
   ldc.i4 0x8000009e
   bne.un Done

TestCase3841:
   ldc.i4 3941
   stloc.0
   ldc.i4 0x7fffff60
   conv.i4
   not
   ldc.i4 0x8000009f
   bne.un Done

TestCase3840:
   ldc.i4 3940
   stloc.0
   ldc.i4 0x7fffff5f
   conv.i4
   not
   ldc.i4 0x800000a0
   bne.un Done

TestCase3839:
   ldc.i4 3939
   stloc.0
   ldc.i4 0x7fffff5e
   conv.i4
   not
   ldc.i4 0x800000a1
   bne.un Done

TestCase3838:
   ldc.i4 3938
   stloc.0
   ldc.i4 0x7fffff5d
   conv.i4
   not
   ldc.i4 0x800000a2
   bne.un Done

TestCase3837:
   ldc.i4 3937
   stloc.0
   ldc.i4 0x7fffff5c
   conv.i4
   not
   ldc.i4 0x800000a3
   beq TestCase3836
   br Done

TestCase3836:
   ldc.i4 3936
   stloc.0
   ldc.i4 0x7fffff5b
   conv.i4
   not
   ldc.i4 0x800000a4
   beq TestCase3835
   br Done

TestCase3835:
   ldc.i4 3935
   stloc.0
   ldc.i4 0x7fffff5a
   conv.i4
   not
   ldc.i4 0x800000a5
   beq TestCase3834
   br Done

TestCase3834:
   ldc.i4 3934
   stloc.0
   ldc.i4 0x7fffff59
   conv.i4
   not
   ldc.i4 0x800000a6
   beq TestCase3833
   br Done

TestCase3833:
   ldc.i4 3933
   stloc.0
   ldc.i4 0x7fffff58
   conv.i4
   not
   ldc.i4 0x800000a7
   bne.un Done

TestCase3832:
   ldc.i4 3932
   stloc.0
   ldc.i4 0x7fffff57
   conv.i4
   not
   ldc.i4 0x800000a8
   bne.un Done

TestCase3831:
   ldc.i4 3931
   stloc.0
   ldc.i4 0x7fffff56
   conv.i4
   not
   ldc.i4 0x800000a9
   bne.un Done

TestCase3830:
   ldc.i4 3930
   stloc.0
   ldc.i4 0x7fffff55
   conv.i4
   not
   ldc.i4 0x800000aa
   bne.un Done

TestCase3829:
   ldc.i4 3929
   stloc.0
   ldc.i4 0x7fffff54
   conv.i4
   not
   ldc.i4 0x800000ab
   bne.un Done

TestCase3828:
   ldc.i4 3928
   stloc.0
   ldc.i4 0x7fffff53
   conv.i4
   not
   ldc.i4 0x800000ac
   bne.un Done

TestCase3827:
   ldc.i4 3927
   stloc.0
   ldc.i4 0x7fffff52
   conv.i4
   not
   ldc.i4 0x800000ad
   bne.un Done

TestCase3826:
   ldc.i4 3926
   stloc.0
   ldc.i4 0x7fffff51
   conv.i4
   not
   ldc.i4 0x800000ae
   bne.un Done

TestCase3825:
   ldc.i4 3925
   stloc.0
   ldc.i4 0x7fffff50
   conv.i4
   not
   ldc.i4 0x800000af
   bne.un Done

TestCase3824:
   ldc.i4 3924
   stloc.0
   ldc.i4 0x7fffff4f
   conv.i4
   not
   ldc.i4 0x800000b0
   beq TestCase3823
   br Done

TestCase3823:
   ldc.i4 3923
   stloc.0
   ldc.i4 0x7fffff4e
   conv.i4
   not
   ldc.i4 0x800000b1
   beq TestCase3822
   br Done

TestCase3822:
   ldc.i4 3922
   stloc.0
   ldc.i4 0x7fffff4d
   conv.i4
   not
   ldc.i4 0x800000b2
   beq TestCase3821
   br Done

TestCase3821:
   ldc.i4 3921
   stloc.0
   ldc.i4 0x7fffff4c
   conv.i4
   not
   ldc.i4 0x800000b3
   bne.un Done

TestCase3820:
   ldc.i4 3920
   stloc.0
   ldc.i4 0x7fffff4b
   conv.i4
   not
   ldc.i4 0x800000b4
   bne.un Done

TestCase3819:
   ldc.i4 3919
   stloc.0
   ldc.i4 0x7fffff4a
   conv.i4
   not
   ldc.i4 0x800000b5
   beq TestCase3818
   br Done

TestCase3818:
   ldc.i4 3918
   stloc.0
   ldc.i4 0x7fffff49
   conv.i4
   not
   ldc.i4 0x800000b6
   bne.un Done

TestCase3817:
   ldc.i4 3917
   stloc.0
   ldc.i4 0x7fffff48
   conv.i4
   not
   ldc.i4 0x800000b7
   bne.un Done

TestCase3816:
   ldc.i4 3916
   stloc.0
   ldc.i4 0x7fffff47
   conv.i4
   not
   ldc.i4 0x800000b8
   beq TestCase3815
   br Done

TestCase3815:
   ldc.i4 3915
   stloc.0
   ldc.i4 0x7fffff46
   conv.i4
   not
   ldc.i4 0x800000b9
   bne.un Done

TestCase3814:
   ldc.i4 3914
   stloc.0
   ldc.i4 0x7fffff45
   conv.i4
   not
   ldc.i4 0x800000ba
   beq TestCase3813
   br Done

TestCase3813:
   ldc.i4 3913
   stloc.0
   ldc.i4 0x7fffff44
   conv.i4
   not
   ldc.i4 0x800000bb
   bne.un Done

TestCase3812:
   ldc.i4 3912
   stloc.0
   ldc.i4 0x7fffff43
   conv.i4
   not
   ldc.i4 0x800000bc
   beq TestCase3811
   br Done

TestCase3811:
   ldc.i4 3911
   stloc.0
   ldc.i4 0x7fffff42
   conv.i4
   not
   ldc.i4 0x800000bd
   beq TestCase3810
   br Done

TestCase3810:
   ldc.i4 3910
   stloc.0
   ldc.i4 0x7fffff41
   conv.i4
   not
   ldc.i4 0x800000be
   beq TestCase3809
   br Done

TestCase3809:
   ldc.i4 3909
   stloc.0
   ldc.i4 0x7fffff40
   conv.i4
   not
   ldc.i4 0x800000bf
   beq TestCase3808
   br Done

TestCase3808:
   ldc.i4 3908
   stloc.0
   ldc.i4 0x7fffff3f
   conv.i4
   not
   ldc.i4 0x800000c0
   beq TestCase3807
   br Done

TestCase3807:
   ldc.i4 3907
   stloc.0
   ldc.i4 0x7fffff3e
   conv.i4
   not
   ldc.i4 0x800000c1
   bne.un Done

TestCase3806:
   ldc.i4 3906
   stloc.0
   ldc.i4 0x7fffff3d
   conv.i4
   not
   ldc.i4 0x800000c2
   beq TestCase3805
   br Done

TestCase3805:
   ldc.i4 3905
   stloc.0
   ldc.i4 0x7fffff3c
   conv.i4
   not
   ldc.i4 0x800000c3
   beq TestCase3804
   br Done

TestCase3804:
   ldc.i4 3904
   stloc.0
   ldc.i4 0x7fffff3b
   conv.i4
   not
   ldc.i4 0x800000c4
   beq TestCase3803
   br Done

TestCase3803:
   ldc.i4 3903
   stloc.0
   ldc.i4 0x7fffff3a
   conv.i4
   not
   ldc.i4 0x800000c5
   bne.un Done

TestCase3802:
   ldc.i4 3902
   stloc.0
   ldc.i4 0x7fffff39
   conv.i4
   not
   ldc.i4 0x800000c6
   beq TestCase3801
   br Done

TestCase3801:
   ldc.i4 3901
   stloc.0
   ldc.i4 0x7fffff38
   conv.i4
   not
   ldc.i4 0x800000c7
   bne.un Done

TestCase3800:
   ldc.i4 3900
   stloc.0
   ldc.i4 0x7fffff37
   conv.i4
   not
   ldc.i4 0x800000c8
   beq TestCase3799
   br Done

TestCase3799:
   ldc.i4 3899
   stloc.0
   ldc.i4 0x7fffff36
   conv.i4
   not
   ldc.i4 0x800000c9
   beq TestCase3798
   br Done

TestCase3798:
   ldc.i4 3898
   stloc.0
   ldc.i4 0x7fffff35
   conv.i4
   not
   ldc.i4 0x800000ca
   bne.un Done

TestCase3797:
   ldc.i4 3897
   stloc.0
   ldc.i4 0x7fffff34
   conv.i4
   not
   ldc.i4 0x800000cb
   beq TestCase3796
   br Done

TestCase3796:
   ldc.i4 3896
   stloc.0
   ldc.i4 0x7fffff33
   conv.i4
   not
   ldc.i4 0x800000cc
   bne.un Done

TestCase3795:
   ldc.i4 3895
   stloc.0
   ldc.i4 0x7fffff32
   conv.i4
   not
   ldc.i4 0x800000cd
   bne.un Done

TestCase3794:
   ldc.i4 3894
   stloc.0
   ldc.i4 0x7fffff31
   conv.i4
   not
   ldc.i4 0x800000ce
   beq TestCase3793
   br Done

TestCase3793:
   ldc.i4 3893
   stloc.0
   ldc.i4 0x7fffff30
   conv.i4
   not
   ldc.i4 0x800000cf
   bne.un Done

TestCase3792:
   ldc.i4 3892
   stloc.0
   ldc.i4 0x7fffff2f
   conv.i4
   not
   ldc.i4 0x800000d0
   beq TestCase3791
   br Done

TestCase3791:
   ldc.i4 3891
   stloc.0
   ldc.i4 0x7fffff2e
   conv.i4
   not
   ldc.i4 0x800000d1
   beq TestCase3790
   br Done

TestCase3790:
   ldc.i4 3890
   stloc.0
   ldc.i4 0x7fffff2d
   conv.i4
   not
   ldc.i4 0x800000d2
   beq TestCase3789
   br Done

TestCase3789:
   ldc.i4 3889
   stloc.0
   ldc.i4 0x7fffff2c
   conv.i4
   not
   ldc.i4 0x800000d3
   beq TestCase3788
   br Done

TestCase3788:
   ldc.i4 3888
   stloc.0
   ldc.i4 0x7fffff2b
   conv.i4
   not
   ldc.i4 0x800000d4
   bne.un Done

TestCase3787:
   ldc.i4 3887
   stloc.0
   ldc.i4 0x7fffff2a
   conv.i4
   not
   ldc.i4 0x800000d5
   beq TestCase3786
   br Done

TestCase3786:
   ldc.i4 3886
   stloc.0
   ldc.i4 0x7fffff29
   conv.i4
   not
   ldc.i4 0x800000d6
   beq TestCase3785
   br Done

TestCase3785:
   ldc.i4 3885
   stloc.0
   ldc.i4 0x7fffff28
   conv.i4
   not
   ldc.i4 0x800000d7
   bne.un Done

TestCase3784:
   ldc.i4 3884
   stloc.0
   ldc.i4 0x7fffff27
   conv.i4
   not
   ldc.i4 0x800000d8
   beq TestCase3783
   br Done

TestCase3783:
   ldc.i4 3883
   stloc.0
   ldc.i4 0x7fffff26
   conv.i4
   not
   ldc.i4 0x800000d9
   bne.un Done

TestCase3782:
   ldc.i4 3882
   stloc.0
   ldc.i4 0x7fffff25
   conv.i4
   not
   ldc.i4 0x800000da
   beq TestCase3781
   br Done

TestCase3781:
   ldc.i4 3881
   stloc.0
   ldc.i4 0x7fffff24
   conv.i4
   not
   ldc.i4 0x800000db
   beq TestCase3780
   br Done

TestCase3780:
   ldc.i4 3880
   stloc.0
   ldc.i4 0x7fffff23
   conv.i4
   not
   ldc.i4 0x800000dc
   beq TestCase3779
   br Done

TestCase3779:
   ldc.i4 3879
   stloc.0
   ldc.i4 0x7fffff22
   conv.i4
   not
   ldc.i4 0x800000dd
   bne.un Done

TestCase3778:
   ldc.i4 3878
   stloc.0
   ldc.i4 0x7fffff21
   conv.i4
   not
   ldc.i4 0x800000de
   beq TestCase3777
   br Done

TestCase3777:
   ldc.i4 3877
   stloc.0
   ldc.i4 0x7fffff20
   conv.i4
   not
   ldc.i4 0x800000df
   beq TestCase3776
   br Done

TestCase3776:
   ldc.i4 3876
   stloc.0
   ldc.i4 0x7fffff1f
   conv.i4
   not
   ldc.i4 0x800000e0
   bne.un Done

TestCase3775:
   ldc.i4 3875
   stloc.0
   ldc.i4 0x7fffff1e
   conv.i4
   not
   ldc.i4 0x800000e1
   beq TestCase3774
   br Done

TestCase3774:
   ldc.i4 3874
   stloc.0
   ldc.i4 0x7fffff1d
   conv.i4
   not
   ldc.i4 0x800000e2
   bne.un Done

TestCase3773:
   ldc.i4 3873
   stloc.0
   ldc.i4 0x7fffff1c
   conv.i4
   not
   ldc.i4 0x800000e3
   beq TestCase3772
   br Done

TestCase3772:
   ldc.i4 3872
   stloc.0
   ldc.i4 0x7fffff1b
   conv.i4
   not
   ldc.i4 0x800000e4
   bne.un Done

TestCase3771:
   ldc.i4 3871
   stloc.0
   ldc.i4 0x7fffff1a
   conv.i4
   not
   ldc.i4 0x800000e5
   beq TestCase3770
   br Done

TestCase3770:
   ldc.i4 3870
   stloc.0
   ldc.i4 0x7fffff19
   conv.i4
   not
   ldc.i4 0x800000e6
   bne.un Done

TestCase3769:
   ldc.i4 3869
   stloc.0
   ldc.i4 0x7fffff18
   conv.i4
   not
   ldc.i4 0x800000e7
   beq TestCase3768
   br Done

TestCase3768:
   ldc.i4 3868
   stloc.0
   ldc.i4 0x7fffff17
   conv.i4
   not
   ldc.i4 0x800000e8
   bne.un Done

TestCase3767:
   ldc.i4 3867
   stloc.0
   ldc.i4 0x7fffff16
   conv.i4
   not
   ldc.i4 0x800000e9
   beq TestCase3766
   br Done

TestCase3766:
   ldc.i4 3866
   stloc.0
   ldc.i4 0x7fffff15
   conv.i4
   not
   ldc.i4 0x800000ea
   bne.un Done

TestCase3765:
   ldc.i4 3865
   stloc.0
   ldc.i4 0x7fffff14
   conv.i4
   not
   ldc.i4 0x800000eb
   bne.un Done

TestCase3764:
   ldc.i4 3864
   stloc.0
   ldc.i4 0x7fffff13
   conv.i4
   not
   ldc.i4 0x800000ec
   bne.un Done

TestCase3763:
   ldc.i4 3863
   stloc.0
   ldc.i4 0x7fffff12
   conv.i4
   not
   ldc.i4 0x800000ed
   bne.un Done

TestCase3762:
   ldc.i4 3862
   stloc.0
   ldc.i4 0x7fffff11
   conv.i4
   not
   ldc.i4 0x800000ee
   beq TestCase3761
   br Done

TestCase3761:
   ldc.i4 3861
   stloc.0
   ldc.i4 0x7fffff10
   conv.i4
   not
   ldc.i4 0x800000ef
   beq TestCase3760
   br Done

TestCase3760:
   ldc.i4 3860
   stloc.0
   ldc.i4 0x7fffff0f
   conv.i4
   not
   ldc.i4 0x800000f0
   beq TestCase3759
   br Done

TestCase3759:
   ldc.i4 3859
   stloc.0
   ldc.i4 0x7fffff0e
   conv.i4
   not
   ldc.i4 0x800000f1
   beq TestCase3758
   br Done

TestCase3758:
   ldc.i4 3858
   stloc.0
   ldc.i4 0x7fffff0d
   conv.i4
   not
   ldc.i4 0x800000f2
   beq TestCase3757
   br Done

TestCase3757:
   ldc.i4 3857
   stloc.0
   ldc.i4 0x7fffff0c
   conv.i4
   not
   ldc.i4 0x800000f3
   bne.un Done

TestCase3756:
   ldc.i4 3856
   stloc.0
   ldc.i4 0x7fffff0b
   conv.i4
   not
   ldc.i4 0x800000f4
   bne.un Done

TestCase3755:
   ldc.i4 3855
   stloc.0
   ldc.i4 0x7fffff0a
   conv.i4
   not
   ldc.i4 0x800000f5
   bne.un Done

TestCase3754:
   ldc.i4 3854
   stloc.0
   ldc.i4 0x7fffff09
   conv.i4
   not
   ldc.i4 0x800000f6
   bne.un Done

TestCase3753:
   ldc.i4 3853
   stloc.0
   ldc.i4 0x7fffff08
   conv.i4
   not
   ldc.i4 0x800000f7
   bne.un Done

TestCase3752:
   ldc.i4 3852
   stloc.0
   ldc.i4 0x7fffff07
   conv.i4
   not
   ldc.i4 0x800000f8
   beq TestCase3751
   br Done

TestCase3751:
   ldc.i4 3851
   stloc.0
   ldc.i4 0x7fffff06
   conv.i4
   not
   ldc.i4 0x800000f9
   beq TestCase3750
   br Done

TestCase3750:
   ldc.i4 3850
   stloc.0
   ldc.i4 0x7fffff05
   conv.i4
   not
   ldc.i4 0x800000fa
   beq TestCase3749
   br Done

TestCase3749:
   ldc.i4 3849
   stloc.0
   ldc.i4 0x7fffff04
   conv.i4
   not
   ldc.i4 0x800000fb
   bne.un Done

TestCase3748:
   ldc.i4 3848
   stloc.0
   ldc.i4 0x7fffff03
   conv.i4
   not
   ldc.i4 0x800000fc
   beq TestCase3747
   br Done

TestCase3747:
   ldc.i4 3847
   stloc.0
   ldc.i4 0x7fffff02
   conv.i4
   not
   ldc.i4 0x800000fd
   bne.un Done

TestCase3746:
   ldc.i4 3846
   stloc.0
   ldc.i4 0x7fffff01
   conv.i4
   not
   ldc.i4 0x800000fe
   beq TestCase3745
   br Done

TestCase3745:
   ldc.i4 3845
   stloc.0
   ldc.i4 0x7fffff00
   conv.i4
   not
   ldc.i4 0x800000ff
   bne.un Done

TestCase3744:
   ldc.i4 3844
   stloc.0
   ldc.i4 0x7ffffeff
   conv.i4
   not
   ldc.i4 0x80000100
   bne.un Done

TestCase3743:
   ldc.i4 3843
   stloc.0
   ldc.i4 0x7ffffefe
   conv.i4
   not
   ldc.i4 0x80000101
   bne.un Done

TestCase3742:
   ldc.i4 3842
   stloc.0
   ldc.i4 0x7ffffefd
   conv.i4
   not
   ldc.i4 0x80000102
   beq TestCase3741
   br Done

TestCase3741:
   ldc.i4 3841
   stloc.0
   ldc.i4 0x7ffffefc
   conv.i4
   not
   ldc.i4 0x80000103
   beq TestCase3740
   br Done

TestCase3740:
   ldc.i4 3840
   stloc.0
   ldc.i4 0x7ffffefb
   conv.i4
   not
   ldc.i4 0x80000104
   bne.un Done

TestCase3739:
   ldc.i4 3839
   stloc.0
   ldc.i4 0x7ffffefa
   conv.i4
   not
   ldc.i4 0x80000105
   bne.un Done

TestCase3738:
   ldc.i4 3838
   stloc.0
   ldc.i4 0x7ffffef9
   conv.i4
   not
   ldc.i4 0x80000106
   bne.un Done

TestCase3737:
   ldc.i4 3837
   stloc.0
   ldc.i4 0x7ffffef8
   conv.i4
   not
   ldc.i4 0x80000107
   beq TestCase3736
   br Done

TestCase3736:
   ldc.i4 3836
   stloc.0
   ldc.i4 0x7ffffef7
   conv.i4
   not
   ldc.i4 0x80000108
   bne.un Done

TestCase3735:
   ldc.i4 3835
   stloc.0
   ldc.i4 0x7ffffef6
   conv.i4
   not
   ldc.i4 0x80000109
   bne.un Done

TestCase3734:
   ldc.i4 3834
   stloc.0
   ldc.i4 0x7ffffef5
   conv.i4
   not
   ldc.i4 0x8000010a
   bne.un Done

TestCase3733:
   ldc.i4 3833
   stloc.0
   ldc.i4 0x7ffffef4
   conv.i4
   not
   ldc.i4 0x8000010b
   bne.un Done

TestCase3732:
   ldc.i4 3832
   stloc.0
   ldc.i4 0x7ffffef3
   conv.i4
   not
   ldc.i4 0x8000010c
   bne.un Done

TestCase3731:
   ldc.i4 3831
   stloc.0
   ldc.i4 0x7ffffef2
   conv.i4
   not
   ldc.i4 0x8000010d
   bne.un Done

TestCase3730:
   ldc.i4 3830
   stloc.0
   ldc.i4 0x7ffffef1
   conv.i4
   not
   ldc.i4 0x8000010e
   beq TestCase3729
   br Done

TestCase3729:
   ldc.i4 3829
   stloc.0
   ldc.i4 0x7ffffef0
   conv.i4
   not
   ldc.i4 0x8000010f
   beq TestCase3728
   br Done

TestCase3728:
   ldc.i4 3828
   stloc.0
   ldc.i4 0x7ffffeef
   conv.i4
   not
   ldc.i4 0x80000110
   beq TestCase3727
   br Done

TestCase3727:
   ldc.i4 3827
   stloc.0
   ldc.i4 0x7ffffeee
   conv.i4
   not
   ldc.i4 0x80000111
   bne.un Done

TestCase3726:
   ldc.i4 3826
   stloc.0
   ldc.i4 0x7ffffeed
   conv.i4
   not
   ldc.i4 0x80000112
   bne.un Done

TestCase3725:
   ldc.i4 3825
   stloc.0
   ldc.i4 0x7ffffeec
   conv.i4
   not
   ldc.i4 0x80000113
   bne.un Done

TestCase3724:
   ldc.i4 3824
   stloc.0
   ldc.i4 0x7ffffeeb
   conv.i4
   not
   ldc.i4 0x80000114
   bne.un Done

TestCase3723:
   ldc.i4 3823
   stloc.0
   ldc.i4 0x7ffffeea
   conv.i4
   not
   ldc.i4 0x80000115
   beq TestCase3722
   br Done

TestCase3722:
   ldc.i4 3822
   stloc.0
   ldc.i4 0x7ffffee9
   conv.i4
   not
   ldc.i4 0x80000116
   beq TestCase3721
   br Done

TestCase3721:
   ldc.i4 3821
   stloc.0
   ldc.i4 0x7ffffee8
   conv.i4
   not
   ldc.i4 0x80000117
   bne.un Done

TestCase3720:
   ldc.i4 3820
   stloc.0
   ldc.i4 0x7ffffee7
   conv.i4
   not
   ldc.i4 0x80000118
   bne.un Done

TestCase3719:
   ldc.i4 3819
   stloc.0
   ldc.i4 0x7ffffee6
   conv.i4
   not
   ldc.i4 0x80000119
   beq TestCase3718
   br Done

TestCase3718:
   ldc.i4 3818
   stloc.0
   ldc.i4 0x7ffffee5
   conv.i4
   not
   ldc.i4 0x8000011a
   bne.un Done

TestCase3717:
   ldc.i4 3817
   stloc.0
   ldc.i4 0x7ffffee4
   conv.i4
   not
   ldc.i4 0x8000011b
   beq TestCase3716
   br Done

TestCase3716:
   ldc.i4 3816
   stloc.0
   ldc.i4 0x7ffffee3
   conv.i4
   not
   ldc.i4 0x8000011c
   bne.un Done

TestCase3715:
   ldc.i4 3815
   stloc.0
   ldc.i4 0x7ffffee2
   conv.i4
   not
   ldc.i4 0x8000011d
   bne.un Done

TestCase3714:
   ldc.i4 3814
   stloc.0
   ldc.i4 0x7ffffee1
   conv.i4
   not
   ldc.i4 0x8000011e
   beq TestCase3713
   br Done

TestCase3713:
   ldc.i4 3813
   stloc.0
   ldc.i4 0x7ffffee0
   conv.i4
   not
   ldc.i4 0x8000011f
   beq TestCase3712
   br Done

TestCase3712:
   ldc.i4 3812
   stloc.0
   ldc.i4 0x7ffffedf
   conv.i4
   not
   ldc.i4 0x80000120
   bne.un Done

TestCase3711:
   ldc.i4 3811
   stloc.0
   ldc.i4 0x7ffffede
   conv.i4
   not
   ldc.i4 0x80000121
   beq TestCase3710
   br Done

TestCase3710:
   ldc.i4 3810
   stloc.0
   ldc.i4 0x7ffffedd
   conv.i4
   not
   ldc.i4 0x80000122
   bne.un Done

TestCase3709:
   ldc.i4 3809
   stloc.0
   ldc.i4 0x7ffffedc
   conv.i4
   not
   ldc.i4 0x80000123
   bne.un Done

TestCase3708:
   ldc.i4 3808
   stloc.0
   ldc.i4 0x7ffffedb
   conv.i4
   not
   ldc.i4 0x80000124
   beq TestCase3707
   br Done

TestCase3707:
   ldc.i4 3807
   stloc.0
   ldc.i4 0x7ffffeda
   conv.i4
   not
   ldc.i4 0x80000125
   bne.un Done

TestCase3706:
   ldc.i4 3806
   stloc.0
   ldc.i4 0x7ffffed9
   conv.i4
   not
   ldc.i4 0x80000126
   beq TestCase3705
   br Done

TestCase3705:
   ldc.i4 3805
   stloc.0
   ldc.i4 0x7ffffed8
   conv.i4
   not
   ldc.i4 0x80000127
   beq TestCase3704
   br Done

TestCase3704:
   ldc.i4 3804
   stloc.0
   ldc.i4 0x7ffffed7
   conv.i4
   not
   ldc.i4 0x80000128
   bne.un Done

TestCase3703:
   ldc.i4 3803
   stloc.0
   ldc.i4 0x7ffffed6
   conv.i4
   not
   ldc.i4 0x80000129
   bne.un Done

TestCase3702:
   ldc.i4 3802
   stloc.0
   ldc.i4 0x7ffffed5
   conv.i4
   not
   ldc.i4 0x8000012a
   bne.un Done

TestCase3701:
   ldc.i4 3801
   stloc.0
   ldc.i4 0x7ffffed4
   conv.i4
   not
   ldc.i4 0x8000012b
   beq TestCase3700
   br Done

TestCase3700:
   ldc.i4 3800
   stloc.0
   ldc.i4 0x7ffffed3
   conv.i4
   not
   ldc.i4 0x8000012c
   beq TestCase3699
   br Done

TestCase3699:
   ldc.i4 3799
   stloc.0
   ldc.i4 0x7ffffed2
   conv.i4
   not
   ldc.i4 0x8000012d
   beq TestCase3698
   br Done

TestCase3698:
   ldc.i4 3798
   stloc.0
   ldc.i4 0x7ffffed1
   conv.i4
   not
   ldc.i4 0x8000012e
   bne.un Done

TestCase3697:
   ldc.i4 3797
   stloc.0
   ldc.i4 0x7ffffed0
   conv.i4
   not
   ldc.i4 0x8000012f
   bne.un Done

TestCase3696:
   ldc.i4 3796
   stloc.0
   ldc.i4 0x7ffffecf
   conv.i4
   not
   ldc.i4 0x80000130
   beq TestCase3695
   br Done

TestCase3695:
   ldc.i4 3795
   stloc.0
   ldc.i4 0x7ffffece
   conv.i4
   not
   ldc.i4 0x80000131
   bne.un Done

TestCase3694:
   ldc.i4 3794
   stloc.0
   ldc.i4 0x7ffffecd
   conv.i4
   not
   ldc.i4 0x80000132
   bne.un Done

TestCase3693:
   ldc.i4 3793
   stloc.0
   ldc.i4 0x7ffffecc
   conv.i4
   not
   ldc.i4 0x80000133
   bne.un Done

TestCase3692:
   ldc.i4 3792
   stloc.0
   ldc.i4 0x7ffffecb
   conv.i4
   not
   ldc.i4 0x80000134
   bne.un Done

TestCase3691:
   ldc.i4 3791
   stloc.0
   ldc.i4 0x7ffffeca
   conv.i4
   not
   ldc.i4 0x80000135
   bne.un Done

TestCase3690:
   ldc.i4 3790
   stloc.0
   ldc.i4 0x7ffffec9
   conv.i4
   not
   ldc.i4 0x80000136
   bne.un Done

TestCase3689:
   ldc.i4 3789
   stloc.0
   ldc.i4 0x7ffffec8
   conv.i4
   not
   ldc.i4 0x80000137
   bne.un Done

TestCase3688:
   ldc.i4 3788
   stloc.0
   ldc.i4 0x7ffffec7
   conv.i4
   not
   ldc.i4 0x80000138
   beq TestCase3687
   br Done

TestCase3687:
   ldc.i4 3787
   stloc.0
   ldc.i4 0x7ffffec6
   conv.i4
   not
   ldc.i4 0x80000139
   bne.un Done

TestCase3686:
   ldc.i4 3786
   stloc.0
   ldc.i4 0x7ffffec5
   conv.i4
   not
   ldc.i4 0x8000013a
   bne.un Done

TestCase3685:
   ldc.i4 3785
   stloc.0
   ldc.i4 0x7ffffec4
   conv.i4
   not
   ldc.i4 0x8000013b
   beq TestCase3684
   br Done

TestCase3684:
   ldc.i4 3784
   stloc.0
   ldc.i4 0x7ffffec3
   conv.i4
   not
   ldc.i4 0x8000013c
   bne.un Done

TestCase3683:
   ldc.i4 3783
   stloc.0
   ldc.i4 0x7ffffec2
   conv.i4
   not
   ldc.i4 0x8000013d
   beq TestCase3682
   br Done

TestCase3682:
   ldc.i4 3782
   stloc.0
   ldc.i4 0x7ffffec1
   conv.i4
   not
   ldc.i4 0x8000013e
   bne.un Done

TestCase3681:
   ldc.i4 3781
   stloc.0
   ldc.i4 0x7ffffec0
   conv.i4
   not
   ldc.i4 0x8000013f
   beq TestCase3680
   br Done

TestCase3680:
   ldc.i4 3780
   stloc.0
   ldc.i4 0x7ffffebf
   conv.i4
   not
   ldc.i4 0x80000140
   beq TestCase3679
   br Done

TestCase3679:
   ldc.i4 3779
   stloc.0
   ldc.i4 0x7ffffebe
   conv.i4
   not
   ldc.i4 0x80000141
   bne.un Done

TestCase3678:
   ldc.i4 3778
   stloc.0
   ldc.i4 0x7ffffebd
   conv.i4
   not
   ldc.i4 0x80000142
   beq TestCase3677
   br Done

TestCase3677:
   ldc.i4 3777
   stloc.0
   ldc.i4 0x7ffffebc
   conv.i4
   not
   ldc.i4 0x80000143
   bne.un Done

TestCase3676:
   ldc.i4 3776
   stloc.0
   ldc.i4 0x7ffffebb
   conv.i4
   not
   ldc.i4 0x80000144
   beq TestCase3675
   br Done

TestCase3675:
   ldc.i4 3775
   stloc.0
   ldc.i4 0x7ffffeba
   conv.i4
   not
   ldc.i4 0x80000145
   beq TestCase3674
   br Done

TestCase3674:
   ldc.i4 3774
   stloc.0
   ldc.i4 0x7ffffeb9
   conv.i4
   not
   ldc.i4 0x80000146
   bne.un Done

TestCase3673:
   ldc.i4 3773
   stloc.0
   ldc.i4 0x7ffffeb8
   conv.i4
   not
   ldc.i4 0x80000147
   beq TestCase3672
   br Done

TestCase3672:
   ldc.i4 3772
   stloc.0
   ldc.i4 0x7ffffeb7
   conv.i4
   not
   ldc.i4 0x80000148
   beq TestCase3671
   br Done

TestCase3671:
   ldc.i4 3771
   stloc.0
   ldc.i4 0x7ffffeb6
   conv.i4
   not
   ldc.i4 0x80000149
   bne.un Done

TestCase3670:
   ldc.i4 3770
   stloc.0
   ldc.i4 0x7ffffeb5
   conv.i4
   not
   ldc.i4 0x8000014a
   bne.un Done

TestCase3669:
   ldc.i4 3769
   stloc.0
   ldc.i4 0x7ffffeb4
   conv.i4
   not
   ldc.i4 0x8000014b
   bne.un Done

TestCase3668:
   ldc.i4 3768
   stloc.0
   ldc.i4 0x7ffffeb3
   conv.i4
   not
   ldc.i4 0x8000014c
   bne.un Done

TestCase3667:
   ldc.i4 3767
   stloc.0
   ldc.i4 0x7ffffeb2
   conv.i4
   not
   ldc.i4 0x8000014d
   bne.un Done

TestCase3666:
   ldc.i4 3766
   stloc.0
   ldc.i4 0x7ffffeb1
   conv.i4
   not
   ldc.i4 0x8000014e
   beq TestCase3665
   br Done

TestCase3665:
   ldc.i4 3765
   stloc.0
   ldc.i4 0x7ffffeb0
   conv.i4
   not
   ldc.i4 0x8000014f
   beq TestCase3664
   br Done

TestCase3664:
   ldc.i4 3764
   stloc.0
   ldc.i4 0x7ffffeaf
   conv.i4
   not
   ldc.i4 0x80000150
   bne.un Done

TestCase3663:
   ldc.i4 3763
   stloc.0
   ldc.i4 0x7ffffeae
   conv.i4
   not
   ldc.i4 0x80000151
   beq TestCase3662
   br Done

TestCase3662:
   ldc.i4 3762
   stloc.0
   ldc.i4 0x7ffffead
   conv.i4
   not
   ldc.i4 0x80000152
   bne.un Done

TestCase3661:
   ldc.i4 3761
   stloc.0
   ldc.i4 0x7ffffeac
   conv.i4
   not
   ldc.i4 0x80000153
   bne.un Done

TestCase3660:
   ldc.i4 3760
   stloc.0
   ldc.i4 0x7ffffeab
   conv.i4
   not
   ldc.i4 0x80000154
   beq TestCase3659
   br Done

TestCase3659:
   ldc.i4 3759
   stloc.0
   ldc.i4 0x7ffffeaa
   conv.i4
   not
   ldc.i4 0x80000155
   beq TestCase3658
   br Done

TestCase3658:
   ldc.i4 3758
   stloc.0
   ldc.i4 0x7ffffea9
   conv.i4
   not
   ldc.i4 0x80000156
   beq TestCase3657
   br Done

TestCase3657:
   ldc.i4 3757
   stloc.0
   ldc.i4 0x7ffffea8
   conv.i4
   not
   ldc.i4 0x80000157
   beq TestCase3656
   br Done

TestCase3656:
   ldc.i4 3756
   stloc.0
   ldc.i4 0x7ffffea7
   conv.i4
   not
   ldc.i4 0x80000158
   bne.un Done

TestCase3655:
   ldc.i4 3755
   stloc.0
   ldc.i4 0x7ffffea6
   conv.i4
   not
   ldc.i4 0x80000159
   bne.un Done

TestCase3654:
   ldc.i4 3754
   stloc.0
   ldc.i4 0x7ffffea5
   conv.i4
   not
   ldc.i4 0x8000015a
   bne.un Done

TestCase3653:
   ldc.i4 3753
   stloc.0
   ldc.i4 0x7ffffea4
   conv.i4
   not
   ldc.i4 0x8000015b
   beq TestCase3652
   br Done

TestCase3652:
   ldc.i4 3752
   stloc.0
   ldc.i4 0x7ffffea3
   conv.i4
   not
   ldc.i4 0x8000015c
   bne.un Done

TestCase3651:
   ldc.i4 3751
   stloc.0
   ldc.i4 0x7ffffea2
   conv.i4
   not
   ldc.i4 0x8000015d
   bne.un Done

TestCase3650:
   ldc.i4 3750
   stloc.0
   ldc.i4 0x7ffffea1
   conv.i4
   not
   ldc.i4 0x8000015e
   bne.un Done

TestCase3649:
   ldc.i4 3749
   stloc.0
   ldc.i4 0x7ffffea0
   conv.i4
   not
   ldc.i4 0x8000015f
   bne.un Done

TestCase3648:
   ldc.i4 3748
   stloc.0
   ldc.i4 0x7ffffe9f
   conv.i4
   not
   ldc.i4 0x80000160
   bne.un Done

TestCase3647:
   ldc.i4 3747
   stloc.0
   ldc.i4 0x7ffffe9e
   conv.i4
   not
   ldc.i4 0x80000161
   bne.un Done

TestCase3646:
   ldc.i4 3746
   stloc.0
   ldc.i4 0x7ffffe9d
   conv.i4
   not
   ldc.i4 0x80000162
   beq TestCase3645
   br Done

TestCase3645:
   ldc.i4 3745
   stloc.0
   ldc.i4 0x7ffffe9c
   conv.i4
   not
   ldc.i4 0x80000163
   bne.un Done

TestCase3644:
   ldc.i4 3744
   stloc.0
   ldc.i4 0x7ffffe9b
   conv.i4
   not
   ldc.i4 0x80000164
   bne.un Done

TestCase3643:
   ldc.i4 3743
   stloc.0
   ldc.i4 0x7ffffe9a
   conv.i4
   not
   ldc.i4 0x80000165
   bne.un Done

TestCase3642:
   ldc.i4 3742
   stloc.0
   ldc.i4 0x7ffffe99
   conv.i4
   not
   ldc.i4 0x80000166
   beq TestCase3641
   br Done

TestCase3641:
   ldc.i4 3741
   stloc.0
   ldc.i4 0x7ffffe98
   conv.i4
   not
   ldc.i4 0x80000167
   beq TestCase3640
   br Done

TestCase3640:
   ldc.i4 3740
   stloc.0
   ldc.i4 0x7ffffe97
   conv.i4
   not
   ldc.i4 0x80000168
   beq TestCase3639
   br Done

TestCase3639:
   ldc.i4 3739
   stloc.0
   ldc.i4 0x7ffffe96
   conv.i4
   not
   ldc.i4 0x80000169
   beq TestCase3638
   br Done

TestCase3638:
   ldc.i4 3738
   stloc.0
   ldc.i4 0x7ffffe95
   conv.i4
   not
   ldc.i4 0x8000016a
   beq TestCase3637
   br Done

TestCase3637:
   ldc.i4 3737
   stloc.0
   ldc.i4 0x7ffffe94
   conv.i4
   not
   ldc.i4 0x8000016b
   bne.un Done

TestCase3636:
   ldc.i4 3736
   stloc.0
   ldc.i4 0x7ffffe93
   conv.i4
   not
   ldc.i4 0x8000016c
   bne.un Done

TestCase3635:
   ldc.i4 3735
   stloc.0
   ldc.i4 0x7ffffe92
   conv.i4
   not
   ldc.i4 0x8000016d
   beq TestCase3634
   br Done

TestCase3634:
   ldc.i4 3734
   stloc.0
   ldc.i4 0x7ffffe91
   conv.i4
   not
   ldc.i4 0x8000016e
   beq TestCase3633
   br Done

TestCase3633:
   ldc.i4 3733
   stloc.0
   ldc.i4 0x7ffffe90
   conv.i4
   not
   ldc.i4 0x8000016f
   beq TestCase3632
   br Done

TestCase3632:
   ldc.i4 3732
   stloc.0
   ldc.i4 0x7ffffe8f
   conv.i4
   not
   ldc.i4 0x80000170
   bne.un Done

TestCase3631:
   ldc.i4 3731
   stloc.0
   ldc.i4 0x7ffffe8e
   conv.i4
   not
   ldc.i4 0x80000171
   bne.un Done

TestCase3630:
   ldc.i4 3730
   stloc.0
   ldc.i4 0x7ffffe8d
   conv.i4
   not
   ldc.i4 0x80000172
   beq TestCase3629
   br Done

TestCase3629:
   ldc.i4 3729
   stloc.0
   ldc.i4 0x7ffffe8c
   conv.i4
   not
   ldc.i4 0x80000173
   beq TestCase3628
   br Done

TestCase3628:
   ldc.i4 3728
   stloc.0
   ldc.i4 0x7ffffe8b
   conv.i4
   not
   ldc.i4 0x80000174
   beq TestCase3627
   br Done

TestCase3627:
   ldc.i4 3727
   stloc.0
   ldc.i4 0x7ffffe8a
   conv.i4
   not
   ldc.i4 0x80000175
   bne.un Done

TestCase3626:
   ldc.i4 3726
   stloc.0
   ldc.i4 0x7ffffe89
   conv.i4
   not
   ldc.i4 0x80000176
   bne.un Done

TestCase3625:
   ldc.i4 3725
   stloc.0
   ldc.i4 0x7ffffe88
   conv.i4
   not
   ldc.i4 0x80000177
   bne.un Done

TestCase3624:
   ldc.i4 3724
   stloc.0
   ldc.i4 0x7ffffe87
   conv.i4
   not
   ldc.i4 0x80000178
   beq TestCase3623
   br Done

TestCase3623:
   ldc.i4 3723
   stloc.0
   ldc.i4 0x7ffffe86
   conv.i4
   not
   ldc.i4 0x80000179
   beq TestCase3622
   br Done

TestCase3622:
   ldc.i4 3722
   stloc.0
   ldc.i4 0x7ffffe85
   conv.i4
   not
   ldc.i4 0x8000017a
   beq TestCase3621
   br Done

TestCase3621:
   ldc.i4 3721
   stloc.0
   ldc.i4 0x7ffffe84
   conv.i4
   not
   ldc.i4 0x8000017b
   beq TestCase3620
   br Done

TestCase3620:
   ldc.i4 3720
   stloc.0
   ldc.i4 0x7ffffe83
   conv.i4
   not
   ldc.i4 0x8000017c
   beq TestCase3619
   br Done

TestCase3619:
   ldc.i4 3719
   stloc.0
   ldc.i4 0x7ffffe82
   conv.i4
   not
   ldc.i4 0x8000017d
   beq TestCase3618
   br Done

TestCase3618:
   ldc.i4 3718
   stloc.0
   ldc.i4 0x7ffffe81
   conv.i4
   not
   ldc.i4 0x8000017e
   bne.un Done

TestCase3617:
   ldc.i4 3717
   stloc.0
   ldc.i4 0x7ffffe80
   conv.i4
   not
   ldc.i4 0x8000017f
   bne.un Done

TestCase3616:
   ldc.i4 3716
   stloc.0
   ldc.i4 0x7ffffe7f
   conv.i4
   not
   ldc.i4 0x80000180
   beq TestCase3615
   br Done

TestCase3615:
   ldc.i4 3715
   stloc.0
   ldc.i4 0x7ffffe7e
   conv.i4
   not
   ldc.i4 0x80000181
   bne.un Done

TestCase3614:
   ldc.i4 3714
   stloc.0
   ldc.i4 0x7ffffe7d
   conv.i4
   not
   ldc.i4 0x80000182
   beq TestCase3613
   br Done

TestCase3613:
   ldc.i4 3713
   stloc.0
   ldc.i4 0x7ffffe7c
   conv.i4
   not
   ldc.i4 0x80000183
   beq TestCase3612
   br Done

TestCase3612:
   ldc.i4 3712
   stloc.0
   ldc.i4 0x7ffffe7b
   conv.i4
   not
   ldc.i4 0x80000184
   beq TestCase3611
   br Done

TestCase3611:
   ldc.i4 3711
   stloc.0
   ldc.i4 0x7ffffe7a
   conv.i4
   not
   ldc.i4 0x80000185
   bne.un Done

TestCase3610:
   ldc.i4 3710
   stloc.0
   ldc.i4 0x7ffffe79
   conv.i4
   not
   ldc.i4 0x80000186
   beq TestCase3609
   br Done

TestCase3609:
   ldc.i4 3709
   stloc.0
   ldc.i4 0x7ffffe78
   conv.i4
   not
   ldc.i4 0x80000187
   beq TestCase3608
   br Done

TestCase3608:
   ldc.i4 3708
   stloc.0
   ldc.i4 0x7ffffe77
   conv.i4
   not
   ldc.i4 0x80000188
   bne.un Done

TestCase3607:
   ldc.i4 3707
   stloc.0
   ldc.i4 0x7ffffe76
   conv.i4
   not
   ldc.i4 0x80000189
   beq TestCase3606
   br Done

TestCase3606:
   ldc.i4 3706
   stloc.0
   ldc.i4 0x7ffffe75
   conv.i4
   not
   ldc.i4 0x8000018a
   bne.un Done

TestCase3605:
   ldc.i4 3705
   stloc.0
   ldc.i4 0x7ffffe74
   conv.i4
   not
   ldc.i4 0x8000018b
   bne.un Done

TestCase3604:
   ldc.i4 3704
   stloc.0
   ldc.i4 0x7ffffe73
   conv.i4
   not
   ldc.i4 0x8000018c
   bne.un Done

TestCase3603:
   ldc.i4 3703
   stloc.0
   ldc.i4 0x7ffffe72
   conv.i4
   not
   ldc.i4 0x8000018d
   bne.un Done

TestCase3602:
   ldc.i4 3702
   stloc.0
   ldc.i4 0x7ffffe71
   conv.i4
   not
   ldc.i4 0x8000018e
   bne.un Done

TestCase3601:
   ldc.i4 3701
   stloc.0
   ldc.i4 0x7ffffe70
   conv.i4
   not
   ldc.i4 0x8000018f
   bne.un Done

TestCase3600:
   ldc.i4 3700
   stloc.0
   ldc.i4 0x7ffffe6f
   conv.i4
   not
   ldc.i4 0x80000190
   beq TestCase3599
   br Done

TestCase3599:
   ldc.i4 3699
   stloc.0
   ldc.i4 0x7ffffe6e
   conv.i4
   not
   ldc.i4 0x80000191
   beq TestCase3598
   br Done

TestCase3598:
   ldc.i4 3698
   stloc.0
   ldc.i4 0x7ffffe6d
   conv.i4
   not
   ldc.i4 0x80000192
   beq TestCase3597
   br Done

TestCase3597:
   ldc.i4 3697
   stloc.0
   ldc.i4 0x7ffffe6c
   conv.i4
   not
   ldc.i4 0x80000193
   bne.un Done

TestCase3596:
   ldc.i4 3696
   stloc.0
   ldc.i4 0x7ffffe6b
   conv.i4
   not
   ldc.i4 0x80000194
   bne.un Done

TestCase3595:
   ldc.i4 3695
   stloc.0
   ldc.i4 0x7ffffe6a
   conv.i4
   not
   ldc.i4 0x80000195
   beq TestCase3594
   br Done

TestCase3594:
   ldc.i4 3694
   stloc.0
   ldc.i4 0x7ffffe69
   conv.i4
   not
   ldc.i4 0x80000196
   beq TestCase3593
   br Done

TestCase3593:
   ldc.i4 3693
   stloc.0
   ldc.i4 0x7ffffe68
   conv.i4
   not
   ldc.i4 0x80000197
   bne.un Done

TestCase3592:
   ldc.i4 3692
   stloc.0
   ldc.i4 0x7ffffe67
   conv.i4
   not
   ldc.i4 0x80000198
   beq TestCase3591
   br Done

TestCase3591:
   ldc.i4 3691
   stloc.0
   ldc.i4 0x7ffffe66
   conv.i4
   not
   ldc.i4 0x80000199
   bne.un Done

TestCase3590:
   ldc.i4 3690
   stloc.0
   ldc.i4 0x7ffffe65
   conv.i4
   not
   ldc.i4 0x8000019a
   beq TestCase3589
   br Done

TestCase3589:
   ldc.i4 3689
   stloc.0
   ldc.i4 0x7ffffe64
   conv.i4
   not
   ldc.i4 0x8000019b
   beq TestCase3588
   br Done

TestCase3588:
   ldc.i4 3688
   stloc.0
   ldc.i4 0x7ffffe63
   conv.i4
   not
   ldc.i4 0x8000019c
   beq TestCase3587
   br Done

TestCase3587:
   ldc.i4 3687
   stloc.0
   ldc.i4 0x7ffffe62
   conv.i4
   not
   ldc.i4 0x8000019d
   bne.un Done

TestCase3586:
   ldc.i4 3686
   stloc.0
   ldc.i4 0x7ffffe61
   conv.i4
   not
   ldc.i4 0x8000019e
   beq TestCase3585
   br Done

TestCase3585:
   ldc.i4 3685
   stloc.0
   ldc.i4 0x7ffffe60
   conv.i4
   not
   ldc.i4 0x8000019f
   beq TestCase3584
   br Done

TestCase3584:
   ldc.i4 3684
   stloc.0
   ldc.i4 0x7ffffe5f
   conv.i4
   not
   ldc.i4 0x800001a0
   beq TestCase3583
   br Done

TestCase3583:
   ldc.i4 3683
   stloc.0
   ldc.i4 0x7ffffe5e
   conv.i4
   not
   ldc.i4 0x800001a1
   bne.un Done

TestCase3582:
   ldc.i4 3682
   stloc.0
   ldc.i4 0x7ffffe5d
   conv.i4
   not
   ldc.i4 0x800001a2
   beq TestCase3581
   br Done

TestCase3581:
   ldc.i4 3681
   stloc.0
   ldc.i4 0x7ffffe5c
   conv.i4
   not
   ldc.i4 0x800001a3
   beq TestCase3580
   br Done

TestCase3580:
   ldc.i4 3680
   stloc.0
   ldc.i4 0x7ffffe5b
   conv.i4
   not
   ldc.i4 0x800001a4
   beq TestCase3579
   br Done

TestCase3579:
   ldc.i4 3679
   stloc.0
   ldc.i4 0x7ffffe5a
   conv.i4
   not
   ldc.i4 0x800001a5
   bne.un Done

TestCase3578:
   ldc.i4 3678
   stloc.0
   ldc.i4 0x7ffffe59
   conv.i4
   not
   ldc.i4 0x800001a6
   bne.un Done

TestCase3577:
   ldc.i4 3677
   stloc.0
   ldc.i4 0x7ffffe58
   conv.i4
   not
   ldc.i4 0x800001a7
   bne.un Done

TestCase3576:
   ldc.i4 3676
   stloc.0
   ldc.i4 0x7ffffe57
   conv.i4
   not
   ldc.i4 0x800001a8
   beq TestCase3575
   br Done

TestCase3575:
   ldc.i4 3675
   stloc.0
   ldc.i4 0x7ffffe56
   conv.i4
   not
   ldc.i4 0x800001a9
   beq TestCase3574
   br Done

TestCase3574:
   ldc.i4 3674
   stloc.0
   ldc.i4 0x7ffffe55
   conv.i4
   not
   ldc.i4 0x800001aa
   bne.un Done

TestCase3573:
   ldc.i4 3673
   stloc.0
   ldc.i4 0x7ffffe54
   conv.i4
   not
   ldc.i4 0x800001ab
   bne.un Done

TestCase3572:
   ldc.i4 3672
   stloc.0
   ldc.i4 0x7ffffe53
   conv.i4
   not
   ldc.i4 0x800001ac
   beq TestCase3571
   br Done

TestCase3571:
   ldc.i4 3671
   stloc.0
   ldc.i4 0x7ffffe52
   conv.i4
   not
   ldc.i4 0x800001ad
   bne.un Done

TestCase3570:
   ldc.i4 3670
   stloc.0
   ldc.i4 0x7ffffe51
   conv.i4
   not
   ldc.i4 0x800001ae
   bne.un Done

TestCase3569:
   ldc.i4 3669
   stloc.0
   ldc.i4 0x7ffffe50
   conv.i4
   not
   ldc.i4 0x800001af
   beq TestCase3568
   br Done

TestCase3568:
   ldc.i4 3668
   stloc.0
   ldc.i4 0x7ffffe4f
   conv.i4
   not
   ldc.i4 0x800001b0
   beq TestCase3567
   br Done

TestCase3567:
   ldc.i4 3667
   stloc.0
   ldc.i4 0x7ffffe4e
   conv.i4
   not
   ldc.i4 0x800001b1
   beq TestCase3566
   br Done

TestCase3566:
   ldc.i4 3666
   stloc.0
   ldc.i4 0x7ffffe4d
   conv.i4
   not
   ldc.i4 0x800001b2
   beq TestCase3565
   br Done

TestCase3565:
   ldc.i4 3665
   stloc.0
   ldc.i4 0x7ffffe4c
   conv.i4
   not
   ldc.i4 0x800001b3
   bne.un Done

TestCase3564:
   ldc.i4 3664
   stloc.0
   ldc.i4 0x7ffffe4b
   conv.i4
   not
   ldc.i4 0x800001b4
   bne.un Done

TestCase3563:
   ldc.i4 3663
   stloc.0
   ldc.i4 0x7ffffe4a
   conv.i4
   not
   ldc.i4 0x800001b5
   bne.un Done

TestCase3562:
   ldc.i4 3662
   stloc.0
   ldc.i4 0x7ffffe49
   conv.i4
   not
   ldc.i4 0x800001b6
   beq TestCase3561
   br Done

TestCase3561:
   ldc.i4 3661
   stloc.0
   ldc.i4 0x7ffffe48
   conv.i4
   not
   ldc.i4 0x800001b7
   bne.un Done

TestCase3560:
   ldc.i4 3660
   stloc.0
   ldc.i4 0x7ffffe47
   conv.i4
   not
   ldc.i4 0x800001b8
   beq TestCase3559
   br Done

TestCase3559:
   ldc.i4 3659
   stloc.0
   ldc.i4 0x7ffffe46
   conv.i4
   not
   ldc.i4 0x800001b9
   bne.un Done

TestCase3558:
   ldc.i4 3658
   stloc.0
   ldc.i4 0x7ffffe45
   conv.i4
   not
   ldc.i4 0x800001ba
   bne.un Done

TestCase3557:
   ldc.i4 3657
   stloc.0
   ldc.i4 0x7ffffe44
   conv.i4
   not
   ldc.i4 0x800001bb
   bne.un Done

TestCase3556:
   ldc.i4 3656
   stloc.0
   ldc.i4 0x7ffffe43
   conv.i4
   not
   ldc.i4 0x800001bc
   beq TestCase3555
   br Done

TestCase3555:
   ldc.i4 3655
   stloc.0
   ldc.i4 0x7ffffe42
   conv.i4
   not
   ldc.i4 0x800001bd
   bne.un Done

TestCase3554:
   ldc.i4 3654
   stloc.0
   ldc.i4 0x7ffffe41
   conv.i4
   not
   ldc.i4 0x800001be
   bne.un Done

TestCase3553:
   ldc.i4 3653
   stloc.0
   ldc.i4 0x7ffffe40
   conv.i4
   not
   ldc.i4 0x800001bf
   bne.un Done

TestCase3552:
   ldc.i4 3652
   stloc.0
   ldc.i4 0x7ffffe3f
   conv.i4
   not
   ldc.i4 0x800001c0
   beq TestCase3551
   br Done

TestCase3551:
   ldc.i4 3651
   stloc.0
   ldc.i4 0x7ffffe3e
   conv.i4
   not
   ldc.i4 0x800001c1
   bne.un Done

TestCase3550:
   ldc.i4 3650
   stloc.0
   ldc.i4 0x7ffffe3d
   conv.i4
   not
   ldc.i4 0x800001c2
   beq TestCase3549
   br Done

TestCase3549:
   ldc.i4 3649
   stloc.0
   ldc.i4 0x7ffffe3c
   conv.i4
   not
   ldc.i4 0x800001c3
   beq TestCase3548
   br Done

TestCase3548:
   ldc.i4 3648
   stloc.0
   ldc.i4 0x7ffffe3b
   conv.i4
   not
   ldc.i4 0x800001c4
   bne.un Done

TestCase3547:
   ldc.i4 3647
   stloc.0
   ldc.i4 0x7ffffe3a
   conv.i4
   not
   ldc.i4 0x800001c5
   beq TestCase3546
   br Done

TestCase3546:
   ldc.i4 3646
   stloc.0
   ldc.i4 0x7ffffe39
   conv.i4
   not
   ldc.i4 0x800001c6
   bne.un Done

TestCase3545:
   ldc.i4 3645
   stloc.0
   ldc.i4 0x7ffffe38
   conv.i4
   not
   ldc.i4 0x800001c7
   beq TestCase3544
   br Done

TestCase3544:
   ldc.i4 3644
   stloc.0
   ldc.i4 0x7ffffe37
   conv.i4
   not
   ldc.i4 0x800001c8
   bne.un Done

TestCase3543:
   ldc.i4 3643
   stloc.0
   ldc.i4 0x7ffffe36
   conv.i4
   not
   ldc.i4 0x800001c9
   beq TestCase3542
   br Done

TestCase3542:
   ldc.i4 3642
   stloc.0
   ldc.i4 0x7ffffe35
   conv.i4
   not
   ldc.i4 0x800001ca
   beq TestCase3541
   br Done

TestCase3541:
   ldc.i4 3641
   stloc.0
   ldc.i4 0x7ffffe34
   conv.i4
   not
   ldc.i4 0x800001cb
   bne.un Done

TestCase3540:
   ldc.i4 3640
   stloc.0
   ldc.i4 0x7ffffe33
   conv.i4
   not
   ldc.i4 0x800001cc
   beq TestCase3539
   br Done

TestCase3539:
   ldc.i4 3639
   stloc.0
   ldc.i4 0x7ffffe32
   conv.i4
   not
   ldc.i4 0x800001cd
   beq TestCase3538
   br Done

TestCase3538:
   ldc.i4 3638
   stloc.0
   ldc.i4 0x7ffffe31
   conv.i4
   not
   ldc.i4 0x800001ce
   beq TestCase3537
   br Done

TestCase3537:
   ldc.i4 3637
   stloc.0
   ldc.i4 0x7ffffe30
   conv.i4
   not
   ldc.i4 0x800001cf
   beq TestCase3536
   br Done

TestCase3536:
   ldc.i4 3636
   stloc.0
   ldc.i4 0x7ffffe2f
   conv.i4
   not
   ldc.i4 0x800001d0
   bne.un Done

TestCase3535:
   ldc.i4 3635
   stloc.0
   ldc.i4 0x7ffffe2e
   conv.i4
   not
   ldc.i4 0x800001d1
   beq TestCase3534
   br Done

TestCase3534:
   ldc.i4 3634
   stloc.0
   ldc.i4 0x7ffffe2d
   conv.i4
   not
   ldc.i4 0x800001d2
   beq TestCase3533
   br Done

TestCase3533:
   ldc.i4 3633
   stloc.0
   ldc.i4 0x7ffffe2c
   conv.i4
   not
   ldc.i4 0x800001d3
   beq TestCase3532
   br Done

TestCase3532:
   ldc.i4 3632
   stloc.0
   ldc.i4 0x7ffffe2b
   conv.i4
   not
   ldc.i4 0x800001d4
   bne.un Done

TestCase3531:
   ldc.i4 3631
   stloc.0
   ldc.i4 0x7ffffe2a
   conv.i4
   not
   ldc.i4 0x800001d5
   bne.un Done

TestCase3530:
   ldc.i4 3630
   stloc.0
   ldc.i4 0x7ffffe29
   conv.i4
   not
   ldc.i4 0x800001d6
   bne.un Done

TestCase3529:
   ldc.i4 3629
   stloc.0
   ldc.i4 0x7ffffe28
   conv.i4
   not
   ldc.i4 0x800001d7
   beq TestCase3528
   br Done

TestCase3528:
   ldc.i4 3628
   stloc.0
   ldc.i4 0x7ffffe27
   conv.i4
   not
   ldc.i4 0x800001d8
   beq TestCase3527
   br Done

TestCase3527:
   ldc.i4 3627
   stloc.0
   ldc.i4 0x7ffffe26
   conv.i4
   not
   ldc.i4 0x800001d9
   beq TestCase3526
   br Done

TestCase3526:
   ldc.i4 3626
   stloc.0
   ldc.i4 0x7ffffe25
   conv.i4
   not
   ldc.i4 0x800001da
   beq TestCase3525
   br Done

TestCase3525:
   ldc.i4 3625
   stloc.0
   ldc.i4 0x7ffffe24
   conv.i4
   not
   ldc.i4 0x800001db
   beq TestCase3524
   br Done

TestCase3524:
   ldc.i4 3624
   stloc.0
   ldc.i4 0x7ffffe23
   conv.i4
   not
   ldc.i4 0x800001dc
   bne.un Done

TestCase3523:
   ldc.i4 3623
   stloc.0
   ldc.i4 0x7ffffe22
   conv.i4
   not
   ldc.i4 0x800001dd
   beq TestCase3522
   br Done

TestCase3522:
   ldc.i4 3622
   stloc.0
   ldc.i4 0x7ffffe21
   conv.i4
   not
   ldc.i4 0x800001de
   beq TestCase3521
   br Done

TestCase3521:
   ldc.i4 3621
   stloc.0
   ldc.i4 0x7ffffe20
   conv.i4
   not
   ldc.i4 0x800001df
   bne.un Done

TestCase3520:
   ldc.i4 3620
   stloc.0
   ldc.i4 0x7ffffe1f
   conv.i4
   not
   ldc.i4 0x800001e0
   beq TestCase3519
   br Done

TestCase3519:
   ldc.i4 3619
   stloc.0
   ldc.i4 0x7ffffe1e
   conv.i4
   not
   ldc.i4 0x800001e1
   bne.un Done

TestCase3518:
   ldc.i4 3618
   stloc.0
   ldc.i4 0x7ffffe1d
   conv.i4
   not
   ldc.i4 0x800001e2
   beq TestCase3517
   br Done

TestCase3517:
   ldc.i4 3617
   stloc.0
   ldc.i4 0x7ffffe1c
   conv.i4
   not
   ldc.i4 0x800001e3
   bne.un Done

TestCase3516:
   ldc.i4 3616
   stloc.0
   ldc.i4 0x7ffffe1b
   conv.i4
   not
   ldc.i4 0x800001e4
   bne.un Done

TestCase3515:
   ldc.i4 3615
   stloc.0
   ldc.i4 0x7ffffe1a
   conv.i4
   not
   ldc.i4 0x800001e5
   bne.un Done

TestCase3514:
   ldc.i4 3614
   stloc.0
   ldc.i4 0x7ffffe19
   conv.i4
   not
   ldc.i4 0x800001e6
   beq TestCase3513
   br Done

TestCase3513:
   ldc.i4 3613
   stloc.0
   ldc.i4 0x7ffffe18
   conv.i4
   not
   ldc.i4 0x800001e7
   beq TestCase3512
   br Done

TestCase3512:
   ldc.i4 3612
   stloc.0
   ldc.i4 0x7ffffe17
   conv.i4
   not
   ldc.i4 0x800001e8
   beq TestCase3511
   br Done

TestCase3511:
   ldc.i4 3611
   stloc.0
   ldc.i4 0x7ffffe16
   conv.i4
   not
   ldc.i4 0x800001e9
   beq TestCase3510
   br Done

TestCase3510:
   ldc.i4 3610
   stloc.0
   ldc.i4 0x7ffffe15
   conv.i4
   not
   ldc.i4 0x800001ea
   beq TestCase3509
   br Done

TestCase3509:
   ldc.i4 3609
   stloc.0
   ldc.i4 0x7ffffe14
   conv.i4
   not
   ldc.i4 0x800001eb
   bne.un Done

TestCase3508:
   ldc.i4 3608
   stloc.0
   ldc.i4 0x7ffffe13
   conv.i4
   not
   ldc.i4 0x800001ec
   beq TestCase3507
   br Done

TestCase3507:
   ldc.i4 3607
   stloc.0
   ldc.i4 0x7ffffe12
   conv.i4
   not
   ldc.i4 0x800001ed
   bne.un Done

TestCase3506:
   ldc.i4 3606
   stloc.0
   ldc.i4 0x7ffffe11
   conv.i4
   not
   ldc.i4 0x800001ee
   beq TestCase3505
   br Done

TestCase3505:
   ldc.i4 3605
   stloc.0
   ldc.i4 0x7ffffe10
   conv.i4
   not
   ldc.i4 0x800001ef
   bne.un Done

TestCase3504:
   ldc.i4 3604
   stloc.0
   ldc.i4 0x7ffffe0f
   conv.i4
   not
   ldc.i4 0x800001f0
   beq TestCase3503
   br Done

TestCase3503:
   ldc.i4 3603
   stloc.0
   ldc.i4 0x7ffffe0e
   conv.i4
   not
   ldc.i4 0x800001f1
   bne.un Done

TestCase3502:
   ldc.i4 3602
   stloc.0
   ldc.i4 0x7ffffe0d
   conv.i4
   not
   ldc.i4 0x800001f2
   beq TestCase3501
   br Done

TestCase3501:
   ldc.i4 3601
   stloc.0
   ldc.i4 0x7ffffe0c
   conv.i4
   not
   ldc.i4 0x800001f3
   beq TestCase3500
   br Done

TestCase3500:
   ldc.i4 3600
   stloc.0
   ldc.i4 0x7ffffe0b
   conv.i4
   not
   ldc.i4 0x800001f4
   beq TestCase3499
   br Done

TestCase3499:
   ldc.i4 3599
   stloc.0
   ldc.i4 0x7ffffe0a
   conv.i4
   not
   ldc.i4 0x800001f5
   bne.un Done

TestCase3498:
   ldc.i4 3598
   stloc.0
   ldc.i4 0x7ffffe09
   conv.i4
   not
   ldc.i4 0x800001f6
   bne.un Done

TestCase3497:
   ldc.i4 3597
   stloc.0
   ldc.i4 0x7ffffe08
   conv.i4
   not
   ldc.i4 0x800001f7
   beq TestCase3496
   br Done

TestCase3496:
   ldc.i4 3596
   stloc.0
   ldc.i4 0x7ffffe07
   conv.i4
   not
   ldc.i4 0x800001f8
   beq TestCase3495
   br Done

TestCase3495:
   ldc.i4 3595
   stloc.0
   ldc.i4 0x7ffffe06
   conv.i4
   not
   ldc.i4 0x800001f9
   beq TestCase3494
   br Done

TestCase3494:
   ldc.i4 3594
   stloc.0
   ldc.i4 0x7ffffe05
   conv.i4
   not
   ldc.i4 0x800001fa
   bne.un Done

TestCase3493:
   ldc.i4 3593
   stloc.0
   ldc.i4 0x7ffffe04
   conv.i4
   not
   ldc.i4 0x800001fb
   beq TestCase3492
   br Done

TestCase3492:
   ldc.i4 3592
   stloc.0
   ldc.i4 0x7ffffe03
   conv.i4
   not
   ldc.i4 0x800001fc
   bne.un Done

TestCase3491:
   ldc.i4 3591
   stloc.0
   ldc.i4 0x7ffffe02
   conv.i4
   not
   ldc.i4 0x800001fd
   bne.un Done

TestCase3490:
   ldc.i4 3590
   stloc.0
   ldc.i4 0x7ffffe01
   conv.i4
   not
   ldc.i4 0x800001fe
   beq TestCase3489
   br Done

TestCase3489:
   ldc.i4 3589
   stloc.0
   ldc.i4 0x7ffffe00
   conv.i4
   not
   ldc.i4 0x800001ff
   beq TestCase3488
   br Done

TestCase3488:
   ldc.i4 3588
   stloc.0
   ldc.i4 0x7ffffdff
   conv.i4
   not
   ldc.i4 0x80000200
   beq TestCase3487
   br Done

TestCase3487:
   ldc.i4 3587
   stloc.0
   ldc.i4 0x7ffffdfe
   conv.i4
   not
   ldc.i4 0x80000201
   beq TestCase3486
   br Done

TestCase3486:
   ldc.i4 3586
   stloc.0
   ldc.i4 0x7ffffdfd
   conv.i4
   not
   ldc.i4 0x80000202
   beq TestCase3485
   br Done

TestCase3485:
   ldc.i4 3585
   stloc.0
   ldc.i4 0x7ffffdfc
   conv.i4
   not
   ldc.i4 0x80000203
   bne.un Done

TestCase3484:
   ldc.i4 3584
   stloc.0
   ldc.i4 0x7ffffdfb
   conv.i4
   not
   ldc.i4 0x80000204
   beq TestCase3483
   br Done

TestCase3483:
   ldc.i4 3583
   stloc.0
   ldc.i4 0x7ffffdfa
   conv.i4
   not
   ldc.i4 0x80000205
   bne.un Done

TestCase3482:
   ldc.i4 3582
   stloc.0
   ldc.i4 0x7ffffdf9
   conv.i4
   not
   ldc.i4 0x80000206
   bne.un Done

TestCase3481:
   ldc.i4 3581
   stloc.0
   ldc.i4 0x7ffffdf8
   conv.i4
   not
   ldc.i4 0x80000207
   bne.un Done

TestCase3480:
   ldc.i4 3580
   stloc.0
   ldc.i4 0x7ffffdf7
   conv.i4
   not
   ldc.i4 0x80000208
   beq TestCase3479
   br Done

TestCase3479:
   ldc.i4 3579
   stloc.0
   ldc.i4 0x7ffffdf6
   conv.i4
   not
   ldc.i4 0x80000209
   beq TestCase3478
   br Done

TestCase3478:
   ldc.i4 3578
   stloc.0
   ldc.i4 0x7ffffdf5
   conv.i4
   not
   ldc.i4 0x8000020a
   bne.un Done

TestCase3477:
   ldc.i4 3577
   stloc.0
   ldc.i4 0x7ffffdf4
   conv.i4
   not
   ldc.i4 0x8000020b
   bne.un Done

TestCase3476:
   ldc.i4 3576
   stloc.0
   ldc.i4 0x7ffffdf3
   conv.i4
   not
   ldc.i4 0x8000020c
   beq TestCase3475
   br Done

TestCase3475:
   ldc.i4 3575
   stloc.0
   ldc.i4 0x7ffffdf2
   conv.i4
   not
   ldc.i4 0x8000020d
   bne.un Done

TestCase3474:
   ldc.i4 3574
   stloc.0
   ldc.i4 0x7ffffdf1
   conv.i4
   not
   ldc.i4 0x8000020e
   bne.un Done

TestCase3473:
   ldc.i4 3573
   stloc.0
   ldc.i4 0x7ffffdf0
   conv.i4
   not
   ldc.i4 0x8000020f
   bne.un Done

TestCase3472:
   ldc.i4 3572
   stloc.0
   ldc.i4 0x7ffffdef
   conv.i4
   not
   ldc.i4 0x80000210
   bne.un Done

TestCase3471:
   ldc.i4 3571
   stloc.0
   ldc.i4 0x7ffffdee
   conv.i4
   not
   ldc.i4 0x80000211
   beq TestCase3470
   br Done

TestCase3470:
   ldc.i4 3570
   stloc.0
   ldc.i4 0x7ffffded
   conv.i4
   not
   ldc.i4 0x80000212
   beq TestCase3469
   br Done

TestCase3469:
   ldc.i4 3569
   stloc.0
   ldc.i4 0x7ffffdec
   conv.i4
   not
   ldc.i4 0x80000213
   bne.un Done

TestCase3468:
   ldc.i4 3568
   stloc.0
   ldc.i4 0x7ffffdeb
   conv.i4
   not
   ldc.i4 0x80000214
   bne.un Done

TestCase3467:
   ldc.i4 3567
   stloc.0
   ldc.i4 0x7ffffdea
   conv.i4
   not
   ldc.i4 0x80000215
   bne.un Done

TestCase3466:
   ldc.i4 3566
   stloc.0
   ldc.i4 0x7ffffde9
   conv.i4
   not
   ldc.i4 0x80000216
   beq TestCase3465
   br Done

TestCase3465:
   ldc.i4 3565
   stloc.0
   ldc.i4 0x7ffffde8
   conv.i4
   not
   ldc.i4 0x80000217
   beq TestCase3464
   br Done

TestCase3464:
   ldc.i4 3564
   stloc.0
   ldc.i4 0x7ffffde7
   conv.i4
   not
   ldc.i4 0x80000218
   beq TestCase3463
   br Done

TestCase3463:
   ldc.i4 3563
   stloc.0
   ldc.i4 0x7ffffde6
   conv.i4
   not
   ldc.i4 0x80000219
   beq TestCase3462
   br Done

TestCase3462:
   ldc.i4 3562
   stloc.0
   ldc.i4 0x7ffffde5
   conv.i4
   not
   ldc.i4 0x8000021a
   bne.un Done

TestCase3461:
   ldc.i4 3561
   stloc.0
   ldc.i4 0x7ffffde4
   conv.i4
   not
   ldc.i4 0x8000021b
   beq TestCase3460
   br Done

TestCase3460:
   ldc.i4 3560
   stloc.0
   ldc.i4 0x7ffffde3
   conv.i4
   not
   ldc.i4 0x8000021c
   beq TestCase3459
   br Done

TestCase3459:
   ldc.i4 3559
   stloc.0
   ldc.i4 0x7ffffde2
   conv.i4
   not
   ldc.i4 0x8000021d
   beq TestCase3458
   br Done

TestCase3458:
   ldc.i4 3558
   stloc.0
   ldc.i4 0x7ffffde1
   conv.i4
   not
   ldc.i4 0x8000021e
   beq TestCase3457
   br Done

TestCase3457:
   ldc.i4 3557
   stloc.0
   ldc.i4 0x7ffffde0
   conv.i4
   not
   ldc.i4 0x8000021f
   beq TestCase3456
   br Done

TestCase3456:
   ldc.i4 3556
   stloc.0
   ldc.i4 0x7ffffddf
   conv.i4
   not
   ldc.i4 0x80000220
   bne.un Done

TestCase3455:
   ldc.i4 3555
   stloc.0
   ldc.i4 0x7ffffdde
   conv.i4
   not
   ldc.i4 0x80000221
   bne.un Done

TestCase3454:
   ldc.i4 3554
   stloc.0
   ldc.i4 0x7ffffddd
   conv.i4
   not
   ldc.i4 0x80000222
   beq TestCase3453
   br Done

TestCase3453:
   ldc.i4 3553
   stloc.0
   ldc.i4 0x7ffffddc
   conv.i4
   not
   ldc.i4 0x80000223
   bne.un Done

TestCase3452:
   ldc.i4 3552
   stloc.0
   ldc.i4 0x7ffffddb
   conv.i4
   not
   ldc.i4 0x80000224
   beq TestCase3451
   br Done

TestCase3451:
   ldc.i4 3551
   stloc.0
   ldc.i4 0x7ffffdda
   conv.i4
   not
   ldc.i4 0x80000225
   bne.un Done

TestCase3450:
   ldc.i4 3550
   stloc.0
   ldc.i4 0x7ffffdd9
   conv.i4
   not
   ldc.i4 0x80000226
   bne.un Done

TestCase3449:
   ldc.i4 3549
   stloc.0
   ldc.i4 0x7ffffdd8
   conv.i4
   not
   ldc.i4 0x80000227
   beq TestCase3448
   br Done

TestCase3448:
   ldc.i4 3548
   stloc.0
   ldc.i4 0x7ffffdd7
   conv.i4
   not
   ldc.i4 0x80000228
   bne.un Done

TestCase3447:
   ldc.i4 3547
   stloc.0
   ldc.i4 0x7ffffdd6
   conv.i4
   not
   ldc.i4 0x80000229
   bne.un Done

TestCase3446:
   ldc.i4 3546
   stloc.0
   ldc.i4 0x7ffffdd5
   conv.i4
   not
   ldc.i4 0x8000022a
   bne.un Done

TestCase3445:
   ldc.i4 3545
   stloc.0
   ldc.i4 0x7ffffdd4
   conv.i4
   not
   ldc.i4 0x8000022b
   beq TestCase3444
   br Done

TestCase3444:
   ldc.i4 3544
   stloc.0
   ldc.i4 0x7ffffdd3
   conv.i4
   not
   ldc.i4 0x8000022c
   beq TestCase3443
   br Done

TestCase3443:
   ldc.i4 3543
   stloc.0
   ldc.i4 0x7ffffdd2
   conv.i4
   not
   ldc.i4 0x8000022d
   bne.un Done

TestCase3442:
   ldc.i4 3542
   stloc.0
   ldc.i4 0x7ffffdd1
   conv.i4
   not
   ldc.i4 0x8000022e
   bne.un Done

TestCase3441:
   ldc.i4 3541
   stloc.0
   ldc.i4 0x7ffffdd0
   conv.i4
   not
   ldc.i4 0x8000022f
   beq TestCase3440
   br Done

TestCase3440:
   ldc.i4 3540
   stloc.0
   ldc.i4 0x7ffffdcf
   conv.i4
   not
   ldc.i4 0x80000230
   beq TestCase3439
   br Done

TestCase3439:
   ldc.i4 3539
   stloc.0
   ldc.i4 0x7ffffdce
   conv.i4
   not
   ldc.i4 0x80000231
   bne.un Done

TestCase3438:
   ldc.i4 3538
   stloc.0
   ldc.i4 0x7ffffdcd
   conv.i4
   not
   ldc.i4 0x80000232
   bne.un Done

TestCase3437:
   ldc.i4 3537
   stloc.0
   ldc.i4 0x7ffffdcc
   conv.i4
   not
   ldc.i4 0x80000233
   beq TestCase3436
   br Done

TestCase3436:
   ldc.i4 3536
   stloc.0
   ldc.i4 0x7ffffdcb
   conv.i4
   not
   ldc.i4 0x80000234
   bne.un Done

TestCase3435:
   ldc.i4 3535
   stloc.0
   ldc.i4 0x7ffffdca
   conv.i4
   not
   ldc.i4 0x80000235
   bne.un Done

TestCase3434:
   ldc.i4 3534
   stloc.0
   ldc.i4 0x7ffffdc9
   conv.i4
   not
   ldc.i4 0x80000236
   beq TestCase3433
   br Done

TestCase3433:
   ldc.i4 3533
   stloc.0
   ldc.i4 0x7ffffdc8
   conv.i4
   not
   ldc.i4 0x80000237
   bne.un Done

TestCase3432:
   ldc.i4 3532
   stloc.0
   ldc.i4 0x7ffffdc7
   conv.i4
   not
   ldc.i4 0x80000238
   bne.un Done

TestCase3431:
   ldc.i4 3531
   stloc.0
   ldc.i4 0x7ffffdc6
   conv.i4
   not
   ldc.i4 0x80000239
   bne.un Done

TestCase3430:
   ldc.i4 3530
   stloc.0
   ldc.i4 0x7ffffdc5
   conv.i4
   not
   ldc.i4 0x8000023a
   beq TestCase3429
   br Done

TestCase3429:
   ldc.i4 3529
   stloc.0
   ldc.i4 0x7ffffdc4
   conv.i4
   not
   ldc.i4 0x8000023b
   bne.un Done

TestCase3428:
   ldc.i4 3528
   stloc.0
   ldc.i4 0x7ffffdc3
   conv.i4
   not
   ldc.i4 0x8000023c
   beq TestCase3427
   br Done

TestCase3427:
   ldc.i4 3527
   stloc.0
   ldc.i4 0x7ffffdc2
   conv.i4
   not
   ldc.i4 0x8000023d
   beq TestCase3426
   br Done

TestCase3426:
   ldc.i4 3526
   stloc.0
   ldc.i4 0x7ffffdc1
   conv.i4
   not
   ldc.i4 0x8000023e
   bne.un Done

TestCase3425:
   ldc.i4 3525
   stloc.0
   ldc.i4 0x7ffffdc0
   conv.i4
   not
   ldc.i4 0x8000023f
   beq TestCase3424
   br Done

TestCase3424:
   ldc.i4 3524
   stloc.0
   ldc.i4 0x7ffffdbf
   conv.i4
   not
   ldc.i4 0x80000240
   bne.un Done

TestCase3423:
   ldc.i4 3523
   stloc.0
   ldc.i4 0x7ffffdbe
   conv.i4
   not
   ldc.i4 0x80000241
   bne.un Done

TestCase3422:
   ldc.i4 3522
   stloc.0
   ldc.i4 0x7ffffdbd
   conv.i4
   not
   ldc.i4 0x80000242
   beq TestCase3421
   br Done

TestCase3421:
   ldc.i4 3521
   stloc.0
   ldc.i4 0x7ffffdbc
   conv.i4
   not
   ldc.i4 0x80000243
   bne.un Done

TestCase3420:
   ldc.i4 3520
   stloc.0
   ldc.i4 0x7ffffdbb
   conv.i4
   not
   ldc.i4 0x80000244
   beq TestCase3419
   br Done

TestCase3419:
   ldc.i4 3519
   stloc.0
   ldc.i4 0x7ffffdba
   conv.i4
   not
   ldc.i4 0x80000245
   beq TestCase3418
   br Done

TestCase3418:
   ldc.i4 3518
   stloc.0
   ldc.i4 0x7ffffdb9
   conv.i4
   not
   ldc.i4 0x80000246
   bne.un Done

TestCase3417:
   ldc.i4 3517
   stloc.0
   ldc.i4 0x7ffffdb8
   conv.i4
   not
   ldc.i4 0x80000247
   bne.un Done

TestCase3416:
   ldc.i4 3516
   stloc.0
   ldc.i4 0x7ffffdb7
   conv.i4
   not
   ldc.i4 0x80000248
   bne.un Done

TestCase3415:
   ldc.i4 3515
   stloc.0
   ldc.i4 0x7ffffdb6
   conv.i4
   not
   ldc.i4 0x80000249
   bne.un Done

TestCase3414:
   ldc.i4 3514
   stloc.0
   ldc.i4 0x7ffffdb5
   conv.i4
   not
   ldc.i4 0x8000024a
   bne.un Done

TestCase3413:
   ldc.i4 3513
   stloc.0
   ldc.i4 0x7ffffdb4
   conv.i4
   not
   ldc.i4 0x8000024b
   bne.un Done

TestCase3412:
   ldc.i4 3512
   stloc.0
   ldc.i4 0x7ffffdb3
   conv.i4
   not
   ldc.i4 0x8000024c
   beq TestCase3411
   br Done

TestCase3411:
   ldc.i4 3511
   stloc.0
   ldc.i4 0x7ffffdb2
   conv.i4
   not
   ldc.i4 0x8000024d
   bne.un Done

TestCase3410:
   ldc.i4 3510
   stloc.0
   ldc.i4 0x7ffffdb1
   conv.i4
   not
   ldc.i4 0x8000024e
   bne.un Done

TestCase3409:
   ldc.i4 3509
   stloc.0
   ldc.i4 0x7ffffdb0
   conv.i4
   not
   ldc.i4 0x8000024f
   beq TestCase3408
   br Done

TestCase3408:
   ldc.i4 3508
   stloc.0
   ldc.i4 0x7ffffdaf
   conv.i4
   not
   ldc.i4 0x80000250
   bne.un Done

TestCase3407:
   ldc.i4 3507
   stloc.0
   ldc.i4 0x7ffffdae
   conv.i4
   not
   ldc.i4 0x80000251
   beq TestCase3406
   br Done

TestCase3406:
   ldc.i4 3506
   stloc.0
   ldc.i4 0x7ffffdad
   conv.i4
   not
   ldc.i4 0x80000252
   bne.un Done

TestCase3405:
   ldc.i4 3505
   stloc.0
   ldc.i4 0x7ffffdac
   conv.i4
   not
   ldc.i4 0x80000253
   bne.un Done

TestCase3404:
   ldc.i4 3504
   stloc.0
   ldc.i4 0x7ffffdab
   conv.i4
   not
   ldc.i4 0x80000254
   beq TestCase3403
   br Done

TestCase3403:
   ldc.i4 3503
   stloc.0
   ldc.i4 0x7ffffdaa
   conv.i4
   not
   ldc.i4 0x80000255
   bne.un Done

TestCase3402:
   ldc.i4 3502
   stloc.0
   ldc.i4 0x7ffffda9
   conv.i4
   not
   ldc.i4 0x80000256
   bne.un Done

TestCase3401:
   ldc.i4 3501
   stloc.0
   ldc.i4 0x7ffffda8
   conv.i4
   not
   ldc.i4 0x80000257
   bne.un Done

TestCase3400:
   ldc.i4 3500
   stloc.0
   ldc.i4 0x7ffffda7
   conv.i4
   not
   ldc.i4 0x80000258
   bne.un Done

TestCase3399:
   ldc.i4 3499
   stloc.0
   ldc.i4 0x7ffffda6
   conv.i4
   not
   ldc.i4 0x80000259
   bne.un Done

TestCase3398:
   ldc.i4 3498
   stloc.0
   ldc.i4 0x7ffffda5
   conv.i4
   not
   ldc.i4 0x8000025a
   beq TestCase3397
   br Done

TestCase3397:
   ldc.i4 3497
   stloc.0
   ldc.i4 0x7ffffda4
   conv.i4
   not
   ldc.i4 0x8000025b
   bne.un Done

TestCase3396:
   ldc.i4 3496
   stloc.0
   ldc.i4 0x7ffffda3
   conv.i4
   not
   ldc.i4 0x8000025c
   bne.un Done

TestCase3395:
   ldc.i4 3495
   stloc.0
   ldc.i4 0x7ffffda2
   conv.i4
   not
   ldc.i4 0x8000025d
   bne.un Done

TestCase3394:
   ldc.i4 3494
   stloc.0
   ldc.i4 0x7ffffda1
   conv.i4
   not
   ldc.i4 0x8000025e
   bne.un Done

TestCase3393:
   ldc.i4 3493
   stloc.0
   ldc.i4 0x7ffffda0
   conv.i4
   not
   ldc.i4 0x8000025f
   bne.un Done

TestCase3392:
   ldc.i4 3492
   stloc.0
   ldc.i4 0x7ffffd9f
   conv.i4
   not
   ldc.i4 0x80000260
   bne.un Done

TestCase3391:
   ldc.i4 3491
   stloc.0
   ldc.i4 0x7ffffd9e
   conv.i4
   not
   ldc.i4 0x80000261
   bne.un Done

TestCase3390:
   ldc.i4 3490
   stloc.0
   ldc.i4 0x7ffffd9d
   conv.i4
   not
   ldc.i4 0x80000262
   beq TestCase3389
   br Done

TestCase3389:
   ldc.i4 3489
   stloc.0
   ldc.i4 0x7ffffd9c
   conv.i4
   not
   ldc.i4 0x80000263
   bne.un Done

TestCase3388:
   ldc.i4 3488
   stloc.0
   ldc.i4 0x7ffffd9b
   conv.i4
   not
   ldc.i4 0x80000264
   bne.un Done

TestCase3387:
   ldc.i4 3487
   stloc.0
   ldc.i4 0x7ffffd9a
   conv.i4
   not
   ldc.i4 0x80000265
   beq TestCase3386
   br Done

TestCase3386:
   ldc.i4 3486
   stloc.0
   ldc.i4 0x7ffffd99
   conv.i4
   not
   ldc.i4 0x80000266
   beq TestCase3385
   br Done

TestCase3385:
   ldc.i4 3485
   stloc.0
   ldc.i4 0x7ffffd98
   conv.i4
   not
   ldc.i4 0x80000267
   beq TestCase3384
   br Done

TestCase3384:
   ldc.i4 3484
   stloc.0
   ldc.i4 0x7ffffd97
   conv.i4
   not
   ldc.i4 0x80000268
   beq TestCase3383
   br Done

TestCase3383:
   ldc.i4 3483
   stloc.0
   ldc.i4 0x7ffffd96
   conv.i4
   not
   ldc.i4 0x80000269
   beq TestCase3382
   br Done

TestCase3382:
   ldc.i4 3482
   stloc.0
   ldc.i4 0x7ffffd95
   conv.i4
   not
   ldc.i4 0x8000026a
   bne.un Done

TestCase3381:
   ldc.i4 3481
   stloc.0
   ldc.i4 0x7ffffd94
   conv.i4
   not
   ldc.i4 0x8000026b
   bne.un Done

TestCase3380:
   ldc.i4 3480
   stloc.0
   ldc.i4 0x7ffffd93
   conv.i4
   not
   ldc.i4 0x8000026c
   bne.un Done

TestCase3379:
   ldc.i4 3479
   stloc.0
   ldc.i4 0x7ffffd92
   conv.i4
   not
   ldc.i4 0x8000026d
   bne.un Done

TestCase3378:
   ldc.i4 3478
   stloc.0
   ldc.i4 0x7ffffd91
   conv.i4
   not
   ldc.i4 0x8000026e
   bne.un Done

TestCase3377:
   ldc.i4 3477
   stloc.0
   ldc.i4 0x7ffffd90
   conv.i4
   not
   ldc.i4 0x8000026f
   bne.un Done

TestCase3376:
   ldc.i4 3476
   stloc.0
   ldc.i4 0x7ffffd8f
   conv.i4
   not
   ldc.i4 0x80000270
   bne.un Done

TestCase3375:
   ldc.i4 3475
   stloc.0
   ldc.i4 0x7ffffd8e
   conv.i4
   not
   ldc.i4 0x80000271
   bne.un Done

TestCase3374:
   ldc.i4 3474
   stloc.0
   ldc.i4 0x7ffffd8d
   conv.i4
   not
   ldc.i4 0x80000272
   bne.un Done

TestCase3373:
   ldc.i4 3473
   stloc.0
   ldc.i4 0x7ffffd8c
   conv.i4
   not
   ldc.i4 0x80000273
   beq TestCase3372
   br Done

TestCase3372:
   ldc.i4 3472
   stloc.0
   ldc.i4 0x7ffffd8b
   conv.i4
   not
   ldc.i4 0x80000274
   bne.un Done

TestCase3371:
   ldc.i4 3471
   stloc.0
   ldc.i4 0x7ffffd8a
   conv.i4
   not
   ldc.i4 0x80000275
   beq TestCase3370
   br Done

TestCase3370:
   ldc.i4 3470
   stloc.0
   ldc.i4 0x7ffffd89
   conv.i4
   not
   ldc.i4 0x80000276
   bne.un Done

TestCase3369:
   ldc.i4 3469
   stloc.0
   ldc.i4 0x7ffffd88
   conv.i4
   not
   ldc.i4 0x80000277
   beq TestCase3368
   br Done

TestCase3368:
   ldc.i4 3468
   stloc.0
   ldc.i4 0x7ffffd87
   conv.i4
   not
   ldc.i4 0x80000278
   bne.un Done

TestCase3367:
   ldc.i4 3467
   stloc.0
   ldc.i4 0x7ffffd86
   conv.i4
   not
   ldc.i4 0x80000279
   bne.un Done

TestCase3366:
   ldc.i4 3466
   stloc.0
   ldc.i4 0x7ffffd85
   conv.i4
   not
   ldc.i4 0x8000027a
   beq TestCase3365
   br Done

TestCase3365:
   ldc.i4 3465
   stloc.0
   ldc.i4 0x7ffffd84
   conv.i4
   not
   ldc.i4 0x8000027b
   bne.un Done

TestCase3364:
   ldc.i4 3464
   stloc.0
   ldc.i4 0x7ffffd83
   conv.i4
   not
   ldc.i4 0x8000027c
   bne.un Done

TestCase3363:
   ldc.i4 3463
   stloc.0
   ldc.i4 0x7ffffd82
   conv.i4
   not
   ldc.i4 0x8000027d
   bne.un Done

TestCase3362:
   ldc.i4 3462
   stloc.0
   ldc.i4 0x7ffffd81
   conv.i4
   not
   ldc.i4 0x8000027e
   bne.un Done

TestCase3361:
   ldc.i4 3461
   stloc.0
   ldc.i4 0x7ffffd80
   conv.i4
   not
   ldc.i4 0x8000027f
   beq TestCase3360
   br Done

TestCase3360:
   ldc.i4 3460
   stloc.0
   ldc.i4 0x7ffffd7f
   conv.i4
   not
   ldc.i4 0x80000280
   bne.un Done

TestCase3359:
   ldc.i4 3459
   stloc.0
   ldc.i4 0x7ffffd7e
   conv.i4
   not
   ldc.i4 0x80000281
   bne.un Done

TestCase3358:
   ldc.i4 3458
   stloc.0
   ldc.i4 0x7ffffd7d
   conv.i4
   not
   ldc.i4 0x80000282
   bne.un Done

TestCase3357:
   ldc.i4 3457
   stloc.0
   ldc.i4 0x7ffffd7c
   conv.i4
   not
   ldc.i4 0x80000283
   beq TestCase3356
   br Done

TestCase3356:
   ldc.i4 3456
   stloc.0
   ldc.i4 0x7ffffd7b
   conv.i4
   not
   ldc.i4 0x80000284
   bne.un Done

TestCase3355:
   ldc.i4 3455
   stloc.0
   ldc.i4 0x7ffffd7a
   conv.i4
   not
   ldc.i4 0x80000285
   bne.un Done

TestCase3354:
   ldc.i4 3454
   stloc.0
   ldc.i4 0x7ffffd79
   conv.i4
   not
   ldc.i4 0x80000286
   beq TestCase3353
   br Done

TestCase3353:
   ldc.i4 3453
   stloc.0
   ldc.i4 0x7ffffd78
   conv.i4
   not
   ldc.i4 0x80000287
   bne.un Done

TestCase3352:
   ldc.i4 3452
   stloc.0
   ldc.i4 0x7ffffd77
   conv.i4
   not
   ldc.i4 0x80000288
   bne.un Done

TestCase3351:
   ldc.i4 3451
   stloc.0
   ldc.i4 0x7ffffd76
   conv.i4
   not
   ldc.i4 0x80000289
   beq TestCase3350
   br Done

TestCase3350:
   ldc.i4 3450
   stloc.0
   ldc.i4 0x7ffffd75
   conv.i4
   not
   ldc.i4 0x8000028a
   beq TestCase3349
   br Done

TestCase3349:
   ldc.i4 3449
   stloc.0
   ldc.i4 0x7ffffd74
   conv.i4
   not
   ldc.i4 0x8000028b
   beq TestCase3348
   br Done

TestCase3348:
   ldc.i4 3448
   stloc.0
   ldc.i4 0x7ffffd73
   conv.i4
   not
   ldc.i4 0x8000028c
   beq TestCase3347
   br Done

TestCase3347:
   ldc.i4 3447
   stloc.0
   ldc.i4 0x7ffffd72
   conv.i4
   not
   ldc.i4 0x8000028d
   beq TestCase3346
   br Done

TestCase3346:
   ldc.i4 3446
   stloc.0
   ldc.i4 0x7ffffd71
   conv.i4
   not
   ldc.i4 0x8000028e
   bne.un Done

TestCase3345:
   ldc.i4 3445
   stloc.0
   ldc.i4 0x7ffffd70
   conv.i4
   not
   ldc.i4 0x8000028f
   beq TestCase3344
   br Done

TestCase3344:
   ldc.i4 3444
   stloc.0
   ldc.i4 0x7ffffd6f
   conv.i4
   not
   ldc.i4 0x80000290
   bne.un Done

TestCase3343:
   ldc.i4 3443
   stloc.0
   ldc.i4 0x7ffffd6e
   conv.i4
   not
   ldc.i4 0x80000291
   beq TestCase3342
   br Done

TestCase3342:
   ldc.i4 3442
   stloc.0
   ldc.i4 0x7ffffd6d
   conv.i4
   not
   ldc.i4 0x80000292
   bne.un Done

TestCase3341:
   ldc.i4 3441
   stloc.0
   ldc.i4 0x7ffffd6c
   conv.i4
   not
   ldc.i4 0x80000293
   bne.un Done

TestCase3340:
   ldc.i4 3440
   stloc.0
   ldc.i4 0x7ffffd6b
   conv.i4
   not
   ldc.i4 0x80000294
   bne.un Done

TestCase3339:
   ldc.i4 3439
   stloc.0
   ldc.i4 0x7ffffd6a
   conv.i4
   not
   ldc.i4 0x80000295
   bne.un Done

TestCase3338:
   ldc.i4 3438
   stloc.0
   ldc.i4 0x7ffffd69
   conv.i4
   not
   ldc.i4 0x80000296
   bne.un Done

TestCase3337:
   ldc.i4 3437
   stloc.0
   ldc.i4 0x7ffffd68
   conv.i4
   not
   ldc.i4 0x80000297
   beq TestCase3336
   br Done

TestCase3336:
   ldc.i4 3436
   stloc.0
   ldc.i4 0x7ffffd67
   conv.i4
   not
   ldc.i4 0x80000298
   beq TestCase3335
   br Done

TestCase3335:
   ldc.i4 3435
   stloc.0
   ldc.i4 0x7ffffd66
   conv.i4
   not
   ldc.i4 0x80000299
   beq TestCase3334
   br Done

TestCase3334:
   ldc.i4 3434
   stloc.0
   ldc.i4 0x7ffffd65
   conv.i4
   not
   ldc.i4 0x8000029a
   bne.un Done

TestCase3333:
   ldc.i4 3433
   stloc.0
   ldc.i4 0x7ffffd64
   conv.i4
   not
   ldc.i4 0x8000029b
   bne.un Done

TestCase3332:
   ldc.i4 3432
   stloc.0
   ldc.i4 0x7ffffd63
   conv.i4
   not
   ldc.i4 0x8000029c
   bne.un Done

TestCase3331:
   ldc.i4 3431
   stloc.0
   ldc.i4 0x7ffffd62
   conv.i4
   not
   ldc.i4 0x8000029d
   beq TestCase3330
   br Done

TestCase3330:
   ldc.i4 3430
   stloc.0
   ldc.i4 0x7ffffd61
   conv.i4
   not
   ldc.i4 0x8000029e
   bne.un Done

TestCase3329:
   ldc.i4 3429
   stloc.0
   ldc.i4 0x7ffffd60
   conv.i4
   not
   ldc.i4 0x8000029f
   beq TestCase3328
   br Done

TestCase3328:
   ldc.i4 3428
   stloc.0
   ldc.i4 0x7ffffd5f
   conv.i4
   not
   ldc.i4 0x800002a0
   bne.un Done

TestCase3327:
   ldc.i4 3427
   stloc.0
   ldc.i4 0x7ffffd5e
   conv.i4
   not
   ldc.i4 0x800002a1
   bne.un Done

TestCase3326:
   ldc.i4 3426
   stloc.0
   ldc.i4 0x7ffffd5d
   conv.i4
   not
   ldc.i4 0x800002a2
   bne.un Done

TestCase3325:
   ldc.i4 3425
   stloc.0
   ldc.i4 0x7ffffd5c
   conv.i4
   not
   ldc.i4 0x800002a3
   beq TestCase3324
   br Done

TestCase3324:
   ldc.i4 3424
   stloc.0
   ldc.i4 0x7ffffd5b
   conv.i4
   not
   ldc.i4 0x800002a4
   bne.un Done

TestCase3323:
   ldc.i4 3423
   stloc.0
   ldc.i4 0x7ffffd5a
   conv.i4
   not
   ldc.i4 0x800002a5
   beq TestCase3322
   br Done

TestCase3322:
   ldc.i4 3422
   stloc.0
   ldc.i4 0x7ffffd59
   conv.i4
   not
   ldc.i4 0x800002a6
   beq TestCase3321
   br Done

TestCase3321:
   ldc.i4 3421
   stloc.0
   ldc.i4 0x7ffffd58
   conv.i4
   not
   ldc.i4 0x800002a7
   beq TestCase3320
   br Done

TestCase3320:
   ldc.i4 3420
   stloc.0
   ldc.i4 0x7ffffd57
   conv.i4
   not
   ldc.i4 0x800002a8
   bne.un Done

TestCase3319:
   ldc.i4 3419
   stloc.0
   ldc.i4 0x7ffffd56
   conv.i4
   not
   ldc.i4 0x800002a9
   beq TestCase3318
   br Done

TestCase3318:
   ldc.i4 3418
   stloc.0
   ldc.i4 0x7ffffd55
   conv.i4
   not
   ldc.i4 0x800002aa
   bne.un Done

TestCase3317:
   ldc.i4 3417
   stloc.0
   ldc.i4 0x7ffffd54
   conv.i4
   not
   ldc.i4 0x800002ab
   beq TestCase3316
   br Done

TestCase3316:
   ldc.i4 3416
   stloc.0
   ldc.i4 0x7ffffd53
   conv.i4
   not
   ldc.i4 0x800002ac
   bne.un Done

TestCase3315:
   ldc.i4 3415
   stloc.0
   ldc.i4 0x7ffffd52
   conv.i4
   not
   ldc.i4 0x800002ad
   beq TestCase3314
   br Done

TestCase3314:
   ldc.i4 3414
   stloc.0
   ldc.i4 0x7ffffd51
   conv.i4
   not
   ldc.i4 0x800002ae
   beq TestCase3313
   br Done

TestCase3313:
   ldc.i4 3413
   stloc.0
   ldc.i4 0x7ffffd50
   conv.i4
   not
   ldc.i4 0x800002af
   beq TestCase3312
   br Done

TestCase3312:
   ldc.i4 3412
   stloc.0
   ldc.i4 0x7ffffd4f
   conv.i4
   not
   ldc.i4 0x800002b0
   beq TestCase3311
   br Done

TestCase3311:
   ldc.i4 3411
   stloc.0
   ldc.i4 0x7ffffd4e
   conv.i4
   not
   ldc.i4 0x800002b1
   bne.un Done

TestCase3310:
   ldc.i4 3410
   stloc.0
   ldc.i4 0x7ffffd4d
   conv.i4
   not
   ldc.i4 0x800002b2
   bne.un Done

TestCase3309:
   ldc.i4 3409
   stloc.0
   ldc.i4 0x7ffffd4c
   conv.i4
   not
   ldc.i4 0x800002b3
   beq TestCase3308
   br Done

TestCase3308:
   ldc.i4 3408
   stloc.0
   ldc.i4 0x7ffffd4b
   conv.i4
   not
   ldc.i4 0x800002b4
   beq TestCase3307
   br Done

TestCase3307:
   ldc.i4 3407
   stloc.0
   ldc.i4 0x7ffffd4a
   conv.i4
   not
   ldc.i4 0x800002b5
   beq TestCase3306
   br Done

TestCase3306:
   ldc.i4 3406
   stloc.0
   ldc.i4 0x7ffffd49
   conv.i4
   not
   ldc.i4 0x800002b6
   beq TestCase3305
   br Done

TestCase3305:
   ldc.i4 3405
   stloc.0
   ldc.i4 0x7ffffd48
   conv.i4
   not
   ldc.i4 0x800002b7
   beq TestCase3304
   br Done

TestCase3304:
   ldc.i4 3404
   stloc.0
   ldc.i4 0x7ffffd47
   conv.i4
   not
   ldc.i4 0x800002b8
   beq TestCase3303
   br Done

TestCase3303:
   ldc.i4 3403
   stloc.0
   ldc.i4 0x7ffffd46
   conv.i4
   not
   ldc.i4 0x800002b9
   beq TestCase3302
   br Done

TestCase3302:
   ldc.i4 3402
   stloc.0
   ldc.i4 0x7ffffd45
   conv.i4
   not
   ldc.i4 0x800002ba
   bne.un Done

TestCase3301:
   ldc.i4 3401
   stloc.0
   ldc.i4 0x7ffffd44
   conv.i4
   not
   ldc.i4 0x800002bb
   beq TestCase3300
   br Done

TestCase3300:
   ldc.i4 3400
   stloc.0
   ldc.i4 0x7ffffd43
   conv.i4
   not
   ldc.i4 0x800002bc
   bne.un Done

TestCase3299:
   ldc.i4 3399
   stloc.0
   ldc.i4 0x7ffffd42
   conv.i4
   not
   ldc.i4 0x800002bd
   beq TestCase3298
   br Done

TestCase3298:
   ldc.i4 3398
   stloc.0
   ldc.i4 0x7ffffd41
   conv.i4
   not
   ldc.i4 0x800002be
   bne.un Done

TestCase3297:
   ldc.i4 3397
   stloc.0
   ldc.i4 0x7ffffd40
   conv.i4
   not
   ldc.i4 0x800002bf
   beq TestCase3296
   br Done

TestCase3296:
   ldc.i4 3396
   stloc.0
   ldc.i4 0x7ffffd3f
   conv.i4
   not
   ldc.i4 0x800002c0
   bne.un Done

TestCase3295:
   ldc.i4 3395
   stloc.0
   ldc.i4 0x7ffffd3e
   conv.i4
   not
   ldc.i4 0x800002c1
   bne.un Done

TestCase3294:
   ldc.i4 3394
   stloc.0
   ldc.i4 0x7ffffd3d
   conv.i4
   not
   ldc.i4 0x800002c2
   bne.un Done

TestCase3293:
   ldc.i4 3393
   stloc.0
   ldc.i4 0x7ffffd3c
   conv.i4
   not
   ldc.i4 0x800002c3
   bne.un Done

TestCase3292:
   ldc.i4 3392
   stloc.0
   ldc.i4 0x7ffffd3b
   conv.i4
   not
   ldc.i4 0x800002c4
   bne.un Done

TestCase3291:
   ldc.i4 3391
   stloc.0
   ldc.i4 0x7ffffd3a
   conv.i4
   not
   ldc.i4 0x800002c5
   bne.un Done

TestCase3290:
   ldc.i4 3390
   stloc.0
   ldc.i4 0x7ffffd39
   conv.i4
   not
   ldc.i4 0x800002c6
   bne.un Done

TestCase3289:
   ldc.i4 3389
   stloc.0
   ldc.i4 0x7ffffd38
   conv.i4
   not
   ldc.i4 0x800002c7
   bne.un Done

TestCase3288:
   ldc.i4 3388
   stloc.0
   ldc.i4 0x7ffffd37
   conv.i4
   not
   ldc.i4 0x800002c8
   bne.un Done

TestCase3287:
   ldc.i4 3387
   stloc.0
   ldc.i4 0x7ffffd36
   conv.i4
   not
   ldc.i4 0x800002c9
   beq TestCase3286
   br Done

TestCase3286:
   ldc.i4 3386
   stloc.0
   ldc.i4 0x7ffffd35
   conv.i4
   not
   ldc.i4 0x800002ca
   bne.un Done

TestCase3285:
   ldc.i4 3385
   stloc.0
   ldc.i4 0x7ffffd34
   conv.i4
   not
   ldc.i4 0x800002cb
   bne.un Done

TestCase3284:
   ldc.i4 3384
   stloc.0
   ldc.i4 0x7ffffd33
   conv.i4
   not
   ldc.i4 0x800002cc
   beq TestCase3283
   br Done

TestCase3283:
   ldc.i4 3383
   stloc.0
   ldc.i4 0x7ffffd32
   conv.i4
   not
   ldc.i4 0x800002cd
   beq TestCase3282
   br Done

TestCase3282:
   ldc.i4 3382
   stloc.0
   ldc.i4 0x7ffffd31
   conv.i4
   not
   ldc.i4 0x800002ce
   bne.un Done

TestCase3281:
   ldc.i4 3381
   stloc.0
   ldc.i4 0x7ffffd30
   conv.i4
   not
   ldc.i4 0x800002cf
   bne.un Done

TestCase3280:
   ldc.i4 3380
   stloc.0
   ldc.i4 0x7ffffd2f
   conv.i4
   not
   ldc.i4 0x800002d0
   bne.un Done

TestCase3279:
   ldc.i4 3379
   stloc.0
   ldc.i4 0x7ffffd2e
   conv.i4
   not
   ldc.i4 0x800002d1
   beq TestCase3278
   br Done

TestCase3278:
   ldc.i4 3378
   stloc.0
   ldc.i4 0x7ffffd2d
   conv.i4
   not
   ldc.i4 0x800002d2
   bne.un Done

TestCase3277:
   ldc.i4 3377
   stloc.0
   ldc.i4 0x7ffffd2c
   conv.i4
   not
   ldc.i4 0x800002d3
   bne.un Done

TestCase3276:
   ldc.i4 3376
   stloc.0
   ldc.i4 0x7ffffd2b
   conv.i4
   not
   ldc.i4 0x800002d4
   bne.un Done

TestCase3275:
   ldc.i4 3375
   stloc.0
   ldc.i4 0x7ffffd2a
   conv.i4
   not
   ldc.i4 0x800002d5
   beq TestCase3274
   br Done

TestCase3274:
   ldc.i4 3374
   stloc.0
   ldc.i4 0x7ffffd29
   conv.i4
   not
   ldc.i4 0x800002d6
   bne.un Done

TestCase3273:
   ldc.i4 3373
   stloc.0
   ldc.i4 0x7ffffd28
   conv.i4
   not
   ldc.i4 0x800002d7
   beq TestCase3272
   br Done

TestCase3272:
   ldc.i4 3372
   stloc.0
   ldc.i4 0x7ffffd27
   conv.i4
   not
   ldc.i4 0x800002d8
   bne.un Done

TestCase3271:
   ldc.i4 3371
   stloc.0
   ldc.i4 0x7ffffd26
   conv.i4
   not
   ldc.i4 0x800002d9
   bne.un Done

TestCase3270:
   ldc.i4 3370
   stloc.0
   ldc.i4 0x7ffffd25
   conv.i4
   not
   ldc.i4 0x800002da
   bne.un Done

TestCase3269:
   ldc.i4 3369
   stloc.0
   ldc.i4 0x7ffffd24
   conv.i4
   not
   ldc.i4 0x800002db
   bne.un Done

TestCase3268:
   ldc.i4 3368
   stloc.0
   ldc.i4 0x7ffffd23
   conv.i4
   not
   ldc.i4 0x800002dc
   bne.un Done

TestCase3267:
   ldc.i4 3367
   stloc.0
   ldc.i4 0x7ffffd22
   conv.i4
   not
   ldc.i4 0x800002dd
   beq TestCase3266
   br Done

TestCase3266:
   ldc.i4 3366
   stloc.0
   ldc.i4 0x7ffffd21
   conv.i4
   not
   ldc.i4 0x800002de
   bne.un Done

TestCase3265:
   ldc.i4 3365
   stloc.0
   ldc.i4 0x7ffffd20
   conv.i4
   not
   ldc.i4 0x800002df
   bne.un Done

TestCase3264:
   ldc.i4 3364
   stloc.0
   ldc.i4 0x7ffffd1f
   conv.i4
   not
   ldc.i4 0x800002e0
   beq TestCase3263
   br Done

TestCase3263:
   ldc.i4 3363
   stloc.0
   ldc.i4 0x7ffffd1e
   conv.i4
   not
   ldc.i4 0x800002e1
   beq TestCase3262
   br Done

TestCase3262:
   ldc.i4 3362
   stloc.0
   ldc.i4 0x7ffffd1d
   conv.i4
   not
   ldc.i4 0x800002e2
   beq TestCase3261
   br Done

TestCase3261:
   ldc.i4 3361
   stloc.0
   ldc.i4 0x7ffffd1c
   conv.i4
   not
   ldc.i4 0x800002e3
   bne.un Done

TestCase3260:
   ldc.i4 3360
   stloc.0
   ldc.i4 0x7ffffd1b
   conv.i4
   not
   ldc.i4 0x800002e4
   beq TestCase3259
   br Done

TestCase3259:
   ldc.i4 3359
   stloc.0
   ldc.i4 0x7ffffd1a
   conv.i4
   not
   ldc.i4 0x800002e5
   beq TestCase3258
   br Done

TestCase3258:
   ldc.i4 3358
   stloc.0
   ldc.i4 0x7ffffd19
   conv.i4
   not
   ldc.i4 0x800002e6
   bne.un Done

TestCase3257:
   ldc.i4 3357
   stloc.0
   ldc.i4 0x7ffffd18
   conv.i4
   not
   ldc.i4 0x800002e7
   bne.un Done

TestCase3256:
   ldc.i4 3356
   stloc.0
   ldc.i4 0x7ffffd17
   conv.i4
   not
   ldc.i4 0x800002e8
   bne.un Done

TestCase3255:
   ldc.i4 3355
   stloc.0
   ldc.i4 0x7ffffd16
   conv.i4
   not
   ldc.i4 0x800002e9
   bne.un Done

TestCase3254:
   ldc.i4 3354
   stloc.0
   ldc.i4 0x7ffffd15
   conv.i4
   not
   ldc.i4 0x800002ea
   bne.un Done

TestCase3253:
   ldc.i4 3353
   stloc.0
   ldc.i4 0x7ffffd14
   conv.i4
   not
   ldc.i4 0x800002eb
   beq TestCase3252
   br Done

TestCase3252:
   ldc.i4 3352
   stloc.0
   ldc.i4 0x7ffffd13
   conv.i4
   not
   ldc.i4 0x800002ec
   beq TestCase3251
   br Done

TestCase3251:
   ldc.i4 3351
   stloc.0
   ldc.i4 0x7ffffd12
   conv.i4
   not
   ldc.i4 0x800002ed
   beq TestCase3250
   br Done

TestCase3250:
   ldc.i4 3350
   stloc.0
   ldc.i4 0x7ffffd11
   conv.i4
   not
   ldc.i4 0x800002ee
   bne.un Done

TestCase3249:
   ldc.i4 3349
   stloc.0
   ldc.i4 0x7ffffd10
   conv.i4
   not
   ldc.i4 0x800002ef
   beq TestCase3248
   br Done

TestCase3248:
   ldc.i4 3348
   stloc.0
   ldc.i4 0x7ffffd0f
   conv.i4
   not
   ldc.i4 0x800002f0
   beq TestCase3247
   br Done

TestCase3247:
   ldc.i4 3347
   stloc.0
   ldc.i4 0x7ffffd0e
   conv.i4
   not
   ldc.i4 0x800002f1
   beq TestCase3246
   br Done

TestCase3246:
   ldc.i4 3346
   stloc.0
   ldc.i4 0x7ffffd0d
   conv.i4
   not
   ldc.i4 0x800002f2
   bne.un Done

TestCase3245:
   ldc.i4 3345
   stloc.0
   ldc.i4 0x7ffffd0c
   conv.i4
   not
   ldc.i4 0x800002f3
   bne.un Done

TestCase3244:
   ldc.i4 3344
   stloc.0
   ldc.i4 0x7ffffd0b
   conv.i4
   not
   ldc.i4 0x800002f4
   bne.un Done

TestCase3243:
   ldc.i4 3343
   stloc.0
   ldc.i4 0x7ffffd0a
   conv.i4
   not
   ldc.i4 0x800002f5
   bne.un Done

TestCase3242:
   ldc.i4 3342
   stloc.0
   ldc.i4 0x7ffffd09
   conv.i4
   not
   ldc.i4 0x800002f6
   beq TestCase3241
   br Done

TestCase3241:
   ldc.i4 3341
   stloc.0
   ldc.i4 0x7ffffd08
   conv.i4
   not
   ldc.i4 0x800002f7
   beq TestCase3240
   br Done

TestCase3240:
   ldc.i4 3340
   stloc.0
   ldc.i4 0x7ffffd07
   conv.i4
   not
   ldc.i4 0x800002f8
   beq TestCase3239
   br Done

TestCase3239:
   ldc.i4 3339
   stloc.0
   ldc.i4 0x7ffffd06
   conv.i4
   not
   ldc.i4 0x800002f9
   beq TestCase3238
   br Done

TestCase3238:
   ldc.i4 3338
   stloc.0
   ldc.i4 0x7ffffd05
   conv.i4
   not
   ldc.i4 0x800002fa
   beq TestCase3237
   br Done

TestCase3237:
   ldc.i4 3337
   stloc.0
   ldc.i4 0x7ffffd04
   conv.i4
   not
   ldc.i4 0x800002fb
   beq TestCase3236
   br Done

TestCase3236:
   ldc.i4 3336
   stloc.0
   ldc.i4 0x7ffffd03
   conv.i4
   not
   ldc.i4 0x800002fc
   bne.un Done

TestCase3235:
   ldc.i4 3335
   stloc.0
   ldc.i4 0x7ffffd02
   conv.i4
   not
   ldc.i4 0x800002fd
   beq TestCase3234
   br Done

TestCase3234:
   ldc.i4 3334
   stloc.0
   ldc.i4 0x7ffffd01
   conv.i4
   not
   ldc.i4 0x800002fe
   bne.un Done

TestCase3233:
   ldc.i4 3333
   stloc.0
   ldc.i4 0x7ffffd00
   conv.i4
   not
   ldc.i4 0x800002ff
   beq TestCase3232
   br Done

TestCase3232:
   ldc.i4 3332
   stloc.0
   ldc.i4 0x7ffffcff
   conv.i4
   not
   ldc.i4 0x80000300
   beq TestCase3231
   br Done

TestCase3231:
   ldc.i4 3331
   stloc.0
   ldc.i4 0x7ffffcfe
   conv.i4
   not
   ldc.i4 0x80000301
   bne.un Done

TestCase3230:
   ldc.i4 3330
   stloc.0
   ldc.i4 0x7ffffcfd
   conv.i4
   not
   ldc.i4 0x80000302
   beq TestCase3229
   br Done

TestCase3229:
   ldc.i4 3329
   stloc.0
   ldc.i4 0x7ffffcfc
   conv.i4
   not
   ldc.i4 0x80000303
   beq TestCase3228
   br Done

TestCase3228:
   ldc.i4 3328
   stloc.0
   ldc.i4 0x7ffffcfb
   conv.i4
   not
   ldc.i4 0x80000304
   bne.un Done

TestCase3227:
   ldc.i4 3327
   stloc.0
   ldc.i4 0x7ffffcfa
   conv.i4
   not
   ldc.i4 0x80000305
   bne.un Done

TestCase3226:
   ldc.i4 3326
   stloc.0
   ldc.i4 0x7ffffcf9
   conv.i4
   not
   ldc.i4 0x80000306
   bne.un Done

TestCase3225:
   ldc.i4 3325
   stloc.0
   ldc.i4 0x7ffffcf8
   conv.i4
   not
   ldc.i4 0x80000307
   bne.un Done

TestCase3224:
   ldc.i4 3324
   stloc.0
   ldc.i4 0x7ffffcf7
   conv.i4
   not
   ldc.i4 0x80000308
   beq TestCase3223
   br Done

TestCase3223:
   ldc.i4 3323
   stloc.0
   ldc.i4 0x7ffffcf6
   conv.i4
   not
   ldc.i4 0x80000309
   beq TestCase3222
   br Done

TestCase3222:
   ldc.i4 3322
   stloc.0
   ldc.i4 0x7ffffcf5
   conv.i4
   not
   ldc.i4 0x8000030a
   bne.un Done

TestCase3221:
   ldc.i4 3321
   stloc.0
   ldc.i4 0x7ffffcf4
   conv.i4
   not
   ldc.i4 0x8000030b
   bne.un Done

TestCase3220:
   ldc.i4 3320
   stloc.0
   ldc.i4 0x7ffffcf3
   conv.i4
   not
   ldc.i4 0x8000030c
   beq TestCase3219
   br Done

TestCase3219:
   ldc.i4 3319
   stloc.0
   ldc.i4 0x7ffffcf2
   conv.i4
   not
   ldc.i4 0x8000030d
   bne.un Done

TestCase3218:
   ldc.i4 3318
   stloc.0
   ldc.i4 0x7ffffcf1
   conv.i4
   not
   ldc.i4 0x8000030e
   bne.un Done

TestCase3217:
   ldc.i4 3317
   stloc.0
   ldc.i4 0x7ffffcf0
   conv.i4
   not
   ldc.i4 0x8000030f
   bne.un Done

TestCase3216:
   ldc.i4 3316
   stloc.0
   ldc.i4 0x7ffffcef
   conv.i4
   not
   ldc.i4 0x80000310
   bne.un Done

TestCase3215:
   ldc.i4 3315
   stloc.0
   ldc.i4 0x7ffffcee
   conv.i4
   not
   ldc.i4 0x80000311
   beq TestCase3214
   br Done

TestCase3214:
   ldc.i4 3314
   stloc.0
   ldc.i4 0x7ffffced
   conv.i4
   not
   ldc.i4 0x80000312
   bne.un Done

TestCase3213:
   ldc.i4 3313
   stloc.0
   ldc.i4 0x7ffffcec
   conv.i4
   not
   ldc.i4 0x80000313
   beq TestCase3212
   br Done

TestCase3212:
   ldc.i4 3312
   stloc.0
   ldc.i4 0x7ffffceb
   conv.i4
   not
   ldc.i4 0x80000314
   bne.un Done

TestCase3211:
   ldc.i4 3311
   stloc.0
   ldc.i4 0x7ffffcea
   conv.i4
   not
   ldc.i4 0x80000315
   bne.un Done

TestCase3210:
   ldc.i4 3310
   stloc.0
   ldc.i4 0x7ffffce9
   conv.i4
   not
   ldc.i4 0x80000316
   bne.un Done

TestCase3209:
   ldc.i4 3309
   stloc.0
   ldc.i4 0x7ffffce8
   conv.i4
   not
   ldc.i4 0x80000317
   bne.un Done

TestCase3208:
   ldc.i4 3308
   stloc.0
   ldc.i4 0x7ffffce7
   conv.i4
   not
   ldc.i4 0x80000318
   beq TestCase3207
   br Done

TestCase3207:
   ldc.i4 3307
   stloc.0
   ldc.i4 0x7ffffce6
   conv.i4
   not
   ldc.i4 0x80000319
   bne.un Done

TestCase3206:
   ldc.i4 3306
   stloc.0
   ldc.i4 0x7ffffce5
   conv.i4
   not
   ldc.i4 0x8000031a
   bne.un Done

TestCase3205:
   ldc.i4 3305
   stloc.0
   ldc.i4 0x7ffffce4
   conv.i4
   not
   ldc.i4 0x8000031b
   bne.un Done

TestCase3204:
   ldc.i4 3304
   stloc.0
   ldc.i4 0x7ffffce3
   conv.i4
   not
   ldc.i4 0x8000031c
   bne.un Done

TestCase3203:
   ldc.i4 3303
   stloc.0
   ldc.i4 0x7ffffce2
   conv.i4
   not
   ldc.i4 0x8000031d
   beq TestCase3202
   br Done

TestCase3202:
   ldc.i4 3302
   stloc.0
   ldc.i4 0x7ffffce1
   conv.i4
   not
   ldc.i4 0x8000031e
   beq TestCase3201
   br Done

TestCase3201:
   ldc.i4 3301
   stloc.0
   ldc.i4 0x7ffffce0
   conv.i4
   not
   ldc.i4 0x8000031f
   beq TestCase3200
   br Done

TestCase3200:
   ldc.i4 3300
   stloc.0
   ldc.i4 0x7ffffcdf
   conv.i4
   not
   ldc.i4 0x80000320
   bne.un Done

TestCase3199:
   ldc.i4 3299
   stloc.0
   ldc.i4 0x7ffffcde
   conv.i4
   not
   ldc.i4 0x80000321
   beq TestCase3198
   br Done

TestCase3198:
   ldc.i4 3298
   stloc.0
   ldc.i4 0x7ffffcdd
   conv.i4
   not
   ldc.i4 0x80000322
   bne.un Done

TestCase3197:
   ldc.i4 3297
   stloc.0
   ldc.i4 0x7ffffcdc
   conv.i4
   not
   ldc.i4 0x80000323
   bne.un Done

TestCase3196:
   ldc.i4 3296
   stloc.0
   ldc.i4 0x7ffffcdb
   conv.i4
   not
   ldc.i4 0x80000324
   bne.un Done

TestCase3195:
   ldc.i4 3295
   stloc.0
   ldc.i4 0x7ffffcda
   conv.i4
   not
   ldc.i4 0x80000325
   bne.un Done

TestCase3194:
   ldc.i4 3294
   stloc.0
   ldc.i4 0x7ffffcd9
   conv.i4
   not
   ldc.i4 0x80000326
   bne.un Done

TestCase3193:
   ldc.i4 3293
   stloc.0
   ldc.i4 0x7ffffcd8
   conv.i4
   not
   ldc.i4 0x80000327
   beq TestCase3192
   br Done

TestCase3192:
   ldc.i4 3292
   stloc.0
   ldc.i4 0x7ffffcd7
   conv.i4
   not
   ldc.i4 0x80000328
   bne.un Done

TestCase3191:
   ldc.i4 3291
   stloc.0
   ldc.i4 0x7ffffcd6
   conv.i4
   not
   ldc.i4 0x80000329
   beq TestCase3190
   br Done

TestCase3190:
   ldc.i4 3290
   stloc.0
   ldc.i4 0x7ffffcd5
   conv.i4
   not
   ldc.i4 0x8000032a
   beq TestCase3189
   br Done

TestCase3189:
   ldc.i4 3289
   stloc.0
   ldc.i4 0x7ffffcd4
   conv.i4
   not
   ldc.i4 0x8000032b
   beq TestCase3188
   br Done

TestCase3188:
   ldc.i4 3288
   stloc.0
   ldc.i4 0x7ffffcd3
   conv.i4
   not
   ldc.i4 0x8000032c
   bne.un Done

TestCase3187:
   ldc.i4 3287
   stloc.0
   ldc.i4 0x7ffffcd2
   conv.i4
   not
   ldc.i4 0x8000032d
   bne.un Done

TestCase3186:
   ldc.i4 3286
   stloc.0
   ldc.i4 0x7ffffcd1
   conv.i4
   not
   ldc.i4 0x8000032e
   bne.un Done

TestCase3185:
   ldc.i4 3285
   stloc.0
   ldc.i4 0x7ffffcd0
   conv.i4
   not
   ldc.i4 0x8000032f
   beq TestCase3184
   br Done

TestCase3184:
   ldc.i4 3284
   stloc.0
   ldc.i4 0x7ffffccf
   conv.i4
   not
   ldc.i4 0x80000330
   beq TestCase3183
   br Done

TestCase3183:
   ldc.i4 3283
   stloc.0
   ldc.i4 0x7ffffcce
   conv.i4
   not
   ldc.i4 0x80000331
   beq TestCase3182
   br Done

TestCase3182:
   ldc.i4 3282
   stloc.0
   ldc.i4 0x7ffffccd
   conv.i4
   not
   ldc.i4 0x80000332
   beq TestCase3181
   br Done

TestCase3181:
   ldc.i4 3281
   stloc.0
   ldc.i4 0x7ffffccc
   conv.i4
   not
   ldc.i4 0x80000333
   beq TestCase3180
   br Done

TestCase3180:
   ldc.i4 3280
   stloc.0
   ldc.i4 0x7ffffccb
   conv.i4
   not
   ldc.i4 0x80000334
   bne.un Done

TestCase3179:
   ldc.i4 3279
   stloc.0
   ldc.i4 0x7ffffcca
   conv.i4
   not
   ldc.i4 0x80000335
   beq TestCase3178
   br Done

TestCase3178:
   ldc.i4 3278
   stloc.0
   ldc.i4 0x7ffffcc9
   conv.i4
   not
   ldc.i4 0x80000336
   beq TestCase3177
   br Done

TestCase3177:
   ldc.i4 3277
   stloc.0
   ldc.i4 0x7ffffcc8
   conv.i4
   not
   ldc.i4 0x80000337
   bne.un Done

TestCase3176:
   ldc.i4 3276
   stloc.0
   ldc.i4 0x7ffffcc7
   conv.i4
   not
   ldc.i4 0x80000338
   bne.un Done

TestCase3175:
   ldc.i4 3275
   stloc.0
   ldc.i4 0x7ffffcc6
   conv.i4
   not
   ldc.i4 0x80000339
   bne.un Done

TestCase3174:
   ldc.i4 3274
   stloc.0
   ldc.i4 0x7ffffcc5
   conv.i4
   not
   ldc.i4 0x8000033a
   bne.un Done

TestCase3173:
   ldc.i4 3273
   stloc.0
   ldc.i4 0x7ffffcc4
   conv.i4
   not
   ldc.i4 0x8000033b
   beq TestCase3172
   br Done

TestCase3172:
   ldc.i4 3272
   stloc.0
   ldc.i4 0x7ffffcc3
   conv.i4
   not
   ldc.i4 0x8000033c
   bne.un Done

TestCase3171:
   ldc.i4 3271
   stloc.0
   ldc.i4 0x7ffffcc2
   conv.i4
   not
   ldc.i4 0x8000033d
   beq TestCase3170
   br Done

TestCase3170:
   ldc.i4 3270
   stloc.0
   ldc.i4 0x7ffffcc1
   conv.i4
   not
   ldc.i4 0x8000033e
   bne.un Done

TestCase3169:
   ldc.i4 3269
   stloc.0
   ldc.i4 0x7ffffcc0
   conv.i4
   not
   ldc.i4 0x8000033f
   bne.un Done

TestCase3168:
   ldc.i4 3268
   stloc.0
   ldc.i4 0x7ffffcbf
   conv.i4
   not
   ldc.i4 0x80000340
   bne.un Done

TestCase3167:
   ldc.i4 3267
   stloc.0
   ldc.i4 0x7ffffcbe
   conv.i4
   not
   ldc.i4 0x80000341
   beq TestCase3166
   br Done

TestCase3166:
   ldc.i4 3266
   stloc.0
   ldc.i4 0x7ffffcbd
   conv.i4
   not
   ldc.i4 0x80000342
   bne.un Done

TestCase3165:
   ldc.i4 3265
   stloc.0
   ldc.i4 0x7ffffcbc
   conv.i4
   not
   ldc.i4 0x80000343
   beq TestCase3164
   br Done

TestCase3164:
   ldc.i4 3264
   stloc.0
   ldc.i4 0x7ffffcbb
   conv.i4
   not
   ldc.i4 0x80000344
   beq TestCase3163
   br Done

TestCase3163:
   ldc.i4 3263
   stloc.0
   ldc.i4 0x7ffffcba
   conv.i4
   not
   ldc.i4 0x80000345
   bne.un Done

TestCase3162:
   ldc.i4 3262
   stloc.0
   ldc.i4 0x7ffffcb9
   conv.i4
   not
   ldc.i4 0x80000346
   bne.un Done

TestCase3161:
   ldc.i4 3261
   stloc.0
   ldc.i4 0x7ffffcb8
   conv.i4
   not
   ldc.i4 0x80000347
   bne.un Done

TestCase3160:
   ldc.i4 3260
   stloc.0
   ldc.i4 0x7ffffcb7
   conv.i4
   not
   ldc.i4 0x80000348
   beq TestCase3159
   br Done

TestCase3159:
   ldc.i4 3259
   stloc.0
   ldc.i4 0x7ffffcb6
   conv.i4
   not
   ldc.i4 0x80000349
   beq TestCase3158
   br Done

TestCase3158:
   ldc.i4 3258
   stloc.0
   ldc.i4 0x7ffffcb5
   conv.i4
   not
   ldc.i4 0x8000034a
   beq TestCase3157
   br Done

TestCase3157:
   ldc.i4 3257
   stloc.0
   ldc.i4 0x7ffffcb4
   conv.i4
   not
   ldc.i4 0x8000034b
   bne.un Done

TestCase3156:
   ldc.i4 3256
   stloc.0
   ldc.i4 0x7ffffcb3
   conv.i4
   not
   ldc.i4 0x8000034c
   beq TestCase3155
   br Done

TestCase3155:
   ldc.i4 3255
   stloc.0
   ldc.i4 0x7ffffcb2
   conv.i4
   not
   ldc.i4 0x8000034d
   bne.un Done

TestCase3154:
   ldc.i4 3254
   stloc.0
   ldc.i4 0x7ffffcb1
   conv.i4
   not
   ldc.i4 0x8000034e
   beq TestCase3153
   br Done

TestCase3153:
   ldc.i4 3253
   stloc.0
   ldc.i4 0x7ffffcb0
   conv.i4
   not
   ldc.i4 0x8000034f
   bne.un Done

TestCase3152:
   ldc.i4 3252
   stloc.0
   ldc.i4 0x7ffffcaf
   conv.i4
   not
   ldc.i4 0x80000350
   beq TestCase3151
   br Done

TestCase3151:
   ldc.i4 3251
   stloc.0
   ldc.i4 0x7ffffcae
   conv.i4
   not
   ldc.i4 0x80000351
   beq TestCase3150
   br Done

TestCase3150:
   ldc.i4 3250
   stloc.0
   ldc.i4 0x7ffffcad
   conv.i4
   not
   ldc.i4 0x80000352
   beq TestCase3149
   br Done

TestCase3149:
   ldc.i4 3249
   stloc.0
   ldc.i4 0x7ffffcac
   conv.i4
   not
   ldc.i4 0x80000353
   bne.un Done

TestCase3148:
   ldc.i4 3248
   stloc.0
   ldc.i4 0x7ffffcab
   conv.i4
   not
   ldc.i4 0x80000354
   beq TestCase3147
   br Done

TestCase3147:
   ldc.i4 3247
   stloc.0
   ldc.i4 0x7ffffcaa
   conv.i4
   not
   ldc.i4 0x80000355
   bne.un Done

TestCase3146:
   ldc.i4 3246
   stloc.0
   ldc.i4 0x7ffffca9
   conv.i4
   not
   ldc.i4 0x80000356
   beq TestCase3145
   br Done

TestCase3145:
   ldc.i4 3245
   stloc.0
   ldc.i4 0x7ffffca8
   conv.i4
   not
   ldc.i4 0x80000357
   beq TestCase3144
   br Done

TestCase3144:
   ldc.i4 3244
   stloc.0
   ldc.i4 0x7ffffca7
   conv.i4
   not
   ldc.i4 0x80000358
   bne.un Done

TestCase3143:
   ldc.i4 3243
   stloc.0
   ldc.i4 0x7ffffca6
   conv.i4
   not
   ldc.i4 0x80000359
   beq TestCase3142
   br Done

TestCase3142:
   ldc.i4 3242
   stloc.0
   ldc.i4 0x7ffffca5
   conv.i4
   not
   ldc.i4 0x8000035a
   bne.un Done

TestCase3141:
   ldc.i4 3241
   stloc.0
   ldc.i4 0x7ffffca4
   conv.i4
   not
   ldc.i4 0x8000035b
   bne.un Done

TestCase3140:
   ldc.i4 3240
   stloc.0
   ldc.i4 0x7ffffca3
   conv.i4
   not
   ldc.i4 0x8000035c
   beq TestCase3139
   br Done

TestCase3139:
   ldc.i4 3239
   stloc.0
   ldc.i4 0x7ffffca2
   conv.i4
   not
   ldc.i4 0x8000035d
   beq TestCase3138
   br Done

TestCase3138:
   ldc.i4 3238
   stloc.0
   ldc.i4 0x7ffffca1
   conv.i4
   not
   ldc.i4 0x8000035e
   beq TestCase3137
   br Done

TestCase3137:
   ldc.i4 3237
   stloc.0
   ldc.i4 0x7ffffca0
   conv.i4
   not
   ldc.i4 0x8000035f
   bne.un Done

TestCase3136:
   ldc.i4 3236
   stloc.0
   ldc.i4 0x7ffffc9f
   conv.i4
   not
   ldc.i4 0x80000360
   beq TestCase3135
   br Done

TestCase3135:
   ldc.i4 3235
   stloc.0
   ldc.i4 0x7ffffc9e
   conv.i4
   not
   ldc.i4 0x80000361
   bne.un Done

TestCase3134:
   ldc.i4 3234
   stloc.0
   ldc.i4 0x7ffffc9d
   conv.i4
   not
   ldc.i4 0x80000362
   bne.un Done

TestCase3133:
   ldc.i4 3233
   stloc.0
   ldc.i4 0x7ffffc9c
   conv.i4
   not
   ldc.i4 0x80000363
   beq TestCase3132
   br Done

TestCase3132:
   ldc.i4 3232
   stloc.0
   ldc.i4 0x7ffffc9b
   conv.i4
   not
   ldc.i4 0x80000364
   beq TestCase3131
   br Done

TestCase3131:
   ldc.i4 3231
   stloc.0
   ldc.i4 0x7ffffc9a
   conv.i4
   not
   ldc.i4 0x80000365
   beq TestCase3130
   br Done

TestCase3130:
   ldc.i4 3230
   stloc.0
   ldc.i4 0x7ffffc99
   conv.i4
   not
   ldc.i4 0x80000366
   beq TestCase3129
   br Done

TestCase3129:
   ldc.i4 3229
   stloc.0
   ldc.i4 0x7ffffc98
   conv.i4
   not
   ldc.i4 0x80000367
   bne.un Done

TestCase3128:
   ldc.i4 3228
   stloc.0
   ldc.i4 0x7ffffc97
   conv.i4
   not
   ldc.i4 0x80000368
   bne.un Done

TestCase3127:
   ldc.i4 3227
   stloc.0
   ldc.i4 0x7ffffc96
   conv.i4
   not
   ldc.i4 0x80000369
   beq TestCase3126
   br Done

TestCase3126:
   ldc.i4 3226
   stloc.0
   ldc.i4 0x7ffffc95
   conv.i4
   not
   ldc.i4 0x8000036a
   bne.un Done

TestCase3125:
   ldc.i4 3225
   stloc.0
   ldc.i4 0x7ffffc94
   conv.i4
   not
   ldc.i4 0x8000036b
   beq TestCase3124
   br Done

TestCase3124:
   ldc.i4 3224
   stloc.0
   ldc.i4 0x7ffffc93
   conv.i4
   not
   ldc.i4 0x8000036c
   bne.un Done

TestCase3123:
   ldc.i4 3223
   stloc.0
   ldc.i4 0x7ffffc92
   conv.i4
   not
   ldc.i4 0x8000036d
   beq TestCase3122
   br Done

TestCase3122:
   ldc.i4 3222
   stloc.0
   ldc.i4 0x7ffffc91
   conv.i4
   not
   ldc.i4 0x8000036e
   beq TestCase3121
   br Done

TestCase3121:
   ldc.i4 3221
   stloc.0
   ldc.i4 0x7ffffc90
   conv.i4
   not
   ldc.i4 0x8000036f
   bne.un Done

TestCase3120:
   ldc.i4 3220
   stloc.0
   ldc.i4 0x7ffffc8f
   conv.i4
   not
   ldc.i4 0x80000370
   beq TestCase3119
   br Done

TestCase3119:
   ldc.i4 3219
   stloc.0
   ldc.i4 0x7ffffc8e
   conv.i4
   not
   ldc.i4 0x80000371
   beq TestCase3118
   br Done

TestCase3118:
   ldc.i4 3218
   stloc.0
   ldc.i4 0x7ffffc8d
   conv.i4
   not
   ldc.i4 0x80000372
   beq TestCase3117
   br Done

TestCase3117:
   ldc.i4 3217
   stloc.0
   ldc.i4 0x7ffffc8c
   conv.i4
   not
   ldc.i4 0x80000373
   beq TestCase3116
   br Done

TestCase3116:
   ldc.i4 3216
   stloc.0
   ldc.i4 0x7ffffc8b
   conv.i4
   not
   ldc.i4 0x80000374
   beq TestCase3115
   br Done

TestCase3115:
   ldc.i4 3215
   stloc.0
   ldc.i4 0x7ffffc8a
   conv.i4
   not
   ldc.i4 0x80000375
   beq TestCase3114
   br Done

TestCase3114:
   ldc.i4 3214
   stloc.0
   ldc.i4 0x7ffffc89
   conv.i4
   not
   ldc.i4 0x80000376
   beq TestCase3113
   br Done

TestCase3113:
   ldc.i4 3213
   stloc.0
   ldc.i4 0x7ffffc88
   conv.i4
   not
   ldc.i4 0x80000377
   bne.un Done

TestCase3112:
   ldc.i4 3212
   stloc.0
   ldc.i4 0x7ffffc87
   conv.i4
   not
   ldc.i4 0x80000378
   beq TestCase3111
   br Done

TestCase3111:
   ldc.i4 3211
   stloc.0
   ldc.i4 0x7ffffc86
   conv.i4
   not
   ldc.i4 0x80000379
   beq TestCase3110
   br Done

TestCase3110:
   ldc.i4 3210
   stloc.0
   ldc.i4 0x7ffffc85
   conv.i4
   not
   ldc.i4 0x8000037a
   bne.un Done

TestCase3109:
   ldc.i4 3209
   stloc.0
   ldc.i4 0x7ffffc84
   conv.i4
   not
   ldc.i4 0x8000037b
   bne.un Done

TestCase3108:
   ldc.i4 3208
   stloc.0
   ldc.i4 0x7ffffc83
   conv.i4
   not
   ldc.i4 0x8000037c
   bne.un Done

TestCase3107:
   ldc.i4 3207
   stloc.0
   ldc.i4 0x7ffffc82
   conv.i4
   not
   ldc.i4 0x8000037d
   bne.un Done

TestCase3106:
   ldc.i4 3206
   stloc.0
   ldc.i4 0x7ffffc81
   conv.i4
   not
   ldc.i4 0x8000037e
   bne.un Done

TestCase3105:
   ldc.i4 3205
   stloc.0
   ldc.i4 0x7ffffc80
   conv.i4
   not
   ldc.i4 0x8000037f
   bne.un Done

TestCase3104:
   ldc.i4 3204
   stloc.0
   ldc.i4 0x7ffffc7f
   conv.i4
   not
   ldc.i4 0x80000380
   beq TestCase3103
   br Done

TestCase3103:
   ldc.i4 3203
   stloc.0
   ldc.i4 0x7ffffc7e
   conv.i4
   not
   ldc.i4 0x80000381
   bne.un Done

TestCase3102:
   ldc.i4 3202
   stloc.0
   ldc.i4 0x7ffffc7d
   conv.i4
   not
   ldc.i4 0x80000382
   beq TestCase3101
   br Done

TestCase3101:
   ldc.i4 3201
   stloc.0
   ldc.i4 0x7ffffc7c
   conv.i4
   not
   ldc.i4 0x80000383
   bne.un Done

TestCase3100:
   ldc.i4 3200
   stloc.0
   ldc.i4 0x7ffffc7b
   conv.i4
   not
   ldc.i4 0x80000384
   beq TestCase3099
   br Done

TestCase3099:
   ldc.i4 3199
   stloc.0
   ldc.i4 0x7ffffc7a
   conv.i4
   not
   ldc.i4 0x80000385
   beq TestCase3098
   br Done

TestCase3098:
   ldc.i4 3198
   stloc.0
   ldc.i4 0x7ffffc79
   conv.i4
   not
   ldc.i4 0x80000386
   beq TestCase3097
   br Done

TestCase3097:
   ldc.i4 3197
   stloc.0
   ldc.i4 0x7ffffc78
   conv.i4
   not
   ldc.i4 0x80000387
   beq TestCase3096
   br Done

TestCase3096:
   ldc.i4 3196
   stloc.0
   ldc.i4 0x7ffffc77
   conv.i4
   not
   ldc.i4 0x80000388
   beq TestCase3095
   br Done

TestCase3095:
   ldc.i4 3195
   stloc.0
   ldc.i4 0x7ffffc76
   conv.i4
   not
   ldc.i4 0x80000389
   bne.un Done

TestCase3094:
   ldc.i4 3194
   stloc.0
   ldc.i4 0x7ffffc75
   conv.i4
   not
   ldc.i4 0x8000038a
   bne.un Done

TestCase3093:
   ldc.i4 3193
   stloc.0
   ldc.i4 0x7ffffc74
   conv.i4
   not
   ldc.i4 0x8000038b
   beq TestCase3092
   br Done

TestCase3092:
   ldc.i4 3192
   stloc.0
   ldc.i4 0x7ffffc73
   conv.i4
   not
   ldc.i4 0x8000038c
   beq TestCase3091
   br Done

TestCase3091:
   ldc.i4 3191
   stloc.0
   ldc.i4 0x7ffffc72
   conv.i4
   not
   ldc.i4 0x8000038d
   bne.un Done

TestCase3090:
   ldc.i4 3190
   stloc.0
   ldc.i4 0x7ffffc71
   conv.i4
   not
   ldc.i4 0x8000038e
   beq TestCase3089
   br Done

TestCase3089:
   ldc.i4 3189
   stloc.0
   ldc.i4 0x7ffffc70
   conv.i4
   not
   ldc.i4 0x8000038f
   bne.un Done

TestCase3088:
   ldc.i4 3188
   stloc.0
   ldc.i4 0x7ffffc6f
   conv.i4
   not
   ldc.i4 0x80000390
   bne.un Done

TestCase3087:
   ldc.i4 3187
   stloc.0
   ldc.i4 0x7ffffc6e
   conv.i4
   not
   ldc.i4 0x80000391
   beq TestCase3086
   br Done

TestCase3086:
   ldc.i4 3186
   stloc.0
   ldc.i4 0x7ffffc6d
   conv.i4
   not
   ldc.i4 0x80000392
   beq TestCase3085
   br Done

TestCase3085:
   ldc.i4 3185
   stloc.0
   ldc.i4 0x7ffffc6c
   conv.i4
   not
   ldc.i4 0x80000393
   beq TestCase3084
   br Done

TestCase3084:
   ldc.i4 3184
   stloc.0
   ldc.i4 0x7ffffc6b
   conv.i4
   not
   ldc.i4 0x80000394
   bne.un Done

TestCase3083:
   ldc.i4 3183
   stloc.0
   ldc.i4 0x7ffffc6a
   conv.i4
   not
   ldc.i4 0x80000395
   beq TestCase3082
   br Done

TestCase3082:
   ldc.i4 3182
   stloc.0
   ldc.i4 0x7ffffc69
   conv.i4
   not
   ldc.i4 0x80000396
   beq TestCase3081
   br Done

TestCase3081:
   ldc.i4 3181
   stloc.0
   ldc.i4 0x7ffffc68
   conv.i4
   not
   ldc.i4 0x80000397
   bne.un Done

TestCase3080:
   ldc.i4 3180
   stloc.0
   ldc.i4 0x7ffffc67
   conv.i4
   not
   ldc.i4 0x80000398
   beq TestCase3079
   br Done

TestCase3079:
   ldc.i4 3179
   stloc.0
   ldc.i4 0x7ffffc66
   conv.i4
   not
   ldc.i4 0x80000399
   beq TestCase3078
   br Done

TestCase3078:
   ldc.i4 3178
   stloc.0
   ldc.i4 0x7ffffc65
   conv.i4
   not
   ldc.i4 0x8000039a
   beq TestCase3077
   br Done

TestCase3077:
   ldc.i4 3177
   stloc.0
   ldc.i4 0x7ffffc64
   conv.i4
   not
   ldc.i4 0x8000039b
   bne.un Done

TestCase3076:
   ldc.i4 3176
   stloc.0
   ldc.i4 0x7ffffc63
   conv.i4
   not
   ldc.i4 0x8000039c
   bne.un Done

TestCase3075:
   ldc.i4 3175
   stloc.0
   ldc.i4 0x7ffffc62
   conv.i4
   not
   ldc.i4 0x8000039d
   bne.un Done

TestCase3074:
   ldc.i4 3174
   stloc.0
   ldc.i4 0x7ffffc61
   conv.i4
   not
   ldc.i4 0x8000039e
   bne.un Done

TestCase3073:
   ldc.i4 3173
   stloc.0
   ldc.i4 0x7ffffc60
   conv.i4
   not
   ldc.i4 0x8000039f
   bne.un Done

TestCase3072:
   ldc.i4 3172
   stloc.0
   ldc.i4 0x7ffffc5f
   conv.i4
   not
   ldc.i4 0x800003a0
   bne.un Done

TestCase3071:
   ldc.i4 3171
   stloc.0
   ldc.i4 0x7ffffc5e
   conv.i4
   not
   ldc.i4 0x800003a1
   bne.un Done

TestCase3070:
   ldc.i4 3170
   stloc.0
   ldc.i4 0x7ffffc5d
   conv.i4
   not
   ldc.i4 0x800003a2
   beq TestCase3069
   br Done

TestCase3069:
   ldc.i4 3169
   stloc.0
   ldc.i4 0x7ffffc5c
   conv.i4
   not
   ldc.i4 0x800003a3
   beq TestCase3068
   br Done

TestCase3068:
   ldc.i4 3168
   stloc.0
   ldc.i4 0x7ffffc5b
   conv.i4
   not
   ldc.i4 0x800003a4
   bne.un Done

TestCase3067:
   ldc.i4 3167
   stloc.0
   ldc.i4 0x7ffffc5a
   conv.i4
   not
   ldc.i4 0x800003a5
   beq TestCase3066
   br Done

TestCase3066:
   ldc.i4 3166
   stloc.0
   ldc.i4 0x7ffffc59
   conv.i4
   not
   ldc.i4 0x800003a6
   beq TestCase3065
   br Done

TestCase3065:
   ldc.i4 3165
   stloc.0
   ldc.i4 0x7ffffc58
   conv.i4
   not
   ldc.i4 0x800003a7
   bne.un Done

TestCase3064:
   ldc.i4 3164
   stloc.0
   ldc.i4 0x7ffffc57
   conv.i4
   not
   ldc.i4 0x800003a8
   bne.un Done

TestCase3063:
   ldc.i4 3163
   stloc.0
   ldc.i4 0x7ffffc56
   conv.i4
   not
   ldc.i4 0x800003a9
   beq TestCase3062
   br Done

TestCase3062:
   ldc.i4 3162
   stloc.0
   ldc.i4 0x7ffffc55
   conv.i4
   not
   ldc.i4 0x800003aa
   beq TestCase3061
   br Done

TestCase3061:
   ldc.i4 3161
   stloc.0
   ldc.i4 0x7ffffc54
   conv.i4
   not
   ldc.i4 0x800003ab
   beq TestCase3060
   br Done

TestCase3060:
   ldc.i4 3160
   stloc.0
   ldc.i4 0x7ffffc53
   conv.i4
   not
   ldc.i4 0x800003ac
   bne.un Done

TestCase3059:
   ldc.i4 3159
   stloc.0
   ldc.i4 0x7ffffc52
   conv.i4
   not
   ldc.i4 0x800003ad
   bne.un Done

TestCase3058:
   ldc.i4 3158
   stloc.0
   ldc.i4 0x7ffffc51
   conv.i4
   not
   ldc.i4 0x800003ae
   bne.un Done

TestCase3057:
   ldc.i4 3157
   stloc.0
   ldc.i4 0x7ffffc50
   conv.i4
   not
   ldc.i4 0x800003af
   beq TestCase3056
   br Done

TestCase3056:
   ldc.i4 3156
   stloc.0
   ldc.i4 0x7ffffc4f
   conv.i4
   not
   ldc.i4 0x800003b0
   beq TestCase3055
   br Done

TestCase3055:
   ldc.i4 3155
   stloc.0
   ldc.i4 0x7ffffc4e
   conv.i4
   not
   ldc.i4 0x800003b1
   bne.un Done

TestCase3054:
   ldc.i4 3154
   stloc.0
   ldc.i4 0x7ffffc4d
   conv.i4
   not
   ldc.i4 0x800003b2
   beq TestCase3053
   br Done

TestCase3053:
   ldc.i4 3153
   stloc.0
   ldc.i4 0x7ffffc4c
   conv.i4
   not
   ldc.i4 0x800003b3
   beq TestCase3052
   br Done

TestCase3052:
   ldc.i4 3152
   stloc.0
   ldc.i4 0x7ffffc4b
   conv.i4
   not
   ldc.i4 0x800003b4
   beq TestCase3051
   br Done

TestCase3051:
   ldc.i4 3151
   stloc.0
   ldc.i4 0x7ffffc4a
   conv.i4
   not
   ldc.i4 0x800003b5
   bne.un Done

TestCase3050:
   ldc.i4 3150
   stloc.0
   ldc.i4 0x7ffffc49
   conv.i4
   not
   ldc.i4 0x800003b6
   beq TestCase3049
   br Done

TestCase3049:
   ldc.i4 3149
   stloc.0
   ldc.i4 0x7ffffc48
   conv.i4
   not
   ldc.i4 0x800003b7
   bne.un Done

TestCase3048:
   ldc.i4 3148
   stloc.0
   ldc.i4 0x7ffffc47
   conv.i4
   not
   ldc.i4 0x800003b8
   bne.un Done

TestCase3047:
   ldc.i4 3147
   stloc.0
   ldc.i4 0x7ffffc46
   conv.i4
   not
   ldc.i4 0x800003b9
   bne.un Done

TestCase3046:
   ldc.i4 3146
   stloc.0
   ldc.i4 0x7ffffc45
   conv.i4
   not
   ldc.i4 0x800003ba
   bne.un Done

TestCase3045:
   ldc.i4 3145
   stloc.0
   ldc.i4 0x7ffffc44
   conv.i4
   not
   ldc.i4 0x800003bb
   beq TestCase3044
   br Done

TestCase3044:
   ldc.i4 3144
   stloc.0
   ldc.i4 0x7ffffc43
   conv.i4
   not
   ldc.i4 0x800003bc
   bne.un Done

TestCase3043:
   ldc.i4 3143
   stloc.0
   ldc.i4 0x7ffffc42
   conv.i4
   not
   ldc.i4 0x800003bd
   beq TestCase3042
   br Done

TestCase3042:
   ldc.i4 3142
   stloc.0
   ldc.i4 0x7ffffc41
   conv.i4
   not
   ldc.i4 0x800003be
   beq TestCase3041
   br Done

TestCase3041:
   ldc.i4 3141
   stloc.0
   ldc.i4 0x7ffffc40
   conv.i4
   not
   ldc.i4 0x800003bf
   beq TestCase3040
   br Done

TestCase3040:
   ldc.i4 3140
   stloc.0
   ldc.i4 0x7ffffc3f
   conv.i4
   not
   ldc.i4 0x800003c0
   beq TestCase3039
   br Done

TestCase3039:
   ldc.i4 3139
   stloc.0
   ldc.i4 0x7ffffc3e
   conv.i4
   not
   ldc.i4 0x800003c1
   beq TestCase3038
   br Done

TestCase3038:
   ldc.i4 3138
   stloc.0
   ldc.i4 0x7ffffc3d
   conv.i4
   not
   ldc.i4 0x800003c2
   bne.un Done

TestCase3037:
   ldc.i4 3137
   stloc.0
   ldc.i4 0x7ffffc3c
   conv.i4
   not
   ldc.i4 0x800003c3
   beq TestCase3036
   br Done

TestCase3036:
   ldc.i4 3136
   stloc.0
   ldc.i4 0x7ffffc3b
   conv.i4
   not
   ldc.i4 0x800003c4
   beq TestCase3035
   br Done

TestCase3035:
   ldc.i4 3135
   stloc.0
   ldc.i4 0x7ffffc3a
   conv.i4
   not
   ldc.i4 0x800003c5
   bne.un Done

TestCase3034:
   ldc.i4 3134
   stloc.0
   ldc.i4 0x7ffffc39
   conv.i4
   not
   ldc.i4 0x800003c6
   bne.un Done

TestCase3033:
   ldc.i4 3133
   stloc.0
   ldc.i4 0x7ffffc38
   conv.i4
   not
   ldc.i4 0x800003c7
   bne.un Done

TestCase3032:
   ldc.i4 3132
   stloc.0
   ldc.i4 0x7ffffc37
   conv.i4
   not
   ldc.i4 0x800003c8
   bne.un Done

TestCase3031:
   ldc.i4 3131
   stloc.0
   ldc.i4 0x7ffffc36
   conv.i4
   not
   ldc.i4 0x800003c9
   bne.un Done

TestCase3030:
   ldc.i4 3130
   stloc.0
   ldc.i4 0x7ffffc35
   conv.i4
   not
   ldc.i4 0x800003ca
   beq TestCase3029
   br Done

TestCase3029:
   ldc.i4 3129
   stloc.0
   ldc.i4 0x7ffffc34
   conv.i4
   not
   ldc.i4 0x800003cb
   beq TestCase3028
   br Done

TestCase3028:
   ldc.i4 3128
   stloc.0
   ldc.i4 0x7ffffc33
   conv.i4
   not
   ldc.i4 0x800003cc
   beq TestCase3027
   br Done

TestCase3027:
   ldc.i4 3127
   stloc.0
   ldc.i4 0x7ffffc32
   conv.i4
   not
   ldc.i4 0x800003cd
   beq TestCase3026
   br Done

TestCase3026:
   ldc.i4 3126
   stloc.0
   ldc.i4 0x7ffffc31
   conv.i4
   not
   ldc.i4 0x800003ce
   bne.un Done

TestCase3025:
   ldc.i4 3125
   stloc.0
   ldc.i4 0x7ffffc30
   conv.i4
   not
   ldc.i4 0x800003cf
   bne.un Done

TestCase3024:
   ldc.i4 3124
   stloc.0
   ldc.i4 0x7ffffc2f
   conv.i4
   not
   ldc.i4 0x800003d0
   beq TestCase3023
   br Done

TestCase3023:
   ldc.i4 3123
   stloc.0
   ldc.i4 0x7ffffc2e
   conv.i4
   not
   ldc.i4 0x800003d1
   bne.un Done

TestCase3022:
   ldc.i4 3122
   stloc.0
   ldc.i4 0x7ffffc2d
   conv.i4
   not
   ldc.i4 0x800003d2
   bne.un Done

TestCase3021:
   ldc.i4 3121
   stloc.0
   ldc.i4 0x7ffffc2c
   conv.i4
   not
   ldc.i4 0x800003d3
   bne.un Done

TestCase3020:
   ldc.i4 3120
   stloc.0
   ldc.i4 0x7ffffc2b
   conv.i4
   not
   ldc.i4 0x800003d4
   beq TestCase3019
   br Done

TestCase3019:
   ldc.i4 3119
   stloc.0
   ldc.i4 0x7ffffc2a
   conv.i4
   not
   ldc.i4 0x800003d5
   beq TestCase3018
   br Done

TestCase3018:
   ldc.i4 3118
   stloc.0
   ldc.i4 0x7ffffc29
   conv.i4
   not
   ldc.i4 0x800003d6
   bne.un Done

TestCase3017:
   ldc.i4 3117
   stloc.0
   ldc.i4 0x7ffffc28
   conv.i4
   not
   ldc.i4 0x800003d7
   beq TestCase3016
   br Done

TestCase3016:
   ldc.i4 3116
   stloc.0
   ldc.i4 0x7ffffc27
   conv.i4
   not
   ldc.i4 0x800003d8
   bne.un Done

TestCase3015:
   ldc.i4 3115
   stloc.0
   ldc.i4 0x7ffffc26
   conv.i4
   not
   ldc.i4 0x800003d9
   bne.un Done

TestCase3014:
   ldc.i4 3114
   stloc.0
   ldc.i4 0x7ffffc25
   conv.i4
   not
   ldc.i4 0x800003da
   beq TestCase3013
   br Done

TestCase3013:
   ldc.i4 3113
   stloc.0
   ldc.i4 0x7ffffc24
   conv.i4
   not
   ldc.i4 0x800003db
   beq TestCase3012
   br Done

TestCase3012:
   ldc.i4 3112
   stloc.0
   ldc.i4 0x7ffffc23
   conv.i4
   not
   ldc.i4 0x800003dc
   beq TestCase3011
   br Done

TestCase3011:
   ldc.i4 3111
   stloc.0
   ldc.i4 0x7ffffc22
   conv.i4
   not
   ldc.i4 0x800003dd
   beq TestCase3010
   br Done

TestCase3010:
   ldc.i4 3110
   stloc.0
   ldc.i4 0x7ffffc21
   conv.i4
   not
   ldc.i4 0x800003de
   bne.un Done

TestCase3009:
   ldc.i4 3109
   stloc.0
   ldc.i4 0x7ffffc20
   conv.i4
   not
   ldc.i4 0x800003df
   beq TestCase3008
   br Done

TestCase3008:
   ldc.i4 3108
   stloc.0
   ldc.i4 0x7ffffc1f
   conv.i4
   not
   ldc.i4 0x800003e0
   bne.un Done

TestCase3007:
   ldc.i4 3107
   stloc.0
   ldc.i4 0x7ffffc1e
   conv.i4
   not
   ldc.i4 0x800003e1
   bne.un Done

TestCase3006:
   ldc.i4 3106
   stloc.0
   ldc.i4 0x7ffffc1d
   conv.i4
   not
   ldc.i4 0x800003e2
   bne.un Done

TestCase3005:
   ldc.i4 3105
   stloc.0
   ldc.i4 0x7ffffc1c
   conv.i4
   not
   ldc.i4 0x800003e3
   beq TestCase3004
   br Done

TestCase3004:
   ldc.i4 3104
   stloc.0
   ldc.i4 0x7ffffc1b
   conv.i4
   not
   ldc.i4 0x800003e4
   bne.un Done

TestCase3003:
   ldc.i4 3103
   stloc.0
   ldc.i4 0x7ffffc1a
   conv.i4
   not
   ldc.i4 0x800003e5
   bne.un Done

TestCase3002:
   ldc.i4 3102
   stloc.0
   ldc.i4 0x7ffffc19
   conv.i4
   not
   ldc.i4 0x800003e6
   bne.un Done

TestCase3001:
   ldc.i4 3101
   stloc.0
   ldc.i4 0x7ffffc18
   conv.i4
   not
   ldc.i4 0x800003e7
   beq TestCase3000
   br Done

TestCase3000:
   ldc.i4 3100
   stloc.0
   ldc.i4 0x1
   conv.i4
   not
   ldc.i4 0xfffffffe
   beq TestCase2999
   br Done

TestCase2999:
   ldc.i4 3099
   stloc.0
   ldc.i4 0x2
   conv.i4
   not
   ldc.i4 0xfffffffd
   bne.un Done

TestCase2998:
   ldc.i4 3098
   stloc.0
   ldc.i4 0x4
   conv.i4
   not
   ldc.i4 0xfffffffb
   bne.un Done

TestCase2997:
   ldc.i4 3097
   stloc.0
   ldc.i4 0x8
   conv.i4
   not
   ldc.i4 0xfffffff7
   beq TestCase2996
   br Done

TestCase2996:
   ldc.i4 3096
   stloc.0
   ldc.i4 0x10
   conv.i4
   not
   ldc.i4 0xffffffef
   bne.un Done

TestCase2995:
   ldc.i4 3095
   stloc.0
   ldc.i4 0x20
   conv.i4
   not
   ldc.i4 0xffffffdf
   bne.un Done

TestCase2994:
   ldc.i4 3094
   stloc.0
   ldc.i4 0x40
   conv.i4
   not
   ldc.i4 0xffffffbf
   beq TestCase2993
   br Done

TestCase2993:
   ldc.i4 3093
   stloc.0
   ldc.i4 0x80
   conv.i4
   not
   ldc.i4 0xffffff7f
   bne.un Done

TestCase2992:
   ldc.i4 3092
   stloc.0
   ldc.i4 0x100
   conv.i4
   not
   ldc.i4 0xfffffeff
   bne.un Done

TestCase2991:
   ldc.i4 3091
   stloc.0
   ldc.i4 0x200
   conv.i4
   not
   ldc.i4 0xfffffdff
   bne.un Done

TestCase2990:
   ldc.i4 3090
   stloc.0
   ldc.i4 0x400
   conv.i4
   not
   ldc.i4 0xfffffbff
   bne.un Done

TestCase2989:
   ldc.i4 3089
   stloc.0
   ldc.i4 0x800
   conv.i4
   not
   ldc.i4 0xfffff7ff
   beq TestCase2988
   br Done

TestCase2988:
   ldc.i4 3088
   stloc.0
   ldc.i4 0x1000
   conv.i4
   not
   ldc.i4 0xffffefff
   bne.un Done

TestCase2987:
   ldc.i4 3087
   stloc.0
   ldc.i4 0x2000
   conv.i4
   not
   ldc.i4 0xffffdfff
   beq TestCase2986
   br Done

TestCase2986:
   ldc.i4 3086
   stloc.0
   ldc.i4 0x4000
   conv.i4
   not
   ldc.i4 0xffffbfff
   bne.un Done

TestCase2985:
   ldc.i4 3085
   stloc.0
   ldc.i4 0x8000
   conv.i4
   not
   ldc.i4 0xffff7fff
   beq TestCase2984
   br Done

TestCase2984:
   ldc.i4 3084
   stloc.0
   ldc.i4 0x10000
   conv.i4
   not
   ldc.i4 0xfffeffff
   beq TestCase2983
   br Done

TestCase2983:
   ldc.i4 3083
   stloc.0
   ldc.i4 0x565e
   conv.i4
   not
   ldc.i4 0xffffa9a1
   beq TestCase2982
   br Done

TestCase2982:
   ldc.i4 3082
   stloc.0
   ldc.i4 0xffff8b53
   conv.i4
   not
   ldc.i4 0x74ac
   beq TestCase2981
   br Done

TestCase2981:
   ldc.i4 3081
   stloc.0
   ldc.i4 0x1363
   conv.i4
   not
   ldc.i4 0xffffec9c
   beq TestCase2980
   br Done

TestCase2980:
   ldc.i4 3080
   stloc.0
   ldc.i4 0xffffcb31
   conv.i4
   not
   ldc.i4 0x34ce
   beq TestCase2979
   br Done

TestCase2979:
   ldc.i4 3079
   stloc.0
   ldc.i4 0xfffff0f2
   conv.i4
   not
   ldc.i4 0xf0d
   bne.un Done

TestCase2978:
   ldc.i4 3078
   stloc.0
   ldc.i4 0xffffee50
   conv.i4
   not
   ldc.i4 0x11af
   beq TestCase2977
   br Done

TestCase2977:
   ldc.i4 3077
   stloc.0
   ldc.i4 0xffffcb3d
   conv.i4
   not
   ldc.i4 0x34c2
   bne.un Done

TestCase2976:
   ldc.i4 3076
   stloc.0
   ldc.i4 0xffffc60a
   conv.i4
   not
   ldc.i4 0x39f5
   bne.un Done

TestCase2975:
   ldc.i4 3075
   stloc.0
   ldc.i4 0xffffc894
   conv.i4
   not
   ldc.i4 0x376b
   bne.un Done

TestCase2974:
   ldc.i4 3074
   stloc.0
   ldc.i4 0x519e
   conv.i4
   not
   ldc.i4 0xffffae61
   beq TestCase2973
   br Done

TestCase2973:
   ldc.i4 3073
   stloc.0
   ldc.i4 0xffffe8d7
   conv.i4
   not
   ldc.i4 0x1728
   beq TestCase2972
   br Done

TestCase2972:
   ldc.i4 3072
   stloc.0
   ldc.i4 0xffffb296
   conv.i4
   not
   ldc.i4 0x4d69
   beq TestCase2971
   br Done

TestCase2971:
   ldc.i4 3071
   stloc.0
   ldc.i4 0xffffccb0
   conv.i4
   not
   ldc.i4 0x334f
   bne.un Done

TestCase2970:
   ldc.i4 3070
   stloc.0
   ldc.i4 0xffffb316
   conv.i4
   not
   ldc.i4 0x4ce9
   bne.un Done

TestCase2969:
   ldc.i4 3069
   stloc.0
   ldc.i4 0x7de5
   conv.i4
   not
   ldc.i4 0xffff821a
   beq TestCase2968
   br Done

TestCase2968:
   ldc.i4 3068
   stloc.0
   ldc.i4 0x599d
   conv.i4
   not
   ldc.i4 0xffffa662
   beq TestCase2967
   br Done

TestCase2967:
   ldc.i4 3067
   stloc.0
   ldc.i4 0x21d8
   conv.i4
   not
   ldc.i4 0xffffde27
   bne.un Done

TestCase2966:
   ldc.i4 3066
   stloc.0
   ldc.i4 0x16d7
   conv.i4
   not
   ldc.i4 0xffffe928
   beq TestCase2965
   br Done

TestCase2965:
   ldc.i4 3065
   stloc.0
   ldc.i4 0xffff805f
   conv.i4
   not
   ldc.i4 0x7fa0
   bne.un Done

TestCase2964:
   ldc.i4 3064
   stloc.0
   ldc.i4 0x17b
   conv.i4
   not
   ldc.i4 0xfffffe84
   beq TestCase2963
   br Done

TestCase2963:
   ldc.i4 3063
   stloc.0
   ldc.i4 0x5c20
   conv.i4
   not
   ldc.i4 0xffffa3df
   bne.un Done

TestCase2962:
   ldc.i4 3062
   stloc.0
   ldc.i4 0x2f9f
   conv.i4
   not
   ldc.i4 0xffffd060
   beq TestCase2961
   br Done

TestCase2961:
   ldc.i4 3061
   stloc.0
   ldc.i4 0xbbd
   conv.i4
   not
   ldc.i4 0xfffff442
   bne.un Done

TestCase2960:
   ldc.i4 3060
   stloc.0
   ldc.i4 0xffffa049
   conv.i4
   not
   ldc.i4 0x5fb6
   beq TestCase2959
   br Done

TestCase2959:
   ldc.i4 3059
   stloc.0
   ldc.i4 0x5842
   conv.i4
   not
   ldc.i4 0xffffa7bd
   bne.un Done

TestCase2958:
   ldc.i4 3058
   stloc.0
   ldc.i4 0xffff9c1f
   conv.i4
   not
   ldc.i4 0x63e0
   bne.un Done

TestCase2957:
   ldc.i4 3057
   stloc.0
   ldc.i4 0xffff9616
   conv.i4
   not
   ldc.i4 0x69e9
   bne.un Done

TestCase2956:
   ldc.i4 3056
   stloc.0
   ldc.i4 0xffff9ea6
   conv.i4
   not
   ldc.i4 0x6159
   bne.un Done

TestCase2955:
   ldc.i4 3055
   stloc.0
   ldc.i4 0x2c90
   conv.i4
   not
   ldc.i4 0xffffd36f
   beq TestCase2954
   br Done

TestCase2954:
   ldc.i4 3054
   stloc.0
   ldc.i4 0xffffbab0
   conv.i4
   not
   ldc.i4 0x454f
   beq TestCase2953
   br Done

TestCase2953:
   ldc.i4 3053
   stloc.0
   ldc.i4 0xffffeff6
   conv.i4
   not
   ldc.i4 0x1009
   beq TestCase2952
   br Done

TestCase2952:
   ldc.i4 3052
   stloc.0
   ldc.i4 0x789
   conv.i4
   not
   ldc.i4 0xfffff876
   bne.un Done

TestCase2951:
   ldc.i4 3051
   stloc.0
   ldc.i4 0x2f10
   conv.i4
   not
   ldc.i4 0xffffd0ef
   beq TestCase2950
   br Done

TestCase2950:
   ldc.i4 3050
   stloc.0
   ldc.i4 0x2899
   conv.i4
   not
   ldc.i4 0xffffd766
   bne.un Done

TestCase2949:
   ldc.i4 3049
   stloc.0
   ldc.i4 0xffff95ba
   conv.i4
   not
   ldc.i4 0x6a45
   bne.un Done

TestCase2948:
   ldc.i4 3048
   stloc.0
   ldc.i4 0x1c97
   conv.i4
   not
   ldc.i4 0xffffe368
   beq TestCase2947
   br Done

TestCase2947:
   ldc.i4 3047
   stloc.0
   ldc.i4 0x6d38
   conv.i4
   not
   ldc.i4 0xffff92c7
   bne.un Done

TestCase2946:
   ldc.i4 3046
   stloc.0
   ldc.i4 0x5494
   conv.i4
   not
   ldc.i4 0xffffab6b
   bne.un Done

TestCase2945:
   ldc.i4 3045
   stloc.0
   ldc.i4 0x716f
   conv.i4
   not
   ldc.i4 0xffff8e90
   beq TestCase2944
   br Done

TestCase2944:
   ldc.i4 3044
   stloc.0
   ldc.i4 0x284a
   conv.i4
   not
   ldc.i4 0xffffd7b5
   bne.un Done

TestCase2943:
   ldc.i4 3043
   stloc.0
   ldc.i4 0x77ea
   conv.i4
   not
   ldc.i4 0xffff8815
   bne.un Done

TestCase2942:
   ldc.i4 3042
   stloc.0
   ldc.i4 0xffffb151
   conv.i4
   not
   ldc.i4 0x4eae
   beq TestCase2941
   br Done

TestCase2941:
   ldc.i4 3041
   stloc.0
   ldc.i4 0xffffd467
   conv.i4
   not
   ldc.i4 0x2b98
   bne.un Done

TestCase2940:
   ldc.i4 3040
   stloc.0
   ldc.i4 0xffffd08f
   conv.i4
   not
   ldc.i4 0x2f70
   bne.un Done

TestCase2939:
   ldc.i4 3039
   stloc.0
   ldc.i4 0xffffc04e
   conv.i4
   not
   ldc.i4 0x3fb1
   beq TestCase2938
   br Done

TestCase2938:
   ldc.i4 3038
   stloc.0
   ldc.i4 0xffff8f6a
   conv.i4
   not
   ldc.i4 0x7095
   beq TestCase2937
   br Done

TestCase2937:
   ldc.i4 3037
   stloc.0
   ldc.i4 0x72f5
   conv.i4
   not
   ldc.i4 0xffff8d0a
   bne.un Done

TestCase2936:
   ldc.i4 3036
   stloc.0
   ldc.i4 0xfffff55a
   conv.i4
   not
   ldc.i4 0xaa5
   bne.un Done

TestCase2935:
   ldc.i4 3035
   stloc.0
   ldc.i4 0xffff82c2
   conv.i4
   not
   ldc.i4 0x7d3d
   bne.un Done

TestCase2934:
   ldc.i4 3034
   stloc.0
   ldc.i4 0x3326
   conv.i4
   not
   ldc.i4 0xffffccd9
   bne.un Done

TestCase2933:
   ldc.i4 3033
   stloc.0
   ldc.i4 0xffffe111
   conv.i4
   not
   ldc.i4 0x1eee
   bne.un Done

TestCase2932:
   ldc.i4 3032
   stloc.0
   ldc.i4 0x605f
   conv.i4
   not
   ldc.i4 0xffff9fa0
   bne.un Done

TestCase2931:
   ldc.i4 3031
   stloc.0
   ldc.i4 0x621d
   conv.i4
   not
   ldc.i4 0xffff9de2
   beq TestCase2930
   br Done

TestCase2930:
   ldc.i4 3030
   stloc.0
   ldc.i4 0xffffa56c
   conv.i4
   not
   ldc.i4 0x5a93
   beq TestCase2929
   br Done

TestCase2929:
   ldc.i4 3029
   stloc.0
   ldc.i4 0x6b88
   conv.i4
   not
   ldc.i4 0xffff9477
   beq TestCase2928
   br Done

TestCase2928:
   ldc.i4 3028
   stloc.0
   ldc.i4 0xfffff1c6
   conv.i4
   not
   ldc.i4 0xe39
   beq TestCase2927
   br Done

TestCase2927:
   ldc.i4 3027
   stloc.0
   ldc.i4 0xdbc
   conv.i4
   not
   ldc.i4 0xfffff243
   beq TestCase2926
   br Done

TestCase2926:
   ldc.i4 3026
   stloc.0
   ldc.i4 0xffff9028
   conv.i4
   not
   ldc.i4 0x6fd7
   bne.un Done

TestCase2925:
   ldc.i4 3025
   stloc.0
   ldc.i4 0xffffc50b
   conv.i4
   not
   ldc.i4 0x3af4
   bne.un Done

TestCase2924:
   ldc.i4 3024
   stloc.0
   ldc.i4 0xd89
   conv.i4
   not
   ldc.i4 0xfffff276
   bne.un Done

TestCase2923:
   ldc.i4 3023
   stloc.0
   ldc.i4 0x29d
   conv.i4
   not
   ldc.i4 0xfffffd62
   bne.un Done

TestCase2922:
   ldc.i4 3022
   stloc.0
   ldc.i4 0xffffcca1
   conv.i4
   not
   ldc.i4 0x335e
   beq TestCase2921
   br Done

TestCase2921:
   ldc.i4 3021
   stloc.0
   ldc.i4 0xd77
   conv.i4
   not
   ldc.i4 0xfffff288
   bne.un Done

TestCase2920:
   ldc.i4 3020
   stloc.0
   ldc.i4 0xffffe0e4
   conv.i4
   not
   ldc.i4 0x1f1b
   beq TestCase2919
   br Done

TestCase2919:
   ldc.i4 3019
   stloc.0
   ldc.i4 0x17d8
   conv.i4
   not
   ldc.i4 0xffffe827
   beq TestCase2918
   br Done

TestCase2918:
   ldc.i4 3018
   stloc.0
   ldc.i4 0xffff8b0f
   conv.i4
   not
   ldc.i4 0x74f0
   beq TestCase2917
   br Done

TestCase2917:
   ldc.i4 3017
   stloc.0
   ldc.i4 0x4b7c
   conv.i4
   not
   ldc.i4 0xffffb483
   beq TestCase2916
   br Done

TestCase2916:
   ldc.i4 3016
   stloc.0
   ldc.i4 0xffffb5b2
   conv.i4
   not
   ldc.i4 0x4a4d
   beq TestCase2915
   br Done

TestCase2915:
   ldc.i4 3015
   stloc.0
   ldc.i4 0xffffdd29
   conv.i4
   not
   ldc.i4 0x22d6
   bne.un Done

TestCase2914:
   ldc.i4 3014
   stloc.0
   ldc.i4 0xffff8320
   conv.i4
   not
   ldc.i4 0x7cdf
   bne.un Done

TestCase2913:
   ldc.i4 3013
   stloc.0
   ldc.i4 0xffffc20a
   conv.i4
   not
   ldc.i4 0x3df5
   beq TestCase2912
   br Done

TestCase2912:
   ldc.i4 3012
   stloc.0
   ldc.i4 0x7b52
   conv.i4
   not
   ldc.i4 0xffff84ad
   beq TestCase2911
   br Done

TestCase2911:
   ldc.i4 3011
   stloc.0
   ldc.i4 0x554d
   conv.i4
   not
   ldc.i4 0xffffaab2
   bne.un Done

TestCase2910:
   ldc.i4 3010
   stloc.0
   ldc.i4 0xffff8d8f
   conv.i4
   not
   ldc.i4 0x7270
   beq TestCase2909
   br Done

TestCase2909:
   ldc.i4 3009
   stloc.0
   ldc.i4 0xffffaf05
   conv.i4
   not
   ldc.i4 0x50fa
   bne.un Done

TestCase2908:
   ldc.i4 3008
   stloc.0
   ldc.i4 0x4cbe
   conv.i4
   not
   ldc.i4 0xffffb341
   beq TestCase2907
   br Done

TestCase2907:
   ldc.i4 3007
   stloc.0
   ldc.i4 0xb77
   conv.i4
   not
   ldc.i4 0xfffff488
   bne.un Done

TestCase2906:
   ldc.i4 3006
   stloc.0
   ldc.i4 0x6b8f
   conv.i4
   not
   ldc.i4 0xffff9470
   bne.un Done

TestCase2905:
   ldc.i4 3005
   stloc.0
   ldc.i4 0x7c9e
   conv.i4
   not
   ldc.i4 0xffff8361
   bne.un Done

TestCase2904:
   ldc.i4 3004
   stloc.0
   ldc.i4 0xffffb7a9
   conv.i4
   not
   ldc.i4 0x4856
   bne.un Done

TestCase2903:
   ldc.i4 3003
   stloc.0
   ldc.i4 0x696
   conv.i4
   not
   ldc.i4 0xfffff969
   bne.un Done

TestCase2902:
   ldc.i4 3002
   stloc.0
   ldc.i4 0x11ad
   conv.i4
   not
   ldc.i4 0xffffee52
   bne.un Done

TestCase2901:
   ldc.i4 3001
   stloc.0
   ldc.i4 0xffff8ff4
   conv.i4
   not
   ldc.i4 0x700b
   bne.un Done

TestCase2900:
   ldc.i4 3000
   stloc.0
   ldc.i4 0xffffd4e1
   conv.i4
   not
   ldc.i4 0x2b1e
   beq TestCase2899
   br Done

TestCase2899:
   ldc.i4 2999
   stloc.0
   ldc.i4 0x4fac
   conv.i4
   not
   ldc.i4 0xffffb053
   bne.un Done

TestCase2898:
   ldc.i4 2998
   stloc.0
   ldc.i4 0xfffff804
   conv.i4
   not
   ldc.i4 0x7fb
   bne.un Done

TestCase2897:
   ldc.i4 2997
   stloc.0
   ldc.i4 0xffffa516
   conv.i4
   not
   ldc.i4 0x5ae9
   bne.un Done

TestCase2896:
   ldc.i4 2996
   stloc.0
   ldc.i4 0xffff8764
   conv.i4
   not
   ldc.i4 0x789b
   bne.un Done

TestCase2895:
   ldc.i4 2995
   stloc.0
   ldc.i4 0xffffc18b
   conv.i4
   not
   ldc.i4 0x3e74
   bne.un Done

TestCase2894:
   ldc.i4 2994
   stloc.0
   ldc.i4 0x7ecd
   conv.i4
   not
   ldc.i4 0xffff8132
   bne.un Done

TestCase2893:
   ldc.i4 2993
   stloc.0
   ldc.i4 0xffff8f14
   conv.i4
   not
   ldc.i4 0x70eb
   beq TestCase2892
   br Done

TestCase2892:
   ldc.i4 2992
   stloc.0
   ldc.i4 0x696a
   conv.i4
   not
   ldc.i4 0xffff9695
   beq TestCase2891
   br Done

TestCase2891:
   ldc.i4 2991
   stloc.0
   ldc.i4 0xffff9f58
   conv.i4
   not
   ldc.i4 0x60a7
   beq TestCase2890
   br Done

TestCase2890:
   ldc.i4 2990
   stloc.0
   ldc.i4 0xffffdfb8
   conv.i4
   not
   ldc.i4 0x2047
   bne.un Done

TestCase2889:
   ldc.i4 2989
   stloc.0
   ldc.i4 0xfffffc47
   conv.i4
   not
   ldc.i4 0x3b8
   bne.un Done

TestCase2888:
   ldc.i4 2988
   stloc.0
   ldc.i4 0x103
   conv.i4
   not
   ldc.i4 0xfffffefc
   bne.un Done

TestCase2887:
   ldc.i4 2987
   stloc.0
   ldc.i4 0x3d6f
   conv.i4
   not
   ldc.i4 0xffffc290
   bne.un Done

TestCase2886:
   ldc.i4 2986
   stloc.0
   ldc.i4 0xffffbdb2
   conv.i4
   not
   ldc.i4 0x424d
   beq TestCase2885
   br Done

TestCase2885:
   ldc.i4 2985
   stloc.0
   ldc.i4 0xffffb102
   conv.i4
   not
   ldc.i4 0x4efd
   beq TestCase2884
   br Done

TestCase2884:
   ldc.i4 2984
   stloc.0
   ldc.i4 0xffffa934
   conv.i4
   not
   ldc.i4 0x56cb
   beq TestCase2883
   br Done

TestCase2883:
   ldc.i4 2983
   stloc.0
   ldc.i4 0xffffcedd
   conv.i4
   not
   ldc.i4 0x3122
   bne.un Done

TestCase2882:
   ldc.i4 2982
   stloc.0
   ldc.i4 0xfffff656
   conv.i4
   not
   ldc.i4 0x9a9
   bne.un Done

TestCase2881:
   ldc.i4 2981
   stloc.0
   ldc.i4 0xffffad30
   conv.i4
   not
   ldc.i4 0x52cf
   bne.un Done

TestCase2880:
   ldc.i4 2980
   stloc.0
   ldc.i4 0xffffa19c
   conv.i4
   not
   ldc.i4 0x5e63
   beq TestCase2879
   br Done

TestCase2879:
   ldc.i4 2979
   stloc.0
   ldc.i4 0xffffa2b5
   conv.i4
   not
   ldc.i4 0x5d4a
   beq TestCase2878
   br Done

TestCase2878:
   ldc.i4 2978
   stloc.0
   ldc.i4 0x6a7f
   conv.i4
   not
   ldc.i4 0xffff9580
   beq TestCase2877
   br Done

TestCase2877:
   ldc.i4 2977
   stloc.0
   ldc.i4 0x5847
   conv.i4
   not
   ldc.i4 0xffffa7b8
   bne.un Done

TestCase2876:
   ldc.i4 2976
   stloc.0
   ldc.i4 0x3b28
   conv.i4
   not
   ldc.i4 0xffffc4d7
   bne.un Done

TestCase2875:
   ldc.i4 2975
   stloc.0
   ldc.i4 0xffff9729
   conv.i4
   not
   ldc.i4 0x68d6
   bne.un Done

TestCase2874:
   ldc.i4 2974
   stloc.0
   ldc.i4 0xffffce13
   conv.i4
   not
   ldc.i4 0x31ec
   beq TestCase2873
   br Done

TestCase2873:
   ldc.i4 2973
   stloc.0
   ldc.i4 0xffff83a9
   conv.i4
   not
   ldc.i4 0x7c56
   beq TestCase2872
   br Done

TestCase2872:
   ldc.i4 2972
   stloc.0
   ldc.i4 0x6b0a
   conv.i4
   not
   ldc.i4 0xffff94f5
   bne.un Done

TestCase2871:
   ldc.i4 2971
   stloc.0
   ldc.i4 0xc99
   conv.i4
   not
   ldc.i4 0xfffff366
   bne.un Done

TestCase2870:
   ldc.i4 2970
   stloc.0
   ldc.i4 0x505
   conv.i4
   not
   ldc.i4 0xfffffafa
   beq TestCase2869
   br Done

TestCase2869:
   ldc.i4 2969
   stloc.0
   ldc.i4 0x5cf1
   conv.i4
   not
   ldc.i4 0xffffa30e
   bne.un Done

TestCase2868:
   ldc.i4 2968
   stloc.0
   ldc.i4 0xffffab6e
   conv.i4
   not
   ldc.i4 0x5491
   bne.un Done

TestCase2867:
   ldc.i4 2967
   stloc.0
   ldc.i4 0xffffd7fd
   conv.i4
   not
   ldc.i4 0x2802
   beq TestCase2866
   br Done

TestCase2866:
   ldc.i4 2966
   stloc.0
   ldc.i4 0xffffff11
   conv.i4
   not
   ldc.i4 0xee
   beq TestCase2865
   br Done

TestCase2865:
   ldc.i4 2965
   stloc.0
   ldc.i4 0x445a
   conv.i4
   not
   ldc.i4 0xffffbba5
   beq TestCase2864
   br Done

TestCase2864:
   ldc.i4 2964
   stloc.0
   ldc.i4 0xfffffbcc
   conv.i4
   not
   ldc.i4 0x433
   bne.un Done

TestCase2863:
   ldc.i4 2963
   stloc.0
   ldc.i4 0x5a27
   conv.i4
   not
   ldc.i4 0xffffa5d8
   bne.un Done

TestCase2862:
   ldc.i4 2962
   stloc.0
   ldc.i4 0xffff97a7
   conv.i4
   not
   ldc.i4 0x6858
   beq TestCase2861
   br Done

TestCase2861:
   ldc.i4 2961
   stloc.0
   ldc.i4 0x6eca
   conv.i4
   not
   ldc.i4 0xffff9135
   bne.un Done

TestCase2860:
   ldc.i4 2960
   stloc.0
   ldc.i4 0x24d4
   conv.i4
   not
   ldc.i4 0xffffdb2b
   bne.un Done

TestCase2859:
   ldc.i4 2959
   stloc.0
   ldc.i4 0xffffa515
   conv.i4
   not
   ldc.i4 0x5aea
   beq TestCase2858
   br Done

TestCase2858:
   ldc.i4 2958
   stloc.0
   ldc.i4 0xa22
   conv.i4
   not
   ldc.i4 0xfffff5dd
   beq TestCase2857
   br Done

TestCase2857:
   ldc.i4 2957
   stloc.0
   ldc.i4 0xffffb9b8
   conv.i4
   not
   ldc.i4 0x4647
   beq TestCase2856
   br Done

TestCase2856:
   ldc.i4 2956
   stloc.0
   ldc.i4 0x7f97
   conv.i4
   not
   ldc.i4 0xffff8068
   beq TestCase2855
   br Done

TestCase2855:
   ldc.i4 2955
   stloc.0
   ldc.i4 0xffffef75
   conv.i4
   not
   ldc.i4 0x108a
   beq TestCase2854
   br Done

TestCase2854:
   ldc.i4 2954
   stloc.0
   ldc.i4 0xffffb2ba
   conv.i4
   not
   ldc.i4 0x4d45
   beq TestCase2853
   br Done

TestCase2853:
   ldc.i4 2953
   stloc.0
   ldc.i4 0x30c4
   conv.i4
   not
   ldc.i4 0xffffcf3b
   beq TestCase2852
   br Done

TestCase2852:
   ldc.i4 2952
   stloc.0
   ldc.i4 0xeeb
   conv.i4
   not
   ldc.i4 0xfffff114
   beq TestCase2851
   br Done

TestCase2851:
   ldc.i4 2951
   stloc.0
   ldc.i4 0xffffe2ac
   conv.i4
   not
   ldc.i4 0x1d53
   beq TestCase2850
   br Done

TestCase2850:
   ldc.i4 2950
   stloc.0
   ldc.i4 0xfffff1f4
   conv.i4
   not
   ldc.i4 0xe0b
   bne.un Done

TestCase2849:
   ldc.i4 2949
   stloc.0
   ldc.i4 0x4c75
   conv.i4
   not
   ldc.i4 0xffffb38a
   beq TestCase2848
   br Done

TestCase2848:
   ldc.i4 2948
   stloc.0
   ldc.i4 0x70d8
   conv.i4
   not
   ldc.i4 0xffff8f27
   bne.un Done

TestCase2847:
   ldc.i4 2947
   stloc.0
   ldc.i4 0x619e
   conv.i4
   not
   ldc.i4 0xffff9e61
   beq TestCase2846
   br Done

TestCase2846:
   ldc.i4 2946
   stloc.0
   ldc.i4 0xffffc792
   conv.i4
   not
   ldc.i4 0x386d
   bne.un Done

TestCase2845:
   ldc.i4 2945
   stloc.0
   ldc.i4 0xffffc98c
   conv.i4
   not
   ldc.i4 0x3673
   bne.un Done

TestCase2844:
   ldc.i4 2944
   stloc.0
   ldc.i4 0xffffbb77
   conv.i4
   not
   ldc.i4 0x4488
   beq TestCase2843
   br Done

TestCase2843:
   ldc.i4 2943
   stloc.0
   ldc.i4 0xffffd1f6
   conv.i4
   not
   ldc.i4 0x2e09
   bne.un Done

TestCase2842:
   ldc.i4 2942
   stloc.0
   ldc.i4 0xffffb437
   conv.i4
   not
   ldc.i4 0x4bc8
   bne.un Done

TestCase2841:
   ldc.i4 2941
   stloc.0
   ldc.i4 0xffffc867
   conv.i4
   not
   ldc.i4 0x3798
   beq TestCase2840
   br Done

TestCase2840:
   ldc.i4 2940
   stloc.0
   ldc.i4 0x3f15
   conv.i4
   not
   ldc.i4 0xffffc0ea
   beq TestCase2839
   br Done

TestCase2839:
   ldc.i4 2939
   stloc.0
   ldc.i4 0x21fd
   conv.i4
   not
   ldc.i4 0xffffde02
   beq TestCase2838
   br Done

TestCase2838:
   ldc.i4 2938
   stloc.0
   ldc.i4 0x33c5
   conv.i4
   not
   ldc.i4 0xffffcc3a
   bne.un Done

TestCase2837:
   ldc.i4 2937
   stloc.0
   ldc.i4 0x7395
   conv.i4
   not
   ldc.i4 0xffff8c6a
   beq TestCase2836
   br Done

TestCase2836:
   ldc.i4 2936
   stloc.0
   ldc.i4 0xfffffc6e
   conv.i4
   not
   ldc.i4 0x391
   bne.un Done

TestCase2835:
   ldc.i4 2935
   stloc.0
   ldc.i4 0xffff8da4
   conv.i4
   not
   ldc.i4 0x725b
   beq TestCase2834
   br Done

TestCase2834:
   ldc.i4 2934
   stloc.0
   ldc.i4 0xffffdd7b
   conv.i4
   not
   ldc.i4 0x2284
   bne.un Done

TestCase2833:
   ldc.i4 2933
   stloc.0
   ldc.i4 0xffff89b0
   conv.i4
   not
   ldc.i4 0x764f
   beq TestCase2832
   br Done

TestCase2832:
   ldc.i4 2932
   stloc.0
   ldc.i4 0xfffff5a5
   conv.i4
   not
   ldc.i4 0xa5a
   bne.un Done

TestCase2831:
   ldc.i4 2931
   stloc.0
   ldc.i4 0xffffeb78
   conv.i4
   not
   ldc.i4 0x1487
   beq TestCase2830
   br Done

TestCase2830:
   ldc.i4 2930
   stloc.0
   ldc.i4 0x5812
   conv.i4
   not
   ldc.i4 0xffffa7ed
   bne.un Done

TestCase2829:
   ldc.i4 2929
   stloc.0
   ldc.i4 0x3b85
   conv.i4
   not
   ldc.i4 0xffffc47a
   beq TestCase2828
   br Done

TestCase2828:
   ldc.i4 2928
   stloc.0
   ldc.i4 0x23a0
   conv.i4
   not
   ldc.i4 0xffffdc5f
   beq TestCase2827
   br Done

TestCase2827:
   ldc.i4 2927
   stloc.0
   ldc.i4 0xfffff664
   conv.i4
   not
   ldc.i4 0x99b
   beq TestCase2826
   br Done

TestCase2826:
   ldc.i4 2926
   stloc.0
   ldc.i4 0xfffffcfb
   conv.i4
   not
   ldc.i4 0x304
   beq TestCase2825
   br Done

TestCase2825:
   ldc.i4 2925
   stloc.0
   ldc.i4 0xc9b
   conv.i4
   not
   ldc.i4 0xfffff364
   bne.un Done

TestCase2824:
   ldc.i4 2924
   stloc.0
   ldc.i4 0xffff9ed1
   conv.i4
   not
   ldc.i4 0x612e
   bne.un Done

TestCase2823:
   ldc.i4 2923
   stloc.0
   ldc.i4 0xffffba1b
   conv.i4
   not
   ldc.i4 0x45e4
   bne.un Done

TestCase2822:
   ldc.i4 2922
   stloc.0
   ldc.i4 0xffffd98d
   conv.i4
   not
   ldc.i4 0x2672
   beq TestCase2821
   br Done

TestCase2821:
   ldc.i4 2921
   stloc.0
   ldc.i4 0x50c5
   conv.i4
   not
   ldc.i4 0xffffaf3a
   bne.un Done

TestCase2820:
   ldc.i4 2920
   stloc.0
   ldc.i4 0xffff8c7e
   conv.i4
   not
   ldc.i4 0x7381
   beq TestCase2819
   br Done

TestCase2819:
   ldc.i4 2919
   stloc.0
   ldc.i4 0x70a2
   conv.i4
   not
   ldc.i4 0xffff8f5d
   beq TestCase2818
   br Done

TestCase2818:
   ldc.i4 2918
   stloc.0
   ldc.i4 0x311d
   conv.i4
   not
   ldc.i4 0xffffcee2
   bne.un Done

TestCase2817:
   ldc.i4 2917
   stloc.0
   ldc.i4 0x2b5c
   conv.i4
   not
   ldc.i4 0xffffd4a3
   bne.un Done

TestCase2816:
   ldc.i4 2916
   stloc.0
   ldc.i4 0x4608
   conv.i4
   not
   ldc.i4 0xffffb9f7
   bne.un Done

TestCase2815:
   ldc.i4 2915
   stloc.0
   ldc.i4 0x77fd
   conv.i4
   not
   ldc.i4 0xffff8802
   bne.un Done

TestCase2814:
   ldc.i4 2914
   stloc.0
   ldc.i4 0x31d8
   conv.i4
   not
   ldc.i4 0xffffce27
   bne.un Done

TestCase2813:
   ldc.i4 2913
   stloc.0
   ldc.i4 0x47b
   conv.i4
   not
   ldc.i4 0xfffffb84
   bne.un Done

TestCase2812:
   ldc.i4 2912
   stloc.0
   ldc.i4 0x66b7
   conv.i4
   not
   ldc.i4 0xffff9948
   beq TestCase2811
   br Done

TestCase2811:
   ldc.i4 2911
   stloc.0
   ldc.i4 0xffffcab8
   conv.i4
   not
   ldc.i4 0x3547
   beq TestCase2810
   br Done

TestCase2810:
   ldc.i4 2910
   stloc.0
   ldc.i4 0x5539
   conv.i4
   not
   ldc.i4 0xffffaac6
   bne.un Done

TestCase2809:
   ldc.i4 2909
   stloc.0
   ldc.i4 0x57db
   conv.i4
   not
   ldc.i4 0xffffa824
   beq TestCase2808
   br Done

TestCase2808:
   ldc.i4 2908
   stloc.0
   ldc.i4 0xfffffb2f
   conv.i4
   not
   ldc.i4 0x4d0
   beq TestCase2807
   br Done

TestCase2807:
   ldc.i4 2907
   stloc.0
   ldc.i4 0x1d7b
   conv.i4
   not
   ldc.i4 0xffffe284
   bne.un Done

TestCase2806:
   ldc.i4 2906
   stloc.0
   ldc.i4 0x1885
   conv.i4
   not
   ldc.i4 0xffffe77a
   beq TestCase2805
   br Done

TestCase2805:
   ldc.i4 2905
   stloc.0
   ldc.i4 0x65f1
   conv.i4
   not
   ldc.i4 0xffff9a0e
   beq TestCase2804
   br Done

TestCase2804:
   ldc.i4 2904
   stloc.0
   ldc.i4 0x12f5
   conv.i4
   not
   ldc.i4 0xffffed0a
   beq TestCase2803
   br Done

TestCase2803:
   ldc.i4 2903
   stloc.0
   ldc.i4 0x39ee
   conv.i4
   not
   ldc.i4 0xffffc611
   bne.un Done

TestCase2802:
   ldc.i4 2902
   stloc.0
   ldc.i4 0x5dd7
   conv.i4
   not
   ldc.i4 0xffffa228
   bne.un Done

TestCase2801:
   ldc.i4 2901
   stloc.0
   ldc.i4 0x2345
   conv.i4
   not
   ldc.i4 0xffffdcba
   bne.un Done

TestCase2800:
   ldc.i4 2900
   stloc.0
   ldc.i4 0xffffa797
   conv.i4
   not
   ldc.i4 0x5868
   bne.un Done

TestCase2799:
   ldc.i4 2899
   stloc.0
   ldc.i4 0x6552
   conv.i4
   not
   ldc.i4 0xffff9aad
   beq TestCase2798
   br Done

TestCase2798:
   ldc.i4 2898
   stloc.0
   ldc.i4 0x4d92
   conv.i4
   not
   ldc.i4 0xffffb26d
   bne.un Done

TestCase2797:
   ldc.i4 2897
   stloc.0
   ldc.i4 0x215
   conv.i4
   not
   ldc.i4 0xfffffdea
   beq TestCase2796
   br Done

TestCase2796:
   ldc.i4 2896
   stloc.0
   ldc.i4 0xffff825a
   conv.i4
   not
   ldc.i4 0x7da5
   beq TestCase2795
   br Done

TestCase2795:
   ldc.i4 2895
   stloc.0
   ldc.i4 0xfffff70b
   conv.i4
   not
   ldc.i4 0x8f4
   bne.un Done

TestCase2794:
   ldc.i4 2894
   stloc.0
   ldc.i4 0x3109
   conv.i4
   not
   ldc.i4 0xffffcef6
   bne.un Done

TestCase2793:
   ldc.i4 2893
   stloc.0
   ldc.i4 0xffff8dd6
   conv.i4
   not
   ldc.i4 0x7229
   beq TestCase2792
   br Done

TestCase2792:
   ldc.i4 2892
   stloc.0
   ldc.i4 0x6025
   conv.i4
   not
   ldc.i4 0xffff9fda
   bne.un Done

TestCase2791:
   ldc.i4 2891
   stloc.0
   ldc.i4 0x5636
   conv.i4
   not
   ldc.i4 0xffffa9c9
   bne.un Done

TestCase2790:
   ldc.i4 2890
   stloc.0
   ldc.i4 0xffffcd93
   conv.i4
   not
   ldc.i4 0x326c
   bne.un Done

TestCase2789:
   ldc.i4 2889
   stloc.0
   ldc.i4 0x32f9
   conv.i4
   not
   ldc.i4 0xffffcd06
   bne.un Done

TestCase2788:
   ldc.i4 2888
   stloc.0
   ldc.i4 0x3968
   conv.i4
   not
   ldc.i4 0xffffc697
   bne.un Done

TestCase2787:
   ldc.i4 2887
   stloc.0
   ldc.i4 0x3845
   conv.i4
   not
   ldc.i4 0xffffc7ba
   bne.un Done

TestCase2786:
   ldc.i4 2886
   stloc.0
   ldc.i4 0xffffd20a
   conv.i4
   not
   ldc.i4 0x2df5
   bne.un Done

TestCase2785:
   ldc.i4 2885
   stloc.0
   ldc.i4 0x53fd
   conv.i4
   not
   ldc.i4 0xffffac02
   beq TestCase2784
   br Done

TestCase2784:
   ldc.i4 2884
   stloc.0
   ldc.i4 0x654a
   conv.i4
   not
   ldc.i4 0xffff9ab5
   bne.un Done

TestCase2783:
   ldc.i4 2883
   stloc.0
   ldc.i4 0x7a20
   conv.i4
   not
   ldc.i4 0xffff85df
   beq TestCase2782
   br Done

TestCase2782:
   ldc.i4 2882
   stloc.0
   ldc.i4 0x54
   conv.i4
   not
   ldc.i4 0xffffffab
   beq TestCase2781
   br Done

TestCase2781:
   ldc.i4 2881
   stloc.0
   ldc.i4 0xffff80ad
   conv.i4
   not
   ldc.i4 0x7f52
   bne.un Done

TestCase2780:
   ldc.i4 2880
   stloc.0
   ldc.i4 0x538b
   conv.i4
   not
   ldc.i4 0xffffac74
   bne.un Done

TestCase2779:
   ldc.i4 2879
   stloc.0
   ldc.i4 0x5cc9
   conv.i4
   not
   ldc.i4 0xffffa336
   beq TestCase2778
   br Done

TestCase2778:
   ldc.i4 2878
   stloc.0
   ldc.i4 0x7e58
   conv.i4
   not
   ldc.i4 0xffff81a7
   bne.un Done

TestCase2777:
   ldc.i4 2877
   stloc.0
   ldc.i4 0x6a96
   conv.i4
   not
   ldc.i4 0xffff9569
   beq TestCase2776
   br Done

TestCase2776:
   ldc.i4 2876
   stloc.0
   ldc.i4 0x7096
   conv.i4
   not
   ldc.i4 0xffff8f69
   bne.un Done

TestCase2775:
   ldc.i4 2875
   stloc.0
   ldc.i4 0x19cc
   conv.i4
   not
   ldc.i4 0xffffe633
   beq TestCase2774
   br Done

TestCase2774:
   ldc.i4 2874
   stloc.0
   ldc.i4 0xfffffe23
   conv.i4
   not
   ldc.i4 0x1dc
   beq TestCase2773
   br Done

TestCase2773:
   ldc.i4 2873
   stloc.0
   ldc.i4 0xfffff604
   conv.i4
   not
   ldc.i4 0x9fb
   beq TestCase2772
   br Done

TestCase2772:
   ldc.i4 2872
   stloc.0
   ldc.i4 0x1194
   conv.i4
   not
   ldc.i4 0xffffee6b
   beq TestCase2771
   br Done

TestCase2771:
   ldc.i4 2871
   stloc.0
   ldc.i4 0xdf0
   conv.i4
   not
   ldc.i4 0xfffff20f
   bne.un Done

TestCase2770:
   ldc.i4 2870
   stloc.0
   ldc.i4 0x37fd
   conv.i4
   not
   ldc.i4 0xffffc802
   beq TestCase2769
   br Done

TestCase2769:
   ldc.i4 2869
   stloc.0
   ldc.i4 0xffff9e4f
   conv.i4
   not
   ldc.i4 0x61b0
   bne.un Done

TestCase2768:
   ldc.i4 2868
   stloc.0
   ldc.i4 0x2fb6
   conv.i4
   not
   ldc.i4 0xffffd049
   beq TestCase2767
   br Done

TestCase2767:
   ldc.i4 2867
   stloc.0
   ldc.i4 0x83c
   conv.i4
   not
   ldc.i4 0xfffff7c3
   beq TestCase2766
   br Done

TestCase2766:
   ldc.i4 2866
   stloc.0
   ldc.i4 0xffffe38f
   conv.i4
   not
   ldc.i4 0x1c70
   bne.un Done

TestCase2765:
   ldc.i4 2865
   stloc.0
   ldc.i4 0xffffee8d
   conv.i4
   not
   ldc.i4 0x1172
   beq TestCase2764
   br Done

TestCase2764:
   ldc.i4 2864
   stloc.0
   ldc.i4 0xffff8142
   conv.i4
   not
   ldc.i4 0x7ebd
   beq TestCase2763
   br Done

TestCase2763:
   ldc.i4 2863
   stloc.0
   ldc.i4 0x792d
   conv.i4
   not
   ldc.i4 0xffff86d2
   beq TestCase2762
   br Done

TestCase2762:
   ldc.i4 2862
   stloc.0
   ldc.i4 0x4e47
   conv.i4
   not
   ldc.i4 0xffffb1b8
   beq TestCase2761
   br Done

TestCase2761:
   ldc.i4 2861
   stloc.0
   ldc.i4 0xffff8992
   conv.i4
   not
   ldc.i4 0x766d
   beq TestCase2760
   br Done

TestCase2760:
   ldc.i4 2860
   stloc.0
   ldc.i4 0xffffd531
   conv.i4
   not
   ldc.i4 0x2ace
   beq TestCase2759
   br Done

TestCase2759:
   ldc.i4 2859
   stloc.0
   ldc.i4 0xffff81cc
   conv.i4
   not
   ldc.i4 0x7e33
   bne.un Done

TestCase2758:
   ldc.i4 2858
   stloc.0
   ldc.i4 0xffffd636
   conv.i4
   not
   ldc.i4 0x29c9
   beq TestCase2757
   br Done

TestCase2757:
   ldc.i4 2857
   stloc.0
   ldc.i4 0xffff80a7
   conv.i4
   not
   ldc.i4 0x7f58
   bne.un Done

TestCase2756:
   ldc.i4 2856
   stloc.0
   ldc.i4 0xffffb68a
   conv.i4
   not
   ldc.i4 0x4975
   beq TestCase2755
   br Done

TestCase2755:
   ldc.i4 2855
   stloc.0
   ldc.i4 0xfffffa8b
   conv.i4
   not
   ldc.i4 0x574
   beq TestCase2754
   br Done

TestCase2754:
   ldc.i4 2854
   stloc.0
   ldc.i4 0xffffac52
   conv.i4
   not
   ldc.i4 0x53ad
   beq TestCase2753
   br Done

TestCase2753:
   ldc.i4 2853
   stloc.0
   ldc.i4 0xfffff12f
   conv.i4
   not
   ldc.i4 0xed0
   beq TestCase2752
   br Done

TestCase2752:
   ldc.i4 2852
   stloc.0
   ldc.i4 0x49f8
   conv.i4
   not
   ldc.i4 0xffffb607
   beq TestCase2751
   br Done

TestCase2751:
   ldc.i4 2851
   stloc.0
   ldc.i4 0x5be
   conv.i4
   not
   ldc.i4 0xfffffa41
   beq TestCase2750
   br Done

TestCase2750:
   ldc.i4 2850
   stloc.0
   ldc.i4 0xffffde85
   conv.i4
   not
   ldc.i4 0x217a
   bne.un Done

TestCase2749:
   ldc.i4 2849
   stloc.0
   ldc.i4 0xfffff80b
   conv.i4
   not
   ldc.i4 0x7f4
   bne.un Done

TestCase2748:
   ldc.i4 2848
   stloc.0
   ldc.i4 0xffff8f24
   conv.i4
   not
   ldc.i4 0x70db
   bne.un Done

TestCase2747:
   ldc.i4 2847
   stloc.0
   ldc.i4 0x6c6
   conv.i4
   not
   ldc.i4 0xfffff939
   beq TestCase2746
   br Done

TestCase2746:
   ldc.i4 2846
   stloc.0
   ldc.i4 0xb3b
   conv.i4
   not
   ldc.i4 0xfffff4c4
   beq TestCase2745
   br Done

TestCase2745:
   ldc.i4 2845
   stloc.0
   ldc.i4 0x6142
   conv.i4
   not
   ldc.i4 0xffff9ebd
   bne.un Done

TestCase2744:
   ldc.i4 2844
   stloc.0
   ldc.i4 0x2abc
   conv.i4
   not
   ldc.i4 0xffffd543
   bne.un Done

TestCase2743:
   ldc.i4 2843
   stloc.0
   ldc.i4 0xffff8a5a
   conv.i4
   not
   ldc.i4 0x75a5
   bne.un Done

TestCase2742:
   ldc.i4 2842
   stloc.0
   ldc.i4 0xffffed9a
   conv.i4
   not
   ldc.i4 0x1265
   bne.un Done

TestCase2741:
   ldc.i4 2841
   stloc.0
   ldc.i4 0xffffa651
   conv.i4
   not
   ldc.i4 0x59ae
   beq TestCase2740
   br Done

TestCase2740:
   ldc.i4 2840
   stloc.0
   ldc.i4 0x4247
   conv.i4
   not
   ldc.i4 0xffffbdb8
   bne.un Done

TestCase2739:
   ldc.i4 2839
   stloc.0
   ldc.i4 0x19c
   conv.i4
   not
   ldc.i4 0xfffffe63
   bne.un Done

TestCase2738:
   ldc.i4 2838
   stloc.0
   ldc.i4 0xffffb5f3
   conv.i4
   not
   ldc.i4 0x4a0c
   beq TestCase2737
   br Done

TestCase2737:
   ldc.i4 2837
   stloc.0
   ldc.i4 0xffffd3f4
   conv.i4
   not
   ldc.i4 0x2c0b
   beq TestCase2736
   br Done

TestCase2736:
   ldc.i4 2836
   stloc.0
   ldc.i4 0xffffba66
   conv.i4
   not
   ldc.i4 0x4599
   beq TestCase2735
   br Done

TestCase2735:
   ldc.i4 2835
   stloc.0
   ldc.i4 0xffff8302
   conv.i4
   not
   ldc.i4 0x7cfd
   bne.un Done

TestCase2734:
   ldc.i4 2834
   stloc.0
   ldc.i4 0x6c45
   conv.i4
   not
   ldc.i4 0xffff93ba
   bne.un Done

TestCase2733:
   ldc.i4 2833
   stloc.0
   ldc.i4 0xffffa369
   conv.i4
   not
   ldc.i4 0x5c96
   beq TestCase2732
   br Done

TestCase2732:
   ldc.i4 2832
   stloc.0
   ldc.i4 0xffffd940
   conv.i4
   not
   ldc.i4 0x26bf
   bne.un Done

TestCase2731:
   ldc.i4 2831
   stloc.0
   ldc.i4 0xffff8185
   conv.i4
   not
   ldc.i4 0x7e7a
   beq TestCase2730
   br Done

TestCase2730:
   ldc.i4 2830
   stloc.0
   ldc.i4 0xffffd72a
   conv.i4
   not
   ldc.i4 0x28d5
   bne.un Done

TestCase2729:
   ldc.i4 2829
   stloc.0
   ldc.i4 0xffff8321
   conv.i4
   not
   ldc.i4 0x7cde
   bne.un Done

TestCase2728:
   ldc.i4 2828
   stloc.0
   ldc.i4 0x6386
   conv.i4
   not
   ldc.i4 0xffff9c79
   bne.un Done

TestCase2727:
   ldc.i4 2827
   stloc.0
   ldc.i4 0x3e99
   conv.i4
   not
   ldc.i4 0xffffc166
   beq TestCase2726
   br Done

TestCase2726:
   ldc.i4 2826
   stloc.0
   ldc.i4 0x1924
   conv.i4
   not
   ldc.i4 0xffffe6db
   beq TestCase2725
   br Done

TestCase2725:
   ldc.i4 2825
   stloc.0
   ldc.i4 0xfffffe23
   conv.i4
   not
   ldc.i4 0x1dc
   bne.un Done

TestCase2724:
   ldc.i4 2824
   stloc.0
   ldc.i4 0xffffbf7c
   conv.i4
   not
   ldc.i4 0x4083
   beq TestCase2723
   br Done

TestCase2723:
   ldc.i4 2823
   stloc.0
   ldc.i4 0x6d6b
   conv.i4
   not
   ldc.i4 0xffff9294
   bne.un Done

TestCase2722:
   ldc.i4 2822
   stloc.0
   ldc.i4 0xfffffaa3
   conv.i4
   not
   ldc.i4 0x55c
   beq TestCase2721
   br Done

TestCase2721:
   ldc.i4 2821
   stloc.0
   ldc.i4 0xfffff7b0
   conv.i4
   not
   ldc.i4 0x84f
   beq TestCase2720
   br Done

TestCase2720:
   ldc.i4 2820
   stloc.0
   ldc.i4 0xffff9c98
   conv.i4
   not
   ldc.i4 0x6367
   beq TestCase2719
   br Done

TestCase2719:
   ldc.i4 2819
   stloc.0
   ldc.i4 0xffff836f
   conv.i4
   not
   ldc.i4 0x7c90
   beq TestCase2718
   br Done

TestCase2718:
   ldc.i4 2818
   stloc.0
   ldc.i4 0xffff991d
   conv.i4
   not
   ldc.i4 0x66e2
   beq TestCase2717
   br Done

TestCase2717:
   ldc.i4 2817
   stloc.0
   ldc.i4 0xffffdca8
   conv.i4
   not
   ldc.i4 0x2357
   bne.un Done

TestCase2716:
   ldc.i4 2816
   stloc.0
   ldc.i4 0xffffa77c
   conv.i4
   not
   ldc.i4 0x5883
   bne.un Done

TestCase2715:
   ldc.i4 2815
   stloc.0
   ldc.i4 0xffffe68b
   conv.i4
   not
   ldc.i4 0x1974
   beq TestCase2714
   br Done

TestCase2714:
   ldc.i4 2814
   stloc.0
   ldc.i4 0xffffac04
   conv.i4
   not
   ldc.i4 0x53fb
   bne.un Done

TestCase2713:
   ldc.i4 2813
   stloc.0
   ldc.i4 0xffffeeaa
   conv.i4
   not
   ldc.i4 0x1155
   bne.un Done

TestCase2712:
   ldc.i4 2812
   stloc.0
   ldc.i4 0x4f9a
   conv.i4
   not
   ldc.i4 0xffffb065
   beq TestCase2711
   br Done

TestCase2711:
   ldc.i4 2811
   stloc.0
   ldc.i4 0xffffac3d
   conv.i4
   not
   ldc.i4 0x53c2
   beq TestCase2710
   br Done

TestCase2710:
   ldc.i4 2810
   stloc.0
   ldc.i4 0x40b7
   conv.i4
   not
   ldc.i4 0xffffbf48
   beq TestCase2709
   br Done

TestCase2709:
   ldc.i4 2809
   stloc.0
   ldc.i4 0x7302
   conv.i4
   not
   ldc.i4 0xffff8cfd
   beq TestCase2708
   br Done

TestCase2708:
   ldc.i4 2808
   stloc.0
   ldc.i4 0x1be6
   conv.i4
   not
   ldc.i4 0xffffe419
   beq TestCase2707
   br Done

TestCase2707:
   ldc.i4 2807
   stloc.0
   ldc.i4 0x2c29
   conv.i4
   not
   ldc.i4 0xffffd3d6
   bne.un Done

TestCase2706:
   ldc.i4 2806
   stloc.0
   ldc.i4 0x4121
   conv.i4
   not
   ldc.i4 0xffffbede
   bne.un Done

TestCase2705:
   ldc.i4 2805
   stloc.0
   ldc.i4 0xffff9f34
   conv.i4
   not
   ldc.i4 0x60cb
   beq TestCase2704
   br Done

TestCase2704:
   ldc.i4 2804
   stloc.0
   ldc.i4 0x436c
   conv.i4
   not
   ldc.i4 0xffffbc93
   bne.un Done

TestCase2703:
   ldc.i4 2803
   stloc.0
   ldc.i4 0x750
   conv.i4
   not
   ldc.i4 0xfffff8af
   beq TestCase2702
   br Done

TestCase2702:
   ldc.i4 2802
   stloc.0
   ldc.i4 0xffffc758
   conv.i4
   not
   ldc.i4 0x38a7
   beq TestCase2701
   br Done

TestCase2701:
   ldc.i4 2801
   stloc.0
   ldc.i4 0x7a78
   conv.i4
   not
   ldc.i4 0xffff8587
   beq TestCase2700
   br Done

TestCase2700:
   ldc.i4 2800
   stloc.0
   ldc.i4 0xffffd67c
   conv.i4
   not
   ldc.i4 0x2983
   beq TestCase2699
   br Done

TestCase2699:
   ldc.i4 2799
   stloc.0
   ldc.i4 0xffffbee7
   conv.i4
   not
   ldc.i4 0x4118
   bne.un Done

TestCase2698:
   ldc.i4 2798
   stloc.0
   ldc.i4 0x22cf
   conv.i4
   not
   ldc.i4 0xffffdd30
   beq TestCase2697
   br Done

TestCase2697:
   ldc.i4 2797
   stloc.0
   ldc.i4 0xffffb30c
   conv.i4
   not
   ldc.i4 0x4cf3
   beq TestCase2696
   br Done

TestCase2696:
   ldc.i4 2796
   stloc.0
   ldc.i4 0x20a2
   conv.i4
   not
   ldc.i4 0xffffdf5d
   bne.un Done

TestCase2695:
   ldc.i4 2795
   stloc.0
   ldc.i4 0x5c1c
   conv.i4
   not
   ldc.i4 0xffffa3e3
   bne.un Done

TestCase2694:
   ldc.i4 2794
   stloc.0
   ldc.i4 0xffff9eed
   conv.i4
   not
   ldc.i4 0x6112
   beq TestCase2693
   br Done

TestCase2693:
   ldc.i4 2793
   stloc.0
   ldc.i4 0x2a7c
   conv.i4
   not
   ldc.i4 0xffffd583
   beq TestCase2692
   br Done

TestCase2692:
   ldc.i4 2792
   stloc.0
   ldc.i4 0xffff9096
   conv.i4
   not
   ldc.i4 0x6f69
   bne.un Done

TestCase2691:
   ldc.i4 2791
   stloc.0
   ldc.i4 0xfffff6a3
   conv.i4
   not
   ldc.i4 0x95c
   beq TestCase2690
   br Done

TestCase2690:
   ldc.i4 2790
   stloc.0
   ldc.i4 0xffffc5e4
   conv.i4
   not
   ldc.i4 0x3a1b
   beq TestCase2689
   br Done

TestCase2689:
   ldc.i4 2789
   stloc.0
   ldc.i4 0xffffaf0f
   conv.i4
   not
   ldc.i4 0x50f0
   beq TestCase2688
   br Done

TestCase2688:
   ldc.i4 2788
   stloc.0
   ldc.i4 0xffffeb0d
   conv.i4
   not
   ldc.i4 0x14f2
   beq TestCase2687
   br Done

TestCase2687:
   ldc.i4 2787
   stloc.0
   ldc.i4 0xfffffbb0
   conv.i4
   not
   ldc.i4 0x44f
   beq TestCase2686
   br Done

TestCase2686:
   ldc.i4 2786
   stloc.0
   ldc.i4 0x7627
   conv.i4
   not
   ldc.i4 0xffff89d8
   beq TestCase2685
   br Done

TestCase2685:
   ldc.i4 2785
   stloc.0
   ldc.i4 0xffff858b
   conv.i4
   not
   ldc.i4 0x7a74
   bne.un Done

TestCase2684:
   ldc.i4 2784
   stloc.0
   ldc.i4 0xffffa7a7
   conv.i4
   not
   ldc.i4 0x5858
   beq TestCase2683
   br Done

TestCase2683:
   ldc.i4 2783
   stloc.0
   ldc.i4 0xffff80f1
   conv.i4
   not
   ldc.i4 0x7f0e
   beq TestCase2682
   br Done

TestCase2682:
   ldc.i4 2782
   stloc.0
   ldc.i4 0x44b2
   conv.i4
   not
   ldc.i4 0xffffbb4d
   beq TestCase2681
   br Done

TestCase2681:
   ldc.i4 2781
   stloc.0
   ldc.i4 0x344a
   conv.i4
   not
   ldc.i4 0xffffcbb5
   beq TestCase2680
   br Done

TestCase2680:
   ldc.i4 2780
   stloc.0
   ldc.i4 0xffff9077
   conv.i4
   not
   ldc.i4 0x6f88
   bne.un Done

TestCase2679:
   ldc.i4 2779
   stloc.0
   ldc.i4 0x1ada
   conv.i4
   not
   ldc.i4 0xffffe525
   bne.un Done

TestCase2678:
   ldc.i4 2778
   stloc.0
   ldc.i4 0xffffa897
   conv.i4
   not
   ldc.i4 0x5768
   beq TestCase2677
   br Done

TestCase2677:
   ldc.i4 2777
   stloc.0
   ldc.i4 0xffffe814
   conv.i4
   not
   ldc.i4 0x17eb
   beq TestCase2676
   br Done

TestCase2676:
   ldc.i4 2776
   stloc.0
   ldc.i4 0x4949
   conv.i4
   not
   ldc.i4 0xffffb6b6
   beq TestCase2675
   br Done

TestCase2675:
   ldc.i4 2775
   stloc.0
   ldc.i4 0xffffd731
   conv.i4
   not
   ldc.i4 0x28ce
   bne.un Done

TestCase2674:
   ldc.i4 2774
   stloc.0
   ldc.i4 0xffffa1b0
   conv.i4
   not
   ldc.i4 0x5e4f
   beq TestCase2673
   br Done

TestCase2673:
   ldc.i4 2773
   stloc.0
   ldc.i4 0xffffbd94
   conv.i4
   not
   ldc.i4 0x426b
   bne.un Done

TestCase2672:
   ldc.i4 2772
   stloc.0
   ldc.i4 0x4686
   conv.i4
   not
   ldc.i4 0xffffb979
   bne.un Done

TestCase2671:
   ldc.i4 2771
   stloc.0
   ldc.i4 0x2eea
   conv.i4
   not
   ldc.i4 0xffffd115
   bne.un Done

TestCase2670:
   ldc.i4 2770
   stloc.0
   ldc.i4 0xffffaecf
   conv.i4
   not
   ldc.i4 0x5130
   beq TestCase2669
   br Done

TestCase2669:
   ldc.i4 2769
   stloc.0
   ldc.i4 0xffffd9f1
   conv.i4
   not
   ldc.i4 0x260e
   bne.un Done

TestCase2668:
   ldc.i4 2768
   stloc.0
   ldc.i4 0x6ee1
   conv.i4
   not
   ldc.i4 0xffff911e
   bne.un Done

TestCase2667:
   ldc.i4 2767
   stloc.0
   ldc.i4 0x6d3e
   conv.i4
   not
   ldc.i4 0xffff92c1
   bne.un Done

TestCase2666:
   ldc.i4 2766
   stloc.0
   ldc.i4 0xffffcdb8
   conv.i4
   not
   ldc.i4 0x3247
   bne.un Done

TestCase2665:
   ldc.i4 2765
   stloc.0
   ldc.i4 0xffffedda
   conv.i4
   not
   ldc.i4 0x1225
   beq TestCase2664
   br Done

TestCase2664:
   ldc.i4 2764
   stloc.0
   ldc.i4 0xffff9385
   conv.i4
   not
   ldc.i4 0x6c7a
   bne.un Done

TestCase2663:
   ldc.i4 2763
   stloc.0
   ldc.i4 0xffffa08c
   conv.i4
   not
   ldc.i4 0x5f73
   bne.un Done

TestCase2662:
   ldc.i4 2762
   stloc.0
   ldc.i4 0xffffc9d3
   conv.i4
   not
   ldc.i4 0x362c
   bne.un Done

TestCase2661:
   ldc.i4 2761
   stloc.0
   ldc.i4 0xd2f
   conv.i4
   not
   ldc.i4 0xfffff2d0
   bne.un Done

TestCase2660:
   ldc.i4 2760
   stloc.0
   ldc.i4 0xffffa501
   conv.i4
   not
   ldc.i4 0x5afe
   beq TestCase2659
   br Done

TestCase2659:
   ldc.i4 2759
   stloc.0
   ldc.i4 0xffff897b
   conv.i4
   not
   ldc.i4 0x7684
   bne.un Done

TestCase2658:
   ldc.i4 2758
   stloc.0
   ldc.i4 0x7d01
   conv.i4
   not
   ldc.i4 0xffff82fe
   bne.un Done

TestCase2657:
   ldc.i4 2757
   stloc.0
   ldc.i4 0xffffa2d3
   conv.i4
   not
   ldc.i4 0x5d2c
   beq TestCase2656
   br Done

TestCase2656:
   ldc.i4 2756
   stloc.0
   ldc.i4 0xffffca10
   conv.i4
   not
   ldc.i4 0x35ef
   beq TestCase2655
   br Done

TestCase2655:
   ldc.i4 2755
   stloc.0
   ldc.i4 0x6b3
   conv.i4
   not
   ldc.i4 0xfffff94c
   beq TestCase2654
   br Done

TestCase2654:
   ldc.i4 2754
   stloc.0
   ldc.i4 0x48dc
   conv.i4
   not
   ldc.i4 0xffffb723
   bne.un Done

TestCase2653:
   ldc.i4 2753
   stloc.0
   ldc.i4 0x5a44
   conv.i4
   not
   ldc.i4 0xffffa5bb
   bne.un Done

TestCase2652:
   ldc.i4 2752
   stloc.0
   ldc.i4 0xffff8dcb
   conv.i4
   not
   ldc.i4 0x7234
   beq TestCase2651
   br Done

TestCase2651:
   ldc.i4 2751
   stloc.0
   ldc.i4 0x25cd
   conv.i4
   not
   ldc.i4 0xffffda32
   bne.un Done

TestCase2650:
   ldc.i4 2750
   stloc.0
   ldc.i4 0xffffa28b
   conv.i4
   not
   ldc.i4 0x5d74
   beq TestCase2649
   br Done

TestCase2649:
   ldc.i4 2749
   stloc.0
   ldc.i4 0xffff9869
   conv.i4
   not
   ldc.i4 0x6796
   bne.un Done

TestCase2648:
   ldc.i4 2748
   stloc.0
   ldc.i4 0xfffff11e
   conv.i4
   not
   ldc.i4 0xee1
   beq TestCase2647
   br Done

TestCase2647:
   ldc.i4 2747
   stloc.0
   ldc.i4 0x75a3
   conv.i4
   not
   ldc.i4 0xffff8a5c
   beq TestCase2646
   br Done

TestCase2646:
   ldc.i4 2746
   stloc.0
   ldc.i4 0x7513
   conv.i4
   not
   ldc.i4 0xffff8aec
   bne.un Done

TestCase2645:
   ldc.i4 2745
   stloc.0
   ldc.i4 0xffffd19b
   conv.i4
   not
   ldc.i4 0x2e64
   beq TestCase2644
   br Done

TestCase2644:
   ldc.i4 2744
   stloc.0
   ldc.i4 0xffffd6b9
   conv.i4
   not
   ldc.i4 0x2946
   bne.un Done

TestCase2643:
   ldc.i4 2743
   stloc.0
   ldc.i4 0x16c6
   conv.i4
   not
   ldc.i4 0xffffe939
   bne.un Done

TestCase2642:
   ldc.i4 2742
   stloc.0
   ldc.i4 0x76b2
   conv.i4
   not
   ldc.i4 0xffff894d
   bne.un Done

TestCase2641:
   ldc.i4 2741
   stloc.0
   ldc.i4 0x7762
   conv.i4
   not
   ldc.i4 0xffff889d
   bne.un Done

TestCase2640:
   ldc.i4 2740
   stloc.0
   ldc.i4 0x603e
   conv.i4
   not
   ldc.i4 0xffff9fc1
   bne.un Done

TestCase2639:
   ldc.i4 2739
   stloc.0
   ldc.i4 0xffffc07c
   conv.i4
   not
   ldc.i4 0x3f83
   beq TestCase2638
   br Done

TestCase2638:
   ldc.i4 2738
   stloc.0
   ldc.i4 0xffffa687
   conv.i4
   not
   ldc.i4 0x5978
   bne.un Done

TestCase2637:
   ldc.i4 2737
   stloc.0
   ldc.i4 0xffffc1ad
   conv.i4
   not
   ldc.i4 0x3e52
   beq TestCase2636
   br Done

TestCase2636:
   ldc.i4 2736
   stloc.0
   ldc.i4 0x12b0
   conv.i4
   not
   ldc.i4 0xffffed4f
   bne.un Done

TestCase2635:
   ldc.i4 2735
   stloc.0
   ldc.i4 0x3322
   conv.i4
   not
   ldc.i4 0xffffccdd
   bne.un Done

TestCase2634:
   ldc.i4 2734
   stloc.0
   ldc.i4 0x615b
   conv.i4
   not
   ldc.i4 0xffff9ea4
   bne.un Done

TestCase2633:
   ldc.i4 2733
   stloc.0
   ldc.i4 0x5bed
   conv.i4
   not
   ldc.i4 0xffffa412
   bne.un Done

TestCase2632:
   ldc.i4 2732
   stloc.0
   ldc.i4 0x4568
   conv.i4
   not
   ldc.i4 0xffffba97
   beq TestCase2631
   br Done

TestCase2631:
   ldc.i4 2731
   stloc.0
   ldc.i4 0xffffeaa5
   conv.i4
   not
   ldc.i4 0x155a
   beq TestCase2630
   br Done

TestCase2630:
   ldc.i4 2730
   stloc.0
   ldc.i4 0xffffdef4
   conv.i4
   not
   ldc.i4 0x210b
   bne.un Done

TestCase2629:
   ldc.i4 2729
   stloc.0
   ldc.i4 0xffff9e12
   conv.i4
   not
   ldc.i4 0x61ed
   bne.un Done

TestCase2628:
   ldc.i4 2728
   stloc.0
   ldc.i4 0x3c1c
   conv.i4
   not
   ldc.i4 0xffffc3e3
   bne.un Done

TestCase2627:
   ldc.i4 2727
   stloc.0
   ldc.i4 0x5619
   conv.i4
   not
   ldc.i4 0xffffa9e6
   bne.un Done

TestCase2626:
   ldc.i4 2726
   stloc.0
   ldc.i4 0xffff92da
   conv.i4
   not
   ldc.i4 0x6d25
   beq TestCase2625
   br Done

TestCase2625:
   ldc.i4 2725
   stloc.0
   ldc.i4 0xfffffa85
   conv.i4
   not
   ldc.i4 0x57a
   bne.un Done

TestCase2624:
   ldc.i4 2724
   stloc.0
   ldc.i4 0xffffee47
   conv.i4
   not
   ldc.i4 0x11b8
   beq TestCase2623
   br Done

TestCase2623:
   ldc.i4 2723
   stloc.0
   ldc.i4 0x3172
   conv.i4
   not
   ldc.i4 0xffffce8d
   beq TestCase2622
   br Done

TestCase2622:
   ldc.i4 2722
   stloc.0
   ldc.i4 0x2c9c
   conv.i4
   not
   ldc.i4 0xffffd363
   beq TestCase2621
   br Done

TestCase2621:
   ldc.i4 2721
   stloc.0
   ldc.i4 0x33bc
   conv.i4
   not
   ldc.i4 0xffffcc43
   bne.un Done

TestCase2620:
   ldc.i4 2720
   stloc.0
   ldc.i4 0xffffa412
   conv.i4
   not
   ldc.i4 0x5bed
   beq TestCase2619
   br Done

TestCase2619:
   ldc.i4 2719
   stloc.0
   ldc.i4 0x7fe5
   conv.i4
   not
   ldc.i4 0xffff801a
   beq TestCase2618
   br Done

TestCase2618:
   ldc.i4 2718
   stloc.0
   ldc.i4 0xffffcda2
   conv.i4
   not
   ldc.i4 0x325d
   beq TestCase2617
   br Done

TestCase2617:
   ldc.i4 2717
   stloc.0
   ldc.i4 0x2cb5
   conv.i4
   not
   ldc.i4 0xffffd34a
   bne.un Done

TestCase2616:
   ldc.i4 2716
   stloc.0
   ldc.i4 0x25fa
   conv.i4
   not
   ldc.i4 0xffffda05
   bne.un Done

TestCase2615:
   ldc.i4 2715
   stloc.0
   ldc.i4 0x52d5
   conv.i4
   not
   ldc.i4 0xffffad2a
   beq TestCase2614
   br Done

TestCase2614:
   ldc.i4 2714
   stloc.0
   ldc.i4 0xc4d
   conv.i4
   not
   ldc.i4 0xfffff3b2
   bne.un Done

TestCase2613:
   ldc.i4 2713
   stloc.0
   ldc.i4 0x4067
   conv.i4
   not
   ldc.i4 0xffffbf98
   beq TestCase2612
   br Done

TestCase2612:
   ldc.i4 2712
   stloc.0
   ldc.i4 0xffffb147
   conv.i4
   not
   ldc.i4 0x4eb8
   bne.un Done

TestCase2611:
   ldc.i4 2711
   stloc.0
   ldc.i4 0x1946
   conv.i4
   not
   ldc.i4 0xffffe6b9
   beq TestCase2610
   br Done

TestCase2610:
   ldc.i4 2710
   stloc.0
   ldc.i4 0x7360
   conv.i4
   not
   ldc.i4 0xffff8c9f
   bne.un Done

TestCase2609:
   ldc.i4 2709
   stloc.0
   ldc.i4 0xfffff5d9
   conv.i4
   not
   ldc.i4 0xa26
   bne.un Done

TestCase2608:
   ldc.i4 2708
   stloc.0
   ldc.i4 0x15ed
   conv.i4
   not
   ldc.i4 0xffffea12
   beq TestCase2607
   br Done

TestCase2607:
   ldc.i4 2707
   stloc.0
   ldc.i4 0xffffb72a
   conv.i4
   not
   ldc.i4 0x48d5
   beq TestCase2606
   br Done

TestCase2606:
   ldc.i4 2706
   stloc.0
   ldc.i4 0x4917
   conv.i4
   not
   ldc.i4 0xffffb6e8
   beq TestCase2605
   br Done

TestCase2605:
   ldc.i4 2705
   stloc.0
   ldc.i4 0x463d
   conv.i4
   not
   ldc.i4 0xffffb9c2
   bne.un Done

TestCase2604:
   ldc.i4 2704
   stloc.0
   ldc.i4 0x64c9
   conv.i4
   not
   ldc.i4 0xffff9b36
   bne.un Done

TestCase2603:
   ldc.i4 2703
   stloc.0
   ldc.i4 0xffffb903
   conv.i4
   not
   ldc.i4 0x46fc
   beq TestCase2602
   br Done

TestCase2602:
   ldc.i4 2702
   stloc.0
   ldc.i4 0xffffb1e0
   conv.i4
   not
   ldc.i4 0x4e1f
   bne.un Done

TestCase2601:
   ldc.i4 2701
   stloc.0
   ldc.i4 0x1dc0
   conv.i4
   not
   ldc.i4 0xffffe23f
   bne.un Done

TestCase2600:
   ldc.i4 2700
   stloc.0
   ldc.i4 0x1dc3
   conv.i4
   not
   ldc.i4 0xffffe23c
   bne.un Done

TestCase2599:
   ldc.i4 2699
   stloc.0
   ldc.i4 0xfffff178
   conv.i4
   not
   ldc.i4 0xe87
   bne.un Done

TestCase2598:
   ldc.i4 2698
   stloc.0
   ldc.i4 0x7e47
   conv.i4
   not
   ldc.i4 0xffff81b8
   beq TestCase2597
   br Done

TestCase2597:
   ldc.i4 2697
   stloc.0
   ldc.i4 0xfffffb50
   conv.i4
   not
   ldc.i4 0x4af
   bne.un Done

TestCase2596:
   ldc.i4 2696
   stloc.0
   ldc.i4 0x7f9a
   conv.i4
   not
   ldc.i4 0xffff8065
   bne.un Done

TestCase2595:
   ldc.i4 2695
   stloc.0
   ldc.i4 0x4d53
   conv.i4
   not
   ldc.i4 0xffffb2ac
   beq TestCase2594
   br Done

TestCase2594:
   ldc.i4 2694
   stloc.0
   ldc.i4 0xffffc25d
   conv.i4
   not
   ldc.i4 0x3da2
   beq TestCase2593
   br Done

TestCase2593:
   ldc.i4 2693
   stloc.0
   ldc.i4 0x654
   conv.i4
   not
   ldc.i4 0xfffff9ab
   bne.un Done

TestCase2592:
   ldc.i4 2692
   stloc.0
   ldc.i4 0xffff985c
   conv.i4
   not
   ldc.i4 0x67a3
   bne.un Done

TestCase2591:
   ldc.i4 2691
   stloc.0
   ldc.i4 0x7645
   conv.i4
   not
   ldc.i4 0xffff89ba
   beq TestCase2590
   br Done

TestCase2590:
   ldc.i4 2690
   stloc.0
   ldc.i4 0xffffbd02
   conv.i4
   not
   ldc.i4 0x42fd
   bne.un Done

TestCase2589:
   ldc.i4 2689
   stloc.0
   ldc.i4 0x1bd5
   conv.i4
   not
   ldc.i4 0xffffe42a
   beq TestCase2588
   br Done

TestCase2588:
   ldc.i4 2688
   stloc.0
   ldc.i4 0x7f5d
   conv.i4
   not
   ldc.i4 0xffff80a2
   beq TestCase2587
   br Done

TestCase2587:
   ldc.i4 2687
   stloc.0
   ldc.i4 0xfffffc6f
   conv.i4
   not
   ldc.i4 0x390
   bne.un Done

TestCase2586:
   ldc.i4 2686
   stloc.0
   ldc.i4 0xffff947c
   conv.i4
   not
   ldc.i4 0x6b83
   bne.un Done

TestCase2585:
   ldc.i4 2685
   stloc.0
   ldc.i4 0x691b
   conv.i4
   not
   ldc.i4 0xffff96e4
   bne.un Done

TestCase2584:
   ldc.i4 2684
   stloc.0
   ldc.i4 0x212c
   conv.i4
   not
   ldc.i4 0xffffded3
   beq TestCase2583
   br Done

TestCase2583:
   ldc.i4 2683
   stloc.0
   ldc.i4 0x652a
   conv.i4
   not
   ldc.i4 0xffff9ad5
   bne.un Done

TestCase2582:
   ldc.i4 2682
   stloc.0
   ldc.i4 0x63af
   conv.i4
   not
   ldc.i4 0xffff9c50
   bne.un Done

TestCase2581:
   ldc.i4 2681
   stloc.0
   ldc.i4 0xffff8419
   conv.i4
   not
   ldc.i4 0x7be6
   beq TestCase2580
   br Done

TestCase2580:
   ldc.i4 2680
   stloc.0
   ldc.i4 0xffffff88
   conv.i4
   not
   ldc.i4 0x77
   bne.un Done

TestCase2579:
   ldc.i4 2679
   stloc.0
   ldc.i4 0xffffa879
   conv.i4
   not
   ldc.i4 0x5786
   beq TestCase2578
   br Done

TestCase2578:
   ldc.i4 2678
   stloc.0
   ldc.i4 0xffffae8f
   conv.i4
   not
   ldc.i4 0x5170
   beq TestCase2577
   br Done

TestCase2577:
   ldc.i4 2677
   stloc.0
   ldc.i4 0x6932
   conv.i4
   not
   ldc.i4 0xffff96cd
   bne.un Done

TestCase2576:
   ldc.i4 2676
   stloc.0
   ldc.i4 0xffff9f16
   conv.i4
   not
   ldc.i4 0x60e9
   beq TestCase2575
   br Done

TestCase2575:
   ldc.i4 2675
   stloc.0
   ldc.i4 0xffffe168
   conv.i4
   not
   ldc.i4 0x1e97
   beq TestCase2574
   br Done

TestCase2574:
   ldc.i4 2674
   stloc.0
   ldc.i4 0x6ba3
   conv.i4
   not
   ldc.i4 0xffff945c
   bne.un Done

TestCase2573:
   ldc.i4 2673
   stloc.0
   ldc.i4 0xffff9b3c
   conv.i4
   not
   ldc.i4 0x64c3
   beq TestCase2572
   br Done

TestCase2572:
   ldc.i4 2672
   stloc.0
   ldc.i4 0xffff96fc
   conv.i4
   not
   ldc.i4 0x6903
   beq TestCase2571
   br Done

TestCase2571:
   ldc.i4 2671
   stloc.0
   ldc.i4 0xffff9ef9
   conv.i4
   not
   ldc.i4 0x6106
   bne.un Done

TestCase2570:
   ldc.i4 2670
   stloc.0
   ldc.i4 0x2aad
   conv.i4
   not
   ldc.i4 0xffffd552
   beq TestCase2569
   br Done

TestCase2569:
   ldc.i4 2669
   stloc.0
   ldc.i4 0xffffd0e9
   conv.i4
   not
   ldc.i4 0x2f16
   beq TestCase2568
   br Done

TestCase2568:
   ldc.i4 2668
   stloc.0
   ldc.i4 0xffffa41e
   conv.i4
   not
   ldc.i4 0x5be1
   beq TestCase2567
   br Done

TestCase2567:
   ldc.i4 2667
   stloc.0
   ldc.i4 0xffffe04d
   conv.i4
   not
   ldc.i4 0x1fb2
   bne.un Done

TestCase2566:
   ldc.i4 2666
   stloc.0
   ldc.i4 0xffff8189
   conv.i4
   not
   ldc.i4 0x7e76
   beq TestCase2565
   br Done

TestCase2565:
   ldc.i4 2665
   stloc.0
   ldc.i4 0x756d
   conv.i4
   not
   ldc.i4 0xffff8a92
   bne.un Done

TestCase2564:
   ldc.i4 2664
   stloc.0
   ldc.i4 0x464e
   conv.i4
   not
   ldc.i4 0xffffb9b1
   beq TestCase2563
   br Done

TestCase2563:
   ldc.i4 2663
   stloc.0
   ldc.i4 0xfffffa97
   conv.i4
   not
   ldc.i4 0x568
   bne.un Done

TestCase2562:
   ldc.i4 2662
   stloc.0
   ldc.i4 0xffffca71
   conv.i4
   not
   ldc.i4 0x358e
   bne.un Done

TestCase2561:
   ldc.i4 2661
   stloc.0
   ldc.i4 0xffffffd1
   conv.i4
   not
   ldc.i4 0x2e
   bne.un Done

TestCase2560:
   ldc.i4 2660
   stloc.0
   ldc.i4 0xffff94f6
   conv.i4
   not
   ldc.i4 0x6b09
   beq TestCase2559
   br Done

TestCase2559:
   ldc.i4 2659
   stloc.0
   ldc.i4 0xfffff51b
   conv.i4
   not
   ldc.i4 0xae4
   beq TestCase2558
   br Done

TestCase2558:
   ldc.i4 2658
   stloc.0
   ldc.i4 0xfffffe66
   conv.i4
   not
   ldc.i4 0x199
   bne.un Done

TestCase2557:
   ldc.i4 2657
   stloc.0
   ldc.i4 0x4b88
   conv.i4
   not
   ldc.i4 0xffffb477
   beq TestCase2556
   br Done

TestCase2556:
   ldc.i4 2656
   stloc.0
   ldc.i4 0x7a59
   conv.i4
   not
   ldc.i4 0xffff85a6
   bne.un Done

TestCase2555:
   ldc.i4 2655
   stloc.0
   ldc.i4 0xffffd4f8
   conv.i4
   not
   ldc.i4 0x2b07
   beq TestCase2554
   br Done

TestCase2554:
   ldc.i4 2654
   stloc.0
   ldc.i4 0xffffbb01
   conv.i4
   not
   ldc.i4 0x44fe
   bne.un Done

TestCase2553:
   ldc.i4 2653
   stloc.0
   ldc.i4 0x6642
   conv.i4
   not
   ldc.i4 0xffff99bd
   beq TestCase2552
   br Done

TestCase2552:
   ldc.i4 2652
   stloc.0
   ldc.i4 0xffff9f31
   conv.i4
   not
   ldc.i4 0x60ce
   bne.un Done

TestCase2551:
   ldc.i4 2651
   stloc.0
   ldc.i4 0xffffdca7
   conv.i4
   not
   ldc.i4 0x2358
   beq TestCase2550
   br Done

TestCase2550:
   ldc.i4 2650
   stloc.0
   ldc.i4 0xffffea2e
   conv.i4
   not
   ldc.i4 0x15d1
   beq TestCase2549
   br Done

TestCase2549:
   ldc.i4 2649
   stloc.0
   ldc.i4 0x32e0
   conv.i4
   not
   ldc.i4 0xffffcd1f
   beq TestCase2548
   br Done

TestCase2548:
   ldc.i4 2648
   stloc.0
   ldc.i4 0xffffc6a7
   conv.i4
   not
   ldc.i4 0x3958
   bne.un Done

TestCase2547:
   ldc.i4 2647
   stloc.0
   ldc.i4 0xffff833f
   conv.i4
   not
   ldc.i4 0x7cc0
   bne.un Done

TestCase2546:
   ldc.i4 2646
   stloc.0
   ldc.i4 0xffffde02
   conv.i4
   not
   ldc.i4 0x21fd
   beq TestCase2545
   br Done

TestCase2545:
   ldc.i4 2645
   stloc.0
   ldc.i4 0xffff9905
   conv.i4
   not
   ldc.i4 0x66fa
   beq TestCase2544
   br Done

TestCase2544:
   ldc.i4 2644
   stloc.0
   ldc.i4 0x2e8f
   conv.i4
   not
   ldc.i4 0xffffd170
   bne.un Done

TestCase2543:
   ldc.i4 2643
   stloc.0
   ldc.i4 0x5882
   conv.i4
   not
   ldc.i4 0xffffa77d
   beq TestCase2542
   br Done

TestCase2542:
   ldc.i4 2642
   stloc.0
   ldc.i4 0x60b6
   conv.i4
   not
   ldc.i4 0xffff9f49
   beq TestCase2541
   br Done

TestCase2541:
   ldc.i4 2641
   stloc.0
   ldc.i4 0xffff9b1f
   conv.i4
   not
   ldc.i4 0x64e0
   beq TestCase2540
   br Done

TestCase2540:
   ldc.i4 2640
   stloc.0
   ldc.i4 0xffffa8c8
   conv.i4
   not
   ldc.i4 0x5737
   bne.un Done

TestCase2539:
   ldc.i4 2639
   stloc.0
   ldc.i4 0xffffc287
   conv.i4
   not
   ldc.i4 0x3d78
   bne.un Done

TestCase2538:
   ldc.i4 2638
   stloc.0
   ldc.i4 0x3d1c
   conv.i4
   not
   ldc.i4 0xffffc2e3
   bne.un Done

TestCase2537:
   ldc.i4 2637
   stloc.0
   ldc.i4 0x4b6b
   conv.i4
   not
   ldc.i4 0xffffb494
   beq TestCase2536
   br Done

TestCase2536:
   ldc.i4 2636
   stloc.0
   ldc.i4 0x5a1e
   conv.i4
   not
   ldc.i4 0xffffa5e1
   bne.un Done

TestCase2535:
   ldc.i4 2635
   stloc.0
   ldc.i4 0xffff9e6c
   conv.i4
   not
   ldc.i4 0x6193
   bne.un Done

TestCase2534:
   ldc.i4 2634
   stloc.0
   ldc.i4 0xffffd9cd
   conv.i4
   not
   ldc.i4 0x2632
   beq TestCase2533
   br Done

TestCase2533:
   ldc.i4 2633
   stloc.0
   ldc.i4 0xffffa7e2
   conv.i4
   not
   ldc.i4 0x581d
   beq TestCase2532
   br Done

TestCase2532:
   ldc.i4 2632
   stloc.0
   ldc.i4 0xffff9aed
   conv.i4
   not
   ldc.i4 0x6512
   bne.un Done

TestCase2531:
   ldc.i4 2631
   stloc.0
   ldc.i4 0x2b95
   conv.i4
   not
   ldc.i4 0xffffd46a
   beq TestCase2530
   br Done

TestCase2530:
   ldc.i4 2630
   stloc.0
   ldc.i4 0x3002
   conv.i4
   not
   ldc.i4 0xffffcffd
   bne.un Done

TestCase2529:
   ldc.i4 2629
   stloc.0
   ldc.i4 0x7785
   conv.i4
   not
   ldc.i4 0xffff887a
   bne.un Done

TestCase2528:
   ldc.i4 2628
   stloc.0
   ldc.i4 0xffffbcbd
   conv.i4
   not
   ldc.i4 0x4342
   bne.un Done

TestCase2527:
   ldc.i4 2627
   stloc.0
   ldc.i4 0xffff96f8
   conv.i4
   not
   ldc.i4 0x6907
   bne.un Done

TestCase2526:
   ldc.i4 2626
   stloc.0
   ldc.i4 0x3209
   conv.i4
   not
   ldc.i4 0xffffcdf6
   bne.un Done

TestCase2525:
   ldc.i4 2625
   stloc.0
   ldc.i4 0x1fcd
   conv.i4
   not
   ldc.i4 0xffffe032
   bne.un Done

TestCase2524:
   ldc.i4 2624
   stloc.0
   ldc.i4 0xffffe144
   conv.i4
   not
   ldc.i4 0x1ebb
   bne.un Done

TestCase2523:
   ldc.i4 2623
   stloc.0
   ldc.i4 0xffff8b7f
   conv.i4
   not
   ldc.i4 0x7480
   beq TestCase2522
   br Done

TestCase2522:
   ldc.i4 2622
   stloc.0
   ldc.i4 0xfffff118
   conv.i4
   not
   ldc.i4 0xee7
   bne.un Done

TestCase2521:
   ldc.i4 2621
   stloc.0
   ldc.i4 0x51a1
   conv.i4
   not
   ldc.i4 0xffffae5e
   bne.un Done

TestCase2520:
   ldc.i4 2620
   stloc.0
   ldc.i4 0xffffc6c6
   conv.i4
   not
   ldc.i4 0x3939
   bne.un Done

TestCase2519:
   ldc.i4 2619
   stloc.0
   ldc.i4 0x481a
   conv.i4
   not
   ldc.i4 0xffffb7e5
   bne.un Done

TestCase2518:
   ldc.i4 2618
   stloc.0
   ldc.i4 0x114d
   conv.i4
   not
   ldc.i4 0xffffeeb2
   bne.un Done

TestCase2517:
   ldc.i4 2617
   stloc.0
   ldc.i4 0xffffa4b8
   conv.i4
   not
   ldc.i4 0x5b47
   bne.un Done

TestCase2516:
   ldc.i4 2616
   stloc.0
   ldc.i4 0x2837
   conv.i4
   not
   ldc.i4 0xffffd7c8
   bne.un Done

TestCase2515:
   ldc.i4 2615
   stloc.0
   ldc.i4 0xffffc7d4
   conv.i4
   not
   ldc.i4 0x382b
   bne.un Done

TestCase2514:
   ldc.i4 2614
   stloc.0
   ldc.i4 0xa1d
   conv.i4
   not
   ldc.i4 0xfffff5e2
   beq TestCase2513
   br Done

TestCase2513:
   ldc.i4 2613
   stloc.0
   ldc.i4 0x39fc
   conv.i4
   not
   ldc.i4 0xffffc603
   bne.un Done

TestCase2512:
   ldc.i4 2612
   stloc.0
   ldc.i4 0xffff9fce
   conv.i4
   not
   ldc.i4 0x6031
   bne.un Done

TestCase2511:
   ldc.i4 2611
   stloc.0
   ldc.i4 0xffffadb4
   conv.i4
   not
   ldc.i4 0x524b
   bne.un Done

TestCase2510:
   ldc.i4 2610
   stloc.0
   ldc.i4 0xffffe41a
   conv.i4
   not
   ldc.i4 0x1be5
   beq TestCase2509
   br Done

TestCase2509:
   ldc.i4 2609
   stloc.0
   ldc.i4 0xffffca74
   conv.i4
   not
   ldc.i4 0x358b
   bne.un Done

TestCase2508:
   ldc.i4 2608
   stloc.0
   ldc.i4 0xffffe4c1
   conv.i4
   not
   ldc.i4 0x1b3e
   beq TestCase2507
   br Done

TestCase2507:
   ldc.i4 2607
   stloc.0
   ldc.i4 0xffffe377
   conv.i4
   not
   ldc.i4 0x1c88
   beq TestCase2506
   br Done

TestCase2506:
   ldc.i4 2606
   stloc.0
   ldc.i4 0xffffa07b
   conv.i4
   not
   ldc.i4 0x5f84
   bne.un Done

TestCase2505:
   ldc.i4 2605
   stloc.0
   ldc.i4 0x1234
   conv.i4
   not
   ldc.i4 0xffffedcb
   bne.un Done

TestCase2504:
   ldc.i4 2604
   stloc.0
   ldc.i4 0xffff9931
   conv.i4
   not
   ldc.i4 0x66ce
   beq TestCase2503
   br Done

TestCase2503:
   ldc.i4 2603
   stloc.0
   ldc.i4 0x30e3
   conv.i4
   not
   ldc.i4 0xffffcf1c
   bne.un Done

TestCase2502:
   ldc.i4 2602
   stloc.0
   ldc.i4 0xffffb1ed
   conv.i4
   not
   ldc.i4 0x4e12
   bne.un Done

TestCase2501:
   ldc.i4 2601
   stloc.0
   ldc.i4 0x74bb
   conv.i4
   not
   ldc.i4 0xffff8b44
   bne.un Done

TestCase2500:
   ldc.i4 2600
   stloc.0
   ldc.i4 0xffff9b40
   conv.i4
   not
   ldc.i4 0x64bf
   bne.un Done

TestCase2499:
   ldc.i4 2599
   stloc.0
   ldc.i4 0x710e
   conv.i4
   not
   ldc.i4 0xffff8ef1
   bne.un Done

TestCase2498:
   ldc.i4 2598
   stloc.0
   ldc.i4 0xffffe3ec
   conv.i4
   not
   ldc.i4 0x1c13
   bne.un Done

TestCase2497:
   ldc.i4 2597
   stloc.0
   ldc.i4 0xffffab32
   conv.i4
   not
   ldc.i4 0x54cd
   beq TestCase2496
   br Done

TestCase2496:
   ldc.i4 2596
   stloc.0
   ldc.i4 0xba7
   conv.i4
   not
   ldc.i4 0xfffff458
   beq TestCase2495
   br Done

TestCase2495:
   ldc.i4 2595
   stloc.0
   ldc.i4 0xffffb199
   conv.i4
   not
   ldc.i4 0x4e66
   beq TestCase2494
   br Done

TestCase2494:
   ldc.i4 2594
   stloc.0
   ldc.i4 0xffff8e1e
   conv.i4
   not
   ldc.i4 0x71e1
   beq TestCase2493
   br Done

TestCase2493:
   ldc.i4 2593
   stloc.0
   ldc.i4 0xffffe665
   conv.i4
   not
   ldc.i4 0x199a
   bne.un Done

TestCase2492:
   ldc.i4 2592
   stloc.0
   ldc.i4 0xffff8da4
   conv.i4
   not
   ldc.i4 0x725b
   beq TestCase2491
   br Done

TestCase2491:
   ldc.i4 2591
   stloc.0
   ldc.i4 0xffff9dca
   conv.i4
   not
   ldc.i4 0x6235
   beq TestCase2490
   br Done

TestCase2490:
   ldc.i4 2590
   stloc.0
   ldc.i4 0xffffd2aa
   conv.i4
   not
   ldc.i4 0x2d55
   bne.un Done

TestCase2489:
   ldc.i4 2589
   stloc.0
   ldc.i4 0x3cb1
   conv.i4
   not
   ldc.i4 0xffffc34e
   beq TestCase2488
   br Done

TestCase2488:
   ldc.i4 2588
   stloc.0
   ldc.i4 0x72c6
   conv.i4
   not
   ldc.i4 0xffff8d39
   bne.un Done

TestCase2487:
   ldc.i4 2587
   stloc.0
   ldc.i4 0x5225
   conv.i4
   not
   ldc.i4 0xffffadda
   beq TestCase2486
   br Done

TestCase2486:
   ldc.i4 2586
   stloc.0
   ldc.i4 0xffffe748
   conv.i4
   not
   ldc.i4 0x18b7
   beq TestCase2485
   br Done

TestCase2485:
   ldc.i4 2585
   stloc.0
   ldc.i4 0xf17
   conv.i4
   not
   ldc.i4 0xfffff0e8
   bne.un Done

TestCase2484:
   ldc.i4 2584
   stloc.0
   ldc.i4 0x2fe9
   conv.i4
   not
   ldc.i4 0xffffd016
   beq TestCase2483
   br Done

TestCase2483:
   ldc.i4 2583
   stloc.0
   ldc.i4 0xffffc300
   conv.i4
   not
   ldc.i4 0x3cff
   bne.un Done

TestCase2482:
   ldc.i4 2582
   stloc.0
   ldc.i4 0x42e8
   conv.i4
   not
   ldc.i4 0xffffbd17
   beq TestCase2481
   br Done

TestCase2481:
   ldc.i4 2581
   stloc.0
   ldc.i4 0xffff8356
   conv.i4
   not
   ldc.i4 0x7ca9
   beq TestCase2480
   br Done

TestCase2480:
   ldc.i4 2580
   stloc.0
   ldc.i4 0x4b2c
   conv.i4
   not
   ldc.i4 0xffffb4d3
   bne.un Done

TestCase2479:
   ldc.i4 2579
   stloc.0
   ldc.i4 0xffffd451
   conv.i4
   not
   ldc.i4 0x2bae
   beq TestCase2478
   br Done

TestCase2478:
   ldc.i4 2578
   stloc.0
   ldc.i4 0x64ce
   conv.i4
   not
   ldc.i4 0xffff9b31
   bne.un Done

TestCase2477:
   ldc.i4 2577
   stloc.0
   ldc.i4 0x69bc
   conv.i4
   not
   ldc.i4 0xffff9643
   bne.un Done

TestCase2476:
   ldc.i4 2576
   stloc.0
   ldc.i4 0x54ef
   conv.i4
   not
   ldc.i4 0xffffab10
   beq TestCase2475
   br Done

TestCase2475:
   ldc.i4 2575
   stloc.0
   ldc.i4 0x4270
   conv.i4
   not
   ldc.i4 0xffffbd8f
   bne.un Done

TestCase2474:
   ldc.i4 2574
   stloc.0
   ldc.i4 0x7ffe
   conv.i4
   not
   ldc.i4 0xffff8001
   bne.un Done

TestCase2473:
   ldc.i4 2573
   stloc.0
   ldc.i4 0xfffff917
   conv.i4
   not
   ldc.i4 0x6e8
   bne.un Done

TestCase2472:
   ldc.i4 2572
   stloc.0
   ldc.i4 0xffffbbb4
   conv.i4
   not
   ldc.i4 0x444b
   bne.un Done

TestCase2471:
   ldc.i4 2571
   stloc.0
   ldc.i4 0x2e58
   conv.i4
   not
   ldc.i4 0xffffd1a7
   beq TestCase2470
   br Done

TestCase2470:
   ldc.i4 2570
   stloc.0
   ldc.i4 0x22af
   conv.i4
   not
   ldc.i4 0xffffdd50
   bne.un Done

TestCase2469:
   ldc.i4 2569
   stloc.0
   ldc.i4 0x373b
   conv.i4
   not
   ldc.i4 0xffffc8c4
   beq TestCase2468
   br Done

TestCase2468:
   ldc.i4 2568
   stloc.0
   ldc.i4 0x22c
   conv.i4
   not
   ldc.i4 0xfffffdd3
   beq TestCase2467
   br Done

TestCase2467:
   ldc.i4 2567
   stloc.0
   ldc.i4 0xb0c
   conv.i4
   not
   ldc.i4 0xfffff4f3
   bne.un Done

TestCase2466:
   ldc.i4 2566
   stloc.0
   ldc.i4 0xffff8322
   conv.i4
   not
   ldc.i4 0x7cdd
   bne.un Done

TestCase2465:
   ldc.i4 2565
   stloc.0
   ldc.i4 0x6481
   conv.i4
   not
   ldc.i4 0xffff9b7e
   beq TestCase2464
   br Done

TestCase2464:
   ldc.i4 2564
   stloc.0
   ldc.i4 0x538d
   conv.i4
   not
   ldc.i4 0xffffac72
   bne.un Done

TestCase2463:
   ldc.i4 2563
   stloc.0
   ldc.i4 0xffffc346
   conv.i4
   not
   ldc.i4 0x3cb9
   beq TestCase2462
   br Done

TestCase2462:
   ldc.i4 2562
   stloc.0
   ldc.i4 0x32bd
   conv.i4
   not
   ldc.i4 0xffffcd42
   beq TestCase2461
   br Done

TestCase2461:
   ldc.i4 2561
   stloc.0
   ldc.i4 0xffffa215
   conv.i4
   not
   ldc.i4 0x5dea
   beq TestCase2460
   br Done

TestCase2460:
   ldc.i4 2560
   stloc.0
   ldc.i4 0xffffd0ee
   conv.i4
   not
   ldc.i4 0x2f11
   beq TestCase2459
   br Done

TestCase2459:
   ldc.i4 2559
   stloc.0
   ldc.i4 0x7a5e
   conv.i4
   not
   ldc.i4 0xffff85a1
   beq TestCase2458
   br Done

TestCase2458:
   ldc.i4 2558
   stloc.0
   ldc.i4 0xffffe79f
   conv.i4
   not
   ldc.i4 0x1860
   beq TestCase2457
   br Done

TestCase2457:
   ldc.i4 2557
   stloc.0
   ldc.i4 0xffffe316
   conv.i4
   not
   ldc.i4 0x1ce9
   beq TestCase2456
   br Done

TestCase2456:
   ldc.i4 2556
   stloc.0
   ldc.i4 0xffffb635
   conv.i4
   not
   ldc.i4 0x49ca
   beq TestCase2455
   br Done

TestCase2455:
   ldc.i4 2555
   stloc.0
   ldc.i4 0x433
   conv.i4
   not
   ldc.i4 0xfffffbcc
   bne.un Done

TestCase2454:
   ldc.i4 2554
   stloc.0
   ldc.i4 0xffffbd55
   conv.i4
   not
   ldc.i4 0x42aa
   bne.un Done

TestCase2453:
   ldc.i4 2553
   stloc.0
   ldc.i4 0xfffff3ba
   conv.i4
   not
   ldc.i4 0xc45
   beq TestCase2452
   br Done

TestCase2452:
   ldc.i4 2552
   stloc.0
   ldc.i4 0x6746
   conv.i4
   not
   ldc.i4 0xffff98b9
   bne.un Done

TestCase2451:
   ldc.i4 2551
   stloc.0
   ldc.i4 0x4276
   conv.i4
   not
   ldc.i4 0xffffbd89
   beq TestCase2450
   br Done

TestCase2450:
   ldc.i4 2550
   stloc.0
   ldc.i4 0xffffe689
   conv.i4
   not
   ldc.i4 0x1976
   bne.un Done

TestCase2449:
   ldc.i4 2549
   stloc.0
   ldc.i4 0x2d80
   conv.i4
   not
   ldc.i4 0xffffd27f
   beq TestCase2448
   br Done

TestCase2448:
   ldc.i4 2548
   stloc.0
   ldc.i4 0x38d5
   conv.i4
   not
   ldc.i4 0xffffc72a
   beq TestCase2447
   br Done

TestCase2447:
   ldc.i4 2547
   stloc.0
   ldc.i4 0x4861
   conv.i4
   not
   ldc.i4 0xffffb79e
   bne.un Done

TestCase2446:
   ldc.i4 2546
   stloc.0
   ldc.i4 0xffffa8fd
   conv.i4
   not
   ldc.i4 0x5702
   beq TestCase2445
   br Done

TestCase2445:
   ldc.i4 2545
   stloc.0
   ldc.i4 0xffffc797
   conv.i4
   not
   ldc.i4 0x3868
   beq TestCase2444
   br Done

TestCase2444:
   ldc.i4 2544
   stloc.0
   ldc.i4 0xffffa7df
   conv.i4
   not
   ldc.i4 0x5820
   bne.un Done

TestCase2443:
   ldc.i4 2543
   stloc.0
   ldc.i4 0x7766
   conv.i4
   not
   ldc.i4 0xffff8899
   beq TestCase2442
   br Done

TestCase2442:
   ldc.i4 2542
   stloc.0
   ldc.i4 0xffffdf5f
   conv.i4
   not
   ldc.i4 0x20a0
   bne.un Done

TestCase2441:
   ldc.i4 2541
   stloc.0
   ldc.i4 0x1103
   conv.i4
   not
   ldc.i4 0xffffeefc
   bne.un Done

TestCase2440:
   ldc.i4 2540
   stloc.0
   ldc.i4 0x28ef
   conv.i4
   not
   ldc.i4 0xffffd710
   bne.un Done

TestCase2439:
   ldc.i4 2539
   stloc.0
   ldc.i4 0x3eab
   conv.i4
   not
   ldc.i4 0xffffc154
   beq TestCase2438
   br Done

TestCase2438:
   ldc.i4 2538
   stloc.0
   ldc.i4 0xffffdb60
   conv.i4
   not
   ldc.i4 0x249f
   bne.un Done

TestCase2437:
   ldc.i4 2537
   stloc.0
   ldc.i4 0xffffd069
   conv.i4
   not
   ldc.i4 0x2f96
   bne.un Done

TestCase2436:
   ldc.i4 2536
   stloc.0
   ldc.i4 0xffffd1d0
   conv.i4
   not
   ldc.i4 0x2e2f
   bne.un Done

TestCase2435:
   ldc.i4 2535
   stloc.0
   ldc.i4 0xffffcd3a
   conv.i4
   not
   ldc.i4 0x32c5
   bne.un Done

TestCase2434:
   ldc.i4 2534
   stloc.0
   ldc.i4 0x6977
   conv.i4
   not
   ldc.i4 0xffff9688
   beq TestCase2433
   br Done

TestCase2433:
   ldc.i4 2533
   stloc.0
   ldc.i4 0x5717
   conv.i4
   not
   ldc.i4 0xffffa8e8
   beq TestCase2432
   br Done

TestCase2432:
   ldc.i4 2532
   stloc.0
   ldc.i4 0xffffe1b1
   conv.i4
   not
   ldc.i4 0x1e4e
   bne.un Done

TestCase2431:
   ldc.i4 2531
   stloc.0
   ldc.i4 0xffff8646
   conv.i4
   not
   ldc.i4 0x79b9
   beq TestCase2430
   br Done

TestCase2430:
   ldc.i4 2530
   stloc.0
   ldc.i4 0x3a33
   conv.i4
   not
   ldc.i4 0xffffc5cc
   bne.un Done

TestCase2429:
   ldc.i4 2529
   stloc.0
   ldc.i4 0x35b5
   conv.i4
   not
   ldc.i4 0xffffca4a
   beq TestCase2428
   br Done

TestCase2428:
   ldc.i4 2528
   stloc.0
   ldc.i4 0xffffbd4a
   conv.i4
   not
   ldc.i4 0x42b5
   bne.un Done

TestCase2427:
   ldc.i4 2527
   stloc.0
   ldc.i4 0xffffb8cf
   conv.i4
   not
   ldc.i4 0x4730
   beq TestCase2426
   br Done

TestCase2426:
   ldc.i4 2526
   stloc.0
   ldc.i4 0x5c22
   conv.i4
   not
   ldc.i4 0xffffa3dd
   beq TestCase2425
   br Done

TestCase2425:
   ldc.i4 2525
   stloc.0
   ldc.i4 0x4bc3
   conv.i4
   not
   ldc.i4 0xffffb43c
   bne.un Done

TestCase2424:
   ldc.i4 2524
   stloc.0
   ldc.i4 0xffff915f
   conv.i4
   not
   ldc.i4 0x6ea0
   beq TestCase2423
   br Done

TestCase2423:
   ldc.i4 2523
   stloc.0
   ldc.i4 0xffff9b06
   conv.i4
   not
   ldc.i4 0x64f9
   beq TestCase2422
   br Done

TestCase2422:
   ldc.i4 2522
   stloc.0
   ldc.i4 0xffff8641
   conv.i4
   not
   ldc.i4 0x79be
   beq TestCase2421
   br Done

TestCase2421:
   ldc.i4 2521
   stloc.0
   ldc.i4 0xffffcb33
   conv.i4
   not
   ldc.i4 0x34cc
   beq TestCase2420
   br Done

TestCase2420:
   ldc.i4 2520
   stloc.0
   ldc.i4 0x1927
   conv.i4
   not
   ldc.i4 0xffffe6d8
   bne.un Done

TestCase2419:
   ldc.i4 2519
   stloc.0
   ldc.i4 0xfffffc3c
   conv.i4
   not
   ldc.i4 0x3c3
   bne.un Done

TestCase2418:
   ldc.i4 2518
   stloc.0
   ldc.i4 0xffff9120
   conv.i4
   not
   ldc.i4 0x6edf
   beq TestCase2417
   br Done

TestCase2417:
   ldc.i4 2517
   stloc.0
   ldc.i4 0xffffd10c
   conv.i4
   not
   ldc.i4 0x2ef3
   bne.un Done

TestCase2416:
   ldc.i4 2516
   stloc.0
   ldc.i4 0x6685
   conv.i4
   not
   ldc.i4 0xffff997a
   bne.un Done

TestCase2415:
   ldc.i4 2515
   stloc.0
   ldc.i4 0x501c
   conv.i4
   not
   ldc.i4 0xffffafe3
   beq TestCase2414
   br Done

TestCase2414:
   ldc.i4 2514
   stloc.0
   ldc.i4 0xffff9de1
   conv.i4
   not
   ldc.i4 0x621e
   bne.un Done

TestCase2413:
   ldc.i4 2513
   stloc.0
   ldc.i4 0x7c8d
   conv.i4
   not
   ldc.i4 0xffff8372
   bne.un Done

TestCase2412:
   ldc.i4 2512
   stloc.0
   ldc.i4 0xffffd353
   conv.i4
   not
   ldc.i4 0x2cac
   beq TestCase2411
   br Done

TestCase2411:
   ldc.i4 2511
   stloc.0
   ldc.i4 0xfffff85c
   conv.i4
   not
   ldc.i4 0x7a3
   bne.un Done

TestCase2410:
   ldc.i4 2510
   stloc.0
   ldc.i4 0xffffa87b
   conv.i4
   not
   ldc.i4 0x5784
   beq TestCase2409
   br Done

TestCase2409:
   ldc.i4 2509
   stloc.0
   ldc.i4 0x7bf9
   conv.i4
   not
   ldc.i4 0xffff8406
   beq TestCase2408
   br Done

TestCase2408:
   ldc.i4 2508
   stloc.0
   ldc.i4 0xffffc0f2
   conv.i4
   not
   ldc.i4 0x3f0d
   beq TestCase2407
   br Done

TestCase2407:
   ldc.i4 2507
   stloc.0
   ldc.i4 0xfffff56b
   conv.i4
   not
   ldc.i4 0xa94
   bne.un Done

TestCase2406:
   ldc.i4 2506
   stloc.0
   ldc.i4 0xffffd783
   conv.i4
   not
   ldc.i4 0x287c
   beq TestCase2405
   br Done

TestCase2405:
   ldc.i4 2505
   stloc.0
   ldc.i4 0xffffee3a
   conv.i4
   not
   ldc.i4 0x11c5
   bne.un Done

TestCase2404:
   ldc.i4 2504
   stloc.0
   ldc.i4 0xda4
   conv.i4
   not
   ldc.i4 0xfffff25b
   bne.un Done

TestCase2403:
   ldc.i4 2503
   stloc.0
   ldc.i4 0x2576
   conv.i4
   not
   ldc.i4 0xffffda89
   bne.un Done

TestCase2402:
   ldc.i4 2502
   stloc.0
   ldc.i4 0xcf7
   conv.i4
   not
   ldc.i4 0xfffff308
   bne.un Done

TestCase2401:
   ldc.i4 2501
   stloc.0
   ldc.i4 0xffffbef9
   conv.i4
   not
   ldc.i4 0x4106
   bne.un Done

TestCase2400:
   ldc.i4 2500
   stloc.0
   ldc.i4 0xffff9cbd
   conv.i4
   not
   ldc.i4 0x6342
   beq TestCase2399
   br Done

TestCase2399:
   ldc.i4 2499
   stloc.0
   ldc.i4 0xf1d
   conv.i4
   not
   ldc.i4 0xfffff0e2
   bne.un Done

TestCase2398:
   ldc.i4 2498
   stloc.0
   ldc.i4 0xffffde26
   conv.i4
   not
   ldc.i4 0x21d9
   bne.un Done

TestCase2397:
   ldc.i4 2497
   stloc.0
   ldc.i4 0xfffff210
   conv.i4
   not
   ldc.i4 0xdef
   bne.un Done

TestCase2396:
   ldc.i4 2496
   stloc.0
   ldc.i4 0xffffb0e0
   conv.i4
   not
   ldc.i4 0x4f1f
   beq TestCase2395
   br Done

TestCase2395:
   ldc.i4 2495
   stloc.0
   ldc.i4 0xffffa919
   conv.i4
   not
   ldc.i4 0x56e6
   beq TestCase2394
   br Done

TestCase2394:
   ldc.i4 2494
   stloc.0
   ldc.i4 0x18b0
   conv.i4
   not
   ldc.i4 0xffffe74f
   beq TestCase2393
   br Done

TestCase2393:
   ldc.i4 2493
   stloc.0
   ldc.i4 0x6b5
   conv.i4
   not
   ldc.i4 0xfffff94a
   bne.un Done

TestCase2392:
   ldc.i4 2492
   stloc.0
   ldc.i4 0x5a0
   conv.i4
   not
   ldc.i4 0xfffffa5f
   beq TestCase2391
   br Done

TestCase2391:
   ldc.i4 2491
   stloc.0
   ldc.i4 0xffffc0cd
   conv.i4
   not
   ldc.i4 0x3f32
   bne.un Done

TestCase2390:
   ldc.i4 2490
   stloc.0
   ldc.i4 0x7c8c
   conv.i4
   not
   ldc.i4 0xffff8373
   bne.un Done

TestCase2389:
   ldc.i4 2489
   stloc.0
   ldc.i4 0xfffff15d
   conv.i4
   not
   ldc.i4 0xea2
   beq TestCase2388
   br Done

TestCase2388:
   ldc.i4 2488
   stloc.0
   ldc.i4 0x4464
   conv.i4
   not
   ldc.i4 0xffffbb9b
   bne.un Done

TestCase2387:
   ldc.i4 2487
   stloc.0
   ldc.i4 0xffffbfdc
   conv.i4
   not
   ldc.i4 0x4023
   beq TestCase2386
   br Done

TestCase2386:
   ldc.i4 2486
   stloc.0
   ldc.i4 0x383d
   conv.i4
   not
   ldc.i4 0xffffc7c2
   bne.un Done

TestCase2385:
   ldc.i4 2485
   stloc.0
   ldc.i4 0xffffb882
   conv.i4
   not
   ldc.i4 0x477d
   beq TestCase2384
   br Done

TestCase2384:
   ldc.i4 2484
   stloc.0
   ldc.i4 0xfffffa6c
   conv.i4
   not
   ldc.i4 0x593
   bne.un Done

TestCase2383:
   ldc.i4 2483
   stloc.0
   ldc.i4 0x2e96
   conv.i4
   not
   ldc.i4 0xffffd169
   bne.un Done

TestCase2382:
   ldc.i4 2482
   stloc.0
   ldc.i4 0xffffb647
   conv.i4
   not
   ldc.i4 0x49b8
   beq TestCase2381
   br Done

TestCase2381:
   ldc.i4 2481
   stloc.0
   ldc.i4 0x351d
   conv.i4
   not
   ldc.i4 0xffffcae2
   bne.un Done

TestCase2380:
   ldc.i4 2480
   stloc.0
   ldc.i4 0xe6a
   conv.i4
   not
   ldc.i4 0xfffff195
   bne.un Done

TestCase2379:
   ldc.i4 2479
   stloc.0
   ldc.i4 0xffffbca0
   conv.i4
   not
   ldc.i4 0x435f
   bne.un Done

TestCase2378:
   ldc.i4 2478
   stloc.0
   ldc.i4 0xffffb13d
   conv.i4
   not
   ldc.i4 0x4ec2
   beq TestCase2377
   br Done

TestCase2377:
   ldc.i4 2477
   stloc.0
   ldc.i4 0xffff88bc
   conv.i4
   not
   ldc.i4 0x7743
   bne.un Done

TestCase2376:
   ldc.i4 2476
   stloc.0
   ldc.i4 0x2502
   conv.i4
   not
   ldc.i4 0xffffdafd
   beq TestCase2375
   br Done

TestCase2375:
   ldc.i4 2475
   stloc.0
   ldc.i4 0x7ecc
   conv.i4
   not
   ldc.i4 0xffff8133
   bne.un Done

TestCase2374:
   ldc.i4 2474
   stloc.0
   ldc.i4 0xffffa922
   conv.i4
   not
   ldc.i4 0x56dd
   beq TestCase2373
   br Done

TestCase2373:
   ldc.i4 2473
   stloc.0
   ldc.i4 0xffffca3f
   conv.i4
   not
   ldc.i4 0x35c0
   beq TestCase2372
   br Done

TestCase2372:
   ldc.i4 2472
   stloc.0
   ldc.i4 0x7960
   conv.i4
   not
   ldc.i4 0xffff869f
   bne.un Done

TestCase2371:
   ldc.i4 2471
   stloc.0
   ldc.i4 0xffffdbad
   conv.i4
   not
   ldc.i4 0x2452
   bne.un Done

TestCase2370:
   ldc.i4 2470
   stloc.0
   ldc.i4 0x2677
   conv.i4
   not
   ldc.i4 0xffffd988
   bne.un Done

TestCase2369:
   ldc.i4 2469
   stloc.0
   ldc.i4 0xffffe536
   conv.i4
   not
   ldc.i4 0x1ac9
   beq TestCase2368
   br Done

TestCase2368:
   ldc.i4 2468
   stloc.0
   ldc.i4 0xdc2
   conv.i4
   not
   ldc.i4 0xfffff23d
   bne.un Done

TestCase2367:
   ldc.i4 2467
   stloc.0
   ldc.i4 0xffffb563
   conv.i4
   not
   ldc.i4 0x4a9c
   bne.un Done

TestCase2366:
   ldc.i4 2466
   stloc.0
   ldc.i4 0x574d
   conv.i4
   not
   ldc.i4 0xffffa8b2
   beq TestCase2365
   br Done

TestCase2365:
   ldc.i4 2465
   stloc.0
   ldc.i4 0xffff886c
   conv.i4
   not
   ldc.i4 0x7793
   bne.un Done

TestCase2364:
   ldc.i4 2464
   stloc.0
   ldc.i4 0xffffd5d9
   conv.i4
   not
   ldc.i4 0x2a26
   beq TestCase2363
   br Done

TestCase2363:
   ldc.i4 2463
   stloc.0
   ldc.i4 0x3bb8
   conv.i4
   not
   ldc.i4 0xffffc447
   bne.un Done

TestCase2362:
   ldc.i4 2462
   stloc.0
   ldc.i4 0x223
   conv.i4
   not
   ldc.i4 0xfffffddc
   beq TestCase2361
   br Done

TestCase2361:
   ldc.i4 2461
   stloc.0
   ldc.i4 0xffff90c9
   conv.i4
   not
   ldc.i4 0x6f36
   bne.un Done

TestCase2360:
   ldc.i4 2460
   stloc.0
   ldc.i4 0xffffd8e1
   conv.i4
   not
   ldc.i4 0x271e
   beq TestCase2359
   br Done

TestCase2359:
   ldc.i4 2459
   stloc.0
   ldc.i4 0x2196
   conv.i4
   not
   ldc.i4 0xffffde69
   beq TestCase2358
   br Done

TestCase2358:
   ldc.i4 2458
   stloc.0
   ldc.i4 0xffffa258
   conv.i4
   not
   ldc.i4 0x5da7
   bne.un Done

TestCase2357:
   ldc.i4 2457
   stloc.0
   ldc.i4 0xffffbc40
   conv.i4
   not
   ldc.i4 0x43bf
   bne.un Done

TestCase2356:
   ldc.i4 2456
   stloc.0
   ldc.i4 0x1bd5
   conv.i4
   not
   ldc.i4 0xffffe42a
   bne.un Done

TestCase2355:
   ldc.i4 2455
   stloc.0
   ldc.i4 0xffffc534
   conv.i4
   not
   ldc.i4 0x3acb
   beq TestCase2354
   br Done

TestCase2354:
   ldc.i4 2454
   stloc.0
   ldc.i4 0xffff815a
   conv.i4
   not
   ldc.i4 0x7ea5
   bne.un Done

TestCase2353:
   ldc.i4 2453
   stloc.0
   ldc.i4 0x2197
   conv.i4
   not
   ldc.i4 0xffffde68
   bne.un Done

TestCase2352:
   ldc.i4 2452
   stloc.0
   ldc.i4 0xffffc4a7
   conv.i4
   not
   ldc.i4 0x3b58
   bne.un Done

TestCase2351:
   ldc.i4 2451
   stloc.0
   ldc.i4 0x1389
   conv.i4
   not
   ldc.i4 0xffffec76
   bne.un Done

TestCase2350:
   ldc.i4 2450
   stloc.0
   ldc.i4 0x2569
   conv.i4
   not
   ldc.i4 0xffffda96
   bne.un Done

TestCase2349:
   ldc.i4 2449
   stloc.0
   ldc.i4 0x514
   conv.i4
   not
   ldc.i4 0xfffffaeb
   beq TestCase2348
   br Done

TestCase2348:
   ldc.i4 2448
   stloc.0
   ldc.i4 0xffff92ce
   conv.i4
   not
   ldc.i4 0x6d31
   beq TestCase2347
   br Done

TestCase2347:
   ldc.i4 2447
   stloc.0
   ldc.i4 0xffff812c
   conv.i4
   not
   ldc.i4 0x7ed3
   bne.un Done

TestCase2346:
   ldc.i4 2446
   stloc.0
   ldc.i4 0x4472
   conv.i4
   not
   ldc.i4 0xffffbb8d
   beq TestCase2345
   br Done

TestCase2345:
   ldc.i4 2445
   stloc.0
   ldc.i4 0x165c
   conv.i4
   not
   ldc.i4 0xffffe9a3
   bne.un Done

TestCase2344:
   ldc.i4 2444
   stloc.0
   ldc.i4 0x6d23
   conv.i4
   not
   ldc.i4 0xffff92dc
   beq TestCase2343
   br Done

TestCase2343:
   ldc.i4 2443
   stloc.0
   ldc.i4 0x4c09
   conv.i4
   not
   ldc.i4 0xffffb3f6
   bne.un Done

TestCase2342:
   ldc.i4 2442
   stloc.0
   ldc.i4 0xffffd3a4
   conv.i4
   not
   ldc.i4 0x2c5b
   beq TestCase2341
   br Done

TestCase2341:
   ldc.i4 2441
   stloc.0
   ldc.i4 0x1780
   conv.i4
   not
   ldc.i4 0xffffe87f
   beq TestCase2340
   br Done

TestCase2340:
   ldc.i4 2440
   stloc.0
   ldc.i4 0xffff81c4
   conv.i4
   not
   ldc.i4 0x7e3b
   beq TestCase2339
   br Done

TestCase2339:
   ldc.i4 2439
   stloc.0
   ldc.i4 0x7495
   conv.i4
   not
   ldc.i4 0xffff8b6a
   beq TestCase2338
   br Done

TestCase2338:
   ldc.i4 2438
   stloc.0
   ldc.i4 0xfffff6f2
   conv.i4
   not
   ldc.i4 0x90d
   beq TestCase2337
   br Done

TestCase2337:
   ldc.i4 2437
   stloc.0
   ldc.i4 0x40d7
   conv.i4
   not
   ldc.i4 0xffffbf28
   beq TestCase2336
   br Done

TestCase2336:
   ldc.i4 2436
   stloc.0
   ldc.i4 0xffff82dc
   conv.i4
   not
   ldc.i4 0x7d23
   beq TestCase2335
   br Done

TestCase2335:
   ldc.i4 2435
   stloc.0
   ldc.i4 0x3df1
   conv.i4
   not
   ldc.i4 0xffffc20e
   beq TestCase2334
   br Done

TestCase2334:
   ldc.i4 2434
   stloc.0
   ldc.i4 0xffffe3df
   conv.i4
   not
   ldc.i4 0x1c20
   bne.un Done

TestCase2333:
   ldc.i4 2433
   stloc.0
   ldc.i4 0xffffd866
   conv.i4
   not
   ldc.i4 0x2799
   bne.un Done

TestCase2332:
   ldc.i4 2432
   stloc.0
   ldc.i4 0x5da5
   conv.i4
   not
   ldc.i4 0xffffa25a
   beq TestCase2331
   br Done

TestCase2331:
   ldc.i4 2431
   stloc.0
   ldc.i4 0xbdc
   conv.i4
   not
   ldc.i4 0xfffff423
   bne.un Done

TestCase2330:
   ldc.i4 2430
   stloc.0
   ldc.i4 0xffff9f6d
   conv.i4
   not
   ldc.i4 0x6092
   bne.un Done

TestCase2329:
   ldc.i4 2429
   stloc.0
   ldc.i4 0xac1
   conv.i4
   not
   ldc.i4 0xfffff53e
   beq TestCase2328
   br Done

TestCase2328:
   ldc.i4 2428
   stloc.0
   ldc.i4 0xffffa088
   conv.i4
   not
   ldc.i4 0x5f77
   beq TestCase2327
   br Done

TestCase2327:
   ldc.i4 2427
   stloc.0
   ldc.i4 0x5edc
   conv.i4
   not
   ldc.i4 0xffffa123
   beq TestCase2326
   br Done

TestCase2326:
   ldc.i4 2426
   stloc.0
   ldc.i4 0x3ad
   conv.i4
   not
   ldc.i4 0xfffffc52
   beq TestCase2325
   br Done

TestCase2325:
   ldc.i4 2425
   stloc.0
   ldc.i4 0xffffdde2
   conv.i4
   not
   ldc.i4 0x221d
   bne.un Done

TestCase2324:
   ldc.i4 2424
   stloc.0
   ldc.i4 0x651
   conv.i4
   not
   ldc.i4 0xfffff9ae
   bne.un Done

TestCase2323:
   ldc.i4 2423
   stloc.0
   ldc.i4 0x3af6
   conv.i4
   not
   ldc.i4 0xffffc509
   bne.un Done

TestCase2322:
   ldc.i4 2422
   stloc.0
   ldc.i4 0xffffa0cf
   conv.i4
   not
   ldc.i4 0x5f30
   bne.un Done

TestCase2321:
   ldc.i4 2421
   stloc.0
   ldc.i4 0xffffb449
   conv.i4
   not
   ldc.i4 0x4bb6
   bne.un Done

TestCase2320:
   ldc.i4 2420
   stloc.0
   ldc.i4 0xffffded3
   conv.i4
   not
   ldc.i4 0x212c
   beq TestCase2319
   br Done

TestCase2319:
   ldc.i4 2419
   stloc.0
   ldc.i4 0x72ab
   conv.i4
   not
   ldc.i4 0xffff8d54
   bne.un Done

TestCase2318:
   ldc.i4 2418
   stloc.0
   ldc.i4 0x60c8
   conv.i4
   not
   ldc.i4 0xffff9f37
   bne.un Done

TestCase2317:
   ldc.i4 2417
   stloc.0
   ldc.i4 0x3368
   conv.i4
   not
   ldc.i4 0xffffcc97
   bne.un Done

TestCase2316:
   ldc.i4 2416
   stloc.0
   ldc.i4 0x6cdb
   conv.i4
   not
   ldc.i4 0xffff9324
   beq TestCase2315
   br Done

TestCase2315:
   ldc.i4 2415
   stloc.0
   ldc.i4 0x1236
   conv.i4
   not
   ldc.i4 0xffffedc9
   bne.un Done

TestCase2314:
   ldc.i4 2414
   stloc.0
   ldc.i4 0xaaa
   conv.i4
   not
   ldc.i4 0xfffff555
   bne.un Done

TestCase2313:
   ldc.i4 2413
   stloc.0
   ldc.i4 0x50b8
   conv.i4
   not
   ldc.i4 0xffffaf47
   beq TestCase2312
   br Done

TestCase2312:
   ldc.i4 2412
   stloc.0
   ldc.i4 0x1dc9
   conv.i4
   not
   ldc.i4 0xffffe236
   bne.un Done

TestCase2311:
   ldc.i4 2411
   stloc.0
   ldc.i4 0xffff98fb
   conv.i4
   not
   ldc.i4 0x6704
   beq TestCase2310
   br Done

TestCase2310:
   ldc.i4 2410
   stloc.0
   ldc.i4 0xffff8672
   conv.i4
   not
   ldc.i4 0x798d
   bne.un Done

TestCase2309:
   ldc.i4 2409
   stloc.0
   ldc.i4 0xffffa906
   conv.i4
   not
   ldc.i4 0x56f9
   beq TestCase2308
   br Done

TestCase2308:
   ldc.i4 2408
   stloc.0
   ldc.i4 0xffffa2f1
   conv.i4
   not
   ldc.i4 0x5d0e
   beq TestCase2307
   br Done

TestCase2307:
   ldc.i4 2407
   stloc.0
   ldc.i4 0xffffa08c
   conv.i4
   not
   ldc.i4 0x5f73
   bne.un Done

TestCase2306:
   ldc.i4 2406
   stloc.0
   ldc.i4 0x1bd4
   conv.i4
   not
   ldc.i4 0xffffe42b
   beq TestCase2305
   br Done

TestCase2305:
   ldc.i4 2405
   stloc.0
   ldc.i4 0x73a7
   conv.i4
   not
   ldc.i4 0xffff8c58
   beq TestCase2304
   br Done

TestCase2304:
   ldc.i4 2404
   stloc.0
   ldc.i4 0x4c0
   conv.i4
   not
   ldc.i4 0xfffffb3f
   beq TestCase2303
   br Done

TestCase2303:
   ldc.i4 2403
   stloc.0
   ldc.i4 0x3ed0
   conv.i4
   not
   ldc.i4 0xffffc12f
   beq TestCase2302
   br Done

TestCase2302:
   ldc.i4 2402
   stloc.0
   ldc.i4 0x5f0
   conv.i4
   not
   ldc.i4 0xfffffa0f
   beq TestCase2301
   br Done

TestCase2301:
   ldc.i4 2401
   stloc.0
   ldc.i4 0xffff8906
   conv.i4
   not
   ldc.i4 0x76f9
   beq TestCase2300
   br Done

TestCase2300:
   ldc.i4 2400
   stloc.0
   ldc.i4 0xffffd890
   conv.i4
   not
   ldc.i4 0x276f
   bne.un Done

TestCase2299:
   ldc.i4 2399
   stloc.0
   ldc.i4 0x618b
   conv.i4
   not
   ldc.i4 0xffff9e74
   beq TestCase2298
   br Done

TestCase2298:
   ldc.i4 2398
   stloc.0
   ldc.i4 0xffffefe7
   conv.i4
   not
   ldc.i4 0x1018
   bne.un Done

TestCase2297:
   ldc.i4 2397
   stloc.0
   ldc.i4 0x42cd
   conv.i4
   not
   ldc.i4 0xffffbd32
   beq TestCase2296
   br Done

TestCase2296:
   ldc.i4 2396
   stloc.0
   ldc.i4 0x2f01
   conv.i4
   not
   ldc.i4 0xffffd0fe
   beq TestCase2295
   br Done

TestCase2295:
   ldc.i4 2395
   stloc.0
   ldc.i4 0xdbb
   conv.i4
   not
   ldc.i4 0xfffff244
   bne.un Done

TestCase2294:
   ldc.i4 2394
   stloc.0
   ldc.i4 0x2932
   conv.i4
   not
   ldc.i4 0xffffd6cd
   beq TestCase2293
   br Done

TestCase2293:
   ldc.i4 2393
   stloc.0
   ldc.i4 0xffffe44d
   conv.i4
   not
   ldc.i4 0x1bb2
   beq TestCase2292
   br Done

TestCase2292:
   ldc.i4 2392
   stloc.0
   ldc.i4 0x1eb2
   conv.i4
   not
   ldc.i4 0xffffe14d
   beq TestCase2291
   br Done

TestCase2291:
   ldc.i4 2391
   stloc.0
   ldc.i4 0xfffff428
   conv.i4
   not
   ldc.i4 0xbd7
   beq TestCase2290
   br Done

TestCase2290:
   ldc.i4 2390
   stloc.0
   ldc.i4 0xfffff508
   conv.i4
   not
   ldc.i4 0xaf7
   bne.un Done

TestCase2289:
   ldc.i4 2389
   stloc.0
   ldc.i4 0xffffc7ab
   conv.i4
   not
   ldc.i4 0x3854
   bne.un Done

TestCase2288:
   ldc.i4 2388
   stloc.0
   ldc.i4 0x4468
   conv.i4
   not
   ldc.i4 0xffffbb97
   beq TestCase2287
   br Done

TestCase2287:
   ldc.i4 2387
   stloc.0
   ldc.i4 0x3b6
   conv.i4
   not
   ldc.i4 0xfffffc49
   beq TestCase2286
   br Done

TestCase2286:
   ldc.i4 2386
   stloc.0
   ldc.i4 0x3b6d
   conv.i4
   not
   ldc.i4 0xffffc492
   bne.un Done

TestCase2285:
   ldc.i4 2385
   stloc.0
   ldc.i4 0x2b11
   conv.i4
   not
   ldc.i4 0xffffd4ee
   bne.un Done

TestCase2284:
   ldc.i4 2384
   stloc.0
   ldc.i4 0x653e
   conv.i4
   not
   ldc.i4 0xffff9ac1
   beq TestCase2283
   br Done

TestCase2283:
   ldc.i4 2383
   stloc.0
   ldc.i4 0xffffa640
   conv.i4
   not
   ldc.i4 0x59bf
   bne.un Done

TestCase2282:
   ldc.i4 2382
   stloc.0
   ldc.i4 0xfffff87d
   conv.i4
   not
   ldc.i4 0x782
   bne.un Done

TestCase2281:
   ldc.i4 2381
   stloc.0
   ldc.i4 0xfffffe30
   conv.i4
   not
   ldc.i4 0x1cf
   beq TestCase2280
   br Done

TestCase2280:
   ldc.i4 2380
   stloc.0
   ldc.i4 0xffffceb5
   conv.i4
   not
   ldc.i4 0x314a
   bne.un Done

TestCase2279:
   ldc.i4 2379
   stloc.0
   ldc.i4 0xfffff78a
   conv.i4
   not
   ldc.i4 0x875
   beq TestCase2278
   br Done

TestCase2278:
   ldc.i4 2378
   stloc.0
   ldc.i4 0x630c
   conv.i4
   not
   ldc.i4 0xffff9cf3
   bne.un Done

TestCase2277:
   ldc.i4 2377
   stloc.0
   ldc.i4 0xffffc7d9
   conv.i4
   not
   ldc.i4 0x3826
   bne.un Done

TestCase2276:
   ldc.i4 2376
   stloc.0
   ldc.i4 0xfffff54d
   conv.i4
   not
   ldc.i4 0xab2
   bne.un Done

TestCase2275:
   ldc.i4 2375
   stloc.0
   ldc.i4 0xffffcdd7
   conv.i4
   not
   ldc.i4 0x3228
   bne.un Done

TestCase2274:
   ldc.i4 2374
   stloc.0
   ldc.i4 0x59e1
   conv.i4
   not
   ldc.i4 0xffffa61e
   bne.un Done

TestCase2273:
   ldc.i4 2373
   stloc.0
   ldc.i4 0x57b1
   conv.i4
   not
   ldc.i4 0xffffa84e
   beq TestCase2272
   br Done

TestCase2272:
   ldc.i4 2372
   stloc.0
   ldc.i4 0xffff9410
   conv.i4
   not
   ldc.i4 0x6bef
   beq TestCase2271
   br Done

TestCase2271:
   ldc.i4 2371
   stloc.0
   ldc.i4 0xffff990c
   conv.i4
   not
   ldc.i4 0x66f3
   beq TestCase2270
   br Done

TestCase2270:
   ldc.i4 2370
   stloc.0
   ldc.i4 0xffff81af
   conv.i4
   not
   ldc.i4 0x7e50
   bne.un Done

TestCase2269:
   ldc.i4 2369
   stloc.0
   ldc.i4 0xffff9d55
   conv.i4
   not
   ldc.i4 0x62aa
   beq TestCase2268
   br Done

TestCase2268:
   ldc.i4 2368
   stloc.0
   ldc.i4 0x7961
   conv.i4
   not
   ldc.i4 0xffff869e
   bne.un Done

TestCase2267:
   ldc.i4 2367
   stloc.0
   ldc.i4 0x5afd
   conv.i4
   not
   ldc.i4 0xffffa502
   beq TestCase2266
   br Done

TestCase2266:
   ldc.i4 2366
   stloc.0
   ldc.i4 0xcc
   conv.i4
   not
   ldc.i4 0xffffff33
   beq TestCase2265
   br Done

TestCase2265:
   ldc.i4 2365
   stloc.0
   ldc.i4 0xffff9b2f
   conv.i4
   not
   ldc.i4 0x64d0
   beq TestCase2264
   br Done

TestCase2264:
   ldc.i4 2364
   stloc.0
   ldc.i4 0xa13
   conv.i4
   not
   ldc.i4 0xfffff5ec
   beq TestCase2263
   br Done

TestCase2263:
   ldc.i4 2363
   stloc.0
   ldc.i4 0x8ed
   conv.i4
   not
   ldc.i4 0xfffff712
   beq TestCase2262
   br Done

TestCase2262:
   ldc.i4 2362
   stloc.0
   ldc.i4 0xffffa330
   conv.i4
   not
   ldc.i4 0x5ccf
   beq TestCase2261
   br Done

TestCase2261:
   ldc.i4 2361
   stloc.0
   ldc.i4 0x4677
   conv.i4
   not
   ldc.i4 0xffffb988
   beq TestCase2260
   br Done

TestCase2260:
   ldc.i4 2360
   stloc.0
   ldc.i4 0xffffcc32
   conv.i4
   not
   ldc.i4 0x33cd
   bne.un Done

TestCase2259:
   ldc.i4 2359
   stloc.0
   ldc.i4 0xffff8354
   conv.i4
   not
   ldc.i4 0x7cab
   bne.un Done

TestCase2258:
   ldc.i4 2358
   stloc.0
   ldc.i4 0xffffb8ee
   conv.i4
   not
   ldc.i4 0x4711
   bne.un Done

TestCase2257:
   ldc.i4 2357
   stloc.0
   ldc.i4 0x3dea
   conv.i4
   not
   ldc.i4 0xffffc215
   bne.un Done

TestCase2256:
   ldc.i4 2356
   stloc.0
   ldc.i4 0xffffd99e
   conv.i4
   not
   ldc.i4 0x2661
   bne.un Done

TestCase2255:
   ldc.i4 2355
   stloc.0
   ldc.i4 0x4261
   conv.i4
   not
   ldc.i4 0xffffbd9e
   bne.un Done

TestCase2254:
   ldc.i4 2354
   stloc.0
   ldc.i4 0x48f0
   conv.i4
   not
   ldc.i4 0xffffb70f
   beq TestCase2253
   br Done

TestCase2253:
   ldc.i4 2353
   stloc.0
   ldc.i4 0xffff84f8
   conv.i4
   not
   ldc.i4 0x7b07
   beq TestCase2252
   br Done

TestCase2252:
   ldc.i4 2352
   stloc.0
   ldc.i4 0xffff9dcd
   conv.i4
   not
   ldc.i4 0x6232
   beq TestCase2251
   br Done

TestCase2251:
   ldc.i4 2351
   stloc.0
   ldc.i4 0xffffca57
   conv.i4
   not
   ldc.i4 0x35a8
   beq TestCase2250
   br Done

TestCase2250:
   ldc.i4 2350
   stloc.0
   ldc.i4 0xffffcaea
   conv.i4
   not
   ldc.i4 0x3515
   bne.un Done

TestCase2249:
   ldc.i4 2349
   stloc.0
   ldc.i4 0x691d
   conv.i4
   not
   ldc.i4 0xffff96e2
   beq TestCase2248
   br Done

TestCase2248:
   ldc.i4 2348
   stloc.0
   ldc.i4 0xffffe9fe
   conv.i4
   not
   ldc.i4 0x1601
   bne.un Done

TestCase2247:
   ldc.i4 2347
   stloc.0
   ldc.i4 0x2d16
   conv.i4
   not
   ldc.i4 0xffffd2e9
   beq TestCase2246
   br Done

TestCase2246:
   ldc.i4 2346
   stloc.0
   ldc.i4 0x616f
   conv.i4
   not
   ldc.i4 0xffff9e90
   bne.un Done

TestCase2245:
   ldc.i4 2345
   stloc.0
   ldc.i4 0x2300
   conv.i4
   not
   ldc.i4 0xffffdcff
   beq TestCase2244
   br Done

TestCase2244:
   ldc.i4 2344
   stloc.0
   ldc.i4 0xffffd002
   conv.i4
   not
   ldc.i4 0x2ffd
   beq TestCase2243
   br Done

TestCase2243:
   ldc.i4 2343
   stloc.0
   ldc.i4 0xffffc216
   conv.i4
   not
   ldc.i4 0x3de9
   beq TestCase2242
   br Done

TestCase2242:
   ldc.i4 2342
   stloc.0
   ldc.i4 0x524e
   conv.i4
   not
   ldc.i4 0xffffadb1
   beq TestCase2241
   br Done

TestCase2241:
   ldc.i4 2341
   stloc.0
   ldc.i4 0x756e
   conv.i4
   not
   ldc.i4 0xffff8a91
   beq TestCase2240
   br Done

TestCase2240:
   ldc.i4 2340
   stloc.0
   ldc.i4 0x6e0a
   conv.i4
   not
   ldc.i4 0xffff91f5
   beq TestCase2239
   br Done

TestCase2239:
   ldc.i4 2339
   stloc.0
   ldc.i4 0xffffebec
   conv.i4
   not
   ldc.i4 0x1413
   beq TestCase2238
   br Done

TestCase2238:
   ldc.i4 2338
   stloc.0
   ldc.i4 0x42dd
   conv.i4
   not
   ldc.i4 0xffffbd22
   bne.un Done

TestCase2237:
   ldc.i4 2337
   stloc.0
   ldc.i4 0xfffff45a
   conv.i4
   not
   ldc.i4 0xba5
   beq TestCase2236
   br Done

TestCase2236:
   ldc.i4 2336
   stloc.0
   ldc.i4 0x7950
   conv.i4
   not
   ldc.i4 0xffff86af
   beq TestCase2235
   br Done

TestCase2235:
   ldc.i4 2335
   stloc.0
   ldc.i4 0x1a6a
   conv.i4
   not
   ldc.i4 0xffffe595
   beq TestCase2234
   br Done

TestCase2234:
   ldc.i4 2334
   stloc.0
   ldc.i4 0x36c5
   conv.i4
   not
   ldc.i4 0xffffc93a
   beq TestCase2233
   br Done

TestCase2233:
   ldc.i4 2333
   stloc.0
   ldc.i4 0xffffddbb
   conv.i4
   not
   ldc.i4 0x2244
   beq TestCase2232
   br Done

TestCase2232:
   ldc.i4 2332
   stloc.0
   ldc.i4 0x5905
   conv.i4
   not
   ldc.i4 0xffffa6fa
   beq TestCase2231
   br Done

TestCase2231:
   ldc.i4 2331
   stloc.0
   ldc.i4 0xffff90d8
   conv.i4
   not
   ldc.i4 0x6f27
   bne.un Done

TestCase2230:
   ldc.i4 2330
   stloc.0
   ldc.i4 0x11e
   conv.i4
   not
   ldc.i4 0xfffffee1
   beq TestCase2229
   br Done

TestCase2229:
   ldc.i4 2329
   stloc.0
   ldc.i4 0xffffa39d
   conv.i4
   not
   ldc.i4 0x5c62
   beq TestCase2228
   br Done

TestCase2228:
   ldc.i4 2328
   stloc.0
   ldc.i4 0xffffb782
   conv.i4
   not
   ldc.i4 0x487d
   bne.un Done

TestCase2227:
   ldc.i4 2327
   stloc.0
   ldc.i4 0x60a9
   conv.i4
   not
   ldc.i4 0xffff9f56
   beq TestCase2226
   br Done

TestCase2226:
   ldc.i4 2326
   stloc.0
   ldc.i4 0xffff9368
   conv.i4
   not
   ldc.i4 0x6c97
   beq TestCase2225
   br Done

TestCase2225:
   ldc.i4 2325
   stloc.0
   ldc.i4 0xffffe913
   conv.i4
   not
   ldc.i4 0x16ec
   beq TestCase2224
   br Done

TestCase2224:
   ldc.i4 2324
   stloc.0
   ldc.i4 0xffffc38d
   conv.i4
   not
   ldc.i4 0x3c72
   beq TestCase2223
   br Done

TestCase2223:
   ldc.i4 2323
   stloc.0
   ldc.i4 0xffff919c
   conv.i4
   not
   ldc.i4 0x6e63
   bne.un Done

TestCase2222:
   ldc.i4 2322
   stloc.0
   ldc.i4 0xffff8f16
   conv.i4
   not
   ldc.i4 0x70e9
   beq TestCase2221
   br Done

TestCase2221:
   ldc.i4 2321
   stloc.0
   ldc.i4 0x5645
   conv.i4
   not
   ldc.i4 0xffffa9ba
   bne.un Done

TestCase2220:
   ldc.i4 2320
   stloc.0
   ldc.i4 0xffffdc6e
   conv.i4
   not
   ldc.i4 0x2391
   bne.un Done

TestCase2219:
   ldc.i4 2319
   stloc.0
   ldc.i4 0xa29
   conv.i4
   not
   ldc.i4 0xfffff5d6
   beq TestCase2218
   br Done

TestCase2218:
   ldc.i4 2318
   stloc.0
   ldc.i4 0xffffde86
   conv.i4
   not
   ldc.i4 0x2179
   beq TestCase2217
   br Done

TestCase2217:
   ldc.i4 2317
   stloc.0
   ldc.i4 0xffffd5e9
   conv.i4
   not
   ldc.i4 0x2a16
   bne.un Done

TestCase2216:
   ldc.i4 2316
   stloc.0
   ldc.i4 0xffffd1ba
   conv.i4
   not
   ldc.i4 0x2e45
   beq TestCase2215
   br Done

TestCase2215:
   ldc.i4 2315
   stloc.0
   ldc.i4 0x159b
   conv.i4
   not
   ldc.i4 0xffffea64
   bne.un Done

TestCase2214:
   ldc.i4 2314
   stloc.0
   ldc.i4 0x414e
   conv.i4
   not
   ldc.i4 0xffffbeb1
   bne.un Done

TestCase2213:
   ldc.i4 2313
   stloc.0
   ldc.i4 0xffffdc83
   conv.i4
   not
   ldc.i4 0x237c
   beq TestCase2212
   br Done

TestCase2212:
   ldc.i4 2312
   stloc.0
   ldc.i4 0xffffd637
   conv.i4
   not
   ldc.i4 0x29c8
   bne.un Done

TestCase2211:
   ldc.i4 2311
   stloc.0
   ldc.i4 0x57f0
   conv.i4
   not
   ldc.i4 0xffffa80f
   beq TestCase2210
   br Done

TestCase2210:
   ldc.i4 2310
   stloc.0
   ldc.i4 0xffffd7ce
   conv.i4
   not
   ldc.i4 0x2831
   bne.un Done

TestCase2209:
   ldc.i4 2309
   stloc.0
   ldc.i4 0x3956
   conv.i4
   not
   ldc.i4 0xffffc6a9
   bne.un Done

TestCase2208:
   ldc.i4 2308
   stloc.0
   ldc.i4 0x3a67
   conv.i4
   not
   ldc.i4 0xffffc598
   beq TestCase2207
   br Done

TestCase2207:
   ldc.i4 2307
   stloc.0
   ldc.i4 0xffff9818
   conv.i4
   not
   ldc.i4 0x67e7
   bne.un Done

TestCase2206:
   ldc.i4 2306
   stloc.0
   ldc.i4 0xffff9ad5
   conv.i4
   not
   ldc.i4 0x652a
   beq TestCase2205
   br Done

TestCase2205:
   ldc.i4 2305
   stloc.0
   ldc.i4 0x36e2
   conv.i4
   not
   ldc.i4 0xffffc91d
   bne.un Done

TestCase2204:
   ldc.i4 2304
   stloc.0
   ldc.i4 0xfffffeec
   conv.i4
   not
   ldc.i4 0x113
   beq TestCase2203
   br Done

TestCase2203:
   ldc.i4 2303
   stloc.0
   ldc.i4 0x460b
   conv.i4
   not
   ldc.i4 0xffffb9f4
   beq TestCase2202
   br Done

TestCase2202:
   ldc.i4 2302
   stloc.0
   ldc.i4 0x4a1a
   conv.i4
   not
   ldc.i4 0xffffb5e5
   bne.un Done

TestCase2201:
   ldc.i4 2301
   stloc.0
   ldc.i4 0x10a2
   conv.i4
   not
   ldc.i4 0xffffef5d
   beq TestCase2200
   br Done

TestCase2200:
   ldc.i4 2300
   stloc.0
   ldc.i4 0xffff87cf
   conv.i4
   not
   ldc.i4 0x7830
   bne.un Done

TestCase2199:
   ldc.i4 2299
   stloc.0
   ldc.i4 0x2326
   conv.i4
   not
   ldc.i4 0xffffdcd9
   bne.un Done

TestCase2198:
   ldc.i4 2298
   stloc.0
   ldc.i4 0xffffb34c
   conv.i4
   not
   ldc.i4 0x4cb3
   beq TestCase2197
   br Done

TestCase2197:
   ldc.i4 2297
   stloc.0
   ldc.i4 0xffffc492
   conv.i4
   not
   ldc.i4 0x3b6d
   bne.un Done

TestCase2196:
   ldc.i4 2296
   stloc.0
   ldc.i4 0xffffec66
   conv.i4
   not
   ldc.i4 0x1399
   bne.un Done

TestCase2195:
   ldc.i4 2295
   stloc.0
   ldc.i4 0xf07
   conv.i4
   not
   ldc.i4 0xfffff0f8
   bne.un Done

TestCase2194:
   ldc.i4 2294
   stloc.0
   ldc.i4 0xffff975f
   conv.i4
   not
   ldc.i4 0x68a0
   beq TestCase2193
   br Done

TestCase2193:
   ldc.i4 2293
   stloc.0
   ldc.i4 0xffff9e29
   conv.i4
   not
   ldc.i4 0x61d6
   beq TestCase2192
   br Done

TestCase2192:
   ldc.i4 2292
   stloc.0
   ldc.i4 0x6ff2
   conv.i4
   not
   ldc.i4 0xffff900d
   bne.un Done

TestCase2191:
   ldc.i4 2291
   stloc.0
   ldc.i4 0xb77
   conv.i4
   not
   ldc.i4 0xfffff488
   bne.un Done

TestCase2190:
   ldc.i4 2290
   stloc.0
   ldc.i4 0xffff910e
   conv.i4
   not
   ldc.i4 0x6ef1
   bne.un Done

TestCase2189:
   ldc.i4 2289
   stloc.0
   ldc.i4 0xffffec41
   conv.i4
   not
   ldc.i4 0x13be
   bne.un Done

TestCase2188:
   ldc.i4 2288
   stloc.0
   ldc.i4 0x1d32
   conv.i4
   not
   ldc.i4 0xffffe2cd
   beq TestCase2187
   br Done

TestCase2187:
   ldc.i4 2287
   stloc.0
   ldc.i4 0x3f77
   conv.i4
   not
   ldc.i4 0xffffc088
   bne.un Done

TestCase2186:
   ldc.i4 2286
   stloc.0
   ldc.i4 0xffff9138
   conv.i4
   not
   ldc.i4 0x6ec7
   bne.un Done

TestCase2185:
   ldc.i4 2285
   stloc.0
   ldc.i4 0x4c35
   conv.i4
   not
   ldc.i4 0xffffb3ca
   bne.un Done

TestCase2184:
   ldc.i4 2284
   stloc.0
   ldc.i4 0x506f
   conv.i4
   not
   ldc.i4 0xffffaf90
   beq TestCase2183
   br Done

TestCase2183:
   ldc.i4 2283
   stloc.0
   ldc.i4 0x46bf
   conv.i4
   not
   ldc.i4 0xffffb940
   bne.un Done

TestCase2182:
   ldc.i4 2282
   stloc.0
   ldc.i4 0xffffdcbf
   conv.i4
   not
   ldc.i4 0x2340
   bne.un Done

TestCase2181:
   ldc.i4 2281
   stloc.0
   ldc.i4 0xffff966b
   conv.i4
   not
   ldc.i4 0x6994
   bne.un Done

TestCase2180:
   ldc.i4 2280
   stloc.0
   ldc.i4 0x8eb
   conv.i4
   not
   ldc.i4 0xfffff714
   bne.un Done

TestCase2179:
   ldc.i4 2279
   stloc.0
   ldc.i4 0x5974
   conv.i4
   not
   ldc.i4 0xffffa68b
   bne.un Done

TestCase2178:
   ldc.i4 2278
   stloc.0
   ldc.i4 0xffffed5a
   conv.i4
   not
   ldc.i4 0x12a5
   bne.un Done

TestCase2177:
   ldc.i4 2277
   stloc.0
   ldc.i4 0x73e0
   conv.i4
   not
   ldc.i4 0xffff8c1f
   beq TestCase2176
   br Done

TestCase2176:
   ldc.i4 2276
   stloc.0
   ldc.i4 0xffff8ba1
   conv.i4
   not
   ldc.i4 0x745e
   beq TestCase2175
   br Done

TestCase2175:
   ldc.i4 2275
   stloc.0
   ldc.i4 0xffffb7d8
   conv.i4
   not
   ldc.i4 0x4827
   bne.un Done

TestCase2174:
   ldc.i4 2274
   stloc.0
   ldc.i4 0x4ad3
   conv.i4
   not
   ldc.i4 0xffffb52c
   bne.un Done

TestCase2173:
   ldc.i4 2273
   stloc.0
   ldc.i4 0xffffb2a8
   conv.i4
   not
   ldc.i4 0x4d57
   beq TestCase2172
   br Done

TestCase2172:
   ldc.i4 2272
   stloc.0
   ldc.i4 0x1e87
   conv.i4
   not
   ldc.i4 0xffffe178
   beq TestCase2171
   br Done

TestCase2171:
   ldc.i4 2271
   stloc.0
   ldc.i4 0x816
   conv.i4
   not
   ldc.i4 0xfffff7e9
   bne.un Done

TestCase2170:
   ldc.i4 2270
   stloc.0
   ldc.i4 0xffff991a
   conv.i4
   not
   ldc.i4 0x66e5
   beq TestCase2169
   br Done

TestCase2169:
   ldc.i4 2269
   stloc.0
   ldc.i4 0xffffd6a0
   conv.i4
   not
   ldc.i4 0x295f
   bne.un Done

TestCase2168:
   ldc.i4 2268
   stloc.0
   ldc.i4 0x37ed
   conv.i4
   not
   ldc.i4 0xffffc812
   beq TestCase2167
   br Done

TestCase2167:
   ldc.i4 2267
   stloc.0
   ldc.i4 0x4b9e
   conv.i4
   not
   ldc.i4 0xffffb461
   bne.un Done

TestCase2166:
   ldc.i4 2266
   stloc.0
   ldc.i4 0x4a2a
   conv.i4
   not
   ldc.i4 0xffffb5d5
   beq TestCase2165
   br Done

TestCase2165:
   ldc.i4 2265
   stloc.0
   ldc.i4 0xffff9a6f
   conv.i4
   not
   ldc.i4 0x6590
   beq TestCase2164
   br Done

TestCase2164:
   ldc.i4 2264
   stloc.0
   ldc.i4 0xffff8007
   conv.i4
   not
   ldc.i4 0x7ff8
   bne.un Done

TestCase2163:
   ldc.i4 2263
   stloc.0
   ldc.i4 0xffff9d02
   conv.i4
   not
   ldc.i4 0x62fd
   bne.un Done

TestCase2162:
   ldc.i4 2262
   stloc.0
   ldc.i4 0xffffc3bb
   conv.i4
   not
   ldc.i4 0x3c44
   bne.un Done

TestCase2161:
   ldc.i4 2261
   stloc.0
   ldc.i4 0x2120
   conv.i4
   not
   ldc.i4 0xffffdedf
   beq TestCase2160
   br Done

TestCase2160:
   ldc.i4 2260
   stloc.0
   ldc.i4 0x1e39
   conv.i4
   not
   ldc.i4 0xffffe1c6
   bne.un Done

TestCase2159:
   ldc.i4 2259
   stloc.0
   ldc.i4 0xffff9eae
   conv.i4
   not
   ldc.i4 0x6151
   beq TestCase2158
   br Done

TestCase2158:
   ldc.i4 2258
   stloc.0
   ldc.i4 0x52a0
   conv.i4
   not
   ldc.i4 0xffffad5f
   bne.un Done

TestCase2157:
   ldc.i4 2257
   stloc.0
   ldc.i4 0xffffd192
   conv.i4
   not
   ldc.i4 0x2e6d
   beq TestCase2156
   br Done

TestCase2156:
   ldc.i4 2256
   stloc.0
   ldc.i4 0xffff8914
   conv.i4
   not
   ldc.i4 0x76eb
   beq TestCase2155
   br Done

TestCase2155:
   ldc.i4 2255
   stloc.0
   ldc.i4 0x41cd
   conv.i4
   not
   ldc.i4 0xffffbe32
   bne.un Done

TestCase2154:
   ldc.i4 2254
   stloc.0
   ldc.i4 0x5317
   conv.i4
   not
   ldc.i4 0xfffface8
   beq TestCase2153
   br Done

TestCase2153:
   ldc.i4 2253
   stloc.0
   ldc.i4 0x3af0
   conv.i4
   not
   ldc.i4 0xffffc50f
   beq TestCase2152
   br Done

TestCase2152:
   ldc.i4 2252
   stloc.0
   ldc.i4 0x46d5
   conv.i4
   not
   ldc.i4 0xffffb92a
   bne.un Done

TestCase2151:
   ldc.i4 2251
   stloc.0
   ldc.i4 0xfffff6c7
   conv.i4
   not
   ldc.i4 0x938
   bne.un Done

TestCase2150:
   ldc.i4 2250
   stloc.0
   ldc.i4 0x7be1
   conv.i4
   not
   ldc.i4 0xffff841e
   beq TestCase2149
   br Done

TestCase2149:
   ldc.i4 2249
   stloc.0
   ldc.i4 0x4941
   conv.i4
   not
   ldc.i4 0xffffb6be
   bne.un Done

TestCase2148:
   ldc.i4 2248
   stloc.0
   ldc.i4 0x123e
   conv.i4
   not
   ldc.i4 0xffffedc1
   bne.un Done

TestCase2147:
   ldc.i4 2247
   stloc.0
   ldc.i4 0x1fac
   conv.i4
   not
   ldc.i4 0xffffe053
   beq TestCase2146
   br Done

TestCase2146:
   ldc.i4 2246
   stloc.0
   ldc.i4 0xffffa33f
   conv.i4
   not
   ldc.i4 0x5cc0
   bne.un Done

TestCase2145:
   ldc.i4 2245
   stloc.0
   ldc.i4 0xffffa67b
   conv.i4
   not
   ldc.i4 0x5984
   bne.un Done

TestCase2144:
   ldc.i4 2244
   stloc.0
   ldc.i4 0xffffccb6
   conv.i4
   not
   ldc.i4 0x3349
   beq TestCase2143
   br Done

TestCase2143:
   ldc.i4 2243
   stloc.0
   ldc.i4 0xffffa174
   conv.i4
   not
   ldc.i4 0x5e8b
   bne.un Done

TestCase2142:
   ldc.i4 2242
   stloc.0
   ldc.i4 0x2d6d
   conv.i4
   not
   ldc.i4 0xffffd292
   bne.un Done

TestCase2141:
   ldc.i4 2241
   stloc.0
   ldc.i4 0x5c00
   conv.i4
   not
   ldc.i4 0xffffa3ff
   bne.un Done

TestCase2140:
   ldc.i4 2240
   stloc.0
   ldc.i4 0xffffec81
   conv.i4
   not
   ldc.i4 0x137e
   bne.un Done

TestCase2139:
   ldc.i4 2239
   stloc.0
   ldc.i4 0xec5
   conv.i4
   not
   ldc.i4 0xfffff13a
   bne.un Done

TestCase2138:
   ldc.i4 2238
   stloc.0
   ldc.i4 0xffffb2c2
   conv.i4
   not
   ldc.i4 0x4d3d
   bne.un Done

TestCase2137:
   ldc.i4 2237
   stloc.0
   ldc.i4 0xffffb60a
   conv.i4
   not
   ldc.i4 0x49f5
   bne.un Done

TestCase2136:
   ldc.i4 2236
   stloc.0
   ldc.i4 0x5c92
   conv.i4
   not
   ldc.i4 0xffffa36d
   bne.un Done

TestCase2135:
   ldc.i4 2235
   stloc.0
   ldc.i4 0xffffacb9
   conv.i4
   not
   ldc.i4 0x5346
   beq TestCase2134
   br Done

TestCase2134:
   ldc.i4 2234
   stloc.0
   ldc.i4 0x5819
   conv.i4
   not
   ldc.i4 0xffffa7e6
   bne.un Done

TestCase2133:
   ldc.i4 2233
   stloc.0
   ldc.i4 0xffffff3c
   conv.i4
   not
   ldc.i4 0xc3
   bne.un Done

TestCase2132:
   ldc.i4 2232
   stloc.0
   ldc.i4 0xffffd38b
   conv.i4
   not
   ldc.i4 0x2c74
   beq TestCase2131
   br Done

TestCase2131:
   ldc.i4 2231
   stloc.0
   ldc.i4 0xffffc43a
   conv.i4
   not
   ldc.i4 0x3bc5
   beq TestCase2130
   br Done

TestCase2130:
   ldc.i4 2230
   stloc.0
   ldc.i4 0xffff8364
   conv.i4
   not
   ldc.i4 0x7c9b
   bne.un Done

TestCase2129:
   ldc.i4 2229
   stloc.0
   ldc.i4 0x3b3f
   conv.i4
   not
   ldc.i4 0xffffc4c0
   bne.un Done

TestCase2128:
   ldc.i4 2228
   stloc.0
   ldc.i4 0xffffe762
   conv.i4
   not
   ldc.i4 0x189d
   beq TestCase2127
   br Done

TestCase2127:
   ldc.i4 2227
   stloc.0
   ldc.i4 0xffff8453
   conv.i4
   not
   ldc.i4 0x7bac
   beq TestCase2126
   br Done

TestCase2126:
   ldc.i4 2226
   stloc.0
   ldc.i4 0x7f8d
   conv.i4
   not
   ldc.i4 0xffff8072
   beq TestCase2125
   br Done

TestCase2125:
   ldc.i4 2225
   stloc.0
   ldc.i4 0x454c
   conv.i4
   not
   ldc.i4 0xffffbab3
   bne.un Done

TestCase2124:
   ldc.i4 2224
   stloc.0
   ldc.i4 0xfffff367
   conv.i4
   not
   ldc.i4 0xc98
   bne.un Done

TestCase2123:
   ldc.i4 2223
   stloc.0
   ldc.i4 0xfffffe9e
   conv.i4
   not
   ldc.i4 0x161
   bne.un Done

TestCase2122:
   ldc.i4 2222
   stloc.0
   ldc.i4 0x187f
   conv.i4
   not
   ldc.i4 0xffffe780
   beq TestCase2121
   br Done

TestCase2121:
   ldc.i4 2221
   stloc.0
   ldc.i4 0xffffba40
   conv.i4
   not
   ldc.i4 0x45bf
   bne.un Done

TestCase2120:
   ldc.i4 2220
   stloc.0
   ldc.i4 0x4fd0
   conv.i4
   not
   ldc.i4 0xffffb02f
   beq TestCase2119
   br Done

TestCase2119:
   ldc.i4 2219
   stloc.0
   ldc.i4 0x69c1
   conv.i4
   not
   ldc.i4 0xffff963e
   bne.un Done

TestCase2118:
   ldc.i4 2218
   stloc.0
   ldc.i4 0xffffec3b
   conv.i4
   not
   ldc.i4 0x13c4
   bne.un Done

TestCase2117:
   ldc.i4 2217
   stloc.0
   ldc.i4 0x3a79
   conv.i4
   not
   ldc.i4 0xffffc586
   beq TestCase2116
   br Done

TestCase2116:
   ldc.i4 2216
   stloc.0
   ldc.i4 0xffffc168
   conv.i4
   not
   ldc.i4 0x3e97
   bne.un Done

TestCase2115:
   ldc.i4 2215
   stloc.0
   ldc.i4 0xfffff42e
   conv.i4
   not
   ldc.i4 0xbd1
   beq TestCase2114
   br Done

TestCase2114:
   ldc.i4 2214
   stloc.0
   ldc.i4 0x659b
   conv.i4
   not
   ldc.i4 0xffff9a64
   beq TestCase2113
   br Done

TestCase2113:
   ldc.i4 2213
   stloc.0
   ldc.i4 0x2bc
   conv.i4
   not
   ldc.i4 0xfffffd43
   bne.un Done

TestCase2112:
   ldc.i4 2212
   stloc.0
   ldc.i4 0xfffffd81
   conv.i4
   not
   ldc.i4 0x27e
   bne.un Done

TestCase2111:
   ldc.i4 2211
   stloc.0
   ldc.i4 0xffffe734
   conv.i4
   not
   ldc.i4 0x18cb
   beq TestCase2110
   br Done

TestCase2110:
   ldc.i4 2210
   stloc.0
   ldc.i4 0x1a92
   conv.i4
   not
   ldc.i4 0xffffe56d
   bne.un Done

TestCase2109:
   ldc.i4 2209
   stloc.0
   ldc.i4 0x13d1
   conv.i4
   not
   ldc.i4 0xffffec2e
   bne.un Done

TestCase2108:
   ldc.i4 2208
   stloc.0
   ldc.i4 0x55d5
   conv.i4
   not
   ldc.i4 0xffffaa2a
   bne.un Done

TestCase2107:
   ldc.i4 2207
   stloc.0
   ldc.i4 0xc4e
   conv.i4
   not
   ldc.i4 0xfffff3b1
   bne.un Done

TestCase2106:
   ldc.i4 2206
   stloc.0
   ldc.i4 0xffffaec4
   conv.i4
   not
   ldc.i4 0x513b
   beq TestCase2105
   br Done

TestCase2105:
   ldc.i4 2205
   stloc.0
   ldc.i4 0xffffcc24
   conv.i4
   not
   ldc.i4 0x33db
   bne.un Done

TestCase2104:
   ldc.i4 2204
   stloc.0
   ldc.i4 0x1e78
   conv.i4
   not
   ldc.i4 0xffffe187
   bne.un Done

TestCase2103:
   ldc.i4 2203
   stloc.0
   ldc.i4 0xffff9725
   conv.i4
   not
   ldc.i4 0x68da
   bne.un Done

TestCase2102:
   ldc.i4 2202
   stloc.0
   ldc.i4 0xffffd6e8
   conv.i4
   not
   ldc.i4 0x2917
   bne.un Done

TestCase2101:
   ldc.i4 2201
   stloc.0
   ldc.i4 0xffffa902
   conv.i4
   not
   ldc.i4 0x56fd
   beq TestCase2100
   br Done

TestCase2100:
   ldc.i4 2200
   stloc.0
   ldc.i4 0xffff941c
   conv.i4
   not
   ldc.i4 0x6be3
   beq TestCase2099
   br Done

TestCase2099:
   ldc.i4 2199
   stloc.0
   ldc.i4 0x7c97
   conv.i4
   not
   ldc.i4 0xffff8368
   bne.un Done

TestCase2098:
   ldc.i4 2198
   stloc.0
   ldc.i4 0xffffad41
   conv.i4
   not
   ldc.i4 0x52be
   bne.un Done

TestCase2097:
   ldc.i4 2197
   stloc.0
   ldc.i4 0xffff8554
   conv.i4
   not
   ldc.i4 0x7aab
   beq TestCase2096
   br Done

TestCase2096:
   ldc.i4 2196
   stloc.0
   ldc.i4 0xffffdb86
   conv.i4
   not
   ldc.i4 0x2479
   bne.un Done

TestCase2095:
   ldc.i4 2195
   stloc.0
   ldc.i4 0x2a40
   conv.i4
   not
   ldc.i4 0xffffd5bf
   bne.un Done

TestCase2094:
   ldc.i4 2194
   stloc.0
   ldc.i4 0x2d4f
   conv.i4
   not
   ldc.i4 0xffffd2b0
   beq TestCase2093
   br Done

TestCase2093:
   ldc.i4 2193
   stloc.0
   ldc.i4 0xffffe4b1
   conv.i4
   not
   ldc.i4 0x1b4e
   beq TestCase2092
   br Done

TestCase2092:
   ldc.i4 2192
   stloc.0
   ldc.i4 0xffff91f1
   conv.i4
   not
   ldc.i4 0x6e0e
   beq TestCase2091
   br Done

TestCase2091:
   ldc.i4 2191
   stloc.0
   ldc.i4 0xffff8d92
   conv.i4
   not
   ldc.i4 0x726d
   bne.un Done

TestCase2090:
   ldc.i4 2190
   stloc.0
   ldc.i4 0xffffe2e8
   conv.i4
   not
   ldc.i4 0x1d17
   bne.un Done

TestCase2089:
   ldc.i4 2189
   stloc.0
   ldc.i4 0x201b
   conv.i4
   not
   ldc.i4 0xffffdfe4
   bne.un Done

TestCase2088:
   ldc.i4 2188
   stloc.0
   ldc.i4 0x1db7
   conv.i4
   not
   ldc.i4 0xffffe248
   beq TestCase2087
   br Done

TestCase2087:
   ldc.i4 2187
   stloc.0
   ldc.i4 0x390e
   conv.i4
   not
   ldc.i4 0xffffc6f1
   beq TestCase2086
   br Done

TestCase2086:
   ldc.i4 2186
   stloc.0
   ldc.i4 0xfffff87b
   conv.i4
   not
   ldc.i4 0x784
   bne.un Done

TestCase2085:
   ldc.i4 2185
   stloc.0
   ldc.i4 0xffffa6cc
   conv.i4
   not
   ldc.i4 0x5933
   bne.un Done

TestCase2084:
   ldc.i4 2184
   stloc.0
   ldc.i4 0xffffc52e
   conv.i4
   not
   ldc.i4 0x3ad1
   beq TestCase2083
   br Done

TestCase2083:
   ldc.i4 2183
   stloc.0
   ldc.i4 0xfffffce4
   conv.i4
   not
   ldc.i4 0x31b
   beq TestCase2082
   br Done

TestCase2082:
   ldc.i4 2182
   stloc.0
   ldc.i4 0xffffdfb4
   conv.i4
   not
   ldc.i4 0x204b
   beq TestCase2081
   br Done

TestCase2081:
   ldc.i4 2181
   stloc.0
   ldc.i4 0xffff9402
   conv.i4
   not
   ldc.i4 0x6bfd
   beq TestCase2080
   br Done

TestCase2080:
   ldc.i4 2180
   stloc.0
   ldc.i4 0x6158
   conv.i4
   not
   ldc.i4 0xffff9ea7
   beq TestCase2079
   br Done

TestCase2079:
   ldc.i4 2179
   stloc.0
   ldc.i4 0xffffd760
   conv.i4
   not
   ldc.i4 0x289f
   bne.un Done

TestCase2078:
   ldc.i4 2178
   stloc.0
   ldc.i4 0x73d9
   conv.i4
   not
   ldc.i4 0xffff8c26
   beq TestCase2077
   br Done

TestCase2077:
   ldc.i4 2177
   stloc.0
   ldc.i4 0xffffb63c
   conv.i4
   not
   ldc.i4 0x49c3
   beq TestCase2076
   br Done

TestCase2076:
   ldc.i4 2176
   stloc.0
   ldc.i4 0xf61
   conv.i4
   not
   ldc.i4 0xfffff09e
   beq TestCase2075
   br Done

TestCase2075:
   ldc.i4 2175
   stloc.0
   ldc.i4 0x36eb
   conv.i4
   not
   ldc.i4 0xffffc914
   bne.un Done

TestCase2074:
   ldc.i4 2174
   stloc.0
   ldc.i4 0xffffa509
   conv.i4
   not
   ldc.i4 0x5af6
   beq TestCase2073
   br Done

TestCase2073:
   ldc.i4 2173
   stloc.0
   ldc.i4 0xc8c
   conv.i4
   not
   ldc.i4 0xfffff373
   bne.un Done

TestCase2072:
   ldc.i4 2172
   stloc.0
   ldc.i4 0x29f7
   conv.i4
   not
   ldc.i4 0xffffd608
   beq TestCase2071
   br Done

TestCase2071:
   ldc.i4 2171
   stloc.0
   ldc.i4 0x7f11
   conv.i4
   not
   ldc.i4 0xffff80ee
   bne.un Done

TestCase2070:
   ldc.i4 2170
   stloc.0
   ldc.i4 0x43bf
   conv.i4
   not
   ldc.i4 0xffffbc40
   beq TestCase2069
   br Done

TestCase2069:
   ldc.i4 2169
   stloc.0
   ldc.i4 0x5638
   conv.i4
   not
   ldc.i4 0xffffa9c7
   beq TestCase2068
   br Done

TestCase2068:
   ldc.i4 2168
   stloc.0
   ldc.i4 0x451f
   conv.i4
   not
   ldc.i4 0xffffbae0
   bne.un Done

TestCase2067:
   ldc.i4 2167
   stloc.0
   ldc.i4 0xca9
   conv.i4
   not
   ldc.i4 0xfffff356
   beq TestCase2066
   br Done

TestCase2066:
   ldc.i4 2166
   stloc.0
   ldc.i4 0xffff843b
   conv.i4
   not
   ldc.i4 0x7bc4
   beq TestCase2065
   br Done

TestCase2065:
   ldc.i4 2165
   stloc.0
   ldc.i4 0xfffff220
   conv.i4
   not
   ldc.i4 0xddf
   beq TestCase2064
   br Done

TestCase2064:
   ldc.i4 2164
   stloc.0
   ldc.i4 0x7b24
   conv.i4
   not
   ldc.i4 0xffff84db
   beq TestCase2063
   br Done

TestCase2063:
   ldc.i4 2163
   stloc.0
   ldc.i4 0xffffff3d
   conv.i4
   not
   ldc.i4 0xc2
   bne.un Done

TestCase2062:
   ldc.i4 2162
   stloc.0
   ldc.i4 0x6781
   conv.i4
   not
   ldc.i4 0xffff987e
   beq TestCase2061
   br Done

TestCase2061:
   ldc.i4 2161
   stloc.0
   ldc.i4 0x176f
   conv.i4
   not
   ldc.i4 0xffffe890
   bne.un Done

TestCase2060:
   ldc.i4 2160
   stloc.0
   ldc.i4 0x5f26
   conv.i4
   not
   ldc.i4 0xffffa0d9
   beq TestCase2059
   br Done

TestCase2059:
   ldc.i4 2159
   stloc.0
   ldc.i4 0xd2a
   conv.i4
   not
   ldc.i4 0xfffff2d5
   beq TestCase2058
   br Done

TestCase2058:
   ldc.i4 2158
   stloc.0
   ldc.i4 0xffffad05
   conv.i4
   not
   ldc.i4 0x52fa
   bne.un Done

TestCase2057:
   ldc.i4 2157
   stloc.0
   ldc.i4 0x617c
   conv.i4
   not
   ldc.i4 0xffff9e83
   bne.un Done

TestCase2056:
   ldc.i4 2156
   stloc.0
   ldc.i4 0xffffa91d
   conv.i4
   not
   ldc.i4 0x56e2
   beq TestCase2055
   br Done

TestCase2055:
   ldc.i4 2155
   stloc.0
   ldc.i4 0xbda
   conv.i4
   not
   ldc.i4 0xfffff425
   bne.un Done

TestCase2054:
   ldc.i4 2154
   stloc.0
   ldc.i4 0x47b9
   conv.i4
   not
   ldc.i4 0xffffb846
   beq TestCase2053
   br Done

TestCase2053:
   ldc.i4 2153
   stloc.0
   ldc.i4 0xffff9a96
   conv.i4
   not
   ldc.i4 0x6569
   beq TestCase2052
   br Done

TestCase2052:
   ldc.i4 2152
   stloc.0
   ldc.i4 0x67c4
   conv.i4
   not
   ldc.i4 0xffff983b
   bne.un Done

TestCase2051:
   ldc.i4 2151
   stloc.0
   ldc.i4 0xffff913c
   conv.i4
   not
   ldc.i4 0x6ec3
   beq TestCase2050
   br Done

TestCase2050:
   ldc.i4 2150
   stloc.0
   ldc.i4 0xffffec13
   conv.i4
   not
   ldc.i4 0x13ec
   bne.un Done

TestCase2049:
   ldc.i4 2149
   stloc.0
   ldc.i4 0x75c2
   conv.i4
   not
   ldc.i4 0xffff8a3d
   bne.un Done

TestCase2048:
   ldc.i4 2148
   stloc.0
   ldc.i4 0xffffbcd5
   conv.i4
   not
   ldc.i4 0x432a
   beq TestCase2047
   br Done

TestCase2047:
   ldc.i4 2147
   stloc.0
   ldc.i4 0xfffffc40
   conv.i4
   not
   ldc.i4 0x3bf
   bne.un Done

TestCase2046:
   ldc.i4 2146
   stloc.0
   ldc.i4 0x6eda
   conv.i4
   not
   ldc.i4 0xffff9125
   bne.un Done

TestCase2045:
   ldc.i4 2145
   stloc.0
   ldc.i4 0x76d0
   conv.i4
   not
   ldc.i4 0xffff892f
   beq TestCase2044
   br Done

TestCase2044:
   ldc.i4 2144
   stloc.0
   ldc.i4 0xffffe206
   conv.i4
   not
   ldc.i4 0x1df9
   beq TestCase2043
   br Done

TestCase2043:
   ldc.i4 2143
   stloc.0
   ldc.i4 0xffffb72e
   conv.i4
   not
   ldc.i4 0x48d1
   bne.un Done

TestCase2042:
   ldc.i4 2142
   stloc.0
   ldc.i4 0x6687
   conv.i4
   not
   ldc.i4 0xffff9978
   beq TestCase2041
   br Done

TestCase2041:
   ldc.i4 2141
   stloc.0
   ldc.i4 0xffffa284
   conv.i4
   not
   ldc.i4 0x5d7b
   beq TestCase2040
   br Done

TestCase2040:
   ldc.i4 2140
   stloc.0
   ldc.i4 0xffffe099
   conv.i4
   not
   ldc.i4 0x1f66
   bne.un Done

TestCase2039:
   ldc.i4 2139
   stloc.0
   ldc.i4 0xffffb35f
   conv.i4
   not
   ldc.i4 0x4ca0
   bne.un Done

TestCase2038:
   ldc.i4 2138
   stloc.0
   ldc.i4 0x2a7
   conv.i4
   not
   ldc.i4 0xfffffd58
   beq TestCase2037
   br Done

TestCase2037:
   ldc.i4 2137
   stloc.0
   ldc.i4 0xffffef97
   conv.i4
   not
   ldc.i4 0x1068
   bne.un Done

TestCase2036:
   ldc.i4 2136
   stloc.0
   ldc.i4 0xffff8d02
   conv.i4
   not
   ldc.i4 0x72fd
   bne.un Done

TestCase2035:
   ldc.i4 2135
   stloc.0
   ldc.i4 0x5734
   conv.i4
   not
   ldc.i4 0xffffa8cb
   bne.un Done

TestCase2034:
   ldc.i4 2134
   stloc.0
   ldc.i4 0xffffb739
   conv.i4
   not
   ldc.i4 0x48c6
   beq TestCase2033
   br Done

TestCase2033:
   ldc.i4 2133
   stloc.0
   ldc.i4 0xfffff8ad
   conv.i4
   not
   ldc.i4 0x752
   bne.un Done

TestCase2032:
   ldc.i4 2132
   stloc.0
   ldc.i4 0x49f6
   conv.i4
   not
   ldc.i4 0xffffb609
   bne.un Done

TestCase2031:
   ldc.i4 2131
   stloc.0
   ldc.i4 0x56ee
   conv.i4
   not
   ldc.i4 0xffffa911
   bne.un Done

TestCase2030:
   ldc.i4 2130
   stloc.0
   ldc.i4 0xfffff248
   conv.i4
   not
   ldc.i4 0xdb7
   bne.un Done

TestCase2029:
   ldc.i4 2129
   stloc.0
   ldc.i4 0xffffbb5b
   conv.i4
   not
   ldc.i4 0x44a4
   beq TestCase2028
   br Done

TestCase2028:
   ldc.i4 2128
   stloc.0
   ldc.i4 0x57b8
   conv.i4
   not
   ldc.i4 0xffffa847
   bne.un Done

TestCase2027:
   ldc.i4 2127
   stloc.0
   ldc.i4 0xffffb234
   conv.i4
   not
   ldc.i4 0x4dcb
   bne.un Done

TestCase2026:
   ldc.i4 2126
   stloc.0
   ldc.i4 0x403f
   conv.i4
   not
   ldc.i4 0xffffbfc0
   beq TestCase2025
   br Done

TestCase2025:
   ldc.i4 2125
   stloc.0
   ldc.i4 0x559
   conv.i4
   not
   ldc.i4 0xfffffaa6
   bne.un Done

TestCase2024:
   ldc.i4 2124
   stloc.0
   ldc.i4 0xffffea54
   conv.i4
   not
   ldc.i4 0x15ab
   bne.un Done

TestCase2023:
   ldc.i4 2123
   stloc.0
   ldc.i4 0xffffc524
   conv.i4
   not
   ldc.i4 0x3adb
   beq TestCase2022
   br Done

TestCase2022:
   ldc.i4 2122
   stloc.0
   ldc.i4 0xffffe706
   conv.i4
   not
   ldc.i4 0x18f9
   bne.un Done

TestCase2021:
   ldc.i4 2121
   stloc.0
   ldc.i4 0xffffbcec
   conv.i4
   not
   ldc.i4 0x4313
   bne.un Done

TestCase2020:
   ldc.i4 2120
   stloc.0
   ldc.i4 0xffffa5ba
   conv.i4
   not
   ldc.i4 0x5a45
   bne.un Done

TestCase2019:
   ldc.i4 2119
   stloc.0
   ldc.i4 0x3c00
   conv.i4
   not
   ldc.i4 0xffffc3ff
   beq TestCase2018
   br Done

TestCase2018:
   ldc.i4 2118
   stloc.0
   ldc.i4 0xffffa46b
   conv.i4
   not
   ldc.i4 0x5b94
   beq TestCase2017
   br Done

TestCase2017:
   ldc.i4 2117
   stloc.0
   ldc.i4 0x6481
   conv.i4
   not
   ldc.i4 0xffff9b7e
   beq TestCase2016
   br Done

TestCase2016:
   ldc.i4 2116
   stloc.0
   ldc.i4 0xfffff0af
   conv.i4
   not
   ldc.i4 0xf50
   beq TestCase2015
   br Done

TestCase2015:
   ldc.i4 2115
   stloc.0
   ldc.i4 0x63e4
   conv.i4
   not
   ldc.i4 0xffff9c1b
   beq TestCase2014
   br Done

TestCase2014:
   ldc.i4 2114
   stloc.0
   ldc.i4 0xffffead2
   conv.i4
   not
   ldc.i4 0x152d
   beq TestCase2013
   br Done

TestCase2013:
   ldc.i4 2113
   stloc.0
   ldc.i4 0x37ed
   conv.i4
   not
   ldc.i4 0xffffc812
   bne.un Done

TestCase2012:
   ldc.i4 2112
   stloc.0
   ldc.i4 0x137a
   conv.i4
   not
   ldc.i4 0xffffec85
   bne.un Done

TestCase2011:
   ldc.i4 2111
   stloc.0
   ldc.i4 0x3d
   conv.i4
   not
   ldc.i4 0xffffffc2
   beq TestCase2010
   br Done

TestCase2010:
   ldc.i4 2110
   stloc.0
   ldc.i4 0xfffffbfb
   conv.i4
   not
   ldc.i4 0x404
   bne.un Done

TestCase2009:
   ldc.i4 2109
   stloc.0
   ldc.i4 0x1424
   conv.i4
   not
   ldc.i4 0xffffebdb
   beq TestCase2008
   br Done

TestCase2008:
   ldc.i4 2108
   stloc.0
   ldc.i4 0xffffece1
   conv.i4
   not
   ldc.i4 0x131e
   beq TestCase2007
   br Done

TestCase2007:
   ldc.i4 2107
   stloc.0
   ldc.i4 0xffffb3bd
   conv.i4
   not
   ldc.i4 0x4c42
   bne.un Done

TestCase2006:
   ldc.i4 2106
   stloc.0
   ldc.i4 0xffffeb99
   conv.i4
   not
   ldc.i4 0x1466
   bne.un Done

TestCase2005:
   ldc.i4 2105
   stloc.0
   ldc.i4 0xffffc476
   conv.i4
   not
   ldc.i4 0x3b89
   bne.un Done

TestCase2004:
   ldc.i4 2104
   stloc.0
   ldc.i4 0x6458
   conv.i4
   not
   ldc.i4 0xffff9ba7
   beq TestCase2003
   br Done

TestCase2003:
   ldc.i4 2103
   stloc.0
   ldc.i4 0xffffb490
   conv.i4
   not
   ldc.i4 0x4b6f
   bne.un Done

TestCase2002:
   ldc.i4 2102
   stloc.0
   ldc.i4 0x1edb
   conv.i4
   not
   ldc.i4 0xffffe124
   bne.un Done

TestCase2001:
   ldc.i4 2101
   stloc.0
   ldc.i4 0x5d7c
   conv.i4
   not
   ldc.i4 0xffffa283
   bne.un Done

TestCase2000:
   ldc.i4 2100
   stloc.0
   ldc.i4 0x3246
   conv.i4
   not
   ldc.i4 0xffffcdb9
   bne.un Done

TestCase1999:
   ldc.i4 2099
   stloc.0
   ldc.i4 0x4879
   conv.i4
   not
   ldc.i4 0xffffb786
   beq TestCase1998
   br Done

TestCase1998:
   ldc.i4 2098
   stloc.0
   ldc.i4 0x538e
   conv.i4
   not
   ldc.i4 0xffffac71
   beq TestCase1997
   br Done

TestCase1997:
   ldc.i4 2097
   stloc.0
   ldc.i4 0x51e8
   conv.i4
   not
   ldc.i4 0xffffae17
   beq TestCase1996
   br Done

TestCase1996:
   ldc.i4 2096
   stloc.0
   ldc.i4 0x39b
   conv.i4
   not
   ldc.i4 0xfffffc64
   beq TestCase1995
   br Done

TestCase1995:
   ldc.i4 2095
   stloc.0
   ldc.i4 0x348d
   conv.i4
   not
   ldc.i4 0xffffcb72
   bne.un Done

TestCase1994:
   ldc.i4 2094
   stloc.0
   ldc.i4 0x7efc
   conv.i4
   not
   ldc.i4 0xffff8103
   bne.un Done

TestCase1993:
   ldc.i4 2093
   stloc.0
   ldc.i4 0x6b2b
   conv.i4
   not
   ldc.i4 0xffff94d4
   bne.un Done

TestCase1992:
   ldc.i4 2092
   stloc.0
   ldc.i4 0x686a
   conv.i4
   not
   ldc.i4 0xffff9795
   bne.un Done

TestCase1991:
   ldc.i4 2091
   stloc.0
   ldc.i4 0xffff9d34
   conv.i4
   not
   ldc.i4 0x62cb
   beq TestCase1990
   br Done

TestCase1990:
   ldc.i4 2090
   stloc.0
   ldc.i4 0x13df
   conv.i4
   not
   ldc.i4 0xffffec20
   bne.un Done

TestCase1989:
   ldc.i4 2089
   stloc.0
   ldc.i4 0xffff85d6
   conv.i4
   not
   ldc.i4 0x7a29
   beq TestCase1988
   br Done

TestCase1988:
   ldc.i4 2088
   stloc.0
   ldc.i4 0xffffa4d0
   conv.i4
   not
   ldc.i4 0x5b2f
   beq TestCase1987
   br Done

TestCase1987:
   ldc.i4 2087
   stloc.0
   ldc.i4 0x5c0a
   conv.i4
   not
   ldc.i4 0xffffa3f5
   bne.un Done

TestCase1986:
   ldc.i4 2086
   stloc.0
   ldc.i4 0x265c
   conv.i4
   not
   ldc.i4 0xffffd9a3
   beq TestCase1985
   br Done

TestCase1985:
   ldc.i4 2085
   stloc.0
   ldc.i4 0xffff9f4d
   conv.i4
   not
   ldc.i4 0x60b2
   beq TestCase1984
   br Done

TestCase1984:
   ldc.i4 2084
   stloc.0
   ldc.i4 0xffffdedf
   conv.i4
   not
   ldc.i4 0x2120
   bne.un Done

TestCase1983:
   ldc.i4 2083
   stloc.0
   ldc.i4 0x46c5
   conv.i4
   not
   ldc.i4 0xffffb93a
   beq TestCase1982
   br Done

TestCase1982:
   ldc.i4 2082
   stloc.0
   ldc.i4 0x446c
   conv.i4
   not
   ldc.i4 0xffffbb93
   bne.un Done

TestCase1981:
   ldc.i4 2081
   stloc.0
   ldc.i4 0x4e15
   conv.i4
   not
   ldc.i4 0xffffb1ea
   beq TestCase1980
   br Done

TestCase1980:
   ldc.i4 2080
   stloc.0
   ldc.i4 0xffffca49
   conv.i4
   not
   ldc.i4 0x35b6
   beq TestCase1979
   br Done

TestCase1979:
   ldc.i4 2079
   stloc.0
   ldc.i4 0x1f63
   conv.i4
   not
   ldc.i4 0xffffe09c
   bne.un Done

TestCase1978:
   ldc.i4 2078
   stloc.0
   ldc.i4 0x5787
   conv.i4
   not
   ldc.i4 0xffffa878
   beq TestCase1977
   br Done

TestCase1977:
   ldc.i4 2077
   stloc.0
   ldc.i4 0x1fd
   conv.i4
   not
   ldc.i4 0xfffffe02
   beq TestCase1976
   br Done

TestCase1976:
   ldc.i4 2076
   stloc.0
   ldc.i4 0xd78
   conv.i4
   not
   ldc.i4 0xfffff287
   beq TestCase1975
   br Done

TestCase1975:
   ldc.i4 2075
   stloc.0
   ldc.i4 0x3caf
   conv.i4
   not
   ldc.i4 0xffffc350
   beq TestCase1974
   br Done

TestCase1974:
   ldc.i4 2074
   stloc.0
   ldc.i4 0xffff8469
   conv.i4
   not
   ldc.i4 0x7b96
   bne.un Done

TestCase1973:
   ldc.i4 2073
   stloc.0
   ldc.i4 0x3193
   conv.i4
   not
   ldc.i4 0xffffce6c
   bne.un Done

TestCase1972:
   ldc.i4 2072
   stloc.0
   ldc.i4 0xffffc3f9
   conv.i4
   not
   ldc.i4 0x3c06
   bne.un Done

TestCase1971:
   ldc.i4 2071
   stloc.0
   ldc.i4 0xffffa36b
   conv.i4
   not
   ldc.i4 0x5c94
   bne.un Done

TestCase1970:
   ldc.i4 2070
   stloc.0
   ldc.i4 0x771b
   conv.i4
   not
   ldc.i4 0xffff88e4
   beq TestCase1969
   br Done

TestCase1969:
   ldc.i4 2069
   stloc.0
   ldc.i4 0xffff952c
   conv.i4
   not
   ldc.i4 0x6ad3
   bne.un Done

TestCase1968:
   ldc.i4 2068
   stloc.0
   ldc.i4 0xffffb693
   conv.i4
   not
   ldc.i4 0x496c
   bne.un Done

TestCase1967:
   ldc.i4 2067
   stloc.0
   ldc.i4 0x351d
   conv.i4
   not
   ldc.i4 0xffffcae2
   beq TestCase1966
   br Done

TestCase1966:
   ldc.i4 2066
   stloc.0
   ldc.i4 0x409a
   conv.i4
   not
   ldc.i4 0xffffbf65
   beq TestCase1965
   br Done

TestCase1965:
   ldc.i4 2065
   stloc.0
   ldc.i4 0x1232
   conv.i4
   not
   ldc.i4 0xffffedcd
   beq TestCase1964
   br Done

TestCase1964:
   ldc.i4 2064
   stloc.0
   ldc.i4 0xffffbd58
   conv.i4
   not
   ldc.i4 0x42a7
   beq TestCase1963
   br Done

TestCase1963:
   ldc.i4 2063
   stloc.0
   ldc.i4 0xffffba5b
   conv.i4
   not
   ldc.i4 0x45a4
   bne.un Done

TestCase1962:
   ldc.i4 2062
   stloc.0
   ldc.i4 0x3d4a
   conv.i4
   not
   ldc.i4 0xffffc2b5
   bne.un Done

TestCase1961:
   ldc.i4 2061
   stloc.0
   ldc.i4 0x5669
   conv.i4
   not
   ldc.i4 0xffffa996
   beq TestCase1960
   br Done

TestCase1960:
   ldc.i4 2060
   stloc.0
   ldc.i4 0x5174
   conv.i4
   not
   ldc.i4 0xffffae8b
   bne.un Done

TestCase1959:
   ldc.i4 2059
   stloc.0
   ldc.i4 0x6463
   conv.i4
   not
   ldc.i4 0xffff9b9c
   beq TestCase1958
   br Done

TestCase1958:
   ldc.i4 2058
   stloc.0
   ldc.i4 0x2501
   conv.i4
   not
   ldc.i4 0xffffdafe
   bne.un Done

TestCase1957:
   ldc.i4 2057
   stloc.0
   ldc.i4 0x36a3
   conv.i4
   not
   ldc.i4 0xffffc95c
   bne.un Done

TestCase1956:
   ldc.i4 2056
   stloc.0
   ldc.i4 0xffff98a7
   conv.i4
   not
   ldc.i4 0x6758
   beq TestCase1955
   br Done

TestCase1955:
   ldc.i4 2055
   stloc.0
   ldc.i4 0xffffbbe7
   conv.i4
   not
   ldc.i4 0x4418
   beq TestCase1954
   br Done

TestCase1954:
   ldc.i4 2054
   stloc.0
   ldc.i4 0x375b
   conv.i4
   not
   ldc.i4 0xffffc8a4
   beq TestCase1953
   br Done

TestCase1953:
   ldc.i4 2053
   stloc.0
   ldc.i4 0xffff8931
   conv.i4
   not
   ldc.i4 0x76ce
   beq TestCase1952
   br Done

TestCase1952:
   ldc.i4 2052
   stloc.0
   ldc.i4 0x47f5
   conv.i4
   not
   ldc.i4 0xffffb80a
   beq TestCase1951
   br Done

TestCase1951:
   ldc.i4 2051
   stloc.0
   ldc.i4 0x6e19
   conv.i4
   not
   ldc.i4 0xffff91e6
   beq TestCase1950
   br Done

TestCase1950:
   ldc.i4 2050
   stloc.0
   ldc.i4 0xffffd1d4
   conv.i4
   not
   ldc.i4 0x2e2b
   bne.un Done

TestCase1949:
   ldc.i4 2049
   stloc.0
   ldc.i4 0x1e3e
   conv.i4
   not
   ldc.i4 0xffffe1c1
   bne.un Done

TestCase1948:
   ldc.i4 2048
   stloc.0
   ldc.i4 0xffff8176
   conv.i4
   not
   ldc.i4 0x7e89
   bne.un Done

TestCase1947:
   ldc.i4 2047
   stloc.0
   ldc.i4 0x6c7c
   conv.i4
   not
   ldc.i4 0xffff9383
   beq TestCase1946
   br Done

TestCase1946:
   ldc.i4 2046
   stloc.0
   ldc.i4 0x1928
   conv.i4
   not
   ldc.i4 0xffffe6d7
   beq TestCase1945
   br Done

TestCase1945:
   ldc.i4 2045
   stloc.0
   ldc.i4 0x5c7e
   conv.i4
   not
   ldc.i4 0xffffa381
   beq TestCase1944
   br Done

TestCase1944:
   ldc.i4 2044
   stloc.0
   ldc.i4 0xfffff536
   conv.i4
   not
   ldc.i4 0xac9
   beq TestCase1943
   br Done

TestCase1943:
   ldc.i4 2043
   stloc.0
   ldc.i4 0x209d
   conv.i4
   not
   ldc.i4 0xffffdf62
   beq TestCase1942
   br Done

TestCase1942:
   ldc.i4 2042
   stloc.0
   ldc.i4 0xfffff130
   conv.i4
   not
   ldc.i4 0xecf
   beq TestCase1941
   br Done

TestCase1941:
   ldc.i4 2041
   stloc.0
   ldc.i4 0xffff8f85
   conv.i4
   not
   ldc.i4 0x707a
   beq TestCase1940
   br Done

TestCase1940:
   ldc.i4 2040
   stloc.0
   ldc.i4 0x50e1
   conv.i4
   not
   ldc.i4 0xffffaf1e
   bne.un Done

TestCase1939:
   ldc.i4 2039
   stloc.0
   ldc.i4 0x1dc
   conv.i4
   not
   ldc.i4 0xfffffe23
   beq TestCase1938
   br Done

TestCase1938:
   ldc.i4 2038
   stloc.0
   ldc.i4 0xffffa161
   conv.i4
   not
   ldc.i4 0x5e9e
   beq TestCase1937
   br Done

TestCase1937:
   ldc.i4 2037
   stloc.0
   ldc.i4 0x6dd3
   conv.i4
   not
   ldc.i4 0xffff922c
   beq TestCase1936
   br Done

TestCase1936:
   ldc.i4 2036
   stloc.0
   ldc.i4 0x18c3
   conv.i4
   not
   ldc.i4 0xffffe73c
   bne.un Done

TestCase1935:
   ldc.i4 2035
   stloc.0
   ldc.i4 0xffffbf70
   conv.i4
   not
   ldc.i4 0x408f
   bne.un Done

TestCase1934:
   ldc.i4 2034
   stloc.0
   ldc.i4 0x3075
   conv.i4
   not
   ldc.i4 0xffffcf8a
   beq TestCase1933
   br Done

TestCase1933:
   ldc.i4 2033
   stloc.0
   ldc.i4 0x7c78
   conv.i4
   not
   ldc.i4 0xffff8387
   beq TestCase1932
   br Done

TestCase1932:
   ldc.i4 2032
   stloc.0
   ldc.i4 0x28b7
   conv.i4
   not
   ldc.i4 0xffffd748
   beq TestCase1931
   br Done

TestCase1931:
   ldc.i4 2031
   stloc.0
   ldc.i4 0x94d
   conv.i4
   not
   ldc.i4 0xfffff6b2
   beq TestCase1930
   br Done

TestCase1930:
   ldc.i4 2030
   stloc.0
   ldc.i4 0x3d42
   conv.i4
   not
   ldc.i4 0xffffc2bd
   beq TestCase1929
   br Done

TestCase1929:
   ldc.i4 2029
   stloc.0
   ldc.i4 0xffff9174
   conv.i4
   not
   ldc.i4 0x6e8b
   bne.un Done

TestCase1928:
   ldc.i4 2028
   stloc.0
   ldc.i4 0x4724
   conv.i4
   not
   ldc.i4 0xffffb8db
   bne.un Done

TestCase1927:
   ldc.i4 2027
   stloc.0
   ldc.i4 0x6457
   conv.i4
   not
   ldc.i4 0xffff9ba8
   bne.un Done

TestCase1926:
   ldc.i4 2026
   stloc.0
   ldc.i4 0xcf7
   conv.i4
   not
   ldc.i4 0xfffff308
   beq TestCase1925
   br Done

TestCase1925:
   ldc.i4 2025
   stloc.0
   ldc.i4 0x4878
   conv.i4
   not
   ldc.i4 0xffffb787
   beq TestCase1924
   br Done

TestCase1924:
   ldc.i4 2024
   stloc.0
   ldc.i4 0xffffd066
   conv.i4
   not
   ldc.i4 0x2f99
   beq TestCase1923
   br Done

TestCase1923:
   ldc.i4 2023
   stloc.0
   ldc.i4 0x6d65
   conv.i4
   not
   ldc.i4 0xffff929a
   beq TestCase1922
   br Done

TestCase1922:
   ldc.i4 2022
   stloc.0
   ldc.i4 0x65a9
   conv.i4
   not
   ldc.i4 0xffff9a56
   beq TestCase1921
   br Done

TestCase1921:
   ldc.i4 2021
   stloc.0
   ldc.i4 0xffffe8b2
   conv.i4
   not
   ldc.i4 0x174d
   bne.un Done

TestCase1920:
   ldc.i4 2020
   stloc.0
   ldc.i4 0x4723
   conv.i4
   not
   ldc.i4 0xffffb8dc
   bne.un Done

TestCase1919:
   ldc.i4 2019
   stloc.0
   ldc.i4 0x5f99
   conv.i4
   not
   ldc.i4 0xffffa066
   beq TestCase1918
   br Done

TestCase1918:
   ldc.i4 2018
   stloc.0
   ldc.i4 0x5807
   conv.i4
   not
   ldc.i4 0xffffa7f8
   beq TestCase1917
   br Done

TestCase1917:
   ldc.i4 2017
   stloc.0
   ldc.i4 0xffffcea0
   conv.i4
   not
   ldc.i4 0x315f
   bne.un Done

TestCase1916:
   ldc.i4 2016
   stloc.0
   ldc.i4 0x1bcb
   conv.i4
   not
   ldc.i4 0xffffe434
   bne.un Done

TestCase1915:
   ldc.i4 2015
   stloc.0
   ldc.i4 0x31bf
   conv.i4
   not
   ldc.i4 0xffffce40
   beq TestCase1914
   br Done

TestCase1914:
   ldc.i4 2014
   stloc.0
   ldc.i4 0x14fd
   conv.i4
   not
   ldc.i4 0xffffeb02
   beq TestCase1913
   br Done

TestCase1913:
   ldc.i4 2013
   stloc.0
   ldc.i4 0x3d20
   conv.i4
   not
   ldc.i4 0xffffc2df
   bne.un Done

TestCase1912:
   ldc.i4 2012
   stloc.0
   ldc.i4 0xffff8493
   conv.i4
   not
   ldc.i4 0x7b6c
   beq TestCase1911
   br Done

TestCase1911:
   ldc.i4 2011
   stloc.0
   ldc.i4 0xffffc13a
   conv.i4
   not
   ldc.i4 0x3ec5
   bne.un Done

TestCase1910:
   ldc.i4 2010
   stloc.0
   ldc.i4 0x78a9
   conv.i4
   not
   ldc.i4 0xffff8756
   beq TestCase1909
   br Done

TestCase1909:
   ldc.i4 2009
   stloc.0
   ldc.i4 0x1a3a
   conv.i4
   not
   ldc.i4 0xffffe5c5
   bne.un Done

TestCase1908:
   ldc.i4 2008
   stloc.0
   ldc.i4 0xffffd241
   conv.i4
   not
   ldc.i4 0x2dbe
   beq TestCase1907
   br Done

TestCase1907:
   ldc.i4 2007
   stloc.0
   ldc.i4 0x367b
   conv.i4
   not
   ldc.i4 0xffffc984
   bne.un Done

TestCase1906:
   ldc.i4 2006
   stloc.0
   ldc.i4 0x767d
   conv.i4
   not
   ldc.i4 0xffff8982
   bne.un Done

TestCase1905:
   ldc.i4 2005
   stloc.0
   ldc.i4 0xfffff710
   conv.i4
   not
   ldc.i4 0x8ef
   bne.un Done

TestCase1904:
   ldc.i4 2004
   stloc.0
   ldc.i4 0x1da0
   conv.i4
   not
   ldc.i4 0xffffe25f
   bne.un Done

TestCase1903:
   ldc.i4 2003
   stloc.0
   ldc.i4 0xffffad74
   conv.i4
   not
   ldc.i4 0x528b
   beq TestCase1902
   br Done

TestCase1902:
   ldc.i4 2002
   stloc.0
   ldc.i4 0x52b5
   conv.i4
   not
   ldc.i4 0xffffad4a
   bne.un Done

TestCase1901:
   ldc.i4 2001
   stloc.0
   ldc.i4 0x2bb4
   conv.i4
   not
   ldc.i4 0xffffd44b
   beq TestCase1900
   br Done

TestCase1900:
   ldc.i4 2000
   stloc.0
   ldc.i4 0xffff8260
   conv.i4
   not
   ldc.i4 0x7d9f
   beq TestCase1899
   br Done

TestCase1899:
   ldc.i4 1999
   stloc.0
   ldc.i4 0xbbb
   conv.i4
   not
   ldc.i4 0xfffff444
   beq TestCase1898
   br Done

TestCase1898:
   ldc.i4 1998
   stloc.0
   ldc.i4 0xffffab04
   conv.i4
   not
   ldc.i4 0x54fb
   beq TestCase1897
   br Done

TestCase1897:
   ldc.i4 1997
   stloc.0
   ldc.i4 0x3f0b
   conv.i4
   not
   ldc.i4 0xffffc0f4
   bne.un Done

TestCase1896:
   ldc.i4 1996
   stloc.0
   ldc.i4 0xffffcc30
   conv.i4
   not
   ldc.i4 0x33cf
   bne.un Done

TestCase1895:
   ldc.i4 1995
   stloc.0
   ldc.i4 0x4b61
   conv.i4
   not
   ldc.i4 0xffffb49e
   bne.un Done

TestCase1894:
   ldc.i4 1994
   stloc.0
   ldc.i4 0xffff9fa7
   conv.i4
   not
   ldc.i4 0x6058
   bne.un Done

TestCase1893:
   ldc.i4 1993
   stloc.0
   ldc.i4 0xffffdc60
   conv.i4
   not
   ldc.i4 0x239f
   beq TestCase1892
   br Done

TestCase1892:
   ldc.i4 1992
   stloc.0
   ldc.i4 0xffffe735
   conv.i4
   not
   ldc.i4 0x18ca
   bne.un Done

TestCase1891:
   ldc.i4 1991
   stloc.0
   ldc.i4 0xffffaada
   conv.i4
   not
   ldc.i4 0x5525
   beq TestCase1890
   br Done

TestCase1890:
   ldc.i4 1990
   stloc.0
   ldc.i4 0x1c5e
   conv.i4
   not
   ldc.i4 0xffffe3a1
   bne.un Done

TestCase1889:
   ldc.i4 1989
   stloc.0
   ldc.i4 0x16a7
   conv.i4
   not
   ldc.i4 0xffffe958
   bne.un Done

TestCase1888:
   ldc.i4 1988
   stloc.0
   ldc.i4 0xffffa9fb
   conv.i4
   not
   ldc.i4 0x5604
   bne.un Done

TestCase1887:
   ldc.i4 1987
   stloc.0
   ldc.i4 0xffff9b02
   conv.i4
   not
   ldc.i4 0x64fd
   bne.un Done

TestCase1886:
   ldc.i4 1986
   stloc.0
   ldc.i4 0x1b94
   conv.i4
   not
   ldc.i4 0xffffe46b
   beq TestCase1885
   br Done

TestCase1885:
   ldc.i4 1985
   stloc.0
   ldc.i4 0x3474
   conv.i4
   not
   ldc.i4 0xffffcb8b
   beq TestCase1884
   br Done

TestCase1884:
   ldc.i4 1984
   stloc.0
   ldc.i4 0x6f52
   conv.i4
   not
   ldc.i4 0xffff90ad
   bne.un Done

TestCase1883:
   ldc.i4 1983
   stloc.0
   ldc.i4 0x3d66
   conv.i4
   not
   ldc.i4 0xffffc299
   bne.un Done

TestCase1882:
   ldc.i4 1982
   stloc.0
   ldc.i4 0xffffd4e0
   conv.i4
   not
   ldc.i4 0x2b1f
   bne.un Done

TestCase1881:
   ldc.i4 1981
   stloc.0
   ldc.i4 0xffffdea5
   conv.i4
   not
   ldc.i4 0x215a
   beq TestCase1880
   br Done

TestCase1880:
   ldc.i4 1980
   stloc.0
   ldc.i4 0x43b9
   conv.i4
   not
   ldc.i4 0xffffbc46
   beq TestCase1879
   br Done

TestCase1879:
   ldc.i4 1979
   stloc.0
   ldc.i4 0x21e0
   conv.i4
   not
   ldc.i4 0xffffde1f
   bne.un Done

TestCase1878:
   ldc.i4 1978
   stloc.0
   ldc.i4 0x37ba
   conv.i4
   not
   ldc.i4 0xffffc845
   beq TestCase1877
   br Done

TestCase1877:
   ldc.i4 1977
   stloc.0
   ldc.i4 0xffff8b37
   conv.i4
   not
   ldc.i4 0x74c8
   bne.un Done

TestCase1876:
   ldc.i4 1976
   stloc.0
   ldc.i4 0xffffee87
   conv.i4
   not
   ldc.i4 0x1178
   bne.un Done

TestCase1875:
   ldc.i4 1975
   stloc.0
   ldc.i4 0x79ac
   conv.i4
   not
   ldc.i4 0xffff8653
   bne.un Done

TestCase1874:
   ldc.i4 1974
   stloc.0
   ldc.i4 0xffff8c31
   conv.i4
   not
   ldc.i4 0x73ce
   beq TestCase1873
   br Done

TestCase1873:
   ldc.i4 1973
   stloc.0
   ldc.i4 0xffffbd5d
   conv.i4
   not
   ldc.i4 0x42a2
   bne.un Done

TestCase1872:
   ldc.i4 1972
   stloc.0
   ldc.i4 0x495c
   conv.i4
   not
   ldc.i4 0xffffb6a3
   beq TestCase1871
   br Done

TestCase1871:
   ldc.i4 1971
   stloc.0
   ldc.i4 0x16c7
   conv.i4
   not
   ldc.i4 0xffffe938
   bne.un Done

TestCase1870:
   ldc.i4 1970
   stloc.0
   ldc.i4 0x2af4
   conv.i4
   not
   ldc.i4 0xffffd50b
   beq TestCase1869
   br Done

TestCase1869:
   ldc.i4 1969
   stloc.0
   ldc.i4 0x5ede
   conv.i4
   not
   ldc.i4 0xffffa121
   beq TestCase1868
   br Done

TestCase1868:
   ldc.i4 1968
   stloc.0
   ldc.i4 0x3d3b
   conv.i4
   not
   ldc.i4 0xffffc2c4
   bne.un Done

TestCase1867:
   ldc.i4 1967
   stloc.0
   ldc.i4 0x5d28
   conv.i4
   not
   ldc.i4 0xffffa2d7
   bne.un Done

TestCase1866:
   ldc.i4 1966
   stloc.0
   ldc.i4 0x6e8e
   conv.i4
   not
   ldc.i4 0xffff9171
   beq TestCase1865
   br Done

TestCase1865:
   ldc.i4 1965
   stloc.0
   ldc.i4 0xffffc8a1
   conv.i4
   not
   ldc.i4 0x375e
   beq TestCase1864
   br Done

TestCase1864:
   ldc.i4 1964
   stloc.0
   ldc.i4 0xffff8a2f
   conv.i4
   not
   ldc.i4 0x75d0
   beq TestCase1863
   br Done

TestCase1863:
   ldc.i4 1963
   stloc.0
   ldc.i4 0x4639
   conv.i4
   not
   ldc.i4 0xffffb9c6
   beq TestCase1862
   br Done

TestCase1862:
   ldc.i4 1962
   stloc.0
   ldc.i4 0xfffff841
   conv.i4
   not
   ldc.i4 0x7be
   beq TestCase1861
   br Done

TestCase1861:
   ldc.i4 1961
   stloc.0
   ldc.i4 0x6014
   conv.i4
   not
   ldc.i4 0xffff9feb
   bne.un Done

TestCase1860:
   ldc.i4 1960
   stloc.0
   ldc.i4 0xffffc43c
   conv.i4
   not
   ldc.i4 0x3bc3
   beq TestCase1859
   br Done

TestCase1859:
   ldc.i4 1959
   stloc.0
   ldc.i4 0x7c94
   conv.i4
   not
   ldc.i4 0xffff836b
   beq TestCase1858
   br Done

TestCase1858:
   ldc.i4 1958
   stloc.0
   ldc.i4 0xffffa56d
   conv.i4
   not
   ldc.i4 0x5a92
   beq TestCase1857
   br Done

TestCase1857:
   ldc.i4 1957
   stloc.0
   ldc.i4 0xffffd2ad
   conv.i4
   not
   ldc.i4 0x2d52
   beq TestCase1856
   br Done

TestCase1856:
   ldc.i4 1956
   stloc.0
   ldc.i4 0x1ff1
   conv.i4
   not
   ldc.i4 0xffffe00e
   beq TestCase1855
   br Done

TestCase1855:
   ldc.i4 1955
   stloc.0
   ldc.i4 0xfffff401
   conv.i4
   not
   ldc.i4 0xbfe
   beq TestCase1854
   br Done

TestCase1854:
   ldc.i4 1954
   stloc.0
   ldc.i4 0xffffd995
   conv.i4
   not
   ldc.i4 0x266a
   bne.un Done

TestCase1853:
   ldc.i4 1953
   stloc.0
   ldc.i4 0xffff878e
   conv.i4
   not
   ldc.i4 0x7871
   bne.un Done

TestCase1852:
   ldc.i4 1952
   stloc.0
   ldc.i4 0xef6
   conv.i4
   not
   ldc.i4 0xfffff109
   bne.un Done

TestCase1851:
   ldc.i4 1951
   stloc.0
   ldc.i4 0xffff9e58
   conv.i4
   not
   ldc.i4 0x61a7
   beq TestCase1850
   br Done

TestCase1850:
   ldc.i4 1950
   stloc.0
   ldc.i4 0x4fa8
   conv.i4
   not
   ldc.i4 0xffffb057
   bne.un Done

TestCase1849:
   ldc.i4 1949
   stloc.0
   ldc.i4 0x6aa6
   conv.i4
   not
   ldc.i4 0xffff9559
   beq TestCase1848
   br Done

TestCase1848:
   ldc.i4 1948
   stloc.0
   ldc.i4 0xffffc3fb
   conv.i4
   not
   ldc.i4 0x3c04
   bne.un Done

TestCase1847:
   ldc.i4 1947
   stloc.0
   ldc.i4 0xffffe75c
   conv.i4
   not
   ldc.i4 0x18a3
   beq TestCase1846
   br Done

TestCase1846:
   ldc.i4 1946
   stloc.0
   ldc.i4 0xffffe165
   conv.i4
   not
   ldc.i4 0x1e9a
   bne.un Done

TestCase1845:
   ldc.i4 1945
   stloc.0
   ldc.i4 0x6a20
   conv.i4
   not
   ldc.i4 0xffff95df
   beq TestCase1844
   br Done

TestCase1844:
   ldc.i4 1944
   stloc.0
   ldc.i4 0x6ee7
   conv.i4
   not
   ldc.i4 0xffff9118
   bne.un Done

TestCase1843:
   ldc.i4 1943
   stloc.0
   ldc.i4 0x4ea5
   conv.i4
   not
   ldc.i4 0xffffb15a
   bne.un Done

TestCase1842:
   ldc.i4 1942
   stloc.0
   ldc.i4 0xffffe053
   conv.i4
   not
   ldc.i4 0x1fac
   beq TestCase1841
   br Done

TestCase1841:
   ldc.i4 1941
   stloc.0
   ldc.i4 0xfffffa9b
   conv.i4
   not
   ldc.i4 0x564
   beq TestCase1840
   br Done

TestCase1840:
   ldc.i4 1940
   stloc.0
   ldc.i4 0x14e
   conv.i4
   not
   ldc.i4 0xfffffeb1
   beq TestCase1839
   br Done

TestCase1839:
   ldc.i4 1939
   stloc.0
   ldc.i4 0xffff8305
   conv.i4
   not
   ldc.i4 0x7cfa
   beq TestCase1838
   br Done

TestCase1838:
   ldc.i4 1938
   stloc.0
   ldc.i4 0x10ca
   conv.i4
   not
   ldc.i4 0xffffef35
   beq TestCase1837
   br Done

TestCase1837:
   ldc.i4 1937
   stloc.0
   ldc.i4 0xffff8713
   conv.i4
   not
   ldc.i4 0x78ec
   beq TestCase1836
   br Done

TestCase1836:
   ldc.i4 1936
   stloc.0
   ldc.i4 0x175f
   conv.i4
   not
   ldc.i4 0xffffe8a0
   bne.un Done

TestCase1835:
   ldc.i4 1935
   stloc.0
   ldc.i4 0xffffee33
   conv.i4
   not
   ldc.i4 0x11cc
   beq TestCase1834
   br Done

TestCase1834:
   ldc.i4 1934
   stloc.0
   ldc.i4 0xfffff7ee
   conv.i4
   not
   ldc.i4 0x811
   bne.un Done

TestCase1833:
   ldc.i4 1933
   stloc.0
   ldc.i4 0xffffd501
   conv.i4
   not
   ldc.i4 0x2afe
   bne.un Done

TestCase1832:
   ldc.i4 1932
   stloc.0
   ldc.i4 0xffffa081
   conv.i4
   not
   ldc.i4 0x5f7e
   bne.un Done

TestCase1831:
   ldc.i4 1931
   stloc.0
   ldc.i4 0xffffba68
   conv.i4
   not
   ldc.i4 0x4597
   beq TestCase1830
   br Done

TestCase1830:
   ldc.i4 1930
   stloc.0
   ldc.i4 0xffffc03e
   conv.i4
   not
   ldc.i4 0x3fc1
   beq TestCase1829
   br Done

TestCase1829:
   ldc.i4 1929
   stloc.0
   ldc.i4 0xffffc634
   conv.i4
   not
   ldc.i4 0x39cb
   bne.un Done

TestCase1828:
   ldc.i4 1928
   stloc.0
   ldc.i4 0xffff9aa3
   conv.i4
   not
   ldc.i4 0x655c
   beq TestCase1827
   br Done

TestCase1827:
   ldc.i4 1927
   stloc.0
   ldc.i4 0x68e7
   conv.i4
   not
   ldc.i4 0xffff9718
   beq TestCase1826
   br Done

TestCase1826:
   ldc.i4 1926
   stloc.0
   ldc.i4 0x335f
   conv.i4
   not
   ldc.i4 0xffffcca0
   beq TestCase1825
   br Done

TestCase1825:
   ldc.i4 1925
   stloc.0
   ldc.i4 0x77c9
   conv.i4
   not
   ldc.i4 0xffff8836
   beq TestCase1824
   br Done

TestCase1824:
   ldc.i4 1924
   stloc.0
   ldc.i4 0x443f
   conv.i4
   not
   ldc.i4 0xffffbbc0
   bne.un Done

TestCase1823:
   ldc.i4 1923
   stloc.0
   ldc.i4 0xffffd9ad
   conv.i4
   not
   ldc.i4 0x2652
   beq TestCase1822
   br Done

TestCase1822:
   ldc.i4 1922
   stloc.0
   ldc.i4 0x7a24
   conv.i4
   not
   ldc.i4 0xffff85db
   bne.un Done

TestCase1821:
   ldc.i4 1921
   stloc.0
   ldc.i4 0xffff8daf
   conv.i4
   not
   ldc.i4 0x7250
   beq TestCase1820
   br Done

TestCase1820:
   ldc.i4 1920
   stloc.0
   ldc.i4 0xffffbb6f
   conv.i4
   not
   ldc.i4 0x4490
   bne.un Done

TestCase1819:
   ldc.i4 1919
   stloc.0
   ldc.i4 0xffff8b43
   conv.i4
   not
   ldc.i4 0x74bc
   bne.un Done

TestCase1818:
   ldc.i4 1918
   stloc.0
   ldc.i4 0xfffff552
   conv.i4
   not
   ldc.i4 0xaad
   beq TestCase1817
   br Done

TestCase1817:
   ldc.i4 1917
   stloc.0
   ldc.i4 0x5e7a
   conv.i4
   not
   ldc.i4 0xffffa185
   beq TestCase1816
   br Done

TestCase1816:
   ldc.i4 1916
   stloc.0
   ldc.i4 0x30aa
   conv.i4
   not
   ldc.i4 0xffffcf55
   beq TestCase1815
   br Done

TestCase1815:
   ldc.i4 1915
   stloc.0
   ldc.i4 0xffffca35
   conv.i4
   not
   ldc.i4 0x35ca
   bne.un Done

TestCase1814:
   ldc.i4 1914
   stloc.0
   ldc.i4 0x53e5
   conv.i4
   not
   ldc.i4 0xffffac1a
   beq TestCase1813
   br Done

TestCase1813:
   ldc.i4 1913
   stloc.0
   ldc.i4 0xfffff7ca
   conv.i4
   not
   ldc.i4 0x835
   beq TestCase1812
   br Done

TestCase1812:
   ldc.i4 1912
   stloc.0
   ldc.i4 0x6ce1
   conv.i4
   not
   ldc.i4 0xffff931e
   bne.un Done

TestCase1811:
   ldc.i4 1911
   stloc.0
   ldc.i4 0x3ca0
   conv.i4
   not
   ldc.i4 0xffffc35f
   bne.un Done

TestCase1810:
   ldc.i4 1910
   stloc.0
   ldc.i4 0x572d
   conv.i4
   not
   ldc.i4 0xffffa8d2
   bne.un Done

TestCase1809:
   ldc.i4 1909
   stloc.0
   ldc.i4 0xffffe052
   conv.i4
   not
   ldc.i4 0x1fad
   beq TestCase1808
   br Done

TestCase1808:
   ldc.i4 1908
   stloc.0
   ldc.i4 0x1ecf
   conv.i4
   not
   ldc.i4 0xffffe130
   beq TestCase1807
   br Done

TestCase1807:
   ldc.i4 1907
   stloc.0
   ldc.i4 0x38df
   conv.i4
   not
   ldc.i4 0xffffc720
   bne.un Done

TestCase1806:
   ldc.i4 1906
   stloc.0
   ldc.i4 0xffffd033
   conv.i4
   not
   ldc.i4 0x2fcc
   bne.un Done

TestCase1805:
   ldc.i4 1905
   stloc.0
   ldc.i4 0xffffff26
   conv.i4
   not
   ldc.i4 0xd9
   bne.un Done

TestCase1804:
   ldc.i4 1904
   stloc.0
   ldc.i4 0x6fe6
   conv.i4
   not
   ldc.i4 0xffff9019
   beq TestCase1803
   br Done

TestCase1803:
   ldc.i4 1903
   stloc.0
   ldc.i4 0xffffbe1f
   conv.i4
   not
   ldc.i4 0x41e0
   beq TestCase1802
   br Done

TestCase1802:
   ldc.i4 1902
   stloc.0
   ldc.i4 0xffffcc61
   conv.i4
   not
   ldc.i4 0x339e
   bne.un Done

TestCase1801:
   ldc.i4 1901
   stloc.0
   ldc.i4 0xffffae9b
   conv.i4
   not
   ldc.i4 0x5164
   bne.un Done

TestCase1800:
   ldc.i4 1900
   stloc.0
   ldc.i4 0xffffc8ce
   conv.i4
   not
   ldc.i4 0x3731
   beq TestCase1799
   br Done

TestCase1799:
   ldc.i4 1899
   stloc.0
   ldc.i4 0xffff81cc
   conv.i4
   not
   ldc.i4 0x7e33
   bne.un Done

TestCase1798:
   ldc.i4 1898
   stloc.0
   ldc.i4 0x18f9
   conv.i4
   not
   ldc.i4 0xffffe706
   beq TestCase1797
   br Done

TestCase1797:
   ldc.i4 1897
   stloc.0
   ldc.i4 0x10bf
   conv.i4
   not
   ldc.i4 0xffffef40
   bne.un Done

TestCase1796:
   ldc.i4 1896
   stloc.0
   ldc.i4 0x166d
   conv.i4
   not
   ldc.i4 0xffffe992
   bne.un Done

TestCase1795:
   ldc.i4 1895
   stloc.0
   ldc.i4 0x2357
   conv.i4
   not
   ldc.i4 0xffffdca8
   bne.un Done

TestCase1794:
   ldc.i4 1894
   stloc.0
   ldc.i4 0xffffb226
   conv.i4
   not
   ldc.i4 0x4dd9
   bne.un Done

TestCase1793:
   ldc.i4 1893
   stloc.0
   ldc.i4 0xffff897e
   conv.i4
   not
   ldc.i4 0x7681
   beq TestCase1792
   br Done

TestCase1792:
   ldc.i4 1892
   stloc.0
   ldc.i4 0xffffa9b7
   conv.i4
   not
   ldc.i4 0x5648
   beq TestCase1791
   br Done

TestCase1791:
   ldc.i4 1891
   stloc.0
   ldc.i4 0x49b4
   conv.i4
   not
   ldc.i4 0xffffb64b
   beq TestCase1790
   br Done

TestCase1790:
   ldc.i4 1890
   stloc.0
   ldc.i4 0x6c58
   conv.i4
   not
   ldc.i4 0xffff93a7
   bne.un Done

TestCase1789:
   ldc.i4 1889
   stloc.0
   ldc.i4 0x3b09
   conv.i4
   not
   ldc.i4 0xffffc4f6
   bne.un Done

TestCase1788:
   ldc.i4 1888
   stloc.0
   ldc.i4 0xdf9
   conv.i4
   not
   ldc.i4 0xfffff206
   bne.un Done

TestCase1787:
   ldc.i4 1887
   stloc.0
   ldc.i4 0x50de
   conv.i4
   not
   ldc.i4 0xffffaf21
   bne.un Done

TestCase1786:
   ldc.i4 1886
   stloc.0
   ldc.i4 0xffff87b7
   conv.i4
   not
   ldc.i4 0x7848
   beq TestCase1785
   br Done

TestCase1785:
   ldc.i4 1885
   stloc.0
   ldc.i4 0x264e
   conv.i4
   not
   ldc.i4 0xffffd9b1
   bne.un Done

TestCase1784:
   ldc.i4 1884
   stloc.0
   ldc.i4 0x6200
   conv.i4
   not
   ldc.i4 0xffff9dff
   bne.un Done

TestCase1783:
   ldc.i4 1883
   stloc.0
   ldc.i4 0x500b
   conv.i4
   not
   ldc.i4 0xffffaff4
   bne.un Done

TestCase1782:
   ldc.i4 1882
   stloc.0
   ldc.i4 0xffffadd0
   conv.i4
   not
   ldc.i4 0x522f
   bne.un Done

TestCase1781:
   ldc.i4 1881
   stloc.0
   ldc.i4 0x104
   conv.i4
   not
   ldc.i4 0xfffffefb
   bne.un Done

TestCase1780:
   ldc.i4 1880
   stloc.0
   ldc.i4 0x663e
   conv.i4
   not
   ldc.i4 0xffff99c1
   bne.un Done

TestCase1779:
   ldc.i4 1879
   stloc.0
   ldc.i4 0x4ed3
   conv.i4
   not
   ldc.i4 0xffffb12c
   beq TestCase1778
   br Done

TestCase1778:
   ldc.i4 1878
   stloc.0
   ldc.i4 0xb69
   conv.i4
   not
   ldc.i4 0xfffff496
   beq TestCase1777
   br Done

TestCase1777:
   ldc.i4 1877
   stloc.0
   ldc.i4 0xffffbc0b
   conv.i4
   not
   ldc.i4 0x43f4
   bne.un Done

TestCase1776:
   ldc.i4 1876
   stloc.0
   ldc.i4 0xffff90c9
   conv.i4
   not
   ldc.i4 0x6f36
   bne.un Done

TestCase1775:
   ldc.i4 1875
   stloc.0
   ldc.i4 0xffffbdd6
   conv.i4
   not
   ldc.i4 0x4229
   bne.un Done

TestCase1774:
   ldc.i4 1874
   stloc.0
   ldc.i4 0xfffff869
   conv.i4
   not
   ldc.i4 0x796
   beq TestCase1773
   br Done

TestCase1773:
   ldc.i4 1873
   stloc.0
   ldc.i4 0xffffde63
   conv.i4
   not
   ldc.i4 0x219c
   bne.un Done

TestCase1772:
   ldc.i4 1872
   stloc.0
   ldc.i4 0xffffa15a
   conv.i4
   not
   ldc.i4 0x5ea5
   beq TestCase1771
   br Done

TestCase1771:
   ldc.i4 1871
   stloc.0
   ldc.i4 0x99b
   conv.i4
   not
   ldc.i4 0xfffff664
   bne.un Done

TestCase1770:
   ldc.i4 1870
   stloc.0
   ldc.i4 0xffffe8b2
   conv.i4
   not
   ldc.i4 0x174d
   beq TestCase1769
   br Done

TestCase1769:
   ldc.i4 1869
   stloc.0
   ldc.i4 0xffffb9fa
   conv.i4
   not
   ldc.i4 0x4605
   beq TestCase1768
   br Done

TestCase1768:
   ldc.i4 1868
   stloc.0
   ldc.i4 0xffffc8bf
   conv.i4
   not
   ldc.i4 0x3740
   beq TestCase1767
   br Done

TestCase1767:
   ldc.i4 1867
   stloc.0
   ldc.i4 0x68c6
   conv.i4
   not
   ldc.i4 0xffff9739
   bne.un Done

TestCase1766:
   ldc.i4 1866
   stloc.0
   ldc.i4 0xffff8804
   conv.i4
   not
   ldc.i4 0x77fb
   bne.un Done

TestCase1765:
   ldc.i4 1865
   stloc.0
   ldc.i4 0x68e5
   conv.i4
   not
   ldc.i4 0xffff971a
   bne.un Done

TestCase1764:
   ldc.i4 1864
   stloc.0
   ldc.i4 0x3ce
   conv.i4
   not
   ldc.i4 0xfffffc31
   beq TestCase1763
   br Done

TestCase1763:
   ldc.i4 1863
   stloc.0
   ldc.i4 0xffffc6e4
   conv.i4
   not
   ldc.i4 0x391b
   bne.un Done

TestCase1762:
   ldc.i4 1862
   stloc.0
   ldc.i4 0xffff9ae3
   conv.i4
   not
   ldc.i4 0x651c
   bne.un Done

TestCase1761:
   ldc.i4 1861
   stloc.0
   ldc.i4 0xfffff20a
   conv.i4
   not
   ldc.i4 0xdf5
   bne.un Done

TestCase1760:
   ldc.i4 1860
   stloc.0
   ldc.i4 0x5d65
   conv.i4
   not
   ldc.i4 0xffffa29a
   bne.un Done

TestCase1759:
   ldc.i4 1859
   stloc.0
   ldc.i4 0x4fd9
   conv.i4
   not
   ldc.i4 0xffffb026
   beq TestCase1758
   br Done

TestCase1758:
   ldc.i4 1858
   stloc.0
   ldc.i4 0x269e
   conv.i4
   not
   ldc.i4 0xffffd961
   bne.un Done

TestCase1757:
   ldc.i4 1857
   stloc.0
   ldc.i4 0xf94
   conv.i4
   not
   ldc.i4 0xfffff06b
   beq TestCase1756
   br Done

TestCase1756:
   ldc.i4 1856
   stloc.0
   ldc.i4 0x1d07
   conv.i4
   not
   ldc.i4 0xffffe2f8
   bne.un Done

TestCase1755:
   ldc.i4 1855
   stloc.0
   ldc.i4 0x5441
   conv.i4
   not
   ldc.i4 0xffffabbe
   bne.un Done

TestCase1754:
   ldc.i4 1854
   stloc.0
   ldc.i4 0xffffb76f
   conv.i4
   not
   ldc.i4 0x4890
   beq TestCase1753
   br Done

TestCase1753:
   ldc.i4 1853
   stloc.0
   ldc.i4 0xffffb065
   conv.i4
   not
   ldc.i4 0x4f9a
   beq TestCase1752
   br Done

TestCase1752:
   ldc.i4 1852
   stloc.0
   ldc.i4 0x45f
   conv.i4
   not
   ldc.i4 0xfffffba0
   bne.un Done

TestCase1751:
   ldc.i4 1851
   stloc.0
   ldc.i4 0xfffffde2
   conv.i4
   not
   ldc.i4 0x21d
   bne.un Done

TestCase1750:
   ldc.i4 1850
   stloc.0
   ldc.i4 0x6812
   conv.i4
   not
   ldc.i4 0xffff97ed
   bne.un Done

TestCase1749:
   ldc.i4 1849
   stloc.0
   ldc.i4 0x2a82
   conv.i4
   not
   ldc.i4 0xffffd57d
   bne.un Done

TestCase1748:
   ldc.i4 1848
   stloc.0
   ldc.i4 0xffff9629
   conv.i4
   not
   ldc.i4 0x69d6
   beq TestCase1747
   br Done

TestCase1747:
   ldc.i4 1847
   stloc.0
   ldc.i4 0x1478
   conv.i4
   not
   ldc.i4 0xffffeb87
   bne.un Done

TestCase1746:
   ldc.i4 1846
   stloc.0
   ldc.i4 0x4178
   conv.i4
   not
   ldc.i4 0xffffbe87
   beq TestCase1745
   br Done

TestCase1745:
   ldc.i4 1845
   stloc.0
   ldc.i4 0xfffff74d
   conv.i4
   not
   ldc.i4 0x8b2
   beq TestCase1744
   br Done

TestCase1744:
   ldc.i4 1844
   stloc.0
   ldc.i4 0x33de
   conv.i4
   not
   ldc.i4 0xffffcc21
   bne.un Done

TestCase1743:
   ldc.i4 1843
   stloc.0
   ldc.i4 0x5496
   conv.i4
   not
   ldc.i4 0xffffab69
   bne.un Done

TestCase1742:
   ldc.i4 1842
   stloc.0
   ldc.i4 0xffff9440
   conv.i4
   not
   ldc.i4 0x6bbf
   beq TestCase1741
   br Done

TestCase1741:
   ldc.i4 1841
   stloc.0
   ldc.i4 0x2a2d
   conv.i4
   not
   ldc.i4 0xffffd5d2
   bne.un Done

TestCase1740:
   ldc.i4 1840
   stloc.0
   ldc.i4 0xffffd088
   conv.i4
   not
   ldc.i4 0x2f77
   bne.un Done

TestCase1739:
   ldc.i4 1839
   stloc.0
   ldc.i4 0x935
   conv.i4
   not
   ldc.i4 0xfffff6ca
   beq TestCase1738
   br Done

TestCase1738:
   ldc.i4 1838
   stloc.0
   ldc.i4 0xffff92cb
   conv.i4
   not
   ldc.i4 0x6d34
   bne.un Done

TestCase1737:
   ldc.i4 1837
   stloc.0
   ldc.i4 0xfffff7a4
   conv.i4
   not
   ldc.i4 0x85b
   bne.un Done

TestCase1736:
   ldc.i4 1836
   stloc.0
   ldc.i4 0xfffff1fb
   conv.i4
   not
   ldc.i4 0xe04
   beq TestCase1735
   br Done

TestCase1735:
   ldc.i4 1835
   stloc.0
   ldc.i4 0xffffbdf9
   conv.i4
   not
   ldc.i4 0x4206
   bne.un Done

TestCase1734:
   ldc.i4 1834
   stloc.0
   ldc.i4 0xffffea9c
   conv.i4
   not
   ldc.i4 0x1563
   beq TestCase1733
   br Done

TestCase1733:
   ldc.i4 1833
   stloc.0
   ldc.i4 0xffff95c8
   conv.i4
   not
   ldc.i4 0x6a37
   bne.un Done

TestCase1732:
   ldc.i4 1832
   stloc.0
   ldc.i4 0xffffe9aa
   conv.i4
   not
   ldc.i4 0x1655
   bne.un Done

TestCase1731:
   ldc.i4 1831
   stloc.0
   ldc.i4 0xffffc492
   conv.i4
   not
   ldc.i4 0x3b6d
   beq TestCase1730
   br Done

TestCase1730:
   ldc.i4 1830
   stloc.0
   ldc.i4 0xffffb7c1
   conv.i4
   not
   ldc.i4 0x483e
   bne.un Done

TestCase1729:
   ldc.i4 1829
   stloc.0
   ldc.i4 0x69c
   conv.i4
   not
   ldc.i4 0xfffff963
   beq TestCase1728
   br Done

TestCase1728:
   ldc.i4 1828
   stloc.0
   ldc.i4 0xffffab13
   conv.i4
   not
   ldc.i4 0x54ec
   bne.un Done

TestCase1727:
   ldc.i4 1827
   stloc.0
   ldc.i4 0x5679
   conv.i4
   not
   ldc.i4 0xffffa986
   beq TestCase1726
   br Done

TestCase1726:
   ldc.i4 1826
   stloc.0
   ldc.i4 0xffffd971
   conv.i4
   not
   ldc.i4 0x268e
   beq TestCase1725
   br Done

TestCase1725:
   ldc.i4 1825
   stloc.0
   ldc.i4 0x50e7
   conv.i4
   not
   ldc.i4 0xffffaf18
   beq TestCase1724
   br Done

TestCase1724:
   ldc.i4 1824
   stloc.0
   ldc.i4 0x5793
   conv.i4
   not
   ldc.i4 0xffffa86c
   bne.un Done

TestCase1723:
   ldc.i4 1823
   stloc.0
   ldc.i4 0xffff9ee1
   conv.i4
   not
   ldc.i4 0x611e
   beq TestCase1722
   br Done

TestCase1722:
   ldc.i4 1822
   stloc.0
   ldc.i4 0xffffc062
   conv.i4
   not
   ldc.i4 0x3f9d
   beq TestCase1721
   br Done

TestCase1721:
   ldc.i4 1821
   stloc.0
   ldc.i4 0xffffb427
   conv.i4
   not
   ldc.i4 0x4bd8
   beq TestCase1720
   br Done

TestCase1720:
   ldc.i4 1820
   stloc.0
   ldc.i4 0xffffbfe1
   conv.i4
   not
   ldc.i4 0x401e
   beq TestCase1719
   br Done

TestCase1719:
   ldc.i4 1819
   stloc.0
   ldc.i4 0xffffd546
   conv.i4
   not
   ldc.i4 0x2ab9
   bne.un Done

TestCase1718:
   ldc.i4 1818
   stloc.0
   ldc.i4 0x3825
   conv.i4
   not
   ldc.i4 0xffffc7da
   bne.un Done

TestCase1717:
   ldc.i4 1817
   stloc.0
   ldc.i4 0xffff815a
   conv.i4
   not
   ldc.i4 0x7ea5
   bne.un Done

TestCase1716:
   ldc.i4 1816
   stloc.0
   ldc.i4 0x3a84
   conv.i4
   not
   ldc.i4 0xffffc57b
   beq TestCase1715
   br Done

TestCase1715:
   ldc.i4 1815
   stloc.0
   ldc.i4 0xffffdf3a
   conv.i4
   not
   ldc.i4 0x20c5
   beq TestCase1714
   br Done

TestCase1714:
   ldc.i4 1814
   stloc.0
   ldc.i4 0xffffed8d
   conv.i4
   not
   ldc.i4 0x1272
   bne.un Done

TestCase1713:
   ldc.i4 1813
   stloc.0
   ldc.i4 0xffff97a1
   conv.i4
   not
   ldc.i4 0x685e
   bne.un Done

TestCase1712:
   ldc.i4 1812
   stloc.0
   ldc.i4 0x221c
   conv.i4
   not
   ldc.i4 0xffffdde3
   bne.un Done

TestCase1711:
   ldc.i4 1811
   stloc.0
   ldc.i4 0xffff9026
   conv.i4
   not
   ldc.i4 0x6fd9
   bne.un Done

TestCase1710:
   ldc.i4 1810
   stloc.0
   ldc.i4 0x3de
   conv.i4
   not
   ldc.i4 0xfffffc21
   beq TestCase1709
   br Done

TestCase1709:
   ldc.i4 1809
   stloc.0
   ldc.i4 0xd82
   conv.i4
   not
   ldc.i4 0xfffff27d
   beq TestCase1708
   br Done

TestCase1708:
   ldc.i4 1808
   stloc.0
   ldc.i4 0xffff9dcd
   conv.i4
   not
   ldc.i4 0x6232
   bne.un Done

TestCase1707:
   ldc.i4 1807
   stloc.0
   ldc.i4 0xffff9b1a
   conv.i4
   not
   ldc.i4 0x64e5
   bne.un Done

TestCase1706:
   ldc.i4 1806
   stloc.0
   ldc.i4 0x36f
   conv.i4
   not
   ldc.i4 0xfffffc90
   beq TestCase1705
   br Done

TestCase1705:
   ldc.i4 1805
   stloc.0
   ldc.i4 0xffff8421
   conv.i4
   not
   ldc.i4 0x7bde
   beq TestCase1704
   br Done

TestCase1704:
   ldc.i4 1804
   stloc.0
   ldc.i4 0xffffa229
   conv.i4
   not
   ldc.i4 0x5dd6
   beq TestCase1703
   br Done

TestCase1703:
   ldc.i4 1803
   stloc.0
   ldc.i4 0xaae
   conv.i4
   not
   ldc.i4 0xfffff551
   bne.un Done

TestCase1702:
   ldc.i4 1802
   stloc.0
   ldc.i4 0x7dc8
   conv.i4
   not
   ldc.i4 0xffff8237
   bne.un Done

TestCase1701:
   ldc.i4 1801
   stloc.0
   ldc.i4 0xffff9f53
   conv.i4
   not
   ldc.i4 0x60ac
   bne.un Done

TestCase1700:
   ldc.i4 1800
   stloc.0
   ldc.i4 0x6add
   conv.i4
   not
   ldc.i4 0xffff9522
   bne.un Done

TestCase1699:
   ldc.i4 1799
   stloc.0
   ldc.i4 0xffffc07a
   conv.i4
   not
   ldc.i4 0x3f85
   beq TestCase1698
   br Done

TestCase1698:
   ldc.i4 1798
   stloc.0
   ldc.i4 0xffffa5a8
   conv.i4
   not
   ldc.i4 0x5a57
   bne.un Done

TestCase1697:
   ldc.i4 1797
   stloc.0
   ldc.i4 0xfffff314
   conv.i4
   not
   ldc.i4 0xceb
   bne.un Done

TestCase1696:
   ldc.i4 1796
   stloc.0
   ldc.i4 0x2c37
   conv.i4
   not
   ldc.i4 0xffffd3c8
   beq TestCase1695
   br Done

TestCase1695:
   ldc.i4 1795
   stloc.0
   ldc.i4 0x3f4e
   conv.i4
   not
   ldc.i4 0xffffc0b1
   bne.un Done

TestCase1694:
   ldc.i4 1794
   stloc.0
   ldc.i4 0xffffc23e
   conv.i4
   not
   ldc.i4 0x3dc1
   beq TestCase1693
   br Done

TestCase1693:
   ldc.i4 1793
   stloc.0
   ldc.i4 0xfffffc03
   conv.i4
   not
   ldc.i4 0x3fc
   bne.un Done

TestCase1692:
   ldc.i4 1792
   stloc.0
   ldc.i4 0x5774
   conv.i4
   not
   ldc.i4 0xffffa88b
   bne.un Done

TestCase1691:
   ldc.i4 1791
   stloc.0
   ldc.i4 0xffffa250
   conv.i4
   not
   ldc.i4 0x5daf
   beq TestCase1690
   br Done

TestCase1690:
   ldc.i4 1790
   stloc.0
   ldc.i4 0x3589
   conv.i4
   not
   ldc.i4 0xffffca76
   bne.un Done

TestCase1689:
   ldc.i4 1789
   stloc.0
   ldc.i4 0x707f
   conv.i4
   not
   ldc.i4 0xffff8f80
   beq TestCase1688
   br Done

TestCase1688:
   ldc.i4 1788
   stloc.0
   ldc.i4 0x3197
   conv.i4
   not
   ldc.i4 0xffffce68
   bne.un Done

TestCase1687:
   ldc.i4 1787
   stloc.0
   ldc.i4 0x6c99
   conv.i4
   not
   ldc.i4 0xffff9366
   bne.un Done

TestCase1686:
   ldc.i4 1786
   stloc.0
   ldc.i4 0x3900
   conv.i4
   not
   ldc.i4 0xffffc6ff
   beq TestCase1685
   br Done

TestCase1685:
   ldc.i4 1785
   stloc.0
   ldc.i4 0x1b6b
   conv.i4
   not
   ldc.i4 0xffffe494
   bne.un Done

TestCase1684:
   ldc.i4 1784
   stloc.0
   ldc.i4 0xffffb0e3
   conv.i4
   not
   ldc.i4 0x4f1c
   bne.un Done

TestCase1683:
   ldc.i4 1783
   stloc.0
   ldc.i4 0xaf5
   conv.i4
   not
   ldc.i4 0xfffff50a
   beq TestCase1682
   br Done

TestCase1682:
   ldc.i4 1782
   stloc.0
   ldc.i4 0x2b71
   conv.i4
   not
   ldc.i4 0xffffd48e
   beq TestCase1681
   br Done

TestCase1681:
   ldc.i4 1781
   stloc.0
   ldc.i4 0x4172
   conv.i4
   not
   ldc.i4 0xffffbe8d
   bne.un Done

TestCase1680:
   ldc.i4 1780
   stloc.0
   ldc.i4 0xffff9cb8
   conv.i4
   not
   ldc.i4 0x6347
   beq TestCase1679
   br Done

TestCase1679:
   ldc.i4 1779
   stloc.0
   ldc.i4 0x57ae
   conv.i4
   not
   ldc.i4 0xffffa851
   beq TestCase1678
   br Done

TestCase1678:
   ldc.i4 1778
   stloc.0
   ldc.i4 0x4b8b
   conv.i4
   not
   ldc.i4 0xffffb474
   bne.un Done

TestCase1677:
   ldc.i4 1777
   stloc.0
   ldc.i4 0x6296
   conv.i4
   not
   ldc.i4 0xffff9d69
   beq TestCase1676
   br Done

TestCase1676:
   ldc.i4 1776
   stloc.0
   ldc.i4 0xffffd54f
   conv.i4
   not
   ldc.i4 0x2ab0
   beq TestCase1675
   br Done

TestCase1675:
   ldc.i4 1775
   stloc.0
   ldc.i4 0xffffbb1a
   conv.i4
   not
   ldc.i4 0x44e5
   beq TestCase1674
   br Done

TestCase1674:
   ldc.i4 1774
   stloc.0
   ldc.i4 0xffffc3ef
   conv.i4
   not
   ldc.i4 0x3c10
   beq TestCase1673
   br Done

TestCase1673:
   ldc.i4 1773
   stloc.0
   ldc.i4 0xffff97fa
   conv.i4
   not
   ldc.i4 0x6805
   bne.un Done

TestCase1672:
   ldc.i4 1772
   stloc.0
   ldc.i4 0xffffc2f3
   conv.i4
   not
   ldc.i4 0x3d0c
   bne.un Done

TestCase1671:
   ldc.i4 1771
   stloc.0
   ldc.i4 0xfffff88e
   conv.i4
   not
   ldc.i4 0x771
   beq TestCase1670
   br Done

TestCase1670:
   ldc.i4 1770
   stloc.0
   ldc.i4 0x11c0
   conv.i4
   not
   ldc.i4 0xffffee3f
   beq TestCase1669
   br Done

TestCase1669:
   ldc.i4 1769
   stloc.0
   ldc.i4 0xffffc3c1
   conv.i4
   not
   ldc.i4 0x3c3e
   beq TestCase1668
   br Done

TestCase1668:
   ldc.i4 1768
   stloc.0
   ldc.i4 0x523d
   conv.i4
   not
   ldc.i4 0xffffadc2
   beq TestCase1667
   br Done

TestCase1667:
   ldc.i4 1767
   stloc.0
   ldc.i4 0x5e9a
   conv.i4
   not
   ldc.i4 0xffffa165
   bne.un Done

TestCase1666:
   ldc.i4 1766
   stloc.0
   ldc.i4 0xffffed80
   conv.i4
   not
   ldc.i4 0x127f
   bne.un Done

TestCase1665:
   ldc.i4 1765
   stloc.0
   ldc.i4 0xffffcea1
   conv.i4
   not
   ldc.i4 0x315e
   bne.un Done

TestCase1664:
   ldc.i4 1764
   stloc.0
   ldc.i4 0xffffb964
   conv.i4
   not
   ldc.i4 0x469b
   beq TestCase1663
   br Done

TestCase1663:
   ldc.i4 1763
   stloc.0
   ldc.i4 0xffffcff1
   conv.i4
   not
   ldc.i4 0x300e
   bne.un Done

TestCase1662:
   ldc.i4 1762
   stloc.0
   ldc.i4 0x11cf
   conv.i4
   not
   ldc.i4 0xffffee30
   beq TestCase1661
   br Done

TestCase1661:
   ldc.i4 1761
   stloc.0
   ldc.i4 0x51cc
   conv.i4
   not
   ldc.i4 0xffffae33
   beq TestCase1660
   br Done

TestCase1660:
   ldc.i4 1760
   stloc.0
   ldc.i4 0xffff957c
   conv.i4
   not
   ldc.i4 0x6a83
   bne.un Done

TestCase1659:
   ldc.i4 1759
   stloc.0
   ldc.i4 0x342d
   conv.i4
   not
   ldc.i4 0xffffcbd2
   bne.un Done

TestCase1658:
   ldc.i4 1758
   stloc.0
   ldc.i4 0xffffe997
   conv.i4
   not
   ldc.i4 0x1668
   bne.un Done

TestCase1657:
   ldc.i4 1757
   stloc.0
   ldc.i4 0xffff88fb
   conv.i4
   not
   ldc.i4 0x7704
   bne.un Done

TestCase1656:
   ldc.i4 1756
   stloc.0
   ldc.i4 0xffff96df
   conv.i4
   not
   ldc.i4 0x6920
   beq TestCase1655
   br Done

TestCase1655:
   ldc.i4 1755
   stloc.0
   ldc.i4 0x2e71
   conv.i4
   not
   ldc.i4 0xffffd18e
   beq TestCase1654
   br Done

TestCase1654:
   ldc.i4 1754
   stloc.0
   ldc.i4 0x353b
   conv.i4
   not
   ldc.i4 0xffffcac4
   bne.un Done

TestCase1653:
   ldc.i4 1753
   stloc.0
   ldc.i4 0xffffbd39
   conv.i4
   not
   ldc.i4 0x42c6
   beq TestCase1652
   br Done

TestCase1652:
   ldc.i4 1752
   stloc.0
   ldc.i4 0xffffad87
   conv.i4
   not
   ldc.i4 0x5278
   bne.un Done

TestCase1651:
   ldc.i4 1751
   stloc.0
   ldc.i4 0xffff91c3
   conv.i4
   not
   ldc.i4 0x6e3c
   beq TestCase1650
   br Done

TestCase1650:
   ldc.i4 1750
   stloc.0
   ldc.i4 0xffffb276
   conv.i4
   not
   ldc.i4 0x4d89
   bne.un Done

TestCase1649:
   ldc.i4 1749
   stloc.0
   ldc.i4 0xffffa99d
   conv.i4
   not
   ldc.i4 0x5662
   beq TestCase1648
   br Done

TestCase1648:
   ldc.i4 1748
   stloc.0
   ldc.i4 0xffffeb44
   conv.i4
   not
   ldc.i4 0x14bb
   bne.un Done

TestCase1647:
   ldc.i4 1747
   stloc.0
   ldc.i4 0x13ca
   conv.i4
   not
   ldc.i4 0xffffec35
   bne.un Done

TestCase1646:
   ldc.i4 1746
   stloc.0
   ldc.i4 0x125f
   conv.i4
   not
   ldc.i4 0xffffeda0
   bne.un Done

TestCase1645:
   ldc.i4 1745
   stloc.0
   ldc.i4 0xffff9ba2
   conv.i4
   not
   ldc.i4 0x645d
   beq TestCase1644
   br Done

TestCase1644:
   ldc.i4 1744
   stloc.0
   ldc.i4 0x70c8
   conv.i4
   not
   ldc.i4 0xffff8f37
   beq TestCase1643
   br Done

TestCase1643:
   ldc.i4 1743
   stloc.0
   ldc.i4 0x556d
   conv.i4
   not
   ldc.i4 0xffffaa92
   bne.un Done

TestCase1642:
   ldc.i4 1742
   stloc.0
   ldc.i4 0xd35
   conv.i4
   not
   ldc.i4 0xfffff2ca
   bne.un Done

TestCase1641:
   ldc.i4 1741
   stloc.0
   ldc.i4 0xffff87b6
   conv.i4
   not
   ldc.i4 0x7849
   bne.un Done

TestCase1640:
   ldc.i4 1740
   stloc.0
   ldc.i4 0x35ff
   conv.i4
   not
   ldc.i4 0xffffca00
   beq TestCase1639
   br Done

TestCase1639:
   ldc.i4 1739
   stloc.0
   ldc.i4 0x410d
   conv.i4
   not
   ldc.i4 0xffffbef2
   bne.un Done

TestCase1638:
   ldc.i4 1738
   stloc.0
   ldc.i4 0xfffffa1b
   conv.i4
   not
   ldc.i4 0x5e4
   bne.un Done

TestCase1637:
   ldc.i4 1737
   stloc.0
   ldc.i4 0x10e3
   conv.i4
   not
   ldc.i4 0xffffef1c
   bne.un Done

TestCase1636:
   ldc.i4 1736
   stloc.0
   ldc.i4 0x514d
   conv.i4
   not
   ldc.i4 0xffffaeb2
   beq TestCase1635
   br Done

TestCase1635:
   ldc.i4 1735
   stloc.0
   ldc.i4 0xffffca1d
   conv.i4
   not
   ldc.i4 0x35e2
   bne.un Done

TestCase1634:
   ldc.i4 1734
   stloc.0
   ldc.i4 0xffff8430
   conv.i4
   not
   ldc.i4 0x7bcf
   beq TestCase1633
   br Done

TestCase1633:
   ldc.i4 1733
   stloc.0
   ldc.i4 0x686d
   conv.i4
   not
   ldc.i4 0xffff9792
   bne.un Done

TestCase1632:
   ldc.i4 1732
   stloc.0
   ldc.i4 0x7b73
   conv.i4
   not
   ldc.i4 0xffff848c
   beq TestCase1631
   br Done

TestCase1631:
   ldc.i4 1731
   stloc.0
   ldc.i4 0x1274
   conv.i4
   not
   ldc.i4 0xffffed8b
   beq TestCase1630
   br Done

TestCase1630:
   ldc.i4 1730
   stloc.0
   ldc.i4 0x1c4e
   conv.i4
   not
   ldc.i4 0xffffe3b1
   bne.un Done

TestCase1629:
   ldc.i4 1729
   stloc.0
   ldc.i4 0x74c
   conv.i4
   not
   ldc.i4 0xfffff8b3
   beq TestCase1628
   br Done

TestCase1628:
   ldc.i4 1728
   stloc.0
   ldc.i4 0x1299
   conv.i4
   not
   ldc.i4 0xffffed66
   beq TestCase1627
   br Done

TestCase1627:
   ldc.i4 1727
   stloc.0
   ldc.i4 0xffffad34
   conv.i4
   not
   ldc.i4 0x52cb
   beq TestCase1626
   br Done

TestCase1626:
   ldc.i4 1726
   stloc.0
   ldc.i4 0x6257
   conv.i4
   not
   ldc.i4 0xffff9da8
   beq TestCase1625
   br Done

TestCase1625:
   ldc.i4 1725
   stloc.0
   ldc.i4 0x7ce4
   conv.i4
   not
   ldc.i4 0xffff831b
   beq TestCase1624
   br Done

TestCase1624:
   ldc.i4 1724
   stloc.0
   ldc.i4 0x6b13
   conv.i4
   not
   ldc.i4 0xffff94ec
   beq TestCase1623
   br Done

TestCase1623:
   ldc.i4 1723
   stloc.0
   ldc.i4 0xffffe504
   conv.i4
   not
   ldc.i4 0x1afb
   bne.un Done

TestCase1622:
   ldc.i4 1722
   stloc.0
   ldc.i4 0xffffb491
   conv.i4
   not
   ldc.i4 0x4b6e
   bne.un Done

TestCase1621:
   ldc.i4 1721
   stloc.0
   ldc.i4 0x5ab3
   conv.i4
   not
   ldc.i4 0xffffa54c
   beq TestCase1620
   br Done

TestCase1620:
   ldc.i4 1720
   stloc.0
   ldc.i4 0x2372
   conv.i4
   not
   ldc.i4 0xffffdc8d
   bne.un Done

TestCase1619:
   ldc.i4 1719
   stloc.0
   ldc.i4 0xffff962a
   conv.i4
   not
   ldc.i4 0x69d5
   bne.un Done

TestCase1618:
   ldc.i4 1718
   stloc.0
   ldc.i4 0xffffb22a
   conv.i4
   not
   ldc.i4 0x4dd5
   bne.un Done

TestCase1617:
   ldc.i4 1717
   stloc.0
   ldc.i4 0x4dab
   conv.i4
   not
   ldc.i4 0xffffb254
   beq TestCase1616
   br Done

TestCase1616:
   ldc.i4 1716
   stloc.0
   ldc.i4 0xffffb832
   conv.i4
   not
   ldc.i4 0x47cd
   bne.un Done

TestCase1615:
   ldc.i4 1715
   stloc.0
   ldc.i4 0x6fe6
   conv.i4
   not
   ldc.i4 0xffff9019
   beq TestCase1614
   br Done

TestCase1614:
   ldc.i4 1714
   stloc.0
   ldc.i4 0xffff8410
   conv.i4
   not
   ldc.i4 0x7bef
   bne.un Done

TestCase1613:
   ldc.i4 1713
   stloc.0
   ldc.i4 0x71d5
   conv.i4
   not
   ldc.i4 0xffff8e2a
   beq TestCase1612
   br Done

TestCase1612:
   ldc.i4 1712
   stloc.0
   ldc.i4 0xffffafaf
   conv.i4
   not
   ldc.i4 0x5050
   bne.un Done

TestCase1611:
   ldc.i4 1711
   stloc.0
   ldc.i4 0xffffb94d
   conv.i4
   not
   ldc.i4 0x46b2
   beq TestCase1610
   br Done

TestCase1610:
   ldc.i4 1710
   stloc.0
   ldc.i4 0xffffff0e
   conv.i4
   not
   ldc.i4 0xf1
   bne.un Done

TestCase1609:
   ldc.i4 1709
   stloc.0
   ldc.i4 0x3c23
   conv.i4
   not
   ldc.i4 0xffffc3dc
   bne.un Done

TestCase1608:
   ldc.i4 1708
   stloc.0
   ldc.i4 0xffffb8f9
   conv.i4
   not
   ldc.i4 0x4706
   bne.un Done

TestCase1607:
   ldc.i4 1707
   stloc.0
   ldc.i4 0xfffffbca
   conv.i4
   not
   ldc.i4 0x435
   bne.un Done

TestCase1606:
   ldc.i4 1706
   stloc.0
   ldc.i4 0x62ce
   conv.i4
   not
   ldc.i4 0xffff9d31
   beq TestCase1605
   br Done

TestCase1605:
   ldc.i4 1705
   stloc.0
   ldc.i4 0x1694
   conv.i4
   not
   ldc.i4 0xffffe96b
   bne.un Done

TestCase1604:
   ldc.i4 1704
   stloc.0
   ldc.i4 0xffffdf1c
   conv.i4
   not
   ldc.i4 0x20e3
   bne.un Done

TestCase1603:
   ldc.i4 1703
   stloc.0
   ldc.i4 0x669a
   conv.i4
   not
   ldc.i4 0xffff9965
   bne.un Done

TestCase1602:
   ldc.i4 1702
   stloc.0
   ldc.i4 0x355f
   conv.i4
   not
   ldc.i4 0xffffcaa0
   beq TestCase1601
   br Done

TestCase1601:
   ldc.i4 1701
   stloc.0
   ldc.i4 0xfffff573
   conv.i4
   not
   ldc.i4 0xa8c
   beq TestCase1600
   br Done

TestCase1600:
   ldc.i4 1700
   stloc.0
   ldc.i4 0xffffe9ea
   conv.i4
   not
   ldc.i4 0x1615
   bne.un Done

TestCase1599:
   ldc.i4 1699
   stloc.0
   ldc.i4 0x1435
   conv.i4
   not
   ldc.i4 0xffffebca
   beq TestCase1598
   br Done

TestCase1598:
   ldc.i4 1698
   stloc.0
   ldc.i4 0x2ad8
   conv.i4
   not
   ldc.i4 0xffffd527
   bne.un Done

TestCase1597:
   ldc.i4 1697
   stloc.0
   ldc.i4 0x1576
   conv.i4
   not
   ldc.i4 0xffffea89
   beq TestCase1596
   br Done

TestCase1596:
   ldc.i4 1696
   stloc.0
   ldc.i4 0xfffffa74
   conv.i4
   not
   ldc.i4 0x58b
   beq TestCase1595
   br Done

TestCase1595:
   ldc.i4 1695
   stloc.0
   ldc.i4 0xffffd439
   conv.i4
   not
   ldc.i4 0x2bc6
   bne.un Done

TestCase1594:
   ldc.i4 1694
   stloc.0
   ldc.i4 0x2d69
   conv.i4
   not
   ldc.i4 0xffffd296
   bne.un Done

TestCase1593:
   ldc.i4 1693
   stloc.0
   ldc.i4 0x394
   conv.i4
   not
   ldc.i4 0xfffffc6b
   beq TestCase1592
   br Done

TestCase1592:
   ldc.i4 1692
   stloc.0
   ldc.i4 0xffffd03a
   conv.i4
   not
   ldc.i4 0x2fc5
   bne.un Done

TestCase1591:
   ldc.i4 1691
   stloc.0
   ldc.i4 0xffffdf0f
   conv.i4
   not
   ldc.i4 0x20f0
   beq TestCase1590
   br Done

TestCase1590:
   ldc.i4 1690
   stloc.0
   ldc.i4 0x6e33
   conv.i4
   not
   ldc.i4 0xffff91cc
   bne.un Done

TestCase1589:
   ldc.i4 1689
   stloc.0
   ldc.i4 0x6d26
   conv.i4
   not
   ldc.i4 0xffff92d9
   bne.un Done

TestCase1588:
   ldc.i4 1688
   stloc.0
   ldc.i4 0x54de
   conv.i4
   not
   ldc.i4 0xffffab21
   bne.un Done

TestCase1587:
   ldc.i4 1687
   stloc.0
   ldc.i4 0xffffdf08
   conv.i4
   not
   ldc.i4 0x20f7
   bne.un Done

TestCase1586:
   ldc.i4 1686
   stloc.0
   ldc.i4 0xffffea92
   conv.i4
   not
   ldc.i4 0x156d
   beq TestCase1585
   br Done

TestCase1585:
   ldc.i4 1685
   stloc.0
   ldc.i4 0x31c2
   conv.i4
   not
   ldc.i4 0xffffce3d
   bne.un Done

TestCase1584:
   ldc.i4 1684
   stloc.0
   ldc.i4 0xfffffe29
   conv.i4
   not
   ldc.i4 0x1d6
   beq TestCase1583
   br Done

TestCase1583:
   ldc.i4 1683
   stloc.0
   ldc.i4 0x7bba
   conv.i4
   not
   ldc.i4 0xffff8445
   beq TestCase1582
   br Done

TestCase1582:
   ldc.i4 1682
   stloc.0
   ldc.i4 0xffff8028
   conv.i4
   not
   ldc.i4 0x7fd7
   bne.un Done

TestCase1581:
   ldc.i4 1681
   stloc.0
   ldc.i4 0xdf0
   conv.i4
   not
   ldc.i4 0xfffff20f
   bne.un Done

TestCase1580:
   ldc.i4 1680
   stloc.0
   ldc.i4 0xffffe6dc
   conv.i4
   not
   ldc.i4 0x1923
   beq TestCase1579
   br Done

TestCase1579:
   ldc.i4 1679
   stloc.0
   ldc.i4 0x4cf0
   conv.i4
   not
   ldc.i4 0xffffb30f
   bne.un Done

TestCase1578:
   ldc.i4 1678
   stloc.0
   ldc.i4 0x3560
   conv.i4
   not
   ldc.i4 0xffffca9f
   bne.un Done

TestCase1577:
   ldc.i4 1677
   stloc.0
   ldc.i4 0x2707
   conv.i4
   not
   ldc.i4 0xffffd8f8
   bne.un Done

TestCase1576:
   ldc.i4 1676
   stloc.0
   ldc.i4 0x627b
   conv.i4
   not
   ldc.i4 0xffff9d84
   bne.un Done

TestCase1575:
   ldc.i4 1675
   stloc.0
   ldc.i4 0xffff9e5b
   conv.i4
   not
   ldc.i4 0x61a4
   bne.un Done

TestCase1574:
   ldc.i4 1674
   stloc.0
   ldc.i4 0xfffffaed
   conv.i4
   not
   ldc.i4 0x512
   bne.un Done

TestCase1573:
   ldc.i4 1673
   stloc.0
   ldc.i4 0x2948
   conv.i4
   not
   ldc.i4 0xffffd6b7
   bne.un Done

TestCase1572:
   ldc.i4 1672
   stloc.0
   ldc.i4 0xffffd225
   conv.i4
   not
   ldc.i4 0x2dda
   bne.un Done

TestCase1571:
   ldc.i4 1671
   stloc.0
   ldc.i4 0x15f6
   conv.i4
   not
   ldc.i4 0xffffea09
   bne.un Done

TestCase1570:
   ldc.i4 1670
   stloc.0
   ldc.i4 0xffff8dba
   conv.i4
   not
   ldc.i4 0x7245
   bne.un Done

TestCase1569:
   ldc.i4 1669
   stloc.0
   ldc.i4 0xffffabc8
   conv.i4
   not
   ldc.i4 0x5437
   bne.un Done

TestCase1568:
   ldc.i4 1668
   stloc.0
   ldc.i4 0xffffaa25
   conv.i4
   not
   ldc.i4 0x55da
   bne.un Done

TestCase1567:
   ldc.i4 1667
   stloc.0
   ldc.i4 0xffffd8f6
   conv.i4
   not
   ldc.i4 0x2709
   beq TestCase1566
   br Done

TestCase1566:
   ldc.i4 1666
   stloc.0
   ldc.i4 0xffffa0fc
   conv.i4
   not
   ldc.i4 0x5f03
   beq TestCase1565
   br Done

TestCase1565:
   ldc.i4 1665
   stloc.0
   ldc.i4 0x1142
   conv.i4
   not
   ldc.i4 0xffffeebd
   bne.un Done

TestCase1564:
   ldc.i4 1664
   stloc.0
   ldc.i4 0xffffd2cb
   conv.i4
   not
   ldc.i4 0x2d34
   bne.un Done

TestCase1563:
   ldc.i4 1663
   stloc.0
   ldc.i4 0x3554
   conv.i4
   not
   ldc.i4 0xffffcaab
   beq TestCase1562
   br Done

TestCase1562:
   ldc.i4 1662
   stloc.0
   ldc.i4 0x1fb9
   conv.i4
   not
   ldc.i4 0xffffe046
   beq TestCase1561
   br Done

TestCase1561:
   ldc.i4 1661
   stloc.0
   ldc.i4 0x708c
   conv.i4
   not
   ldc.i4 0xffff8f73
   beq TestCase1560
   br Done

TestCase1560:
   ldc.i4 1660
   stloc.0
   ldc.i4 0xffffdc6e
   conv.i4
   not
   ldc.i4 0x2391
   bne.un Done

TestCase1559:
   ldc.i4 1659
   stloc.0
   ldc.i4 0x7308
   conv.i4
   not
   ldc.i4 0xffff8cf7
   bne.un Done

TestCase1558:
   ldc.i4 1658
   stloc.0
   ldc.i4 0x641f
   conv.i4
   not
   ldc.i4 0xffff9be0
   bne.un Done

TestCase1557:
   ldc.i4 1657
   stloc.0
   ldc.i4 0x2819
   conv.i4
   not
   ldc.i4 0xffffd7e6
   bne.un Done

TestCase1556:
   ldc.i4 1656
   stloc.0
   ldc.i4 0x2d96
   conv.i4
   not
   ldc.i4 0xffffd269
   beq TestCase1555
   br Done

TestCase1555:
   ldc.i4 1655
   stloc.0
   ldc.i4 0x3adb
   conv.i4
   not
   ldc.i4 0xffffc524
   bne.un Done

TestCase1554:
   ldc.i4 1654
   stloc.0
   ldc.i4 0x2169
   conv.i4
   not
   ldc.i4 0xffffde96
   bne.un Done

TestCase1553:
   ldc.i4 1653
   stloc.0
   ldc.i4 0x711f
   conv.i4
   not
   ldc.i4 0xffff8ee0
   beq TestCase1552
   br Done

TestCase1552:
   ldc.i4 1652
   stloc.0
   ldc.i4 0x3c2e
   conv.i4
   not
   ldc.i4 0xffffc3d1
   beq TestCase1551
   br Done

TestCase1551:
   ldc.i4 1651
   stloc.0
   ldc.i4 0xafe
   conv.i4
   not
   ldc.i4 0xfffff501
   bne.un Done

TestCase1550:
   ldc.i4 1650
   stloc.0
   ldc.i4 0x69ae
   conv.i4
   not
   ldc.i4 0xffff9651
   beq TestCase1549
   br Done

TestCase1549:
   ldc.i4 1649
   stloc.0
   ldc.i4 0x5faa
   conv.i4
   not
   ldc.i4 0xffffa055
   beq TestCase1548
   br Done

TestCase1548:
   ldc.i4 1648
   stloc.0
   ldc.i4 0xffffb0e7
   conv.i4
   not
   ldc.i4 0x4f18
   beq TestCase1547
   br Done

TestCase1547:
   ldc.i4 1647
   stloc.0
   ldc.i4 0x205c
   conv.i4
   not
   ldc.i4 0xffffdfa3
   beq TestCase1546
   br Done

TestCase1546:
   ldc.i4 1646
   stloc.0
   ldc.i4 0x5c98
   conv.i4
   not
   ldc.i4 0xffffa367
   bne.un Done

TestCase1545:
   ldc.i4 1645
   stloc.0
   ldc.i4 0x1071
   conv.i4
   not
   ldc.i4 0xffffef8e
   beq TestCase1544
   br Done

TestCase1544:
   ldc.i4 1644
   stloc.0
   ldc.i4 0x6eb4
   conv.i4
   not
   ldc.i4 0xffff914b
   beq TestCase1543
   br Done

TestCase1543:
   ldc.i4 1643
   stloc.0
   ldc.i4 0xffffd1ee
   conv.i4
   not
   ldc.i4 0x2e11
   bne.un Done

TestCase1542:
   ldc.i4 1642
   stloc.0
   ldc.i4 0xffffe2b4
   conv.i4
   not
   ldc.i4 0x1d4b
   beq TestCase1541
   br Done

TestCase1541:
   ldc.i4 1641
   stloc.0
   ldc.i4 0x48f7
   conv.i4
   not
   ldc.i4 0xffffb708
   beq TestCase1540
   br Done

TestCase1540:
   ldc.i4 1640
   stloc.0
   ldc.i4 0x190a
   conv.i4
   not
   ldc.i4 0xffffe6f5
   beq TestCase1539
   br Done

TestCase1539:
   ldc.i4 1639
   stloc.0
   ldc.i4 0x6d2e
   conv.i4
   not
   ldc.i4 0xffff92d1
   beq TestCase1538
   br Done

TestCase1538:
   ldc.i4 1638
   stloc.0
   ldc.i4 0xffff8665
   conv.i4
   not
   ldc.i4 0x799a
   beq TestCase1537
   br Done

TestCase1537:
   ldc.i4 1637
   stloc.0
   ldc.i4 0xffff9a1a
   conv.i4
   not
   ldc.i4 0x65e5
   beq TestCase1536
   br Done

TestCase1536:
   ldc.i4 1636
   stloc.0
   ldc.i4 0x6e1b
   conv.i4
   not
   ldc.i4 0xffff91e4
   bne.un Done

TestCase1535:
   ldc.i4 1635
   stloc.0
   ldc.i4 0xac
   conv.i4
   not
   ldc.i4 0xffffff53
   beq TestCase1534
   br Done

TestCase1534:
   ldc.i4 1634
   stloc.0
   ldc.i4 0x2e6a
   conv.i4
   not
   ldc.i4 0xffffd195
   bne.un Done

TestCase1533:
   ldc.i4 1633
   stloc.0
   ldc.i4 0x33a7
   conv.i4
   not
   ldc.i4 0xffffcc58
   bne.un Done

TestCase1532:
   ldc.i4 1632
   stloc.0
   ldc.i4 0x276b
   conv.i4
   not
   ldc.i4 0xffffd894
   beq TestCase1531
   br Done

TestCase1531:
   ldc.i4 1631
   stloc.0
   ldc.i4 0x69ad
   conv.i4
   not
   ldc.i4 0xffff9652
   beq TestCase1530
   br Done

TestCase1530:
   ldc.i4 1630
   stloc.0
   ldc.i4 0x755f
   conv.i4
   not
   ldc.i4 0xffff8aa0
   beq TestCase1529
   br Done

TestCase1529:
   ldc.i4 1629
   stloc.0
   ldc.i4 0xfffff2bb
   conv.i4
   not
   ldc.i4 0xd44
   beq TestCase1528
   br Done

TestCase1528:
   ldc.i4 1628
   stloc.0
   ldc.i4 0x26ce
   conv.i4
   not
   ldc.i4 0xffffd931
   bne.un Done

TestCase1527:
   ldc.i4 1627
   stloc.0
   ldc.i4 0xfffff807
   conv.i4
   not
   ldc.i4 0x7f8
   bne.un Done

TestCase1526:
   ldc.i4 1626
   stloc.0
   ldc.i4 0xffffb834
   conv.i4
   not
   ldc.i4 0x47cb
   beq TestCase1525
   br Done

TestCase1525:
   ldc.i4 1625
   stloc.0
   ldc.i4 0x5d83
   conv.i4
   not
   ldc.i4 0xffffa27c
   bne.un Done

TestCase1524:
   ldc.i4 1624
   stloc.0
   ldc.i4 0x2872
   conv.i4
   not
   ldc.i4 0xffffd78d
   beq TestCase1523
   br Done

TestCase1523:
   ldc.i4 1623
   stloc.0
   ldc.i4 0x62b7
   conv.i4
   not
   ldc.i4 0xffff9d48
   bne.un Done

TestCase1522:
   ldc.i4 1622
   stloc.0
   ldc.i4 0x2e0
   conv.i4
   not
   ldc.i4 0xfffffd1f
   beq TestCase1521
   br Done

TestCase1521:
   ldc.i4 1621
   stloc.0
   ldc.i4 0x5e3b
   conv.i4
   not
   ldc.i4 0xffffa1c4
   beq TestCase1520
   br Done

TestCase1520:
   ldc.i4 1620
   stloc.0
   ldc.i4 0x642d
   conv.i4
   not
   ldc.i4 0xffff9bd2
   beq TestCase1519
   br Done

TestCase1519:
   ldc.i4 1619
   stloc.0
   ldc.i4 0x356a
   conv.i4
   not
   ldc.i4 0xffffca95
   bne.un Done

TestCase1518:
   ldc.i4 1618
   stloc.0
   ldc.i4 0xffffa6f5
   conv.i4
   not
   ldc.i4 0x590a
   bne.un Done

TestCase1517:
   ldc.i4 1617
   stloc.0
   ldc.i4 0xffffce07
   conv.i4
   not
   ldc.i4 0x31f8
   beq TestCase1516
   br Done

TestCase1516:
   ldc.i4 1616
   stloc.0
   ldc.i4 0xffffd5f9
   conv.i4
   not
   ldc.i4 0x2a06
   bne.un Done

TestCase1515:
   ldc.i4 1615
   stloc.0
   ldc.i4 0x7d2f
   conv.i4
   not
   ldc.i4 0xffff82d0
   beq TestCase1514
   br Done

TestCase1514:
   ldc.i4 1614
   stloc.0
   ldc.i4 0x3cb1
   conv.i4
   not
   ldc.i4 0xffffc34e
   bne.un Done

TestCase1513:
   ldc.i4 1613
   stloc.0
   ldc.i4 0x33d7
   conv.i4
   not
   ldc.i4 0xffffcc28
   bne.un Done

TestCase1512:
   ldc.i4 1612
   stloc.0
   ldc.i4 0xffffb9f6
   conv.i4
   not
   ldc.i4 0x4609
   beq TestCase1511
   br Done

TestCase1511:
   ldc.i4 1611
   stloc.0
   ldc.i4 0x235
   conv.i4
   not
   ldc.i4 0xfffffdca
   bne.un Done

TestCase1510:
   ldc.i4 1610
   stloc.0
   ldc.i4 0x3012
   conv.i4
   not
   ldc.i4 0xffffcfed
   beq TestCase1509
   br Done

TestCase1509:
   ldc.i4 1609
   stloc.0
   ldc.i4 0x199a
   conv.i4
   not
   ldc.i4 0xffffe665
   bne.un Done

TestCase1508:
   ldc.i4 1608
   stloc.0
   ldc.i4 0x3749
   conv.i4
   not
   ldc.i4 0xffffc8b6
   bne.un Done

TestCase1507:
   ldc.i4 1607
   stloc.0
   ldc.i4 0x197c
   conv.i4
   not
   ldc.i4 0xffffe683
   bne.un Done

TestCase1506:
   ldc.i4 1606
   stloc.0
   ldc.i4 0x2678
   conv.i4
   not
   ldc.i4 0xffffd987
   beq TestCase1505
   br Done

TestCase1505:
   ldc.i4 1605
   stloc.0
   ldc.i4 0xffffbbf2
   conv.i4
   not
   ldc.i4 0x440d
   beq TestCase1504
   br Done

TestCase1504:
   ldc.i4 1604
   stloc.0
   ldc.i4 0x62e
   conv.i4
   not
   ldc.i4 0xfffff9d1
   beq TestCase1503
   br Done

TestCase1503:
   ldc.i4 1603
   stloc.0
   ldc.i4 0x1ad1
   conv.i4
   not
   ldc.i4 0xffffe52e
   bne.un Done

TestCase1502:
   ldc.i4 1602
   stloc.0
   ldc.i4 0x62a3
   conv.i4
   not
   ldc.i4 0xffff9d5c
   bne.un Done

TestCase1501:
   ldc.i4 1601
   stloc.0
   ldc.i4 0x599e
   conv.i4
   not
   ldc.i4 0xffffa661
   beq TestCase1500
   br Done

TestCase1500:
   ldc.i4 1600
   stloc.0
   ldc.i4 0x4608
   conv.i4
   not
   ldc.i4 0xffffb9f7
   beq TestCase1499
   br Done

TestCase1499:
   ldc.i4 1599
   stloc.0
   ldc.i4 0xffffb8f7
   conv.i4
   not
   ldc.i4 0x4708
   beq TestCase1498
   br Done

TestCase1498:
   ldc.i4 1598
   stloc.0
   ldc.i4 0xffff818f
   conv.i4
   not
   ldc.i4 0x7e70
   bne.un Done

TestCase1497:
   ldc.i4 1597
   stloc.0
   ldc.i4 0x736
   conv.i4
   not
   ldc.i4 0xfffff8c9
   bne.un Done

TestCase1496:
   ldc.i4 1596
   stloc.0
   ldc.i4 0xfffff22c
   conv.i4
   not
   ldc.i4 0xdd3
   bne.un Done

TestCase1495:
   ldc.i4 1595
   stloc.0
   ldc.i4 0xffffe584
   conv.i4
   not
   ldc.i4 0x1a7b
   beq TestCase1494
   br Done

TestCase1494:
   ldc.i4 1594
   stloc.0
   ldc.i4 0xfffff82f
   conv.i4
   not
   ldc.i4 0x7d0
   beq TestCase1493
   br Done

TestCase1493:
   ldc.i4 1593
   stloc.0
   ldc.i4 0xfffffca3
   conv.i4
   not
   ldc.i4 0x35c
   bne.un Done

TestCase1492:
   ldc.i4 1592
   stloc.0
   ldc.i4 0x744a
   conv.i4
   not
   ldc.i4 0xffff8bb5
   bne.un Done

TestCase1491:
   ldc.i4 1591
   stloc.0
   ldc.i4 0x47c4
   conv.i4
   not
   ldc.i4 0xffffb83b
   beq TestCase1490
   br Done

TestCase1490:
   ldc.i4 1590
   stloc.0
   ldc.i4 0x5ee9
   conv.i4
   not
   ldc.i4 0xffffa116
   beq TestCase1489
   br Done

TestCase1489:
   ldc.i4 1589
   stloc.0
   ldc.i4 0xffffd073
   conv.i4
   not
   ldc.i4 0x2f8c
   beq TestCase1488
   br Done

TestCase1488:
   ldc.i4 1588
   stloc.0
   ldc.i4 0x5b2a
   conv.i4
   not
   ldc.i4 0xffffa4d5
   beq TestCase1487
   br Done

TestCase1487:
   ldc.i4 1587
   stloc.0
   ldc.i4 0xffffa949
   conv.i4
   not
   ldc.i4 0x56b6
   beq TestCase1486
   br Done

TestCase1486:
   ldc.i4 1586
   stloc.0
   ldc.i4 0xffffbe7b
   conv.i4
   not
   ldc.i4 0x4184
   bne.un Done

TestCase1485:
   ldc.i4 1585
   stloc.0
   ldc.i4 0xffff8c2b
   conv.i4
   not
   ldc.i4 0x73d4
   beq TestCase1484
   br Done

TestCase1484:
   ldc.i4 1584
   stloc.0
   ldc.i4 0xffffea38
   conv.i4
   not
   ldc.i4 0x15c7
   beq TestCase1483
   br Done

TestCase1483:
   ldc.i4 1583
   stloc.0
   ldc.i4 0xffffdc5f
   conv.i4
   not
   ldc.i4 0x23a0
   beq TestCase1482
   br Done

TestCase1482:
   ldc.i4 1582
   stloc.0
   ldc.i4 0xffffd03d
   conv.i4
   not
   ldc.i4 0x2fc2
   bne.un Done

TestCase1481:
   ldc.i4 1581
   stloc.0
   ldc.i4 0x30b2
   conv.i4
   not
   ldc.i4 0xffffcf4d
   beq TestCase1480
   br Done

TestCase1480:
   ldc.i4 1580
   stloc.0
   ldc.i4 0xffffc92b
   conv.i4
   not
   ldc.i4 0x36d4
   bne.un Done

TestCase1479:
   ldc.i4 1579
   stloc.0
   ldc.i4 0x3ac6
   conv.i4
   not
   ldc.i4 0xffffc539
   bne.un Done

TestCase1478:
   ldc.i4 1578
   stloc.0
   ldc.i4 0x15fc
   conv.i4
   not
   ldc.i4 0xffffea03
   beq TestCase1477
   br Done

TestCase1477:
   ldc.i4 1577
   stloc.0
   ldc.i4 0xffff9cd7
   conv.i4
   not
   ldc.i4 0x6328
   beq TestCase1476
   br Done

TestCase1476:
   ldc.i4 1576
   stloc.0
   ldc.i4 0x1170
   conv.i4
   not
   ldc.i4 0xffffee8f
   beq TestCase1475
   br Done

TestCase1475:
   ldc.i4 1575
   stloc.0
   ldc.i4 0xffffb3e9
   conv.i4
   not
   ldc.i4 0x4c16
   beq TestCase1474
   br Done

TestCase1474:
   ldc.i4 1574
   stloc.0
   ldc.i4 0xffff980b
   conv.i4
   not
   ldc.i4 0x67f4
   beq TestCase1473
   br Done

TestCase1473:
   ldc.i4 1573
   stloc.0
   ldc.i4 0xffffa8d8
   conv.i4
   not
   ldc.i4 0x5727
   beq TestCase1472
   br Done

TestCase1472:
   ldc.i4 1572
   stloc.0
   ldc.i4 0x396c
   conv.i4
   not
   ldc.i4 0xffffc693
   beq TestCase1471
   br Done

TestCase1471:
   ldc.i4 1571
   stloc.0
   ldc.i4 0x3332
   conv.i4
   not
   ldc.i4 0xffffcccd
   bne.un Done

TestCase1470:
   ldc.i4 1570
   stloc.0
   ldc.i4 0x1946
   conv.i4
   not
   ldc.i4 0xffffe6b9
   bne.un Done

TestCase1469:
   ldc.i4 1569
   stloc.0
   ldc.i4 0x41e
   conv.i4
   not
   ldc.i4 0xfffffbe1
   bne.un Done

TestCase1468:
   ldc.i4 1568
   stloc.0
   ldc.i4 0xee2
   conv.i4
   not
   ldc.i4 0xfffff11d
   bne.un Done

TestCase1467:
   ldc.i4 1567
   stloc.0
   ldc.i4 0x4f9f
   conv.i4
   not
   ldc.i4 0xffffb060
   bne.un Done

TestCase1466:
   ldc.i4 1566
   stloc.0
   ldc.i4 0x1f09
   conv.i4
   not
   ldc.i4 0xffffe0f6
   beq TestCase1465
   br Done

TestCase1465:
   ldc.i4 1565
   stloc.0
   ldc.i4 0x6fd8
   conv.i4
   not
   ldc.i4 0xffff9027
   beq TestCase1464
   br Done

TestCase1464:
   ldc.i4 1564
   stloc.0
   ldc.i4 0x20fa
   conv.i4
   not
   ldc.i4 0xffffdf05
   beq TestCase1463
   br Done

TestCase1463:
   ldc.i4 1563
   stloc.0
   ldc.i4 0x2949
   conv.i4
   not
   ldc.i4 0xffffd6b6
   bne.un Done

TestCase1462:
   ldc.i4 1562
   stloc.0
   ldc.i4 0xffff993a
   conv.i4
   not
   ldc.i4 0x66c5
   beq TestCase1461
   br Done

TestCase1461:
   ldc.i4 1561
   stloc.0
   ldc.i4 0xffffcc62
   conv.i4
   not
   ldc.i4 0x339d
   beq TestCase1460
   br Done

TestCase1460:
   ldc.i4 1560
   stloc.0
   ldc.i4 0x73f5
   conv.i4
   not
   ldc.i4 0xffff8c0a
   bne.un Done

TestCase1459:
   ldc.i4 1559
   stloc.0
   ldc.i4 0xffffe2c6
   conv.i4
   not
   ldc.i4 0x1d39
   bne.un Done

TestCase1458:
   ldc.i4 1558
   stloc.0
   ldc.i4 0x6e9e
   conv.i4
   not
   ldc.i4 0xffff9161
   bne.un Done

TestCase1457:
   ldc.i4 1557
   stloc.0
   ldc.i4 0x7f8e
   conv.i4
   not
   ldc.i4 0xffff8071
   beq TestCase1456
   br Done

TestCase1456:
   ldc.i4 1556
   stloc.0
   ldc.i4 0x767e
   conv.i4
   not
   ldc.i4 0xffff8981
   bne.un Done

TestCase1455:
   ldc.i4 1555
   stloc.0
   ldc.i4 0xfffff165
   conv.i4
   not
   ldc.i4 0xe9a
   beq TestCase1454
   br Done

TestCase1454:
   ldc.i4 1554
   stloc.0
   ldc.i4 0x6ab7
   conv.i4
   not
   ldc.i4 0xffff9548
   beq TestCase1453
   br Done

TestCase1453:
   ldc.i4 1553
   stloc.0
   ldc.i4 0xffffc7c9
   conv.i4
   not
   ldc.i4 0x3836
   bne.un Done

TestCase1452:
   ldc.i4 1552
   stloc.0
   ldc.i4 0xffffcdce
   conv.i4
   not
   ldc.i4 0x3231
   beq TestCase1451
   br Done

TestCase1451:
   ldc.i4 1551
   stloc.0
   ldc.i4 0x57ea
   conv.i4
   not
   ldc.i4 0xffffa815
   bne.un Done

TestCase1450:
   ldc.i4 1550
   stloc.0
   ldc.i4 0xffffdc19
   conv.i4
   not
   ldc.i4 0x23e6
   bne.un Done

TestCase1449:
   ldc.i4 1549
   stloc.0
   ldc.i4 0xa79
   conv.i4
   not
   ldc.i4 0xfffff586
   beq TestCase1448
   br Done

TestCase1448:
   ldc.i4 1548
   stloc.0
   ldc.i4 0x36d
   conv.i4
   not
   ldc.i4 0xfffffc92
   beq TestCase1447
   br Done

TestCase1447:
   ldc.i4 1547
   stloc.0
   ldc.i4 0xffff8783
   conv.i4
   not
   ldc.i4 0x787c
   beq TestCase1446
   br Done

TestCase1446:
   ldc.i4 1546
   stloc.0
   ldc.i4 0xffff845d
   conv.i4
   not
   ldc.i4 0x7ba2
   beq TestCase1445
   br Done

TestCase1445:
   ldc.i4 1545
   stloc.0
   ldc.i4 0x119b
   conv.i4
   not
   ldc.i4 0xffffee64
   bne.un Done

TestCase1444:
   ldc.i4 1544
   stloc.0
   ldc.i4 0x6456
   conv.i4
   not
   ldc.i4 0xffff9ba9
   bne.un Done

TestCase1443:
   ldc.i4 1543
   stloc.0
   ldc.i4 0x3a36
   conv.i4
   not
   ldc.i4 0xffffc5c9
   bne.un Done

TestCase1442:
   ldc.i4 1542
   stloc.0
   ldc.i4 0x4129
   conv.i4
   not
   ldc.i4 0xffffbed6
   bne.un Done

TestCase1441:
   ldc.i4 1541
   stloc.0
   ldc.i4 0xffff9453
   conv.i4
   not
   ldc.i4 0x6bac
   beq TestCase1440
   br Done

TestCase1440:
   ldc.i4 1540
   stloc.0
   ldc.i4 0xffffe8d4
   conv.i4
   not
   ldc.i4 0x172b
   beq TestCase1439
   br Done

TestCase1439:
   ldc.i4 1539
   stloc.0
   ldc.i4 0x4b88
   conv.i4
   not
   ldc.i4 0xffffb477
   beq TestCase1438
   br Done

TestCase1438:
   ldc.i4 1538
   stloc.0
   ldc.i4 0xffffbc15
   conv.i4
   not
   ldc.i4 0x43ea
   beq TestCase1437
   br Done

TestCase1437:
   ldc.i4 1537
   stloc.0
   ldc.i4 0x5820
   conv.i4
   not
   ldc.i4 0xffffa7df
   bne.un Done

TestCase1436:
   ldc.i4 1536
   stloc.0
   ldc.i4 0x3bd2
   conv.i4
   not
   ldc.i4 0xffffc42d
   bne.un Done

TestCase1435:
   ldc.i4 1535
   stloc.0
   ldc.i4 0x9a9
   conv.i4
   not
   ldc.i4 0xfffff656
   bne.un Done

TestCase1434:
   ldc.i4 1534
   stloc.0
   ldc.i4 0xffffb0c2
   conv.i4
   not
   ldc.i4 0x4f3d
   beq TestCase1433
   br Done

TestCase1433:
   ldc.i4 1533
   stloc.0
   ldc.i4 0x3ca3
   conv.i4
   not
   ldc.i4 0xffffc35c
   bne.un Done

TestCase1432:
   ldc.i4 1532
   stloc.0
   ldc.i4 0xffffe367
   conv.i4
   not
   ldc.i4 0x1c98
   bne.un Done

TestCase1431:
   ldc.i4 1531
   stloc.0
   ldc.i4 0x7718
   conv.i4
   not
   ldc.i4 0xffff88e7
   bne.un Done

TestCase1430:
   ldc.i4 1530
   stloc.0
   ldc.i4 0xffff84bc
   conv.i4
   not
   ldc.i4 0x7b43
   beq TestCase1429
   br Done

TestCase1429:
   ldc.i4 1529
   stloc.0
   ldc.i4 0xffffd3c1
   conv.i4
   not
   ldc.i4 0x2c3e
   beq TestCase1428
   br Done

TestCase1428:
   ldc.i4 1528
   stloc.0
   ldc.i4 0xffffc1b3
   conv.i4
   not
   ldc.i4 0x3e4c
   beq TestCase1427
   br Done

TestCase1427:
   ldc.i4 1527
   stloc.0
   ldc.i4 0x1a52
   conv.i4
   not
   ldc.i4 0xffffe5ad
   beq TestCase1426
   br Done

TestCase1426:
   ldc.i4 1526
   stloc.0
   ldc.i4 0xffff80e9
   conv.i4
   not
   ldc.i4 0x7f16
   bne.un Done

TestCase1425:
   ldc.i4 1525
   stloc.0
   ldc.i4 0xffffb34b
   conv.i4
   not
   ldc.i4 0x4cb4
   beq TestCase1424
   br Done

TestCase1424:
   ldc.i4 1524
   stloc.0
   ldc.i4 0x7f81
   conv.i4
   not
   ldc.i4 0xffff807e
   bne.un Done

TestCase1423:
   ldc.i4 1523
   stloc.0
   ldc.i4 0xffffbf31
   conv.i4
   not
   ldc.i4 0x40ce
   bne.un Done

TestCase1422:
   ldc.i4 1522
   stloc.0
   ldc.i4 0xffff8fca
   conv.i4
   not
   ldc.i4 0x7035
   beq TestCase1421
   br Done

TestCase1421:
   ldc.i4 1521
   stloc.0
   ldc.i4 0x3617
   conv.i4
   not
   ldc.i4 0xffffc9e8
   bne.un Done

TestCase1420:
   ldc.i4 1520
   stloc.0
   ldc.i4 0x531e
   conv.i4
   not
   ldc.i4 0xfffface1
   bne.un Done

TestCase1419:
   ldc.i4 1519
   stloc.0
   ldc.i4 0xffff9dbd
   conv.i4
   not
   ldc.i4 0x6242
   beq TestCase1418
   br Done

TestCase1418:
   ldc.i4 1518
   stloc.0
   ldc.i4 0x1b34
   conv.i4
   not
   ldc.i4 0xffffe4cb
   bne.un Done

TestCase1417:
   ldc.i4 1517
   stloc.0
   ldc.i4 0x28a5
   conv.i4
   not
   ldc.i4 0xffffd75a
   beq TestCase1416
   br Done

TestCase1416:
   ldc.i4 1516
   stloc.0
   ldc.i4 0xffff9dd7
   conv.i4
   not
   ldc.i4 0x6228
   bne.un Done

TestCase1415:
   ldc.i4 1515
   stloc.0
   ldc.i4 0xffffdfee
   conv.i4
   not
   ldc.i4 0x2011
   bne.un Done

TestCase1414:
   ldc.i4 1514
   stloc.0
   ldc.i4 0x619d
   conv.i4
   not
   ldc.i4 0xffff9e62
   bne.un Done

TestCase1413:
   ldc.i4 1513
   stloc.0
   ldc.i4 0xffffcb17
   conv.i4
   not
   ldc.i4 0x34e8
   bne.un Done

TestCase1412:
   ldc.i4 1512
   stloc.0
   ldc.i4 0xffffdf2c
   conv.i4
   not
   ldc.i4 0x20d3
   beq TestCase1411
   br Done

TestCase1411:
   ldc.i4 1511
   stloc.0
   ldc.i4 0x1d13
   conv.i4
   not
   ldc.i4 0xffffe2ec
   bne.un Done

TestCase1410:
   ldc.i4 1510
   stloc.0
   ldc.i4 0xffffb6d4
   conv.i4
   not
   ldc.i4 0x492b
   beq TestCase1409
   br Done

TestCase1409:
   ldc.i4 1509
   stloc.0
   ldc.i4 0x3215
   conv.i4
   not
   ldc.i4 0xffffcdea
   bne.un Done

TestCase1408:
   ldc.i4 1508
   stloc.0
   ldc.i4 0x22c8
   conv.i4
   not
   ldc.i4 0xffffdd37
   bne.un Done

TestCase1407:
   ldc.i4 1507
   stloc.0
   ldc.i4 0x2989
   conv.i4
   not
   ldc.i4 0xffffd676
   bne.un Done

TestCase1406:
   ldc.i4 1506
   stloc.0
   ldc.i4 0xffffd7d5
   conv.i4
   not
   ldc.i4 0x282a
   bne.un Done

TestCase1405:
   ldc.i4 1505
   stloc.0
   ldc.i4 0xffffe165
   conv.i4
   not
   ldc.i4 0x1e9a
   bne.un Done

TestCase1404:
   ldc.i4 1504
   stloc.0
   ldc.i4 0xffffc550
   conv.i4
   not
   ldc.i4 0x3aaf
   beq TestCase1403
   br Done

TestCase1403:
   ldc.i4 1503
   stloc.0
   ldc.i4 0xffff80a2
   conv.i4
   not
   ldc.i4 0x7f5d
   beq TestCase1402
   br Done

TestCase1402:
   ldc.i4 1502
   stloc.0
   ldc.i4 0x1b29
   conv.i4
   not
   ldc.i4 0xffffe4d6
   beq TestCase1401
   br Done

TestCase1401:
   ldc.i4 1501
   stloc.0
   ldc.i4 0x470e
   conv.i4
   not
   ldc.i4 0xffffb8f1
   bne.un Done

TestCase1400:
   ldc.i4 1500
   stloc.0
   ldc.i4 0xffffcef7
   conv.i4
   not
   ldc.i4 0x3108
   bne.un Done

TestCase1399:
   ldc.i4 1499
   stloc.0
   ldc.i4 0xfffff55f
   conv.i4
   not
   ldc.i4 0xaa0
   beq TestCase1398
   br Done

TestCase1398:
   ldc.i4 1498
   stloc.0
   ldc.i4 0xffffd01e
   conv.i4
   not
   ldc.i4 0x2fe1
   bne.un Done

TestCase1397:
   ldc.i4 1497
   stloc.0
   ldc.i4 0x4f38
   conv.i4
   not
   ldc.i4 0xffffb0c7
   bne.un Done

TestCase1396:
   ldc.i4 1496
   stloc.0
   ldc.i4 0xffffe1d8
   conv.i4
   not
   ldc.i4 0x1e27
   beq TestCase1395
   br Done

TestCase1395:
   ldc.i4 1495
   stloc.0
   ldc.i4 0x7a42
   conv.i4
   not
   ldc.i4 0xffff85bd
   bne.un Done

TestCase1394:
   ldc.i4 1494
   stloc.0
   ldc.i4 0xffff8e96
   conv.i4
   not
   ldc.i4 0x7169
   bne.un Done

TestCase1393:
   ldc.i4 1493
   stloc.0
   ldc.i4 0xffff8685
   conv.i4
   not
   ldc.i4 0x797a
   beq TestCase1392
   br Done

TestCase1392:
   ldc.i4 1492
   stloc.0
   ldc.i4 0x338b
   conv.i4
   not
   ldc.i4 0xffffcc74
   bne.un Done

TestCase1391:
   ldc.i4 1491
   stloc.0
   ldc.i4 0xffffeaf4
   conv.i4
   not
   ldc.i4 0x150b
   beq TestCase1390
   br Done

TestCase1390:
   ldc.i4 1490
   stloc.0
   ldc.i4 0x419c
   conv.i4
   not
   ldc.i4 0xffffbe63
   bne.un Done

TestCase1389:
   ldc.i4 1489
   stloc.0
   ldc.i4 0xffffb794
   conv.i4
   not
   ldc.i4 0x486b
   bne.un Done

TestCase1388:
   ldc.i4 1488
   stloc.0
   ldc.i4 0x64
   conv.i4
   not
   ldc.i4 0xffffff9b
   bne.un Done

TestCase1387:
   ldc.i4 1487
   stloc.0
   ldc.i4 0xee2
   conv.i4
   not
   ldc.i4 0xfffff11d
   bne.un Done

TestCase1386:
   ldc.i4 1486
   stloc.0
   ldc.i4 0xffffd43e
   conv.i4
   not
   ldc.i4 0x2bc1
   beq TestCase1385
   br Done

TestCase1385:
   ldc.i4 1485
   stloc.0
   ldc.i4 0xffffa953
   conv.i4
   not
   ldc.i4 0x56ac
   bne.un Done

TestCase1384:
   ldc.i4 1484
   stloc.0
   ldc.i4 0xffff829e
   conv.i4
   not
   ldc.i4 0x7d61
   beq TestCase1383
   br Done

TestCase1383:
   ldc.i4 1483
   stloc.0
   ldc.i4 0xffffa5c3
   conv.i4
   not
   ldc.i4 0x5a3c
   beq TestCase1382
   br Done

TestCase1382:
   ldc.i4 1482
   stloc.0
   ldc.i4 0x1c85
   conv.i4
   not
   ldc.i4 0xffffe37a
   beq TestCase1381
   br Done

TestCase1381:
   ldc.i4 1481
   stloc.0
   ldc.i4 0xfffffef6
   conv.i4
   not
   ldc.i4 0x109
   bne.un Done

TestCase1380:
   ldc.i4 1480
   stloc.0
   ldc.i4 0xffff943e
   conv.i4
   not
   ldc.i4 0x6bc1
   beq TestCase1379
   br Done

TestCase1379:
   ldc.i4 1479
   stloc.0
   ldc.i4 0xffffce1b
   conv.i4
   not
   ldc.i4 0x31e4
   bne.un Done

TestCase1378:
   ldc.i4 1478
   stloc.0
   ldc.i4 0xffffd4ea
   conv.i4
   not
   ldc.i4 0x2b15
   bne.un Done

TestCase1377:
   ldc.i4 1477
   stloc.0
   ldc.i4 0x3ed7
   conv.i4
   not
   ldc.i4 0xffffc128
   bne.un Done

TestCase1376:
   ldc.i4 1476
   stloc.0
   ldc.i4 0xffffac6a
   conv.i4
   not
   ldc.i4 0x5395
   bne.un Done

TestCase1375:
   ldc.i4 1475
   stloc.0
   ldc.i4 0x56ef
   conv.i4
   not
   ldc.i4 0xffffa910
   beq TestCase1374
   br Done

TestCase1374:
   ldc.i4 1474
   stloc.0
   ldc.i4 0x5f9d
   conv.i4
   not
   ldc.i4 0xffffa062
   beq TestCase1373
   br Done

TestCase1373:
   ldc.i4 1473
   stloc.0
   ldc.i4 0xffffbb78
   conv.i4
   not
   ldc.i4 0x4487
   bne.un Done

TestCase1372:
   ldc.i4 1472
   stloc.0
   ldc.i4 0x2d54
   conv.i4
   not
   ldc.i4 0xffffd2ab
   bne.un Done

TestCase1371:
   ldc.i4 1471
   stloc.0
   ldc.i4 0xffffc10a
   conv.i4
   not
   ldc.i4 0x3ef5
   beq TestCase1370
   br Done

TestCase1370:
   ldc.i4 1470
   stloc.0
   ldc.i4 0x22e1
   conv.i4
   not
   ldc.i4 0xffffdd1e
   bne.un Done

TestCase1369:
   ldc.i4 1469
   stloc.0
   ldc.i4 0xffffb36f
   conv.i4
   not
   ldc.i4 0x4c90
   beq TestCase1368
   br Done

TestCase1368:
   ldc.i4 1468
   stloc.0
   ldc.i4 0xfffffd5f
   conv.i4
   not
   ldc.i4 0x2a0
   bne.un Done

TestCase1367:
   ldc.i4 1467
   stloc.0
   ldc.i4 0x2e9d
   conv.i4
   not
   ldc.i4 0xffffd162
   beq TestCase1366
   br Done

TestCase1366:
   ldc.i4 1466
   stloc.0
   ldc.i4 0xffffbcc8
   conv.i4
   not
   ldc.i4 0x4337
   bne.un Done

TestCase1365:
   ldc.i4 1465
   stloc.0
   ldc.i4 0x2280
   conv.i4
   not
   ldc.i4 0xffffdd7f
   beq TestCase1364
   br Done

TestCase1364:
   ldc.i4 1464
   stloc.0
   ldc.i4 0xffffeda1
   conv.i4
   not
   ldc.i4 0x125e
   beq TestCase1363
   br Done

TestCase1363:
   ldc.i4 1463
   stloc.0
   ldc.i4 0xffff83bc
   conv.i4
   not
   ldc.i4 0x7c43
   bne.un Done

TestCase1362:
   ldc.i4 1462
   stloc.0
   ldc.i4 0x3ab1
   conv.i4
   not
   ldc.i4 0xffffc54e
   bne.un Done

TestCase1361:
   ldc.i4 1461
   stloc.0
   ldc.i4 0xffffb3a8
   conv.i4
   not
   ldc.i4 0x4c57
   beq TestCase1360
   br Done

TestCase1360:
   ldc.i4 1460
   stloc.0
   ldc.i4 0xffffbc0c
   conv.i4
   not
   ldc.i4 0x43f3
   beq TestCase1359
   br Done

TestCase1359:
   ldc.i4 1459
   stloc.0
   ldc.i4 0xffff88f8
   conv.i4
   not
   ldc.i4 0x7707
   bne.un Done

TestCase1358:
   ldc.i4 1458
   stloc.0
   ldc.i4 0xffffdd80
   conv.i4
   not
   ldc.i4 0x227f
   beq TestCase1357
   br Done

TestCase1357:
   ldc.i4 1457
   stloc.0
   ldc.i4 0xffff91d1
   conv.i4
   not
   ldc.i4 0x6e2e
   beq TestCase1356
   br Done

TestCase1356:
   ldc.i4 1456
   stloc.0
   ldc.i4 0xffff8a25
   conv.i4
   not
   ldc.i4 0x75da
   bne.un Done

TestCase1355:
   ldc.i4 1455
   stloc.0
   ldc.i4 0x2e01
   conv.i4
   not
   ldc.i4 0xffffd1fe
   beq TestCase1354
   br Done

TestCase1354:
   ldc.i4 1454
   stloc.0
   ldc.i4 0xffff8057
   conv.i4
   not
   ldc.i4 0x7fa8
   beq TestCase1353
   br Done

TestCase1353:
   ldc.i4 1453
   stloc.0
   ldc.i4 0x440a
   conv.i4
   not
   ldc.i4 0xffffbbf5
   beq TestCase1352
   br Done

TestCase1352:
   ldc.i4 1452
   stloc.0
   ldc.i4 0xffff8c90
   conv.i4
   not
   ldc.i4 0x736f
   bne.un Done

TestCase1351:
   ldc.i4 1451
   stloc.0
   ldc.i4 0xffffec49
   conv.i4
   not
   ldc.i4 0x13b6
   beq TestCase1350
   br Done

TestCase1350:
   ldc.i4 1450
   stloc.0
   ldc.i4 0x54f0
   conv.i4
   not
   ldc.i4 0xffffab0f
   bne.un Done

TestCase1349:
   ldc.i4 1449
   stloc.0
   ldc.i4 0x4df7
   conv.i4
   not
   ldc.i4 0xffffb208
   beq TestCase1348
   br Done

TestCase1348:
   ldc.i4 1448
   stloc.0
   ldc.i4 0x1df8
   conv.i4
   not
   ldc.i4 0xffffe207
   beq TestCase1347
   br Done

TestCase1347:
   ldc.i4 1447
   stloc.0
   ldc.i4 0x39e4
   conv.i4
   not
   ldc.i4 0xffffc61b
   beq TestCase1346
   br Done

TestCase1346:
   ldc.i4 1446
   stloc.0
   ldc.i4 0xffffda01
   conv.i4
   not
   ldc.i4 0x25fe
   bne.un Done

TestCase1345:
   ldc.i4 1445
   stloc.0
   ldc.i4 0xfffff594
   conv.i4
   not
   ldc.i4 0xa6b
   beq TestCase1344
   br Done

TestCase1344:
   ldc.i4 1444
   stloc.0
   ldc.i4 0xffffeb11
   conv.i4
   not
   ldc.i4 0x14ee
   beq TestCase1343
   br Done

TestCase1343:
   ldc.i4 1443
   stloc.0
   ldc.i4 0xffffde90
   conv.i4
   not
   ldc.i4 0x216f
   bne.un Done

TestCase1342:
   ldc.i4 1442
   stloc.0
   ldc.i4 0xffffa827
   conv.i4
   not
   ldc.i4 0x57d8
   bne.un Done

TestCase1341:
   ldc.i4 1441
   stloc.0
   ldc.i4 0xffffa521
   conv.i4
   not
   ldc.i4 0x5ade
   bne.un Done

TestCase1340:
   ldc.i4 1440
   stloc.0
   ldc.i4 0xffff966b
   conv.i4
   not
   ldc.i4 0x6994
   bne.un Done

TestCase1339:
   ldc.i4 1439
   stloc.0
   ldc.i4 0x5aa9
   conv.i4
   not
   ldc.i4 0xffffa556
   beq TestCase1338
   br Done

TestCase1338:
   ldc.i4 1438
   stloc.0
   ldc.i4 0xffffe582
   conv.i4
   not
   ldc.i4 0x1a7d
   bne.un Done

TestCase1337:
   ldc.i4 1437
   stloc.0
   ldc.i4 0xffff915a
   conv.i4
   not
   ldc.i4 0x6ea5
   beq TestCase1336
   br Done

TestCase1336:
   ldc.i4 1436
   stloc.0
   ldc.i4 0xffffd645
   conv.i4
   not
   ldc.i4 0x29ba
   beq TestCase1335
   br Done

TestCase1335:
   ldc.i4 1435
   stloc.0
   ldc.i4 0x71c2
   conv.i4
   not
   ldc.i4 0xffff8e3d
   beq TestCase1334
   br Done

TestCase1334:
   ldc.i4 1434
   stloc.0
   ldc.i4 0x47df
   conv.i4
   not
   ldc.i4 0xffffb820
   beq TestCase1333
   br Done

TestCase1333:
   ldc.i4 1433
   stloc.0
   ldc.i4 0xffffaff0
   conv.i4
   not
   ldc.i4 0x500f
   beq TestCase1332
   br Done

TestCase1332:
   ldc.i4 1432
   stloc.0
   ldc.i4 0x4dc9
   conv.i4
   not
   ldc.i4 0xffffb236
   bne.un Done

TestCase1331:
   ldc.i4 1431
   stloc.0
   ldc.i4 0xffff9c72
   conv.i4
   not
   ldc.i4 0x638d
   bne.un Done

TestCase1330:
   ldc.i4 1430
   stloc.0
   ldc.i4 0xffff9bfd
   conv.i4
   not
   ldc.i4 0x6402
   bne.un Done

TestCase1329:
   ldc.i4 1429
   stloc.0
   ldc.i4 0x6fc3
   conv.i4
   not
   ldc.i4 0xffff903c
   bne.un Done

TestCase1328:
   ldc.i4 1428
   stloc.0
   ldc.i4 0xffffa9ed
   conv.i4
   not
   ldc.i4 0x5612
   bne.un Done

TestCase1327:
   ldc.i4 1427
   stloc.0
   ldc.i4 0xffffe983
   conv.i4
   not
   ldc.i4 0x167c
   beq TestCase1326
   br Done

TestCase1326:
   ldc.i4 1426
   stloc.0
   ldc.i4 0xffffd586
   conv.i4
   not
   ldc.i4 0x2a79
   beq TestCase1325
   br Done

TestCase1325:
   ldc.i4 1425
   stloc.0
   ldc.i4 0x4a57
   conv.i4
   not
   ldc.i4 0xffffb5a8
   beq TestCase1324
   br Done

TestCase1324:
   ldc.i4 1424
   stloc.0
   ldc.i4 0x235d
   conv.i4
   not
   ldc.i4 0xffffdca2
   bne.un Done

TestCase1323:
   ldc.i4 1423
   stloc.0
   ldc.i4 0xffffec2a
   conv.i4
   not
   ldc.i4 0x13d5
   beq TestCase1322
   br Done

TestCase1322:
   ldc.i4 1422
   stloc.0
   ldc.i4 0x5502
   conv.i4
   not
   ldc.i4 0xffffaafd
   beq TestCase1321
   br Done

TestCase1321:
   ldc.i4 1421
   stloc.0
   ldc.i4 0x433
   conv.i4
   not
   ldc.i4 0xfffffbcc
   beq TestCase1320
   br Done

TestCase1320:
   ldc.i4 1420
   stloc.0
   ldc.i4 0xffffa955
   conv.i4
   not
   ldc.i4 0x56aa
   beq TestCase1319
   br Done

TestCase1319:
   ldc.i4 1419
   stloc.0
   ldc.i4 0x7d36
   conv.i4
   not
   ldc.i4 0xffff82c9
   bne.un Done

TestCase1318:
   ldc.i4 1418
   stloc.0
   ldc.i4 0x778
   conv.i4
   not
   ldc.i4 0xfffff887
   bne.un Done

TestCase1317:
   ldc.i4 1417
   stloc.0
   ldc.i4 0x17a8
   conv.i4
   not
   ldc.i4 0xffffe857
   beq TestCase1316
   br Done

TestCase1316:
   ldc.i4 1416
   stloc.0
   ldc.i4 0xffffbbb2
   conv.i4
   not
   ldc.i4 0x444d
   beq TestCase1315
   br Done

TestCase1315:
   ldc.i4 1415
   stloc.0
   ldc.i4 0x7a48
   conv.i4
   not
   ldc.i4 0xffff85b7
   beq TestCase1314
   br Done

TestCase1314:
   ldc.i4 1414
   stloc.0
   ldc.i4 0xd00
   conv.i4
   not
   ldc.i4 0xfffff2ff
   bne.un Done

TestCase1313:
   ldc.i4 1413
   stloc.0
   ldc.i4 0xffff8eb5
   conv.i4
   not
   ldc.i4 0x714a
   bne.un Done

TestCase1312:
   ldc.i4 1412
   stloc.0
   ldc.i4 0x462b
   conv.i4
   not
   ldc.i4 0xffffb9d4
   beq TestCase1311
   br Done

TestCase1311:
   ldc.i4 1411
   stloc.0
   ldc.i4 0x1ac6
   conv.i4
   not
   ldc.i4 0xffffe539
   beq TestCase1310
   br Done

TestCase1310:
   ldc.i4 1410
   stloc.0
   ldc.i4 0x5277
   conv.i4
   not
   ldc.i4 0xffffad88
   beq TestCase1309
   br Done

TestCase1309:
   ldc.i4 1409
   stloc.0
   ldc.i4 0xffffe969
   conv.i4
   not
   ldc.i4 0x1696
   beq TestCase1308
   br Done

TestCase1308:
   ldc.i4 1408
   stloc.0
   ldc.i4 0x794d
   conv.i4
   not
   ldc.i4 0xffff86b2
   beq TestCase1307
   br Done

TestCase1307:
   ldc.i4 1407
   stloc.0
   ldc.i4 0x68af
   conv.i4
   not
   ldc.i4 0xffff9750
   bne.un Done

TestCase1306:
   ldc.i4 1406
   stloc.0
   ldc.i4 0xffffb5e7
   conv.i4
   not
   ldc.i4 0x4a18
   bne.un Done

TestCase1305:
   ldc.i4 1405
   stloc.0
   ldc.i4 0xffffcd3e
   conv.i4
   not
   ldc.i4 0x32c1
   bne.un Done

TestCase1304:
   ldc.i4 1404
   stloc.0
   ldc.i4 0xffffcd53
   conv.i4
   not
   ldc.i4 0x32ac
   beq TestCase1303
   br Done

TestCase1303:
   ldc.i4 1403
   stloc.0
   ldc.i4 0x26c9
   conv.i4
   not
   ldc.i4 0xffffd936
   beq TestCase1302
   br Done

TestCase1302:
   ldc.i4 1402
   stloc.0
   ldc.i4 0x246e
   conv.i4
   not
   ldc.i4 0xffffdb91
   bne.un Done

TestCase1301:
   ldc.i4 1401
   stloc.0
   ldc.i4 0xffffda21
   conv.i4
   not
   ldc.i4 0x25de
   beq TestCase1300
   br Done

TestCase1300:
   ldc.i4 1400
   stloc.0
   ldc.i4 0x3740
   conv.i4
   not
   ldc.i4 0xffffc8bf
   bne.un Done

TestCase1299:
   ldc.i4 1399
   stloc.0
   ldc.i4 0x775a
   conv.i4
   not
   ldc.i4 0xffff88a5
   beq TestCase1298
   br Done

TestCase1298:
   ldc.i4 1398
   stloc.0
   ldc.i4 0xfffff587
   conv.i4
   not
   ldc.i4 0xa78
   bne.un Done

TestCase1297:
   ldc.i4 1397
   stloc.0
   ldc.i4 0xffff8dcb
   conv.i4
   not
   ldc.i4 0x7234
   bne.un Done

TestCase1296:
   ldc.i4 1396
   stloc.0
   ldc.i4 0x341
   conv.i4
   not
   ldc.i4 0xfffffcbe
   beq TestCase1295
   br Done

TestCase1295:
   ldc.i4 1395
   stloc.0
   ldc.i4 0x6722
   conv.i4
   not
   ldc.i4 0xffff98dd
   beq TestCase1294
   br Done

TestCase1294:
   ldc.i4 1394
   stloc.0
   ldc.i4 0xffffbadd
   conv.i4
   not
   ldc.i4 0x4522
   beq TestCase1293
   br Done

TestCase1293:
   ldc.i4 1393
   stloc.0
   ldc.i4 0xffffdc23
   conv.i4
   not
   ldc.i4 0x23dc
   bne.un Done

TestCase1292:
   ldc.i4 1392
   stloc.0
   ldc.i4 0x2cc0
   conv.i4
   not
   ldc.i4 0xffffd33f
   bne.un Done

TestCase1291:
   ldc.i4 1391
   stloc.0
   ldc.i4 0xffffe765
   conv.i4
   not
   ldc.i4 0x189a
   beq TestCase1290
   br Done

TestCase1290:
   ldc.i4 1390
   stloc.0
   ldc.i4 0x7de4
   conv.i4
   not
   ldc.i4 0xffff821b
   bne.un Done

TestCase1289:
   ldc.i4 1389
   stloc.0
   ldc.i4 0xfffff15e
   conv.i4
   not
   ldc.i4 0xea1
   bne.un Done

TestCase1288:
   ldc.i4 1388
   stloc.0
   ldc.i4 0xffffd295
   conv.i4
   not
   ldc.i4 0x2d6a
   beq TestCase1287
   br Done

TestCase1287:
   ldc.i4 1387
   stloc.0
   ldc.i4 0xb73
   conv.i4
   not
   ldc.i4 0xfffff48c
   bne.un Done

TestCase1286:
   ldc.i4 1386
   stloc.0
   ldc.i4 0xfffffb4c
   conv.i4
   not
   ldc.i4 0x4b3
   bne.un Done

TestCase1285:
   ldc.i4 1385
   stloc.0
   ldc.i4 0x7e14
   conv.i4
   not
   ldc.i4 0xffff81eb
   beq TestCase1284
   br Done

TestCase1284:
   ldc.i4 1384
   stloc.0
   ldc.i4 0x1f9b
   conv.i4
   not
   ldc.i4 0xffffe064
   beq TestCase1283
   br Done

TestCase1283:
   ldc.i4 1383
   stloc.0
   ldc.i4 0xffffc3b7
   conv.i4
   not
   ldc.i4 0x3c48
   beq TestCase1282
   br Done

TestCase1282:
   ldc.i4 1382
   stloc.0
   ldc.i4 0x4731
   conv.i4
   not
   ldc.i4 0xffffb8ce
   beq TestCase1281
   br Done

TestCase1281:
   ldc.i4 1381
   stloc.0
   ldc.i4 0xffffcc13
   conv.i4
   not
   ldc.i4 0x33ec
   bne.un Done

TestCase1280:
   ldc.i4 1380
   stloc.0
   ldc.i4 0x3aa1
   conv.i4
   not
   ldc.i4 0xffffc55e
   bne.un Done

TestCase1279:
   ldc.i4 1379
   stloc.0
   ldc.i4 0x18a7
   conv.i4
   not
   ldc.i4 0xffffe758
   beq TestCase1278
   br Done

TestCase1278:
   ldc.i4 1378
   stloc.0
   ldc.i4 0x6510
   conv.i4
   not
   ldc.i4 0xffff9aef
   beq TestCase1277
   br Done

TestCase1277:
   ldc.i4 1377
   stloc.0
   ldc.i4 0xffffaf57
   conv.i4
   not
   ldc.i4 0x50a8
   bne.un Done

TestCase1276:
   ldc.i4 1376
   stloc.0
   ldc.i4 0x1a52
   conv.i4
   not
   ldc.i4 0xffffe5ad
   beq TestCase1275
   br Done

TestCase1275:
   ldc.i4 1375
   stloc.0
   ldc.i4 0xfffffcc0
   conv.i4
   not
   ldc.i4 0x33f
   beq TestCase1274
   br Done

TestCase1274:
   ldc.i4 1374
   stloc.0
   ldc.i4 0xfffff238
   conv.i4
   not
   ldc.i4 0xdc7
   beq TestCase1273
   br Done

TestCase1273:
   ldc.i4 1373
   stloc.0
   ldc.i4 0xfffffda0
   conv.i4
   not
   ldc.i4 0x25f
   beq TestCase1272
   br Done

TestCase1272:
   ldc.i4 1372
   stloc.0
   ldc.i4 0xffffe230
   conv.i4
   not
   ldc.i4 0x1dcf
   beq TestCase1271
   br Done

TestCase1271:
   ldc.i4 1371
   stloc.0
   ldc.i4 0x646b
   conv.i4
   not
   ldc.i4 0xffff9b94
   bne.un Done

TestCase1270:
   ldc.i4 1370
   stloc.0
   ldc.i4 0xffffec83
   conv.i4
   not
   ldc.i4 0x137c
   bne.un Done

TestCase1269:
   ldc.i4 1369
   stloc.0
   ldc.i4 0x15e6
   conv.i4
   not
   ldc.i4 0xffffea19
   bne.un Done

TestCase1268:
   ldc.i4 1368
   stloc.0
   ldc.i4 0x699a
   conv.i4
   not
   ldc.i4 0xffff9665
   beq TestCase1267
   br Done

TestCase1267:
   ldc.i4 1367
   stloc.0
   ldc.i4 0x2de
   conv.i4
   not
   ldc.i4 0xfffffd21
   beq TestCase1266
   br Done

TestCase1266:
   ldc.i4 1366
   stloc.0
   ldc.i4 0x5f2a
   conv.i4
   not
   ldc.i4 0xffffa0d5
   bne.un Done

TestCase1265:
   ldc.i4 1365
   stloc.0
   ldc.i4 0x4625
   conv.i4
   not
   ldc.i4 0xffffb9da
   bne.un Done

TestCase1264:
   ldc.i4 1364
   stloc.0
   ldc.i4 0xffffa72a
   conv.i4
   not
   ldc.i4 0x58d5
   beq TestCase1263
   br Done

TestCase1263:
   ldc.i4 1363
   stloc.0
   ldc.i4 0xffffdf52
   conv.i4
   not
   ldc.i4 0x20ad
   beq TestCase1262
   br Done

TestCase1262:
   ldc.i4 1362
   stloc.0
   ldc.i4 0x3212
   conv.i4
   not
   ldc.i4 0xffffcded
   bne.un Done

TestCase1261:
   ldc.i4 1361
   stloc.0
   ldc.i4 0x65b9
   conv.i4
   not
   ldc.i4 0xffff9a46
   beq TestCase1260
   br Done

TestCase1260:
   ldc.i4 1360
   stloc.0
   ldc.i4 0xffff9623
   conv.i4
   not
   ldc.i4 0x69dc
   bne.un Done

TestCase1259:
   ldc.i4 1359
   stloc.0
   ldc.i4 0xffffe77a
   conv.i4
   not
   ldc.i4 0x1885
   bne.un Done

TestCase1258:
   ldc.i4 1358
   stloc.0
   ldc.i4 0xffff9f98
   conv.i4
   not
   ldc.i4 0x6067
   beq TestCase1257
   br Done

TestCase1257:
   ldc.i4 1357
   stloc.0
   ldc.i4 0xffffc134
   conv.i4
   not
   ldc.i4 0x3ecb
   beq TestCase1256
   br Done

TestCase1256:
   ldc.i4 1356
   stloc.0
   ldc.i4 0xffff8df9
   conv.i4
   not
   ldc.i4 0x7206
   bne.un Done

TestCase1255:
   ldc.i4 1355
   stloc.0
   ldc.i4 0xffff88d9
   conv.i4
   not
   ldc.i4 0x7726
   bne.un Done

TestCase1254:
   ldc.i4 1354
   stloc.0
   ldc.i4 0x713c
   conv.i4
   not
   ldc.i4 0xffff8ec3
   bne.un Done

TestCase1253:
   ldc.i4 1353
   stloc.0
   ldc.i4 0x4534
   conv.i4
   not
   ldc.i4 0xffffbacb
   bne.un Done

TestCase1252:
   ldc.i4 1352
   stloc.0
   ldc.i4 0x24b8
   conv.i4
   not
   ldc.i4 0xffffdb47
   bne.un Done

TestCase1251:
   ldc.i4 1351
   stloc.0
   ldc.i4 0x4d20
   conv.i4
   not
   ldc.i4 0xffffb2df
   beq TestCase1250
   br Done

TestCase1250:
   ldc.i4 1350
   stloc.0
   ldc.i4 0xffffb058
   conv.i4
   not
   ldc.i4 0x4fa7
   beq TestCase1249
   br Done

TestCase1249:
   ldc.i4 1349
   stloc.0
   ldc.i4 0x76ca
   conv.i4
   not
   ldc.i4 0xffff8935
   beq TestCase1248
   br Done

TestCase1248:
   ldc.i4 1348
   stloc.0
   ldc.i4 0xffffc655
   conv.i4
   not
   ldc.i4 0x39aa
   beq TestCase1247
   br Done

TestCase1247:
   ldc.i4 1347
   stloc.0
   ldc.i4 0x34cd
   conv.i4
   not
   ldc.i4 0xffffcb32
   beq TestCase1246
   br Done

TestCase1246:
   ldc.i4 1346
   stloc.0
   ldc.i4 0xffff86c5
   conv.i4
   not
   ldc.i4 0x793a
   beq TestCase1245
   br Done

TestCase1245:
   ldc.i4 1345
   stloc.0
   ldc.i4 0xfffff9f4
   conv.i4
   not
   ldc.i4 0x60b
   bne.un Done

TestCase1244:
   ldc.i4 1344
   stloc.0
   ldc.i4 0xffffc183
   conv.i4
   not
   ldc.i4 0x3e7c
   beq TestCase1243
   br Done

TestCase1243:
   ldc.i4 1343
   stloc.0
   ldc.i4 0x4894
   conv.i4
   not
   ldc.i4 0xffffb76b
   beq TestCase1242
   br Done

TestCase1242:
   ldc.i4 1342
   stloc.0
   ldc.i4 0x55a6
   conv.i4
   not
   ldc.i4 0xffffaa59
   bne.un Done

TestCase1241:
   ldc.i4 1341
   stloc.0
   ldc.i4 0x2af6
   conv.i4
   not
   ldc.i4 0xffffd509
   beq TestCase1240
   br Done

TestCase1240:
   ldc.i4 1340
   stloc.0
   ldc.i4 0xffffa084
   conv.i4
   not
   ldc.i4 0x5f7b
   bne.un Done

TestCase1239:
   ldc.i4 1339
   stloc.0
   ldc.i4 0x55e
   conv.i4
   not
   ldc.i4 0xfffffaa1
   beq TestCase1238
   br Done

TestCase1238:
   ldc.i4 1338
   stloc.0
   ldc.i4 0x23a4
   conv.i4
   not
   ldc.i4 0xffffdc5b
   bne.un Done

TestCase1237:
   ldc.i4 1337
   stloc.0
   ldc.i4 0xffffc9e4
   conv.i4
   not
   ldc.i4 0x361b
   beq TestCase1236
   br Done

TestCase1236:
   ldc.i4 1336
   stloc.0
   ldc.i4 0x33b0
   conv.i4
   not
   ldc.i4 0xffffcc4f
   beq TestCase1235
   br Done

TestCase1235:
   ldc.i4 1335
   stloc.0
   ldc.i4 0x5554
   conv.i4
   not
   ldc.i4 0xffffaaab
   beq TestCase1234
   br Done

TestCase1234:
   ldc.i4 1334
   stloc.0
   ldc.i4 0x272e
   conv.i4
   not
   ldc.i4 0xffffd8d1
   bne.un Done

TestCase1233:
   ldc.i4 1333
   stloc.0
   ldc.i4 0xffffbdf5
   conv.i4
   not
   ldc.i4 0x420a
   beq TestCase1232
   br Done

TestCase1232:
   ldc.i4 1332
   stloc.0
   ldc.i4 0x582a
   conv.i4
   not
   ldc.i4 0xffffa7d5
   beq TestCase1231
   br Done

TestCase1231:
   ldc.i4 1331
   stloc.0
   ldc.i4 0xffffc125
   conv.i4
   not
   ldc.i4 0x3eda
   bne.un Done

TestCase1230:
   ldc.i4 1330
   stloc.0
   ldc.i4 0x4df
   conv.i4
   not
   ldc.i4 0xfffffb20
   beq TestCase1229
   br Done

TestCase1229:
   ldc.i4 1329
   stloc.0
   ldc.i4 0xffffe11e
   conv.i4
   not
   ldc.i4 0x1ee1
   beq TestCase1228
   br Done

TestCase1228:
   ldc.i4 1328
   stloc.0
   ldc.i4 0xffff9974
   conv.i4
   not
   ldc.i4 0x668b
   bne.un Done

TestCase1227:
   ldc.i4 1327
   stloc.0
   ldc.i4 0xffff8c2f
   conv.i4
   not
   ldc.i4 0x73d0
   beq TestCase1226
   br Done

TestCase1226:
   ldc.i4 1326
   stloc.0
   ldc.i4 0x76a5
   conv.i4
   not
   ldc.i4 0xffff895a
   beq TestCase1225
   br Done

TestCase1225:
   ldc.i4 1325
   stloc.0
   ldc.i4 0xffffabd5
   conv.i4
   not
   ldc.i4 0x542a
   beq TestCase1224
   br Done

TestCase1224:
   ldc.i4 1324
   stloc.0
   ldc.i4 0x12d9
   conv.i4
   not
   ldc.i4 0xffffed26
   beq TestCase1223
   br Done

TestCase1223:
   ldc.i4 1323
   stloc.0
   ldc.i4 0x3d08
   conv.i4
   not
   ldc.i4 0xffffc2f7
   bne.un Done

TestCase1222:
   ldc.i4 1322
   stloc.0
   ldc.i4 0xffffba57
   conv.i4
   not
   ldc.i4 0x45a8
   beq TestCase1221
   br Done

TestCase1221:
   ldc.i4 1321
   stloc.0
   ldc.i4 0xffffab01
   conv.i4
   not
   ldc.i4 0x54fe
   beq TestCase1220
   br Done

TestCase1220:
   ldc.i4 1320
   stloc.0
   ldc.i4 0xffff8383
   conv.i4
   not
   ldc.i4 0x7c7c
   bne.un Done

TestCase1219:
   ldc.i4 1319
   stloc.0
   ldc.i4 0xffff81fd
   conv.i4
   not
   ldc.i4 0x7e02
   beq TestCase1218
   br Done

TestCase1218:
   ldc.i4 1318
   stloc.0
   ldc.i4 0xffffca81
   conv.i4
   not
   ldc.i4 0x357e
   bne.un Done

TestCase1217:
   ldc.i4 1317
   stloc.0
   ldc.i4 0xffffbda7
   conv.i4
   not
   ldc.i4 0x4258
   beq TestCase1216
   br Done

TestCase1216:
   ldc.i4 1316
   stloc.0
   ldc.i4 0x7e9f
   conv.i4
   not
   ldc.i4 0xffff8160
   bne.un Done

TestCase1215:
   ldc.i4 1315
   stloc.0
   ldc.i4 0x4cef
   conv.i4
   not
   ldc.i4 0xffffb310
   bne.un Done

TestCase1214:
   ldc.i4 1314
   stloc.0
   ldc.i4 0x4c91
   conv.i4
   not
   ldc.i4 0xffffb36e
   beq TestCase1213
   br Done

TestCase1213:
   ldc.i4 1313
   stloc.0
   ldc.i4 0x57b9
   conv.i4
   not
   ldc.i4 0xffffa846
   bne.un Done

TestCase1212:
   ldc.i4 1312
   stloc.0
   ldc.i4 0xba6
   conv.i4
   not
   ldc.i4 0xfffff459
   beq TestCase1211
   br Done

TestCase1211:
   ldc.i4 1311
   stloc.0
   ldc.i4 0x5ae4
   conv.i4
   not
   ldc.i4 0xffffa51b
   beq TestCase1210
   br Done

TestCase1210:
   ldc.i4 1310
   stloc.0
   ldc.i4 0x25c8
   conv.i4
   not
   ldc.i4 0xffffda37
   beq TestCase1209
   br Done

TestCase1209:
   ldc.i4 1309
   stloc.0
   ldc.i4 0x5dfc
   conv.i4
   not
   ldc.i4 0xffffa203
   beq TestCase1208
   br Done

TestCase1208:
   ldc.i4 1308
   stloc.0
   ldc.i4 0xffff9ff7
   conv.i4
   not
   ldc.i4 0x6008
   bne.un Done

TestCase1207:
   ldc.i4 1307
   stloc.0
   ldc.i4 0xffffd7a4
   conv.i4
   not
   ldc.i4 0x285b
   bne.un Done

TestCase1206:
   ldc.i4 1306
   stloc.0
   ldc.i4 0x377c
   conv.i4
   not
   ldc.i4 0xffffc883
   beq TestCase1205
   br Done

TestCase1205:
   ldc.i4 1305
   stloc.0
   ldc.i4 0x407a
   conv.i4
   not
   ldc.i4 0xffffbf85
   beq TestCase1204
   br Done

TestCase1204:
   ldc.i4 1304
   stloc.0
   ldc.i4 0x185a
   conv.i4
   not
   ldc.i4 0xffffe7a5
   beq TestCase1203
   br Done

TestCase1203:
   ldc.i4 1303
   stloc.0
   ldc.i4 0xffffbe0d
   conv.i4
   not
   ldc.i4 0x41f2
   beq TestCase1202
   br Done

TestCase1202:
   ldc.i4 1302
   stloc.0
   ldc.i4 0xffffc462
   conv.i4
   not
   ldc.i4 0x3b9d
   beq TestCase1201
   br Done

TestCase1201:
   ldc.i4 1301
   stloc.0
   ldc.i4 0xffffdf9f
   conv.i4
   not
   ldc.i4 0x2060
   beq TestCase1200
   br Done

TestCase1200:
   ldc.i4 1300
   stloc.0
   ldc.i4 0x7695
   conv.i4
   not
   ldc.i4 0xffff896a
   beq TestCase1199
   br Done

TestCase1199:
   ldc.i4 1299
   stloc.0
   ldc.i4 0xfffffea0
   conv.i4
   not
   ldc.i4 0x15f
   beq TestCase1198
   br Done

TestCase1198:
   ldc.i4 1298
   stloc.0
   ldc.i4 0x2523
   conv.i4
   not
   ldc.i4 0xffffdadc
   beq TestCase1197
   br Done

TestCase1197:
   ldc.i4 1297
   stloc.0
   ldc.i4 0xfffffdb0
   conv.i4
   not
   ldc.i4 0x24f
   bne.un Done

TestCase1196:
   ldc.i4 1296
   stloc.0
   ldc.i4 0x62a5
   conv.i4
   not
   ldc.i4 0xffff9d5a
   bne.un Done

TestCase1195:
   ldc.i4 1295
   stloc.0
   ldc.i4 0xfffff14d
   conv.i4
   not
   ldc.i4 0xeb2
   bne.un Done

TestCase1194:
   ldc.i4 1294
   stloc.0
   ldc.i4 0x4eb3
   conv.i4
   not
   ldc.i4 0xffffb14c
   bne.un Done

TestCase1193:
   ldc.i4 1293
   stloc.0
   ldc.i4 0xffff8dc9
   conv.i4
   not
   ldc.i4 0x7236
   bne.un Done

TestCase1192:
   ldc.i4 1292
   stloc.0
   ldc.i4 0xffff8dc9
   conv.i4
   not
   ldc.i4 0x7236
   beq TestCase1191
   br Done

TestCase1191:
   ldc.i4 1291
   stloc.0
   ldc.i4 0xffffe230
   conv.i4
   not
   ldc.i4 0x1dcf
   beq TestCase1190
   br Done

TestCase1190:
   ldc.i4 1290
   stloc.0
   ldc.i4 0x4e92
   conv.i4
   not
   ldc.i4 0xffffb16d
   beq TestCase1189
   br Done

TestCase1189:
   ldc.i4 1289
   stloc.0
   ldc.i4 0x396d
   conv.i4
   not
   ldc.i4 0xffffc692
   bne.un Done

TestCase1188:
   ldc.i4 1288
   stloc.0
   ldc.i4 0x3c2
   conv.i4
   not
   ldc.i4 0xfffffc3d
   bne.un Done

TestCase1187:
   ldc.i4 1287
   stloc.0
   ldc.i4 0xffffdbd5
   conv.i4
   not
   ldc.i4 0x242a
   bne.un Done

TestCase1186:
   ldc.i4 1286
   stloc.0
   ldc.i4 0x1bcd
   conv.i4
   not
   ldc.i4 0xffffe432
   beq TestCase1185
   br Done

TestCase1185:
   ldc.i4 1285
   stloc.0
   ldc.i4 0xfffffcee
   conv.i4
   not
   ldc.i4 0x311
   beq TestCase1184
   br Done

TestCase1184:
   ldc.i4 1284
   stloc.0
   ldc.i4 0xffffc72a
   conv.i4
   not
   ldc.i4 0x38d5
   bne.un Done

TestCase1183:
   ldc.i4 1283
   stloc.0
   ldc.i4 0x2c5
   conv.i4
   not
   ldc.i4 0xfffffd3a
   bne.un Done

TestCase1182:
   ldc.i4 1282
   stloc.0
   ldc.i4 0x70a9
   conv.i4
   not
   ldc.i4 0xffff8f56
   beq TestCase1181
   br Done

TestCase1181:
   ldc.i4 1281
   stloc.0
   ldc.i4 0xaa7
   conv.i4
   not
   ldc.i4 0xfffff558
   bne.un Done

TestCase1180:
   ldc.i4 1280
   stloc.0
   ldc.i4 0x5ba4
   conv.i4
   not
   ldc.i4 0xffffa45b
   beq TestCase1179
   br Done

TestCase1179:
   ldc.i4 1279
   stloc.0
   ldc.i4 0x6066
   conv.i4
   not
   ldc.i4 0xffff9f99
   bne.un Done

TestCase1178:
   ldc.i4 1278
   stloc.0
   ldc.i4 0x1648
   conv.i4
   not
   ldc.i4 0xffffe9b7
   beq TestCase1177
   br Done

TestCase1177:
   ldc.i4 1277
   stloc.0
   ldc.i4 0x30eb
   conv.i4
   not
   ldc.i4 0xffffcf14
   bne.un Done

TestCase1176:
   ldc.i4 1276
   stloc.0
   ldc.i4 0xffff8432
   conv.i4
   not
   ldc.i4 0x7bcd
   bne.un Done

TestCase1175:
   ldc.i4 1275
   stloc.0
   ldc.i4 0x301c
   conv.i4
   not
   ldc.i4 0xffffcfe3
   beq TestCase1174
   br Done

TestCase1174:
   ldc.i4 1274
   stloc.0
   ldc.i4 0x59c
   conv.i4
   not
   ldc.i4 0xfffffa63
   bne.un Done

TestCase1173:
   ldc.i4 1273
   stloc.0
   ldc.i4 0xffffed08
   conv.i4
   not
   ldc.i4 0x12f7
   bne.un Done

TestCase1172:
   ldc.i4 1272
   stloc.0
   ldc.i4 0x4e70
   conv.i4
   not
   ldc.i4 0xffffb18f
   bne.un Done

TestCase1171:
   ldc.i4 1271
   stloc.0
   ldc.i4 0x1ff3
   conv.i4
   not
   ldc.i4 0xffffe00c
   bne.un Done

TestCase1170:
   ldc.i4 1270
   stloc.0
   ldc.i4 0x991
   conv.i4
   not
   ldc.i4 0xfffff66e
   bne.un Done

TestCase1169:
   ldc.i4 1269
   stloc.0
   ldc.i4 0xffff8063
   conv.i4
   not
   ldc.i4 0x7f9c
   bne.un Done

TestCase1168:
   ldc.i4 1268
   stloc.0
   ldc.i4 0xffffe292
   conv.i4
   not
   ldc.i4 0x1d6d
   bne.un Done

TestCase1167:
   ldc.i4 1267
   stloc.0
   ldc.i4 0xffff940b
   conv.i4
   not
   ldc.i4 0x6bf4
   beq TestCase1166
   br Done

TestCase1166:
   ldc.i4 1266
   stloc.0
   ldc.i4 0x4e75
   conv.i4
   not
   ldc.i4 0xffffb18a
   bne.un Done

TestCase1165:
   ldc.i4 1265
   stloc.0
   ldc.i4 0xffff9904
   conv.i4
   not
   ldc.i4 0x66fb
   bne.un Done

TestCase1164:
   ldc.i4 1264
   stloc.0
   ldc.i4 0xffffae01
   conv.i4
   not
   ldc.i4 0x51fe
   beq TestCase1163
   br Done

TestCase1163:
   ldc.i4 1263
   stloc.0
   ldc.i4 0x5164
   conv.i4
   not
   ldc.i4 0xffffae9b
   beq TestCase1162
   br Done

TestCase1162:
   ldc.i4 1262
   stloc.0
   ldc.i4 0xfffff556
   conv.i4
   not
   ldc.i4 0xaa9
   beq TestCase1161
   br Done

TestCase1161:
   ldc.i4 1261
   stloc.0
   ldc.i4 0xffffdf99
   conv.i4
   not
   ldc.i4 0x2066
   bne.un Done

TestCase1160:
   ldc.i4 1260
   stloc.0
   ldc.i4 0xffff9986
   conv.i4
   not
   ldc.i4 0x6679
   bne.un Done

TestCase1159:
   ldc.i4 1259
   stloc.0
   ldc.i4 0xffffe1ca
   conv.i4
   not
   ldc.i4 0x1e35
   beq TestCase1158
   br Done

TestCase1158:
   ldc.i4 1258
   stloc.0
   ldc.i4 0x2c8f
   conv.i4
   not
   ldc.i4 0xffffd370
   bne.un Done

TestCase1157:
   ldc.i4 1257
   stloc.0
   ldc.i4 0x2278
   conv.i4
   not
   ldc.i4 0xffffdd87
   bne.un Done

TestCase1156:
   ldc.i4 1256
   stloc.0
   ldc.i4 0x4b5f
   conv.i4
   not
   ldc.i4 0xffffb4a0
   bne.un Done

TestCase1155:
   ldc.i4 1255
   stloc.0
   ldc.i4 0x18d0
   conv.i4
   not
   ldc.i4 0xffffe72f
   bne.un Done

TestCase1154:
   ldc.i4 1254
   stloc.0
   ldc.i4 0xffffc655
   conv.i4
   not
   ldc.i4 0x39aa
   bne.un Done

TestCase1153:
   ldc.i4 1253
   stloc.0
   ldc.i4 0x96f
   conv.i4
   not
   ldc.i4 0xfffff690
   bne.un Done

TestCase1152:
   ldc.i4 1252
   stloc.0
   ldc.i4 0xba8
   conv.i4
   not
   ldc.i4 0xfffff457
   bne.un Done

TestCase1151:
   ldc.i4 1251
   stloc.0
   ldc.i4 0x3c07
   conv.i4
   not
   ldc.i4 0xffffc3f8
   beq TestCase1150
   br Done

TestCase1150:
   ldc.i4 1250
   stloc.0
   ldc.i4 0xffffb4e5
   conv.i4
   not
   ldc.i4 0x4b1a
   beq TestCase1149
   br Done

TestCase1149:
   ldc.i4 1249
   stloc.0
   ldc.i4 0xfffff831
   conv.i4
   not
   ldc.i4 0x7ce
   bne.un Done

TestCase1148:
   ldc.i4 1248
   stloc.0
   ldc.i4 0x3c51
   conv.i4
   not
   ldc.i4 0xffffc3ae
   beq TestCase1147
   br Done

TestCase1147:
   ldc.i4 1247
   stloc.0
   ldc.i4 0xfffffcad
   conv.i4
   not
   ldc.i4 0x352
   beq TestCase1146
   br Done

TestCase1146:
   ldc.i4 1246
   stloc.0
   ldc.i4 0x533d
   conv.i4
   not
   ldc.i4 0xffffacc2
   bne.un Done

TestCase1145:
   ldc.i4 1245
   stloc.0
   ldc.i4 0x1d3b
   conv.i4
   not
   ldc.i4 0xffffe2c4
   beq TestCase1144
   br Done

TestCase1144:
   ldc.i4 1244
   stloc.0
   ldc.i4 0xfffffcdc
   conv.i4
   not
   ldc.i4 0x323
   beq TestCase1143
   br Done

TestCase1143:
   ldc.i4 1243
   stloc.0
   ldc.i4 0x4355
   conv.i4
   not
   ldc.i4 0xffffbcaa
   beq TestCase1142
   br Done

TestCase1142:
   ldc.i4 1242
   stloc.0
   ldc.i4 0x189d
   conv.i4
   not
   ldc.i4 0xffffe762
   beq TestCase1141
   br Done

TestCase1141:
   ldc.i4 1241
   stloc.0
   ldc.i4 0xffffd073
   conv.i4
   not
   ldc.i4 0x2f8c
   bne.un Done

TestCase1140:
   ldc.i4 1240
   stloc.0
   ldc.i4 0xffffbf36
   conv.i4
   not
   ldc.i4 0x40c9
   beq TestCase1139
   br Done

TestCase1139:
   ldc.i4 1239
   stloc.0
   ldc.i4 0xffffb574
   conv.i4
   not
   ldc.i4 0x4a8b
   beq TestCase1138
   br Done

TestCase1138:
   ldc.i4 1238
   stloc.0
   ldc.i4 0x121f
   conv.i4
   not
   ldc.i4 0xffffede0
   beq TestCase1137
   br Done

TestCase1137:
   ldc.i4 1237
   stloc.0
   ldc.i4 0x6237
   conv.i4
   not
   ldc.i4 0xffff9dc8
   beq TestCase1136
   br Done

TestCase1136:
   ldc.i4 1236
   stloc.0
   ldc.i4 0x2a0f
   conv.i4
   not
   ldc.i4 0xffffd5f0
   bne.un Done

TestCase1135:
   ldc.i4 1235
   stloc.0
   ldc.i4 0xffffb9ae
   conv.i4
   not
   ldc.i4 0x4651
   bne.un Done

TestCase1134:
   ldc.i4 1234
   stloc.0
   ldc.i4 0x106d
   conv.i4
   not
   ldc.i4 0xffffef92
   beq TestCase1133
   br Done

TestCase1133:
   ldc.i4 1233
   stloc.0
   ldc.i4 0xffffb822
   conv.i4
   not
   ldc.i4 0x47dd
   bne.un Done

TestCase1132:
   ldc.i4 1232
   stloc.0
   ldc.i4 0x5c74
   conv.i4
   not
   ldc.i4 0xffffa38b
   bne.un Done

TestCase1131:
   ldc.i4 1231
   stloc.0
   ldc.i4 0xffffd7e4
   conv.i4
   not
   ldc.i4 0x281b
   bne.un Done

TestCase1130:
   ldc.i4 1230
   stloc.0
   ldc.i4 0x60a3
   conv.i4
   not
   ldc.i4 0xffff9f5c
   beq TestCase1129
   br Done

TestCase1129:
   ldc.i4 1229
   stloc.0
   ldc.i4 0xffffb5cc
   conv.i4
   not
   ldc.i4 0x4a33
   bne.un Done

TestCase1128:
   ldc.i4 1228
   stloc.0
   ldc.i4 0x39f9
   conv.i4
   not
   ldc.i4 0xffffc606
   beq TestCase1127
   br Done

TestCase1127:
   ldc.i4 1227
   stloc.0
   ldc.i4 0xfffff90f
   conv.i4
   not
   ldc.i4 0x6f0
   bne.un Done

TestCase1126:
   ldc.i4 1226
   stloc.0
   ldc.i4 0xffffabc8
   conv.i4
   not
   ldc.i4 0x5437
   beq TestCase1125
   br Done

TestCase1125:
   ldc.i4 1225
   stloc.0
   ldc.i4 0xffff9bea
   conv.i4
   not
   ldc.i4 0x6415
   beq TestCase1124
   br Done

TestCase1124:
   ldc.i4 1224
   stloc.0
   ldc.i4 0x182c
   conv.i4
   not
   ldc.i4 0xffffe7d3
   bne.un Done

TestCase1123:
   ldc.i4 1223
   stloc.0
   ldc.i4 0x392b
   conv.i4
   not
   ldc.i4 0xffffc6d4
   beq TestCase1122
   br Done

TestCase1122:
   ldc.i4 1222
   stloc.0
   ldc.i4 0xffffa5d0
   conv.i4
   not
   ldc.i4 0x5a2f
   beq TestCase1121
   br Done

TestCase1121:
   ldc.i4 1221
   stloc.0
   ldc.i4 0xfffff61b
   conv.i4
   not
   ldc.i4 0x9e4
   bne.un Done

TestCase1120:
   ldc.i4 1220
   stloc.0
   ldc.i4 0xfffff193
   conv.i4
   not
   ldc.i4 0xe6c
   bne.un Done

TestCase1119:
   ldc.i4 1219
   stloc.0
   ldc.i4 0x226d
   conv.i4
   not
   ldc.i4 0xffffdd92
   bne.un Done

TestCase1118:
   ldc.i4 1218
   stloc.0
   ldc.i4 0xffffea81
   conv.i4
   not
   ldc.i4 0x157e
   beq TestCase1117
   br Done

TestCase1117:
   ldc.i4 1217
   stloc.0
   ldc.i4 0x5c29
   conv.i4
   not
   ldc.i4 0xffffa3d6
   beq TestCase1116
   br Done

TestCase1116:
   ldc.i4 1216
   stloc.0
   ldc.i4 0x6f83
   conv.i4
   not
   ldc.i4 0xffff907c
   beq TestCase1115
   br Done

TestCase1115:
   ldc.i4 1215
   stloc.0
   ldc.i4 0xffff8e1c
   conv.i4
   not
   ldc.i4 0x71e3
   beq TestCase1114
   br Done

TestCase1114:
   ldc.i4 1214
   stloc.0
   ldc.i4 0x1cbf
   conv.i4
   not
   ldc.i4 0xffffe340
   beq TestCase1113
   br Done

TestCase1113:
   ldc.i4 1213
   stloc.0
   ldc.i4 0x63
   conv.i4
   not
   ldc.i4 0xffffff9c
   beq TestCase1112
   br Done

TestCase1112:
   ldc.i4 1212
   stloc.0
   ldc.i4 0xffffc59c
   conv.i4
   not
   ldc.i4 0x3a63
   bne.un Done

TestCase1111:
   ldc.i4 1211
   stloc.0
   ldc.i4 0x4cc1
   conv.i4
   not
   ldc.i4 0xffffb33e
   bne.un Done

TestCase1110:
   ldc.i4 1210
   stloc.0
   ldc.i4 0xffffb0eb
   conv.i4
   not
   ldc.i4 0x4f14
   bne.un Done

TestCase1109:
   ldc.i4 1209
   stloc.0
   ldc.i4 0xffffc3cf
   conv.i4
   not
   ldc.i4 0x3c30
   bne.un Done

TestCase1108:
   ldc.i4 1208
   stloc.0
   ldc.i4 0xe3f
   conv.i4
   not
   ldc.i4 0xfffff1c0
   beq TestCase1107
   br Done

TestCase1107:
   ldc.i4 1207
   stloc.0
   ldc.i4 0x20f6
   conv.i4
   not
   ldc.i4 0xffffdf09
   beq TestCase1106
   br Done

TestCase1106:
   ldc.i4 1206
   stloc.0
   ldc.i4 0xffff95a0
   conv.i4
   not
   ldc.i4 0x6a5f
   beq TestCase1105
   br Done

TestCase1105:
   ldc.i4 1205
   stloc.0
   ldc.i4 0xffffd158
   conv.i4
   not
   ldc.i4 0x2ea7
   bne.un Done

TestCase1104:
   ldc.i4 1204
   stloc.0
   ldc.i4 0xffff9232
   conv.i4
   not
   ldc.i4 0x6dcd
   bne.un Done

TestCase1103:
   ldc.i4 1203
   stloc.0
   ldc.i4 0xffffe3d1
   conv.i4
   not
   ldc.i4 0x1c2e
   beq TestCase1102
   br Done

TestCase1102:
   ldc.i4 1202
   stloc.0
   ldc.i4 0x4ea4
   conv.i4
   not
   ldc.i4 0xffffb15b
   beq TestCase1101
   br Done

TestCase1101:
   ldc.i4 1201
   stloc.0
   ldc.i4 0xffffbc13
   conv.i4
   not
   ldc.i4 0x43ec
   beq TestCase1100
   br Done

TestCase1100:
   ldc.i4 1200
   stloc.0
   ldc.i4 0x5dda
   conv.i4
   not
   ldc.i4 0xffffa225
   beq TestCase1099
   br Done

TestCase1099:
   ldc.i4 1199
   stloc.0
   ldc.i4 0xfffffced
   conv.i4
   not
   ldc.i4 0x312
   beq TestCase1098
   br Done

TestCase1098:
   ldc.i4 1198
   stloc.0
   ldc.i4 0xffffe54b
   conv.i4
   not
   ldc.i4 0x1ab4
   bne.un Done

TestCase1097:
   ldc.i4 1197
   stloc.0
   ldc.i4 0x3f16
   conv.i4
   not
   ldc.i4 0xffffc0e9
   beq TestCase1096
   br Done

TestCase1096:
   ldc.i4 1196
   stloc.0
   ldc.i4 0xffffd4f2
   conv.i4
   not
   ldc.i4 0x2b0d
   bne.un Done

TestCase1095:
   ldc.i4 1195
   stloc.0
   ldc.i4 0x64bb
   conv.i4
   not
   ldc.i4 0xffff9b44
   beq TestCase1094
   br Done

TestCase1094:
   ldc.i4 1194
   stloc.0
   ldc.i4 0xe26
   conv.i4
   not
   ldc.i4 0xfffff1d9
   beq TestCase1093
   br Done

TestCase1093:
   ldc.i4 1193
   stloc.0
   ldc.i4 0xffff89c1
   conv.i4
   not
   ldc.i4 0x763e
   bne.un Done

TestCase1092:
   ldc.i4 1192
   stloc.0
   ldc.i4 0x6300
   conv.i4
   not
   ldc.i4 0xffff9cff
   beq TestCase1091
   br Done

TestCase1091:
   ldc.i4 1191
   stloc.0
   ldc.i4 0x6672
   conv.i4
   not
   ldc.i4 0xffff998d
   beq TestCase1090
   br Done

TestCase1090:
   ldc.i4 1190
   stloc.0
   ldc.i4 0x5c74
   conv.i4
   not
   ldc.i4 0xffffa38b
   beq TestCase1089
   br Done

TestCase1089:
   ldc.i4 1189
   stloc.0
   ldc.i4 0x1cef
   conv.i4
   not
   ldc.i4 0xffffe310
   bne.un Done

TestCase1088:
   ldc.i4 1188
   stloc.0
   ldc.i4 0xffffb384
   conv.i4
   not
   ldc.i4 0x4c7b
   beq TestCase1087
   br Done

TestCase1087:
   ldc.i4 1187
   stloc.0
   ldc.i4 0xffffdc51
   conv.i4
   not
   ldc.i4 0x23ae
   beq TestCase1086
   br Done

TestCase1086:
   ldc.i4 1186
   stloc.0
   ldc.i4 0x5d6e
   conv.i4
   not
   ldc.i4 0xffffa291
   bne.un Done

TestCase1085:
   ldc.i4 1185
   stloc.0
   ldc.i4 0x38ac
   conv.i4
   not
   ldc.i4 0xffffc753
   beq TestCase1084
   br Done

TestCase1084:
   ldc.i4 1184
   stloc.0
   ldc.i4 0xffff94ed
   conv.i4
   not
   ldc.i4 0x6b12
   beq TestCase1083
   br Done

TestCase1083:
   ldc.i4 1183
   stloc.0
   ldc.i4 0x5e4f
   conv.i4
   not
   ldc.i4 0xffffa1b0
   bne.un Done

TestCase1082:
   ldc.i4 1182
   stloc.0
   ldc.i4 0xffffb919
   conv.i4
   not
   ldc.i4 0x46e6
   beq TestCase1081
   br Done

TestCase1081:
   ldc.i4 1181
   stloc.0
   ldc.i4 0xffffe425
   conv.i4
   not
   ldc.i4 0x1bda
   beq TestCase1080
   br Done

TestCase1080:
   ldc.i4 1180
   stloc.0
   ldc.i4 0xffffa61c
   conv.i4
   not
   ldc.i4 0x59e3
   bne.un Done

TestCase1079:
   ldc.i4 1179
   stloc.0
   ldc.i4 0xffff84e9
   conv.i4
   not
   ldc.i4 0x7b16
   bne.un Done

TestCase1078:
   ldc.i4 1178
   stloc.0
   ldc.i4 0xffffa462
   conv.i4
   not
   ldc.i4 0x5b9d
   beq TestCase1077
   br Done

TestCase1077:
   ldc.i4 1177
   stloc.0
   ldc.i4 0xfffffd22
   conv.i4
   not
   ldc.i4 0x2dd
   bne.un Done

TestCase1076:
   ldc.i4 1176
   stloc.0
   ldc.i4 0x9a8
   conv.i4
   not
   ldc.i4 0xfffff657
   beq TestCase1075
   br Done

TestCase1075:
   ldc.i4 1175
   stloc.0
   ldc.i4 0xfffff997
   conv.i4
   not
   ldc.i4 0x668
   beq TestCase1074
   br Done

TestCase1074:
   ldc.i4 1174
   stloc.0
   ldc.i4 0x3b6d
   conv.i4
   not
   ldc.i4 0xffffc492
   bne.un Done

TestCase1073:
   ldc.i4 1173
   stloc.0
   ldc.i4 0xffffd098
   conv.i4
   not
   ldc.i4 0x2f67
   beq TestCase1072
   br Done

TestCase1072:
   ldc.i4 1172
   stloc.0
   ldc.i4 0x2e04
   conv.i4
   not
   ldc.i4 0xffffd1fb
   beq TestCase1071
   br Done

TestCase1071:
   ldc.i4 1171
   stloc.0
   ldc.i4 0xffffc2bc
   conv.i4
   not
   ldc.i4 0x3d43
   bne.un Done

TestCase1070:
   ldc.i4 1170
   stloc.0
   ldc.i4 0xffffbf4f
   conv.i4
   not
   ldc.i4 0x40b0
   beq TestCase1069
   br Done

TestCase1069:
   ldc.i4 1169
   stloc.0
   ldc.i4 0xffffe1de
   conv.i4
   not
   ldc.i4 0x1e21
   bne.un Done

TestCase1068:
   ldc.i4 1168
   stloc.0
   ldc.i4 0x660a
   conv.i4
   not
   ldc.i4 0xffff99f5
   bne.un Done

TestCase1067:
   ldc.i4 1167
   stloc.0
   ldc.i4 0x5681
   conv.i4
   not
   ldc.i4 0xffffa97e
   bne.un Done

TestCase1066:
   ldc.i4 1166
   stloc.0
   ldc.i4 0xffffe908
   conv.i4
   not
   ldc.i4 0x16f7
   beq TestCase1065
   br Done

TestCase1065:
   ldc.i4 1165
   stloc.0
   ldc.i4 0xffffcd7d
   conv.i4
   not
   ldc.i4 0x3282
   bne.un Done

TestCase1064:
   ldc.i4 1164
   stloc.0
   ldc.i4 0x6c0f
   conv.i4
   not
   ldc.i4 0xffff93f0
   beq TestCase1063
   br Done

TestCase1063:
   ldc.i4 1163
   stloc.0
   ldc.i4 0x84b
   conv.i4
   not
   ldc.i4 0xfffff7b4
   beq TestCase1062
   br Done

TestCase1062:
   ldc.i4 1162
   stloc.0
   ldc.i4 0x76f1
   conv.i4
   not
   ldc.i4 0xffff890e
   bne.un Done

TestCase1061:
   ldc.i4 1161
   stloc.0
   ldc.i4 0x74ec
   conv.i4
   not
   ldc.i4 0xffff8b13
   bne.un Done

TestCase1060:
   ldc.i4 1160
   stloc.0
   ldc.i4 0x58b4
   conv.i4
   not
   ldc.i4 0xffffa74b
   beq TestCase1059
   br Done

TestCase1059:
   ldc.i4 1159
   stloc.0
   ldc.i4 0x5df
   conv.i4
   not
   ldc.i4 0xfffffa20
   bne.un Done

TestCase1058:
   ldc.i4 1158
   stloc.0
   ldc.i4 0xffffcc71
   conv.i4
   not
   ldc.i4 0x338e
   bne.un Done

TestCase1057:
   ldc.i4 1157
   stloc.0
   ldc.i4 0x3f03
   conv.i4
   not
   ldc.i4 0xffffc0fc
   bne.un Done

TestCase1056:
   ldc.i4 1156
   stloc.0
   ldc.i4 0x452f
   conv.i4
   not
   ldc.i4 0xffffbad0
   bne.un Done

TestCase1055:
   ldc.i4 1155
   stloc.0
   ldc.i4 0x7185
   conv.i4
   not
   ldc.i4 0xffff8e7a
   beq TestCase1054
   br Done

TestCase1054:
   ldc.i4 1154
   stloc.0
   ldc.i4 0x47f
   conv.i4
   not
   ldc.i4 0xfffffb80
   bne.un Done

TestCase1053:
   ldc.i4 1153
   stloc.0
   ldc.i4 0x7250
   conv.i4
   not
   ldc.i4 0xffff8daf
   bne.un Done

TestCase1052:
   ldc.i4 1152
   stloc.0
   ldc.i4 0xffffa727
   conv.i4
   not
   ldc.i4 0x58d8
   bne.un Done

TestCase1051:
   ldc.i4 1151
   stloc.0
   ldc.i4 0x7ece
   conv.i4
   not
   ldc.i4 0xffff8131
   bne.un Done

TestCase1050:
   ldc.i4 1150
   stloc.0
   ldc.i4 0xffffc634
   conv.i4
   not
   ldc.i4 0x39cb
   bne.un Done

TestCase1049:
   ldc.i4 1149
   stloc.0
   ldc.i4 0x151d
   conv.i4
   not
   ldc.i4 0xffffeae2
   beq TestCase1048
   br Done

TestCase1048:
   ldc.i4 1148
   stloc.0
   ldc.i4 0xdfc
   conv.i4
   not
   ldc.i4 0xfffff203
   beq TestCase1047
   br Done

TestCase1047:
   ldc.i4 1147
   stloc.0
   ldc.i4 0x410f
   conv.i4
   not
   ldc.i4 0xffffbef0
   beq TestCase1046
   br Done

TestCase1046:
   ldc.i4 1146
   stloc.0
   ldc.i4 0x44d0
   conv.i4
   not
   ldc.i4 0xffffbb2f
   beq TestCase1045
   br Done

TestCase1045:
   ldc.i4 1145
   stloc.0
   ldc.i4 0x7189
   conv.i4
   not
   ldc.i4 0xffff8e76
   beq TestCase1044
   br Done

TestCase1044:
   ldc.i4 1144
   stloc.0
   ldc.i4 0xffffc622
   conv.i4
   not
   ldc.i4 0x39dd
   bne.un Done

TestCase1043:
   ldc.i4 1143
   stloc.0
   ldc.i4 0xffff9de3
   conv.i4
   not
   ldc.i4 0x621c
   bne.un Done

TestCase1042:
   ldc.i4 1142
   stloc.0
   ldc.i4 0x425d
   conv.i4
   not
   ldc.i4 0xffffbda2
   beq TestCase1041
   br Done

TestCase1041:
   ldc.i4 1141
   stloc.0
   ldc.i4 0xffffed12
   conv.i4
   not
   ldc.i4 0x12ed
   bne.un Done

TestCase1040:
   ldc.i4 1140
   stloc.0
   ldc.i4 0x2408
   conv.i4
   not
   ldc.i4 0xffffdbf7
   beq TestCase1039
   br Done

TestCase1039:
   ldc.i4 1139
   stloc.0
   ldc.i4 0x3d49
   conv.i4
   not
   ldc.i4 0xffffc2b6
   beq TestCase1038
   br Done

TestCase1038:
   ldc.i4 1138
   stloc.0
   ldc.i4 0xffff8dd4
   conv.i4
   not
   ldc.i4 0x722b
   bne.un Done

TestCase1037:
   ldc.i4 1137
   stloc.0
   ldc.i4 0xffffc28b
   conv.i4
   not
   ldc.i4 0x3d74
   bne.un Done

TestCase1036:
   ldc.i4 1136
   stloc.0
   ldc.i4 0x38db
   conv.i4
   not
   ldc.i4 0xffffc724
   beq TestCase1035
   br Done

TestCase1035:
   ldc.i4 1135
   stloc.0
   ldc.i4 0xffffe562
   conv.i4
   not
   ldc.i4 0x1a9d
   bne.un Done

TestCase1034:
   ldc.i4 1134
   stloc.0
   ldc.i4 0xffffb87b
   conv.i4
   not
   ldc.i4 0x4784
   bne.un Done

TestCase1033:
   ldc.i4 1133
   stloc.0
   ldc.i4 0xffffc40a
   conv.i4
   not
   ldc.i4 0x3bf5
   bne.un Done

TestCase1032:
   ldc.i4 1132
   stloc.0
   ldc.i4 0x2356
   conv.i4
   not
   ldc.i4 0xffffdca9
   beq TestCase1031
   br Done

TestCase1031:
   ldc.i4 1131
   stloc.0
   ldc.i4 0x659
   conv.i4
   not
   ldc.i4 0xfffff9a6
   bne.un Done

TestCase1030:
   ldc.i4 1130
   stloc.0
   ldc.i4 0xffff80d1
   conv.i4
   not
   ldc.i4 0x7f2e
   beq TestCase1029
   br Done

TestCase1029:
   ldc.i4 1129
   stloc.0
   ldc.i4 0x2014
   conv.i4
   not
   ldc.i4 0xffffdfeb
   bne.un Done

TestCase1028:
   ldc.i4 1128
   stloc.0
   ldc.i4 0x5a20
   conv.i4
   not
   ldc.i4 0xffffa5df
   bne.un Done

TestCase1027:
   ldc.i4 1127
   stloc.0
   ldc.i4 0xa9
   conv.i4
   not
   ldc.i4 0xffffff56
   bne.un Done

TestCase1026:
   ldc.i4 1126
   stloc.0
   ldc.i4 0xffff9966
   conv.i4
   not
   ldc.i4 0x6699
   bne.un Done

TestCase1025:
   ldc.i4 1125
   stloc.0
   ldc.i4 0x5099
   conv.i4
   not
   ldc.i4 0xffffaf66
   beq TestCase1024
   br Done

TestCase1024:
   ldc.i4 1124
   stloc.0
   ldc.i4 0x6bdc
   conv.i4
   not
   ldc.i4 0xffff9423
   beq TestCase1023
   br Done

TestCase1023:
   ldc.i4 1123
   stloc.0
   ldc.i4 0x1a7
   conv.i4
   not
   ldc.i4 0xfffffe58
   beq TestCase1022
   br Done

TestCase1022:
   ldc.i4 1122
   stloc.0
   ldc.i4 0x404a
   conv.i4
   not
   ldc.i4 0xffffbfb5
   bne.un Done

TestCase1021:
   ldc.i4 1121
   stloc.0
   ldc.i4 0xfffffdf2
   conv.i4
   not
   ldc.i4 0x20d
   beq TestCase1020
   br Done

TestCase1020:
   ldc.i4 1120
   stloc.0
   ldc.i4 0x16ad
   conv.i4
   not
   ldc.i4 0xffffe952
   beq TestCase1019
   br Done

TestCase1019:
   ldc.i4 1119
   stloc.0
   ldc.i4 0xfffff268
   conv.i4
   not
   ldc.i4 0xd97
   beq TestCase1018
   br Done

TestCase1018:
   ldc.i4 1118
   stloc.0
   ldc.i4 0x7186
   conv.i4
   not
   ldc.i4 0xffff8e79
   beq TestCase1017
   br Done

TestCase1017:
   ldc.i4 1117
   stloc.0
   ldc.i4 0xffff8a62
   conv.i4
   not
   ldc.i4 0x759d
   beq TestCase1016
   br Done

TestCase1016:
   ldc.i4 1116
   stloc.0
   ldc.i4 0x7706
   conv.i4
   not
   ldc.i4 0xffff88f9
   beq TestCase1015
   br Done

TestCase1015:
   ldc.i4 1115
   stloc.0
   ldc.i4 0xffffaa9f
   conv.i4
   not
   ldc.i4 0x5560
   beq TestCase1014
   br Done

TestCase1014:
   ldc.i4 1114
   stloc.0
   ldc.i4 0x6541
   conv.i4
   not
   ldc.i4 0xffff9abe
   beq TestCase1013
   br Done

TestCase1013:
   ldc.i4 1113
   stloc.0
   ldc.i4 0xffffe5d3
   conv.i4
   not
   ldc.i4 0x1a2c
   bne.un Done

TestCase1012:
   ldc.i4 1112
   stloc.0
   ldc.i4 0x59b6
   conv.i4
   not
   ldc.i4 0xffffa649
   bne.un Done

TestCase1011:
   ldc.i4 1111
   stloc.0
   ldc.i4 0x1f4a
   conv.i4
   not
   ldc.i4 0xffffe0b5
   bne.un Done

TestCase1010:
   ldc.i4 1110
   stloc.0
   ldc.i4 0x5048
   conv.i4
   not
   ldc.i4 0xffffafb7
   bne.un Done

TestCase1009:
   ldc.i4 1109
   stloc.0
   ldc.i4 0x6bb3
   conv.i4
   not
   ldc.i4 0xffff944c
   bne.un Done

TestCase1008:
   ldc.i4 1108
   stloc.0
   ldc.i4 0x7d34
   conv.i4
   not
   ldc.i4 0xffff82cb
   beq TestCase1007
   br Done

TestCase1007:
   ldc.i4 1107
   stloc.0
   ldc.i4 0x73f6
   conv.i4
   not
   ldc.i4 0xffff8c09
   beq TestCase1006
   br Done

TestCase1006:
   ldc.i4 1106
   stloc.0
   ldc.i4 0xffffbd53
   conv.i4
   not
   ldc.i4 0x42ac
   bne.un Done

TestCase1005:
   ldc.i4 1105
   stloc.0
   ldc.i4 0x2cdd
   conv.i4
   not
   ldc.i4 0xffffd322
   beq TestCase1004
   br Done

TestCase1004:
   ldc.i4 1104
   stloc.0
   ldc.i4 0x4e27
   conv.i4
   not
   ldc.i4 0xffffb1d8
   beq TestCase1003
   br Done

TestCase1003:
   ldc.i4 1103
   stloc.0
   ldc.i4 0xffffb45e
   conv.i4
   not
   ldc.i4 0x4ba1
   bne.un Done

TestCase1002:
   ldc.i4 1102
   stloc.0
   ldc.i4 0x3273
   conv.i4
   not
   ldc.i4 0xffffcd8c
   bne.un Done

TestCase1001:
   ldc.i4 1101
   stloc.0
   ldc.i4 0xffffa91b
   conv.i4
   not
   ldc.i4 0x56e4
   beq TestCase1000
   br Done

TestCase1000:
   ldc.i4 1100
   stloc.0
   ldc.i4 0xffff94c7
   conv.i4
   not
   ldc.i4 0x6b38
   beq TestCase999
   br Done

TestCase999:
   ldc.i4 1099
   stloc.0
   ldc.i4 0x379d
   conv.i4
   not
   ldc.i4 0xffffc862
   bne.un Done

TestCase998:
   ldc.i4 1098
   stloc.0
   ldc.i4 0xffff9486
   conv.i4
   not
   ldc.i4 0x6b79
   bne.un Done

TestCase997:
   ldc.i4 1097
   stloc.0
   ldc.i4 0x5bb1
   conv.i4
   not
   ldc.i4 0xffffa44e
   bne.un Done

TestCase996:
   ldc.i4 1096
   stloc.0
   ldc.i4 0xffff83e4
   conv.i4
   not
   ldc.i4 0x7c1b
   beq TestCase995
   br Done

TestCase995:
   ldc.i4 1095
   stloc.0
   ldc.i4 0x4407
   conv.i4
   not
   ldc.i4 0xffffbbf8
   beq TestCase994
   br Done

TestCase994:
   ldc.i4 1094
   stloc.0
   ldc.i4 0x10ac
   conv.i4
   not
   ldc.i4 0xffffef53
   beq TestCase993
   br Done

TestCase993:
   ldc.i4 1093
   stloc.0
   ldc.i4 0x162a
   conv.i4
   not
   ldc.i4 0xffffe9d5
   bne.un Done

TestCase992:
   ldc.i4 1092
   stloc.0
   ldc.i4 0x27dc
   conv.i4
   not
   ldc.i4 0xffffd823
   bne.un Done

TestCase991:
   ldc.i4 1091
   stloc.0
   ldc.i4 0xfffff233
   conv.i4
   not
   ldc.i4 0xdcc
   bne.un Done

TestCase990:
   ldc.i4 1090
   stloc.0
   ldc.i4 0xffffe598
   conv.i4
   not
   ldc.i4 0x1a67
   bne.un Done

TestCase989:
   ldc.i4 1089
   stloc.0
   ldc.i4 0xfffffb22
   conv.i4
   not
   ldc.i4 0x4dd
   bne.un Done

TestCase988:
   ldc.i4 1088
   stloc.0
   ldc.i4 0x3666
   conv.i4
   not
   ldc.i4 0xffffc999
   bne.un Done

TestCase987:
   ldc.i4 1087
   stloc.0
   ldc.i4 0xffffb71c
   conv.i4
   not
   ldc.i4 0x48e3
   beq TestCase986
   br Done

TestCase986:
   ldc.i4 1086
   stloc.0
   ldc.i4 0xffff81d2
   conv.i4
   not
   ldc.i4 0x7e2d
   bne.un Done

TestCase985:
   ldc.i4 1085
   stloc.0
   ldc.i4 0xffffd527
   conv.i4
   not
   ldc.i4 0x2ad8
   beq TestCase984
   br Done

TestCase984:
   ldc.i4 1084
   stloc.0
   ldc.i4 0x2958
   conv.i4
   not
   ldc.i4 0xffffd6a7
   beq TestCase983
   br Done

TestCase983:
   ldc.i4 1083
   stloc.0
   ldc.i4 0xffffcd3b
   conv.i4
   not
   ldc.i4 0x32c4
   beq TestCase982
   br Done

TestCase982:
   ldc.i4 1082
   stloc.0
   ldc.i4 0x6b8b
   conv.i4
   not
   ldc.i4 0xffff9474
   beq TestCase981
   br Done

TestCase981:
   ldc.i4 1081
   stloc.0
   ldc.i4 0xffffcd41
   conv.i4
   not
   ldc.i4 0x32be
   beq TestCase980
   br Done

TestCase980:
   ldc.i4 1080
   stloc.0
   ldc.i4 0xffffa7f5
   conv.i4
   not
   ldc.i4 0x580a
   beq TestCase979
   br Done

TestCase979:
   ldc.i4 1079
   stloc.0
   ldc.i4 0x2d28
   conv.i4
   not
   ldc.i4 0xffffd2d7
   beq TestCase978
   br Done

TestCase978:
   ldc.i4 1078
   stloc.0
   ldc.i4 0x4a47
   conv.i4
   not
   ldc.i4 0xffffb5b8
   beq TestCase977
   br Done

TestCase977:
   ldc.i4 1077
   stloc.0
   ldc.i4 0xffffcfd2
   conv.i4
   not
   ldc.i4 0x302d
   bne.un Done

TestCase976:
   ldc.i4 1076
   stloc.0
   ldc.i4 0xffffa522
   conv.i4
   not
   ldc.i4 0x5add
   bne.un Done

TestCase975:
   ldc.i4 1075
   stloc.0
   ldc.i4 0xffff82fc
   conv.i4
   not
   ldc.i4 0x7d03
   beq TestCase974
   br Done

TestCase974:
   ldc.i4 1074
   stloc.0
   ldc.i4 0xffff9a33
   conv.i4
   not
   ldc.i4 0x65cc
   beq TestCase973
   br Done

TestCase973:
   ldc.i4 1073
   stloc.0
   ldc.i4 0x1b52
   conv.i4
   not
   ldc.i4 0xffffe4ad
   bne.un Done

TestCase972:
   ldc.i4 1072
   stloc.0
   ldc.i4 0x51c4
   conv.i4
   not
   ldc.i4 0xffffae3b
   bne.un Done

TestCase971:
   ldc.i4 1071
   stloc.0
   ldc.i4 0x69c5
   conv.i4
   not
   ldc.i4 0xffff963a
   beq TestCase970
   br Done

TestCase970:
   ldc.i4 1070
   stloc.0
   ldc.i4 0x71da
   conv.i4
   not
   ldc.i4 0xffff8e25
   beq TestCase969
   br Done

TestCase969:
   ldc.i4 1069
   stloc.0
   ldc.i4 0x62c9
   conv.i4
   not
   ldc.i4 0xffff9d36
   bne.un Done

TestCase968:
   ldc.i4 1068
   stloc.0
   ldc.i4 0xffffadf0
   conv.i4
   not
   ldc.i4 0x520f
   beq TestCase967
   br Done

TestCase967:
   ldc.i4 1067
   stloc.0
   ldc.i4 0xffffc731
   conv.i4
   not
   ldc.i4 0x38ce
   bne.un Done

TestCase966:
   ldc.i4 1066
   stloc.0
   ldc.i4 0x5469
   conv.i4
   not
   ldc.i4 0xffffab96
   bne.un Done

TestCase965:
   ldc.i4 1065
   stloc.0
   ldc.i4 0x2fb8
   conv.i4
   not
   ldc.i4 0xffffd047
   bne.un Done

TestCase964:
   ldc.i4 1064
   stloc.0
   ldc.i4 0xfffff881
   conv.i4
   not
   ldc.i4 0x77e
   bne.un Done

TestCase963:
   ldc.i4 1063
   stloc.0
   ldc.i4 0xffffbecd
   conv.i4
   not
   ldc.i4 0x4132
   beq TestCase962
   br Done

TestCase962:
   ldc.i4 1062
   stloc.0
   ldc.i4 0x60dd
   conv.i4
   not
   ldc.i4 0xffff9f22
   bne.un Done

TestCase961:
   ldc.i4 1061
   stloc.0
   ldc.i4 0xffffc7d3
   conv.i4
   not
   ldc.i4 0x382c
   beq TestCase960
   br Done

TestCase960:
   ldc.i4 1060
   stloc.0
   ldc.i4 0xffffab7a
   conv.i4
   not
   ldc.i4 0x5485
   beq TestCase959
   br Done

TestCase959:
   ldc.i4 1059
   stloc.0
   ldc.i4 0xffffec50
   conv.i4
   not
   ldc.i4 0x13af
   bne.un Done

TestCase958:
   ldc.i4 1058
   stloc.0
   ldc.i4 0xffff8f90
   conv.i4
   not
   ldc.i4 0x706f
   bne.un Done

TestCase957:
   ldc.i4 1057
   stloc.0
   ldc.i4 0x3bb8
   conv.i4
   not
   ldc.i4 0xffffc447
   beq TestCase956
   br Done

TestCase956:
   ldc.i4 1056
   stloc.0
   ldc.i4 0xffff8223
   conv.i4
   not
   ldc.i4 0x7ddc
   bne.un Done

TestCase955:
   ldc.i4 1055
   stloc.0
   ldc.i4 0x72ec
   conv.i4
   not
   ldc.i4 0xffff8d13
   beq TestCase954
   br Done

TestCase954:
   ldc.i4 1054
   stloc.0
   ldc.i4 0x3db
   conv.i4
   not
   ldc.i4 0xfffffc24
   beq TestCase953
   br Done

TestCase953:
   ldc.i4 1053
   stloc.0
   ldc.i4 0xfffff9bc
   conv.i4
   not
   ldc.i4 0x643
   beq TestCase952
   br Done

TestCase952:
   ldc.i4 1052
   stloc.0
   ldc.i4 0x2d4c
   conv.i4
   not
   ldc.i4 0xffffd2b3
   bne.un Done

TestCase951:
   ldc.i4 1051
   stloc.0
   ldc.i4 0x17f3
   conv.i4
   not
   ldc.i4 0xffffe80c
   bne.un Done

TestCase950:
   ldc.i4 1050
   stloc.0
   ldc.i4 0x5a46
   conv.i4
   not
   ldc.i4 0xffffa5b9
   bne.un Done

TestCase949:
   ldc.i4 1049
   stloc.0
   ldc.i4 0xffffeac6
   conv.i4
   not
   ldc.i4 0x1539
   bne.un Done

TestCase948:
   ldc.i4 1048
   stloc.0
   ldc.i4 0x57b4
   conv.i4
   not
   ldc.i4 0xffffa84b
   bne.un Done

TestCase947:
   ldc.i4 1047
   stloc.0
   ldc.i4 0x7eef
   conv.i4
   not
   ldc.i4 0xffff8110
   beq TestCase946
   br Done

TestCase946:
   ldc.i4 1046
   stloc.0
   ldc.i4 0x2971
   conv.i4
   not
   ldc.i4 0xffffd68e
   beq TestCase945
   br Done

TestCase945:
   ldc.i4 1045
   stloc.0
   ldc.i4 0xffffc528
   conv.i4
   not
   ldc.i4 0x3ad7
   bne.un Done

TestCase944:
   ldc.i4 1044
   stloc.0
   ldc.i4 0x525f
   conv.i4
   not
   ldc.i4 0xffffada0
   beq TestCase943
   br Done

TestCase943:
   ldc.i4 1043
   stloc.0
   ldc.i4 0x3829
   conv.i4
   not
   ldc.i4 0xffffc7d6
   beq TestCase942
   br Done

TestCase942:
   ldc.i4 1042
   stloc.0
   ldc.i4 0x6323
   conv.i4
   not
   ldc.i4 0xffff9cdc
   bne.un Done

TestCase941:
   ldc.i4 1041
   stloc.0
   ldc.i4 0xffffa433
   conv.i4
   not
   ldc.i4 0x5bcc
   beq TestCase940
   br Done

TestCase940:
   ldc.i4 1040
   stloc.0
   ldc.i4 0x238c
   conv.i4
   not
   ldc.i4 0xffffdc73
   beq TestCase939
   br Done

TestCase939:
   ldc.i4 1039
   stloc.0
   ldc.i4 0xffff86c0
   conv.i4
   not
   ldc.i4 0x793f
   bne.un Done

TestCase938:
   ldc.i4 1038
   stloc.0
   ldc.i4 0xffffcc2d
   conv.i4
   not
   ldc.i4 0x33d2
   beq TestCase937
   br Done

TestCase937:
   ldc.i4 1037
   stloc.0
   ldc.i4 0xffffdce7
   conv.i4
   not
   ldc.i4 0x2318
   bne.un Done

TestCase936:
   ldc.i4 1036
   stloc.0
   ldc.i4 0xffffa5cb
   conv.i4
   not
   ldc.i4 0x5a34
   beq TestCase935
   br Done

TestCase935:
   ldc.i4 1035
   stloc.0
   ldc.i4 0x6aa7
   conv.i4
   not
   ldc.i4 0xffff9558
   beq TestCase934
   br Done

TestCase934:
   ldc.i4 1034
   stloc.0
   ldc.i4 0x2694
   conv.i4
   not
   ldc.i4 0xffffd96b
   bne.un Done

TestCase933:
   ldc.i4 1033
   stloc.0
   ldc.i4 0x4823
   conv.i4
   not
   ldc.i4 0xffffb7dc
   beq TestCase932
   br Done

TestCase932:
   ldc.i4 1032
   stloc.0
   ldc.i4 0x7923
   conv.i4
   not
   ldc.i4 0xffff86dc
   bne.un Done

TestCase931:
   ldc.i4 1031
   stloc.0
   ldc.i4 0xffff929c
   conv.i4
   not
   ldc.i4 0x6d63
   beq TestCase930
   br Done

TestCase930:
   ldc.i4 1030
   stloc.0
   ldc.i4 0x1a46
   conv.i4
   not
   ldc.i4 0xffffe5b9
   bne.un Done

TestCase929:
   ldc.i4 1029
   stloc.0
   ldc.i4 0xffffbce4
   conv.i4
   not
   ldc.i4 0x431b
   beq TestCase928
   br Done

TestCase928:
   ldc.i4 1028
   stloc.0
   ldc.i4 0xffffbecc
   conv.i4
   not
   ldc.i4 0x4133
   bne.un Done

TestCase927:
   ldc.i4 1027
   stloc.0
   ldc.i4 0x5506
   conv.i4
   not
   ldc.i4 0xffffaaf9
   beq TestCase926
   br Done

TestCase926:
   ldc.i4 1026
   stloc.0
   ldc.i4 0xffff8c06
   conv.i4
   not
   ldc.i4 0x73f9
   bne.un Done

TestCase925:
   ldc.i4 1025
   stloc.0
   ldc.i4 0xb92
   conv.i4
   not
   ldc.i4 0xfffff46d
   beq TestCase924
   br Done

TestCase924:
   ldc.i4 1024
   stloc.0
   ldc.i4 0xffff9916
   conv.i4
   not
   ldc.i4 0x66e9
   bne.un Done

TestCase923:
   ldc.i4 1023
   stloc.0
   ldc.i4 0x51e7
   conv.i4
   not
   ldc.i4 0xffffae18
   bne.un Done

TestCase922:
   ldc.i4 1022
   stloc.0
   ldc.i4 0xffffc55b
   conv.i4
   not
   ldc.i4 0x3aa4
   beq TestCase921
   br Done

TestCase921:
   ldc.i4 1021
   stloc.0
   ldc.i4 0x3d57
   conv.i4
   not
   ldc.i4 0xffffc2a8
   bne.un Done

TestCase920:
   ldc.i4 1020
   stloc.0
   ldc.i4 0xffff80c8
   conv.i4
   not
   ldc.i4 0x7f37
   bne.un Done

TestCase919:
   ldc.i4 1019
   stloc.0
   ldc.i4 0x7a3
   conv.i4
   not
   ldc.i4 0xfffff85c
   beq TestCase918
   br Done

TestCase918:
   ldc.i4 1018
   stloc.0
   ldc.i4 0x808
   conv.i4
   not
   ldc.i4 0xfffff7f7
   bne.un Done

TestCase917:
   ldc.i4 1017
   stloc.0
   ldc.i4 0xffffc06c
   conv.i4
   not
   ldc.i4 0x3f93
   beq TestCase916
   br Done

TestCase916:
   ldc.i4 1016
   stloc.0
   ldc.i4 0xffffd078
   conv.i4
   not
   ldc.i4 0x2f87
   bne.un Done

TestCase915:
   ldc.i4 1015
   stloc.0
   ldc.i4 0xbd8
   conv.i4
   not
   ldc.i4 0xfffff427
   bne.un Done

TestCase914:
   ldc.i4 1014
   stloc.0
   ldc.i4 0xffff8521
   conv.i4
   not
   ldc.i4 0x7ade
   bne.un Done

TestCase913:
   ldc.i4 1013
   stloc.0
   ldc.i4 0x4a28
   conv.i4
   not
   ldc.i4 0xffffb5d7
   beq TestCase912
   br Done

TestCase912:
   ldc.i4 1012
   stloc.0
   ldc.i4 0x6d0f
   conv.i4
   not
   ldc.i4 0xffff92f0
   beq TestCase911
   br Done

TestCase911:
   ldc.i4 1011
   stloc.0
   ldc.i4 0xffffb6e2
   conv.i4
   not
   ldc.i4 0x491d
   beq TestCase910
   br Done

TestCase910:
   ldc.i4 1010
   stloc.0
   ldc.i4 0x1648
   conv.i4
   not
   ldc.i4 0xffffe9b7
   bne.un Done

TestCase909:
   ldc.i4 1009
   stloc.0
   ldc.i4 0x6546
   conv.i4
   not
   ldc.i4 0xffff9ab9
   beq TestCase908
   br Done

TestCase908:
   ldc.i4 1008
   stloc.0
   ldc.i4 0xffffa8ab
   conv.i4
   not
   ldc.i4 0x5754
   bne.un Done

TestCase907:
   ldc.i4 1007
   stloc.0
   ldc.i4 0xfffffdb7
   conv.i4
   not
   ldc.i4 0x248
   bne.un Done

TestCase906:
   ldc.i4 1006
   stloc.0
   ldc.i4 0xffffc58b
   conv.i4
   not
   ldc.i4 0x3a74
   bne.un Done

TestCase905:
   ldc.i4 1005
   stloc.0
   ldc.i4 0xffffc0b2
   conv.i4
   not
   ldc.i4 0x3f4d
   beq TestCase904
   br Done

TestCase904:
   ldc.i4 1004
   stloc.0
   ldc.i4 0xffff8602
   conv.i4
   not
   ldc.i4 0x79fd
   beq TestCase903
   br Done

TestCase903:
   ldc.i4 1003
   stloc.0
   ldc.i4 0xffff8e23
   conv.i4
   not
   ldc.i4 0x71dc
   beq TestCase902
   br Done

TestCase902:
   ldc.i4 1002
   stloc.0
   ldc.i4 0xffffdd6c
   conv.i4
   not
   ldc.i4 0x2293
   bne.un Done

TestCase901:
   ldc.i4 1001
   stloc.0
   ldc.i4 0x74ea
   conv.i4
   not
   ldc.i4 0xffff8b15
   beq TestCase900
   br Done

TestCase900:
   ldc.i4 1000
   stloc.0
   ldc.i4 0xfffffe22
   conv.i4
   not
   ldc.i4 0x1dd
   beq TestCase899
   br Done

TestCase899:
   ldc.i4 999
   stloc.0
   ldc.i4 0x41d2
   conv.i4
   not
   ldc.i4 0xffffbe2d
   bne.un Done

TestCase898:
   ldc.i4 998
   stloc.0
   ldc.i4 0x13fe
   conv.i4
   not
   ldc.i4 0xffffec01
   beq TestCase897
   br Done

TestCase897:
   ldc.i4 997
   stloc.0
   ldc.i4 0x276c
   conv.i4
   not
   ldc.i4 0xffffd893
   bne.un Done

TestCase896:
   ldc.i4 996
   stloc.0
   ldc.i4 0xffffcec4
   conv.i4
   not
   ldc.i4 0x313b
   bne.un Done

TestCase895:
   ldc.i4 995
   stloc.0
   ldc.i4 0x5789
   conv.i4
   not
   ldc.i4 0xffffa876
   beq TestCase894
   br Done

TestCase894:
   ldc.i4 994
   stloc.0
   ldc.i4 0xffffd561
   conv.i4
   not
   ldc.i4 0x2a9e
   bne.un Done

TestCase893:
   ldc.i4 993
   stloc.0
   ldc.i4 0xffff8299
   conv.i4
   not
   ldc.i4 0x7d66
   bne.un Done

TestCase892:
   ldc.i4 992
   stloc.0
   ldc.i4 0x1f65
   conv.i4
   not
   ldc.i4 0xffffe09a
   bne.un Done

TestCase891:
   ldc.i4 991
   stloc.0
   ldc.i4 0x300d
   conv.i4
   not
   ldc.i4 0xffffcff2
   beq TestCase890
   br Done

TestCase890:
   ldc.i4 990
   stloc.0
   ldc.i4 0xffff915b
   conv.i4
   not
   ldc.i4 0x6ea4
   beq TestCase889
   br Done

TestCase889:
   ldc.i4 989
   stloc.0
   ldc.i4 0x698b
   conv.i4
   not
   ldc.i4 0xffff9674
   beq TestCase888
   br Done

TestCase888:
   ldc.i4 988
   stloc.0
   ldc.i4 0xfffff08c
   conv.i4
   not
   ldc.i4 0xf73
   beq TestCase887
   br Done

TestCase887:
   ldc.i4 987
   stloc.0
   ldc.i4 0x488d
   conv.i4
   not
   ldc.i4 0xffffb772
   bne.un Done

TestCase886:
   ldc.i4 986
   stloc.0
   ldc.i4 0xffffe095
   conv.i4
   not
   ldc.i4 0x1f6a
   bne.un Done

TestCase885:
   ldc.i4 985
   stloc.0
   ldc.i4 0x53c5
   conv.i4
   not
   ldc.i4 0xffffac3a
   bne.un Done

TestCase884:
   ldc.i4 984
   stloc.0
   ldc.i4 0xffff95a0
   conv.i4
   not
   ldc.i4 0x6a5f
   bne.un Done

TestCase883:
   ldc.i4 983
   stloc.0
   ldc.i4 0xffffb0e7
   conv.i4
   not
   ldc.i4 0x4f18
   bne.un Done

TestCase882:
   ldc.i4 982
   stloc.0
   ldc.i4 0xffffa059
   conv.i4
   not
   ldc.i4 0x5fa6
   bne.un Done

TestCase881:
   ldc.i4 981
   stloc.0
   ldc.i4 0xffffff69
   conv.i4
   not
   ldc.i4 0x96
   beq TestCase880
   br Done

TestCase880:
   ldc.i4 980
   stloc.0
   ldc.i4 0x2846
   conv.i4
   not
   ldc.i4 0xffffd7b9
   beq TestCase879
   br Done

TestCase879:
   ldc.i4 979
   stloc.0
   ldc.i4 0xffffe864
   conv.i4
   not
   ldc.i4 0x179b
   beq TestCase878
   br Done

TestCase878:
   ldc.i4 978
   stloc.0
   ldc.i4 0xffffd12e
   conv.i4
   not
   ldc.i4 0x2ed1
   bne.un Done

TestCase877:
   ldc.i4 977
   stloc.0
   ldc.i4 0xffffad4c
   conv.i4
   not
   ldc.i4 0x52b3
   beq TestCase876
   br Done

TestCase876:
   ldc.i4 976
   stloc.0
   ldc.i4 0x34f7
   conv.i4
   not
   ldc.i4 0xffffcb08
   bne.un Done

TestCase875:
   ldc.i4 975
   stloc.0
   ldc.i4 0x1119
   conv.i4
   not
   ldc.i4 0xffffeee6
   beq TestCase874
   br Done

TestCase874:
   ldc.i4 974
   stloc.0
   ldc.i4 0x5bd8
   conv.i4
   not
   ldc.i4 0xffffa427
   beq TestCase873
   br Done

TestCase873:
   ldc.i4 973
   stloc.0
   ldc.i4 0xffffbd21
   conv.i4
   not
   ldc.i4 0x42de
   beq TestCase872
   br Done

TestCase872:
   ldc.i4 972
   stloc.0
   ldc.i4 0x1bc5
   conv.i4
   not
   ldc.i4 0xffffe43a
   beq TestCase871
   br Done

TestCase871:
   ldc.i4 971
   stloc.0
   ldc.i4 0x372a
   conv.i4
   not
   ldc.i4 0xffffc8d5
   beq TestCase870
   br Done

TestCase870:
   ldc.i4 970
   stloc.0
   ldc.i4 0x5d00
   conv.i4
   not
   ldc.i4 0xffffa2ff
   beq TestCase869
   br Done

TestCase869:
   ldc.i4 969
   stloc.0
   ldc.i4 0x2a05
   conv.i4
   not
   ldc.i4 0xffffd5fa
   beq TestCase868
   br Done

TestCase868:
   ldc.i4 968
   stloc.0
   ldc.i4 0xfffff977
   conv.i4
   not
   ldc.i4 0x688
   beq TestCase867
   br Done

TestCase867:
   ldc.i4 967
   stloc.0
   ldc.i4 0x3bb5
   conv.i4
   not
   ldc.i4 0xffffc44a
   bne.un Done

TestCase866:
   ldc.i4 966
   stloc.0
   ldc.i4 0xffffc6e3
   conv.i4
   not
   ldc.i4 0x391c
   beq TestCase865
   br Done

TestCase865:
   ldc.i4 965
   stloc.0
   ldc.i4 0x3997
   conv.i4
   not
   ldc.i4 0xffffc668
   bne.un Done

TestCase864:
   ldc.i4 964
   stloc.0
   ldc.i4 0x5bf7
   conv.i4
   not
   ldc.i4 0xffffa408
   beq TestCase863
   br Done

TestCase863:
   ldc.i4 963
   stloc.0
   ldc.i4 0x64ef
   conv.i4
   not
   ldc.i4 0xffff9b10
   beq TestCase862
   br Done

TestCase862:
   ldc.i4 962
   stloc.0
   ldc.i4 0xffff920a
   conv.i4
   not
   ldc.i4 0x6df5
   bne.un Done

TestCase861:
   ldc.i4 961
   stloc.0
   ldc.i4 0x3e2d
   conv.i4
   not
   ldc.i4 0xffffc1d2
   bne.un Done

TestCase860:
   ldc.i4 960
   stloc.0
   ldc.i4 0x926
   conv.i4
   not
   ldc.i4 0xfffff6d9
   beq TestCase859
   br Done

TestCase859:
   ldc.i4 959
   stloc.0
   ldc.i4 0xffffa46a
   conv.i4
   not
   ldc.i4 0x5b95
   bne.un Done

TestCase858:
   ldc.i4 958
   stloc.0
   ldc.i4 0xffff800e
   conv.i4
   not
   ldc.i4 0x7ff1
   beq TestCase857
   br Done

TestCase857:
   ldc.i4 957
   stloc.0
   ldc.i4 0xfffff7a8
   conv.i4
   not
   ldc.i4 0x857
   bne.un Done

TestCase856:
   ldc.i4 956
   stloc.0
   ldc.i4 0x425a
   conv.i4
   not
   ldc.i4 0xffffbda5
   bne.un Done

TestCase855:
   ldc.i4 955
   stloc.0
   ldc.i4 0x6569
   conv.i4
   not
   ldc.i4 0xffff9a96
   beq TestCase854
   br Done

TestCase854:
   ldc.i4 954
   stloc.0
   ldc.i4 0xffffe0fa
   conv.i4
   not
   ldc.i4 0x1f05
   beq TestCase853
   br Done

TestCase853:
   ldc.i4 953
   stloc.0
   ldc.i4 0xffffe839
   conv.i4
   not
   ldc.i4 0x17c6
   beq TestCase852
   br Done

TestCase852:
   ldc.i4 952
   stloc.0
   ldc.i4 0xffffb8ec
   conv.i4
   not
   ldc.i4 0x4713
   bne.un Done

TestCase851:
   ldc.i4 951
   stloc.0
   ldc.i4 0xffffb815
   conv.i4
   not
   ldc.i4 0x47ea
   beq TestCase850
   br Done

TestCase850:
   ldc.i4 950
   stloc.0
   ldc.i4 0xce3
   conv.i4
   not
   ldc.i4 0xfffff31c
   bne.un Done

TestCase849:
   ldc.i4 949
   stloc.0
   ldc.i4 0x649d
   conv.i4
   not
   ldc.i4 0xffff9b62
   beq TestCase848
   br Done

TestCase848:
   ldc.i4 948
   stloc.0
   ldc.i4 0xffff8aa4
   conv.i4
   not
   ldc.i4 0x755b
   bne.un Done

TestCase847:
   ldc.i4 947
   stloc.0
   ldc.i4 0x4f59
   conv.i4
   not
   ldc.i4 0xffffb0a6
   beq TestCase846
   br Done

TestCase846:
   ldc.i4 946
   stloc.0
   ldc.i4 0x305b
   conv.i4
   not
   ldc.i4 0xffffcfa4
   beq TestCase845
   br Done

TestCase845:
   ldc.i4 945
   stloc.0
   ldc.i4 0xffff9cf0
   conv.i4
   not
   ldc.i4 0x630f
   beq TestCase844
   br Done

TestCase844:
   ldc.i4 944
   stloc.0
   ldc.i4 0xffffc7b4
   conv.i4
   not
   ldc.i4 0x384b
   beq TestCase843
   br Done

TestCase843:
   ldc.i4 943
   stloc.0
   ldc.i4 0xffffca17
   conv.i4
   not
   ldc.i4 0x35e8
   beq TestCase842
   br Done

TestCase842:
   ldc.i4 942
   stloc.0
   ldc.i4 0xfffffdec
   conv.i4
   not
   ldc.i4 0x213
   beq TestCase841
   br Done

TestCase841:
   ldc.i4 941
   stloc.0
   ldc.i4 0x1d45
   conv.i4
   not
   ldc.i4 0xffffe2ba
   bne.un Done

TestCase840:
   ldc.i4 940
   stloc.0
   ldc.i4 0x1de0
   conv.i4
   not
   ldc.i4 0xffffe21f
   bne.un Done

TestCase839:
   ldc.i4 939
   stloc.0
   ldc.i4 0xffff98bc
   conv.i4
   not
   ldc.i4 0x6743
   beq TestCase838
   br Done

TestCase838:
   ldc.i4 938
   stloc.0
   ldc.i4 0xffffc18f
   conv.i4
   not
   ldc.i4 0x3e70
   bne.un Done

TestCase837:
   ldc.i4 937
   stloc.0
   ldc.i4 0xffff8095
   conv.i4
   not
   ldc.i4 0x7f6a
   beq TestCase836
   br Done

TestCase836:
   ldc.i4 936
   stloc.0
   ldc.i4 0x1ffc
   conv.i4
   not
   ldc.i4 0xffffe003
   bne.un Done

TestCase835:
   ldc.i4 935
   stloc.0
   ldc.i4 0x5c45
   conv.i4
   not
   ldc.i4 0xffffa3ba
   bne.un Done

TestCase834:
   ldc.i4 934
   stloc.0
   ldc.i4 0xfffff744
   conv.i4
   not
   ldc.i4 0x8bb
   beq TestCase833
   br Done

TestCase833:
   ldc.i4 933
   stloc.0
   ldc.i4 0xffffcdeb
   conv.i4
   not
   ldc.i4 0x3214
   bne.un Done

TestCase832:
   ldc.i4 932
   stloc.0
   ldc.i4 0xffff9343
   conv.i4
   not
   ldc.i4 0x6cbc
   bne.un Done

TestCase831:
   ldc.i4 931
   stloc.0
   ldc.i4 0xaf2
   conv.i4
   not
   ldc.i4 0xfffff50d
   bne.un Done

TestCase830:
   ldc.i4 930
   stloc.0
   ldc.i4 0xffff9469
   conv.i4
   not
   ldc.i4 0x6b96
   beq TestCase829
   br Done

TestCase829:
   ldc.i4 929
   stloc.0
   ldc.i4 0xffffe124
   conv.i4
   not
   ldc.i4 0x1edb
   beq TestCase828
   br Done

TestCase828:
   ldc.i4 928
   stloc.0
   ldc.i4 0xffffc5fb
   conv.i4
   not
   ldc.i4 0x3a04
   bne.un Done

TestCase827:
   ldc.i4 927
   stloc.0
   ldc.i4 0x26ba
   conv.i4
   not
   ldc.i4 0xffffd945
   bne.un Done

TestCase826:
   ldc.i4 926
   stloc.0
   ldc.i4 0x62a4
   conv.i4
   not
   ldc.i4 0xffff9d5b
   beq TestCase825
   br Done

TestCase825:
   ldc.i4 925
   stloc.0
   ldc.i4 0xffff84cb
   conv.i4
   not
   ldc.i4 0x7b34
   bne.un Done

TestCase824:
   ldc.i4 924
   stloc.0
   ldc.i4 0xffff8fc0
   conv.i4
   not
   ldc.i4 0x703f
   beq TestCase823
   br Done

TestCase823:
   ldc.i4 923
   stloc.0
   ldc.i4 0x2cf0
   conv.i4
   not
   ldc.i4 0xffffd30f
   beq TestCase822
   br Done

TestCase822:
   ldc.i4 922
   stloc.0
   ldc.i4 0xffffa68e
   conv.i4
   not
   ldc.i4 0x5971
   beq TestCase821
   br Done

TestCase821:
   ldc.i4 921
   stloc.0
   ldc.i4 0x758f
   conv.i4
   not
   ldc.i4 0xffff8a70
   bne.un Done

TestCase820:
   ldc.i4 920
   stloc.0
   ldc.i4 0xffffc784
   conv.i4
   not
   ldc.i4 0x387b
   bne.un Done

TestCase819:
   ldc.i4 919
   stloc.0
   ldc.i4 0x7987
   conv.i4
   not
   ldc.i4 0xffff8678
   beq TestCase818
   br Done

TestCase818:
   ldc.i4 918
   stloc.0
   ldc.i4 0x4baa
   conv.i4
   not
   ldc.i4 0xffffb455
   beq TestCase817
   br Done

TestCase817:
   ldc.i4 917
   stloc.0
   ldc.i4 0xffff9f4f
   conv.i4
   not
   ldc.i4 0x60b0
   beq TestCase816
   br Done

TestCase816:
   ldc.i4 916
   stloc.0
   ldc.i4 0x39a8
   conv.i4
   not
   ldc.i4 0xffffc657
   bne.un Done

TestCase815:
   ldc.i4 915
   stloc.0
   ldc.i4 0xffff9ff1
   conv.i4
   not
   ldc.i4 0x600e
   bne.un Done

TestCase814:
   ldc.i4 914
   stloc.0
   ldc.i4 0x727b
   conv.i4
   not
   ldc.i4 0xffff8d84
   bne.un Done

TestCase813:
   ldc.i4 913
   stloc.0
   ldc.i4 0xffff86af
   conv.i4
   not
   ldc.i4 0x7950
   bne.un Done

TestCase812:
   ldc.i4 912
   stloc.0
   ldc.i4 0x112a
   conv.i4
   not
   ldc.i4 0xffffeed5
   bne.un Done

TestCase811:
   ldc.i4 911
   stloc.0
   ldc.i4 0xfffffb11
   conv.i4
   not
   ldc.i4 0x4ee
   beq TestCase810
   br Done

TestCase810:
   ldc.i4 910
   stloc.0
   ldc.i4 0x2b42
   conv.i4
   not
   ldc.i4 0xffffd4bd
   beq TestCase809
   br Done

TestCase809:
   ldc.i4 909
   stloc.0
   ldc.i4 0xffffde09
   conv.i4
   not
   ldc.i4 0x21f6
   bne.un Done

TestCase808:
   ldc.i4 908
   stloc.0
   ldc.i4 0x52a7
   conv.i4
   not
   ldc.i4 0xffffad58
   beq TestCase807
   br Done

TestCase807:
   ldc.i4 907
   stloc.0
   ldc.i4 0xffff8d1c
   conv.i4
   not
   ldc.i4 0x72e3
   beq TestCase806
   br Done

TestCase806:
   ldc.i4 906
   stloc.0
   ldc.i4 0x37d
   conv.i4
   not
   ldc.i4 0xfffffc82
   bne.un Done

TestCase805:
   ldc.i4 905
   stloc.0
   ldc.i4 0xffffceec
   conv.i4
   not
   ldc.i4 0x3113
   beq TestCase804
   br Done

TestCase804:
   ldc.i4 904
   stloc.0
   ldc.i4 0x1b0e
   conv.i4
   not
   ldc.i4 0xffffe4f1
   beq TestCase803
   br Done

TestCase803:
   ldc.i4 903
   stloc.0
   ldc.i4 0xffffd746
   conv.i4
   not
   ldc.i4 0x28b9
   beq TestCase802
   br Done

TestCase802:
   ldc.i4 902
   stloc.0
   ldc.i4 0xffffea0e
   conv.i4
   not
   ldc.i4 0x15f1
   beq TestCase801
   br Done

TestCase801:
   ldc.i4 901
   stloc.0
   ldc.i4 0xffffd2a2
   conv.i4
   not
   ldc.i4 0x2d5d
   bne.un Done

TestCase800:
   ldc.i4 900
   stloc.0
   ldc.i4 0x2e6
   conv.i4
   not
   ldc.i4 0xfffffd19
   beq TestCase799
   br Done

TestCase799:
   ldc.i4 899
   stloc.0
   ldc.i4 0xffffe4b8
   conv.i4
   not
   ldc.i4 0x1b47
   bne.un Done

TestCase798:
   ldc.i4 898
   stloc.0
   ldc.i4 0x251a
   conv.i4
   not
   ldc.i4 0xffffdae5
   beq TestCase797
   br Done

TestCase797:
   ldc.i4 897
   stloc.0
   ldc.i4 0xffffeb93
   conv.i4
   not
   ldc.i4 0x146c
   beq TestCase796
   br Done

TestCase796:
   ldc.i4 896
   stloc.0
   ldc.i4 0xffff8427
   conv.i4
   not
   ldc.i4 0x7bd8
   bne.un Done

TestCase795:
   ldc.i4 895
   stloc.0
   ldc.i4 0x67b1
   conv.i4
   not
   ldc.i4 0xffff984e
   bne.un Done

TestCase794:
   ldc.i4 894
   stloc.0
   ldc.i4 0x1ed3
   conv.i4
   not
   ldc.i4 0xffffe12c
   beq TestCase793
   br Done

TestCase793:
   ldc.i4 893
   stloc.0
   ldc.i4 0xfffff865
   conv.i4
   not
   ldc.i4 0x79a
   bne.un Done

TestCase792:
   ldc.i4 892
   stloc.0
   ldc.i4 0xffff9a80
   conv.i4
   not
   ldc.i4 0x657f
   beq TestCase791
   br Done

TestCase791:
   ldc.i4 891
   stloc.0
   ldc.i4 0xffffce28
   conv.i4
   not
   ldc.i4 0x31d7
   bne.un Done

TestCase790:
   ldc.i4 890
   stloc.0
   ldc.i4 0xffff8aba
   conv.i4
   not
   ldc.i4 0x7545
   bne.un Done

TestCase789:
   ldc.i4 889
   stloc.0
   ldc.i4 0x7b18
   conv.i4
   not
   ldc.i4 0xffff84e7
   beq TestCase788
   br Done

TestCase788:
   ldc.i4 888
   stloc.0
   ldc.i4 0x656f
   conv.i4
   not
   ldc.i4 0xffff9a90
   bne.un Done

TestCase787:
   ldc.i4 887
   stloc.0
   ldc.i4 0x6b24
   conv.i4
   not
   ldc.i4 0xffff94db
   beq TestCase786
   br Done

TestCase786:
   ldc.i4 886
   stloc.0
   ldc.i4 0x7915
   conv.i4
   not
   ldc.i4 0xffff86ea
   bne.un Done

TestCase785:
   ldc.i4 885
   stloc.0
   ldc.i4 0x434b
   conv.i4
   not
   ldc.i4 0xffffbcb4
   bne.un Done

TestCase784:
   ldc.i4 884
   stloc.0
   ldc.i4 0x4e83
   conv.i4
   not
   ldc.i4 0xffffb17c
   beq TestCase783
   br Done

TestCase783:
   ldc.i4 883
   stloc.0
   ldc.i4 0x7d75
   conv.i4
   not
   ldc.i4 0xffff828a
   bne.un Done

TestCase782:
   ldc.i4 882
   stloc.0
   ldc.i4 0xffffdf38
   conv.i4
   not
   ldc.i4 0x20c7
   beq TestCase781
   br Done

TestCase781:
   ldc.i4 881
   stloc.0
   ldc.i4 0xffffe390
   conv.i4
   not
   ldc.i4 0x1c6f
   beq TestCase780
   br Done

TestCase780:
   ldc.i4 880
   stloc.0
   ldc.i4 0xffffdc95
   conv.i4
   not
   ldc.i4 0x236a
   bne.un Done

TestCase779:
   ldc.i4 879
   stloc.0
   ldc.i4 0x1665
   conv.i4
   not
   ldc.i4 0xffffe99a
   bne.un Done

TestCase778:
   ldc.i4 878
   stloc.0
   ldc.i4 0x6979
   conv.i4
   not
   ldc.i4 0xffff9686
   beq TestCase777
   br Done

TestCase777:
   ldc.i4 877
   stloc.0
   ldc.i4 0xfffff791
   conv.i4
   not
   ldc.i4 0x86e
   bne.un Done

TestCase776:
   ldc.i4 876
   stloc.0
   ldc.i4 0xffffff8c
   conv.i4
   not
   ldc.i4 0x73
   bne.un Done

TestCase775:
   ldc.i4 875
   stloc.0
   ldc.i4 0xfffff13d
   conv.i4
   not
   ldc.i4 0xec2
   bne.un Done

TestCase774:
   ldc.i4 874
   stloc.0
   ldc.i4 0xffffc342
   conv.i4
   not
   ldc.i4 0x3cbd
   beq TestCase773
   br Done

TestCase773:
   ldc.i4 873
   stloc.0
   ldc.i4 0x1ef9
   conv.i4
   not
   ldc.i4 0xffffe106
   beq TestCase772
   br Done

TestCase772:
   ldc.i4 872
   stloc.0
   ldc.i4 0x7899
   conv.i4
   not
   ldc.i4 0xffff8766
   beq TestCase771
   br Done

TestCase771:
   ldc.i4 871
   stloc.0
   ldc.i4 0x2a4c
   conv.i4
   not
   ldc.i4 0xffffd5b3
   beq TestCase770
   br Done

TestCase770:
   ldc.i4 870
   stloc.0
   ldc.i4 0x7d7
   conv.i4
   not
   ldc.i4 0xfffff828
   beq TestCase769
   br Done

TestCase769:
   ldc.i4 869
   stloc.0
   ldc.i4 0x2be9
   conv.i4
   not
   ldc.i4 0xffffd416
   bne.un Done

TestCase768:
   ldc.i4 868
   stloc.0
   ldc.i4 0xffffce36
   conv.i4
   not
   ldc.i4 0x31c9
   beq TestCase767
   br Done

TestCase767:
   ldc.i4 867
   stloc.0
   ldc.i4 0xffffd455
   conv.i4
   not
   ldc.i4 0x2baa
   beq TestCase766
   br Done

TestCase766:
   ldc.i4 866
   stloc.0
   ldc.i4 0x7018
   conv.i4
   not
   ldc.i4 0xffff8fe7
   bne.un Done

TestCase765:
   ldc.i4 865
   stloc.0
   ldc.i4 0xffffc828
   conv.i4
   not
   ldc.i4 0x37d7
   beq TestCase764
   br Done

TestCase764:
   ldc.i4 864
   stloc.0
   ldc.i4 0x4c7b
   conv.i4
   not
   ldc.i4 0xffffb384
   beq TestCase763
   br Done

TestCase763:
   ldc.i4 863
   stloc.0
   ldc.i4 0x78b3
   conv.i4
   not
   ldc.i4 0xffff874c
   beq TestCase762
   br Done

TestCase762:
   ldc.i4 862
   stloc.0
   ldc.i4 0x5098
   conv.i4
   not
   ldc.i4 0xffffaf67
   bne.un Done

TestCase761:
   ldc.i4 861
   stloc.0
   ldc.i4 0x5701
   conv.i4
   not
   ldc.i4 0xffffa8fe
   bne.un Done

TestCase760:
   ldc.i4 860
   stloc.0
   ldc.i4 0x6270
   conv.i4
   not
   ldc.i4 0xffff9d8f
   bne.un Done

TestCase759:
   ldc.i4 859
   stloc.0
   ldc.i4 0x4adb
   conv.i4
   not
   ldc.i4 0xffffb524
   beq TestCase758
   br Done

TestCase758:
   ldc.i4 858
   stloc.0
   ldc.i4 0xffff92e6
   conv.i4
   not
   ldc.i4 0x6d19
   bne.un Done

TestCase757:
   ldc.i4 857
   stloc.0
   ldc.i4 0xffffa5a9
   conv.i4
   not
   ldc.i4 0x5a56
   bne.un Done

TestCase756:
   ldc.i4 856
   stloc.0
   ldc.i4 0x28c7
   conv.i4
   not
   ldc.i4 0xffffd738
   bne.un Done

TestCase755:
   ldc.i4 855
   stloc.0
   ldc.i4 0x27f9
   conv.i4
   not
   ldc.i4 0xffffd806
   beq TestCase754
   br Done

TestCase754:
   ldc.i4 854
   stloc.0
   ldc.i4 0xfffff9c5
   conv.i4
   not
   ldc.i4 0x63a
   bne.un Done

TestCase753:
   ldc.i4 853
   stloc.0
   ldc.i4 0xffffc11c
   conv.i4
   not
   ldc.i4 0x3ee3
   beq TestCase752
   br Done

TestCase752:
   ldc.i4 852
   stloc.0
   ldc.i4 0x145
   conv.i4
   not
   ldc.i4 0xfffffeba
   bne.un Done

TestCase751:
   ldc.i4 851
   stloc.0
   ldc.i4 0xffffe0be
   conv.i4
   not
   ldc.i4 0x1f41
   bne.un Done

TestCase750:
   ldc.i4 850
   stloc.0
   ldc.i4 0xffffc524
   conv.i4
   not
   ldc.i4 0x3adb
   bne.un Done

TestCase749:
   ldc.i4 849
   stloc.0
   ldc.i4 0x7765
   conv.i4
   not
   ldc.i4 0xffff889a
   beq TestCase748
   br Done

TestCase748:
   ldc.i4 848
   stloc.0
   ldc.i4 0xfffff91b
   conv.i4
   not
   ldc.i4 0x6e4
   beq TestCase747
   br Done

TestCase747:
   ldc.i4 847
   stloc.0
   ldc.i4 0xfffff97e
   conv.i4
   not
   ldc.i4 0x681
   beq TestCase746
   br Done

TestCase746:
   ldc.i4 846
   stloc.0
   ldc.i4 0x62d2
   conv.i4
   not
   ldc.i4 0xffff9d2d
   bne.un Done

TestCase745:
   ldc.i4 845
   stloc.0
   ldc.i4 0x6c22
   conv.i4
   not
   ldc.i4 0xffff93dd
   bne.un Done

TestCase744:
   ldc.i4 844
   stloc.0
   ldc.i4 0x619e
   conv.i4
   not
   ldc.i4 0xffff9e61
   beq TestCase743
   br Done

TestCase743:
   ldc.i4 843
   stloc.0
   ldc.i4 0xffffcc69
   conv.i4
   not
   ldc.i4 0x3396
   bne.un Done

TestCase742:
   ldc.i4 842
   stloc.0
   ldc.i4 0x4ecc
   conv.i4
   not
   ldc.i4 0xffffb133
   beq TestCase741
   br Done

TestCase741:
   ldc.i4 841
   stloc.0
   ldc.i4 0xffffb2ee
   conv.i4
   not
   ldc.i4 0x4d11
   bne.un Done

TestCase740:
   ldc.i4 840
   stloc.0
   ldc.i4 0xffff928d
   conv.i4
   not
   ldc.i4 0x6d72
   bne.un Done

TestCase739:
   ldc.i4 839
   stloc.0
   ldc.i4 0xffff91cd
   conv.i4
   not
   ldc.i4 0x6e32
   beq TestCase738
   br Done

TestCase738:
   ldc.i4 838
   stloc.0
   ldc.i4 0x1984
   conv.i4
   not
   ldc.i4 0xffffe67b
   bne.un Done

TestCase737:
   ldc.i4 837
   stloc.0
   ldc.i4 0x122e
   conv.i4
   not
   ldc.i4 0xffffedd1
   bne.un Done

TestCase736:
   ldc.i4 836
   stloc.0
   ldc.i4 0xc0
   conv.i4
   not
   ldc.i4 0xffffff3f
   bne.un Done

TestCase735:
   ldc.i4 835
   stloc.0
   ldc.i4 0xffffea2e
   conv.i4
   not
   ldc.i4 0x15d1
   beq TestCase734
   br Done

TestCase734:
   ldc.i4 834
   stloc.0
   ldc.i4 0xffff89d8
   conv.i4
   not
   ldc.i4 0x7627
   beq TestCase733
   br Done

TestCase733:
   ldc.i4 833
   stloc.0
   ldc.i4 0xffffb9ec
   conv.i4
   not
   ldc.i4 0x4613
   bne.un Done

TestCase732:
   ldc.i4 832
   stloc.0
   ldc.i4 0xffffa23d
   conv.i4
   not
   ldc.i4 0x5dc2
   bne.un Done

TestCase731:
   ldc.i4 831
   stloc.0
   ldc.i4 0x37f8
   conv.i4
   not
   ldc.i4 0xffffc807
   bne.un Done

TestCase730:
   ldc.i4 830
   stloc.0
   ldc.i4 0xffffd7f6
   conv.i4
   not
   ldc.i4 0x2809
   beq TestCase729
   br Done

TestCase729:
   ldc.i4 829
   stloc.0
   ldc.i4 0x76e0
   conv.i4
   not
   ldc.i4 0xffff891f
   beq TestCase728
   br Done

TestCase728:
   ldc.i4 828
   stloc.0
   ldc.i4 0x1b69
   conv.i4
   not
   ldc.i4 0xffffe496
   bne.un Done

TestCase727:
   ldc.i4 827
   stloc.0
   ldc.i4 0xffffed51
   conv.i4
   not
   ldc.i4 0x12ae
   beq TestCase726
   br Done

TestCase726:
   ldc.i4 826
   stloc.0
   ldc.i4 0xfffff079
   conv.i4
   not
   ldc.i4 0xf86
   bne.un Done

TestCase725:
   ldc.i4 825
   stloc.0
   ldc.i4 0x6947
   conv.i4
   not
   ldc.i4 0xffff96b8
   bne.un Done

TestCase724:
   ldc.i4 824
   stloc.0
   ldc.i4 0x115a
   conv.i4
   not
   ldc.i4 0xffffeea5
   bne.un Done

TestCase723:
   ldc.i4 823
   stloc.0
   ldc.i4 0xffffc0c0
   conv.i4
   not
   ldc.i4 0x3f3f
   beq TestCase722
   br Done

TestCase722:
   ldc.i4 822
   stloc.0
   ldc.i4 0xffff87cc
   conv.i4
   not
   ldc.i4 0x7833
   beq TestCase721
   br Done

TestCase721:
   ldc.i4 821
   stloc.0
   ldc.i4 0xffff960c
   conv.i4
   not
   ldc.i4 0x69f3
   beq TestCase720
   br Done

TestCase720:
   ldc.i4 820
   stloc.0
   ldc.i4 0xffffecba
   conv.i4
   not
   ldc.i4 0x1345
   beq TestCase719
   br Done

TestCase719:
   ldc.i4 819
   stloc.0
   ldc.i4 0xfffffed0
   conv.i4
   not
   ldc.i4 0x12f
   beq TestCase718
   br Done

TestCase718:
   ldc.i4 818
   stloc.0
   ldc.i4 0x144f
   conv.i4
   not
   ldc.i4 0xffffebb0
   bne.un Done

TestCase717:
   ldc.i4 817
   stloc.0
   ldc.i4 0x5927
   conv.i4
   not
   ldc.i4 0xffffa6d8
   beq TestCase716
   br Done

TestCase716:
   ldc.i4 816
   stloc.0
   ldc.i4 0x771
   conv.i4
   not
   ldc.i4 0xfffff88e
   beq TestCase715
   br Done

TestCase715:
   ldc.i4 815
   stloc.0
   ldc.i4 0x757d
   conv.i4
   not
   ldc.i4 0xffff8a82
   beq TestCase714
   br Done

TestCase714:
   ldc.i4 814
   stloc.0
   ldc.i4 0xffffae9c
   conv.i4
   not
   ldc.i4 0x5163
   bne.un Done

TestCase713:
   ldc.i4 813
   stloc.0
   ldc.i4 0x4c8a
   conv.i4
   not
   ldc.i4 0xffffb375
   beq TestCase712
   br Done

TestCase712:
   ldc.i4 812
   stloc.0
   ldc.i4 0xffffdb84
   conv.i4
   not
   ldc.i4 0x247b
   bne.un Done

TestCase711:
   ldc.i4 811
   stloc.0
   ldc.i4 0x1e1a
   conv.i4
   not
   ldc.i4 0xffffe1e5
   beq TestCase710
   br Done

TestCase710:
   ldc.i4 810
   stloc.0
   ldc.i4 0xffff8dc3
   conv.i4
   not
   ldc.i4 0x723c
   beq TestCase709
   br Done

TestCase709:
   ldc.i4 809
   stloc.0
   ldc.i4 0x6356
   conv.i4
   not
   ldc.i4 0xffff9ca9
   beq TestCase708
   br Done

TestCase708:
   ldc.i4 808
   stloc.0
   ldc.i4 0xffff9d8b
   conv.i4
   not
   ldc.i4 0x6274
   bne.un Done

TestCase707:
   ldc.i4 807
   stloc.0
   ldc.i4 0x5a7
   conv.i4
   not
   ldc.i4 0xfffffa58
   bne.un Done

TestCase706:
   ldc.i4 806
   stloc.0
   ldc.i4 0xffffe5f1
   conv.i4
   not
   ldc.i4 0x1a0e
   bne.un Done

TestCase705:
   ldc.i4 805
   stloc.0
   ldc.i4 0xffffd75a
   conv.i4
   not
   ldc.i4 0x28a5
   beq TestCase704
   br Done

TestCase704:
   ldc.i4 804
   stloc.0
   ldc.i4 0xffff94de
   conv.i4
   not
   ldc.i4 0x6b21
   bne.un Done

TestCase703:
   ldc.i4 803
   stloc.0
   ldc.i4 0xffff888b
   conv.i4
   not
   ldc.i4 0x7774
   bne.un Done

TestCase702:
   ldc.i4 802
   stloc.0
   ldc.i4 0x34e3
   conv.i4
   not
   ldc.i4 0xffffcb1c
   bne.un Done

TestCase701:
   ldc.i4 801
   stloc.0
   ldc.i4 0xffffdfe6
   conv.i4
   not
   ldc.i4 0x2019
   beq TestCase700
   br Done

TestCase700:
   ldc.i4 800
   stloc.0
   ldc.i4 0x1589
   conv.i4
   not
   ldc.i4 0xffffea76
   bne.un Done

TestCase699:
   ldc.i4 799
   stloc.0
   ldc.i4 0x7b8
   conv.i4
   not
   ldc.i4 0xfffff847
   bne.un Done

TestCase698:
   ldc.i4 798
   stloc.0
   ldc.i4 0x6942
   conv.i4
   not
   ldc.i4 0xffff96bd
   bne.un Done

TestCase697:
   ldc.i4 797
   stloc.0
   ldc.i4 0xffffa750
   conv.i4
   not
   ldc.i4 0x58af
   bne.un Done

TestCase696:
   ldc.i4 796
   stloc.0
   ldc.i4 0x38c2
   conv.i4
   not
   ldc.i4 0xffffc73d
   beq TestCase695
   br Done

TestCase695:
   ldc.i4 795
   stloc.0
   ldc.i4 0x680e
   conv.i4
   not
   ldc.i4 0xffff97f1
   beq TestCase694
   br Done

TestCase694:
   ldc.i4 794
   stloc.0
   ldc.i4 0x7f23
   conv.i4
   not
   ldc.i4 0xffff80dc
   beq TestCase693
   br Done

TestCase693:
   ldc.i4 793
   stloc.0
   ldc.i4 0xffffde22
   conv.i4
   not
   ldc.i4 0x21dd
   beq TestCase692
   br Done

TestCase692:
   ldc.i4 792
   stloc.0
   ldc.i4 0xffff9dff
   conv.i4
   not
   ldc.i4 0x6200
   bne.un Done

TestCase691:
   ldc.i4 791
   stloc.0
   ldc.i4 0x4430
   conv.i4
   not
   ldc.i4 0xffffbbcf
   bne.un Done

TestCase690:
   ldc.i4 790
   stloc.0
   ldc.i4 0xffff97e7
   conv.i4
   not
   ldc.i4 0x6818
   beq TestCase689
   br Done

TestCase689:
   ldc.i4 789
   stloc.0
   ldc.i4 0x3ef3
   conv.i4
   not
   ldc.i4 0xffffc10c
   bne.un Done

TestCase688:
   ldc.i4 788
   stloc.0
   ldc.i4 0x39de
   conv.i4
   not
   ldc.i4 0xffffc621
   bne.un Done

TestCase687:
   ldc.i4 787
   stloc.0
   ldc.i4 0x22f6
   conv.i4
   not
   ldc.i4 0xffffdd09
   beq TestCase686
   br Done

TestCase686:
   ldc.i4 786
   stloc.0
   ldc.i4 0x54b7
   conv.i4
   not
   ldc.i4 0xffffab48
   bne.un Done

TestCase685:
   ldc.i4 785
   stloc.0
   ldc.i4 0xffffcb52
   conv.i4
   not
   ldc.i4 0x34ad
   bne.un Done

TestCase684:
   ldc.i4 784
   stloc.0
   ldc.i4 0x24e6
   conv.i4
   not
   ldc.i4 0xffffdb19
   bne.un Done

TestCase683:
   ldc.i4 783
   stloc.0
   ldc.i4 0xfffff870
   conv.i4
   not
   ldc.i4 0x78f
   bne.un Done

TestCase682:
   ldc.i4 782
   stloc.0
   ldc.i4 0xfffffcb9
   conv.i4
   not
   ldc.i4 0x346
   bne.un Done

TestCase681:
   ldc.i4 781
   stloc.0
   ldc.i4 0xd1f
   conv.i4
   not
   ldc.i4 0xfffff2e0
   beq TestCase680
   br Done

TestCase680:
   ldc.i4 780
   stloc.0
   ldc.i4 0xffff9499
   conv.i4
   not
   ldc.i4 0x6b66
   beq TestCase679
   br Done

TestCase679:
   ldc.i4 779
   stloc.0
   ldc.i4 0x4f02
   conv.i4
   not
   ldc.i4 0xffffb0fd
   beq TestCase678
   br Done

TestCase678:
   ldc.i4 778
   stloc.0
   ldc.i4 0x33ab
   conv.i4
   not
   ldc.i4 0xffffcc54
   beq TestCase677
   br Done

TestCase677:
   ldc.i4 777
   stloc.0
   ldc.i4 0x2dbe
   conv.i4
   not
   ldc.i4 0xffffd241
   beq TestCase676
   br Done

TestCase676:
   ldc.i4 776
   stloc.0
   ldc.i4 0xffff8b89
   conv.i4
   not
   ldc.i4 0x7476
   bne.un Done

TestCase675:
   ldc.i4 775
   stloc.0
   ldc.i4 0xfffffa1f
   conv.i4
   not
   ldc.i4 0x5e0
   beq TestCase674
   br Done

TestCase674:
   ldc.i4 774
   stloc.0
   ldc.i4 0x6c93
   conv.i4
   not
   ldc.i4 0xffff936c
   beq TestCase673
   br Done

TestCase673:
   ldc.i4 773
   stloc.0
   ldc.i4 0x1bc9
   conv.i4
   not
   ldc.i4 0xffffe436
   bne.un Done

TestCase672:
   ldc.i4 772
   stloc.0
   ldc.i4 0xffff9fba
   conv.i4
   not
   ldc.i4 0x6045
   beq TestCase671
   br Done

TestCase671:
   ldc.i4 771
   stloc.0
   ldc.i4 0xffffcdb5
   conv.i4
   not
   ldc.i4 0x324a
   bne.un Done

TestCase670:
   ldc.i4 770
   stloc.0
   ldc.i4 0xffff8220
   conv.i4
   not
   ldc.i4 0x7ddf
   bne.un Done

TestCase669:
   ldc.i4 769
   stloc.0
   ldc.i4 0xffffb51e
   conv.i4
   not
   ldc.i4 0x4ae1
   bne.un Done

TestCase668:
   ldc.i4 768
   stloc.0
   ldc.i4 0xfffff25c
   conv.i4
   not
   ldc.i4 0xda3
   beq TestCase667
   br Done

TestCase667:
   ldc.i4 767
   stloc.0
   ldc.i4 0xffffb1d4
   conv.i4
   not
   ldc.i4 0x4e2b
   beq TestCase666
   br Done

TestCase666:
   ldc.i4 766
   stloc.0
   ldc.i4 0xffff93aa
   conv.i4
   not
   ldc.i4 0x6c55
   beq TestCase665
   br Done

TestCase665:
   ldc.i4 765
   stloc.0
   ldc.i4 0xffffae18
   conv.i4
   not
   ldc.i4 0x51e7
   beq TestCase664
   br Done

TestCase664:
   ldc.i4 764
   stloc.0
   ldc.i4 0xffffa92b
   conv.i4
   not
   ldc.i4 0x56d4
   bne.un Done

TestCase663:
   ldc.i4 763
   stloc.0
   ldc.i4 0x6dae
   conv.i4
   not
   ldc.i4 0xffff9251
   beq TestCase662
   br Done

TestCase662:
   ldc.i4 762
   stloc.0
   ldc.i4 0x3288
   conv.i4
   not
   ldc.i4 0xffffcd77
   bne.un Done

TestCase661:
   ldc.i4 761
   stloc.0
   ldc.i4 0xffffddec
   conv.i4
   not
   ldc.i4 0x2213
   beq TestCase660
   br Done

TestCase660:
   ldc.i4 760
   stloc.0
   ldc.i4 0x1194
   conv.i4
   not
   ldc.i4 0xffffee6b
   bne.un Done

TestCase659:
   ldc.i4 759
   stloc.0
   ldc.i4 0x4e01
   conv.i4
   not
   ldc.i4 0xffffb1fe
   bne.un Done

TestCase658:
   ldc.i4 758
   stloc.0
   ldc.i4 0x300
   conv.i4
   not
   ldc.i4 0xfffffcff
   beq TestCase657
   br Done

TestCase657:
   ldc.i4 757
   stloc.0
   ldc.i4 0xffffe3a9
   conv.i4
   not
   ldc.i4 0x1c56
   bne.un Done

TestCase656:
   ldc.i4 756
   stloc.0
   ldc.i4 0xffffe19c
   conv.i4
   not
   ldc.i4 0x1e63
   bne.un Done

TestCase655:
   ldc.i4 755
   stloc.0
   ldc.i4 0x484c
   conv.i4
   not
   ldc.i4 0xffffb7b3
   bne.un Done

TestCase654:
   ldc.i4 754
   stloc.0
   ldc.i4 0x77ae
   conv.i4
   not
   ldc.i4 0xffff8851
   bne.un Done

TestCase653:
   ldc.i4 753
   stloc.0
   ldc.i4 0xffffdf0d
   conv.i4
   not
   ldc.i4 0x20f2
   bne.un Done

TestCase652:
   ldc.i4 752
   stloc.0
   ldc.i4 0x131e
   conv.i4
   not
   ldc.i4 0xffffece1
   beq TestCase651
   br Done

TestCase651:
   ldc.i4 751
   stloc.0
   ldc.i4 0xffff9b0e
   conv.i4
   not
   ldc.i4 0x64f1
   beq TestCase650
   br Done

TestCase650:
   ldc.i4 750
   stloc.0
   ldc.i4 0xffffa56d
   conv.i4
   not
   ldc.i4 0x5a92
   beq TestCase649
   br Done

TestCase649:
   ldc.i4 749
   stloc.0
   ldc.i4 0xffff8aa8
   conv.i4
   not
   ldc.i4 0x7557
   beq TestCase648
   br Done

TestCase648:
   ldc.i4 748
   stloc.0
   ldc.i4 0x4cb7
   conv.i4
   not
   ldc.i4 0xffffb348
   beq TestCase647
   br Done

TestCase647:
   ldc.i4 747
   stloc.0
   ldc.i4 0xb09
   conv.i4
   not
   ldc.i4 0xfffff4f6
   beq TestCase646
   br Done

TestCase646:
   ldc.i4 746
   stloc.0
   ldc.i4 0xffff8c52
   conv.i4
   not
   ldc.i4 0x73ad
   bne.un Done

TestCase645:
   ldc.i4 745
   stloc.0
   ldc.i4 0xffffe3bf
   conv.i4
   not
   ldc.i4 0x1c40
   bne.un Done

TestCase644:
   ldc.i4 744
   stloc.0
   ldc.i4 0xffff8baf
   conv.i4
   not
   ldc.i4 0x7450
   beq TestCase643
   br Done

TestCase643:
   ldc.i4 743
   stloc.0
   ldc.i4 0xfffff7e9
   conv.i4
   not
   ldc.i4 0x816
   beq TestCase642
   br Done

TestCase642:
   ldc.i4 742
   stloc.0
   ldc.i4 0xfffff7db
   conv.i4
   not
   ldc.i4 0x824
   bne.un Done

TestCase641:
   ldc.i4 741
   stloc.0
   ldc.i4 0xffff83f2
   conv.i4
   not
   ldc.i4 0x7c0d
   beq TestCase640
   br Done

TestCase640:
   ldc.i4 740
   stloc.0
   ldc.i4 0xffffbc7c
   conv.i4
   not
   ldc.i4 0x4383
   beq TestCase639
   br Done

TestCase639:
   ldc.i4 739
   stloc.0
   ldc.i4 0xffff93f2
   conv.i4
   not
   ldc.i4 0x6c0d
   beq TestCase638
   br Done

TestCase638:
   ldc.i4 738
   stloc.0
   ldc.i4 0xffff8949
   conv.i4
   not
   ldc.i4 0x76b6
   bne.un Done

TestCase637:
   ldc.i4 737
   stloc.0
   ldc.i4 0xffff909d
   conv.i4
   not
   ldc.i4 0x6f62
   bne.un Done

TestCase636:
   ldc.i4 736
   stloc.0
   ldc.i4 0x13ee
   conv.i4
   not
   ldc.i4 0xffffec11
   beq TestCase635
   br Done

TestCase635:
   ldc.i4 735
   stloc.0
   ldc.i4 0x758a
   conv.i4
   not
   ldc.i4 0xffff8a75
   beq TestCase634
   br Done

TestCase634:
   ldc.i4 734
   stloc.0
   ldc.i4 0xffffe2a0
   conv.i4
   not
   ldc.i4 0x1d5f
   bne.un Done

TestCase633:
   ldc.i4 733
   stloc.0
   ldc.i4 0x1c00
   conv.i4
   not
   ldc.i4 0xffffe3ff
   beq TestCase632
   br Done

TestCase632:
   ldc.i4 732
   stloc.0
   ldc.i4 0x5f8
   conv.i4
   not
   ldc.i4 0xfffffa07
   bne.un Done

TestCase631:
   ldc.i4 731
   stloc.0
   ldc.i4 0xa49
   conv.i4
   not
   ldc.i4 0xfffff5b6
   bne.un Done

TestCase630:
   ldc.i4 730
   stloc.0
   ldc.i4 0x744e
   conv.i4
   not
   ldc.i4 0xffff8bb1
   bne.un Done

TestCase629:
   ldc.i4 729
   stloc.0
   ldc.i4 0x2be2
   conv.i4
   not
   ldc.i4 0xffffd41d
   beq TestCase628
   br Done

TestCase628:
   ldc.i4 728
   stloc.0
   ldc.i4 0xffffb516
   conv.i4
   not
   ldc.i4 0x4ae9
   bne.un Done

TestCase627:
   ldc.i4 727
   stloc.0
   ldc.i4 0xffff8816
   conv.i4
   not
   ldc.i4 0x77e9
   beq TestCase626
   br Done

TestCase626:
   ldc.i4 726
   stloc.0
   ldc.i4 0xfffff5fc
   conv.i4
   not
   ldc.i4 0xa03
   beq TestCase625
   br Done

TestCase625:
   ldc.i4 725
   stloc.0
   ldc.i4 0x449b
   conv.i4
   not
   ldc.i4 0xffffbb64
   beq TestCase624
   br Done

TestCase624:
   ldc.i4 724
   stloc.0
   ldc.i4 0x5e31
   conv.i4
   not
   ldc.i4 0xffffa1ce
   beq TestCase623
   br Done

TestCase623:
   ldc.i4 723
   stloc.0
   ldc.i4 0xffff8716
   conv.i4
   not
   ldc.i4 0x78e9
   beq TestCase622
   br Done

TestCase622:
   ldc.i4 722
   stloc.0
   ldc.i4 0x4ccc
   conv.i4
   not
   ldc.i4 0xffffb333
   beq TestCase621
   br Done

TestCase621:
   ldc.i4 721
   stloc.0
   ldc.i4 0xffffa712
   conv.i4
   not
   ldc.i4 0x58ed
   beq TestCase620
   br Done

TestCase620:
   ldc.i4 720
   stloc.0
   ldc.i4 0x39ef
   conv.i4
   not
   ldc.i4 0xffffc610
   bne.un Done

TestCase619:
   ldc.i4 719
   stloc.0
   ldc.i4 0xffffc625
   conv.i4
   not
   ldc.i4 0x39da
   bne.un Done

TestCase618:
   ldc.i4 718
   stloc.0
   ldc.i4 0xffffb29f
   conv.i4
   not
   ldc.i4 0x4d60
   bne.un Done

TestCase617:
   ldc.i4 717
   stloc.0
   ldc.i4 0x30ae
   conv.i4
   not
   ldc.i4 0xffffcf51
   beq TestCase616
   br Done

TestCase616:
   ldc.i4 716
   stloc.0
   ldc.i4 0x52c4
   conv.i4
   not
   ldc.i4 0xffffad3b
   bne.un Done

TestCase615:
   ldc.i4 715
   stloc.0
   ldc.i4 0x5ae6
   conv.i4
   not
   ldc.i4 0xffffa519
   bne.un Done

TestCase614:
   ldc.i4 714
   stloc.0
   ldc.i4 0x66dc
   conv.i4
   not
   ldc.i4 0xffff9923
   bne.un Done

TestCase613:
   ldc.i4 713
   stloc.0
   ldc.i4 0x42d8
   conv.i4
   not
   ldc.i4 0xffffbd27
   bne.un Done

TestCase612:
   ldc.i4 712
   stloc.0
   ldc.i4 0x66da
   conv.i4
   not
   ldc.i4 0xffff9925
   bne.un Done

TestCase611:
   ldc.i4 711
   stloc.0
   ldc.i4 0x2982
   conv.i4
   not
   ldc.i4 0xffffd67d
   bne.un Done

TestCase610:
   ldc.i4 710
   stloc.0
   ldc.i4 0x77df
   conv.i4
   not
   ldc.i4 0xffff8820
   bne.un Done

TestCase609:
   ldc.i4 709
   stloc.0
   ldc.i4 0xffffc212
   conv.i4
   not
   ldc.i4 0x3ded
   beq TestCase608
   br Done

TestCase608:
   ldc.i4 708
   stloc.0
   ldc.i4 0xffff93ca
   conv.i4
   not
   ldc.i4 0x6c35
   beq TestCase607
   br Done

TestCase607:
   ldc.i4 707
   stloc.0
   ldc.i4 0xfffff18b
   conv.i4
   not
   ldc.i4 0xe74
   bne.un Done

TestCase606:
   ldc.i4 706
   stloc.0
   ldc.i4 0x1900
   conv.i4
   not
   ldc.i4 0xffffe6ff
   beq TestCase605
   br Done

TestCase605:
   ldc.i4 705
   stloc.0
   ldc.i4 0x5a95
   conv.i4
   not
   ldc.i4 0xffffa56a
   beq TestCase604
   br Done

TestCase604:
   ldc.i4 704
   stloc.0
   ldc.i4 0x2a3d
   conv.i4
   not
   ldc.i4 0xffffd5c2
   beq TestCase603
   br Done

TestCase603:
   ldc.i4 703
   stloc.0
   ldc.i4 0x6c0c
   conv.i4
   not
   ldc.i4 0xffff93f3
   bne.un Done

TestCase602:
   ldc.i4 702
   stloc.0
   ldc.i4 0x7c78
   conv.i4
   not
   ldc.i4 0xffff8387
   bne.un Done

TestCase601:
   ldc.i4 701
   stloc.0
   ldc.i4 0xffffab8f
   conv.i4
   not
   ldc.i4 0x5470
   bne.un Done

TestCase600:
   ldc.i4 700
   stloc.0
   ldc.i4 0xffffb57c
   conv.i4
   not
   ldc.i4 0x4a83
   bne.un Done

TestCase599:
   ldc.i4 699
   stloc.0
   ldc.i4 0x6896
   conv.i4
   not
   ldc.i4 0xffff9769
   beq TestCase598
   br Done

TestCase598:
   ldc.i4 698
   stloc.0
   ldc.i4 0xffff9cf4
   conv.i4
   not
   ldc.i4 0x630b
   beq TestCase597
   br Done

TestCase597:
   ldc.i4 697
   stloc.0
   ldc.i4 0xffffdac0
   conv.i4
   not
   ldc.i4 0x253f
   beq TestCase596
   br Done

TestCase596:
   ldc.i4 696
   stloc.0
   ldc.i4 0x6cdd
   conv.i4
   not
   ldc.i4 0xffff9322
   beq TestCase595
   br Done

TestCase595:
   ldc.i4 695
   stloc.0
   ldc.i4 0xffff9edb
   conv.i4
   not
   ldc.i4 0x6124
   beq TestCase594
   br Done

TestCase594:
   ldc.i4 694
   stloc.0
   ldc.i4 0xffffadc6
   conv.i4
   not
   ldc.i4 0x5239
   bne.un Done

TestCase593:
   ldc.i4 693
   stloc.0
   ldc.i4 0xffffa1cb
   conv.i4
   not
   ldc.i4 0x5e34
   beq TestCase592
   br Done

TestCase592:
   ldc.i4 692
   stloc.0
   ldc.i4 0xa9e
   conv.i4
   not
   ldc.i4 0xfffff561
   bne.un Done

TestCase591:
   ldc.i4 691
   stloc.0
   ldc.i4 0xffffff78
   conv.i4
   not
   ldc.i4 0x87
   beq TestCase590
   br Done

TestCase590:
   ldc.i4 690
   stloc.0
   ldc.i4 0x77a7
   conv.i4
   not
   ldc.i4 0xffff8858
   bne.un Done

TestCase589:
   ldc.i4 689
   stloc.0
   ldc.i4 0xffffc46a
   conv.i4
   not
   ldc.i4 0x3b95
   beq TestCase588
   br Done

TestCase588:
   ldc.i4 688
   stloc.0
   ldc.i4 0xffffcace
   conv.i4
   not
   ldc.i4 0x3531
   bne.un Done

TestCase587:
   ldc.i4 687
   stloc.0
   ldc.i4 0x3683
   conv.i4
   not
   ldc.i4 0xffffc97c
   bne.un Done

TestCase586:
   ldc.i4 686
   stloc.0
   ldc.i4 0x65c8
   conv.i4
   not
   ldc.i4 0xffff9a37
   bne.un Done

TestCase585:
   ldc.i4 685
   stloc.0
   ldc.i4 0x4e99
   conv.i4
   not
   ldc.i4 0xffffb166
   bne.un Done

TestCase584:
   ldc.i4 684
   stloc.0
   ldc.i4 0x7be5
   conv.i4
   not
   ldc.i4 0xffff841a
   bne.un Done

TestCase583:
   ldc.i4 683
   stloc.0
   ldc.i4 0xfffff4b0
   conv.i4
   not
   ldc.i4 0xb4f
   bne.un Done

TestCase582:
   ldc.i4 682
   stloc.0
   ldc.i4 0x15cf
   conv.i4
   not
   ldc.i4 0xffffea30
   bne.un Done

TestCase581:
   ldc.i4 681
   stloc.0
   ldc.i4 0x597a
   conv.i4
   not
   ldc.i4 0xffffa685
   bne.un Done

TestCase580:
   ldc.i4 680
   stloc.0
   ldc.i4 0xfffffb14
   conv.i4
   not
   ldc.i4 0x4eb
   bne.un Done

TestCase579:
   ldc.i4 679
   stloc.0
   ldc.i4 0xb5b
   conv.i4
   not
   ldc.i4 0xfffff4a4
   beq TestCase578
   br Done

TestCase578:
   ldc.i4 678
   stloc.0
   ldc.i4 0x5ad8
   conv.i4
   not
   ldc.i4 0xffffa527
   beq TestCase577
   br Done

TestCase577:
   ldc.i4 677
   stloc.0
   ldc.i4 0x39e0
   conv.i4
   not
   ldc.i4 0xffffc61f
   bne.un Done

TestCase576:
   ldc.i4 676
   stloc.0
   ldc.i4 0x25b3
   conv.i4
   not
   ldc.i4 0xffffda4c
   bne.un Done

TestCase575:
   ldc.i4 675
   stloc.0
   ldc.i4 0x4738
   conv.i4
   not
   ldc.i4 0xffffb8c7
   beq TestCase574
   br Done

TestCase574:
   ldc.i4 674
   stloc.0
   ldc.i4 0x3254
   conv.i4
   not
   ldc.i4 0xffffcdab
   bne.un Done

TestCase573:
   ldc.i4 673
   stloc.0
   ldc.i4 0xffff828f
   conv.i4
   not
   ldc.i4 0x7d70
   beq TestCase572
   br Done

TestCase572:
   ldc.i4 672
   stloc.0
   ldc.i4 0x66a
   conv.i4
   not
   ldc.i4 0xfffff995
   beq TestCase571
   br Done

TestCase571:
   ldc.i4 671
   stloc.0
   ldc.i4 0x23e8
   conv.i4
   not
   ldc.i4 0xffffdc17
   beq TestCase570
   br Done

TestCase570:
   ldc.i4 670
   stloc.0
   ldc.i4 0xffffe24b
   conv.i4
   not
   ldc.i4 0x1db4
   beq TestCase569
   br Done

TestCase569:
   ldc.i4 669
   stloc.0
   ldc.i4 0xffff834d
   conv.i4
   not
   ldc.i4 0x7cb2
   beq TestCase568
   br Done

TestCase568:
   ldc.i4 668
   stloc.0
   ldc.i4 0xffffe32f
   conv.i4
   not
   ldc.i4 0x1cd0
   beq TestCase567
   br Done

TestCase567:
   ldc.i4 667
   stloc.0
   ldc.i4 0x774d
   conv.i4
   not
   ldc.i4 0xffff88b2
   bne.un Done

TestCase566:
   ldc.i4 666
   stloc.0
   ldc.i4 0x715a
   conv.i4
   not
   ldc.i4 0xffff8ea5
   bne.un Done

TestCase565:
   ldc.i4 665
   stloc.0
   ldc.i4 0x1825
   conv.i4
   not
   ldc.i4 0xffffe7da
   beq TestCase564
   br Done

TestCase564:
   ldc.i4 664
   stloc.0
   ldc.i4 0xa43
   conv.i4
   not
   ldc.i4 0xfffff5bc
   bne.un Done

TestCase563:
   ldc.i4 663
   stloc.0
   ldc.i4 0x2ce9
   conv.i4
   not
   ldc.i4 0xffffd316
   beq TestCase562
   br Done

TestCase562:
   ldc.i4 662
   stloc.0
   ldc.i4 0xffffbb44
   conv.i4
   not
   ldc.i4 0x44bb
   beq TestCase561
   br Done

TestCase561:
   ldc.i4 661
   stloc.0
   ldc.i4 0x739d
   conv.i4
   not
   ldc.i4 0xffff8c62
   bne.un Done

TestCase560:
   ldc.i4 660
   stloc.0
   ldc.i4 0x2900
   conv.i4
   not
   ldc.i4 0xffffd6ff
   bne.un Done

TestCase559:
   ldc.i4 659
   stloc.0
   ldc.i4 0xffff9122
   conv.i4
   not
   ldc.i4 0x6edd
   bne.un Done

TestCase558:
   ldc.i4 658
   stloc.0
   ldc.i4 0x6fd9
   conv.i4
   not
   ldc.i4 0xffff9026
   beq TestCase557
   br Done

TestCase557:
   ldc.i4 657
   stloc.0
   ldc.i4 0xffffb41c
   conv.i4
   not
   ldc.i4 0x4be3
   beq TestCase556
   br Done

TestCase556:
   ldc.i4 656
   stloc.0
   ldc.i4 0xffffab41
   conv.i4
   not
   ldc.i4 0x54be
   beq TestCase555
   br Done

TestCase555:
   ldc.i4 655
   stloc.0
   ldc.i4 0xfffff0de
   conv.i4
   not
   ldc.i4 0xf21
   beq TestCase554
   br Done

TestCase554:
   ldc.i4 654
   stloc.0
   ldc.i4 0x41e0
   conv.i4
   not
   ldc.i4 0xffffbe1f
   bne.un Done

TestCase553:
   ldc.i4 653
   stloc.0
   ldc.i4 0xffffbd71
   conv.i4
   not
   ldc.i4 0x428e
   bne.un Done

TestCase552:
   ldc.i4 652
   stloc.0
   ldc.i4 0xffffc58c
   conv.i4
   not
   ldc.i4 0x3a73
   bne.un Done

TestCase551:
   ldc.i4 651
   stloc.0
   ldc.i4 0xffffd2ff
   conv.i4
   not
   ldc.i4 0x2d00
   bne.un Done

TestCase550:
   ldc.i4 650
   stloc.0
   ldc.i4 0xffffa2e2
   conv.i4
   not
   ldc.i4 0x5d1d
   bne.un Done

TestCase549:
   ldc.i4 649
   stloc.0
   ldc.i4 0x3c20
   conv.i4
   not
   ldc.i4 0xffffc3df
   beq TestCase548
   br Done

TestCase548:
   ldc.i4 648
   stloc.0
   ldc.i4 0x3b5d
   conv.i4
   not
   ldc.i4 0xffffc4a2
   bne.un Done

TestCase547:
   ldc.i4 647
   stloc.0
   ldc.i4 0xffffad28
   conv.i4
   not
   ldc.i4 0x52d7
   beq TestCase546
   br Done

TestCase546:
   ldc.i4 646
   stloc.0
   ldc.i4 0x6428
   conv.i4
   not
   ldc.i4 0xffff9bd7
   bne.un Done

TestCase545:
   ldc.i4 645
   stloc.0
   ldc.i4 0xffffd3a3
   conv.i4
   not
   ldc.i4 0x2c5c
   bne.un Done

TestCase544:
   ldc.i4 644
   stloc.0
   ldc.i4 0xffffd0ae
   conv.i4
   not
   ldc.i4 0x2f51
   beq TestCase543
   br Done

TestCase543:
   ldc.i4 643
   stloc.0
   ldc.i4 0xfffff7f0
   conv.i4
   not
   ldc.i4 0x80f
   beq TestCase542
   br Done

TestCase542:
   ldc.i4 642
   stloc.0
   ldc.i4 0x644a
   conv.i4
   not
   ldc.i4 0xffff9bb5
   beq TestCase541
   br Done

TestCase541:
   ldc.i4 641
   stloc.0
   ldc.i4 0x6cef
   conv.i4
   not
   ldc.i4 0xffff9310
   bne.un Done

TestCase540:
   ldc.i4 640
   stloc.0
   ldc.i4 0x124d
   conv.i4
   not
   ldc.i4 0xffffedb2
   beq TestCase539
   br Done

TestCase539:
   ldc.i4 639
   stloc.0
   ldc.i4 0x1358
   conv.i4
   not
   ldc.i4 0xffffeca7
   bne.un Done

TestCase538:
   ldc.i4 638
   stloc.0
   ldc.i4 0x292f
   conv.i4
   not
   ldc.i4 0xffffd6d0
   bne.un Done

TestCase537:
   ldc.i4 637
   stloc.0
   ldc.i4 0xffff85c3
   conv.i4
   not
   ldc.i4 0x7a3c
   bne.un Done

TestCase536:
   ldc.i4 636
   stloc.0
   ldc.i4 0x6937
   conv.i4
   not
   ldc.i4 0xffff96c8
   bne.un Done

TestCase535:
   ldc.i4 635
   stloc.0
   ldc.i4 0x6926
   conv.i4
   not
   ldc.i4 0xffff96d9
   beq TestCase534
   br Done

TestCase534:
   ldc.i4 634
   stloc.0
   ldc.i4 0x65d0
   conv.i4
   not
   ldc.i4 0xffff9a2f
   bne.un Done

TestCase533:
   ldc.i4 633
   stloc.0
   ldc.i4 0xffffcd76
   conv.i4
   not
   ldc.i4 0x3289
   bne.un Done

TestCase532:
   ldc.i4 632
   stloc.0
   ldc.i4 0x69f5
   conv.i4
   not
   ldc.i4 0xffff960a
   beq TestCase531
   br Done

TestCase531:
   ldc.i4 631
   stloc.0
   ldc.i4 0x26f
   conv.i4
   not
   ldc.i4 0xfffffd90
   bne.un Done

TestCase530:
   ldc.i4 630
   stloc.0
   ldc.i4 0x5ea1
   conv.i4
   not
   ldc.i4 0xffffa15e
   beq TestCase529
   br Done

TestCase529:
   ldc.i4 629
   stloc.0
   ldc.i4 0x734d
   conv.i4
   not
   ldc.i4 0xffff8cb2
   beq TestCase528
   br Done

TestCase528:
   ldc.i4 628
   stloc.0
   ldc.i4 0xffffcd4e
   conv.i4
   not
   ldc.i4 0x32b1
   bne.un Done

TestCase527:
   ldc.i4 627
   stloc.0
   ldc.i4 0xffffe85c
   conv.i4
   not
   ldc.i4 0x17a3
   bne.un Done

TestCase526:
   ldc.i4 626
   stloc.0
   ldc.i4 0xffffff07
   conv.i4
   not
   ldc.i4 0xf8
   beq TestCase525
   br Done

TestCase525:
   ldc.i4 625
   stloc.0
   ldc.i4 0xffffcf30
   conv.i4
   not
   ldc.i4 0x30cf
   bne.un Done

TestCase524:
   ldc.i4 624
   stloc.0
   ldc.i4 0x1c6e
   conv.i4
   not
   ldc.i4 0xffffe391
   bne.un Done

TestCase523:
   ldc.i4 623
   stloc.0
   ldc.i4 0x7bf0
   conv.i4
   not
   ldc.i4 0xffff840f
   beq TestCase522
   br Done

TestCase522:
   ldc.i4 622
   stloc.0
   ldc.i4 0x13c2
   conv.i4
   not
   ldc.i4 0xffffec3d
   bne.un Done

TestCase521:
   ldc.i4 621
   stloc.0
   ldc.i4 0xffff9bf5
   conv.i4
   not
   ldc.i4 0x640a
   beq TestCase520
   br Done

TestCase520:
   ldc.i4 620
   stloc.0
   ldc.i4 0xffff8b39
   conv.i4
   not
   ldc.i4 0x74c6
   beq TestCase519
   br Done

TestCase519:
   ldc.i4 619
   stloc.0
   ldc.i4 0x14b0
   conv.i4
   not
   ldc.i4 0xffffeb4f
   beq TestCase518
   br Done

TestCase518:
   ldc.i4 618
   stloc.0
   ldc.i4 0xffff929e
   conv.i4
   not
   ldc.i4 0x6d61
   beq TestCase517
   br Done

TestCase517:
   ldc.i4 617
   stloc.0
   ldc.i4 0x6ac2
   conv.i4
   not
   ldc.i4 0xffff953d
   bne.un Done

TestCase516:
   ldc.i4 616
   stloc.0
   ldc.i4 0x2b06
   conv.i4
   not
   ldc.i4 0xffffd4f9
   bne.un Done

TestCase515:
   ldc.i4 615
   stloc.0
   ldc.i4 0x2933
   conv.i4
   not
   ldc.i4 0xffffd6cc
   bne.un Done

TestCase514:
   ldc.i4 614
   stloc.0
   ldc.i4 0xffffa518
   conv.i4
   not
   ldc.i4 0x5ae7
   beq TestCase513
   br Done

TestCase513:
   ldc.i4 613
   stloc.0
   ldc.i4 0xffffca24
   conv.i4
   not
   ldc.i4 0x35db
   beq TestCase512
   br Done

TestCase512:
   ldc.i4 612
   stloc.0
   ldc.i4 0xffffc394
   conv.i4
   not
   ldc.i4 0x3c6b
   bne.un Done

TestCase511:
   ldc.i4 611
   stloc.0
   ldc.i4 0xfffff94d
   conv.i4
   not
   ldc.i4 0x6b2
   bne.un Done

TestCase510:
   ldc.i4 610
   stloc.0
   ldc.i4 0x247a
   conv.i4
   not
   ldc.i4 0xffffdb85
   bne.un Done

TestCase509:
   ldc.i4 609
   stloc.0
   ldc.i4 0x6206
   conv.i4
   not
   ldc.i4 0xffff9df9
   beq TestCase508
   br Done

TestCase508:
   ldc.i4 608
   stloc.0
   ldc.i4 0x6bbe
   conv.i4
   not
   ldc.i4 0xffff9441
   beq TestCase507
   br Done

TestCase507:
   ldc.i4 607
   stloc.0
   ldc.i4 0x3492
   conv.i4
   not
   ldc.i4 0xffffcb6d
   beq TestCase506
   br Done

TestCase506:
   ldc.i4 606
   stloc.0
   ldc.i4 0x5afe
   conv.i4
   not
   ldc.i4 0xffffa501
   bne.un Done

TestCase505:
   ldc.i4 605
   stloc.0
   ldc.i4 0xffffe978
   conv.i4
   not
   ldc.i4 0x1687
   bne.un Done

TestCase504:
   ldc.i4 604
   stloc.0
   ldc.i4 0xffffeff2
   conv.i4
   not
   ldc.i4 0x100d
   bne.un Done

TestCase503:
   ldc.i4 603
   stloc.0
   ldc.i4 0x34d9
   conv.i4
   not
   ldc.i4 0xffffcb26
   bne.un Done

TestCase502:
   ldc.i4 602
   stloc.0
   ldc.i4 0xffffa4f9
   conv.i4
   not
   ldc.i4 0x5b06
   bne.un Done

TestCase501:
   ldc.i4 601
   stloc.0
   ldc.i4 0x4666
   conv.i4
   not
   ldc.i4 0xffffb999
   bne.un Done

TestCase500:
   ldc.i4 600
   stloc.0
   ldc.i4 0x6cb
   conv.i4
   not
   ldc.i4 0xfffff934
   bne.un Done

TestCase499:
   ldc.i4 599
   stloc.0
   ldc.i4 0xfffff313
   conv.i4
   not
   ldc.i4 0xcec
   bne.un Done

TestCase498:
   ldc.i4 598
   stloc.0
   ldc.i4 0xffffceff
   conv.i4
   not
   ldc.i4 0x3100
   bne.un Done

TestCase497:
   ldc.i4 597
   stloc.0
   ldc.i4 0x2fba
   conv.i4
   not
   ldc.i4 0xffffd045
   bne.un Done

TestCase496:
   ldc.i4 596
   stloc.0
   ldc.i4 0xffffeaf5
   conv.i4
   not
   ldc.i4 0x150a
   bne.un Done

TestCase495:
   ldc.i4 595
   stloc.0
   ldc.i4 0xffffed6e
   conv.i4
   not
   ldc.i4 0x1291
   bne.un Done

TestCase494:
   ldc.i4 594
   stloc.0
   ldc.i4 0xffffb560
   conv.i4
   not
   ldc.i4 0x4a9f
   beq TestCase493
   br Done

TestCase493:
   ldc.i4 593
   stloc.0
   ldc.i4 0xffffcaaf
   conv.i4
   not
   ldc.i4 0x3550
   bne.un Done

TestCase492:
   ldc.i4 592
   stloc.0
   ldc.i4 0x4417
   conv.i4
   not
   ldc.i4 0xffffbbe8
   beq TestCase491
   br Done

TestCase491:
   ldc.i4 591
   stloc.0
   ldc.i4 0x3925
   conv.i4
   not
   ldc.i4 0xffffc6da
   bne.un Done

TestCase490:
   ldc.i4 590
   stloc.0
   ldc.i4 0x2185
   conv.i4
   not
   ldc.i4 0xffffde7a
   bne.un Done

TestCase489:
   ldc.i4 589
   stloc.0
   ldc.i4 0xffff917d
   conv.i4
   not
   ldc.i4 0x6e82
   bne.un Done

TestCase488:
   ldc.i4 588
   stloc.0
   ldc.i4 0xfffffac9
   conv.i4
   not
   ldc.i4 0x536
   beq TestCase487
   br Done

TestCase487:
   ldc.i4 587
   stloc.0
   ldc.i4 0xffffa4eb
   conv.i4
   not
   ldc.i4 0x5b14
   bne.un Done

TestCase486:
   ldc.i4 586
   stloc.0
   ldc.i4 0xffffb4ce
   conv.i4
   not
   ldc.i4 0x4b31
   beq TestCase485
   br Done

TestCase485:
   ldc.i4 585
   stloc.0
   ldc.i4 0x957
   conv.i4
   not
   ldc.i4 0xfffff6a8
   bne.un Done

TestCase484:
   ldc.i4 584
   stloc.0
   ldc.i4 0x28bf
   conv.i4
   not
   ldc.i4 0xffffd740
   bne.un Done

TestCase483:
   ldc.i4 583
   stloc.0
   ldc.i4 0x3ba3
   conv.i4
   not
   ldc.i4 0xffffc45c
   bne.un Done

TestCase482:
   ldc.i4 582
   stloc.0
   ldc.i4 0x1a2f
   conv.i4
   not
   ldc.i4 0xffffe5d0
   beq TestCase481
   br Done

TestCase481:
   ldc.i4 581
   stloc.0
   ldc.i4 0xffffa52a
   conv.i4
   not
   ldc.i4 0x5ad5
   bne.un Done

TestCase480:
   ldc.i4 580
   stloc.0
   ldc.i4 0x7459
   conv.i4
   not
   ldc.i4 0xffff8ba6
   beq TestCase479
   br Done

TestCase479:
   ldc.i4 579
   stloc.0
   ldc.i4 0xffffc326
   conv.i4
   not
   ldc.i4 0x3cd9
   bne.un Done

TestCase478:
   ldc.i4 578
   stloc.0
   ldc.i4 0x3c7d
   conv.i4
   not
   ldc.i4 0xffffc382
   bne.un Done

TestCase477:
   ldc.i4 577
   stloc.0
   ldc.i4 0x39af
   conv.i4
   not
   ldc.i4 0xffffc650
   bne.un Done

TestCase476:
   ldc.i4 576
   stloc.0
   ldc.i4 0x6494
   conv.i4
   not
   ldc.i4 0xffff9b6b
   bne.un Done

TestCase475:
   ldc.i4 575
   stloc.0
   ldc.i4 0x5d0
   conv.i4
   not
   ldc.i4 0xfffffa2f
   bne.un Done

TestCase474:
   ldc.i4 574
   stloc.0
   ldc.i4 0x33a
   conv.i4
   not
   ldc.i4 0xfffffcc5
   beq TestCase473
   br Done

TestCase473:
   ldc.i4 573
   stloc.0
   ldc.i4 0x7f0b
   conv.i4
   not
   ldc.i4 0xffff80f4
   beq TestCase472
   br Done

TestCase472:
   ldc.i4 572
   stloc.0
   ldc.i4 0xffff9a53
   conv.i4
   not
   ldc.i4 0x65ac
   beq TestCase471
   br Done

TestCase471:
   ldc.i4 571
   stloc.0
   ldc.i4 0xffffeae2
   conv.i4
   not
   ldc.i4 0x151d
   beq TestCase470
   br Done

TestCase470:
   ldc.i4 570
   stloc.0
   ldc.i4 0x3f96
   conv.i4
   not
   ldc.i4 0xffffc069
   beq TestCase469
   br Done

TestCase469:
   ldc.i4 569
   stloc.0
   ldc.i4 0xffffb64e
   conv.i4
   not
   ldc.i4 0x49b1
   beq TestCase468
   br Done

TestCase468:
   ldc.i4 568
   stloc.0
   ldc.i4 0xf9c
   conv.i4
   not
   ldc.i4 0xfffff063
   bne.un Done

TestCase467:
   ldc.i4 567
   stloc.0
   ldc.i4 0xffff8464
   conv.i4
   not
   ldc.i4 0x7b9b
   beq TestCase466
   br Done

TestCase466:
   ldc.i4 566
   stloc.0
   ldc.i4 0x60f5
   conv.i4
   not
   ldc.i4 0xffff9f0a
   bne.un Done

TestCase465:
   ldc.i4 565
   stloc.0
   ldc.i4 0xffffa0a1
   conv.i4
   not
   ldc.i4 0x5f5e
   bne.un Done

TestCase464:
   ldc.i4 564
   stloc.0
   ldc.i4 0xffffce9e
   conv.i4
   not
   ldc.i4 0x3161
   beq TestCase463
   br Done

TestCase463:
   ldc.i4 563
   stloc.0
   ldc.i4 0x3b61
   conv.i4
   not
   ldc.i4 0xffffc49e
   bne.un Done

TestCase462:
   ldc.i4 562
   stloc.0
   ldc.i4 0x4065
   conv.i4
   not
   ldc.i4 0xffffbf9a
   bne.un Done

TestCase461:
   ldc.i4 561
   stloc.0
   ldc.i4 0x485f
   conv.i4
   not
   ldc.i4 0xffffb7a0
   beq TestCase460
   br Done

TestCase460:
   ldc.i4 560
   stloc.0
   ldc.i4 0xffffc86f
   conv.i4
   not
   ldc.i4 0x3790
   beq TestCase459
   br Done

TestCase459:
   ldc.i4 559
   stloc.0
   ldc.i4 0x6f9
   conv.i4
   not
   ldc.i4 0xfffff906
   beq TestCase458
   br Done

TestCase458:
   ldc.i4 558
   stloc.0
   ldc.i4 0x4541
   conv.i4
   not
   ldc.i4 0xffffbabe
   bne.un Done

TestCase457:
   ldc.i4 557
   stloc.0
   ldc.i4 0x170
   conv.i4
   not
   ldc.i4 0xfffffe8f
   beq TestCase456
   br Done

TestCase456:
   ldc.i4 556
   stloc.0
   ldc.i4 0xffffc5e4
   conv.i4
   not
   ldc.i4 0x3a1b
   bne.un Done

TestCase455:
   ldc.i4 555
   stloc.0
   ldc.i4 0x5ce8
   conv.i4
   not
   ldc.i4 0xffffa317
   beq TestCase454
   br Done

TestCase454:
   ldc.i4 554
   stloc.0
   ldc.i4 0x4f26
   conv.i4
   not
   ldc.i4 0xffffb0d9
   beq TestCase453
   br Done

TestCase453:
   ldc.i4 553
   stloc.0
   ldc.i4 0xffffa029
   conv.i4
   not
   ldc.i4 0x5fd6
   bne.un Done

TestCase452:
   ldc.i4 552
   stloc.0
   ldc.i4 0xffffc2a1
   conv.i4
   not
   ldc.i4 0x3d5e
   bne.un Done

TestCase451:
   ldc.i4 551
   stloc.0
   ldc.i4 0x3b5f
   conv.i4
   not
   ldc.i4 0xffffc4a0
   beq TestCase450
   br Done

TestCase450:
   ldc.i4 550
   stloc.0
   ldc.i4 0x1116
   conv.i4
   not
   ldc.i4 0xffffeee9
   bne.un Done

TestCase449:
   ldc.i4 549
   stloc.0
   ldc.i4 0xfffffc3d
   conv.i4
   not
   ldc.i4 0x3c2
   bne.un Done

TestCase448:
   ldc.i4 548
   stloc.0
   ldc.i4 0x7271
   conv.i4
   not
   ldc.i4 0xffff8d8e
   bne.un Done

TestCase447:
   ldc.i4 547
   stloc.0
   ldc.i4 0x683d
   conv.i4
   not
   ldc.i4 0xffff97c2
   beq TestCase446
   br Done

TestCase446:
   ldc.i4 546
   stloc.0
   ldc.i4 0xffffb616
   conv.i4
   not
   ldc.i4 0x49e9
   beq TestCase445
   br Done

TestCase445:
   ldc.i4 545
   stloc.0
   ldc.i4 0x74e1
   conv.i4
   not
   ldc.i4 0xffff8b1e
   bne.un Done

TestCase444:
   ldc.i4 544
   stloc.0
   ldc.i4 0xfffffd58
   conv.i4
   not
   ldc.i4 0x2a7
   bne.un Done

TestCase443:
   ldc.i4 543
   stloc.0
   ldc.i4 0x949
   conv.i4
   not
   ldc.i4 0xfffff6b6
   bne.un Done

TestCase442:
   ldc.i4 542
   stloc.0
   ldc.i4 0xfffffa04
   conv.i4
   not
   ldc.i4 0x5fb
   bne.un Done

TestCase441:
   ldc.i4 541
   stloc.0
   ldc.i4 0x453f
   conv.i4
   not
   ldc.i4 0xffffbac0
   beq TestCase440
   br Done

TestCase440:
   ldc.i4 540
   stloc.0
   ldc.i4 0x326d
   conv.i4
   not
   ldc.i4 0xffffcd92
   bne.un Done

TestCase439:
   ldc.i4 539
   stloc.0
   ldc.i4 0x8ad
   conv.i4
   not
   ldc.i4 0xfffff752
   beq TestCase438
   br Done

TestCase438:
   ldc.i4 538
   stloc.0
   ldc.i4 0x5615
   conv.i4
   not
   ldc.i4 0xffffa9ea
   bne.un Done

TestCase437:
   ldc.i4 537
   stloc.0
   ldc.i4 0x5666
   conv.i4
   not
   ldc.i4 0xffffa999
   bne.un Done

TestCase436:
   ldc.i4 536
   stloc.0
   ldc.i4 0x6742
   conv.i4
   not
   ldc.i4 0xffff98bd
   bne.un Done

TestCase435:
   ldc.i4 535
   stloc.0
   ldc.i4 0x1b4
   conv.i4
   not
   ldc.i4 0xfffffe4b
   bne.un Done

TestCase434:
   ldc.i4 534
   stloc.0
   ldc.i4 0x2794
   conv.i4
   not
   ldc.i4 0xffffd86b
   beq TestCase433
   br Done

TestCase433:
   ldc.i4 533
   stloc.0
   ldc.i4 0x1cb1
   conv.i4
   not
   ldc.i4 0xffffe34e
   beq TestCase432
   br Done

TestCase432:
   ldc.i4 532
   stloc.0
   ldc.i4 0xffffa2f0
   conv.i4
   not
   ldc.i4 0x5d0f
   beq TestCase431
   br Done

TestCase431:
   ldc.i4 531
   stloc.0
   ldc.i4 0xffffce39
   conv.i4
   not
   ldc.i4 0x31c6
   bne.un Done

TestCase430:
   ldc.i4 530
   stloc.0
   ldc.i4 0x29e0
   conv.i4
   not
   ldc.i4 0xffffd61f
   bne.un Done

TestCase429:
   ldc.i4 529
   stloc.0
   ldc.i4 0x7cf3
   conv.i4
   not
   ldc.i4 0xffff830c
   beq TestCase428
   br Done

TestCase428:
   ldc.i4 528
   stloc.0
   ldc.i4 0x16b5
   conv.i4
   not
   ldc.i4 0xffffe94a
   bne.un Done

TestCase427:
   ldc.i4 527
   stloc.0
   ldc.i4 0x29a5
   conv.i4
   not
   ldc.i4 0xffffd65a
   beq TestCase426
   br Done

TestCase426:
   ldc.i4 526
   stloc.0
   ldc.i4 0x6d0f
   conv.i4
   not
   ldc.i4 0xffff92f0
   beq TestCase425
   br Done

TestCase425:
   ldc.i4 525
   stloc.0
   ldc.i4 0x2049
   conv.i4
   not
   ldc.i4 0xffffdfb6
   bne.un Done

TestCase424:
   ldc.i4 524
   stloc.0
   ldc.i4 0xffffaa33
   conv.i4
   not
   ldc.i4 0x55cc
   bne.un Done

TestCase423:
   ldc.i4 523
   stloc.0
   ldc.i4 0x3ae1
   conv.i4
   not
   ldc.i4 0xffffc51e
   bne.un Done

TestCase422:
   ldc.i4 522
   stloc.0
   ldc.i4 0xffffda2a
   conv.i4
   not
   ldc.i4 0x25d5
   bne.un Done

TestCase421:
   ldc.i4 521
   stloc.0
   ldc.i4 0x5a3c
   conv.i4
   not
   ldc.i4 0xffffa5c3
   beq TestCase420
   br Done

TestCase420:
   ldc.i4 520
   stloc.0
   ldc.i4 0xffffda40
   conv.i4
   not
   ldc.i4 0x25bf
   bne.un Done

TestCase419:
   ldc.i4 519
   stloc.0
   ldc.i4 0x5da1
   conv.i4
   not
   ldc.i4 0xffffa25e
   beq TestCase418
   br Done

TestCase418:
   ldc.i4 518
   stloc.0
   ldc.i4 0xffffbd4d
   conv.i4
   not
   ldc.i4 0x42b2
   beq TestCase417
   br Done

TestCase417:
   ldc.i4 517
   stloc.0
   ldc.i4 0xffff82e6
   conv.i4
   not
   ldc.i4 0x7d19
   bne.un Done

TestCase416:
   ldc.i4 516
   stloc.0
   ldc.i4 0xffffb9f5
   conv.i4
   not
   ldc.i4 0x460a
   beq TestCase415
   br Done

TestCase415:
   ldc.i4 515
   stloc.0
   ldc.i4 0xa95
   conv.i4
   not
   ldc.i4 0xfffff56a
   beq TestCase414
   br Done

TestCase414:
   ldc.i4 514
   stloc.0
   ldc.i4 0xffffc1a7
   conv.i4
   not
   ldc.i4 0x3e58
   bne.un Done

TestCase413:
   ldc.i4 513
   stloc.0
   ldc.i4 0xffffc76b
   conv.i4
   not
   ldc.i4 0x3894
   beq TestCase412
   br Done

TestCase412:
   ldc.i4 512
   stloc.0
   ldc.i4 0xffff802e
   conv.i4
   not
   ldc.i4 0x7fd1
   beq TestCase411
   br Done

TestCase411:
   ldc.i4 511
   stloc.0
   ldc.i4 0xffffbacb
   conv.i4
   not
   ldc.i4 0x4534
   beq TestCase410
   br Done

TestCase410:
   ldc.i4 510
   stloc.0
   ldc.i4 0x5b5c
   conv.i4
   not
   ldc.i4 0xffffa4a3
   bne.un Done

TestCase409:
   ldc.i4 509
   stloc.0
   ldc.i4 0xffffa164
   conv.i4
   not
   ldc.i4 0x5e9b
   bne.un Done

TestCase408:
   ldc.i4 508
   stloc.0
   ldc.i4 0xffffcd5a
   conv.i4
   not
   ldc.i4 0x32a5
   bne.un Done

TestCase407:
   ldc.i4 507
   stloc.0
   ldc.i4 0xffffadc2
   conv.i4
   not
   ldc.i4 0x523d
   bne.un Done

TestCase406:
   ldc.i4 506
   stloc.0
   ldc.i4 0xffffcae3
   conv.i4
   not
   ldc.i4 0x351c
   beq TestCase405
   br Done

TestCase405:
   ldc.i4 505
   stloc.0
   ldc.i4 0x2a79
   conv.i4
   not
   ldc.i4 0xffffd586
   bne.un Done

TestCase404:
   ldc.i4 504
   stloc.0
   ldc.i4 0xffffdb6d
   conv.i4
   not
   ldc.i4 0x2492
   beq TestCase403
   br Done

TestCase403:
   ldc.i4 503
   stloc.0
   ldc.i4 0xffffa992
   conv.i4
   not
   ldc.i4 0x566d
   beq TestCase402
   br Done

TestCase402:
   ldc.i4 502
   stloc.0
   ldc.i4 0xffffae0a
   conv.i4
   not
   ldc.i4 0x51f5
   bne.un Done

TestCase401:
   ldc.i4 501
   stloc.0
   ldc.i4 0xffff99d5
   conv.i4
   not
   ldc.i4 0x662a
   bne.un Done

TestCase400:
   ldc.i4 500
   stloc.0
   ldc.i4 0x616d
   conv.i4
   not
   ldc.i4 0xffff9e92
   bne.un Done

TestCase399:
   ldc.i4 499
   stloc.0
   ldc.i4 0x597d
   conv.i4
   not
   ldc.i4 0xffffa682
   beq TestCase398
   br Done

TestCase398:
   ldc.i4 498
   stloc.0
   ldc.i4 0xffffdd57
   conv.i4
   not
   ldc.i4 0x22a8
   bne.un Done

TestCase397:
   ldc.i4 497
   stloc.0
   ldc.i4 0xa05
   conv.i4
   not
   ldc.i4 0xfffff5fa
   bne.un Done

TestCase396:
   ldc.i4 496
   stloc.0
   ldc.i4 0xffffcaa5
   conv.i4
   not
   ldc.i4 0x355a
   beq TestCase395
   br Done

TestCase395:
   ldc.i4 495
   stloc.0
   ldc.i4 0xffff96a1
   conv.i4
   not
   ldc.i4 0x695e
   bne.un Done

TestCase394:
   ldc.i4 494
   stloc.0
   ldc.i4 0xffffe4fa
   conv.i4
   not
   ldc.i4 0x1b05
   beq TestCase393
   br Done

TestCase393:
   ldc.i4 493
   stloc.0
   ldc.i4 0x3287
   conv.i4
   not
   ldc.i4 0xffffcd78
   bne.un Done

TestCase392:
   ldc.i4 492
   stloc.0
   ldc.i4 0xea4
   conv.i4
   not
   ldc.i4 0xfffff15b
   beq TestCase391
   br Done

TestCase391:
   ldc.i4 491
   stloc.0
   ldc.i4 0xffffa647
   conv.i4
   not
   ldc.i4 0x59b8
   beq TestCase390
   br Done

TestCase390:
   ldc.i4 490
   stloc.0
   ldc.i4 0xffffb025
   conv.i4
   not
   ldc.i4 0x4fda
   bne.un Done

TestCase389:
   ldc.i4 489
   stloc.0
   ldc.i4 0xfffff785
   conv.i4
   not
   ldc.i4 0x87a
   bne.un Done

TestCase388:
   ldc.i4 488
   stloc.0
   ldc.i4 0xffffad48
   conv.i4
   not
   ldc.i4 0x52b7
   beq TestCase387
   br Done

TestCase387:
   ldc.i4 487
   stloc.0
   ldc.i4 0xffffa460
   conv.i4
   not
   ldc.i4 0x5b9f
   bne.un Done

TestCase386:
   ldc.i4 486
   stloc.0
   ldc.i4 0xffffb9e0
   conv.i4
   not
   ldc.i4 0x461f
   bne.un Done

TestCase385:
   ldc.i4 485
   stloc.0
   ldc.i4 0xffff88ad
   conv.i4
   not
   ldc.i4 0x7752
   beq TestCase384
   br Done

TestCase384:
   ldc.i4 484
   stloc.0
   ldc.i4 0x7281
   conv.i4
   not
   ldc.i4 0xffff8d7e
   beq TestCase383
   br Done

TestCase383:
   ldc.i4 483
   stloc.0
   ldc.i4 0x6998
   conv.i4
   not
   ldc.i4 0xffff9667
   bne.un Done

TestCase382:
   ldc.i4 482
   stloc.0
   ldc.i4 0xffffa09d
   conv.i4
   not
   ldc.i4 0x5f62
   bne.un Done

TestCase381:
   ldc.i4 481
   stloc.0
   ldc.i4 0xffffb23e
   conv.i4
   not
   ldc.i4 0x4dc1
   bne.un Done

TestCase380:
   ldc.i4 480
   stloc.0
   ldc.i4 0x49ea
   conv.i4
   not
   ldc.i4 0xffffb615
   beq TestCase379
   br Done

TestCase379:
   ldc.i4 479
   stloc.0
   ldc.i4 0x43cb
   conv.i4
   not
   ldc.i4 0xffffbc34
   beq TestCase378
   br Done

TestCase378:
   ldc.i4 478
   stloc.0
   ldc.i4 0xf70
   conv.i4
   not
   ldc.i4 0xfffff08f
   bne.un Done

TestCase377:
   ldc.i4 477
   stloc.0
   ldc.i4 0xffffdb67
   conv.i4
   not
   ldc.i4 0x2498
   beq TestCase376
   br Done

TestCase376:
   ldc.i4 476
   stloc.0
   ldc.i4 0xffffe950
   conv.i4
   not
   ldc.i4 0x16af
   bne.un Done

TestCase375:
   ldc.i4 475
   stloc.0
   ldc.i4 0xffff8777
   conv.i4
   not
   ldc.i4 0x7888
   beq TestCase374
   br Done

TestCase374:
   ldc.i4 474
   stloc.0
   ldc.i4 0x4745
   conv.i4
   not
   ldc.i4 0xffffb8ba
   bne.un Done

TestCase373:
   ldc.i4 473
   stloc.0
   ldc.i4 0xffffd81d
   conv.i4
   not
   ldc.i4 0x27e2
   beq TestCase372
   br Done

TestCase372:
   ldc.i4 472
   stloc.0
   ldc.i4 0x1268
   conv.i4
   not
   ldc.i4 0xffffed97
   bne.un Done

TestCase371:
   ldc.i4 471
   stloc.0
   ldc.i4 0xffffcb00
   conv.i4
   not
   ldc.i4 0x34ff
   beq TestCase370
   br Done

TestCase370:
   ldc.i4 470
   stloc.0
   ldc.i4 0xffffbecc
   conv.i4
   not
   ldc.i4 0x4133
   beq TestCase369
   br Done

TestCase369:
   ldc.i4 469
   stloc.0
   ldc.i4 0xffffe1bd
   conv.i4
   not
   ldc.i4 0x1e42
   bne.un Done

TestCase368:
   ldc.i4 468
   stloc.0
   ldc.i4 0x7bd1
   conv.i4
   not
   ldc.i4 0xffff842e
   beq TestCase367
   br Done

TestCase367:
   ldc.i4 467
   stloc.0
   ldc.i4 0x5a3c
   conv.i4
   not
   ldc.i4 0xffffa5c3
   bne.un Done

TestCase366:
   ldc.i4 466
   stloc.0
   ldc.i4 0xffffe5a7
   conv.i4
   not
   ldc.i4 0x1a58
   beq TestCase365
   br Done

TestCase365:
   ldc.i4 465
   stloc.0
   ldc.i4 0xffffb574
   conv.i4
   not
   ldc.i4 0x4a8b
   bne.un Done

TestCase364:
   ldc.i4 464
   stloc.0
   ldc.i4 0x1b5b
   conv.i4
   not
   ldc.i4 0xffffe4a4
   bne.un Done

TestCase363:
   ldc.i4 463
   stloc.0
   ldc.i4 0x1261
   conv.i4
   not
   ldc.i4 0xffffed9e
   bne.un Done

TestCase362:
   ldc.i4 462
   stloc.0
   ldc.i4 0x653e
   conv.i4
   not
   ldc.i4 0xffff9ac1
   bne.un Done

TestCase361:
   ldc.i4 461
   stloc.0
   ldc.i4 0x3a1
   conv.i4
   not
   ldc.i4 0xfffffc5e
   beq TestCase360
   br Done

TestCase360:
   ldc.i4 460
   stloc.0
   ldc.i4 0xfffff109
   conv.i4
   not
   ldc.i4 0xef6
   beq TestCase359
   br Done

TestCase359:
   ldc.i4 459
   stloc.0
   ldc.i4 0x6226
   conv.i4
   not
   ldc.i4 0xffff9dd9
   beq TestCase358
   br Done

TestCase358:
   ldc.i4 458
   stloc.0
   ldc.i4 0x21cc
   conv.i4
   not
   ldc.i4 0xffffde33
   bne.un Done

TestCase357:
   ldc.i4 457
   stloc.0
   ldc.i4 0xffffe171
   conv.i4
   not
   ldc.i4 0x1e8e
   bne.un Done

TestCase356:
   ldc.i4 456
   stloc.0
   ldc.i4 0xffff96e0
   conv.i4
   not
   ldc.i4 0x691f
   bne.un Done

TestCase355:
   ldc.i4 455
   stloc.0
   ldc.i4 0x7294
   conv.i4
   not
   ldc.i4 0xffff8d6b
   bne.un Done

TestCase354:
   ldc.i4 454
   stloc.0
   ldc.i4 0x4675
   conv.i4
   not
   ldc.i4 0xffffb98a
   beq TestCase353
   br Done

TestCase353:
   ldc.i4 453
   stloc.0
   ldc.i4 0x6e8
   conv.i4
   not
   ldc.i4 0xfffff917
   bne.un Done

TestCase352:
   ldc.i4 452
   stloc.0
   ldc.i4 0xffff90d7
   conv.i4
   not
   ldc.i4 0x6f28
   bne.un Done

TestCase351:
   ldc.i4 451
   stloc.0
   ldc.i4 0xfffff100
   conv.i4
   not
   ldc.i4 0xeff
   bne.un Done

TestCase350:
   ldc.i4 450
   stloc.0
   ldc.i4 0x469f
   conv.i4
   not
   ldc.i4 0xffffb960
   bne.un Done

TestCase349:
   ldc.i4 449
   stloc.0
   ldc.i4 0x2161
   conv.i4
   not
   ldc.i4 0xffffde9e
   bne.un Done

TestCase348:
   ldc.i4 448
   stloc.0
   ldc.i4 0x2c8
   conv.i4
   not
   ldc.i4 0xfffffd37
   bne.un Done

TestCase347:
   ldc.i4 447
   stloc.0
   ldc.i4 0xffffecbb
   conv.i4
   not
   ldc.i4 0x1344
   bne.un Done

TestCase346:
   ldc.i4 446
   stloc.0
   ldc.i4 0x4252
   conv.i4
   not
   ldc.i4 0xffffbdad
   beq TestCase345
   br Done

TestCase345:
   ldc.i4 445
   stloc.0
   ldc.i4 0xffffab52
   conv.i4
   not
   ldc.i4 0x54ad
   bne.un Done

TestCase344:
   ldc.i4 444
   stloc.0
   ldc.i4 0xfffffd1d
   conv.i4
   not
   ldc.i4 0x2e2
   bne.un Done

TestCase343:
   ldc.i4 443
   stloc.0
   ldc.i4 0x4646
   conv.i4
   not
   ldc.i4 0xffffb9b9
   beq TestCase342
   br Done

TestCase342:
   ldc.i4 442
   stloc.0
   ldc.i4 0x4b25
   conv.i4
   not
   ldc.i4 0xffffb4da
   bne.un Done

TestCase341:
   ldc.i4 441
   stloc.0
   ldc.i4 0x549d
   conv.i4
   not
   ldc.i4 0xffffab62
   bne.un Done

TestCase340:
   ldc.i4 440
   stloc.0
   ldc.i4 0x2f99
   conv.i4
   not
   ldc.i4 0xffffd066
   bne.un Done

TestCase339:
   ldc.i4 439
   stloc.0
   ldc.i4 0x5ca3
   conv.i4
   not
   ldc.i4 0xffffa35c
   beq TestCase338
   br Done

TestCase338:
   ldc.i4 438
   stloc.0
   ldc.i4 0xffff959b
   conv.i4
   not
   ldc.i4 0x6a64
   bne.un Done

TestCase337:
   ldc.i4 437
   stloc.0
   ldc.i4 0xfffff48f
   conv.i4
   not
   ldc.i4 0xb70
   bne.un Done

TestCase336:
   ldc.i4 436
   stloc.0
   ldc.i4 0xffffa26c
   conv.i4
   not
   ldc.i4 0x5d93
   bne.un Done

TestCase335:
   ldc.i4 435
   stloc.0
   ldc.i4 0x1fba
   conv.i4
   not
   ldc.i4 0xffffe045
   beq TestCase334
   br Done

TestCase334:
   ldc.i4 434
   stloc.0
   ldc.i4 0x5487
   conv.i4
   not
   ldc.i4 0xffffab78
   bne.un Done

TestCase333:
   ldc.i4 433
   stloc.0
   ldc.i4 0xffffc27f
   conv.i4
   not
   ldc.i4 0x3d80
   beq TestCase332
   br Done

TestCase332:
   ldc.i4 432
   stloc.0
   ldc.i4 0xfffffb73
   conv.i4
   not
   ldc.i4 0x48c
   beq TestCase331
   br Done

TestCase331:
   ldc.i4 431
   stloc.0
   ldc.i4 0x74e1
   conv.i4
   not
   ldc.i4 0xffff8b1e
   bne.un Done

TestCase330:
   ldc.i4 430
   stloc.0
   ldc.i4 0x75d0
   conv.i4
   not
   ldc.i4 0xffff8a2f
   beq TestCase329
   br Done

TestCase329:
   ldc.i4 429
   stloc.0
   ldc.i4 0x4310
   conv.i4
   not
   ldc.i4 0xffffbcef
   beq TestCase328
   br Done

TestCase328:
   ldc.i4 428
   stloc.0
   ldc.i4 0xffff8ae1
   conv.i4
   not
   ldc.i4 0x751e
   beq TestCase327
   br Done

TestCase327:
   ldc.i4 427
   stloc.0
   ldc.i4 0x40e1
   conv.i4
   not
   ldc.i4 0xffffbf1e
   bne.un Done

TestCase326:
   ldc.i4 426
   stloc.0
   ldc.i4 0x4441
   conv.i4
   not
   ldc.i4 0xffffbbbe
   bne.un Done

TestCase325:
   ldc.i4 425
   stloc.0
   ldc.i4 0xffff8b92
   conv.i4
   not
   ldc.i4 0x746d
   bne.un Done

TestCase324:
   ldc.i4 424
   stloc.0
   ldc.i4 0xffffbe30
   conv.i4
   not
   ldc.i4 0x41cf
   bne.un Done

TestCase323:
   ldc.i4 423
   stloc.0
   ldc.i4 0x63b5
   conv.i4
   not
   ldc.i4 0xffff9c4a
   beq TestCase322
   br Done

TestCase322:
   ldc.i4 422
   stloc.0
   ldc.i4 0x2c8
   conv.i4
   not
   ldc.i4 0xfffffd37
   bne.un Done

TestCase321:
   ldc.i4 421
   stloc.0
   ldc.i4 0x244f
   conv.i4
   not
   ldc.i4 0xffffdbb0
   beq TestCase320
   br Done

TestCase320:
   ldc.i4 420
   stloc.0
   ldc.i4 0xffffd8a4
   conv.i4
   not
   ldc.i4 0x275b
   bne.un Done

TestCase319:
   ldc.i4 419
   stloc.0
   ldc.i4 0xffffb77d
   conv.i4
   not
   ldc.i4 0x4882
   bne.un Done

TestCase318:
   ldc.i4 418
   stloc.0
   ldc.i4 0x61a5
   conv.i4
   not
   ldc.i4 0xffff9e5a
   bne.un Done

TestCase317:
   ldc.i4 417
   stloc.0
   ldc.i4 0x446a
   conv.i4
   not
   ldc.i4 0xffffbb95
   bne.un Done

TestCase316:
   ldc.i4 416
   stloc.0
   ldc.i4 0x44
   conv.i4
   not
   ldc.i4 0xffffffbb
   beq TestCase315
   br Done

TestCase315:
   ldc.i4 415
   stloc.0
   ldc.i4 0xffffba26
   conv.i4
   not
   ldc.i4 0x45d9
   bne.un Done

TestCase314:
   ldc.i4 414
   stloc.0
   ldc.i4 0xffffd7e7
   conv.i4
   not
   ldc.i4 0x2818
   beq TestCase313
   br Done

TestCase313:
   ldc.i4 413
   stloc.0
   ldc.i4 0xffffefab
   conv.i4
   not
   ldc.i4 0x1054
   bne.un Done

TestCase312:
   ldc.i4 412
   stloc.0
   ldc.i4 0xfffff06a
   conv.i4
   not
   ldc.i4 0xf95
   beq TestCase311
   br Done

TestCase311:
   ldc.i4 411
   stloc.0
   ldc.i4 0x4a2e
   conv.i4
   not
   ldc.i4 0xffffb5d1
   beq TestCase310
   br Done

TestCase310:
   ldc.i4 410
   stloc.0
   ldc.i4 0x5355
   conv.i4
   not
   ldc.i4 0xffffacaa
   beq TestCase309
   br Done

TestCase309:
   ldc.i4 409
   stloc.0
   ldc.i4 0x5a9f
   conv.i4
   not
   ldc.i4 0xffffa560
   bne.un Done

TestCase308:
   ldc.i4 408
   stloc.0
   ldc.i4 0x2efe
   conv.i4
   not
   ldc.i4 0xffffd101
   beq TestCase307
   br Done

TestCase307:
   ldc.i4 407
   stloc.0
   ldc.i4 0xffffef70
   conv.i4
   not
   ldc.i4 0x108f
   bne.un Done

TestCase306:
   ldc.i4 406
   stloc.0
   ldc.i4 0xffffa95f
   conv.i4
   not
   ldc.i4 0x56a0
   beq TestCase305
   br Done

TestCase305:
   ldc.i4 405
   stloc.0
   ldc.i4 0x582e
   conv.i4
   not
   ldc.i4 0xffffa7d1
   bne.un Done

TestCase304:
   ldc.i4 404
   stloc.0
   ldc.i4 0xffffd3f3
   conv.i4
   not
   ldc.i4 0x2c0c
   beq TestCase303
   br Done

TestCase303:
   ldc.i4 403
   stloc.0
   ldc.i4 0x59b1
   conv.i4
   not
   ldc.i4 0xffffa64e
   bne.un Done

TestCase302:
   ldc.i4 402
   stloc.0
   ldc.i4 0xffff9735
   conv.i4
   not
   ldc.i4 0x68ca
   beq TestCase301
   br Done

TestCase301:
   ldc.i4 401
   stloc.0
   ldc.i4 0x5db
   conv.i4
   not
   ldc.i4 0xfffffa24
   bne.un Done

TestCase300:
   ldc.i4 400
   stloc.0
   ldc.i4 0xffff9f38
   conv.i4
   not
   ldc.i4 0x60c7
   bne.un Done

TestCase299:
   ldc.i4 399
   stloc.0
   ldc.i4 0x6519
   conv.i4
   not
   ldc.i4 0xffff9ae6
   beq TestCase298
   br Done

TestCase298:
   ldc.i4 398
   stloc.0
   ldc.i4 0xad2
   conv.i4
   not
   ldc.i4 0xfffff52d
   bne.un Done

TestCase297:
   ldc.i4 397
   stloc.0
   ldc.i4 0x44b
   conv.i4
   not
   ldc.i4 0xfffffbb4
   bne.un Done

TestCase296:
   ldc.i4 396
   stloc.0
   ldc.i4 0xffffac8c
   conv.i4
   not
   ldc.i4 0x5373
   bne.un Done

TestCase295:
   ldc.i4 395
   stloc.0
   ldc.i4 0xffffd95e
   conv.i4
   not
   ldc.i4 0x26a1
   bne.un Done

TestCase294:
   ldc.i4 394
   stloc.0
   ldc.i4 0x33d3
   conv.i4
   not
   ldc.i4 0xffffcc2c
   bne.un Done

TestCase293:
   ldc.i4 393
   stloc.0
   ldc.i4 0x610
   conv.i4
   not
   ldc.i4 0xfffff9ef
   bne.un Done

TestCase292:
   ldc.i4 392
   stloc.0
   ldc.i4 0x699f
   conv.i4
   not
   ldc.i4 0xffff9660
   bne.un Done

TestCase291:
   ldc.i4 391
   stloc.0
   ldc.i4 0xffff8bc5
   conv.i4
   not
   ldc.i4 0x743a
   beq TestCase290
   br Done

TestCase290:
   ldc.i4 390
   stloc.0
   ldc.i4 0xffffb1cd
   conv.i4
   not
   ldc.i4 0x4e32
   beq TestCase289
   br Done

TestCase289:
   ldc.i4 389
   stloc.0
   ldc.i4 0x2c00
   conv.i4
   not
   ldc.i4 0xffffd3ff
   bne.un Done

TestCase288:
   ldc.i4 388
   stloc.0
   ldc.i4 0x7a72
   conv.i4
   not
   ldc.i4 0xffff858d
   beq TestCase287
   br Done

TestCase287:
   ldc.i4 387
   stloc.0
   ldc.i4 0xffffd825
   conv.i4
   not
   ldc.i4 0x27da
   beq TestCase286
   br Done

TestCase286:
   ldc.i4 386
   stloc.0
   ldc.i4 0xffff8df3
   conv.i4
   not
   ldc.i4 0x720c
   bne.un Done

TestCase285:
   ldc.i4 385
   stloc.0
   ldc.i4 0x6bd4
   conv.i4
   not
   ldc.i4 0xffff942b
   bne.un Done

TestCase284:
   ldc.i4 384
   stloc.0
   ldc.i4 0xffffb3cb
   conv.i4
   not
   ldc.i4 0x4c34
   beq TestCase283
   br Done

TestCase283:
   ldc.i4 383
   stloc.0
   ldc.i4 0xffff8e3e
   conv.i4
   not
   ldc.i4 0x71c1
   beq TestCase282
   br Done

TestCase282:
   ldc.i4 382
   stloc.0
   ldc.i4 0x8de
   conv.i4
   not
   ldc.i4 0xfffff721
   beq TestCase281
   br Done

TestCase281:
   ldc.i4 381
   stloc.0
   ldc.i4 0x5d03
   conv.i4
   not
   ldc.i4 0xffffa2fc
   bne.un Done

TestCase280:
   ldc.i4 380
   stloc.0
   ldc.i4 0x4922
   conv.i4
   not
   ldc.i4 0xffffb6dd
   bne.un Done

TestCase279:
   ldc.i4 379
   stloc.0
   ldc.i4 0x56f7
   conv.i4
   not
   ldc.i4 0xffffa908
   bne.un Done

TestCase278:
   ldc.i4 378
   stloc.0
   ldc.i4 0x466e
   conv.i4
   not
   ldc.i4 0xffffb991
   bne.un Done

TestCase277:
   ldc.i4 377
   stloc.0
   ldc.i4 0x7e0
   conv.i4
   not
   ldc.i4 0xfffff81f
   beq TestCase276
   br Done

TestCase276:
   ldc.i4 376
   stloc.0
   ldc.i4 0x776f
   conv.i4
   not
   ldc.i4 0xffff8890
   bne.un Done

TestCase275:
   ldc.i4 375
   stloc.0
   ldc.i4 0xffffe802
   conv.i4
   not
   ldc.i4 0x17fd
   bne.un Done

TestCase274:
   ldc.i4 374
   stloc.0
   ldc.i4 0x2301
   conv.i4
   not
   ldc.i4 0xffffdcfe
   beq TestCase273
   br Done

TestCase273:
   ldc.i4 373
   stloc.0
   ldc.i4 0x12f2
   conv.i4
   not
   ldc.i4 0xffffed0d
   beq TestCase272
   br Done

TestCase272:
   ldc.i4 372
   stloc.0
   ldc.i4 0xffffff6b
   conv.i4
   not
   ldc.i4 0x94
   bne.un Done

TestCase271:
   ldc.i4 371
   stloc.0
   ldc.i4 0x7bd8
   conv.i4
   not
   ldc.i4 0xffff8427
   bne.un Done

TestCase270:
   ldc.i4 370
   stloc.0
   ldc.i4 0x42be
   conv.i4
   not
   ldc.i4 0xffffbd41
   bne.un Done

TestCase269:
   ldc.i4 369
   stloc.0
   ldc.i4 0xffff956d
   conv.i4
   not
   ldc.i4 0x6a92
   beq TestCase268
   br Done

TestCase268:
   ldc.i4 368
   stloc.0
   ldc.i4 0x53e5
   conv.i4
   not
   ldc.i4 0xffffac1a
   beq TestCase267
   br Done

TestCase267:
   ldc.i4 367
   stloc.0
   ldc.i4 0x3b3f
   conv.i4
   not
   ldc.i4 0xffffc4c0
   beq TestCase266
   br Done

TestCase266:
   ldc.i4 366
   stloc.0
   ldc.i4 0x765f
   conv.i4
   not
   ldc.i4 0xffff89a0
   beq TestCase265
   br Done

TestCase265:
   ldc.i4 365
   stloc.0
   ldc.i4 0xffffc134
   conv.i4
   not
   ldc.i4 0x3ecb
   beq TestCase264
   br Done

TestCase264:
   ldc.i4 364
   stloc.0
   ldc.i4 0x304d
   conv.i4
   not
   ldc.i4 0xffffcfb2
   beq TestCase263
   br Done

TestCase263:
   ldc.i4 363
   stloc.0
   ldc.i4 0xfffff7de
   conv.i4
   not
   ldc.i4 0x821
   beq TestCase262
   br Done

TestCase262:
   ldc.i4 362
   stloc.0
   ldc.i4 0x4119
   conv.i4
   not
   ldc.i4 0xffffbee6
   bne.un Done

TestCase261:
   ldc.i4 361
   stloc.0
   ldc.i4 0xffff8c92
   conv.i4
   not
   ldc.i4 0x736d
   beq TestCase260
   br Done

TestCase260:
   ldc.i4 360
   stloc.0
   ldc.i4 0xffffde99
   conv.i4
   not
   ldc.i4 0x2166
   beq TestCase259
   br Done

TestCase259:
   ldc.i4 359
   stloc.0
   ldc.i4 0x6d5e
   conv.i4
   not
   ldc.i4 0xffff92a1
   bne.un Done

TestCase258:
   ldc.i4 358
   stloc.0
   ldc.i4 0xfffff033
   conv.i4
   not
   ldc.i4 0xfcc
   bne.un Done

TestCase257:
   ldc.i4 357
   stloc.0
   ldc.i4 0x5e74
   conv.i4
   not
   ldc.i4 0xffffa18b
   beq TestCase256
   br Done

TestCase256:
   ldc.i4 356
   stloc.0
   ldc.i4 0x3bc2
   conv.i4
   not
   ldc.i4 0xffffc43d
   bne.un Done

TestCase255:
   ldc.i4 355
   stloc.0
   ldc.i4 0xfffffd41
   conv.i4
   not
   ldc.i4 0x2be
   bne.un Done

TestCase254:
   ldc.i4 354
   stloc.0
   ldc.i4 0xfffff290
   conv.i4
   not
   ldc.i4 0xd6f
   bne.un Done

TestCase253:
   ldc.i4 353
   stloc.0
   ldc.i4 0x7098
   conv.i4
   not
   ldc.i4 0xffff8f67
   bne.un Done

TestCase252:
   ldc.i4 352
   stloc.0
   ldc.i4 0xffffdb8c
   conv.i4
   not
   ldc.i4 0x2473
   beq TestCase251
   br Done

TestCase251:
   ldc.i4 351
   stloc.0
   ldc.i4 0x3136
   conv.i4
   not
   ldc.i4 0xffffcec9
   bne.un Done

TestCase250:
   ldc.i4 350
   stloc.0
   ldc.i4 0x40b8
   conv.i4
   not
   ldc.i4 0xffffbf47
   beq TestCase249
   br Done

TestCase249:
   ldc.i4 349
   stloc.0
   ldc.i4 0xffffdbcd
   conv.i4
   not
   ldc.i4 0x2432
   beq TestCase248
   br Done

TestCase248:
   ldc.i4 348
   stloc.0
   ldc.i4 0xffffd021
   conv.i4
   not
   ldc.i4 0x2fde
   beq TestCase247
   br Done

TestCase247:
   ldc.i4 347
   stloc.0
   ldc.i4 0xfffffca6
   conv.i4
   not
   ldc.i4 0x359
   bne.un Done

TestCase246:
   ldc.i4 346
   stloc.0
   ldc.i4 0xffffa0f9
   conv.i4
   not
   ldc.i4 0x5f06
   beq TestCase245
   br Done

TestCase245:
   ldc.i4 345
   stloc.0
   ldc.i4 0xffffb1a7
   conv.i4
   not
   ldc.i4 0x4e58
   beq TestCase244
   br Done

TestCase244:
   ldc.i4 344
   stloc.0
   ldc.i4 0x23c3
   conv.i4
   not
   ldc.i4 0xffffdc3c
   bne.un Done

TestCase243:
   ldc.i4 343
   stloc.0
   ldc.i4 0x3e24
   conv.i4
   not
   ldc.i4 0xffffc1db
   beq TestCase242
   br Done

TestCase242:
   ldc.i4 342
   stloc.0
   ldc.i4 0xffffff64
   conv.i4
   not
   ldc.i4 0x9b
   beq TestCase241
   br Done

TestCase241:
   ldc.i4 341
   stloc.0
   ldc.i4 0x2e34
   conv.i4
   not
   ldc.i4 0xffffd1cb
   bne.un Done

TestCase240:
   ldc.i4 340
   stloc.0
   ldc.i4 0x6884
   conv.i4
   not
   ldc.i4 0xffff977b
   beq TestCase239
   br Done

TestCase239:
   ldc.i4 339
   stloc.0
   ldc.i4 0xffffc21a
   conv.i4
   not
   ldc.i4 0x3de5
   bne.un Done

TestCase238:
   ldc.i4 338
   stloc.0
   ldc.i4 0xffffde09
   conv.i4
   not
   ldc.i4 0x21f6
   beq TestCase237
   br Done

TestCase237:
   ldc.i4 337
   stloc.0
   ldc.i4 0x56a0
   conv.i4
   not
   ldc.i4 0xffffa95f
   bne.un Done

TestCase236:
   ldc.i4 336
   stloc.0
   ldc.i4 0xffffca44
   conv.i4
   not
   ldc.i4 0x35bb
   bne.un Done

TestCase235:
   ldc.i4 335
   stloc.0
   ldc.i4 0x538d
   conv.i4
   not
   ldc.i4 0xffffac72
   beq TestCase234
   br Done

TestCase234:
   ldc.i4 334
   stloc.0
   ldc.i4 0x1e8c
   conv.i4
   not
   ldc.i4 0xffffe173
   bne.un Done

TestCase233:
   ldc.i4 333
   stloc.0
   ldc.i4 0x4e08
   conv.i4
   not
   ldc.i4 0xffffb1f7
   beq TestCase232
   br Done

TestCase232:
   ldc.i4 332
   stloc.0
   ldc.i4 0x604
   conv.i4
   not
   ldc.i4 0xfffff9fb
   beq TestCase231
   br Done

TestCase231:
   ldc.i4 331
   stloc.0
   ldc.i4 0x1e17
   conv.i4
   not
   ldc.i4 0xffffe1e8
   bne.un Done

TestCase230:
   ldc.i4 330
   stloc.0
   ldc.i4 0x10ac
   conv.i4
   not
   ldc.i4 0xffffef53
   beq TestCase229
   br Done

TestCase229:
   ldc.i4 329
   stloc.0
   ldc.i4 0xffffff80
   conv.i4
   not
   ldc.i4 0x7f
   beq TestCase228
   br Done

TestCase228:
   ldc.i4 328
   stloc.0
   ldc.i4 0x5dfe
   conv.i4
   not
   ldc.i4 0xffffa201
   bne.un Done

TestCase227:
   ldc.i4 327
   stloc.0
   ldc.i4 0xffffe3aa
   conv.i4
   not
   ldc.i4 0x1c55
   bne.un Done

TestCase226:
   ldc.i4 326
   stloc.0
   ldc.i4 0xffffbd51
   conv.i4
   not
   ldc.i4 0x42ae
   beq TestCase225
   br Done

TestCase225:
   ldc.i4 325
   stloc.0
   ldc.i4 0xffff9fde
   conv.i4
   not
   ldc.i4 0x6021
   bne.un Done

TestCase224:
   ldc.i4 324
   stloc.0
   ldc.i4 0xffffcd5a
   conv.i4
   not
   ldc.i4 0x32a5
   beq TestCase223
   br Done

TestCase223:
   ldc.i4 323
   stloc.0
   ldc.i4 0xffff8d1a
   conv.i4
   not
   ldc.i4 0x72e5
   beq TestCase222
   br Done

TestCase222:
   ldc.i4 322
   stloc.0
   ldc.i4 0xffffe29b
   conv.i4
   not
   ldc.i4 0x1d64
   beq TestCase221
   br Done

TestCase221:
   ldc.i4 321
   stloc.0
   ldc.i4 0xffffb053
   conv.i4
   not
   ldc.i4 0x4fac
   beq TestCase220
   br Done

TestCase220:
   ldc.i4 320
   stloc.0
   ldc.i4 0x7ad9
   conv.i4
   not
   ldc.i4 0xffff8526
   beq TestCase219
   br Done

TestCase219:
   ldc.i4 319
   stloc.0
   ldc.i4 0xffffdd94
   conv.i4
   not
   ldc.i4 0x226b
   bne.un Done

TestCase218:
   ldc.i4 318
   stloc.0
   ldc.i4 0x3fbf
   conv.i4
   not
   ldc.i4 0xffffc040
   beq TestCase217
   br Done

TestCase217:
   ldc.i4 317
   stloc.0
   ldc.i4 0xffffcca5
   conv.i4
   not
   ldc.i4 0x335a
   beq TestCase216
   br Done

TestCase216:
   ldc.i4 316
   stloc.0
   ldc.i4 0x3825
   conv.i4
   not
   ldc.i4 0xffffc7da
   beq TestCase215
   br Done

TestCase215:
   ldc.i4 315
   stloc.0
   ldc.i4 0x2a10
   conv.i4
   not
   ldc.i4 0xffffd5ef
   beq TestCase214
   br Done

TestCase214:
   ldc.i4 314
   stloc.0
   ldc.i4 0x2bb9
   conv.i4
   not
   ldc.i4 0xffffd446
   beq TestCase213
   br Done

TestCase213:
   ldc.i4 313
   stloc.0
   ldc.i4 0xffffbbff
   conv.i4
   not
   ldc.i4 0x4400
   bne.un Done

TestCase212:
   ldc.i4 312
   stloc.0
   ldc.i4 0xfffffd2d
   conv.i4
   not
   ldc.i4 0x2d2
   bne.un Done

TestCase211:
   ldc.i4 311
   stloc.0
   ldc.i4 0x523c
   conv.i4
   not
   ldc.i4 0xffffadc3
   bne.un Done

TestCase210:
   ldc.i4 310
   stloc.0
   ldc.i4 0x348a
   conv.i4
   not
   ldc.i4 0xffffcb75
   beq TestCase209
   br Done

TestCase209:
   ldc.i4 309
   stloc.0
   ldc.i4 0xffffff93
   conv.i4
   not
   ldc.i4 0x6c
   bne.un Done

TestCase208:
   ldc.i4 308
   stloc.0
   ldc.i4 0xffff9ace
   conv.i4
   not
   ldc.i4 0x6531
   beq TestCase207
   br Done

TestCase207:
   ldc.i4 307
   stloc.0
   ldc.i4 0xffffe46b
   conv.i4
   not
   ldc.i4 0x1b94
   bne.un Done

TestCase206:
   ldc.i4 306
   stloc.0
   ldc.i4 0x1a0e
   conv.i4
   not
   ldc.i4 0xffffe5f1
   bne.un Done

TestCase205:
   ldc.i4 305
   stloc.0
   ldc.i4 0xffffa705
   conv.i4
   not
   ldc.i4 0x58fa
   beq TestCase204
   br Done

TestCase204:
   ldc.i4 304
   stloc.0
   ldc.i4 0x1225
   conv.i4
   not
   ldc.i4 0xffffedda
   bne.un Done

TestCase203:
   ldc.i4 303
   stloc.0
   ldc.i4 0xfffff1c6
   conv.i4
   not
   ldc.i4 0xe39
   beq TestCase202
   br Done

TestCase202:
   ldc.i4 302
   stloc.0
   ldc.i4 0xffff828d
   conv.i4
   not
   ldc.i4 0x7d72
   beq TestCase201
   br Done

TestCase201:
   ldc.i4 301
   stloc.0
   ldc.i4 0xffff8e87
   conv.i4
   not
   ldc.i4 0x7178
   beq TestCase200
   br Done

TestCase200:
   ldc.i4 300
   stloc.0
   ldc.i4 0x42ef
   conv.i4
   not
   ldc.i4 0xffffbd10
   bne.un Done

TestCase199:
   ldc.i4 299
   stloc.0
   ldc.i4 0xffff9ac5
   conv.i4
   not
   ldc.i4 0x653a
   bne.un Done

TestCase198:
   ldc.i4 298
   stloc.0
   ldc.i4 0xffffe4de
   conv.i4
   not
   ldc.i4 0x1b21
   bne.un Done

TestCase197:
   ldc.i4 297
   stloc.0
   ldc.i4 0xffffeac3
   conv.i4
   not
   ldc.i4 0x153c
   beq TestCase196
   br Done

TestCase196:
   ldc.i4 296
   stloc.0
   ldc.i4 0xffffb7c3
   conv.i4
   not
   ldc.i4 0x483c
   beq TestCase195
   br Done

TestCase195:
   ldc.i4 295
   stloc.0
   ldc.i4 0xffffada4
   conv.i4
   not
   ldc.i4 0x525b
   beq TestCase194
   br Done

TestCase194:
   ldc.i4 294
   stloc.0
   ldc.i4 0xffff8a10
   conv.i4
   not
   ldc.i4 0x75ef
   beq TestCase193
   br Done

TestCase193:
   ldc.i4 293
   stloc.0
   ldc.i4 0xffffc67d
   conv.i4
   not
   ldc.i4 0x3982
   beq TestCase192
   br Done

TestCase192:
   ldc.i4 292
   stloc.0
   ldc.i4 0x1a74
   conv.i4
   not
   ldc.i4 0xffffe58b
   bne.un Done

TestCase191:
   ldc.i4 291
   stloc.0
   ldc.i4 0x560a
   conv.i4
   not
   ldc.i4 0xffffa9f5
   beq TestCase190
   br Done

TestCase190:
   ldc.i4 290
   stloc.0
   ldc.i4 0x5f85
   conv.i4
   not
   ldc.i4 0xffffa07a
   beq TestCase189
   br Done

TestCase189:
   ldc.i4 289
   stloc.0
   ldc.i4 0xffffd5f4
   conv.i4
   not
   ldc.i4 0x2a0b
   bne.un Done

TestCase188:
   ldc.i4 288
   stloc.0
   ldc.i4 0xffff8ac4
   conv.i4
   not
   ldc.i4 0x753b
   beq TestCase187
   br Done

TestCase187:
   ldc.i4 287
   stloc.0
   ldc.i4 0xffff9863
   conv.i4
   not
   ldc.i4 0x679c
   beq TestCase186
   br Done

TestCase186:
   ldc.i4 286
   stloc.0
   ldc.i4 0xfffff5f5
   conv.i4
   not
   ldc.i4 0xa0a
   bne.un Done

TestCase185:
   ldc.i4 285
   stloc.0
   ldc.i4 0x3bf3
   conv.i4
   not
   ldc.i4 0xffffc40c
   bne.un Done

TestCase184:
   ldc.i4 284
   stloc.0
   ldc.i4 0x4a4c
   conv.i4
   not
   ldc.i4 0xffffb5b3
   bne.un Done

TestCase183:
   ldc.i4 283
   stloc.0
   ldc.i4 0x69d0
   conv.i4
   not
   ldc.i4 0xffff962f
   beq TestCase182
   br Done

TestCase182:
   ldc.i4 282
   stloc.0
   ldc.i4 0xfffff0e5
   conv.i4
   not
   ldc.i4 0xf1a
   beq TestCase181
   br Done

TestCase181:
   ldc.i4 281
   stloc.0
   ldc.i4 0xffffdd2f
   conv.i4
   not
   ldc.i4 0x22d0
   beq TestCase180
   br Done

TestCase180:
   ldc.i4 280
   stloc.0
   ldc.i4 0x1777
   conv.i4
   not
   ldc.i4 0xffffe888
   beq TestCase179
   br Done

TestCase179:
   ldc.i4 279
   stloc.0
   ldc.i4 0xffffd883
   conv.i4
   not
   ldc.i4 0x277c
   bne.un Done

TestCase178:
   ldc.i4 278
   stloc.0
   ldc.i4 0xffffe81c
   conv.i4
   not
   ldc.i4 0x17e3
   beq TestCase177
   br Done

TestCase177:
   ldc.i4 277
   stloc.0
   ldc.i4 0x4414
   conv.i4
   not
   ldc.i4 0xffffbbeb
   bne.un Done

TestCase176:
   ldc.i4 276
   stloc.0
   ldc.i4 0xffff9409
   conv.i4
   not
   ldc.i4 0x6bf6
   bne.un Done

TestCase175:
   ldc.i4 275
   stloc.0
   ldc.i4 0x549c
   conv.i4
   not
   ldc.i4 0xffffab63
   beq TestCase174
   br Done

TestCase174:
   ldc.i4 274
   stloc.0
   ldc.i4 0x129c
   conv.i4
   not
   ldc.i4 0xffffed63
   bne.un Done

TestCase173:
   ldc.i4 273
   stloc.0
   ldc.i4 0xffffdb64
   conv.i4
   not
   ldc.i4 0x249b
   bne.un Done

TestCase172:
   ldc.i4 272
   stloc.0
   ldc.i4 0xffffd708
   conv.i4
   not
   ldc.i4 0x28f7
   bne.un Done

TestCase171:
   ldc.i4 271
   stloc.0
   ldc.i4 0x4f7d
   conv.i4
   not
   ldc.i4 0xffffb082
   beq TestCase170
   br Done

TestCase170:
   ldc.i4 270
   stloc.0
   ldc.i4 0x212b
   conv.i4
   not
   ldc.i4 0xffffded4
   bne.un Done

TestCase169:
   ldc.i4 269
   stloc.0
   ldc.i4 0x4c97
   conv.i4
   not
   ldc.i4 0xffffb368
   beq TestCase168
   br Done

TestCase168:
   ldc.i4 268
   stloc.0
   ldc.i4 0xffffb699
   conv.i4
   not
   ldc.i4 0x4966
   beq TestCase167
   br Done

TestCase167:
   ldc.i4 267
   stloc.0
   ldc.i4 0x1e45
   conv.i4
   not
   ldc.i4 0xffffe1ba
   beq TestCase166
   br Done

TestCase166:
   ldc.i4 266
   stloc.0
   ldc.i4 0x2d15
   conv.i4
   not
   ldc.i4 0xffffd2ea
   beq TestCase165
   br Done

TestCase165:
   ldc.i4 265
   stloc.0
   ldc.i4 0x3d9e
   conv.i4
   not
   ldc.i4 0xffffc261
   bne.un Done

TestCase164:
   ldc.i4 264
   stloc.0
   ldc.i4 0xffffc304
   conv.i4
   not
   ldc.i4 0x3cfb
   beq TestCase163
   br Done

TestCase163:
   ldc.i4 263
   stloc.0
   ldc.i4 0xffff9b5a
   conv.i4
   not
   ldc.i4 0x64a5
   beq TestCase162
   br Done

TestCase162:
   ldc.i4 262
   stloc.0
   ldc.i4 0xffffb358
   conv.i4
   not
   ldc.i4 0x4ca7
   beq TestCase161
   br Done

TestCase161:
   ldc.i4 261
   stloc.0
   ldc.i4 0xffffa8f1
   conv.i4
   not
   ldc.i4 0x570e
   beq TestCase160
   br Done

TestCase160:
   ldc.i4 260
   stloc.0
   ldc.i4 0xffffdd7b
   conv.i4
   not
   ldc.i4 0x2284
   bne.un Done

TestCase159:
   ldc.i4 259
   stloc.0
   ldc.i4 0x4de2
   conv.i4
   not
   ldc.i4 0xffffb21d
   bne.un Done

TestCase158:
   ldc.i4 258
   stloc.0
   ldc.i4 0xffff9a99
   conv.i4
   not
   ldc.i4 0x6566
   bne.un Done

TestCase157:
   ldc.i4 257
   stloc.0
   ldc.i4 0xffffbb54
   conv.i4
   not
   ldc.i4 0x44ab
   bne.un Done

TestCase156:
   ldc.i4 256
   stloc.0
   ldc.i4 0xffff928b
   conv.i4
   not
   ldc.i4 0x6d74
   beq TestCase155
   br Done

TestCase155:
   ldc.i4 255
   stloc.0
   ldc.i4 0x2701
   conv.i4
   not
   ldc.i4 0xffffd8fe
   bne.un Done

TestCase154:
   ldc.i4 254
   stloc.0
   ldc.i4 0x37b5
   conv.i4
   not
   ldc.i4 0xffffc84a
   beq TestCase153
   br Done

TestCase153:
   ldc.i4 253
   stloc.0
   ldc.i4 0x5374
   conv.i4
   not
   ldc.i4 0xffffac8b
   bne.un Done

TestCase152:
   ldc.i4 252
   stloc.0
   ldc.i4 0x42ac
   conv.i4
   not
   ldc.i4 0xffffbd53
   beq TestCase151
   br Done

TestCase151:
   ldc.i4 251
   stloc.0
   ldc.i4 0x2551
   conv.i4
   not
   ldc.i4 0xffffdaae
   beq TestCase150
   br Done

TestCase150:
   ldc.i4 250
   stloc.0
   ldc.i4 0xffffc03b
   conv.i4
   not
   ldc.i4 0x3fc4
   beq TestCase149
   br Done

TestCase149:
   ldc.i4 249
   stloc.0
   ldc.i4 0x48c1
   conv.i4
   not
   ldc.i4 0xffffb73e
   beq TestCase148
   br Done

TestCase148:
   ldc.i4 248
   stloc.0
   ldc.i4 0xffffe779
   conv.i4
   not
   ldc.i4 0x1886
   beq TestCase147
   br Done

TestCase147:
   ldc.i4 247
   stloc.0
   ldc.i4 0x151f
   conv.i4
   not
   ldc.i4 0xffffeae0
   bne.un Done

TestCase146:
   ldc.i4 246
   stloc.0
   ldc.i4 0x17c0
   conv.i4
   not
   ldc.i4 0xffffe83f
   beq TestCase145
   br Done

TestCase145:
   ldc.i4 245
   stloc.0
   ldc.i4 0x17a2
   conv.i4
   not
   ldc.i4 0xffffe85d
   beq TestCase144
   br Done

TestCase144:
   ldc.i4 244
   stloc.0
   ldc.i4 0x662a
   conv.i4
   not
   ldc.i4 0xffff99d5
   bne.un Done

TestCase143:
   ldc.i4 243
   stloc.0
   ldc.i4 0x6cb3
   conv.i4
   not
   ldc.i4 0xffff934c
   bne.un Done

TestCase142:
   ldc.i4 242
   stloc.0
   ldc.i4 0x4738
   conv.i4
   not
   ldc.i4 0xffffb8c7
   beq TestCase141
   br Done

TestCase141:
   ldc.i4 241
   stloc.0
   ldc.i4 0x107a
   conv.i4
   not
   ldc.i4 0xffffef85
   beq TestCase140
   br Done

TestCase140:
   ldc.i4 240
   stloc.0
   ldc.i4 0x6e0c
   conv.i4
   not
   ldc.i4 0xffff91f3
   bne.un Done

TestCase139:
   ldc.i4 239
   stloc.0
   ldc.i4 0x7f90
   conv.i4
   not
   ldc.i4 0xffff806f
   beq TestCase138
   br Done

TestCase138:
   ldc.i4 238
   stloc.0
   ldc.i4 0x2bcc
   conv.i4
   not
   ldc.i4 0xffffd433
   beq TestCase137
   br Done

TestCase137:
   ldc.i4 237
   stloc.0
   ldc.i4 0xffff9927
   conv.i4
   not
   ldc.i4 0x66d8
   beq TestCase136
   br Done

TestCase136:
   ldc.i4 236
   stloc.0
   ldc.i4 0xffff903c
   conv.i4
   not
   ldc.i4 0x6fc3
   bne.un Done

TestCase135:
   ldc.i4 235
   stloc.0
   ldc.i4 0x4dec
   conv.i4
   not
   ldc.i4 0xffffb213
   beq TestCase134
   br Done

TestCase134:
   ldc.i4 234
   stloc.0
   ldc.i4 0x671c
   conv.i4
   not
   ldc.i4 0xffff98e3
   bne.un Done

TestCase133:
   ldc.i4 233
   stloc.0
   ldc.i4 0xffffd666
   conv.i4
   not
   ldc.i4 0x2999
   beq TestCase132
   br Done

TestCase132:
   ldc.i4 232
   stloc.0
   ldc.i4 0xffffb2ed
   conv.i4
   not
   ldc.i4 0x4d12
   bne.un Done

TestCase131:
   ldc.i4 231
   stloc.0
   ldc.i4 0x2c6d
   conv.i4
   not
   ldc.i4 0xffffd392
   bne.un Done

TestCase130:
   ldc.i4 230
   stloc.0
   ldc.i4 0xffff8210
   conv.i4
   not
   ldc.i4 0x7def
   beq TestCase129
   br Done

TestCase129:
   ldc.i4 229
   stloc.0
   ldc.i4 0x93d
   conv.i4
   not
   ldc.i4 0xfffff6c2
   beq TestCase128
   br Done

TestCase128:
   ldc.i4 228
   stloc.0
   ldc.i4 0x6e31
   conv.i4
   not
   ldc.i4 0xffff91ce
   beq TestCase127
   br Done

TestCase127:
   ldc.i4 227
   stloc.0
   ldc.i4 0x4026
   conv.i4
   not
   ldc.i4 0xffffbfd9
   bne.un Done

TestCase126:
   ldc.i4 226
   stloc.0
   ldc.i4 0x14a3
   conv.i4
   not
   ldc.i4 0xffffeb5c
   beq TestCase125
   br Done

TestCase125:
   ldc.i4 225
   stloc.0
   ldc.i4 0x886
   conv.i4
   not
   ldc.i4 0xfffff779
   bne.un Done

TestCase124:
   ldc.i4 224
   stloc.0
   ldc.i4 0x715b
   conv.i4
   not
   ldc.i4 0xffff8ea4
   bne.un Done

TestCase123:
   ldc.i4 223
   stloc.0
   ldc.i4 0xfffff52f
   conv.i4
   not
   ldc.i4 0xad0
   bne.un Done

TestCase122:
   ldc.i4 222
   stloc.0
   ldc.i4 0xffff8b2c
   conv.i4
   not
   ldc.i4 0x74d3
   bne.un Done

TestCase121:
   ldc.i4 221
   stloc.0
   ldc.i4 0xffff8caa
   conv.i4
   not
   ldc.i4 0x7355
   beq TestCase120
   br Done

TestCase120:
   ldc.i4 220
   stloc.0
   ldc.i4 0xffff8e64
   conv.i4
   not
   ldc.i4 0x719b
   bne.un Done

TestCase119:
   ldc.i4 219
   stloc.0
   ldc.i4 0x34d
   conv.i4
   not
   ldc.i4 0xfffffcb2
   beq TestCase118
   br Done

TestCase118:
   ldc.i4 218
   stloc.0
   ldc.i4 0x484f
   conv.i4
   not
   ldc.i4 0xffffb7b0
   beq TestCase117
   br Done

TestCase117:
   ldc.i4 217
   stloc.0
   ldc.i4 0xffffc838
   conv.i4
   not
   ldc.i4 0x37c7
   bne.un Done

TestCase116:
   ldc.i4 216
   stloc.0
   ldc.i4 0xffff8f26
   conv.i4
   not
   ldc.i4 0x70d9
   bne.un Done

TestCase115:
   ldc.i4 215
   stloc.0
   ldc.i4 0x7659
   conv.i4
   not
   ldc.i4 0xffff89a6
   bne.un Done

TestCase114:
   ldc.i4 214
   stloc.0
   ldc.i4 0x3539
   conv.i4
   not
   ldc.i4 0xffffcac6
   beq TestCase113
   br Done

TestCase113:
   ldc.i4 213
   stloc.0
   ldc.i4 0xffff8271
   conv.i4
   not
   ldc.i4 0x7d8e
   beq TestCase112
   br Done

TestCase112:
   ldc.i4 212
   stloc.0
   ldc.i4 0x3124
   conv.i4
   not
   ldc.i4 0xffffcedb
   bne.un Done

TestCase111:
   ldc.i4 211
   stloc.0
   ldc.i4 0x2b92
   conv.i4
   not
   ldc.i4 0xffffd46d
   bne.un Done

TestCase110:
   ldc.i4 210
   stloc.0
   ldc.i4 0x477a
   conv.i4
   not
   ldc.i4 0xffffb885
   beq TestCase109
   br Done

TestCase109:
   ldc.i4 209
   stloc.0
   ldc.i4 0xffffe872
   conv.i4
   not
   ldc.i4 0x178d
   beq TestCase108
   br Done

TestCase108:
   ldc.i4 208
   stloc.0
   ldc.i4 0x93b
   conv.i4
   not
   ldc.i4 0xfffff6c4
   beq TestCase107
   br Done

TestCase107:
   ldc.i4 207
   stloc.0
   ldc.i4 0xaaa
   conv.i4
   not
   ldc.i4 0xfffff555
   beq TestCase106
   br Done

TestCase106:
   ldc.i4 206
   stloc.0
   ldc.i4 0x536f
   conv.i4
   not
   ldc.i4 0xffffac90
   bne.un Done

TestCase105:
   ldc.i4 205
   stloc.0
   ldc.i4 0xffffd448
   conv.i4
   not
   ldc.i4 0x2bb7
   bne.un Done

TestCase104:
   ldc.i4 204
   stloc.0
   ldc.i4 0x5e
   conv.i4
   not
   ldc.i4 0xffffffa1
   beq TestCase103
   br Done

TestCase103:
   ldc.i4 203
   stloc.0
   ldc.i4 0x3cea
   conv.i4
   not
   ldc.i4 0xffffc315
   bne.un Done

TestCase102:
   ldc.i4 202
   stloc.0
   ldc.i4 0x3dd0
   conv.i4
   not
   ldc.i4 0xffffc22f
   bne.un Done

TestCase101:
   ldc.i4 201
   stloc.0
   ldc.i4 0x2d2a
   conv.i4
   not
   ldc.i4 0xffffd2d5
   beq TestCase100
   br Done

TestCase100:
   ldc.i4 200
   stloc.0
   ldc.i4 0xffffc2a7
   conv.i4
   not
   ldc.i4 0x3d58
   beq TestCase99
   br Done

TestCase99:
   ldc.i4 199
   stloc.0
   ldc.i4 0xfffff916
   conv.i4
   not
   ldc.i4 0x6e9
   beq TestCase98
   br Done

TestCase98:
   ldc.i4 198
   stloc.0
   ldc.i4 0x54de
   conv.i4
   not
   ldc.i4 0xffffab21
   beq TestCase97
   br Done

TestCase97:
   ldc.i4 197
   stloc.0
   ldc.i4 0x486
   conv.i4
   not
   ldc.i4 0xfffffb79
   bne.un Done

TestCase96:
   ldc.i4 196
   stloc.0
   ldc.i4 0xffffeb30
   conv.i4
   not
   ldc.i4 0x14cf
   beq TestCase95
   br Done

TestCase95:
   ldc.i4 195
   stloc.0
   ldc.i4 0x568e
   conv.i4
   not
   ldc.i4 0xffffa971
   beq TestCase94
   br Done

TestCase94:
   ldc.i4 194
   stloc.0
   ldc.i4 0x26d1
   conv.i4
   not
   ldc.i4 0xffffd92e
   bne.un Done

TestCase93:
   ldc.i4 193
   stloc.0
   ldc.i4 0xfffffd6f
   conv.i4
   not
   ldc.i4 0x290
   beq TestCase92
   br Done

TestCase92:
   ldc.i4 192
   stloc.0
   ldc.i4 0x42c7
   conv.i4
   not
   ldc.i4 0xffffbd38
   beq TestCase91
   br Done

TestCase91:
   ldc.i4 191
   stloc.0
   ldc.i4 0xffffbebd
   conv.i4
   not
   ldc.i4 0x4142
   bne.un Done

TestCase90:
   ldc.i4 190
   stloc.0
   ldc.i4 0x180
   conv.i4
   not
   ldc.i4 0xfffffe7f
   bne.un Done

TestCase89:
   ldc.i4 189
   stloc.0
   ldc.i4 0x7910
   conv.i4
   not
   ldc.i4 0xffff86ef
   beq TestCase88
   br Done

TestCase88:
   ldc.i4 188
   stloc.0
   ldc.i4 0x1b75
   conv.i4
   not
   ldc.i4 0xffffe48a
   beq TestCase87
   br Done

TestCase87:
   ldc.i4 187
   stloc.0
   ldc.i4 0xffff9185
   conv.i4
   not
   ldc.i4 0x6e7a
   beq TestCase86
   br Done

TestCase86:
   ldc.i4 186
   stloc.0
   ldc.i4 0xfffff8ae
   conv.i4
   not
   ldc.i4 0x751
   bne.un Done

TestCase85:
   ldc.i4 185
   stloc.0
   ldc.i4 0x15df
   conv.i4
   not
   ldc.i4 0xffffea20
   beq TestCase84
   br Done

TestCase84:
   ldc.i4 184
   stloc.0
   ldc.i4 0xffffc0b6
   conv.i4
   not
   ldc.i4 0x3f49
   bne.un Done

TestCase83:
   ldc.i4 183
   stloc.0
   ldc.i4 0x2a65
   conv.i4
   not
   ldc.i4 0xffffd59a
   bne.un Done

TestCase82:
   ldc.i4 182
   stloc.0
   ldc.i4 0x564
   conv.i4
   not
   ldc.i4 0xfffffa9b
   beq TestCase81
   br Done

TestCase81:
   ldc.i4 181
   stloc.0
   ldc.i4 0x5c52
   conv.i4
   not
   ldc.i4 0xffffa3ad
   bne.un Done

TestCase80:
   ldc.i4 180
   stloc.0
   ldc.i4 0x1e3e
   conv.i4
   not
   ldc.i4 0xffffe1c1
   bne.un Done

TestCase79:
   ldc.i4 179
   stloc.0
   ldc.i4 0x1cf1
   conv.i4
   not
   ldc.i4 0xffffe30e
   beq TestCase78
   br Done

TestCase78:
   ldc.i4 178
   stloc.0
   ldc.i4 0xffff8905
   conv.i4
   not
   ldc.i4 0x76fa
   beq TestCase77
   br Done

TestCase77:
   ldc.i4 177
   stloc.0
   ldc.i4 0x58cf
   conv.i4
   not
   ldc.i4 0xffffa730
   beq TestCase76
   br Done

TestCase76:
   ldc.i4 176
   stloc.0
   ldc.i4 0x565b
   conv.i4
   not
   ldc.i4 0xffffa9a4
   beq TestCase75
   br Done

TestCase75:
   ldc.i4 175
   stloc.0
   ldc.i4 0x6103
   conv.i4
   not
   ldc.i4 0xffff9efc
   beq TestCase74
   br Done

TestCase74:
   ldc.i4 174
   stloc.0
   ldc.i4 0xffff9dd5
   conv.i4
   not
   ldc.i4 0x622a
   bne.un Done

TestCase73:
   ldc.i4 173
   stloc.0
   ldc.i4 0xffff9554
   conv.i4
   not
   ldc.i4 0x6aab
   beq TestCase72
   br Done

TestCase72:
   ldc.i4 172
   stloc.0
   ldc.i4 0x398c
   conv.i4
   not
   ldc.i4 0xffffc673
   beq TestCase71
   br Done

TestCase71:
   ldc.i4 171
   stloc.0
   ldc.i4 0xffff880a
   conv.i4
   not
   ldc.i4 0x77f5
   beq TestCase70
   br Done

TestCase70:
   ldc.i4 170
   stloc.0
   ldc.i4 0xffff8638
   conv.i4
   not
   ldc.i4 0x79c7
   bne.un Done

TestCase69:
   ldc.i4 169
   stloc.0
   ldc.i4 0xffffbfbb
   conv.i4
   not
   ldc.i4 0x4044
   bne.un Done

TestCase68:
   ldc.i4 168
   stloc.0
   ldc.i4 0x6a8
   conv.i4
   not
   ldc.i4 0xfffff957
   bne.un Done

TestCase67:
   ldc.i4 167
   stloc.0
   ldc.i4 0x5553
   conv.i4
   not
   ldc.i4 0xffffaaac
   bne.un Done

TestCase66:
   ldc.i4 166
   stloc.0
   ldc.i4 0x308c
   conv.i4
   not
   ldc.i4 0xffffcf73
   bne.un Done

TestCase65:
   ldc.i4 165
   stloc.0
   ldc.i4 0x6161
   conv.i4
   not
   ldc.i4 0xffff9e9e
   bne.un Done

TestCase64:
   ldc.i4 164
   stloc.0
   ldc.i4 0xffffde47
   conv.i4
   not
   ldc.i4 0x21b8
   beq TestCase63
   br Done

TestCase63:
   ldc.i4 163
   stloc.0
   ldc.i4 0xffff812e
   conv.i4
   not
   ldc.i4 0x7ed1
   bne.un Done

TestCase62:
   ldc.i4 162
   stloc.0
   ldc.i4 0x298a
   conv.i4
   not
   ldc.i4 0xffffd675
   bne.un Done

TestCase61:
   ldc.i4 161
   stloc.0
   ldc.i4 0xffff9c3a
   conv.i4
   not
   ldc.i4 0x63c5
   beq TestCase60
   br Done

TestCase60:
   ldc.i4 160
   stloc.0
   ldc.i4 0x5791
   conv.i4
   not
   ldc.i4 0xffffa86e
   beq TestCase59
   br Done

TestCase59:
   ldc.i4 159
   stloc.0
   ldc.i4 0xffff836f
   conv.i4
   not
   ldc.i4 0x7c90
   beq TestCase58
   br Done

TestCase58:
   ldc.i4 158
   stloc.0
   ldc.i4 0xffffce2e
   conv.i4
   not
   ldc.i4 0x31d1
   bne.un Done

TestCase57:
   ldc.i4 157
   stloc.0
   ldc.i4 0xffffd9e6
   conv.i4
   not
   ldc.i4 0x2619
   beq TestCase56
   br Done

TestCase56:
   ldc.i4 156
   stloc.0
   ldc.i4 0x788f
   conv.i4
   not
   ldc.i4 0xffff8770
   beq TestCase55
   br Done

TestCase55:
   ldc.i4 155
   stloc.0
   ldc.i4 0x1592
   conv.i4
   not
   ldc.i4 0xffffea6d
   beq TestCase54
   br Done

TestCase54:
   ldc.i4 154
   stloc.0
   ldc.i4 0x2f65
   conv.i4
   not
   ldc.i4 0xffffd09a
   beq TestCase53
   br Done

TestCase53:
   ldc.i4 153
   stloc.0
   ldc.i4 0xffffd303
   conv.i4
   not
   ldc.i4 0x2cfc
   beq TestCase52
   br Done

TestCase52:
   ldc.i4 152
   stloc.0
   ldc.i4 0x56ad
   conv.i4
   not
   ldc.i4 0xffffa952
   bne.un Done

TestCase51:
   ldc.i4 151
   stloc.0
   ldc.i4 0x147a
   conv.i4
   not
   ldc.i4 0xffffeb85
   bne.un Done

TestCase50:
   ldc.i4 150
   stloc.0
   ldc.i4 0xffffcea5
   conv.i4
   not
   ldc.i4 0x315a
   bne.un Done

TestCase49:
   ldc.i4 149
   stloc.0
   ldc.i4 0x7e63
   conv.i4
   not
   ldc.i4 0xffff819c
   beq TestCase48
   br Done

TestCase48:
   ldc.i4 148
   stloc.0
   ldc.i4 0x72cd
   conv.i4
   not
   ldc.i4 0xffff8d32
   bne.un Done

TestCase47:
   ldc.i4 147
   stloc.0
   ldc.i4 0x1088
   conv.i4
   not
   ldc.i4 0xffffef77
   bne.un Done

TestCase46:
   ldc.i4 146
   stloc.0
   ldc.i4 0xfffff2ae
   conv.i4
   not
   ldc.i4 0xd51
   bne.un Done

TestCase45:
   ldc.i4 145
   stloc.0
   ldc.i4 0xffffe8ca
   conv.i4
   not
   ldc.i4 0x1735
   beq TestCase44
   br Done

TestCase44:
   ldc.i4 144
   stloc.0
   ldc.i4 0xffff9abb
   conv.i4
   not
   ldc.i4 0x6544
   beq TestCase43
   br Done

TestCase43:
   ldc.i4 143
   stloc.0
   ldc.i4 0x76d3
   conv.i4
   not
   ldc.i4 0xffff892c
   beq TestCase42
   br Done

TestCase42:
   ldc.i4 142
   stloc.0
   ldc.i4 0x3619
   conv.i4
   not
   ldc.i4 0xffffc9e6
   beq TestCase41
   br Done

TestCase41:
   ldc.i4 141
   stloc.0
   ldc.i4 0x572b
   conv.i4
   not
   ldc.i4 0xffffa8d4
   bne.un Done

TestCase40:
   ldc.i4 140
   stloc.0
   ldc.i4 0x4da8
   conv.i4
   not
   ldc.i4 0xffffb257
   bne.un Done

TestCase39:
   ldc.i4 139
   stloc.0
   ldc.i4 0x4fc8
   conv.i4
   not
   ldc.i4 0xffffb037
   bne.un Done

TestCase38:
   ldc.i4 138
   stloc.0
   ldc.i4 0x379c
   conv.i4
   not
   ldc.i4 0xffffc863
   beq TestCase37
   br Done

TestCase37:
   ldc.i4 137
   stloc.0
   ldc.i4 0xffff811c
   conv.i4
   not
   ldc.i4 0x7ee3
   beq TestCase36
   br Done

TestCase36:
   ldc.i4 136
   stloc.0
   ldc.i4 0x55d6
   conv.i4
   not
   ldc.i4 0xffffaa29
   beq TestCase35
   br Done

TestCase35:
   ldc.i4 135
   stloc.0
   ldc.i4 0x7ce2
   conv.i4
   not
   ldc.i4 0xffff831d
   beq TestCase34
   br Done

TestCase34:
   ldc.i4 134
   stloc.0
   ldc.i4 0xfffffbee
   conv.i4
   not
   ldc.i4 0x411
   beq TestCase33
   br Done

TestCase33:
   ldc.i4 133
   stloc.0
   ldc.i4 0x1c47
   conv.i4
   not
   ldc.i4 0xffffe3b8
   beq TestCase32
   br Done

TestCase32:
   ldc.i4 132
   stloc.0
   ldc.i4 0x1445
   conv.i4
   not
   ldc.i4 0xffffebba
   beq TestCase31
   br Done

TestCase31:
   ldc.i4 131
   stloc.0
   ldc.i4 0xffff9556
   conv.i4
   not
   ldc.i4 0x6aa9
   beq TestCase30
   br Done

TestCase30:
   ldc.i4 130
   stloc.0
   ldc.i4 0x7e65
   conv.i4
   not
   ldc.i4 0xffff819a
   bne.un Done

TestCase29:
   ldc.i4 129
   stloc.0
   ldc.i4 0xfffffee3
   conv.i4
   not
   ldc.i4 0x11c
   beq TestCase28
   br Done

TestCase28:
   ldc.i4 128
   stloc.0
   ldc.i4 0x5991
   conv.i4
   not
   ldc.i4 0xffffa66e
   bne.un Done

TestCase27:
   ldc.i4 127
   stloc.0
   ldc.i4 0xffff8d0f
   conv.i4
   not
   ldc.i4 0x72f0
   bne.un Done

TestCase26:
   ldc.i4 126
   stloc.0
   ldc.i4 0x6de1
   conv.i4
   not
   ldc.i4 0xffff921e
   beq TestCase25
   br Done

TestCase25:
   ldc.i4 125
   stloc.0
   ldc.i4 0xffffe012
   conv.i4
   not
   ldc.i4 0x1fed
   bne.un Done

TestCase24:
   ldc.i4 124
   stloc.0
   ldc.i4 0xffffcabe
   conv.i4
   not
   ldc.i4 0x3541
   bne.un Done

TestCase23:
   ldc.i4 123
   stloc.0
   ldc.i4 0x6b4c
   conv.i4
   not
   ldc.i4 0xffff94b3
   bne.un Done

TestCase22:
   ldc.i4 122
   stloc.0
   ldc.i4 0x4720
   conv.i4
   not
   ldc.i4 0xffffb8df
   bne.un Done

TestCase21:
   ldc.i4 121
   stloc.0
   ldc.i4 0xffff94e6
   conv.i4
   not
   ldc.i4 0x6b19
   beq TestCase20
   br Done

TestCase20:
   ldc.i4 120
   stloc.0
   ldc.i4 0x3ddc
   conv.i4
   not
   ldc.i4 0xffffc223
   beq TestCase19
   br Done

TestCase19:
   ldc.i4 119
   stloc.0
   ldc.i4 0x1bd0
   conv.i4
   not
   ldc.i4 0xffffe42f
   bne.un Done

TestCase18:
   ldc.i4 118
   stloc.0
   ldc.i4 0xffff89cb
   conv.i4
   not
   ldc.i4 0x7634
   bne.un Done

TestCase17:
   ldc.i4 117
   stloc.0
   ldc.i4 0xffffedc5
   conv.i4
   not
   ldc.i4 0x123a
   bne.un Done

TestCase16:
   ldc.i4 116
   stloc.0
   ldc.i4 0x682c
   conv.i4
   not
   ldc.i4 0xffff97d3
   bne.un Done

TestCase15:
   ldc.i4 115
   stloc.0
   ldc.i4 0xffffe5f1
   conv.i4
   not
   ldc.i4 0x1a0e
   bne.un Done

TestCase14:
   ldc.i4 114
   stloc.0
   ldc.i4 0x1617
   conv.i4
   not
   ldc.i4 0xffffe9e8
   bne.un Done

TestCase13:
   ldc.i4 113
   stloc.0
   ldc.i4 0xffffc646
   conv.i4
   not
   ldc.i4 0x39b9
   bne.un Done

TestCase12:
   ldc.i4 112
   stloc.0
   ldc.i4 0x79d1
   conv.i4
   not
   ldc.i4 0xffff862e
   bne.un Done

TestCase11:
   ldc.i4 111
   stloc.0
   ldc.i4 0xffffbbae
   conv.i4
   not
   ldc.i4 0x4451
   bne.un Done

TestCase10:
   ldc.i4 110
   stloc.0
   ldc.i4 0xffffbeb1
   conv.i4
   not
   ldc.i4 0x414e
   bne.un Done

TestCase9:
   ldc.i4 109
   stloc.0
   ldc.i4 0xfffff750
   conv.i4
   not
   ldc.i4 0x8af
   beq TestCase8
   br Done

TestCase8:
   ldc.i4 108
   stloc.0
   ldc.i4 0xffff9cf8
   conv.i4
   not
   ldc.i4 0x6307
   beq TestCase7
   br Done

TestCase7:
   ldc.i4 107
   stloc.0
   ldc.i4 0x5918
   conv.i4
   not
   ldc.i4 0xffffa6e7
   beq TestCase6
   br Done

TestCase6:
   ldc.i4 106
   stloc.0
   ldc.i4 0xffff981b
   conv.i4
   not
   ldc.i4 0x67e4
   bne.un Done

TestCase5:
   ldc.i4 105
   stloc.0
   ldc.i4 0xffffd701
   conv.i4
   not
   ldc.i4 0x28fe
   bne.un Done

TestCase4:
   ldc.i4 104
   stloc.0
   ldc.i4 0xfffff444
   conv.i4
   not
   ldc.i4 0xbbb
   bne.un Done

TestCase3:
   ldc.i4 103
   stloc.0
   ldc.i4 0xffff9933
   conv.i4
   not
   ldc.i4 0x66cc
   bne.un Done

TestCase2:
   ldc.i4 102
   stloc.0
   ldc.i4 0x5687
   conv.i4
   not
   ldc.i4 0xffffa978
   beq TestCase1
   br Done

TestCase1:
   ldc.i4 101
   stloc.0
   ldc.i4 0x15af
   conv.i4
   not
   ldc.i4 0xffffea50
   bne.un Done

   ldc.i4 100
   stloc.0

Done:
   ldloc.0
   ret
}
}
