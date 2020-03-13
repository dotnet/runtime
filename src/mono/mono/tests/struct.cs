using System;

struct Point 
{ 
  public int x, y, z; 
  public Point(int x, int y) { 
  	this.x = x; 
  	this.y = y;
  	this.z = 5;
  }
  public static Point get_zerop () {
		Point p = new Point (0, 0);
		p.z = 0;
		return p;
  }
  public static int struct_param (Point p) {
		if (p.x != p.y || p.y != p.z || p.z != 0)
			return 1;
		/* should modify the local copy */
		p.x = 1;
		p.y = 2;
		p.z = 3;
		return 0;
  }
} 

public class test {
	public static int Main () {
		Point p = new Point (10, 20);
		Point c = p;
		Point zp;
		
		if (c.x != 10)
			return 1;
		if (c.y != 20)
			return 2;
		if (c.z != 5)
			return 3;
		if (p.x != 10)
			return 4;
		if (p.y != 20)
			return 5;
		if (p.z != 5)
			return 6;
		p.z = 7;
		if (p.z != 7)
			return 7;
		if (c.x != 10)
			return 8;

		zp = Point.get_zerop ();
		if (zp.x != zp.y || zp.y != zp.z || zp.z != 0)
			return 9;
		if (Point.struct_param (zp) != 0)
			return 10;
		if (zp.x != zp.y || zp.y != zp.z || zp.z != 0)
			return 11;

		// Test that the object is properly unboxed when called through
		// the reflection interface
		object o = Activator.CreateInstance (typeof (Point), new object [] { 1, 2 });
		if (!(o is Point))
			return 12;

		return 0;
	}
}
