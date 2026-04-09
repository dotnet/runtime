// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Test_test1
{
    public int func(int type)
    {
        try
        {
        }
        finally
        {
            switch (type)
            {
                case -433:
                    type = func(type);
                    break;
                case -660:
                case -5:
                case 524:
                case 537:
                case 818:
                case -115:
                case 576:
                    try
                    {
                    }
                    finally
                    {
                        switch (type)
                        {
                            case 174:
                            case -556:
                                type = func(type);
                                break;
                            case -363:
                                type = func(type);
                                break;
                            case -599:
                            case 57:
                            case 511:
                            case 465:
                            case 769:
                            case 146:
                            case 707:
                                type = func(type);
                                break;
                            case -107:
                            case -375:
                                type = func(type);
                                break;
                            case -40:
                                type = func(type);
                                break;
                            case -580:
                            case -883:
                            case 341:
                                type = func(type);
                                break;
                            case -608:
                            case -395:
                            case -886:
                            case 983:
                            case 687:
                            case 230:
                            case -838:
                            case 479:
                            case -687:
                                type = func(type);
                                break;
                            case 504:
                            case 578:
                            case 478:
                            case -494:
                                type = func(type);
                                break;
                            case -649:
                            case 547:
                                type = func(type);
                                break;
                            case 730:
                            case 659:
                                type = func(type);
                                break;
                            case -698:
                            case -868:
                            case -835:
                                type = func(type);
                                break;
                            case 217:
                            case 653:
                            case 613:
                            case -565:
                            case -731:
                            case 221:
                            case -793:
                            case -497:
                                type = func(type);
                                break;
                            case -948:
                            case 432:
                            case -478:
                                type = func(type);
                                break;
                            case -287:
                            case 826:
                            case 348:
                            case 986:
                            case 456:
                            case -589:
                            case -797:
                                type = func(type);
                                break;
                            default:
                                break;
                        }
                        switch (type)
                        {
                            case -881:
                            case 901:
                            case -392:
                            case 576:
                            case -451:
                                type = func(type);
                                break;
                            case -957:
                            case 513:
                            case 296:
                            case 74:
                            case -112:
                                type = func(type);
                                break;
                            case -198:
                            case 828:
                            case -842:
                                type = func(type);
                                break;
                            case 903:
                            case -689:
                            case -444:
                            case -155:
                            case 721:
                            case 962:
                            case -261:
                            case 802:
                                type = func(type);
                                break;
                            case -179:
                            case -40:
                            case -758:
                            case -750:
                            case -201:
                            case -58:
                            case 413:
                            case -103:
                                type = func(type);
                                break;
                            case 741:
                            case -474:
                            case 664:
                            case 98:
                            case -221:
                                type = func(type);
                                break;
                            default:
                                break;
                        }
                    }
                    type = func(type);
                    break;
                case -571:
                case -518:
                case 370:
                case -174:
                case 245:
                    type = func(type);
                    break;
                case 986:
                case 688:
                case -333:
                case -958:
                    type = func(type);
                    break;
                case 216:
                case -132:
                case -640:
                case 474:
                    type = func(type);
                    break;
                case 682:
                case -508:
                    type = func(type);
                    break;
                case 450:
                case -620:
                case -284:
                    type = func(type);
                    break;
                case -413:
                    type = func(type);
                    break;
                case 575:
                case -793:
                case 962:
                case 725:
                case -455:
                case -463:
                    type = func(type);
                    break;
                case 488:
                case -156:
                case 171:
                case 255:
                case 738:
                case 33:
                case -171:
                    type = func(type);
                    break;
                default:
                    break;
            }
        }
        return type;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Test_test1 test = new Test_test1();
        if (test.func(-1) == -1)
        {
            System.Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            System.Console.WriteLine("FAIL");
            return 1;
        }
    }
}
