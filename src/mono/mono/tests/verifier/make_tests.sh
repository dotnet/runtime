#! /bin/sh

# Stack Size Tests
for OP in 'starg.s 0' 'stloc.0' 'stloc.s 0' 'stfld int32 Class::fld' pop ret
do
  ./make_stack_0_test.sh "$OP"
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
  ./make_bin_test.sh bin_num_op_1_${I} invalid $OP int32 int64
  ./make_bin_test.sh bin_num_op_2_${I} invalid $OP int32 float64
  ./make_bin_test.sh bin_num_op_3_${I} invalid $OP int32 object

  ./make_bin_test.sh bin_num_op_4_${I} invalid $OP int64 int32
  ./make_bin_test.sh bin_num_op_5_${I} invalid $OP int64 'native int'
  ./make_bin_test.sh bin_num_op_6_${I} invalid $OP int64 float64
  ./make_bin_test.sh bin_num_op_7_${I} invalid $OP int64 'int64&'
  ./make_bin_test.sh bin_num_op_8_${I} invalid $OP int64 object

  ./make_bin_test.sh bin_num_op_9_${I} invalid $OP 'native int' int64
  ./make_bin_test.sh bin_num_op_10_${I} invalid $OP 'native int' float64
  ./make_bin_test.sh bin_num_op_11_${I} invalid $OP 'native int' object

  ./make_bin_test.sh bin_num_op_12_${I} invalid $OP float64 int32
  ./make_bin_test.sh bin_num_op_13_${I} invalid $OP float64 int64
  ./make_bin_test.sh bin_num_op_14_${I} invalid $OP float64 'native int'
  ./make_bin_test.sh bin_num_op_15_${I} invalid $OP float64 'float64&'
  ./make_bin_test.sh bin_num_op_16_${I} invalid $OP float64 object

  ./make_bin_test.sh bin_num_op_17_${I} invalid $OP 'int64&' int64
  ./make_bin_test.sh bin_num_op_18_${I} invalid $OP 'float64&' float64
  ./make_bin_test.sh bin_num_op_19_${I} invalid $OP 'object&' object

  ./make_bin_test.sh bin_num_op_20_${I} invalid $OP object int32
  ./make_bin_test.sh bin_num_op_21_${I} invalid $OP object int64
  ./make_bin_test.sh bin_num_op_22_${I} invalid $OP object 'native int'
  ./make_bin_test.sh bin_num_op_23_${I} invalid $OP object float64
  ./make_bin_test.sh bin_num_op_24_${I} invalid $OP object 'object&'
  ./make_bin_test.sh bin_num_op_25_${I} invalid $OP object object
  I=`expr $I + 1`
done

I=1
for OP in div mul rem sub
do
  ./make_bin_test.sh bin_num_op_26_${I} invalid $OP int32 'int32&'
  ./make_bin_test.sh bin_num_op_27_${I} invalid $OP 'native int' 'native int&'
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
  ./make_bin_test.sh bin_num_op_28_${I} invalid $OP 'int32&' int32
  ./make_bin_test.sh bin_num_op_29_${I} invalid $OP 'native int&' 'native int'
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
  ./make_bin_test.sh bin_num_op_30_${I} invalid $OP 'int32&' 'int32&'
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
  ./make_bin_test.sh bin_comp_op_1_${I} invalid $OP int32 int64
  ./make_bin_test.sh bin_comp_op_2_${I} invalid $OP int32 float64
  ./make_bin_test.sh bin_comp_op_3_${I} invalid $OP int32 'int32&'
  ./make_bin_test.sh bin_comp_op_4_${I} invalid $OP int32 object

  ./make_bin_test.sh bin_comp_op_5_${I} invalid $OP int64 int32
  ./make_bin_test.sh bin_comp_op_6_${I} invalid $OP int64 'native int'
  ./make_bin_test.sh bin_comp_op_7_${I} invalid $OP int64 float64
  ./make_bin_test.sh bin_comp_op_8_${I} invalid $OP int64 'int64&'
  ./make_bin_test.sh bin_comp_op_9_${I} invalid $OP int64 object

  ./make_bin_test.sh bin_comp_op_10_${I} invalid $OP 'native int' int64
  ./make_bin_test.sh bin_comp_op_11_${I} invalid $OP 'native int' float64
  ./make_bin_test.sh bin_comp_op_12_${I} invalid $OP 'native int' object

  ./make_bin_test.sh bin_comp_op_13_${I} invalid $OP float64 int32
  ./make_bin_test.sh bin_comp_op_14_${I} invalid $OP float64 int64
  ./make_bin_test.sh bin_comp_op_15_${I} invalid $OP float64 'native int'
  ./make_bin_test.sh bin_comp_op_16_${I} invalid $OP float64 'float64&'
  ./make_bin_test.sh bin_comp_op_17_${I} invalid $OP float64 object

  ./make_bin_test.sh bin_comp_op_18_${I} invalid $OP 'int32&' int32
  ./make_bin_test.sh bin_comp_op_19_${I} invalid $OP 'int64&' int64
  ./make_bin_test.sh bin_comp_op_20_${I} invalid $OP 'float64&' float64
  ./make_bin_test.sh bin_comp_op_21_${I} invalid $OP 'object&' object

  ./make_bin_test.sh bin_comp_op_22_${I} invalid $OP object int32
  ./make_bin_test.sh bin_comp_op_23_${I} invalid $OP object int64
  ./make_bin_test.sh bin_comp_op_24_${I} invalid $OP object 'native int'
  ./make_bin_test.sh bin_comp_op_25_${I} invalid $OP object float64
  ./make_bin_test.sh bin_comp_op_26_${I} invalid $OP object 'object&'
  I=`expr $I + 1`
done

I=1
for OP in cgt clt
do
  ./make_bin_test.sh bin_comp_op_27_${I} invalid $OP 'native int' 'native int&'
  ./make_bin_test.sh bin_comp_op_28_${I} invalid $OP 'native int&' 'native int'
  ./make_bin_test.sh bin_comp_op_29_${I} invalid $OP object object
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
  ./make_bin_test.sh bin_int_op_1_${I} invalid "$OP" int32 int64
  ./make_bin_test.sh bin_int_op_2_${I} invalid "$OP" int32 float64
  ./make_bin_test.sh bin_int_op_3_${I} invalid "$OP" int32 'int32&'
  ./make_bin_test.sh bin_int_op_4_${I} invalid "$OP" int32 object

  ./make_bin_test.sh bin_int_op_5_${I} invalid "$OP" int64 int32
  ./make_bin_test.sh bin_int_op_6_${I} invalid "$OP" int64 'native int'
  ./make_bin_test.sh bin_int_op_7_${I} invalid "$OP" int64 float64
  ./make_bin_test.sh bin_int_op_8_${I} invalid "$OP" int64 'int64&'
  ./make_bin_test.sh bin_int_op_9_${I} invalid "$OP" int64 object

  ./make_bin_test.sh bin_int_op_10_${I} invalid "$OP" 'native int' int64
  ./make_bin_test.sh bin_int_op_11_${I} invalid "$OP" 'native int' float64
  ./make_bin_test.sh bin_int_op_12_${I} invalid "$OP" 'native int' 'native int&'
  ./make_bin_test.sh bin_int_op_13_${I} invalid "$OP" 'native int' object

  ./make_bin_test.sh bin_int_op_14_${I} invalid "$OP" float64 int32
  ./make_bin_test.sh bin_int_op_15_${I} invalid "$OP" float64 int64
  ./make_bin_test.sh bin_int_op_16_${I} invalid "$OP" float64 'native int'
  ./make_bin_test.sh bin_int_op_17_${I} invalid "$OP" float64 float64
  ./make_bin_test.sh bin_int_op_18_${I} invalid "$OP" float64 'int32&'
  ./make_bin_test.sh bin_int_op_19_${I} invalid "$OP" float64 object

  ./make_bin_test.sh bin_int_op_20_${I} invalid "$OP" 'int32&' int32
  ./make_bin_test.sh bin_int_op_21_${I} invalid "$OP" 'int64&' int64
  ./make_bin_test.sh bin_int_op_22_${I} invalid "$OP" 'native int&' 'native int'
  ./make_bin_test.sh bin_int_op_23_${I} invalid "$OP" 'float64&' float64
  ./make_bin_test.sh bin_int_op_24_${I} invalid "$OP" 'int32&' 'int32&'
  ./make_bin_test.sh bin_int_op_25_${I} invalid "$OP" 'float64&' object

  ./make_bin_test.sh bin_int_op_26_${I} invalid "$OP" object int32
  ./make_bin_test.sh bin_int_op_27_${I} invalid "$OP" object int64
  ./make_bin_test.sh bin_int_op_28_${I} invalid "$OP" object 'native int'
  ./make_bin_test.sh bin_int_op_29_${I} invalid "$OP" object float64
  ./make_bin_test.sh bin_int_op_30_${I} invalid "$OP" object 'int32&'
  ./make_bin_test.sh bin_int_op_31_${I} invalid "$OP" object object
  I=`expr $I + 1`
done

for OP in "not\n\tpop"
do
  ./make_unary_test.sh not_1 invalid "$OP" float64
  ./make_unary_test.sh not_2 invalid "$OP" 'int32&'
  ./make_unary_test.sh not_3 invalid "$OP" object
done

# Table 6: Shift Operators
I=1
for OP in shl shr
do
  ./make_bin_test.sh shift_op_1_${I} invalid $OP int32 int64
  ./make_bin_test.sh shift_op_2_${I} invalid $OP int32 float64
  ./make_bin_test.sh shift_op_3_${I} invalid $OP int32 'int32&'
  ./make_bin_test.sh shift_op_4_${I} invalid $OP int32 object

  ./make_bin_test.sh shift_op_5_${I} invalid $OP int64 int64
  ./make_bin_test.sh shift_op_6_${I} invalid $OP int64 float64
  ./make_bin_test.sh shift_op_7_${I} invalid $OP int64 'int32&'
  ./make_bin_test.sh shift_op_8_${I} invalid $OP int64 object

  ./make_bin_test.sh shift_op_9_${I} invalid $OP 'native int' int64
  ./make_bin_test.sh shift_op_10_${I} invalid $OP 'native int' float64
  ./make_bin_test.sh shift_op_11_${I} invalid $OP 'native int' 'native int&'
  ./make_bin_test.sh shift_op_12_${I} invalid $OP 'native int' object

  ./make_bin_test.sh shift_op_13_${I} invalid $OP float64 int32
  ./make_bin_test.sh shift_op_14_${I} invalid $OP float64 int64
  ./make_bin_test.sh shift_op_15_${I} invalid $OP float64 'native int'
  ./make_bin_test.sh shift_op_16_${I} invalid $OP float64 float64
  ./make_bin_test.sh shift_op_17_${I} invalid $OP float64 'int32&'
  ./make_bin_test.sh shift_op_18_${I} invalid $OP float64 object

  ./make_bin_test.sh shift_op_19_${I} invalid $OP 'int32&' int32
  ./make_bin_test.sh shift_op_20_${I} invalid $OP 'int64&' int64
  ./make_bin_test.sh shift_op_21_${I} invalid $OP 'native int&' 'native int'
  ./make_bin_test.sh shift_op_22_${I} invalid $OP 'float64&' float64
  ./make_bin_test.sh shift_op_23_${I} invalid $OP 'int32&' 'int32&'
  ./make_bin_test.sh shift_op_24_${I} invalid $OP 'float64&' object

  ./make_bin_test.sh shift_op_25_${I} invalid $OP object int32
  ./make_bin_test.sh shift_op_26_${I} invalid $OP object int64
  ./make_bin_test.sh shift_op_27_${I} invalid $OP object 'native int'
  ./make_bin_test.sh shift_op_28_${I} invalid $OP object float64
  ./make_bin_test.sh shift_op_29_${I} invalid $OP object 'int32&'
  ./make_bin_test.sh shift_op_30_${I} invalid $OP object object
  I=`expr $I + 1`
done

# Table 8: Conversion Operations
I=1
for OP in "conv.i4\n\tpop" "conv.r8\n\tpop"
do
  ./make_unary_test.sh conv_op_1_${I} invalid $OP 'int32&'
  ./make_unary_test.sh conv_op_2_${I} invalid $OP object
  I=`expr $I + 1`
done

# 1.6 Implicit argument coercion (Table 9: Signature Matching)
I=1
for OP in stloc.0 "stloc.s 0" "starg.s 0"
do
  ./make_store_test.sh coercion_1_${I} invalid "$OP" int8 int64
  ./make_store_test.sh coercion_2_${I} invalid "$OP" int8 float64
  ./make_store_test.sh coercion_3_${I} invalid "$OP" int8 'int8&'
  ./make_store_test.sh coercion_4_${I} invalid "$OP" int8 object

  ./make_store_test.sh coercion_5_${I} invalid "$OP" 'unsigned int8' int64
  ./make_store_test.sh coercion_6_${I} invalid "$OP" 'unsigned int8' float64
  ./make_store_test.sh coercion_7_${I} invalid "$OP" 'unsigned int8' 'unsigned int8&'
  ./make_store_test.sh coercion_8_${I} invalid "$OP" 'unsigned int8' object

  ./make_store_test.sh coercion_9_${I} invalid "$OP" bool int64
  ./make_store_test.sh coercion_10_${I} invalid "$OP" bool float64
  ./make_store_test.sh coercion_11_${I} invalid "$OP" bool 'bool&'
  ./make_store_test.sh coercion_12_${I} invalid "$OP" bool object

  ./make_store_test.sh coercion_13_${I} invalid "$OP" int16 int64
  ./make_store_test.sh coercion_14_${I} invalid "$OP" int16 float64
  ./make_store_test.sh coercion_15_${I} invalid "$OP" int16 'int16&'
  ./make_store_test.sh coercion_16_${I} invalid "$OP" int16 object
  
  ./make_store_test.sh coercion_17_${I} invalid "$OP" 'unsigned int16' int64
  ./make_store_test.sh coercion_18_${I} invalid "$OP" 'unsigned int16' float64
  ./make_store_test.sh coercion_19_${I} invalid "$OP" 'unsigned int16' 'unsigned int16&'
  ./make_store_test.sh coercion_20_${I} invalid "$OP" 'unsigned int16' object
  
  ./make_store_test.sh coercion_21_${I} invalid "$OP" char int64
  ./make_store_test.sh coercion_22_${I} invalid "$OP" char float64
  ./make_store_test.sh coercion_23_${I} invalid "$OP" char 'char&'
  ./make_store_test.sh coercion_24_${I} invalid "$OP" char object
  
  ./make_store_test.sh coercion_25_${I} invalid "$OP" int32 int64
  ./make_store_test.sh coercion_26_${I} invalid "$OP" int32 float64
  ./make_store_test.sh coercion_27_${I} invalid "$OP" int32 'int32&'
  ./make_store_test.sh coercion_28_${I} invalid "$OP" int32 object
  
  ./make_store_test.sh coercion_29_${I} invalid "$OP" 'unsigned int32' int64
  ./make_store_test.sh coercion_30_${I} invalid "$OP" 'unsigned int32' float64
  ./make_store_test.sh coercion_31_${I} invalid "$OP" 'unsigned int32' 'unsigned int32&'
  ./make_store_test.sh coercion_32_${I} invalid "$OP" 'unsigned int32' object
 
  ./make_store_test.sh coercion_33_${I} invalid "$OP" int64 int32
  ./make_store_test.sh coercion_34_${I} invalid "$OP" int64 'native int'
  ./make_store_test.sh coercion_35_${I} invalid "$OP" int64 float64
  ./make_store_test.sh coercion_36_${I} invalid "$OP" int64 'int64&'
  ./make_store_test.sh coercion_37_${I} invalid "$OP" int64 object
  
  ./make_store_test.sh coercion_38_${I} invalid "$OP" 'unsigned int64' int32
  ./make_store_test.sh coercion_39_${I} invalid "$OP" 'unsigned int64' 'native int'
  ./make_store_test.sh coercion_40_${I} invalid "$OP" 'unsigned int64' float64
  ./make_store_test.sh coercion_41_${I} invalid "$OP" 'unsigned int64' 'unsigned int64&'
  ./make_store_test.sh coercion_42_${I} invalid "$OP" 'unsigned int64' object
  
  ./make_store_test.sh coercion_43_${I} invalid "$OP" 'native int' int64
  ./make_store_test.sh coercion_44_${I} invalid "$OP" 'native int' float64
  ./make_store_test.sh coercion_45_${I} invalid "$OP" 'native int' 'native int&'
  ./make_store_test.sh coercion_46_${I} invalid "$OP" 'native int' object
  
  ./make_store_test.sh coercion_47_${I} invalid "$OP" 'native unsigned int' int64
  ./make_store_test.sh coercion_48_${I} invalid "$OP" 'native unsigned int' float64
  ./make_store_test.sh coercion_49_${I} invalid "$OP" 'native unsigned int' 'native unsigned int&'
  ./make_store_test.sh coercion_50_${I} invalid "$OP" 'native unsigned int' object
  
  ./make_store_test.sh coercion_51_${I} invalid "$OP" float32 int32
  ./make_store_test.sh coercion_52_${I} invalid "$OP" float32 'native int'
  ./make_store_test.sh coercion_53_${I} invalid "$OP" float32 int64
  ./make_store_test.sh coercion_54_${I} invalid "$OP" float32 'float32&'
  ./make_store_test.sh coercion_55_${I} invalid "$OP" float32 object
  
  ./make_store_test.sh coercion_56_${I} invalid "$OP" float64 int32
  ./make_store_test.sh coercion_57_${I} invalid "$OP" float64 'native int'
  ./make_store_test.sh coercion_58_${I} invalid "$OP" float64 int64
  ./make_store_test.sh coercion_59_${I} invalid "$OP" float64 'float64&'
  ./make_store_test.sh coercion_60_${I} invalid "$OP" float64 object

  ./make_store_test.sh coercion_61_${I} invalid "$OP" object int32
  ./make_store_test.sh coercion_62_${I} invalid "$OP" object 'native int'
  ./make_store_test.sh coercion_63_${I} invalid "$OP" object int64
  ./make_store_test.sh coercion_64_${I} invalid "$OP" object float64
  ./make_store_test.sh coercion_65_${I} invalid "$OP" object 'object&'
  
  ./make_store_test.sh coercion_66_${I} invalid "$OP" 'class ValueType' int32
  ./make_store_test.sh coercion_67_${I} invalid "$OP" 'class ValueType' 'native int'
  ./make_store_test.sh coercion_68_${I} invalid "$OP" 'class ValueType' int64
  ./make_store_test.sh coercion_69_${I} invalid "$OP" 'class ValueType' float64
  ./make_store_test.sh coercion_70_${I} invalid "$OP" 'class ValueType' 'class ValueType&'
  ./make_store_test.sh coercion_71_${I} invalid "$OP" 'class ValueType' object
  
  ./make_store_test.sh coercion_72_${I} invalid "$OP" 'int32&' int32
  ./make_store_test.sh coercion_73_${I} unverifiable "$OP" 'int32&' 'native int'
  ./make_store_test.sh coercion_74_${I} invalid "$OP" 'int32&' int64
  ./make_store_test.sh coercion_75_${I} invalid "$OP" 'int32&' float64
  ./make_store_test.sh coercion_76_${I} invalid "$OP" 'int32&' object
  
  ./make_store_test.sh coercion_77_${I} invalid "$OP" typedref int32
  ./make_store_test.sh coercion_78_${I} invalid "$OP" typedref 'native int'
  ./make_store_test.sh coercion_89_${I} invalid "$OP" typedref int64
  ./make_store_test.sh coercion_80_${I} invalid "$OP" typedref float64
  ./make_store_test.sh coercion_81_${I} invalid "$OP" typedref 'typedref&'
  ./make_store_test.sh coercion_82_${I} invalid "$OP" typedref object
  I=`expr $I + 1`
done

I=1
for OP in "stfld TYPE1 Class::fld" "call void Class::Method(TYPE1)"
do
  ./make_obj_store_test.sh obj_coercion_1_${I} invalid "$OP" int8 int64
  ./make_obj_store_test.sh obj_coercion_2_${I} invalid "$OP" int8 float64
  ./make_obj_store_test.sh obj_coercion_3_${I} invalid "$OP" int8 'int8&'
  ./make_obj_store_test.sh obj_coercion_4_${I} invalid "$OP" int8 object

  ./make_obj_store_test.sh obj_coercion_5_${I} invalid "$OP" 'unsigned int8' int64
  ./make_obj_store_test.sh obj_coercion_6_${I} invalid "$OP" 'unsigned int8' float64
  ./make_obj_store_test.sh obj_coercion_7_${I} invalid "$OP" 'unsigned int8' 'unsigned int8&'
  ./make_obj_store_test.sh obj_coercion_8_${I} invalid "$OP" 'unsigned int8' object

  ./make_obj_store_test.sh obj_coercion_9_${I} invalid "$OP" bool int64
  ./make_obj_store_test.sh obj_coercion_10_${I} invalid "$OP" bool float64
  ./make_obj_store_test.sh obj_coercion_11_${I} invalid "$OP" bool 'bool&'
  ./make_obj_store_test.sh obj_coercion_12_${I} invalid "$OP" bool object

  ./make_obj_store_test.sh obj_coercion_13_${I} invalid "$OP" int16 int64
  ./make_obj_store_test.sh obj_coercion_14_${I} invalid "$OP" int16 float64
  ./make_obj_store_test.sh obj_coercion_15_${I} invalid "$OP" int16 'int16&'
  ./make_obj_store_test.sh obj_coercion_16_${I} invalid "$OP" int16 object
  
  ./make_obj_store_test.sh obj_coercion_17_${I} invalid "$OP" 'unsigned int16' int64
  ./make_obj_store_test.sh obj_coercion_18_${I} invalid "$OP" 'unsigned int16' float64
  ./make_obj_store_test.sh obj_coercion_19_${I} invalid "$OP" 'unsigned int16' 'unsigned int16&'
  ./make_obj_store_test.sh obj_coercion_20_${I} invalid "$OP" 'unsigned int16' object
  
  ./make_obj_store_test.sh obj_coercion_21_${I} invalid "$OP" char int64
  ./make_obj_store_test.sh obj_coercion_22_${I} invalid "$OP" char float64
  ./make_obj_store_test.sh obj_coercion_23_${I} invalid "$OP" char 'char&'
  ./make_obj_store_test.sh obj_coercion_24_${I} invalid "$OP" char object
  
  ./make_obj_store_test.sh obj_coercion_25_${I} invalid "$OP" int32 int64
  ./make_obj_store_test.sh obj_coercion_26_${I} invalid "$OP" int32 float64
  ./make_obj_store_test.sh obj_coercion_27_${I} invalid "$OP" int32 'int32&'
  ./make_obj_store_test.sh obj_coercion_28_${I} invalid "$OP" int32 object
  
  ./make_obj_store_test.sh obj_coercion_29_${I} invalid "$OP" 'unsigned int32' int64
  ./make_obj_store_test.sh obj_coercion_30_${I} invalid "$OP" 'unsigned int32' float64
  ./make_obj_store_test.sh obj_coercion_31_${I} invalid "$OP" 'unsigned int32' 'unsigned int32&'
  ./make_obj_store_test.sh obj_coercion_32_${I} invalid "$OP" 'unsigned int32' object
 
  ./make_obj_store_test.sh obj_coercion_33_${I} invalid "$OP" int64 int32
  ./make_obj_store_test.sh obj_coercion_34_${I} invalid "$OP" int64 'native int'
  ./make_obj_store_test.sh obj_coercion_35_${I} invalid "$OP" int64 float64
  ./make_obj_store_test.sh obj_coercion_36_${I} invalid "$OP" int64 'int64&'
  ./make_obj_store_test.sh obj_coercion_37_${I} invalid "$OP" int64 object
  
  ./make_obj_store_test.sh obj_coercion_38_${I} invalid "$OP" 'unsigned int64' int32
  ./make_obj_store_test.sh obj_coercion_39_${I} invalid "$OP" 'unsigned int64' 'native int'
  ./make_obj_store_test.sh obj_coercion_40_${I} invalid "$OP" 'unsigned int64' float64
  ./make_obj_store_test.sh obj_coercion_41_${I} invalid "$OP" 'unsigned int64' 'unsigned int64&'
  ./make_obj_store_test.sh obj_coercion_42_${I} invalid "$OP" 'unsigned int64' object
  
  ./make_obj_store_test.sh obj_coercion_43_${I} invalid "$OP" 'native int' int64
  ./make_obj_store_test.sh obj_coercion_44_${I} invalid "$OP" 'native int' float64
  ./make_obj_store_test.sh obj_coercion_45_${I} invalid "$OP" 'native int' 'native int&'
  ./make_obj_store_test.sh obj_coercion_46_${I} invalid "$OP" 'native int' object
  
  ./make_obj_store_test.sh obj_coercion_47_${I} invalid "$OP" 'native unsigned int' int64
  ./make_obj_store_test.sh obj_coercion_48_${I} invalid "$OP" 'native unsigned int' float64
  ./make_obj_store_test.sh obj_coercion_49_${I} invalid "$OP" 'native unsigned int' 'native unsigned int&'
  ./make_obj_store_test.sh obj_coercion_50_${I} invalid "$OP" 'native unsigned int' object
  
  ./make_obj_store_test.sh obj_coercion_51_${I} invalid "$OP" float32 int32
  ./make_obj_store_test.sh obj_coercion_52_${I} invalid "$OP" float32 'native int'
  ./make_obj_store_test.sh obj_coercion_53_${I} invalid "$OP" float32 int64
  ./make_obj_store_test.sh obj_coercion_54_${I} invalid "$OP" float32 'float32&'
  ./make_obj_store_test.sh obj_coercion_55_${I} invalid "$OP" float32 object
  
  ./make_obj_store_test.sh obj_coercion_56_${I} invalid "$OP" float64 int32
  ./make_obj_store_test.sh obj_coercion_57_${I} invalid "$OP" float64 'native int'
  ./make_obj_store_test.sh obj_coercion_58_${I} invalid "$OP" float64 int64
  ./make_obj_store_test.sh obj_coercion_59_${I} invalid "$OP" float64 'float64&'
  ./make_obj_store_test.sh obj_coercion_60_${I} invalid "$OP" float64 object

  ./make_obj_store_test.sh obj_coercion_61_${I} invalid "$OP" object int32
  ./make_obj_store_test.sh obj_coercion_62_${I} invalid "$OP" object 'native int'
  ./make_obj_store_test.sh obj_coercion_63_${I} invalid "$OP" object int64
  ./make_obj_store_test.sh obj_coercion_64_${I} invalid "$OP" object float64
  ./make_obj_store_test.sh obj_coercion_65_${I} invalid "$OP" object 'object&'
  
  ./make_obj_store_test.sh obj_coercion_66_${I} invalid "$OP" 'class ValueType' int32
  ./make_obj_store_test.sh obj_coercion_67_${I} invalid "$OP" 'class ValueType' 'native int'
  ./make_obj_store_test.sh obj_coercion_68_${I} invalid "$OP" 'class ValueType' int64
  ./make_obj_store_test.sh obj_coercion_69_${I} invalid "$OP" 'class ValueType' float64
  ./make_obj_store_test.sh obj_coercion_70_${I} invalid "$OP" 'class ValueType' 'class ValueType&'
  ./make_obj_store_test.sh obj_coercion_71_${I} invalid "$OP" 'class ValueType' object
  
  ./make_obj_store_test.sh obj_coercion_72_${I} invalid "$OP" 'int32&' int32
  ./make_obj_store_test.sh obj_coercion_73_${I} unverifiable "$OP" 'int32&' 'native int'
  ./make_obj_store_test.sh obj_coercion_74_${I} invalid "$OP" 'int32&' int64
  ./make_obj_store_test.sh obj_coercion_75_${I} invalid "$OP" 'int32&' float64
  ./make_obj_store_test.sh obj_coercion_76_${I} invalid "$OP" 'int32&' object
  
  ./make_obj_store_test.sh obj_coercion_77_${I} invalid "$OP" typedref int32
  ./make_obj_store_test.sh obj_coercion_78_${I} invalid "$OP" typedref 'native int'
  ./make_obj_store_test.sh obj_coercion_79_${I} invalid "$OP" typedref int64
  ./make_obj_store_test.sh obj_coercion_80_${I} invalid "$OP" typedref float64
  ./make_obj_store_test.sh obj_coercion_81_${I} invalid "$OP" typedref 'typedref&'
  ./make_obj_store_test.sh obj_coercion_82_${I} invalid "$OP" typedref object
  I=`expr $I + 1`
done

# 1.8.1.2.3 Verification type compatibility (Assignment compatibility)
I=1
for OP in stloc.0 "stloc.s 0" "starg.s 0"
do
  # ClassB not subtype of ClassA.
  ./make_store_test.sh assign_compat_1_${I} unverifiable "$OP" 'class ClassA' 'classB'

  # ValueTypeSubType base class of ValueType, but is a value type.
  ./make_store_test.sh assign_compat_2_${I} unverifiable "$OP" 'valuetype ValueType' 'valuetype ValueTypeSubType'

  # ClassA not interface type.
  ./make_store_test.sh assign_compat_3_${I} unverifiable "$OP" object 'class ClassA'
  
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
  I=`expr $I + 1`
done

for OP in "stfld TYPE1 Class::fld" "call void Class::Method(TYPE1)"
do
  # ClassB not subtype of ClassA.
  ./make_obj_store_test.sh assign_compat_1_${I} unverifiable "$OP" 'class ClassA' 'classB'

  # ValueTypeSubType base class of ValueType, but is a value type.
  ./make_obj_store_test.sh assign_compat_2_${I} unverifiable "$OP" 'valuetype ValueType' 'valuetype ValueTypeSubType'

  # ClassA not interface type.
  ./make_obj_store_test.sh assign_compat_3_${I} unverifiable "$OP" object 'class ClassA'
  
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
  I=`expr $I + 1`
done

# 1.8.1.3 Merging stack states
I=1
for TYPE1 in int32 int64 'native int' float64 'valuetype ValueType' 'class Class' 'int8&' 'int16&' 'int32&' 'int64&' 'native int&' 'float32&' 'float64&' 'valuetype ValueType&' 'class Class&' 'method int32 *(int32)' 'method int32 *(int32)' 'method float32 *(int32)' 'method int32 *(float64)' 'method vararg int32 *(int32)'
do
  for TYPE2 in int32 int64 'native int' float64 'valuetype ValueType' 'class Class' 'int8&' 'int16&' 'int32&' 'int64&' 'native int&' 'float32&' 'float64&' 'valuetype ValueType&' 'class Class&' 'method int32 *(int32)' 'method int32 *(int32)' 'method float32 *(int32)' 'method int32 *(float64)' 'method vararg int32 *(int32)'
  do
    if [ "$TYPE1" != "$TYPE2" ]; then
	./make_stack_merge_test.sh stack_merge_${I} unverifiable "$TYPE1" "$TYPE2"
	I=`expr $I + 1`
    fi
  done
done

# Unverifiable array stack merges

for TYPE1 in 'string []' 'string [,]' 'string [,,]' 
do
  for TYPE2 in 'string []' 'string [,]' 'string [,,]' 
  do
    if [ "$TYPE1" != "$TYPE2" ]; then
	./make_stack_merge_test.sh stack_merge_${I} unverifiable "$TYPE1" "$TYPE2"
	I=`expr $I + 1`
    fi
  done
done

# Exception block branch tests (see 3.15)
I=1
for OP in br "ldc.i4.0\n\tbrfalse"
do
  ./make_exception_branch_test.sh in_try_${I} "$OP branch_target1"
  ./make_exception_branch_test.sh in_catch_${I} "$OP branch_target2"
  ./make_exception_branch_test.sh in_finally_${I} "$OP branch_target3"
  ./make_exception_branch_test.sh in_filter_${I} "$OP branch_target4"
  ./make_exception_branch_test.sh out_try_${I} "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_catch_${I} "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_finally_${I} "" "" "" "$OP branch_target5"
  ./make_exception_branch_test.sh out_filter_${I} "" "" "" "" "$OP branch_target5"
  I=`expr $I + 1`
done

./make_exception_branch_test.sh ret_out_try "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_catch "" "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_finally "" "" "" "ldc.i4.0\n\tret"
./make_exception_branch_test.sh ret_out_filter "" "" "" "" "ldc.i4.0\n\tret"

# Unary branch op type tests (see 3.17)

for OP in brfalse
do
  ./make_unary_test.sh un_branch_op invalid "$OP branch_target" float64
done

# Ldloc.0 and Ldarg tests (see 3.38)

I=1
for OP in "ldarg.s 0" "ldarg.0" "ldarga.s 0" "ldloc.s 1" "ldloca.s 1"
do
  ./make_unary_test.sh ld_no_slot_${I} invalid "pop\n\t$OP\n\tpop" int32
  I=`expr $I + 1`
done

# Starg and Stloc tests (see 3.61)

I=1
for OP in "starg.s 0" "stloc.s 1"
do
  ./make_unary_test.sh st_no_slot_${I} invalid "$OP" int32
  I=`expr $I + 1`
done

# Ldfld and Ldflda tests (see 4.10)

for OP in ldfld ldflda
do
  ./make_unary_test.sh ${OP}_no_fld invalid "$OP int32 Class::invalid\n\tpop" "class Class"
  ./make_unary_test.sh ${OP}_bad_obj invalid "$OP int32 Class::valid\n\tpop" object
  ./make_unary_test.sh ${OP}_obj_int32 invalid "$OP int32 Class::valid\n\tpop" int32
  ./make_unary_test.sh ${OP}_obj_int64 invalid "$OP int32 Class::valid\n\tpop" int64
  ./make_unary_test.sh ${OP}_obj_float64 invalid "$OP int32 Class::valid\n\tpop" float64
  ./make_unary_test.sh ${OP}_obj_native_int unverifiable "$OP int32 Class::valid\n\tpop" 'native int'
  ./make_unary_test.sh ${OP}_obj_native_int unverifiable "$OP object Overlapped::objVal\n\tpop" "class Overlapped"
  ./make_unary_test.sh ${OP}_obj_native_int unverifiable "$OP int32 Overlapped::publicIntVal\n\tpop" "class Overlapped"
done

# Stfld tests (see 4.28)

./make_unary_test.sh stfld_no_fld invalid "ldc.i4.0\n\tstfld int32 Class::invalid" "class Class"
./make_unary_test.sh stfld_bad_obj invalid "ldc.i4.0\n\tstfld int32 Class::valid" object
./make_unary_test.sh stfld_obj_int32 invalid "ldc.i4.0\n\tstfld int32 Class::valid" int32
./make_unary_test.sh stfld_obj_int64 invalid "ldc.i4.0\n\tstfld int32 Class::valid" int64
./make_unary_test.sh stfld_obj_float64 invalid "ldc.i4.0\n\tstfld int32 Class::valid" float64
./make_unary_test.sh stfld_no_int invalid "stfld int32 Class::valid" "class Class"
./make_unary_test.sh stfld_obj_native_int unverifiable "ldc.i4.0\n\tstfld int32 Class::valid" 'native int'

# Box tests (see 4.1)

# Box non-existent type.
./make_unary_test.sh box_bad_type unverifiable "box valuetype NonExistent\n\tpop" "valuetype NonExistent"

# Top of stack not assignment compatible with typeToc.
./make_unary_test.sh box_not_compat unverifiable "box [mscorlib]System.Int32\n\tpop" float32

# Box byref type.
./make_unary_test.sh box_byref unverifiable "box [mscorlib]System.Int32&\n\tpop" 'int32&'

# Box byref-like type.
./make_unary_test.sh box_byref_like unverifiable "box [mscorlib]System.TypedRefrence\n\tpop" typedref

# Box void type.
./make_unary_test.sh box_void unverifiable "box [mscorlib]System.Void\n\tpop" "class [mscorlib]System.Void"