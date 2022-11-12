using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace CurlyKale._05_Self_Locking
{
    public class GhcGetEdgeTreeData : GH_Component
    {
        
        public GhcGetEdgeTreeData()
          : base("GetEdgeTreeData", "GetEdgeTreeData",
              "获取需要的三角网格的树形结构数据，便于后续操作",
              "CurlyKale", "05 Self-Locking")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("BaseCurves", "BaseCurves", "初始的曲线树形数据", GH_ParamAccess.tree);
            pManager.AddCurveParameter("OffsetCurves", "OffsetCurves", "每根面曲线向内offset后树形数据", GH_ParamAccess.tree);
        }

       
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {          
            pManager.AddCurveParameter("InternalCurves", "InternalCurves", "只包含内部曲线的树形结构，两根对边为一组", GH_ParamAccess.tree);
            pManager.AddCurveParameter("BoundaryCurves", "BoundaryCurves", "只包含边界的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("BulgeCurves", "BulgeCurves", "用作凸结构的线的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("ConcaveCurves", "ConcaveCurves", "用作凹结构的线的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("BulgeCurvesNeighbors", "BulgeCurvesNeighbors", "用作凸结构的线的对边的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);         
            pManager.AddCurveParameter("ConcaveCurvesNeighbors", "ConcaveCurvesNeighbors", "用作凹结构的线的对边的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("BulgeCurvesOrigin", "BulgeCurvesOrigin", "用作凸结构的线的对应原始边的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("ConcaveCurvesOrigin", "ConcaveCurvesOrigin", "用作凹结构的线的对应原始边的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("AllBulgeCurvesInOneFace", "AllBulgeCurvesInOneFace", "只包含凸结构的面的边的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
            pManager.AddCurveParameter("AllBulgeCurvesInOneFaceWithBoundary", "AllBulgeCurvesInOneFaceWithBoundary", "只包含凸结构的面的边和边界的树形结构，共有三角面数个分枝", GH_ParamAccess.tree);
        }
     
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> iBaseCurves = new GH_Structure<GH_Curve>();  //原始曲线
            GH_Structure<GH_Curve> iBaseOffsetCurves = new GH_Structure<GH_Curve>();  //offset后曲线

            if (!DA.GetDataTree("BaseCurves", out iBaseCurves)) return;
            if (!DA.GetDataTree("OffsetCurves", out iBaseOffsetCurves)) return;

            //...............................................................................初始化数据

            DataTree<Curve> OriginalCurves = new DataTree<Curve>();     //原始DataTree
            DataTree<Curve> OriginalOffsetCurves = new DataTree<Curve>();     //原始DataTree
           


            //.............................................................................将ghcurve转换为curve
            for (int i = 0; i < iBaseCurves.Branches.Count; i++)   //对于每个树枝
            {
                GH_Path subPath = new GH_Path(i);

                for (int j = 0; j < iBaseCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    Curve curve1 = iBaseCurves.Branches[i][j].Value;
                    OriginalCurves.Add(curve1, subPath);    //将ghcurve转换为curve
                }
            }

            for (int i = 0; i < iBaseOffsetCurves.Branches.Count; i++)   //对于每个树枝
            {
                GH_Path subPath = new GH_Path(i);

                for (int j = 0; j < iBaseOffsetCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    Curve curve1 = iBaseOffsetCurves.Branches[i][j].Value;
                    OriginalOffsetCurves.Add(curve1, subPath);    //将ghcurve转换为curve
                }
            }

            //.............................................................................获取只包含内部曲线的树形结构，两根对边为一组

            DataTree<Curve> InternalCurves = new DataTree<Curve>();     //输出只包含内部曲线的树形结构DataTree
            List<Curve> allHalfEdge = OriginalCurves.AllData();
            List<Curve> allOffsetHalfEdge = OriginalOffsetCurves.AllData();

            int count = 0;
            for (int i=0; i < allHalfEdge.Count; i++)
            {
                for (int j = i + 1; j < allHalfEdge.Count; j++)
                {
                    Point3d point1 = GetMidPoint(allHalfEdge[i]);
                    Point3d point2 = GetMidPoint(allHalfEdge[j]);

                    if (point1.DistanceTo(point2)<0.01)  //如果是重合半边
                    {
                        GH_Path subPath = new GH_Path(count);
                        InternalCurves.Add(allOffsetHalfEdge[i],subPath);
                        InternalCurves.Add(allOffsetHalfEdge[j], subPath);

                        count += 1;
                    }
                }
            }


            //.............................................................................获取只包含边界的树形结构，共有三角面数个分枝

            DataTree<Curve> BoundaryCurves = new DataTree<Curve>();     //输出只包含边界的树形结构，共有三角面数个分枝
            List<Curve> Internal = InternalCurves.AllData();  //所有内部线

            for (int i = 0; i < OriginalOffsetCurves.Branches.Count; i++)   //对于每个面
            {
                for (int j = 0; j < OriginalOffsetCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    Curve curve1 = OriginalOffsetCurves.Branches[i][j]; 

                    if (!Internal.Contains(curve1))//如果不是内部线
                    {

                        GH_Path subPath = new GH_Path(i);
                        BoundaryCurves.Add(curve1, subPath);
                    }
                    
                }
            }


            //.............................................................................用作凸结构的线的树形结构，共有三角面数个分枝
            //.............................................................................用作凸结构的线的对边的树形结构，共有三角面数个分枝
     
            DataTree<Curve> BulgeCurves = new DataTree<Curve>();     //输出只包含凸结构的树形结构，共有三角面数个分枝
            DataTree<Curve> BulgeCurvesHalfEdges = new DataTree<Curve>();     //输出只凸结构的线的对边的树形结构DataTree
            DataTree<Curve> BulgeCurvesOrigin = new DataTree<Curve>();     //输出只包含边界的树形结构，共有三角面数个分枝
            DataTree<Curve> AllBulgeCurvesInOneFace = new DataTree<Curve>();     //输出只包含凸结构的面的边的树形结构，共有三角面数个分枝
            DataTree<Curve> AllBulgeCurvesInOneFaceWithBoundary = new DataTree<Curve>();     //输出只包含凸结构的面的边和边界的树形结构，共有三角面数个分枝


            for (int i = 0; i < OriginalOffsetCurves.Branches.Count; i++)   //对于每个面
            {
                for (int j = 0; j < OriginalOffsetCurves.Branches[i].Count; j++)   //对于每个树枝
                {                

                    for (int k = 0; k < InternalCurves.Branches.Count; k++)
                    {
                        Curve curve1 = OriginalOffsetCurves.Branches[i][j];

                        if (InternalCurves.Branches[k][0] == curve1)
                        {
                            GH_Path subPath = new GH_Path(i);

                            BulgeCurves.Add(curve1, subPath);
                            BulgeCurvesHalfEdges.Add(InternalCurves.Branches[k][1],subPath);
                            BulgeCurvesOrigin.Add(OriginalCurves.Branches[i][j], subPath);
                        }                     
                    }
                }
            }

            DataTree<Curve> AllBulgeCurvesInOneFaceWithBoundary1 =  new DataTree<Curve>();   //复制一份

            for (int i = 0; i<BulgeCurves.Branches.Count;i++)
            {
                GH_Path subPath = BulgeCurves.Paths[i];
                if (OriginalOffsetCurves.Branch(subPath).Count == BulgeCurves.Branch(subPath).Count)
                {
                    AllBulgeCurvesInOneFace.AddRange(BulgeCurves.Branch(subPath), subPath);
                }

                AllBulgeCurvesInOneFaceWithBoundary1.AddRange(BulgeCurves.Branch(subPath), subPath);
            }
      
            AllBulgeCurvesInOneFaceWithBoundary1.MergeTree(BoundaryCurves);

            for (int i = 0; i < AllBulgeCurvesInOneFaceWithBoundary1.Branches.Count; i++)
            {
                GH_Path subPath = AllBulgeCurvesInOneFaceWithBoundary1.Paths[i];
                if ((OriginalOffsetCurves.Branch(subPath).Count == AllBulgeCurvesInOneFaceWithBoundary1.Branch(subPath).Count) && (OriginalOffsetCurves.Branch(subPath).Count != BulgeCurves.Branch(subPath).Count))
                {
                    AllBulgeCurvesInOneFaceWithBoundary.AddRange(BulgeCurves.Branch(subPath), subPath);
                }

            }

            //.............................................................................用作凹结构的线的树形结构，共有三角面数个分枝

            DataTree <Curve> ConcaveCurves = new DataTree<Curve>();     //输出只包含边界的树形结构，共有三角面数个分枝
            DataTree<Curve> ConcaveCurvesHalfEdges = new DataTree<Curve>();     //输出只包含边界的树形结构，共有三角面数个分枝
            DataTree<Curve> ConcaveCurvesOrigin = new DataTree<Curve>();     //输出只包含边界的树形结构，共有三角面数个分枝


            for (int i = 0; i < OriginalOffsetCurves.Branches.Count; i++)   //对于每个面
            {
                for (int j = 0; j < OriginalOffsetCurves.Branches[i].Count; j++)   //对于每个树枝
                {
                    for (int k = 0; k < InternalCurves.Branches.Count; k++)
                    {
                        Curve curve1 = OriginalOffsetCurves.Branches[i][j];

                        if (InternalCurves.Branches[k][1] == curve1)
                        {
                            GH_Path subPath = new GH_Path(i);

                            ConcaveCurves.Add(curve1, subPath);
                            ConcaveCurvesHalfEdges.Add(InternalCurves.Branches[k][0], subPath);
                            ConcaveCurvesOrigin.Add(OriginalCurves.Branches[i][j], subPath);
                        }
                    }
                }
            }



            //.............................................................................输出

            DA.SetDataTree(0, InternalCurves);
            DA.SetDataTree(1, BoundaryCurves);
            DA.SetDataTree(2, BulgeCurves);
            DA.SetDataTree(3, ConcaveCurves);
            DA.SetDataTree(4, BulgeCurvesHalfEdges);
            DA.SetDataTree(5, ConcaveCurvesHalfEdges);
            DA.SetDataTree(6, BulgeCurvesOrigin);
            DA.SetDataTree(7, ConcaveCurvesOrigin);
            DA.SetDataTree(8, AllBulgeCurvesInOneFace);
            DA.SetDataTree(9, AllBulgeCurvesInOneFaceWithBoundary);

        }

        private Point3d GetMidPoint(Curve curve)
        {
            Point3d mid = new Point3d();

            Point3d start = curve.PointAtStart;
            Point3d end = curve.PointAtEnd;

            mid = 0.5 * (start+end);

            return mid;
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
            get { return new Guid("E31F3AAD-3960-4EC5-868E-5535B7E2E103"); }
        }
    }
}