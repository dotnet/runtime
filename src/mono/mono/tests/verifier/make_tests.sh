#! /bin/sh

# Stack Size Tests
for OP in 'starg.s 0'
do
  ./make_stack_0_test.sh invalid "$OP"
done

for OP in 'stloc.0' 'stloc.s 0' 'stfld int32 Class::fld' pop ret
do
  ./make_stack_0_test.sh invalid "$OP"
done

for OP in add and 'box [mscorlib]System.Int32' 'brfalse branch_target' ceq cgt clt conv.i4 conv.r8 div dup 'ldfld int32 Class::fld' 'ldflda int32 Class::fld' mul not or rem shl shr sub xor
do
  ./make_stack_0_pop_test.sh "$OP"
done

for OP in add and ceq cgt clt div dup mul or rem shl shr sub xor 'stfld int32 Class::fld'
do
  ./make_stack_1_pop_test.sh "$OP" int32
done

# Table 2: Binary Numeric Operators
I=1
for OP in add div mul rem sub
do
  if [ "$OP" = "div" ] || [ "$OP" = "rem" ]; then
  	INIT="yes";
  else
  	INIT="no";
  fi

  ./make_bin_test.sh bin_num_op_32_${I} valid $OP int32 int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_33_${I} valid $OP int32 'native int' "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_34_${I} valid $OP int64 int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_35_${I} valid $OP 'native int' int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_36_${I} valid $OP 'native int' 'native int' "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_37_${I} valid $OP float64 float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_38_${I} valid $OP float32 float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_39_${I} valid $OP float64 float32 "ldc.r4 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_40_${I} valid $OP float32 float32 "ldc.r4 0" "${INIT}"

  ./make_bin_test.sh bin_num_op_1_${I} unverifiable $OP int32 int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_2_${I} unverifiable $OP int32 float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_3_${I} unverifiable $OP int32 object "ldnull" "${INIT}"

  ./make_bin_test.sh bin_num_op_4_${I} unverifiable $OP int64 int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_5_${I} unverifiable $OP int64 'native int' "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_6_${I} unverifiable $OP int64 float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_7_${I} unverifiable $OP int64 'int64&' "ldnull" "${INIT}"
  ./make_bin_test.sh bin_num_op_8_${I} unverifiable $OP int64 object "ldnull" "${INIT}"

  ./make_bin_test.sh bin_num_op_9_${I} unverifiable $OP 'native int' int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_10_${I} unverifiable $OP 'native int' float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_11_${I} unverifiable $OP 'native int' object "ldnull" "${INIT}"

  ./make_bin_test.sh bin_num_op_12_${I} unverifiable $OP float64 int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_13_${I} unverifiable $OP float64 int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_14_${I} unverifiable $OP float64 'native int' "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_15_${I} unverifiable $OP float64 'float64&' "ldnull" "${INIT}"
  ./make_bin_test.sh bin_num_op_16_${I} unverifiable $OP float64 object "ldnull" "${INIT}"

  ./make_bin_test.sh bin_num_op_17_${I} unverifiable $OP 'int64&' int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_18_${I} unverifiable $OP 'float64&' float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_19_${I} unverifiable $OP 'object&' object "ldnull" "${INIT}"

  ./make_bin_test.sh bin_num_op_20_${I} unverifiable $OP object int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_21_${I} unverifiable $OP object int64 "ldc.i8 1" "${INIT}"
  ./make_bin_test.sh bin_num_op_22_${I} unverifiable $OP object 'native int' "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_23_${I} unverifiable $OP object float64 "ldc.r8 0" "${INIT}"
  ./make_bin_test.sh bin_num_op_24_${I} unverifiable $OP object 'object&' "ldnull" "${INIT}"
  ./make_bin_test.sh bin_num_op_25_${I} unverifiable $OP object object "ldnull" "${INIT}"
  I=`expr $I + 1`
done

I=1
for OP in div mul rem sub
do
  ./make_bin_test.sh bin_num_op_26_${I} unverifiable $OP int32 'int32&'
  ./make_bin_test.sh bin_num_op_27_${I} unverifiable $OP 'native int' 'native int&'
  I=`expr $I + 1`
done

for OP in add
do
  ./make_bin_test.sh bin_num_op_26_${I} unverifiable $OP int32 'int32&'
  ./make_bin_test.sh bin_num_op_27_${I} unverifiable $OP 'native int' 'native int&'
  I=`expr $I + 1`
done

I=1
for OP in div mul rem
do
  if [ "$OP" = "div" ] || [ "$OP" = "div" ]; then
  	INIT="yes";
  else
  	INIT="no";
  fi
  ./make_bin_test.sh bin_num_op_28_${I} unverifiable $OP 'int32&' int32 "ldc.i4.1" "${INIT}"
  ./make_bin_test.sh bin_num_op_29_${I} unverifiable $OP 'native int&' 'native int' "ldc.i4.1" "${INIT}"
  I=`expr $I + 1`
done

for OP in add sub
do
  ./make_bin_test.sh bin_num_op_28_${I} unverifiable $OP 'int32&' int32
  ./make_bin_test.sh bin_num_op_29_${I} unverifiable $OP 'native int&' 'native int'
  I=`expr $I + 1`
done

I=1
for OP in div mul rem add
do
  if [ "$OP" = "div" ] || [ "$OP" = "div" ]; then
  	INIT="yes";
  else
  	INIT="no";
  fi
  ./make_bin_test.sh bin_num_op_30_${I} unverifiable $OP 'int32&' 'int32&' "ldnull" "${INIT}"
  I=`expr $I + 1`
done

for OP in sub
do
  ./make_bin_test.sh bin_num_op_30_${I} unverifiable $OP 'int32&' 'int32&'
  I=`expr $I + 1`
done

# Table 4: Binary Comparison or Branch Operations
I=1
for OP in ceq cgt clt
do
  ./make_bin_test.sh bin_comp_op_1_${I} unverifiable $OP int32 int64
  ./make_bin_test.sh bin_comp_op_2_${I} unverifiable $OP int32 float64
  ./make_bin_test.sh bin_comp_op_3_${I} unverifiable $OP int32 'int32&'
  ./make_bin_test.sh bin_comp_op_4_${I} unverifiable $OP int32 object

  ./make_bin_test.sh bin_comp_op_5_${I} unverifiable $OP int64 int32
  ./make_bin_test.sh bin_comp_op_6_${I} unverifiable $OP int64 'native int'
  ./make_bin_test.sh bin_comp_op_7_${I} unverifiable $OP int64 float64
  ./make_bin_test.sh bin_comp_op_8_${I} unverifiable $OP int64 'int64&'
  ./make_bin_test.sh bin_comp_op_9_${I} unverifiable $OP int64 object

  ./make_bin_test.sh bin_comp_op_10_${I} unverifiable $OP 'native int' int64
  ./make_bin_test.sh bin_comp_op_11_${I} unverifiable $OP 'native int' float64
  ./make_bin_test.sh bin_comp_op_12_${I} unverifiable $OP 'native int' object

  ./make_bin_test.sh bin_comp_op_13_${I} unverifiable $OP float64 int32
  ./make_bin_test.sh bin_comp_op_14_${I} unverifiable $OP float64 int64
  ./make_bin_test.sh bin_comp_op_15_${I} unverifiable $OP float64 'native int'
  ./make_bin_test.sh bin_comp_op_16_${I} unverifiable $OP float64 'float64&'
  ./make_bin_test.sh bin_comp_op_17_${I} unverifiable $OP float64 object

  ./make_bin_test.sh bin_comp_op_18_${I} unverifiable $OP 'int32&' int32
  ./make_bin_test.sh bin_comp_op_19_${I} unverifiable $OP 'int64&' int64
  ./make_bin_test.sh bin_comp_op_20_${I} unverifiable $OP 'float64&' float64
  ./make_bin_test.sh bin_comp_op_21_${I} unverifiable $OP 'object&' object

  ./make_bin_test.sh bin_comp_op_22_${I} unverifiable $OP object int32
  ./make_bin_test.sh bin_comp_op_23_${I} unverifiable $OP object int64
  ./make_bin_test.sh bin_comp_op_24_${I} unverifiable $OP object 'native int'
  ./make_bin_test.sh bin_comp_op_25_${I} unverifiable $OP object float64
  ./make_bin_test.sh bin_comp_op_26_${I} unverifiable $OP object 'object&'
  I=`expr $I + 1`
done

I=1
for OP in cgt clt
do
  ./make_bin_test.sh bin_comp_op_27_${I} unverifiable $OP 'native int' 'native int&'
  ./make_bin_test.sh bin_comp_op_28_${I} unverifiable $OP 'native int&' 'native int'
  ./make_bin_test.sh bin_comp_op_29_${I} unverifiable $OP object object
  I=`expr $I + 1`
done

#tests for the difference between cgt.un and others
I=1
for TYPE in string object
do
	./make_bin_test.sh bin_cgt_un_a_${I} valid 'cgt.un' "${TYPE}" 'object'
	./make_bin_test.sh bin_cgt_un_b_${I} valid 'cgt.un' 'object' "${TYPE}"
  I=`expr $I + 1`
done


for TYPE in int32 float32 int64 "int32&" "native int"
do
	./make_bin_test.sh bin_cgt_un_a_${I} unverifiable 'cgt.un' "${TYPE}" 'object'
	./make_bin_test.sh bin_cgt_un_b_${I} unverifiable 'cgt.un' 'object' "${TYPE}"
  I=`expr $I + 1`
done

for OP in ceq
do
  ./make_bin_test.sh bin_comp_op_27_${I} unverifiable $OP 'native int' 'native int&'
  ./make_bin_test.sh bin_comp_op_28_${I} unverifiable $OP 'native int&' 'native int'
  I=`expr $I + 1`
done

# Table 5: Integer Operations
I=1
for OP in and or xor
do
  ./make_bin_test.sh bin_int_op_1_${I} unverifiable "$OP" int32 int64
  ./make_bin_test.sh bin_int_op_2_${I} unverifiable "$OP" int32 float64
  ./make_bin_test.sh bin_int_op_3_${I} unverifiable "$OP" int32 'int32&'
  ./make_bin_test.sh bin_int_op_4_${I} unverifiable "$OP" int32 object

  ./make_bin_test.sh bin_int_op_5_${I} unverifiable "$OP" int64 int32
  ./make_bin_test.sh bin_int_op_6_${I} unverifiable "$OP" int64 'native int'
  ./make_bin_test.sh bin_int_op_7_${I} unverifiable "$OP" int64 float64
  ./make_bin_test.sh bin_int_op_8_${I} unverifiable "$OP" int64 'int64&'
  ./make_bin_test.sh bin_int_op_9_${I} unverifiable "$OP" int64 object

  ./make_bin_test.sh bin_int_op_10_${I} unverifiable "$OP" 'native int' int64
  ./make_bin_test.sh bin_int_op_11_${I} unverifiable "$OP" 'native int' float64
  ./make_bin_test.sh bin_int_op_12_${I} unverifiable "$OP" 'native int' 'native int&'
  ./make_bin_test.sh bin_int_op_13_${I} unverifiable "$OP" 'native int' object

  ./make_bin_test.sh bin_int_op_14_${I} unverifiable "$OP" float64 int32
  ./make_bin_test.sh bin_int_op_15_${I} unverifiable "$OP" float64 int64
  ./make_bin_test.sh bin_int_op_16_${I} unverifiable "$OP" float64 'native int'
  ./make_bin_test.sh bin_int_op_17_${I} unverifiable "$OP" float64 float64
  ./make_bin_test.sh bin_int_op_18_${I} unverifiable "$OP" float64 'int32&'
  ./make_bin_test.sh bin_int_op_19_${I} unverifiable "$OP" float64 object

  ./make_bin_test.sh bin_int_op_20_${I} unverifiable "$OP" 'int32&' int32
  ./make_bin_test.sh bin_int_op_21_${I} unverifiable "$OP" 'int64&' int64
  ./make_bin_test.sh bin_int_op_22_${I} unverifiable "$OP" 'native int&' 'native int'
  ./make_bin_test.sh bin_int_op_23_${I} unverifiable "$OP" 'float64&' float64
  ./make_bin_test.sh bin_int_op_24_${I} unverifiable "$OP" 'int32&' 'int32&'
  ./make_bin_test.sh bin_int_op_25_${I} unverifiable "$OP" 'float64&' object

  ./make_bin_test.sh bin_int_op_26_${I} unverifiable "$OP" object int32
  ./make_bin_test.sh bin_int_op_27_${I} unverifiable "$OP" object int64
  ./make_bin_test.sh bin_int_op_28_${I} unverifiable "$OP" object 'native int'
  ./make_bin_test.sh bin_int_op_29_${I} unverifiable "$OP" object float64
  ./make_bin_test.sh bin_int_op_30_${I} unverifiable "$OP" object 'int32&'
  ./make_bin_test.sh bin_int_op_31_${I} unverifiable "$OP" object object
  I=`expr $I + 1`
done

I=1
for TYPE in bool int8 int16 char int32 int64 'native int' 'class Int8Enum'
do
  ./make_unary_test.sh not_${I} valid "not\n\tpop" "$TYPE"
  I=`expr $I + 1`
done

for TYPE in 'int32&' object "class MyStruct" typedref "method int32 *(int32)" 'class Template\`1<int32>' float32 float64
do
  ./make_unary_test.sh not_${I} unverifiable "not\n\tpop" "$TYPE"
  I=`expr $I + 1`
done

I=1
for TYPE in bool int8 int16 char int32 int64 'native int' 'class Int8Enum' float32 float64
do
  ./make_unary_test.sh neg_${I} valid "neg\n\tpop" "$TYPE"
  I=`expr $I + 1`
done

for TYPE in 'int32&' object "class MyStruct" typedref "method int32 *(int32)" 'class Template\`1<int32>'
do
  ./make_unary_test.sh neg_${I} unverifiable "neg\n\tpop" "$TYPE"
  I=`expr $I + 1`
done


# Table 6: Shift Operators
I=1
for OP in shl shr
do
  ./make_bin_test.sh shift_op_1_${I} unverifiable $OP int32 int64
  ./make_bin_test.sh shift_op_2_${I} unverifiable $OP int32 float64
  ./make_bin_test.sh shift_op_3_${I} unverifiable $OP int32 'int32&'
  ./make_bin_test.sh shift_op_4_${I} unverifiable $OP int32 object

  ./make_bin_test.sh shift_op_5_${I} unverifiable $OP int64 int64
  ./make_bin_test.sh shift_op_6_${I} unverifiable $OP int64 float64
  ./make_bin_test.sh shift_op_7_${I} unverifiable $OP int64 'int32&'
  ./make_bin_test.sh shift_op_8_${I} unverifiable $OP int64 object

  ./make_bin_test.sh shift_op_9_${I} unverifiable $OP 'native int' int64
  ./make_bin_test.sh shift_op_10_${I} unverifiable $OP 'native int' float64
  ./make_bin_test.sh shift_op_11_${I} unverifiable $OP 'native int' 'native int&'
  ./make_bin_test.sh shift_op_12_${I} unverifiable $OP 'native int' object

  ./make_bin_test.sh shift_op_13_${I} unverifiable $OP float64 int32
  ./make_bin_test.sh shift_op_14_${I} unverifiable $OP float64 int64
  ./make_bin_test.sh shift_op_15_${I} unverifiable $OP float64 'native int'
  ./make_bin_test.sh shift_op_16_${I} unverifiable $OP float64 float64
  ./make_bin_test.sh shift_op_17_${I} unverifiable $OP float64 'int32&'
  ./make_bin_test.sh shift_op_18_${I} unverifiable $OP float64 object

  ./make_bin_test.sh shift_op_19_${I} unverifiable $OP 'int32&' int32
  ./make_bin_test.sh shift_op_20_${I} unverifiable $OP 'int64&' int64
  ./make_bin_test.sh shift_op_21_${I} unverifiable $OP 'native int&' 'native int'
  ./make_bin_test.sh shift_op_22_${I} unverifiable $OP 'float64&' float64
  ./make_bin_test.sh shift_op_23_${I} unverifiable $OP 'int32&' 'int32&'
  ./make_bin_test.sh shift_op_24_${I} unverifiable $OP 'float64&' object

  ./make_bin_test.sh shift_op_25_${I} unverifiable $OP object int32
  ./make_bin_test.sh shift_op_26_${I} unverifiable $OP object int64
  ./make_bin_test.sh shift_op_27_${I} unverifiable $OP object 'native int'
  ./make_bin_test.sh shift_op_28_${I} unverifiable $OP object float64
  ./make_bin_test.sh shift_op_29_${I} unverifiable $OP object 'int32&'
  ./make_bin_test.sh shift_op_30_${I} unverifiable $OP object object
  I=`expr $I + 1`
done

# Table 8: Conversion Operations
I=1
J=1
for OP in "conv.i1\n\tpop" "conv.i2\n\tpop" "conv.i4\n\tpop" "conv.i8\n\tpop" "conv.r4\n\tpop" "conv.r8\n\tpop" "conv.u1\n\tpop" "conv.u2\n\tpop" "conv.u4\n\tpop" "conv.u8\n\tpop" "conv.i\n\tpop" "conv.u\n\tpop" "conv.r.un\n\tpop" "conv.ovf.i1\n\tpop" "conv.ovf.i2\n\tpop" "conv.ovf.i4\n\tpop" "conv.ovf.i8\n\tpop" "conv.ovf.u1\n\tpop" "conv.ovf.u2\n\tpop" "conv.ovf.u4\n\tpop" "conv.ovf.u8\n\tpop" "conv.ovf.i\n\tpop"  "conv.ovf.u\n\tpop" "conv.ovf.i1.un\n\tpop" "conv.ovf.i2.un\n\tpop" "conv.ovf.i4.un\n\tpop" "conv.ovf.i8.un\n\tpop" "conv.ovf.u1.un\n\tpop" "conv.ovf.u2.un\n\tpop" "conv.ovf.u4.un\n\tpop" "conv.ovf.u8.un\n\tpop" "conv.ovf.i.un\n\tpop"  "conv.ovf.u.un\n\tpop"
do
  for TYPE in 'int8' 'bool' 'unsigned int8' 'int16' 'char' 'unsigned int16' 'int32' 'unsigned int32' 'int64' 'unsigned int64' 'float32' 'float64' 'native int' 'native unsigned int'
  do
    ./make_unary_test.sh conv_op_${J}_${I} valid $OP "$TYPE"
    I=`expr $I + 1`
  done

  for TYPE in 'object' 'string' 'class Class' 'valuetype MyStruct' 'int32[]' 'int32[,]' 'typedref' 'int32*' 'method int32 *(int32)' 'class Template`1<object>' 'int8&' 'bool&' 'unsigned int8&' 'int16&' 'char&' 'unsigned int16&' 'int32&' 'unsigned int32&' 'int64&' 'unsigned int64&' 'float32&' 'float64&' 'native int&' 'native unsigned int&' 'object&' 'string&' 'class Class&' 'valuetype MyStruct&' 'int32[]&' 'int32[,]&' 'class Template`1<object>&'
  do
    ./make_unary_test.sh conv_op_${J}_${I} unverifiable $OP "$TYPE"
    I=`expr $I + 1`
  done

  ./make_unary_test.sh conv_op_${J}_${I} invalid $OP "typedref&"
  J=`expr $J + 1`
  I=1
done



#local and argument store with invalid values lead to unverifiable code
I=1
for OP in stloc.0 "stloc.s 0" "starg 0" "starg.s 0"
do
  ./make_store_test.sh coercion_1_${I} unverifiable "$OP" int8 int64
  ./make_store_test.sh coercion_2_${I} unverifiable "$OP" int8 float64
  ./make_store_test.sh coercion_3_${I} unverifiable "$OP" int8 'int8&'
  ./make_store_test.sh coercion_4_${I} unverifiable "$OP" int8 object

  ./make_store_test.sh coercion_5_${I} unverifiable "$OP" 'unsigned int8' int64
  ./make_store_test.sh coercion_6_${I} unverifiable "$OP" 'unsigned int8' float64
  ./make_store_test.sh coercion_7_${I} unverifiable "$OP" 'unsigned int8' 'unsigned int8&'
  ./make_store_test.sh coercion_8_${I} unverifiable "$OP" 'unsigned int8' object

  ./make_store_test.sh coercion_9_${I} unverifiable "$OP" bool int64
  ./make_store_test.sh coercion_10_${I} unverifiable "$OP" bool float64
  ./make_store_test.sh coercion_11_${I} unverifiable "$OP" bool 'bool&'
  ./make_store_test.sh coercion_12_${I} unverifiable "$OP" bool object

  ./make_store_test.sh coercion_13_${I} unverifiable "$OP" int16 int64
  ./make_store_test.sh coercion_14_${I} unverifiable "$OP" int16 float64
  ./make_store_test.sh coercion_15_${I} unverifiable "$OP" int16 'int16&'
  ./make_store_test.sh coercion_16_${I} unverifiable "$OP" int16 object

  ./make_store_test.sh coercion_17_${I} unverifiable "$OP" 'unsigned int16' int64
  ./make_store_test.sh coercion_18_${I} unverifiable "$OP" 'unsigned int16' float64
  ./make_store_test.sh coercion_19_${I} unverifiable "$OP" 'unsigned int16' 'unsigned int16&'
  ./make_store_test.sh coercion_20_${I} unverifiable "$OP" 'unsigned int16' object

  ./make_store_test.sh coercion_21_${I} unverifiable "$OP" char int64
  ./make_store_test.sh coercion_22_${I} unverifiable "$OP" char float64
  ./make_store_test.sh coercion_23_${I} unverifiable "$OP" char 'char&'
  ./make_store_test.sh coercion_24_${I} unverifiable "$OP" char object

  ./make_store_test.sh coercion_25_${I} unverifiable "$OP" int32 int64
  ./make_store_test.sh coercion_26_${I} unverifiable "$OP" int32 float64
  ./make_store_test.sh coercion_27_${I} unverifiable "$OP" int32 'int32&'
  ./make_store_test.sh coercion_28_${I} unverifiable "$OP" int32 object

  ./make_store_test.sh coercion_29_${I} unverifiable "$OP" 'unsigned int32' int64
  ./make_store_test.sh coercion_30_${I} unverifiable "$OP" 'unsigned int32' float64
  ./make_store_test.sh coercion_31_${I} unverifiable "$OP" 'unsigned int32' 'unsigned int32&'
  ./make_store_test.sh coercion_32_${I} unverifiable "$OP" 'unsigned int32' object

  ./make_store_test.sh coercion_33_${I} unverifiable "$OP" int64 int32
  ./make_store_test.sh coercion_34_${I} unverifiable "$OP" int64 'native int'
  ./make_store_test.sh coercion_35_${I} unverifiable "$OP" int64 float64
  ./make_store_test.sh coercion_36_${I} unverifiable "$OP" int64 'int64&'
  ./make_store_test.sh coercion_37_${I} unverifiable "$OP" int64 object

  ./make_store_test.sh coercion_38_${I} unverifiable "$OP" 'unsigned int64' int32
  ./make_store_test.sh coercion_39_${I} unverifiable "$OP" 'unsigned int64' 'native int'
  ./make_store_test.sh coercion_40_${I} unverifiable "$OP" 'unsigned int64' float64
  ./make_store_test.sh coercion_41_${I} unverifiable "$OP" 'unsigned int64' 'unsigned int64&'
  ./make_store_test.sh coercion_42_${I} unverifiable "$OP" 'unsigned int64' object

  ./make_store_test.sh coercion_43_${I} unverifiable "$OP" 'native int' int64
  ./make_store_test.sh coercion_44_${I} unverifiable "$OP" 'native int' float64
  ./make_store_test.sh coercion_45_${I} unverifiable "$OP" 'native int' 'native int&'
  ./make_store_test.sh coercion_46_${I} unverifiable "$OP" 'native int' object

  ./make_store_test.sh coercion_47_${I} unverifiable "$OP" 'native unsigned int' int64
  ./make_store_test.sh coercion_48_${I} unverifiable "$OP" 'native unsigned int' float64
  ./make_store_test.sh coercion_49_${I} unverifiable "$OP" 'native unsigned int' 'native unsigned int&'
  ./make_store_test.sh coercion_50_${I} unverifiable "$OP" 'native unsigned int' object

  ./make_store_test.sh coercion_51_${I} unverifiable "$OP" float32 int32
  ./make_store_test.sh coercion_52_${I} unverifiable "$OP" float32 'native int'
  ./make_store_test.sh coercion_53_${I} unverifiable "$OP" float32 int64
  ./make_store_test.sh coercion_54_${I} unverifiable "$OP" float32 'float32&'
  ./make_store_test.sh coercion_55_${I} unverifiable "$OP" float32 object

  ./make_store_test.sh coercion_56_${I} unverifiable "$OP" float64 int32
  ./make_store_test.sh coercion_57_${I} unverifiable "$OP" float64 'native int'
  ./make_store_test.sh coercion_58_${I} unverifiable "$OP" float64 int64
  ./make_store_test.sh coercion_59_${I} unverifiable "$OP" float64 'float64&'
  ./make_store_test.sh coercion_60_${I} unverifiable "$OP" float64 object

  ./make_store_test.sh coercion_61_${I} unverifiable "$OP" object int32
  ./make_store_test.sh coercion_62_${I} unverifiable "$OP" object 'native int'
  ./make_store_test.sh coercion_63_${I} unverifiable "$OP" object int64
  ./make_store_test.sh coercion_64_${I} unverifiable "$OP" object float64
  ./make_store_test.sh coercion_65_${I} unverifiable "$OP" object 'object&'

  ./make_store_test.sh coercion_66_${I} unverifiable "$OP" 'class ValueType' int32
  ./make_store_test.sh coercion_67_${I} unverifiable "$OP" 'class ValueType' 'native int'
  ./make_store_test.sh coercion_68_${I} unverifiable "$OP" 'class ValueType' int64
  ./make_store_test.sh coercion_69_${I} unverifiable "$OP" 'class ValueType' float64
  ./make_store_test.sh coercion_70_${I} unverifiable "$OP" 'class ValueType' 'class ValueType&'
  ./make_store_test.sh coercion_71_${I} unverifiable "$OP" 'class ValueType' object

  ./make_store_test.sh coercion_72_${I} unverifiable "$OP" 'int32&' int32
  ./make_store_test.sh coercion_73_${I} unverifiable "$OP" 'int32&' 'native int'
  ./make_store_test.sh coercion_74_${I} unverifiable "$OP" 'int32&' int64
  ./make_store_test.sh coercion_75_${I} unverifiable "$OP" 'int32&' float64
  ./make_store_test.sh coercion_76_${I} unverifiable "$OP" 'int32&' object

  ./make_store_test.sh coercion_77_${I} unverifiable "$OP" typedref int32
  ./make_store_test.sh coercion_78_${I} unverifiable "$OP" typedref 'native int'
  ./make_store_test.sh coercion_89_${I} unverifiable "$OP" typedref int64
  ./make_store_test.sh coercion_80_${I} unverifiable "$OP" typedref float64
  ./make_store_test.sh coercion_81_${I} invalid "$OP" typedref 'typedref&'
  ./make_store_test.sh coercion_82_${I} unverifiable "$OP" typedref object
  I=`expr $I + 1`
done

#valid coercion between native int and int32
I=1
for OP in stloc.0 "starg 0"
do
	./make_store_test.sh coercion_83_${I} valid "$OP" int32 "native int"
 	./make_store_test.sh coercion_84_${I} valid "$OP" "native int" int32

	./make_store_test.sh coercion_85_${I} valid "$OP" "unsigned int32" "native int"
 	./make_store_test.sh coercion_86_${I} valid "$OP" "native int" "unsigned int32"

	./make_store_test.sh coercion_87_${I} valid "$OP" int32 "native unsigned int"
 	./make_store_test.sh coercion_88_${I} valid "$OP" "native unsigned int" int32

	./make_store_test.sh coercion_89_${I} valid "$OP" "unsigned int32" "native int"
 	./make_store_test.sh coercion_90_${I} valid "$OP" "native unsigned int" "unsigned int32"

	I=`expr $I + 1`
done

#test for unverifiable types

I=1
for OP in "stloc.0" "starg 0"
do
  ./make_store_test.sh misc_types_store_1_${I} valid "$OP" typedref typedref
  ./make_store_test.sh misc_types_store_2_${I} unverifiable "$OP" "int32*" "int32*"
  ./make_store_test.sh misc_types_store_3_${I} unverifiable "$OP" "method int32 *(int32)" "method int32 *(int32)"

  ./make_store_test.sh misc_types_store_4_${I} unverifiable "$OP" "method int32 *(int32)" "method void *(int32)"
  ./make_store_test.sh misc_types_store_5_${I} unverifiable "$OP" "int32*" "int8*"
  ./make_store_test.sh misc_types_store_6_${I} unverifiable "$OP" typedref "native int&"


  ./make_store_test.sh managed_pointer_store_1_${I} valid "$OP" "int32&" "int32&"
  ./make_store_test.sh managed_pointer_store_2_${I} valid "$OP" "int32&" "native int&"
  ./make_store_test.sh managed_pointer_store_3_${I} valid "$OP" "int32&" "unsigned int32&"
  ./make_store_test.sh managed_pointer_store_4_${I} valid "$OP" "native int&" "int32&"
  ./make_store_test.sh managed_pointer_store_5_${I} unverifiable "$OP" "int32&" "int16&"

  ./make_store_test.sh managed_pointer_store_6_${I} valid "$OP" "int8&" "unsigned int8&"
  ./make_store_test.sh managed_pointer_store_7_${I} valid "$OP" "int8&" "bool&"
  ./make_store_test.sh managed_pointer_store_8_${I} unverifiable "$OP" "int8&" "int16&"

  ./make_store_test.sh managed_pointer_store_9_${I} valid "$OP" "int16&" "unsigned int16&"
  ./make_store_test.sh managed_pointer_store_10_${I} valid "$OP" "int16&" "char&"
  ./make_store_test.sh managed_pointer_store_11_${I} unverifiable "$OP" "int16&" "int32&"

  ./make_store_test.sh managed_pointer_store_12_${I} unverifiable "$OP" "float32&" "float64&"
  ./make_store_test.sh managed_pointer_store_13_${I} unverifiable "$OP" "float64&" "float32&"

  I=`expr $I + 1`
done


function fix () {
	if [ -n "$3" ]; then
		A=$3;
	elif [ -n "$2" ]; then
		A=$2;
	else
		A=$1;
	fi

	if [ "$A" = "bool&" ]; then
		A="int8&";
	elif [ "$A" = "char&" ]; then
		A="int16&";
	fi

	echo "$A";
}

#Tests related to storing reference types on other reference types
I=1
for OP in stloc.0 "stloc.s 0" "starg 0" "starg.s 0"
do
	for TYPE1 in 'native int&' 'native unsigned int&'
	do
		for TYPE2 in 'int8&' 'unsigned int8&' 'bool&' 'int16&' 'unsigned int16&' 'char&' 'int32&' 'unsigned int32&' 'int64&' 'unsigned int64&' 'float32&' 'float64&' 'native int&' 'native unsigned int&'
		do
			TA="$(fix $TYPE1)"
			TB="$(fix $TYPE2)"
			if [ "$TA" = "$TB" ]; then
				./make_store_test.sh ref_coercion_${I} valid "$OP" "$TYPE1" "$TYPE2"
			elif [ "$TA" = "int32&" ] && [ "$TB" = "int&" ]; then
				./make_store_test.sh ref_coercion_${I} valid "$OP" "$TYPE1" "$TYPE2"
			elif [ "$TA" = "int&" ] && [ "$TB" = "int32&" ]; then
				./make_store_test.sh ref_coercion_${I} valid "$OP" "$TYPE1" "$TYPE2"
			else
				./make_store_test.sh ref_coercion_${I} unverifiable "$OP" "$TYPE1" "$TYPE2"
			fi
			I=`expr $I + 1`
		done
	done
done

I=1
for OP in stloc.0 "stloc.s 0" "starg 0" "starg.s 0"
do
	for TYPE1 in 'int8&' 'unsigned int8&' 'bool&' 'int16&' 'unsigned int16&' 'char&' 'int32&' 'unsigned int32&' 'int64&' 'unsigned int64&' 'float32&' 'float64&'
	do
		for TYPE2 in 'int8&' 'unsigned int8&' 'bool&' 'int16&' 'unsigned int16&' 'char&' 'int32&' 'unsigned int32&' 'int64&' 'unsigned int64&' 'float32&' 'float64&'
		do
			TA="$(fix $TYPE1)"
			TB="$(fix $TYPE2)"
			if [ "$TA" = "$TB" ]; then
				./make_store_test.sh ref_coercion_${I} valid "$OP" "$TYPE1" "$TYPE2"
			else
				./make_store_test.sh ref_coercion_${I} unverifiable "$OP" "$TYPE1" "$TYPE2"
			fi
			I=`expr $I + 1`
		done
	done
done

for OP in stloc.0 "stloc.s 0" "starg 0" "starg.s 0"
do
	for TYPE1 in 'class ClassA&' 'class ClassB&' 'class InterfaceA&' 'class InterfaceB&' 'class ValueType&'
	do
		for TYPE2 in 'class ClassA&' 'class ClassB&' 'class InterfaceA&' 'class InterfaceB&' 'class ValueType&'
		do
			if [ "$TYPE1" = "$TYPE2" ]; then
				./make_store_test.sh ref_coercion_${I} valid "$OP" "$TYPE1" "$TYPE2"
			else
				./make_store_test.sh ref_coercion_${I} unverifiable "$OP" "$TYPE1" "$TYPE2"
			fi
			I=`expr $I + 1`
		done
	done
done

#Field store parameter compatibility leads to invalid code
#Calling method with different verification types on stack lead to invalid code
I=1
for OP in "stfld TYPE1 Class::fld" "stsfld TYPE1 Class::sfld\n\tpop"  "call void Class::Method(TYPE1)"
do
  ./make_obj_store_test.sh obj_coercion_1_${I} unverifiable "$OP" int8 int64
  ./make_obj_store_test.sh obj_coercion_2_${I} unverifiable "$OP" int8 float64
  ./make_obj_store_test.sh obj_coercion_3_${I} unverifiable "$OP" int8 'int8&'
  ./make_obj_store_test.sh obj_coercion_4_${I} unverifiable "$OP" int8 object

  ./make_obj_store_test.sh obj_coercion_5_${I} unverifiable "$OP" 'unsigned int8' int64
  ./make_obj_store_test.sh obj_coercion_6_${I} unverifiable "$OP" 'unsigned int8' float64
  ./make_obj_store_test.sh obj_coercion_7_${I} unverifiable "$OP" 'unsigned int8' 'unsigned int8&'
  ./make_obj_store_test.sh obj_coercion_8_${I} unverifiable "$OP" 'unsigned int8' object

  ./make_obj_store_test.sh obj_coercion_9_${I} unverifiable "$OP" bool int64
  ./make_obj_store_test.sh obj_coercion_10_${I} unverifiable "$OP" bool float64
  ./make_obj_store_test.sh obj_coercion_11_${I} unverifiable "$OP" bool 'bool&'
  ./make_obj_store_test.sh obj_coercion_12_${I} unverifiable "$OP" bool object

  ./make_obj_store_test.sh obj_coercion_13_${I} unverifiable "$OP" int16 int64
  ./make_obj_store_test.sh obj_coercion_14_${I} unverifiable "$OP" int16 float64
  ./make_obj_store_test.sh obj_coercion_15_${I} unverifiable "$OP" int16 'int16&'
  ./make_obj_store_test.sh obj_coercion_16_${I} unverifiable "$OP" int16 object

  ./make_obj_store_test.sh obj_coercion_17_${I} unverifiable "$OP" 'unsigned int16' int64
  ./make_obj_store_test.sh obj_coercion_18_${I} unverifiable "$OP" 'unsigned int16' float64
  ./make_obj_store_test.sh obj_coercion_19_${I} unverifiable "$OP" 'unsigned int16' 'unsigned int16&'
  ./make_obj_store_test.sh obj_coercion_20_${I} unverifiable "$OP" 'unsigned int16' object

  ./make_obj_store_test.sh obj_coercion_21_${I} unverifiable "$OP" char int64
  ./make_obj_store_test.sh obj_coercion_22_${I} unverifiable "$OP" char float64
  ./make_obj_store_test.sh obj_coercion_23_${I} unverifiable "$OP" char 'char&'
  ./make_obj_store_test.sh obj_coercion_24_${I} unverifiable "$OP" char object

  ./make_obj_store_test.sh obj_coercion_25_${I} unverifiable "$OP" int32 int64
  ./make_obj_store_test.sh obj_coercion_26_${I} unverifiable "$OP" int32 float64
  ./make_obj_store_test.sh obj_coercion_27_${I} unverifiable "$OP" int32 'int32&'
  ./make_obj_store_test.sh obj_coercion_28_${I} unverifiable "$OP" int32 object

  ./make_obj_store_test.sh obj_coercion_29_${I} unverifiable "$OP" 'unsigned int32' int64
  ./make_obj_store_test.sh obj_coercion_30_${I} unverifiable "$OP" 'unsigned int32' float64
  ./make_obj_store_test.sh obj_coercion_31_${I} unverifiable "$OP" 'unsigned int32' 'unsigned int32&'
  ./make_obj_store_test.sh obj_coercion_32_${I} unverifiable "$OP" 'unsigned int32' object

  ./make_obj_store_test.sh obj_coercion_33_${I} unverifiable "$OP" int64 int32
  ./make_obj_store_test.sh obj_coercion_34_${I} unverifiable "$OP" int64 'native int'
  ./make_obj_store_test.sh obj_coercion_35_${I} unverifiable "$OP" int64 float64
  ./make_obj_store_test.sh obj_coercion_36_${I} unverifiable "$OP" int64 'int64&'
  ./make_obj_store_test.sh obj_coercion_37_${I} unverifiable "$OP" int64 object

  ./make_obj_store_test.sh obj_coercion_38_${I} unverifiable "$OP" 'unsigned int64' int32
  ./make_obj_store_test.sh obj_coercion_39_${I} unverifiable "$OP" 'unsigned int64' 'native int'
  ./make_obj_store_test.sh obj_coercion_40_${I} unverifiable "$OP" 'unsigned int64' float64
  ./make_obj_store_test.sh obj_coercion_41_${I} unverifiable "$OP" 'unsigned int64' 'unsigned int64&'
  ./make_obj_store_test.sh obj_coercion_42_${I} unverifiable "$OP" 'unsigned int64' object

  ./make_obj_store_test.sh obj_coercion_43_${I} unverifiable "$OP" 'native int' int64
  ./make_obj_store_test.sh obj_coercion_44_${I} unverifiable "$OP" 'native int' float64
  ./make_obj_store_test.sh obj_coercion_45_${I} unverifiable "$OP" 'native int' 'native int&'
  ./make_obj_store_test.sh obj_coercion_46_${I} unverifiable "$OP" 'native int' object

  ./make_obj_store_test.sh obj_coercion_47_${I} unverifiable "$OP" 'native unsigned int' int64
  ./make_obj_store_test.sh obj_coercion_48_${I} unverifiable "$OP" 'native unsigned int' float64
  ./make_obj_store_test.sh obj_coercion_49_${I} unverifiable "$OP" 'native unsigned int' 'native unsigned int&'
  ./make_obj_store_test.sh obj_coercion_50_${I} unverifiable "$OP" 'native unsigned int' object

  ./make_obj_store_test.sh obj_coercion_51_${I} unverifiable "$OP" float32 int32
  ./make_obj_store_test.sh obj_coercion_52_${I} unverifiable "$OP" float32 'native int'
  ./make_obj_store_test.sh obj_coercion_53_${I} unverifiable "$OP" float32 int64
  ./make_obj_store_test.sh obj_coercion_54_${I} unverifiable "$OP" float32 'float32&'
  ./make_obj_store_test.sh obj_coercion_55_${I} unverifiable "$OP" float32 object

  ./make_obj_store_test.sh obj_coercion_56_${I} unverifiable "$OP" float64 int32
  ./make_obj_store_test.sh obj_coercion_57_${I} unverifiable "$OP" float64 'native int'
  ./make_obj_store_test.sh obj_coercion_58_${I} unverifiable "$OP" float64 int64
  ./make_obj_store_test.sh obj_coercion_59_${I} unverifiable "$OP" float64 'float64&'
  ./make_obj_store_test.sh obj_coercion_60_${I} unverifiable "$OP" float64 object

  ./make_obj_store_test.sh obj_coercion_61_${I} unverifiable "$OP" object int32
  ./make_obj_store_test.sh obj_coercion_62_${I} unverifiable "$OP" object 'native int'
  ./make_obj_store_test.sh obj_coercion_63_${I} unverifiable "$OP" object int64
  ./make_obj_store_test.sh obj_coercion_64_${I} unverifiable "$OP" object float64
  ./make_obj_store_test.sh obj_coercion_65_${I} unverifiable "$OP" object 'object&'

  ./make_obj_store_test.sh obj_coercion_66_${I} unverifiable "$OP" 'class ValueType' int32
  ./make_obj_store_test.sh obj_coercion_67_${I} unverifiable "$OP" 'class ValueType' 'native int'
  ./make_obj_store_test.sh obj_coercion_68_${I} unverifiable "$OP" 'class ValueType' int64
  ./make_obj_store_test.sh obj_coercion_69_${I} unverifiable "$OP" 'class ValueType' float64
  ./make_obj_store_test.sh obj_coercion_70_${I} unverifiable "$OP" 'class ValueType' 'class ValueType&'
  ./make_obj_store_test.sh obj_coercion_71_${I} unverifiable "$OP" 'class ValueType' object


  #These tests don't test store error since one cannot have an 'int32&' field
  #They should exist in the structural tests session
  #./make_obj_store_test.sh obj_coercion_72_${I} invalid "$OP" 'int32&' int32
  #./make_obj_store_test.sh obj_coercion_73_${I} invalid "$OP" 'int32&' 'native int'
  #./make_obj_store_test.sh obj_coercion_74_${I} invalid "$OP" 'int32&' int64
  #./make_obj_store_test.sh obj_coercion_75_${I} invalid "$OP" 'int32&' float64
  #./make_obj_store_test.sh obj_coercion_76_${I} invalid "$OP" 'int32&' object


  ./make_obj_store_test.sh obj_coercion_83_${I} valid "$OP" int32 "native int"
  ./make_obj_store_test.sh obj_coercion_84_${I} valid "$OP" "native int" int32

  ./make_obj_store_test.sh obj_coercion_85_${I} valid "$OP" "unsigned int32" "native int"
  ./make_obj_store_test.sh obj_coercion_86_${I} valid "$OP" "native int" "unsigned int32"

  ./make_obj_store_test.sh obj_coercion_87_${I} valid "$OP" int32 "native unsigned int"
  ./make_obj_store_test.sh obj_coercion_88_${I} valid "$OP" "native unsigned int" int32

  ./make_obj_store_test.sh obj_coercion_89_${I} valid "$OP" "unsigned int32" "native int"
  ./make_obj_store_test.sh obj_coercion_90_${I} valid "$OP" "native unsigned int" "unsigned int32"
  I=`expr $I + 1`
done

I=1
for OP in "call void Class::Method(TYPE1)"
do
  ./make_obj_store_test.sh obj_coercion_77_${I} unverifiable "$OP" typedref int32 "no"
  ./make_obj_store_test.sh obj_coercion_78_${I} unverifiable "$OP" typedref 'native int' "no"
  ./make_obj_store_test.sh obj_coercion_79_${I} unverifiable "$OP" typedref int64 "no"
  ./make_obj_store_test.sh obj_coercion_80_${I} unverifiable "$OP" typedref float64 "no"
  ./make_obj_store_test.sh obj_coercion_82_${I} unverifiable "$OP" typedref object "no"
  I=`expr $I + 1`
done

# 1.8.1.2.3 Verification type compatibility (Assignment compatibility)
I=1
for OP in stloc.0 "stloc.s 0" "starg.s 0"
do
  # ClassB not subtype of ClassA.
  ./make_store_test.sh assign_compat_1_${I} unverifiable "$OP" 'class ClassA' 'class ClassB'

  # ClassA not interface type.
  # FIXME: what was the purpoise of this test? on it's current for it is valid and not unverifiable
  ./make_store_test.sh assign_compat_3_${I} valid "$OP" object 'class ClassA'

  # Implementation of InterfaceB does not require the implementation of InterfaceA
  ./make_store_test.sh assign_compat_4_${I} unverifiable "$OP" 'class InterfaceA' 'class InterfaceB'

  # Array/vector.
  ./make_store_test.sh assign_compat_5_${I} unverifiable "$OP" 'string []' 'string[,]'

  # Vector/array.
  ./make_store_test.sh assign_compat_6_${I} unverifiable "$OP" 'string [,]' 'string[]'

  # Arrays with different rank.
  ./make_store_test.sh assign_compat_7_${I} unverifiable "$OP" 'string [,]' 'string[,,]'

  # Method pointers with different return types.
  ./make_store_test.sh assign_compat_8_${I} unverifiable "$OP" 'method int32 *(int32)' 'method float32 *(int32)'

  # Method pointers with different parameters.
  ./make_store_test.sh assign_compat_9_${I} unverifiable "$OP" 'method int32 *(float64)' 'method int32 *(int32)'

  # Method pointers with different calling conventions.
  ./make_store_test.sh assign_compat_10_${I} unverifiable "$OP" 'method vararg int32 *(int32)' 'method int32 *(int32)'

  # Method pointers with different calling conventions. (2)
  ./make_store_test.sh assign_compat_11_${I} unverifiable "$OP" 'method unmanaged fastcall int32 *(int32)' 'method int32 *(int32)'

  # Method pointers with different calling conventions. (3)
  ./make_store_test.sh assign_compat_12_${I} unverifiable "$OP" 'method unmanaged fastcall int32 *(int32)' 'method unmanaged stdcall int32 *(int32)'
  I=`expr $I + 1`
done

for OP in "stfld TYPE1 Class::fld" "stsfld TYPE1 Class::sfld\n\tpop"  "call void Class::Method(TYPE1)"
do
  # ClassB not subtype of ClassA.
  ./make_obj_store_test.sh assign_compat_1_${I} unverifiable "$OP" 'class ClassA' 'class ClassB'

  # object not subtype of ClassA.
  ./make_obj_store_test.sh assign_compat_2_${I} unverifiable "$OP" 'class ClassA' 'object'

  # ClassA not interface type.
  #FIXME: this test is valid, you can store type ClassA in a object field
  ./make_obj_store_test.sh assign_compat_3_${I} valid "$OP" object 'class ClassA'

  # Implementation of InterfaceB does not require the implementation of InterfaceA
  ./make_obj_store_test.sh assign_compat_4_${I} unverifiable "$OP" 'class InterfaceA' 'class InterfaceB'

  # Array/vector.
  ./make_obj_store_test.sh assign_compat_5_${I} unverifiable "$OP" 'string []' 'string[,]'

  # Vector/array.
  ./make_obj_store_test.sh assign_compat_6_${I} unverifiable "$OP" 'string [,]' 'string[]'

  # Arrays with different rank.
  ./make_obj_store_test.sh assign_compat_7_${I} unverifiable "$OP" 'string [,]' 'string[,,]'

  # Method pointers with different return types.
  ./make_obj_store_test.sh assign_compat_8_${I} unverifiable "$OP" 'method int32 *(int32)' 'method float32 *(int32)'

  # Method pointers with different parameters.
  ./make_obj_store_test.sh assign_compat_9_${I} unverifiable "$OP" 'method int32 *(float64)' 'method int32 *(int32)'

  # Method pointers with different calling conventions.
  ./make_obj_store_test.sh assign_compat_10_${I} unverifiable "$OP" 'method vararg int32 *(int32)' 'method int32 *(int32)'

    # Method pointers with different calling conventions. (2)
  ./make_obj_store_test.sh assign_compat_11_${I} unverifiable "$OP" 'method unmanaged fastcall int32 *(int32)' 'method int32 *(int32)'

    # Method pointers with different calling conventions. (3)
  ./make_obj_store_test.sh assign_compat_12_${I} unverifiable "$OP" 'method unmanaged fastcall int32 *(int32)' 'method unmanaged stdcall int32 *(int32)'

  I=`expr $I + 1`
done

# 1.8.1.3 Merging stack states
I=1
for TYPE1 in int32 int64 'native int' float64 'valuetype ValueType' 'class Class' 'int8&' 'int16&' 'int32&' 'int64&' 'native int&' 'float32&' 'float64&' 'valuetype ValueType&' 'class Class&' 'method int32 *(int32)' 'method float32 *(int32)' 'method int32 *(float64)' 'method vararg int32 *(int32)'
do
  for TYPE2 in int32 int64 'native int' float64 'valuetype ValueType' 'class Class' 'int8&' 'int16&' 'int32&' 'int64&' 'native int&' 'float32&' 'float64&' 'valuetype ValueType&' 'class Class&' 'method int32 *(int32)' 'method float32 *(int32)' 'method int32 *(float64)' 'method vararg int32 *(int32)'
  do
  	ZZ=`echo $TYPE1 | grep "*";`
 	T1_PTR=$?
 	ZZ=`echo $TYPE2 | grep "*";`
 	T2_PTR=$?

    if [ $T1_PTR -eq 0 ] || [ $T2_PTR -eq 0 ]; then
		./make_stack_merge_test.sh stack_merge_${I} unverifiable "$TYPE1" "$TYPE2"
    elif [ "$TYPE1" = "$TYPE2" ]; then
		./make_stack_merge_test.sh stack_merge_${I} valid "$TYPE1" "$TYPE2"
	elif [ "$TYPE1" = "int32" ] && [ "$TYPE2" = "native int" ]; then
		./make_stack_merge_test.sh stack_merge_${I} valid "$TYPE1" "$TYPE2"
	elif [ "$TYPE1" = "native int" ] && [ "$TYPE2" = "int32" ]; then
		./make_stack_merge_test.sh stack_merge_${I} valid "$TYPE1" "$TYPE2"
	elif [ "$TYPE1" = "int32&" ] && [ "$TYPE2" = "native int&" ]; then
		./make_stack_merge_test.sh stack_merge_${I} valid "$TYPE1" "$TYPE2"
	elif [ "$TYPE1" = "native int&" ] && [ "$TYPE2" = "int32&" ]; then
		./make_stack_merge_test.sh stack_merge_${I} valid "$TYPE1" "$TYPE2"
	else
		./make_stack_merge_test.sh stack_merge_${I} unverifiable "$TYPE1" "$TYPE2"
    fi
	I=`expr $I + 1`
  done
done

# Unverifiable array stack merges

# These are verifiable, the merged type is 'object' or 'Array'
#for TYPE1 in 'string []' 'string [,]' 'string [,,]'
#do
#  for TYPE2 in 'string []' 'string [,]' 'string [,,]'
#  do
#    if [ "$TYPE1" != "$TYPE2" ]; then
#	./make_stack_merge_test.sh stack_merge_${I} unverifiable "$TYPE1" "$TYPE2"
#	I=`expr $I + 1`
#    fi
#  done
#done

# Exception block branch tests (see 3.15)
I=1
for OP in br "ldc.i4.0\n\tbrfalse"
do
  ./make_exception_branch_test.sh in_try_${I} unverifiable "$OP branch_target1"
  ./make_exception_branch_test.sh in_catch_${I} unverifiable "$OP branch_target2"
  ./make_exception_branch_test.sh in_finally_${I} invalid "$OP branch_target3"
  ./make_exception_branch_test.sh in_filter_${I} unverifiable "$OP branch_target4"
  ./make_exception_branch_test.sh out_try_${I} unverifiable "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_catch_${I} unverifiable "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_finally_${I} unverifiable "" "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_filter_${I} unverifiable "" "" "" "" "$OP branch_target5"
  I=`expr $I + 1`
done

for OP in "ldloc.0\n\tldloc.1\n\tbeq" "ldloc.0\n\tldloc.1\n\tbge"
do
  ./make_exception_branch_test.sh in_try_${I} invalid "$OP branch_target1"
  ./make_exception_branch_test.sh in_catch_${I} invalid "$OP branch_target2"
  ./make_exception_branch_test.sh in_finally_${I} invalid "$OP branch_target3"
  ./make_exception_branch_test.sh in_filter_${I} invalid "$OP branch_target4"
  ./make_exception_branch_test.sh out_try_${I} invalid "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_catch_${I} invalid "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_finally_${I} unverifiable "" "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_filter_${I} unverifiable "" "" "" "" "$OP branch_target5"
  I=`expr $I + 1`
done

./make_exception_branch_test.sh ret_out_try unverifiable "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_catch unverifiable "" "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_finally unverifiable "" "" "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_filter unverifiable "" "" "" "" "ldc.i4.0\n\tret"


# Unary branch op type tests (see 3.17)

for OP in brfalse
do
  ./make_unary_test.sh un_branch_op unverifiable "$OP branch_target" float64
done

# Ldloc.0 and Ldarg tests (see 3.38)

I=1
for OP in "ldarg.s 0" "ldarg.0"
do
  ./make_unary_test.sh ld_no_slot_${I} unverifiable "pop\n\t$OP\n\tpop" int32
  I=`expr $I + 1`
done

for OP in "ldloc.s 1" "ldloc.1" "ldloc 1"
do
  ./make_unary_test.sh ld_no_slot_${I} invalid "pop\n\t$OP\n\tpop" int32
  I=`expr $I + 1`
done

for OP in "ldarga.s 0" "ldloca.s 1"
do
  ./make_unary_test.sh ld_no_slot_${I} invalid "pop\n\t$OP\n\tpop" int32
  I=`expr $I + 1`
done

# Starg and Stloc tests (see 3.61)

I=1
for OP in "starg.s 0"
do
  ./make_unary_test.sh st_no_slot_${I} unverifiable "$OP" int32
  I=`expr $I + 1`
done

for OP in "stloc.s 1"
do
  ./make_unary_test.sh st_no_slot_${I} invalid "$OP" int32
  I=`expr $I + 1`
done

# Ldfld and Ldflda tests (see 4.10)

for OP in ldfld ldflda
do
  ./make_unary_test.sh ${OP}_no_fld invalid "$OP int32 Class::invalid\n\tpop" "class Class"
  ./make_unary_test.sh ${OP}_bad_obj unverifiable "$OP int32 Class::valid\n\tpop" object
  ./make_unary_test.sh ${OP}_obj_int32 unverifiable "$OP int32 Class::valid\n\tpop" int32
  ./make_unary_test.sh ${OP}_obj_int64 unverifiable "$OP int32 Class::valid\n\tpop" int64
  ./make_unary_test.sh ${OP}_obj_float64 unverifiable "$OP int32 Class::valid\n\tpop" float64
  ./make_unary_test.sh ${OP}_obj_native_int unverifiable "$OP int32 Class::valid\n\tpop" 'native int'
#overlapped checks must be done separately
#  ./make_unary_test.sh ${OP}_obj_ref_overlapped unverifiable "$OP object Overlapped::objVal\n\tpop" "class Overlapped"
#  ./make_unary_test.sh ${OP}_obj_overlapped_field_not_accessible unverifiable "$OP int32 Overlapped::publicIntVal\n\tpop" "class Overlapped"
done

#TODO: these tests are bogus, they need to be fixed
# Stfld tests (see 4.28)

./make_unary_test.sh stfld_no_fld invalid "ldc.i4.0\n\tstfld int32 Class::invalid" "class Class"
./make_unary_test.sh stfld_bad_obj unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" object
./make_unary_test.sh stfld_obj_int32 unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" int32
./make_unary_test.sh stfld_obj_int64 unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" int64
./make_unary_test.sh stfld_obj_float64 unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" float64
./make_unary_test.sh stfld_no_int invalid "stfld int32 Class::valid" "class Class"
./make_unary_test.sh stfld_obj_native_int unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" 'native int'

# Box tests (see 4.1)

# Box non-existent type.
./make_unary_test.sh box_bad_type unverifiable "box valuetype NonExistent\n\tpop" "valuetype NonExistent"

# Top of stack not assignment compatible with typeToc.
./make_unary_test.sh box_not_compat unverifiable "box [mscorlib]System.Int32\n\tpop" float32

# Box byref type.
./make_unary_test.sh box_byref invalid "box [mscorlib]System.Int32\&\n\tpop" 'int32&'

# Box byref-like type.
./make_unary_test.sh box_byref_like unverifiable "box [mscorlib]System.TypedReference\n\tpop" typedref

#boxing between Int32 and IntPtr
./make_unary_test.sh box_compat_1 valid "box [mscorlib]System.Int32\n\tpop" "native int"
./make_unary_test.sh box_compat_2 valid "box [mscorlib]System.IntPtr\n\tpop" "int32"

#This is illegal since you cannot have a Void local variable, it should go into the structural tests part
# Box void type.
#./make_unary_test.sh box_void unverifiable "box [mscorlib]System.Void\n\tpop" "class [mscorlib]System.Void"





./make_ret_test.sh ret_coercion_1 unverifiable int8 int64
./make_ret_test.sh ret_coercion_2 unverifiable int8 float64
./make_ret_test.sh ret_coercion_3 unverifiable int8 'int8&'
./make_ret_test.sh ret_coercion_4 unverifiable int8 object

./make_ret_test.sh ret_coercion_5 unverifiable 'unsigned int8' int64
./make_ret_test.sh ret_coercion_6 unverifiable 'unsigned int8' float64
./make_ret_test.sh ret_coercion_6 unverifiable 'unsigned int8' float64
./make_ret_test.sh ret_coercion_6 unverifiable 'unsigned int8' float64
./make_ret_test.sh ret_coercion_7 unverifiable 'unsigned int8' 'unsigned int8&'
./make_ret_test.sh ret_coercion_8 unverifiable 'unsigned int8' object

./make_ret_test.sh ret_coercion_9 unverifiable bool int64
./make_ret_test.sh ret_coercion_10 unverifiable bool float64
./make_ret_test.sh ret_coercion_11 unverifiable bool 'bool&'
./make_ret_test.sh ret_coercion_12 unverifiable bool object

./make_ret_test.sh ret_coercion_13 unverifiable int16 int64
./make_ret_test.sh ret_coercion_14 unverifiable int16 float64
./make_ret_test.sh ret_coercion_15 unverifiable int16 'int16&'
./make_ret_test.sh ret_coercion_16 unverifiable int16 object

./make_ret_test.sh ret_coercion_17 unverifiable 'unsigned int16' int64
./make_ret_test.sh ret_coercion_18 unverifiable 'unsigned int16' float64
./make_ret_test.sh ret_coercion_19 unverifiable 'unsigned int16' 'unsigned int16&'
./make_ret_test.sh ret_coercion_20 unverifiable 'unsigned int16' object

./make_ret_test.sh ret_coercion_21 unverifiable char int64
./make_ret_test.sh ret_coercion_22 unverifiable char float64
./make_ret_test.sh ret_coercion_23 unverifiable char 'char&'
./make_ret_test.sh ret_coercion_24 unverifiable char object

./make_ret_test.sh ret_coercion_25 unverifiable int32 int64
./make_ret_test.sh ret_coercion_26 unverifiable int32 float64
./make_ret_test.sh ret_coercion_27 unverifiable int32 'int32&'
./make_ret_test.sh ret_coercion_28 unverifiable int32 object

./make_ret_test.sh ret_coercion_29 unverifiable 'unsigned int32' int64
./make_ret_test.sh ret_coercion_30 unverifiable 'unsigned int32' float64
./make_ret_test.sh ret_coercion_31 unverifiable 'unsigned int32' 'unsigned int32&'
./make_ret_test.sh ret_coercion_32 unverifiable 'unsigned int32' object

./make_ret_test.sh ret_coercion_33 unverifiable int64 int32
./make_ret_test.sh ret_coercion_34 unverifiable int64 'native int'
./make_ret_test.sh ret_coercion_35 unverifiable int64 float64
./make_ret_test.sh ret_coercion_36 unverifiable int64 'int64&'
./make_ret_test.sh ret_coercion_37 unverifiable int64 object

./make_ret_test.sh ret_coercion_38 unverifiable 'unsigned int64' int32
./make_ret_test.sh ret_coercion_39 unverifiable 'unsigned int64' 'native int'
./make_ret_test.sh ret_coercion_40 unverifiable 'unsigned int64' float64
./make_ret_test.sh ret_coercion_41 unverifiable 'unsigned int64' 'unsigned int64&'
./make_ret_test.sh ret_coercion_42 unverifiable 'unsigned int64' object

./make_ret_test.sh ret_coercion_43 unverifiable 'native int' int64
./make_ret_test.sh ret_coercion_44 unverifiable 'native int' float64
./make_ret_test.sh ret_coercion_45 unverifiable 'native int' 'native int&'
./make_ret_test.sh ret_coercion_46 unverifiable 'native int' object

./make_ret_test.sh ret_coercion_47 unverifiable 'native unsigned int' int64
./make_ret_test.sh ret_coercion_48 unverifiable 'native unsigned int' float64
./make_ret_test.sh ret_coercion_49 unverifiable 'native unsigned int' 'native unsigned int&'
./make_ret_test.sh ret_coercion_50 unverifiable 'native unsigned int' object

./make_ret_test.sh ret_coercion_51 unverifiable float32 int32
./make_ret_test.sh ret_coercion_52 unverifiable float32 'native int'
./make_ret_test.sh ret_coercion_53 unverifiable float32 int64
./make_ret_test.sh ret_coercion_54 unverifiable float32 'float32&'
./make_ret_test.sh ret_coercion_55 unverifiable float32 object

./make_ret_test.sh ret_coercion_56 unverifiable float64 int32
./make_ret_test.sh ret_coercion_57 unverifiable float64 'native int'
./make_ret_test.sh ret_coercion_58 unverifiable float64 int64
./make_ret_test.sh ret_coercion_59 unverifiable float64 'float64&'
./make_ret_test.sh ret_coercion_60 unverifiable float64 object

./make_ret_test.sh ret_coercion_61 unverifiable object int32
./make_ret_test.sh ret_coercion_62 unverifiable object 'native int'
./make_ret_test.sh ret_coercion_63 unverifiable object int64
./make_ret_test.sh ret_coercion_64 unverifiable object float64
./make_ret_test.sh ret_coercion_65 unverifiable object 'object&'

./make_ret_test.sh ret_coercion_66 unverifiable 'class MyValueType' int32
./make_ret_test.sh ret_coercion_67 unverifiable 'class MyValueType' 'native int'
./make_ret_test.sh ret_coercion_68 unverifiable 'class MyValueType' int64
./make_ret_test.sh ret_coercion_69 unverifiable 'class MyValueType' float64
./make_ret_test.sh ret_coercion_70 unverifiable 'class MyValueType' 'class MyValueType&'
./make_ret_test.sh ret_coercion_71 unverifiable 'class MyValueType' object

./make_ret_test.sh ret_coercion_72 unverifiable 'int32&' int32
./make_ret_test.sh ret_coercion_73 unverifiable 'int32&' 'native int'
./make_ret_test.sh ret_coercion_74 unverifiable 'int32&' int64
./make_ret_test.sh ret_coercion_75 unverifiable 'int32&' float64
./make_ret_test.sh ret_coercion_76 unverifiable 'int32&' object

./make_ret_test.sh ret_coercion_77 unverifiable typedref int32
./make_ret_test.sh ret_coercion_78 unverifiable typedref 'native int'
./make_ret_test.sh ret_coercion_79 unverifiable typedref int64
./make_ret_test.sh ret_coercion_80 unverifiable typedref float64
./make_ret_test.sh ret_coercion_81 badmd typedref 'typedref&'
./make_ret_test.sh ret_coercion_82 unverifiable typedref object

./make_ret_test.sh ret_coercion_83 valid int32 "native int"
./make_ret_test.sh ret_coercion_84 valid "native int" int32
./make_ret_test.sh ret_coercion_85 valid "unsigned int32" "native int"
./make_ret_test.sh ret_coercion_86 valid "native int" "unsigned int32"
./make_ret_test.sh ret_coercion_87 valid int32 "native unsigned int"
./make_ret_test.sh ret_coercion_88 valid "native unsigned int" int32
./make_ret_test.sh ret_coercion_89 valid "unsigned int32" "native int"
./make_ret_test.sh ret_coercion_90 valid "native unsigned int" "unsigned int32"

#type is unverifable
./make_ret_test.sh ret_coercion_100 unverifiable "int32*" "int32*"
./make_ret_test.sh ret_coercion_101 unverifiable "method int32* (int32)" "method int32* (int32)"

#typedbyref as parm is ok
./make_ret_test.sh ret_coercion_102 unverifiable int32 typedref
./make_ret_test.sh ret_coercion_103 unverifiable typedref int32

#unverifable return type: byref, typedbyref and ArgInterator
./make_ret_test.sh bad_ret_type_1 unverifiable typedref typedref
./make_ret_test.sh bad_ret_type_2 unverifiable "int32&" "int32&"
./make_ret_test.sh bad_ret_type_4 unverifiable "valuetype [mscorlib]System.ArgIterator" "valuetype [mscorlib]System.ArgIterator"


./make_ret_test.sh ret_sub_type valid ClassA ClassSubA
./make_ret_test.sh ret_same_type valid ClassA ClassA
./make_ret_test.sh ret_obj_iface valid object InterfaceA
./make_ret_test.sh ret_obj_obj valid object object
./make_ret_test.sh ret_obj_string valid object string
./make_ret_test.sh ret_string_string valid string string
./make_ret_test.sh ret_obj_vector valid object 'int32[]'
./make_ret_test.sh ret_obj_array valid object 'int32[,]'
./make_ret_test.sh ret_obj_generic valid object 'class Template`1<object>'
./make_ret_test.sh ret_obj_value_type unverifiable object 'MyValueType'
./make_ret_test.sh ret_string_value_type unverifiable string 'MyValueType'
./make_ret_test.sh ret_class_value_type unverifiable ClassA 'MyValueType'

./make_ret_test.sh ret_string_string unverifiable string object
./make_ret_test.sh ret_string_string unverifiable 'int32[]' object

./make_ret_test.sh ret_iface_imple valid InterfaceA ImplA
./make_ret_test.sh ret_arrays_same_vector valid 'int32[]' 'int32[]'
./make_ret_test.sh ret_arrays_same_rank valid 'int32[,]' 'int32[,]'

./make_ret_test.sh ret_sub_type_array_covariant valid 'ClassA[]' 'ClassSubA[]'
./make_ret_test.sh ret_same_type_array_covariant valid 'ClassA[]' 'ClassA[]'
./make_ret_test.sh ret_obj_iface_array_covariant valid 'object[]' 'InterfaceA[]'
./make_ret_test.sh ret_iface_imple_array_covariant valid 'InterfaceA[]' 'ImplA[]'

./make_ret_test.sh ret_diff_types unverifiable ClassA ClassB
./make_ret_test.sh ret_class_vale_type unverifiable ClassA MyValueType
./make_ret_test.sh ret_diff_vale_type unverifiable MyValueType2 MyValueType
./make_ret_test.sh ret_value_type_class unverifiable MyValueType ClassA
./make_ret_test.sh ret_super_type unverifiable ClassSubA ClassB
./make_ret_test.sh ret_interfaces unverifiable InterfaceA InterfaceB
./make_ret_test.sh ret_interface_class unverifiable ClassA InterfaceB

./make_ret_test.sh ret_object_type valid object ClassA
./make_ret_test.sh ret_type_object unverifiable ClassA object


./make_ret_test.sh ret_array_diff_rank unverifiable 'int32[]' 'int32[,]'
./make_ret_test.sh ret_array_diff_rank2 unverifiable 'int32[,]' 'int32[]'
./make_ret_test.sh ret_array_diff_rank3 unverifiable 'int32[,,]' 'int32[,]'
./make_ret_test.sh ret_array_not_covar unverifiable 'ClassA[]' 'ClassB[]'
./make_ret_test.sh ret_array_not_covar2 unverifiable 'ClassSubA[]' 'ClassA[]'
./make_ret_test.sh ret_array_not_covar3 unverifiable 'ClassA[]' 'InterfaceA[]'
./make_ret_test.sh ret_array_not_covar4 unverifiable 'ImplA[]' 'InterfaceA[]'
./make_ret_test.sh ret_array_not_covar5 unverifiable 'InterfaceA[]' 'object[]'


#generics tests
./make_ret_test.sh ret_generics_1 valid 'class Template' 'class Template'
./make_ret_test.sh ret_generics_2 valid 'class Template`1<int32>' 'class Template`1<int32>'
./make_ret_test.sh ret_generics_3 valid 'class Template`2<int32,object>' 'class Template`2<int32,object>'

./make_ret_test.sh ret_generics_4 unverifiable 'class Template' 'class Template`1<object>'
./make_ret_test.sh ret_generics_5 unverifiable 'class Template`1<object>' 'class Template'
./make_ret_test.sh ret_generics_6 unverifiable 'class Template`1<object>' 'class Template`1<string>'
./make_ret_test.sh ret_generics_7 unverifiable 'class Template`1<string>' 'class Template`1<object>'
./make_ret_test.sh ret_generics_8 unverifiable 'class Template`1<object>' 'class Template`2<object, object>'
./make_ret_test.sh ret_generics_9 unverifiable 'class Template`2<object, object>' 'class Template`1<object>'

./make_ret_test.sh ret_generics_10 unverifiable 'class Template`1<int32>' 'class Template`1<int16>'
./make_ret_test.sh ret_generics_11 unverifiable 'class Template`1<int16>' 'class Template`1<int32>'
./make_ret_test.sh ret_generics_12 unverifiable 'class Template`1<unsigned int32>' 'class Template`1<int32>'
./make_ret_test.sh ret_generics_13 unverifiable 'class Template`1<float32>' 'class Template`1<float64>'
./make_ret_test.sh ret_generics_14 unverifiable 'class Template`1<float64>' 'class Template`1<float32>'

#variance tests
./make_ret_test.sh ret_generics_15 valid 'class ICovariant`1<object>' 'class ICovariant`1<string>'
./make_ret_test.sh ret_generics_16 valid 'class ICovariant`1<string>' 'class ICovariant`1<string>'
./make_ret_test.sh ret_generics_17 unverifiable 'class ICovariant`1<string>' 'class ICovariant`1<object>'

./make_ret_test.sh ret_generics_18 valid 'class IContravariant`1<string>' 'class IContravariant`1<object>'
./make_ret_test.sh ret_generics_19 valid 'class IContravariant`1<string>' 'class IContravariant`1<string>'
./make_ret_test.sh ret_generics_20 unverifiable 'class IContravariant`1<object>' 'class IContravariant`1<string>'

./make_ret_test.sh ret_generics_21 valid 'class ICovariant`1<ClassA>' 'class ICovariant`1<ClassSubA>'
./make_ret_test.sh ret_generics_22 valid 'class ICovariant`1<ClassSubA>' 'class ICovariant`1<ClassSubA>'
./make_ret_test.sh ret_generics_23 unverifiable 'class ICovariant`1<ClassSubA>' 'class ICovariant`1<ClassA>'

./make_ret_test.sh ret_generics_24 valid 'class IContravariant`1<ClassSubA>' 'class IContravariant`1<ClassA>'
./make_ret_test.sh ret_generics_25 valid 'class IContravariant`1<ClassSubA>' 'class IContravariant`1<ClassSubA>'
./make_ret_test.sh ret_generics_26 unverifiable 'class IContravariant`1<ClassA>' 'class IContravariant`1<ClassSubA>'


./make_ret_test.sh ret_generics_27 valid 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<ClassA, ClassB>'
./make_ret_test.sh ret_generics_28 valid 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<ClassA, object>'
./make_ret_test.sh ret_generics_29 valid 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<ClassSubA, ClassB>'
./make_ret_test.sh ret_generics_30 valid 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<ClassSubA, object>'
./make_ret_test.sh ret_generics_31 unverifiable 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<object, ClassB>'
./make_ret_test.sh ret_generics_32 unverifiable 'class Bivariant`2<ClassA, ClassB>' 'class Bivariant`2<object, object>'
./make_ret_test.sh ret_generics_33 unverifiable 'class Bivariant`2<ClassA, object>' 'class Bivariant`2<object, ClassB>'
./make_ret_test.sh ret_generics_34 unverifiable 'class Bivariant`2<ClassA, object>' 'class Bivariant`2<ClassA, ClassB>'

#mix parameter types
./make_ret_test.sh ret_generics_types_1 unverifiable 'class Template`1<int8>' 'class Template`1<unsigned int8>'
./make_ret_test.sh ret_generics_types_2 unverifiable 'class Template`1<int8>' 'class Template`1<int16>'
./make_ret_test.sh ret_generics_types_3 unverifiable 'class Template`1<int8>' 'class Template`1<unsigned int16>'
./make_ret_test.sh ret_generics_types_4 unverifiable 'class Template`1<int8>' 'class Template`1<int32>'
./make_ret_test.sh ret_generics_types_5 unverifiable 'class Template`1<int8>' 'class Template`1<unsigned int32>'
./make_ret_test.sh ret_generics_types_6 unverifiable 'class Template`1<int8>' 'class Template`1<int64>'
./make_ret_test.sh ret_generics_types_7 unverifiable 'class Template`1<int8>' 'class Template`1<unsigned int64>'
./make_ret_test.sh ret_generics_types_8 unverifiable 'class Template`1<int8>' 'class Template`1<float32>'
./make_ret_test.sh ret_generics_types_9 unverifiable 'class Template`1<int8>' 'class Template`1<float64>'
./make_ret_test.sh ret_generics_types_10 unverifiable 'class Template`1<int8>' 'class Template`1<bool>'

./make_ret_test.sh ret_generics_types_11 unverifiable 'class Template`1<int8>' 'class Template`1<native int>'
./make_ret_test.sh ret_generics_types_12 unverifiable 'class Template`1<int8>' 'class Template`1<native unsigned int>'
./make_ret_test.sh ret_generics_types_13 unverifiable 'class Template`1<int8>' 'class Template`1<int32 *>'


#inheritance tests
./make_ret_test.sh ret_generics_inheritante_1 valid 'class Base`1<int32>' 'class SubClass1`1<int32>'
./make_ret_test.sh ret_generics_inheritante_2 valid 'class SubClass1`1<int32>' 'class SubClass1`1<int32>'
./make_ret_test.sh ret_generics_inheritante_3 unverifiable 'class SubClass1`1<int32>' 'class Base`1<int32>'
./make_ret_test.sh ret_generics_inheritante_4 unverifiable 'class Base`1<int32>' 'class SubClass1`1<float32>'
./make_ret_test.sh ret_generics_inheritante_5 valid 'class Base`1<object>' 'class SubClass1`1<object>'

./make_ret_test.sh ret_generics_inheritante_6 valid 'class BaseBase`2<int32, object>' 'class SubClass1`1<object>'
./make_ret_test.sh ret_generics_inheritante_7 valid 'class BaseBase`2<int32, object>' 'class Base`1<object>'

./make_ret_test.sh ret_generics_inheritante_8 unverifiable 'class BaseBase`2<int64, object>' 'class Base`1<object>'
./make_ret_test.sh ret_generics_inheritante_9 unverifiable 'class BaseBase`2<int64, object>' 'class SubClass1`1<object>'
./make_ret_test.sh ret_generics_inheritante_10 unverifiable 'class BaseBase`2<int32, object>' 'class SubClass1`1<string>'

#interface tests

./make_ret_test.sh ret_generics_inheritante_12 valid 'class Interface`1<int32>' 'class InterfaceImpl`1<int32>'
./make_ret_test.sh ret_generics_inheritante_13 valid 'class InterfaceImpl`1<int32>' 'class InterfaceImpl`1<int32>'
./make_ret_test.sh ret_generics_inheritante_14 unverifiable 'class InterfaceImpl`1<int32>' 'class Interface`1<int32>'
./make_ret_test.sh ret_generics_inheritante_15 unverifiable 'class Interface`1<int32>' 'class InterfaceImpl`1<float32>'
./make_ret_test.sh ret_generics_inheritante_16 valid 'class Interface`1<object>' 'class InterfaceImpl`1<object>'


#mix variance with inheritance
#only interfaces or delegates can have covariance

#mix variance with interfaces

./make_ret_test.sh ret_generics_inheritante_28 valid 'class ICovariant`1<object>' 'class CovariantImpl`1<string>'
./make_ret_test.sh ret_generics_inheritante_29 valid 'class ICovariant`1<string>' 'class CovariantImpl`1<string>'
./make_ret_test.sh ret_generics_inheritante_30 unverifiable 'class ICovariant`1<string>' 'class CovariantImpl`1<object>'

./make_ret_test.sh ret_generics_inheritante_31 valid 'class IContravariant`1<string>' 'class ContravariantImpl`1<object>'
./make_ret_test.sh ret_generics_inheritante_32 valid 'class IContravariant`1<string>' 'class ContravariantImpl`1<string>'
./make_ret_test.sh ret_generics_inheritante_33 unverifiable 'class IContravariant`1<object>' 'class ContravariantImpl`1<string>'

./make_ret_test.sh ret_generics_inheritante_34 valid 'class ICovariant`1<ClassA>' 'class CovariantImpl`1<ClassSubA>'
./make_ret_test.sh ret_generics_inheritante_35 valid 'class ICovariant`1<ClassSubA>' 'class CovariantImpl`1<ClassSubA>'
./make_ret_test.sh ret_generics_inheritante_36 unverifiable 'class ICovariant`1<ClassSubA>' 'class CovariantImpl`1<ClassA>'

./make_ret_test.sh ret_generics_inheritante_37 valid 'class IContravariant`1<ClassSubA>' 'class ContravariantImpl`1<ClassA>'
./make_ret_test.sh ret_generics_inheritante_38 valid 'class IContravariant`1<ClassSubA>' 'class ContravariantImpl`1<ClassSubA>'
./make_ret_test.sh ret_generics_inheritante_39 unverifiable 'class IContravariant`1<ClassA>' 'class ContravariantImpl`1<ClassSubA>'


#mix variance with arrays

./make_ret_test.sh ret_generics_arrays_1 valid 'class ICovariant`1<object>' 'class ICovariant`1<object[]>'
./make_ret_test.sh ret_generics_arrays_2 valid 'class ICovariant`1<object>' 'class ICovariant`1<int32[]>'
./make_ret_test.sh ret_generics_arrays_3 valid 'class ICovariant`1<object>' 'class ICovariant`1<int32[,]>'
./make_ret_test.sh ret_generics_arrays_4 valid 'class ICovariant`1<object>' 'class ICovariant`1<string[]>'
./make_ret_test.sh ret_generics_arrays_5 valid 'class ICovariant`1<object[]>' 'class ICovariant`1<string[]>'
./make_ret_test.sh ret_generics_arrays_6 valid 'class ICovariant`1<object[]>' 'class ICovariant`1<ClassA[]>'
./make_ret_test.sh ret_generics_arrays_7 valid 'class ICovariant`1<ClassA[]>' 'class ICovariant`1<ClassSubA[]>'
./make_ret_test.sh ret_generics_arrays_8 valid 'class ICovariant`1<InterfaceA[]>' 'class ICovariant`1<ImplA[]>'
./make_ret_test.sh ret_generics_arrays_9 valid 'class ICovariant`1<object[,]>' 'class ICovariant`1<string[,]>'
./make_ret_test.sh ret_generics_arrays_10 valid 'class ICovariant`1<ClassA[,]>' 'class ICovariant`1<ClassSubA[,]>'

./make_ret_test.sh ret_generics_arrays_1_b valid 'class ICovariant`1<object>' 'class CovariantImpl`1<object[]>'
./make_ret_test.sh ret_generics_arrays_2_b valid 'class ICovariant`1<object>' 'class CovariantImpl`1<int32[]>'
./make_ret_test.sh ret_generics_arrays_3_b valid 'class ICovariant`1<object>' 'class CovariantImpl`1<int32[,]>'
./make_ret_test.sh ret_generics_arrays_4_b valid 'class ICovariant`1<object>' 'class ICovariant`1<string[]>'
./make_ret_test.sh ret_generics_arrays_5_b valid 'class ICovariant`1<object[]>' 'class CovariantImpl`1<string[]>'
./make_ret_test.sh ret_generics_arrays_6_b valid 'class ICovariant`1<object[]>' 'class CovariantImpl`1<ClassA[]>'
./make_ret_test.sh ret_generics_arrays_7_b valid 'class ICovariant`1<ClassA[]>' 'class CovariantImpl`1<ClassSubA[]>'
./make_ret_test.sh ret_generics_arrays_8_b valid 'class ICovariant`1<InterfaceA[]>' 'class CovariantImpl`1<ImplA[]>'
./make_ret_test.sh ret_generics_arrays_9_b valid 'class ICovariant`1<object[,]>' 'class CovariantImpl`1<string[,]>'
./make_ret_test.sh ret_generics_arrays_10_b valid 'class ICovariant`1<ClassA[,]>' 'class CovariantImpl`1<ClassSubA[,]>'

./make_ret_test.sh ret_generics_arrays_11 valid 'class IContravariant`1<object[]>' 'class IContravariant`1<object>'
./make_ret_test.sh ret_generics_arrays_12 valid 'class IContravariant`1<int32[]>' 'class IContravariant`1<object>'
./make_ret_test.sh ret_generics_arrays_13 valid 'class IContravariant`1<int32[,]>' 'class IContravariant`1<object>'
./make_ret_test.sh ret_generics_arrays_14 valid 'class IContravariant`1<string[]>' 'class IContravariant`1<object>'
./make_ret_test.sh ret_generics_arrays_15 valid 'class IContravariant`1<string[]>' 'class IContravariant`1<object[]>'
./make_ret_test.sh ret_generics_arrays_16 valid 'class IContravariant`1<ClassA[]>' 'class IContravariant`1<object[]>'
./make_ret_test.sh ret_generics_arrays_17 valid 'class IContravariant`1<ClassSubA[]>' 'class IContravariant`1<ClassA[]>'
./make_ret_test.sh ret_generics_arrays_18 valid 'class IContravariant`1<ImplA[]>' 'class IContravariant`1<InterfaceA[]>'
./make_ret_test.sh ret_generics_arrays_19 valid 'class IContravariant`1<string[,]>' 'class IContravariant`1<object[,]>'
./make_ret_test.sh ret_generics_arrays_20 valid 'class IContravariant`1<ClassSubA[,]>' 'class IContravariant`1<ClassA[,]>'

./make_ret_test.sh ret_generics_arrays_11_b valid 'class IContravariant`1<object[]>' 'class ContravariantImpl`1<object>'
./make_ret_test.sh ret_generics_arrays_12_b valid 'class IContravariant`1<int32[]>' 'class ContravariantImpl`1<object>'
./make_ret_test.sh ret_generics_arrays_13_b valid 'class IContravariant`1<int32[,]>' 'class ContravariantImpl`1<object>'
./make_ret_test.sh ret_generics_arrays_14_b valid 'class IContravariant`1<string[]>' 'class ContravariantImpl`1<object>'
./make_ret_test.sh ret_generics_arrays_15_b valid 'class IContravariant`1<string[]>' 'class ContravariantImpl`1<object[]>'
./make_ret_test.sh ret_generics_arrays_16_b valid 'class IContravariant`1<ClassA[]>' 'class ContravariantImpl`1<object[]>'
./make_ret_test.sh ret_generics_arrays_17_b valid 'class IContravariant`1<ClassSubA[]>' 'class ContravariantImpl`1<ClassA[]>'
./make_ret_test.sh ret_generics_arrays_18_b valid 'class IContravariant`1<ImplA[]>' 'class ContravariantImpl`1<InterfaceA[]>'
./make_ret_test.sh ret_generics_arrays_19_b valid 'class IContravariant`1<string[,]>' 'class ContravariantImpl`1<object[,]>'
./make_ret_test.sh ret_generics_arrays_20_b valid 'class IContravariant`1<ClassSubA[,]>' 'class ContravariantImpl`1<ClassA[,]>'

./make_ret_test.sh ret_generics_arrays_21 unverifiable 'class ICovariant`1<int32[]>' 'class ICovariant`1<object>'
./make_ret_test.sh ret_generics_arrays_22 unverifiable 'class ICovariant`1<int32[]>' 'class ICovariant`1<object[]>'
./make_ret_test.sh ret_generics_arrays_23 unverifiable 'class ICovariant`1<string[]>' 'class ICovariant`1<object[]>'
./make_ret_test.sh ret_generics_arrays_24 unverifiable 'class ICovariant`1<ClassSubA[]>' 'class ICovariant`1<ClassA[]>'
./make_ret_test.sh ret_generics_arrays_25 unverifiable 'class ICovariant`1<int32[]>' 'class ICovariant`1<int32[,]>'
./make_ret_test.sh ret_generics_arrays_26 unverifiable 'class ICovariant`1<ImplA[]>' 'class ICovariant`1<InterfaceA[]>'

./make_ret_test.sh ret_generics_arrays_27 unverifiable 'class IContravariant`1<object>' 'class IContravariant`1<int32[]>'
./make_ret_test.sh ret_generics_arrays_28 unverifiable 'class IContravariant`1<object[]>' 'class IContravariant`1<int32[]>'
./make_ret_test.sh ret_generics_arrays_29 unverifiable 'class IContravariant`1<object[]>' 'class IContravariant`1<string[]>'
./make_ret_test.sh ret_generics_arrays_30 unverifiable 'class IContravariant`1<ClassA[]>' 'class IContravariant`1<ClassSubA[]>'
./make_ret_test.sh ret_generics_arrays_31 unverifiable 'class IContravariant`1<int32[,]>' 'class IContravariant`1<int32[]>'
./make_ret_test.sh ret_generics_arrays_32 unverifiable 'class IContravariant`1<InterfaceA[]>' 'class IContravariant`1<ImplA[]>'


#generic with value types

./make_ret_test.sh ret_generics_vt_1 valid 'class Template`1<MyValueType>' 'class Template`1<MyValueType>'
./make_ret_test.sh ret_generics_vt_2 unverifiable 'class Template`1<MyValueType>' 'class Template`1<MyValueType2>'
./make_ret_test.sh ret_generics_vt_3 unverifiable 'class ICovariant`1<MyValueType>' 'class ICovariant`1<MyValueType2>'
./make_ret_test.sh ret_generics_vt_4 unverifiable 'class ICovariant`1<object>' 'class ICovariant`1<MyValueType2>'


#mix variance and generic compatibility with all kinds of types valid for a generic parameter (hellish task - huge task)
#test with composite generics ( Foo<Bar<int>> )

#test variance with delegates
#generic methods
#generic attributes
#generic delegates
#generic code
#the verifier must check if the generic instantiation is valid

for OP in ldarg ldloc
do
	ARGS_1='int32 V'
	LOCALS_1=''
	CALL_1='ldc.i4.0'
	SIG_1='int32'

	ARGS_2='int32 V, int32 V1'
	LOCALS_2=''
	CALL_2='ldc.i4.0\n\tldc.i4.0'
	SIG_2='int32, int32'

	ARGS_3='int32 V, int32 V1, int32 V1'
	LOCALS_3=''
	CALL_3='ldc.i4.0\n\tldc.i4.0\n\tldc.i4.0'
	SIG_3='int32, int32, int32'

	ARGS_4='int32 V, int32 V1, int32 V1, int32 V1'
	LOCALS_4=''
	CALL_4='ldc.i4.0\n\tldc.i4.0\n\tldc.i4.0\n\tldc.i4.0'
	SIG_4='int32, int32, int32, int32'
	MAX_PARAM_RESULT="unverifiable"
	POPS="pop\npop\npop\npop\npop\npop\npop\npop\n"

	if [ "$OP" = "ldloc" ]; then
		MAX_PARAM_RESULT="invalid"

		LOCALS_1=$ARGS_1
		ARGS_1=''
		CALL_1=''
		SIG_1=''

		LOCALS_2=$ARGS_2
		ARGS_2=''
		CALL_2=''
		SIG_2=''

		LOCALS_3=$ARGS_3
		ARGS_3=''
		CALL_3=''
		SIG_3=''

		LOCALS_4=$ARGS_4
		ARGS_4=''
		CALL_4=''
		SIG_4=''
	fi;

	./make_load_test.sh ${OP}0_max_params "${MAX_PARAM_RESULT}" "${OP}.0" '' '' '' ''
	./make_load_test.sh ${OP}1_max_params "${MAX_PARAM_RESULT}" "${OP}.1" '' '' '' ''
	./make_load_test.sh ${OP}2_max_params "${MAX_PARAM_RESULT}" "${OP}.2" '' '' '' ''
	./make_load_test.sh ${OP}3_max_params "${MAX_PARAM_RESULT}" "${OP}.3" '' '' '' ''

	./make_load_test.sh ${OP}1_1_max_params "${MAX_PARAM_RESULT}" "${OP}.1" "${ARGS_1}" "${LOCALS_1}" "${CALL_1}" "${SIG_1}"
	./make_load_test.sh ${OP}2_1_max_params "${MAX_PARAM_RESULT}" "${OP}.2" "${ARGS_1}" "${LOCALS_1}" "${CALL_1}" "${SIG_1}"
	./make_load_test.sh ${OP}3_1_max_params "${MAX_PARAM_RESULT}" "${OP}.3" "${ARGS_1}" "${LOCALS_1}" "${CALL_1}" "${SIG_1}"

	./make_load_test.sh ${OP}2_2_max_params "${MAX_PARAM_RESULT}" "${OP}.2" "${ARGS_2}" "${LOCALS_2}" "${CALL_2}" "${SIG_2}"
	./make_load_test.sh ${OP}3_2_max_params "${MAX_PARAM_RESULT}" "${OP}.3" "${ARGS_2}" "${LOCALS_2}" "${CALL_2}" "${SIG_2}"

	./make_load_test.sh ${OP}3_3_max_params "${MAX_PARAM_RESULT}" "${OP}.3" "${ARGS_3}" "${LOCALS_3}" "${CALL_3}" "${SIG_3}"

	./make_load_test.sh ${OP}0_max_params valid "${OP}.0" "${ARGS_1}" "${LOCALS_1}" "${CALL_1}" "${SIG_1}"
	./make_load_test.sh ${OP}1_max_params valid "${OP}.1" "${ARGS_2}" "${LOCALS_2}" "${CALL_2}" "${SIG_2}"
	./make_load_test.sh ${OP}2_max_params valid "${OP}.2" "${ARGS_3}" "${LOCALS_3}" "${CALL_3}" "${SIG_3}"
	./make_load_test.sh ${OP}3_max_params valid "${OP}.3" "${ARGS_4}" "${LOCALS_4}" "${CALL_4}" "${SIG_4}"

	./make_load_test.sh ${OP}0_stack_overflow invalid "${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${OP}.0\n${POPS}" "${ARGS_4}" "${LOCALS_4}" "${CALL_4}" "${SIG_4}"
	./make_load_test.sh ${OP}1_stack_overflow invalid "${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${OP}.1\n${POPS}" "${ARGS_4}" "${LOCALS_4}" "${CALL_4}" "${SIG_4}"
	./make_load_test.sh ${OP}2_stack_overflow invalid "${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${OP}.2\n${POPS}" "${ARGS_4}" "${LOCALS_4}" "${CALL_4}" "${SIG_4}"
	./make_load_test.sh ${OP}3_stack_overflow invalid "${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${OP}.3\n${POPS}" "${ARGS_4}" "${LOCALS_4}" "${CALL_4}" "${SIG_4}"
done

#Test if the values used for brtrue and brfalse are valid
I=1
for OP in brfalse brtrue 'brfalse.s' 'brtrue.s'
do
	./make_bool_branch_test.sh boolean_branch_${I}_1 valid ${OP} int8
	./make_bool_branch_test.sh boolean_branch_${I}_2 valid ${OP} int16
	./make_bool_branch_test.sh boolean_branch_${I}_3 valid ${OP} int32
	./make_bool_branch_test.sh boolean_branch_${I}_4 valid ${OP} int64
	./make_bool_branch_test.sh boolean_branch_${I}_5 valid ${OP} 'native int'

	#unmanaged pointers are not verifiable types, all ops on unmanaged pointers are unverifiable
	./make_bool_branch_test.sh boolean_branch_${I}_6 unverifiable ${OP} 'int32*'
	./make_bool_branch_test.sh boolean_branch_${I}_8 unverifiable ${OP} 'method int32 *(int32)'

	./make_bool_branch_test.sh boolean_branch_${I}_7 valid ${OP} 'int32&'
	./make_bool_branch_test.sh boolean_branch_${I}_9 valid ${OP} object
	./make_bool_branch_test.sh boolean_branch_${I}_10 valid ${OP} string
	./make_bool_branch_test.sh boolean_branch_${I}_11 valid ${OP} 'ClassA'
	./make_bool_branch_test.sh boolean_branch_${I}_12 valid ${OP} 'int32[]'
	./make_bool_branch_test.sh boolean_branch_${I}_13 valid ${OP} 'int32[,,]'
	./make_bool_branch_test.sh boolean_branch_${I}_14 valid ${OP} 'class Template`1<object>'
	./make_bool_branch_test.sh boolean_branch_${I}_15 valid ${OP} 'class Template`1<object>[]'
	./make_bool_branch_test.sh boolean_branch_${I}_16 valid ${OP} 'class Template`1<object>[,,]'

	./make_bool_branch_test.sh boolean_branch_${I}_17 unverifiable ${OP} float32
	./make_bool_branch_test.sh boolean_branch_${I}_18 unverifiable ${OP} float64
	./make_bool_branch_test.sh boolean_branch_${I}_19 unverifiable ${OP} 'class MyValueType'
	./make_bool_branch_test.sh boolean_branch_${I}_20 unverifiable ${OP} 'class ValueTypeTemplate`1<object>'

	./make_bool_branch_test.sh boolean_branch_${I}_21 valid ${OP} object "pop\n\tldnull"
	./make_bool_branch_test.sh boolean_branch_${I}_22 valid ${OP} MyValueType "pop\n\tldnull\n\tisinst MyValueType"

	I=`expr $I + 1`
done

#tests for field loading
I=1
for OP in 'ldfld' 'ldflda'
do
	./make_field_store_test.sh field_store_${I}_1 unverifiable "${OP} int32 ClassA::fld" int32 int32
	./make_field_store_test.sh field_store_${I}_2 unverifiable "${OP} int32 ClassA::fld" int32 'class ClassB' yes
	./make_field_store_test.sh field_store_${I}_3 unverifiable "${OP} int32 ClassA::fld" int32 object yes
	./make_field_store_test.sh field_store_${I}_4 unverifiable "${OP} int32 ClassA::fld" int32 'class MyValueType'
	./make_field_store_test.sh field_store_${I}_5 valid "${OP} int32 ClassA::fld" int32 'class ClassA' yes
	./make_field_store_test.sh field_store_${I}_6 valid "${OP} int32 ClassA::fld" int32 'class SubClass' yes
	#ldfld and ldflda works different with value objects, you cannot take the address of a value-object on the stack
	#./make_field_store_test.sh field_store_${I}_7 valid "${OP} int32 MyValueType::fld" int32 'class MyValueType'
	#Not usefull as it throws NRE
	#./make_field_store_test.sh field_store_${I}_8 valid "${OP} int32 MyValueType::fld" int32 'class MyValueType \&'
	./make_field_store_test.sh field_store_${I}_9 unverifiable "${OP} int32 MyValueType::fld" int32 'native int'
	./make_field_store_test.sh field_store_${I}_10 unverifiable "${OP} int32 MyValueType::fld" int32 'class MyValueType *'
	./make_field_store_test.sh field_store_${I}_11 unverifiable "${OP} int32 ClassA::fld" int32 'class ClassA *'
	#overlapped field tests should be done separately
	#./make_field_store_test.sh field_store_${I}_12 valid "${OP} int32 Overlapped::field1" int32 'class Overlapped' yes
	#./make_field_store_test.sh field_store_${I}_13 unverifiable "${OP} ClassA Overlapped::field1" 'class ClassA' 'class Overlapped' yes
	#./make_field_store_test.sh field_store_${I}_14 valid "${OP} int32 Overlapped::field1" int32 'class SubOverlapped' yes
	#./make_field_store_test.sh field_store_${I}_15 unverifiable "${OP} ClassA Overlapped::field1" 'class ClassA' 'class SubOverlapped' yes
	#./make_field_store_test.sh field_store_${I}_16 valid "${OP} int32 SubOverlapped::field6" int32 'class SubOverlapped' yes
	#./make_field_store_test.sh field_store_${I}_17 unverifiable "${OP} ClassA SubOverlapped::field6" 'class ClassA' 'class SubOverlapped' yes
	#./make_field_store_test.sh field_store_${I}_18 valid "${OP} int32 Overlapped::field10" int32 'class Overlapped' yes
	#./make_field_store_test.sh field_store_${I}_20 unverifiable "${OP} int32 Overlapped::field10" 'class ClassA' 'class Overlapped' yes

	./make_field_store_test.sh field_store_${I}_22 invalid "${OP} int32 ClassA::unknown_field" 'class ClassA' 'class ClassA' yes
	./make_field_store_test.sh field_store_${I}_23 unverifiable "${OP} int32 ClassA::const_field" int32 'int32 \&'

	./make_field_store_test.sh field_store_${I}_24 valid "${OP} int32 ClassA::sfld" int32 'class ClassA' yes
	I=`expr $I + 1`
done

./make_field_store_test.sh field_store_2_25 unverifiable 'ldflda int32 ClassA::const_field' int32 'class ClassA'

#tests form static field loading
I=1
for OP in 'ldsfld' 'ldsflda'
do
	#unknown field
	./make_field_store_test.sh static_field_store_${I}_1 invalid "${OP} int32 ClassA::unknown_field\n\tpop" 'class ClassA' 'class ClassA'
	#non static field
	./make_field_store_test.sh static_field_store_${I}_2 invalid "${OP} int32 ClassA::fld\n\tpop" 'class ClassA' 'class ClassA'
	#valid
	./make_field_store_test.sh static_field_store_${I}_3 valid "${OP} ClassA ClassA::sfld\n\tpop" 'class ClassA' 'class ClassA'
	I=`expr $I + 1`
done

./make_field_store_test.sh static_field_store_2_25 unverifiable 'ldsflda int32 ClassA::st_const_field\n\tpop' int32 'class ClassA'


#stfld with null values
./make_field_store_test.sh field_store_null_value valid "ldnull\n\tstfld string ClassA::fld\n\tldc.i4.0" 'string' 'class ClassA' yes
./make_field_store_test.sh field_store_null_object valid "pop\n\tldnull\n\tldnull\n\tstfld string ClassA::fld\n\tldc.i4.0" 'string' 'class ClassA' yes


./make_field_valuetype_test.sh value_type_field_load_1 valid 'ldfld int32 MyValueType::fld' 'ldloc.0'
./make_field_valuetype_test.sh value_type_field_load_2 unverifiable 'ldflda int32 MyValueType::fld' 'ldloc.0'
./make_field_valuetype_test.sh value_type_field_load_3 valid 'ldfld int32 MyValueType::fld' 'ldloca.s 0'
./make_field_valuetype_test.sh value_type_field_load_4 valid 'ldflda int32 MyValueType::fld' 'ldloca.s 0'

./make_field_valuetype_test.sh value_type_field_load_1 valid 'ldfld int32 MyValueType::fld' 'ldloc.1'
./make_field_valuetype_test.sh value_type_field_load_2 valid 'ldflda int32 MyValueType::fld' 'ldloc.1'
./make_field_valuetype_test.sh value_type_field_load_3 unverifiable 'ldfld int32 MyValueType::fld' 'ldloca.s 1'
./make_field_valuetype_test.sh value_type_field_load_4 unverifiable 'ldflda int32 MyValueType::fld' 'ldloca.s 1'



#Tests for access checks
#TODO tests with static calls
#TODO tests with multiple assemblies, involving friend assemblies, with and without matching public key

I=1
for OP in "callvirt instance int32 class Owner\/Nested::Target()" "call instance int32 class Owner\/Nested::Target()" "ldc.i4.0\n\t\tstfld int32 Owner\/Nested::fld\n\t\tldc.i4.0" "ldc.i4.0\n\t\tstsfld int32 Owner\/Nested::sfld" "ldsfld int32 Owner\/Nested::sfld\n\n\tpop" "ldfld int32 Owner\/Nested::fld" "ldsflda int32 Owner\/Nested::sfld\n\n\tpop" "ldflda int32 Owner\/Nested::fld"
do
	./make_nested_access_test.sh nested_access_check_1_${I} valid "$OP" public public no
	./make_nested_access_test.sh nested_access_check_2_${I} valid "$OP" public public yes
	./make_nested_access_test.sh nested_access_check_3_${I} unverifiable "$OP" public private no
	./make_nested_access_test.sh nested_access_check_4_${I} unverifiable "$OP" public private yes
	./make_nested_access_test.sh nested_access_check_5_${I} unverifiable "$OP" public family no
	./make_nested_access_test.sh nested_access_check_6_${I} unverifiable "$OP" public family yes
	./make_nested_access_test.sh nested_access_check_7_${I} valid "$OP" public assembly no
	./make_nested_access_test.sh nested_access_check_8_${I} valid "$OP" public assembly yes
	./make_nested_access_test.sh nested_access_check_9_${I} unverifiable "$OP" public famandassem no
	./make_nested_access_test.sh nested_access_check_a_${I} unverifiable "$OP" public famandassem yes
	./make_nested_access_test.sh nested_access_check_b_${I} valid "$OP" public famorassem no
	./make_nested_access_test.sh nested_access_check_c_${I} valid "$OP" public famorassem yes

	./make_nested_access_test.sh nested_access_check_11_${I} unverifiable "$OP" private public no
	./make_nested_access_test.sh nested_access_check_12_${I} unverifiable "$OP" private public yes
	./make_nested_access_test.sh nested_access_check_13_${I} unverifiable "$OP" private private no
	./make_nested_access_test.sh nested_access_check_14_${I} unverifiable "$OP" private private yes
	./make_nested_access_test.sh nested_access_check_15_${I} unverifiable "$OP" private family no
	./make_nested_access_test.sh nested_access_check_16_${I} unverifiable "$OP" private family yes
	./make_nested_access_test.sh nested_access_check_17_${I} unverifiable "$OP" private assembly no
	./make_nested_access_test.sh nested_access_check_18_${I} unverifiable "$OP" private assembly yes
	./make_nested_access_test.sh nested_access_check_19_${I} unverifiable "$OP" private famandassem no
	./make_nested_access_test.sh nested_access_check_1a_${I} unverifiable "$OP" private famandassem yes
	./make_nested_access_test.sh nested_access_check_1b_${I} unverifiable "$OP" private famorassem no
	./make_nested_access_test.sh nested_access_check_1c_${I} unverifiable "$OP" private famorassem yes

	./make_nested_access_test.sh nested_access_check_21_${I} unverifiable "$OP" family public no
	./make_nested_access_test.sh nested_access_check_22_${I} valid "$OP" family public yes
	./make_nested_access_test.sh nested_access_check_23_${I} unverifiable "$OP" family private no
	./make_nested_access_test.sh nested_access_check_24_${I} unverifiable "$OP" family private yes
	./make_nested_access_test.sh nested_access_check_25_${I} unverifiable "$OP" family family no
	./make_nested_access_test.sh nested_access_check_26_${I} unverifiable "$OP" family family yes
	./make_nested_access_test.sh nested_access_check_27_${I} unverifiable "$OP" family assembly no
	./make_nested_access_test.sh nested_access_check_28_${I} valid "$OP" family assembly yes
	./make_nested_access_test.sh nested_access_check_29_${I} unverifiable "$OP" family famandassem no
	./make_nested_access_test.sh nested_access_check_2a_${I} unverifiable "$OP" family famandassem yes
	./make_nested_access_test.sh nested_access_check_2b_${I} unverifiable "$OP" family famorassem no
	./make_nested_access_test.sh nested_access_check_2c_${I} valid "$OP" family famorassem yes

	./make_nested_access_test.sh nested_access_check_31_${I} valid "$OP" assembly public no
	./make_nested_access_test.sh nested_access_check_32_${I} valid "$OP" assembly public yes
	./make_nested_access_test.sh nested_access_check_33_${I} unverifiable "$OP" assembly private no
	./make_nested_access_test.sh nested_access_check_34_${I} unverifiable "$OP" assembly private yes
	./make_nested_access_test.sh nested_access_check_35_${I} unverifiable "$OP" assembly family no
	./make_nested_access_test.sh nested_access_check_36_${I} unverifiable "$OP" assembly family yes
	./make_nested_access_test.sh nested_access_check_37_${I} valid "$OP" assembly assembly no
	./make_nested_access_test.sh nested_access_check_38_${I} valid "$OP" assembly assembly yes
	./make_nested_access_test.sh nested_access_check_39_${I} unverifiable "$OP" assembly famandassem no
	./make_nested_access_test.sh nested_access_check_3a_${I} unverifiable "$OP" assembly famandassem yes
	./make_nested_access_test.sh nested_access_check_3b_${I} valid "$OP" assembly famorassem no
	./make_nested_access_test.sh nested_access_check_3c_${I} valid "$OP" assembly famorassem yes

	./make_nested_access_test.sh nested_access_check_41_${I} unverifiable "$OP" famandassem public no
	./make_nested_access_test.sh nested_access_check_42_${I} valid "$OP" famandassem public yes
	./make_nested_access_test.sh nested_access_check_43_${I} unverifiable "$OP" famandassem private no
	./make_nested_access_test.sh nested_access_check_44_${I} unverifiable "$OP" famandassem private yes
	./make_nested_access_test.sh nested_access_check_45_${I} unverifiable "$OP" famandassem family no
	./make_nested_access_test.sh nested_access_check_46_${I} unverifiable "$OP" famandassem family yes
	./make_nested_access_test.sh nested_access_check_47_${I} unverifiable "$OP" famandassem assembly no
	./make_nested_access_test.sh nested_access_check_48_${I} valid "$OP" famandassem assembly yes
	./make_nested_access_test.sh nested_access_check_49_${I} unverifiable "$OP" famandassem famandassem no
	./make_nested_access_test.sh nested_access_check_4a_${I} unverifiable "$OP" famandassem famandassem yes
	./make_nested_access_test.sh nested_access_check_4b_${I} unverifiable "$OP" famandassem famorassem no
	./make_nested_access_test.sh nested_access_check_4c_${I} valid "$OP" famandassem famorassem yes

	./make_nested_access_test.sh nested_access_check_51_${I} valid "$OP" famorassem public no
	./make_nested_access_test.sh nested_access_check_52_${I} valid "$OP" famorassem public yes
	./make_nested_access_test.sh nested_access_check_53_${I} unverifiable "$OP" famorassem private no
	./make_nested_access_test.sh nested_access_check_54_${I} unverifiable "$OP" famorassem private yes
	./make_nested_access_test.sh nested_access_check_55_${I} unverifiable "$OP" famorassem family no
	./make_nested_access_test.sh nested_access_check_56_${I} unverifiable "$OP" famorassem family yes
	./make_nested_access_test.sh nested_access_check_57_${I} valid "$OP" famorassem assembly no
	./make_nested_access_test.sh nested_access_check_58_${I} valid "$OP" famorassem assembly yes
	./make_nested_access_test.sh nested_access_check_59_${I} unverifiable "$OP" famorassem famandassem no
	./make_nested_access_test.sh nested_access_check_5a_${I} unverifiable "$OP" famorassem famandassem yes
	./make_nested_access_test.sh nested_access_check_5b_${I} valid "$OP" famorassem famorassem no
	./make_nested_access_test.sh nested_access_check_5c_${I} valid "$OP" famorassem famorassem yes
	I=`expr $I + 1`
done

#Tests for accessing an owned nested type
I=1
for OP in "callvirt instance int32 class Outer\/Inner::Target()" "call instance int32 class Outer\/Inner::Target()" "ldc.i4.0\n\t\tstfld int32 Outer\/Inner::fld\n\t\tldc.i4.0" "ldc.i4.0\n\t\tstsfld int32 Outer\/Inner::sfld" "ldsfld int32 Outer\/Inner::sfld\n\n\tpop" "ldfld int32 Outer\/Inner::fld" "ldsflda int32 Outer\/Inner::sfld\n\n\tpop" "ldflda int32 Outer\/Inner::fld"
do
	./make_self_nested_test.sh self_nested_access_check_1_${I} valid "$OP" public public
	./make_self_nested_test.sh self_nested_access_check_2_${I} unverifiable "$OP" public private
	./make_self_nested_test.sh self_nested_access_check_3_${I} unverifiable "$OP" public family
	./make_self_nested_test.sh self_nested_access_check_4_${I} valid "$OP" public assembly
	./make_self_nested_test.sh self_nested_access_check_5_${I} unverifiable "$OP" public famandassem
	./make_self_nested_test.sh self_nested_access_check_6_${I} valid "$OP" public famorassem

	./make_self_nested_test.sh self_nested_access_check_7_${I} valid "$OP" private public
	./make_self_nested_test.sh self_nested_access_check_8_${I} unverifiable "$OP" private private
	./make_self_nested_test.sh self_nested_access_check_9_${I} unverifiable "$OP" private family
	./make_self_nested_test.sh self_nested_access_check_10_${I} valid "$OP" private assembly
	./make_self_nested_test.sh self_nested_access_check_11_${I} unverifiable "$OP" private famandassem
	./make_self_nested_test.sh self_nested_access_check_12_${I} valid "$OP" private famorassem

	./make_self_nested_test.sh self_nested_access_check_13_${I} valid "$OP" family public
	./make_self_nested_test.sh self_nested_access_check_14_${I} unverifiable "$OP" family private
	./make_self_nested_test.sh self_nested_access_check_15_${I} unverifiable "$OP" family family
	./make_self_nested_test.sh self_nested_access_check_16_${I} valid "$OP" family assembly
	./make_self_nested_test.sh self_nested_access_check_17_${I} unverifiable "$OP" family famandassem
	./make_self_nested_test.sh self_nested_access_check_18_${I} valid "$OP" family famorassem

	./make_self_nested_test.sh self_nested_access_check_19_${I} valid "$OP" assembly public
	./make_self_nested_test.sh self_nested_access_check_20_${I} unverifiable "$OP" assembly private
	./make_self_nested_test.sh self_nested_access_check_21_${I} unverifiable "$OP" assembly family
	./make_self_nested_test.sh self_nested_access_check_22_${I} valid "$OP" assembly assembly
	./make_self_nested_test.sh self_nested_access_check_23_${I} unverifiable "$OP" assembly famandassem
	./make_self_nested_test.sh self_nested_access_check_24_${I} valid "$OP" assembly famorassem

	./make_self_nested_test.sh self_nested_access_check_25_${I} valid "$OP" famandassem public
	./make_self_nested_test.sh self_nested_access_check_26_${I} unverifiable "$OP" famandassem private
	./make_self_nested_test.sh self_nested_access_check_27_${I} unverifiable "$OP" famandassem family
	./make_self_nested_test.sh self_nested_access_check_28_${I} valid "$OP" famandassem assembly
	./make_self_nested_test.sh self_nested_access_check_29_${I} valid "$unverifiable" famandassem famandassem
	./make_self_nested_test.sh self_nested_access_check_30_${I} valid "$OP" famandassem famorassem

	./make_self_nested_test.sh self_nested_access_check_31_${I} valid "$OP" famorassem public
	./make_self_nested_test.sh self_nested_access_check_32_${I} unverifiable "$OP" famorassem private
	./make_self_nested_test.sh self_nested_access_check_33_${I} unverifiable "$OP" famorassem family
	./make_self_nested_test.sh self_nested_access_check_34_${I} valid "$OP" famorassem assembly
	./make_self_nested_test.sh self_nested_access_check_35_${I} unverifiable "$OP" famorassem famandassem
	./make_self_nested_test.sh self_nested_access_check_36_${I} valid "$OP" famorassem famorassem
	I=`expr $I + 1`
done


I=1
for OP in "ldc.i4.0\n\t\tstsfld int32 Owner\/Nested::sfld" "ldsfld int32 Owner\/Nested::sfld\n\n\tpop" "ldsflda int32 Owner\/Nested::sfld\n\n\tpop" "callvirt instance int32 class Owner\/Nested::Target()" "call instance int32 class Owner\/Nested::Target()" "ldc.i4.0\n\t\tstfld int32 Owner\/Nested::fld\n\t\tldc.i4.0" "ldfld int32 Owner\/Nested::fld" "ldflda int32 Owner\/Nested::fld"
do
	./make_cross_nested_access_test.sh cross_nested_access_check_1_${I} valid "$OP" public public no
	./make_cross_nested_access_test.sh cross_nested_access_check_2_${I} valid "$OP" public public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3_${I} unverifiable "$OP" public private no
	./make_cross_nested_access_test.sh cross_nested_access_check_4_${I} unverifiable "$OP" public private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5_${I} unverifiable "$OP" public family no
	./make_cross_nested_access_test.sh cross_nested_access_check_7_${I} valid "$OP" public assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_8_${I} valid "$OP" public assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_9_${I} unverifiable "$OP" public famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_b_${I} valid "$OP" public famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_c_${I} valid "$OP" public famorassem yes

	./make_cross_nested_access_test.sh cross_nested_access_check_11_${I} valid "$OP" private public no
	./make_cross_nested_access_test.sh cross_nested_access_check_12_${I} valid "$OP" private public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_13_${I} unverifiable "$OP" private private no
	./make_cross_nested_access_test.sh cross_nested_access_check_14_${I} unverifiable "$OP" private private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_15_${I} unverifiable "$OP" private family no
	./make_cross_nested_access_test.sh cross_nested_access_check_17_${I} valid "$OP" private assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_18_${I} valid "$OP" private assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_19_${I} unverifiable "$OP" private famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_1b_${I} valid "$OP" private famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_1c_${I} valid "$OP" private famorassem yes

	./make_cross_nested_access_test.sh cross_nested_access_check_21_${I} valid "$OP" family public no
	./make_cross_nested_access_test.sh cross_nested_access_check_22_${I} valid "$OP" family public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_23_${I} unverifiable "$OP" family private no
	./make_cross_nested_access_test.sh cross_nested_access_check_24_${I} unverifiable "$OP" family private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_25_${I} unverifiable "$OP" family family no
	./make_cross_nested_access_test.sh cross_nested_access_check_27_${I} valid "$OP" family assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_28_${I} valid "$OP" family assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_29_${I} unverifiable "$OP" family famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_2b_${I} valid "$OP" family famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_2c_${I} valid "$OP" family famorassem yes

	./make_cross_nested_access_test.sh cross_nested_access_check_31_${I} valid "$OP" assembly public no
	./make_cross_nested_access_test.sh cross_nested_access_check_32_${I} valid "$OP" assembly public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_33_${I} unverifiable "$OP" assembly private no
	./make_cross_nested_access_test.sh cross_nested_access_check_34_${I} unverifiable "$OP" assembly private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_35_${I} unverifiable "$OP" assembly family no
	./make_cross_nested_access_test.sh cross_nested_access_check_37_${I} valid "$OP" assembly assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_38_${I} valid "$OP" assembly assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_39_${I} unverifiable "$OP" assembly famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_3b_${I} valid "$OP" assembly famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_3c_${I} valid "$OP" assembly famorassem yes

	./make_cross_nested_access_test.sh cross_nested_access_check_41_${I} valid "$OP" famandassem public no
	./make_cross_nested_access_test.sh cross_nested_access_check_42_${I} valid "$OP" famandassem public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_43_${I} unverifiable "$OP" famandassem private no
	./make_cross_nested_access_test.sh cross_nested_access_check_44_${I} unverifiable "$OP" famandassem private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_45_${I} unverifiable "$OP" famandassem family no
	./make_cross_nested_access_test.sh cross_nested_access_check_47_${I} valid "$OP" famandassem assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_48_${I} valid "$OP" famandassem assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_49_${I} unverifiable "$OP" famandassem famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_4b_${I} valid "$OP" famandassem famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_4c_${I} valid "$OP" famandassem famorassem yes

	./make_cross_nested_access_test.sh cross_nested_access_check_51_${I} valid "$OP" famorassem public no
	./make_cross_nested_access_test.sh cross_nested_access_check_52_${I} valid "$OP" famorassem public yes
	./make_cross_nested_access_test.sh cross_nested_access_check_53_${I} unverifiable "$OP" famorassem private no
	./make_cross_nested_access_test.sh cross_nested_access_check_54_${I} unverifiable "$OP" famorassem private yes
	./make_cross_nested_access_test.sh cross_nested_access_check_55_${I} unverifiable "$OP" famorassem family no
	./make_cross_nested_access_test.sh cross_nested_access_check_57_${I} valid "$OP" famorassem assembly no
	./make_cross_nested_access_test.sh cross_nested_access_check_58_${I} valid "$OP" famorassem assembly yes
	./make_cross_nested_access_test.sh cross_nested_access_check_59_${I} unverifiable "$OP" famorassem famandassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_5b_${I} valid "$OP" famorassem famorassem no
	./make_cross_nested_access_test.sh cross_nested_access_check_5c_${I} valid "$OP" famorassem famorassem yes
	I=`expr $I + 1`
done


I=1
for OP in "callvirt instance int32 class Owner\/Nested::Target()" "call instance int32 class Owner\/Nested::Target()" "ldc.i4.0\n\t\tstfld int32 Owner\/Nested::fld\n\t\tldc.i4.0" "ldc.i4.0\n\t\tstsfld int32 Owner\/Nested::sfld" "ldsfld int32 Owner\/Nested::sfld\n\n\tpop" "ldfld int32 Owner\/Nested::fld" "ldsflda int32 Owner\/Nested::sfld\n\n\tpop" "ldflda int32 Owner\/Nested::fld"
do
	./make_cross_nested_access_test.sh cross_nested_access_check_2a_${I} valid "$OP" public public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4a_${I} unverifiable "$OP" public private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_8a_${I} valid "$OP" public assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_ca_${I} valid "$OP" public famorassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_12a_${I} valid "$OP" private public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_14a_${I} unverifiable "$OP" private private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_18a_${I} valid "$OP" private assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_1ca_${I} valid "$OP" private famorassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_22a_${I} valid "$OP" family public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_24a_${I} unverifiable "$OP" family private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_28a_${I} valid "$OP" family assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_2ca_${I} valid "$OP" family famorassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_32a_${I} valid "$OP" assembly public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_34a_${I} unverifiable "$OP" assembly private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_38a_${I} valid "$OP" assembly assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3ca_${I} valid "$OP" assembly famorassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_42a_${I} valid "$OP" famandassem public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_44a_${I} unverifiable "$OP" famandassem private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_48a_${I} valid "$OP" famandassem assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4ca_${I} valid "$OP" famandassem famorassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_52a_${I} valid "$OP" famorassem public yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_54a_${I} unverifiable "$OP" famorassem private yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_58a_${I} valid "$OP" famorassem assembly yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5ca_${I} valid "$OP" famorassem famorassem yes yes
	I=`expr $I + 1`
done


I=1
for OP in "callvirt instance int32 class Owner\/Nested::Target()" "call instance int32 class Owner\/Nested::Target()" "ldc.i4.0\n\t\tstfld int32 Owner\/Nested::fld\n\t\tldc.i4.0" "ldfld int32 Owner\/Nested::fld" "ldflda int32 Owner\/Nested::fld"
do
	./make_cross_nested_access_test.sh cross_nested_access_check_6_${I} unverifiable "$OP" public family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_6a_${I} valid "$OP" public family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_a_${I} unverifiable "$OP" public famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_aa_${I} valid "$OP" public famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_16_${I} unverifiable "$OP" private family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_16a_${I} valid "$OP" private family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_1a_${I} unverifiable "$OP" private famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_1aa_${I} valid "$OP" private famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_26_${I} unverifiable "$OP" family family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_26a_${I} valid "$OP" family family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_2a_${I} unverifiable "$OP" family famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_2aa_${I} valid "$OP" family famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_36_${I} unverifiable "$OP" assembly family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_36a_${I} valid "$OP" assembly family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3a_${I} unverifiable "$OP" assembly famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3aa_${I} valid "$OP" assembly famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_46_${I} unverifiable "$OP" famandassem family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_46a_${I} valid "$OP" famandassem family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4a_${I} unverifiable "$OP" famandassem famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4aa_${I} valid "$OP" famandassem famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_56_${I} unverifiable "$OP" famorassem family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_56a_${I} valid "$OP" famorassem family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5a_${I} unverifiable "$OP" famorassem famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5aa_${I} valid "$OP" famorassem famandassem yes yes

	I=`expr $I + 1`
done

for OP in "ldc.i4.0\n\t\tstsfld int32 Owner\/Nested::sfld" "ldsfld int32 Owner\/Nested::sfld\n\n\tpop" "ldsflda int32 Owner\/Nested::sfld\n\n\tpop"
do
	./make_cross_nested_access_test.sh cross_nested_access_check_6_${I} valid "$OP" public family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_6a_${I} valid "$OP" public family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_a_${I} valid "$OP" public famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_aa_${I} valid "$OP" public famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_16_${I} valid "$OP" private family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_16a_${I} valid "$OP" private family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_1a_${I} valid "$OP" private famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_1aa_${I} valid "$OP" private famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_26_${I} valid "$OP" family family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_26a_${I} valid "$OP" family family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_2a_${I} valid "$OP" family famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_2aa_${I} valid "$OP" family famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_36_${I} valid "$OP" assembly family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_36a_${I} valid "$OP" assembly family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3a_${I} valid "$OP" assembly famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_3aa_${I} valid "$OP" assembly famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_46_${I} valid "$OP" famandassem family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_46a_${I} valid "$OP" famandassem family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4a_${I} valid "$OP" famandassem famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_4aa_${I} valid "$OP" famandassem famandassem yes yes

	./make_cross_nested_access_test.sh cross_nested_access_check_56_${I} valid "$OP" famorassem family yes
	./make_cross_nested_access_test.sh cross_nested_access_check_56a_${I} valid "$OP" famorassem family yes yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5a_${I} valid "$OP" famorassem famandassem yes
	./make_cross_nested_access_test.sh cross_nested_access_check_5aa_${I} valid "$OP" famorassem famandassem yes yes

	I=`expr $I + 1`
done



I=1
for OP in "ldc.i4.0\n\t\tstfld int32 Class::fld\n\t\tldc.i4.0" "ldc.i4.0\n\t\tstsfld int32 Class::sfld" "ldsfld int32 Class::sfld\n\n\tpop" "ldfld int32 Class::fld" "ldsflda int32 Class::sfld\n\n\tpop" "ldflda int32 Class::fld" "call instance int32 Class::Method()" "callvirt int32 Class::Method()"
do
	./make_access_test.sh access_check_1_${I} valid "$OP" public public no
	./make_access_test.sh access_check_2_${I} valid "$OP" public public yes
	./make_access_test.sh access_check_3_${I} unverifiable "$OP" public private no
	./make_access_test.sh access_check_4_${I} unverifiable "$OP" public private yes
	./make_access_test.sh access_check_5_${I} unverifiable "$OP" public family no
	./make_access_test.sh access_check_7_${I} valid "$OP" public assembly no
	./make_access_test.sh access_check_8_${I} valid "$OP" public assembly yes
	./make_access_test.sh access_check_9_${I} unverifiable "$OP" public famandassem no
	./make_access_test.sh access_check_b_${I} valid "$OP" public famorassem no
	./make_access_test.sh access_check_c_${I} valid "$OP" public famorassem yes

	./make_access_test.sh access_check_11_${I} valid "$OP" private public no
	./make_access_test.sh access_check_12_${I} valid "$OP" private public yes
	./make_access_test.sh access_check_13_${I} unverifiable "$OP" private private no
	./make_access_test.sh access_check_14_${I} unverifiable "$OP" private private yes
	./make_access_test.sh access_check_15_${I} unverifiable "$OP" private family no
	./make_access_test.sh access_check_17_${I} valid "$OP" private assembly no
	./make_access_test.sh access_check_18_${I} valid "$OP" private assembly yes
	./make_access_test.sh access_check_19_${I} unverifiable "$OP" private famandassem no
	./make_access_test.sh access_check_1b_${I} valid "$OP" private famorassem no
	./make_access_test.sh access_check_1c_${I} valid "$OP" private famorassem yes

	./make_access_test.sh access_check_31_${I} valid "$OP" " " public no
	./make_access_test.sh access_check_32_${I} valid "$OP" " " public yes
	./make_access_test.sh access_check_33_${I} unverifiable "$OP" " " private no
	./make_access_test.sh access_check_34_${I} unverifiable "$OP" " " private yes
	./make_access_test.sh access_check_35_${I} unverifiable "$OP" " " family no
	./make_access_test.sh access_check_37_${I} valid "$OP" " " assembly no
	./make_access_test.sh access_check_38_${I} valid "$OP" " " assembly yes
	./make_access_test.sh access_check_39_${I} unverifiable "$OP" " " famandassem no
	./make_access_test.sh access_check_3b_${I} valid "$OP" " " famorassem no
	./make_access_test.sh access_check_3c_${I} valid "$OP" " " famorassem yes

	I=`expr $I + 1`
done

#static members are different from instance members
I=1
for OP in "ldc.i4.0\n\t\tstsfld int32 Class::sfld" "ldsfld int32 Class::sfld\n\n\tpop" "ldsflda int32 Class::sfld\n\n\tpop"
do
	./make_access_test.sh access_check_41_${I} valid "$OP" public family yes
	./make_access_test.sh access_check_42_${I} valid "$OP" public famandassem yes
	./make_access_test.sh access_check_43_${I} valid "$OP" private family yes
	./make_access_test.sh access_check_44_${I} valid "$OP" private famandassem yes
	./make_access_test.sh access_check_45_${I} valid "$OP" " " family yes
	./make_access_test.sh access_check_46_${I} valid "$OP" " " famandassem yes
	I=`expr $I + 1`
done

#try to access the base stuff directly
I=1
for OP in "ldc.i4.0\n\t\tstfld int32 Class::fld\n\t\tldc.i4.0" "ldfld int32 Class::fld" "ldflda int32 Class::fld" "call instance int32 Class::Method()" "callvirt int32 Class::Method()"
do
	./make_access_test.sh access_check_51_${I} unverifiable "$OP" public family yes
	./make_access_test.sh access_check_52_${I} unverifiable "$OP" public famandassem yes
	./make_access_test.sh access_check_53_${I} unverifiable "$OP" private family yes
	./make_access_test.sh access_check_54_${I} unverifiable "$OP" private famandassem yes
	./make_access_test.sh access_check_55_${I} unverifiable "$OP" " " family yes
	./make_access_test.sh access_check_56_${I} unverifiable "$OP" " " famandassem yes
	I=`expr $I + 1`
done

#try to access the subclass stuff
I=1
for OP in "ldc.i4.0\n\t\tstfld int32 Class::fld\n\t\tldc.i4.0" "ldfld int32 Class::fld" "ldflda int32 Class::fld" "call instance int32 Class::Method()" "callvirt int32 Class::Method()"
do
	./make_access_test.sh access_check_61_${I} valid "$OP" public family yes yes
	./make_access_test.sh access_check_62_${I} valid "$OP" public famandassem yes yes
	./make_access_test.sh access_check_63_${I} valid "$OP" private family yes yes
	./make_access_test.sh access_check_64_${I} valid "$OP" private famandassem yes yes
	./make_access_test.sh access_check_65_${I} valid "$OP" " " family yes yes
	./make_access_test.sh access_check_66_${I} valid "$OP" " " famandassem yes yes
	I=`expr $I + 1`
done


function create_nesting_test_same_result () {
  K=$1
  for BASE in yes no
  do
    for NESTED in yes no
      do
        for LOAD in yes no
        do
          if ! ( [ "$NESTED" = "no" ] && [ "$LOAD" = "yes" ] ) ; then
            ./make_double_nesting_test.sh double_nesting_access_check_${K}_$I $2 "$OP" $3 $4 $5 "$BASE" "$NESTED" "$LOAD"
            K=`expr $K + 1`
          fi
      done
    done
  done
}

function create_nesting_test_only_first_ok () {
  FIRST=$1
  K=$1
  for BASE in yes no
  do
    for NESTED in yes no
      do
        for LOAD in yes no
        do
          if ! ( [ "$NESTED" = "no" ] && [ "$LOAD" = "yes" ] ) ; then
	       EXPECT=unverifiable
           if [ "$FIRST" = "$K" ]; then
              EXPECT=valid
           fi
           ./make_double_nesting_test.sh double_nesting_access_check_${K}_$I $EXPECT "$OP" $2 $3 $4 "$BASE" "$NESTED" "$LOAD"
           K=`expr $K + 1`
         fi
     done
    done
  done
}

I=1

for OP in "callvirt instance int32 class Root\/Nested::Target()" "ldc.i4.0\n\t\tstfld int32 Root\/Nested::fld\n\t\tldc.i4.0" "ldfld int32 Root\/Nested::fld" "ldflda int32 Root\/Nested::fld"
do
  create_nesting_test_same_result 1 valid public assembly assembly

  ./make_double_nesting_test.sh double_nesting_access_check_7_$I valid "$OP" public assembly family yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_8_$I unverifiable "$OP" public assembly family yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_9_$I unverifiable "$OP" public assembly family yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_10_$I valid "$OP" public assembly family no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_11_$I unverifiable "$OP" public assembly family no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_12_$I unverifiable "$OP" public assembly family no no no

  ./make_double_nesting_test.sh double_nesting_access_check_13_$I valid "$OP" public assembly famandassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_14_$I unverifiable "$OP" public assembly famandassem yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_15_$I unverifiable "$OP" public assembly famandassem yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_16_$I valid "$OP" public assembly famandassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_17_$I unverifiable "$OP" public assembly famandassem no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_18_$I unverifiable "$OP" public assembly famandassem no no no

  create_nesting_test_same_result 19 valid public assembly famorassem
  create_nesting_test_same_result 25 unverifiable public assembly private
  create_nesting_test_same_result 31 valid public assembly public

  ./make_double_nesting_test.sh double_nesting_access_check_37_$I valid "$OP" public family assembly yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_38_$I valid "$OP" public family assembly yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_39_$I valid "$OP" public family assembly yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_40_$I unverifiable "$OP" public family assembly no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_41_$I unverifiable "$OP" public family assembly no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_42_$I unverifiable "$OP" public family assembly no no no

  create_nesting_test_only_first_ok 43 public family family
  create_nesting_test_only_first_ok 49 public family famandassem

  ./make_double_nesting_test.sh double_nesting_access_check_55_$I valid "$OP" public family famorassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_56_$I valid "$OP" public family famorassem yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_57_$I valid "$OP" public family famorassem yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_58_$I unverifiable "$OP" public family famorassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_59_$I unverifiable "$OP" public family famorassem no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_60_$I unverifiable "$OP" public family famorassem no no no

   create_nesting_test_same_result 61 unverifiable public family private

  ./make_double_nesting_test.sh double_nesting_access_check_67_$I valid "$OP" public family public yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_68_$I valid "$OP" public family public yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_69_$I valid "$OP" public family public yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_70_$I unverifiable "$OP" public family public no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_71_$I unverifiable "$OP" public family public no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_72_$I unverifiable "$OP" public family public no no no

  ./make_double_nesting_test.sh double_nesting_access_check_73_$I valid "$OP" public famandassem assembly yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_74_$I valid "$OP" public famandassem assembly yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_75_$I valid "$OP" public famandassem assembly yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_76_$I unverifiable "$OP" public famandassem assembly no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_77_$I unverifiable "$OP" public famandassem assembly no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_78_$I unverifiable "$OP" public famandassem assembly no no no

  create_nesting_test_only_first_ok 79  public famandassem family
  create_nesting_test_only_first_ok 85  public famandassem famandassem

  ./make_double_nesting_test.sh double_nesting_access_check_91_$I valid "$OP" public famandassem famorassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_92_$I valid "$OP" public famandassem famorassem yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_93_$I valid "$OP" public famandassem famorassem yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_94_$I unverifiable "$OP" public famandassem famorassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_95_$I unverifiable "$OP" public famandassem famorassem no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_96_$I unverifiable "$OP" public famandassem famorassem no no no

   create_nesting_test_same_result 97 unverifiable public famandassem private

  ./make_double_nesting_test.sh double_nesting_access_check_103_$I valid "$OP" public famandassem public yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_104_$I valid "$OP" public famandassem public yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_105_$I valid "$OP" public famandassem public yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_106_$I unverifiable "$OP" public famandassem public no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_107_$I unverifiable "$OP" public famandassem public no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_108_$I unverifiable "$OP" public famandassem public no no no

  create_nesting_test_same_result 109 valid public famorassem assembly

  ./make_double_nesting_test.sh double_nesting_access_check_115_$I valid "$OP" public famorassem family yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_116_$I unverifiable "$OP" public famorassem family yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_117_$I unverifiable "$OP" public famorassem family yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_118_$I valid "$OP" public famorassem family no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_119_$I unverifiable "$OP" public famorassem family no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_120_$I unverifiable "$OP" public famorassem family no no no

  ./make_double_nesting_test.sh double_nesting_access_check_121_$I valid "$OP" public famorassem famandassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_122_$I unverifiable "$OP" public famorassem famandassem yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_123_$I unverifiable "$OP" public famorassem famandassem yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_124_$I valid "$OP" public famorassem famandassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_125_$I unverifiable "$OP" public famorassem famandassem no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_126_$I unverifiable "$OP" public famorassem famandassem no no no

  create_nesting_test_same_result 127 valid public famorassem famorassem
  create_nesting_test_same_result 133 unverifiable public famorassem private
  create_nesting_test_same_result 139 valid public famorassem public
  create_nesting_test_same_result 145 unverifiable public private assembly
  create_nesting_test_same_result 151 unverifiable public private family
  create_nesting_test_same_result 157 unverifiable public private famandassem
  create_nesting_test_same_result 163 unverifiable public private famorassem
  create_nesting_test_same_result 169 unverifiable public private private
  create_nesting_test_same_result 175 unverifiable public private public
  create_nesting_test_same_result 181 valid public public assembly

  ./make_double_nesting_test.sh double_nesting_access_check_187_$I valid "$OP" public public family yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_188_$I unverifiable "$OP" public public family yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_189_$I unverifiable "$OP" public public family yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_190_$I valid "$OP" public public family no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_191_$I unverifiable "$OP" public public family no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_192_$I unverifiable "$OP" public public family no no no

  ./make_double_nesting_test.sh double_nesting_access_check_193_$I valid "$OP" public public famandassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_194_$I unverifiable "$OP" public public famandassem yes yes no
  ./make_double_nesting_test.sh double_nesting_access_check_195_$I unverifiable "$OP" public public famandassem yes no no
  ./make_double_nesting_test.sh double_nesting_access_check_196_$I valid "$OP" public public famandassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_197_$I unverifiable "$OP" public public famandassem no yes no
  ./make_double_nesting_test.sh double_nesting_access_check_198_$I unverifiable "$OP" public public famandassem no no no

  create_nesting_test_same_result 199 valid public public famorassem
  create_nesting_test_same_result 205 unverifiable public public private
  create_nesting_test_same_result 211 valid public public public
  I=`expr $I + 1`
done

function create_nesting_test_same_result_static () {
  K=$1
  for BASE in yes no
  do
    for NESTED in yes no
      do
        ./make_double_nesting_test.sh double_nesting_access_check_${K}_$I $2 "$OP" $3 $4 $5 "$BASE" "$NESTED" yes
        K=`expr $K + 1`
    done
  done
}

function create_nesting_test_strips_result_static () {
  K=$1
  for BASE in yes no
  do
    for NESTED in yes no
      do
        EXPECT=unverifiable
        if [ "$NESTED" = "yes" ]; then
          EXPECT=valid
        fi
        ./make_double_nesting_test.sh double_nesting_access_check_${K}_$I $EXPECT "$OP" $2 $3 $4 "$BASE" "$NESTED" yes
        K=`expr $K + 1`
    done
  done
}

for OP in "ldc.i4.0\n\t\tstsfld int32 Root\/Nested::sfld" "ldsfld int32 Root\/Nested::sfld\n\n\tpop" "ldsflda int32 Root\/Nested::sfld\n\n\tpop"
do
   create_nesting_test_same_result 1 valid public assembly assembly

  create_nesting_test_strips_result_static 5 public assembly family
  create_nesting_test_strips_result_static 9 public assembly family

  create_nesting_test_same_result 13 valid public assembly famorassem
  create_nesting_test_same_result 17 unverifiable public assembly private
  create_nesting_test_same_result 21 valid public assembly public

  ./make_double_nesting_test.sh double_nesting_access_check_25_$I valid "$OP" public family assembly yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_26_$I valid "$OP" public family assembly yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_27_$I unverifiable "$OP" public family assembly no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_27_$I unverifiable "$OP" public family assembly no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_29_$I valid "$OP" public family family yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_30_$I unverifiable "$OP" public family family yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_31_$I unverifiable "$OP" public family family no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_32_$I unverifiable "$OP" public family family no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_33_$I valid "$OP" public family famandassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_34_$I unverifiable "$OP" public family famandassem yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_35_$I unverifiable "$OP" public family famandassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_36_$I unverifiable "$OP" public family famandassem no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_37_$I valid "$OP" public family famorassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_38_$I valid "$OP" public family famorassem yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_39_$I unverifiable "$OP" public family famorassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_40_$I unverifiable "$OP" public family famorassem no no yes

   create_nesting_test_same_result 41 unverifiable public family private

  ./make_double_nesting_test.sh double_nesting_access_check_45_$I valid "$OP" public family public yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_46_$I valid "$OP" public family public yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_47_$I unverifiable "$OP" public family public no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_48_$I unverifiable "$OP" public family public no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_49_$I valid "$OP" public famandassem assembly yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_50_$I valid "$OP" public famandassem assembly yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_51_$I unverifiable "$OP" public famandassem assembly no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_52_$I unverifiable "$OP" public famandassem assembly no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_53_$I valid "$OP" public famandassem family yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_54_$I unverifiable "$OP" public famandassem family yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_55_$I unverifiable "$OP" public famandassem family no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_56_$I unverifiable "$OP" public famandassem family no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_57_$I valid "$OP" public famandassem famandassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_58_$I unverifiable "$OP" public famandassem famandassem yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_59_$I unverifiable "$OP" public famandassem famandassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_60_$I unverifiable "$OP" public famandassem famandassem no no yes

  ./make_double_nesting_test.sh double_nesting_access_check_61_$I valid "$OP" public famandassem famorassem yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_62_$I valid "$OP" public famandassem famorassem yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_63_$I unverifiable "$OP" public famandassem famorassem no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_64_$I unverifiable "$OP" public famandassem famorassem no no yes

  create_nesting_test_same_result 65 unverifiable public famandassem private

  ./make_double_nesting_test.sh double_nesting_access_check_69_$I valid "$OP" public famandassem public yes yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_70_$I valid "$OP" public famandassem public yes no yes
  ./make_double_nesting_test.sh double_nesting_access_check_71_$I unverifiable "$OP" public famandassem public no yes yes
  ./make_double_nesting_test.sh double_nesting_access_check_72_$I unverifiable "$OP" public famandassem public no no yes

  create_nesting_test_same_result 73 valid public famorassem assembly
  create_nesting_test_strips_result_static 77 public famorassem family
  create_nesting_test_strips_result_static 81 public famorassem famandassem

  create_nesting_test_same_result 85 valid public famorassem famorassem
  create_nesting_test_same_result 89 unverifiable public famorassem private
  create_nesting_test_same_result 93 valid public famorassem public
  create_nesting_test_same_result 97 unverifiable public private assembly
  create_nesting_test_same_result 101 unverifiable public private family
  create_nesting_test_same_result 105 unverifiable public private famandassem
  create_nesting_test_same_result 109 unverifiable public private famorassem
  create_nesting_test_same_result 113 unverifiable public private private
  create_nesting_test_same_result 117 unverifiable public private public
  create_nesting_test_same_result 121 valid public public assembly
  create_nesting_test_strips_result_static 125 public public family
  create_nesting_test_strips_result_static 129 public public famandassem
  create_nesting_test_same_result 133 valid public public famorassem
  create_nesting_test_same_result 137 unverifiable public public private
  create_nesting_test_same_result 141 valid public public public

  I=`expr $I + 1`
done


#ldtoken tests

./make_ldtoken_test.sh ldtoken_class valid "ldtoken class Example" "call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)"

./make_ldtoken_test.sh ldtoken_class invalid "ldtoken class [mscorlib]ExampleMM" "call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)"

./make_ldtoken_test.sh ldtoken_field valid "ldtoken field int32 Example::fld" "call class [mscorlib]System.Reflection.FieldInfo class [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle)"

./make_ldtoken_test.sh ldtoken_field invalid "ldtoken field int32 Example::MM" "call class [mscorlib]System.Reflection.FieldInfo class [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle)"

./make_ldtoken_test.sh ldtoken_method valid "ldtoken method void Example::Method()" "call class [mscorlib]System.Reflection.MethodBase class [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(valuetype [mscorlib]System.RuntimeMethodHandle)"

./make_ldtoken_test.sh ldtoken_method invalid "ldtoken method int32 Example::Method()" "call class [mscorlib]System.Reflection.MethodBase class [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(valuetype [mscorlib]System.RuntimeMethodHandle)"


#ldobj tests
function fix_ldobj () {
	if [ -n "$3" ]; then
		A="$3";
	elif [ -n "$2" ]; then
		A="$2";
	else
		A="$1";
	fi

	if [ "$A" = "bool" ]; then
		A="int8";
	elif [ "$A" = "char" ]; then
		A="int16";
	fi

	echo "$A";
}


I=1

#valid
for T1 in 'int8' 'bool' 'unsigned int8' 'int16' 'char' 'unsigned int16' 'int32' 'unsigned int32' 'int64' 'unsigned int64' 'float32' 'float64'
do
	for T2 in 'int8' 'bool' 'unsigned int8' 'int16' 'char' 'unsigned int16' 'int32' 'unsigned int32' 'int64' 'unsigned int64' 'float32' 'float64'
	do
		TYPE1="$(fix_ldobj $T1)"
		TYPE2="$(fix_ldobj $T2)"
		if [ "$TYPE1" = "$TYPE2" ]; then
			./make_ldobj_test.sh ldobj_${I} valid "${T1}\&" "${T2}"
		else
			./make_ldobj_test.sh ldobj_${I} unverifiable "${T1}\&" "${T2}"
		fi
		I=`expr $I + 1`
	done
done



#unverifiable
#for T1 in "int8" "int64" "float64" "object" "string" "class Class" "int32[]" "int32[,]" "valuetype MyStruct" "valuetype MyStruct2" "int32 *" "valuetype MyStruct *" "method int32 *(int32)"
for T1 in "native int" "int8*" "typedref"
do
	for T2 in "int8" "int64" "float64" "object" "string" "class Class" "int32[]" "int32[,]" "valuetype MyStruct" "valuetype MyStruct2"   "int32 *" "valuetype MyStruct *" "method int32 *(int32)" "native int"  "typedref" "class Template\`1<object>" "valuetype StructTemplate\`1<object>" "valuetype StructTemplate2\`1<object>"
	do
		./make_ldobj_test.sh ldobj_${I} unverifiable "${T1}" "${T2}"
		I=`expr $I + 1`
	done
done

for T1 in "native int" "int8*" "typedref"
do
	./make_ldobj_test.sh ldobj_${I} invalid "${T1}" "typedref\&"
	I=`expr $I + 1`
done



#invalid
#for T1 in "int8" "int64" "float64" "object" "string" "class Class" "int32[]" "int32[,]" "valuetype MyStruct" "valuetype MyStruct2" "int32 *" "valuetype MyStruct *" "method int32 *(int32)"
for T1 in 'int8' 'native int'
do
	for T2 in "int8\&" "int64\&" "float64\&" "object\&" "string\&" "class Class\&" "valuetype MyStruct\&" "native int\&" "class Template\`1<object>\&" "valuetype StructTemplate\`1<object>\&"  "valuetype StructTemplate2\`1<object>\&" "class [mscorlib]ExampleMM" "class [mscorlib]ExampleMM\&"
	do
		./make_ldobj_test.sh ldobj_${I} invalid "${T1}" "${T2}"
		I=`expr $I + 1`
	done
done

./make_ldobj_test.sh ldobj_struct_1 valid  "valuetype MyStruct\&" "valuetype MyStruct"
./make_ldobj_test.sh ldobj_struct_2 unverifiable  "valuetype MyStruct\&" "valuetype MyStruct2"
./make_ldobj_test.sh ldobj_struct_3 valid  "valuetype StructTemplate\`1<object>\&" "valuetype StructTemplate\`1<object>"
./make_ldobj_test.sh ldobj_struct_4 unverifiable  "valuetype StructTemplate\`1<object>\&" "valuetype StructTemplate2\`1<object>"

./make_ldobj_test.sh ldobj_struct_5 valid  "object\&"  "object"
./make_ldobj_test.sh ldobj_struct_6 valid  "string\&"  "string"
./make_ldobj_test.sh ldobj_struct_7 valid  "int32[]\&"  "int32[]"
./make_ldobj_test.sh ldobj_struct_8 valid  "int32[,]\&"  "int32[,]"
./make_ldobj_test.sh ldobj_struct_9 valid  "class Template\`1<object>\&"  "class Template\`1<object>"


# Unbox Test


# unbox non-existent type.
./make_unbox_test.sh unbox_bad_type invalid "valuetype [mscorlib]NonExistent" "valuetype [mscorlib]NonExistent"

# Unbox byref type.
./make_unbox_test.sh unbox_byref_type invalid "int32" 'int32\&'

# Box unbox-like type.
./make_unbox_test.sh unbox_byref_like unverifiable typedref typedref

# Box unbox-like type.
./make_unbox_test.sh unbox_wrong_types valid object int32

#This is illegal since you cannot have a Void local variable, it should go into the structural tests part
# Box void type.
#./make_unary_test.sh box_void unverifiable "box [mscorlib]System.Void\n\tpop" "class [mscorlib]System.Void"
I=1;
for OP in "native int" "int32*" typedref int16 float32
do
	./make_unbox_test.sh unbox_bad_stack_${I} unverifiable "${OP}" int32 "nop" "yes"
	I=`expr $I + 1`
done


#unboxing from int32
./make_unbox_test.sh unbox_wrong_types_1 unverifiable int32 int32 "nop" "yes"

#unboxing from valuetype
./make_unbox_test.sh unbox_wrong_types_2 unverifiable "valuetype MyStruct" int32 "nop" "yes"

#unboxing from managed ref
./make_unbox_test.sh unbox_stack_byref unverifiable "valuetype MyEnum\&" "valuetype MyEnum" "nop" "yes"

# valid unboxing
./make_unbox_test.sh unbox_primitive valid "int32" "int32"
./make_unbox_test.sh unbox_struct valid "valuetype MyStruct" 'valuetype MyStruct'
./make_unbox_test.sh unbox_template valid "valuetype StructTemplate\`1<object>" "valuetype StructTemplate\`1<object>"
./make_unbox_test.sh unbox_enum valid "valuetype MyEnum" "valuetype MyEnum"


#test if the unboxed value is right
./make_unbox_test.sh unbox_use_result_1 valid "valuetype MyStruct" "valuetype MyStruct" "ldfld int32 MyStruct::foo"
./make_unbox_test.sh unbox_use_result_2 valid "valuetype MyStruct" "valuetype MyStruct" "ldobj valuetype MyStruct\n\tstloc.0\n\tldc.i4.0"
./make_unbox_test.sh unbox_use_result_3 valid "int32" "int32" "ldind.i4\n\tstloc.1\n\tldc.i4.0"


# newarray Test

#no int size on stack
#invalid size type on stack
#invalid array type (with bytref)

#Empty stack
./make_newarr_test.sh newarr_empty_stack invalid int32 int32 pop

#Stack type tests
./make_newarr_test.sh newarr_stack_type_1 valid int32 int32
./make_newarr_test.sh newarr_stack_type_2 valid "native int" int32

./make_newarr_test.sh newarr_stack_type_3 unverifiable float32 int32
./make_newarr_test.sh newarr_stack_type_4 unverifiable object int32

#Invalid array element type (with byref)
./make_newarr_test.sh newarr_array_type_1 invalid int32 "int32\&"

#Check if the verifier push the right type on stack
./make_newarr_test.sh newarr_array_value valid int32 int32 nop "ldc.i4.0\n\tcallvirt instance int32 class [mscorlib]System.Array::GetLength(int32)"




#Tests for ldind.X
I=1
for OP in "ldind.i1" "ldind.u1"
do
	for TYPE in "int8" "bool" "unsigned int8"
	do
		./make_load_indirect_test.sh indirect_load_i1_${I} valid "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int16" "char" "unsigned int16" "int32" "unsigned int32" "int64" "unsigned int64" "native int" "native unsigned int" "object" "string" "float32" "float64" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
	do
		./make_load_indirect_test.sh indirect_load_i1_${I} unverifiable "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done
done

I=1
for OP in "ldind.i2" "ldind.u2"
do
	for TYPE in "int16" "char" "unsigned int16"
	do
		./make_load_indirect_test.sh indirect_load_i2_${I} valid "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int8" "bool" "unsigned int8" "int32" "unsigned int32" "int64" "unsigned int64" "native int" "native unsigned int" "object" "string" "float32" "float64" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
	do
		./make_load_indirect_test.sh indirect_load_i2_${I} unverifiable "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done
done

I=1
for OP in "ldind.i4" "ldind.u4"
do
	for TYPE in "int32" "unsigned int32" "native int" "native unsigned int"
	do
		./make_load_indirect_test.sh indirect_load_i4_${I} valid "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int64" "unsigned int64" "object" "string" "float32" "float64" "class Class" "valuetype MyStruct" "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
	do
		./make_load_indirect_test.sh indirect_load_i4_${I} unverifiable "${OP}" "${TYPE}"
		I=`expr $I + 1`
	done
done


#no need to test ldind.u8 as it aliases to ldind.i8
I=1
for TYPE in "int64" "unsigned int64"
do
	./make_load_indirect_test.sh indirect_load_i8_${I} valid "ldind.i8" "${TYPE}"
	I=`expr $I + 1`
done

for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "object" "string" "float32" "float64" "class Class" "valuetype MyStruct" "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_load_indirect_test.sh indirect_load_i8_${I} unverifiable "ldind.i8" "${TYPE}"
	I=`expr $I + 1`
done


I=1
for TYPE in "float32"
do
	./make_load_indirect_test.sh indirect_load_r4_${I} valid "ldind.r4" "${TYPE}"
	I=`expr $I + 1`
done

for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "int64" "unsigned int64" "float64" "native int" "native unsigned int" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_load_indirect_test.sh indirect_load_r4_${I} unverifiable "ldind.r4" "${TYPE}"
	I=`expr $I + 1`
done


I=1
for TYPE in "float64"
do
	./make_load_indirect_test.sh indirect_load_r8_${I} valid "ldind.r8" "${TYPE}"
	I=`expr $I + 1`
done

for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "int64" "unsigned int64" "float32" "native int" "native unsigned int" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_load_indirect_test.sh indirect_load_r8_${I} unverifiable "ldind.r8" "${TYPE}"
	I=`expr $I + 1`
done


I=1
for TYPE in "int32" "unsigned int32" "native int" "native unsigned int"
do
	./make_load_indirect_test.sh indirect_load_i_${I} valid "ldind.i" "${TYPE}"
	I=`expr $I + 1`
done

for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int64" "unsigned int64" "float32" "float64" "object" "string" "class Class" "valuetype MyStruct" "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_load_indirect_test.sh indirect_load_i_${I} unverifiable "ldind.i" "${TYPE}"
	I=`expr $I + 1`
done


I=1
for TYPE in "object" "string" "class Class"  "int32[]" "int32[,]" "class Template\`1<object>"
do
	./make_load_indirect_test.sh indirect_load_r_${I} valid "ldind.ref" "${TYPE}"
	I=`expr $I + 1`
done

for TYPE in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "int64" "unsigned int64" "native int" "native unsigned int" "float32" "float64" "valuetype MyStruct" "int32*" "method int32 *(int32)"
do
	./make_load_indirect_test.sh indirect_load_r_${I} unverifiable "ldind.ref" "${TYPE}"
	I=`expr $I + 1`
done


#valid stores
./make_store_indirect_test.sh indirect_store_i1_1 valid "stind.i1" "int8\&" "int8"
./make_store_indirect_test.sh indirect_store_i1_2 valid "stind.i1" "bool\&" "int8"
./make_store_indirect_test.sh indirect_store_i1_3 valid "stind.i1" "int8\&" "bool"
./make_store_indirect_test.sh indirect_store_i1_4 valid "stind.i1" "bool\&" "bool"

./make_store_indirect_test.sh indirect_store_i2_1 valid "stind.i2" "int16\&" "int16"
./make_store_indirect_test.sh indirect_store_i2_2 valid "stind.i2" "char\&" "int16"
./make_store_indirect_test.sh indirect_store_i2_3 valid "stind.i2" "int16\&" "char"
./make_store_indirect_test.sh indirect_store_i2_4 valid "stind.i2" "char\&" "char"

./make_store_indirect_test.sh indirect_store_i4_1 valid "stind.i4" "int32\&" "int32"
./make_store_indirect_test.sh indirect_store_i4_2 valid "stind.i4" "native int\&" "int32"
./make_store_indirect_test.sh indirect_store_i4_3 valid "stind.i4" "int32\&" "native int"
./make_store_indirect_test.sh indirect_store_i4_4 valid "stind.i4" "native int\&" "native int"


./make_store_indirect_test.sh indirect_store_i8_1 valid "stind.i8" "int64\&" "int64"

./make_store_indirect_test.sh indirect_store_r4_1 valid "stind.r4" "float32\&" "float32"

./make_store_indirect_test.sh indirect_store_r8_1 valid "stind.r8" "float64\&" "float64"

./make_store_indirect_test.sh indirect_store_i_1 valid "stind.i" "native int\&" "int32"
./make_store_indirect_test.sh indirect_store_i_2 valid "stind.i" "int32\&" "int32"
./make_store_indirect_test.sh indirect_store_i_3 valid "stind.i" "native int\&" "native int"
./make_store_indirect_test.sh indirect_store_i_4 valid "stind.i" "int32\&" "native int"

./make_store_indirect_test.sh indirect_store_r_1 valid "stind.ref" "object\&" "object"
./make_store_indirect_test.sh indirect_store_r_2 valid "stind.ref" "object\&" "string"
./make_store_indirect_test.sh indirect_store_r_3 valid "stind.ref" "string\&" "string"
./make_store_indirect_test.sh indirect_store_r_4 unverifiable "stind.ref" "valuetype MyStruct\&" "MyStruct"




#stdind tests
#unverifiable due to unmanaged pointers
./make_store_indirect_test.sh indirect_store_unmanaged_pointer_1 unverifiable "stind.i1" "int8*" "int8"
./make_store_indirect_test.sh indirect_store_unmanaged_pointer_2 unverifiable "stind.i1" "native int" "int8"

#invalid due to unrelated types on stack
./make_store_indirect_test.sh indirect_store_bad_type_1 unverifiable "stind.i1" "int8" "int8"
./make_store_indirect_test.sh indirect_store_bad_type_2 unverifiable "stind.i1" "int8" "int8"

#invalid stind.ref with valuetypes
./make_store_indirect_test.sh indirect_store_bad_type_r_3 valid "stind.ref" "int32[]\&" "int32[]"

#invalid operands
I=1
for TYPE1 in "int16" "char" "int32" "native int"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i1_${I} unverifiable "stind.i1" "${TYPE1}\&" "int8"
	./make_store_indirect_test.sh indirect_store_good_val_i1_${I} valid "stind.i1" "int8\&" "${TYPE1}"
	I=`expr $I + 1`
done

for TYPE1 in  "int64" "float32" "float64" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i1_${I} unverifiable "stind.i1" "${TYPE1}\&" "int8"
	./make_store_indirect_test.sh indirect_store_bad_val_i1_${I} unverifiable "stind.i1" "int8\&" "${TYPE1}"
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int32" "native int"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i2_${I} unverifiable "stind.i2" "${TYPE1}\&" "int16"
	./make_store_indirect_test.sh indirect_store_good_val_i2_${I} valid "stind.i2" "int16\&" "${TYPE1}"
	I=`expr $I + 1`
done

for TYPE1 in "int64" "float32" "float64" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i2_${I} unverifiable "stind.i2" "${TYPE1}\&" "int16"
	./make_store_indirect_test.sh indirect_store_bad_val_i2_${I} unverifiable "stind.i2" "int16\&" "${TYPE1}"
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i4_${I} unverifiable "stind.i4" "${TYPE1}\&" "int32"
	./make_store_indirect_test.sh indirect_store_good_val_i4_${I} valid "stind.i4" "int32\&" "${TYPE1}"
	I=`expr $I + 1`
done

for TYPE1 in  "int64" "float32" "float64" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i4_${I} unverifiable "stind.i4" "${TYPE1}\&" "int32"
	./make_store_indirect_test.sh indirect_store_bad_val_i4_${I} unverifiable "stind.i4" "int32\&" "${TYPE1}"
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char" "int32" "float32" "float64" "native int" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i8_${I} unverifiable "stind.i8" "${TYPE1}\&" "int64"
	./make_store_indirect_test.sh indirect_store_bad_val_i8_${I} unverifiable "stind.i8" "int64\&" "${TYPE1}"
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char" "int32" "int64" "float64" "native int" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_r4_${I} unverifiable "stind.r4" "${TYPE1}\&" "float32"
	if [ "$TYPE1" = "float64" ]; then
		./make_store_indirect_test.sh indirect_store_good_val_r4_${I} valid "stind.r4" "float32\&" "${TYPE1}"
	else
		./make_store_indirect_test.sh indirect_store_bad_val_r4_${I} unverifiable "stind.r4" "float32\&" "${TYPE1}"
	fi
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char" "int32" "int64" "float32" "native int" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_r8_${I} unverifiable "stind.r8" "${TYPE1}\&" "float64"
	if [ "$TYPE1" = "float32" ]; then
		./make_store_indirect_test.sh indirect_store_good_val_r8_${I} valid "stind.r8" "float64\&" "${TYPE1}";
	else
		./make_store_indirect_test.sh indirect_store_bad_val_r8_${I} unverifiable "stind.r8" "float64\&" "${TYPE1}";
	fi
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i_${I} unverifiable "stind.i" "${TYPE1}\&" "native int"
	./make_store_indirect_test.sh indirect_store_good_val_i_${I} valid "stind.i" "native int\&" "${TYPE1}"
	I=`expr $I + 1`
done

for TYPE1 in "int64" "float32" "float64" "object" "string" "class Class" "valuetype MyStruct"  "int32[]" "int32[,]" "int32*" "method int32 *(int32)"  "class Template\`1<object>"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_i_${I} unverifiable "stind.i" "${TYPE1}\&" "native int"
	./make_store_indirect_test.sh indirect_store_bad_val_i_${I} unverifiable "stind.i" "native int\&" "${TYPE1}"
	I=`expr $I + 1`
done


I=1
for TYPE1 in "int8" "bool" "int16" "char" "int32" "int64" "float32" "float64" "native int"
do
	./make_store_indirect_test.sh indirect_store_bad_addr_ref_${I} unverifiable "stind.ref" "${TYPE1}\&" "object"
	./make_store_indirect_test.sh indirect_store_bad_val_ref_${I} unverifiable "stind.ref" "object\&" "${TYPE1}"
	I=`expr $I + 1`
done


#underflow
./make_newobj_test.sh newobj_underflow invalid "newobj instance void class ClassA::.ctor(int32,int32)" "int32" "int32"

#good simple cases
./make_newobj_test.sh newobj_good_instantiation_1 valid "newobj instance void class ClassA::.ctor(int32)" "int32" "int32"
./make_newobj_test.sh newobj_good_instantiation_2 valid "newobj instance void class ClassA::.ctor(int32)" "native int" "int32"
./make_newobj_test.sh newobj_good_instantiation_3 valid "newobj instance void class ClassA::.ctor(native int)" "int32" "native int"
./make_newobj_test.sh newobj_good_instantiation_4 valid "newobj instance void class ClassA::.ctor(native int)" "int32" "native int"
./make_newobj_test.sh newobj_good_instantiation_5 valid "ldloc.0\n\tnewobj instance void int32[,]::.ctor(int32, int32)" "int32" "native int"
./make_newobj_test.sh newobj_good_instantiation_6 valid "newobj instance void class ClassA::.ctor(typedref)" "typedref" "typedref"

#unverifable types
./make_newobj_test.sh newobj_unverifiable_types_1 unverifiable "newobj instance void class ClassA::.ctor(int32*)" "int32*" "int32*"
./make_newobj_test.sh newobj_unverifiable_types_2 unverifiable "newobj instance void class ClassA::.ctor(method int32 *(int32))" "method int32 *(int32)" "method int32 *(int32)"



#abstract type
./make_newobj_test.sh newobj_bad_inst_1 unverifiable "newobj instance void class AbsClass::.ctor()" "int32" "int32"

#bad types
./make_newobj_test.sh newobj_bad_args_1 unverifiable "newobj instance void class ClassA::.ctor(int32)" "int64" "int32"
./make_newobj_test.sh newobj_bad_args_2 unverifiable "newobj instance void class ClassA::.ctor(int32)" "object" "int32"
./make_newobj_test.sh newobj_bad_args_3 unverifiable "newobj instance void class ClassA::.ctor(int32)" "int32\&" "int32"
./make_newobj_test.sh newobj_bad_args_4 unverifiable "newobj instance void class ClassA::.ctor(int32)" "float32" "int32"

./make_newobj_test.sh newobj_bad_args_5 unverifiable "newobj instance void class ClassA::.ctor(int64)" "int32" "int64"
./make_newobj_test.sh newobj_bad_args_6 unverifiable "newobj instance void class ClassA::.ctor(int64)" "object" "int64"
./make_newobj_test.sh newobj_bad_args_7 unverifiable "newobj instance void class ClassA::.ctor(int64)" "int32\&" "int64"
./make_newobj_test.sh newobj_bad_args_8 unverifiable "newobj instance void class ClassA::.ctor(int64)" "float32" "int64"

./make_newobj_test.sh newobj_bad_args_9 unverifiable "newobj instance void class ClassA::.ctor(object)" "int64" "object"
./make_newobj_test.sh newobj_bad_args_10 unverifiable "newobj instance void class ClassA::.ctor(object)" "int32" "object"
./make_newobj_test.sh newobj_bad_args_11 unverifiable "newobj instance void class ClassA::.ctor(object)" "int32\&" "object"
./make_newobj_test.sh newobj_bad_args_12 unverifiable "newobj instance void class ClassA::.ctor(object)" "float32" "object"

./make_newobj_test.sh newobj_bad_args_13 unverifiable "newobj instance void class ClassA::.ctor(float32)" "int64" "float32"
./make_newobj_test.sh newobj_bad_args_14 unverifiable "newobj instance void class ClassA::.ctor(float32)" "object" "float32"
./make_newobj_test.sh newobj_bad_args_15 unverifiable "newobj instance void class ClassA::.ctor(float32)" "int32\&" "float32"
./make_newobj_test.sh newobj_bad_args_16 unverifiable "newobj instance void class ClassA::.ctor(float32)" "int32" "float32"

./make_newobj_test.sh newobj_bad_args_17 unverifiable "newobj instance void class ClassA::.ctor(typedref)" "int32" "typedref"
./make_newobj_test.sh newobj_bad_args_18 unverifiable "newobj instance void class ClassA::.ctor(typedref)" "object" "typedref"
./make_newobj_test.sh newobj_bad_args_19 unverifiable "newobj instance void class ClassA::.ctor(typedref)" "int32\&" "typedref"
./make_newobj_test.sh newobj_bad_args_20 unverifiable "newobj instance void class ClassA::.ctor(typedref)" "float32" "typedref"


#calling something that it's not an instance constructor

./make_newobj_test.sh newobj_method_not_ctor_1 invalid "newobj instance void class ClassA::ctor(int32)" "int32" "int32"
./make_newobj_test.sh newobj_method_not_ctor_2 invalid "newobj instance void class ClassA::sctor(int32)" "int32" "int32"
./make_newobj_test.sh newobj_method_not_ctor_1 invalid "pop\n\tnewobj instance void class ClassA::.cctor()" "int32" "int32"


#ldlen tests
./make_ldlen_test.sh ldlen_int_array valid "ldc.i4.0\n\tnewarr int32"
./make_ldlen_test.sh ldlen_array_array valid "ldc.i4.0\n\tnewarr string[]"

./make_ldlen_test.sh ldlen_multi_dyn_array unverifiable "ldc.i4.0\n\tldc.i4.0\n\tnewobj instance void string[,]::.ctor(int32, int32)"

#TODO add tests for arrays that are not zero-based
#./make_ldlen_test.sh ldlen_size_bounded_array unverifiable "call int32[1...5] mkarr()"

./make_ldlen_test.sh ldlen_empty_stack invalid "nop"

I=1
for OP in "ldc.i4.0" "ldc.r4 0" " newobj instance void object::.ctor()"
do
  ./make_ldlen_test.sh ldlen_bad_stuff_on_stack_${I} unverifiable "$OP"
  I=`expr $I + 1`
done


#ldelema

#TODO add tests for CMMP (read only prefix)
#TODO add tests for arrays that are not zero-based

./make_ldelema_test.sh ldelema_int_array valid "newarr int32" "ldc.i4.0" "int32"
./make_ldelema_test.sh ldelema_null_array valid "pop\n\tldnull" "ldc.i4.0" "int32"

./make_ldelema_test.sh ldelema_int_array_native_int valid "newarr int32" "ldc.i4.0\n\tconv.i" "int32"
./make_ldelema_test.sh ldelema_null_array_native_int valid "pop\n\tldnull" "ldc.i4.0\n\tconv.i" "int32"


./make_ldelema_test.sh ldelema_empty_stack_1 invalid "pop" "nop" "int32"
./make_ldelema_test.sh ldelema_empty_stack_2 invalid "newarr int32" "nop" "int32"
./make_ldelema_test.sh ldelema_empty_stack_3 invalid "pop" "ldc.i4.0" "int32"

I=1
for ARR in "int8" "int16" "int32"
do
 ./make_ldelema_test.sh ldelema_size_compat_${I} valid "newarr ${ARR}" "ldc.i4.0" "unsigned ${ARR}"
  I=`expr $I + 1`
done

for ARR in "int8" "int16" "int32"
do
 ./make_ldelema_test.sh ldelema_size_compat_${I} valid "newarr unsigned ${ARR}" "ldc.i4.0" "${ARR}"
  I=`expr $I + 1`
done

./make_ldelema_test.sh ldelema_size_compat_nat_1 valid "newarr native int" "ldc.i4.0" "native unsigned int"
./make_ldelema_test.sh ldelema_size_compat_nat_2 valid "newarr native unsigned int" "ldc.i4.0" "native int"


./make_ldelema_test.sh ldelema_misc_size_compat_1 valid "newarr bool" "ldc.i4.0" "int8"
./make_ldelema_test.sh ldelema_misc_size_compat_2 valid "newarr char" "ldc.i4.0" "int16"
./make_ldelema_test.sh ldelema_misc_size_compat_3 valid "newarr native int" "ldc.i4.0" "int32"
./make_ldelema_test.sh ldelema_misc_size_compat_4 valid "newarr native unsigned int" "ldc.i4.0" "int32"

./make_ldelema_test.sh ldelema_misc_size_compat_5 valid "newarr int8" "ldc.i4.0" "bool"
./make_ldelema_test.sh ldelema_misc_size_compat_6 valid "newarr int16" "ldc.i4.0" "char"
./make_ldelema_test.sh ldelema_misc_size_compat_7 valid "newarr int32" "ldc.i4.0" "native int"
./make_ldelema_test.sh ldelema_misc_size_compat_8 valid "newarr int32" "ldc.i4.0" "native unsigned int"

./make_ldelema_test.sh ldelema_misc_size_compat_9 valid "newarr unsigned int8" "ldc.i4.0" "bool"
./make_ldelema_test.sh ldelema_misc_size_compat_10 valid "newarr unsigned int16" "ldc.i4.0" "char"
./make_ldelema_test.sh ldelema_misc_size_compat_11 valid "newarr unsigned int32" "ldc.i4.0" "native int"
./make_ldelema_test.sh ldelema_misc_size_compat_12 valid "newarr unsigned int32" "ldc.i4.0" "native unsigned int"


I=1
for ARR in "newobj instance void object::.ctor()" "ldc.i4.0\n\tldc.i4.0\n\tnewobj instance void string[,]::.ctor(int32, int32)" "ldc.r4 0" "ldc.r8 0" "ldc.i8 0" "ldc.i4.0" "ldc.i4.0\n\tconv.i"
do
 ./make_ldelema_test.sh ldelema_bad_array_${I} unverifiable "pop\n\t${ARR}" "ldc.i4.0" "int32"
  I=`expr $I + 1`
done


I=1
for IDX in "newobj instance void object::.ctor()" "ldc.i8 0" "ldc.r4 0"
do
 ./make_ldelema_test.sh ldelema_bad_index_${I} unverifiable "newarr int32" "${IDX}" "int32"
  I=`expr $I + 1`
done

I=1
for TOKEN in "object" "int64" "int32[]"
do
./make_ldelema_test.sh ldelema_type_mismatch_${I} unverifiable "newarr int32" "ldc.i4.0" "${TOKEN}"
  I=`expr $I + 1`
done

for TOKEN in "object" "int32"
do
./make_ldelema_test.sh ldelema_type_mismatch_${I} unverifiable "newarr string" "ldc.i4.0" "${TOKEN}"
  I=`expr $I + 1`
done

for TOKEN in "object" "int32" "ClassSubA"
do
./make_ldelema_test.sh ldelema_type_mismatch_${I} unverifiable "newarr ClassA" "ldc.i4.0" "${TOKEN}"
  I=`expr $I + 1`
done


#ldelem.X

#TODO add tests for CMMP (read only prefix)
#TODO add tests for arrays that are not zero-based


I=1
for ARR in "int8" "bool" "unsigned int8" "ByteEnum"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.i1"
	./make_ldelem_test.sh ldelem_base_types_u_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.u1"
	I=`expr $I + 1`
done

for ARR in "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.i1"
	./make_ldelem_test.sh ldelem_base_types_u_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.u1"
	I=`expr $I + 1`
done


for ARR in "int16" "char" "unsigned int16" "ShortEnum"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.i2"
	./make_ldelem_test.sh ldelem_base_types_u_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.u2"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.i2"
	./make_ldelem_test.sh ldelem_base_types_u_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.u2"
	I=`expr $I + 1`
done

for ARR in "int32" "unsigned int32" "IntEnum"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.i4"
	./make_ldelem_test.sh ldelem_base_types_u_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.u4"
	./make_ldelem_test.sh ldelem_base_types_n_${I} strict "newarr ${ARR}" "ldc.i4.0" "ldelem.i"
	I=`expr $I + 1`
done

for ARR in "native int" "native unsigned int" "NativeIntEnum"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} strict "newarr ${ARR}" "ldc.i4.0" "ldelem.i4"
	./make_ldelem_test.sh ldelem_base_types_u_${I} strict "newarr ${ARR}" "ldc.i4.0" "ldelem.u4"
	./make_ldelem_test.sh ldelem_base_types_n_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.i"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.i4"
	./make_ldelem_test.sh ldelem_base_types_u_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.u4"
	./make_ldelem_test.sh ldelem_base_types_n_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.i"
	I=`expr $I + 1`
done


for ARR in "int64" "unsigned int64" "LongEnum"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.i8"
	./make_ldelem_test.sh ldelem_base_types_u_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.u8"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "float32" "float64" "object"
do
	./make_ldelem_test.sh ldelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.i8"
	./make_ldelem_test.sh ldelem_base_types_u_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.u8"
	I=`expr $I + 1`
done


for ARR in "float32"
do
	./make_ldelem_test.sh ldelem_base_types_f_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.r4"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float64" "object"
do
	./make_ldelem_test.sh ldelem_base_types_f_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.r4"
	I=`expr $I + 1`
done


for ARR in "float64"
do
	./make_ldelem_test.sh ldelem_base_types_f_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.r8"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "object"
do
	./make_ldelem_test.sh ldelem_base_types_f_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.r8"
	I=`expr $I + 1`
done


for ARR in "object" "string" "ClassA"
do
	./make_ldelem_test.sh ldelem_base_types_o_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem.ref"
	I=`expr $I + 1`
done

for ARR in "valuetype MyStruct" "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64"
do
	./make_ldelem_test.sh ldelem_base_types_o_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldelem.ref"
	I=`expr $I + 1`
done

I=1
for OP in i1 u1 i2 u2 i4 u4 i8 u8 r4 r8 i ref
do
	./make_ldelem_test.sh ldelem_null_array_${I} valid "pop\n\tldnull" "ldc.i4.0" "ldelem.${OP}"
	I=`expr $I + 1`
done


I=1
for OP in i1 u1 i2 u2 i4 u4 i8 u8 r4 r8 i ref
do
	./make_ldelem_test.sh ldelem_empty_stack_1_${I} invalid "pop" "nop" "ldelem.${OP}"
	./make_ldelem_test.sh ldelem_empty_stack_2_${I} invalid "newarr int32" "nop" "ldelem.${OP}"
	./make_ldelem_test.sh ldelem_empty_stack_3_${I} invalid "pop" "ldc.i4.0" "ldelem.${OP}"
	I=`expr $I + 1`
done

I=1
for OP in i1 u1 i2 u2 i4 u4 i8 u8 r4 r8 i ref
do
	for ARR in "newobj instance void object::.ctor()" "ldc.i4.0\n\tldc.i4.0\n\tnewobj instance void string[,]::.ctor(int32, int32)" "ldc.r4 0" "ldc.r8 0" "ldc.i8 0" "ldc.i4.0" "ldc.i4.0\n\tconv.i"
	do
	 ./make_ldelem_test.sh ldelema_bad_array_${I} unverifiable "pop\n\t${ARR}" "ldc.i4.0" "ldelem.${OP}"
	  I=`expr $I + 1`
	done
done


I=1
for OP in i1 u1 i2 u2 i4 u4 i8 u8 r4 r8 i ref
do
	for IDX in "newobj instance void object::.ctor()" "ldc.i8 0" "ldc.r4 0"
	do
	 ./make_ldelem_test.sh ldelema_bad_index_${I} unverifiable "pop\n\tldnull" "${IDX}" "ldelem.${OP}"
	  I=`expr $I + 1`
	done
done


#adicional tests for ldelem
I=1
for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_ldelem_test.sh ldelem_token_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldelem ${ARR}"
	I=`expr $I + 1`
done


#stdelem.X
#TODO add tests for arrays that are not zero-based

I=1
for ARR in "int8" "bool" "unsigned int8"
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i1"
	I=`expr $I + 1`
done

for ARR in "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i1"
	I=`expr $I + 1`
done


for ARR in "int16" "char" "unsigned int16"
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i2"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i2"
	I=`expr $I + 1`
done


for ARR in "int32" "unsigned int32" "native int" "native unsigned int"
do
	./make_stelem_test.sh stelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0\n\tconv.i" "stelem.i4"
	./make_stelem_test.sh stelem_base_types_i4_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i4"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0\n\tconv.i" "stelem.i4"
	./make_stelem_test.sh stelem_base_types_i4_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i4"
	I=`expr $I + 1`
done


for ARR in "int64" "unsigned int64"
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i8 0" "stelem.i8"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "float32" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i8 0" "stelem.i8"
	I=`expr $I + 1`
done


for ARR in "float32"
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.r4 0" "stelem.r4"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.r4 0" "stelem.r4"
	I=`expr $I + 1`
done


for ARR in "float64"
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.r8 0" "stelem.r8"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "object"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.r8 0" "stelem.r8"
	I=`expr $I + 1`
done


for ARR in "int32" "unsigned int32" "native int" "native unsigned int"
do
	./make_stelem_test.sh stelem_base_types_i_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0\n\tconv.i" "stelem.i"
	./make_stelem_test.sh stelem_base_types_i4_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16"  "int64" "unsigned int64" "float32" "float64" "object"
do
	./make_stelem_test.sh stelem_base_types_i_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0\n\tconv.i" "stelem.i"
	./make_stelem_test.sh stelem_base_types_i4_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i"
	I=`expr $I + 1`
done


for ARR in object ClassA ClassSubA
do
	./make_stelem_test.sh stelem_base_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "newobj instance void ${ARR}::.ctor()" "stelem.ref"
	I=`expr $I + 1`
done

for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int" "int64" "unsigned int64" "float32" "float64"
do
	./make_stelem_test.sh stelem_base_types_${I} unverifiable "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.ref"
	I=`expr $I + 1`
done



./make_stelem_test.sh stelem_native_int_index valid "newarr int32" "ldc.i4.0\n\tconv.i" "ldc.i4.0" "stelem.i4"


#tests with null arrays and values (for ref types)

I=1
for OP in i1 i2 i4 i
do
	./make_stelem_test.sh stelem_null_array_index_i_${I} valid "pop\n\tldnull" "ldc.i4.0" "ldc.i4.0" "stelem.${OP}"
	I=`expr $I + 1`
done

./make_stelem_test.sh stelem_null_array_index_i8 valid "pop\n\tldnull" "ldc.i4.0" "ldc.i8 0" "stelem.i8"
./make_stelem_test.sh stelem_null_array_index_r4 valid "pop\n\tldnull" "ldc.i4.0" "ldc.r4 0" "stelem.r4"
./make_stelem_test.sh stelem_null_array_index_r8 valid "pop\n\tldnull" "ldc.i4.0" "ldc.r8 0" "stelem.r4"
./make_stelem_test.sh stelem_null_array_index_ref valid "pop\n\tldnull" "ldc.i4.0" "newobj instance void object::.ctor()" "stelem.ref"

./make_stelem_test.sh stelem_null_value_1 valid "newarr object" "ldc.i4.0" "ldnull" "stelem.ref"
./make_stelem_test.sh stelem_null_value_2 valid "newarr string" "ldc.i4.0" "ldnull" "stelem.ref"

#both need to be reference types
./make_stelem_test.sh stelem_variance_1 valid "newarr object" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_variance_2 valid "newarr object" "ldc.i4.0" "newobj instance void ClassSubA::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_variance_3 valid "newarr ClassA" "ldc.i4.0" "newobj instance void ClassSubA::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_variance_4 valid "newarr ClassSubA" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_variance_5 valid "newarr ClassSubA" "ldc.i4.0" "newobj instance void object::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_variance_6 valid "newarr string" "ldc.i4.0" "newobj instance void object::.ctor()" "stelem.ref"

./make_stelem_test.sh stelem_value_type_1 unverifiable "newarr object" "ldc.i4.0" "ldloc.0" "stelem.ref"
./make_stelem_test.sh stelem_value_type_2 unverifiable "newarr object" "ldc.i4.0" "ldloca.s 0" "stelem.ref"
./make_stelem_test.sh stelem_value_type_3 unverifiable "newarr MyStruct" "ldc.i4.0" "newobj instance void object::.ctor()" "stelem.ref"
./make_stelem_test.sh stelem_value_type_4 unverifiable "newarr MyStruct" "ldc.i4.0" "ldloc.0" "stelem.ref"
./make_stelem_test.sh stelem_value_type_5 unverifiable "newarr MyStruct" "ldc.i4.0" "ldloca.s 0" "stelem.ref"


#bad index values
I=1
for IDX in "ldc.i8 0" "ldc.r4 0" "ldc.r8 0" "newobj instance void ClassA::.ctor()"
do
	./make_stelem_test.sh stelem_bad_index_${I} unverifiable "newarr int32" "${IDX}" "ldc.i4.0" "stelem.i4"
	I=`expr $I + 1`
done

#bad array values
I=1
for ARR in "ldc.i4.0" "ldc.i8 0" "ldc.r4 0" "ldc.r8 0" "newobj instance void ClassA::.ctor()"
do
	./make_stelem_test.sh stelem_bad_index_${I} unverifiable "pop\n\t${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem.i4"
	I=`expr $I + 1`
done


#empty stack
./make_stelem_test.sh stelem_empty_stack_1 invalid "newarr object" "ldc.i4.0" "pop" "stelem.ref"
./make_stelem_test.sh stelem_empty_stack_2 invalid "newarr object" "nop" "ldnull" "stelem.ref"
./make_stelem_test.sh stelem_empty_stack_3 invalid "newarr object" "nop" "nop" "stelem.ref"
./make_stelem_test.sh stelem_empty_stack_4 invalid "pop" "nop" "nop" "stelem.ref"


#test with multi-dim array
./make_stelem_test.sh stelem_multi_dim_array unverifiable "ldc.i4.0\n\tnewobj instance void string[,]::.ctor(int32, int32)" "ldc.i4.0" "ldc.i4.0" "stelem.i4"




#adicional tests for stelem
I=1
for ARR in "int8" "bool" "unsigned int8" "int16" "char" "unsigned int16" "int32" "unsigned int32" "native int" "native unsigned int"
do
	./make_stelem_test.sh stelem_token_basic_types_${I} valid "newarr ${ARR}" "ldc.i4.0" "ldc.i4.0" "stelem ${ARR}"
	I=`expr $I + 1`
done

./make_stelem_test.sh stelem_token_basic_types_i8 valid "newarr int64" "ldc.i4.0" "ldc.i8 0" "stelem int64"
./make_stelem_test.sh stelem_token_basic_types_r4 valid "newarr float32" "ldc.i4.0" "ldc.r4 0" "stelem float32"
./make_stelem_test.sh stelem_token_basic_types_r8 valid "newarr float64" "ldc.i4.0" "ldc.r8 0" "stelem float64"

I=1
for TYPE in "object" "ClassA" "ClassSubA"
do
	./make_stelem_test.sh stelem_token_simple_ref_${I} valid "newarr object" "ldc.i4.0" "newobj instance void ${TYPE}::.ctor()" "stelem object"
	I=`expr $I + 1`
done

#the array elem type must be a super type of the stelem token
./make_stelem_test.sh stelem_token_ref_1 valid "newarr object" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem ClassA"
./make_stelem_test.sh stelem_token_ref_2 valid "newarr ClassA" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem ClassA"
./make_stelem_test.sh stelem_token_ref_3 unverifiable "newarr ClassSubA" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem ClassA"

#the value type must be assignment compatible with token
./make_stelem_test.sh stelem_token_ref_4 valid "newarr object" "ldc.i4.0" "newobj instance void ClassA::.ctor()" "stelem ClassA"
./make_stelem_test.sh stelem_token_ref_5 valid "newarr object" "ldc.i4.0" "newobj instance void ClassSubA::.ctor()" "stelem ClassA"
./make_stelem_test.sh stelem_token_ref_6 unverifiable "newarr object" "ldc.i4.0" "newobj instance void object::.ctor()" "stelem ClassA"



#cast class and isins tests

#empty stack
./make_cast_test.sh cast_empty_stack invalid "int32" "nop" "castclass object"
./make_cast_test.sh isinst_empty_stack invalid "int32"  "nop" "isinst object"

#type
I=1
for OBJ in int32 int64 float32 float64 "int32\&" "valuetype MyStruct" "int32*" "typedref" "method int32 *(int32)"
do
	./make_cast_test.sh cast_object_${I} unverifiable "$OBJ" "ldloc.0" "castclass object"
	./make_cast_test.sh isinst_object_${I} unverifiable "$OBJ" "ldloc.0" "isinst object"
	I=`expr $I + 1`
done

for OBJ in "int32[]" "string"
do
	./make_cast_test.sh cast_object_${I} valid "$OBJ" "ldloc.0" "castclass object"
	./make_cast_test.sh isinst_object_${I} valid "$OBJ" "ldloc.0" "isinst object"
	I=`expr $I + 1`
done
#token

I=1
for TOKEN in int32 int64 float32 float64 "valuetype MyStruct" "int32[]" "string"
do
	./make_cast_test.sh cast_token_${I} valid "object" "ldloc.0" "castclass $TOKEN"
	./make_cast_test.sh isinst_token_${I} valid "object" "ldloc.0" "isinst $TOKEN"
	I=`expr $I + 1`
done

for TOKEN in "int32*" "method int32 *(int32)" "typedref"
do
	./make_cast_test.sh cast_token_${I} unverifiable "object" "ldloc.0" "castclass $TOKEN"
	./make_cast_test.sh isinst_token_${I} unverifiable "object" "ldloc.0" "isinst $TOKEN"
	I=`expr $I + 1`
done

for TOKEN in "int32\&"
do
	./make_cast_test.sh cast_token_${I} invalid "object" "ldloc.0" "castclass $TOKEN"
	./make_cast_test.sh isinst_token_${I} invalid "object" "ldloc.0" "isinst $TOKEN"
	I=`expr $I + 1`
done

#object

I=1
for LOAD in "ldloc.0" "ldnull"
do
	./make_cast_test.sh cast_good_obj_${I} valid "object" "$LOAD" "castclass object"
	./make_cast_test.sh isinst_good_obj_${I} valid "object" "$LOAD" "isinst object"
	I=`expr $I + 1`
done



#throw tests

#empty stack
./make_throw_test.sh throw_empty_stack invalid int32 pop

#null literal
./make_throw_test.sh throw_null_literal valid int32 "pop\n\tldnull"

#valid types
I=1
for TYPE in object string "[mscorlib]System.Exception" "int32[]" "ClassA" "class [mscorlib]System.IComparable\`1<int32>" "int32[,]"
do
	./make_throw_test.sh throw_ref_type_${I} valid "${TYPE}"
	I=`expr $I + 1`
done

#invalid types
I=1
for TYPE in "valuetype MyStruct" int32 int64 float32 float64 "native int" "int32*" "typedref" "object\&"
do
	./make_throw_test.sh throw_value_type_${I} unverifiable "${TYPE}"
	I=`expr $I + 1`
done


# Exception block branch tests (see 2.19)

I=1; while [ $I -le 2 ]
do
	./make_rethrow_test.sh rethrow_from_catch_${I} invalid ${I}
	I=$((I + 1))
done

I=3; while [ $I -le 10 ]
do
	./make_rethrow_test.sh rethrow_from_catch_${I} valid ${I}
	I=$((I + 1))
done



# endfinally / endfault

I=1; while [ $I -le 7 ]
do
	./make_endfinally_test.sh endfinally_block_${I} invalid finally ${I}
	./make_endfinally_test.sh endfault_block_${I} invalid fault ${I}
	I=$((I + 1))
done

I=8; while [ $I -le 9 ]
do
	./make_endfinally_test.sh endfinally_block_${I} valid finally ${I}
	./make_endfinally_test.sh endfault_block_${I} valid fault ${I}
	I=$((I + 1))
done

#stack can have stuff and endfinally or endfault will just empty it

./make_endfinally_test.sh endfinally_clean_stack valid finally 8 "ldc.i4.0"
./make_endfinally_test.sh endfault_clean_stack valid fault 8 "ldc.i4.0"



# endfilter

#valid endfilter
./make_endfilter_test.sh endfilter_at_end_of_filter_block valid 9

#endfilter outside protected block
./make_endfilter_test.sh endfilter_outside_protected_block invalid 1 "ldc.i4.1\n\t\tendfilter"

#endfilter inside bad protected block
./make_endfilter_test.sh endfilter_inside_protected_block_3 invalid 3 "ldc.i4.1\n\t\tendfilter"
./make_endfilter_test.sh endfilter_inside_protected_block_5 strict 5 "ldc.i4.1\n\t\tendfilter"

for I in 2 4 6;
do
	./make_endfilter_test.sh endfilter_inside_protected_block_${I} unverifiable ${I} "ldc.i4.1\n\t\tendfilter"
done


#endfilter is the first instruction
./make_endfilter_test.sh endfilter_first_instruction_of_filter_block invalid 7 "ldc.i4.1\n\tendfilter"
./make_endfilter_test.sh endfilter_in_the_midle_instruction_of_filter_block invalid 8 "ldc.i4.1\n\t\tendfilter"

#stack sizes

./make_endfilter_test.sh endfilter_empty_stack strict 9 "pop"
./make_endfilter_test.sh endfilter_too_big strict 9 "ldc.i4.0"


I=1
for OP in "ldc.i8 0" "ldnull" "ldc.r4 0" "ldc.r8 1" "ldc.i4.0\n\t\tconv.i" "ldc.r8 99999"
do
	./make_endfilter_test.sh endfilter_bad_arg_${I} strict 9 "pop\t\n\n${OP}"
	I=`expr $I + 1`
done


# leave

#leave in all positions

EXTRA="ldloc.0\n\tbrfalse END"

#it's "OK" to use leave as a br
./make_leave_test.sh "filter_block_test_1" valid "1" "leave END" "$EXTRA"

#but not ok to leave finally or filter
I=2; while [ $I -le 3 ]; do
	./make_leave_test.sh "filter_block_test_${I}" unverifiable "${I}" "leave END" "${EXTRA}_${I}"
	I=$((I + 1))
done

#neither is to branch to invalid regions of code
./make_leave_test.sh "filter_branch_before_start" invalid "1" "leave -400" "$EXTRA"
./make_leave_test.sh "filter_branch_after_end" invalid "1" "leave 400" "$EXTRA"


# br.X
#valid tests
I=1; while [ $I -le 6 ]; do
	./make_branch_test.sh branch_inside_same_block_${I} valid ${I} "br BLOCK_${I}";
	./make_branch_test.sh branch_inside_same_block_${I}_s valid ${I} "br.s BLOCK_${I}";
	I=$((I + 1))
done

#branching outside of the protected block
I=2; while [ $I -le 6 ]; do
	./make_branch_test.sh branch_outside_protected_block_${I} unverifiable ${I} "br END";
	I=$((I + 1))
done

#branching to a protected block from the outside
I=2; while [ $I -le 6 ]; do
	if [ $I -eq 4 ]; then
		./make_branch_test.sh branch_inside_protected_block_from_outside_${I}_finally invalid 1 "br BLOCK_${I}" "finally";
		./make_branch_test.sh branch_inside_protected_block_from_outside_${I}_fault invalid 1 "br BLOCK_${I}" "fault";
	else
		./make_branch_test.sh branch_inside_protected_block_from_outside_${I} unverifiable 1 "br BLOCK_${I}";
	fi
	I=$((I + 1))
done


#branching out of range
./make_branch_test.sh branch_out_of_bounds_before_start invalid 1 "br -1000";
./make_branch_test.sh branch_out_of_bounds_after_end invalid 1 "br 1000";

#branching in the middle of an instruction
./make_branch_test.sh branch_middle_of_instruction invalid 1 "br 2";

#branching in between prefix and instruction
./make_branch_test.sh branch_middle_of_instruction_prefix_1 invalid 1 "br AFTER_FIRST_PREFIX";
./make_branch_test.sh branch_middle_of_instruction_prefix_2 invalid 1 "br AFTER_SECOND_PREFIX";

#TODO test the encoding of the switch table
# switch
#valid tests
I=1; while [ $I -le 6 ]; do
	./make_switch_test.sh switch_inside_same_block_${I} valid ${I} "ldloc.0" "switch (BLOCK_${I}, BLOCK_${I}_B)";
	I=$((I + 1))
done

./make_switch_test.sh switch_with_native_int_on_stack valid 1 "ldloc.1" "switch (BLOCK_1, BLOCK_1_B)";

#branching outside of the protected block
I=2; while [ $I -le 6 ]; do
	./make_switch_test.sh switch_outside_protected_block_${I} unverifiable ${I} "ldloc.0" "switch (END, BLOCK_1, BLOCK_1_B)";
	I=$((I + 1))
done

#branching to a protected block from the outside
I=2; while [ $I -le 6 ]; do
	if [ $I -eq 4 ]; then
		./make_switch_test.sh switch_inside_protected_block_from_outside_${I}_finally invalid 1 "ldloc.0" "switch (BLOCK_${I}, BLOCK_${I}_B)" "finally";
		./make_switch_test.sh switch_inside_protected_block_from_outside_${I}_fault invalid 1 "ldloc.0" "switch (BLOCK_${I}, BLOCK_${I}_B)" "fault";
	else
		./make_switch_test.sh switch_inside_protected_block_from_outside_${I} unverifiable 1 "ldloc.0" "switch (BLOCK_${I}, BLOCK_${I}_B)";
	fi
	I=$((I + 1))
done

#TODO branching out of range (FIX ilasm first)
#./make_switch_test.sh switch_out_of_bounds_before_start invalid 1 "ldloc.0" "switch (-1000, -2000)"
#./make_switch_test.sh switch_out_of_bounds_after_end invalid 1 "ldloc.0" "switch (BLOCK_1, 1000, 1500)"

#empty stack
./make_switch_test.sh switch_empty_stack invalid 1 "nop" "switch (BLOCK_1, BLOCK_1_B)"

#wrong type on stack
I=1
for TYPE in "ldnull" "ldc.i8 0" "ldc.r4 0" "ldc.r8 0"
do
	./make_switch_test.sh switch_bad_type_on_stack_${I} unverifiable 1 "$TYPE" "switch (BLOCK_1, BLOCK_1_B)"
	I=`expr $I + 1`
done

#switch landing in the middle of instructions
#FIXME (ilasm don't work with offsets on switch statements)
#./make_switch_test.sh switch_target_middle_of_instruction invalid 1 "ldloc.1" "switch (BLOCK_1, BLOCK_1)";

./make_switch_test.sh switch_target_between_prefix_1 invalid 1 "ldloc.1" "switch (AFTER_FIRST_PREFIX, BLOCK_1)";
./make_switch_test.sh switch_target_between_prefix_2 invalid 1 "ldloc.1" "switch (AFTER_SECOND_PREFIX, BLOCK_1)";
./make_switch_test.sh switch_target_bad_merge_point invalid 1 "ldloc.1" "switch (INVALID_MERGE_POINT, BLOCK_1)";


#TESTS for exception clauses. As described in P1 12.4.2.7.
#regions must not overlap with each other
./make_exception_overlap_test.sh exception_entry_overlap_separate_1 valid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh exception_entry_overlap_separate_2 valid ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END"

./make_exception_overlap_test.sh exception_entry_overlap_try_over_catch invalid ".try TRY_BLOCK_1 to CATCH_BLOCK_1_A catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh exception_entry_overlap_try_over_filter invalid ".try TRY_BLOCK_1 to FILTER_BLOCK_3_A filter FILTER_BLOCK_3 handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" "yes"

#blocks start in the middle of an instruction
./make_exception_overlap_test.sh try_block_start_in_the_middle_of_a_instruction invalid ".try AFTER_PREFIX_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh catch_block_start_in_the_middle_of_a_instruction invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler AFTER_PREFIX_2 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh filter_block_start_in_the_middle_of_a_instructior invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END filter AFTER_PREFIX_4 handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" "yes"


#block end in the middle of an instruction
./make_exception_overlap_test.sh try_block_end_in_the_middle_of_a_instruction invalid ".try TRY_BLOCK_1 to AFTER_PREFIX_4 catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh catch_block_end_in_the_middle_of_a_instruction invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to AFTER_PREFIX_5" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"


#regions are disjoint
./make_exception_overlap_test.sh exception_entry_overlap_disjoint_1 valid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END"

./make_exception_overlap_test.sh exception_entry_overlap_disjoint_2 valid ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

#nesting
./make_exception_overlap_test.sh nested_exception_entry_comes_first valid ".try TRY_BLOCK_1_A to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_1 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh nested_exception_entry_comes_after invalid ".try TRY_BLOCK_1 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" ".try TRY_BLOCK_1_A to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END"


#mutual protectiong
./make_exception_overlap_test.sh exception_same_try valid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END"

./make_exception_overlap_test.sh exception_same_catch invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" ".try TRY_BLOCK_2 to TRY_BLOCK_2_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END"

./make_exception_overlap_test.sh exception_same_try_with_catch_and_filter valid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_2 to CATCH_BLOCK_2_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END filter FILTER_BLOCK_3 handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" "yes"

./make_exception_overlap_test.sh exception_same_try_with_catch_and_finally invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END finally handler FINALLY_BLOCK_1 to FINALLY_BLOCK_1_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" "no" "yes"

./make_exception_overlap_test.sh exception_same_try_with_catch_and_fault invalid ".try TRY_BLOCK_1 to TRY_BLOCK_1_END fault handler FINALLY_BLOCK_1 to FINALLY_BLOCK_1_END" ".try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler CATCH_BLOCK_1 to CATCH_BLOCK_1_END" "no" "yes"


#ldftn
./make_ldftn_test.sh ldftn_static_method valid "ldftn void class Driver::Method()"
./make_ldftn_test.sh ldftn_virtual_method valid "ldftn instance void class Driver::VirtMethod()"
./make_ldftn_test.sh ldftn_corlib_method valid "ldftn instance string string::ToUpper()"

#this is encoded as a memberref
./make_ldftn_test.sh ldftn_bad_function invalid "ldftn void class Test::NonPresent()"

./make_ldftn_test.sh ldftn_overflow invalid "ldftn instance void class Driver::Method()" "ldc.i4.0\n\tldc.i4.0"

./make_ldftn_test.sh ldftn_ctor unverifiable "ldftn void class Test::.ctor()"
./make_ldftn_test.sh ldftn_static_method valid "ldftn void class Test::StaticMethod()"
./make_ldftn_test.sh ldftn_non_virtual_method valid "ldftn instance void class Test::Method()"
./make_ldftn_test.sh ldftn_virtual_method valid "ldftn instance void class Test::VirtMethod()"


#ldvirtftn
#TODO test visibility for ldftn and ldvirtftn

./make_ldvirtftn_test.sh ldvirtftn_virt_method valid "ldvirtftn instance void class Test::VirtMethod()" "newobj void class Test::.ctor()"
./make_ldvirtftn_test.sh ldvirtftn_virt_underflow invalid "ldvirtftn instance void class Test::VirtMethod()" "nop"
./make_ldvirtftn_test.sh ldvirtftn_valid_obj_on_stack valid "ldvirtftn instance string object::ToString()" "newobj void object::.ctor()"

I=1
for TYPE in "ldc.i4.0" "ldc.i8 0" "ldc.r4 2" "ldc.i4.1\n\tconv.i" "ldloca 0" "ldloc.1"
do
	./make_ldvirtftn_test.sh ldvirtftn_invalid_type_on_stack_${I} unverifiable "ldvirtftn instance string object::ToString()" "$TYPE"
	I=`expr $I + 1`
done

./make_ldvirtftn_test.sh ldvirtftn_non_virtual_method valid "ldvirtftn instance void class Test::Method()" "newobj void class Test::.ctor()"

./make_ldvirtftn_test.sh ldvirtftn_ctor unverifiable "ldvirtftn void class Test::.ctor()" "newobj void class Test::.ctor()"
./make_ldvirtftn_test.sh ldvirtftn_static_method unverifiable "ldvirtftn void class Test::StaticMethod()" "newobj void class Test::.ctor()"
./make_ldvirtftn_test.sh ldvirtftn_method_not_present invalid "ldvirtftn void class Test::NonExistent()" "newobj void Test::.ctor()"


./make_ldvirtftn_test.sh ldvirtftn_method_stack_type_obj_compatible_1 valid "ldvirtftn instance string object::ToString()" "newobj void Test::.ctor()"
./make_ldvirtftn_test.sh ldvirtftn_method_stack_type_obj_compatible_2 valid "ldvirtftn void class Test::VirtMethod()" "newobj void Test::.ctor()"
./make_ldvirtftn_test.sh ldvirtftn_method_stack_type_obj_compatible_3 unverifiable "ldvirtftn void class Test::VirtMethod()" "newobj void object::.ctor()"


#Delegates
#ldftn delegates
#pure native int
./make_delegate_test.sh delegate_with_native_int unverifiable "ldarg.1\n\tconv.i" "DelegateNoArg" "ldarg.0"

#random types
I=1;
for TYPE in "ldc.i4.0" "ldc.i8 0" "ldc.r4 0" "ldc.r8 1" "ldarga 1"
do
	./make_delegate_test.sh delegate_with_bad_type_${I} unverifiable "ldftn void Driver::Method()" "DelegateNoArg" "$TYPE"
	I=`expr $I + 1`
done

#ldftn
#static method
./make_delegate_test.sh delegate_ldftn_static_method_1 valid "ldftn void Driver::Method()" "DelegateNoArg" "ldnull"
./make_delegate_test.sh delegate_ldftn_static_method_2 valid "ldftn void Driver::Method2(int32)" "DelegateIntArg" "ldnull"
./make_delegate_test.sh delegate_ldftn_static_method_3 unverifiable "ldftn void Driver::Method2(int32)" "DelegateNoArg" "ldnull"
./make_delegate_test.sh delegate_ldftn_static_method_4 unverifiable "ldftn void Driver::Method()" "DelegateIntArg" "ldnull"

#non-virtual
#null this
./make_delegate_test.sh delegate_ldftn_non_virtual_method_1 valid "ldftn instance void Driver::NonVirtMethod()" "DelegateNoArg" "ldnull"
./make_delegate_test.sh delegate_ldftn_non_virtual_method_2 valid "ldftn instance void Driver::NonVirtMethod2(int32)" "DelegateIntArg" "ldnull"

#method on this
./make_delegate_test.sh delegate_ldftn_non_virtual_method_3 valid "ldftn instance void Driver::NonVirtMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"
./make_delegate_test.sh delegate_ldftn_non_virtual_method_4 valid "ldftn instance void Driver::NonVirtMethod2(int32)" "DelegateIntArg" "newobj instance void class Driver::.ctor()"
#method on parent
./make_delegate_test.sh delegate_ldftn_non_virtual_method_5 valid "ldftn instance void Parent::ParentMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"

#invalid this
./make_delegate_test.sh delegate_ldftn_non_virtual_method_6 unverifiable "ldftn instance void Driver::NonVirtMethod()" "DelegateNoArg" "newobj void object::.ctor()"
./make_delegate_test.sh delegate_ldftn_non_virtual_method_7 unverifiable "ldftn instance void Driver::NonVirtMethod()" "DelegateNoArg" "newobj void Parent::.ctor()"

#virtual methods
./make_delegate_test.sh delegate_ldftn_virtual_method_1 valid "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0"
./make_delegate_test.sh delegate_ldftn_virtual_method_2 valid "ldftn instance void Driver::VirtMethod2(int32)" "DelegateIntArg" "ldarg.0"
./make_delegate_test.sh delegate_ldftn_virtual_method_3 valid "ldftn instance void Driver::ParentVirtMethod()" "DelegateNoArg" "ldarg.0"
./make_delegate_test.sh delegate_ldftn_virtual_method_4 valid "ldftn instance void Parent::ParentVirtMethod()" "DelegateNoArg" "ldarg.0"

#other forms of ldarg
./make_delegate_test.sh delegate_ldftn_virtual_method_5 valid "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.s 0"
./make_delegate_test.sh delegate_ldftn_virtual_method_6 valid "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg 0"

#object is not this
./make_delegate_test.sh delegate_ldftn_virtual_method_7 unverifiable "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"
./make_delegate_test.sh delegate_ldftn_virtual_method_8 unverifiable "ldftn instance void Parent::VirtMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"
./make_delegate_test.sh delegate_ldftn_virtual_method_9 unverifiable "ldftn instance void Driver::ParentVirtMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"
./make_delegate_test.sh delegate_ldftn_virtual_method_10 unverifiable "ldftn instance void Parent::ParentVirtMethod()" "DelegateNoArg" "newobj instance void class Driver::.ctor()"

#static method
./make_delegate_test.sh delegate_ldftn_virtual_method_11 unverifiable "ldftn void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0" "Driver"
./make_delegate_test.sh delegate_ldftn_virtual_method_12 unverifiable "ldftn void Parent::VirtMethod()" "DelegateNoArg" "ldarg.0" "Driver"
./make_delegate_test.sh delegate_ldftn_virtual_method_13 unverifiable "ldftn void Parent::ParentVirtMethod()" "DelegateNoArg" "ldarg.0" "Driver"

#final virtual
./make_delegate_test.sh delegate_ldftn_virtual_method_14 valid "ldftn instance void Driver::SealedVirtMethod()" "DelegateNoArg" "ldarg.0" "Driver"
./make_delegate_test.sh delegate_ldftn_virtual_method_15 unverifiable "ldftn instance void Parent::SealedVirtMethod()" "DelegateNoArg" "ldarg.0" "Driver"
./make_delegate_test.sh delegate_ldftn_virtual_method_16 unverifiable "ldftn instance void Parent::SealedVirtMethod()" "DelegateNoArg" "ldarg.0" "Parent"


#instruction sequence
./make_delegate_test.sh delegate_ldftn_bad_sequence unverifiable "ldftn void Driver::Method()\n\t\tnop" "DelegateNoArg" "ldarg.0"
#this one is terribly hard to read

./make_delegate_test.sh delegate_ldftn_different_basic_block unverifiable "pop\n\t\tpop\n\t\tldarg.0\n\t\tldftn void Driver::Method()" "DelegateNoArg" "ldarg.0\n\t\tldftn void Driver::Method()\n\t\tldarg.1\n\t\tbrfalse DELEGATE_OP"

#it's not necessary to test split due to a protected block since the stack must be empty at the beginning.


#virtual method with starg.0
./make_delegate_test.sh delegate_ldftn_virtual_method_with_starg0_1 unverifiable "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\tstarg.s 0\n\tldarg.0"
./make_delegate_test.sh delegate_ldftn_virtual_method_with_starg0_2 unverifiable "ldftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\tstarg 0\n\tldarg.0"

#value types
./make_delegate_test.sh delegate_ldftn_non_virtual_method_valuetype valid "ldftn instance void MyValueType::NonVirtMethod()" "DelegateNoArg" "ldloc.0\n\tbox MyValueType"
./make_delegate_test.sh delegate_ldftn_virtual_method_valuetype valid "ldftn instance string MyValueType::ToString()" "ToStringDelegate" "ldloc.0\n\tbox MyValueType"

./make_delegate_test.sh delegate_ldftn_virtual_method_valuetype_byref unverifiable "ldftn instance string MyValueType::ToString()" "ToStringDelegate" "ldloca 0"


#ldvirtftn
#ok cases
./make_delegate_test.sh delegate_ldvirtftn_non_virtual_method valid "ldvirtftn instance void Driver::NonVirtMethod()" "DelegateNoArg" "ldarg.0\n\tdup"
./make_delegate_test.sh delegate_ldvirtftn_virtual_method valid "ldvirtftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\tdup"

#wrong instruction sequence
./make_delegate_test.sh delegate_ldvirtftn_bad_sequence unverifiable "ldvirtftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\tldarg.0"

./make_delegate_test.sh delegate_ldvirtftn_different_basic_block unverifiable "pop\n\t\tdup\n\t\tldvirtftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\t\tldarg.0\n\t\tldvirtftn instance void Driver::VirtMethod()\n\t\tldarg.1\n\t\tbrfalse DELEGATE_OP"

./make_delegate_test.sh delegate_ldvirtftn_different_basic_block_dup unverifiable "DUP_OP: ldvirtftn instance void Driver::VirtMethod()" "DelegateNoArg" "ldarg.0\n\t\tdup\n\t\tldarg.1\n\t\tbrfalse DUP_OP\n\t\tpop\n\t\tdup"


#tests for ovf opcodes
I=1
for OP in "add.ovf" "add.ovf.un" "mul.ovf" "mul.ovf.un" "sub.ovf" "sub.ovf.un"
do
	for TYPE in "object" "string" "float32" "float64" "int32*" "typedref" "int32[]" "int32[,]" "method int32 *(int32)"
	do
		./make_bin_test.sh bin_ovf_math_1_${I} unverifiable $OP int32 "${TYPE}"
		./make_bin_test.sh bin_ovf_math_2_${I} unverifiable $OP int64 "${TYPE}"
		./make_bin_test.sh bin_ovf_math_3_${I} unverifiable $OP "native int" "${TYPE}"
		./make_bin_test.sh bin_ovf_math_4_${I} unverifiable $OP "int32&" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int32" "native int"
	do
		./make_bin_test.sh bin_ovf_math_5_${I} valid $OP int32 "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int32" "native int"
	do
		./make_bin_test.sh bin_ovf_math_6_${I} valid $OP "native int" "${TYPE}"
		I=`expr $I + 1`
	done
done

for OP in "add.ovf.un" "sub.ovf.un"
do
	for TYPE in "int32" "native int" "int32&"
	do
		./make_bin_test.sh bin_ovf_math_7_${I} unverifiable $OP "int32&" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int32" "native int" "int32&"
	do
		./make_bin_test.sh bin_ovf_math_8_${I} unverifiable $OP "${TYPE}" "int32&"
		I=`expr $I + 1`
	done
done

#should be invalid
for OP in "add.ovf" "mul.ovf" "mul.ovf.un" "sub.ovf"
do
	for TYPE in "int32" "native int" "int32&"
	do
		./make_bin_test.sh bin_ovf_math_7_${I} unverifiable $OP "int32&" "${TYPE}"
		I=`expr $I + 1`
	done

	for TYPE in "int32" "native int" "int32&"
	do
		./make_bin_test.sh bin_ovf_math_8_${I} unverifiable $OP "${TYPE}" "int32&"
		I=`expr $I + 1`
	done
done

#ovf math doesn't work with floats
I=1
for OP in "add.ovf.un" "add.ovf" "sub.ovf.un" "sub.ovf" "mul.ovf.un" "mul.ovf"
do
	for TYPE in "float32" "float64"
	do
		./make_bin_test.sh bin_ovf_math_f_${I} unverifiable $OP "${TYPE}" "${TYPE}"
		I=`expr $I + 1`
	done
done

#unbox.any

./make_unbox_any_test.sh unbox_any_valuetype valid object int32 "stloc.1" "ldc.i4.0\n\tbox int32"
./make_unbox_any_test.sh unbox_any_reference valid object string "stloc.1" "ldstr \"str\""
./make_unbox_any_test.sh unbox_any_reference_null valid object string "stloc.1" "ldnull"

#object is not a reference type
I=1
for TYPE in "ldc.i4.0" "ldc.i8 0" "ldc.r8 0" "ldloca 0" "ldc.i4.0\n\tconv.u"
do
	./make_unbox_any_test.sh unbox_any_bad_object_${I} unverifiable object int32 "stloc.1" "$TYPE"
	I=`expr $I + 1`
done

#token is byref, byref-like or void
./make_unbox_any_test.sh unbox_any_bad_token_1 invalid object "int32\&" "pop" "ldnull"
./make_unbox_any_test.sh unbox_any_bad_token_2 unverifiable object typedref "pop" "ldnull"
./make_unbox_any_test.sh unbox_any_bad_token_3 invalid object void "pop" "ldnull"



#stobj
#bad src
I=1
for TYPE in "int32" "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]" "native int"
do
	./make_stobj_test.sh stobj_simple_${I} valid "$TYPE" "$TYPE\&" "$TYPE"
	I=`expr $I + 1`
done


for TYPE in "int32*" "method int32 *(int32)"
do
	./make_stobj_test.sh stobj_simple_${I} unverifiable "$TYPE" "$TYPE\&" "$TYPE"
	I=`expr $I + 1`
done

for TYPE in "int32\&" "void" "typedref"
do
	./make_stobj_test.sh stobj_simple_${I} invalid "$TYPE" "$TYPE\&" "$TYPE"
	I=`expr $I + 1`
done

#src should not be ptr or byref
I=1
for TYPE in "int32\&" "int32*" "typedref"
do
	./make_stobj_test.sh stobj_bad_src_${I} unverifiable "$TYPE" "int32\&" "int32"
	I=`expr $I + 1`
done

#dest type is not a managed pointer
I=1
for TYPE in "int32" "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]" "native int"
do
	./make_stobj_test.sh stobj_dest_not_managed_pointer_${I} unverifiable "$TYPE" "$TYPE" "$TYPE"
	I=`expr $I + 1`
done

#src is compat to dest
I=1
for TYPE in "int8" "unsigned int8" "bool" "int16" "unsigned int16" "char" "int32" "unsigned int32" "native int" "native unsigned int"
do
	./make_stobj_test.sh stobj_src_compat_to_token_${I} valid "$TYPE" "int32\&" "int32"
	I=`expr $I + 1`
done

for TYPE in "int64" "unsigned int64" "float32" "float64" string object
do
	./make_stobj_test.sh stobj_src_compat_to_token_${I} unverifiable "$TYPE" "int32\&" "int32"
	I=`expr $I + 1`
done

for TYPE in string object Class
do
	./make_stobj_test.sh stobj_src_compat_to_token_${I} valid "$TYPE" "object\&" "object"
	I=`expr $I + 1`
done

./make_stobj_test.sh stobj_src_compat_to_token_boxed_vt valid "int32" "object\&" "object" "box int32"
./make_stobj_test.sh stobj_src_compat_to_token_null_literal valid "object" "object\&" "object" "pop\n\tldnull"


#token type subtype of dest_type
for TYPE in string object Class "int32[]" "int32[,]"
do
	./make_stobj_test.sh stobj_token_subtype_of_dest_${I} valid "$TYPE" "object\&" "$TYPE"
	I=`expr $I + 1`
done




#initobj
I=1
for TYPE in int32 int64 float32 float64 object string MyStruct Class "valuetype StructTemplate\`1<object>" "native int"
do
	./make_initobj_test.sh initobj_good_types_${I} valid "${TYPE}\&" "${TYPE}"
	I=`expr $I + 1`
done

#pointers
I=1
for TYPE in "native int" "int32*"
do
	./make_initobj_test.sh initobj_pointer_like_types_${I} unverifiable "${TYPE}" "${TYPE}"
	I=`expr $I + 1`
done

#bad dest
I=1
for TYPE in int32 int64 float32 float64 string MyStruct typedref
do
	./make_initobj_test.sh initobj_wrong_on_stack_types_${I} unverifiable "${TYPE}" "${TYPE}"
	I=`expr $I + 1`
done


#invalid token
I=1
for TYPE in "int32\&" void
do
	./make_initobj_test.sh initobj_bad_token_type_${I} invalid "int32\&" "${TYPE}"
	I=`expr $I + 1`
done

#bad token
I=1
for TYPE in int64 float32 float64 object string MyStruct Class "valuetype StructTemplate\`1<object>"
do
	./make_initobj_test.sh initobj_wrong_type_on_stack_${I} unverifiable "int32\&" "${TYPE}"
	I=`expr $I + 1`
done

#type and token are compatible
./make_initobj_test.sh initobj_compatible_type_on_stack_1 valid "int32\&" "native int"
./make_initobj_test.sh initobj_compatible_type_on_stack_2 strict "object\&" "string"
./make_initobj_test.sh initobj_compatible_type_on_stack_3 unverifiable "string\&" "object"

./make_initobj_test.sh initobj_stack_underflow invalid "int32\&" "int32" "pop"

./make_initobj_test.sh initobj_null_literal unverifiable "int32\&" "int32" "pop\n\tldnull"
./make_initobj_test.sh initobj_boxed_value unverifiable "int32\&" "int32" "pop\n\tldc.i4.0\n\tbox int32"



#cpobj
I=1
for TYPE in int32 int64 float32 float64 object string "valuetype MyStruct" "int32[]" "int32[,]" "native int"
do
	./make_cpobj_test.sh cpobj_simple_${I} valid "${TYPE}\&" "${TYPE}\&" "${TYPE}"
	I=`expr $I + 1`
done

#should be able to use unmanaged types
for TYPE in "int32*" "method int32 *(int32)"
do
	./make_cpobj_test.sh cpobj_simple_${I} unverifiable "${TYPE}\&" "${TYPE}\&" "${TYPE}"
	I=`expr $I + 1`
done

#should be able to use invalid types
for TYPE in "int32\&" "void" "typedref"
do
	./make_cpobj_test.sh cpobj_simple_${I} invalid "${TYPE}\&" "${TYPE}\&" "${TYPE}"
	I=`expr $I + 1`
done

#src not a managed pointer
I=1
for TYPE in "int32" "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]" "native int"
do
	./make_cpobj_test.sh cpobj_src_not_byref_${I} unverifiable "${TYPE}" "${TYPE}\&" "${TYPE}"
	I=`expr $I + 1`
done

#dest not a managed pointer
I=1
for TYPE in "int32" "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]" "native int"
do
	./make_cpobj_test.sh cpobj_dest_not_byref_${I} unverifiable "${TYPE}\&" "${TYPE}" "${TYPE}"
	I=`expr $I + 1`
done

#src and dest not a managed pointer
I=1
for TYPE in "int32" "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]" "native int"
do
	./make_cpobj_test.sh cpobj_src_and_dest_not_byref_${I} unverifiable "${TYPE}" "${TYPE}" "${TYPE}"
	I=`expr $I + 1`
done


#bad token type
I=1
for TYPE in "int32\&"
do
	./make_cpobj_test.sh cpobj_bad_token_type_${I} invalid "int32\&" "int32\&" "${TYPE}"
	I=`expr $I + 1`
done

./make_cpobj_test.sh cpobj_bad_token_type_2 badmd "int32\&" "int32\&" "void"

#src compat to token
./make_cpobj_test.sh cpobj_src_compat_1 valid "int32\&" "int32\&" "native int"
./make_cpobj_test.sh cpobj_src_compat_2 valid "native int\&" "int32\&" "int32"

./make_cpobj_test.sh cpobj_src_compat_3 valid "string\&" "object\&" "object"
./make_cpobj_test.sh cpobj_src_compat_4 valid "Class\&" "object\&" "object"

./make_cpobj_test.sh cpobj_src_compat_5 unverifiable "object\&" "string\&" "string"
./make_cpobj_test.sh cpobj_src_compat_6 unverifiable "object\&" "Class\&" "Class"


#src not compat to token
I=1
for TYPE in  "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]"
do
	./make_cpobj_test.sh cpobj_src_not_compat_to_token_${I} unverifiable "${TYPE}\&" "int32\&" "int32"
	I=`expr $I + 1`
done

#token compat to dest
./make_cpobj_test.sh cpobj_token_compat_1 valid "int32\&" "int32\&" "native int"
./make_cpobj_test.sh cpobj_token_compat_2 valid "int32\&" "native int\&" "int32"

./make_cpobj_test.sh cpobj_token_compat_3 valid "Class\&" "object\&" "Class"
./make_cpobj_test.sh cpobj_token_compat_4 valid "string\&" "object\&" "string"

./make_cpobj_test.sh cpobj_token_compat_5 unverifiable "object\&" "Class\&" "object"
./make_cpobj_test.sh cpobj_token_compat_6 unverifiable "object\&" "string\&" "object"

#token to compat to dest
I=1
for TYPE in  "int64" "float32" "float64" Class MyStruct string object "int32[]" "int32[,]"
do
	./make_cpobj_test.sh cpobj_token_not_compat_to_dest${I} unverifiable "int32\&" "int32\&" "${TYPE}"
	I=`expr $I + 1`
done

#src and dest not a managed pointer
./make_cpobj_test.sh cpobj_bad_src_and_dest unverifiable "int32" "int32" "int32"

#src or dest are null or boxed
./make_cpobj_test.sh cpobj_src_is_null unverifiable "int32\&" "int32\&" "int32" "pop\n\tldnull"
./make_cpobj_test.sh cpobj_dest_is_null unverifiable "int32\&" "int32\&" "int32" "pop\n\tpop\n\tldnull\n\tldloc.0"

./make_cpobj_test.sh cpobj_src_is_boxed unverifiable "int32" "int32\&" "int32" "box int32"
./make_cpobj_test.sh cpobj_dest_is_boxed unverifiable "int32\&" "int32" "int32" "pop\n\tbox int32\n\tldloc.0"

./make_cpobj_test.sh cpobj_underflow_1 invalid "int32\&" "int32\&" "int32" "pop"
./make_cpobj_test.sh cpobj_underflow_2 invalid "int32\&" "int32\&" "int32" "pop\n\tpop"



#sizeof
I=1
for TYPE in int32 object string "int32[]" "native int" "int32[,]" typedref "int32*" "method int32 *(int32)" "class Template\`1<int32>" "valuetype MyStruct"
do
	./make_sizeof_test.sh sizeof_${I} valid "$TYPE"
	I=`expr $I + 1`
done

for TYPE in void "int32&"
do
	./make_sizeof_test.sh sizeof_${I} invalid "$TYPE"
	I=`expr $I + 1`
done


#localloc

#valid types
I=1
for INIT in "ldc.i4.1" "ldc.i4.1\n\tconv.i"
do
	./make_localloc_test.sh localloc_stack_type_$I unverifiable "$INIT"
	I=`expr $I + 1`
done

#these types should be invalid
for INIT in "ldc.i8 2" "ldc.r4 2.2" "ldc.r8 2.2" "ldloca 1"
do
	./make_localloc_test.sh localloc_stack_type_$I unverifiable "$INIT"
	I=`expr $I + 1`
done

#stack underflow
./make_localloc_test.sh localloc_empty_stack invalid
./make_localloc_test.sh localloc_stack_with_more_than_2_items invalid "ldc.i4.1\n\tldc.i4.1"

#inside exception blocks
./make_localloc_test.sh localloc_inside_catch_handler invalid "ldc.i4.1" "catch"
./make_localloc_test.sh localloc_inside_filter invalid "ldc.i4.1" "filter"
./make_localloc_test.sh localloc_inside_handler invalid "ldc.i4.1" "handler"
./make_localloc_test.sh localloc_inside_finally invalid "ldc.i4.1" "finally"
./make_localloc_test.sh localloc_inside_fault invalid "ldc.i4.1" "fault"






#tests for call and callvirt

#call test
#invalid method token
#valid
#validate the this pointer for signatures with HASTHIS.
#this ptr: reference types must be a value, value type can be a MP or a BT.
#number of args
#args are compatible
#method is abstract
#calling base class constructor
#calling a value type constructor
#visibility
#calling non-final virtual calls on something not a boxed valuetype, this arg must have THIS_POINTER_MASK and no starg.0 or ldarga.0 happens


I=1
for CTYPE in "call" "callvirt"
do
	./make_call_test.sh call_${I}_non_virtual_1 valid "${CTYPE} instance void ClassA::Method1()" "newobj instance void ClassA::.ctor()"
	./make_call_test.sh call_${I}_non_virtual_2 valid "${CTYPE} instance void ClassA::Method1()" "ldnull"
	./make_call_test.sh call_${I}_non_virtual_3 valid "${CTYPE} instance void ClassA::Method2(int32)" "newobj instance void ClassA::.ctor()\n\t\tldc.i4.0"
	./make_call_test.sh call_${I}_non_virtual_underflow invalid "${CTYPE} instance void ClassA::Method12(int32)" "newobj instance void ClassA::.ctor()"
	./make_call_test.sh call_${I}_non_virtual_bad_this invalid "${CTYPE} instance void ClassA::Method12(int32)" "newobj instance void ClassB::.ctor()"

	./make_call_test.sh call_${I}_non_virtual_compat_this_1 valid "${CTYPE} instance void ClassA::Method1()" "newobj instance void ClassC::.ctor()"
	./make_call_test.sh call_${I}_non_virtual_compat_this_2 valid "${CTYPE} instance void ClassC::Method1()" "newobj instance void ClassC::.ctor()"
#This test passes peverify but fails under MS runtime due to a bug on their implementation of method token resolution.
	./make_call_test.sh call_${I}_non_virtual_compat_this_3 valid "${CTYPE} instance void ClassC::Method1()" "newobj instance void ClassA::.ctor()"

	./make_call_test.sh call_${I}_final_virtual_method_1 valid "${CTYPE} instance void ClassC::VirtMethod()" "newobj instance void ClassC::.ctor()"

	./make_call_test.sh call_${I}_virtual_method_1 valid "${CTYPE} instance void Driver::VirtMethod()" "ldarg.0" "instance"
	./make_call_test.sh call_${I}_virtual_method_2 valid "${CTYPE} instance void BaseClass::VirtMethod()" "ldarg.0" "instance"

	I=`expr $I + 1`
done


#tests for call only
./make_call_test.sh call_global_1 valid "call void GlobalMethod1()"
./make_call_test.sh call_global_2 valid "call void GlobalMethod2(int32)" "ldc.i4.0"
./make_call_test.sh call_global_underflow invalid "call void GlobalMethod2(int32)" ""
./make_call_test.sh call_abstract_method unverifiable "call instance void InterfaceA::AbsMethod()" "newobj instance void ImplIfaceA::.ctor()"
./make_call_test.sh call_final_virtual_method_2 unverifiable "call instance void ClassC::VirtMethod()" "newobj instance void ClassA::.ctor()"
./make_call_test.sh call_final_virtual_method_3 unverifiable "call instance void ClassA::VirtMethod()" "newobj instance void ClassA::.ctor()"

./make_call_test.sh call_virtual_method_3 unverifiable "call instance void BaseClass::VirtMethod()" "ldarg.0" "instance" "ldarg.0\n\t\tstarg 0"
./make_call_test.sh call_virtual_method_4 unverifiable "call instance void BaseClass::VirtMethod()" "ldarg.0" "instance" "ldarga 0\n\t\tpop"

#value type (we can call non final virtual on boxed VT)
./make_call_test.sh call_valuetype_1 valid "call instance void MyValueType::Method()" "ldloca 0"
./make_call_test.sh call_valuetype_2 unverifiable "call instance void MyValueType::Method()" "ldloc.0\n\t\tbox MyValueType"
./make_call_test.sh call_valuetype_3 unverifiable "call instance void MyValueType::Method()" "ldloc.0"

./make_call_test.sh call_valuetype_4 unverifiable "call instance int32 [mscorlib]System.ValueType::GetHashCode()" "ldloca 0" "static" "pop"
./make_call_test.sh call_valuetype_5 valid "call instance int32 MyValueType::GetHashCode()" "ldloca 0" "static" "pop"

./make_call_test.sh call_valuetype_6 valid "call instance int32 [mscorlib]System.ValueType::GetHashCode()"  "ldloc.0\n\t\tbox MyValueType" "static" "pop"
./make_call_test.sh call_valuetype_7 valid "call instance bool object::Equals(object)" "ldloc.0\n\t\tbox MyValueType\n\t\tldnull" "static" "pop"

./make_call_test.sh call_valuetype_8 valid "call instance int32 [mscorlib]System.Object::GetHashCode()"  "ldloc.0\n\t\tbox MyValueType" "static" "pop"

#tests for callvirt only
#FIXME ilasm encode the signature with instance even if it doesn't state so.
#./make_call_test.sh call_virt_global_1 invalid "callvirt void GlobalMethod1()"

./make_call_test.sh callvirt_abstract_method valid "callvirt instance void InterfaceA::AbsMethod()" "newobj instance void ImplIfaceA::.ctor()"
./make_call_test.sh callvirt_final_virtual_method_2 unverifiable "callvirt instance void ClassC::VirtMethod()" "newobj instance void ClassA::.ctor()"
./make_call_test.sh callvirt_final_virtual_method_3 valid "callvirt instance void ClassA::VirtMethod()" "newobj instance void ClassA::.ctor()"

./make_call_test.sh callvirt_virtual_method_3 valid "callvirt instance void BaseClass::VirtMethod()" "ldarg.0" "instance" "ldarg.0\n\t\tstarg 0"
./make_call_test.sh callvirt_virtual_method_4 valid "callvirt instance void BaseClass::VirtMethod()" "ldarg.0" "instance" "ldarga 0\n\t\tpop"

#value type (we can call non final virtual on boxed VT)
./make_call_test.sh callvirt_valuetype_1 unverifiable "callvirt instance void MyValueType::Method()" "ldloca 0"
./make_call_test.sh callvirt_valuetype_2 unverifiable "callvirt instance void MyValueType::Method()" "ldloc.0\n\t\tbox MyValueType"
./make_call_test.sh callvirt_valuetype_3 unverifiable "callvirt instance void MyValueType::Method()" "ldloc.0"

./make_call_test.sh callvirt_valuetype_4 unverifiable "callvirt instance int32 [mscorlib]System.ValueType::GetHashCode()" "ldloca 0" "static" "pop"
./make_call_test.sh callvirt_valuetype_5 unverifiable "callvirt instance int32 MyValueType::GetHashCode()" "ldloca 0" "static" "pop"

./make_call_test.sh callvirt_valuetype_6 valid "callvirt instance int32 [mscorlib]System.ValueType::GetHashCode()"  "ldloc.0\n\t\tbox MyValueType" "static" "pop"
./make_call_test.sh callvirt_valuetype_7 valid "callvirt instance bool object::Equals(object)" "ldloc.0\n\t\tbox MyValueType\n\t\tldnull" "static" "pop"
./make_call_test.sh callvirt_valuetype_8 valid "callvirt instance int32 [mscorlib]System.Object::GetHashCode()"  "ldloc.0\n\t\tbox MyValueType" "static" "pop"


#mkrefany
./make_mkrefany.sh mkrefany_empty_stack invalid int32 int32 "pop"

./make_mkrefany.sh mkrefany_good_type_1 valid int32 int32
./make_mkrefany.sh mkrefany_good_type_2 valid int32 "unsigned int32"
./make_mkrefany.sh mkrefany_good_type_3 valid int32 "native int"
./make_mkrefany.sh mkrefany_good_type_4 valid object object

./make_mkrefany.sh mkrefany_type_not_compat_1 unverifiable string object
./make_mkrefany.sh mkrefany_type_not_compat_2 unverifiable int32 int8
./make_mkrefany.sh mkrefany_type_not_compat_3 unverifiable object string

./make_mkrefany.sh mkrefany_native_int unverifiable int32 int32 "conv.i"

./make_mkrefany.sh mkrefany_bad_type_1 unverifiable int32 int32 "pop\n\t\tldc.i4.0"

./make_mkrefany.sh mkrefany_bad_type_2 invalid int32 "int32\&"


#method definition return type validation
./make_invalid_ret_type.sh ret_type_byref unverifiable "int32\&"
./make_invalid_ret_type.sh ret_type_typedref unverifiable "typedref"
./make_invalid_ret_type.sh ret_type_arg_interator unverifiable "valuetype [mscorlib]System.ArgIterator"
./make_invalid_ret_type.sh ret_type_arg_handle unverifiable "valuetype [mscorlib]System.RuntimeArgumentHandle"



#delegate compat tests P. II 14.6
#TODO generic related testss

#for T in {}
#./make_delegate_compat_test.sh valid


#all types
#int32 int64 "native int" float64 "valuetype MyValueType" "class ClassA" "int8\&" "int16\&" "int32\&" "int64\&" "native int\&" "float32\&" "float64\&" "valuetype MyValueType\&" "class ClassA\&" "int32*" "method int32 *(int32)" "method float32 *(int32)" "method int32 *(float64)" "method vararg int32 *(int32)"
#verifiable
#int32 int64 "native int" float64 "valuetype MyValueType" "class ClassA" "int8\&" "int16\&" "int32\&" "int64\&" "native int\&" "float32\&" "float64\&" "valuetype MyValueType\&" "class ClassA\&"

I=1
for TYPE1 in int32 int64 "native int" float64 "valuetype MyValueType" "class ClassA" "int8\&" "int16\&" "int32\&" "int64\&" "native int\&" "float32\&" "float64\&" "valuetype MyValueType\&" "class ClassA\&"
do
	./make_delegate_compat_test.sh delegate_compat_basic_types_${I} valid int32 int32 "$TYPE1" "$TYPE1"
	I=`expr $I + 1`
done

for TYPE1 in "int32*" "method int32 *(int32)" "method float32 *(int32)" "method int32 *(float64)" "method vararg int32 *(int32)"
do
	./make_delegate_compat_test.sh delegate_compat_basic_types_${I} unverifiable int32 int32 "$TYPE1" "$TYPE1"
	I=`expr $I + 1`
done

#D is delegate and T is target method
#arguments
#3. D:=T if T is a base type or an interface implemented by D. D is not valuetype (primitive, pointers, etc)

./make_delegate_compat_test.sh delegate_super_type_arg_1 valid int32 int32 string object
./make_delegate_compat_test.sh delegate_super_type_arg_2 valid int32 int32 ImplA InterfaceA

./make_delegate_compat_test.sh delegate_super_type_arg_3 unverifiable int32 int32 object string
./make_delegate_compat_test.sh delegate_super_type_arg_4 unverifiable int32 int32 InterfaceA ImplA

./make_delegate_compat_test.sh delegate_super_type_arg_5 unverifiable int32 int32 object int32
./make_delegate_compat_test.sh delegate_super_type_arg_6 unverifiable int32 int32 object "int32\&"
./make_delegate_compat_test.sh delegate_super_type_arg_7 unverifiable int32 int32 object "object\&"


#primitive type size based conversion - don't work with delegates
./make_delegate_compat_test.sh delegate_primitive_arg_same_size_1 unverifiable int32 int32 "unsigned int32" int32
./make_delegate_compat_test.sh delegate_primitive_arg_same_size_2 unverifiable int32 int32 "native int" int32

./make_delegate_compat_test.sh delegate_primitive_arg_same_size_3 unverifiable int32 int32 int32 "unsigned int32"
./make_delegate_compat_test.sh delegate_primitive_arg_same_size_4 unverifiable int32 int32 int32 "native int"

#value types
./make_delegate_compat_test.sh delegate_valuetype_arg_1 valid int32 int32 "valuetype MyValueType" "valuetype MyValueType"
./make_delegate_compat_test.sh delegate_valuetype_arg_2 unverifiable int32 int32 "valuetype MyValueType" int32


#4. D:=T if both are interfaces and implementation D requires implementation of T (D is a supertype of T)
./make_delegate_compat_test.sh delegate_super_iface_arg_1 valid int32 int32 InterfaceB InterfaceA
./make_delegate_compat_test.sh delegate_super_iface_arg_2 unverifiable int32 int32 InterfaceA InterfaceB


#5. D[]:=T[] if D:=T and are both vector or both have the same rank
./make_delegate_compat_test.sh delegate_array_arg_1 valid int32 int32 "int32[]" "int32[]"
./make_delegate_compat_test.sh delegate_array_arg_2 valid int32 int32 "string[]" "object[]"

./make_delegate_compat_test.sh delegate_array_arg_3 unverifiable int32 int32 "object[]" "object[,]"
./make_delegate_compat_test.sh delegate_array_arg_4 unverifiable int32 int32 "object[,]" "object[]"
./make_delegate_compat_test.sh delegate_array_arg_5 unverifiable int32 int32 "object[]" "string[]"

#6. D:=T if both are method pointers and obey this rules for all args and params
#TODO how can we cook a test that will be verifiable but use such types
./make_delegate_compat_test.sh delegate_method_ptr_arg_1 unverifiable int32 int32 "method int32 *(float64)" "method int32 *(float64)"
#no way do say this is invalid
./make_delegate_compat_test.sh delegate_method_ptr_arg_2 unverifiable int32 int32 "method int32 *(float64)" "method int32 *(int32)"
#and that it is valid
./make_delegate_compat_test.sh delegate_method_ptr_arg_2 unverifiable int32 int32 "method int32 *(string)" "method int32 *(object)"


#TODO we don't perform proper type load verification
#./make_delegate_compat_test.sh delegate_void_args invalid int32 int32 void void

./make_delegate_compat_test.sh delegate_enum_args_1 unverifiable int32 int32 Int32Enum int32
./make_delegate_compat_test.sh delegate_enum_args_2 unverifiable int32 int32 int32 Int32Enum

#TODO check using native method
./make_delegate_compat_test.sh delegate_pointers_args_1 unverifiable int32 int32 "string*" "object*"
./make_delegate_compat_test.sh delegate_pointers_args_2 unverifiable int32 int32 "ImplA*" "InterfaceA*"
./make_delegate_compat_test.sh delegate_pointers_args_3 unverifiable int32 int32 "object*" "string*"
./make_delegate_compat_test.sh delegate_pointers_args_4 unverifiable int32 int32 "int32*" "int32*"


#TODO: 7,8,9 generic related.

#call conv
./make_delegate_compat_test.sh delegate_bad_cconv_1 unverifiable int32 int32 int32 int32 "default" "vararg"
#This is invalid because we don't properly decode memberref signatures
./make_delegate_compat_test.sh delegate_bad_cconv_2 invalid int32 int32 int32 int32 "vararg" "	"

#return type

#3. D:=T if D is a basetype or implemented interface of T and T is not a valuetype

./make_delegate_compat_test.sh delegate_super_type_ret_1 valid object string int32 int32
./make_delegate_compat_test.sh delegate_super_type_ret_2 valid InterfaceA ImplA int32 int32

./make_delegate_compat_test.sh delegate_super_type_ret_3 unverifiable string object int32 int32
./make_delegate_compat_test.sh delegate_super_type_ret_4 unverifiable ImplA InterfaceA int32 int32

./make_delegate_compat_test.sh delegate_super_type_ret_5 unverifiable object int32 int32 int32
./make_delegate_compat_test.sh delegate_super_type_ret_6 unverifiable object "int32\&" int32 int32
./make_delegate_compat_test.sh delegate_super_type_ret_7 unverifiable object "object\&" int32 int32


#primitive type size based conversion - don't work with delegates
./make_delegate_compat_test.sh delegate_primitive_ret_same_size_1 unverifiable "unsigned int32" int32 int32 int32
./make_delegate_compat_test.sh delegate_primitive_ret_same_size_2 unverifiable "native int" int32 int32 int32

./make_delegate_compat_test.sh delegate_primitive_ret_same_size_3 unverifiable int32 "unsigned int32" int32 int32
./make_delegate_compat_test.sh delegate_primitive_ret_same_size_4 unverifiable int32 "native int" int32 int32

#value types
./make_delegate_compat_test.sh delegate_valuetype_ret_1 valid "valuetype MyValueType" "valuetype MyValueType" int32 int32
./make_delegate_compat_test.sh delegate_valuetype_ret_2 unverifiable "valuetype MyValueType" int32 int32 int32
./make_delegate_compat_test.sh delegate_valuetype_ret_2 unverifiable int32 "valuetype MyValueType" int32 int32


#4. D:=T if both are interfaces and implementation T requires implementation of D (T is a supertype of D)
./make_delegate_compat_test.sh delegate_super_iface_arg_1 valid InterfaceA InterfaceB int32 int32
./make_delegate_compat_test.sh delegate_super_iface_arg_2 valid InterfaceA InterfaceA int32 int32
./make_delegate_compat_test.sh delegate_super_iface_arg_3 unverifiable InterfaceB InterfaceA int32 int32


#5. D[]:=T[] if D:=T and are both vector or both have the same rank
./make_delegate_compat_test.sh delegate_array_ret_1 valid "int32[]" "int32[]" int32 int32
./make_delegate_compat_test.sh delegate_array_ret_2 valid "object[]" "string[]" int32 int32

./make_delegate_compat_test.sh delegate_array_ret_3 unverifiable "object[]" "object[,]" int32 int32
./make_delegate_compat_test.sh delegate_array_ret_4 unverifiable "object[,]" "object[]" int32 int32
./make_delegate_compat_test.sh delegate_array_ret_5 unverifiable "string[]" "object[]" int32 int32

#6. D:=T if both are method pointers and obey this rules for all args and params
#TODO how can we cook a test that will be verifiable but use such types
./make_delegate_compat_test.sh delegate_method_ptr_arg_1 unverifiable "method int32 *(float64)" "method int32 *(float64)" int32 int32
#no way do say this is invalid
./make_delegate_compat_test.sh delegate_method_ptr_arg_2 unverifiable "method int32 *(float64)" "method int32 *(int32)" int32 int32
#and that it is valid
./make_delegate_compat_test.sh delegate_method_ptr_arg_2 unverifiable "method int32 *(object)" "method int32 *(string)" int32 int32

#TODO: 7,8,9 generic related.


./make_delegate_compat_test.sh delegate_void_ret valid void void int32 int32

./make_delegate_compat_test.sh delegate_enum_ret_1 unverifiable int32 int32 Int32Enum int32
./make_delegate_compat_test.sh delegate_enum_ret_2 unverifiable int32 int32 int32 Int32Enum

./make_delegate_compat_test.sh delegate_pointers_ret_1 unverifiable "object*" "string*" int32 int32
./make_delegate_compat_test.sh delegate_pointers_ret_2 unverifiable "InterfaceA*" "ImplA*" int32 int32
./make_delegate_compat_test.sh delegate_pointers_ret_3 unverifiable "string*" "object*" int32 int32
./make_delegate_compat_test.sh delegate_pointers_ret_4 unverifiable  "int32*" "int32*" int32 int32


#pointer tests using native method and not invoking
./make_delegate_compat_test.sh delegate_method_ptr_pinvoke_arg_1 valid int32 int32 "method int32 *(float64)" "method int32 *(float64)" "" "" "pinvoke"
#no way do say this is invalid
./make_delegate_compat_test.sh delegate_method_ptr_pinvoke_arg_2 unverifiable int32 int32 "method int32 *(float64)" "method int32 *(int32)" "" "" "pinvoke"


#constrained prefix

#good type tests
I=1
for TYPE in object string MyValueType
do
	./make_constrained_test.sh constrained_prefix_basic_types_$I valid "$TYPE" "$TYPE" "callvirt string object::ToString()"
	I=`expr $I + 1`
done

#method that exist on the value type
./make_constrained_test.sh constrained_prefix_basic_types_$I valid "MyValueType" "MyValueType" "callvirt int32 object::GetHashCode()"


#mismatch between constrained. type token and this argument
./make_constrained_test.sh constrained_prefix_type_mismatch_1 unverifiable "object" "string" "callvirt instance int32 object::GetHashCode()"
./make_constrained_test.sh constrained_prefix_type_mismatch_2 unverifiable "string" "object" "callvirt instance int32 object::GetHashCode()"

./make_constrained_test.sh constrained_prefix_type_mismatch_3 unverifiable "object" "MyValueType" "callvirt instance int32 object::GetHashCode()"
./make_constrained_test.sh constrained_prefix_type_mismatch_4 unverifiable "MyValueType" "object" "callvirt instance int32 object::GetHashCode()"

#bad constrained token
./make_constrained_test.sh constrained_prefix_bad_token_1 unverifiable "object" "object&" "callvirt instance int32 object::GetHashCode()"
./make_constrained_test.sh constrained_prefix_bad_token_1 unverifiable "object*" "object*" "callvirt instance int32 object::GetHashCode()"

#constrained no before a callvirt
./make_constrained_test.sh constrained_prefix_not_before_a_callvirt invalid "string" "string" "call instance string string::Trim()"

#wrong stack type

./make_constrained_test.sh constrained_prefix_bad_stack_type_1 unverifiable "object" "object" "callvirt instance int32 object::GetHashCode()" "ldloc.0"
./make_constrained_test.sh constrained_prefix_bad_stack_type_2 unverifiable "object" "object" "callvirt instance int32 object::GetHashCode()" "ldnull"
./make_constrained_test.sh constrained_prefix_bad_stack_type_3 unverifiable "object" "object" "callvirt instance int32 object::GetHashCode()" "ldc.i4.0"



#cmmp support
#ldind.x tests
I=1
for TYPE in "ldind.i1 int8" "ldind.u1 int8" "ldind.i2 int16" "ldind.u2 int16" "ldind.i4 int32" "ldind.u4 int32" "ldind.i8 int64" "ldind.u8 int64" "ldind.i native int" "ldind.r4 float32" "ldind.r8 float64"
do
	LOAD=`echo $TYPE | cut -d' ' -f 1`
	TYPE=`echo $TYPE | cut -d' ' -f 2-`
	./make_cmmp_test.sh cmmp_basic_test_ro_$I valid "readonly. ldelema $TYPE" "$LOAD" "$TYPE"
	./make_cmmp_test.sh cmmp_basic_test_ub_$I valid "unbox $TYPE" "$LOAD" "$TYPE"
	I=`expr $I + 1`
done

#unbox is only for value types, so cannot be paired with ldind.ref
./make_cmmp_test.sh cmmp_basic_test_ro_$I valid "readonly. ldelema object" "ldind.ref" "object"

./make_cmmp_test.sh cmmp_basic_test_ro_ldobj valid "readonly. ldelema int32" "ldobj int32" "int32"
./make_cmmp_test.sh cmmp_basic_test_ub_ldobj valid "unbox int32" "ldobj int32" "int32"

./make_cmmp_test.sh cmmp_basic_test_ro_ldfld valid "readonly. ldelema valuetype MyStruct" "ldfld int32 MyStruct::foo" "valuetype MyStruct"
./make_cmmp_test.sh cmmp_basic_test_ub_ldfld valid "unbox valuetype MyStruct" "ldfld int32 MyStruct::foo" "valuetype MyStruct "

./make_cmmp_test.sh cmmp_basic_test_ro_ldflda valid "readonly. ldelema valuetype MyStruct" "ldflda int32 MyStruct::foo" "valuetype MyStruct"
./make_cmmp_test.sh cmmp_basic_test_ub_ldflda valid "unbox valuetype MyStruct" "ldflda int32 MyStruct::foo" "valuetype MyStruct "

#as the object parameter of callvirt, constrained. callvirt
#ldobj

./make_cmmp_test.sh cmmp_basic_test_ro_stfld_ptr valid "readonly. ldelema valuetype MyStruct" "ldc.i4.0\n\tstfld int32 MyStruct::foo" "valuetype MyStruct"
./make_cmmp_test.sh cmmp_basic_test_ub_stfld_ptr valid "unbox valuetype MyStruct" "ldc.i4.0\n\tstfld int32 MyStruct::foo" "valuetype MyStruct "

#testing for second argument in stfld is pointless as fields cannot be a byref type

#can be the this ptr for call
./make_cmmp_test.sh cmmp_basic_test_ro_call_this valid "readonly. ldelema int32" "ldc.i4.0\n\tcall instance int32 int32::CompareTo(int32)" "int32"
./make_cmmp_test.sh cmmp_basic_test_ub_call_this valid "unbox int32" "ldc.i4.0\n\tcall instance int32 int32::CompareTo(int32)" "int32"


#cannot be parameter
./make_cmmp_test.sh cmmp_basic_test_ro_call_arg unverifiable "readonly. ldelema valuetype MyStruct" "call void MyStruct::Test(MyStruct\&)" "valuetype MyStruct"
./make_cmmp_test.sh cmmp_basic_test_ub_call_arg unverifiable "unbox valuetype MyStruct" "call void MyStruct::Test(MyStruct\&)" "valuetype MyStruct"


#FIXME it's not possible to use a managed pointer with an unconstrained callvirt

#constrained. callvirt
./make_cmmp_test.sh cmmp_basic_test_ro_callvirt_this valid "readonly. ldelema int32" "constrained. int32 callvirt instance int32 object::GetHashCode()" "int32"
./make_cmmp_test.sh cmmp_basic_test_ub_callvirt_this valid "unbox int32" "constrained. int32 callvirt instance int32 object::GetHashCode()" "int32"


#constrained. callvirt as parameter
./make_cmmp_test.sh cmmp_basic_test_ro_callvirt_arg unverifiable "readonly. ldelema ClassA" "dup\n\tconstrained. ClassA callvirt instance void ClassA::VirtTest(ClassA\&)" "ClassA"


#invalid instructions with cmmp argument
#test, at least, stobj, cpobj, stind, initobj, mkrefany, newobj, ceq

#stind.x
I=1
for TYPE in "stind.i1 int8" "stind.i2 int16" "stind.i4 int32" "stind.i8 int64" "stind.r4 float32" "stind.r8 float64" "stind.i native int"
do
	STORE=`echo $TYPE | cut -d' ' -f 1`
	TYPE=`echo $TYPE | cut -d' ' -f 2-`
	./make_cmmp_test.sh cmmp_bad_ops_test_ro_$I unverifiable "readonly. ldelema $TYPE" "ldloc.0\n\t$STORE" "$TYPE"
	./make_cmmp_test.sh cmmp_bad_ops_test_ub_$I unverifiable "unbox $TYPE" "ldloc.0\n\t$STORE" "$TYPE"
	I=`expr $I + 1`
done

#unbox is only for value types, so cannot be paired with ldind.ref
./make_cmmp_test.sh cmmp_bad_ops_test_ro_$I unverifiable "readonly. ldelema object" "ldloc.0\n\tstind.ref" "object"


./make_cmmp_test.sh cmmp_stobj_test_ro unverifiable "readonly. ldelema int32" "ldloc.0\n\tstobj int32" "int32"
./make_cmmp_test.sh cmmp_stobj_test_ub unverifiable "unbox int32" "ldloc.0\n\tstobj int32" "int32"

./make_cmmp_test.sh cmmp_cpobj_src_ro valid "readonly. ldelema int32" "cpobj int32" "int32" "ldloca 0"
./make_cmmp_test.sh cmmp_cpobj_src_ub valid "unbox int32" "cpobj int32" "int32" "ldloca 0"

./make_cmmp_test.sh cmmp_cpobj_dst_ro unverifiable "readonly. ldelema int32" "ldloca 0\n\tcpobj int32" "int32"
./make_cmmp_test.sh cmmp_cpobj_dst_ub unverifiable "unbox int32" "ldloca 0\n\tcpobj int32" "int32"

./make_cmmp_test.sh cmmp_initobj_test_ro unverifiable "readonly. ldelema int32" "initobj int32" "int32"
./make_cmmp_test.sh cmmp_initobj_test_ub unverifiable "unbox int32" "initobj int32" "int32"

./make_cmmp_test.sh cmmp_mkrefany_test_ro unverifiable "readonly. ldelema int32" "mkrefany int32" "int32"
./make_cmmp_test.sh cmmp_mkrefany_test_ub unverifiable "unbox int32" "mkrefany int32" "int32"

./make_cmmp_test.sh cmmp_newobj_test_ro unverifiable "readonly. ldelema int32" "newobj instance void class ClassA::.ctor(int32\&)" "int32"
./make_cmmp_test.sh cmmp_newobj_test_ub unverifiable "unbox int32" "newobj instance void class ClassA::.ctor(int32\&)" "int32"

./make_cmmp_test.sh cmmp_ceq_test_ro valid "readonly. ldelema int32" "dup\n\tceq" "int32"
./make_cmmp_test.sh cmmp_ceq_test_ub valid "unbox int32" "dup\n\tceq" "int32"

./make_cmmp_test.sh cmmp_basic_test_address_method_1 valid "ldc.i4.1\n\tldc.i4.1\n\tnewobj instance void int32[,]::.ctor(int32, int32)\n\tldc.i4.0\n\tldc.i4.0\n\tcall instance int32\&  int32[,]::Address(int32, int32)" "ldloc.0\n\tstind.i4" "int32"

./make_cmmp_test.sh cmmp_basic_test_address_method_2 unverifiable "ldc.i4.1\n\tldc.i4.1\n\tnewobj instance void int32[,]::.ctor(int32, int32)\n\tldc.i4.0\n\tldc.i4.0\n\treadonly. call instance int32\&  int32[,]::Address(int32, int32)" "ldloc.0\n\tstind.i4" "int32"


#readonly before an invalid instruction
./make_cmmp_test.sh readonly_before_invalid_instruction_1 invalid "readonly. ldelem int32" "nop" "int32"
./make_cmmp_test.sh readonly_before_invalid_instruction_2 invalid "ldstr \"tst\"\n\treadonly. call string int32::Parse(string)" "nop" "int32"



#type constraint verification
#see PII 10.1.7 for details

#TODO test usage of open types
#TODO test recursive types

I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_no_ct_$I valid "$TYPE" ""
	./make_type_constraint_test.sh type_constraint_object_ct_$I valid "$TYPE" "(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_no_ct_$I valid "$TYPE" ""
	./make_method_constraint_test.sh method_constraint_object_ct_$I valid "$TYPE" "(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_class_ct_$I valid "$TYPE" "class "
	./make_type_constraint_test.sh type_constraint_class_object_ct_$I valid "$TYPE" "class (class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_ct_$I valid "$TYPE" "class "
	./make_method_constraint_test.sh method_constraint_class_object_ct_$I valid "$TYPE" "class (class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

for TYPE in "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_class_ct_$I invalid "$TYPE" "class "
	./make_type_constraint_test.sh type_constraint_class_object_ct_$I invalid "$TYPE" "class (class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_ct_$I invalid "$TYPE" "class "
	./make_method_constraint_test.sh method_constraint_class_object_ct_$I invalid "$TYPE" "class (class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object AbstractClass ClassWithDefaultCtor IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_class_ctor_ct_$I valid "$TYPE" "class .ctor"
	./make_type_constraint_test.sh type_constraint_class_ctor_object_ct_$I valid "$TYPE" "class .ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_ctor_ct_$I valid "$TYPE" "class .ctor"
	./make_method_constraint_test.sh method_constraint_class_ctor_object_ct_$I valid "$TYPE" "class .ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

for TYPE in ClassNoDefaultCtor ClassWithDefaultCtorNotVisible "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_class_ctor_ct_$I invalid "$TYPE" "class .ctor"
	./make_type_constraint_test.sh type_constraint_class_ctor_object_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_ctor_ct_$I invalid "$TYPE" "class .ctor"
	./make_method_constraint_test.sh method_constraint_class_ctor_object_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_vt_ct_$I invalid "$TYPE" "valuetype"
	./make_type_constraint_test.sh type_constraint_vt_object_ct_$I invalid "$TYPE" "valuetype(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_vt_ct_$I invalid "$TYPE" "valuetype"
	./make_method_constraint_test.sh method_constraint_vt_object_ct_$I invalid "$TYPE" "valuetype(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

for TYPE in "valuetype MyValueType" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_vt_ct_$I valid "$TYPE" "valuetype"
	./make_type_constraint_test.sh type_constraint_vt_object_ct_$I valid "$TYPE" "valuetype(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_vt_ct_$I valid "$TYPE" "valuetype"
	./make_method_constraint_test.sh method_constraint_vt_object_ct_$I valid "$TYPE" "valuetype(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_vt_ctor_ct_$I invalid "$TYPE"  "valuetype .ctor"
	./make_type_constraint_test.sh type_constraint_vt_ctor_object_ct_$I invalid "$TYPE"  "valuetype .ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_vt_ctor_ct_$I invalid "$TYPE"  "valuetype .ctor"
	./make_method_constraint_test.sh method_constraint_vt_ctor_object_ct_$I invalid "$TYPE"  "valuetype .ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

for TYPE in "valuetype MyValueType" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_vt_ctor_ct_$I valid "$TYPE"  "valuetype .ctor"
	./make_type_constraint_test.sh type_constraint_vt_ctor_object_ct_$I valid "$TYPE"  "valuetype .ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_vt_ctor_ct_$I valid "$TYPE"  "valuetype .ctor"
	./make_method_constraint_test.sh method_constraint_vt_ctor_object_ct_$I valid "$TYPE"  "valuetype .ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object AbstractClass ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_ctor_ct_$I valid "$TYPE" ".ctor"
	./make_type_constraint_test.sh type_constraint_ctor_object_ct_$I valid "$TYPE" ".ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_ctor_ct_$I valid "$TYPE" ".ctor"
	./make_method_constraint_test.sh method_constraint_ctor_object_ct_$I valid "$TYPE" ".ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

for TYPE in ClassNoDefaultCtor ClassWithDefaultCtorNotVisible "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace
do
	./make_type_constraint_test.sh type_constraint_ctor_ct_$I invalid "$TYPE" ".ctor"
	./make_type_constraint_test.sh type_constraint_ctor_object_ct_$I invalid "$TYPE" ".ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_ctor_ct_$I invalid "$TYPE" ".ctor"
	./make_method_constraint_test.sh method_constraint_ctor_object_ct_$I invalid "$TYPE" ".ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_class_vt_ct_$I invalid "$TYPE" "class valuetype"
	./make_type_constraint_test.sh type_constraint_class_vt_object_ct_$I invalid "$TYPE" "class valuetype(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_vt_ct_$I invalid "$TYPE" "class valuetype"
	./make_method_constraint_test.sh method_constraint_class_vt_object_ct_$I invalid "$TYPE" "class valuetype(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done

I=1
for TYPE in ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace
do
	./make_type_constraint_test.sh type_constraint_class_vt_ctor_ct_$I invalid "$TYPE" "class valuetype .ctor"
	./make_type_constraint_test.sh type_constraint_class_vt_ctor_object_ct_$I invalid "$TYPE" "class valuetype .ctor(class [mscorlib]System.Object)"

	./make_method_constraint_test.sh method_constraint_class_vt_ctor_ct_$I invalid "$TYPE" "class valuetype .ctor"
	./make_method_constraint_test.sh method_constraint_class_vt_ctor_object_ct_$I invalid "$TYPE" "class valuetype .ctor(class [mscorlib]System.Object)"

	I=`expr $I + 1`
done



I=1
for TYPE in "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_ctor_ct_$I valid "$TYPE" ".ctor (class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_ctor_ct_$I valid "$TYPE" ".ctor (class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_ctor_ct_$I invalid "$TYPE" ".ctor (class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_ctor_ct_$I invalid "$TYPE" ".ctor (class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done


I=1
for TYPE in "valuetype MyValueType" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_vt_ct_$I valid "$TYPE" "valuetype(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_vt_ct_$I valid "$TYPE" "valuetype(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor  "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_vt_ct_$I invalid "$TYPE" "valuetype(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_vt_ct_$I invalid "$TYPE" "valuetype(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done


I=1
for TYPE in "valuetype MyValueType" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_vt_ctor_ct_$I valid "$TYPE" "valuetype .ctor(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_vt_ctor_ct_$I valid "$TYPE" "valuetype .ctor(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_vt_ctor_ct_$I invalid "$TYPE" "valuetype .ctor(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_vt_ctor_ct_$I invalid "$TYPE" "valuetype .ctor(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done


I=1
for TYPE in "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum"  "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_ct_$I valid "$TYPE" "(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_ct_$I valid "$TYPE" "(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_ct_$I invalid "$TYPE" "(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_ct_$I invalid "$TYPE" "(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_class_ct_$I invalid "$TYPE" "class (class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_class_ct_$I invalid "$TYPE" "class (class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done

for TYPE in "[mscorlib]System.ValueType" "[mscorlib]System.Enum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_class_ct_$I valid "$TYPE" "class (class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_class_ct_$I valid "$TYPE" "class (class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_valuetype_class_ctor_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.ValueType)"

	./make_method_constraint_test.sh method_constraint_system_valuetype_class_ctor_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.ValueType)"

	I=`expr $I + 1`
done



I=1
for TYPE in "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_enum_ctor_ct_$I valid "$TYPE" ".ctor (class [mscorlib]System.Enum)"
	./make_type_constraint_test.sh type_constraint_system_enum_vt_ct_$I valid "$TYPE" "valuetype (class [mscorlib]System.Enum)"
	./make_type_constraint_test.sh type_constraint_system_enum_vt_ctor_ct_$I valid "$TYPE" "valuetype .ctor(class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_ctor_ct_$I valid "$TYPE" ".ctor (class [mscorlib]System.Enum)"
	./make_method_constraint_test.sh method_constraint_system_enum_vt_ct_$I valid "$TYPE" "valuetype (class [mscorlib]System.Enum)"
	./make_method_constraint_test.sh method_constraint_system_enum_vt_ctor_ct_$I valid "$TYPE" "valuetype .ctor(class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_enum_ctor_ct_$I invalid "$TYPE" ".ctor (class [mscorlib]System.Enum)"
	./make_type_constraint_test.sh type_constraint_system_enum_vt_ct_$I invalid "$TYPE" "valuetype (class [mscorlib]System.Enum)"
	./make_type_constraint_test.sh type_constraint_system_enum_vt_ctor_ct_$I invalid "$TYPE" "valuetype .ctor(class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_ctor_ct_$I invalid "$TYPE" ".ctor (class [mscorlib]System.Enum)"
	./make_method_constraint_test.sh method_constraint_system_enum_vt_ct_$I invalid "$TYPE" "valuetype (class [mscorlib]System.Enum)"
	./make_method_constraint_test.sh method_constraint_system_enum_vt_ctor_ct_$I invalid "$TYPE" "valuetype .ctor(class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done


I=1
for TYPE in "[mscorlib]System.Enum" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_enum_ct_$I valid "$TYPE" "(class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_ct_$I valid "$TYPE" "(class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_system_enum_ct_$I invalid "$TYPE" "(class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_ct_$I invalid "$TYPE" "(class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done


I=1
for TYPE in "[mscorlib]System.Enum"
do
	./make_type_constraint_test.sh type_constraint_system_enum_class_ct_$I valid "$TYPE" "class (class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_class_ct_$I valid "$TYPE" "class (class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_enum_class_ct_$I invalid "$TYPE" "class (class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_class_ct_$I invalid "$TYPE" "class (class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done


I=1
for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" IFace IFaceImpl "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_system_enum_class_ctor_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.Enum)"

	./make_method_constraint_test.sh method_constraint_system_enum_class_ctor_ct_$I invalid "$TYPE" "class .ctor(class [mscorlib]System.Enum)"

	I=`expr $I + 1`
done


I=1
for TYPE in IFace IFaceImpl
do
	./make_type_constraint_test.sh type_constraint_iface_ct_$I valid "$TYPE" "(IFace)"

	./make_method_constraint_test.sh method_constraint_iface_ct_$I valid "$TYPE" "(IFace)"

	I=`expr $I + 1`
done

for TYPE in object ClassNoDefaultCtor AbstractClass ClassWithDefaultCtorNotVisible ClassWithDefaultCtor "valuetype MyValueType" "valuetype [mscorlib]System.Nullable\`1<valuetype MyValueType>" "[mscorlib]System.ValueType" "[mscorlib]System.Enum" "valuetype MyEnum"
do
	./make_type_constraint_test.sh type_constraint_iface_ct_$I invalid "$TYPE" "(IFace)"

	./make_method_constraint_test.sh method_constraint_iface_ct_$I invalid "$TYPE" "(IFace)"

	I=`expr $I + 1`
done

#misc tests
./make_type_constraint_test.sh type_constraint_nested_class invalid "class TemplateTarget<valuetype MyValueType>" "class"


#prefixes volatile. and unaligned

#stind.x
I=1
for TYPE in "stind.i1 int8" "stind.i2 int16" "stind.i4 int32" "stind.i8 int64" "stind.r4 float32" "stind.r8 float64" "stind.i native int"
do
	STORE=`echo $TYPE | cut -d' ' -f 1`
	TYPE=`echo $TYPE | cut -d' ' -f 2-`
	./make_prefix_test.sh "prefix_test_stind_volatile_$I" valid "volatile. $STORE" "ldloca 0\n\tldloc.0" "$TYPE"
	./make_prefix_test.sh "prefix_test_stind_unaligned_$I" valid  "unaligned. 1 $STORE" "ldloca 0\n\tldloc.0" "$TYPE"
	I=`expr $I + 1`
done

./make_prefix_test.sh "prefix_test_stind_volatile_$I" valid "volatile. stind.ref" "ldloca 0\n\tldnull" "object"
./make_prefix_test.sh "prefix_test_stind_unaligned_$I" valid  "unaligned. 1 stind.ref" "ldloca 0\n\tldnull" "object"


#ldind.x
I=1
for TYPE in "ldind.i1 int8" "ldind.u1 unsigned int8" "ldind.i2 int16" "ldind.u2 unsigned int16" "ldind.i4 int32" "ldind.u4 unsigned int32" "ldind.i8 int64" "ldind.u8 unsigned int64" "ldind.r4 float32" "ldind.r8 float64" "ldind.i native int"
do
	STORE=`echo $TYPE | cut -d' ' -f 1`
	TYPE=`echo $TYPE | cut -d' ' -f 2-`
	./make_prefix_test.sh "prefix_test_ldind_volatile_$I" valid "volatile. $STORE" "ldloca 0" "$TYPE"
	./make_prefix_test.sh "prefix_test_ldind_unaligned_$I" valid  "unaligned. 1 $STORE" "ldloca 0" "$TYPE"
	I=`expr $I + 1`
done

./make_prefix_test.sh "prefix_test_ldind_volatilee_$I" valid "volatile. ldind.ref" "ldloca 0" "object"
./make_prefix_test.sh "prefix_test_ldind_unalignede_$I" valid  "unaligned. 1 ldind.ref" "ldloca 0" "object"


./make_prefix_test.sh "prefix_test_ldfld_volatile" valid "volatile. ldfld int32 MyStruct::foo" "ldloca 0" "MyStruct"
./make_prefix_test.sh "prefix_test_ldfld_unaligned" valid  "unaligned. 1 ldfld int32 MyStruct::foo " "ldloca 0" "MyStruct"


./make_prefix_test.sh "prefix_test_stfld_volatile" valid "volatile. stfld int32 MyStruct::foo" "ldloca 0\n\tldc.i4.0" "MyStruct"
./make_prefix_test.sh "prefix_test_stfld_unaligned" valid  "unaligned. 1 stfld int32 MyStruct::foo" "ldloca 0\n\tldc.i4.0" "MyStruct"

./make_prefix_test.sh "prefix_test_ldobj_volatile" valid "volatile. ldobj MyStruct" "ldloca 0" "MyStruct"
./make_prefix_test.sh "prefix_test_ldobj_unaligned" valid  "unaligned. 1 ldobj MyStruct" "ldloca 0" "MyStruct"

./make_prefix_test.sh "prefix_test_stobj_volatile" valid "volatile. stobj MyStruct" "ldloca 0\n\tldloc.0" "MyStruct"
./make_prefix_test.sh "prefix_test_stobj_unaligned" valid  "unaligned. 1 stobj MyStruct" "ldloca 0\n\tldloc.0" "MyStruct"

./make_prefix_test.sh "prefix_test_cpblk_volatile" unverifiable "volatile. cpblk" "ldloca 0\n\tldloca 0\n\tldc.i4.1" "MyStruct"
./make_prefix_test.sh "prefix_test_cpblk_unaligned" unverifiable  "unaligned. 1 cpblk" "ldloca 0\n\tldloca 0\n\tldc.i4.1" "MyStruct"

./make_prefix_test.sh "prefix_test_initblk_volatile" unverifiable "volatile. initblk" "ldloca 0\n\tldc.i4.0\n\tldc.i4.1" "MyStruct"
./make_prefix_test.sh "prefix_test_initblk_unaligned" unverifiable  "unaligned. 1 initblk" "ldloca 0\n\tldc.i4.0\n\tldc.i4.1" "MyStruct"

./make_prefix_test.sh "prefix_test_stsfld_volatile" valid "volatile. stsfld int32 MyStruct::stFoo" "ldc.i4.0" "MyStruct"
./make_prefix_test.sh "prefix_test_stsfld_unaligned" invalid  "unaligned. 1 stsfld int32 MyStruct::stFoo" "ldc.i4.0" "MyStruct"

./make_prefix_test.sh "prefix_test_ldsfld_volatile" valid "volatile. ldsfld int32 MyStruct::stFoo" "" "MyStruct"
./make_prefix_test.sh "prefix_test_ldsfld_unaligned" invalid  "unaligned. 1 ldsfld int32 MyStruct::stFoo" "" "MyStruct"


I=1
for TYPE in "nop" "new object::.ctor()" "call void MyStruct::Test()"
do
	./make_prefix_test.sh "prefix_test_invalid_op_volatile_$I" invalid  "volatile. $OP" "nop" "int32"
	./make_prefix_test.sh "prefix_test_invalid_op_unaligned_$I" invalid  "unaligned. 1 nop" "nop" "int32"
	I=`expr $I + 1`
done



#prefix tail.
./make_tail_call_test.sh "prefix_test_tail_call" valid "call void MyStruct::Test()" "" "int32"
./make_tail_call_test.sh "prefix_test_tail_callvirt" valid "callvirt instance void ClassA::VirtTest()" "newobj instance void ClassA::.ctor()" "int32"

#MS runtime maks calli as been unverifiable even on a scenario that is verifiable by the spec.
./make_tail_call_test.sh "prefix_test_tail_calli" unverifiable "calli void()" "ldftn void MyStruct::Test()" "int32"

#not followed by a ret
./make_tail_call_test.sh "prefix_test_tail_call_not_followed_by_ret" unverifiable "call void MyStruct::Test()\n\tnop" "" "int32"

#caller return type is different
./make_tail_call_test.sh "prefix_test_tail_call_different_return_type" invalid "call void MyStruct::Test()\n\tnop" "" "int32" "int32"


./make_tail_call_test.sh "prefix_test_tail_call_compatible_return_type" valid "call string MyStruct::StrTest()" "" "string" "object"


#callee receive byref
./make_tail_call_test.sh "prefix_test_tail_call_callee_with_byref_arg" invalid "call void MyStruct::Test(int32\&)\n\tnop" "ldloca 0" "int32"


./make_tail_call_test.sh "prefix_test_tail_call_middle_of_instruction" invalid "call void MyStruct::Test()" "newobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue MIDDLE" "int32"

./make_tail_call_test.sh "prefix_test_tail_with_invalid_instruction" invalid "nop" "" "int32"


#ckfinite

I=1
for TYPE in float32 float64
do
	./make_unary_test.sh ck_finite_test_$I valid "ckfinite" "$TYPE"
	I=`expr $I + 1`
done

I=1
for TYPE in int8 bool int32 int64 "int32&" object
do
	./make_unary_test.sh ck_finite_test_bad_arg_$I invalid "ckfinite" "$TYPE"
	I=`expr $I + 1`
done

./make_unary_test.sh ck_finite_tes_underflow invalid "pop\n\tckfinite" "$TYPE"


#overlapped types
./make_overlapped_test.sh not_overlapped_test valid 0 4 0

./make_overlapped_test.sh obj_overlapped_with_long invalid 0 4 0 int64


for I in 0 1 2 3
do
	./make_overlapped_test.sh bad_overlapped_$I invalid 0 $I 0
done

for I in 1 2 3 5 6 7
do
	./make_overlapped_test.sh bad_overlapped_end_$I invalid 0 $I 4
done

for I in 1 2 3 5 6 7
do
	./make_overlapped_test.sh obj_bad_aligned_$I invalid 0 $I 0
done

#we must be carefull as on 64 bits machines a reference takes 8 bytes.
for I in 13 14 15
do
	./make_overlapped_test.sh int_bad_aligned_$I valid 0 4 $I
done

#Tests for aligned overllaping reference fields.
./make_overlapped_test.sh ref_only_overlapping_1 typeunverifiable 0 0 8 object
./make_overlapped_test.sh ref_only_overlapping_2 invalid 0 1 8 object
./make_overlapped_test.sh ref_only_overlapping_3 invalid 0 2 8 object
./make_overlapped_test.sh ref_only_overlapping_4 typeunverifiable 0 0 8 "int8[]"
./make_overlapped_test.sh ref_only_overlapping_5 invalid 0 0 8 int32

#invalid opcodes
I=166; while [ $I -le 178 ]
do
	./make_bad_op_test.sh bad_op_$I invalid $I
	I=$((I + 1))
done


I=187; while [ $I -le 193 ]
do
	./make_bad_op_test.sh bad_op_$I invalid $I
	I=$((I + 1))
done

I=196; while [ $I -le 207 ]
do
	./make_bad_op_test.sh bad_op_$I invalid $I
	I=$((I + 1))
done

I=225; while [ $I -le 253 ]
do
	./make_bad_op_test.sh bad_op_$I invalid $I
	I=$((I + 1))
done

./make_bad_op_test.sh bad_op_xff invalid 255


I=35; while [ $I -le 255 ]
do
	./make_bad_op_test.sh bad_op_with_prefix_$I invalid 0xFE $I
	I=$((I + 1))
done




#interaction between boxed generic arguments and its constraints
./make_boxed_genarg_test.sh boxed_genarg_cnstr_obj valid "constrained. !0 callvirt instance int32 object::GetHashCode()" "ldloca 0"
./make_boxed_genarg_test.sh boxed_genarg_cnstr_iface valid "constrained. !0 callvirt instance void class IFace::Tst()" "ldloca 0"

./make_boxed_genarg_test.sh boxed_genarg_delegate valid "ldvirtftn instance void class IFace::Tst()\n\tnewobj instance void class TstDelegate::'.ctor'(object, native int)" "ldloc.0\n\tbox !T\n\tdup"

./make_boxed_genarg_test.sh boxed_genarg_stfld_arg valid "stfld IFace class DriverClass<!0>::ifField" "ldarg.0\n\tldloc.0\n\tbox !T"

./make_boxed_genarg_test.sh boxed_genarg_stfld_this_1 valid "stfld int32 class BaseClass::fld" "ldloc.0\n\tbox !T\n\tldc.i4.1" "BaseClass"

./make_boxed_genarg_test.sh boxed_genarg_stfld_this_2 unverifiable "stfld int32 class BaseClass::fld" "ldloc.0\n\tldc.i4.1" "BaseClass"

./make_boxed_genarg_test.sh boxed_genarg_ldfld_1 valid "ldfld int32 class BaseClass::fld" "ldloc.0\n\tbox !T" "BaseClass"

./make_boxed_genarg_test.sh boxed_genarg_ldfld_2 unverifiable "ldfld int32 class BaseClass::fld" "ldloc.0" "BaseClass"


./make_boxed_genarg_test.sh boxed_genarg_initobj_1 unverifiable "initobj IFace" "ldloca 0"
./make_boxed_genarg_test.sh boxed_genarg_initobj_2 unverifiable "initobj !0" "ldloca 1"

./make_boxed_genarg_test.sh boxed_genarg_stobj_1 unverifiable "stobj IFace" "ldloca 0\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stobj_2 unverifiable "stobj !0" "ldloca 0\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stobj_3 valid "stobj IFace" "ldloca 1\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stobj_4 unverifiable "stobj !0" "ldloca 1\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stobj_5 valid "stobj !0" "ldloca 0\n\tldloc.0"

./make_stobj_test.sh stobj_boxing_test_1 valid "int32" "int32\&" "int32" ""
./make_stobj_test.sh stobj_boxing_test_2 unverifiable "int32" "int32\&" "int32" "box int32"
./make_stobj_test.sh stobj_boxing_test_3 valid "object" "object\&" "object" "box object"

./make_boxed_genarg_test.sh boxed_genarg_stind_ref_1 unverifiable "stind.ref" "ldloca 0\n\tldloc.0"
./make_boxed_genarg_test.sh boxed_genarg_stind_ref_2 unverifiable "stind.ref" "ldloca 0\n\tldloc.0\n\tbox !T"

./make_boxed_genarg_test.sh boxed_genarg_stind_ref_3 unverifiable "stind.ref" "ldloca 1\n\tldloc.0"
./make_boxed_genarg_test.sh boxed_genarg_stind_ref_4 valid "stind.ref" "ldloca 1\n\tldloc.0\n\tbox !T"

./make_boxed_genarg_test.sh boxed_genarg_stelem_1 unverifiable "stelem IFace" "ldloc.2\n\tldc.i4.0\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stelem_2 unverifiable "stelem !0" "ldloc.2\n\tldc.i4.0\n\tldloc.0\n\tbox !T"

./make_boxed_genarg_test.sh boxed_genarg_stelem_3 unverifiable "stelem IFace" "ldloc.2\n\tldc.i4.0\n\tldloc.1"
./make_boxed_genarg_test.sh boxed_genarg_stelem_4 unverifiable "stelem !0" "ldloc.2\n\tldc.i4.0\n\tldloc.1"

./make_boxed_genarg_test.sh boxed_genarg_stelem_5 valid "stelem IFace" "ldloc.3\n\tldc.i4.0\n\tldloc.0\n\tbox !T"
./make_boxed_genarg_test.sh boxed_genarg_stelem_6 unverifiable "stelem !0" "ldloc.3\n\tldc.i4.0\n\tldloc.0\n\tbox !T"

./make_boxed_genarg_test.sh boxed_genarg_stelem_7 valid "stelem IFace" "ldloc.3\n\tldc.i4.0\n\tldloc.1"
./make_boxed_genarg_test.sh boxed_genarg_stelem_8 unverifiable "stelem !0" "ldloc.3\n\tldc.i4.0\n\tldloc.1"

./make_boxed_genarg_test.sh boxed_genarg_stelem_9 unverifiable "stelem IFace" "ldloc.2\n\tldc.i4.0\n\tldloc.0"
./make_boxed_genarg_test.sh boxed_genarg_stelem_10 valid "stelem !0" "ldloc.2\n\tldc.i4.0\n\tldloc.0"

./make_boxed_genarg_test.sh boxed_genarg_stelem_11 unverifiable "stelem IFace" "ldloc.3\n\tldc.i4.0\n\tldloc.0"
./make_boxed_genarg_test.sh boxed_genarg_stelem_12 unverifiable "stelem !0" "ldloc.3\n\tldc.i4.0\n\tldloc.0"


./make_boxed_genarg_test.sh boxed_genarg_stack_merge_1 valid "pop\n\tldloc.1" "ldloc.1\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"


./make_boxed_genarg_test.sh boxed_genarg_stack_merge_2 unverifiable "pop\n\tldloc.1" "ldloc.0\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"

./make_boxed_genarg_test.sh boxed_genarg_stack_merge_3 valid "pop\n\tldloc.1" "ldloc.0\n\tbox !T\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"

./make_boxed_genarg_test.sh boxed_genarg_stack_merge_4 unverifiable "pop\n\tldloc.1" "ldloc.0\n\tbox IFace\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"


./make_boxed_genarg_test.sh boxed_genarg_stack_merge_5 unverifiable "pop\n\tldloc.0" "ldloc.1\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"

./make_boxed_genarg_test.sh boxed_genarg_stack_merge_6 valid "pop\n\tldloc.0\n\tbox !T" "ldloc.1\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"

./make_boxed_genarg_test.sh boxed_genarg_stack_merge_7 unverifiable "pop\n\tldloc.0\n\tbox IFace" "ldloc.1\n\tnewobj instance void object::.ctor()\n\tcallvirt instance int32 object::GetHashCode()\n\tbrtrue TARGET"



#test for IL overflow

for I in 0x0E 0x0F 0x10 0x11 0x12 0x13 0x1F 0x2B 0x2C 0x2D 0x2E 0x2F 0x30 0x31 0x32 0x33 0x34 0x35 0x36 0x37 0xDE 0xFE
do
	./make_il_overflow_test.sh incomplete_op_${I} invalid $I
done

for I in 0x20 0x21 0x22 0x23 0x28 0x29 0x38 0x39 0x3A 0x3B 0x3C 0x3D 0x3E 0x3F 0x40 0x41 0x42 0x43 0x44 0x6F 0x70 0x71 0x72 0x73 0x74 0x75 0x79 0x7B 0x7C 0x7D 0x7E 0x7F 0x80 0x81 0x8D 0x8C 0x8F 0xA3 0xA4 0xA5 0xC2 0xC6 0xD0 0xDD
do
	./make_il_overflow_test.sh incomplete_op_${I} invalid $I
	./make_il_overflow_test.sh incomplete_op_${I}_0x00 invalid $I 0x00
	./make_il_overflow_test.sh incomplete_op_${I}_0x00_0x00 invalid $I 0x00 0x00
	./make_il_overflow_test.sh incomplete_op_${I}_0x00_0x00_0x00 invalid $I 0x00 0x00
done


for I in 0x06 0x07 0x09 0x0A 0x0E 0x0B 0x0C 0x0D 0x12 0x15 0x16 0x19 0x1C
do
	./make_il_overflow_test.sh incomplete_op_0xFE_${I} invalid 0xFE $I
	./make_il_overflow_test.sh incomplete_op_0xFE_${I}_0x00 invalid 0xFE $I 0x00
done

#switch
./make_il_overflow_test.sh incomplete_switch_1 invalid 0x45
./make_il_overflow_test.sh incomplete_switch_2 invalid 0x45 0x00
./make_il_overflow_test.sh incomplete_switch_3 invalid 0x45 0x00 0x00
./make_il_overflow_test.sh incomplete_switch_4 invalid 0x45 0x00 0x00

./make_il_overflow_test.sh incomplete_switch_arg_1 invalid 0x45 0x00 0x00 0x00 0x01
./make_il_overflow_test.sh incomplete_switch_arg_2 invalid 0x45 0x00 0x00 0x00 0x01 0x00



#tests for visibility of instantiated generic types and methods
./make_type_visibility_test.sh type_vis_gist_1 valid "newobj instance void class Foo<[test_lib]ClassC>::.ctor()"
./make_type_visibility_test.sh type_vis_gist_2 unverifiable "newobj instance void class Foo<[test_lib]NotExportedA>::.ctor()"


./make_type_visibility_test.sh type_vis_cast_1 valid "castclass class [test_lib]ClassC"
./make_type_visibility_test.sh type_vis_cast_2 valid "castclass class [test_lib]NotExportedA"
./make_type_visibility_test.sh type_vis_cast_gist_1 valid "castclass class Foo<[test_lib]ClassC>"
./make_type_visibility_test.sh type_vis_cast_gist_2 valid "castclass class Foo<[test_lib]NotExportedA>"


./make_type_visibility_test.sh type_vis_sizeof_1 valid "sizeof class [test_lib]ClassC"
./make_type_visibility_test.sh type_vis_sizeof_2 valid "sizeof class [test_lib]NotExportedA"
./make_type_visibility_test.sh type_vis_sizeof_gist_1 valid "sizeof class Foo<[test_lib]ClassC>"
./make_type_visibility_test.sh type_vis_sizeof_gist_2 valid "sizeof class Foo<[test_lib]NotExportedA>"


./make_type_visibility_test.sh type_vis_newarr_1 valid "newarr class [test_lib]ClassC" "ldc.i4.1"
./make_type_visibility_test.sh type_vis_newarr_2 valid "newarr class [test_lib]NotExportedA" "ldc.i4.1"
./make_type_visibility_test.sh type_vis_newarr_gist_1 valid "newarr class Foo<[test_lib]ClassC>" "ldc.i4.1"
./make_type_visibility_test.sh type_vis_newarr_gist_2 valid "newarr class Foo<[test_lib]NotExportedA>" "ldc.i4.1"


./make_type_visibility_test.sh type_vis_ldelem_1 valid "ldelem class [test_lib]ClassC" "ldc.i4.1\n\tnewarr class [test_lib]ClassC\n\tldc.i4.0"
./make_type_visibility_test.sh type_vis_ldelem_2 valid "ldelem class [test_lib]NotExportedA" "ldc.i4.1\n\tnewarr class [test_lib]NotExportedA\n\tldc.i4.0"
./make_type_visibility_test.sh type_vis_ldelem_gist_1 valid "ldelem class Foo<[test_lib]ClassC>" "ldc.i4.1\n\tnewarr class Foo<[test_lib]ClassC>\n\tldc.i4.0"
./make_type_visibility_test.sh type_vis_ldelem_gist_2 valid "ldelem class Foo<[test_lib]NotExportedA>" "ldc.i4.1\n\tnewarr class Foo<[test_lib]NotExportedA>\n\tldc.i4.0"


./make_type_visibility_test.sh type_vis_stelem_1 valid "stelem class [test_lib]ClassC" "ldc.i4.1\n\tnewarr class [test_lib]ClassC\n\tldc.i4.0\n\tldnull"
./make_type_visibility_test.sh type_vis_stelem_2 valid "stelem class [test_lib]NotExportedA" "ldc.i4.1\n\tnewarr class [test_lib]NotExportedA\n\tldc.i4.0\n\tldnull"
./make_type_visibility_test.sh type_vis_stelem_gist_1 valid "stelem class Foo<[test_lib]ClassC>" "ldc.i4.1\n\tnewarr class Foo<[test_lib]ClassC>\n\tldc.i4.0\n\tldnull"
./make_type_visibility_test.sh type_vis_stelem_gist_2 valid "stelem class Foo<[test_lib]NotExportedA>" "ldc.i4.1\n\tnewarr class Foo<[test_lib]NotExportedA>\n\tldc.i4.0\n\tldnull"


./make_type_visibility_test.sh type_vis_box_1 valid "box valuetype [test_lib]PublicStruct" "ldloc.1" "valuetype [test_lib]PublicStruct"
./make_type_visibility_test.sh type_vis_box_2 valid "box valuetype [test_lib]NBStruct" "ldloc.1" "valuetype [test_lib]NBStruct"


#generic method

./make_type_visibility_test.sh type_vis_gmethod_1 valid "call void SimpleClass::Generic<[test_lib]ClassC>()"
./make_type_visibility_test.sh type_vis_gmethod_2 unverifiable "call void SimpleClass::Generic<[test_lib]NotExportedA>()"


#Constructor tests

./make_ctor_test.sh ctor_good_ops_1 valid "call instance void object::'.ctor'()"
./make_ctor_test.sh ctor_good_ops_2 valid "nop\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_good_ops_3 valid "dup\n\tldc.i4.0\n\tstfld int32 Test::val\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_good_ops_4 valid "dup\n\tldfld int32 Test::val\n\tpop\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_good_ops_5 valid "dup\n\tpop\n\tcall instance void object::'.ctor'()"

./make_ctor_test.sh ctor_call_super_2x valid "call instance void object::'.ctor'()\n\tldarg.0\n\tcall instance void object::'.ctor'()"

./make_ctor_test.sh ctor_pass_this_as_arg_1 unverifiable "ldarg.0\n\tcall instance void TestClass::'.ctor'(object)" "other"
./make_ctor_test.sh ctor_pass_this_as_arg_2 unverifiable "dup\n\tcall instance void TestClass::'.ctor'(object)" "other"


./make_ctor_test.sh ctor_no_super_call unverifiable "nop"
./make_ctor_test.sh ctor_call_invalid_super unverifiable "call instance void TestClass::'.ctor'()"
./make_call_test.sh ctor_call_outside_ctor unverifiable "call instance void ClassA::.ctor()" "newobj instance void ClassA::.ctor()"

./make_ctor_test.sh ctor_use_non_this_ptr unverifiable "ldloc.0\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_store_this_on_field unverifiable "dup\n\tdup\n\tstfld object Test::obj\n\tcall instance void object::'.ctor'()"


./make_ctor_test.sh ctor_use_uninit_this_1 unverifiable "dup\n\tcall void Test::StaticMethod(object)\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_use_uninit_this_2 unverifiable "dup\n\tcall instance void Test::InstanceMethod()\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_use_uninit_this_3 unverifiable "dup\n\tcastclass [mscorlib]System.String\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_use_uninit_this_4 unverifiable "dup\n\tunbox.any Test\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_use_uninit_this_5 unverifiable "dup\n\tcall instance void object::'.ctor'()\n\tcall instance void Test::InstanceMethod()"

./make_ctor_test.sh ctor_bad_ops_1 unverifiable "dup\n\tstloc.0\n\tcall instance void object::'.ctor'()"
./make_ctor_test.sh ctor_bad_ops_2 unverifiable "dup\n\tcall instance void object::'.ctor'()\n\tcall instance void Test::InstanceMethod()"

#TODO try / catch inside constructor


#TODO methods cannot have variance, but this should be checked at load time
#TODO check for variance compatibility between types

#generic delegate validation

./make_generic_argument_constraints_test.sh no_constraints valid "" ""

I=1
for SRC in "(IfaceA)" "(IfaceB)" "(IfaceA, IfaceB)" ".ctor" "class"
do
	./make_generic_argument_constraints_test.sh src_ctrs_only_${I} unverifiable "$SRC" ""
	I=`expr $I + 1`
done

./make_generic_argument_constraints_test.sh src_ctrs_only_vt unverifiable "valuetype" "" "int32"


#Type arg compatibility
./make_generic_argument_constraints_test.sh type_ctrs_1 valid "(IfaceA)" "(IfaceA)"
./make_generic_argument_constraints_test.sh type_ctrs_2 valid "(IfaceA)" "(IfaceB, IfaceA)"
./make_generic_argument_constraints_test.sh type_ctrs_3 unverifiable "(IfaceA)" "(IfaceB)"
./make_generic_argument_constraints_test.sh type_ctrs_4 unverifiable "(IfaceA, IfaceB)" "(IfaceA)"

#DefaultArgument implements IfaceA
./make_generic_argument_constraints_test.sh type_ctrs_5 valid "(IfaceA)" "(DefaultArgument)"


./make_generic_argument_constraints_test.sh type_ctor_1 valid ".ctor" ".ctor"

./make_generic_argument_constraints_test.sh type_class_1 valid "class" "class"

./make_generic_argument_constraints_test.sh type_valuetype_1 valid "valuetype" "valuetype" "int32"

./make_generic_argument_constraints_test.sh type_mixed_1 valid "class (IfaceA)" "class (IfaceA)"
./make_generic_argument_constraints_test.sh type_mixed_2 valid "(IfaceA)" "class (IfaceA)"

./make_generic_argument_constraints_test.sh type_mixed_3 valid "" "(IfaceA)"
./make_generic_argument_constraints_test.sh type_mixed_4 valid "" "class (IfaceA)"
