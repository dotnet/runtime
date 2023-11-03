// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test loads a type with deeply nested generic structs (up to level 500)

using System;
using Xunit;

public class Test_NestedGenericStructs
{
   [Fact]
   public static int TestEntryPoint()
   {
      #pragma warning disable 219
MyStruct0<int>.MyStruct1<int>.MyStruct2<int>.MyStruct3<int>.MyStruct4<int>.MyStruct5<int>.MyStruct6<int>.MyStruct7<int>.MyStruct8<int>.MyStruct9<int>.MyStruct10<int>.MyStruct11<int>.MyStruct12<int>.MyStruct13<int>.MyStruct14<int>.MyStruct15<int>.MyStruct16<int>.MyStruct17<int>.MyStruct18<int>.MyStruct19<int>.MyStruct20<int>.MyStruct21<int>.MyStruct22<int>.MyStruct23<int>.MyStruct24<int>.MyStruct25<int>.MyStruct26<int>.MyStruct27<int>.MyStruct28<int>.MyStruct29<int>.MyStruct30<int>.MyStruct31<int>.MyStruct32<int>.MyStruct33<int>.MyStruct34<int>.MyStruct35<int>.MyStruct36<int>.MyStruct37<int>.MyStruct38<int>.MyStruct39<int>.MyStruct40<int>.MyStruct41<int>.MyStruct42<int>.MyStruct43<int>.MyStruct44<int>.MyStruct45<int>.MyStruct46<int>.MyStruct47<int>.MyStruct48<int>.MyStruct49<int>.MyStruct50<int>.MyStruct51<int>.MyStruct52<int>.MyStruct53<int>.MyStruct54<int>.MyStruct55<int>.MyStruct56<int>.MyStruct57<int>.MyStruct58<int>.MyStruct59<int>.MyStruct60<int>.MyStruct61<int>.MyStruct62<int>.MyStruct63<int>.MyStruct64<int>.MyStruct65<int>.MyStruct66<int>.MyStruct67<int>.MyStruct68<int>.MyStruct69<int>.MyStruct70<int>.MyStruct71<int>.MyStruct72<int>.MyStruct73<int>.MyStruct74<int>.MyStruct75<int>.MyStruct76<int>.MyStruct77<int>.MyStruct78<int>.MyStruct79<int>.MyStruct80<int>.MyStruct81<int>.MyStruct82<int>.MyStruct83<int>.MyStruct84<int>.MyStruct85<int>.MyStruct86<int>.MyStruct87<int>.MyStruct88<int>.MyStruct89<int>.MyStruct90<int>.MyStruct91<int>.MyStruct92<int>.MyStruct93<int>.MyStruct94<int>.MyStruct95<int>.MyStruct96<int>.MyStruct97<int>.MyStruct98<int>.MyStruct99<int>.MyStruct100<int>.MyStruct101<int>.MyStruct102<int>.MyStruct103<int>.MyStruct104<int>.MyStruct105<int>.MyStruct106<int>.MyStruct107<int>.MyStruct108<int>.MyStruct109<int>.MyStruct110<int>.MyStruct111<int>.MyStruct112<int>.MyStruct113<int>.MyStruct114<int>.MyStruct115<int>.MyStruct116<int>.MyStruct117<int>.MyStruct118<int>.MyStruct119<int>.MyStruct120<int>.MyStruct121<int>.MyStruct122<int>.MyStruct123<int>.MyStruct124<int>.MyStruct125<int>.MyStruct126<int>.MyStruct127<int>.MyStruct128<int>.MyStruct129<int>.MyStruct130<int>.MyStruct131<int>.MyStruct132<int>.MyStruct133<int>.MyStruct134<int>.MyStruct135<int>.MyStruct136<int>.MyStruct137<int>.MyStruct138<int>.MyStruct139<int>.MyStruct140<int>.MyStruct141<int>.MyStruct142<int>.MyStruct143<int>.MyStruct144<int>.MyStruct145<int>.MyStruct146<int>.MyStruct147<int>.MyStruct148<int>.MyStruct149<int>.MyStruct150<int>.MyStruct151<int>.MyStruct152<int>.MyStruct153<int>.MyStruct154<int>.MyStruct155<int>.MyStruct156<int>.MyStruct157<int>.MyStruct158<int>.MyStruct159<int>.MyStruct160<int>.MyStruct161<int>.MyStruct162<int>.MyStruct163<int>.MyStruct164<int>.MyStruct165<int>.MyStruct166<int>.MyStruct167<int>.MyStruct168<int>.MyStruct169<int>.MyStruct170<int>.MyStruct171<int>.MyStruct172<int>.MyStruct173<int>.MyStruct174<int>.MyStruct175<int>.MyStruct176<int>.MyStruct177<int>.MyStruct178<int>.MyStruct179<int>.MyStruct180<int>.MyStruct181<int>.MyStruct182<int>.MyStruct183<int>.MyStruct184<int>.MyStruct185<int>.MyStruct186<int>.MyStruct187<int>.MyStruct188<int>.MyStruct189<int>.MyStruct190<int>.MyStruct191<int>.MyStruct192<int>.MyStruct193<int>.MyStruct194<int>.MyStruct195<int>.MyStruct196<int>.MyStruct197<int>.MyStruct198<int>.MyStruct199<int>.MyStruct200<int>.MyStruct201<int>.MyStruct202<int>.MyStruct203<int>.MyStruct204<int>.MyStruct205<int>.MyStruct206<int>.MyStruct207<int>.MyStruct208<int>.MyStruct209<int>.MyStruct210<int>.MyStruct211<int>.MyStruct212<int>.MyStruct213<int>.MyStruct214<int>.MyStruct215<int>.MyStruct216<int>.MyStruct217<int>.MyStruct218<int>.MyStruct219<int>.MyStruct220<int>.MyStruct221<int>.MyStruct222<int>.MyStruct223<int>.MyStruct224<int>.MyStruct225<int>.MyStruct226<int>.MyStruct227<int>.MyStruct228<int>.MyStruct229<int>.MyStruct230<int>.MyStruct231<int>.MyStruct232<int>.MyStruct233<int>.MyStruct234<int>.MyStruct235<int>.MyStruct236<int>.MyStruct237<int>.MyStruct238<int>.MyStruct239<int>.MyStruct240<int>.MyStruct241<int>.MyStruct242<int>.MyStruct243<int>.MyStruct244<int>.MyStruct245<int>.MyStruct246<int>.MyStruct247<int>.MyStruct248<int>.MyStruct249<int>.MyStruct250<int>.MyStruct251<int>.MyStruct252<int>.MyStruct253<int>.MyStruct254<int>.MyStruct255<int>.MyStruct256<int>.MyStruct257<int>.MyStruct258<int>.MyStruct259<int>.MyStruct260<int>.MyStruct261<int>.MyStruct262<int>.MyStruct263<int>.MyStruct264<int>.MyStruct265<int>.MyStruct266<int>.MyStruct267<int>.MyStruct268<int>.MyStruct269<int>.MyStruct270<int>.MyStruct271<int>.MyStruct272<int>.MyStruct273<int>.MyStruct274<int>.MyStruct275<int>.MyStruct276<int>.MyStruct277<int>.MyStruct278<int>.MyStruct279<int>.MyStruct280<int>.MyStruct281<int>.MyStruct282<int>.MyStruct283<int>.MyStruct284<int>.MyStruct285<int>.MyStruct286<int>.MyStruct287<int>.MyStruct288<int>.MyStruct289<int>.MyStruct290<int>.MyStruct291<int>.MyStruct292<int>.MyStruct293<int>.MyStruct294<int>.MyStruct295<int>.MyStruct296<int>.MyStruct297<int>.MyStruct298<int>.MyStruct299<int>.MyStruct300<int>.MyStruct301<int>.MyStruct302<int>.MyStruct303<int>.MyStruct304<int>.MyStruct305<int>.MyStruct306<int>.MyStruct307<int>.MyStruct308<int>.MyStruct309<int>.MyStruct310<int>.MyStruct311<int>.MyStruct312<int>.MyStruct313<int>.MyStruct314<int>.MyStruct315<int>.MyStruct316<int>.MyStruct317<int>.MyStruct318<int>.MyStruct319<int>.MyStruct320<int>.MyStruct321<int>.MyStruct322<int>.MyStruct323<int>.MyStruct324<int>.MyStruct325<int>.MyStruct326<int>.MyStruct327<int>.MyStruct328<int>.MyStruct329<int>.MyStruct330<int>.MyStruct331<int>.MyStruct332<int>.MyStruct333<int>.MyStruct334<int>.MyStruct335<int>.MyStruct336<int>.MyStruct337<int>.MyStruct338<int>.MyStruct339<int>.MyStruct340<int>.MyStruct341<int>.MyStruct342<int>.MyStruct343<int>.MyStruct344<int>.MyStruct345<int>.MyStruct346<int>.MyStruct347<int>.MyStruct348<int>.MyStruct349<int>.MyStruct350<int>.MyStruct351<int>.MyStruct352<int>.MyStruct353<int>.MyStruct354<int>.MyStruct355<int>.MyStruct356<int>.MyStruct357<int>.MyStruct358<int>.MyStruct359<int>.MyStruct360<int>.MyStruct361<int>.MyStruct362<int>.MyStruct363<int>.MyStruct364<int>.MyStruct365<int>.MyStruct366<int>.MyStruct367<int>.MyStruct368<int>.MyStruct369<int>.MyStruct370<int>.MyStruct371<int>.MyStruct372<int>.MyStruct373<int>.MyStruct374<int>.MyStruct375<int>.MyStruct376<int>.MyStruct377<int>.MyStruct378<int>.MyStruct379<int>.MyStruct380<int>.MyStruct381<int>.MyStruct382<int>.MyStruct383<int>.MyStruct384<int>.MyStruct385<int>.MyStruct386<int>.MyStruct387<int>.MyStruct388<int>.MyStruct389<int>.MyStruct390<int>.MyStruct391<int>.MyStruct392<int>.MyStruct393<int>.MyStruct394<int>.MyStruct395<int>.MyStruct396<int>.MyStruct397<int>.MyStruct398<int>.MyStruct399<int>.MyStruct400<int>.MyStruct401<int>.MyStruct402<int>.MyStruct403<int>.MyStruct404<int>.MyStruct405<int>.MyStruct406<int>.MyStruct407<int>.MyStruct408<int>.MyStruct409<int>.MyStruct410<int>.MyStruct411<int>.MyStruct412<int>.MyStruct413<int>.MyStruct414<int>.MyStruct415<int>.MyStruct416<int>.MyStruct417<int>.MyStruct418<int>.MyStruct419<int>.MyStruct420<int>.MyStruct421<int>.MyStruct422<int>.MyStruct423<int>.MyStruct424<int>.MyStruct425<int>.MyStruct426<int>.MyStruct427<int>.MyStruct428<int>.MyStruct429<int>.MyStruct430<int>.MyStruct431<int>.MyStruct432<int>.MyStruct433<int>.MyStruct434<int>.MyStruct435<int>.MyStruct436<int>.MyStruct437<int>.MyStruct438<int>.MyStruct439<int>.MyStruct440<int>.MyStruct441<int>.MyStruct442<int>.MyStruct443<int>.MyStruct444<int>.MyStruct445<int>.MyStruct446<int>.MyStruct447<int>.MyStruct448<int>.MyStruct449<int>.MyStruct450<int>.MyStruct451<int>.MyStruct452<int>.MyStruct453<int>.MyStruct454<int>.MyStruct455<int>.MyStruct456<int>.MyStruct457<int>.MyStruct458<int>.MyStruct459<int>.MyStruct460<int>.MyStruct461<int>.MyStruct462<int>.MyStruct463<int>.MyStruct464<int>.MyStruct465<int>.MyStruct466<int>.MyStruct467<int>.MyStruct468<int>.MyStruct469<int>.MyStruct470<int>.MyStruct471<int>.MyStruct472<int>.MyStruct473<int>.MyStruct474<int>.MyStruct475<int>.MyStruct476<int>.MyStruct477<int>.MyStruct478<int>.MyStruct479<int>.MyStruct480<int>.MyStruct481<int>.MyStruct482<int>.MyStruct483<int>.MyStruct484<int>.MyStruct485<int>.MyStruct486<int>.MyStruct487<int>.MyStruct488<int>.MyStruct489<int>.MyStruct490<int>.MyStruct491<int>.MyStruct492<int>.MyStruct493<int>.MyStruct494<int>.MyStruct495<int>.MyStruct496<int>.MyStruct497<int>.MyStruct498<int>.MyStruct499<int> obj = new MyStruct0<int>.MyStruct1<int>.MyStruct2<int>.MyStruct3<int>.MyStruct4<int>.MyStruct5<int>.MyStruct6<int>.MyStruct7<int>.MyStruct8<int>.MyStruct9<int>.MyStruct10<int>.MyStruct11<int>.MyStruct12<int>.MyStruct13<int>.MyStruct14<int>.MyStruct15<int>.MyStruct16<int>.MyStruct17<int>.MyStruct18<int>.MyStruct19<int>.MyStruct20<int>.MyStruct21<int>.MyStruct22<int>.MyStruct23<int>.MyStruct24<int>.MyStruct25<int>.MyStruct26<int>.MyStruct27<int>.MyStruct28<int>.MyStruct29<int>.MyStruct30<int>.MyStruct31<int>.MyStruct32<int>.MyStruct33<int>.MyStruct34<int>.MyStruct35<int>.MyStruct36<int>.MyStruct37<int>.MyStruct38<int>.MyStruct39<int>.MyStruct40<int>.MyStruct41<int>.MyStruct42<int>.MyStruct43<int>.MyStruct44<int>.MyStruct45<int>.MyStruct46<int>.MyStruct47<int>.MyStruct48<int>.MyStruct49<int>.MyStruct50<int>.MyStruct51<int>.MyStruct52<int>.MyStruct53<int>.MyStruct54<int>.MyStruct55<int>.MyStruct56<int>.MyStruct57<int>.MyStruct58<int>.MyStruct59<int>.MyStruct60<int>.MyStruct61<int>.MyStruct62<int>.MyStruct63<int>.MyStruct64<int>.MyStruct65<int>.MyStruct66<int>.MyStruct67<int>.MyStruct68<int>.MyStruct69<int>.MyStruct70<int>.MyStruct71<int>.MyStruct72<int>.MyStruct73<int>.MyStruct74<int>.MyStruct75<int>.MyStruct76<int>.MyStruct77<int>.MyStruct78<int>.MyStruct79<int>.MyStruct80<int>.MyStruct81<int>.MyStruct82<int>.MyStruct83<int>.MyStruct84<int>.MyStruct85<int>.MyStruct86<int>.MyStruct87<int>.MyStruct88<int>.MyStruct89<int>.MyStruct90<int>.MyStruct91<int>.MyStruct92<int>.MyStruct93<int>.MyStruct94<int>.MyStruct95<int>.MyStruct96<int>.MyStruct97<int>.MyStruct98<int>.MyStruct99<int>.MyStruct100<int>.MyStruct101<int>.MyStruct102<int>.MyStruct103<int>.MyStruct104<int>.MyStruct105<int>.MyStruct106<int>.MyStruct107<int>.MyStruct108<int>.MyStruct109<int>.MyStruct110<int>.MyStruct111<int>.MyStruct112<int>.MyStruct113<int>.MyStruct114<int>.MyStruct115<int>.MyStruct116<int>.MyStruct117<int>.MyStruct118<int>.MyStruct119<int>.MyStruct120<int>.MyStruct121<int>.MyStruct122<int>.MyStruct123<int>.MyStruct124<int>.MyStruct125<int>.MyStruct126<int>.MyStruct127<int>.MyStruct128<int>.MyStruct129<int>.MyStruct130<int>.MyStruct131<int>.MyStruct132<int>.MyStruct133<int>.MyStruct134<int>.MyStruct135<int>.MyStruct136<int>.MyStruct137<int>.MyStruct138<int>.MyStruct139<int>.MyStruct140<int>.MyStruct141<int>.MyStruct142<int>.MyStruct143<int>.MyStruct144<int>.MyStruct145<int>.MyStruct146<int>.MyStruct147<int>.MyStruct148<int>.MyStruct149<int>.MyStruct150<int>.MyStruct151<int>.MyStruct152<int>.MyStruct153<int>.MyStruct154<int>.MyStruct155<int>.MyStruct156<int>.MyStruct157<int>.MyStruct158<int>.MyStruct159<int>.MyStruct160<int>.MyStruct161<int>.MyStruct162<int>.MyStruct163<int>.MyStruct164<int>.MyStruct165<int>.MyStruct166<int>.MyStruct167<int>.MyStruct168<int>.MyStruct169<int>.MyStruct170<int>.MyStruct171<int>.MyStruct172<int>.MyStruct173<int>.MyStruct174<int>.MyStruct175<int>.MyStruct176<int>.MyStruct177<int>.MyStruct178<int>.MyStruct179<int>.MyStruct180<int>.MyStruct181<int>.MyStruct182<int>.MyStruct183<int>.MyStruct184<int>.MyStruct185<int>.MyStruct186<int>.MyStruct187<int>.MyStruct188<int>.MyStruct189<int>.MyStruct190<int>.MyStruct191<int>.MyStruct192<int>.MyStruct193<int>.MyStruct194<int>.MyStruct195<int>.MyStruct196<int>.MyStruct197<int>.MyStruct198<int>.MyStruct199<int>.MyStruct200<int>.MyStruct201<int>.MyStruct202<int>.MyStruct203<int>.MyStruct204<int>.MyStruct205<int>.MyStruct206<int>.MyStruct207<int>.MyStruct208<int>.MyStruct209<int>.MyStruct210<int>.MyStruct211<int>.MyStruct212<int>.MyStruct213<int>.MyStruct214<int>.MyStruct215<int>.MyStruct216<int>.MyStruct217<int>.MyStruct218<int>.MyStruct219<int>.MyStruct220<int>.MyStruct221<int>.MyStruct222<int>.MyStruct223<int>.MyStruct224<int>.MyStruct225<int>.MyStruct226<int>.MyStruct227<int>.MyStruct228<int>.MyStruct229<int>.MyStruct230<int>.MyStruct231<int>.MyStruct232<int>.MyStruct233<int>.MyStruct234<int>.MyStruct235<int>.MyStruct236<int>.MyStruct237<int>.MyStruct238<int>.MyStruct239<int>.MyStruct240<int>.MyStruct241<int>.MyStruct242<int>.MyStruct243<int>.MyStruct244<int>.MyStruct245<int>.MyStruct246<int>.MyStruct247<int>.MyStruct248<int>.MyStruct249<int>.MyStruct250<int>.MyStruct251<int>.MyStruct252<int>.MyStruct253<int>.MyStruct254<int>.MyStruct255<int>.MyStruct256<int>.MyStruct257<int>.MyStruct258<int>.MyStruct259<int>.MyStruct260<int>.MyStruct261<int>.MyStruct262<int>.MyStruct263<int>.MyStruct264<int>.MyStruct265<int>.MyStruct266<int>.MyStruct267<int>.MyStruct268<int>.MyStruct269<int>.MyStruct270<int>.MyStruct271<int>.MyStruct272<int>.MyStruct273<int>.MyStruct274<int>.MyStruct275<int>.MyStruct276<int>.MyStruct277<int>.MyStruct278<int>.MyStruct279<int>.MyStruct280<int>.MyStruct281<int>.MyStruct282<int>.MyStruct283<int>.MyStruct284<int>.MyStruct285<int>.MyStruct286<int>.MyStruct287<int>.MyStruct288<int>.MyStruct289<int>.MyStruct290<int>.MyStruct291<int>.MyStruct292<int>.MyStruct293<int>.MyStruct294<int>.MyStruct295<int>.MyStruct296<int>.MyStruct297<int>.MyStruct298<int>.MyStruct299<int>.MyStruct300<int>.MyStruct301<int>.MyStruct302<int>.MyStruct303<int>.MyStruct304<int>.MyStruct305<int>.MyStruct306<int>.MyStruct307<int>.MyStruct308<int>.MyStruct309<int>.MyStruct310<int>.MyStruct311<int>.MyStruct312<int>.MyStruct313<int>.MyStruct314<int>.MyStruct315<int>.MyStruct316<int>.MyStruct317<int>.MyStruct318<int>.MyStruct319<int>.MyStruct320<int>.MyStruct321<int>.MyStruct322<int>.MyStruct323<int>.MyStruct324<int>.MyStruct325<int>.MyStruct326<int>.MyStruct327<int>.MyStruct328<int>.MyStruct329<int>.MyStruct330<int>.MyStruct331<int>.MyStruct332<int>.MyStruct333<int>.MyStruct334<int>.MyStruct335<int>.MyStruct336<int>.MyStruct337<int>.MyStruct338<int>.MyStruct339<int>.MyStruct340<int>.MyStruct341<int>.MyStruct342<int>.MyStruct343<int>.MyStruct344<int>.MyStruct345<int>.MyStruct346<int>.MyStruct347<int>.MyStruct348<int>.MyStruct349<int>.MyStruct350<int>.MyStruct351<int>.MyStruct352<int>.MyStruct353<int>.MyStruct354<int>.MyStruct355<int>.MyStruct356<int>.MyStruct357<int>.MyStruct358<int>.MyStruct359<int>.MyStruct360<int>.MyStruct361<int>.MyStruct362<int>.MyStruct363<int>.MyStruct364<int>.MyStruct365<int>.MyStruct366<int>.MyStruct367<int>.MyStruct368<int>.MyStruct369<int>.MyStruct370<int>.MyStruct371<int>.MyStruct372<int>.MyStruct373<int>.MyStruct374<int>.MyStruct375<int>.MyStruct376<int>.MyStruct377<int>.MyStruct378<int>.MyStruct379<int>.MyStruct380<int>.MyStruct381<int>.MyStruct382<int>.MyStruct383<int>.MyStruct384<int>.MyStruct385<int>.MyStruct386<int>.MyStruct387<int>.MyStruct388<int>.MyStruct389<int>.MyStruct390<int>.MyStruct391<int>.MyStruct392<int>.MyStruct393<int>.MyStruct394<int>.MyStruct395<int>.MyStruct396<int>.MyStruct397<int>.MyStruct398<int>.MyStruct399<int>.MyStruct400<int>.MyStruct401<int>.MyStruct402<int>.MyStruct403<int>.MyStruct404<int>.MyStruct405<int>.MyStruct406<int>.MyStruct407<int>.MyStruct408<int>.MyStruct409<int>.MyStruct410<int>.MyStruct411<int>.MyStruct412<int>.MyStruct413<int>.MyStruct414<int>.MyStruct415<int>.MyStruct416<int>.MyStruct417<int>.MyStruct418<int>.MyStruct419<int>.MyStruct420<int>.MyStruct421<int>.MyStruct422<int>.MyStruct423<int>.MyStruct424<int>.MyStruct425<int>.MyStruct426<int>.MyStruct427<int>.MyStruct428<int>.MyStruct429<int>.MyStruct430<int>.MyStruct431<int>.MyStruct432<int>.MyStruct433<int>.MyStruct434<int>.MyStruct435<int>.MyStruct436<int>.MyStruct437<int>.MyStruct438<int>.MyStruct439<int>.MyStruct440<int>.MyStruct441<int>.MyStruct442<int>.MyStruct443<int>.MyStruct444<int>.MyStruct445<int>.MyStruct446<int>.MyStruct447<int>.MyStruct448<int>.MyStruct449<int>.MyStruct450<int>.MyStruct451<int>.MyStruct452<int>.MyStruct453<int>.MyStruct454<int>.MyStruct455<int>.MyStruct456<int>.MyStruct457<int>.MyStruct458<int>.MyStruct459<int>.MyStruct460<int>.MyStruct461<int>.MyStruct462<int>.MyStruct463<int>.MyStruct464<int>.MyStruct465<int>.MyStruct466<int>.MyStruct467<int>.MyStruct468<int>.MyStruct469<int>.MyStruct470<int>.MyStruct471<int>.MyStruct472<int>.MyStruct473<int>.MyStruct474<int>.MyStruct475<int>.MyStruct476<int>.MyStruct477<int>.MyStruct478<int>.MyStruct479<int>.MyStruct480<int>.MyStruct481<int>.MyStruct482<int>.MyStruct483<int>.MyStruct484<int>.MyStruct485<int>.MyStruct486<int>.MyStruct487<int>.MyStruct488<int>.MyStruct489<int>.MyStruct490<int>.MyStruct491<int>.MyStruct492<int>.MyStruct493<int>.MyStruct494<int>.MyStruct495<int>.MyStruct496<int>.MyStruct497<int>.MyStruct498<int>.MyStruct499<int>();

      #pragma warning restore 219
	
      Console.WriteLine("PASS");
      return 100;
   }
}

public struct MyStruct0<T0> {
public struct MyStruct1<T1> {
public struct MyStruct2<T2> {
public struct MyStruct3<T3> {
public struct MyStruct4<T4> {
public struct MyStruct5<T5> {
public struct MyStruct6<T6> {
public struct MyStruct7<T7> {
public struct MyStruct8<T8> {
public struct MyStruct9<T9> {
public struct MyStruct10<T10> {
public struct MyStruct11<T11> {
public struct MyStruct12<T12> {
public struct MyStruct13<T13> {
public struct MyStruct14<T14> {
public struct MyStruct15<T15> {
public struct MyStruct16<T16> {
public struct MyStruct17<T17> {
public struct MyStruct18<T18> {
public struct MyStruct19<T19> {
public struct MyStruct20<T20> {
public struct MyStruct21<T21> {
public struct MyStruct22<T22> {
public struct MyStruct23<T23> {
public struct MyStruct24<T24> {
public struct MyStruct25<T25> {
public struct MyStruct26<T26> {
public struct MyStruct27<T27> {
public struct MyStruct28<T28> {
public struct MyStruct29<T29> {
public struct MyStruct30<T30> {
public struct MyStruct31<T31> {
public struct MyStruct32<T32> {
public struct MyStruct33<T33> {
public struct MyStruct34<T34> {
public struct MyStruct35<T35> {
public struct MyStruct36<T36> {
public struct MyStruct37<T37> {
public struct MyStruct38<T38> {
public struct MyStruct39<T39> {
public struct MyStruct40<T40> {
public struct MyStruct41<T41> {
public struct MyStruct42<T42> {
public struct MyStruct43<T43> {
public struct MyStruct44<T44> {
public struct MyStruct45<T45> {
public struct MyStruct46<T46> {
public struct MyStruct47<T47> {
public struct MyStruct48<T48> {
public struct MyStruct49<T49> {
public struct MyStruct50<T50> {
public struct MyStruct51<T51> {
public struct MyStruct52<T52> {
public struct MyStruct53<T53> {
public struct MyStruct54<T54> {
public struct MyStruct55<T55> {
public struct MyStruct56<T56> {
public struct MyStruct57<T57> {
public struct MyStruct58<T58> {
public struct MyStruct59<T59> {
public struct MyStruct60<T60> {
public struct MyStruct61<T61> {
public struct MyStruct62<T62> {
public struct MyStruct63<T63> {
public struct MyStruct64<T64> {
public struct MyStruct65<T65> {
public struct MyStruct66<T66> {
public struct MyStruct67<T67> {
public struct MyStruct68<T68> {
public struct MyStruct69<T69> {
public struct MyStruct70<T70> {
public struct MyStruct71<T71> {
public struct MyStruct72<T72> {
public struct MyStruct73<T73> {
public struct MyStruct74<T74> {
public struct MyStruct75<T75> {
public struct MyStruct76<T76> {
public struct MyStruct77<T77> {
public struct MyStruct78<T78> {
public struct MyStruct79<T79> {
public struct MyStruct80<T80> {
public struct MyStruct81<T81> {
public struct MyStruct82<T82> {
public struct MyStruct83<T83> {
public struct MyStruct84<T84> {
public struct MyStruct85<T85> {
public struct MyStruct86<T86> {
public struct MyStruct87<T87> {
public struct MyStruct88<T88> {
public struct MyStruct89<T89> {
public struct MyStruct90<T90> {
public struct MyStruct91<T91> {
public struct MyStruct92<T92> {
public struct MyStruct93<T93> {
public struct MyStruct94<T94> {
public struct MyStruct95<T95> {
public struct MyStruct96<T96> {
public struct MyStruct97<T97> {
public struct MyStruct98<T98> {
public struct MyStruct99<T99> {
public struct MyStruct100<T100> {
public struct MyStruct101<T101> {
public struct MyStruct102<T102> {
public struct MyStruct103<T103> {
public struct MyStruct104<T104> {
public struct MyStruct105<T105> {
public struct MyStruct106<T106> {
public struct MyStruct107<T107> {
public struct MyStruct108<T108> {
public struct MyStruct109<T109> {
public struct MyStruct110<T110> {
public struct MyStruct111<T111> {
public struct MyStruct112<T112> {
public struct MyStruct113<T113> {
public struct MyStruct114<T114> {
public struct MyStruct115<T115> {
public struct MyStruct116<T116> {
public struct MyStruct117<T117> {
public struct MyStruct118<T118> {
public struct MyStruct119<T119> {
public struct MyStruct120<T120> {
public struct MyStruct121<T121> {
public struct MyStruct122<T122> {
public struct MyStruct123<T123> {
public struct MyStruct124<T124> {
public struct MyStruct125<T125> {
public struct MyStruct126<T126> {
public struct MyStruct127<T127> {
public struct MyStruct128<T128> {
public struct MyStruct129<T129> {
public struct MyStruct130<T130> {
public struct MyStruct131<T131> {
public struct MyStruct132<T132> {
public struct MyStruct133<T133> {
public struct MyStruct134<T134> {
public struct MyStruct135<T135> {
public struct MyStruct136<T136> {
public struct MyStruct137<T137> {
public struct MyStruct138<T138> {
public struct MyStruct139<T139> {
public struct MyStruct140<T140> {
public struct MyStruct141<T141> {
public struct MyStruct142<T142> {
public struct MyStruct143<T143> {
public struct MyStruct144<T144> {
public struct MyStruct145<T145> {
public struct MyStruct146<T146> {
public struct MyStruct147<T147> {
public struct MyStruct148<T148> {
public struct MyStruct149<T149> {
public struct MyStruct150<T150> {
public struct MyStruct151<T151> {
public struct MyStruct152<T152> {
public struct MyStruct153<T153> {
public struct MyStruct154<T154> {
public struct MyStruct155<T155> {
public struct MyStruct156<T156> {
public struct MyStruct157<T157> {
public struct MyStruct158<T158> {
public struct MyStruct159<T159> {
public struct MyStruct160<T160> {
public struct MyStruct161<T161> {
public struct MyStruct162<T162> {
public struct MyStruct163<T163> {
public struct MyStruct164<T164> {
public struct MyStruct165<T165> {
public struct MyStruct166<T166> {
public struct MyStruct167<T167> {
public struct MyStruct168<T168> {
public struct MyStruct169<T169> {
public struct MyStruct170<T170> {
public struct MyStruct171<T171> {
public struct MyStruct172<T172> {
public struct MyStruct173<T173> {
public struct MyStruct174<T174> {
public struct MyStruct175<T175> {
public struct MyStruct176<T176> {
public struct MyStruct177<T177> {
public struct MyStruct178<T178> {
public struct MyStruct179<T179> {
public struct MyStruct180<T180> {
public struct MyStruct181<T181> {
public struct MyStruct182<T182> {
public struct MyStruct183<T183> {
public struct MyStruct184<T184> {
public struct MyStruct185<T185> {
public struct MyStruct186<T186> {
public struct MyStruct187<T187> {
public struct MyStruct188<T188> {
public struct MyStruct189<T189> {
public struct MyStruct190<T190> {
public struct MyStruct191<T191> {
public struct MyStruct192<T192> {
public struct MyStruct193<T193> {
public struct MyStruct194<T194> {
public struct MyStruct195<T195> {
public struct MyStruct196<T196> {
public struct MyStruct197<T197> {
public struct MyStruct198<T198> {
public struct MyStruct199<T199> {
public struct MyStruct200<T200> {
public struct MyStruct201<T201> {
public struct MyStruct202<T202> {
public struct MyStruct203<T203> {
public struct MyStruct204<T204> {
public struct MyStruct205<T205> {
public struct MyStruct206<T206> {
public struct MyStruct207<T207> {
public struct MyStruct208<T208> {
public struct MyStruct209<T209> {
public struct MyStruct210<T210> {
public struct MyStruct211<T211> {
public struct MyStruct212<T212> {
public struct MyStruct213<T213> {
public struct MyStruct214<T214> {
public struct MyStruct215<T215> {
public struct MyStruct216<T216> {
public struct MyStruct217<T217> {
public struct MyStruct218<T218> {
public struct MyStruct219<T219> {
public struct MyStruct220<T220> {
public struct MyStruct221<T221> {
public struct MyStruct222<T222> {
public struct MyStruct223<T223> {
public struct MyStruct224<T224> {
public struct MyStruct225<T225> {
public struct MyStruct226<T226> {
public struct MyStruct227<T227> {
public struct MyStruct228<T228> {
public struct MyStruct229<T229> {
public struct MyStruct230<T230> {
public struct MyStruct231<T231> {
public struct MyStruct232<T232> {
public struct MyStruct233<T233> {
public struct MyStruct234<T234> {
public struct MyStruct235<T235> {
public struct MyStruct236<T236> {
public struct MyStruct237<T237> {
public struct MyStruct238<T238> {
public struct MyStruct239<T239> {
public struct MyStruct240<T240> {
public struct MyStruct241<T241> {
public struct MyStruct242<T242> {
public struct MyStruct243<T243> {
public struct MyStruct244<T244> {
public struct MyStruct245<T245> {
public struct MyStruct246<T246> {
public struct MyStruct247<T247> {
public struct MyStruct248<T248> {
public struct MyStruct249<T249> {
public struct MyStruct250<T250> {
public struct MyStruct251<T251> {
public struct MyStruct252<T252> {
public struct MyStruct253<T253> {
public struct MyStruct254<T254> {
public struct MyStruct255<T255> {
public struct MyStruct256<T256> {
public struct MyStruct257<T257> {
public struct MyStruct258<T258> {
public struct MyStruct259<T259> {
public struct MyStruct260<T260> {
public struct MyStruct261<T261> {
public struct MyStruct262<T262> {
public struct MyStruct263<T263> {
public struct MyStruct264<T264> {
public struct MyStruct265<T265> {
public struct MyStruct266<T266> {
public struct MyStruct267<T267> {
public struct MyStruct268<T268> {
public struct MyStruct269<T269> {
public struct MyStruct270<T270> {
public struct MyStruct271<T271> {
public struct MyStruct272<T272> {
public struct MyStruct273<T273> {
public struct MyStruct274<T274> {
public struct MyStruct275<T275> {
public struct MyStruct276<T276> {
public struct MyStruct277<T277> {
public struct MyStruct278<T278> {
public struct MyStruct279<T279> {
public struct MyStruct280<T280> {
public struct MyStruct281<T281> {
public struct MyStruct282<T282> {
public struct MyStruct283<T283> {
public struct MyStruct284<T284> {
public struct MyStruct285<T285> {
public struct MyStruct286<T286> {
public struct MyStruct287<T287> {
public struct MyStruct288<T288> {
public struct MyStruct289<T289> {
public struct MyStruct290<T290> {
public struct MyStruct291<T291> {
public struct MyStruct292<T292> {
public struct MyStruct293<T293> {
public struct MyStruct294<T294> {
public struct MyStruct295<T295> {
public struct MyStruct296<T296> {
public struct MyStruct297<T297> {
public struct MyStruct298<T298> {
public struct MyStruct299<T299> {
public struct MyStruct300<T300> {
public struct MyStruct301<T301> {
public struct MyStruct302<T302> {
public struct MyStruct303<T303> {
public struct MyStruct304<T304> {
public struct MyStruct305<T305> {
public struct MyStruct306<T306> {
public struct MyStruct307<T307> {
public struct MyStruct308<T308> {
public struct MyStruct309<T309> {
public struct MyStruct310<T310> {
public struct MyStruct311<T311> {
public struct MyStruct312<T312> {
public struct MyStruct313<T313> {
public struct MyStruct314<T314> {
public struct MyStruct315<T315> {
public struct MyStruct316<T316> {
public struct MyStruct317<T317> {
public struct MyStruct318<T318> {
public struct MyStruct319<T319> {
public struct MyStruct320<T320> {
public struct MyStruct321<T321> {
public struct MyStruct322<T322> {
public struct MyStruct323<T323> {
public struct MyStruct324<T324> {
public struct MyStruct325<T325> {
public struct MyStruct326<T326> {
public struct MyStruct327<T327> {
public struct MyStruct328<T328> {
public struct MyStruct329<T329> {
public struct MyStruct330<T330> {
public struct MyStruct331<T331> {
public struct MyStruct332<T332> {
public struct MyStruct333<T333> {
public struct MyStruct334<T334> {
public struct MyStruct335<T335> {
public struct MyStruct336<T336> {
public struct MyStruct337<T337> {
public struct MyStruct338<T338> {
public struct MyStruct339<T339> {
public struct MyStruct340<T340> {
public struct MyStruct341<T341> {
public struct MyStruct342<T342> {
public struct MyStruct343<T343> {
public struct MyStruct344<T344> {
public struct MyStruct345<T345> {
public struct MyStruct346<T346> {
public struct MyStruct347<T347> {
public struct MyStruct348<T348> {
public struct MyStruct349<T349> {
public struct MyStruct350<T350> {
public struct MyStruct351<T351> {
public struct MyStruct352<T352> {
public struct MyStruct353<T353> {
public struct MyStruct354<T354> {
public struct MyStruct355<T355> {
public struct MyStruct356<T356> {
public struct MyStruct357<T357> {
public struct MyStruct358<T358> {
public struct MyStruct359<T359> {
public struct MyStruct360<T360> {
public struct MyStruct361<T361> {
public struct MyStruct362<T362> {
public struct MyStruct363<T363> {
public struct MyStruct364<T364> {
public struct MyStruct365<T365> {
public struct MyStruct366<T366> {
public struct MyStruct367<T367> {
public struct MyStruct368<T368> {
public struct MyStruct369<T369> {
public struct MyStruct370<T370> {
public struct MyStruct371<T371> {
public struct MyStruct372<T372> {
public struct MyStruct373<T373> {
public struct MyStruct374<T374> {
public struct MyStruct375<T375> {
public struct MyStruct376<T376> {
public struct MyStruct377<T377> {
public struct MyStruct378<T378> {
public struct MyStruct379<T379> {
public struct MyStruct380<T380> {
public struct MyStruct381<T381> {
public struct MyStruct382<T382> {
public struct MyStruct383<T383> {
public struct MyStruct384<T384> {
public struct MyStruct385<T385> {
public struct MyStruct386<T386> {
public struct MyStruct387<T387> {
public struct MyStruct388<T388> {
public struct MyStruct389<T389> {
public struct MyStruct390<T390> {
public struct MyStruct391<T391> {
public struct MyStruct392<T392> {
public struct MyStruct393<T393> {
public struct MyStruct394<T394> {
public struct MyStruct395<T395> {
public struct MyStruct396<T396> {
public struct MyStruct397<T397> {
public struct MyStruct398<T398> {
public struct MyStruct399<T399> {
public struct MyStruct400<T400> {
public struct MyStruct401<T401> {
public struct MyStruct402<T402> {
public struct MyStruct403<T403> {
public struct MyStruct404<T404> {
public struct MyStruct405<T405> {
public struct MyStruct406<T406> {
public struct MyStruct407<T407> {
public struct MyStruct408<T408> {
public struct MyStruct409<T409> {
public struct MyStruct410<T410> {
public struct MyStruct411<T411> {
public struct MyStruct412<T412> {
public struct MyStruct413<T413> {
public struct MyStruct414<T414> {
public struct MyStruct415<T415> {
public struct MyStruct416<T416> {
public struct MyStruct417<T417> {
public struct MyStruct418<T418> {
public struct MyStruct419<T419> {
public struct MyStruct420<T420> {
public struct MyStruct421<T421> {
public struct MyStruct422<T422> {
public struct MyStruct423<T423> {
public struct MyStruct424<T424> {
public struct MyStruct425<T425> {
public struct MyStruct426<T426> {
public struct MyStruct427<T427> {
public struct MyStruct428<T428> {
public struct MyStruct429<T429> {
public struct MyStruct430<T430> {
public struct MyStruct431<T431> {
public struct MyStruct432<T432> {
public struct MyStruct433<T433> {
public struct MyStruct434<T434> {
public struct MyStruct435<T435> {
public struct MyStruct436<T436> {
public struct MyStruct437<T437> {
public struct MyStruct438<T438> {
public struct MyStruct439<T439> {
public struct MyStruct440<T440> {
public struct MyStruct441<T441> {
public struct MyStruct442<T442> {
public struct MyStruct443<T443> {
public struct MyStruct444<T444> {
public struct MyStruct445<T445> {
public struct MyStruct446<T446> {
public struct MyStruct447<T447> {
public struct MyStruct448<T448> {
public struct MyStruct449<T449> {
public struct MyStruct450<T450> {
public struct MyStruct451<T451> {
public struct MyStruct452<T452> {
public struct MyStruct453<T453> {
public struct MyStruct454<T454> {
public struct MyStruct455<T455> {
public struct MyStruct456<T456> {
public struct MyStruct457<T457> {
public struct MyStruct458<T458> {
public struct MyStruct459<T459> {
public struct MyStruct460<T460> {
public struct MyStruct461<T461> {
public struct MyStruct462<T462> {
public struct MyStruct463<T463> {
public struct MyStruct464<T464> {
public struct MyStruct465<T465> {
public struct MyStruct466<T466> {
public struct MyStruct467<T467> {
public struct MyStruct468<T468> {
public struct MyStruct469<T469> {
public struct MyStruct470<T470> {
public struct MyStruct471<T471> {
public struct MyStruct472<T472> {
public struct MyStruct473<T473> {
public struct MyStruct474<T474> {
public struct MyStruct475<T475> {
public struct MyStruct476<T476> {
public struct MyStruct477<T477> {
public struct MyStruct478<T478> {
public struct MyStruct479<T479> {
public struct MyStruct480<T480> {
public struct MyStruct481<T481> {
public struct MyStruct482<T482> {
public struct MyStruct483<T483> {
public struct MyStruct484<T484> {
public struct MyStruct485<T485> {
public struct MyStruct486<T486> {
public struct MyStruct487<T487> {
public struct MyStruct488<T488> {
public struct MyStruct489<T489> {
public struct MyStruct490<T490> {
public struct MyStruct491<T491> {
public struct MyStruct492<T492> {
public struct MyStruct493<T493> {
public struct MyStruct494<T494> {
public struct MyStruct495<T495> {
public struct MyStruct496<T496> {
public struct MyStruct497<T497> {
public struct MyStruct498<T498> {
public struct MyStruct499<T499> {
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
