// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test loads a type with deeply nested generic structs and classes (up to level 500)

using System;
using Xunit;

public class Test_NestedGenericTypesMix
{
   [Fact]
   public static int TestEntryPoint()
   {
      MyStruct0<int>.MyClass1<int>.MyStruct2<int>.MyClass3<int>.MyStruct4<int>.MyClass5<int>.MyStruct6<int>.MyClass7<int>.MyStruct8<int>.MyClass9<int>.MyStruct10<int>.MyClass11<int>.MyStruct12<int>.MyClass13<int>.MyStruct14<int>.MyClass15<int>.MyStruct16<int>.MyClass17<int>.MyStruct18<int>.MyClass19<int>.MyStruct20<int>.MyClass21<int>.MyStruct22<int>.MyClass23<int>.MyStruct24<int>.MyClass25<int>.MyStruct26<int>.MyClass27<int>.MyStruct28<int>.MyClass29<int>.MyStruct30<int>.MyClass31<int>.MyStruct32<int>.MyClass33<int>.MyStruct34<int>.MyClass35<int>.MyStruct36<int>.MyClass37<int>.MyStruct38<int>.MyClass39<int>.MyStruct40<int>.MyClass41<int>.MyStruct42<int>.MyClass43<int>.MyStruct44<int>.MyClass45<int>.MyStruct46<int>.MyClass47<int>.MyStruct48<int>.MyClass49<int>.MyStruct50<int>.MyClass51<int>.MyStruct52<int>.MyClass53<int>.MyStruct54<int>.MyClass55<int>.MyStruct56<int>.MyClass57<int>.MyStruct58<int>.MyClass59<int>.MyStruct60<int>.MyClass61<int>.MyStruct62<int>.MyClass63<int>.MyStruct64<int>.MyClass65<int>.MyStruct66<int>.MyClass67<int>.MyStruct68<int>.MyClass69<int>.MyStruct70<int>.MyClass71<int>.MyStruct72<int>.MyClass73<int>.MyStruct74<int>.MyClass75<int>.MyStruct76<int>.MyClass77<int>.MyStruct78<int>.MyClass79<int>.MyStruct80<int>.MyClass81<int>.MyStruct82<int>.MyClass83<int>.MyStruct84<int>.MyClass85<int>.MyStruct86<int>.MyClass87<int>.MyStruct88<int>.MyClass89<int>.MyStruct90<int>.MyClass91<int>.MyStruct92<int>.MyClass93<int>.MyStruct94<int>.MyClass95<int>.MyStruct96<int>.MyClass97<int>.MyStruct98<int>.MyClass99<int>.MyStruct100<int>.MyClass101<int>.MyStruct102<int>.MyClass103<int>.MyStruct104<int>.MyClass105<int>.MyStruct106<int>.MyClass107<int>.MyStruct108<int>.MyClass109<int>.MyStruct110<int>.MyClass111<int>.MyStruct112<int>.MyClass113<int>.MyStruct114<int>.MyClass115<int>.MyStruct116<int>.MyClass117<int>.MyStruct118<int>.MyClass119<int>.MyStruct120<int>.MyClass121<int>.MyStruct122<int>.MyClass123<int>.MyStruct124<int>.MyClass125<int>.MyStruct126<int>.MyClass127<int>.MyStruct128<int>.MyClass129<int>.MyStruct130<int>.MyClass131<int>.MyStruct132<int>.MyClass133<int>.MyStruct134<int>.MyClass135<int>.MyStruct136<int>.MyClass137<int>.MyStruct138<int>.MyClass139<int>.MyStruct140<int>.MyClass141<int>.MyStruct142<int>.MyClass143<int>.MyStruct144<int>.MyClass145<int>.MyStruct146<int>.MyClass147<int>.MyStruct148<int>.MyClass149<int>.MyStruct150<int>.MyClass151<int>.MyStruct152<int>.MyClass153<int>.MyStruct154<int>.MyClass155<int>.MyStruct156<int>.MyClass157<int>.MyStruct158<int>.MyClass159<int>.MyStruct160<int>.MyClass161<int>.MyStruct162<int>.MyClass163<int>.MyStruct164<int>.MyClass165<int>.MyStruct166<int>.MyClass167<int>.MyStruct168<int>.MyClass169<int>.MyStruct170<int>.MyClass171<int>.MyStruct172<int>.MyClass173<int>.MyStruct174<int>.MyClass175<int>.MyStruct176<int>.MyClass177<int>.MyStruct178<int>.MyClass179<int>.MyStruct180<int>.MyClass181<int>.MyStruct182<int>.MyClass183<int>.MyStruct184<int>.MyClass185<int>.MyStruct186<int>.MyClass187<int>.MyStruct188<int>.MyClass189<int>.MyStruct190<int>.MyClass191<int>.MyStruct192<int>.MyClass193<int>.MyStruct194<int>.MyClass195<int>.MyStruct196<int>.MyClass197<int>.MyStruct198<int>.MyClass199<int>.MyStruct200<int>.MyClass201<int>.MyStruct202<int>.MyClass203<int>.MyStruct204<int>.MyClass205<int>.MyStruct206<int>.MyClass207<int>.MyStruct208<int>.MyClass209<int>.MyStruct210<int>.MyClass211<int>.MyStruct212<int>.MyClass213<int>.MyStruct214<int>.MyClass215<int>.MyStruct216<int>.MyClass217<int>.MyStruct218<int>.MyClass219<int>.MyStruct220<int>.MyClass221<int>.MyStruct222<int>.MyClass223<int>.MyStruct224<int>.MyClass225<int>.MyStruct226<int>.MyClass227<int>.MyStruct228<int>.MyClass229<int>.MyStruct230<int>.MyClass231<int>.MyStruct232<int>.MyClass233<int>.MyStruct234<int>.MyClass235<int>.MyStruct236<int>.MyClass237<int>.MyStruct238<int>.MyClass239<int>.MyStruct240<int>.MyClass241<int>.MyStruct242<int>.MyClass243<int>.MyStruct244<int>.MyClass245<int>.MyStruct246<int>.MyClass247<int>.MyStruct248<int>.MyClass249<int>.MyStruct250<int>.MyClass251<int>.MyStruct252<int>.MyClass253<int>.MyStruct254<int>.MyClass255<int>.MyStruct256<int>.MyClass257<int>.MyStruct258<int>.MyClass259<int>.MyStruct260<int>.MyClass261<int>.MyStruct262<int>.MyClass263<int>.MyStruct264<int>.MyClass265<int>.MyStruct266<int>.MyClass267<int>.MyStruct268<int>.MyClass269<int>.MyStruct270<int>.MyClass271<int>.MyStruct272<int>.MyClass273<int>.MyStruct274<int>.MyClass275<int>.MyStruct276<int>.MyClass277<int>.MyStruct278<int>.MyClass279<int>.MyStruct280<int>.MyClass281<int>.MyStruct282<int>.MyClass283<int>.MyStruct284<int>.MyClass285<int>.MyStruct286<int>.MyClass287<int>.MyStruct288<int>.MyClass289<int>.MyStruct290<int>.MyClass291<int>.MyStruct292<int>.MyClass293<int>.MyStruct294<int>.MyClass295<int>.MyStruct296<int>.MyClass297<int>.MyStruct298<int>.MyClass299<int>.MyStruct300<int>.MyClass301<int>.MyStruct302<int>.MyClass303<int>.MyStruct304<int>.MyClass305<int>.MyStruct306<int>.MyClass307<int>.MyStruct308<int>.MyClass309<int>.MyStruct310<int>.MyClass311<int>.MyStruct312<int>.MyClass313<int>.MyStruct314<int>.MyClass315<int>.MyStruct316<int>.MyClass317<int>.MyStruct318<int>.MyClass319<int>.MyStruct320<int>.MyClass321<int>.MyStruct322<int>.MyClass323<int>.MyStruct324<int>.MyClass325<int>.MyStruct326<int>.MyClass327<int>.MyStruct328<int>.MyClass329<int>.MyStruct330<int>.MyClass331<int>.MyStruct332<int>.MyClass333<int>.MyStruct334<int>.MyClass335<int>.MyStruct336<int>.MyClass337<int>.MyStruct338<int>.MyClass339<int>.MyStruct340<int>.MyClass341<int>.MyStruct342<int>.MyClass343<int>.MyStruct344<int>.MyClass345<int>.MyStruct346<int>.MyClass347<int>.MyStruct348<int>.MyClass349<int>.MyStruct350<int>.MyClass351<int>.MyStruct352<int>.MyClass353<int>.MyStruct354<int>.MyClass355<int>.MyStruct356<int>.MyClass357<int>.MyStruct358<int>.MyClass359<int>.MyStruct360<int>.MyClass361<int>.MyStruct362<int>.MyClass363<int>.MyStruct364<int>.MyClass365<int>.MyStruct366<int>.MyClass367<int>.MyStruct368<int>.MyClass369<int>.MyStruct370<int>.MyClass371<int>.MyStruct372<int>.MyClass373<int>.MyStruct374<int>.MyClass375<int>.MyStruct376<int>.MyClass377<int>.MyStruct378<int>.MyClass379<int>.MyStruct380<int>.MyClass381<int>.MyStruct382<int>.MyClass383<int>.MyStruct384<int>.MyClass385<int>.MyStruct386<int>.MyClass387<int>.MyStruct388<int>.MyClass389<int>.MyStruct390<int>.MyClass391<int>.MyStruct392<int>.MyClass393<int>.MyStruct394<int>.MyClass395<int>.MyStruct396<int>.MyClass397<int>.MyStruct398<int>.MyClass399<int>.MyStruct400<int>.MyClass401<int>.MyStruct402<int>.MyClass403<int>.MyStruct404<int>.MyClass405<int>.MyStruct406<int>.MyClass407<int>.MyStruct408<int>.MyClass409<int>.MyStruct410<int>.MyClass411<int>.MyStruct412<int>.MyClass413<int>.MyStruct414<int>.MyClass415<int>.MyStruct416<int>.MyClass417<int>.MyStruct418<int>.MyClass419<int>.MyStruct420<int>.MyClass421<int>.MyStruct422<int>.MyClass423<int>.MyStruct424<int>.MyClass425<int>.MyStruct426<int>.MyClass427<int>.MyStruct428<int>.MyClass429<int>.MyStruct430<int>.MyClass431<int>.MyStruct432<int>.MyClass433<int>.MyStruct434<int>.MyClass435<int>.MyStruct436<int>.MyClass437<int>.MyStruct438<int>.MyClass439<int>.MyStruct440<int>.MyClass441<int>.MyStruct442<int>.MyClass443<int>.MyStruct444<int>.MyClass445<int>.MyStruct446<int>.MyClass447<int>.MyStruct448<int>.MyClass449<int>.MyStruct450<int>.MyClass451<int>.MyStruct452<int>.MyClass453<int>.MyStruct454<int>.MyClass455<int>.MyStruct456<int>.MyClass457<int>.MyStruct458<int>.MyClass459<int>.MyStruct460<int>.MyClass461<int>.MyStruct462<int>.MyClass463<int>.MyStruct464<int>.MyClass465<int>.MyStruct466<int>.MyClass467<int>.MyStruct468<int>.MyClass469<int>.MyStruct470<int>.MyClass471<int>.MyStruct472<int>.MyClass473<int>.MyStruct474<int>.MyClass475<int>.MyStruct476<int>.MyClass477<int>.MyStruct478<int>.MyClass479<int>.MyStruct480<int>.MyClass481<int>.MyStruct482<int>.MyClass483<int>.MyStruct484<int>.MyClass485<int>.MyStruct486<int>.MyClass487<int>.MyStruct488<int>.MyClass489<int>.MyStruct490<int>.MyClass491<int>.MyStruct492<int>.MyClass493<int>.MyStruct494<int>.MyClass495<int>.MyStruct496<int>.MyClass497<int>.MyStruct498<int>.MyClass499<int> obj = new MyStruct0<int>.MyClass1<int>.MyStruct2<int>.MyClass3<int>.MyStruct4<int>.MyClass5<int>.MyStruct6<int>.MyClass7<int>.MyStruct8<int>.MyClass9<int>.MyStruct10<int>.MyClass11<int>.MyStruct12<int>.MyClass13<int>.MyStruct14<int>.MyClass15<int>.MyStruct16<int>.MyClass17<int>.MyStruct18<int>.MyClass19<int>.MyStruct20<int>.MyClass21<int>.MyStruct22<int>.MyClass23<int>.MyStruct24<int>.MyClass25<int>.MyStruct26<int>.MyClass27<int>.MyStruct28<int>.MyClass29<int>.MyStruct30<int>.MyClass31<int>.MyStruct32<int>.MyClass33<int>.MyStruct34<int>.MyClass35<int>.MyStruct36<int>.MyClass37<int>.MyStruct38<int>.MyClass39<int>.MyStruct40<int>.MyClass41<int>.MyStruct42<int>.MyClass43<int>.MyStruct44<int>.MyClass45<int>.MyStruct46<int>.MyClass47<int>.MyStruct48<int>.MyClass49<int>.MyStruct50<int>.MyClass51<int>.MyStruct52<int>.MyClass53<int>.MyStruct54<int>.MyClass55<int>.MyStruct56<int>.MyClass57<int>.MyStruct58<int>.MyClass59<int>.MyStruct60<int>.MyClass61<int>.MyStruct62<int>.MyClass63<int>.MyStruct64<int>.MyClass65<int>.MyStruct66<int>.MyClass67<int>.MyStruct68<int>.MyClass69<int>.MyStruct70<int>.MyClass71<int>.MyStruct72<int>.MyClass73<int>.MyStruct74<int>.MyClass75<int>.MyStruct76<int>.MyClass77<int>.MyStruct78<int>.MyClass79<int>.MyStruct80<int>.MyClass81<int>.MyStruct82<int>.MyClass83<int>.MyStruct84<int>.MyClass85<int>.MyStruct86<int>.MyClass87<int>.MyStruct88<int>.MyClass89<int>.MyStruct90<int>.MyClass91<int>.MyStruct92<int>.MyClass93<int>.MyStruct94<int>.MyClass95<int>.MyStruct96<int>.MyClass97<int>.MyStruct98<int>.MyClass99<int>.MyStruct100<int>.MyClass101<int>.MyStruct102<int>.MyClass103<int>.MyStruct104<int>.MyClass105<int>.MyStruct106<int>.MyClass107<int>.MyStruct108<int>.MyClass109<int>.MyStruct110<int>.MyClass111<int>.MyStruct112<int>.MyClass113<int>.MyStruct114<int>.MyClass115<int>.MyStruct116<int>.MyClass117<int>.MyStruct118<int>.MyClass119<int>.MyStruct120<int>.MyClass121<int>.MyStruct122<int>.MyClass123<int>.MyStruct124<int>.MyClass125<int>.MyStruct126<int>.MyClass127<int>.MyStruct128<int>.MyClass129<int>.MyStruct130<int>.MyClass131<int>.MyStruct132<int>.MyClass133<int>.MyStruct134<int>.MyClass135<int>.MyStruct136<int>.MyClass137<int>.MyStruct138<int>.MyClass139<int>.MyStruct140<int>.MyClass141<int>.MyStruct142<int>.MyClass143<int>.MyStruct144<int>.MyClass145<int>.MyStruct146<int>.MyClass147<int>.MyStruct148<int>.MyClass149<int>.MyStruct150<int>.MyClass151<int>.MyStruct152<int>.MyClass153<int>.MyStruct154<int>.MyClass155<int>.MyStruct156<int>.MyClass157<int>.MyStruct158<int>.MyClass159<int>.MyStruct160<int>.MyClass161<int>.MyStruct162<int>.MyClass163<int>.MyStruct164<int>.MyClass165<int>.MyStruct166<int>.MyClass167<int>.MyStruct168<int>.MyClass169<int>.MyStruct170<int>.MyClass171<int>.MyStruct172<int>.MyClass173<int>.MyStruct174<int>.MyClass175<int>.MyStruct176<int>.MyClass177<int>.MyStruct178<int>.MyClass179<int>.MyStruct180<int>.MyClass181<int>.MyStruct182<int>.MyClass183<int>.MyStruct184<int>.MyClass185<int>.MyStruct186<int>.MyClass187<int>.MyStruct188<int>.MyClass189<int>.MyStruct190<int>.MyClass191<int>.MyStruct192<int>.MyClass193<int>.MyStruct194<int>.MyClass195<int>.MyStruct196<int>.MyClass197<int>.MyStruct198<int>.MyClass199<int>.MyStruct200<int>.MyClass201<int>.MyStruct202<int>.MyClass203<int>.MyStruct204<int>.MyClass205<int>.MyStruct206<int>.MyClass207<int>.MyStruct208<int>.MyClass209<int>.MyStruct210<int>.MyClass211<int>.MyStruct212<int>.MyClass213<int>.MyStruct214<int>.MyClass215<int>.MyStruct216<int>.MyClass217<int>.MyStruct218<int>.MyClass219<int>.MyStruct220<int>.MyClass221<int>.MyStruct222<int>.MyClass223<int>.MyStruct224<int>.MyClass225<int>.MyStruct226<int>.MyClass227<int>.MyStruct228<int>.MyClass229<int>.MyStruct230<int>.MyClass231<int>.MyStruct232<int>.MyClass233<int>.MyStruct234<int>.MyClass235<int>.MyStruct236<int>.MyClass237<int>.MyStruct238<int>.MyClass239<int>.MyStruct240<int>.MyClass241<int>.MyStruct242<int>.MyClass243<int>.MyStruct244<int>.MyClass245<int>.MyStruct246<int>.MyClass247<int>.MyStruct248<int>.MyClass249<int>.MyStruct250<int>.MyClass251<int>.MyStruct252<int>.MyClass253<int>.MyStruct254<int>.MyClass255<int>.MyStruct256<int>.MyClass257<int>.MyStruct258<int>.MyClass259<int>.MyStruct260<int>.MyClass261<int>.MyStruct262<int>.MyClass263<int>.MyStruct264<int>.MyClass265<int>.MyStruct266<int>.MyClass267<int>.MyStruct268<int>.MyClass269<int>.MyStruct270<int>.MyClass271<int>.MyStruct272<int>.MyClass273<int>.MyStruct274<int>.MyClass275<int>.MyStruct276<int>.MyClass277<int>.MyStruct278<int>.MyClass279<int>.MyStruct280<int>.MyClass281<int>.MyStruct282<int>.MyClass283<int>.MyStruct284<int>.MyClass285<int>.MyStruct286<int>.MyClass287<int>.MyStruct288<int>.MyClass289<int>.MyStruct290<int>.MyClass291<int>.MyStruct292<int>.MyClass293<int>.MyStruct294<int>.MyClass295<int>.MyStruct296<int>.MyClass297<int>.MyStruct298<int>.MyClass299<int>.MyStruct300<int>.MyClass301<int>.MyStruct302<int>.MyClass303<int>.MyStruct304<int>.MyClass305<int>.MyStruct306<int>.MyClass307<int>.MyStruct308<int>.MyClass309<int>.MyStruct310<int>.MyClass311<int>.MyStruct312<int>.MyClass313<int>.MyStruct314<int>.MyClass315<int>.MyStruct316<int>.MyClass317<int>.MyStruct318<int>.MyClass319<int>.MyStruct320<int>.MyClass321<int>.MyStruct322<int>.MyClass323<int>.MyStruct324<int>.MyClass325<int>.MyStruct326<int>.MyClass327<int>.MyStruct328<int>.MyClass329<int>.MyStruct330<int>.MyClass331<int>.MyStruct332<int>.MyClass333<int>.MyStruct334<int>.MyClass335<int>.MyStruct336<int>.MyClass337<int>.MyStruct338<int>.MyClass339<int>.MyStruct340<int>.MyClass341<int>.MyStruct342<int>.MyClass343<int>.MyStruct344<int>.MyClass345<int>.MyStruct346<int>.MyClass347<int>.MyStruct348<int>.MyClass349<int>.MyStruct350<int>.MyClass351<int>.MyStruct352<int>.MyClass353<int>.MyStruct354<int>.MyClass355<int>.MyStruct356<int>.MyClass357<int>.MyStruct358<int>.MyClass359<int>.MyStruct360<int>.MyClass361<int>.MyStruct362<int>.MyClass363<int>.MyStruct364<int>.MyClass365<int>.MyStruct366<int>.MyClass367<int>.MyStruct368<int>.MyClass369<int>.MyStruct370<int>.MyClass371<int>.MyStruct372<int>.MyClass373<int>.MyStruct374<int>.MyClass375<int>.MyStruct376<int>.MyClass377<int>.MyStruct378<int>.MyClass379<int>.MyStruct380<int>.MyClass381<int>.MyStruct382<int>.MyClass383<int>.MyStruct384<int>.MyClass385<int>.MyStruct386<int>.MyClass387<int>.MyStruct388<int>.MyClass389<int>.MyStruct390<int>.MyClass391<int>.MyStruct392<int>.MyClass393<int>.MyStruct394<int>.MyClass395<int>.MyStruct396<int>.MyClass397<int>.MyStruct398<int>.MyClass399<int>.MyStruct400<int>.MyClass401<int>.MyStruct402<int>.MyClass403<int>.MyStruct404<int>.MyClass405<int>.MyStruct406<int>.MyClass407<int>.MyStruct408<int>.MyClass409<int>.MyStruct410<int>.MyClass411<int>.MyStruct412<int>.MyClass413<int>.MyStruct414<int>.MyClass415<int>.MyStruct416<int>.MyClass417<int>.MyStruct418<int>.MyClass419<int>.MyStruct420<int>.MyClass421<int>.MyStruct422<int>.MyClass423<int>.MyStruct424<int>.MyClass425<int>.MyStruct426<int>.MyClass427<int>.MyStruct428<int>.MyClass429<int>.MyStruct430<int>.MyClass431<int>.MyStruct432<int>.MyClass433<int>.MyStruct434<int>.MyClass435<int>.MyStruct436<int>.MyClass437<int>.MyStruct438<int>.MyClass439<int>.MyStruct440<int>.MyClass441<int>.MyStruct442<int>.MyClass443<int>.MyStruct444<int>.MyClass445<int>.MyStruct446<int>.MyClass447<int>.MyStruct448<int>.MyClass449<int>.MyStruct450<int>.MyClass451<int>.MyStruct452<int>.MyClass453<int>.MyStruct454<int>.MyClass455<int>.MyStruct456<int>.MyClass457<int>.MyStruct458<int>.MyClass459<int>.MyStruct460<int>.MyClass461<int>.MyStruct462<int>.MyClass463<int>.MyStruct464<int>.MyClass465<int>.MyStruct466<int>.MyClass467<int>.MyStruct468<int>.MyClass469<int>.MyStruct470<int>.MyClass471<int>.MyStruct472<int>.MyClass473<int>.MyStruct474<int>.MyClass475<int>.MyStruct476<int>.MyClass477<int>.MyStruct478<int>.MyClass479<int>.MyStruct480<int>.MyClass481<int>.MyStruct482<int>.MyClass483<int>.MyStruct484<int>.MyClass485<int>.MyStruct486<int>.MyClass487<int>.MyStruct488<int>.MyClass489<int>.MyStruct490<int>.MyClass491<int>.MyStruct492<int>.MyClass493<int>.MyStruct494<int>.MyClass495<int>.MyStruct496<int>.MyClass497<int>.MyStruct498<int>.MyClass499<int>();

      Console.WriteLine("PASS");
      return 100;
   }
}

public struct MyStruct0<T0> {
public class MyClass1<T1> {
public struct MyStruct2<T2> {
public class MyClass3<T3> {
public struct MyStruct4<T4> {
public class MyClass5<T5> {
public struct MyStruct6<T6> {
public class MyClass7<T7> {
public struct MyStruct8<T8> {
public class MyClass9<T9> {
public struct MyStruct10<T10> {
public class MyClass11<T11> {
public struct MyStruct12<T12> {
public class MyClass13<T13> {
public struct MyStruct14<T14> {
public class MyClass15<T15> {
public struct MyStruct16<T16> {
public class MyClass17<T17> {
public struct MyStruct18<T18> {
public class MyClass19<T19> {
public struct MyStruct20<T20> {
public class MyClass21<T21> {
public struct MyStruct22<T22> {
public class MyClass23<T23> {
public struct MyStruct24<T24> {
public class MyClass25<T25> {
public struct MyStruct26<T26> {
public class MyClass27<T27> {
public struct MyStruct28<T28> {
public class MyClass29<T29> {
public struct MyStruct30<T30> {
public class MyClass31<T31> {
public struct MyStruct32<T32> {
public class MyClass33<T33> {
public struct MyStruct34<T34> {
public class MyClass35<T35> {
public struct MyStruct36<T36> {
public class MyClass37<T37> {
public struct MyStruct38<T38> {
public class MyClass39<T39> {
public struct MyStruct40<T40> {
public class MyClass41<T41> {
public struct MyStruct42<T42> {
public class MyClass43<T43> {
public struct MyStruct44<T44> {
public class MyClass45<T45> {
public struct MyStruct46<T46> {
public class MyClass47<T47> {
public struct MyStruct48<T48> {
public class MyClass49<T49> {
public struct MyStruct50<T50> {
public class MyClass51<T51> {
public struct MyStruct52<T52> {
public class MyClass53<T53> {
public struct MyStruct54<T54> {
public class MyClass55<T55> {
public struct MyStruct56<T56> {
public class MyClass57<T57> {
public struct MyStruct58<T58> {
public class MyClass59<T59> {
public struct MyStruct60<T60> {
public class MyClass61<T61> {
public struct MyStruct62<T62> {
public class MyClass63<T63> {
public struct MyStruct64<T64> {
public class MyClass65<T65> {
public struct MyStruct66<T66> {
public class MyClass67<T67> {
public struct MyStruct68<T68> {
public class MyClass69<T69> {
public struct MyStruct70<T70> {
public class MyClass71<T71> {
public struct MyStruct72<T72> {
public class MyClass73<T73> {
public struct MyStruct74<T74> {
public class MyClass75<T75> {
public struct MyStruct76<T76> {
public class MyClass77<T77> {
public struct MyStruct78<T78> {
public class MyClass79<T79> {
public struct MyStruct80<T80> {
public class MyClass81<T81> {
public struct MyStruct82<T82> {
public class MyClass83<T83> {
public struct MyStruct84<T84> {
public class MyClass85<T85> {
public struct MyStruct86<T86> {
public class MyClass87<T87> {
public struct MyStruct88<T88> {
public class MyClass89<T89> {
public struct MyStruct90<T90> {
public class MyClass91<T91> {
public struct MyStruct92<T92> {
public class MyClass93<T93> {
public struct MyStruct94<T94> {
public class MyClass95<T95> {
public struct MyStruct96<T96> {
public class MyClass97<T97> {
public struct MyStruct98<T98> {
public class MyClass99<T99> {
public struct MyStruct100<T100> {
public class MyClass101<T101> {
public struct MyStruct102<T102> {
public class MyClass103<T103> {
public struct MyStruct104<T104> {
public class MyClass105<T105> {
public struct MyStruct106<T106> {
public class MyClass107<T107> {
public struct MyStruct108<T108> {
public class MyClass109<T109> {
public struct MyStruct110<T110> {
public class MyClass111<T111> {
public struct MyStruct112<T112> {
public class MyClass113<T113> {
public struct MyStruct114<T114> {
public class MyClass115<T115> {
public struct MyStruct116<T116> {
public class MyClass117<T117> {
public struct MyStruct118<T118> {
public class MyClass119<T119> {
public struct MyStruct120<T120> {
public class MyClass121<T121> {
public struct MyStruct122<T122> {
public class MyClass123<T123> {
public struct MyStruct124<T124> {
public class MyClass125<T125> {
public struct MyStruct126<T126> {
public class MyClass127<T127> {
public struct MyStruct128<T128> {
public class MyClass129<T129> {
public struct MyStruct130<T130> {
public class MyClass131<T131> {
public struct MyStruct132<T132> {
public class MyClass133<T133> {
public struct MyStruct134<T134> {
public class MyClass135<T135> {
public struct MyStruct136<T136> {
public class MyClass137<T137> {
public struct MyStruct138<T138> {
public class MyClass139<T139> {
public struct MyStruct140<T140> {
public class MyClass141<T141> {
public struct MyStruct142<T142> {
public class MyClass143<T143> {
public struct MyStruct144<T144> {
public class MyClass145<T145> {
public struct MyStruct146<T146> {
public class MyClass147<T147> {
public struct MyStruct148<T148> {
public class MyClass149<T149> {
public struct MyStruct150<T150> {
public class MyClass151<T151> {
public struct MyStruct152<T152> {
public class MyClass153<T153> {
public struct MyStruct154<T154> {
public class MyClass155<T155> {
public struct MyStruct156<T156> {
public class MyClass157<T157> {
public struct MyStruct158<T158> {
public class MyClass159<T159> {
public struct MyStruct160<T160> {
public class MyClass161<T161> {
public struct MyStruct162<T162> {
public class MyClass163<T163> {
public struct MyStruct164<T164> {
public class MyClass165<T165> {
public struct MyStruct166<T166> {
public class MyClass167<T167> {
public struct MyStruct168<T168> {
public class MyClass169<T169> {
public struct MyStruct170<T170> {
public class MyClass171<T171> {
public struct MyStruct172<T172> {
public class MyClass173<T173> {
public struct MyStruct174<T174> {
public class MyClass175<T175> {
public struct MyStruct176<T176> {
public class MyClass177<T177> {
public struct MyStruct178<T178> {
public class MyClass179<T179> {
public struct MyStruct180<T180> {
public class MyClass181<T181> {
public struct MyStruct182<T182> {
public class MyClass183<T183> {
public struct MyStruct184<T184> {
public class MyClass185<T185> {
public struct MyStruct186<T186> {
public class MyClass187<T187> {
public struct MyStruct188<T188> {
public class MyClass189<T189> {
public struct MyStruct190<T190> {
public class MyClass191<T191> {
public struct MyStruct192<T192> {
public class MyClass193<T193> {
public struct MyStruct194<T194> {
public class MyClass195<T195> {
public struct MyStruct196<T196> {
public class MyClass197<T197> {
public struct MyStruct198<T198> {
public class MyClass199<T199> {
public struct MyStruct200<T200> {
public class MyClass201<T201> {
public struct MyStruct202<T202> {
public class MyClass203<T203> {
public struct MyStruct204<T204> {
public class MyClass205<T205> {
public struct MyStruct206<T206> {
public class MyClass207<T207> {
public struct MyStruct208<T208> {
public class MyClass209<T209> {
public struct MyStruct210<T210> {
public class MyClass211<T211> {
public struct MyStruct212<T212> {
public class MyClass213<T213> {
public struct MyStruct214<T214> {
public class MyClass215<T215> {
public struct MyStruct216<T216> {
public class MyClass217<T217> {
public struct MyStruct218<T218> {
public class MyClass219<T219> {
public struct MyStruct220<T220> {
public class MyClass221<T221> {
public struct MyStruct222<T222> {
public class MyClass223<T223> {
public struct MyStruct224<T224> {
public class MyClass225<T225> {
public struct MyStruct226<T226> {
public class MyClass227<T227> {
public struct MyStruct228<T228> {
public class MyClass229<T229> {
public struct MyStruct230<T230> {
public class MyClass231<T231> {
public struct MyStruct232<T232> {
public class MyClass233<T233> {
public struct MyStruct234<T234> {
public class MyClass235<T235> {
public struct MyStruct236<T236> {
public class MyClass237<T237> {
public struct MyStruct238<T238> {
public class MyClass239<T239> {
public struct MyStruct240<T240> {
public class MyClass241<T241> {
public struct MyStruct242<T242> {
public class MyClass243<T243> {
public struct MyStruct244<T244> {
public class MyClass245<T245> {
public struct MyStruct246<T246> {
public class MyClass247<T247> {
public struct MyStruct248<T248> {
public class MyClass249<T249> {
public struct MyStruct250<T250> {
public class MyClass251<T251> {
public struct MyStruct252<T252> {
public class MyClass253<T253> {
public struct MyStruct254<T254> {
public class MyClass255<T255> {
public struct MyStruct256<T256> {
public class MyClass257<T257> {
public struct MyStruct258<T258> {
public class MyClass259<T259> {
public struct MyStruct260<T260> {
public class MyClass261<T261> {
public struct MyStruct262<T262> {
public class MyClass263<T263> {
public struct MyStruct264<T264> {
public class MyClass265<T265> {
public struct MyStruct266<T266> {
public class MyClass267<T267> {
public struct MyStruct268<T268> {
public class MyClass269<T269> {
public struct MyStruct270<T270> {
public class MyClass271<T271> {
public struct MyStruct272<T272> {
public class MyClass273<T273> {
public struct MyStruct274<T274> {
public class MyClass275<T275> {
public struct MyStruct276<T276> {
public class MyClass277<T277> {
public struct MyStruct278<T278> {
public class MyClass279<T279> {
public struct MyStruct280<T280> {
public class MyClass281<T281> {
public struct MyStruct282<T282> {
public class MyClass283<T283> {
public struct MyStruct284<T284> {
public class MyClass285<T285> {
public struct MyStruct286<T286> {
public class MyClass287<T287> {
public struct MyStruct288<T288> {
public class MyClass289<T289> {
public struct MyStruct290<T290> {
public class MyClass291<T291> {
public struct MyStruct292<T292> {
public class MyClass293<T293> {
public struct MyStruct294<T294> {
public class MyClass295<T295> {
public struct MyStruct296<T296> {
public class MyClass297<T297> {
public struct MyStruct298<T298> {
public class MyClass299<T299> {
public struct MyStruct300<T300> {
public class MyClass301<T301> {
public struct MyStruct302<T302> {
public class MyClass303<T303> {
public struct MyStruct304<T304> {
public class MyClass305<T305> {
public struct MyStruct306<T306> {
public class MyClass307<T307> {
public struct MyStruct308<T308> {
public class MyClass309<T309> {
public struct MyStruct310<T310> {
public class MyClass311<T311> {
public struct MyStruct312<T312> {
public class MyClass313<T313> {
public struct MyStruct314<T314> {
public class MyClass315<T315> {
public struct MyStruct316<T316> {
public class MyClass317<T317> {
public struct MyStruct318<T318> {
public class MyClass319<T319> {
public struct MyStruct320<T320> {
public class MyClass321<T321> {
public struct MyStruct322<T322> {
public class MyClass323<T323> {
public struct MyStruct324<T324> {
public class MyClass325<T325> {
public struct MyStruct326<T326> {
public class MyClass327<T327> {
public struct MyStruct328<T328> {
public class MyClass329<T329> {
public struct MyStruct330<T330> {
public class MyClass331<T331> {
public struct MyStruct332<T332> {
public class MyClass333<T333> {
public struct MyStruct334<T334> {
public class MyClass335<T335> {
public struct MyStruct336<T336> {
public class MyClass337<T337> {
public struct MyStruct338<T338> {
public class MyClass339<T339> {
public struct MyStruct340<T340> {
public class MyClass341<T341> {
public struct MyStruct342<T342> {
public class MyClass343<T343> {
public struct MyStruct344<T344> {
public class MyClass345<T345> {
public struct MyStruct346<T346> {
public class MyClass347<T347> {
public struct MyStruct348<T348> {
public class MyClass349<T349> {
public struct MyStruct350<T350> {
public class MyClass351<T351> {
public struct MyStruct352<T352> {
public class MyClass353<T353> {
public struct MyStruct354<T354> {
public class MyClass355<T355> {
public struct MyStruct356<T356> {
public class MyClass357<T357> {
public struct MyStruct358<T358> {
public class MyClass359<T359> {
public struct MyStruct360<T360> {
public class MyClass361<T361> {
public struct MyStruct362<T362> {
public class MyClass363<T363> {
public struct MyStruct364<T364> {
public class MyClass365<T365> {
public struct MyStruct366<T366> {
public class MyClass367<T367> {
public struct MyStruct368<T368> {
public class MyClass369<T369> {
public struct MyStruct370<T370> {
public class MyClass371<T371> {
public struct MyStruct372<T372> {
public class MyClass373<T373> {
public struct MyStruct374<T374> {
public class MyClass375<T375> {
public struct MyStruct376<T376> {
public class MyClass377<T377> {
public struct MyStruct378<T378> {
public class MyClass379<T379> {
public struct MyStruct380<T380> {
public class MyClass381<T381> {
public struct MyStruct382<T382> {
public class MyClass383<T383> {
public struct MyStruct384<T384> {
public class MyClass385<T385> {
public struct MyStruct386<T386> {
public class MyClass387<T387> {
public struct MyStruct388<T388> {
public class MyClass389<T389> {
public struct MyStruct390<T390> {
public class MyClass391<T391> {
public struct MyStruct392<T392> {
public class MyClass393<T393> {
public struct MyStruct394<T394> {
public class MyClass395<T395> {
public struct MyStruct396<T396> {
public class MyClass397<T397> {
public struct MyStruct398<T398> {
public class MyClass399<T399> {
public struct MyStruct400<T400> {
public class MyClass401<T401> {
public struct MyStruct402<T402> {
public class MyClass403<T403> {
public struct MyStruct404<T404> {
public class MyClass405<T405> {
public struct MyStruct406<T406> {
public class MyClass407<T407> {
public struct MyStruct408<T408> {
public class MyClass409<T409> {
public struct MyStruct410<T410> {
public class MyClass411<T411> {
public struct MyStruct412<T412> {
public class MyClass413<T413> {
public struct MyStruct414<T414> {
public class MyClass415<T415> {
public struct MyStruct416<T416> {
public class MyClass417<T417> {
public struct MyStruct418<T418> {
public class MyClass419<T419> {
public struct MyStruct420<T420> {
public class MyClass421<T421> {
public struct MyStruct422<T422> {
public class MyClass423<T423> {
public struct MyStruct424<T424> {
public class MyClass425<T425> {
public struct MyStruct426<T426> {
public class MyClass427<T427> {
public struct MyStruct428<T428> {
public class MyClass429<T429> {
public struct MyStruct430<T430> {
public class MyClass431<T431> {
public struct MyStruct432<T432> {
public class MyClass433<T433> {
public struct MyStruct434<T434> {
public class MyClass435<T435> {
public struct MyStruct436<T436> {
public class MyClass437<T437> {
public struct MyStruct438<T438> {
public class MyClass439<T439> {
public struct MyStruct440<T440> {
public class MyClass441<T441> {
public struct MyStruct442<T442> {
public class MyClass443<T443> {
public struct MyStruct444<T444> {
public class MyClass445<T445> {
public struct MyStruct446<T446> {
public class MyClass447<T447> {
public struct MyStruct448<T448> {
public class MyClass449<T449> {
public struct MyStruct450<T450> {
public class MyClass451<T451> {
public struct MyStruct452<T452> {
public class MyClass453<T453> {
public struct MyStruct454<T454> {
public class MyClass455<T455> {
public struct MyStruct456<T456> {
public class MyClass457<T457> {
public struct MyStruct458<T458> {
public class MyClass459<T459> {
public struct MyStruct460<T460> {
public class MyClass461<T461> {
public struct MyStruct462<T462> {
public class MyClass463<T463> {
public struct MyStruct464<T464> {
public class MyClass465<T465> {
public struct MyStruct466<T466> {
public class MyClass467<T467> {
public struct MyStruct468<T468> {
public class MyClass469<T469> {
public struct MyStruct470<T470> {
public class MyClass471<T471> {
public struct MyStruct472<T472> {
public class MyClass473<T473> {
public struct MyStruct474<T474> {
public class MyClass475<T475> {
public struct MyStruct476<T476> {
public class MyClass477<T477> {
public struct MyStruct478<T478> {
public class MyClass479<T479> {
public struct MyStruct480<T480> {
public class MyClass481<T481> {
public struct MyStruct482<T482> {
public class MyClass483<T483> {
public struct MyStruct484<T484> {
public class MyClass485<T485> {
public struct MyStruct486<T486> {
public class MyClass487<T487> {
public struct MyStruct488<T488> {
public class MyClass489<T489> {
public struct MyStruct490<T490> {
public class MyClass491<T491> {
public struct MyStruct492<T492> {
public class MyClass493<T493> {
public struct MyStruct494<T494> {
public class MyClass495<T495> {
public struct MyStruct496<T496> {
public class MyClass497<T497> {
public struct MyStruct498<T498> {
public class MyClass499<T499> {
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
