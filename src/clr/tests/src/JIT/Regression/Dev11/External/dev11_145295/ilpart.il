// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.module extern CSharpPart.netmodule


.assembly extern mscorlib
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
    .ver 4:0:0:0
}


.assembly extern CSharpPart {}


.assembly ilpart {}


.module ilpart.dll
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class
    public abstract auto ansi sealed beforefieldinit
    Test.ILPart
        extends [mscorlib]System.Object
{

    .method
        public hidebysig
        static int32 CallThroughFrameWithMultipleEndfinallyOps_TopLevel
        (
            int32 calloutIndex
        )
        cil managed noinlining
    {

        .maxstack 8

        .locals init (int32 computedValue)


        ldc.i4      100
        stloc       computedValue


        .try
        {

            leave       END_OF_METHOD_BODY

        }
        finally
        {

            ldarg       calloutIndex

            switch      (RUN_CALLOUT_0,
                         RUN_CALLOUT_1,
                         RUN_CALLOUT_2,
                         RUN_CALLOUT_3,
                         RUN_CALLOUT_4,
                         RUN_CALLOUT_5,
                         RUN_CALLOUT_6)


        RUN_CALLOUT_0:
            ldc.i4      0
            stloc       computedValue
            endfinally

        RUN_CALLOUT_1:
            ldc.i4      1
            stloc       computedValue
            endfinally

        RUN_CALLOUT_2:
            ldc.i4      2
            stloc       computedValue
            endfinally

        RUN_CALLOUT_3:
            call        void [CSharpPart]Test.Helpers::Callout3()
            endfinally

        RUN_CALLOUT_4:
            ldc.i4      4
            stloc       computedValue
            call        void [CSharpPart]Test.Helpers::Callout4()
            endfinally

        RUN_CALLOUT_5:
            call        void [CSharpPart]Test.Helpers::Callout5()
            endfinally

        RUN_CALLOUT_6:
            call        void [CSharpPart]Test.Helpers::Callout6()
            endfinally

        }


    END_OF_METHOD_BODY:

        ldloc       computedValue
        ret
    }



    .method
        public hidebysig
        static int32 CallThroughFrameWithMultipleEndfinallyOps_Nested
        (
            int32 calloutIndex
        )
        cil managed noinlining
    {

        .maxstack 8

        .locals init (int32 computedValue)


        ldc.i4      100
        stloc       computedValue


        .try
        {

            call        void [CSharpPart]Test.Helpers::Throw()
            leave       END_OF_METHOD_BODY

        }
        catch [mscorlib]System.Exception
        {

            pop


            .try
            {

                leave       END_OF_CATCH_BLOCK

            }
            finally
            {

                ldarg       calloutIndex

                switch      (RUN_CALLOUT_0,
                             RUN_CALLOUT_1,
                             RUN_CALLOUT_2,
                             RUN_CALLOUT_3,
                             RUN_CALLOUT_4,
                             RUN_CALLOUT_5,
                             RUN_CALLOUT_6)


            RUN_CALLOUT_0:
                ldc.i4      0
                stloc       computedValue
                endfinally

            RUN_CALLOUT_1:
                ldc.i4      1
                stloc       computedValue
                endfinally

            RUN_CALLOUT_2:
                ldc.i4      2
                stloc       computedValue
                endfinally

            RUN_CALLOUT_3:
                call        void [CSharpPart]Test.Helpers::Callout3()
                endfinally

            RUN_CALLOUT_4:
                call        void [CSharpPart]Test.Helpers::Callout4()
                endfinally

            RUN_CALLOUT_5:
                call        void [CSharpPart]Test.Helpers::Callout5()
                endfinally

            RUN_CALLOUT_6:
                call        void [CSharpPart]Test.Helpers::Callout6()
                endfinally

            }


        END_OF_CATCH_BLOCK:

            leave       END_OF_METHOD_BODY

        }


    END_OF_METHOD_BODY:

        ldloc       computedValue
        ret
    }

}
