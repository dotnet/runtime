
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Reflection;

class MyRealProxy : RealProxy, IRemotingTypeInfo
{
	MBRO mbro;
	bool can_cast_to_called = false;

	public MyRealProxy (MBRO mbro)
		: base (typeof (MBRO)) {
		this.mbro = mbro;
	}

	public override System.Runtime.Remoting.Messaging.IMessage Invoke (System.Runtime.Remoting.Messaging.IMessage msg) {
		IMethodCallMessage call = (IMethodCallMessage)msg;

		return new ReturnMessage (mbro.CallMe ((int)call.Args[0]), null, 0, null, call);
	}

	public bool CanCastTo (Type fromType, object o) {
		can_cast_to_called = true;
		return true;
	}

	public string TypeName {
		get {
			return "";
		}
		set {
			throw new Exception ("Should not be called");
		}
	}

	public bool CanCastToCalled {
		get {
			return can_cast_to_called;
		}
		set {
			can_cast_to_called = value;
		}
	}
}

class MBRO : MarshalByRefObject
{
	public int CallMe (int a) {
		return a;
	}
}

class MainClass
{
	static int Main (string[] args) {
		int total = 0;
		MyRealProxy mrp = new MyRealProxy (new MBRO ());
		object o = mrp.GetTransparentProxy ();

		mrp.CanCastToCalled = false;
		I1 itf1 = (I1)o;
		if (!mrp.CanCastToCalled)
			return 1;
		total += itf1.CallMe (1);
		mrp.CanCastToCalled = false;
		itf1 = (I1)o;
		if (mrp.CanCastToCalled)
			return 1;
		total += itf1.CallMe (1);

		mrp.CanCastToCalled = false;
		I2 itf2 = (I2)o;
		if (!mrp.CanCastToCalled)
			return 2;
		total += itf2.CallMe (2);
		mrp.CanCastToCalled = false;
		itf2 = (I2)o;
		if (mrp.CanCastToCalled)
			return 2;
		total += itf2.CallMe (2);

		mrp.CanCastToCalled = false;
		I3 itf3 = (I3)o;
		if (!mrp.CanCastToCalled)
			return 3;
		total += itf3.CallMe (3);
		mrp.CanCastToCalled = false;
		itf3 = (I3)o;
		if (mrp.CanCastToCalled)
			return 3;
		total += itf3.CallMe (3);

		mrp.CanCastToCalled = false;
		I4 itf4 = (I4)o;
		if (!mrp.CanCastToCalled)
			return 4;
		total += itf4.CallMe (4);
		mrp.CanCastToCalled = false;
		itf4 = (I4)o;
		if (mrp.CanCastToCalled)
			return 4;
		total += itf4.CallMe (4);

		mrp.CanCastToCalled = false;
		I5 itf5 = (I5)o;
		if (!mrp.CanCastToCalled)
			return 5;
		total += itf5.CallMe (5);
		mrp.CanCastToCalled = false;
		itf5 = (I5)o;
		if (mrp.CanCastToCalled)
			return 5;
		total += itf5.CallMe (5);

		mrp.CanCastToCalled = false;
		I6 itf6 = (I6)o;
		if (!mrp.CanCastToCalled)
			return 6;
		total += itf6.CallMe (6);
		mrp.CanCastToCalled = false;
		itf6 = (I6)o;
		if (mrp.CanCastToCalled)
			return 6;
		total += itf6.CallMe (6);

		mrp.CanCastToCalled = false;
		I7 itf7 = (I7)o;
		if (!mrp.CanCastToCalled)
			return 7;
		total += itf7.CallMe (7);
		mrp.CanCastToCalled = false;
		itf7 = (I7)o;
		if (mrp.CanCastToCalled)
			return 7;
		total += itf7.CallMe (7);

		mrp.CanCastToCalled = false;
		I8 itf8 = (I8)o;
		if (!mrp.CanCastToCalled)
			return 8;
		total += itf8.CallMe (8);
		mrp.CanCastToCalled = false;
		itf8 = (I8)o;
		if (mrp.CanCastToCalled)
			return 8;
		total += itf8.CallMe (8);

		mrp.CanCastToCalled = false;
		I9 itf9 = (I9)o;
		if (!mrp.CanCastToCalled)
			return 9;
		total += itf9.CallMe (9);
		mrp.CanCastToCalled = false;
		itf9 = (I9)o;
		if (mrp.CanCastToCalled)
			return 9;
		total += itf9.CallMe (9);

		mrp.CanCastToCalled = false;
		I10 itf10 = (I10)o;
		if (!mrp.CanCastToCalled)
			return 10;
		total += itf10.CallMe (10);
		mrp.CanCastToCalled = false;
		itf10 = (I10)o;
		if (mrp.CanCastToCalled)
			return 10;
		total += itf10.CallMe (10);

		mrp.CanCastToCalled = false;
		I11 itf11 = (I11)o;
		if (!mrp.CanCastToCalled)
			return 11;
		total += itf11.CallMe (11);
		mrp.CanCastToCalled = false;
		itf11 = (I11)o;
		if (mrp.CanCastToCalled)
			return 11;
		total += itf11.CallMe (11);

		mrp.CanCastToCalled = false;
		I12 itf12 = (I12)o;
		if (!mrp.CanCastToCalled)
			return 12;
		total += itf12.CallMe (12);
		mrp.CanCastToCalled = false;
		itf12 = (I12)o;
		if (mrp.CanCastToCalled)
			return 12;
		total += itf12.CallMe (12);

		mrp.CanCastToCalled = false;
		I13 itf13 = (I13)o;
		if (!mrp.CanCastToCalled)
			return 13;
		total += itf13.CallMe (13);
		mrp.CanCastToCalled = false;
		itf13 = (I13)o;
		if (mrp.CanCastToCalled)
			return 13;
		total += itf13.CallMe (13);

		mrp.CanCastToCalled = false;
		I14 itf14 = (I14)o;
		if (!mrp.CanCastToCalled)
			return 14;
		total += itf14.CallMe (14);
		mrp.CanCastToCalled = false;
		itf14 = (I14)o;
		if (mrp.CanCastToCalled)
			return 14;
		total += itf14.CallMe (14);

		mrp.CanCastToCalled = false;
		I15 itf15 = (I15)o;
		if (!mrp.CanCastToCalled)
			return 15;
		total += itf15.CallMe (15);
		mrp.CanCastToCalled = false;
		itf15 = (I15)o;
		if (mrp.CanCastToCalled)
			return 15;
		total += itf15.CallMe (15);

		mrp.CanCastToCalled = false;
		I16 itf16 = (I16)o;
		if (!mrp.CanCastToCalled)
			return 16;
		total += itf16.CallMe (16);
		mrp.CanCastToCalled = false;
		itf16 = (I16)o;
		if (mrp.CanCastToCalled)
			return 16;
		total += itf16.CallMe (16);

		mrp.CanCastToCalled = false;
		I17 itf17 = (I17)o;
		if (!mrp.CanCastToCalled)
			return 17;
		total += itf17.CallMe (17);
		mrp.CanCastToCalled = false;
		itf17 = (I17)o;
		if (mrp.CanCastToCalled)
			return 17;
		total += itf17.CallMe (17);

		mrp.CanCastToCalled = false;
		I18 itf18 = (I18)o;
		if (!mrp.CanCastToCalled)
			return 18;
		total += itf18.CallMe (18);
		mrp.CanCastToCalled = false;
		itf18 = (I18)o;
		if (mrp.CanCastToCalled)
			return 18;
		total += itf18.CallMe (18);

		mrp.CanCastToCalled = false;
		I19 itf19 = (I19)o;
		if (!mrp.CanCastToCalled)
			return 19;
		total += itf19.CallMe (19);
		mrp.CanCastToCalled = false;
		itf19 = (I19)o;
		if (mrp.CanCastToCalled)
			return 19;
		total += itf19.CallMe (19);

		mrp.CanCastToCalled = false;
		I20 itf20 = (I20)o;
		if (!mrp.CanCastToCalled)
			return 20;
		total += itf20.CallMe (20);
		mrp.CanCastToCalled = false;
		itf20 = (I20)o;
		if (mrp.CanCastToCalled)
			return 20;
		total += itf20.CallMe (20);

		mrp.CanCastToCalled = false;
		I21 itf21 = (I21)o;
		if (!mrp.CanCastToCalled)
			return 21;
		total += itf21.CallMe (21);
		mrp.CanCastToCalled = false;
		itf21 = (I21)o;
		if (mrp.CanCastToCalled)
			return 21;
		total += itf21.CallMe (21);

		mrp.CanCastToCalled = false;
		I22 itf22 = (I22)o;
		if (!mrp.CanCastToCalled)
			return 22;
		total += itf22.CallMe (22);
		mrp.CanCastToCalled = false;
		itf22 = (I22)o;
		if (mrp.CanCastToCalled)
			return 22;
		total += itf22.CallMe (22);

		mrp.CanCastToCalled = false;
		I23 itf23 = (I23)o;
		if (!mrp.CanCastToCalled)
			return 23;
		total += itf23.CallMe (23);
		mrp.CanCastToCalled = false;
		itf23 = (I23)o;
		if (mrp.CanCastToCalled)
			return 23;
		total += itf23.CallMe (23);

		mrp.CanCastToCalled = false;
		I24 itf24 = (I24)o;
		if (!mrp.CanCastToCalled)
			return 24;
		total += itf24.CallMe (24);
		mrp.CanCastToCalled = false;
		itf24 = (I24)o;
		if (mrp.CanCastToCalled)
			return 24;
		total += itf24.CallMe (24);

		mrp.CanCastToCalled = false;
		I25 itf25 = (I25)o;
		if (!mrp.CanCastToCalled)
			return 25;
		total += itf25.CallMe (25);
		mrp.CanCastToCalled = false;
		itf25 = (I25)o;
		if (mrp.CanCastToCalled)
			return 25;
		total += itf25.CallMe (25);

		mrp.CanCastToCalled = false;
		I26 itf26 = (I26)o;
		if (!mrp.CanCastToCalled)
			return 26;
		total += itf26.CallMe (26);
		mrp.CanCastToCalled = false;
		itf26 = (I26)o;
		if (mrp.CanCastToCalled)
			return 26;
		total += itf26.CallMe (26);

		mrp.CanCastToCalled = false;
		I27 itf27 = (I27)o;
		if (!mrp.CanCastToCalled)
			return 27;
		total += itf27.CallMe (27);
		mrp.CanCastToCalled = false;
		itf27 = (I27)o;
		if (mrp.CanCastToCalled)
			return 27;
		total += itf27.CallMe (27);

		mrp.CanCastToCalled = false;
		I28 itf28 = (I28)o;
		if (!mrp.CanCastToCalled)
			return 28;
		total += itf28.CallMe (28);
		mrp.CanCastToCalled = false;
		itf28 = (I28)o;
		if (mrp.CanCastToCalled)
			return 28;
		total += itf28.CallMe (28);

		mrp.CanCastToCalled = false;
		I29 itf29 = (I29)o;
		if (!mrp.CanCastToCalled)
			return 29;
		total += itf29.CallMe (29);
		mrp.CanCastToCalled = false;
		itf29 = (I29)o;
		if (mrp.CanCastToCalled)
			return 29;
		total += itf29.CallMe (29);

		mrp.CanCastToCalled = false;
		I30 itf30 = (I30)o;
		if (!mrp.CanCastToCalled)
			return 30;
		total += itf30.CallMe (30);
		mrp.CanCastToCalled = false;
		itf30 = (I30)o;
		if (mrp.CanCastToCalled)
			return 30;
		total += itf30.CallMe (30);

		mrp.CanCastToCalled = false;
		I31 itf31 = (I31)o;
		if (!mrp.CanCastToCalled)
			return 31;
		total += itf31.CallMe (31);
		mrp.CanCastToCalled = false;
		itf31 = (I31)o;
		if (mrp.CanCastToCalled)
			return 31;
		total += itf31.CallMe (31);

		mrp.CanCastToCalled = false;
		I32 itf32 = (I32)o;
		if (!mrp.CanCastToCalled)
			return 32;
		total += itf32.CallMe (32);
		mrp.CanCastToCalled = false;
		itf32 = (I32)o;
		if (mrp.CanCastToCalled)
			return 32;
		total += itf32.CallMe (32);

		mrp.CanCastToCalled = false;
		I33 itf33 = (I33)o;
		if (!mrp.CanCastToCalled)
			return 33;
		total += itf33.CallMe (33);
		mrp.CanCastToCalled = false;
		itf33 = (I33)o;
		if (mrp.CanCastToCalled)
			return 33;
		total += itf33.CallMe (33);

		mrp.CanCastToCalled = false;
		I34 itf34 = (I34)o;
		if (!mrp.CanCastToCalled)
			return 34;
		total += itf34.CallMe (34);
		mrp.CanCastToCalled = false;
		itf34 = (I34)o;
		if (mrp.CanCastToCalled)
			return 34;
		total += itf34.CallMe (34);

		mrp.CanCastToCalled = false;
		I35 itf35 = (I35)o;
		if (!mrp.CanCastToCalled)
			return 35;
		total += itf35.CallMe (35);
		mrp.CanCastToCalled = false;
		itf35 = (I35)o;
		if (mrp.CanCastToCalled)
			return 35;
		total += itf35.CallMe (35);

		mrp.CanCastToCalled = false;
		I36 itf36 = (I36)o;
		if (!mrp.CanCastToCalled)
			return 36;
		total += itf36.CallMe (36);
		mrp.CanCastToCalled = false;
		itf36 = (I36)o;
		if (mrp.CanCastToCalled)
			return 36;
		total += itf36.CallMe (36);

		mrp.CanCastToCalled = false;
		I37 itf37 = (I37)o;
		if (!mrp.CanCastToCalled)
			return 37;
		total += itf37.CallMe (37);
		mrp.CanCastToCalled = false;
		itf37 = (I37)o;
		if (mrp.CanCastToCalled)
			return 37;
		total += itf37.CallMe (37);

		mrp.CanCastToCalled = false;
		I38 itf38 = (I38)o;
		if (!mrp.CanCastToCalled)
			return 38;
		total += itf38.CallMe (38);
		mrp.CanCastToCalled = false;
		itf38 = (I38)o;
		if (mrp.CanCastToCalled)
			return 38;
		total += itf38.CallMe (38);

		mrp.CanCastToCalled = false;
		I39 itf39 = (I39)o;
		if (!mrp.CanCastToCalled)
			return 39;
		total += itf39.CallMe (39);
		mrp.CanCastToCalled = false;
		itf39 = (I39)o;
		if (mrp.CanCastToCalled)
			return 39;
		total += itf39.CallMe (39);

		mrp.CanCastToCalled = false;
		I40 itf40 = (I40)o;
		if (!mrp.CanCastToCalled)
			return 40;
		total += itf40.CallMe (40);
		mrp.CanCastToCalled = false;
		itf40 = (I40)o;
		if (mrp.CanCastToCalled)
			return 40;
		total += itf40.CallMe (40);

		mrp.CanCastToCalled = false;
		I41 itf41 = (I41)o;
		if (!mrp.CanCastToCalled)
			return 41;
		total += itf41.CallMe (41);
		mrp.CanCastToCalled = false;
		itf41 = (I41)o;
		if (mrp.CanCastToCalled)
			return 41;
		total += itf41.CallMe (41);

		mrp.CanCastToCalled = false;
		I42 itf42 = (I42)o;
		if (!mrp.CanCastToCalled)
			return 42;
		total += itf42.CallMe (42);
		mrp.CanCastToCalled = false;
		itf42 = (I42)o;
		if (mrp.CanCastToCalled)
			return 42;
		total += itf42.CallMe (42);

		mrp.CanCastToCalled = false;
		I43 itf43 = (I43)o;
		if (!mrp.CanCastToCalled)
			return 43;
		total += itf43.CallMe (43);
		mrp.CanCastToCalled = false;
		itf43 = (I43)o;
		if (mrp.CanCastToCalled)
			return 43;
		total += itf43.CallMe (43);

		mrp.CanCastToCalled = false;
		I44 itf44 = (I44)o;
		if (!mrp.CanCastToCalled)
			return 44;
		total += itf44.CallMe (44);
		mrp.CanCastToCalled = false;
		itf44 = (I44)o;
		if (mrp.CanCastToCalled)
			return 44;
		total += itf44.CallMe (44);

		mrp.CanCastToCalled = false;
		I45 itf45 = (I45)o;
		if (!mrp.CanCastToCalled)
			return 45;
		total += itf45.CallMe (45);
		mrp.CanCastToCalled = false;
		itf45 = (I45)o;
		if (mrp.CanCastToCalled)
			return 45;
		total += itf45.CallMe (45);

		mrp.CanCastToCalled = false;
		I46 itf46 = (I46)o;
		if (!mrp.CanCastToCalled)
			return 46;
		total += itf46.CallMe (46);
		mrp.CanCastToCalled = false;
		itf46 = (I46)o;
		if (mrp.CanCastToCalled)
			return 46;
		total += itf46.CallMe (46);

		mrp.CanCastToCalled = false;
		I47 itf47 = (I47)o;
		if (!mrp.CanCastToCalled)
			return 47;
		total += itf47.CallMe (47);
		mrp.CanCastToCalled = false;
		itf47 = (I47)o;
		if (mrp.CanCastToCalled)
			return 47;
		total += itf47.CallMe (47);

		mrp.CanCastToCalled = false;
		I48 itf48 = (I48)o;
		if (!mrp.CanCastToCalled)
			return 48;
		total += itf48.CallMe (48);
		mrp.CanCastToCalled = false;
		itf48 = (I48)o;
		if (mrp.CanCastToCalled)
			return 48;
		total += itf48.CallMe (48);

		mrp.CanCastToCalled = false;
		I49 itf49 = (I49)o;
		if (!mrp.CanCastToCalled)
			return 49;
		total += itf49.CallMe (49);
		mrp.CanCastToCalled = false;
		itf49 = (I49)o;
		if (mrp.CanCastToCalled)
			return 49;
		total += itf49.CallMe (49);

		mrp.CanCastToCalled = false;
		I50 itf50 = (I50)o;
		if (!mrp.CanCastToCalled)
			return 50;
		total += itf50.CallMe (50);
		mrp.CanCastToCalled = false;
		itf50 = (I50)o;
		if (mrp.CanCastToCalled)
			return 50;
		total += itf50.CallMe (50);

		mrp.CanCastToCalled = false;
		I51 itf51 = (I51)o;
		if (!mrp.CanCastToCalled)
			return 51;
		total += itf51.CallMe (51);
		mrp.CanCastToCalled = false;
		itf51 = (I51)o;
		if (mrp.CanCastToCalled)
			return 51;
		total += itf51.CallMe (51);

		mrp.CanCastToCalled = false;
		I52 itf52 = (I52)o;
		if (!mrp.CanCastToCalled)
			return 52;
		total += itf52.CallMe (52);
		mrp.CanCastToCalled = false;
		itf52 = (I52)o;
		if (mrp.CanCastToCalled)
			return 52;
		total += itf52.CallMe (52);

		mrp.CanCastToCalled = false;
		I53 itf53 = (I53)o;
		if (!mrp.CanCastToCalled)
			return 53;
		total += itf53.CallMe (53);
		mrp.CanCastToCalled = false;
		itf53 = (I53)o;
		if (mrp.CanCastToCalled)
			return 53;
		total += itf53.CallMe (53);

		mrp.CanCastToCalled = false;
		I54 itf54 = (I54)o;
		if (!mrp.CanCastToCalled)
			return 54;
		total += itf54.CallMe (54);
		mrp.CanCastToCalled = false;
		itf54 = (I54)o;
		if (mrp.CanCastToCalled)
			return 54;
		total += itf54.CallMe (54);

		mrp.CanCastToCalled = false;
		I55 itf55 = (I55)o;
		if (!mrp.CanCastToCalled)
			return 55;
		total += itf55.CallMe (55);
		mrp.CanCastToCalled = false;
		itf55 = (I55)o;
		if (mrp.CanCastToCalled)
			return 55;
		total += itf55.CallMe (55);

		mrp.CanCastToCalled = false;
		I56 itf56 = (I56)o;
		if (!mrp.CanCastToCalled)
			return 56;
		total += itf56.CallMe (56);
		mrp.CanCastToCalled = false;
		itf56 = (I56)o;
		if (mrp.CanCastToCalled)
			return 56;
		total += itf56.CallMe (56);

		mrp.CanCastToCalled = false;
		I57 itf57 = (I57)o;
		if (!mrp.CanCastToCalled)
			return 57;
		total += itf57.CallMe (57);
		mrp.CanCastToCalled = false;
		itf57 = (I57)o;
		if (mrp.CanCastToCalled)
			return 57;
		total += itf57.CallMe (57);

		mrp.CanCastToCalled = false;
		I58 itf58 = (I58)o;
		if (!mrp.CanCastToCalled)
			return 58;
		total += itf58.CallMe (58);
		mrp.CanCastToCalled = false;
		itf58 = (I58)o;
		if (mrp.CanCastToCalled)
			return 58;
		total += itf58.CallMe (58);

		mrp.CanCastToCalled = false;
		I59 itf59 = (I59)o;
		if (!mrp.CanCastToCalled)
			return 59;
		total += itf59.CallMe (59);
		mrp.CanCastToCalled = false;
		itf59 = (I59)o;
		if (mrp.CanCastToCalled)
			return 59;
		total += itf59.CallMe (59);

		mrp.CanCastToCalled = false;
		I60 itf60 = (I60)o;
		if (!mrp.CanCastToCalled)
			return 60;
		total += itf60.CallMe (60);
		mrp.CanCastToCalled = false;
		itf60 = (I60)o;
		if (mrp.CanCastToCalled)
			return 60;
		total += itf60.CallMe (60);

		mrp.CanCastToCalled = false;
		I61 itf61 = (I61)o;
		if (!mrp.CanCastToCalled)
			return 61;
		total += itf61.CallMe (61);
		mrp.CanCastToCalled = false;
		itf61 = (I61)o;
		if (mrp.CanCastToCalled)
			return 61;
		total += itf61.CallMe (61);

		mrp.CanCastToCalled = false;
		I62 itf62 = (I62)o;
		if (!mrp.CanCastToCalled)
			return 62;
		total += itf62.CallMe (62);
		mrp.CanCastToCalled = false;
		itf62 = (I62)o;
		if (mrp.CanCastToCalled)
			return 62;
		total += itf62.CallMe (62);

		mrp.CanCastToCalled = false;
		I63 itf63 = (I63)o;
		if (!mrp.CanCastToCalled)
			return 63;
		total += itf63.CallMe (63);
		mrp.CanCastToCalled = false;
		itf63 = (I63)o;
		if (mrp.CanCastToCalled)
			return 63;
		total += itf63.CallMe (63);

		mrp.CanCastToCalled = false;
		I64 itf64 = (I64)o;
		if (!mrp.CanCastToCalled)
			return 64;
		total += itf64.CallMe (64);
		mrp.CanCastToCalled = false;
		itf64 = (I64)o;
		if (mrp.CanCastToCalled)
			return 64;
		total += itf64.CallMe (64);

		mrp.CanCastToCalled = false;
		I65 itf65 = (I65)o;
		if (!mrp.CanCastToCalled)
			return 65;
		total += itf65.CallMe (65);
		mrp.CanCastToCalled = false;
		itf65 = (I65)o;
		if (mrp.CanCastToCalled)
			return 65;
		total += itf65.CallMe (65);

		mrp.CanCastToCalled = false;
		I66 itf66 = (I66)o;
		if (!mrp.CanCastToCalled)
			return 66;
		total += itf66.CallMe (66);
		mrp.CanCastToCalled = false;
		itf66 = (I66)o;
		if (mrp.CanCastToCalled)
			return 66;
		total += itf66.CallMe (66);

		mrp.CanCastToCalled = false;
		I67 itf67 = (I67)o;
		if (!mrp.CanCastToCalled)
			return 67;
		total += itf67.CallMe (67);
		mrp.CanCastToCalled = false;
		itf67 = (I67)o;
		if (mrp.CanCastToCalled)
			return 67;
		total += itf67.CallMe (67);

		mrp.CanCastToCalled = false;
		I68 itf68 = (I68)o;
		if (!mrp.CanCastToCalled)
			return 68;
		total += itf68.CallMe (68);
		mrp.CanCastToCalled = false;
		itf68 = (I68)o;
		if (mrp.CanCastToCalled)
			return 68;
		total += itf68.CallMe (68);

		mrp.CanCastToCalled = false;
		I69 itf69 = (I69)o;
		if (!mrp.CanCastToCalled)
			return 69;
		total += itf69.CallMe (69);
		mrp.CanCastToCalled = false;
		itf69 = (I69)o;
		if (mrp.CanCastToCalled)
			return 69;
		total += itf69.CallMe (69);

		mrp.CanCastToCalled = false;
		I70 itf70 = (I70)o;
		if (!mrp.CanCastToCalled)
			return 70;
		total += itf70.CallMe (70);
		mrp.CanCastToCalled = false;
		itf70 = (I70)o;
		if (mrp.CanCastToCalled)
			return 70;
		total += itf70.CallMe (70);

		mrp.CanCastToCalled = false;
		I71 itf71 = (I71)o;
		if (!mrp.CanCastToCalled)
			return 71;
		total += itf71.CallMe (71);
		mrp.CanCastToCalled = false;
		itf71 = (I71)o;
		if (mrp.CanCastToCalled)
			return 71;
		total += itf71.CallMe (71);

		mrp.CanCastToCalled = false;
		I72 itf72 = (I72)o;
		if (!mrp.CanCastToCalled)
			return 72;
		total += itf72.CallMe (72);
		mrp.CanCastToCalled = false;
		itf72 = (I72)o;
		if (mrp.CanCastToCalled)
			return 72;
		total += itf72.CallMe (72);

		mrp.CanCastToCalled = false;
		I73 itf73 = (I73)o;
		if (!mrp.CanCastToCalled)
			return 73;
		total += itf73.CallMe (73);
		mrp.CanCastToCalled = false;
		itf73 = (I73)o;
		if (mrp.CanCastToCalled)
			return 73;
		total += itf73.CallMe (73);

		mrp.CanCastToCalled = false;
		I74 itf74 = (I74)o;
		if (!mrp.CanCastToCalled)
			return 74;
		total += itf74.CallMe (74);
		mrp.CanCastToCalled = false;
		itf74 = (I74)o;
		if (mrp.CanCastToCalled)
			return 74;
		total += itf74.CallMe (74);

		mrp.CanCastToCalled = false;
		I75 itf75 = (I75)o;
		if (!mrp.CanCastToCalled)
			return 75;
		total += itf75.CallMe (75);
		mrp.CanCastToCalled = false;
		itf75 = (I75)o;
		if (mrp.CanCastToCalled)
			return 75;
		total += itf75.CallMe (75);

		mrp.CanCastToCalled = false;
		I76 itf76 = (I76)o;
		if (!mrp.CanCastToCalled)
			return 76;
		total += itf76.CallMe (76);
		mrp.CanCastToCalled = false;
		itf76 = (I76)o;
		if (mrp.CanCastToCalled)
			return 76;
		total += itf76.CallMe (76);

		mrp.CanCastToCalled = false;
		I77 itf77 = (I77)o;
		if (!mrp.CanCastToCalled)
			return 77;
		total += itf77.CallMe (77);
		mrp.CanCastToCalled = false;
		itf77 = (I77)o;
		if (mrp.CanCastToCalled)
			return 77;
		total += itf77.CallMe (77);

		mrp.CanCastToCalled = false;
		I78 itf78 = (I78)o;
		if (!mrp.CanCastToCalled)
			return 78;
		total += itf78.CallMe (78);
		mrp.CanCastToCalled = false;
		itf78 = (I78)o;
		if (mrp.CanCastToCalled)
			return 78;
		total += itf78.CallMe (78);

		mrp.CanCastToCalled = false;
		I79 itf79 = (I79)o;
		if (!mrp.CanCastToCalled)
			return 79;
		total += itf79.CallMe (79);
		mrp.CanCastToCalled = false;
		itf79 = (I79)o;
		if (mrp.CanCastToCalled)
			return 79;
		total += itf79.CallMe (79);

		mrp.CanCastToCalled = false;
		I80 itf80 = (I80)o;
		if (!mrp.CanCastToCalled)
			return 80;
		total += itf80.CallMe (80);
		mrp.CanCastToCalled = false;
		itf80 = (I80)o;
		if (mrp.CanCastToCalled)
			return 80;
		total += itf80.CallMe (80);

		mrp.CanCastToCalled = false;
		I81 itf81 = (I81)o;
		if (!mrp.CanCastToCalled)
			return 81;
		total += itf81.CallMe (81);
		mrp.CanCastToCalled = false;
		itf81 = (I81)o;
		if (mrp.CanCastToCalled)
			return 81;
		total += itf81.CallMe (81);

		mrp.CanCastToCalled = false;
		I82 itf82 = (I82)o;
		if (!mrp.CanCastToCalled)
			return 82;
		total += itf82.CallMe (82);
		mrp.CanCastToCalled = false;
		itf82 = (I82)o;
		if (mrp.CanCastToCalled)
			return 82;
		total += itf82.CallMe (82);

		mrp.CanCastToCalled = false;
		I83 itf83 = (I83)o;
		if (!mrp.CanCastToCalled)
			return 83;
		total += itf83.CallMe (83);
		mrp.CanCastToCalled = false;
		itf83 = (I83)o;
		if (mrp.CanCastToCalled)
			return 83;
		total += itf83.CallMe (83);

		mrp.CanCastToCalled = false;
		I84 itf84 = (I84)o;
		if (!mrp.CanCastToCalled)
			return 84;
		total += itf84.CallMe (84);
		mrp.CanCastToCalled = false;
		itf84 = (I84)o;
		if (mrp.CanCastToCalled)
			return 84;
		total += itf84.CallMe (84);

		mrp.CanCastToCalled = false;
		I85 itf85 = (I85)o;
		if (!mrp.CanCastToCalled)
			return 85;
		total += itf85.CallMe (85);
		mrp.CanCastToCalled = false;
		itf85 = (I85)o;
		if (mrp.CanCastToCalled)
			return 85;
		total += itf85.CallMe (85);

		mrp.CanCastToCalled = false;
		I86 itf86 = (I86)o;
		if (!mrp.CanCastToCalled)
			return 86;
		total += itf86.CallMe (86);
		mrp.CanCastToCalled = false;
		itf86 = (I86)o;
		if (mrp.CanCastToCalled)
			return 86;
		total += itf86.CallMe (86);

		mrp.CanCastToCalled = false;
		I87 itf87 = (I87)o;
		if (!mrp.CanCastToCalled)
			return 87;
		total += itf87.CallMe (87);
		mrp.CanCastToCalled = false;
		itf87 = (I87)o;
		if (mrp.CanCastToCalled)
			return 87;
		total += itf87.CallMe (87);

		mrp.CanCastToCalled = false;
		I88 itf88 = (I88)o;
		if (!mrp.CanCastToCalled)
			return 88;
		total += itf88.CallMe (88);
		mrp.CanCastToCalled = false;
		itf88 = (I88)o;
		if (mrp.CanCastToCalled)
			return 88;
		total += itf88.CallMe (88);

		mrp.CanCastToCalled = false;
		I89 itf89 = (I89)o;
		if (!mrp.CanCastToCalled)
			return 89;
		total += itf89.CallMe (89);
		mrp.CanCastToCalled = false;
		itf89 = (I89)o;
		if (mrp.CanCastToCalled)
			return 89;
		total += itf89.CallMe (89);

		mrp.CanCastToCalled = false;
		I90 itf90 = (I90)o;
		if (!mrp.CanCastToCalled)
			return 90;
		total += itf90.CallMe (90);
		mrp.CanCastToCalled = false;
		itf90 = (I90)o;
		if (mrp.CanCastToCalled)
			return 90;
		total += itf90.CallMe (90);

		mrp.CanCastToCalled = false;
		I91 itf91 = (I91)o;
		if (!mrp.CanCastToCalled)
			return 91;
		total += itf91.CallMe (91);
		mrp.CanCastToCalled = false;
		itf91 = (I91)o;
		if (mrp.CanCastToCalled)
			return 91;
		total += itf91.CallMe (91);

		mrp.CanCastToCalled = false;
		I92 itf92 = (I92)o;
		if (!mrp.CanCastToCalled)
			return 92;
		total += itf92.CallMe (92);
		mrp.CanCastToCalled = false;
		itf92 = (I92)o;
		if (mrp.CanCastToCalled)
			return 92;
		total += itf92.CallMe (92);

		mrp.CanCastToCalled = false;
		I93 itf93 = (I93)o;
		if (!mrp.CanCastToCalled)
			return 93;
		total += itf93.CallMe (93);
		mrp.CanCastToCalled = false;
		itf93 = (I93)o;
		if (mrp.CanCastToCalled)
			return 93;
		total += itf93.CallMe (93);

		mrp.CanCastToCalled = false;
		I94 itf94 = (I94)o;
		if (!mrp.CanCastToCalled)
			return 94;
		total += itf94.CallMe (94);
		mrp.CanCastToCalled = false;
		itf94 = (I94)o;
		if (mrp.CanCastToCalled)
			return 94;
		total += itf94.CallMe (94);

		mrp.CanCastToCalled = false;
		I95 itf95 = (I95)o;
		if (!mrp.CanCastToCalled)
			return 95;
		total += itf95.CallMe (95);
		mrp.CanCastToCalled = false;
		itf95 = (I95)o;
		if (mrp.CanCastToCalled)
			return 95;
		total += itf95.CallMe (95);

		mrp.CanCastToCalled = false;
		I96 itf96 = (I96)o;
		if (!mrp.CanCastToCalled)
			return 96;
		total += itf96.CallMe (96);
		mrp.CanCastToCalled = false;
		itf96 = (I96)o;
		if (mrp.CanCastToCalled)
			return 96;
		total += itf96.CallMe (96);

		mrp.CanCastToCalled = false;
		I97 itf97 = (I97)o;
		if (!mrp.CanCastToCalled)
			return 97;
		total += itf97.CallMe (97);
		mrp.CanCastToCalled = false;
		itf97 = (I97)o;
		if (mrp.CanCastToCalled)
			return 97;
		total += itf97.CallMe (97);

		mrp.CanCastToCalled = false;
		I98 itf98 = (I98)o;
		if (!mrp.CanCastToCalled)
			return 98;
		total += itf98.CallMe (98);
		mrp.CanCastToCalled = false;
		itf98 = (I98)o;
		if (mrp.CanCastToCalled)
			return 98;
		total += itf98.CallMe (98);

		mrp.CanCastToCalled = false;
		I99 itf99 = (I99)o;
		if (!mrp.CanCastToCalled)
			return 99;
		total += itf99.CallMe (99);
		mrp.CanCastToCalled = false;
		itf99 = (I99)o;
		if (mrp.CanCastToCalled)
			return 99;
		total += itf99.CallMe (99);

		mrp.CanCastToCalled = false;
		I100 itf100 = (I100)o;
		if (!mrp.CanCastToCalled)
			return 100;
		total += itf100.CallMe (100);
		mrp.CanCastToCalled = false;
		itf100 = (I100)o;
		if (mrp.CanCastToCalled)
			return 100;
		total += itf100.CallMe (100);

		mrp.CanCastToCalled = false;
		I101 itf101 = (I101)o;
		if (!mrp.CanCastToCalled)
			return 101;
		total += itf101.CallMe (101);
		mrp.CanCastToCalled = false;
		itf101 = (I101)o;
		if (mrp.CanCastToCalled)
			return 101;
		total += itf101.CallMe (101);

		mrp.CanCastToCalled = false;
		I102 itf102 = (I102)o;
		if (!mrp.CanCastToCalled)
			return 102;
		total += itf102.CallMe (102);
		mrp.CanCastToCalled = false;
		itf102 = (I102)o;
		if (mrp.CanCastToCalled)
			return 102;
		total += itf102.CallMe (102);

		mrp.CanCastToCalled = false;
		I103 itf103 = (I103)o;
		if (!mrp.CanCastToCalled)
			return 103;
		total += itf103.CallMe (103);
		mrp.CanCastToCalled = false;
		itf103 = (I103)o;
		if (mrp.CanCastToCalled)
			return 103;
		total += itf103.CallMe (103);

		mrp.CanCastToCalled = false;
		I104 itf104 = (I104)o;
		if (!mrp.CanCastToCalled)
			return 104;
		total += itf104.CallMe (104);
		mrp.CanCastToCalled = false;
		itf104 = (I104)o;
		if (mrp.CanCastToCalled)
			return 104;
		total += itf104.CallMe (104);

		mrp.CanCastToCalled = false;
		I105 itf105 = (I105)o;
		if (!mrp.CanCastToCalled)
			return 105;
		total += itf105.CallMe (105);
		mrp.CanCastToCalled = false;
		itf105 = (I105)o;
		if (mrp.CanCastToCalled)
			return 105;
		total += itf105.CallMe (105);

		mrp.CanCastToCalled = false;
		I106 itf106 = (I106)o;
		if (!mrp.CanCastToCalled)
			return 106;
		total += itf106.CallMe (106);
		mrp.CanCastToCalled = false;
		itf106 = (I106)o;
		if (mrp.CanCastToCalled)
			return 106;
		total += itf106.CallMe (106);

		mrp.CanCastToCalled = false;
		I107 itf107 = (I107)o;
		if (!mrp.CanCastToCalled)
			return 107;
		total += itf107.CallMe (107);
		mrp.CanCastToCalled = false;
		itf107 = (I107)o;
		if (mrp.CanCastToCalled)
			return 107;
		total += itf107.CallMe (107);

		mrp.CanCastToCalled = false;
		I108 itf108 = (I108)o;
		if (!mrp.CanCastToCalled)
			return 108;
		total += itf108.CallMe (108);
		mrp.CanCastToCalled = false;
		itf108 = (I108)o;
		if (mrp.CanCastToCalled)
			return 108;
		total += itf108.CallMe (108);

		mrp.CanCastToCalled = false;
		I109 itf109 = (I109)o;
		if (!mrp.CanCastToCalled)
			return 109;
		total += itf109.CallMe (109);
		mrp.CanCastToCalled = false;
		itf109 = (I109)o;
		if (mrp.CanCastToCalled)
			return 109;
		total += itf109.CallMe (109);

		mrp.CanCastToCalled = false;
		I110 itf110 = (I110)o;
		if (!mrp.CanCastToCalled)
			return 110;
		total += itf110.CallMe (110);
		mrp.CanCastToCalled = false;
		itf110 = (I110)o;
		if (mrp.CanCastToCalled)
			return 110;
		total += itf110.CallMe (110);

		mrp.CanCastToCalled = false;
		I111 itf111 = (I111)o;
		if (!mrp.CanCastToCalled)
			return 111;
		total += itf111.CallMe (111);
		mrp.CanCastToCalled = false;
		itf111 = (I111)o;
		if (mrp.CanCastToCalled)
			return 111;
		total += itf111.CallMe (111);

		mrp.CanCastToCalled = false;
		I112 itf112 = (I112)o;
		if (!mrp.CanCastToCalled)
			return 112;
		total += itf112.CallMe (112);
		mrp.CanCastToCalled = false;
		itf112 = (I112)o;
		if (mrp.CanCastToCalled)
			return 112;
		total += itf112.CallMe (112);

		mrp.CanCastToCalled = false;
		I113 itf113 = (I113)o;
		if (!mrp.CanCastToCalled)
			return 113;
		total += itf113.CallMe (113);
		mrp.CanCastToCalled = false;
		itf113 = (I113)o;
		if (mrp.CanCastToCalled)
			return 113;
		total += itf113.CallMe (113);

		mrp.CanCastToCalled = false;
		I114 itf114 = (I114)o;
		if (!mrp.CanCastToCalled)
			return 114;
		total += itf114.CallMe (114);
		mrp.CanCastToCalled = false;
		itf114 = (I114)o;
		if (mrp.CanCastToCalled)
			return 114;
		total += itf114.CallMe (114);

		mrp.CanCastToCalled = false;
		I115 itf115 = (I115)o;
		if (!mrp.CanCastToCalled)
			return 115;
		total += itf115.CallMe (115);
		mrp.CanCastToCalled = false;
		itf115 = (I115)o;
		if (mrp.CanCastToCalled)
			return 115;
		total += itf115.CallMe (115);

		mrp.CanCastToCalled = false;
		I116 itf116 = (I116)o;
		if (!mrp.CanCastToCalled)
			return 116;
		total += itf116.CallMe (116);
		mrp.CanCastToCalled = false;
		itf116 = (I116)o;
		if (mrp.CanCastToCalled)
			return 116;
		total += itf116.CallMe (116);

		mrp.CanCastToCalled = false;
		I117 itf117 = (I117)o;
		if (!mrp.CanCastToCalled)
			return 117;
		total += itf117.CallMe (117);
		mrp.CanCastToCalled = false;
		itf117 = (I117)o;
		if (mrp.CanCastToCalled)
			return 117;
		total += itf117.CallMe (117);

		mrp.CanCastToCalled = false;
		I118 itf118 = (I118)o;
		if (!mrp.CanCastToCalled)
			return 118;
		total += itf118.CallMe (118);
		mrp.CanCastToCalled = false;
		itf118 = (I118)o;
		if (mrp.CanCastToCalled)
			return 118;
		total += itf118.CallMe (118);

		mrp.CanCastToCalled = false;
		I119 itf119 = (I119)o;
		if (!mrp.CanCastToCalled)
			return 119;
		total += itf119.CallMe (119);
		mrp.CanCastToCalled = false;
		itf119 = (I119)o;
		if (mrp.CanCastToCalled)
			return 119;
		total += itf119.CallMe (119);

		mrp.CanCastToCalled = false;
		I120 itf120 = (I120)o;
		if (!mrp.CanCastToCalled)
			return 120;
		total += itf120.CallMe (120);
		mrp.CanCastToCalled = false;
		itf120 = (I120)o;
		if (mrp.CanCastToCalled)
			return 120;
		total += itf120.CallMe (120);

		mrp.CanCastToCalled = false;
		I121 itf121 = (I121)o;
		if (!mrp.CanCastToCalled)
			return 121;
		total += itf121.CallMe (121);
		mrp.CanCastToCalled = false;
		itf121 = (I121)o;
		if (mrp.CanCastToCalled)
			return 121;
		total += itf121.CallMe (121);

		mrp.CanCastToCalled = false;
		I122 itf122 = (I122)o;
		if (!mrp.CanCastToCalled)
			return 122;
		total += itf122.CallMe (122);
		mrp.CanCastToCalled = false;
		itf122 = (I122)o;
		if (mrp.CanCastToCalled)
			return 122;
		total += itf122.CallMe (122);

		mrp.CanCastToCalled = false;
		I123 itf123 = (I123)o;
		if (!mrp.CanCastToCalled)
			return 123;
		total += itf123.CallMe (123);
		mrp.CanCastToCalled = false;
		itf123 = (I123)o;
		if (mrp.CanCastToCalled)
			return 123;
		total += itf123.CallMe (123);

		mrp.CanCastToCalled = false;
		I124 itf124 = (I124)o;
		if (!mrp.CanCastToCalled)
			return 124;
		total += itf124.CallMe (124);
		mrp.CanCastToCalled = false;
		itf124 = (I124)o;
		if (mrp.CanCastToCalled)
			return 124;
		total += itf124.CallMe (124);

		mrp.CanCastToCalled = false;
		I125 itf125 = (I125)o;
		if (!mrp.CanCastToCalled)
			return 125;
		total += itf125.CallMe (125);
		mrp.CanCastToCalled = false;
		itf125 = (I125)o;
		if (mrp.CanCastToCalled)
			return 125;
		total += itf125.CallMe (125);

		mrp.CanCastToCalled = false;
		I126 itf126 = (I126)o;
		if (!mrp.CanCastToCalled)
			return 126;
		total += itf126.CallMe (126);
		mrp.CanCastToCalled = false;
		itf126 = (I126)o;
		if (mrp.CanCastToCalled)
			return 126;
		total += itf126.CallMe (126);

		mrp.CanCastToCalled = false;
		I127 itf127 = (I127)o;
		if (!mrp.CanCastToCalled)
			return 127;
		total += itf127.CallMe (127);
		mrp.CanCastToCalled = false;
		itf127 = (I127)o;
		if (mrp.CanCastToCalled)
			return 127;
		total += itf127.CallMe (127);

		mrp.CanCastToCalled = false;
		I128 itf128 = (I128)o;
		if (!mrp.CanCastToCalled)
			return 128;
		total += itf128.CallMe (128);
		mrp.CanCastToCalled = false;
		itf128 = (I128)o;
		if (mrp.CanCastToCalled)
			return 128;
		total += itf128.CallMe (128);

		mrp.CanCastToCalled = false;
		I129 itf129 = (I129)o;
		if (!mrp.CanCastToCalled)
			return 129;
		total += itf129.CallMe (129);
		mrp.CanCastToCalled = false;
		itf129 = (I129)o;
		if (mrp.CanCastToCalled)
			return 129;
		total += itf129.CallMe (129);

		mrp.CanCastToCalled = false;
		I130 itf130 = (I130)o;
		if (!mrp.CanCastToCalled)
			return 130;
		total += itf130.CallMe (130);
		mrp.CanCastToCalled = false;
		itf130 = (I130)o;
		if (mrp.CanCastToCalled)
			return 130;
		total += itf130.CallMe (130);

		mrp.CanCastToCalled = false;
		I131 itf131 = (I131)o;
		if (!mrp.CanCastToCalled)
			return 131;
		total += itf131.CallMe (131);
		mrp.CanCastToCalled = false;
		itf131 = (I131)o;
		if (mrp.CanCastToCalled)
			return 131;
		total += itf131.CallMe (131);

		mrp.CanCastToCalled = false;
		I132 itf132 = (I132)o;
		if (!mrp.CanCastToCalled)
			return 132;
		total += itf132.CallMe (132);
		mrp.CanCastToCalled = false;
		itf132 = (I132)o;
		if (mrp.CanCastToCalled)
			return 132;
		total += itf132.CallMe (132);

		mrp.CanCastToCalled = false;
		I133 itf133 = (I133)o;
		if (!mrp.CanCastToCalled)
			return 133;
		total += itf133.CallMe (133);
		mrp.CanCastToCalled = false;
		itf133 = (I133)o;
		if (mrp.CanCastToCalled)
			return 133;
		total += itf133.CallMe (133);

		mrp.CanCastToCalled = false;
		I134 itf134 = (I134)o;
		if (!mrp.CanCastToCalled)
			return 134;
		total += itf134.CallMe (134);
		mrp.CanCastToCalled = false;
		itf134 = (I134)o;
		if (mrp.CanCastToCalled)
			return 134;
		total += itf134.CallMe (134);

		mrp.CanCastToCalled = false;
		I135 itf135 = (I135)o;
		if (!mrp.CanCastToCalled)
			return 135;
		total += itf135.CallMe (135);
		mrp.CanCastToCalled = false;
		itf135 = (I135)o;
		if (mrp.CanCastToCalled)
			return 135;
		total += itf135.CallMe (135);

		mrp.CanCastToCalled = false;
		I136 itf136 = (I136)o;
		if (!mrp.CanCastToCalled)
			return 136;
		total += itf136.CallMe (136);
		mrp.CanCastToCalled = false;
		itf136 = (I136)o;
		if (mrp.CanCastToCalled)
			return 136;
		total += itf136.CallMe (136);

		mrp.CanCastToCalled = false;
		I137 itf137 = (I137)o;
		if (!mrp.CanCastToCalled)
			return 137;
		total += itf137.CallMe (137);
		mrp.CanCastToCalled = false;
		itf137 = (I137)o;
		if (mrp.CanCastToCalled)
			return 137;
		total += itf137.CallMe (137);

		mrp.CanCastToCalled = false;
		I138 itf138 = (I138)o;
		if (!mrp.CanCastToCalled)
			return 138;
		total += itf138.CallMe (138);
		mrp.CanCastToCalled = false;
		itf138 = (I138)o;
		if (mrp.CanCastToCalled)
			return 138;
		total += itf138.CallMe (138);

		mrp.CanCastToCalled = false;
		I139 itf139 = (I139)o;
		if (!mrp.CanCastToCalled)
			return 139;
		total += itf139.CallMe (139);
		mrp.CanCastToCalled = false;
		itf139 = (I139)o;
		if (mrp.CanCastToCalled)
			return 139;
		total += itf139.CallMe (139);

		mrp.CanCastToCalled = false;
		I140 itf140 = (I140)o;
		if (!mrp.CanCastToCalled)
			return 140;
		total += itf140.CallMe (140);
		mrp.CanCastToCalled = false;
		itf140 = (I140)o;
		if (mrp.CanCastToCalled)
			return 140;
		total += itf140.CallMe (140);

		mrp.CanCastToCalled = false;
		I141 itf141 = (I141)o;
		if (!mrp.CanCastToCalled)
			return 141;
		total += itf141.CallMe (141);
		mrp.CanCastToCalled = false;
		itf141 = (I141)o;
		if (mrp.CanCastToCalled)
			return 141;
		total += itf141.CallMe (141);

		mrp.CanCastToCalled = false;
		I142 itf142 = (I142)o;
		if (!mrp.CanCastToCalled)
			return 142;
		total += itf142.CallMe (142);
		mrp.CanCastToCalled = false;
		itf142 = (I142)o;
		if (mrp.CanCastToCalled)
			return 142;
		total += itf142.CallMe (142);

		mrp.CanCastToCalled = false;
		I143 itf143 = (I143)o;
		if (!mrp.CanCastToCalled)
			return 143;
		total += itf143.CallMe (143);
		mrp.CanCastToCalled = false;
		itf143 = (I143)o;
		if (mrp.CanCastToCalled)
			return 143;
		total += itf143.CallMe (143);

		mrp.CanCastToCalled = false;
		I144 itf144 = (I144)o;
		if (!mrp.CanCastToCalled)
			return 144;
		total += itf144.CallMe (144);
		mrp.CanCastToCalled = false;
		itf144 = (I144)o;
		if (mrp.CanCastToCalled)
			return 144;
		total += itf144.CallMe (144);

		mrp.CanCastToCalled = false;
		I145 itf145 = (I145)o;
		if (!mrp.CanCastToCalled)
			return 145;
		total += itf145.CallMe (145);
		mrp.CanCastToCalled = false;
		itf145 = (I145)o;
		if (mrp.CanCastToCalled)
			return 145;
		total += itf145.CallMe (145);

		mrp.CanCastToCalled = false;
		I146 itf146 = (I146)o;
		if (!mrp.CanCastToCalled)
			return 146;
		total += itf146.CallMe (146);
		mrp.CanCastToCalled = false;
		itf146 = (I146)o;
		if (mrp.CanCastToCalled)
			return 146;
		total += itf146.CallMe (146);

		mrp.CanCastToCalled = false;
		I147 itf147 = (I147)o;
		if (!mrp.CanCastToCalled)
			return 147;
		total += itf147.CallMe (147);
		mrp.CanCastToCalled = false;
		itf147 = (I147)o;
		if (mrp.CanCastToCalled)
			return 147;
		total += itf147.CallMe (147);

		mrp.CanCastToCalled = false;
		I148 itf148 = (I148)o;
		if (!mrp.CanCastToCalled)
			return 148;
		total += itf148.CallMe (148);
		mrp.CanCastToCalled = false;
		itf148 = (I148)o;
		if (mrp.CanCastToCalled)
			return 148;
		total += itf148.CallMe (148);

		mrp.CanCastToCalled = false;
		I149 itf149 = (I149)o;
		if (!mrp.CanCastToCalled)
			return 149;
		total += itf149.CallMe (149);
		mrp.CanCastToCalled = false;
		itf149 = (I149)o;
		if (mrp.CanCastToCalled)
			return 149;
		total += itf149.CallMe (149);

		mrp.CanCastToCalled = false;
		I150 itf150 = (I150)o;
		if (!mrp.CanCastToCalled)
			return 150;
		total += itf150.CallMe (150);
		mrp.CanCastToCalled = false;
		itf150 = (I150)o;
		if (mrp.CanCastToCalled)
			return 150;
		total += itf150.CallMe (150);

		mrp.CanCastToCalled = false;
		I151 itf151 = (I151)o;
		if (!mrp.CanCastToCalled)
			return 151;
		total += itf151.CallMe (151);
		mrp.CanCastToCalled = false;
		itf151 = (I151)o;
		if (mrp.CanCastToCalled)
			return 151;
		total += itf151.CallMe (151);

		mrp.CanCastToCalled = false;
		I152 itf152 = (I152)o;
		if (!mrp.CanCastToCalled)
			return 152;
		total += itf152.CallMe (152);
		mrp.CanCastToCalled = false;
		itf152 = (I152)o;
		if (mrp.CanCastToCalled)
			return 152;
		total += itf152.CallMe (152);

		mrp.CanCastToCalled = false;
		I153 itf153 = (I153)o;
		if (!mrp.CanCastToCalled)
			return 153;
		total += itf153.CallMe (153);
		mrp.CanCastToCalled = false;
		itf153 = (I153)o;
		if (mrp.CanCastToCalled)
			return 153;
		total += itf153.CallMe (153);

		mrp.CanCastToCalled = false;
		I154 itf154 = (I154)o;
		if (!mrp.CanCastToCalled)
			return 154;
		total += itf154.CallMe (154);
		mrp.CanCastToCalled = false;
		itf154 = (I154)o;
		if (mrp.CanCastToCalled)
			return 154;
		total += itf154.CallMe (154);

		mrp.CanCastToCalled = false;
		I155 itf155 = (I155)o;
		if (!mrp.CanCastToCalled)
			return 155;
		total += itf155.CallMe (155);
		mrp.CanCastToCalled = false;
		itf155 = (I155)o;
		if (mrp.CanCastToCalled)
			return 155;
		total += itf155.CallMe (155);

		mrp.CanCastToCalled = false;
		I156 itf156 = (I156)o;
		if (!mrp.CanCastToCalled)
			return 156;
		total += itf156.CallMe (156);
		mrp.CanCastToCalled = false;
		itf156 = (I156)o;
		if (mrp.CanCastToCalled)
			return 156;
		total += itf156.CallMe (156);

		mrp.CanCastToCalled = false;
		I157 itf157 = (I157)o;
		if (!mrp.CanCastToCalled)
			return 157;
		total += itf157.CallMe (157);
		mrp.CanCastToCalled = false;
		itf157 = (I157)o;
		if (mrp.CanCastToCalled)
			return 157;
		total += itf157.CallMe (157);

		mrp.CanCastToCalled = false;
		I158 itf158 = (I158)o;
		if (!mrp.CanCastToCalled)
			return 158;
		total += itf158.CallMe (158);
		mrp.CanCastToCalled = false;
		itf158 = (I158)o;
		if (mrp.CanCastToCalled)
			return 158;
		total += itf158.CallMe (158);

		mrp.CanCastToCalled = false;
		I159 itf159 = (I159)o;
		if (!mrp.CanCastToCalled)
			return 159;
		total += itf159.CallMe (159);
		mrp.CanCastToCalled = false;
		itf159 = (I159)o;
		if (mrp.CanCastToCalled)
			return 159;
		total += itf159.CallMe (159);

		mrp.CanCastToCalled = false;
		I160 itf160 = (I160)o;
		if (!mrp.CanCastToCalled)
			return 160;
		total += itf160.CallMe (160);
		mrp.CanCastToCalled = false;
		itf160 = (I160)o;
		if (mrp.CanCastToCalled)
			return 160;
		total += itf160.CallMe (160);

		mrp.CanCastToCalled = false;
		I161 itf161 = (I161)o;
		if (!mrp.CanCastToCalled)
			return 161;
		total += itf161.CallMe (161);
		mrp.CanCastToCalled = false;
		itf161 = (I161)o;
		if (mrp.CanCastToCalled)
			return 161;
		total += itf161.CallMe (161);

		mrp.CanCastToCalled = false;
		I162 itf162 = (I162)o;
		if (!mrp.CanCastToCalled)
			return 162;
		total += itf162.CallMe (162);
		mrp.CanCastToCalled = false;
		itf162 = (I162)o;
		if (mrp.CanCastToCalled)
			return 162;
		total += itf162.CallMe (162);

		mrp.CanCastToCalled = false;
		I163 itf163 = (I163)o;
		if (!mrp.CanCastToCalled)
			return 163;
		total += itf163.CallMe (163);
		mrp.CanCastToCalled = false;
		itf163 = (I163)o;
		if (mrp.CanCastToCalled)
			return 163;
		total += itf163.CallMe (163);

		mrp.CanCastToCalled = false;
		I164 itf164 = (I164)o;
		if (!mrp.CanCastToCalled)
			return 164;
		total += itf164.CallMe (164);
		mrp.CanCastToCalled = false;
		itf164 = (I164)o;
		if (mrp.CanCastToCalled)
			return 164;
		total += itf164.CallMe (164);

		mrp.CanCastToCalled = false;
		I165 itf165 = (I165)o;
		if (!mrp.CanCastToCalled)
			return 165;
		total += itf165.CallMe (165);
		mrp.CanCastToCalled = false;
		itf165 = (I165)o;
		if (mrp.CanCastToCalled)
			return 165;
		total += itf165.CallMe (165);

		mrp.CanCastToCalled = false;
		I166 itf166 = (I166)o;
		if (!mrp.CanCastToCalled)
			return 166;
		total += itf166.CallMe (166);
		mrp.CanCastToCalled = false;
		itf166 = (I166)o;
		if (mrp.CanCastToCalled)
			return 166;
		total += itf166.CallMe (166);

		mrp.CanCastToCalled = false;
		I167 itf167 = (I167)o;
		if (!mrp.CanCastToCalled)
			return 167;
		total += itf167.CallMe (167);
		mrp.CanCastToCalled = false;
		itf167 = (I167)o;
		if (mrp.CanCastToCalled)
			return 167;
		total += itf167.CallMe (167);

		mrp.CanCastToCalled = false;
		I168 itf168 = (I168)o;
		if (!mrp.CanCastToCalled)
			return 168;
		total += itf168.CallMe (168);
		mrp.CanCastToCalled = false;
		itf168 = (I168)o;
		if (mrp.CanCastToCalled)
			return 168;
		total += itf168.CallMe (168);

		mrp.CanCastToCalled = false;
		I169 itf169 = (I169)o;
		if (!mrp.CanCastToCalled)
			return 169;
		total += itf169.CallMe (169);
		mrp.CanCastToCalled = false;
		itf169 = (I169)o;
		if (mrp.CanCastToCalled)
			return 169;
		total += itf169.CallMe (169);

		mrp.CanCastToCalled = false;
		I170 itf170 = (I170)o;
		if (!mrp.CanCastToCalled)
			return 170;
		total += itf170.CallMe (170);
		mrp.CanCastToCalled = false;
		itf170 = (I170)o;
		if (mrp.CanCastToCalled)
			return 170;
		total += itf170.CallMe (170);

		mrp.CanCastToCalled = false;
		I171 itf171 = (I171)o;
		if (!mrp.CanCastToCalled)
			return 171;
		total += itf171.CallMe (171);
		mrp.CanCastToCalled = false;
		itf171 = (I171)o;
		if (mrp.CanCastToCalled)
			return 171;
		total += itf171.CallMe (171);

		mrp.CanCastToCalled = false;
		I172 itf172 = (I172)o;
		if (!mrp.CanCastToCalled)
			return 172;
		total += itf172.CallMe (172);
		mrp.CanCastToCalled = false;
		itf172 = (I172)o;
		if (mrp.CanCastToCalled)
			return 172;
		total += itf172.CallMe (172);

		mrp.CanCastToCalled = false;
		I173 itf173 = (I173)o;
		if (!mrp.CanCastToCalled)
			return 173;
		total += itf173.CallMe (173);
		mrp.CanCastToCalled = false;
		itf173 = (I173)o;
		if (mrp.CanCastToCalled)
			return 173;
		total += itf173.CallMe (173);

		mrp.CanCastToCalled = false;
		I174 itf174 = (I174)o;
		if (!mrp.CanCastToCalled)
			return 174;
		total += itf174.CallMe (174);
		mrp.CanCastToCalled = false;
		itf174 = (I174)o;
		if (mrp.CanCastToCalled)
			return 174;
		total += itf174.CallMe (174);

		mrp.CanCastToCalled = false;
		I175 itf175 = (I175)o;
		if (!mrp.CanCastToCalled)
			return 175;
		total += itf175.CallMe (175);
		mrp.CanCastToCalled = false;
		itf175 = (I175)o;
		if (mrp.CanCastToCalled)
			return 175;
		total += itf175.CallMe (175);

		mrp.CanCastToCalled = false;
		I176 itf176 = (I176)o;
		if (!mrp.CanCastToCalled)
			return 176;
		total += itf176.CallMe (176);
		mrp.CanCastToCalled = false;
		itf176 = (I176)o;
		if (mrp.CanCastToCalled)
			return 176;
		total += itf176.CallMe (176);

		mrp.CanCastToCalled = false;
		I177 itf177 = (I177)o;
		if (!mrp.CanCastToCalled)
			return 177;
		total += itf177.CallMe (177);
		mrp.CanCastToCalled = false;
		itf177 = (I177)o;
		if (mrp.CanCastToCalled)
			return 177;
		total += itf177.CallMe (177);

		mrp.CanCastToCalled = false;
		I178 itf178 = (I178)o;
		if (!mrp.CanCastToCalled)
			return 178;
		total += itf178.CallMe (178);
		mrp.CanCastToCalled = false;
		itf178 = (I178)o;
		if (mrp.CanCastToCalled)
			return 178;
		total += itf178.CallMe (178);

		mrp.CanCastToCalled = false;
		I179 itf179 = (I179)o;
		if (!mrp.CanCastToCalled)
			return 179;
		total += itf179.CallMe (179);
		mrp.CanCastToCalled = false;
		itf179 = (I179)o;
		if (mrp.CanCastToCalled)
			return 179;
		total += itf179.CallMe (179);

		mrp.CanCastToCalled = false;
		I180 itf180 = (I180)o;
		if (!mrp.CanCastToCalled)
			return 180;
		total += itf180.CallMe (180);
		mrp.CanCastToCalled = false;
		itf180 = (I180)o;
		if (mrp.CanCastToCalled)
			return 180;
		total += itf180.CallMe (180);

		mrp.CanCastToCalled = false;
		I181 itf181 = (I181)o;
		if (!mrp.CanCastToCalled)
			return 181;
		total += itf181.CallMe (181);
		mrp.CanCastToCalled = false;
		itf181 = (I181)o;
		if (mrp.CanCastToCalled)
			return 181;
		total += itf181.CallMe (181);

		mrp.CanCastToCalled = false;
		I182 itf182 = (I182)o;
		if (!mrp.CanCastToCalled)
			return 182;
		total += itf182.CallMe (182);
		mrp.CanCastToCalled = false;
		itf182 = (I182)o;
		if (mrp.CanCastToCalled)
			return 182;
		total += itf182.CallMe (182);

		mrp.CanCastToCalled = false;
		I183 itf183 = (I183)o;
		if (!mrp.CanCastToCalled)
			return 183;
		total += itf183.CallMe (183);
		mrp.CanCastToCalled = false;
		itf183 = (I183)o;
		if (mrp.CanCastToCalled)
			return 183;
		total += itf183.CallMe (183);

		mrp.CanCastToCalled = false;
		I184 itf184 = (I184)o;
		if (!mrp.CanCastToCalled)
			return 184;
		total += itf184.CallMe (184);
		mrp.CanCastToCalled = false;
		itf184 = (I184)o;
		if (mrp.CanCastToCalled)
			return 184;
		total += itf184.CallMe (184);

		mrp.CanCastToCalled = false;
		I185 itf185 = (I185)o;
		if (!mrp.CanCastToCalled)
			return 185;
		total += itf185.CallMe (185);
		mrp.CanCastToCalled = false;
		itf185 = (I185)o;
		if (mrp.CanCastToCalled)
			return 185;
		total += itf185.CallMe (185);

		mrp.CanCastToCalled = false;
		I186 itf186 = (I186)o;
		if (!mrp.CanCastToCalled)
			return 186;
		total += itf186.CallMe (186);
		mrp.CanCastToCalled = false;
		itf186 = (I186)o;
		if (mrp.CanCastToCalled)
			return 186;
		total += itf186.CallMe (186);

		mrp.CanCastToCalled = false;
		I187 itf187 = (I187)o;
		if (!mrp.CanCastToCalled)
			return 187;
		total += itf187.CallMe (187);
		mrp.CanCastToCalled = false;
		itf187 = (I187)o;
		if (mrp.CanCastToCalled)
			return 187;
		total += itf187.CallMe (187);

		mrp.CanCastToCalled = false;
		I188 itf188 = (I188)o;
		if (!mrp.CanCastToCalled)
			return 188;
		total += itf188.CallMe (188);
		mrp.CanCastToCalled = false;
		itf188 = (I188)o;
		if (mrp.CanCastToCalled)
			return 188;
		total += itf188.CallMe (188);

		mrp.CanCastToCalled = false;
		I189 itf189 = (I189)o;
		if (!mrp.CanCastToCalled)
			return 189;
		total += itf189.CallMe (189);
		mrp.CanCastToCalled = false;
		itf189 = (I189)o;
		if (mrp.CanCastToCalled)
			return 189;
		total += itf189.CallMe (189);

		mrp.CanCastToCalled = false;
		I190 itf190 = (I190)o;
		if (!mrp.CanCastToCalled)
			return 190;
		total += itf190.CallMe (190);
		mrp.CanCastToCalled = false;
		itf190 = (I190)o;
		if (mrp.CanCastToCalled)
			return 190;
		total += itf190.CallMe (190);

		mrp.CanCastToCalled = false;
		I191 itf191 = (I191)o;
		if (!mrp.CanCastToCalled)
			return 191;
		total += itf191.CallMe (191);
		mrp.CanCastToCalled = false;
		itf191 = (I191)o;
		if (mrp.CanCastToCalled)
			return 191;
		total += itf191.CallMe (191);

		mrp.CanCastToCalled = false;
		I192 itf192 = (I192)o;
		if (!mrp.CanCastToCalled)
			return 192;
		total += itf192.CallMe (192);
		mrp.CanCastToCalled = false;
		itf192 = (I192)o;
		if (mrp.CanCastToCalled)
			return 192;
		total += itf192.CallMe (192);

		mrp.CanCastToCalled = false;
		I193 itf193 = (I193)o;
		if (!mrp.CanCastToCalled)
			return 193;
		total += itf193.CallMe (193);
		mrp.CanCastToCalled = false;
		itf193 = (I193)o;
		if (mrp.CanCastToCalled)
			return 193;
		total += itf193.CallMe (193);

		mrp.CanCastToCalled = false;
		I194 itf194 = (I194)o;
		if (!mrp.CanCastToCalled)
			return 194;
		total += itf194.CallMe (194);
		mrp.CanCastToCalled = false;
		itf194 = (I194)o;
		if (mrp.CanCastToCalled)
			return 194;
		total += itf194.CallMe (194);

		mrp.CanCastToCalled = false;
		I195 itf195 = (I195)o;
		if (!mrp.CanCastToCalled)
			return 195;
		total += itf195.CallMe (195);
		mrp.CanCastToCalled = false;
		itf195 = (I195)o;
		if (mrp.CanCastToCalled)
			return 195;
		total += itf195.CallMe (195);

		mrp.CanCastToCalled = false;
		I196 itf196 = (I196)o;
		if (!mrp.CanCastToCalled)
			return 196;
		total += itf196.CallMe (196);
		mrp.CanCastToCalled = false;
		itf196 = (I196)o;
		if (mrp.CanCastToCalled)
			return 196;
		total += itf196.CallMe (196);

		mrp.CanCastToCalled = false;
		I197 itf197 = (I197)o;
		if (!mrp.CanCastToCalled)
			return 197;
		total += itf197.CallMe (197);
		mrp.CanCastToCalled = false;
		itf197 = (I197)o;
		if (mrp.CanCastToCalled)
			return 197;
		total += itf197.CallMe (197);

		mrp.CanCastToCalled = false;
		I198 itf198 = (I198)o;
		if (!mrp.CanCastToCalled)
			return 198;
		total += itf198.CallMe (198);
		mrp.CanCastToCalled = false;
		itf198 = (I198)o;
		if (mrp.CanCastToCalled)
			return 198;
		total += itf198.CallMe (198);

		mrp.CanCastToCalled = false;
		I199 itf199 = (I199)o;
		if (!mrp.CanCastToCalled)
			return 199;
		total += itf199.CallMe (199);
		mrp.CanCastToCalled = false;
		itf199 = (I199)o;
		if (mrp.CanCastToCalled)
			return 199;
		total += itf199.CallMe (199);

		mrp.CanCastToCalled = false;
		I200 itf200 = (I200)o;
		if (!mrp.CanCastToCalled)
			return 200;
		total += itf200.CallMe (200);
		mrp.CanCastToCalled = false;
		itf200 = (I200)o;
		if (mrp.CanCastToCalled)
			return 200;
		total += itf200.CallMe (200);

		mrp.CanCastToCalled = false;
		I201 itf201 = (I201)o;
		if (!mrp.CanCastToCalled)
			return 201;
		total += itf201.CallMe (201);
		mrp.CanCastToCalled = false;
		itf201 = (I201)o;
		if (mrp.CanCastToCalled)
			return 201;
		total += itf201.CallMe (201);

		mrp.CanCastToCalled = false;
		I202 itf202 = (I202)o;
		if (!mrp.CanCastToCalled)
			return 202;
		total += itf202.CallMe (202);
		mrp.CanCastToCalled = false;
		itf202 = (I202)o;
		if (mrp.CanCastToCalled)
			return 202;
		total += itf202.CallMe (202);

		mrp.CanCastToCalled = false;
		I203 itf203 = (I203)o;
		if (!mrp.CanCastToCalled)
			return 203;
		total += itf203.CallMe (203);
		mrp.CanCastToCalled = false;
		itf203 = (I203)o;
		if (mrp.CanCastToCalled)
			return 203;
		total += itf203.CallMe (203);

		mrp.CanCastToCalled = false;
		I204 itf204 = (I204)o;
		if (!mrp.CanCastToCalled)
			return 204;
		total += itf204.CallMe (204);
		mrp.CanCastToCalled = false;
		itf204 = (I204)o;
		if (mrp.CanCastToCalled)
			return 204;
		total += itf204.CallMe (204);

		mrp.CanCastToCalled = false;
		I205 itf205 = (I205)o;
		if (!mrp.CanCastToCalled)
			return 205;
		total += itf205.CallMe (205);
		mrp.CanCastToCalled = false;
		itf205 = (I205)o;
		if (mrp.CanCastToCalled)
			return 205;
		total += itf205.CallMe (205);

		mrp.CanCastToCalled = false;
		I206 itf206 = (I206)o;
		if (!mrp.CanCastToCalled)
			return 206;
		total += itf206.CallMe (206);
		mrp.CanCastToCalled = false;
		itf206 = (I206)o;
		if (mrp.CanCastToCalled)
			return 206;
		total += itf206.CallMe (206);

		mrp.CanCastToCalled = false;
		I207 itf207 = (I207)o;
		if (!mrp.CanCastToCalled)
			return 207;
		total += itf207.CallMe (207);
		mrp.CanCastToCalled = false;
		itf207 = (I207)o;
		if (mrp.CanCastToCalled)
			return 207;
		total += itf207.CallMe (207);

		mrp.CanCastToCalled = false;
		I208 itf208 = (I208)o;
		if (!mrp.CanCastToCalled)
			return 208;
		total += itf208.CallMe (208);
		mrp.CanCastToCalled = false;
		itf208 = (I208)o;
		if (mrp.CanCastToCalled)
			return 208;
		total += itf208.CallMe (208);

		mrp.CanCastToCalled = false;
		I209 itf209 = (I209)o;
		if (!mrp.CanCastToCalled)
			return 209;
		total += itf209.CallMe (209);
		mrp.CanCastToCalled = false;
		itf209 = (I209)o;
		if (mrp.CanCastToCalled)
			return 209;
		total += itf209.CallMe (209);

		mrp.CanCastToCalled = false;
		I210 itf210 = (I210)o;
		if (!mrp.CanCastToCalled)
			return 210;
		total += itf210.CallMe (210);
		mrp.CanCastToCalled = false;
		itf210 = (I210)o;
		if (mrp.CanCastToCalled)
			return 210;
		total += itf210.CallMe (210);

		mrp.CanCastToCalled = false;
		I211 itf211 = (I211)o;
		if (!mrp.CanCastToCalled)
			return 211;
		total += itf211.CallMe (211);
		mrp.CanCastToCalled = false;
		itf211 = (I211)o;
		if (mrp.CanCastToCalled)
			return 211;
		total += itf211.CallMe (211);

		mrp.CanCastToCalled = false;
		I212 itf212 = (I212)o;
		if (!mrp.CanCastToCalled)
			return 212;
		total += itf212.CallMe (212);
		mrp.CanCastToCalled = false;
		itf212 = (I212)o;
		if (mrp.CanCastToCalled)
			return 212;
		total += itf212.CallMe (212);

		mrp.CanCastToCalled = false;
		I213 itf213 = (I213)o;
		if (!mrp.CanCastToCalled)
			return 213;
		total += itf213.CallMe (213);
		mrp.CanCastToCalled = false;
		itf213 = (I213)o;
		if (mrp.CanCastToCalled)
			return 213;
		total += itf213.CallMe (213);

		mrp.CanCastToCalled = false;
		I214 itf214 = (I214)o;
		if (!mrp.CanCastToCalled)
			return 214;
		total += itf214.CallMe (214);
		mrp.CanCastToCalled = false;
		itf214 = (I214)o;
		if (mrp.CanCastToCalled)
			return 214;
		total += itf214.CallMe (214);

		mrp.CanCastToCalled = false;
		I215 itf215 = (I215)o;
		if (!mrp.CanCastToCalled)
			return 215;
		total += itf215.CallMe (215);
		mrp.CanCastToCalled = false;
		itf215 = (I215)o;
		if (mrp.CanCastToCalled)
			return 215;
		total += itf215.CallMe (215);

		mrp.CanCastToCalled = false;
		I216 itf216 = (I216)o;
		if (!mrp.CanCastToCalled)
			return 216;
		total += itf216.CallMe (216);
		mrp.CanCastToCalled = false;
		itf216 = (I216)o;
		if (mrp.CanCastToCalled)
			return 216;
		total += itf216.CallMe (216);

		mrp.CanCastToCalled = false;
		I217 itf217 = (I217)o;
		if (!mrp.CanCastToCalled)
			return 217;
		total += itf217.CallMe (217);
		mrp.CanCastToCalled = false;
		itf217 = (I217)o;
		if (mrp.CanCastToCalled)
			return 217;
		total += itf217.CallMe (217);

		mrp.CanCastToCalled = false;
		I218 itf218 = (I218)o;
		if (!mrp.CanCastToCalled)
			return 218;
		total += itf218.CallMe (218);
		mrp.CanCastToCalled = false;
		itf218 = (I218)o;
		if (mrp.CanCastToCalled)
			return 218;
		total += itf218.CallMe (218);

		mrp.CanCastToCalled = false;
		I219 itf219 = (I219)o;
		if (!mrp.CanCastToCalled)
			return 219;
		total += itf219.CallMe (219);
		mrp.CanCastToCalled = false;
		itf219 = (I219)o;
		if (mrp.CanCastToCalled)
			return 219;
		total += itf219.CallMe (219);

		mrp.CanCastToCalled = false;
		I220 itf220 = (I220)o;
		if (!mrp.CanCastToCalled)
			return 220;
		total += itf220.CallMe (220);
		mrp.CanCastToCalled = false;
		itf220 = (I220)o;
		if (mrp.CanCastToCalled)
			return 220;
		total += itf220.CallMe (220);

		mrp.CanCastToCalled = false;
		I221 itf221 = (I221)o;
		if (!mrp.CanCastToCalled)
			return 221;
		total += itf221.CallMe (221);
		mrp.CanCastToCalled = false;
		itf221 = (I221)o;
		if (mrp.CanCastToCalled)
			return 221;
		total += itf221.CallMe (221);

		mrp.CanCastToCalled = false;
		I222 itf222 = (I222)o;
		if (!mrp.CanCastToCalled)
			return 222;
		total += itf222.CallMe (222);
		mrp.CanCastToCalled = false;
		itf222 = (I222)o;
		if (mrp.CanCastToCalled)
			return 222;
		total += itf222.CallMe (222);

		mrp.CanCastToCalled = false;
		I223 itf223 = (I223)o;
		if (!mrp.CanCastToCalled)
			return 223;
		total += itf223.CallMe (223);
		mrp.CanCastToCalled = false;
		itf223 = (I223)o;
		if (mrp.CanCastToCalled)
			return 223;
		total += itf223.CallMe (223);

		mrp.CanCastToCalled = false;
		I224 itf224 = (I224)o;
		if (!mrp.CanCastToCalled)
			return 224;
		total += itf224.CallMe (224);
		mrp.CanCastToCalled = false;
		itf224 = (I224)o;
		if (mrp.CanCastToCalled)
			return 224;
		total += itf224.CallMe (224);

		mrp.CanCastToCalled = false;
		I225 itf225 = (I225)o;
		if (!mrp.CanCastToCalled)
			return 225;
		total += itf225.CallMe (225);
		mrp.CanCastToCalled = false;
		itf225 = (I225)o;
		if (mrp.CanCastToCalled)
			return 225;
		total += itf225.CallMe (225);

		mrp.CanCastToCalled = false;
		I226 itf226 = (I226)o;
		if (!mrp.CanCastToCalled)
			return 226;
		total += itf226.CallMe (226);
		mrp.CanCastToCalled = false;
		itf226 = (I226)o;
		if (mrp.CanCastToCalled)
			return 226;
		total += itf226.CallMe (226);

		mrp.CanCastToCalled = false;
		I227 itf227 = (I227)o;
		if (!mrp.CanCastToCalled)
			return 227;
		total += itf227.CallMe (227);
		mrp.CanCastToCalled = false;
		itf227 = (I227)o;
		if (mrp.CanCastToCalled)
			return 227;
		total += itf227.CallMe (227);

		mrp.CanCastToCalled = false;
		I228 itf228 = (I228)o;
		if (!mrp.CanCastToCalled)
			return 228;
		total += itf228.CallMe (228);
		mrp.CanCastToCalled = false;
		itf228 = (I228)o;
		if (mrp.CanCastToCalled)
			return 228;
		total += itf228.CallMe (228);

		mrp.CanCastToCalled = false;
		I229 itf229 = (I229)o;
		if (!mrp.CanCastToCalled)
			return 229;
		total += itf229.CallMe (229);
		mrp.CanCastToCalled = false;
		itf229 = (I229)o;
		if (mrp.CanCastToCalled)
			return 229;
		total += itf229.CallMe (229);

		mrp.CanCastToCalled = false;
		I230 itf230 = (I230)o;
		if (!mrp.CanCastToCalled)
			return 230;
		total += itf230.CallMe (230);
		mrp.CanCastToCalled = false;
		itf230 = (I230)o;
		if (mrp.CanCastToCalled)
			return 230;
		total += itf230.CallMe (230);

		mrp.CanCastToCalled = false;
		I231 itf231 = (I231)o;
		if (!mrp.CanCastToCalled)
			return 231;
		total += itf231.CallMe (231);
		mrp.CanCastToCalled = false;
		itf231 = (I231)o;
		if (mrp.CanCastToCalled)
			return 231;
		total += itf231.CallMe (231);

		mrp.CanCastToCalled = false;
		I232 itf232 = (I232)o;
		if (!mrp.CanCastToCalled)
			return 232;
		total += itf232.CallMe (232);
		mrp.CanCastToCalled = false;
		itf232 = (I232)o;
		if (mrp.CanCastToCalled)
			return 232;
		total += itf232.CallMe (232);

		mrp.CanCastToCalled = false;
		I233 itf233 = (I233)o;
		if (!mrp.CanCastToCalled)
			return 233;
		total += itf233.CallMe (233);
		mrp.CanCastToCalled = false;
		itf233 = (I233)o;
		if (mrp.CanCastToCalled)
			return 233;
		total += itf233.CallMe (233);

		mrp.CanCastToCalled = false;
		I234 itf234 = (I234)o;
		if (!mrp.CanCastToCalled)
			return 234;
		total += itf234.CallMe (234);
		mrp.CanCastToCalled = false;
		itf234 = (I234)o;
		if (mrp.CanCastToCalled)
			return 234;
		total += itf234.CallMe (234);

		mrp.CanCastToCalled = false;
		I235 itf235 = (I235)o;
		if (!mrp.CanCastToCalled)
			return 235;
		total += itf235.CallMe (235);
		mrp.CanCastToCalled = false;
		itf235 = (I235)o;
		if (mrp.CanCastToCalled)
			return 235;
		total += itf235.CallMe (235);

		mrp.CanCastToCalled = false;
		I236 itf236 = (I236)o;
		if (!mrp.CanCastToCalled)
			return 236;
		total += itf236.CallMe (236);
		mrp.CanCastToCalled = false;
		itf236 = (I236)o;
		if (mrp.CanCastToCalled)
			return 236;
		total += itf236.CallMe (236);

		mrp.CanCastToCalled = false;
		I237 itf237 = (I237)o;
		if (!mrp.CanCastToCalled)
			return 237;
		total += itf237.CallMe (237);
		mrp.CanCastToCalled = false;
		itf237 = (I237)o;
		if (mrp.CanCastToCalled)
			return 237;
		total += itf237.CallMe (237);

		mrp.CanCastToCalled = false;
		I238 itf238 = (I238)o;
		if (!mrp.CanCastToCalled)
			return 238;
		total += itf238.CallMe (238);
		mrp.CanCastToCalled = false;
		itf238 = (I238)o;
		if (mrp.CanCastToCalled)
			return 238;
		total += itf238.CallMe (238);

		mrp.CanCastToCalled = false;
		I239 itf239 = (I239)o;
		if (!mrp.CanCastToCalled)
			return 239;
		total += itf239.CallMe (239);
		mrp.CanCastToCalled = false;
		itf239 = (I239)o;
		if (mrp.CanCastToCalled)
			return 239;
		total += itf239.CallMe (239);

		mrp.CanCastToCalled = false;
		I240 itf240 = (I240)o;
		if (!mrp.CanCastToCalled)
			return 240;
		total += itf240.CallMe (240);
		mrp.CanCastToCalled = false;
		itf240 = (I240)o;
		if (mrp.CanCastToCalled)
			return 240;
		total += itf240.CallMe (240);

		mrp.CanCastToCalled = false;
		I241 itf241 = (I241)o;
		if (!mrp.CanCastToCalled)
			return 241;
		total += itf241.CallMe (241);
		mrp.CanCastToCalled = false;
		itf241 = (I241)o;
		if (mrp.CanCastToCalled)
			return 241;
		total += itf241.CallMe (241);

		mrp.CanCastToCalled = false;
		I242 itf242 = (I242)o;
		if (!mrp.CanCastToCalled)
			return 242;
		total += itf242.CallMe (242);
		mrp.CanCastToCalled = false;
		itf242 = (I242)o;
		if (mrp.CanCastToCalled)
			return 242;
		total += itf242.CallMe (242);

		mrp.CanCastToCalled = false;
		I243 itf243 = (I243)o;
		if (!mrp.CanCastToCalled)
			return 243;
		total += itf243.CallMe (243);
		mrp.CanCastToCalled = false;
		itf243 = (I243)o;
		if (mrp.CanCastToCalled)
			return 243;
		total += itf243.CallMe (243);

		mrp.CanCastToCalled = false;
		I244 itf244 = (I244)o;
		if (!mrp.CanCastToCalled)
			return 244;
		total += itf244.CallMe (244);
		mrp.CanCastToCalled = false;
		itf244 = (I244)o;
		if (mrp.CanCastToCalled)
			return 244;
		total += itf244.CallMe (244);

		mrp.CanCastToCalled = false;
		I245 itf245 = (I245)o;
		if (!mrp.CanCastToCalled)
			return 245;
		total += itf245.CallMe (245);
		mrp.CanCastToCalled = false;
		itf245 = (I245)o;
		if (mrp.CanCastToCalled)
			return 245;
		total += itf245.CallMe (245);

		mrp.CanCastToCalled = false;
		I246 itf246 = (I246)o;
		if (!mrp.CanCastToCalled)
			return 246;
		total += itf246.CallMe (246);
		mrp.CanCastToCalled = false;
		itf246 = (I246)o;
		if (mrp.CanCastToCalled)
			return 246;
		total += itf246.CallMe (246);

		mrp.CanCastToCalled = false;
		I247 itf247 = (I247)o;
		if (!mrp.CanCastToCalled)
			return 247;
		total += itf247.CallMe (247);
		mrp.CanCastToCalled = false;
		itf247 = (I247)o;
		if (mrp.CanCastToCalled)
			return 247;
		total += itf247.CallMe (247);

		mrp.CanCastToCalled = false;
		I248 itf248 = (I248)o;
		if (!mrp.CanCastToCalled)
			return 248;
		total += itf248.CallMe (248);
		mrp.CanCastToCalled = false;
		itf248 = (I248)o;
		if (mrp.CanCastToCalled)
			return 248;
		total += itf248.CallMe (248);

		mrp.CanCastToCalled = false;
		I249 itf249 = (I249)o;
		if (!mrp.CanCastToCalled)
			return 249;
		total += itf249.CallMe (249);
		mrp.CanCastToCalled = false;
		itf249 = (I249)o;
		if (mrp.CanCastToCalled)
			return 249;
		total += itf249.CallMe (249);

		mrp.CanCastToCalled = false;
		I250 itf250 = (I250)o;
		if (!mrp.CanCastToCalled)
			return 250;
		total += itf250.CallMe (250);
		mrp.CanCastToCalled = false;
		itf250 = (I250)o;
		if (mrp.CanCastToCalled)
			return 250;
		total += itf250.CallMe (250);

		mrp.CanCastToCalled = false;
		I251 itf251 = (I251)o;
		if (!mrp.CanCastToCalled)
			return 251;
		total += itf251.CallMe (251);
		mrp.CanCastToCalled = false;
		itf251 = (I251)o;
		if (mrp.CanCastToCalled)
			return 251;
		total += itf251.CallMe (251);

		mrp.CanCastToCalled = false;
		I252 itf252 = (I252)o;
		if (!mrp.CanCastToCalled)
			return 252;
		total += itf252.CallMe (252);
		mrp.CanCastToCalled = false;
		itf252 = (I252)o;
		if (mrp.CanCastToCalled)
			return 252;
		total += itf252.CallMe (252);

		mrp.CanCastToCalled = false;
		I253 itf253 = (I253)o;
		if (!mrp.CanCastToCalled)
			return 253;
		total += itf253.CallMe (253);
		mrp.CanCastToCalled = false;
		itf253 = (I253)o;
		if (mrp.CanCastToCalled)
			return 253;
		total += itf253.CallMe (253);

		mrp.CanCastToCalled = false;
		I254 itf254 = (I254)o;
		if (!mrp.CanCastToCalled)
			return 254;
		total += itf254.CallMe (254);
		mrp.CanCastToCalled = false;
		itf254 = (I254)o;
		if (mrp.CanCastToCalled)
			return 254;
		total += itf254.CallMe (254);

		mrp.CanCastToCalled = false;
		I255 itf255 = (I255)o;
		if (!mrp.CanCastToCalled)
			return 255;
		total += itf255.CallMe (255);
		mrp.CanCastToCalled = false;
		itf255 = (I255)o;
		if (mrp.CanCastToCalled)
			return 255;
		total += itf255.CallMe (255);

		mrp.CanCastToCalled = false;
		I256 itf256 = (I256)o;
		if (!mrp.CanCastToCalled)
			return 256;
		total += itf256.CallMe (256);
		mrp.CanCastToCalled = false;
		itf256 = (I256)o;
		if (mrp.CanCastToCalled)
			return 256;
		total += itf256.CallMe (256);

		mrp.CanCastToCalled = false;
		I257 itf257 = (I257)o;
		if (!mrp.CanCastToCalled)
			return 257;
		total += itf257.CallMe (257);
		mrp.CanCastToCalled = false;
		itf257 = (I257)o;
		if (mrp.CanCastToCalled)
			return 257;
		total += itf257.CallMe (257);

		mrp.CanCastToCalled = false;
		I258 itf258 = (I258)o;
		if (!mrp.CanCastToCalled)
			return 258;
		total += itf258.CallMe (258);
		mrp.CanCastToCalled = false;
		itf258 = (I258)o;
		if (mrp.CanCastToCalled)
			return 258;
		total += itf258.CallMe (258);

		mrp.CanCastToCalled = false;
		I259 itf259 = (I259)o;
		if (!mrp.CanCastToCalled)
			return 259;
		total += itf259.CallMe (259);
		mrp.CanCastToCalled = false;
		itf259 = (I259)o;
		if (mrp.CanCastToCalled)
			return 259;
		total += itf259.CallMe (259);

		mrp.CanCastToCalled = false;
		I260 itf260 = (I260)o;
		if (!mrp.CanCastToCalled)
			return 260;
		total += itf260.CallMe (260);
		mrp.CanCastToCalled = false;
		itf260 = (I260)o;
		if (mrp.CanCastToCalled)
			return 260;
		total += itf260.CallMe (260);

		mrp.CanCastToCalled = false;
		I261 itf261 = (I261)o;
		if (!mrp.CanCastToCalled)
			return 261;
		total += itf261.CallMe (261);
		mrp.CanCastToCalled = false;
		itf261 = (I261)o;
		if (mrp.CanCastToCalled)
			return 261;
		total += itf261.CallMe (261);

		mrp.CanCastToCalled = false;
		I262 itf262 = (I262)o;
		if (!mrp.CanCastToCalled)
			return 262;
		total += itf262.CallMe (262);
		mrp.CanCastToCalled = false;
		itf262 = (I262)o;
		if (mrp.CanCastToCalled)
			return 262;
		total += itf262.CallMe (262);

		mrp.CanCastToCalled = false;
		I263 itf263 = (I263)o;
		if (!mrp.CanCastToCalled)
			return 263;
		total += itf263.CallMe (263);
		mrp.CanCastToCalled = false;
		itf263 = (I263)o;
		if (mrp.CanCastToCalled)
			return 263;
		total += itf263.CallMe (263);

		mrp.CanCastToCalled = false;
		I264 itf264 = (I264)o;
		if (!mrp.CanCastToCalled)
			return 264;
		total += itf264.CallMe (264);
		mrp.CanCastToCalled = false;
		itf264 = (I264)o;
		if (mrp.CanCastToCalled)
			return 264;
		total += itf264.CallMe (264);

		mrp.CanCastToCalled = false;
		I265 itf265 = (I265)o;
		if (!mrp.CanCastToCalled)
			return 265;
		total += itf265.CallMe (265);
		mrp.CanCastToCalled = false;
		itf265 = (I265)o;
		if (mrp.CanCastToCalled)
			return 265;
		total += itf265.CallMe (265);

		mrp.CanCastToCalled = false;
		I266 itf266 = (I266)o;
		if (!mrp.CanCastToCalled)
			return 266;
		total += itf266.CallMe (266);
		mrp.CanCastToCalled = false;
		itf266 = (I266)o;
		if (mrp.CanCastToCalled)
			return 266;
		total += itf266.CallMe (266);

		mrp.CanCastToCalled = false;
		I267 itf267 = (I267)o;
		if (!mrp.CanCastToCalled)
			return 267;
		total += itf267.CallMe (267);
		mrp.CanCastToCalled = false;
		itf267 = (I267)o;
		if (mrp.CanCastToCalled)
			return 267;
		total += itf267.CallMe (267);

		mrp.CanCastToCalled = false;
		I268 itf268 = (I268)o;
		if (!mrp.CanCastToCalled)
			return 268;
		total += itf268.CallMe (268);
		mrp.CanCastToCalled = false;
		itf268 = (I268)o;
		if (mrp.CanCastToCalled)
			return 268;
		total += itf268.CallMe (268);

		mrp.CanCastToCalled = false;
		I269 itf269 = (I269)o;
		if (!mrp.CanCastToCalled)
			return 269;
		total += itf269.CallMe (269);
		mrp.CanCastToCalled = false;
		itf269 = (I269)o;
		if (mrp.CanCastToCalled)
			return 269;
		total += itf269.CallMe (269);

		mrp.CanCastToCalled = false;
		I270 itf270 = (I270)o;
		if (!mrp.CanCastToCalled)
			return 270;
		total += itf270.CallMe (270);
		mrp.CanCastToCalled = false;
		itf270 = (I270)o;
		if (mrp.CanCastToCalled)
			return 270;
		total += itf270.CallMe (270);

		mrp.CanCastToCalled = false;
		I271 itf271 = (I271)o;
		if (!mrp.CanCastToCalled)
			return 271;
		total += itf271.CallMe (271);
		mrp.CanCastToCalled = false;
		itf271 = (I271)o;
		if (mrp.CanCastToCalled)
			return 271;
		total += itf271.CallMe (271);

		mrp.CanCastToCalled = false;
		I272 itf272 = (I272)o;
		if (!mrp.CanCastToCalled)
			return 272;
		total += itf272.CallMe (272);
		mrp.CanCastToCalled = false;
		itf272 = (I272)o;
		if (mrp.CanCastToCalled)
			return 272;
		total += itf272.CallMe (272);

		mrp.CanCastToCalled = false;
		I273 itf273 = (I273)o;
		if (!mrp.CanCastToCalled)
			return 273;
		total += itf273.CallMe (273);
		mrp.CanCastToCalled = false;
		itf273 = (I273)o;
		if (mrp.CanCastToCalled)
			return 273;
		total += itf273.CallMe (273);

		mrp.CanCastToCalled = false;
		I274 itf274 = (I274)o;
		if (!mrp.CanCastToCalled)
			return 274;
		total += itf274.CallMe (274);
		mrp.CanCastToCalled = false;
		itf274 = (I274)o;
		if (mrp.CanCastToCalled)
			return 274;
		total += itf274.CallMe (274);

		mrp.CanCastToCalled = false;
		I275 itf275 = (I275)o;
		if (!mrp.CanCastToCalled)
			return 275;
		total += itf275.CallMe (275);
		mrp.CanCastToCalled = false;
		itf275 = (I275)o;
		if (mrp.CanCastToCalled)
			return 275;
		total += itf275.CallMe (275);

		mrp.CanCastToCalled = false;
		I276 itf276 = (I276)o;
		if (!mrp.CanCastToCalled)
			return 276;
		total += itf276.CallMe (276);
		mrp.CanCastToCalled = false;
		itf276 = (I276)o;
		if (mrp.CanCastToCalled)
			return 276;
		total += itf276.CallMe (276);

		mrp.CanCastToCalled = false;
		I277 itf277 = (I277)o;
		if (!mrp.CanCastToCalled)
			return 277;
		total += itf277.CallMe (277);
		mrp.CanCastToCalled = false;
		itf277 = (I277)o;
		if (mrp.CanCastToCalled)
			return 277;
		total += itf277.CallMe (277);

		mrp.CanCastToCalled = false;
		I278 itf278 = (I278)o;
		if (!mrp.CanCastToCalled)
			return 278;
		total += itf278.CallMe (278);
		mrp.CanCastToCalled = false;
		itf278 = (I278)o;
		if (mrp.CanCastToCalled)
			return 278;
		total += itf278.CallMe (278);

		mrp.CanCastToCalled = false;
		I279 itf279 = (I279)o;
		if (!mrp.CanCastToCalled)
			return 279;
		total += itf279.CallMe (279);
		mrp.CanCastToCalled = false;
		itf279 = (I279)o;
		if (mrp.CanCastToCalled)
			return 279;
		total += itf279.CallMe (279);

		mrp.CanCastToCalled = false;
		I280 itf280 = (I280)o;
		if (!mrp.CanCastToCalled)
			return 280;
		total += itf280.CallMe (280);
		mrp.CanCastToCalled = false;
		itf280 = (I280)o;
		if (mrp.CanCastToCalled)
			return 280;
		total += itf280.CallMe (280);

		mrp.CanCastToCalled = false;
		I281 itf281 = (I281)o;
		if (!mrp.CanCastToCalled)
			return 281;
		total += itf281.CallMe (281);
		mrp.CanCastToCalled = false;
		itf281 = (I281)o;
		if (mrp.CanCastToCalled)
			return 281;
		total += itf281.CallMe (281);

		mrp.CanCastToCalled = false;
		I282 itf282 = (I282)o;
		if (!mrp.CanCastToCalled)
			return 282;
		total += itf282.CallMe (282);
		mrp.CanCastToCalled = false;
		itf282 = (I282)o;
		if (mrp.CanCastToCalled)
			return 282;
		total += itf282.CallMe (282);

		mrp.CanCastToCalled = false;
		I283 itf283 = (I283)o;
		if (!mrp.CanCastToCalled)
			return 283;
		total += itf283.CallMe (283);
		mrp.CanCastToCalled = false;
		itf283 = (I283)o;
		if (mrp.CanCastToCalled)
			return 283;
		total += itf283.CallMe (283);

		mrp.CanCastToCalled = false;
		I284 itf284 = (I284)o;
		if (!mrp.CanCastToCalled)
			return 284;
		total += itf284.CallMe (284);
		mrp.CanCastToCalled = false;
		itf284 = (I284)o;
		if (mrp.CanCastToCalled)
			return 284;
		total += itf284.CallMe (284);

		mrp.CanCastToCalled = false;
		I285 itf285 = (I285)o;
		if (!mrp.CanCastToCalled)
			return 285;
		total += itf285.CallMe (285);
		mrp.CanCastToCalled = false;
		itf285 = (I285)o;
		if (mrp.CanCastToCalled)
			return 285;
		total += itf285.CallMe (285);

		mrp.CanCastToCalled = false;
		I286 itf286 = (I286)o;
		if (!mrp.CanCastToCalled)
			return 286;
		total += itf286.CallMe (286);
		mrp.CanCastToCalled = false;
		itf286 = (I286)o;
		if (mrp.CanCastToCalled)
			return 286;
		total += itf286.CallMe (286);

		mrp.CanCastToCalled = false;
		I287 itf287 = (I287)o;
		if (!mrp.CanCastToCalled)
			return 287;
		total += itf287.CallMe (287);
		mrp.CanCastToCalled = false;
		itf287 = (I287)o;
		if (mrp.CanCastToCalled)
			return 287;
		total += itf287.CallMe (287);

		mrp.CanCastToCalled = false;
		I288 itf288 = (I288)o;
		if (!mrp.CanCastToCalled)
			return 288;
		total += itf288.CallMe (288);
		mrp.CanCastToCalled = false;
		itf288 = (I288)o;
		if (mrp.CanCastToCalled)
			return 288;
		total += itf288.CallMe (288);

		mrp.CanCastToCalled = false;
		I289 itf289 = (I289)o;
		if (!mrp.CanCastToCalled)
			return 289;
		total += itf289.CallMe (289);
		mrp.CanCastToCalled = false;
		itf289 = (I289)o;
		if (mrp.CanCastToCalled)
			return 289;
		total += itf289.CallMe (289);

		mrp.CanCastToCalled = false;
		I290 itf290 = (I290)o;
		if (!mrp.CanCastToCalled)
			return 290;
		total += itf290.CallMe (290);
		mrp.CanCastToCalled = false;
		itf290 = (I290)o;
		if (mrp.CanCastToCalled)
			return 290;
		total += itf290.CallMe (290);

		mrp.CanCastToCalled = false;
		I291 itf291 = (I291)o;
		if (!mrp.CanCastToCalled)
			return 291;
		total += itf291.CallMe (291);
		mrp.CanCastToCalled = false;
		itf291 = (I291)o;
		if (mrp.CanCastToCalled)
			return 291;
		total += itf291.CallMe (291);

		mrp.CanCastToCalled = false;
		I292 itf292 = (I292)o;
		if (!mrp.CanCastToCalled)
			return 292;
		total += itf292.CallMe (292);
		mrp.CanCastToCalled = false;
		itf292 = (I292)o;
		if (mrp.CanCastToCalled)
			return 292;
		total += itf292.CallMe (292);

		mrp.CanCastToCalled = false;
		I293 itf293 = (I293)o;
		if (!mrp.CanCastToCalled)
			return 293;
		total += itf293.CallMe (293);
		mrp.CanCastToCalled = false;
		itf293 = (I293)o;
		if (mrp.CanCastToCalled)
			return 293;
		total += itf293.CallMe (293);

		mrp.CanCastToCalled = false;
		I294 itf294 = (I294)o;
		if (!mrp.CanCastToCalled)
			return 294;
		total += itf294.CallMe (294);
		mrp.CanCastToCalled = false;
		itf294 = (I294)o;
		if (mrp.CanCastToCalled)
			return 294;
		total += itf294.CallMe (294);

		mrp.CanCastToCalled = false;
		I295 itf295 = (I295)o;
		if (!mrp.CanCastToCalled)
			return 295;
		total += itf295.CallMe (295);
		mrp.CanCastToCalled = false;
		itf295 = (I295)o;
		if (mrp.CanCastToCalled)
			return 295;
		total += itf295.CallMe (295);

		mrp.CanCastToCalled = false;
		I296 itf296 = (I296)o;
		if (!mrp.CanCastToCalled)
			return 296;
		total += itf296.CallMe (296);
		mrp.CanCastToCalled = false;
		itf296 = (I296)o;
		if (mrp.CanCastToCalled)
			return 296;
		total += itf296.CallMe (296);

		mrp.CanCastToCalled = false;
		I297 itf297 = (I297)o;
		if (!mrp.CanCastToCalled)
			return 297;
		total += itf297.CallMe (297);
		mrp.CanCastToCalled = false;
		itf297 = (I297)o;
		if (mrp.CanCastToCalled)
			return 297;
		total += itf297.CallMe (297);

		mrp.CanCastToCalled = false;
		I298 itf298 = (I298)o;
		if (!mrp.CanCastToCalled)
			return 298;
		total += itf298.CallMe (298);
		mrp.CanCastToCalled = false;
		itf298 = (I298)o;
		if (mrp.CanCastToCalled)
			return 298;
		total += itf298.CallMe (298);

		mrp.CanCastToCalled = false;
		I299 itf299 = (I299)o;
		if (!mrp.CanCastToCalled)
			return 299;
		total += itf299.CallMe (299);
		mrp.CanCastToCalled = false;
		itf299 = (I299)o;
		if (mrp.CanCastToCalled)
			return 299;
		total += itf299.CallMe (299);

		mrp.CanCastToCalled = false;
		I300 itf300 = (I300)o;
		if (!mrp.CanCastToCalled)
			return 300;
		total += itf300.CallMe (300);
		mrp.CanCastToCalled = false;
		itf300 = (I300)o;
		if (mrp.CanCastToCalled)
			return 300;
		total += itf300.CallMe (300);

		Console.WriteLine ("finished");

		return 0;
	}
}

interface I1
{
	int CallMe (int a);
}

interface I2
{
	int CallMe (int a);
}

interface I3
{
	int CallMe (int a);
}

interface I4
{
	int CallMe (int a);
}

interface I5
{
	int CallMe (int a);
}

interface I6
{
	int CallMe (int a);
}

interface I7
{
	int CallMe (int a);
}

interface I8
{
	int CallMe (int a);
}

interface I9
{
	int CallMe (int a);
}

interface I10
{
	int CallMe (int a);
}

interface I11
{
	int CallMe (int a);
}

interface I12
{
	int CallMe (int a);
}

interface I13
{
	int CallMe (int a);
}

interface I14
{
	int CallMe (int a);
}

interface I15
{
	int CallMe (int a);
}

interface I16
{
	int CallMe (int a);
}

interface I17
{
	int CallMe (int a);
}

interface I18
{
	int CallMe (int a);
}

interface I19
{
	int CallMe (int a);
}

interface I20
{
	int CallMe (int a);
}

interface I21
{
	int CallMe (int a);
}

interface I22
{
	int CallMe (int a);
}

interface I23
{
	int CallMe (int a);
}

interface I24
{
	int CallMe (int a);
}

interface I25
{
	int CallMe (int a);
}

interface I26
{
	int CallMe (int a);
}

interface I27
{
	int CallMe (int a);
}

interface I28
{
	int CallMe (int a);
}

interface I29
{
	int CallMe (int a);
}

interface I30
{
	int CallMe (int a);
}

interface I31
{
	int CallMe (int a);
}

interface I32
{
	int CallMe (int a);
}

interface I33
{
	int CallMe (int a);
}

interface I34
{
	int CallMe (int a);
}

interface I35
{
	int CallMe (int a);
}

interface I36
{
	int CallMe (int a);
}

interface I37
{
	int CallMe (int a);
}

interface I38
{
	int CallMe (int a);
}

interface I39
{
	int CallMe (int a);
}

interface I40
{
	int CallMe (int a);
}

interface I41
{
	int CallMe (int a);
}

interface I42
{
	int CallMe (int a);
}

interface I43
{
	int CallMe (int a);
}

interface I44
{
	int CallMe (int a);
}

interface I45
{
	int CallMe (int a);
}

interface I46
{
	int CallMe (int a);
}

interface I47
{
	int CallMe (int a);
}

interface I48
{
	int CallMe (int a);
}

interface I49
{
	int CallMe (int a);
}

interface I50
{
	int CallMe (int a);
}

interface I51
{
	int CallMe (int a);
}

interface I52
{
	int CallMe (int a);
}

interface I53
{
	int CallMe (int a);
}

interface I54
{
	int CallMe (int a);
}

interface I55
{
	int CallMe (int a);
}

interface I56
{
	int CallMe (int a);
}

interface I57
{
	int CallMe (int a);
}

interface I58
{
	int CallMe (int a);
}

interface I59
{
	int CallMe (int a);
}

interface I60
{
	int CallMe (int a);
}

interface I61
{
	int CallMe (int a);
}

interface I62
{
	int CallMe (int a);
}

interface I63
{
	int CallMe (int a);
}

interface I64
{
	int CallMe (int a);
}

interface I65
{
	int CallMe (int a);
}

interface I66
{
	int CallMe (int a);
}

interface I67
{
	int CallMe (int a);
}

interface I68
{
	int CallMe (int a);
}

interface I69
{
	int CallMe (int a);
}

interface I70
{
	int CallMe (int a);
}

interface I71
{
	int CallMe (int a);
}

interface I72
{
	int CallMe (int a);
}

interface I73
{
	int CallMe (int a);
}

interface I74
{
	int CallMe (int a);
}

interface I75
{
	int CallMe (int a);
}

interface I76
{
	int CallMe (int a);
}

interface I77
{
	int CallMe (int a);
}

interface I78
{
	int CallMe (int a);
}

interface I79
{
	int CallMe (int a);
}

interface I80
{
	int CallMe (int a);
}

interface I81
{
	int CallMe (int a);
}

interface I82
{
	int CallMe (int a);
}

interface I83
{
	int CallMe (int a);
}

interface I84
{
	int CallMe (int a);
}

interface I85
{
	int CallMe (int a);
}

interface I86
{
	int CallMe (int a);
}

interface I87
{
	int CallMe (int a);
}

interface I88
{
	int CallMe (int a);
}

interface I89
{
	int CallMe (int a);
}

interface I90
{
	int CallMe (int a);
}

interface I91
{
	int CallMe (int a);
}

interface I92
{
	int CallMe (int a);
}

interface I93
{
	int CallMe (int a);
}

interface I94
{
	int CallMe (int a);
}

interface I95
{
	int CallMe (int a);
}

interface I96
{
	int CallMe (int a);
}

interface I97
{
	int CallMe (int a);
}

interface I98
{
	int CallMe (int a);
}

interface I99
{
	int CallMe (int a);
}

interface I100
{
	int CallMe (int a);
}

interface I101
{
	int CallMe (int a);
}

interface I102
{
	int CallMe (int a);
}

interface I103
{
	int CallMe (int a);
}

interface I104
{
	int CallMe (int a);
}

interface I105
{
	int CallMe (int a);
}

interface I106
{
	int CallMe (int a);
}

interface I107
{
	int CallMe (int a);
}

interface I108
{
	int CallMe (int a);
}

interface I109
{
	int CallMe (int a);
}

interface I110
{
	int CallMe (int a);
}

interface I111
{
	int CallMe (int a);
}

interface I112
{
	int CallMe (int a);
}

interface I113
{
	int CallMe (int a);
}

interface I114
{
	int CallMe (int a);
}

interface I115
{
	int CallMe (int a);
}

interface I116
{
	int CallMe (int a);
}

interface I117
{
	int CallMe (int a);
}

interface I118
{
	int CallMe (int a);
}

interface I119
{
	int CallMe (int a);
}

interface I120
{
	int CallMe (int a);
}

interface I121
{
	int CallMe (int a);
}

interface I122
{
	int CallMe (int a);
}

interface I123
{
	int CallMe (int a);
}

interface I124
{
	int CallMe (int a);
}

interface I125
{
	int CallMe (int a);
}

interface I126
{
	int CallMe (int a);
}

interface I127
{
	int CallMe (int a);
}

interface I128
{
	int CallMe (int a);
}

interface I129
{
	int CallMe (int a);
}

interface I130
{
	int CallMe (int a);
}

interface I131
{
	int CallMe (int a);
}

interface I132
{
	int CallMe (int a);
}

interface I133
{
	int CallMe (int a);
}

interface I134
{
	int CallMe (int a);
}

interface I135
{
	int CallMe (int a);
}

interface I136
{
	int CallMe (int a);
}

interface I137
{
	int CallMe (int a);
}

interface I138
{
	int CallMe (int a);
}

interface I139
{
	int CallMe (int a);
}

interface I140
{
	int CallMe (int a);
}

interface I141
{
	int CallMe (int a);
}

interface I142
{
	int CallMe (int a);
}

interface I143
{
	int CallMe (int a);
}

interface I144
{
	int CallMe (int a);
}

interface I145
{
	int CallMe (int a);
}

interface I146
{
	int CallMe (int a);
}

interface I147
{
	int CallMe (int a);
}

interface I148
{
	int CallMe (int a);
}

interface I149
{
	int CallMe (int a);
}

interface I150
{
	int CallMe (int a);
}

interface I151
{
	int CallMe (int a);
}

interface I152
{
	int CallMe (int a);
}

interface I153
{
	int CallMe (int a);
}

interface I154
{
	int CallMe (int a);
}

interface I155
{
	int CallMe (int a);
}

interface I156
{
	int CallMe (int a);
}

interface I157
{
	int CallMe (int a);
}

interface I158
{
	int CallMe (int a);
}

interface I159
{
	int CallMe (int a);
}

interface I160
{
	int CallMe (int a);
}

interface I161
{
	int CallMe (int a);
}

interface I162
{
	int CallMe (int a);
}

interface I163
{
	int CallMe (int a);
}

interface I164
{
	int CallMe (int a);
}

interface I165
{
	int CallMe (int a);
}

interface I166
{
	int CallMe (int a);
}

interface I167
{
	int CallMe (int a);
}

interface I168
{
	int CallMe (int a);
}

interface I169
{
	int CallMe (int a);
}

interface I170
{
	int CallMe (int a);
}

interface I171
{
	int CallMe (int a);
}

interface I172
{
	int CallMe (int a);
}

interface I173
{
	int CallMe (int a);
}

interface I174
{
	int CallMe (int a);
}

interface I175
{
	int CallMe (int a);
}

interface I176
{
	int CallMe (int a);
}

interface I177
{
	int CallMe (int a);
}

interface I178
{
	int CallMe (int a);
}

interface I179
{
	int CallMe (int a);
}

interface I180
{
	int CallMe (int a);
}

interface I181
{
	int CallMe (int a);
}

interface I182
{
	int CallMe (int a);
}

interface I183
{
	int CallMe (int a);
}

interface I184
{
	int CallMe (int a);
}

interface I185
{
	int CallMe (int a);
}

interface I186
{
	int CallMe (int a);
}

interface I187
{
	int CallMe (int a);
}

interface I188
{
	int CallMe (int a);
}

interface I189
{
	int CallMe (int a);
}

interface I190
{
	int CallMe (int a);
}

interface I191
{
	int CallMe (int a);
}

interface I192
{
	int CallMe (int a);
}

interface I193
{
	int CallMe (int a);
}

interface I194
{
	int CallMe (int a);
}

interface I195
{
	int CallMe (int a);
}

interface I196
{
	int CallMe (int a);
}

interface I197
{
	int CallMe (int a);
}

interface I198
{
	int CallMe (int a);
}

interface I199
{
	int CallMe (int a);
}

interface I200
{
	int CallMe (int a);
}

interface I201
{
	int CallMe (int a);
}

interface I202
{
	int CallMe (int a);
}

interface I203
{
	int CallMe (int a);
}

interface I204
{
	int CallMe (int a);
}

interface I205
{
	int CallMe (int a);
}

interface I206
{
	int CallMe (int a);
}

interface I207
{
	int CallMe (int a);
}

interface I208
{
	int CallMe (int a);
}

interface I209
{
	int CallMe (int a);
}

interface I210
{
	int CallMe (int a);
}

interface I211
{
	int CallMe (int a);
}

interface I212
{
	int CallMe (int a);
}

interface I213
{
	int CallMe (int a);
}

interface I214
{
	int CallMe (int a);
}

interface I215
{
	int CallMe (int a);
}

interface I216
{
	int CallMe (int a);
}

interface I217
{
	int CallMe (int a);
}

interface I218
{
	int CallMe (int a);
}

interface I219
{
	int CallMe (int a);
}

interface I220
{
	int CallMe (int a);
}

interface I221
{
	int CallMe (int a);
}

interface I222
{
	int CallMe (int a);
}

interface I223
{
	int CallMe (int a);
}

interface I224
{
	int CallMe (int a);
}

interface I225
{
	int CallMe (int a);
}

interface I226
{
	int CallMe (int a);
}

interface I227
{
	int CallMe (int a);
}

interface I228
{
	int CallMe (int a);
}

interface I229
{
	int CallMe (int a);
}

interface I230
{
	int CallMe (int a);
}

interface I231
{
	int CallMe (int a);
}

interface I232
{
	int CallMe (int a);
}

interface I233
{
	int CallMe (int a);
}

interface I234
{
	int CallMe (int a);
}

interface I235
{
	int CallMe (int a);
}

interface I236
{
	int CallMe (int a);
}

interface I237
{
	int CallMe (int a);
}

interface I238
{
	int CallMe (int a);
}

interface I239
{
	int CallMe (int a);
}

interface I240
{
	int CallMe (int a);
}

interface I241
{
	int CallMe (int a);
}

interface I242
{
	int CallMe (int a);
}

interface I243
{
	int CallMe (int a);
}

interface I244
{
	int CallMe (int a);
}

interface I245
{
	int CallMe (int a);
}

interface I246
{
	int CallMe (int a);
}

interface I247
{
	int CallMe (int a);
}

interface I248
{
	int CallMe (int a);
}

interface I249
{
	int CallMe (int a);
}

interface I250
{
	int CallMe (int a);
}

interface I251
{
	int CallMe (int a);
}

interface I252
{
	int CallMe (int a);
}

interface I253
{
	int CallMe (int a);
}

interface I254
{
	int CallMe (int a);
}

interface I255
{
	int CallMe (int a);
}

interface I256
{
	int CallMe (int a);
}

interface I257
{
	int CallMe (int a);
}

interface I258
{
	int CallMe (int a);
}

interface I259
{
	int CallMe (int a);
}

interface I260
{
	int CallMe (int a);
}

interface I261
{
	int CallMe (int a);
}

interface I262
{
	int CallMe (int a);
}

interface I263
{
	int CallMe (int a);
}

interface I264
{
	int CallMe (int a);
}

interface I265
{
	int CallMe (int a);
}

interface I266
{
	int CallMe (int a);
}

interface I267
{
	int CallMe (int a);
}

interface I268
{
	int CallMe (int a);
}

interface I269
{
	int CallMe (int a);
}

interface I270
{
	int CallMe (int a);
}

interface I271
{
	int CallMe (int a);
}

interface I272
{
	int CallMe (int a);
}

interface I273
{
	int CallMe (int a);
}

interface I274
{
	int CallMe (int a);
}

interface I275
{
	int CallMe (int a);
}

interface I276
{
	int CallMe (int a);
}

interface I277
{
	int CallMe (int a);
}

interface I278
{
	int CallMe (int a);
}

interface I279
{
	int CallMe (int a);
}

interface I280
{
	int CallMe (int a);
}

interface I281
{
	int CallMe (int a);
}

interface I282
{
	int CallMe (int a);
}

interface I283
{
	int CallMe (int a);
}

interface I284
{
	int CallMe (int a);
}

interface I285
{
	int CallMe (int a);
}

interface I286
{
	int CallMe (int a);
}

interface I287
{
	int CallMe (int a);
}

interface I288
{
	int CallMe (int a);
}

interface I289
{
	int CallMe (int a);
}

interface I290
{
	int CallMe (int a);
}

interface I291
{
	int CallMe (int a);
}

interface I292
{
	int CallMe (int a);
}

interface I293
{
	int CallMe (int a);
}

interface I294
{
	int CallMe (int a);
}

interface I295
{
	int CallMe (int a);
}

interface I296
{
	int CallMe (int a);
}

interface I297
{
	int CallMe (int a);
}

interface I298
{
	int CallMe (int a);
}

interface I299
{
	int CallMe (int a);
}

interface I300
{
	int CallMe (int a);
}
