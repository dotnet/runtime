.assembly extern mscorlib{}
.assembly extern xunit.core {}
.assembly rvastatic3{}
.class public A{
 .method static native int Call1(int64) {.maxstack  50
  ldarg.0
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  conv.i
  ret
 }
 .method static native int Call2(float64) {.maxstack  50
  ldarg.0
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i
  ret
 }
 .method static void V1() {.maxstack  50
  ldsfld int64 [rvastatic3]A::a0100
  ldc.i8 0
  beq a0101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0101:
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 1
  beq a0102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0102:
  ldsfld float32 [rvastatic3]A::a0102
  ldc.r4 2.0
  beq a0103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0103:
  ldsfld int64 [rvastatic3]A::a0103
  ldc.i8 3
  beq a0104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0104:
  ldsfld int64 [rvastatic3]A::a0104
  ldc.i8 4
  beq a0105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0105:
  ldsfld int16 [rvastatic3]A::a0105
  ldc.i4 5
  beq a0106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0106:
  ldsfld int16 [rvastatic3]A::a0106
  ldc.i4 6
  beq a0107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0107:
  ldsfld float32 [rvastatic3]A::a0107
  ldc.r4 7.0
  beq a0108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0108:
  ldsfld int32 [rvastatic3]A::a0108
  ldc.i4 8
  beq a0109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0109:
  ldsfld int8 [rvastatic3]A::a0109
  ldc.i4 9
  beq a01010
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01010:
  ldsfld int32 [rvastatic3]A::a01010
  ldc.i4 10
  beq a01011
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01011:
  ldsfld int64 [rvastatic3]A::a01011
  ldc.i8 11
  beq a01012
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01012:
  ldsfld int32 [rvastatic3]A::a01012
  ldc.i4 12
  beq a01013
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01013:
  ldsfld int8 [rvastatic3]A::a01013
  ldc.i4 13
  beq a01014
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01014:
  ldsfld int16 [rvastatic3]A::a01014
  ldc.i4 14
  beq a01015
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01015:
  ldsfld int16 [rvastatic3]A::a01015
  ldc.i4 15
  beq a01016
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01016:
  ldsfld float32 [rvastatic3]A::a01016
  ldc.r4 16.0
  beq a01017
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01017:
  ldsfld float32 [rvastatic3]A::a01017
  ldc.r4 17.0
  beq a01018
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01018:
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 18
  beq a01019
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01019:
  ldsfld int8 [rvastatic3]A::a01019
  ldc.i4 19
  beq a01020
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01020:
  ldsfld int32 [rvastatic3]A::a01020
  ldc.i4 20
  beq a01021
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01021:
  ldsfld int32 [rvastatic3]A::a01021
  ldc.i4 21
  beq a01022
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01022:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 22
  beq a01023
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01023:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 23
  beq a01024
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01024:
  ldsfld int8 [rvastatic3]A::a01024
  ldc.i4 24
  beq a01025
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01025:
  ldsfld int8 [rvastatic3]A::a01025
  ldc.i4 25
  beq a01026
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01026:
  ldsfld int16 [rvastatic3]A::a01026
  ldc.i4 26
  beq a01027
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01027:
  ldsfld int8 [rvastatic3]A::a01027
  ldc.i4 27
  beq a01028
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01028:
  ldsfld int16 [rvastatic3]A::a01028
  ldc.i4 28
  beq a01029
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01029:
  ldsfld int64 [rvastatic3]A::a01029
  ldc.i8 29
  beq a01030
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01030:
  ldsfld int32 [rvastatic3]A::a01030
  ldc.i4 30
  beq a01031
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01031:
  ldsfld int32 [rvastatic3]A::a01031
  ldc.i4 31
  beq a01032
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01032:
  ldsfld int32 [rvastatic3]A::a01032
  ldc.i4 32
  beq a01033
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01033:
  ldsfld int8 [rvastatic3]A::a01033
  ldc.i4 33
  beq a01034
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01034:
  ldsfld int16 [rvastatic3]A::a01034
  ldc.i4 34
  beq a01035
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01035:
  ldsfld int32 [rvastatic3]A::a01035
  ldc.i4 35
  beq a01036
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01036:
  ldsfld int32 [rvastatic3]A::a01036
  ldc.i4 36
  beq a01037
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01037:
  ldsfld int16 [rvastatic3]A::a01037
  ldc.i4 37
  beq a01038
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01038:
  ldsfld float32 [rvastatic3]A::a01038
  ldc.r4 38.0
  beq a01039
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01039:
  ldsfld int8 [rvastatic3]A::a01039
  ldc.i4 39
  beq a01040
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01040:
  ldsfld int8 [rvastatic3]A::a01040
  ldc.i4 40
  beq a01041
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01041:
  ldsfld float32 [rvastatic3]A::a01041
  ldc.r4 41.0
  beq a01042
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01042:
  ldsfld int32 [rvastatic3]A::a01042
  ldc.i4 42
  beq a01043
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01043:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 43
  beq a01044
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01044:
  ldsfld int32 [rvastatic3]A::a01044
  ldc.i4 44
  beq a01045
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01045:
  ldsfld int64 [rvastatic3]A::a01045
  ldc.i8 45
  beq a01046
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01046:
  ldsfld int64 [rvastatic3]A::a01046
  ldc.i8 46
  beq a01047
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01047:
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 47
  beq a01048
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01048:
  ldsfld float32 [rvastatic3]A::a01048
  ldc.r4 48.0
  beq a01049
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01049:
  ldsfld int64 [rvastatic3]A::a01049
  ldc.i8 49
  beq a01050
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01050:
  ldsfld int32 [rvastatic3]A::a01050
  ldc.i4 50
  beq a01051
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01051:
  ldsfld float32 [rvastatic3]A::a01051
  ldc.r4 51.0
  beq a01052
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01052:
  ldsfld int32 [rvastatic3]A::a01052
  ldc.i4 52
  beq a01053
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01053:
  ldsfld int64 [rvastatic3]A::a01053
  ldc.i8 53
  beq a01054
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01054:
  ldsfld int8 [rvastatic3]A::a01054
  ldc.i4 54
  beq a01055
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01055:
  ldsfld int8 [rvastatic3]A::a01055
  ldc.i4 55
  beq a01056
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01056:
  ldsfld float32 [rvastatic3]A::a01056
  ldc.r4 56.0
  beq a01057
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01057:
  ldsfld int32 [rvastatic3]A::a01057
  ldc.i4 57
  beq a01058
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01058:
  ldsfld int64 [rvastatic3]A::a01058
  ldc.i8 58
  beq a01059
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01059:
  ldsfld int64 [rvastatic3]A::a01059
  ldc.i8 59
  beq a01060
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01060:
  ldsfld int8 [rvastatic3]A::a01060
  ldc.i4 60
  beq a01061
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01061:
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 61
  beq a01062
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01062:
  ldsfld int64 [rvastatic3]A::a01062
  ldc.i8 62
  beq a01063
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01063:
  ldsfld float32 [rvastatic3]A::a01063
  ldc.r4 63.0
  beq a01064
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01064:
  ldsfld int64 [rvastatic3]A::a01064
  ldc.i8 64
  beq a01065
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01065:
  ldsfld float32 [rvastatic3]A::a01065
  ldc.r4 65.0
  beq a01066
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01066:
  ldsfld int8 [rvastatic3]A::a01066
  ldc.i4 66
  beq a01067
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01067:
  ldsfld int16 [rvastatic3]A::a01067
  ldc.i4 67
  beq a01068
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01068:
  ldsfld int64 [rvastatic3]A::a01068
  ldc.i8 68
  beq a01069
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01069:
  ldsfld int64 [rvastatic3]A::a01069
  ldc.i8 69
  beq a01070
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01070:
  ldsfld int64 [rvastatic3]A::a01070
  ldc.i8 70
  beq a01071
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01071:
  ldsfld int32 [rvastatic3]A::a01071
  ldc.i4 71
  beq a01072
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01072:
  ldsfld int8 [rvastatic3]A::a01072
  ldc.i4 72
  beq a01073
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01073:
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 73
  beq a01074
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01074:
  ldsfld float32 [rvastatic3]A::a01074
  ldc.r4 74.0
  beq a01075
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01075:
  ldsfld int64 [rvastatic3]A::a01075
  ldc.i8 75
  beq a01076
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01076:
  ldsfld int8 [rvastatic3]A::a01076
  ldc.i4 76
  beq a01077
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01077:
  ldsfld int8 [rvastatic3]A::a01077
  ldc.i4 77
  beq a01078
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01078:
  ldsfld int8 [rvastatic3]A::a01078
  ldc.i4 78
  beq a01079
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01079:
  ldsfld int32 [rvastatic3]A::a01079
  ldc.i4 79
  beq a01080
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01080:
  ldsfld int16 [rvastatic3]A::a01080
  ldc.i4 80
  beq a01081
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01081:
  ldsfld int32 [rvastatic3]A::a01081
  ldc.i4 81
  beq a01082
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01082:
  ldsfld int8 [rvastatic3]A::a01082
  ldc.i4 82
  beq a01083
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01083:
  ldsfld int32 [rvastatic3]A::a01083
  ldc.i4 83
  beq a01084
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01084:
  ldsfld int8 [rvastatic3]A::a01084
  ldc.i4 84
  beq a01085
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01085:
  ldsfld int32 [rvastatic3]A::a01085
  ldc.i4 85
  beq a01086
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01086:
  ldsfld int8 [rvastatic3]A::a01086
  ldc.i4 86
  beq a01087
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01087:
  ldsfld int64 [rvastatic3]A::a01087
  ldc.i8 87
  beq a01088
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01088:
  ldsfld int8 [rvastatic3]A::a01088
  ldc.i4 88
  beq a01089
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01089:
  ldsfld int16 [rvastatic3]A::a01089
  ldc.i4 89
  beq a01090
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01090:
  ldsfld int64 [rvastatic3]A::a01090
  ldc.i8 90
  beq a01091
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01091:
  ldsfld int8 [rvastatic3]A::a01091
  ldc.i4 91
  beq a01092
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01092:
  ldsfld int64 [rvastatic3]A::a01092
  ldc.i8 92
  beq a01093
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01093:
  ldsfld int16 [rvastatic3]A::a01093
  ldc.i4 93
  beq a01094
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01094:
  ldsfld int8 [rvastatic3]A::a01094
  ldc.i4 94
  beq a01095
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01095:
  ldsfld float32 [rvastatic3]A::a01095
  ldc.r4 95.0
  beq a01096
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01096:
  ldsfld int16 [rvastatic3]A::a01096
  ldc.i4 96
  beq a01097
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01097:
  ldsfld int64 [rvastatic3]A::a01097
  ldc.i8 97
  beq a01098
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01098:
  ldsfld float32 [rvastatic3]A::a01098
  ldc.r4 98.0
  beq a01099
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01099:
  ldsfld int32 [rvastatic3]A::a01099
  ldc.i4 99
  beq a010100
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010100:
  ldsfld int32 [rvastatic3]A::a010100
  ldc.i4 100
  beq a010101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010101:
  ldsfld int32 [rvastatic3]A::a010101
  ldc.i4 101
  beq a010102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010102:
  ldsfld int8 [rvastatic3]A::a010102
  ldc.i4 102
  beq a010103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010103:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 103
  beq a010104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010104:
  ldsfld int8 [rvastatic3]A::a010104
  ldc.i4 104
  beq a010105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010105:
  ldsfld float32 [rvastatic3]A::a010105
  ldc.r4 105.0
  beq a010106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010106:
  ldsfld float32 [rvastatic3]A::a010106
  ldc.r4 106.0
  beq a010107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010107:
  ldsfld int8 [rvastatic3]A::a010107
  ldc.i4 107
  beq a010108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010108:
  ldsfld float32 [rvastatic3]A::a010108
  ldc.r4 108.0
  beq a010109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010109:
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 109
  beq a010110
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010110:
  ldsfld int8 [rvastatic3]A::a010110
  ldc.i4 110
  beq a010111
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010111:
  ldsfld int64 [rvastatic3]A::a010111
  ldc.i8 111
  beq a010112
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010112:
  ldsfld int16 [rvastatic3]A::a010112
  ldc.i4 112
  beq a010113
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010113:
  ldsfld int16 [rvastatic3]A::a010113
  ldc.i4 113
  beq a010114
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010114:
  ldsfld int32 [rvastatic3]A::a010114
  ldc.i4 114
  beq a010115
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010115:
  ldsfld int32 [rvastatic3]A::a010115
  ldc.i4 115
  beq a010116
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010116:
  ldsfld int8 [rvastatic3]A::a010116
  ldc.i4 116
  beq a010117
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010117:
  ldsfld int16 [rvastatic3]A::a010117
  ldc.i4 117
  beq a010118
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010118:
  ldsfld int64 [rvastatic3]A::a010118
  ldc.i8 118
  beq a010119
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010119:
  ldsfld int8 [rvastatic3]A::a010119
  ldc.i4 119
  beq a010120
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010120:
  ldsfld int64 [rvastatic3]A::a010120
  ldc.i8 120
  beq a010121
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010121:
  ldsfld int32 [rvastatic3]A::a010121
  ldc.i4 121
  beq a010122
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010122:
  ldsfld int8 [rvastatic3]A::a010122
  ldc.i4 2
  beq a010123
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010123:
  ldsfld int32 [rvastatic3]A::a010123
  ldc.i4 123
  beq a010124
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010124:
  ldsfld int8 [rvastatic3]A::a010124
  ldc.i4 4
  beq a010125
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010125:
  ldsfld int64 [rvastatic3]A::a010125
  ldc.i8 125
  beq a010126
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010126:
  ldsfld float32 [rvastatic3]A::a010126
  ldc.r4 126.0
  beq a010127
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010127:
  ldsfld int64 [rvastatic3]A::a010127
  ldc.i8 127
  beq a010128
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010128:
 ret}
 .method static void V2() {.maxstack  50
  ldsflda int64 [rvastatic3]A::a0100
  ldind.i8
  ldc.i8 0
  beq a0100
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0100:
  ldsflda int64 [rvastatic3]A::a0101
  ldind.i8
  ldc.i8 1
  beq a0101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0101:
  ldsflda float32 [rvastatic3]A::a0102
  ldind.r4
  ldc.r4 2.0
  beq a0102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0102:
  ldsflda int64 [rvastatic3]A::a0103
  ldind.i8
  ldc.i8 3
  beq a0103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0103:
  ldsflda int64 [rvastatic3]A::a0104
  ldind.i8
  ldc.i8 4
  beq a0104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0104:
  ldsflda int16 [rvastatic3]A::a0105
  ldind.i2
  ldc.i4 5
  beq a0105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0105:
  ldsflda int16 [rvastatic3]A::a0106
  ldind.i2
  ldc.i4 6
  beq a0106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0106:
  ldsflda float32 [rvastatic3]A::a0107
  ldind.r4
  ldc.r4 7.0
  beq a0107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0107:
  ldsflda int32 [rvastatic3]A::a0108
  ldind.i4
  ldc.i4 8
  beq a0108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0108:
  ldsflda int8 [rvastatic3]A::a0109
  ldind.i1
  ldc.i4 9
  beq a0109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0109:
  ldsflda int32 [rvastatic3]A::a01010
  ldind.i4
  ldc.i4 10
  beq a01010
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01010:
  ldsflda int64 [rvastatic3]A::a01011
  ldind.i8
  ldc.i8 11
  beq a01011
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01011:
  ldsflda int32 [rvastatic3]A::a01012
  ldind.i4
  ldc.i4 12
  beq a01012
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01012:
  ldsflda int8 [rvastatic3]A::a01013
  ldind.i1
  ldc.i4 13
  beq a01013
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01013:
  ldsflda int16 [rvastatic3]A::a01014
  ldind.i2
  ldc.i4 14
  beq a01014
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01014:
  ldsflda int16 [rvastatic3]A::a01015
  ldind.i2
  ldc.i4 15
  beq a01015
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01015:
  ldsflda float32 [rvastatic3]A::a01016
  ldind.r4
  ldc.r4 16.0
  beq a01016
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01016:
  ldsflda float32 [rvastatic3]A::a01017
  ldind.r4
  ldc.r4 17.0
  beq a01017
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01017:
  ldsflda int32 [rvastatic3]A::a01018
  ldind.i4
  ldc.i4 18
  beq a01018
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01018:
  ldsflda int8 [rvastatic3]A::a01019
  ldind.i1
  ldc.i4 19
  beq a01019
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01019:
  ldsflda int32 [rvastatic3]A::a01020
  ldind.i4
  ldc.i4 20
  beq a01020
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01020:
  ldsflda int32 [rvastatic3]A::a01021
  ldind.i4
  ldc.i4 21
  beq a01021
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01021:
  ldsflda int64 [rvastatic3]A::a01022
  ldind.i8
  ldc.i8 22
  beq a01022
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01022:
  ldsflda int32 [rvastatic3]A::a01023
  ldind.i4
  ldc.i4 23
  beq a01023
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01023:
  ldsflda int8 [rvastatic3]A::a01024
  ldind.i1
  ldc.i4 24
  beq a01024
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01024:
  ldsflda int8 [rvastatic3]A::a01025
  ldind.i1
  ldc.i4 25
  beq a01025
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01025:
  ldsflda int16 [rvastatic3]A::a01026
  ldind.i2
  ldc.i4 26
  beq a01026
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01026:
  ldsflda int8 [rvastatic3]A::a01027
  ldind.i1
  ldc.i4 27
  beq a01027
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01027:
  ldsflda int16 [rvastatic3]A::a01028
  ldind.i2
  ldc.i4 28
  beq a01028
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01028:
  ldsflda int64 [rvastatic3]A::a01029
  ldind.i8
  ldc.i8 29
  beq a01029
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01029:
  ldsflda int32 [rvastatic3]A::a01030
  ldind.i4
  ldc.i4 30
  beq a01030
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01030:
  ldsflda int32 [rvastatic3]A::a01031
  ldind.i4
  ldc.i4 31
  beq a01031
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01031:
  ldsflda int32 [rvastatic3]A::a01032
  ldind.i4
  ldc.i4 32
  beq a01032
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01032:
  ldsflda int8 [rvastatic3]A::a01033
  ldind.i1
  ldc.i4 33
  beq a01033
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01033:
  ldsflda int16 [rvastatic3]A::a01034
  ldind.i2
  ldc.i4 34
  beq a01034
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01034:
  ldsflda int32 [rvastatic3]A::a01035
  ldind.i4
  ldc.i4 35
  beq a01035
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01035:
  ldsflda int32 [rvastatic3]A::a01036
  ldind.i4
  ldc.i4 36
  beq a01036
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01036:
  ldsflda int16 [rvastatic3]A::a01037
  ldind.i2
  ldc.i4 37
  beq a01037
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01037:
  ldsflda float32 [rvastatic3]A::a01038
  ldind.r4
  ldc.r4 38.0
  beq a01038
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01038:
  ldsflda int8 [rvastatic3]A::a01039
  ldind.i1
  ldc.i4 39
  beq a01039
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01039:
  ldsflda int8 [rvastatic3]A::a01040
  ldind.i1
  ldc.i4 40
  beq a01040
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01040:
  ldsflda float32 [rvastatic3]A::a01041
  ldind.r4
  ldc.r4 41.0
  beq a01041
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01041:
  ldsflda int32 [rvastatic3]A::a01042
  ldind.i4
  ldc.i4 42
  beq a01042
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01042:
  ldsflda int32 [rvastatic3]A::a01043
  ldind.i4
  ldc.i4 43
  beq a01043
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01043:
  ldsflda int32 [rvastatic3]A::a01044
  ldind.i4
  ldc.i4 44
  beq a01044
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01044:
  ldsflda int64 [rvastatic3]A::a01045
  ldind.i8
  ldc.i8 45
  beq a01045
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01045:
  ldsflda int64 [rvastatic3]A::a01046
  ldind.i8
  ldc.i8 46
  beq a01046
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01046:
  ldsflda int64 [rvastatic3]A::a01047
  ldind.i8
  ldc.i8 47
  beq a01047
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01047:
  ldsflda float32 [rvastatic3]A::a01048
  ldind.r4
  ldc.r4 48.0
  beq a01048
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01048:
  ldsflda int64 [rvastatic3]A::a01049
  ldind.i8
  ldc.i8 49
  beq a01049
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01049:
  ldsflda int32 [rvastatic3]A::a01050
  ldind.i4
  ldc.i4 50
  beq a01050
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01050:
  ldsflda float32 [rvastatic3]A::a01051
  ldind.r4
  ldc.r4 51.0
  beq a01051
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01051:
  ldsflda int32 [rvastatic3]A::a01052
  ldind.i4
  ldc.i4 52
  beq a01052
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01052:
  ldsflda int64 [rvastatic3]A::a01053
  ldind.i8
  ldc.i8 53
  beq a01053
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01053:
  ldsflda int8 [rvastatic3]A::a01054
  ldind.i1
  ldc.i4 54
  beq a01054
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01054:
  ldsflda int8 [rvastatic3]A::a01055
  ldind.i1
  ldc.i4 55
  beq a01055
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01055:
  ldsflda float32 [rvastatic3]A::a01056
  ldind.r4
  ldc.r4 56.0
  beq a01056
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01056:
  ldsflda int32 [rvastatic3]A::a01057
  ldind.i4
  ldc.i4 57
  beq a01057
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01057:
  ldsflda int64 [rvastatic3]A::a01058
  ldind.i8
  ldc.i8 58
  beq a01058
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01058:
  ldsflda int64 [rvastatic3]A::a01059
  ldind.i8
  ldc.i8 59
  beq a01059
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01059:
  ldsflda int8 [rvastatic3]A::a01060
  ldind.i1
  ldc.i4 60
  beq a01060
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01060:
  ldsflda int16 [rvastatic3]A::a01061
  ldind.i2
  ldc.i4 61
  beq a01061
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01061:
  ldsflda int64 [rvastatic3]A::a01062
  ldind.i8
  ldc.i8 62
  beq a01062
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01062:
  ldsflda float32 [rvastatic3]A::a01063
  ldind.r4
  ldc.r4 63.0
  beq a01063
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01063:
  ldsflda int64 [rvastatic3]A::a01064
  ldind.i8
  ldc.i8 64
  beq a01064
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01064:
  ldsflda float32 [rvastatic3]A::a01065
  ldind.r4
  ldc.r4 65.0
  beq a01065
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01065:
  ldsflda int8 [rvastatic3]A::a01066
  ldind.i1
  ldc.i4 66
  beq a01066
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01066:
  ldsflda int16 [rvastatic3]A::a01067
  ldind.i2
  ldc.i4 67
  beq a01067
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01067:
  ldsflda int64 [rvastatic3]A::a01068
  ldind.i8
  ldc.i8 68
  beq a01068
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01068:
  ldsflda int64 [rvastatic3]A::a01069
  ldind.i8
  ldc.i8 69
  beq a01069
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01069:
  ldsflda int64 [rvastatic3]A::a01070
  ldind.i8
  ldc.i8 70
  beq a01070
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01070:
  ldsflda int32 [rvastatic3]A::a01071
  ldind.i4
  ldc.i4 71
  beq a01071
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01071:
  ldsflda int8 [rvastatic3]A::a01072
  ldind.i1
  ldc.i4 72
  beq a01072
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01072:
  ldsflda int32 [rvastatic3]A::a01073
  ldind.i4
  ldc.i4 73
  beq a01073
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01073:
  ldsflda float32 [rvastatic3]A::a01074
  ldind.r4
  ldc.r4 74.0
  beq a01074
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01074:
  ldsflda int64 [rvastatic3]A::a01075
  ldind.i8
  ldc.i8 75
  beq a01075
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01075:
  ldsflda int8 [rvastatic3]A::a01076
  ldind.i1
  ldc.i4 76
  beq a01076
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01076:
  ldsflda int8 [rvastatic3]A::a01077
  ldind.i1
  ldc.i4 77
  beq a01077
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01077:
  ldsflda int8 [rvastatic3]A::a01078
  ldind.i1
  ldc.i4 78
  beq a01078
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01078:
  ldsflda int32 [rvastatic3]A::a01079
  ldind.i4
  ldc.i4 79
  beq a01079
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01079:
  ldsflda int16 [rvastatic3]A::a01080
  ldind.i2
  ldc.i4 80
  beq a01080
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01080:
  ldsflda int32 [rvastatic3]A::a01081
  ldind.i4
  ldc.i4 81
  beq a01081
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01081:
  ldsflda int8 [rvastatic3]A::a01082
  ldind.i1
  ldc.i4 82
  beq a01082
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01082:
  ldsflda int32 [rvastatic3]A::a01083
  ldind.i4
  ldc.i4 83
  beq a01083
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01083:
  ldsflda int8 [rvastatic3]A::a01084
  ldind.i1
  ldc.i4 84
  beq a01084
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01084:
  ldsflda int32 [rvastatic3]A::a01085
  ldind.i4
  ldc.i4 85
  beq a01085
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01085:
  ldsflda int8 [rvastatic3]A::a01086
  ldind.i1
  ldc.i4 86
  beq a01086
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01086:
  ldsflda int64 [rvastatic3]A::a01087
  ldind.i8
  ldc.i8 87
  beq a01087
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01087:
  ldsflda int8 [rvastatic3]A::a01088
  ldind.i1
  ldc.i4 88
  beq a01088
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01088:
  ldsflda int16 [rvastatic3]A::a01089
  ldind.i2
  ldc.i4 89
  beq a01089
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01089:
  ldsflda int64 [rvastatic3]A::a01090
  ldind.i8
  ldc.i8 90
  beq a01090
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01090:
  ldsflda int8 [rvastatic3]A::a01091
  ldind.i1
  ldc.i4 91
  beq a01091
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01091:
  ldsflda int64 [rvastatic3]A::a01092
  ldind.i8
  ldc.i8 92
  beq a01092
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01092:
  ldsflda int16 [rvastatic3]A::a01093
  ldind.i2
  ldc.i4 93
  beq a01093
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01093:
  ldsflda int8 [rvastatic3]A::a01094
  ldind.i1
  ldc.i4 94
  beq a01094
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01094:
  ldsflda float32 [rvastatic3]A::a01095
  ldind.r4
  ldc.r4 95.0
  beq a01095
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01095:
  ldsflda int16 [rvastatic3]A::a01096
  ldind.i2
  ldc.i4 96
  beq a01096
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01096:
  ldsflda int64 [rvastatic3]A::a01097
  ldind.i8
  ldc.i8 97
  beq a01097
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01097:
  ldsflda float32 [rvastatic3]A::a01098
  ldind.r4
  ldc.r4 98.0
  beq a01098
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01098:
  ldsflda int32 [rvastatic3]A::a01099
  ldind.i4
  ldc.i4 99
  beq a01099
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01099:
  ldsflda int32 [rvastatic3]A::a010100
  ldind.i4
  ldc.i4 100
  beq a010100
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010100:
  ldsflda int32 [rvastatic3]A::a010101
  ldind.i4
  ldc.i4 101
  beq a010101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010101:
  ldsflda int8 [rvastatic3]A::a010102
  ldind.i1
  ldc.i4 102
  beq a010102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010102:
  ldsflda int16 [rvastatic3]A::a010103
  ldind.i2
  ldc.i4 103
  beq a010103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010103:
  ldsflda int8 [rvastatic3]A::a010104
  ldind.i1
  ldc.i4 104
  beq a010104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010104:
  ldsflda float32 [rvastatic3]A::a010105
  ldind.r4
  ldc.r4 105.0
  beq a010105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010105:
  ldsflda float32 [rvastatic3]A::a010106
  ldind.r4
  ldc.r4 106.0
  beq a010106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010106:
  ldsflda int8 [rvastatic3]A::a010107
  ldind.i1
  ldc.i4 107
  beq a010107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010107:
  ldsflda float32 [rvastatic3]A::a010108
  ldind.r4
  ldc.r4 108.0
  beq a010108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010108:
  ldsflda int16 [rvastatic3]A::a010109
  ldind.i2
  ldc.i4 109
  beq a010109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010109:
  ldsflda int8 [rvastatic3]A::a010110
  ldind.i1
  ldc.i4 110
  beq a010110
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010110:
  ldsflda int64 [rvastatic3]A::a010111
  ldind.i8
  ldc.i8 111
  beq a010111
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010111:
  ldsflda int16 [rvastatic3]A::a010112
  ldind.i2
  ldc.i4 112
  beq a010112
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010112:
  ldsflda int16 [rvastatic3]A::a010113
  ldind.i2
  ldc.i4 113
  beq a010113
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010113:
  ldsflda int32 [rvastatic3]A::a010114
  ldind.i4
  ldc.i4 114
  beq a010114
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010114:
  ldsflda int32 [rvastatic3]A::a010115
  ldind.i4
  ldc.i4 115
  beq a010115
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010115:
  ldsflda int8 [rvastatic3]A::a010116
  ldind.i1
  ldc.i4 116
  beq a010116
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010116:
  ldsflda int16 [rvastatic3]A::a010117
  ldind.i2
  ldc.i4 117
  beq a010117
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010117:
  ldsflda int64 [rvastatic3]A::a010118
  ldind.i8
  ldc.i8 118
  beq a010118
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010118:
  ldsflda int8 [rvastatic3]A::a010119
  ldind.i1
  ldc.i4 119
  beq a010119
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010119:
  ldsflda int64 [rvastatic3]A::a010120
  ldind.i8
  ldc.i8 120
  beq a010120
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010120:
  ldsflda int32 [rvastatic3]A::a010121
  ldind.i4
  ldc.i4 121
  beq a010121
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010121:
  ldsflda int8 [rvastatic3]A::a010122
  ldind.i1
  ldc.i4 2
  beq a010122
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010122:
  ldsflda int32 [rvastatic3]A::a010123
  ldind.i4
  ldc.i4 123
  beq a010123
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010123:
  ldsflda int8 [rvastatic3]A::a010124
  ldind.i1
  ldc.i4 4
  beq a010124
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010124:
  ldsflda int64 [rvastatic3]A::a010125
  ldind.i8
  ldc.i8 125
  beq a010125
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010125:
  ldsflda float32 [rvastatic3]A::a010126
  ldind.r4
  ldc.r4 126.0
  beq a010126
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010126:
  ldsflda int64 [rvastatic3]A::a010127
  ldind.i8
  ldc.i8 127
  beq a010127
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010127:
 ret}
 .method static void V3() {.maxstack  50
  ldsfld int32 [rvastatic3]A::a01044
  ldc.i4 44
  beq a010129
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010129:
  ldsfld float32 [rvastatic3]A::a01056
  ldc.r4 56.0
  beq a010130
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010130:
  ldsfld int16 [rvastatic3]A::a01028
  ldc.i4 28
  beq a010131
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010131:
  ldsfld int8 [rvastatic3]A::a01066
  ldc.i4 66
  beq a010132
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010132:
  ldsfld int64 [rvastatic3]A::a010125
  ldc.i8 125
  beq a010133
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010133:
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 109
  beq a010134
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010134:
  ldsfld int64 [rvastatic3]A::a01058
  ldc.i8 58
  beq a010135
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010135:
  ldsfld int8 [rvastatic3]A::a010107
  ldc.i4 107
  beq a010136
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010136:
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 47
  beq a010137
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010137:
  ldsfld int32 [rvastatic3]A::a01030
  ldc.i4 30
  beq a010138
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010138:
  ldsfld int16 [rvastatic3]A::a01067
  ldc.i4 67
  beq a010139
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010139:
  ldsfld int32 [rvastatic3]A::a01012
  ldc.i4 12
  beq a010140
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010140:
  ldsfld int32 [rvastatic3]A::a010100
  ldc.i4 100
  beq a010141
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010141:
  ldsfld int32 [rvastatic3]A::a010100
  ldc.i4 100
  beq a010142
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010142:
  ldsfld int8 [rvastatic3]A::a01019
  ldc.i4 19
  beq a010143
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010143:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 103
  beq a010144
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010144:
  ldsfld int16 [rvastatic3]A::a01093
  ldc.i4 93
  beq a010145
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010145:
  ldsfld int64 [rvastatic3]A::a01058
  ldc.i8 58
  beq a010146
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010146:
  ldsfld float32 [rvastatic3]A::a010106
  ldc.r4 106.0
  beq a010147
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010147:
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 61
  beq a010148
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010148:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 103
  beq a010149
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010149:
  ldsfld float32 [rvastatic3]A::a0102
  ldc.r4 2.0
  beq a010150
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010150:
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 109
  beq a010151
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010151:
  ldsfld float32 [rvastatic3]A::a010105
  ldc.r4 105.0
  beq a010152
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010152:
  ldsfld int64 [rvastatic3]A::a01097
  ldc.i8 97
  beq a010153
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010153:
  ldsfld int32 [rvastatic3]A::a010114
  ldc.i4 114
  beq a010154
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010154:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 43
  beq a010155
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010155:
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 73
  beq a010156
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010156:
  ldsfld float32 [rvastatic3]A::a0102
  ldc.r4 2.0
  beq a010157
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010157:
  ldsfld int64 [rvastatic3]A::a01046
  ldc.i8 46
  beq a010158
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010158:
  ldsfld int16 [rvastatic3]A::a01015
  ldc.i4 15
  beq a010159
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010159:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 23
  beq a010160
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010160:
  ldsfld int64 [rvastatic3]A::a01068
  ldc.i8 68
  beq a010161
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010161:
  ldsfld int64 [rvastatic3]A::a010120
  ldc.i8 120
  beq a010162
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010162:
  ldsfld int16 [rvastatic3]A::a0106
  ldc.i4 6
  beq a010163
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010163:
  ldsfld int32 [rvastatic3]A::a01079
  ldc.i4 79
  beq a010164
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010164:
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 73
  beq a010165
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010165:
  ldsfld int8 [rvastatic3]A::a010104
  ldc.i4 104
  beq a010166
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010166:
  ldsfld int8 [rvastatic3]A::a01033
  ldc.i4 33
  beq a010167
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010167:
  ldsfld int32 [rvastatic3]A::a01071
  ldc.i4 71
  beq a010168
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010168:
  ldsfld int32 [rvastatic3]A::a0108
  ldc.i4 8
  beq a010169
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010169:
  ldsfld float32 [rvastatic3]A::a01041
  ldc.r4 41.0
  beq a010170
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010170:
  ldsfld int16 [rvastatic3]A::a010112
  ldc.i4 112
  beq a010171
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010171:
  ldsfld float32 [rvastatic3]A::a01065
  ldc.r4 65.0
  beq a010172
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010172:
  ldsfld int8 [rvastatic3]A::a01078
  ldc.i4 78
  beq a010173
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010173:
  ldsfld int16 [rvastatic3]A::a01089
  ldc.i4 89
  beq a010174
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010174:
  ldsfld float32 [rvastatic3]A::a01095
  ldc.r4 95.0
  beq a010175
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010175:
  ldsfld int8 [rvastatic3]A::a01039
  ldc.i4 39
  beq a010176
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010176:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 43
  beq a010177
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010177:
  ldsfld float32 [rvastatic3]A::a01048
  ldc.r4 48.0
  beq a010178
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010178:
  ldsfld int8 [rvastatic3]A::a010122
  ldc.i4 2
  beq a010179
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010179:
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 47
  beq a010180
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010180:
  ldsfld int8 [rvastatic3]A::a010119
  ldc.i4 119
  beq a010181
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010181:
  ldsfld float32 [rvastatic3]A::a010126
  ldc.r4 126.0
  beq a010182
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010182:
  ldsfld float32 [rvastatic3]A::a01017
  ldc.r4 17.0
  beq a010183
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010183:
  ldsfld float32 [rvastatic3]A::a01041
  ldc.r4 41.0
  beq a010184
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010184:
  ldsfld int64 [rvastatic3]A::a01075
  ldc.i8 75
  beq a010185
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010185:
  ldsfld int32 [rvastatic3]A::a01050
  ldc.i4 50
  beq a010186
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010186:
  ldsfld int16 [rvastatic3]A::a01096
  ldc.i4 96
  beq a010187
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010187:
  ldsfld int32 [rvastatic3]A::a01010
  ldc.i4 10
  beq a010188
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010188:
  ldsfld int8 [rvastatic3]A::a01066
  ldc.i4 66
  beq a010189
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010189:
  ldsfld int16 [rvastatic3]A::a010113
  ldc.i4 113
  beq a010190
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010190:
  ldsfld int8 [rvastatic3]A::a010104
  ldc.i4 104
  beq a010191
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010191:
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 1
  beq a010192
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010192:
  ldsfld int16 [rvastatic3]A::a01015
  ldc.i4 15
  beq a010193
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010193:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 43
  beq a010194
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010194:
  ldsfld int32 [rvastatic3]A::a01071
  ldc.i4 71
  beq a010195
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010195:
  ldsfld int8 [rvastatic3]A::a010107
  ldc.i4 107
  beq a010196
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010196:
  ldsfld int8 [rvastatic3]A::a01094
  ldc.i4 94
  beq a010197
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010197:
  ldsfld int64 [rvastatic3]A::a01068
  ldc.i8 68
  beq a010198
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010198:
  ldsfld int64 [rvastatic3]A::a01029
  ldc.i8 29
  beq a010199
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010199:
  ldsfld int8 [rvastatic3]A::a010116
  ldc.i4 116
  beq a010200
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010200:
  ldsfld int8 [rvastatic3]A::a01025
  ldc.i4 25
  beq a010201
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010201:
  ldsfld int16 [rvastatic3]A::a01034
  ldc.i4 34
  beq a010202
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010202:
  ldsfld int32 [rvastatic3]A::a01052
  ldc.i4 52
  beq a010203
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010203:
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 61
  beq a010204
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010204:
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 18
  beq a010205
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010205:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 43
  beq a010206
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010206:
  ldsfld int16 [rvastatic3]A::a01026
  ldc.i4 26
  beq a010207
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010207:
  ldsfld float32 [rvastatic3]A::a0107
  ldc.r4 7.0
  beq a010208
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010208:
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 18
  beq a010209
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010209:
  ldsfld int64 [rvastatic3]A::a0104
  ldc.i8 4
  beq a010210
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010210:
  ldsfld int64 [rvastatic3]A::a01029
  ldc.i8 29
  beq a010211
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010211:
  ldsfld int8 [rvastatic3]A::a01082
  ldc.i4 82
  beq a010212
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010212:
  ldsfld int32 [rvastatic3]A::a01052
  ldc.i4 52
  beq a010213
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010213:
  ldsfld float32 [rvastatic3]A::a01095
  ldc.r4 95.0
  beq a010214
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010214:
  ldsfld int32 [rvastatic3]A::a01032
  ldc.i4 32
  beq a010215
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010215:
  ldsfld int64 [rvastatic3]A::a01069
  ldc.i8 69
  beq a010216
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010216:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 103
  beq a010217
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010217:
  ldsfld int64 [rvastatic3]A::a01092
  ldc.i8 92
  beq a010218
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010218:
  ldsfld int64 [rvastatic3]A::a0104
  ldc.i8 4
  beq a010219
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010219:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 22
  beq a010220
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010220:
  ldsfld float32 [rvastatic3]A::a0107
  ldc.r4 7.0
  beq a010221
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010221:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 22
  beq a010222
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010222:
  ldsfld int16 [rvastatic3]A::a0105
  ldc.i4 5
  beq a010223
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010223:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 23
  beq a010224
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010224:
  ldsfld int64 [rvastatic3]A::a01064
  ldc.i8 64
  beq a010225
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010225:
  ldsfld int8 [rvastatic3]A::a010110
  ldc.i4 110
  beq a010226
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010226:
  ldsfld int32 [rvastatic3]A::a01050
  ldc.i4 50
  beq a010227
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010227:
  ldsfld int32 [rvastatic3]A::a01035
  ldc.i4 35
  beq a010228
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010228:
  ldsfld float32 [rvastatic3]A::a01017
  ldc.r4 17.0
  beq a010229
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010229:
  ldsfld int8 [rvastatic3]A::a010116
  ldc.i4 116
  beq a010230
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010230:
  ldsfld int8 [rvastatic3]A::a01072
  ldc.i4 72
  beq a010231
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010231:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 103
  beq a010232
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010232:
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 18
  beq a010233
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010233:
  ldsfld int16 [rvastatic3]A::a0105
  ldc.i4 5
  beq a010234
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010234:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 22
  beq a010235
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010235:
  ldsfld int8 [rvastatic3]A::a01084
  ldc.i4 84
  beq a010236
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010236:
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 73
  beq a010237
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010237:
  ldsfld int32 [rvastatic3]A::a01083
  ldc.i4 83
  beq a010238
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010238:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 23
  beq a010239
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010239:
  ldsfld int32 [rvastatic3]A::a01031
  ldc.i4 31
  beq a010240
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010240:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 23
  beq a010241
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010241:
  ldsfld int16 [rvastatic3]A::a01089
  ldc.i4 89
  beq a010242
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010242:
  ldsfld int64 [rvastatic3]A::a010120
  ldc.i8 120
  beq a010243
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010243:
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 61
  beq a010244
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010244:
  ldsfld int32 [rvastatic3]A::a01083
  ldc.i4 83
  beq a010245
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010245:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 22
  beq a010246
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010246:
  ldsfld int8 [rvastatic3]A::a01076
  ldc.i4 76
  beq a010247
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010247:
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 47
  beq a010248
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010248:
  ldsfld int32 [rvastatic3]A::a01010
  ldc.i4 10
  beq a010249
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010249:
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 1
  beq a010250
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010250:
  ldsfld int64 [rvastatic3]A::a0103
  ldc.i8 3
  beq a010251
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010251:
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 1
  beq a010252
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010252:
  ldsfld int64 [rvastatic3]A::a01064
  ldc.i8 64
  beq a010253
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010253:
  ldsfld int16 [rvastatic3]A::a0106
  ldc.i4 6
  beq a010254
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010254:
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 109
  beq a010255
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010255:
  ldsfld int64 [rvastatic3]A::a0103
  ldc.i8 3
  beq a010256
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010256:
 ret}
 .method static void V4() {.maxstack  50
  ldsflda int64 [rvastatic3]A::a0100
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 0
  beq a0100
  ldstr "a0100"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0100:
  ldsflda int64 [rvastatic3]A::a0101
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i8
  ldc.i8 1
  beq a0101
  ldstr "a0101"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0101:
  ldsflda float32 [rvastatic3]A::a0102
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.r4
  ldc.r4 2.0
  beq a0102
  ldstr "a0102"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0102:
  ldsflda int64 [rvastatic3]A::a0103
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 3
  beq a0103
  ldstr "a0103"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0103:
  ldsflda int64 [rvastatic3]A::a0104
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i8
  ldc.i8 4
  beq a0104
  ldstr "a0104"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0104:
  ldsflda int16 [rvastatic3]A::a0105
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i2
  ldc.i4 5
  beq a0105
  ldstr "a0105"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0105:
  ldsflda int16 [rvastatic3]A::a0106
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i2
  ldc.i4 6
  beq a0106
  ldstr "a0106"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0106:
  ldsflda float32 [rvastatic3]A::a0107
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.r4
  ldc.r4 7.0
  beq a0107
  ldstr "a0107"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0107:
  ldsflda int32 [rvastatic3]A::a0108
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 8
  beq a0108
  ldstr "a0108"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0108:
  ldsflda int8 [rvastatic3]A::a0109
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i1
  ldc.i4 9
  beq a0109
  ldstr "a0109"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0109:
  ldsflda int32 [rvastatic3]A::a01010
  conv.i8
  ldc.i8 40202
  add
  conv.i8
  ldc.i8 40202
  sub
  conv.i
  ldind.i4
  ldc.i4 10
  beq a01010
  ldstr "a01010"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01010:
  ldsflda int64 [rvastatic3]A::a01011
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i8
  ldc.i8 11
  beq a01011
  ldstr "a01011"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01011:
  ldsflda int32 [rvastatic3]A::a01012
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i4
  ldc.i4 12
  beq a01012
  ldstr "a01012"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01012:
  ldsflda int8 [rvastatic3]A::a01013
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i1
  ldc.i4 13
  beq a01013
  ldstr "a01013"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01013:
  ldsflda int16 [rvastatic3]A::a01014
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i2
  ldc.i4 14
  beq a01014
  ldstr "a01014"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01014:
  ldsflda int16 [rvastatic3]A::a01015
  conv.i8
  ldc.i8 37800
  add
  conv.i8
  ldc.i8 37800
  sub
  conv.i
  ldind.i2
  ldc.i4 15
  beq a01015
  ldstr "a01015"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01015:
  ldsflda float32 [rvastatic3]A::a01016
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.r4
  ldc.r4 16.0
  beq a01016
  ldstr "a01016"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01016:
  ldsflda float32 [rvastatic3]A::a01017
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.r4
  ldc.r4 17.0
  beq a01017
  ldstr "a01017"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01017:
  ldsflda int32 [rvastatic3]A::a01018
  conv.i8
  ldc.i8 2058
  add
  conv.i8
  ldc.i8 2058
  sub
  conv.i
  ldind.i4
  ldc.i4 18
  beq a01018
  ldstr "a01018"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01018:
  ldsflda int8 [rvastatic3]A::a01019
  conv.i8
  ldc.i8 8916
  add
  conv.i8
  ldc.i8 8916
  sub
  conv.i
  ldind.i1
  ldc.i4 19
  beq a01019
  ldstr "a01019"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01019:
  ldsflda int32 [rvastatic3]A::a01020
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i4
  ldc.i4 20
  beq a01020
  ldstr "a01020"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01020:
  ldsflda int32 [rvastatic3]A::a01021
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i4
  ldc.i4 21
  beq a01021
  ldstr "a01021"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01021:
  ldsflda int64 [rvastatic3]A::a01022
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 22
  beq a01022
  ldstr "a01022"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01022:
  ldsflda int32 [rvastatic3]A::a01023
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i4
  ldc.i4 23
  beq a01023
  ldstr "a01023"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01023:
  ldsflda int8 [rvastatic3]A::a01024
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i1
  ldc.i4 24
  beq a01024
  ldstr "a01024"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01024:
  ldsflda int8 [rvastatic3]A::a01025
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 25
  beq a01025
  ldstr "a01025"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01025:
  ldsflda int16 [rvastatic3]A::a01026
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i2
  ldc.i4 26
  beq a01026
  ldstr "a01026"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01026:
  ldsflda int8 [rvastatic3]A::a01027
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i1
  ldc.i4 27
  beq a01027
  ldstr "a01027"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01027:
  ldsflda int16 [rvastatic3]A::a01028
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i2
  ldc.i4 28
  beq a01028
  ldstr "a01028"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01028:
  ldsflda int64 [rvastatic3]A::a01029
  conv.i8
  ldc.i8 877
  add
  conv.i8
  ldc.i8 877
  sub
  conv.i
  ldind.i8
  ldc.i8 29
  beq a01029
  ldstr "a01029"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01029:
  ldsflda int32 [rvastatic3]A::a01030
  conv.i8
  ldc.i8 47449
  add
  conv.i8
  ldc.i8 47449
  sub
  conv.i
  ldind.i4
  ldc.i4 30
  beq a01030
  ldstr "a01030"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01030:
  ldsflda int32 [rvastatic3]A::a01031
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i4
  ldc.i4 31
  beq a01031
  ldstr "a01031"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01031:
  ldsflda int32 [rvastatic3]A::a01032
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i4
  ldc.i4 32
  beq a01032
  ldstr "a01032"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01032:
  ldsflda int8 [rvastatic3]A::a01033
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i1
  ldc.i4 33
  beq a01033
  ldstr "a01033"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01033:
  ldsflda int16 [rvastatic3]A::a01034
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i2
  ldc.i4 34
  beq a01034
  ldstr "a01034"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01034:
  ldsflda int32 [rvastatic3]A::a01035
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i4
  ldc.i4 35
  beq a01035
  ldstr "a01035"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01035:
  ldsflda int32 [rvastatic3]A::a01036
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 36
  beq a01036
  ldstr "a01036"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01036:
  ldsflda int16 [rvastatic3]A::a01037
  conv.i8
  ldc.i8 25670
  add
  conv.i8
  ldc.i8 25670
  sub
  conv.i
  ldind.i2
  ldc.i4 37
  beq a01037
  ldstr "a01037"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01037:
  ldsflda float32 [rvastatic3]A::a01038
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.r4
  ldc.r4 38.0
  beq a01038
  ldstr "a01038"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01038:
  ldsflda int8 [rvastatic3]A::a01039
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i1
  ldc.i4 39
  beq a01039
  ldstr "a01039"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01039:
  ldsflda int8 [rvastatic3]A::a01040
  conv.i8
  ldc.i8 63628
  add
  conv.i8
  ldc.i8 63628
  sub
  conv.i
  ldind.i1
  ldc.i4 40
  beq a01040
  ldstr "a01040"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01040:
  ldsflda float32 [rvastatic3]A::a01041
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.r4
  ldc.r4 41.0
  beq a01041
  ldstr "a01041"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01041:
  ldsflda int32 [rvastatic3]A::a01042
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 42
  beq a01042
  ldstr "a01042"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01042:
  ldsflda int32 [rvastatic3]A::a01043
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 43
  beq a01043
  ldstr "a01043"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01043:
  ldsflda int32 [rvastatic3]A::a01044
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 44
  beq a01044
  ldstr "a01044"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01044:
  ldsflda int64 [rvastatic3]A::a01045
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i8
  ldc.i8 45
  beq a01045
  ldstr "a01045"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01045:
  ldsflda int64 [rvastatic3]A::a01046
  conv.i8
  ldc.i8 53032
  add
  conv.i8
  ldc.i8 53032
  sub
  conv.i
  ldind.i8
  ldc.i8 46
  beq a01046
  ldstr "a01046"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01046:
  ldsflda int64 [rvastatic3]A::a01047
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i8
  ldc.i8 47
  beq a01047
  ldstr "a01047"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01047:
  ldsflda float32 [rvastatic3]A::a01048
  conv.i8
  ldc.i8 48333
  add
  conv.i8
  ldc.i8 48333
  sub
  conv.i
  ldind.r4
  ldc.r4 48.0
  beq a01048
  ldstr "a01048"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01048:
  ldsflda int64 [rvastatic3]A::a01049
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i8
  ldc.i8 49
  beq a01049
  ldstr "a01049"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01049:
  ldsflda int32 [rvastatic3]A::a01050
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i4
  ldc.i4 50
  beq a01050
  ldstr "a01050"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01050:
  ldsflda float32 [rvastatic3]A::a01051
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.r4
  ldc.r4 51.0
  beq a01051
  ldstr "a01051"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01051:
  ldsflda int32 [rvastatic3]A::a01052
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 52
  beq a01052
  ldstr "a01052"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01052:
  ldsflda int64 [rvastatic3]A::a01053
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i8
  ldc.i8 53
  beq a01053
  ldstr "a01053"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01053:
  ldsflda int8 [rvastatic3]A::a01054
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i1
  ldc.i4 54
  beq a01054
  ldstr "a01054"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01054:
  ldsflda int8 [rvastatic3]A::a01055
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i1
  ldc.i4 55
  beq a01055
  ldstr "a01055"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01055:
  ldsflda float32 [rvastatic3]A::a01056
  conv.i8
  ldc.i8 4395
  add
  conv.i8
  ldc.i8 4395
  sub
  conv.i
  ldind.r4
  ldc.r4 56.0
  beq a01056
  ldstr "a01056"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01056:
  ldsflda int32 [rvastatic3]A::a01057
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i4
  ldc.i4 57
  beq a01057
  ldstr "a01057"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01057:
  ldsflda int64 [rvastatic3]A::a01058
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i8
  ldc.i8 58
  beq a01058
  ldstr "a01058"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01058:
  ldsflda int64 [rvastatic3]A::a01059
  conv.i8
  ldc.i8 18075
  add
  conv.i8
  ldc.i8 18075
  sub
  conv.i
  ldind.i8
  ldc.i8 59
  beq a01059
  ldstr "a01059"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01059:
  ldsflda int8 [rvastatic3]A::a01060
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i1
  ldc.i4 60
  beq a01060
  ldstr "a01060"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01060:
  ldsflda int16 [rvastatic3]A::a01061
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i2
  ldc.i4 61
  beq a01061
  ldstr "a01061"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01061:
  ldsflda int64 [rvastatic3]A::a01062
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i8
  ldc.i8 62
  beq a01062
  ldstr "a01062"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01062:
  ldsflda float32 [rvastatic3]A::a01063
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.r4
  ldc.r4 63.0
  beq a01063
  ldstr "a01063"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01063:
  ldsflda int64 [rvastatic3]A::a01064
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 64
  beq a01064
  ldstr "a01064"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01064:
  ldsflda float32 [rvastatic3]A::a01065
  conv.i8
  ldc.i8 46752
  add
  conv.i8
  ldc.i8 46752
  sub
  conv.i
  ldind.r4
  ldc.r4 65.0
  beq a01065
  ldstr "a01065"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01065:
  ldsflda int8 [rvastatic3]A::a01066
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 66
  beq a01066
  ldstr "a01066"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01066:
  ldsflda int16 [rvastatic3]A::a01067
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i2
  ldc.i4 67
  beq a01067
  ldstr "a01067"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01067:
  ldsflda int64 [rvastatic3]A::a01068
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i8
  ldc.i8 68
  beq a01068
  ldstr "a01068"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01068:
  ldsflda int64 [rvastatic3]A::a01069
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i8
  ldc.i8 69
  beq a01069
  ldstr "a01069"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01069:
  ldsflda int64 [rvastatic3]A::a01070
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 70
  beq a01070
  ldstr "a01070"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01070:
  ldsflda int32 [rvastatic3]A::a01071
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i4
  ldc.i4 71
  beq a01071
  ldstr "a01071"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01071:
  ldsflda int8 [rvastatic3]A::a01072
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i1
  ldc.i4 72
  beq a01072
  ldstr "a01072"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01072:
  ldsflda int32 [rvastatic3]A::a01073
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i4
  ldc.i4 73
  beq a01073
  ldstr "a01073"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01073:
  ldsflda float32 [rvastatic3]A::a01074
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.r4
  ldc.r4 74.0
  beq a01074
  ldstr "a01074"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01074:
  ldsflda int64 [rvastatic3]A::a01075
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i8
  ldc.i8 75
  beq a01075
  ldstr "a01075"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01075:
  ldsflda int8 [rvastatic3]A::a01076
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i1
  ldc.i4 76
  beq a01076
  ldstr "a01076"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01076:
  ldsflda int8 [rvastatic3]A::a01077
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i1
  ldc.i4 77
  beq a01077
  ldstr "a01077"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01077:
  ldsflda int8 [rvastatic3]A::a01078
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 78
  beq a01078
  ldstr "a01078"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01078:
  ldsflda int32 [rvastatic3]A::a01079
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i4
  ldc.i4 79
  beq a01079
  ldstr "a01079"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01079:
  ldsflda int16 [rvastatic3]A::a01080
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i2
  ldc.i4 80
  beq a01080
  ldstr "a01080"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01080:
  ldsflda int32 [rvastatic3]A::a01081
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i4
  ldc.i4 81
  beq a01081
  ldstr "a01081"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01081:
  ldsflda int8 [rvastatic3]A::a01082
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i1
  ldc.i4 82
  beq a01082
  ldstr "a01082"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01082:
  ldsflda int32 [rvastatic3]A::a01083
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 83
  beq a01083
  ldstr "a01083"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01083:
  ldsflda int8 [rvastatic3]A::a01084
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i1
  ldc.i4 84
  beq a01084
  ldstr "a01084"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01084:
  ldsflda int32 [rvastatic3]A::a01085
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 85
  beq a01085
  ldstr "a01085"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01085:
  ldsflda int8 [rvastatic3]A::a01086
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 86
  beq a01086
  ldstr "a01086"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01086:
  ldsflda int64 [rvastatic3]A::a01087
  conv.i8
  ldc.i8 13996
  add
  conv.i8
  ldc.i8 13996
  sub
  conv.i
  ldind.i8
  ldc.i8 87
  beq a01087
  ldstr "a01087"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01087:
  ldsflda int8 [rvastatic3]A::a01088
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i1
  ldc.i4 88
  beq a01088
  ldstr "a01088"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01088:
  ldsflda int16 [rvastatic3]A::a01089
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i2
  ldc.i4 89
  beq a01089
  ldstr "a01089"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01089:
  ldsflda int64 [rvastatic3]A::a01090
  conv.i8
  ldc.i8 38561
  add
  conv.i8
  ldc.i8 38561
  sub
  conv.i
  ldind.i8
  ldc.i8 90
  beq a01090
  ldstr "a01090"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01090:
  ldsflda int8 [rvastatic3]A::a01091
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.i1
  ldc.i4 91
  beq a01091
  ldstr "a01091"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01091:
  ldsflda int64 [rvastatic3]A::a01092
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i8
  ldc.i8 92
  beq a01092
  ldstr "a01092"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01092:
  ldsflda int16 [rvastatic3]A::a01093
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i2
  ldc.i4 93
  beq a01093
  ldstr "a01093"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01093:
  ldsflda int8 [rvastatic3]A::a01094
  conv.i8
  ldc.i8 16915
  add
  conv.i8
  ldc.i8 16915
  sub
  conv.i
  ldind.i1
  ldc.i4 94
  beq a01094
  ldstr "a01094"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01094:
  ldsflda float32 [rvastatic3]A::a01095
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.r4
  ldc.r4 95.0
  beq a01095
  ldstr "a01095"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01095:
  ldsflda int16 [rvastatic3]A::a01096
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i2
  ldc.i4 96
  beq a01096
  ldstr "a01096"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01096:
  ldsflda int64 [rvastatic3]A::a01097
  conv.i8
  ldc.i8 53815
  add
  conv.i8
  ldc.i8 53815
  sub
  conv.i
  ldind.i8
  ldc.i8 97
  beq a01097
  ldstr "a01097"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01097:
  ldsflda float32 [rvastatic3]A::a01098
  conv.i8
  dup
  ldc.i8 0xffffffff00000000
  and
  ldc.i4 32
  shr.un
  conv.i8
  ldc.i4 32
  shl
  or
  conv.i
  ldind.r4
  ldc.r4 98.0
  beq a01098
  ldstr "a01098"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01098:
  ldsflda int32 [rvastatic3]A::a01099
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 99
  beq a01099
  ldstr "a01099"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01099:
  ldsflda int32 [rvastatic3]A::a010100
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i4
  ldc.i4 100
  beq a010100
  ldstr "a010100"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010100:
  ldsflda int32 [rvastatic3]A::a010101
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i4
  ldc.i4 101
  beq a010101
  ldstr "a010101"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010101:
  ldsflda int8 [rvastatic3]A::a010102
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i1
  ldc.i4 102
  beq a010102
  ldstr "a010102"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010102:
  ldsflda int16 [rvastatic3]A::a010103
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i2
  ldc.i4 103
  beq a010103
  ldstr "a010103"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010103:
  ldsflda int8 [rvastatic3]A::a010104
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i1
  ldc.i4 104
  beq a010104
  ldstr "a010104"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010104:
  ldsflda float32 [rvastatic3]A::a010105
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.r4
  ldc.r4 105.0
  beq a010105
  ldstr "a010105"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010105:
  ldsflda float32 [rvastatic3]A::a010106
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.r4
  ldc.r4 106.0
  beq a010106
  ldstr "a010106"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010106:
  ldsflda int8 [rvastatic3]A::a010107
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i1
  ldc.i4 107
  beq a010107
  ldstr "a010107"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010107:
  ldsflda float32 [rvastatic3]A::a010108
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.r4
  ldc.r4 108.0
  beq a010108
  ldstr "a010108"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010108:
  ldsflda int16 [rvastatic3]A::a010109
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i2
  ldc.i4 109
  beq a010109
  ldstr "a010109"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010109:
  ldsflda int8 [rvastatic3]A::a010110
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i1
  ldc.i4 110
  beq a010110
  ldstr "a010110"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010110:
  ldsflda int64 [rvastatic3]A::a010111
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i8
  ldc.i8 111
  beq a010111
  ldstr "a010111"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010111:
  ldsflda int16 [rvastatic3]A::a010112
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i2
  ldc.i4 112
  beq a010112
  ldstr "a010112"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010112:
  ldsflda int16 [rvastatic3]A::a010113
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i2
  ldc.i4 113
  beq a010113
  ldstr "a010113"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010113:
  ldsflda int32 [rvastatic3]A::a010114
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i4
  ldc.i4 114
  beq a010114
  ldstr "a010114"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010114:
  ldsflda int32 [rvastatic3]A::a010115
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 115
  beq a010115
  ldstr "a010115"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010115:
  ldsflda int8 [rvastatic3]A::a010116
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 116
  beq a010116
  ldstr "a010116"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010116:
  ldsflda int16 [rvastatic3]A::a010117
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i2
  ldc.i4 117
  beq a010117
  ldstr "a010117"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010117:
  ldsflda int64 [rvastatic3]A::a010118
  conv.i8
  ldc.i8 2
  mul
  ldc.i4 1
  shr.un
  conv.i
  ldind.i8
  ldc.i8 118
  beq a010118
  ldstr "a010118"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010118:
  ldsflda int8 [rvastatic3]A::a010119
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 119
  beq a010119
  ldstr "a010119"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010119:
  ldsflda int64 [rvastatic3]A::a010120
  conv.r8
  ldc.r8 234.098
  add
  conv.r8
  ldc.r8 -234.098
  add
  conv.i
  ldind.i8
  ldc.i8 120
  beq a010120
  ldstr "a010120"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010120:
  ldsflda int32 [rvastatic3]A::a010121
  conv.i8
  dup
  dup
  xor
  xor
  conv.i
  ldind.i4
  ldc.i4 121
  beq a010121
  ldstr "a010121"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010121:
  ldsflda int8 [rvastatic3]A::a010122
  conv.i8
  ldc.i8 51550
  add
  conv.i8
  ldc.i8 51550
  sub
  conv.i
  ldind.i1
  ldc.i4 2
  beq a010122
  ldstr "a010122"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010122:
  ldsflda int32 [rvastatic3]A::a010123
  conv.i8
  dup
  ldc.i8 0x0000000000ffffff
  and
  ldc.i4 32
  shl
  conv.i8
  ldc.i4 32
  shr.un
  or
  conv.i
  ldind.i4
  ldc.i4 123
  beq a010123
  ldstr "a010123"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010123:
  ldsflda int8 [rvastatic3]A::a010124
  conv.i8
  conv.r8
  conv.u8
  conv.i
  conv.i8
  conv.r8
  conv.u8
  conv.i
  ldind.i1
  ldc.i4 4
  beq a010124
  ldstr "a010124"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010124:
  ldsflda int64 [rvastatic3]A::a010125
  conv.i8
  ldc.i8 47464
  add
  conv.i8
  ldc.i8 47464
  sub
  conv.i
  ldind.i8
  ldc.i8 125
  beq a010125
  ldstr "a010125"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010125:
  ldsflda float32 [rvastatic3]A::a010126
  conv.i8
  ldc.i8 24077
  add
  conv.i8
  ldc.i8 24077
  sub
  conv.i
  ldind.r4
  ldc.r4 126.0
  beq a010126
  ldstr "a010126"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010126:
  ldsflda int64 [rvastatic3]A::a010127
  conv.i8
  ldc.i8 28583
  add
  conv.i8
  ldc.i8 28583
  sub
  conv.i
  ldind.i8
  ldc.i8 127
  beq a010127
  ldstr "a010127"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010127:
 ret}
 .method static void V5() {.maxstack  50
  ldsflda int64 [rvastatic3]A::a0100
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 0
  beq a0100
  ldstr "a0100"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0100:
  ldsflda int64 [rvastatic3]A::a0101
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 1
  beq a0101
  ldstr "a0101"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0101:
  ldsflda float32 [rvastatic3]A::a0102
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.r4
  ldc.r4 2.0
  beq a0102
  ldstr "a0102"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0102:
  ldsflda int64 [rvastatic3]A::a0103
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 3
  beq a0103
  ldstr "a0103"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0103:
  ldsflda int64 [rvastatic3]A::a0104
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 4
  beq a0104
  ldstr "a0104"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0104:
  ldsflda int16 [rvastatic3]A::a0105
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 5
  beq a0105
  ldstr "a0105"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0105:
  ldsflda int16 [rvastatic3]A::a0106
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 6
  beq a0106
  ldstr "a0106"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0106:
  ldsflda float32 [rvastatic3]A::a0107
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 7.0
  beq a0107
  ldstr "a0107"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0107:
  ldsflda int32 [rvastatic3]A::a0108
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 8
  beq a0108
  ldstr "a0108"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0108:
  ldsflda int8 [rvastatic3]A::a0109
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 9
  beq a0109
  ldstr "a0109"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a0109:
  ldsflda int32 [rvastatic3]A::a01010
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 10
  beq a01010
  ldstr "a01010"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01010:
  ldsflda int64 [rvastatic3]A::a01011
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 11
  beq a01011
  ldstr "a01011"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01011:
  ldsflda int32 [rvastatic3]A::a01012
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 12
  beq a01012
  ldstr "a01012"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01012:
  ldsflda int8 [rvastatic3]A::a01013
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 13
  beq a01013
  ldstr "a01013"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01013:
  ldsflda int16 [rvastatic3]A::a01014
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 14
  beq a01014
  ldstr "a01014"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01014:
  ldsflda int16 [rvastatic3]A::a01015
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 15
  beq a01015
  ldstr "a01015"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01015:
  ldsflda float32 [rvastatic3]A::a01016
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 16.0
  beq a01016
  ldstr "a01016"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01016:
  ldsflda float32 [rvastatic3]A::a01017
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.r4
  ldc.r4 17.0
  beq a01017
  ldstr "a01017"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01017:
  ldsflda int32 [rvastatic3]A::a01018
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 18
  beq a01018
  ldstr "a01018"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01018:
  ldsflda int8 [rvastatic3]A::a01019
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 19
  beq a01019
  ldstr "a01019"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01019:
  ldsflda int32 [rvastatic3]A::a01020
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 20
  beq a01020
  ldstr "a01020"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01020:
  ldsflda int32 [rvastatic3]A::a01021
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 21
  beq a01021
  ldstr "a01021"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01021:
  ldsflda int64 [rvastatic3]A::a01022
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 22
  beq a01022
  ldstr "a01022"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01022:
  ldsflda int32 [rvastatic3]A::a01023
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 23
  beq a01023
  ldstr "a01023"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01023:
  ldsflda int8 [rvastatic3]A::a01024
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 24
  beq a01024
  ldstr "a01024"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01024:
  ldsflda int8 [rvastatic3]A::a01025
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 25
  beq a01025
  ldstr "a01025"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01025:
  ldsflda int16 [rvastatic3]A::a01026
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 26
  beq a01026
  ldstr "a01026"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01026:
  ldsflda int8 [rvastatic3]A::a01027
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 27
  beq a01027
  ldstr "a01027"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01027:
  ldsflda int16 [rvastatic3]A::a01028
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 28
  beq a01028
  ldstr "a01028"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01028:
  ldsflda int64 [rvastatic3]A::a01029
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 29
  beq a01029
  ldstr "a01029"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01029:
  ldsflda int32 [rvastatic3]A::a01030
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 30
  beq a01030
  ldstr "a01030"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01030:
  ldsflda int32 [rvastatic3]A::a01031
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 31
  beq a01031
  ldstr "a01031"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01031:
  ldsflda int32 [rvastatic3]A::a01032
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 32
  beq a01032
  ldstr "a01032"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01032:
  ldsflda int8 [rvastatic3]A::a01033
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 33
  beq a01033
  ldstr "a01033"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01033:
  ldsflda int16 [rvastatic3]A::a01034
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 34
  beq a01034
  ldstr "a01034"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01034:
  ldsflda int32 [rvastatic3]A::a01035
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 35
  beq a01035
  ldstr "a01035"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01035:
  ldsflda int32 [rvastatic3]A::a01036
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 36
  beq a01036
  ldstr "a01036"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01036:
  ldsflda int16 [rvastatic3]A::a01037
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 37
  beq a01037
  ldstr "a01037"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01037:
  ldsflda float32 [rvastatic3]A::a01038
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 38.0
  beq a01038
  ldstr "a01038"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01038:
  ldsflda int8 [rvastatic3]A::a01039
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 39
  beq a01039
  ldstr "a01039"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01039:
  ldsflda int8 [rvastatic3]A::a01040
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 40
  beq a01040
  ldstr "a01040"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01040:
  ldsflda float32 [rvastatic3]A::a01041
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 41.0
  beq a01041
  ldstr "a01041"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01041:
  ldsflda int32 [rvastatic3]A::a01042
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 42
  beq a01042
  ldstr "a01042"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01042:
  ldsflda int32 [rvastatic3]A::a01043
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 43
  beq a01043
  ldstr "a01043"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01043:
  ldsflda int32 [rvastatic3]A::a01044
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 44
  beq a01044
  ldstr "a01044"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01044:
  ldsflda int64 [rvastatic3]A::a01045
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 45
  beq a01045
  ldstr "a01045"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01045:
  ldsflda int64 [rvastatic3]A::a01046
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 46
  beq a01046
  ldstr "a01046"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01046:
  ldsflda int64 [rvastatic3]A::a01047
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 47
  beq a01047
  ldstr "a01047"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01047:
  ldsflda float32 [rvastatic3]A::a01048
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 48.0
  beq a01048
  ldstr "a01048"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01048:
  ldsflda int64 [rvastatic3]A::a01049
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 49
  beq a01049
  ldstr "a01049"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01049:
  ldsflda int32 [rvastatic3]A::a01050
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 50
  beq a01050
  ldstr "a01050"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01050:
  ldsflda float32 [rvastatic3]A::a01051
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 51.0
  beq a01051
  ldstr "a01051"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01051:
  ldsflda int32 [rvastatic3]A::a01052
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 52
  beq a01052
  ldstr "a01052"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01052:
  ldsflda int64 [rvastatic3]A::a01053
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 53
  beq a01053
  ldstr "a01053"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01053:
  ldsflda int8 [rvastatic3]A::a01054
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 54
  beq a01054
  ldstr "a01054"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01054:
  ldsflda int8 [rvastatic3]A::a01055
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 55
  beq a01055
  ldstr "a01055"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01055:
  ldsflda float32 [rvastatic3]A::a01056
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 56.0
  beq a01056
  ldstr "a01056"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01056:
  ldsflda int32 [rvastatic3]A::a01057
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 57
  beq a01057
  ldstr "a01057"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01057:
  ldsflda int64 [rvastatic3]A::a01058
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 58
  beq a01058
  ldstr "a01058"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01058:
  ldsflda int64 [rvastatic3]A::a01059
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 59
  beq a01059
  ldstr "a01059"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01059:
  ldsflda int8 [rvastatic3]A::a01060
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 60
  beq a01060
  ldstr "a01060"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01060:
  ldsflda int16 [rvastatic3]A::a01061
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 61
  beq a01061
  ldstr "a01061"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01061:
  ldsflda int64 [rvastatic3]A::a01062
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 62
  beq a01062
  ldstr "a01062"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01062:
  ldsflda float32 [rvastatic3]A::a01063
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 63.0
  beq a01063
  ldstr "a01063"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01063:
  ldsflda int64 [rvastatic3]A::a01064
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 64
  beq a01064
  ldstr "a01064"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01064:
  ldsflda float32 [rvastatic3]A::a01065
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 65.0
  beq a01065
  ldstr "a01065"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01065:
  ldsflda int8 [rvastatic3]A::a01066
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 66
  beq a01066
  ldstr "a01066"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01066:
  ldsflda int16 [rvastatic3]A::a01067
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 67
  beq a01067
  ldstr "a01067"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01067:
  ldsflda int64 [rvastatic3]A::a01068
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 68
  beq a01068
  ldstr "a01068"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01068:
  ldsflda int64 [rvastatic3]A::a01069
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 69
  beq a01069
  ldstr "a01069"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01069:
  ldsflda int64 [rvastatic3]A::a01070
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 70
  beq a01070
  ldstr "a01070"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01070:
  ldsflda int32 [rvastatic3]A::a01071
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 71
  beq a01071
  ldstr "a01071"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01071:
  ldsflda int8 [rvastatic3]A::a01072
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 72
  beq a01072
  ldstr "a01072"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01072:
  ldsflda int32 [rvastatic3]A::a01073
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 73
  beq a01073
  ldstr "a01073"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01073:
  ldsflda float32 [rvastatic3]A::a01074
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 74.0
  beq a01074
  ldstr "a01074"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01074:
  ldsflda int64 [rvastatic3]A::a01075
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 75
  beq a01075
  ldstr "a01075"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01075:
  ldsflda int8 [rvastatic3]A::a01076
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 76
  beq a01076
  ldstr "a01076"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01076:
  ldsflda int8 [rvastatic3]A::a01077
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 77
  beq a01077
  ldstr "a01077"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01077:
  ldsflda int8 [rvastatic3]A::a01078
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 78
  beq a01078
  ldstr "a01078"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01078:
  ldsflda int32 [rvastatic3]A::a01079
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 79
  beq a01079
  ldstr "a01079"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01079:
  ldsflda int16 [rvastatic3]A::a01080
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 80
  beq a01080
  ldstr "a01080"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01080:
  ldsflda int32 [rvastatic3]A::a01081
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 81
  beq a01081
  ldstr "a01081"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01081:
  ldsflda int8 [rvastatic3]A::a01082
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 82
  beq a01082
  ldstr "a01082"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01082:
  ldsflda int32 [rvastatic3]A::a01083
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 83
  beq a01083
  ldstr "a01083"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01083:
  ldsflda int8 [rvastatic3]A::a01084
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 84
  beq a01084
  ldstr "a01084"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01084:
  ldsflda int32 [rvastatic3]A::a01085
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 85
  beq a01085
  ldstr "a01085"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01085:
  ldsflda int8 [rvastatic3]A::a01086
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 86
  beq a01086
  ldstr "a01086"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01086:
  ldsflda int64 [rvastatic3]A::a01087
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 87
  beq a01087
  ldstr "a01087"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01087:
  ldsflda int8 [rvastatic3]A::a01088
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 88
  beq a01088
  ldstr "a01088"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01088:
  ldsflda int16 [rvastatic3]A::a01089
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 89
  beq a01089
  ldstr "a01089"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01089:
  ldsflda int64 [rvastatic3]A::a01090
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 90
  beq a01090
  ldstr "a01090"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01090:
  ldsflda int8 [rvastatic3]A::a01091
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 91
  beq a01091
  ldstr "a01091"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01091:
  ldsflda int64 [rvastatic3]A::a01092
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 92
  beq a01092
  ldstr "a01092"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01092:
  ldsflda int16 [rvastatic3]A::a01093
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 93
  beq a01093
  ldstr "a01093"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01093:
  ldsflda int8 [rvastatic3]A::a01094
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 94
  beq a01094
  ldstr "a01094"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01094:
  ldsflda float32 [rvastatic3]A::a01095
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 95.0
  beq a01095
  ldstr "a01095"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01095:
  ldsflda int16 [rvastatic3]A::a01096
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 96
  beq a01096
  ldstr "a01096"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01096:
  ldsflda int64 [rvastatic3]A::a01097
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 97
  beq a01097
  ldstr "a01097"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01097:
  ldsflda float32 [rvastatic3]A::a01098
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 98.0
  beq a01098
  ldstr "a01098"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01098:
  ldsflda int32 [rvastatic3]A::a01099
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 99
  beq a01099
  ldstr "a01099"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a01099:
  ldsflda int32 [rvastatic3]A::a010100
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 100
  beq a010100
  ldstr "a010100"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010100:
  ldsflda int32 [rvastatic3]A::a010101
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 101
  beq a010101
  ldstr "a010101"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010101:
  ldsflda int8 [rvastatic3]A::a010102
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i1
  ldc.i4 102
  beq a010102
  ldstr "a010102"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010102:
  ldsflda int16 [rvastatic3]A::a010103
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 103
  beq a010103
  ldstr "a010103"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010103:
  ldsflda int8 [rvastatic3]A::a010104
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 104
  beq a010104
  ldstr "a010104"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010104:
  ldsflda float32 [rvastatic3]A::a010105
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 105.0
  beq a010105
  ldstr "a010105"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010105:
  ldsflda float32 [rvastatic3]A::a010106
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 106.0
  beq a010106
  ldstr "a010106"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010106:
  ldsflda int8 [rvastatic3]A::a010107
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 107
  beq a010107
  ldstr "a010107"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010107:
  ldsflda float32 [rvastatic3]A::a010108
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 108.0
  beq a010108
  ldstr "a010108"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010108:
  ldsflda int16 [rvastatic3]A::a010109
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 109
  beq a010109
  ldstr "a010109"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010109:
  ldsflda int8 [rvastatic3]A::a010110
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 110
  beq a010110
  ldstr "a010110"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010110:
  ldsflda int64 [rvastatic3]A::a010111
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 111
  beq a010111
  ldstr "a010111"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010111:
  ldsflda int16 [rvastatic3]A::a010112
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 112
  beq a010112
  ldstr "a010112"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010112:
  ldsflda int16 [rvastatic3]A::a010113
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i2
  ldc.i4 113
  beq a010113
  ldstr "a010113"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010113:
  ldsflda int32 [rvastatic3]A::a010114
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 114
  beq a010114
  ldstr "a010114"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010114:
  ldsflda int32 [rvastatic3]A::a010115
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 115
  beq a010115
  ldstr "a010115"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010115:
  ldsflda int8 [rvastatic3]A::a010116
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 116
  beq a010116
  ldstr "a010116"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010116:
  ldsflda int16 [rvastatic3]A::a010117
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i2
  ldc.i4 117
  beq a010117
  ldstr "a010117"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010117:
  ldsflda int64 [rvastatic3]A::a010118
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 118
  beq a010118
  ldstr "a010118"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010118:
  ldsflda int8 [rvastatic3]A::a010119
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 119
  beq a010119
  ldstr "a010119"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010119:
  ldsflda int64 [rvastatic3]A::a010120
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 120
  beq a010120
  ldstr "a010120"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010120:
  ldsflda int32 [rvastatic3]A::a010121
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i4
  ldc.i4 121
  beq a010121
  ldstr "a010121"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010121:
  ldsflda int8 [rvastatic3]A::a010122
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 2
  beq a010122
  ldstr "a010122"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010122:
  ldsflda int32 [rvastatic3]A::a010123
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i4
  ldc.i4 123
  beq a010123
  ldstr "a010123"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010123:
  ldsflda int8 [rvastatic3]A::a010124
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i1
  ldc.i4 4
  beq a010124
  ldstr "a010124"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010124:
  ldsflda int64 [rvastatic3]A::a010125
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.i8
  ldc.i8 125
  beq a010125
  ldstr "a010125"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010125:
  ldsflda float32 [rvastatic3]A::a010126
  conv.r8
  call native int [rvastatic3]A::Call2(float64)
  ldind.r4
  ldc.r4 126.0
  beq a010126
  ldstr "a010126"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010126:
  ldsflda int64 [rvastatic3]A::a010127
  conv.i8
  call native int [rvastatic3]A::Call1(int64)
  ldind.i8
  ldc.i8 127
  beq a010127
  ldstr "a010127"
  newobj instance void [mscorlib]System.Exception::.ctor(string)
  throw
a010127:
 ret}
   .method static void V6() {.maxstack  50
  ldsfld int64 [rvastatic3]A::a0100
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a0100
  ldsfld int64 [rvastatic3]A::a0100
  ldc.i8 1
  beq a0100
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0100:
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a0101
  ldsfld int64 [rvastatic3]A::a0101
  ldc.i8 2
  beq a0101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0101:
  ldsfld float32 [rvastatic3]A::a0102
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a0102
  ldsfld float32 [rvastatic3]A::a0102
  ldc.r4 3.0
  beq a0102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0102:
  ldsfld int64 [rvastatic3]A::a0103
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a0103
  ldsfld int64 [rvastatic3]A::a0103
  ldc.i8 4
  beq a0103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0103:
  ldsfld int64 [rvastatic3]A::a0104
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a0104
  ldsfld int64 [rvastatic3]A::a0104
  ldc.i8 5
  beq a0104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0104:
  ldsfld int16 [rvastatic3]A::a0105
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a0105
  ldsfld int16 [rvastatic3]A::a0105
  ldc.i4 6
  beq a0105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0105:
  ldsfld int16 [rvastatic3]A::a0106
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a0106
  ldsfld int16 [rvastatic3]A::a0106
  ldc.i4 7
  beq a0106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0106:
  ldsfld float32 [rvastatic3]A::a0107
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a0107
  ldsfld float32 [rvastatic3]A::a0107
  ldc.r4 8.0
  beq a0107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0107:
  ldsfld int32 [rvastatic3]A::a0108
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a0108
  ldsfld int32 [rvastatic3]A::a0108
  ldc.i4 9
  beq a0108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0108:
  ldsfld int8 [rvastatic3]A::a0109
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a0109
  ldsfld int8 [rvastatic3]A::a0109
  ldc.i4 10
  beq a0109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a0109:
  ldsfld int32 [rvastatic3]A::a01010
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01010
  ldsfld int32 [rvastatic3]A::a01010
  ldc.i4 11
  beq a01010
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01010:
  ldsfld int64 [rvastatic3]A::a01011
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01011
  ldsfld int64 [rvastatic3]A::a01011
  ldc.i8 12
  beq a01011
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01011:
  ldsfld int32 [rvastatic3]A::a01012
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01012
  ldsfld int32 [rvastatic3]A::a01012
  ldc.i4 13
  beq a01012
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01012:
  ldsfld int8 [rvastatic3]A::a01013
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01013
  ldsfld int8 [rvastatic3]A::a01013
  ldc.i4 14
  beq a01013
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01013:
  ldsfld int16 [rvastatic3]A::a01014
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01014
  ldsfld int16 [rvastatic3]A::a01014
  ldc.i4 15
  beq a01014
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01014:
  ldsfld int16 [rvastatic3]A::a01015
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01015
  ldsfld int16 [rvastatic3]A::a01015
  ldc.i4 16
  beq a01015
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01015:
  ldsfld float32 [rvastatic3]A::a01016
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01016
  ldsfld float32 [rvastatic3]A::a01016
  ldc.r4 17.0
  beq a01016
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01016:
  ldsfld float32 [rvastatic3]A::a01017
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01017
  ldsfld float32 [rvastatic3]A::a01017
  ldc.r4 18.0
  beq a01017
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01017:
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01018
  ldsfld int32 [rvastatic3]A::a01018
  ldc.i4 19
  beq a01018
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01018:
  ldsfld int8 [rvastatic3]A::a01019
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01019
  ldsfld int8 [rvastatic3]A::a01019
  ldc.i4 20
  beq a01019
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01019:
  ldsfld int32 [rvastatic3]A::a01020
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01020
  ldsfld int32 [rvastatic3]A::a01020
  ldc.i4 21
  beq a01020
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01020:
  ldsfld int32 [rvastatic3]A::a01021
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01021
  ldsfld int32 [rvastatic3]A::a01021
  ldc.i4 22
  beq a01021
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01021:
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01022
  ldsfld int64 [rvastatic3]A::a01022
  ldc.i8 23
  beq a01022
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01022:
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01023
  ldsfld int32 [rvastatic3]A::a01023
  ldc.i4 24
  beq a01023
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01023:
  ldsfld int8 [rvastatic3]A::a01024
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01024
  ldsfld int8 [rvastatic3]A::a01024
  ldc.i4 25
  beq a01024
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01024:
  ldsfld int8 [rvastatic3]A::a01025
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01025
  ldsfld int8 [rvastatic3]A::a01025
  ldc.i4 26
  beq a01025
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01025:
  ldsfld int16 [rvastatic3]A::a01026
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01026
  ldsfld int16 [rvastatic3]A::a01026
  ldc.i4 27
  beq a01026
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01026:
  ldsfld int8 [rvastatic3]A::a01027
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01027
  ldsfld int8 [rvastatic3]A::a01027
  ldc.i4 28
  beq a01027
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01027:
  ldsfld int16 [rvastatic3]A::a01028
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01028
  ldsfld int16 [rvastatic3]A::a01028
  ldc.i4 29
  beq a01028
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01028:
  ldsfld int64 [rvastatic3]A::a01029
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01029
  ldsfld int64 [rvastatic3]A::a01029
  ldc.i8 30
  beq a01029
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01029:
  ldsfld int32 [rvastatic3]A::a01030
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01030
  ldsfld int32 [rvastatic3]A::a01030
  ldc.i4 31
  beq a01030
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01030:
  ldsfld int32 [rvastatic3]A::a01031
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01031
  ldsfld int32 [rvastatic3]A::a01031
  ldc.i4 32
  beq a01031
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01031:
  ldsfld int32 [rvastatic3]A::a01032
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01032
  ldsfld int32 [rvastatic3]A::a01032
  ldc.i4 33
  beq a01032
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01032:
  ldsfld int8 [rvastatic3]A::a01033
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01033
  ldsfld int8 [rvastatic3]A::a01033
  ldc.i4 34
  beq a01033
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01033:
  ldsfld int16 [rvastatic3]A::a01034
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01034
  ldsfld int16 [rvastatic3]A::a01034
  ldc.i4 35
  beq a01034
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01034:
  ldsfld int32 [rvastatic3]A::a01035
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01035
  ldsfld int32 [rvastatic3]A::a01035
  ldc.i4 36
  beq a01035
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01035:
  ldsfld int32 [rvastatic3]A::a01036
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01036
  ldsfld int32 [rvastatic3]A::a01036
  ldc.i4 37
  beq a01036
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01036:
  ldsfld int16 [rvastatic3]A::a01037
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01037
  ldsfld int16 [rvastatic3]A::a01037
  ldc.i4 38
  beq a01037
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01037:
  ldsfld float32 [rvastatic3]A::a01038
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01038
  ldsfld float32 [rvastatic3]A::a01038
  ldc.r4 39.0
  beq a01038
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01038:
  ldsfld int8 [rvastatic3]A::a01039
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01039
  ldsfld int8 [rvastatic3]A::a01039
  ldc.i4 40
  beq a01039
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01039:
  ldsfld int8 [rvastatic3]A::a01040
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01040
  ldsfld int8 [rvastatic3]A::a01040
  ldc.i4 41
  beq a01040
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01040:
  ldsfld float32 [rvastatic3]A::a01041
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01041
  ldsfld float32 [rvastatic3]A::a01041
  ldc.r4 42.0
  beq a01041
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01041:
  ldsfld int32 [rvastatic3]A::a01042
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01042
  ldsfld int32 [rvastatic3]A::a01042
  ldc.i4 43
  beq a01042
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01042:
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01043
  ldsfld int32 [rvastatic3]A::a01043
  ldc.i4 44
  beq a01043
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01043:
  ldsfld int32 [rvastatic3]A::a01044
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01044
  ldsfld int32 [rvastatic3]A::a01044
  ldc.i4 45
  beq a01044
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01044:
  ldsfld int64 [rvastatic3]A::a01045
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01045
  ldsfld int64 [rvastatic3]A::a01045
  ldc.i8 46
  beq a01045
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01045:
  ldsfld int64 [rvastatic3]A::a01046
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01046
  ldsfld int64 [rvastatic3]A::a01046
  ldc.i8 47
  beq a01046
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01046:
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01047
  ldsfld int64 [rvastatic3]A::a01047
  ldc.i8 48
  beq a01047
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01047:
  ldsfld float32 [rvastatic3]A::a01048
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01048
  ldsfld float32 [rvastatic3]A::a01048
  ldc.r4 49.0
  beq a01048
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01048:
  ldsfld int64 [rvastatic3]A::a01049
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01049
  ldsfld int64 [rvastatic3]A::a01049
  ldc.i8 50
  beq a01049
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01049:
  ldsfld int32 [rvastatic3]A::a01050
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01050
  ldsfld int32 [rvastatic3]A::a01050
  ldc.i4 51
  beq a01050
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01050:
  ldsfld float32 [rvastatic3]A::a01051
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01051
  ldsfld float32 [rvastatic3]A::a01051
  ldc.r4 52.0
  beq a01051
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01051:
  ldsfld int32 [rvastatic3]A::a01052
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01052
  ldsfld int32 [rvastatic3]A::a01052
  ldc.i4 53
  beq a01052
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01052:
  ldsfld int64 [rvastatic3]A::a01053
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01053
  ldsfld int64 [rvastatic3]A::a01053
  ldc.i8 54
  beq a01053
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01053:
  ldsfld int8 [rvastatic3]A::a01054
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01054
  ldsfld int8 [rvastatic3]A::a01054
  ldc.i4 55
  beq a01054
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01054:
  ldsfld int8 [rvastatic3]A::a01055
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01055
  ldsfld int8 [rvastatic3]A::a01055
  ldc.i4 56
  beq a01055
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01055:
  ldsfld float32 [rvastatic3]A::a01056
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01056
  ldsfld float32 [rvastatic3]A::a01056
  ldc.r4 57.0
  beq a01056
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01056:
  ldsfld int32 [rvastatic3]A::a01057
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01057
  ldsfld int32 [rvastatic3]A::a01057
  ldc.i4 58
  beq a01057
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01057:
  ldsfld int64 [rvastatic3]A::a01058
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01058
  ldsfld int64 [rvastatic3]A::a01058
  ldc.i8 59
  beq a01058
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01058:
  ldsfld int64 [rvastatic3]A::a01059
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01059
  ldsfld int64 [rvastatic3]A::a01059
  ldc.i8 60
  beq a01059
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01059:
  ldsfld int8 [rvastatic3]A::a01060
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01060
  ldsfld int8 [rvastatic3]A::a01060
  ldc.i4 61
  beq a01060
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01060:
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01061
  ldsfld int16 [rvastatic3]A::a01061
  ldc.i4 62
  beq a01061
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01061:
  ldsfld int64 [rvastatic3]A::a01062
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01062
  ldsfld int64 [rvastatic3]A::a01062
  ldc.i8 63
  beq a01062
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01062:
  ldsfld float32 [rvastatic3]A::a01063
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01063
  ldsfld float32 [rvastatic3]A::a01063
  ldc.r4 64.0
  beq a01063
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01063:
  ldsfld int64 [rvastatic3]A::a01064
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01064
  ldsfld int64 [rvastatic3]A::a01064
  ldc.i8 65
  beq a01064
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01064:
  ldsfld float32 [rvastatic3]A::a01065
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01065
  ldsfld float32 [rvastatic3]A::a01065
  ldc.r4 66.0
  beq a01065
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01065:
  ldsfld int8 [rvastatic3]A::a01066
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01066
  ldsfld int8 [rvastatic3]A::a01066
  ldc.i4 67
  beq a01066
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01066:
  ldsfld int16 [rvastatic3]A::a01067
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01067
  ldsfld int16 [rvastatic3]A::a01067
  ldc.i4 68
  beq a01067
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01067:
  ldsfld int64 [rvastatic3]A::a01068
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01068
  ldsfld int64 [rvastatic3]A::a01068
  ldc.i8 69
  beq a01068
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01068:
  ldsfld int64 [rvastatic3]A::a01069
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01069
  ldsfld int64 [rvastatic3]A::a01069
  ldc.i8 70
  beq a01069
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01069:
  ldsfld int64 [rvastatic3]A::a01070
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01070
  ldsfld int64 [rvastatic3]A::a01070
  ldc.i8 71
  beq a01070
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01070:
  ldsfld int32 [rvastatic3]A::a01071
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01071
  ldsfld int32 [rvastatic3]A::a01071
  ldc.i4 72
  beq a01071
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01071:
  ldsfld int8 [rvastatic3]A::a01072
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01072
  ldsfld int8 [rvastatic3]A::a01072
  ldc.i4 73
  beq a01072
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01072:
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01073
  ldsfld int32 [rvastatic3]A::a01073
  ldc.i4 74
  beq a01073
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01073:
  ldsfld float32 [rvastatic3]A::a01074
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01074
  ldsfld float32 [rvastatic3]A::a01074
  ldc.r4 75.0
  beq a01074
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01074:
  ldsfld int64 [rvastatic3]A::a01075
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01075
  ldsfld int64 [rvastatic3]A::a01075
  ldc.i8 76
  beq a01075
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01075:
  ldsfld int8 [rvastatic3]A::a01076
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01076
  ldsfld int8 [rvastatic3]A::a01076
  ldc.i4 77
  beq a01076
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01076:
  ldsfld int8 [rvastatic3]A::a01077
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01077
  ldsfld int8 [rvastatic3]A::a01077
  ldc.i4 78
  beq a01077
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01077:
  ldsfld int8 [rvastatic3]A::a01078
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01078
  ldsfld int8 [rvastatic3]A::a01078
  ldc.i4 79
  beq a01078
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01078:
  ldsfld int32 [rvastatic3]A::a01079
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01079
  ldsfld int32 [rvastatic3]A::a01079
  ldc.i4 80
  beq a01079
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01079:
  ldsfld int16 [rvastatic3]A::a01080
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01080
  ldsfld int16 [rvastatic3]A::a01080
  ldc.i4 81
  beq a01080
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01080:
  ldsfld int32 [rvastatic3]A::a01081
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01081
  ldsfld int32 [rvastatic3]A::a01081
  ldc.i4 82
  beq a01081
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01081:
  ldsfld int8 [rvastatic3]A::a01082
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01082
  ldsfld int8 [rvastatic3]A::a01082
  ldc.i4 83
  beq a01082
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01082:
  ldsfld int32 [rvastatic3]A::a01083
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01083
  ldsfld int32 [rvastatic3]A::a01083
  ldc.i4 84
  beq a01083
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01083:
  ldsfld int8 [rvastatic3]A::a01084
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01084
  ldsfld int8 [rvastatic3]A::a01084
  ldc.i4 85
  beq a01084
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01084:
  ldsfld int32 [rvastatic3]A::a01085
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01085
  ldsfld int32 [rvastatic3]A::a01085
  ldc.i4 86
  beq a01085
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01085:
  ldsfld int8 [rvastatic3]A::a01086
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01086
  ldsfld int8 [rvastatic3]A::a01086
  ldc.i4 87
  beq a01086
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01086:
  ldsfld int64 [rvastatic3]A::a01087
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01087
  ldsfld int64 [rvastatic3]A::a01087
  ldc.i8 88
  beq a01087
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01087:
  ldsfld int8 [rvastatic3]A::a01088
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01088
  ldsfld int8 [rvastatic3]A::a01088
  ldc.i4 89
  beq a01088
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01088:
  ldsfld int16 [rvastatic3]A::a01089
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01089
  ldsfld int16 [rvastatic3]A::a01089
  ldc.i4 90
  beq a01089
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01089:
  ldsfld int64 [rvastatic3]A::a01090
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01090
  ldsfld int64 [rvastatic3]A::a01090
  ldc.i8 91
  beq a01090
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01090:
  ldsfld int8 [rvastatic3]A::a01091
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01091
  ldsfld int8 [rvastatic3]A::a01091
  ldc.i4 92
  beq a01091
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01091:
  ldsfld int64 [rvastatic3]A::a01092
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01092
  ldsfld int64 [rvastatic3]A::a01092
  ldc.i8 93
  beq a01092
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01092:
  ldsfld int16 [rvastatic3]A::a01093
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01093
  ldsfld int16 [rvastatic3]A::a01093
  ldc.i4 94
  beq a01093
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01093:
  ldsfld int8 [rvastatic3]A::a01094
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a01094
  ldsfld int8 [rvastatic3]A::a01094
  ldc.i4 95
  beq a01094
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01094:
  ldsfld float32 [rvastatic3]A::a01095
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01095
  ldsfld float32 [rvastatic3]A::a01095
  ldc.r4 96.0
  beq a01095
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01095:
  ldsfld int16 [rvastatic3]A::a01096
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a01096
  ldsfld int16 [rvastatic3]A::a01096
  ldc.i4 97
  beq a01096
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01096:
  ldsfld int64 [rvastatic3]A::a01097
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a01097
  ldsfld int64 [rvastatic3]A::a01097
  ldc.i8 98
  beq a01097
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01097:
  ldsfld float32 [rvastatic3]A::a01098
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a01098
  ldsfld float32 [rvastatic3]A::a01098
  ldc.r4 99.0
  beq a01098
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01098:
  ldsfld int32 [rvastatic3]A::a01099
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a01099
  ldsfld int32 [rvastatic3]A::a01099
  ldc.i4 100
  beq a01099
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a01099:
  ldsfld int32 [rvastatic3]A::a010100
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010100
  ldsfld int32 [rvastatic3]A::a010100
  ldc.i4 101
  beq a010100
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010100:
  ldsfld int32 [rvastatic3]A::a010101
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010101
  ldsfld int32 [rvastatic3]A::a010101
  ldc.i4 102
  beq a010101
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010101:
  ldsfld int8 [rvastatic3]A::a010102
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010102
  ldsfld int8 [rvastatic3]A::a010102
  ldc.i4 103
  beq a010102
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010102:
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a010103
  ldsfld int16 [rvastatic3]A::a010103
  ldc.i4 104
  beq a010103
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010103:
  ldsfld int8 [rvastatic3]A::a010104
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010104
  ldsfld int8 [rvastatic3]A::a010104
  ldc.i4 105
  beq a010104
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010104:
  ldsfld float32 [rvastatic3]A::a010105
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a010105
  ldsfld float32 [rvastatic3]A::a010105
  ldc.r4 106.0
  beq a010105
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010105:
  ldsfld float32 [rvastatic3]A::a010106
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a010106
  ldsfld float32 [rvastatic3]A::a010106
  ldc.r4 107.0
  beq a010106
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010106:
  ldsfld int8 [rvastatic3]A::a010107
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010107
  ldsfld int8 [rvastatic3]A::a010107
  ldc.i4 108
  beq a010107
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010107:
  ldsfld float32 [rvastatic3]A::a010108
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a010108
  ldsfld float32 [rvastatic3]A::a010108
  ldc.r4 109.0
  beq a010108
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010108:
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a010109
  ldsfld int16 [rvastatic3]A::a010109
  ldc.i4 110
  beq a010109
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010109:
  ldsfld int8 [rvastatic3]A::a010110
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010110
  ldsfld int8 [rvastatic3]A::a010110
  ldc.i4 111
  beq a010110
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010110:
  ldsfld int64 [rvastatic3]A::a010111
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a010111
  ldsfld int64 [rvastatic3]A::a010111
  ldc.i8 112
  beq a010111
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010111:
  ldsfld int16 [rvastatic3]A::a010112
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a010112
  ldsfld int16 [rvastatic3]A::a010112
  ldc.i4 113
  beq a010112
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010112:
  ldsfld int16 [rvastatic3]A::a010113
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a010113
  ldsfld int16 [rvastatic3]A::a010113
  ldc.i4 114
  beq a010113
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010113:
  ldsfld int32 [rvastatic3]A::a010114
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010114
  ldsfld int32 [rvastatic3]A::a010114
  ldc.i4 115
  beq a010114
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010114:
  ldsfld int32 [rvastatic3]A::a010115
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010115
  ldsfld int32 [rvastatic3]A::a010115
  ldc.i4 116
  beq a010115
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010115:
  ldsfld int8 [rvastatic3]A::a010116
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010116
  ldsfld int8 [rvastatic3]A::a010116
  ldc.i4 117
  beq a010116
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010116:
  ldsfld int16 [rvastatic3]A::a010117
  ldc.i4 1
  add
  stsfld int16 [rvastatic3]A::a010117
  ldsfld int16 [rvastatic3]A::a010117
  ldc.i4 118
  beq a010117
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010117:
  ldsfld int64 [rvastatic3]A::a010118
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a010118
  ldsfld int64 [rvastatic3]A::a010118
  ldc.i8 119
  beq a010118
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010118:
  ldsfld int8 [rvastatic3]A::a010119
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010119
  ldsfld int8 [rvastatic3]A::a010119
  ldc.i4 120
  beq a010119
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010119:
  ldsfld int64 [rvastatic3]A::a010120
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a010120
  ldsfld int64 [rvastatic3]A::a010120
  ldc.i8 121
  beq a010120
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010120:
  ldsfld int32 [rvastatic3]A::a010121
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010121
  ldsfld int32 [rvastatic3]A::a010121
  ldc.i4 122
  beq a010121
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010121:
  ldsfld int8 [rvastatic3]A::a010122
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010122
  ldsfld int8 [rvastatic3]A::a010122
  ldc.i4 3
  beq a010122
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010122:
  ldsfld int32 [rvastatic3]A::a010123
  ldc.i4 1
  add
  stsfld int32 [rvastatic3]A::a010123
  ldsfld int32 [rvastatic3]A::a010123
  ldc.i4 124
  beq a010123
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010123:
  ldsfld int8 [rvastatic3]A::a010124
  ldc.i4 1
  add
  stsfld int8 [rvastatic3]A::a010124
  ldsfld int8 [rvastatic3]A::a010124
  ldc.i4 5
  beq a010124
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010124:
  ldsfld int64 [rvastatic3]A::a010125
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a010125
  ldsfld int64 [rvastatic3]A::a010125
  ldc.i8 126
  beq a010125
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010125:
  ldsfld float32 [rvastatic3]A::a010126
  ldc.r4 1
  add
  stsfld float32 [rvastatic3]A::a010126
  ldsfld float32 [rvastatic3]A::a010126
  ldc.r4 127.0
  beq a010126
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010126:
  ldsfld int64 [rvastatic3]A::a010127
  ldc.i8 1
  add
  stsfld int64 [rvastatic3]A::a010127
  ldsfld int64 [rvastatic3]A::a010127
  ldc.i8 128
  beq a010127
  newobj instance void [mscorlib]System.Exception::.ctor()
  throw
a010127:
  ret}
 .method public static int32 Main(){.entrypoint .maxstack  5
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  call void [rvastatic3]A::V1()
  call void [rvastatic3]A::V2()
  call void [rvastatic3]A::V3()
  call void [rvastatic3]A::V4()
  call void [rvastatic3]A::V5()
  call void [rvastatic3]A::V6()
  ldc.i4 100
 ret}
.field public static int64 a0100 at b0100
.field public static int64 a0101 at b0101
.field public static float32 a0102 at b0102
.field private static int32 aALIGN10102 at bALIGN10102
.field public static int64 a0103 at b0103
.field public static int64 a0104 at b0104
.field public static int16 a0105 at b0105
.field private static int16 aALIGN10105 at bALIGN10105
.field private static int32 aALIGN20105 at bALIGN20105
.field public static int16 a0106 at b0106
.field private static int16 aALIGN10106 at bALIGN10106
.field private static int32 aALIGN20106 at bALIGN20106
.field public static float32 a0107 at b0107
.field private static int32 aALIGN10107 at bALIGN10107
.field public static int32 a0108 at b0108
.field private static int32 aALIGN10108 at bALIGN10108
.field public static int8 a0109 at b0109
.field private static int32 aALIGN10109 at bALIGN10109
.field private static int16 aALIGN20109 at bALIGN20109
.field private static int8 aALIGN20109 at bALIGN30109
.field public static int32 a01010 at b01010
.field private static int32 aALIGN101010 at bALIGN101010
.field public static int64 a01011 at b01011
.field public static int32 a01012 at b01012
.field private static int32 aALIGN101012 at bALIGN101012
.field public static int8 a01013 at b01013
.field private static int32 aALIGN101013 at bALIGN101013
.field private static int16 aALIGN201013 at bALIGN201013
.field private static int8 aALIGN201013 at bALIGN301013
.field public static int16 a01014 at b01014
.field private static int16 aALIGN101014 at bALIGN101014
.field private static int32 aALIGN201014 at bALIGN201014
.field public static int16 a01015 at b01015
.field private static int16 aALIGN101015 at bALIGN101015
.field private static int32 aALIGN201015 at bALIGN201015
.field public static float32 a01016 at b01016
.field private static int32 aALIGN101016 at bALIGN101016
.field public static float32 a01017 at b01017
.field private static int32 aALIGN101017 at bALIGN101017
.field public static int32 a01018 at b01018
.field private static int32 aALIGN101018 at bALIGN101018
.field public static int8 a01019 at b01019
.field private static int32 aALIGN101019 at bALIGN101019
.field private static int16 aALIGN201019 at bALIGN201019
.field private static int8 aALIGN201019 at bALIGN301019
.field public static int32 a01020 at b01020
.field private static int32 aALIGN101020 at bALIGN101020
.field public static int32 a01021 at b01021
.field private static int32 aALIGN101021 at bALIGN101021
.field public static int64 a01022 at b01022
.field public static int32 a01023 at b01023
.field private static int32 aALIGN101023 at bALIGN101023
.field public static int8 a01024 at b01024
.field private static int32 aALIGN101024 at bALIGN101024
.field private static int16 aALIGN201024 at bALIGN201024
.field private static int8 aALIGN201024 at bALIGN301024
.field public static int8 a01025 at b01025
.field private static int32 aALIGN101025 at bALIGN101025
.field private static int16 aALIGN201025 at bALIGN201025
.field private static int8 aALIGN201025 at bALIGN301025
.field public static int16 a01026 at b01026
.field private static int16 aALIGN101026 at bALIGN101026
.field private static int32 aALIGN201026 at bALIGN201026
.field public static int8 a01027 at b01027
.field private static int32 aALIGN101027 at bALIGN101027
.field private static int16 aALIGN201027 at bALIGN201027
.field private static int8 aALIGN201027 at bALIGN301027
.field public static int16 a01028 at b01028
.field private static int16 aALIGN101028 at bALIGN101028
.field private static int32 aALIGN201028 at bALIGN201028
.field public static int64 a01029 at b01029
.field public static int32 a01030 at b01030
.field private static int32 aALIGN101030 at bALIGN101030
.field public static int32 a01031 at b01031
.field private static int32 aALIGN101031 at bALIGN101031
.field public static int32 a01032 at b01032
.field private static int32 aALIGN101032 at bALIGN101032
.field public static int8 a01033 at b01033
.field private static int32 aALIGN101033 at bALIGN101033
.field private static int16 aALIGN201033 at bALIGN201033
.field private static int8 aALIGN201033 at bALIGN301033
.field public static int16 a01034 at b01034
.field private static int16 aALIGN101034 at bALIGN101034
.field private static int32 aALIGN201034 at bALIGN201034
.field public static int32 a01035 at b01035
.field private static int32 aALIGN101035 at bALIGN101035
.field public static int32 a01036 at b01036
.field private static int32 aALIGN101036 at bALIGN101036
.field public static int16 a01037 at b01037
.field private static int16 aALIGN101037 at bALIGN101037
.field private static int32 aALIGN201037 at bALIGN201037
.field public static float32 a01038 at b01038
.field private static int32 aALIGN101038 at bALIGN101038
.field public static int8 a01039 at b01039
.field private static int32 aALIGN101039 at bALIGN101039
.field private static int16 aALIGN201039 at bALIGN201039
.field private static int8 aALIGN201039 at bALIGN301039
.field public static int8 a01040 at b01040
.field private static int32 aALIGN101040 at bALIGN101040
.field private static int16 aALIGN201040 at bALIGN201040
.field private static int8 aALIGN201040 at bALIGN301040
.field public static float32 a01041 at b01041
.field private static int32 aALIGN101041 at bALIGN101041
.field public static int32 a01042 at b01042
.field private static int32 aALIGN101042 at bALIGN101042
.field public static int32 a01043 at b01043
.field private static int32 aALIGN101043 at bALIGN101043
.field public static int32 a01044 at b01044
.field private static int32 aALIGN101044 at bALIGN101044
.field public static int64 a01045 at b01045
.field public static int64 a01046 at b01046
.field public static int64 a01047 at b01047
.field public static float32 a01048 at b01048
.field private static int32 aALIGN101048 at bALIGN101048
.field public static int64 a01049 at b01049
.field public static int32 a01050 at b01050
.field private static int32 aALIGN101050 at bALIGN101050
.field public static float32 a01051 at b01051
.field private static int32 aALIGN101051 at bALIGN101051
.field public static int32 a01052 at b01052
.field private static int32 aALIGN101052 at bALIGN101052
.field public static int64 a01053 at b01053
.field public static int8 a01054 at b01054
.field private static int32 aALIGN101054 at bALIGN101054
.field private static int16 aALIGN201054 at bALIGN201054
.field private static int8 aALIGN201054 at bALIGN301054
.field public static int8 a01055 at b01055
.field private static int32 aALIGN101055 at bALIGN101055
.field private static int16 aALIGN201055 at bALIGN201055
.field private static int8 aALIGN201055 at bALIGN301055
.field public static float32 a01056 at b01056
.field private static int32 aALIGN101056 at bALIGN101056
.field public static int32 a01057 at b01057
.field private static int32 aALIGN101057 at bALIGN101057
.field public static int64 a01058 at b01058
.field public static int64 a01059 at b01059
.field public static int8 a01060 at b01060
.field private static int32 aALIGN101060 at bALIGN101060
.field private static int16 aALIGN201060 at bALIGN201060
.field private static int8 aALIGN201060 at bALIGN301060
.field public static int16 a01061 at b01061
.field private static int16 aALIGN101061 at bALIGN101061
.field private static int32 aALIGN201061 at bALIGN201061
.field public static int64 a01062 at b01062
.field public static float32 a01063 at b01063
.field private static int32 aALIGN101063 at bALIGN101063
.field public static int64 a01064 at b01064
.field public static float32 a01065 at b01065
.field private static int32 aALIGN101065 at bALIGN101065
.field public static int8 a01066 at b01066
.field private static int32 aALIGN101066 at bALIGN101066
.field private static int16 aALIGN201066 at bALIGN201066
.field private static int8 aALIGN201066 at bALIGN301066
.field public static int16 a01067 at b01067
.field private static int16 aALIGN101067 at bALIGN101067
.field private static int32 aALIGN201067 at bALIGN201067
.field public static int64 a01068 at b01068
.field public static int64 a01069 at b01069
.field public static int64 a01070 at b01070
.field public static int32 a01071 at b01071
.field private static int32 aALIGN101071 at bALIGN101071
.field public static int8 a01072 at b01072
.field private static int32 aALIGN101072 at bALIGN101072
.field private static int16 aALIGN201072 at bALIGN201072
.field private static int8 aALIGN201072 at bALIGN301072
.field public static int32 a01073 at b01073
.field private static int32 aALIGN101073 at bALIGN101073
.field public static float32 a01074 at b01074
.field private static int32 aALIGN101074 at bALIGN101074
.field public static int64 a01075 at b01075
.field public static int8 a01076 at b01076
.field private static int32 aALIGN101076 at bALIGN101076
.field private static int16 aALIGN201076 at bALIGN201076
.field private static int8 aALIGN201076 at bALIGN301076
.field public static int8 a01077 at b01077
.field private static int32 aALIGN101077 at bALIGN101077
.field private static int16 aALIGN201077 at bALIGN201077
.field private static int8 aALIGN201077 at bALIGN301077
.field public static int8 a01078 at b01078
.field private static int32 aALIGN101078 at bALIGN101078
.field private static int16 aALIGN201078 at bALIGN201078
.field private static int8 aALIGN201078 at bALIGN301078
.field public static int32 a01079 at b01079
.field private static int32 aALIGN101079 at bALIGN101079
.field public static int16 a01080 at b01080
.field private static int16 aALIGN101080 at bALIGN101080
.field private static int32 aALIGN201080 at bALIGN201080
.field public static int32 a01081 at b01081
.field private static int32 aALIGN101081 at bALIGN101081
.field public static int8 a01082 at b01082
.field private static int32 aALIGN101082 at bALIGN101082
.field private static int16 aALIGN201082 at bALIGN201082
.field private static int8 aALIGN201082 at bALIGN301082
.field public static int32 a01083 at b01083
.field private static int32 aALIGN101083 at bALIGN101083
.field public static int8 a01084 at b01084
.field private static int32 aALIGN101084 at bALIGN101084
.field private static int16 aALIGN201084 at bALIGN201084
.field private static int8 aALIGN201084 at bALIGN301084
.field public static int32 a01085 at b01085
.field private static int32 aALIGN101085 at bALIGN101085
.field public static int8 a01086 at b01086
.field private static int32 aALIGN101086 at bALIGN101086
.field private static int16 aALIGN201086 at bALIGN201086
.field private static int8 aALIGN201086 at bALIGN301086
.field public static int64 a01087 at b01087
.field public static int8 a01088 at b01088
.field private static int32 aALIGN101088 at bALIGN101088
.field private static int16 aALIGN201088 at bALIGN201088
.field private static int8 aALIGN201088 at bALIGN301088
.field public static int16 a01089 at b01089
.field private static int16 aALIGN101089 at bALIGN101089
.field private static int32 aALIGN201089 at bALIGN201089
.field public static int64 a01090 at b01090
.field public static int8 a01091 at b01091
.field private static int32 aALIGN101091 at bALIGN101091
.field private static int16 aALIGN201091 at bALIGN201091
.field private static int8 aALIGN201091 at bALIGN301091
.field public static int64 a01092 at b01092
.field public static int16 a01093 at b01093
.field private static int16 aALIGN101093 at bALIGN101093
.field private static int32 aALIGN201093 at bALIGN201093
.field public static int8 a01094 at b01094
.field private static int32 aALIGN101094 at bALIGN101094
.field private static int16 aALIGN201094 at bALIGN201094
.field private static int8 aALIGN201094 at bALIGN301094
.field public static float32 a01095 at b01095
.field private static int32 aALIGN101095 at bALIGN101095
.field public static int16 a01096 at b01096
.field private static int16 aALIGN101096 at bALIGN101096
.field private static int32 aALIGN201096 at bALIGN201096
.field public static int64 a01097 at b01097
.field public static float32 a01098 at b01098
.field private static int32 aALIGN101098 at bALIGN101098
.field public static int32 a01099 at b01099
.field private static int32 aALIGN101099 at bALIGN101099
.field public static int32 a010100 at b010100
.field private static int32 aALIGN1010100 at bALIGN1010100
.field public static int32 a010101 at b010101
.field private static int32 aALIGN1010101 at bALIGN1010101
.field public static int8 a010102 at b010102
.field private static int32 aALIGN1010102 at bALIGN1010102
.field private static int16 aALIGN2010102 at bALIGN2010102
.field private static int8 aALIGN2010102 at bALIGN3010102
.field public static int16 a010103 at b010103
.field private static int16 aALIGN1010103 at bALIGN1010103
.field private static int32 aALIGN2010103 at bALIGN2010103
.field public static int8 a010104 at b010104
.field private static int32 aALIGN1010104 at bALIGN1010104
.field private static int16 aALIGN2010104 at bALIGN2010104
.field private static int8 aALIGN2010104 at bALIGN3010104
.field public static float32 a010105 at b010105
.field private static int32 aALIGN1010105 at bALIGN1010105
.field public static float32 a010106 at b010106
.field private static int32 aALIGN1010106 at bALIGN1010106
.field public static int8 a010107 at b010107
.field private static int32 aALIGN1010107 at bALIGN1010107
.field private static int16 aALIGN2010107 at bALIGN2010107
.field private static int8 aALIGN2010107 at bALIGN3010107
.field public static float32 a010108 at b010108
.field private static int32 aALIGN1010108 at bALIGN1010108
.field public static int16 a010109 at b010109
.field private static int16 aALIGN1010109 at bALIGN1010109
.field private static int32 aALIGN2010109 at bALIGN2010109
.field public static int8 a010110 at b010110
.field private static int32 aALIGN1010110 at bALIGN1010110
.field private static int16 aALIGN2010110 at bALIGN2010110
.field private static int8 aALIGN2010110 at bALIGN3010110
.field public static int64 a010111 at b010111
.field public static int16 a010112 at b010112
.field private static int16 aALIGN1010112 at bALIGN1010112
.field private static int32 aALIGN2010112 at bALIGN2010112
.field public static int16 a010113 at b010113
.field private static int16 aALIGN1010113 at bALIGN1010113
.field private static int32 aALIGN2010113 at bALIGN2010113
.field public static int32 a010114 at b010114
.field private static int32 aALIGN1010114 at bALIGN1010114
.field public static int32 a010115 at b010115
.field private static int32 aALIGN1010115 at bALIGN1010115
.field public static int8 a010116 at b010116
.field private static int32 aALIGN1010116 at bALIGN1010116
.field private static int16 aALIGN2010116 at bALIGN2010116
.field private static int8 aALIGN2010116 at bALIGN3010116
.field public static int16 a010117 at b010117
.field private static int16 aALIGN1010117 at bALIGN1010117
.field private static int32 aALIGN2010117 at bALIGN2010117
.field public static int64 a010118 at b010118
.field public static int8 a010119 at b010119
.field private static int32 aALIGN1010119 at bALIGN1010119
.field private static int16 aALIGN2010119 at bALIGN2010119
.field private static int8 aALIGN2010119 at bALIGN3010119
.field public static int64 a010120 at b010120
.field public static int32 a010121 at b010121
.field private static int32 aALIGN1010121 at bALIGN1010121
.field public static int8 a010122 at b010122
.field private static int32 aALIGN1010122 at bALIGN1010122
.field private static int16 aALIGN2010122 at bALIGN2010122
.field private static int8 aALIGN2010122 at bALIGN3010122
.field public static int32 a010123 at b010123
.field private static int32 aALIGN1010123 at bALIGN1010123
.field public static int8 a010124 at b010124
.field private static int32 aALIGN1010124 at bALIGN1010124
.field private static int16 aALIGN2010124 at bALIGN2010124
.field private static int8 aALIGN2010124 at bALIGN3010124
.field public static int64 a010125 at b010125
.field public static float32 a010126 at b010126
.field private static int32 aALIGN1010126 at bALIGN1010126
.field public static int64 a010127 at b010127
  }
.data b0100 = int64(0)
.data b0101 = int64(1)
.data b0102 = float32(2.0)
.data bALIGN10102 = int32(0)
.data b0103 = int64(3)
.data b0104 = int64(4)
.data b0105 = int16(5)
.data bALIGN10105 = int16(0)
.data bALIGN20105 = int32(0)
.data b0106 = int16(6)
.data bALIGN10106 = int16(0)
.data bALIGN20106 = int32(0)
.data b0107 = float32(7.0)
.data bALIGN10107 = int32(0)
.data b0108 = int32(8)
.data bALIGN10108 = int32(0)
.data b0109 = int8(9)
.data bALIGN10109 = int32(0)
.data bALIGN20109 = int16(0)
.data bALIGN30109 = int8(0)
.data b01010 = int32(10)
.data bALIGN101010 = int32(0)
.data b01011 = int64(11)
.data b01012 = int32(12)
.data bALIGN101012 = int32(0)
.data b01013 = int8(13)
.data bALIGN101013 = int32(0)
.data bALIGN201013 = int16(0)
.data bALIGN301013 = int8(0)
.data b01014 = int16(14)
.data bALIGN101014 = int16(0)
.data bALIGN201014 = int32(0)
.data b01015 = int16(15)
.data bALIGN101015 = int16(0)
.data bALIGN201015 = int32(0)
.data b01016 = float32(16.0)
.data bALIGN101016 = int32(0)
.data b01017 = float32(17.0)
.data bALIGN101017 = int32(0)
.data b01018 = int32(18)
.data bALIGN101018 = int32(0)
.data b01019 = int8(19)
.data bALIGN101019 = int32(0)
.data bALIGN201019 = int16(0)
.data bALIGN301019 = int8(0)
.data b01020 = int32(20)
.data bALIGN101020 = int32(0)
.data b01021 = int32(21)
.data bALIGN101021 = int32(0)
.data b01022 = int64(22)
.data b01023 = int32(23)
.data bALIGN101023 = int32(0)
.data b01024 = int8(24)
.data bALIGN101024 = int32(0)
.data bALIGN201024 = int16(0)
.data bALIGN301024 = int8(0)
.data b01025 = int8(25)
.data bALIGN101025 = int32(0)
.data bALIGN201025 = int16(0)
.data bALIGN301025 = int8(0)
.data b01026 = int16(26)
.data bALIGN101026 = int16(0)
.data bALIGN201026 = int32(0)
.data b01027 = int8(27)
.data bALIGN101027 = int32(0)
.data bALIGN201027 = int16(0)
.data bALIGN301027 = int8(0)
.data b01028 = int16(28)
.data bALIGN101028 = int16(0)
.data bALIGN201028 = int32(0)
.data b01029 = int64(29)
.data b01030 = int32(30)
.data bALIGN101030 = int32(0)
.data b01031 = int32(31)
.data bALIGN101031 = int32(0)
.data b01032 = int32(32)
.data bALIGN101032 = int32(0)
.data b01033 = int8(33)
.data bALIGN101033 = int32(0)
.data bALIGN201033 = int16(0)
.data bALIGN301033 = int8(0)
.data b01034 = int16(34)
.data bALIGN101034 = int16(0)
.data bALIGN201034 = int32(0)
.data b01035 = int32(35)
.data bALIGN101035 = int32(0)
.data b01036 = int32(36)
.data bALIGN101036 = int32(0)
.data b01037 = int16(37)
.data bALIGN101037 = int16(0)
.data bALIGN201037 = int32(0)
.data b01038 = float32(38.0)
.data bALIGN101038 = int32(0)
.data b01039 = int8(39)
.data bALIGN101039 = int32(0)
.data bALIGN201039 = int16(0)
.data bALIGN301039 = int8(0)
.data b01040 = int8(40)
.data bALIGN101040 = int32(0)
.data bALIGN201040 = int16(0)
.data bALIGN301040 = int8(0)
.data b01041 = float32(41.0)
.data bALIGN101041 = int32(0)
.data b01042 = int32(42)
.data bALIGN101042 = int32(0)
.data b01043 = int32(43)
.data bALIGN101043 = int32(0)
.data b01044 = int32(44)
.data bALIGN101044 = int32(0)
.data b01045 = int64(45)
.data b01046 = int64(46)
.data b01047 = int64(47)
.data b01048 = float32(48.0)
.data bALIGN101048 = int32(0)
.data b01049 = int64(49)
.data b01050 = int32(50)
.data bALIGN101050 = int32(0)
.data b01051 = float32(51.0)
.data bALIGN101051 = int32(0)
.data b01052 = int32(52)
.data bALIGN101052 = int32(0)
.data b01053 = int64(53)
.data b01054 = int8(54)
.data bALIGN101054 = int32(0)
.data bALIGN201054 = int16(0)
.data bALIGN301054 = int8(0)
.data b01055 = int8(55)
.data bALIGN101055 = int32(0)
.data bALIGN201055 = int16(0)
.data bALIGN301055 = int8(0)
.data b01056 = float32(56.0)
.data bALIGN101056 = int32(0)
.data b01057 = int32(57)
.data bALIGN101057 = int32(0)
.data b01058 = int64(58)
.data b01059 = int64(59)
.data b01060 = int8(60)
.data bALIGN101060 = int32(0)
.data bALIGN201060 = int16(0)
.data bALIGN301060 = int8(0)
.data b01061 = int16(61)
.data bALIGN101061 = int16(0)
.data bALIGN201061 = int32(0)
.data b01062 = int64(62)
.data b01063 = float32(63.0)
.data bALIGN101063 = int32(0)
.data b01064 = int64(64)
.data b01065 = float32(65.0)
.data bALIGN101065 = int32(0)
.data b01066 = int8(66)
.data bALIGN101066 = int32(0)
.data bALIGN201066 = int16(0)
.data bALIGN301066 = int8(0)
.data b01067 = int16(67)
.data bALIGN101067 = int16(0)
.data bALIGN201067 = int32(0)
.data b01068 = int64(68)
.data b01069 = int64(69)
.data b01070 = int64(70)
.data b01071 = int32(71)
.data bALIGN101071 = int32(0)
.data b01072 = int8(72)
.data bALIGN101072 = int32(0)
.data bALIGN201072 = int16(0)
.data bALIGN301072 = int8(0)
.data b01073 = int32(73)
.data bALIGN101073 = int32(0)
.data b01074 = float32(74.0)
.data bALIGN101074 = int32(0)
.data b01075 = int64(75)
.data b01076 = int8(76)
.data bALIGN101076 = int32(0)
.data bALIGN201076 = int16(0)
.data bALIGN301076 = int8(0)
.data b01077 = int8(77)
.data bALIGN101077 = int32(0)
.data bALIGN201077 = int16(0)
.data bALIGN301077 = int8(0)
.data b01078 = int8(78)
.data bALIGN101078 = int32(0)
.data bALIGN201078 = int16(0)
.data bALIGN301078 = int8(0)
.data b01079 = int32(79)
.data bALIGN101079 = int32(0)
.data b01080 = int16(80)
.data bALIGN101080 = int16(0)
.data bALIGN201080 = int32(0)
.data b01081 = int32(81)
.data bALIGN101081 = int32(0)
.data b01082 = int8(82)
.data bALIGN101082 = int32(0)
.data bALIGN201082 = int16(0)
.data bALIGN301082 = int8(0)
.data b01083 = int32(83)
.data bALIGN101083 = int32(0)
.data b01084 = int8(84)
.data bALIGN101084 = int32(0)
.data bALIGN201084 = int16(0)
.data bALIGN301084 = int8(0)
.data b01085 = int32(85)
.data bALIGN101085 = int32(0)
.data b01086 = int8(86)
.data bALIGN101086 = int32(0)
.data bALIGN201086 = int16(0)
.data bALIGN301086 = int8(0)
.data b01087 = int64(87)
.data b01088 = int8(88)
.data bALIGN101088 = int32(0)
.data bALIGN201088 = int16(0)
.data bALIGN301088 = int8(0)
.data b01089 = int16(89)
.data bALIGN101089 = int16(0)
.data bALIGN201089 = int32(0)
.data b01090 = int64(90)
.data b01091 = int8(91)
.data bALIGN101091 = int32(0)
.data bALIGN201091 = int16(0)
.data bALIGN301091 = int8(0)
.data b01092 = int64(92)
.data b01093 = int16(93)
.data bALIGN101093 = int16(0)
.data bALIGN201093 = int32(0)
.data b01094 = int8(94)
.data bALIGN101094 = int32(0)
.data bALIGN201094 = int16(0)
.data bALIGN301094 = int8(0)
.data b01095 = float32(95.0)
.data bALIGN101095 = int32(0)
.data b01096 = int16(96)
.data bALIGN101096 = int16(0)
.data bALIGN201096 = int32(0)
.data b01097 = int64(97)
.data b01098 = float32(98.0)
.data bALIGN101098 = int32(0)
.data b01099 = int32(99)
.data bALIGN101099 = int32(0)
.data b010100 = int32(100)
.data bALIGN1010100 = int32(0)
.data b010101 = int32(101)
.data bALIGN1010101 = int32(0)
.data b010102 = int8(102)
.data bALIGN1010102 = int32(0)
.data bALIGN2010102 = int16(0)
.data bALIGN3010102 = int8(0)
.data b010103 = int16(103)
.data bALIGN1010103 = int16(0)
.data bALIGN2010103 = int32(0)
.data b010104 = int8(104)
.data bALIGN1010104 = int32(0)
.data bALIGN2010104 = int16(0)
.data bALIGN3010104 = int8(0)
.data b010105 = float32(105.0)
.data bALIGN1010105 = int32(0)
.data b010106 = float32(106.0)
.data bALIGN1010106 = int32(0)
.data b010107 = int8(107)
.data bALIGN1010107 = int32(0)
.data bALIGN2010107 = int16(0)
.data bALIGN3010107 = int8(0)
.data b010108 = float32(108.0)
.data bALIGN1010108 = int32(0)
.data b010109 = int16(109)
.data bALIGN1010109 = int16(0)
.data bALIGN2010109 = int32(0)
.data b010110 = int8(110)
.data bALIGN1010110 = int32(0)
.data bALIGN2010110 = int16(0)
.data bALIGN3010110 = int8(0)
.data b010111 = int64(111)
.data b010112 = int16(112)
.data bALIGN1010112 = int16(0)
.data bALIGN2010112 = int32(0)
.data b010113 = int16(113)
.data bALIGN1010113 = int16(0)
.data bALIGN2010113 = int32(0)
.data b010114 = int32(114)
.data bALIGN1010114 = int32(0)
.data b010115 = int32(115)
.data bALIGN1010115 = int32(0)
.data b010116 = int8(116)
.data bALIGN1010116 = int32(0)
.data bALIGN2010116 = int16(0)
.data bALIGN3010116 = int8(0)
.data b010117 = int16(117)
.data bALIGN1010117 = int16(0)
.data bALIGN2010117 = int32(0)
.data b010118 = int64(118)
.data b010119 = int8(119)
.data bALIGN1010119 = int32(0)
.data bALIGN2010119 = int16(0)
.data bALIGN3010119 = int8(0)
.data b010120 = int64(120)
.data b010121 = int32(121)
.data bALIGN1010121 = int32(0)
.data b010122 = int8(2)
.data bALIGN1010122 = int32(0)
.data bALIGN2010122 = int16(0)
.data bALIGN3010122 = int8(0)
.data b010123 = int32(123)
.data bALIGN1010123 = int32(0)
.data b010124 = int8(4)
.data bALIGN1010124 = int32(0)
.data bALIGN2010124 = int16(0)
.data bALIGN3010124 = int8(0)
.data b010125 = int64(125)
.data b010126 = float32(126.0)
.data bALIGN1010126 = int32(0)
.data b010127 = int64(127)
