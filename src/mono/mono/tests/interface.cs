using System;

namespace Obj {
	interface Measurable {
		double Area ();
	};
	class Obj : Measurable {
		public double Area () {
			return 0.0;
		}
	};
	class Rect : Obj {
		int x, y, w, h;
		public Rect (int vx, int vy, int vw, int vh) {
			x = vx;
			y = vy;
			w = vw;
			h = vh;
		}
		public new double Area () {
			return (double)w*h;
		}
	}
	class Circle : Obj {
		int x, y, r;
		public Circle (int vx, int vy, int vr) {
			x = vx;
			y = vy;
			r = vr;
		}
		public new double Area () {
			return r*r*System.Math.PI;
		}
	}
	class Test {
		static public int Main () {
			Obj rect, circle;
			double sum;
			rect = new Rect (0, 0, 10, 20);
			circle = new Circle (0, 0, 20);
			sum = rect.Area() + circle.Area ();
			/* surprise! this calls Obj.Area... */
			if (sum != 0.0)
				return 1;
			/* now call the derived methods */
			sum = ((Rect)rect).Area() + ((Circle)circle).Area ();
			if (sum != (200 + 400*System.Math.PI))
				return 2;
			/* let's try to cast to the interface, instead */
			sum = ((Measurable)rect).Area() + ((Measurable)circle).Area ();
			if (sum != 0.0)
				return 3;
			return 0;
		}
	};
};
