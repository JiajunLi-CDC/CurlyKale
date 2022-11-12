using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._05_MeshTools
{
    public class GhcAddBoundaryCurvesToTree : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcAddboundaryCurvesToTree class.
        /// </summary>
        public GhcAddBoundaryCurvesToTree()
          : base("AddBoundaryCurvesToTree",
                "AddBoundaryCurves",
                "用于拼图算法中将边界的曲线添加至每块拼图的树形结构中，一般对接ReplaceCurveTreeAfterDeleteDuplicateCurves电池",
                "CurlyKale",
                "07 MeshTools")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("OriginCurves", "OCurves", "生长后曲线的树形结构，对接ReplaceCurveTreeAfterDeleteDuplicateCurves电池的输出NewCurves", GH_ParamAccess.tree);
            pManager.AddCurveParameter("BoundaryCurves", "BCurves", "分割好后的边界曲线，list结构", GH_ParamAccess.list);
            pManager.AddCurveParameter("PuzzleCurves", "PCurves", "join后的拼图曲线，用于判断现有曲线是否闭合，list结构", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tolerance", "首尾点重合的阈值范围", GH_ParamAccess.item, 0.05f);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("NewCurves", "NCurves", "恢复后曲线的树形结构", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> iOriginCurves = new GH_Structure<GH_Curve>();  //原始曲线
            List<Curve> iBoundaryCurves = new List<Curve>();   //迭代后曲线
            List<Curve> iPuzzleCurves = new List<Curve>();   //迭代后曲线
            double tolerance = 0;

            if (!DA.GetDataTree("OriginCurves", out iOriginCurves)) return;
            if (!DA.GetDataList("BoundaryCurves", iBoundaryCurves)) return;
            if (!DA.GetDataList("PuzzleCurves", iPuzzleCurves)) return;
            if (!DA.GetData("Tolerance", ref tolerance)) return;

            //...............................................................................初始化数据
            DataTree<Curve> outCurves;     //输出DataTree

            outCurves = new DataTree<Curve>();

            for (int i = 0; i < iPuzzleCurves.Count; i++)   //对于每个树枝
            {
                GH_Path subPath = new GH_Path(i);

                for (int j = 0; j < iOriginCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    Curve curve1 = iOriginCurves.Branches[i][j].Value;
                    outCurves.Add(curve1, subPath);    //将ghcurve转换为curve
                }
            }

            for (int i = 0; i < iBoundaryCurves.Count; i++)   //对于每个边界线
            {
                Curve curveNow = iBoundaryCurves[i];
                Point3d startPoint = curveNow.PointAtStart;
                Point3d endPoint = curveNow.PointAtEnd;     //获取每根边界层曲线的首尾点


                for (int j = 0; j < iPuzzleCurves.Count; j++)   //对于每个拼图边界线
                {
                    GH_Path subPath = new GH_Path(j); //拼图路径序号和曲线分支序号相同

                    if (iPuzzleCurves[j].IsClosed)
                    {
                        continue;
                    }
                    else
                    {
                        Point3d startPoint1 = iPuzzleCurves[j].PointAtStart;
                        Point3d endPoint1 = iPuzzleCurves[j].PointAtEnd;    //获取拼图曲线的首尾点

                        if (startPoint.DistanceTo(startPoint1) < tolerance && endPoint.DistanceTo(endPoint1) < tolerance)
                        {
                            outCurves.Add(iBoundaryCurves[i], subPath);    //如果两根曲线首尾相同则在第j组数形结构中加入边界线
                        }
                        else if (startPoint.DistanceTo(endPoint1) < tolerance && endPoint.DistanceTo(startPoint1) < tolerance)
                        {
                            outCurves.Add(iBoundaryCurves[i], subPath);    //如果两根曲线首尾相同则加入新的数形结构
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
                return Properties.Resources.AddBoundaryCurvesToTreeIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("9b2bed3e-dfb2-4ee5-82a4-6cfd8d998c33");
            }
        }
    }
}