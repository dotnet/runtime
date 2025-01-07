// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test loads a type with deeply nested generic classes (up to level 260)

using System;
using Xunit;

public class Test_NestedGenericClasses
{
   [Fact]
   public static int TestEntryPoint()
   {
      MyClass0<int>.MyClass1<int>.MyClass2<int>.MyClass3<int>.MyClass4<int>.MyClass5<int>.MyClass6<int>.MyClass7<int>.MyClass8<int>.MyClass9<int>.MyClass10<int>.MyClass11<int>.MyClass12<int>.MyClass13<int>.MyClass14<int>.MyClass15<int>.MyClass16<int>.MyClass17<int>.MyClass18<int>.MyClass19<int>.MyClass20<int>.MyClass21<int>.MyClass22<int>.MyClass23<int>.MyClass24<int>.MyClass25<int>.MyClass26<int>.MyClass27<int>.MyClass28<int>.MyClass29<int>.MyClass30<int>.MyClass31<int>.MyClass32<int>.MyClass33<int>.MyClass34<int>.MyClass35<int>.MyClass36<int>.MyClass37<int>.MyClass38<int>.MyClass39<int>.MyClass40<int>.MyClass41<int>.MyClass42<int>.MyClass43<int>.MyClass44<int>.MyClass45<int>.MyClass46<int>.MyClass47<int>.MyClass48<int>.MyClass49<int>.MyClass50<int>.MyClass51<int>.MyClass52<int>.MyClass53<int>.MyClass54<int>.MyClass55<int>.MyClass56<int>.MyClass57<int>.MyClass58<int>.MyClass59<int>.MyClass60<int>.MyClass61<int>.MyClass62<int>.MyClass63<int>.MyClass64<int>.MyClass65<int>.MyClass66<int>.MyClass67<int>.MyClass68<int>.MyClass69<int>.MyClass70<int>.MyClass71<int>.MyClass72<int>.MyClass73<int>.MyClass74<int>.MyClass75<int>.MyClass76<int>.MyClass77<int>.MyClass78<int>.MyClass79<int>.MyClass80<int>.MyClass81<int>.MyClass82<int>.MyClass83<int>.MyClass84<int>.MyClass85<int>.MyClass86<int>.MyClass87<int>.MyClass88<int>.MyClass89<int>.MyClass90<int>.MyClass91<int>.MyClass92<int>.MyClass93<int>.MyClass94<int>.MyClass95<int>.MyClass96<int>.MyClass97<int>.MyClass98<int>.MyClass99<int>.MyClass100<int>.MyClass101<int>.MyClass102<int>.MyClass103<int>.MyClass104<int>.MyClass105<int>.MyClass106<int>.MyClass107<int>.MyClass108<int>.MyClass109<int>.MyClass110<int>.MyClass111<int>.MyClass112<int>.MyClass113<int>.MyClass114<int>.MyClass115<int>.MyClass116<int>.MyClass117<int>.MyClass118<int>.MyClass119<int>.MyClass120<int>.MyClass121<int>.MyClass122<int>.MyClass123<int>.MyClass124<int>.MyClass125<int>.MyClass126<int>.MyClass127<int>.MyClass128<int>.MyClass129<int>.MyClass130<int>.MyClass131<int>.MyClass132<int>.MyClass133<int>.MyClass134<int>.MyClass135<int>.MyClass136<int>.MyClass137<int>.MyClass138<int>.MyClass139<int>.MyClass140<int>.MyClass141<int>.MyClass142<int>.MyClass143<int>.MyClass144<int>.MyClass145<int>.MyClass146<int>.MyClass147<int>.MyClass148<int>.MyClass149<int>.MyClass150<int>.MyClass151<int>.MyClass152<int>.MyClass153<int>.MyClass154<int>.MyClass155<int>.MyClass156<int>.MyClass157<int>.MyClass158<int>.MyClass159<int>.MyClass160<int>.MyClass161<int>.MyClass162<int>.MyClass163<int>.MyClass164<int>.MyClass165<int>.MyClass166<int>.MyClass167<int>.MyClass168<int>.MyClass169<int>.MyClass170<int>.MyClass171<int>.MyClass172<int>.MyClass173<int>.MyClass174<int>.MyClass175<int>.MyClass176<int>.MyClass177<int>.MyClass178<int>.MyClass179<int>.MyClass180<int>.MyClass181<int>.MyClass182<int>.MyClass183<int>.MyClass184<int>.MyClass185<int>.MyClass186<int>.MyClass187<int>.MyClass188<int>.MyClass189<int>.MyClass190<int>.MyClass191<int>.MyClass192<int>.MyClass193<int>.MyClass194<int>.MyClass195<int>.MyClass196<int>.MyClass197<int>.MyClass198<int>.MyClass199<int>.MyClass200<int>.MyClass201<int>.MyClass202<int>.MyClass203<int>.MyClass204<int>.MyClass205<int>.MyClass206<int>.MyClass207<int>.MyClass208<int>.MyClass209<int>.MyClass210<int>.MyClass211<int>.MyClass212<int>.MyClass213<int>.MyClass214<int>.MyClass215<int>.MyClass216<int>.MyClass217<int>.MyClass218<int>.MyClass219<int>.MyClass220<int>.MyClass221<int>.MyClass222<int>.MyClass223<int>.MyClass224<int>.MyClass225<int>.MyClass226<int>.MyClass227<int>.MyClass228<int>.MyClass229<int>.MyClass230<int>.MyClass231<int>.MyClass232<int>.MyClass233<int>.MyClass234<int>.MyClass235<int>.MyClass236<int>.MyClass237<int>.MyClass238<int>.MyClass239<int>.MyClass240<int>.MyClass241<int>.MyClass242<int>.MyClass243<int>.MyClass244<int>.MyClass245<int>.MyClass246<int>.MyClass247<int>.MyClass248<int>.MyClass249<int>.MyClass250<int>.MyClass251<int>.MyClass252<int>.MyClass253<int>.MyClass254<int>.MyClass255<int>.MyClass256<int>.MyClass257<int>.MyClass258<int>.MyClass259<int> obj = new MyClass0<int>.MyClass1<int>.MyClass2<int>.MyClass3<int>.MyClass4<int>.MyClass5<int>.MyClass6<int>.MyClass7<int>.MyClass8<int>.MyClass9<int>.MyClass10<int>.MyClass11<int>.MyClass12<int>.MyClass13<int>.MyClass14<int>.MyClass15<int>.MyClass16<int>.MyClass17<int>.MyClass18<int>.MyClass19<int>.MyClass20<int>.MyClass21<int>.MyClass22<int>.MyClass23<int>.MyClass24<int>.MyClass25<int>.MyClass26<int>.MyClass27<int>.MyClass28<int>.MyClass29<int>.MyClass30<int>.MyClass31<int>.MyClass32<int>.MyClass33<int>.MyClass34<int>.MyClass35<int>.MyClass36<int>.MyClass37<int>.MyClass38<int>.MyClass39<int>.MyClass40<int>.MyClass41<int>.MyClass42<int>.MyClass43<int>.MyClass44<int>.MyClass45<int>.MyClass46<int>.MyClass47<int>.MyClass48<int>.MyClass49<int>.MyClass50<int>.MyClass51<int>.MyClass52<int>.MyClass53<int>.MyClass54<int>.MyClass55<int>.MyClass56<int>.MyClass57<int>.MyClass58<int>.MyClass59<int>.MyClass60<int>.MyClass61<int>.MyClass62<int>.MyClass63<int>.MyClass64<int>.MyClass65<int>.MyClass66<int>.MyClass67<int>.MyClass68<int>.MyClass69<int>.MyClass70<int>.MyClass71<int>.MyClass72<int>.MyClass73<int>.MyClass74<int>.MyClass75<int>.MyClass76<int>.MyClass77<int>.MyClass78<int>.MyClass79<int>.MyClass80<int>.MyClass81<int>.MyClass82<int>.MyClass83<int>.MyClass84<int>.MyClass85<int>.MyClass86<int>.MyClass87<int>.MyClass88<int>.MyClass89<int>.MyClass90<int>.MyClass91<int>.MyClass92<int>.MyClass93<int>.MyClass94<int>.MyClass95<int>.MyClass96<int>.MyClass97<int>.MyClass98<int>.MyClass99<int>.MyClass100<int>.MyClass101<int>.MyClass102<int>.MyClass103<int>.MyClass104<int>.MyClass105<int>.MyClass106<int>.MyClass107<int>.MyClass108<int>.MyClass109<int>.MyClass110<int>.MyClass111<int>.MyClass112<int>.MyClass113<int>.MyClass114<int>.MyClass115<int>.MyClass116<int>.MyClass117<int>.MyClass118<int>.MyClass119<int>.MyClass120<int>.MyClass121<int>.MyClass122<int>.MyClass123<int>.MyClass124<int>.MyClass125<int>.MyClass126<int>.MyClass127<int>.MyClass128<int>.MyClass129<int>.MyClass130<int>.MyClass131<int>.MyClass132<int>.MyClass133<int>.MyClass134<int>.MyClass135<int>.MyClass136<int>.MyClass137<int>.MyClass138<int>.MyClass139<int>.MyClass140<int>.MyClass141<int>.MyClass142<int>.MyClass143<int>.MyClass144<int>.MyClass145<int>.MyClass146<int>.MyClass147<int>.MyClass148<int>.MyClass149<int>.MyClass150<int>.MyClass151<int>.MyClass152<int>.MyClass153<int>.MyClass154<int>.MyClass155<int>.MyClass156<int>.MyClass157<int>.MyClass158<int>.MyClass159<int>.MyClass160<int>.MyClass161<int>.MyClass162<int>.MyClass163<int>.MyClass164<int>.MyClass165<int>.MyClass166<int>.MyClass167<int>.MyClass168<int>.MyClass169<int>.MyClass170<int>.MyClass171<int>.MyClass172<int>.MyClass173<int>.MyClass174<int>.MyClass175<int>.MyClass176<int>.MyClass177<int>.MyClass178<int>.MyClass179<int>.MyClass180<int>.MyClass181<int>.MyClass182<int>.MyClass183<int>.MyClass184<int>.MyClass185<int>.MyClass186<int>.MyClass187<int>.MyClass188<int>.MyClass189<int>.MyClass190<int>.MyClass191<int>.MyClass192<int>.MyClass193<int>.MyClass194<int>.MyClass195<int>.MyClass196<int>.MyClass197<int>.MyClass198<int>.MyClass199<int>.MyClass200<int>.MyClass201<int>.MyClass202<int>.MyClass203<int>.MyClass204<int>.MyClass205<int>.MyClass206<int>.MyClass207<int>.MyClass208<int>.MyClass209<int>.MyClass210<int>.MyClass211<int>.MyClass212<int>.MyClass213<int>.MyClass214<int>.MyClass215<int>.MyClass216<int>.MyClass217<int>.MyClass218<int>.MyClass219<int>.MyClass220<int>.MyClass221<int>.MyClass222<int>.MyClass223<int>.MyClass224<int>.MyClass225<int>.MyClass226<int>.MyClass227<int>.MyClass228<int>.MyClass229<int>.MyClass230<int>.MyClass231<int>.MyClass232<int>.MyClass233<int>.MyClass234<int>.MyClass235<int>.MyClass236<int>.MyClass237<int>.MyClass238<int>.MyClass239<int>.MyClass240<int>.MyClass241<int>.MyClass242<int>.MyClass243<int>.MyClass244<int>.MyClass245<int>.MyClass246<int>.MyClass247<int>.MyClass248<int>.MyClass249<int>.MyClass250<int>.MyClass251<int>.MyClass252<int>.MyClass253<int>.MyClass254<int>.MyClass255<int>.MyClass256<int>.MyClass257<int>.MyClass258<int>.MyClass259<int>();

      Console.WriteLine("PASS");
      return 100;
   }
}

public class MyClass0<T0> {
public class MyClass1<T1> {
public class MyClass2<T2> {
public class MyClass3<T3> {
public class MyClass4<T4> {
public class MyClass5<T5> {
public class MyClass6<T6> {
public class MyClass7<T7> {
public class MyClass8<T8> {
public class MyClass9<T9> {
public class MyClass10<T10> {
public class MyClass11<T11> {
public class MyClass12<T12> {
public class MyClass13<T13> {
public class MyClass14<T14> {
public class MyClass15<T15> {
public class MyClass16<T16> {
public class MyClass17<T17> {
public class MyClass18<T18> {
public class MyClass19<T19> {
public class MyClass20<T20> {
public class MyClass21<T21> {
public class MyClass22<T22> {
public class MyClass23<T23> {
public class MyClass24<T24> {
public class MyClass25<T25> {
public class MyClass26<T26> {
public class MyClass27<T27> {
public class MyClass28<T28> {
public class MyClass29<T29> {
public class MyClass30<T30> {
public class MyClass31<T31> {
public class MyClass32<T32> {
public class MyClass33<T33> {
public class MyClass34<T34> {
public class MyClass35<T35> {
public class MyClass36<T36> {
public class MyClass37<T37> {
public class MyClass38<T38> {
public class MyClass39<T39> {
public class MyClass40<T40> {
public class MyClass41<T41> {
public class MyClass42<T42> {
public class MyClass43<T43> {
public class MyClass44<T44> {
public class MyClass45<T45> {
public class MyClass46<T46> {
public class MyClass47<T47> {
public class MyClass48<T48> {
public class MyClass49<T49> {
public class MyClass50<T50> {
public class MyClass51<T51> {
public class MyClass52<T52> {
public class MyClass53<T53> {
public class MyClass54<T54> {
public class MyClass55<T55> {
public class MyClass56<T56> {
public class MyClass57<T57> {
public class MyClass58<T58> {
public class MyClass59<T59> {
public class MyClass60<T60> {
public class MyClass61<T61> {
public class MyClass62<T62> {
public class MyClass63<T63> {
public class MyClass64<T64> {
public class MyClass65<T65> {
public class MyClass66<T66> {
public class MyClass67<T67> {
public class MyClass68<T68> {
public class MyClass69<T69> {
public class MyClass70<T70> {
public class MyClass71<T71> {
public class MyClass72<T72> {
public class MyClass73<T73> {
public class MyClass74<T74> {
public class MyClass75<T75> {
public class MyClass76<T76> {
public class MyClass77<T77> {
public class MyClass78<T78> {
public class MyClass79<T79> {
public class MyClass80<T80> {
public class MyClass81<T81> {
public class MyClass82<T82> {
public class MyClass83<T83> {
public class MyClass84<T84> {
public class MyClass85<T85> {
public class MyClass86<T86> {
public class MyClass87<T87> {
public class MyClass88<T88> {
public class MyClass89<T89> {
public class MyClass90<T90> {
public class MyClass91<T91> {
public class MyClass92<T92> {
public class MyClass93<T93> {
public class MyClass94<T94> {
public class MyClass95<T95> {
public class MyClass96<T96> {
public class MyClass97<T97> {
public class MyClass98<T98> {
public class MyClass99<T99> {
public class MyClass100<T100> {
public class MyClass101<T101> {
public class MyClass102<T102> {
public class MyClass103<T103> {
public class MyClass104<T104> {
public class MyClass105<T105> {
public class MyClass106<T106> {
public class MyClass107<T107> {
public class MyClass108<T108> {
public class MyClass109<T109> {
public class MyClass110<T110> {
public class MyClass111<T111> {
public class MyClass112<T112> {
public class MyClass113<T113> {
public class MyClass114<T114> {
public class MyClass115<T115> {
public class MyClass116<T116> {
public class MyClass117<T117> {
public class MyClass118<T118> {
public class MyClass119<T119> {
public class MyClass120<T120> {
public class MyClass121<T121> {
public class MyClass122<T122> {
public class MyClass123<T123> {
public class MyClass124<T124> {
public class MyClass125<T125> {
public class MyClass126<T126> {
public class MyClass127<T127> {
public class MyClass128<T128> {
public class MyClass129<T129> {
public class MyClass130<T130> {
public class MyClass131<T131> {
public class MyClass132<T132> {
public class MyClass133<T133> {
public class MyClass134<T134> {
public class MyClass135<T135> {
public class MyClass136<T136> {
public class MyClass137<T137> {
public class MyClass138<T138> {
public class MyClass139<T139> {
public class MyClass140<T140> {
public class MyClass141<T141> {
public class MyClass142<T142> {
public class MyClass143<T143> {
public class MyClass144<T144> {
public class MyClass145<T145> {
public class MyClass146<T146> {
public class MyClass147<T147> {
public class MyClass148<T148> {
public class MyClass149<T149> {
public class MyClass150<T150> {
public class MyClass151<T151> {
public class MyClass152<T152> {
public class MyClass153<T153> {
public class MyClass154<T154> {
public class MyClass155<T155> {
public class MyClass156<T156> {
public class MyClass157<T157> {
public class MyClass158<T158> {
public class MyClass159<T159> {
public class MyClass160<T160> {
public class MyClass161<T161> {
public class MyClass162<T162> {
public class MyClass163<T163> {
public class MyClass164<T164> {
public class MyClass165<T165> {
public class MyClass166<T166> {
public class MyClass167<T167> {
public class MyClass168<T168> {
public class MyClass169<T169> {
public class MyClass170<T170> {
public class MyClass171<T171> {
public class MyClass172<T172> {
public class MyClass173<T173> {
public class MyClass174<T174> {
public class MyClass175<T175> {
public class MyClass176<T176> {
public class MyClass177<T177> {
public class MyClass178<T178> {
public class MyClass179<T179> {
public class MyClass180<T180> {
public class MyClass181<T181> {
public class MyClass182<T182> {
public class MyClass183<T183> {
public class MyClass184<T184> {
public class MyClass185<T185> {
public class MyClass186<T186> {
public class MyClass187<T187> {
public class MyClass188<T188> {
public class MyClass189<T189> {
public class MyClass190<T190> {
public class MyClass191<T191> {
public class MyClass192<T192> {
public class MyClass193<T193> {
public class MyClass194<T194> {
public class MyClass195<T195> {
public class MyClass196<T196> {
public class MyClass197<T197> {
public class MyClass198<T198> {
public class MyClass199<T199> {
public class MyClass200<T200> {
public class MyClass201<T201> {
public class MyClass202<T202> {
public class MyClass203<T203> {
public class MyClass204<T204> {
public class MyClass205<T205> {
public class MyClass206<T206> {
public class MyClass207<T207> {
public class MyClass208<T208> {
public class MyClass209<T209> {
public class MyClass210<T210> {
public class MyClass211<T211> {
public class MyClass212<T212> {
public class MyClass213<T213> {
public class MyClass214<T214> {
public class MyClass215<T215> {
public class MyClass216<T216> {
public class MyClass217<T217> {
public class MyClass218<T218> {
public class MyClass219<T219> {
public class MyClass220<T220> {
public class MyClass221<T221> {
public class MyClass222<T222> {
public class MyClass223<T223> {
public class MyClass224<T224> {
public class MyClass225<T225> {
public class MyClass226<T226> {
public class MyClass227<T227> {
public class MyClass228<T228> {
public class MyClass229<T229> {
public class MyClass230<T230> {
public class MyClass231<T231> {
public class MyClass232<T232> {
public class MyClass233<T233> {
public class MyClass234<T234> {
public class MyClass235<T235> {
public class MyClass236<T236> {
public class MyClass237<T237> {
public class MyClass238<T238> {
public class MyClass239<T239> {
public class MyClass240<T240> {
public class MyClass241<T241> {
public class MyClass242<T242> {
public class MyClass243<T243> {
public class MyClass244<T244> {
public class MyClass245<T245> {
public class MyClass246<T246> {
public class MyClass247<T247> {
public class MyClass248<T248> {
public class MyClass249<T249> {
public class MyClass250<T250> {
public class MyClass251<T251> {
public class MyClass252<T252> {
public class MyClass253<T253> {
public class MyClass254<T254> {
public class MyClass255<T255> {
public class MyClass256<T256> {
public class MyClass257<T257> {
public class MyClass258<T258> {
public class MyClass259<T259> {
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
