// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{ 
}
.class private  sealed  VT
       extends [mscorlib]System.ValueType
{
  .field public int32 m
  .field public static int32 n
  .method private specialname rtspecialname static 
          void .cctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.3
    IL_0001:  stsfld     int32 VT::n
    IL_0006:  ret
  } // end of method VT::.cctor

} // end of class VT

.class private sealed Days
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal value class Days Sun = int32(0x00000000)
  .field public static literal value class Days Mon = int32(0x00000001)
} // end of class Days

.class public sealed delegate1
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(class System.Object o,
                               native unsigned int 'method') runtime managed
  {
  } // end of method delegate1::.ctor

  .method public virtual instance void
          Invoke() runtime managed
  {
  } // end of method delegate1::Invoke

  .method public newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      class System.Object o) runtime managed
  {
  } // end of method delegate1::BeginInvoke

  .method public newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method delegate1::EndInvoke

} // end of class delegate1

.class interface public abstract ITest
{
  .method public newslot virtual abstract 
          instance int32 test() il managed
  {
  } // end of method ITest::test

} // end of class ITest

.class public CTest
       extends [mscorlib]System.Object
       implements ITest
{
  .field private class delegate1 dg
  .method public specialname instance void 
          add_dg(class delegate1 'value') il managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class delegate1 CTest::dg
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  delegate1
    IL_0012:  stfld      class delegate1 CTest::dg
    IL_0017:  ret
  } // end of method CTest::add_dg

  .method public specialname instance void 
          remove_dg(class delegate1 'value') il managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class delegate1 CTest::dg
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  delegate1
    IL_0012:  stfld      class delegate1 CTest::dg
    IL_0017:  ret
  } // end of method CTest::remove_dg

  .method public instance void f() il managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CTest::f

  .method public newslot final virtual 
          instance int32 test() il managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method CTest::test

  .method public specialname rtspecialname 
          instance void .ctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CTest::.ctor

  .event delegate1 dg
  {
    .addon instance void CTest::add_dg(class delegate1)
    .removeon instance void CTest::remove_dg(class delegate1)
  } // end of event CTest::dg
} // end of class CTest

.class public  filter2
       extends [mscorlib]System.Object
{
  .method public static void f1() il managed
  {
    // Code size       176 (0xb0)
    .maxstack  60
    .locals (int32 V_0,
             unsigned int32 V_1,
             int16 V_2,
             unsigned int16 V_3,
             unsigned int8 V_4,
             int8 V_5,
             int64 V_6,
             unsigned int64 V_7,
             wchar V_8,
             float32 V_9,
             float32 V_10,
             wchar V_11,
             unsigned int64 V_12,
             int64 V_13,
             int8 V_14,
             unsigned int8 V_15,
             unsigned int16 V_16,
             int16 V_17,
             unsigned int32 V_18,
             int32 V_19)
    .try
    {
      IL_0000:  ldc.i4     0xffff63c0
      IL_0005:  stloc.0
      .try
      {
        IL_0006:  ldc.i4     0x9c40
        IL_000b:  stloc.1
        .try
        {
          IL_000c:  ldc.i4     0xfffffc18
          IL_0011:  stloc.2
          .try
          {
            IL_0012:  ldc.i4     0x3e8
            IL_0017:  stloc.3
            .try
            {
              IL_0018:  ldc.i4.s   20
              IL_001a:  stloc.s    V_4

              start_try3:
              
                IL_001c:  ldc.i4.s   -20
                IL_001e:  stloc.s    V_5
                .try
                {
                  IL_0020:  ldc.i8     0x8000000000000000
                  IL_0029:  stloc.s    V_6
                  .try
                  {
                    IL_002b:  ldc.i4.0
                    IL_002c:  conv.i8
                    IL_002d:  stloc.s    V_7
                    .try
                    {
                      IL_002f:  ldc.i4.s   49
                      IL_0031:  stloc.s    V_8

                      start_try2:                  
                        IL_0033:  ldc.r4     -2.5699999
                        IL_0038:  stloc.s    V_9

                        start_try1:
                          IL_003a:  ldc.r4     2.5699999
                          //_003f:  stloc.s    V_10
                          IL_0041:  leave.s    IL_0046
                        end_try1: //

                        exception_filter1:
					    	pop
					    	ldc.i4.1
					    	endfilter
    					start_filter1:
                          ldc.r4     2.5699999
                          leave.s    IL_0046
                        end_filter1: //

                        start_handler1:
                          //_0043:  pop
                          ldc.r4     2.5699999
                          leave.s    IL_0046
                        end_handler1: //
                        
                        .try start_try1 to end_try1
                        filter exception_filter1 handler start_filter1 to end_filter1
                        catch [mscorlib]System.Object handler start_handler1 to end_handler1
                        
                        IL_0046:  ldc.i4.s   65
                        //_0048:  stloc.s    V_11
                        IL_004a:  leave.s    IL_004f

                      end_try2: //

                      exception_filter2:
				    	pop
				    	ldc.i4.1
				    	endfilter

    				  start_filter2:
                          ldc.r4     -2.5699999
                          leave.s    IL_004f
                      end_filter2: //
                      
                      start_handler2:
                        //_004c:  pop
                        ldc.r4     -2.5699999
                        IL_004d:  leave.s    IL_004f
                      end_handler2: //
                      
                      IL_004f:  ldc.i4.m1
                      IL_0050:  conv.i8
                      //_0051:  stloc.s    V_12

                      .try start_try2 to end_try2
                      filter exception_filter2 handler start_filter2 to end_filter2
                      catch [mscorlib]System.Object handler start_handler2 to end_handler2
                        
                      IL_0053:  leave.s    IL_0058

                    }  // end .try
                    catch [mscorlib]System.Object 
                    {
                      //_0055:  pop
                      ldc.i4.s   49
                      IL_0056:  leave.s    IL_0058

                    }  // end handler
                    IL_0058:  ldc.i8     0x7fffffffffffffff
                    //_0061:  stloc.s    V_13
                    IL_0063:  leave.s    IL_0068

                  }  // end .try
                  catch [mscorlib]System.Object 
                  {
                    //_0065:  pop
                    ldc.i4.0
                    conv.i8
                    IL_0066:  leave.s    IL_0068

                  }  // end handler
                  IL_0068:  ldc.i4.s   -20
                  //_006a:  stloc.s    V_14
                  IL_006c:  leave.s    IL_0071

                }  // end .try
                catch [mscorlib]System.Object 
                {
                  //_006e:  pop
                  ldc.i8     0x8000000000000000
                  IL_006f:  leave.s    IL_0071

                }  // end handler
                IL_0071:  ldc.i4.s   20
                //_0073:  stloc.s    V_15
                IL_0075:  leave.s    IL_007a

              end_try3:  //

              exception_filter3:
		    	pop
		    	ldc.i4.1
		    	endfilter
		      start_filter3:
                ldc.i4.s   -20
                leave.s    IL_007a
              end_filter3: //
              
              start_handler3:
                //_0077:  pop
                ldc.i4.s   -20
                IL_0078:  leave.s    IL_007a
              end_handler3: //
              
              IL_007a:  ldc.i4     0x3e8
              //_007f:  stloc.s    V_16
              IL_0081:  leave.s    IL_0086

              .try start_try3 to end_try3
              filter exception_filter3 handler start_filter3 to end_filter3
              catch [mscorlib]System.Object handler start_handler3 to end_handler3

            }  // end .try
            catch [mscorlib]System.Object 
            {
              //_0083:  pop
              IL_0084:  leave.s    IL_0086

            }  // end handler
            IL_0086:  ldc.i4     0xfffffc18
            //_008b:  stloc.s    V_17
            IL_008d:  leave.s    IL_0092

          }  // end .try
          catch [mscorlib]System.Object 
          {
            //_008f:  pop
            ldc.i4     0x3e8
            IL_0090:  leave.s    IL_0092

          }  // end handler
          IL_0092:  ldc.i4     0x9c40
          //_0097:  stloc.s    V_18
          IL_0099:  leave.s    IL_009e

        }  // end .try
        catch [mscorlib]System.Object 
        {
          //_009b:  pop
          IL_009c:  leave.s    IL_009e

        }  // end handler
        IL_009e:  ldc.i4     0xffff63c0
        //_00a3:  stloc.s    V_19
        IL_00a5:  leave.s    IL_00aa

      }  // end .try
      filter
      {
    	pop
    	ldc.i4.1
    	endfilter
      }
      {
      	ldc.i4     0x9c40
        leave.s    IL_00aa
      }
      catch [mscorlib]System.Object 
      {
        //_00a7:  pop
        ldc.i4     0x9c40
        IL_00a8:  leave.s    IL_00aa

      }  // end handler
      IL_00aa:  leave.s    IL_00af

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //_00ac:  pop
      IL_00ad:  leave.s    IL_00af

    }  // end handler
    IL_00af:  ret
  } // end of method filter2::f1

  .method public static void f11() il managed
  {
    // Code size       307 (0x133)
    .maxstack  60
    .locals (float32 V_0,
             float64 V_1,
             bool V_2,
             value class [mscorlib]System.Decimal V_3,
             value class VT V_4,
             int32 V_5,
             int32 V_6,
             value class Days V_7,
             class System.Object V_8,
             class filter2 V_9,
             class System.String V_10,
             int32[] V_11,
             int32[] V_12,
             int32 V_13,
             class System.String V_14,
             class filter2 V_15,
             class System.Object V_16,
             value class Days V_17,
             value class VT V_18,
             int32 V_19,
             int32 V_20,
             value class [mscorlib]System.Decimal V_21,
             bool V_22,
             float64 V_23,
             float32 V_24,
             int32[] V_25)
    IL_0000:  ldloca.s   V_4
    IL_0002:  initobj    VT
    IL_0008:  ldloca.s   V_18
    IL_000a:  initobj    VT
    IL_0010:  ldc.r4     -2.5699999
    IL_0015:  stloc.0

    start_try1:
      IL_0016:  ldc.r8     -1.2888888888888881
      IL_001f:  stloc.1

      start_try2:
        IL_0020:  ldc.i4.1
        IL_0021:  stloc.2
        .try
        {
          IL_0022:  ldc.i4     0x2715
          IL_0027:  ldc.i4.0
          IL_0028:  ldc.i4.0
          IL_0029:  ldc.i4     0x80
          IL_002e:  ldc.i4.2
          IL_002f:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                             int32,
                                                                             int32,
                                                                             bool,
                                                                             unsigned int8)
          IL_0034:  stloc.3
          .try
          {
            IL_0035:  ldloca.s   V_4
            IL_0037:  ldc.i4     0xfffffed4
            IL_003c:  stfld      int32 VT::m
            IL_0041:  ldloca.s   V_4
            IL_0043:  ldfld      int32 VT::m
            IL_0048:  stloc.s    V_5
            IL_004a:  ldsfld     int32 VT::n
            IL_004f:  stloc.s    V_6
            .try
            {
              IL_0051:  ldc.i4.0
              IL_0052:  stloc.s    V_7
              .try
              {
                IL_0054:  newobj     instance void [mscorlib]System.Object::.ctor()
                IL_0059:  stloc.s    V_8
                .try
                {
                  IL_005b:  newobj     instance void filter2::.ctor()
                  IL_0060:  stloc.s    V_9
                  .try
                  {
                    IL_0062:  ldstr      "test string"
                    IL_0067:  stloc.s    V_10
                    .try
                    {
                      IL_0069:  ldc.i4.2
                      IL_006a:  newarr     [mscorlib]System.Int32
                      IL_006f:  stloc.s    V_25
                      IL_0071:  ldloc.s    V_25
                      IL_0073:  ldc.i4.0
                      IL_0074:  ldc.i4.1
                      IL_0075:  stelem.i4
                      IL_0076:  ldloc.s    V_25
                      IL_0078:  ldc.i4.1
                      IL_0079:  ldc.i4.2
                      IL_007a:  stelem.i4
                      IL_007b:  ldloc.s    V_25
                      IL_007d:  stloc.s    V_11

                      start_try3:
                        IL_007f:  ldc.i4.2
                        IL_0080:  newarr     [mscorlib]System.Int32
                        IL_0085:  stloc.s    V_25
                        IL_0087:  ldloc.s    V_25
                        IL_0089:  ldc.i4.0
                        IL_008a:  ldc.i4.3
                        IL_008b:  stelem.i4
                        IL_008c:  ldloc.s    V_25
                        IL_008e:  ldc.i4.1
                        IL_008f:  ldc.i4.4
                        IL_0090:  stelem.i4
                        IL_0091:  ldloc.s    V_25
                        IL_0093:  stloc.s    V_12
                        IL_0095:  ldloc.s    V_12
                        IL_0097:  ldc.i4.0
                        IL_0098:  ldelem.i4
                        //_0099:  stloc.s    V_13
                        IL_009b:  leave.s    IL_00a0
                      end_try3: //
                      
                      exception_filter3:
				    	pop
				    	ldc.i4.1
				    	endfilter
				      start_filter3:
                        ldc.i4.2
                        newarr     [mscorlib]System.Int32
                        stloc.s    V_25
                        ldloc.s    V_25
                        ldc.i4.0
                        ldc.i4.3
                        stelem.i4
                        ldloc.s    V_25
                        ldc.i4.1
                        ldc.i4.4
                        stelem.i4
                        ldloc.s    V_25
                        stloc.s    V_12
                        ldloc.s    V_12
                        ldc.i4.0
                        ldelem.i4
                        leave.s    IL_00a0
                      end_filter3: //

                      start_handler3: 
                        //_009d:  pop
                        ldc.i4.2
                        newarr     [mscorlib]System.Int32
                        stloc.s    V_25
                        ldloc.s    V_25
                        ldc.i4.0
                        ldc.i4.3
                        stelem.i4
                        ldloc.s    V_25
                        ldc.i4.1
                        ldc.i4.4
                        stelem.i4
                        ldloc.s    V_25
                        stloc.s    V_12
                        ldloc.s    V_12
                        ldc.i4.0
                        ldelem.i4
                        IL_009e:  leave.s    IL_00a0
                      end_handler3: //

                      .try start_try3 to end_try3
                      filter exception_filter3 handler start_filter3 to end_filter3
                      catch [mscorlib]System.Object handler start_handler3 to end_handler3
    
                      IL_00a0:  ldstr      "string test"
                      //_00a5:  stloc.s    V_14
                      IL_00a7:  leave.s    IL_00ac

                    }  // end .try
                    catch [mscorlib]System.Object 
                    {
                      //_00a9:  pop
                      ldstr      "string test"
                      IL_00aa:  leave.s    IL_00ac

                    }  // end handler
                    IL_00ac:  newobj     instance void filter2::.ctor()
                    //_00b1:  stloc.s    V_15
                    IL_00b3:  leave.s    IL_00b8

                  }  // end .try                  
                  catch [mscorlib]System.Object 
                  {
                    //_00b5:  pop
                    //newobj     instance void filter2::.ctor()
                    IL_00b6:  leave.s    IL_00b8

                  }  // end handler
                  IL_00b8:  newobj     instance void [mscorlib]System.Object::.ctor()
                  //_00bd:  stloc.s    V_16
                  IL_00bf:  leave.s    IL_00c4

                }  // end .try
                catch [mscorlib]System.Object 
                {
                  //_00c1:  pop
                  IL_00c2:  leave.s    IL_00c4

                }  // end handler
                IL_00c4:  ldc.i4.1
                IL_00c5:  stloc.s    V_17
                IL_00c7:  ldloc.s    V_17
                IL_00c9:  box        Days
                //IL_00ce:  call       void [System.Console]System.Console::WriteLine(class System.Object)
                IL_00d3:  leave.s    IL_00d8

              }  // end .try
              catch [mscorlib]System.Object 
              {
                //IL_00d5:  pop
                ldc.i4.1
                stloc.s    V_17
                ldloc.s    V_17
                box        Days
                IL_00d6:  leave.s    IL_00d8

              }  // end handler
              IL_00d8:  ldloca.s   V_18
              IL_00da:  ldc.i4     0x12c
              IL_00df:  stfld      int32 VT::m
              IL_00e4:  ldloca.s   V_18
              IL_00e6:  ldfld      int32 VT::m
              IL_00eb:  stloc.s    V_19
              IL_00ed:  ldsfld     int32 VT::n
              //IL_00f2:  stloc.s    V_20
              IL_00f4:  leave.s    IL_00f9

            }  // end .try
            filter
            {
		    	pop
		    	ldc.i4.1
		    	endfilter
	    	}
            {
              ldloca.s   V_18
              ldc.i4     0x12c
              stfld      int32 VT::m
              ldloca.s   V_18
              ldfld      int32 VT::m
              stloc.s    V_19
              ldsfld     int32 VT::n
              leave.s    IL_00f9
            }
            catch [mscorlib]System.Object 
            {
              //IL_00f6:  pop
              ldloca.s   V_18
              ldc.i4     0x12c
              stfld      int32 VT::m
              ldloca.s   V_18
              ldfld      int32 VT::m
              stloc.s    V_19
              ldsfld     int32 VT::n
              IL_00f7:  leave.s    IL_00f9
            }  // end handler
            IL_00f9:  ldc.i4     0x676
            IL_00fe:  ldc.i4.0
            IL_00ff:   ldc.i4.0
            IL_0100:  ldc.i4.0
            IL_0101:  ldc.i4.3
            IL_0102:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                               int32,
                                                                               int32,
                                                                               bool,
                                                                               unsigned int8)
            //IL_0107:  stloc.s    V_21
            IL_0109:  leave.s    IL_010e

          }  // end .try
          catch [mscorlib]System.Object 
          {
            //IL_010b:  pop
            IL_010c:  leave.s    IL_010e

          }  // end handler
          IL_010e:  ldc.i4.0
          //IL_010f:  stloc.s    V_22
          IL_0111:  leave.s    IL_0116

        }  // end .try
        catch [mscorlib]System.Object 
        {
          //IL_0113:  pop
          IL_0114:  leave.s    IL_0116

        }  // end handler
        IL_0116:  ldc.r8     1.2888888888888881
        //IL_011f:  stloc.s    V_23
        IL_0121:  leave.s    IL_0126
      end_try2: //

      exception_filter2:
    	pop
    	ldc.i4.1
    	endfilter
      start_filter2:
        ldc.r8     1.2888888888888881
        leave.s    IL_0126
      end_filter2: //
      
      start_handler2:
        //IL_0123:  pop
        ldc.r8     1.2888888888888881
        IL_0124:  leave.s    IL_0126
      end_handler2: //
      
      IL_0126:  ldc.r4     2.5699999
      //IL_012b:  stloc.s    V_24
      IL_012d:  leave.s    IL_0132
    end_try1: //
    
    exception_filter1:
    	pop
    	ldc.i4.1
    	endfilter
    start_filter1:
      ldc.r4     2.5699999
      leave.s    IL_0132
    end_filter1: //

    start_handler1:
      //IL_012f:  pop
      ldc.r4     2.5699999
      IL_0130:  leave.s    IL_0132
    end_handler1: //

    .try start_try2 to end_try2
    filter exception_filter2 handler start_filter2 to end_filter2
    catch [mscorlib]System.Object handler start_handler2 to end_handler2

    .try start_try1 to end_try1
    filter exception_filter1 handler start_filter1 to end_filter1
    catch [mscorlib]System.Object handler start_handler1 to end_handler1
    
    IL_0132:  ret
  } // end of method filter2::f11

  .method public static void  f25(int32 arg) il managed
  {
    // Code size       218 (0xda)
    .maxstack  60
    .locals (int32 V_0,
             int32[0...,0...] V_1,
             int32[0...,0...,0...] V_2,
             class CTest V_3,
             int32 V_4,
             class CTest V_5,
             class CTest V_6,
             class CTest V_7,
             int32 V_8,
             int32[0...,0...,0...] V_9,
             int32 V_10,
             int32[0...,0...] V_11,
             int32 V_12,
             int32 V_13)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldc.i4.2
      IL_0003:  ldc.i4.2
      IL_0004:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_0009:  dup
      IL_000a:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-1' '<GlobalName>'::'$$method0x600000e-1'
      IL_000f:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      IL_0014:  stloc.1

      start_try2:
        IL_0015:  ldc.i4.3
        IL_0016:  ldc.i4.1
        IL_0017:  ldc.i4.3
        IL_0018:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                        int32,
                                                                        int32)
        IL_001d:  dup
        IL_001e:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-2' '<GlobalName>'::'$$method0x600000e-2'
        IL_0023:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                            value class [mscorlib]System.RuntimeFieldHandle)
        IL_0028:  stloc.2
        .try
        {
          IL_0029:  newobj     instance void CTest::.ctor()
          IL_002e:  stloc.3
          IL_002f:  ldloc.3
          IL_0030:  callvirt   instance int32 CTest::test()
          IL_0035:  stloc.s    V_4

          start_try1:
            IL_0037:  newobj     instance void CTest::.ctor()
            IL_003c:  stloc.s    V_5
            IL_003e:  ldloc.s    V_5
            IL_0040:  ldloc.s    V_5
            IL_0042:  ldftn      instance void CTest::f()
            IL_0048:  newobj     instance void delegate1::.ctor(class System.Object,
                                                                native unsigned int)
            IL_004d:  callvirt   instance void CTest::add_dg(class delegate1)
            .try
            {
              IL_0052:  newobj     instance void CTest::.ctor()
              IL_0057:  stloc.s    V_6
              IL_0059:  ldloc.s    V_6
              IL_005b:  ldloc.s    V_6
              IL_005d:  ldftn      instance void CTest::f()
              IL_0063:  newobj     instance void delegate1::.ctor(class System.Object,
                                                                  native unsigned int)
              //IL_0068:  callvirt   instance void CTest::add_dg(class delegate1)
              IL_006d:  leave.s    IL_0072

            }  // end .try
            filter
            {
		    	pop
		    	ldc.i4.1
		    	endfilter
	    	}
            {
              leave.s    IL_0072
            }
            catch [mscorlib]System.Object 
            {
              IL_006f:  pop
              IL_0070:  leave.s    IL_0072

            }  // end handler
            IL_0072:  newobj     instance void CTest::.ctor()
            IL_0077:  stloc.s    V_7
            IL_0079:  ldloc.s    V_7
            //IL_007b:  callvirt   instance int32 CTest::test()
            //IL_0080:  stloc.s    V_8
            IL_0082:  leave.s    IL_0087
          end_try1: //

          exception_filter1:
		    	pop
		    	ldc.i4.1
		    	endfilter
    	  start_filter1:
            leave.s    IL_0087
          end_filter1: //            

          start_handler1:
            //IL_0084:  pop
            IL_0085:  leave.s    IL_0087
          end_handler1:

          .try start_try1 to end_try1
          filter exception_filter1 handler start_filter1 to end_filter1
          catch [mscorlib]System.Object handler start_handler1 to end_handler1
          
          IL_0087:  ldc.i4.3
          IL_0088:  ldc.i4.1
          IL_0089:  ldc.i4.3
          IL_008a:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                          int32,
                                                                          int32)
          IL_008f:  dup
          IL_0090:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-3' '<GlobalName>'::'$$method0x600000e-3'
          //IL_0095:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
          //                                                                                                    value class [mscorlib]System.RuntimeFieldHandle)
          //IL_009a:  stloc.s    V_9
          //IL_009c:  ldloc.s    V_9
          //IL_009e:  ldc.i4.0
          //IL_009f:  ldc.i4.0
          //IL_00a0:  ldc.i4.1
          //IL_00a1:  call       instance int32 int32[0...,0...,0...]::Get(int32,
          //                                                               int32,
          //                                                               int32)
          //IL_00a6:  stloc.s    V_10
          IL_00a8:  leave.s    IL_00ad

        }  // end .try
        filter
        {
	    	pop
	    	ldc.i4.1
	    	endfilter
    	}
        {
          ldc.i4.3
          ldc.i4.1
          ldc.i4.3
          newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                          int32,
                                                                          int32)
          dup
          ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-3' '<GlobalName>'::'$$method0x600000e-3'
          leave.s    IL_00ad
        }
        catch [mscorlib]System.Object 
        {
          //IL_00aa:  pop
          ldc.i4.3
          ldc.i4.1
          ldc.i4.3
          newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                          int32,
                                                                          int32)
          dup
          ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-3' '<GlobalName>'::'$$method0x600000e-3'
          IL_00ab:  leave.s    IL_00ad
        }  // end handler
        IL_00ad:  leave.s    IL_00b2

      end_try2://

      exception_filter2:
    	pop
    	ldc.i4.1
    	endfilter
      start_filter2:
        leave.s    IL_00b2
      end_filter2: //
      
      start_handler2: 
        IL_00af:  pop
        IL_00b0:  leave.s    IL_00b2
      end_handler2:
      
      IL_00b2:  ldc.i4.2
      IL_00b3:  ldc.i4.2
      IL_00b4:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_00b9:  dup
      IL_00ba:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-4' '<GlobalName>'::'$$method0x600000e-4'
      IL_00bf:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      //IL_00c4:  stloc.s    V_11
      //IL_00c6:  ldloc.s    V_11
      //IL_00c8:  ldc.i4.0
      //IL_00c9:  ldc.i4.1
      //IL_00ca:  call       instance int32 int32[0...,0...]::Get(int32,
      //                                                          int32)
      //IL_00cf:  stloc.s    V_12

      .try start_try2 to end_try2
      filter exception_filter2 handler start_filter2 to end_filter2
      catch [mscorlib]System.Object handler start_handler2 to end_handler2
    
      IL_00d1:  leave.s    IL_00d6

    }  // end .try
    filter
    {
    	pop
    	ldc.i4.1
    	endfilter
    }
    {
      ldc.i4.2
      ldc.i4.2
      newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      dup
      ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-4' '<GlobalName>'::'$$method0x600000e-4'
      call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      leave.s    IL_00d6
    }
    catch [mscorlib]System.Object 
    {
      //IL_00d3:  pop
      ldc.i4.2
      ldc.i4.2
      newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      dup
      ldtoken    field value class '<GlobalName>'/'$$struct0x600000e-4' '<GlobalName>'::'$$method0x600000e-4'
      call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      IL_00d4:  leave.s    IL_00d6
    }  // end handler
    IL_00d6:  ldarg.0
    IL_00d7:  stloc.s    V_13
    IL_00d9:  ret
  } // end of method filter2::f25

  .method public static void  f26(unsigned int16 arg1,
                                            int32 arg2,
                                            class System.String arg3,
                                            class System.Object arg4) il managed
  {
    // Code size       146 (0x92)
    .maxstack  60
    .locals (unsigned int16 V_0,
             int32 V_1,
             class System.String V_2,
             class System.Object V_3,
             int16 V_4,
             unsigned int8 V_5,
             class System.String V_6,
             int32 V_7,
             class System.String[] V_8,
             unsigned int16 V_9,
             unsigned int16* V_10,
             unsigned int16 V_11,
             unsigned int8 V_12,
             class System.String V_13,
             int32 V_14,
             class System.String[] V_15,
             int32 V_16,
             int16 V_17,
             unsigned int16 V_18,
             int32 V_19,
             class System.String V_20,
             class System.Object V_21,
             class System.String[] V_22,
             unsigned int16 BUF)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  stloc.1
    IL_0004:  ldarg.2
    IL_0005:  stloc.2
    IL_0006:  ldarg.3
    IL_0007:  stloc.3

    start_try4:
      IL_0008:  ldc.i4     0xfffffc18
      IL_000d:  stloc.s    V_4

      start_try3:
        IL_000f:  ldc.i4.s   20
        IL_0011:  stloc.s    V_5
        IL_0013:  ldstr      "f28"
        IL_0018:  stloc.s    V_6
        IL_001a:  ldc.i4     0xffff15a0
        IL_001f:  stloc.s    V_7
        IL_0021:  ldc.i4.2
        IL_0022:  newarr     [mscorlib]System.String
        IL_0027:  stloc.s    V_22
        IL_0029:  ldloc.s    V_22
        IL_002b:  ldc.i4.0
        IL_002c:  ldstr      "a"
        IL_0031:  stelem.ref
        IL_0032:  ldloc.s    V_22
        IL_0034:  ldc.i4.1
        IL_0035:  ldstr      "b"
        IL_003a:  stelem.ref
        IL_003b:  ldloc.s    V_22
        IL_003d:  stloc.s    V_8

        start_try2:
          IL_003f:  ldc.i4     0x3e8
          IL_0044:  stloc.s    V_9

		  start_try1:
            IL_0046:  ldc.i4.2
            IL_0047:  ldc.i4.1
            IL_0048:  mul
            IL_0049:  localloc
            IL_004b:  stloc.s    V_10
            IL_004d:  ldloc.s    V_10
            IL_004f:  ldloc.s    V_9
            IL_0051:  stind.i2
            IL_0052:  ldloc.s    V_10
            IL_0054:  ldind.u2
            //IL_0055:  stloc.s    V_11
            IL_0057:  leave.s    IL_005c
          end_try1: //
          exception_filter1:
	    	pop
	    	ldc.i4.1
	    	endfilter
	      start_filter1:
            pop
            ldc.i4.2
            ldc.i4.1
            mul
            pop ldloca.s BUF //localloc
            stloc.s    V_10
            ldloc.s    V_10
            ldloc.s    V_9
            stind.i2
            ldloc.s    V_10
            ldind.u2
            leave.s    IL_005c
          end_filter1: //
          start_handler1:
            pop
            ldc.i4.2
            ldc.i4.1
            mul
            pop ldloca.s BUF //localloc
            stloc.s    V_10
            ldloc.s    V_10
            ldloc.s    V_9
            stind.i2
            ldloc.s    V_10
            ldind.u2
            IL_005a:  leave.s    IL_005c
          end_handler1: //
          IL_005c:  ldloc.s    V_5
          IL_005e:  stloc.s    V_12
          IL_0060:  ldloc.s    V_6
          IL_0062:  stloc.s    V_13
          IL_0064:  ldloc.s    V_7
          IL_0066:  stloc.s    V_14
          IL_0068:  ldloc.s    V_8
          IL_006a:  stloc.s    V_15
          IL_006c:  ldloc.s    V_8
          IL_006e:  ldlen
          //IL_006f:  conv.i4
          //IL_0070:  stloc.s    V_16
          IL_0072:  leave.s    IL_0077
        end_try2: //
        exception_filter2:
	    	pop
	    	ldc.i4.1
	    	endfilter
    	start_filter2:
          ldloc.s    V_5
          stloc.s    V_12
          ldloc.s    V_6
          stloc.s    V_13
          ldloc.s    V_7
          stloc.s    V_14
          ldloc.s    V_8
          stloc.s    V_15
          ldloc.s    V_8
          ldlen
          leave.s    IL_0077
        end_filter2: //
        start_handler2:
          //IL_0074:  pop
          ldloc.s    V_5
          stloc.s    V_12
          ldloc.s    V_6
          stloc.s    V_13
          ldloc.s    V_7
          stloc.s    V_14
          ldloc.s    V_8
          stloc.s    V_15
          ldloc.s    V_8
          ldlen
          IL_0075:  leave.s    IL_0077
        end_handler2: //
        IL_0077:  ldloc.s    V_4
        //IL_0079:  stloc.s    V_17
        IL_007b:  leave.s    IL_0080
      end_try3: //
      exception_filter3:
    	pop
    	ldc.i4.1
    	endfilter
      start_filter3:
        leave.s    IL_0080
      end_filter3: //
      start_handler3: 
        //IL_007d:  pop
        IL_007e:  leave.s    IL_0080
      end_handler3: //
      
      IL_0080:  ldarg.0
      //IL_0081:  stloc.s    V_18
      IL_0083:  ldarg.1
      //IL_0084:  stloc.s    V_19
      IL_0086:  ldarg.2
      //IL_0087:  stloc.s    V_20
      IL_0089:  ldarg.3
      //IL_008a:  stloc.s    V_21
      IL_008c:  leave.s    IL_0091

    end_try4: //
    exception_filter4:
    	pop
    	ldc.i4.1
    	endfilter
    start_filter4:
      ldarg.0
      ldarg.1
      ldarg.2
      ldarg.3
      leave.s    IL_0091
    end_filter4: //
    start_handler4:
      //IL_008e:  pop
      ldarg.0
      ldarg.1
      ldarg.2
      ldarg.3
      IL_008f:  leave.s    IL_0091
    end_handler4: //
    IL_0091:  ret

    .try start_try1 to end_try1
    filter exception_filter1 handler start_filter1 to end_filter1
    catch [mscorlib]System.Object handler start_handler1 to end_handler1

    .try start_try2 to end_try2
    filter exception_filter2 handler start_filter2 to end_filter2
    catch [mscorlib]System.Object handler start_handler2 to end_handler2

    .try start_try3 to end_try3
    filter exception_filter3 handler start_filter3 to end_filter3
    catch [mscorlib]System.Object handler start_handler3 to end_handler3

    .try start_try4 to end_try4
    filter exception_filter4 handler start_filter4 to end_filter4
    catch [mscorlib]System.Object handler start_handler4 to end_handler4
    
  } // end of method filter2::f26

  .method public static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       81 (0x51)
    .maxstack  30
    .locals (bool V_0,
             int32 V_1)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  call       void filter2::f1()
    IL_0007:  call       void filter2::f11()
    IL_000c:  ldc.i4     0x9c40
    IL_0011:  call       void filter2::f25(int32)
    IL_0016:  ldc.i4     0xff
    IL_001b:  ldc.i4     0x80000000
    IL_0020:  ldstr      "f26"
    IL_0025:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_002a:  call       void filter2::f26(unsigned int16,
                                        int32,
                                        class System.String,
                                        class System.Object)
    IL_002f:  ldloc.0
    IL_0030:  brfalse.s  IL_0041

    IL_0032:  ldstr      "PASSED"
    IL_0037:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_003c:  ldc.i4.s   100
    IL_003e:  stloc.1
    IL_003f:  br.s       IL_004f

    IL_0041:  ldstr      "FAILED"
    IL_0046:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_004b:  ldc.i4.1
    IL_004c:  stloc.1
    IL_004d:  br.s       IL_004f

    IL_004f:  ldloc.1
    IL_0050:  ret
  } // end of method filter2::Main

  .method public hidebysig specialname rtspecialname 
        instance void .ctor()
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method filter2::.ctor


} // end of class filter2

.class private  '<GlobalName>'
       extends [mscorlib]System.Object
{
  .class explicit ansi sealed nested private '$$struct0x600000e-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x600000e-1'

  .class explicit ansi sealed nested private '$$struct0x600000e-2'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x600000e-2'

  .class explicit ansi sealed nested private '$$struct0x600000e-3'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x600000e-3'

  .class explicit ansi sealed nested private '$$struct0x600000e-4'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x600000e-4'

  .field static assembly value class '<GlobalName>'/'$$struct0x600000e-1' '$$method0x600000e-1' at D_000024A8
  .field static assembly value class '<GlobalName>'/'$$struct0x600000e-2' '$$method0x600000e-2' at D_000024B8
  .field static assembly value class '<GlobalName>'/'$$struct0x600000e-3' '$$method0x600000e-3' at D_000024E0
  .field static assembly value class '<GlobalName>'/'$$struct0x600000e-4' '$$method0x600000e-4' at D_00002508
} // end of class '<GlobalName>'

.data D_000024A8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000024B8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_000024E0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_00002508 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 0B 30 04 00 DA 00 00 00 04 00 00 11 02 0A 18 18  // .0..............
                 73 08 00 00 0A 25 D0 07 00 00 04 28 09 00 00 0A  // s....%.....(....
                 0B 19 17 19 73 0A 00 00 0A 25 D0 08 00 00 04 28  // ....s....%.....(
                 09 00 00 0A 0C 73 0B 00 00 06 0D 09 6F 0A 00 00  // .....s......o...
                 06 13 04 73 0B 00 00 06 13 05 11 05 11 05 FE 06  // ...s............
                 09 00 00 06 73 02 00 00 06 6F 07 00 00 06 73 0B  // ....s....o....s.
                 00 00 06 13 06 11 06 11 06 FE 06 09 00 00 06 73  // ...............s
                 02 00 00 06 6F 07 00 00 06 DE 03 26 DE 00 73 0B  // ....o......&..s.
                 00 00 06 13 07 11 07 6F 0A 00 00 06 13 08 DE 03  // .......o........
                 26 DE 00 19 17 19 73 0A 00 00 0A 25 D0 09 00 00  // &.....s....%....
                 04 28 09 00 00 0A 13 09 11 09 16 16 17 28 0B 00  // .(...........(..
                 00 0A 13 0A DE 03 26 DE 00 DE 03 26 DE 00 18 18  // ......&....&....
                 73 08 00 00 0A 25 D0 0A 00 00 04 28 09 00 00 0A  // s....%.....(....
                 13 0B 11 0B 16 17 28 0C 00 00 0A 13 0C DE 03 26  // ......(........&
                 DE 00 02 13 0D 2A 00 00 01 40 00 00 00 00 52 00  // .....*...@....R.
                 1D 6F 00 03 04 00 00 01 00 00 37 00 4D 84 00 03  // .o........7.M...
                 04 00 00 01 00 00 29 00 81 AA 00 03 04 00 00 01  // ......).........
                 00 00 15 00 9A AF 00 03 04 00 00 01 00 00 02 00
                 D1 D3 00 03 04 00 00 01 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 0B 30 03 00 92 00 00 00 05 00 00 11  // .....0..........
                 02 0A 03 0B 04 0C 05 0D 20 18 FC FF FF 13 04 1F  // ........ .......
                 14 13 05 72 31 00 00 70 13 06 20 A0 15 FF FF 13  // ...r1..p.. .....
                 07 18 8D 11 00 00 01 13 16 11 16 16 72 39 00 00  // ............r9..
                 70 A2 11 16 17 72 3D 00 00 70 A2 11 16 13 08 20  // p....r=..p..... 
                 E8 03 00 00 13 09 18 17 5A FE 0F 13 0A 11 0A 11  // ........Z.......
                 09 53 11 0A 49 13 0B DE 03 26 DE 00 11 05 13 0C  // .S..I....&......
                 11 06 13 0D 11 07 13 0E 11 08 13 0F 11 08 8E 69  // ...............i
                 13 10 DE 03 26 DE 00 11 04 13 11 DE 03 26 DE 00  // ....&........&..
                 02 13 12 03 13 13 04 13 14 05 13 15 DE 03 26 DE  // ..............&.
                 00 2A 00 00 01 34 00 00 00 00 46 00 13 59 00 03  // .*...4....F..Y..
                 04 00 00 01 00 00 3F 00 35 74 00 03 04 00 00 01  // ......?.5t......
                 00 00 0F 00 6E 7D 00 03 04 00 00 01 00 00 08 00  // ....n}..........
                 86 8E 00 03 04 00 00 01 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 03 30 04 00 51 00 00 00  // .........0..Q...
                 06 00 00 11 17 0A 28 0C 00 00 06 28 0D 00 00 06  // ......(....(....
                 20 40 9C 00 00 28 0E 00 00 06 20 FF 00 00 00 20  //  @...(.... .... 
                 00 00 00 80 72 41 00 00 70 73 05 00 00 0A 28 0F  // ....rA..ps....(.
                 00 00 06 06 2C 0F 72 49 00 00 70 28 0D 00 00 0A  // ....,.rI..p(....
                 1F 64 0B 2B 0E 72 57 00 00 70 28 0D 00 00 0A 17  // .d.+.rW..p(.....
                 0B 2B 00 07 2A 1E 02 28 05 00 00 0A 2A 00 00 00  // .+..*..(....*...
                 42 53 4A 42 01 00 01 00 00 00 00 00 0C 00 00 00  // BSJB............
                 76 31 2E 78 38 36 63 68 6B 00 00 00 00 00 05 00  // v1.x86chk.......
                 6C 00 00 00 0C 04 00 00 23 7E 00 00 78 04 00 00  // l.......#~..x...
                 64 03 00 00 23 53 74 72 69 6E 67 73 00 00 00 00  // d...#Strings....
                 DC 07 00 00 6C 03 00 00 23 42 6C 6F 62 00 00 00  // ....l...#Blob...
                 48 0B 00 00 68 00 00 00 23 55 53 00 B0 0B 00 00  // H...h...#US.....
                 10 00 00 00 23 47 55 49 44 00 00 00 00 00 00 00  // ....#GUID.......
                 01 00 00 01 57 DF 16 29 09 02 00 00 00 FA 01 33  // ....W..).......3
                 00 02 00 00 01 00 00 00 12 00 00 00 0C 00 00 00
                 0A 00 00 00 11 00 00 00 0C 00 00 00 01 00 00 00
                 0E 00 00 00 02 00 00 00 02 00 00 00 01 00 00 00
                 04 00 00 00 06 00 00 00 01 00 00 00 01 00 00 00
                 02 00 00 00 02 00 00 00 04 00 00 00 01 00 00 00
                 01 00 00 00 04 00 00 00 00 00 4C 00 01 00 00 00  // ..........L.....
                 00 00 06 00 69 00 62 00 06 00 76 00 62 00 06 00  // ....i.b...v.b...
                 80 00 62 00 06 00 A2 00 62 00 06 00 DC 00 62 00  // ..b.....b.....b.
                 06 00 E9 00 62 00 06 00 58 01 3C 01 06 00 74 01  // ....b...X.<...t.
                 3C 01 06 00 96 01 83 01 06 00 D7 01 62 00 06 00  // <...........b...
                 EF 01 62 00 06 00 F7 01 62 00 06 00 FD 01 62 00  // ..b.....b.....b.
                 06 00 68 02 48 02 06 00 77 02 62 00 06 00 7D 02  // ..h.H...w.b...}.
                 62 00 06 00 30 03 62 00 06 00 47 03 37 03 00 00  // b...0.b...G.7...
                 00 00 43 00 00 00 00 00 01 00 01 00 08 01 10 00  // ..C.............
                 73 00 00 00 05 00 01 00 01 00 00 21 00 00 7B 00  // s..........!..{.
                 00 00 09 00 03 00 02 00 01 01 00 00 92 00 00 00
                 0D 00 06 00 02 00 A1 00 00 00 9C 00 00 00 00 00
                 06 00 06 00 01 00 10 00 A9 00 00 00 11 00 06 00
                 07 00 01 00 10 00 AF 00 00 00 11 00 07 00 0C 00
                 00 00 00 00 13 02 00 00 11 00 07 00 12 00 13 01
                 00 00 20 02 00 00 05 00 0B 00 12 00 13 01 00 00  // .. .............
                 A0 02 00 00 05 00 0B 00 12 00 13 01 00 00 C8 02
                 00 00 05 00 0B 00 12 00 13 01 00 00 F4 02 00 00
                 05 00 0B 00 12 00 06 00 B4 00 1F 00 16 00 B6 00
                 1F 00 06 06 BF 00 1F 00 56 80 C7 00 26 00 56 80  // ........V...&.V.
                 CB 00 26 00 01 00 12 01 50 00 13 01 34 02 DF 00  // ..&.....P...4...
                 13 01 B4 02 FB 00 13 01 DC 02 FF 00 13 01 08 03
                 0A 01 50 20 00 00 00 00 91 18 B8 00 22 00 01 00  // ..P ........"...
                 00 00 00 00 03 00 86 18 CF 00 34 00 01 00 00 00  // ..........4.....
                 00 00 03 00 C6 00 D5 00 3A 00 03 00 00 00 00 00  // ........:.......
                 03 00 C6 01 F7 00 3E 00 03 00 00 00 00 00 03 00  // ......>.........
                 C6 01 03 01 46 00 05 00 00 00 00 00 00 00 C6 05  // ....F...........
                 0D 01 4C 00 06 00 58 20 00 00 20 00 86 08 15 01  // ..L...X .. .....
                 54 00 06 00 71 20 00 00 20 00 86 08 1C 01 54 00  // T...q .. .....T.
                 07 00 8A 20 00 00 00 00 86 00 26 01 3A 00 08 00  // ... ......&.:...
                 8C 20 00 00 00 00 E6 01 0D 01 4C 00 08 00 9E 20  // . ........L.... 
                 00 00 00 00 86 18 CF 00 3A 00 08 00 A8 20 00 00  // ........:.... ..
                 00 00 96 00 28 01 22 00 08 00 70 22 00 00 00 00  // ....(."...p"....
                 96 00 2B 01 22 00 08 00 18 25 00 00 00 00 96 00  // ..+."....%......
                 2F 01 5A 00 08 00 7C 26 00 00 00 00 96 00 33 01  // /.Z...|&......3.
                 5F 00 09 00 80 27 00 00 00 00 96 00 37 01 67 00  // _....'......7.g.
                 0D 00 DD 27 00 00 00 00 86 18 CF 00 3A 00 0D 00  // ...'........:...
                 00 00 01 00 B3 01 00 00 02 00 BA 01 00 00 01 00
                 C1 01 00 00 02 00 B3 01 00 00 01 00 CA 01 00 00
                 01 00 D1 01 00 00 01 00 D1 01 00 00 01 00 0F 02
                 00 00 01 00 1C 03 00 00 02 00 21 03 00 00 03 00  // ..........!.....
                 26 03 00 00 04 00 2B 03 06 00 14 00 39 00 CF 00  // &.....+.....9...
                 6B 00 49 00 CF 00 71 00 51 00 E0 01 77 00 51 00  // k.I...q.Q...w.Q.
                 E8 01 77 00 21 00 CF 00 3A 00 59 00 CF 00 9B 00  // ..w.!...:.Y.....
                 69 00 05 02 A4 00 0C 00 CF 00 D9 00 71 00 90 02  // i...........q...
                 E3 00 14 00 CF 00 F4 00 14 00 F0 02 03 01 0C 00
                 F0 02 0E 01 69 00 05 02 61 01 91 00 CF 00 3A 00  // ....i...a.....:.
                 08 00 10 00 2A 00 08 00 14 00 2F 00 27 00 73 00  // ....*...../.'.s.
                 2F 00 2E 00 13 00 6B 01 08 00 06 00 72 01 01 00  // /.....k.....r...
                 10 00 00 00 09 00 01 00 24 00 00 00 0A 00 01 00  // ........$.......
                 24 00 00 00 0B 00 01 00 10 00 00 00 0C 00 80 00  // $...............
                 84 00 A9 00 14 01 43 01 66 01 06 00 01 00 00 00  // ......C.f.......
                 12 01 10 00 08 00 07 00 02 00 10 00 08 00 02 00
                 D1 00 EB 00 A8 24 00 00 07 00 B8 24 00 00 08 00  // .....$.....$....
                 E0 24 00 00 09 00 08 25 00 00 0A 00 04 80 00 00  // .$.....%........
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 AA 01
                 00 00 01 00 00 00 6B 09 00 00 00 00 00 00 01 00  // ......k.........
                 59 00 00 00 0A 00 09 00 08 00 0A 00 08 00 0B 00  // Y...............
                 08 00 0C 00 08 00 00 00 00 56 65 72 73 69 6F 6E  // .........Version
                 20 6F 66 20 72 75 6E 74 69 6D 65 20 61 67 61 69  //  of runtime agai
                 6E 73 74 20 77 68 69 63 68 20 74 68 65 20 62 69  // nst which the bi
                 6E 61 72 79 20 69 73 20 62 75 69 6C 74 20 3A 20  // nary is built : 
                 31 2E 30 2E 32 34 31 35 2E 30 00 3C 4D 6F 64 75  // 1.0.2415.0.<Modu
                 6C 65 3E 00 74 72 79 32 5F 73 61 76 2E 65 78 65  // le>.filter2.exe
                 00 6D 73 63 6F 72 6C 69 62 00 53 79 73 74 65 6D  // .mscorlib.System
                 00 56 61 6C 75 65 54 79 70 65 00 56 54 00 45 6E  // .ValueType.VT.En
                 75 6D 00 44 61 79 73 00 4D 75 6C 74 69 63 61 73  // um.Days.Multicas
                 74 44 65 6C 65 67 61 74 65 00 64 65 6C 65 67 61  // tDelegate.delega
                 74 65 31 00 49 54 65 73 74 00 4F 62 6A 65 63 74  // te1.ITest.Object
                 00 43 54 65 73 74 00 74 72 79 32 00 6D 00 6E 00  // .CTest.filter2.m.n.
                 2E 63 63 74 6F 72 00 76 61 6C 75 65 5F 5F 00 53  // .cctor.value__.S
                 75 6E 00 4D 6F 6E 00 2E 63 74 6F 72 00 49 6E 76  // un.Mon..ctor.Inv
                 6F 6B 65 00 49 41 73 79 6E 63 52 65 73 75 6C 74  // oke.IAsyncResult
                 00 41 73 79 6E 63 43 61 6C 6C 62 61 63 6B 00 42  // .AsyncCallback.B
                 65 67 69 6E 49 6E 76 6F 6B 65 00 45 6E 64 49 6E  // eginInvoke.EndIn
                 76 6F 6B 65 00 74 65 73 74 00 64 67 00 61 64 64  // voke.test.dg.add
                 5F 64 67 00 72 65 6D 6F 76 65 5F 64 67 00 66 00  // _dg.remove_dg.f.
                 66 31 00 66 31 31 00 66 32 35 00 66 32 36 00 4D  // f1.f11.f25.f26.M
                 61 69 6E 00 53 79 73 74 65 6D 2E 53 65 63 75 72  // ain.System.Secur
                 69 74 79 2E 50 65 72 6D 69 73 73 69 6F 6E 73 00  // ity.Permissions.
                 53 65 63 75 72 69 74 79 50 65 72 6D 69 73 73 69  // SecurityPermissi
                 6F 6E 41 74 74 72 69 62 75 74 65 00 53 65 63 75  // onAttribute.Secu
                 72 69 74 79 41 63 74 69 6F 6E 00 53 79 73 74 65  // rityAction.Syste
                 6D 2E 44 69 61 67 6E 6F 73 74 69 63 73 00 44 65  // m.Diagnostics.De
                 62 75 67 67 61 62 6C 65 41 74 74 72 69 62 75 74  // buggableAttribut
                 65 00 74 72 79 32 5F 73 61 76 00 6F 62 6A 65 63  // e.filter2.objec
                 74 00 6D 65 74 68 6F 64 00 63 61 6C 6C 62 61 63  // t.method.callbac
                 6B 00 72 65 73 75 6C 74 00 76 61 6C 75 65 00 44  // k.result.value.D
                 65 6C 65 67 61 74 65 00 43 6F 6D 62 69 6E 65 00  // elegate.Combine.
                 52 65 6D 6F 76 65 00 44 65 63 69 6D 61 6C 00 49  // Remove.Decimal.I
                 6E 74 33 32 00 43 6F 6E 73 6F 6C 65 00 57 72 69  // nt32.Console.Wri
                 74 65 4C 69 6E 65 00 61 72 67 00 3C 47 6C 6F 62  // teLine.arg.<Glob
                 61 6C 4E 61 6D 65 3E 00 24 24 73 74 72 75 63 74  // alName>.$$struct
                 30 78 36 30 30 30 30 30 65 2D 31 00 24 24 6D 65  // 0x600000e-1.$$me
                 74 68 6F 64 30 78 36 30 30 30 30 30 65 2D 31 00  // thod0x600000e-1.
                 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 43  // System.Runtime.C
                 6F 6D 70 69 6C 65 72 53 65 72 76 69 63 65 73 00  // ompilerServices.
                 52 75 6E 74 69 6D 65 48 65 6C 70 65 72 73 00 41  // RuntimeHelpers.A
                 72 72 61 79 00 52 75 6E 74 69 6D 65 46 69 65 6C  // rray.RuntimeFiel
                 64 48 61 6E 64 6C 65 00 49 6E 69 74 69 61 6C 69  // dHandle.Initiali
                 7A 65 41 72 72 61 79 00 24 24 73 74 72 75 63 74  // zeArray.$$struct
                 30 78 36 30 30 30 30 30 65 2D 32 00 24 24 6D 65  // 0x600000e-2.$$me
                 74 68 6F 64 30 78 36 30 30 30 30 30 65 2D 32 00  // thod0x600000e-2.
                 24 24 73 74 72 75 63 74 30 78 36 30 30 30 30 30  // $$struct0x600000
                 65 2D 33 00 24 24 6D 65 74 68 6F 64 30 78 36 30  // e-3.$$method0x60
                 30 30 30 30 65 2D 33 00 47 65 74 00 24 24 73 74  // 0000e-3.Get.$$st
                 72 75 63 74 30 78 36 30 30 30 30 30 65 2D 34 00  // ruct0x600000e-4.
                 24 24 6D 65 74 68 6F 64 30 78 36 30 30 30 30 30  // $$method0x600000
                 65 2D 34 00 61 72 67 31 00 61 72 67 32 00 61 72  // e-4.arg1.arg2.ar
                 67 33 00 61 72 67 34 00 53 74 72 69 6E 67 00 53  // g3.arg4.String.S
                 79 73 74 65 6D 2E 53 65 63 75 72 69 74 79 00 55  // ystem.Security.U
                 6E 76 65 72 69 66 69 61 62 6C 65 43 6F 64 65 41  // nverifiableCodeA
                 74 74 72 69 62 75 74 65 00 00 00 00 00 08 B7 7A  // ttribute.......z
                 5C 56 19 34 E0 89 14 37 FA 5D 33 C8 54 C6 0E DC  // \V.4...7.]3.T...
                 49 CF D9 83 91 36 9C 9C D8 9C 2B 02 06 08 03 00  // I....6....+.....
                 00 01 03 06 11 0C 04 00 00 00 00 04 01 00 00 00
                 05 20 02 01 1C 19 03 20 00 01 07 20 02 12 15 12  // . ..... ... ....
                 19 1C 05 20 01 01 12 15 03 20 00 08 03 06 12 10  // ... ..... ......
                 05 20 01 01 12 10 04 00 01 01 08 07 00 04 01 07  // . ..............
                 08 0E 1C 03 00 00 08 05 20 01 01 11 21 05 20 02  // ........ ...!. .
                 01 02 02 08 00 02 12 29 12 29 12 29 03 07 01 08  // .......).).)....
                 16 07 14 08 09 06 07 05 04 0A 0B 03 0C 0C 03 0B
                 0A 04 05 07 06 09 08 08 20 05 01 08 08 08 02 05  // ........ .......
                 04 00 01 01 1C 27 07 1A 0C 0D 02 11 2D 11 08 08  // .....'......-...
                 08 11 0C 1C 12 1C 0E 1D 08 1D 08 08 0E 12 1C 1C
                 11 0C 11 08 08 08 11 2D 02 0D 0C 1D 08 07 14 08  // .......-........
                 02 00 02 00 00 05 20 02 01 08 08 03 06 11 24 07  // ...... .......$.
                 00 02 01 12 3D 11 41 08 14 08 03 00 03 00 00 00  // ....=.A.........
                 06 20 03 01 08 08 08 03 06 11 28 03 06 11 2C 06  // . ........(...,.
                 20 03 08 08 08 08 03 06 11 30 05 20 02 08 08 08  //  ........0. ....
                 2E 07 0E 08 14 08 02 00 02 00 00 14 08 03 00 03  // ................
                 00 00 00 12 18 08 12 18 12 18 12 18 08 14 08 03
                 00 03 00 00 00 08 14 08 02 00 02 00 00 08 08 1D
                 07 17 07 08 0E 1C 06 05 0E 08 1D 0E 07 0F 07 07
                 05 0E 08 1D 0E 08 06 07 08 0E 1C 1D 0E 04 00 01
                 01 0E 04 07 02 02 08 06 01 00 00 01 00 00 81 F6
                 00 01 00 00 00 FF FF FF FF 01 00 00 00 00 00 00
                 00 02 01 00 00 00 1D 53 79 73 74 65 6D 2E 53 65  // .......System.Se
                 63 75 72 69 74 79 2E 50 65 72 6D 69 73 73 69 6F  // curity.Permissio
                 6E 53 65 74 04 00 00 00 0C 72 65 61 64 61 62 6C  // nSet.....readabl
                 65 6F 6E 6C 79 0E 6D 5F 55 6E 72 65 73 74 72 69  // eonly.m_Unrestri
                 63 74 65 64 15 6D 5F 75 6E 72 65 73 74 72 69 63  // cted.m_unrestric
                 74 65 64 50 65 72 6D 53 65 74 0F 6D 5F 6E 6F 72  // tedPermSet.m_nor
                 6D 61 6C 50 65 72 6D 53 65 74 01 00 09 02 00 00  // malPermSet......
                 00 09 03 00 00 00 02 02 00 00 00 22 53 79 73 74  // ..........."Syst
                 65 6D 2E 53 65 63 75 72 69 74 79 2E 55 74 69 6C  // em.Security.Util
                 2E 54 6F 6B 65 6E 42 61 73 65 64 53 65 74 05 00  // .TokenBasedSet..
                 00 00 08 6D 5F 6F 62 6A 53 65 74 06 6D 5F 63 45  // ...m_objSet.m_cE
                 6C 74 0A 6D 5F 69 6E 69 74 53 69 7A 65 0B 6D 5F  // lt.m_initSize.m_
                 69 6E 63 72 65 6D 65 6E 74 0A 6D 5F 6D 61 78 49  // increment.m_maxI
                 6E 64 65 78 09 04 00 00 00 01 00 00 00 10 00 00  // ndex............
                 00 08 00 00 00 06 00 00 00 01 03 00 00 00 02 00
                 00 00 09 05 00 00 00 00 00 00 00 08 00 00 00 08
                 00 00 00 FF FF FF FF 07 04 00 00 00 00 01 00 00
                 00 10 00 00 00 03 0D 53 79 73 74 65 6D 2E 4F 62  // .......System.Ob
                 6A 65 63 74 0A 0A 0A 0A 0A 0A 09 06 00 00 00 0A  // ject............
                 0A 0A 0A 0A 0A 0A 0A 0A 07 05 00 00 00 00 01 00
                 00 00 08 00 00 00 03 0D 53 79 73 74 65 6D 2E 4F  // ........System.O
                 62 6A 65 63 74 0A 0A 0A 0A 0A 0A 0A 0A 02 06 00  // bject...........
                 00 00 2E 53 79 73 74 65 6D 2E 53 65 63 75 72 69  // ...System.Securi
                 74 79 2E 50 65 72 6D 69 73 73 69 6F 6E 73 2E 53  // ty.Permissions.S
                 65 63 75 72 69 74 79 50 65 72 6D 69 73 73 69 6F  // ecurityPermissio
                 6E 01 00 00 00 07 6D 5F 66 6C 61 67 73 02 FF FF  // n.....m_flags...
                 FF FF 32 53 79 73 74 65 6D 2E 53 65 63 75 72 69  // ..2System.Securi
                 74 79 2E 50 65 72 6D 69 73 73 69 6F 6E 73 2E 53  // ty.Permissions.S
                 65 63 75 72 69 74 79 50 65 72 6D 69 73 73 69 6F  // ecurityPermissio
                 6E 46 6C 61 67 01 00 00 00 07 76 61 6C 75 65 5F  // nFlag.....value_
                 5F 04 00 00 00 0B 00 00 00 17 74 00 65 00 73 00  // _.........t.e.s.
                 74 00 20 00 73 00 74 00 72 00 69 00 6E 00 67 00  // t. .s.t.r.i.n.g.
                 00 17 73 00 74 00 72 00 69 00 6E 00 67 00 20 00  // ..s.t.r.i.n.g. .
                 74 00 65 00 73 00 74 00 00 07 66 00 32 00 38 00  // t.e.s.t...f.2.8.
                 00 03 61 00 00 03 62 00 00 07 66 00 32 00 36 00  // ..a...b...f.2.6.
                 00 0D 50 00 41 00 53 00 53 00 45 00 44 00 00 0D  // ..P.A.S.S.E.D...
                 46 00 41 00 49 00 4C 00 45 00 44 00 00 00 00 00  // F.A.I.L.E.D.....
                 B4 E2 E3 72 07 F7 D2 44 BF B9 3B CB 39 B4 CD 9E  // ...r...D..;.9...
                 D0 33 00 00 00 00 00 00 00 00 00 00 EE 33 00 00  // .3...........3..
                 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00  // . ..............
                 00 00 00 00 00 00 00 00 E0 33 00 00 00 00 00 00  // .........3......
                 00 00 00 00 00 00 00 00 00 00 5F 43 6F 72 45 78  // .........._CorEx
                 65 4D 61 69 6E 00 6D 73 63 6F 72 65 65 2E 64 6C  // eMain.mscoree.dl
                 6C 00 00 00 00 00 FF 25 00 20 40 00)             // l......%. @.
//*********** DISASSEMBLY COMPLETE ***********************
