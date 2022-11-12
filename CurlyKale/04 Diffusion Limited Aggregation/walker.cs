//using Grasshopper;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Data;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace CurlyKale
//{
//    /*
//     * LJJ修改于2022.8.7
//     * 获取顶点的相连顶点采用了topology方法，不用遍历每个顶点，加快了运算速度
//     * 采用了多线程写法，加快了运算速度
//     * */
//    public class Walker
//    {
//        Point3d pos;
//        bool stuck;
//        double r;
//        Random random = new Random();

//        public Walker(double radius,double rangeCircle)
//        {
//            pos = randomPoint(rangeCircle);
//            stuck = false;
//            r = radius;
//        }

//        public Walker(Point3d pos_, double radius)
//        {
//            pos = pos_;
//            stuck = true;
//            r = radius;
//        }

//        public void walk()
//        {

//            Vector3d vel = new Vector3d(random.NextDouble() * -1, random.NextDouble() * 1, 0);
//            // PVector vel = createVector(random(-1, 1), random(-0.5, 1));
//            pos += vel;

//            //pos.X = constrain(pos.x, 0, width);
//            //pos.Y = constrain(pos.y, 0, height);
//        }

//        void show()
//        {

//        }

//        public bool checkStuck(List<Walker> others)
//        {
//            for (int i = 0; i < others.Count(); i++)
//            {           
//                double d = pos.DistanceTo(others[i].pos);
//                Walker other = others[i];
//                if (d < (r + other.r))  //如果发生碰撞
//                {
//                    //if (random(1) < 0.1) {
//                    stuck = true;
//                    return true;
//                    //break;
//                    //}
//                }
//            }
//            return false;
//        }

//        float distSq(Point3d a, Vector3d b)
//        {
//            float dx = (float)(b.X - a.X);
//            float dy = (float)(b.Y - a.Y);
//            return dx * dx + dy * dy;
//        }



//        Point3d randomPoint(double rangeCircle)
//        {
//            double x = random.NextDouble() * rangeCircle;
//            double y = random.NextDouble() * rangeCircle;
//            Point3d pos_ = new Point3d(x,y,0);

//            return pos_;
//        }


//    }



//}

