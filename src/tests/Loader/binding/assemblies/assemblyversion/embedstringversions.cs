/**********************************************************************/
/**********************************************************************/
/**                                                                  **/
/**   Basic API tes suite for System.Version class using only strings**/
/**    And error chars					     **/
/**	Other tests have skipped a couple of API's and operators     **/
/**	This suite ensures complete coverage			     **/
/**                                                                  **/
/**********************************************************************/
/**********************************************************************/

//                      API summary
// constructors
//        public Version()*
//                defaults to 0.0 with build and revision undefined
//        public Version(int major, int minor, int build, int revision)*
//                all fields defined
//        public Version(int major, int minor, int build)*
//                revision remains undefined
//        public Version(int major, int minor)*
//                build and revision remain undefined
//        public Version(String version)*
//                stringized version in form W.X.Y.Z or subset thereof - minimum Major and Minor
//
// properties
//        public int Major	RO*
//        public int Minor	RO*
//        public int Build	RO*
//        public int Revision	RO*
//
// methods
//        public Object Clone()*
//        public int CompareTo(Object version) - < 1 less than, =0 same, >1 greater than*
//        public override bool Equals(Object obj)*
//        public override int GetHashCode()*
//        public override String ToString()*
//        public String ToString(int fieldCount) Returns only fieldcount of (Major, Minor, Build, Revision) concat'd*
//
// operators
//        public static bool operator ==(Version v1, Version v2)
//        public static bool operator !=(Version v1, Version v2)
//        public static bool operator <(Version v1, Version v2)
//        public static bool operator <=(Version v1, Version v2)
//        public static bool operator >(Version v1, Version v2)
//        public static bool operator >=(Version v1, Version v2)
//


using System;
using System.Globalization;
using Xunit;


public class StringVersionClass {
    const int MAX_TEST = 179;
    const int MAX_ARG  = 2;
    const int MAX_FORMAT = 89;
    const int MAX_RANGE = 99;
    const int MAX_UNICODE = 178;

    private Version[] testor = new Version[MAX_TEST];

    private bool[] result = new bool[MAX_TEST];

    [Fact]
    public static int TestEntryPoint()    {
	
        StringVersionClass Me = new StringVersionClass();
	int ret = -1;
	int i;
	String Stemp = null;

	// Set up Results report
	for ( i = 0; i < MAX_TEST; i++ )
	    Me.result[i] = false;

	//Proceed to tests
	for ( i = 0; i < MAX_TEST; i++ ) {
	    switch (i) {
		//Null arg
		case   0 : break;

		//Argument exceptions
		//  Invalid separators
		case   1 : Stemp = "99.99,99.99,99.99,99.99";
		           break;

		// Incorrect separator
		case   2 : Stemp = "9,9,9,9";
			   break;

		// Incorrect use of grouping char
		case   3 : Stemp = "9,999. 9,999. 9,999. 9,999 ";
			   break;

		// Invalid chars
		case   4 : Stemp = "#999.#9999.#9999.#9999";
			   break;

		case   5 : Stemp = "CLR.CLR.CLR.CLR";
			   break;

		case   6 : Stemp = "1/2.1/2.1.1";
			   break;

		case   7 : Stemp = "###.###.###.###";
			   break;


		case   8 : Stemp = "%1.2.3.4";
			   break;

		case   9 : Stemp = "1.2.3.4 %";
			   break;

		case  10 : Stemp = "1.2.3.4%";
			   break;

		case  11 : Stemp = "1%.2.3.4";
			   break;

		case  12 : Stemp = "1.2%.3.4";
			   break;

		case  13 : Stemp = "1.2.%3.4";
			   break;

		case  14 : Stemp = "@1.2.3.4";
			   break;

		case  15 : Stemp = "1.2.3.4 @";
			   break;

		case  16 : Stemp = "1.2.3.4@";
			   break;

		case  17 : Stemp = "1@.2.3.4";
			   break;

		case  18 : Stemp = "1.2@.3.4";
			   break;

		case  19 : Stemp = "1.2.@3.4";
			   break;

		case  20 : Stemp = "!1.2.3.4";
			   break;

		case  21 : Stemp = "1.2.3.4 !";
			   break;

		case  22 : Stemp = "1.2.3.4!";
			   break;

		case  23 : Stemp = "1!.2.3.4";
			   break;

		case  24 : Stemp = "1.2!.3.4";
			   break;

		case  25 : Stemp = "1.2.!3.4";
			   break;

		case  26 : Stemp = "1-.2.3.4";
			   break;

		case  27 : Stemp = "1.2.3.4 -";
			   break;
		case  28 : Stemp = "1.2.3.4-";
			   break;
		case  29 : Stemp = "1.2-.3.4";
			   break;
		case  30 : Stemp = "1.2.3-.4";
			   break;
		case  31 : Stemp = "1.2.3+.4";
			   break;
		case  32 : Stemp = "1.2.3.4 +";
			   break;
		case  33 : Stemp = "1.2.3.4+";
			   break;
		case  34 : Stemp = "1+.2.3.4";
			   break;
		case  35 : Stemp = "1.2+.3.4";
			   break;
		case  36 : Stemp = "1.2.3.4++";
			   break;
		case  37 : Stemp = "=1.2.3.4";
			   break;
		case  38 : Stemp = "1.2.3.4 =";
			   break;
		case  39 : Stemp = "1=.2.3.4";
			   break;
		case  40 : Stemp = "1.2=.3.4";
			   break;
		case  41 : Stemp = "1.2.=3.4";
			   break;
		case  42 : Stemp = "*1.2.3.4";
			   break;
		case  43 : Stemp = "1.2.3.4 *";
			   break;
		case  44 : Stemp = "1.2.3.4*";
			   break;
		case  45 : Stemp = "1*.2.3.4";
			   break;
		case  46 : Stemp = "1.2*.3.4";
			   break;
		case  47 : Stemp = "1.2.*3.4";
			   break;
		case  48 : Stemp = "&1.2.3.4";
			   break;
		case  49 : Stemp = "1.2.3.4 &";
			   break;
		case  50 : Stemp = "1&.2.3.4";
			   break;
		case  51 : Stemp = "1.2&.3.4";
			   break;
		case  52 : Stemp = "1.2.&3.4";
			   break;
		case  53 : Stemp = "|1.2.3.4";
			   break;
		case  54 : Stemp = "1.2.3.4 |";
			   break;
		case  55 : Stemp = "1.2.3.4|";
			   break;
		case  56 : Stemp = "1|.2.3.4";
			   break;
		case  57 : Stemp = "1.2|.3.4";
			   break;
		case  58 : Stemp = "1.2.|3.4";
			   break;
		case  59 : Stemp = "\\1.2.3.4";
			   break;
		case  60 : Stemp = "1.2.3.4 \\";
			   break;
		case  61 : Stemp = "1.2.3.4\\";
			   break;
		case  62 : Stemp = "1\\.2.3.4";
			   break;
		case  63 : Stemp = "1.2\\.3.4";
			   break;
		case  64 : Stemp = "1.2.\\3.4";
			   break;
		case  65 : Stemp = ":1.2.3.4";
			   break;
		case  66 : Stemp = "1.2.3.4 :";
			   break;
		case  67 : Stemp = "1.2.3.4:";
			   break;
		case  68 : Stemp = "1:.2.3.4";
			   break;
		case  69 : Stemp = "1.:2.3.4";
			   break;
		case  70 : Stemp = "1.2.3:.4";
			   break;
		case  71 : Stemp = ";1.2.3.4";
			   break;
		case  72 : Stemp = "1.2.3.4;";
			   break;
		case  73 : Stemp = "1;.2.3.4";
			   break;
		case  74 : Stemp = "1.2;.;3.4";
			   break;
		case  75 : Stemp = "1.2.;3.4";
			   break;
		case  76 : Stemp = "\"1.2.3.4";
			   break;
		case  77 : Stemp = "1.2.3.4\"";
			   break;
		case  78 : Stemp = "1\".2.3.4";
			   break;
		case  79 : Stemp = "1.2\".3.4";
			   break;
		case  80 : Stemp = "1.2.\"3.4";
			   break;
		case  81 : Stemp = "\'1.2.3.4";
			   break;
		case  82 : Stemp = "1.2.3.4\'";
			   break;
		case  83 : Stemp = "1\'.2.3.4";
			   break;
		case  84 : Stemp = "1.2\'.3.4";
			   break;
		case  85 : Stemp = "1.2.\'3.4";
			   break;
		case  86 : Stemp = "f1.2.3.4";
			   break;
		case  87 : Stemp = "1.2.3.4f";
			   break;
		case  88 : Stemp = "1.f2.3.4";
			   break;
		case  89 : Stemp = "1.2.f3.4";
			   break;

		// Out of range
		case  90 : Stemp = "-1.2.3.4";
			   break;
		case  91 : Stemp = "1.-2.3.4";
			   break;
		case  92 : Stemp = "1.2.-3.4";
			   break;
		case  93 : Stemp = "1.2.3.-4";
			   break;
		case  94 : Stemp = "-1000000.-1000000.-1000000.-1000000";
			   break;
		case  95 : Stemp = "1000000.-1000000.-1000000.-1000000";
			   break;
		case  96 : Stemp = "1000000.1000000.-1000000.-1000000";
			   break;
		case  97 : Stemp = "1000000.1000000.1000000.-1000000";
			   break;
		case  98 : Stemp = "1000000.-1000000.1000000.1000000";
			   break;
		case  99 : Stemp = "1000000.1000000.-1000000.1000000";
			   break;

		//Skip this one = generate no error
		case 100 : Stemp = "10000.20000.30000.40000";
			   Me.result[i] = true;
			   break;

		//Unicode chars

		//Chinese
		case 101 :Stemp = "ق1.2.3.4";
			   break;
		case 102 :Stemp = "1.ق2.3.4";
			   break;
		case 103 :Stemp = "1.2.3ق.4";
			   break;
		case 104 :Stemp = "1.2.3.4ق";
			   break;
		case 105 :Stemp = "1.2.3.4 ق";
			   break;
		case 106 :Stemp = "1.ق2ق.3.4";
			   break;

		case 107 :Stemp = "ش1.2.3.4";
			   break;
		case 108 :Stemp = "1.ش2.3.4";
			   break;
		case 109 :Stemp = "1.2.3ش.4";
			   break;
		case 110 :Stemp = "1.2.3.4ش";
			   break;
		case 111 :Stemp = "1.2.3.4 ش";
			   break;
		case 112 :Stemp = "1.ش2ش.3.4";
			   break;

		case 113 :Stemp = "ش1.2.3.4";
			   break;
		case 114 :Stemp = "1.ش2.3.4";
			   break;
		case 115 :Stemp = "1.2.3ش.4";
			   break;
		case 116 :Stemp = "1.2.3.4ش";
			   break;
		case 117 :Stemp = "1.2.3.4 ش";
			   break;
		case 118 :Stemp = "1.ش2ش.3.4";
			   break;

		case 119 :Stemp = "ل1.2.3.4";
			   break;
		case 120 :Stemp = "1.ل2.3.4";
			   break;
		case 121 :Stemp = "1.2.3ل.4";
			   break;
		case 122 :Stemp = "1.2.3.4ل";
			   break;
		case 123 :Stemp = "1.2.3.4 ل";
			   break;
		case 124 :Stemp = "1.لل.3.4";
			   break;

		case 125 :Stemp = "ؤ1.2.3.4";
			   break;
		case 126 :Stemp = "1.ؤ2.3.4";
			   break;
		case 127 :Stemp = "1.2.3ؤ.4";
			   break;
		case 128 :Stemp = "1.2.3.4ؤ";
			   break;
		case 129 :Stemp = "1.2.3.4 ؤ";
			   break;
		case 130 :Stemp = "1.ؤؤ.3.4";
			   break;

		//Japanese
		case 131 :Stemp = "प1.2.3.4";
			   break;
		case 132 :Stemp = "1.प2.3.4";
			   break;
		case 133 :Stemp = "1.2.3प.4";
			   break;
		case 134 :Stemp = "1.2.3.4प";
			   break;
		case 135 :Stemp = "1.2.3.4 प";
			   break;
		case 136 :Stemp = "1.पप.3.4";
			   break;

		case 137 :Stemp = "ग1.2.3.4";
			   break;
		case 138 :Stemp = "1.ग2.3.4";
			   break;
		case 139 :Stemp = "1.2.3ग.4";
			   break;
		case 140 :Stemp = "1.2.3.4ग";
			   break;
		case 141 :Stemp = "1.2.3.4 ग";
			   break;
		case 142 :Stemp = "1.गग.3.4";
			   break;

		case 143 :Stemp = "ल1.2.3.4";
			   break;
		case 144 :Stemp = "1.ल2.3.4";
			   break;
		case 145 :Stemp = "1.2.3ल.4";
			   break;
		case 146 :Stemp = "1.2.3.4ल";
			   break;
		case 147 :Stemp = "1.2.3.4 ल";
			   break;
		case 148 :Stemp = "1.लल.3.4";
			   break;

		case 149 :Stemp = "्1.2.3.4";
			   break;
		case 150 :Stemp = "1.्2.3.4";
			   break;
		case 151 :Stemp = "1.2.3्.4";
			   break;
		case 152 :Stemp = "1.2.3.4्";
			   break;
		case 153 :Stemp = "1.2.3.4 ्";
			   break;
		case 154 :Stemp = "1.््.3.4";
			   break;

		// Russian
		case 155 :Stemp = "К1.2.3.4";
			   break;
		case 156 :Stemp = "1.К2.3.4";
			   break;
		case 157 :Stemp = "1.2.3К.4";
			   break;
		case 158 :Stemp = "1.2.3.4К";
			   break;
		case 159 :Stemp = "1.2.3.4 К";
			   break;
		case 160 :Stemp = "1.КК.3.4";
			   break;

		case 161 :Stemp = "г1.2.3.4";
			   break;
		case 162 :Stemp = "1.г2.3.4";
			   break;
		case 163 :Stemp = "1.2.3г.4";
			   break;
		case 164 :Stemp = "1.2.3.4г";
			   break;
		case 165 :Stemp = "1.2.3.4 г";
			   break;
		case 166 :Stemp = "1.г.3.4";
			   break;

		case 167 :Stemp = "ы1.2.3.4";
			   break;
		case 168 :Stemp = "1.ы2.3.4";
			   break;
		case 169 :Stemp = "1.2.3ы.4";
			   break;
		case 170 :Stemp = "1.2.3.4ы";
			   break;
		case 171 :Stemp = "1.2.3.4 ы";
			   break;
		case 172 :Stemp = "1.ыы.3.4";
			   break;

		case 173 :Stemp = "ш1.2.3.4";
			   break;
		case 174 :Stemp = "1.ш2.3.4";
			   break;
		case 175 :Stemp = "1.2.3ш.4";
			   break;
		case 176 :Stemp = "1.2.3.4ш";
			   break;
		case 177 :Stemp = "1.2.3.4 ш";
			   break;
		case 178 :Stemp = "1.шш.3.4";
			   break;


/**/
	    } //end switch

	    try {
		Me.testor[i] = new Version( Stemp );
	    } catch ( FormatException e ) {
		if ( ( i > MAX_ARG ) && ( i <= MAX_FORMAT ) ) {
		    Me.result[ i ] = true;
		    //Console.WriteLine( "Exception for " + i + ": " + e.ToString() );
		} else if ( ( i > 100 ) && ( i <= MAX_UNICODE ) ) {
		    Me.result[ i ] = true;
		    //Console.WriteLine( "Exception for " + i + ": " + e.ToString() );
		} else {
		    Console.WriteLine( "Wrong Exception for " + i + ": " + e.ToString() );
		    Console.WriteLine( "Should be FormatException." );
		}
	    } catch ( ArgumentNullException e0 ) {
		if ( i == 0 ) {
		    Me.result[ i ] = true;
		    //Console.WriteLine( "Exception for " + i + ": " + e0.ToString() );
		} else {
		    Console.WriteLine( "Wrong Exception for " + i + ": " + e0.ToString() );
		    Console.WriteLine( "Should be ArgumentNullException." );
		}
	    } catch ( ArgumentOutOfRangeException e2 ) {
		if ( ( i >= MAX_FORMAT ) && ( i <= MAX_RANGE ) ) {
		    Me.result[ i ] = true;
		    //Console.WriteLine( "Exception for " + i + ": " + e2.ToString() );
		} else {
		    Console.WriteLine( "Wrong Exception for " + i + ": " + e2.ToString() );
		    Console.WriteLine( "Should be ArgumentOutOfRangeException." );
		}
	    } catch ( ArgumentException e1 ) {
		if ( i <= MAX_ARG ) {
		    Me.result[ i ] = true;
		    //Console.WriteLine( "Exception for " + i + ": " + e1.ToString() );
		} else {
		    Console.WriteLine( "Wrong Exception for " + i + ": " + e1.ToString() );
		    Console.WriteLine( "Should be ArgumentNullException." );
		}
	    } catch ( Exception e ) {
		Console.WriteLine( "Wrong Exception for " + i + ": " + e.ToString() );
		Console.WriteLine( "Should be ArgumentNullException." );
	    }
	}

	// Compile results for return value
	for ( i = 0; i < MAX_TEST; i++ ) {
	    if ( !Me.result[i] ) {
		ret = i;
	        Console.WriteLine( "Error for #" + i + " - Version is: " + Me.testor[i].ToString() );
	    }
	}
	if ( ret < 0 )
	    ret = 100;

	if ( ret == 100 ) 
	    Console.WriteLine( "Success!" );
	else
	    Console.WriteLine( "FAILED TEST!" );

	return ret;
    }

}