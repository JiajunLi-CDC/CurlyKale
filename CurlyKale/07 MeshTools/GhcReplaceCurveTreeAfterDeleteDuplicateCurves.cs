using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._05_MeshTools
{
    public class GhcReplaceCurveTreeAfterDeleteDuplicateCurves : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcReplaceCurveTreeAfterDeleteDuplicateCurves class.
        /// </summary>
        public GhcReplaceCurveTreeAfterDeleteDuplicateCurves()
          : base("ReplaceCurveTreeAfterDeleteDuplicateCurves",
                "ReplaceCurveTree",
                "用于拼图算法中拍平曲线数组，删除重复线并迭代后重新恢复曲线的树形结构，根据遍历曲线首尾点判断",
                "CurlyKale",
                "07 MeshTools")
        {
        }

     
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("OriginCurves", "OCurves", "原始曲线的树形结构", GH_ParamAccess.tree);
            pManager.AddCurveParameter("AfterCurves", "ACurves", "拍平后删除重复线后的曲线，list结构", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tolerance", "首尾点重合的阈值范围", GH_ParamAccess.item, 0.05f);
        }

      
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("NewCurves", "NCurves", "恢复后曲线的树形结构", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> iOriginCurves = new GH_Structure<GH_Curve>();  //原始曲线
            List<Curve> iAfterCurves = new List<Curve>();   //迭代后曲线
            double tolerance = 0;

            if (!DA.GetDataTree("OriginCurves", out iOriginCurves)) return;
            if (!DA.GetDataList("AfterCurves", iAfterCurves)) return;
            if (!DA.GetData("Tolerance", ref tolerance)) return;

            //...............................................................................初始化数据

            DataTree<Curve> outCurves;     //输出DataTree

            outCurves = new DataTree<Curve>();

            for (int i = 0; i < iOriginCurves.Branches.Count; i++)   //对于每个树枝
            {
                GH_Path subPath = new GH_Path(i);

                for (int j = 0; j < iOriginCurves.Branches[i].Count; j++)   //对于每个树枝中的曲线
                {
                    Curve curveNow = iOriginCurves.Branches[i][j].Value;   
                    Point3d startPoint = curveNow.PointAtStart;
                    Point3d endPoint = curveNow.PointAtEnd;     //获取每根原始曲线的首尾点

                    for(int k=0; k < iAfterCurves.Count; k++)
                    {
                        Point3d startPoint1 = iAfterCurves[k].PointAtStart;
                        Point3d endPoint1 = iAfterCurves[k].PointAtEnd;    //获取迭代后曲线的首尾点

                        if (startPoint.DistanceTo(startPoint1)< tolerance && endPoint.DistanceTo(endPoint1)< tolerance)
                        {
                            outCurves.Add(iAfterCurves[k],subPath);    //如果两根曲线首尾相同则加入新的数形结构
                        }else if (startPoint.DistanceTo(endPoint1) < tolerance && endPoint.DistanceTo(startPoint1) < tolerance)
                        {
                            outCurves.Add(iAfterCurves[k], subPath);    //如果两根曲线首尾相同则加入新的数形结构
                        }
         
                    }
                }
            }

            DA.SetDataTree(0, outCurves);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.ReplaceCurveTreeAfterDeleteDuplicateCurvesIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("781b8e58-0b6e-4c7c-8ad6-706107482655");
            }
        }
    }
}