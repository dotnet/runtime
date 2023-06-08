// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))

//permutations for  ((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))
//((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))
//(((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))
//(((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))+((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))))
//((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))
//((a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//a[6]
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(a[0]+(a[1]*a[2]))
//((a[1]*a[2])+a[0])
//a[0]
//(a[1]*a[2])
//(a[2]*a[1])
//a[1]
//a[2]
//(a[2]*a[1])
//(a[1]*a[2])
//((a[1]*a[2])+a[0])
//(a[0]+(a[1]*a[2]))
//(a[2]*a[3])
//(a[3]*a[2])
//a[2]
//a[3]
//(a[3]*a[2])
//(a[2]*a[3])
//(a[0]+(a[1]*a[2]))
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[2]+(a[4]*a[5]))
//((a[4]*a[5])+a[2])
//a[2]
//(a[4]*a[5])
//(a[5]*a[4])
//a[4]
//a[5]
//(a[5]*a[4])
//(a[4]*a[5])
//((a[4]*a[5])+a[2])
//(a[2]+(a[4]*a[5]))
//(a[6]*a[7])
//(a[7]*a[6])
//a[6]
//a[7]
//(a[7]*a[6])
//(a[6]*a[7])
//(a[2]+(a[4]*a[5]))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))
//(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+a[4])
//a[4]
//((a[8]+(a[10]*a[4]))-(a[2]*a[3]))
//(a[8]+(a[10]*a[4]))
//((a[10]*a[4])+a[8])
//a[8]
//(a[10]*a[4])
//(a[4]*a[10])
//a[10]
//a[4]
//(a[4]*a[10])
//(a[10]*a[4])
//((a[10]*a[4])+a[8])
//(a[8]+(a[10]*a[4]))
//(a[2]*a[3])
//(a[3]*a[2])
//a[2]
//a[3]
//(a[3]*a[2])
//(a[2]*a[3])
//(a[8]+(a[10]*a[4]))
//((a[8]+(a[10]*a[4]))-(a[2]*a[3]))
//(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+a[4])
//(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))
//(a[4]+(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))))
//(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+(a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))))
//(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))
//((a[8]+(a[10]*a[4]))-(a[2]*a[3]))
//(a[8]+(a[10]*a[4]))
//((a[10]*a[4])+a[8])
//a[8]
//(a[10]*a[4])
//(a[4]*a[10])
//a[10]
//a[4]
//(a[4]*a[10])
//(a[10]*a[4])
//((a[10]*a[4])+a[8])
//(a[8]+(a[10]*a[4]))
//(a[2]*a[3])
//(a[3]*a[2])
//a[2]
//a[3]
//(a[3]*a[2])
//(a[2]*a[3])
//(a[8]+(a[10]*a[4]))
//((a[8]+(a[10]*a[4]))-(a[2]*a[3]))
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//a[6]
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(a[0]+(a[1]*a[2]))
//((a[1]*a[2])+a[0])
//a[0]
//(a[1]*a[2])
//(a[2]*a[1])
//a[1]
//a[2]
//(a[2]*a[1])
//(a[1]*a[2])
//((a[1]*a[2])+a[0])
//(a[0]+(a[1]*a[2]))
//(a[2]*a[3])
//(a[3]*a[2])
//a[2]
//a[3]
//(a[3]*a[2])
//(a[2]*a[3])
//(a[0]+(a[1]*a[2]))
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[2]+(a[4]*a[5]))
//((a[4]*a[5])+a[2])
//a[2]
//(a[4]*a[5])
//(a[5]*a[4])
//a[4]
//a[5]
//(a[5]*a[4])
//(a[4]*a[5])
//((a[4]*a[5])+a[2])
//(a[2]+(a[4]*a[5]))
//(a[6]*a[7])
//(a[7]*a[6])
//a[6]
//a[7]
//(a[7]*a[6])
//(a[6]*a[7])
//(a[2]+(a[4]*a[5]))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))
//(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//(a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+a[4])
//a[4]
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//a[6]
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(a[0]+(a[1]*a[2]))
//((a[1]*a[2])+a[0])
//a[0]
//(a[1]*a[2])
//(a[2]*a[1])
//a[1]
//a[2]
//(a[2]*a[1])
//(a[1]*a[2])
//((a[1]*a[2])+a[0])
//(a[0]+(a[1]*a[2]))
//(a[2]*a[3])
//(a[3]*a[2])
//a[2]
//a[3]
//(a[3]*a[2])
//(a[2]*a[3])
//(a[0]+(a[1]*a[2]))
//((a[0]+(a[1]*a[2]))-(a[2]*a[3]))
//(((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[2]+(a[4]*a[5]))
//((a[4]*a[5])+a[2])
//a[2]
//(a[4]*a[5])
//(a[5]*a[4])
//a[4]
//a[5]
//(a[5]*a[4])
//(a[4]*a[5])
//((a[4]*a[5])+a[2])
//(a[2]+(a[4]*a[5]))
//(a[6]*a[7])
//(a[7]*a[6])
//a[6]
//a[7]
//(a[7]*a[6])
//(a[6]*a[7])
//(a[2]+(a[4]*a[5]))
//((a[2]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))
//((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+a[4])
//(a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//((a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))
//(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))
//(((a[5]+(a[4]*a[5]))-(a[6]*a[7]))+a[6])
//a[6]
//((a[5]+(a[4]*a[5]))-(a[6]*a[7]))
//(a[5]+(a[4]*a[5]))
//((a[4]*a[5])+a[5])
//a[5]
//(a[4]*a[5])
//(a[5]*a[4])
//a[4]
//a[5]
//(a[5]*a[4])
//(a[4]*a[5])
//((a[4]*a[5])+a[5])
//(a[5]+(a[4]*a[5]))
//(a[6]*a[7])
//(a[7]*a[6])
//a[6]
//a[7]
//(a[7]*a[6])
//(a[6]*a[7])
//(a[5]+(a[4]*a[5]))
//((a[5]+(a[4]*a[5]))-(a[6]*a[7]))
//(((a[5]+(a[4]*a[5]))-(a[6]*a[7]))+a[6])
//(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))
//(((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))
//((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))
//((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))
//((((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))*(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20]))))
//(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))
//(((a[7]+(a[5]+a[6]))-(a[15]*a[20]))+a[0])
//a[0]
//((a[7]+(a[5]+a[6]))-(a[15]*a[20]))
//(a[7]+(a[5]+a[6]))
//((a[5]+a[6])+a[7])
//a[7]
//(a[5]+a[6])
//(a[6]+a[5])
//a[5]
//a[6]
//(a[6]+a[5])
//(a[5]+a[6])
//(a[5]+(a[6]+a[7]))
//(a[6]+(a[5]+a[7]))
//(a[6]+a[7])
//(a[7]+a[6])
//a[6]
//a[7]
//(a[7]+a[6])
//(a[6]+a[7])
//(a[5]+a[7])
//(a[7]+a[5])
//a[5]
//a[7]
//(a[7]+a[5])
//(a[5]+a[7])
//((a[5]+a[6])+a[7])
//(a[7]+(a[5]+a[6]))
//(a[15]*a[20])
//(a[20]*a[15])
//a[15]
//a[20]
//(a[20]*a[15])
//(a[15]*a[20])
//(a[7]+(a[5]+a[6]))
//((a[7]+(a[5]+a[6]))-(a[15]*a[20]))
//(((a[7]+(a[5]+a[6]))-(a[15]*a[20]))+a[0])
//(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))
//(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))
//((a[0]+a[1])+a[6])
//(a[6]+(a[0]+a[1]))
//(a[0]+a[1])
//(a[1]+a[0])
//a[0]
//a[1]
//(a[1]+a[0])
//(a[0]+a[1])
//a[6]
//(a[0]+(a[1]+a[6]))
//(a[1]+(a[0]+a[6]))
//(a[1]+a[6])
//(a[6]+a[1])
//a[1]
//a[6]
//(a[6]+a[1])
//(a[1]+a[6])
//(a[0]+a[6])
//(a[6]+a[0])
//a[0]
//a[6]
//(a[6]+a[0])
//(a[0]+a[6])
//(a[6]+(a[0]+a[1]))
//((a[0]+a[1])+a[6])
//((a[2]+a[1])*a[10])
//(a[10]*(a[2]+a[1]))
//(a[2]+a[1])
//(a[1]+a[2])
//a[2]
//a[1]
//(a[1]+a[2])
//(a[2]+a[1])
//a[10]
//(a[10]*(a[2]+a[1]))
//((a[2]+a[1])*a[10])
//((a[0]+a[1])+a[6])
//(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))
//((((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))*(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20]))))
//((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))
//(((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))+((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))))
//(((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))
//(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10]))
//((a[1]*a[1])+a[6])
//(a[6]+(a[1]*a[1]))
//(a[1]*a[1])
//(a[1]*a[1])
//a[1]
//a[1]
//(a[1]*a[1])
//(a[1]*a[1])
//a[6]
//(a[6]+(a[1]*a[1]))
//((a[1]*a[1])+a[6])
//((a[2]+a[1])*a[10])
//(a[10]*(a[2]+a[1]))
//(a[2]+a[1])
//(a[1]+a[2])
//a[2]
//a[1]
//(a[1]+a[2])
//(a[2]+a[1])
//a[10]
//(a[10]*(a[2]+a[1]))
//((a[2]+a[1])*a[10])
//((a[1]*a[1])+a[6])
//(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10]))
//(((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))
//((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))
namespace CseTest
{
    using System;


    public class Test_Main
    {

        [Fact]
        public static int TestEntryPoint()
        {
            int ret = 100;
            int[] a = new int[50];
            a[6] = return_int(false, -106);
            a[0] = return_int(false, -45);
            a[1] = return_int(false, -56);
            a[2] = return_int(false, -26);
            a[3] = return_int(false, -109);
            a[4] = return_int(false, -17);
            a[5] = return_int(false, -79);
            a[7] = return_int(false, -66);
            a[8] = return_int(false, 22);
            a[10] = return_int(false, -149);
            a[15] = return_int(false, -32);
            a[20] = return_int(false, -135);
            int v;

#if LOOP
			do {
#endif
            v = ((((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))) + ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])))) - (((a[1] * a[1]) + a[6]) - ((a[2] + a[1]) * a[10])));
            if (v != 57525047)
            {
                Console.WriteLine("test0: for ((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))) + ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]))));
            if (v != 57515859)
            {
                Console.WriteLine("test1: for (((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[7] = return_int(false, -52);
#if LOOP
			for (int i = 0; i < 10; i++) {
#endif
            v = (((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]))) + ((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))));
            if (v != 57338941)
            {
                Console.WriteLine("test2: for (((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))+((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 159091)
            {
                Console.WriteLine("test3: for ((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				try {
#endif
            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3]))));
            if (v != 154737)
            {
                Console.WriteLine("test4: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				} finally {
#endif
            v = ((a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3]))) + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 154737)
            {
                Console.WriteLine("test5: for ((a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				}

#endif
            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 155033)
            {
                Console.WriteLine("test6: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 150838)
            {
                Console.WriteLine("test7: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 150838)
            {
                Console.WriteLine("test8: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -1423)
            {
                Console.WriteLine("test9: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != 1411)
            {
                Console.WriteLine("test10: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[2]) + a[0]);
            if (v != 1411)
            {
                Console.WriteLine("test11: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[2]);
            if (v != 1456)
            {
                Console.WriteLine("test12: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[1]);
            if (v != 1456)
            {
                Console.WriteLine("test13: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[1]);
            if (v != 1456)
            {
                Console.WriteLine("test14: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[2]);
            if (v != 1456)
            {
                Console.WriteLine("test15: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[2]) + a[0]);
            if (v != 1411)
            {
                Console.WriteLine("test16: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[7] = return_int(false, -52);
#if LOOP
			}
#endif

            a[3] = return_int(false, -56);
            v = (a[0] + (a[1] * a[2]));
            if (v != 1411)
            {
                Console.WriteLine("test17: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != 1456)
            {
                Console.WriteLine("test18: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != 1456)
            {
                Console.WriteLine("test19: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != 1456)
            {
                Console.WriteLine("test20: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != 1456)
            {
                Console.WriteLine("test21: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != 1411)
            {
                Console.WriteLine("test22: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -45)
            {
                Console.WriteLine("test23: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 4770)
            {
                Console.WriteLine("test24: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 4770)
            {
                Console.WriteLine("test25: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != -4195)
            {
                Console.WriteLine("test26: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 1317)
            {
                Console.WriteLine("test27: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 1317)
            {
                Console.WriteLine("test28: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 1343)
            {
                Console.WriteLine("test29: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 1343)
            {
                Console.WriteLine("test30: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 1343)
            {
                Console.WriteLine("test31: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 1343)
            {
                Console.WriteLine("test32: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 1317)
            {
                Console.WriteLine("test33: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 1317)
            {
                Console.WriteLine("test34: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[2] = return_int(false, 45);
            v = (a[6] * a[7]);
            if (v != 5512)
            {
                Console.WriteLine("test35: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 5512)
            {
                Console.WriteLine("test36: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 5512)
            {
                Console.WriteLine("test37: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 5512)
            {
                Console.WriteLine("test38: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 1388)
            {
                Console.WriteLine("test39: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != -4124)
            {
                Console.WriteLine("test40: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 4770)
            {
                Console.WriteLine("test41: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 8894)
            {
                Console.WriteLine("test42: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])));
            if (v != 5058)
            {
                Console.WriteLine("test43: for (a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + a[4]);
            if (v != 5058)
            {
                Console.WriteLine("test44: for (((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[8] + (a[10] * a[4])) - (a[2] * a[3]));
            if (v != 5075)
            {
                Console.WriteLine("test45: for ((a[8]+(a[10]*a[4]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test46: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[5] = return_int(false, -75);
            v = ((a[10] * a[4]) + a[8]);
            if (v != 2555)
            {
                Console.WriteLine("test47: for ((a[10]*a[4])+a[8])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * a[4]);
            if (v != 2533)
            {
                Console.WriteLine("test48: for (a[10]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[10]);
            if (v != 2533)
            {
                Console.WriteLine("test49: for (a[4]*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[10]);
            if (v != 2533)
            {
                Console.WriteLine("test50: for (a[4]*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * a[4]);
            if (v != 2533)
            {
                Console.WriteLine("test51: for (a[10]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[10] * a[4]) + a[8]);
            if (v != 2555)
            {
                Console.WriteLine("test52: for ((a[10]*a[4])+a[8])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test53: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test54: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test55: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test56: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test57: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test58: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[8] + (a[10] * a[4])) - (a[2] * a[3]));
            if (v != 5075)
            {
                Console.WriteLine("test59: for ((a[8]+(a[10]*a[4]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + a[4]);
            if (v != 5058)
            {
                Console.WriteLine("test60: for (((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])));
            if (v != 5058)
            {
                Console.WriteLine("test61: for (a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] + (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])))));
            if (v != 14020)
            {
                Console.WriteLine("test62: for (a[4]+(((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + (a[4] + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])))));
            if (v != 14020)
            {
                Console.WriteLine("test63: for (((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+(a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 14037)
            {
                Console.WriteLine("test64: for (((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])));
            if (v != 14037)
            {
                Console.WriteLine("test65: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[8] + (a[10] * a[4])) - (a[2] * a[3]));
            if (v != 5075)
            {
                Console.WriteLine("test66: for ((a[8]+(a[10]*a[4]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test67: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[10] * a[4]) + a[8]);
            if (v != 2555)
            {
                Console.WriteLine("test68: for ((a[10]*a[4])+a[8])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * a[4]);
            if (v != 2533)
            {
                Console.WriteLine("test69: for (a[10]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[15] = return_int(false, -34);
            v = (a[4] * a[10]);
            if (v != 2533)
            {
                Console.WriteLine("test70: for (a[4]*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[10]);
            if (v != 2533)
            {
                Console.WriteLine("test71: for (a[4]*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * a[4]);
            if (v != 2533)
            {
                Console.WriteLine("test72: for (a[10]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[10] * a[4]) + a[8]);
            if (v != 2555)
            {
                Console.WriteLine("test73: for ((a[10]*a[4])+a[8])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test74: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test75: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test76: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test77: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test78: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[8] + (a[10] * a[4]));
            if (v != 2555)
            {
                Console.WriteLine("test79: for (a[8]+(a[10]*a[4]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[8] + (a[10] * a[4])) - (a[2] * a[3]));
            if (v != 5075)
            {
                Console.WriteLine("test80: for ((a[8]+(a[10]*a[4]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 8962)
            {
                Console.WriteLine("test81: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 4770)
            {
                Console.WriteLine("test82: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 4770)
            {
                Console.WriteLine("test83: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -45)
            {
                Console.WriteLine("test84: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != -2565)
            {
                Console.WriteLine("test85: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[2]) + a[0]);
            if (v != -2565)
            {
                Console.WriteLine("test86: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test87: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[1]);
            if (v != -2520)
            {
                Console.WriteLine("test88: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[4] = return_int(false, 10);
            v = (a[2] * a[1]);
            if (v != -2520)
            {
                Console.WriteLine("test89: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test90: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -66);
            v = ((a[1] * a[2]) + a[0]);
            if (v != -2565)
            {
                Console.WriteLine("test91: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != -2565)
            {
                Console.WriteLine("test92: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[4] = return_int(false, -109);
            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test93: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test94: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test95: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test96: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -39);
            v = (a[0] + (a[1] * a[2]));
            if (v != -2565)
            {
                Console.WriteLine("test97: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -45)
            {
                Console.WriteLine("test98: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 4770)
            {
                Console.WriteLine("test99: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 4770)
            {
                Console.WriteLine("test100: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 2708)
            {
                Console.WriteLine("test101: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test102: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 8220)
            {
                Console.WriteLine("test103: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test104: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test105: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[6] = return_int(false, -83);
            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test106: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test107: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 8220)
            {
                Console.WriteLine("test108: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test109: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test110: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test111: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test112: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[0] = return_int(false, -75);
            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test113: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test114: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 3904)
            {
                Console.WriteLine("test115: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 6225)
            {
                Console.WriteLine("test116: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 2321)
            {
                Console.WriteLine("test117: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])));
            if (v != 21104)
            {
                Console.WriteLine("test118: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[8] + (a[10] * a[4])) - (a[2] * a[3])) + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 21104)
            {
                Console.WriteLine("test119: for (((a[8]+(a[10]*a[4]))-(a[2]*a[3]))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 2212)
            {
                Console.WriteLine("test120: for (a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + a[4]);
            if (v != 2212)
            {
                Console.WriteLine("test121: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 2321)
            {
                Console.WriteLine("test122: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 6225)
            {
                Console.WriteLine("test123: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 6225)
            {
                Console.WriteLine("test124: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -75)
            {
                Console.WriteLine("test125: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != -2595)
            {
                Console.WriteLine("test126: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[2]) + a[0]);
            if (v != -2595)
            {
                Console.WriteLine("test127: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test128: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[1]);
            if (v != -2520)
            {
                Console.WriteLine("test129: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[1]);
            if (v != -2520)
            {
                Console.WriteLine("test130: for (a[2]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[15] = return_int(false, -51);
            v = (a[1] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test131: for (a[1]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[2]) + a[0]);
            if (v != -2595)
            {
                Console.WriteLine("test132: for ((a[1]*a[2])+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != -2595)
            {
                Console.WriteLine("test133: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test134: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test135: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[3] * a[2]);
            if (v != -2520)
            {
                Console.WriteLine("test136: for (a[3]*a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] * a[3]);
            if (v != -2520)
            {
                Console.WriteLine("test137: for (a[2]*a[3])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] * a[2]));
            if (v != -2595)
            {
                Console.WriteLine("test138: for (a[0]+(a[1]*a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -93);
            a[3] = return_int(false, -19);
            v = ((a[0] + (a[1] * a[2])) - (a[2] * a[3]));
            if (v != -1740)
            {
                Console.WriteLine("test139: for ((a[0]+(a[1]*a[2]))-(a[2]*a[3]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + (a[1] * a[2])) - (a[2] * a[3])) * a[6]);
            if (v != 144420)
            {
                Console.WriteLine("test140: for (((a[0]+(a[1]*a[2]))-(a[2]*a[3]))*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 144420)
            {
                Console.WriteLine("test141: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 3904)
            {
                Console.WriteLine("test142: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test143: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 8220)
            {
                Console.WriteLine("test144: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test145: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test146: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test147: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test148: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[2]);
            if (v != 8220)
            {
                Console.WriteLine("test149: for ((a[4]*a[5])+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test150: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test151: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test152: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test153: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test154: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[4] * a[5]));
            if (v != 8220)
            {
                Console.WriteLine("test155: for (a[2]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 3904)
            {
                Console.WriteLine("test156: for ((a[2]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3])));
            if (v != 144420)
            {
                Console.WriteLine("test157: for (a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -64);
            v = ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 140516)
            {
                Console.WriteLine("test158: for ((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + a[4]);
            if (v != 140407)
            {
                Console.WriteLine("test159: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 140407)
            {
                Console.WriteLine("test160: for (a[4]+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3]))) + ((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 157525)
            {
                Console.WriteLine("test161: for ((a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3])))+((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3]))));
            if (v != 157525)
            {
                Console.WriteLine("test162: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 3701)
            {
                Console.WriteLine("test163: for (a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[15] = return_int(false, -73);
            v = (((a[5] + (a[4] * a[5])) - (a[6] * a[7])) + a[6]);
            if (v != 3701)
            {
                Console.WriteLine("test164: for (((a[5]+(a[4]*a[5]))-(a[6]*a[7]))+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[5] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 3784)
            {
                Console.WriteLine("test165: for ((a[5]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -194);
            v = (a[5] + (a[4] * a[5]));
            if (v != 8100)
            {
                Console.WriteLine("test166: for (a[5]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[5]);
            if (v != 8100)
            {
                Console.WriteLine("test167: for ((a[4]*a[5])+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test168: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test169: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] * a[4]);
            if (v != 8175)
            {
                Console.WriteLine("test170: for (a[5]*a[4])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[4] * a[5]);
            if (v != 8175)
            {
                Console.WriteLine("test171: for (a[4]*a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[4] * a[5]) + a[5]);
            if (v != 8100)
            {
                Console.WriteLine("test172: for ((a[4]*a[5])+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[15] = return_int(false, -193);
            v = (a[5] + (a[4] * a[5]));
            if (v != 8100)
            {
                Console.WriteLine("test173: for (a[5]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test174: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test175: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] * a[6]);
            if (v != 4316)
            {
                Console.WriteLine("test176: for (a[7]*a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] * a[7]);
            if (v != 4316)
            {
                Console.WriteLine("test177: for (a[6]*a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] + (a[4] * a[5]));
            if (v != 8100)
            {
                Console.WriteLine("test178: for (a[5]+(a[4]*a[5]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[5] + (a[4] * a[5])) - (a[6] * a[7]));
            if (v != 3784)
            {
                Console.WriteLine("test179: for ((a[5]+(a[4]*a[5]))-(a[6]*a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[5] + (a[4] * a[5])) - (a[6] * a[7])) + a[6]);
            if (v != 3701)
            {
                Console.WriteLine("test180: for (((a[5]+(a[4]*a[5]))-(a[6]*a[7]))+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])));
            if (v != 3701)
            {
                Console.WriteLine("test181: for (a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3]))));
            if (v != 157525)
            {
                Console.WriteLine("test182: for (((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7]))));
            if (v != 153824)
            {
                Console.WriteLine("test183: for ((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])));
            if (v != 69908131)
            {
                Console.WriteLine("test184: for ((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])) * (a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))));
            if (v != 69908131)
            {
                Console.WriteLine("test185: for ((((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))*(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20])));
            if (v != -37727)
            {
                Console.WriteLine("test186: for (a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[7] + (a[5] + a[6])) - (a[15] * a[20])) + a[0]);
            if (v != -37727)
            {
                Console.WriteLine("test187: for (((a[7]+(a[5]+a[6]))-(a[15]*a[20]))+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[7] + (a[5] + a[6])) - (a[15] * a[20]));
            if (v != -37652)
            {
                Console.WriteLine("test188: for ((a[7]+(a[5]+a[6]))-(a[15]*a[20]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + (a[5] + a[6]));
            if (v != -210)
            {
                Console.WriteLine("test189: for (a[7]+(a[5]+a[6]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[5] + a[6]) + a[7]);
            if (v != -210)
            {
                Console.WriteLine("test190: for ((a[5]+a[6])+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] + a[6]);
            if (v != -158)
            {
                Console.WriteLine("test191: for (a[5]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[5]);
            if (v != -158)
            {
                Console.WriteLine("test192: for (a[6]+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[5]);
            if (v != -158)
            {
                Console.WriteLine("test193: for (a[6]+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] + a[6]);
            if (v != -158)
            {
                Console.WriteLine("test194: for (a[5]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] + (a[6] + a[7]));
            if (v != -210)
            {
                Console.WriteLine("test195: for (a[5]+(a[6]+a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + (a[5] + a[7]));
            if (v != -210)
            {
                Console.WriteLine("test196: for (a[6]+(a[5]+a[7]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[7]);
            if (v != -135)
            {
                Console.WriteLine("test197: for (a[6]+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + a[6]);
            if (v != -135)
            {
                Console.WriteLine("test198: for (a[7]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + a[6]);
            if (v != -135)
            {
                Console.WriteLine("test199: for (a[7]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[7]);
            if (v != -135)
            {
                Console.WriteLine("test200: for (a[6]+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[0] = return_int(false, -54);
            v = (a[5] + a[7]);
            if (v != -127)
            {
                Console.WriteLine("test201: for (a[5]+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + a[5]);
            if (v != -127)
            {
                Console.WriteLine("test202: for (a[7]+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + a[5]);
            if (v != -127)
            {
                Console.WriteLine("test203: for (a[7]+a[5])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[5] + a[7]);
            if (v != -127)
            {
                Console.WriteLine("test204: for (a[5]+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[5] + a[6]) + a[7]);
            if (v != -210)
            {
                Console.WriteLine("test205: for ((a[5]+a[6])+a[7])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[7] + (a[5] + a[6]));
            if (v != -210)
            {
                Console.WriteLine("test206: for (a[7]+(a[5]+a[6]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[15] * a[20]);
            if (v != 37442)
            {
                Console.WriteLine("test207: for (a[15]*a[20])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[20] * a[15]);
            if (v != 37442)
            {
                Console.WriteLine("test208: for (a[20]*a[15])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[20] * a[15]);
            if (v != 37442)
            {
                Console.WriteLine("test209: for (a[20]*a[15])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[15] * a[20]);
            if (v != 37442)
            {
                Console.WriteLine("test210: for (a[15]*a[20])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[6] = return_int(false, -80);
            v = (a[7] + (a[5] + a[6]));
            if (v != -207)
            {
                Console.WriteLine("test211: for (a[7]+(a[5]+a[6]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[7] + (a[5] + a[6])) - (a[15] * a[20]));
            if (v != -37649)
            {
                Console.WriteLine("test212: for ((a[7]+(a[5]+a[6]))-(a[15]*a[20]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[1] = return_int(false, -11);
            v = (((a[7] + (a[5] + a[6])) - (a[15] * a[20])) + a[0]);
            if (v != -37703)
            {
                Console.WriteLine("test213: for (((a[7]+(a[5]+a[6]))-(a[15]*a[20]))+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[20] = return_int(false, -117);
            v = (a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20])));
            if (v != -22842)
            {
                Console.WriteLine("test214: for (a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]));
            if (v != 4921)
            {
                Console.WriteLine("test215: for (((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + a[1]) + a[6]);
            if (v != -145)
            {
                Console.WriteLine("test216: for ((a[0]+a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + (a[0] + a[1]));
            if (v != -145)
            {
                Console.WriteLine("test217: for (a[6]+(a[0]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + a[1]);
            if (v != -65)
            {
                Console.WriteLine("test218: for (a[0]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[0]);
            if (v != -65)
            {
                Console.WriteLine("test219: for (a[1]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[0]);
            if (v != -65)
            {
                Console.WriteLine("test220: for (a[1]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + a[1]);
            if (v != -65)
            {
                Console.WriteLine("test221: for (a[0]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[1] + a[6]));
            if (v != -145)
            {
                Console.WriteLine("test222: for (a[0]+(a[1]+a[6]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + (a[0] + a[6]));
            if (v != -145)
            {
                Console.WriteLine("test223: for (a[1]+(a[0]+a[6]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[6]);
            if (v != -91)
            {
                Console.WriteLine("test224: for (a[1]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[6] = return_int(false, -43);
            v = (a[6] + a[1]);
            if (v != -54)
            {
                Console.WriteLine("test225: for (a[6]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[1]);
            if (v != -54)
            {
                Console.WriteLine("test226: for (a[6]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[6]);
            if (v != -54)
            {
                Console.WriteLine("test227: for (a[1]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + a[6]);
            if (v != -97)
            {
                Console.WriteLine("test228: for (a[0]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[0]);
            if (v != -97)
            {
                Console.WriteLine("test229: for (a[6]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + a[0]);
            if (v != -97)
            {
                Console.WriteLine("test230: for (a[6]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + a[6]);
            if (v != -97)
            {
                Console.WriteLine("test231: for (a[0]+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + (a[0] + a[1]));
            if (v != -108)
            {
                Console.WriteLine("test232: for (a[6]+(a[0]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + a[1]) + a[6]);
            if (v != -108)
            {
                Console.WriteLine("test233: for ((a[0]+a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[6] = return_int(false, -34);
            v = ((a[2] + a[1]) * a[10]);
            if (v != -5066)
            {
                Console.WriteLine("test234: for ((a[2]+a[1])*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * (a[2] + a[1]));
            if (v != -5066)
            {
                Console.WriteLine("test235: for (a[10]*(a[2]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[1]);
            if (v != 34)
            {
                Console.WriteLine("test236: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[2]);
            if (v != 34)
            {
                Console.WriteLine("test237: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[2]);
            if (v != 34)
            {
                Console.WriteLine("test238: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[1]);
            if (v != 34)
            {
                Console.WriteLine("test239: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * (a[2] + a[1]));
            if (v != -5066)
            {
                Console.WriteLine("test240: for (a[10]*(a[2]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + a[1]) * a[10]);
            if (v != -5066)
            {
                Console.WriteLine("test241: for ((a[2]+a[1])*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[2] = return_int(false, -75);
            v = ((a[0] + a[1]) + a[6]);
            if (v != -99)
            {
                Console.WriteLine("test242: for ((a[0]+a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]));
            if (v != -12913)
            {
                Console.WriteLine("test243: for (((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])) * (a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))));
            if (v != 294364748)
            {
                Console.WriteLine("test244: for ((((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))*(a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])));
            if (v != 294364748)
            {
                Console.WriteLine("test245: for ((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]))) + ((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))));
            if (v != 294389083)
            {
                Console.WriteLine("test246: for (((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10])))+((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7])))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))) + ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]))));
            if (v != 294389083)
            {
                Console.WriteLine("test247: for (((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[1] * a[1]) + a[6]) - ((a[2] + a[1]) * a[10]));
            if (v != -12727)
            {
                Console.WriteLine("test248: for (((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[1]) + a[6]);
            if (v != 87)
            {
                Console.WriteLine("test249: for ((a[1]*a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + (a[1] * a[1]));
            if (v != 87)
            {
                Console.WriteLine("test250: for (a[6]+(a[1]*a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[1]);
            if (v != 121)
            {
                Console.WriteLine("test251: for (a[1]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[1]);
            if (v != 121)
            {
                Console.WriteLine("test252: for (a[1]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[1]);
            if (v != 121)
            {
                Console.WriteLine("test253: for (a[1]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] * a[1]);
            if (v != 121)
            {
                Console.WriteLine("test254: for (a[1]*a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[6] + (a[1] * a[1]));
            if (v != 87)
            {
                Console.WriteLine("test255: for (a[6]+(a[1]*a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[15] = return_int(false, -153);
            v = ((a[1] * a[1]) + a[6]);
            if (v != 87)
            {
                Console.WriteLine("test256: for ((a[1]*a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + a[1]) * a[10]);
            if (v != 12814)
            {
                Console.WriteLine("test257: for ((a[2]+a[1])*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * (a[2] + a[1]));
            if (v != 12814)
            {
                Console.WriteLine("test258: for (a[10]*(a[2]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[1]);
            if (v != -86)
            {
                Console.WriteLine("test259: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[2]);
            if (v != -86)
            {
                Console.WriteLine("test260: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP            

          do {
#endif
            v = (a[1] + a[2]);
            if (v != -86)
            {
                Console.WriteLine("test261: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
            } while (v==0);
#endif
            v = (a[2] + a[1]);
            if (v != -86)
            {
                Console.WriteLine("test262: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[10] * (a[2] + a[1]));
            if (v != 12814)
            {
                Console.WriteLine("test263: for (a[10]*(a[2]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[2] + a[1]) * a[10]);
            if (v != 12814)
            {
                Console.WriteLine("test264: for ((a[2]+a[1])*a[10])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[1] * a[1]) + a[6]);
            if (v != 87)
            {
                Console.WriteLine("test265: for ((a[1]*a[1])+a[6])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a[1] * a[1]) + a[6]) - ((a[2] + a[1]) * a[10]));
            if (v != -12727)
            {
                Console.WriteLine("test266: for (((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))) + ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10]))));
            if (v != 233956243)
            {
                Console.WriteLine("test267: for (((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((((a[6] * ((a[0] + (a[1] * a[2])) - (a[2] * a[3]))) - ((a[2] + (a[4] * a[5])) - (a[6] * a[7]))) + (a[4] + ((a[8] + (a[10] * a[4])) - (a[2] * a[3])))) - (a[6] + ((a[5] + (a[4] * a[5])) - (a[6] * a[7])))) + ((a[0] + ((a[7] + (a[5] + a[6])) - (a[15] * a[20]))) * (((a[0] + a[1]) + a[6]) - ((a[2] + a[1]) * a[10])))) - (((a[1] * a[1]) + a[6]) - ((a[2] + a[1]) * a[10])));
            if (v != 233968970)
            {
                Console.WriteLine("test268: for ((((((a[6]*((a[0]+(a[1]*a[2]))-(a[2]*a[3])))-((a[2]+(a[4]*a[5]))-(a[6]*a[7])))+(a[4]+((a[8]+(a[10]*a[4]))-(a[2]*a[3]))))-(a[6]+((a[5]+(a[4]*a[5]))-(a[6]*a[7]))))+((a[0]+((a[7]+(a[5]+a[6]))-(a[15]*a[20])))*(((a[0]+a[1])+a[6])-((a[2]+a[1])*a[10]))))-(((a[1]*a[1])+a[6])-((a[2]+a[1])*a[10])))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			} while (v == 0);
#endif
            Console.WriteLine(ret);
            return ret;
        }

        private static int return_int(bool verbose, int input)
        {
            int ans;
            try
            {
                ans = input;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning  : ans");
                }
            }
            return ans;
        }
    }
}

