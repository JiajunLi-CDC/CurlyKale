using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._05_Self_Locking
{
    public class GhcGetPointOnCurve : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcGetPointOnCurve class.
        /// </summary>
        public GhcGetPointOnCurve()
          : base("GetPointOnCurve", "GetPointOnCurve",
              "根据给定的值求取曲线上的点",
               "CurlyKale", "05 Self-Locking")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("BaseCurves", "BaseCurves", "初始的曲线树形数据", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Parameter", "Parameter", "点的位置参数值", GH_ParamAccess.item, 0.5f);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "曲线上的点", GH_ParamAccess.tree);
        }

       
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> iBaseCurves = new GH_Structure<GH_Curve>();  //原始曲线
            double iParameter = 0;

            if (!DA.GetDataTree("BaseCurves", out iBaseCurves)) return;
            if (!DA.GetData("Parameter", ref iParameter)) return;

            //...............................................................................初始化数据
            DataTree<Point3d> OutPoints = new DataTree<Point3d>();     //输出DataTree
       

            for (int i = 0; i < iBaseCurves.PathCount; i++)   //对于每个树枝
            {
                
                for (int j = 0; j < iBaseCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    GH_Path subPath = new GH_Path(i);
                    Curve curve1 = iBaseCurves.Branches[i][j].Value;
                    Point3d point1 = curve1.PointAtNormalizedLength(iParameter);
                    OutPoints.Add(point1, subPath);    //将ghcurve转换为curve
                }
            }


            //.............................................................................输出

            DA.SetDataTree(0, OutPoints);

        }

 
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("30989cc2-4425-4526-85d7-b42a102a1a6c");
            }
        }
    }
}