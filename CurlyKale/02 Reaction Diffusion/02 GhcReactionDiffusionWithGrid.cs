using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CurlyKale._02_Reaction_Diffusion
{
    public class GhcReactionDiffusionWithGrid : GH_Component
    {
        
        public GhcReactionDiffusionWithGrid()
          : base("ReactionDiffusionWithGrid", "Reation2DGrid",
             "用于平面矩阵的反应扩散算法",
             "CurlyKale", "02 Reaction Diffusion")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset Simulation", "RES", "Clear and Reload all values.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Run Simulation", "RUN", "Run Simulation", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Diffusion Rate A", "dA", "A扩散率.", GH_ParamAccess.item, 1d);
            pManager.AddNumberParameter("Diffusion Rate B", "dB", "B扩散率.", GH_ParamAccess.item, 0.3d);
            pManager.AddNumberParameter("Feed Rate", "F", "给进B的速率.", GH_ParamAccess.item, 0.055d);
            pManager.AddNumberParameter("Kill Rate", "K", "杀死A的速率.", GH_ParamAccess.item, 0.062d);
            pManager.AddIntegerParameter("Iteration Count", "IC", "迭代次数", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Grid Width", "X", "X方向个数", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Grid Height", "Y", "Y方向个数", GH_ParamAccess.item, 100);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Debug Log", "OUT", "Message log for debugging.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "对应的格点", GH_ParamAccess.list);
            pManager.AddNumberParameter("Value", "V", "每个点相应的数值，0-1", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData(0, ref reset)) return;
            if (!DA.GetData(1, ref run)) return;

            int numCount = 0;
            if (!DA.GetData(6, ref numCount)) return;

            if (reset)
            {
                int tempx = 0;
                int tempy = 0;
                double dA = 0;
                double dB = 0;
                double kill = 0;
                double feed = 0;
               

                if (!DA.GetData(2, ref dA)) return;
                if (!DA.GetData(3, ref dB)) return;
                if (!DA.GetData(4, ref feed)) return;
                if (!DA.GetData(5, ref kill)) return;               
                if (!DA.GetData(7, ref tempx)) return;
                if (!DA.GetData(8, ref tempy)) return;             

                Setup(tempx, tempy, dA, dB, feed, kill);
            }

            if (run) Update(numCount);

            //DA.SetDataList(0, debugLog);
            DA.SetDataList(0, rd.GetOutPoints());
            DA.SetDataList(1, rd.GetOutValues());
        }

        bool reset, run;

        ReactionDiffuser rd;
        public static List<string> debugLog = new List<string>();
        public static int frameCount;


        public void Setup(int xRes_, int yRes_, double dA_, double dB_, double feed_, double kill_)
        {
            rd = new ReactionDiffuser(xRes_, yRes_, dA_, dB_, feed_, kill_);
            rd.SetSeed(10, xRes_ , 10, yRes_);         
            //frameCount = 0;
        }


        public void Update(int numCount_)
        {
            //debugLog.Clear();
            for (int i=0;i< numCount_; i++)
            {
                rd.Update();
            }
            
            //frameCount++;
            //debugLog.Add(frameCount.ToString());
        }


        //===============================================================================

        //add classes, functions

        public class ReactionDiffuser
        {
            //................................................world holds no geometry. pure relative location + chemical ratio
            private double[,,] grid;      //初始状态
            private double[,,] nextGrid;           //下一次迭代
            //...........................................................geom for output
            //public List<Mesh> pixelsOut = new List<Mesh>();

            public List<double> outValue;   //输出值
            public List<Point3d> outPoints;   //输出点

            private int xRes, yRes;
            private Random r = new Random();

            public int XRes
            {
                get
                {
                    return xRes;
                }
                set
                {
                    xRes = value;
                }
            }
            public int YRes
            {
                get
                {
                    return YRes;
                }
                set
                {
                    yRes = value;
                }
            }

            //diffusion parameters 输入参数

            private double dA = 1f;
            private double dB = 0.3f;
            private double feed = 0.055f;
            private double kill = 0.062f;

            public double DA
            {
                get
                {
                    return dA;
                }
                set
                {
                    dA = value;
                }
            }
            public double DB
            {
                get
                {
                    return dB;
                }
                set
                {
                    dB = value;
                }
            }

            public double Feed
            {
                get
                {
                    return feed;
                }
                set
                {
                    feed = value;
                }
            }

            public double Kill
            {
                get
                {
                    return kill;
                }
                set
                {
                    kill = value;
                }
            }


            //private int counter;

            public ReactionDiffuser(int xRes_, int yRes_, double dA_, double dB_, double feed_, double kill_)
            {
                xRes = xRes_;
                yRes = yRes_;
                dA = dA_;
                dB = dB_;
                feed = feed_;
                kill = kill_;
                CreateGrid(xRes_, yRes_);
                nextGrid = grid;
            }

            private void CreateGrid(int xRes, int yRes)
            {
                grid = new double[xRes,yRes,2];      //创建数组记录ab的值
                nextGrid = new double[xRes,yRes,2];      //创建数组记录下一时刻ab的值

                for (int i = 0; i < xRes; i++)
                {
                    for (int j = 0; j < yRes; j++)
                    {
                        grid[i, j, 0] = 1;     //a值           
                        grid[i, j, 1] = 0;     //b值

                        nextGrid[i, j, 0] = 0;    //下一刻a值
                        nextGrid[i, j, 1] = 0;    //下一刻b值
                    }
                }



                outPoints = new List<Point3d>();
                for (int i = 0; i < xRes; i++)
                {
                    for (int j = 0; j < yRes; j++)
                    {
                        Point3d pointout = new Point3d(i, j, 0);
                        outPoints.Add(pointout);
                    }
                }
            }

            public void SetSeed(int xInter, int xOuter, int yInter, int yOuter)
            {
                int a = (int)(xOuter * 0.5);
                int b = (int)(yOuter * 0.5);
                int c = (int)(xInter * 0.5);   //内部初始矩形种子边长
                int d = (int)(yInter * 0.5);

                for (int i = a - c; i < a + c; i++)
                {
                    for (int j = b - d; j < b + d; j++)
                    {
                        grid[i, j, 1] = 1;  //设置初始种子b值
                    }
                }
            }
            public void Update()
            {

                //counter++;
                //if (counter > 5)
                //{
                //    //pixelsOut = MeshRender();
                //    counter = 0;
                //}
                ReactionDiffuse();
                SwapGrid();
            }

            private void SwapGrid()
            {
                double[,,] tempGrid = grid;
                grid = nextGrid;
                nextGrid = tempGrid;
            }

            private void ReactionDiffuse()
            {
                //double deltaTime = 1f;
                for (int i = 1; i < xRes - 1; i++)
                {
                    for (int j = 1; j < yRes - 1; j++)
                    {
                        double a = grid[i, j, 0];
                        double b = grid[i, j, 1];

                        //double rA = a + dA * LaPlaceA(i, j) - a * b * b + feed * (1 - a) * deltaTime;
                        //double rB = b + dB * LaPlaceB(i, j) + a * b * b - (kill + feed) * b * deltaTime;

                        double rA = a + dA * LaPlaceA(i, j) - a * b * b + feed * (1 - a);         //反应的前后状态公式，详见文章
                        double rB = b + dB * LaPlaceB(i, j) + a * b * b - (kill + feed) * b ;

                        rA = Constrain(rA, 0, 1d);  //确保值在0,1范围内
                        rB = Constrain(rB, 0, 1d);

                        nextGrid[i, j, 0] = rA;
                        nextGrid[i, j, 1] = rB;
                    }
                }
            }

            private double LaPlaceA(int i, int j)
            {                                    //卷积公式，下一时刻细胞状态取决于周围一圈的格子状态，每个格子施加一个影响系数
                double sumA = 0;
                sumA += grid[i,j,0] * -1;
                sumA += grid[i - 1,j,0] * 0.2;
                sumA += grid[i + 1,j,0] * 0.2;
                sumA += grid[i,j - 1,0] * 0.2;
                sumA += grid[i,j + 1,0] * 0.2;
                sumA += grid[i - 1,j - 1,0] * 0.05;
                sumA += grid[i - 1,j + 1,0] * 0.05;
                sumA += grid[i + 1,j - 1,0] * 0.05;
                sumA += grid[i + 1,j + 1,0] * 0.05;
                return sumA;
            }
            private double LaPlaceB(int i, int j)
            {
                double sumB = 0;
                sumB += grid[i,j,1] * -1;
                sumB += grid[i - 1,j,1] * 0.2;
                sumB += grid[i + 1,j,1] * 0.2;
                sumB += grid[i,j - 1,1] * 0.2;
                sumB += grid[i,j + 1,1] * 0.2;
                sumB += grid[i - 1,j - 1,1] * 0.05;
                sumB += grid[i - 1,j + 1,1] * 0.05;
                sumB += grid[i + 1,j - 1,1] * 0.05;
                sumB += grid[i + 1,j + 1,1] * 0.05;
                return sumB;
            }

           

            public List<Point3d> GetOutPoints()
            {
              

               

                return outPoints;
            }

            public List<double> GetOutValues()
            {
                outValue = new List<double>();

                for (int i = 0; i < xRes; i++)
                {
                    for (int j = 0; j < yRes; j++)
                    {
                        double val = Constrain((grid[i, j, 0] - grid[i, j, 1]), 0, 1d); ;
                        //double val = grid[i, j, 0] - grid[i, j, 1];
                        outValue.Add(val);
                    }
                }

                return outValue;
            }


            //create a mesh-representation in 3d space for each pixel
            //public List<Mesh> MeshRender()
            //{

            //    List<Mesh> recs = new List<Mesh>();

            //    for (int i = 0; i < xRes; i++)
            //    {
            //        for (int j = 0; j < yRes; j++)
            //        {
            //            double a = grid[i][j][0];
            //            double b = grid[i][j][1];
            //            int balance = (int)Constrain((a - b) * 255, 0d, 255d);
            //            Color c = Color.FromArgb(255, balance, balance, balance);

            //            Point3d p1 = new Point3d(i, j, 0);
            //            Point3d p2 = new Point3d(p1.X + 1, p1.Y, 0);
            //            Point3d p3 = new Point3d(p1.X + 1, p1.Y + 1, 0);
            //            Point3d p4 = new Point3d(p1.X, p1.Y + 1, 0);

            //            Mesh tempMesh = new Mesh();
            //            tempMesh.Vertices.Add(p1);
            //            tempMesh.VertexColors.Add(c);
            //            tempMesh.Vertices.Add(p2);
            //            tempMesh.VertexColors.Add(c);
            //            tempMesh.Vertices.Add(p3);
            //            tempMesh.VertexColors.Add(c);
            //            tempMesh.Vertices.Add(p4);
            //            tempMesh.VertexColors.Add(c);
            //            tempMesh.Faces.AddFace(0, 1, 2, 3);
            //            recs.Add(tempMesh);
            //        }
            //    }
            //    return recs;
            //}

        }



        public static double Constrain(double val, double min, double max)
        {
            if (val > max) val = max;
            else if (val < min) val = min;
            return val;
        }


        //Cell holding chemical ratio
        //public struct Cell
        //{
        //    public double a, b;

        //    public Cell(double a_, double b_)
        //    {
        //        a = a_;
        //        b = b_;
        //    }
        //}
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.ReactionDiffusionIcon; 
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("b2e52f17-a499-4bf8-bebe-9db4ac90f1bb");
            }
        }
    }
}