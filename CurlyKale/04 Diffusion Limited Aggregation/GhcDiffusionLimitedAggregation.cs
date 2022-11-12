//using Grasshopper.Kernel;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace CurlyKale._04_Diffusion_Limited_Aggregation
//{
//    public class GhcDiffusionLimitedAggregation : GH_Component
//    {
//        /// <summary>
//        /// Initializes a new instance of the GhcDiffusionLimitedAggregation class.
//        /// </summary>
//        public GhcDiffusionLimitedAggregation()
//          : base("GhcDiffusionLimitedAggregation", "DLA",
//              "用于扩散限制聚合的2D与3D计算",
//              "CurlyKale", "04 Diffusion Limited Aggregation")
//        {
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddPointParameter("StartPoints", "SP", "初始的生长点", GH_ParamAccess.list);
//            pManager.AddIntegerParameter("MaxPointsCount", "MCount", "最大节点数", GH_ParamAccess.item);
//            pManager.AddBooleanParameter("ifReset", "ifRest", "ifRest", GH_ParamAccess.item, true);
//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {

//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            List<Point3d> iStartPoints = new List<Point3d>();
//            List<Walker> tree = new List<Walker>();       //装载最终的树点
//            List<Walker> walkers = new List<Walker>();   //随机游走的点
//            bool ifReset = true;
//            int maxWalkers = 150;
//            int iterations = 1000;
//            double radius = 12;
//            double shrink = 0.995;

//            if (!DA.GetDataList("StartPoints", iStartPoints)) return;
//            if (!DA.GetData("MaxPointsCount", ref maxWalkers)) return;
//            if (!DA.GetData("ifReset", ref ifReset)) return;


//            if(ifReset)
//            {
//                radius *= shrink;
//                for (int i = 0; i < maxWalkers; i++)
//                {
//                    walkers.Add(new Walker(radius));
//                    radius *= shrink;
//                }

//                for (int i = 0; i < iStartPoints.Count; i++)
//                {
//                    tree.Add(new Walker(iStartPoints[i], radius));
//                }
//            }

//            //for (int i = 0; i < tree.Count(); i++)
//            //{
//            //    tree[i].show();
//            //}

//            //for (int i = 0; i < walkers.Count(); i++)
//            //{
//            //    walkers[i].show();
//            //}

//            for (int n = 0; n < iterations; n++)
//            {
//                for (int i = walkers.Count() - 1; i >= 0; i--)
//                {
//                    Walker walker = walkers[i];
//                    walker.walk();
//                    if (walker.checkStuck(tree))  //如果发生碰撞
//                    {
//                        tree.Add(walker);
//                        walkers.RemoveAt(i);
//                    }
//                }
//            }

//            //float r = walkers.get(walkers.size() - 1).r;
//            while (walkers.Count() < maxWalkers && radius > 1)
//            {
//                radius *= shrink;
//                walkers.Add(new Walker(radius));
//            }
//        }


//        /// <summary>
//        /// Provides an Icon for the component.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                //You can add image files to your project resources and access them like this:
//                // return Resources.IconForThisComponent;
//                return null;
//            }
//        }

//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get
//            {
//                return new Guid("a66bcab8-1e4c-420d-8ab8-259b283b7e7f");
//            }
//        }
//    }
//}