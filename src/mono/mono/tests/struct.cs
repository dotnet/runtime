struct Point 
{ 
  public int x, y, z; 
  public Point(int x, int y) { 
  this.x = x; 
  this.y = y;
  this.z = 5;
 } 
} 

public class test {
	public static int Main () {
		Point p = new Point (10, 20);
		Point c = p;
		if (c.x != 10)
			return 1;
		if (c.y != 20)
			return 2;
		return 0;
	}
}
