// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;



namespace DefaultNamespace
{

    using System;

    internal abstract class baseObject
    {
        private static int s_count = 0;
        private static int s_id = 1;

        public virtual void changeCount(int amount)
        {
            if ((s_count + amount) < 0)
                throw new ArgumentException();
            s_count += amount;
        }

        internal abstract int getId();
        internal abstract float getArea();

        public virtual int readId()
        {
            return s_id;
        }

        public virtual int readcount()
        {
            return s_count;
        }
    }  // baseObject




    internal class Rectangle : baseObject
    {
        public static int count = 0;  // same name as base member
        internal static int id = 2;

        private float _dimension1;
        private float _dimension2;

        internal Rectangle(float d1, float d2)
        {

            _dimension1 = _dimension2 = (float)0.0;

            if ((d1 <= 0.0) || (d2 <= 0.0))
                throw new ArgumentException();
            _dimension1 = d1;
            _dimension2 = d2;
            changeCount(1);
        }

        public override void changeCount(int amount)
        {
            base.changeCount(amount);
            if ((count + amount) < 0)
                throw new ArgumentException();
            count += amount;
        }

        internal override float getArea()
        {
            float area;
            area = _dimension1 * _dimension2;
            return area;
        }

        public virtual void remove()
        {
            changeCount(-1);
        }

        internal override int getId()
        {
            return Rectangle.id;
        }
    }  // class Rectangle


    internal class Circle : baseObject
    {
        private float _radius = (float)0.0;
        public int count = 0;  // same name as base member
        internal int id = 3;

        internal Circle(float r)
        {
            if (r <= 0.0)
                throw new ArgumentException();
            _radius = r;
            changeCount(1);
        }


        public override void changeCount(int amount)
        {
            if ((count + amount) < 0)
                throw new ArgumentException();
            count += amount;
            base.changeCount(amount);
        }

        internal override float getArea()
        {
            float area;
            area = (float)(3.14 * _radius * _radius);
            return area;
        }

        internal override int getId()
        {
            return id;
        }

        virtual public void remove()
        {
            changeCount(-1);
        }
    }  // class Circle



    public class EAObject
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int successes = 0;
            int result;
            float area;

            baseObject[] aObjects = new baseObject[10];
            Rectangle rRectangle;
            Circle rCircle;

            aObjects[0] = new Rectangle((float)5.0, (float)7.0);

            result = aObjects[0].getId();
            if (result != 2)
            {
                return 1;
            }
            else successes = 1;

            area = aObjects[0].getArea();
            if (area != 35.0)
            {
                return 1;
            }
            else successes++;


            result = aObjects[0].readId();
            if (result != 1)
            {
                return 1;
            }
            else successes++;

            result = aObjects[0].readcount();
            if (result != 1)
            {
                return 1;
            }
            else successes++;

            aObjects[1] = new Circle((float)4.0);

            result = aObjects[1].getId();
            if (result != 3)
            {
                return 1;
            }
            else successes++;

            area = aObjects[1].getArea();
            if (area != (float)(4.0 * 4.0 * 3.14))
            {
                return 1;
            }
            else successes++;


            result = aObjects[1].readcount();
            if (result != 2)
            {
                return 1;
            }
            else successes++;

            rRectangle = (Rectangle)aObjects[0];
            result = Rectangle.count;
            if (result != 1)
            {
                return 1;
            }
            else successes++;

            rCircle = (Circle)aObjects[1];
            result = rCircle.count;
            if (result != 1)
            {
                return 1;
            }
            else successes++;

            bool ok = true;
            int tryvar = 1;
            try
            {
                aObjects[5] = new Rectangle((float)0.0, (float)7.0);
                tryvar = 2;
            }
            catch (ArgumentException /*ae*/  )
            {
                if (tryvar != 1)
                {
                    return 1;
                }
                tryvar = 3;
            }
            finally
            {
                ok = tryvar == 3;
            }
            if (!ok)
            {
                return 1;
            }

            tryvar = 1;
            try
            {
                aObjects[5] = new Circle((float)0.0);
                tryvar = 2;
            }
            catch (ArgumentException /*ae*/  )
            {
                if (tryvar != 1)
                {
                    return 1;
                }
                tryvar = 3;
            }
            finally
            {
                ok = tryvar == 3;
            }
            if (!ok)
            {
                return 1;
            }

            try
            {
                tryvar = 1;
                rRectangle.changeCount(-5);
            }
            catch (ArgumentException /*ae1*/ )
            {
                tryvar = 2;
            }
            finally
            {
                ok = tryvar == 2;
            }
            if (!ok)
            {
                return 1;
            }


            aObjects[2] = new Rectangle((float)2.0, (float)3.0);

            result = aObjects[2].getId();
            if (result != 2)
            {
                return 1;
            }
            else successes++;

            area = aObjects[2].getArea();
            if (area != 6.0)
            {
                return 1;
            }
            else successes++;


            result = aObjects[2].readId();
            if (result != 1)
            {
                return 1;
            }
            else successes++;

            result = aObjects[2].readcount();
            if (result != 3)
            {
                return 1;
            }
            else successes++;

            aObjects[3] = new Circle((float)8.0);
            result = aObjects[3].getId();
            if (result != 3)
            {
                return 1;
            }
            else successes++;

            area = aObjects[3].getArea();
            if (area != (float)(8.0 * 8.0 * 3.14))
            {
                return 1;
            }
            else successes++;


            result = aObjects[3].readcount();
            if (result != 4)
            {
                return 1;
            }
            else successes++;


            rRectangle = (Rectangle)aObjects[0];
            result = Rectangle.count;
            if (result != 2)
            {
                return 1;
            }
            else successes++;

            rCircle = (Circle)aObjects[3];
            result = rCircle.count;



            return 100;
        }  // end main()
    }  // end class EAObject
}
