using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class GhcInsertPointWithData : GH_Component
    {

        public GhcInsertPointWithData()
          : base("InsertPointWithData",
                "InsertPoints",
                "用于增加最终节点的密度和其对应的半径数值，便于后续网格处理的精度",
                "CurlyKale",
                "07 MeshTools")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Centers", "Centers", "最终所有节点", GH_ParamAccess.tree);
            pManager.AddCurveParameter("StartCurves", "SCurves", "初始曲线", GH_ParamAccess.list);           
            pManager.AddNumberParameter("CollisionDistanceNow", "CollisionDistanceNow", "当前碰撞距离", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Iteration", "Iteration", "迭代次数", GH_ParamAccess.item, 1);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Centers", "Centers", "最终所有节点", GH_ParamAccess.tree);
            pManager.AddNumberParameter("CollisionDistanceNow", "CollisionDistanceNow", "当前碰撞距离", GH_ParamAccess.tree);

        }


        


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iOriginCurves = new List<Curve>();   //原始曲线
            GH_Structure<GH_Point> iStartCenters =new GH_Structure<GH_Point>();
            GH_Structure<GH_Number> iCollisionDistanceNow = new GH_Structure<GH_Number>();
            int iIteration = 0;

            if (!DA.GetDataTree("Centers", out iStartCenters)) return;
            if (!DA.GetDataList("StartCurves", iOriginCurves)) return;          
            if (!DA.GetDataTree("CollisionDistanceNow", out iCollisionDistanceNow)) return;
            if (!DA.GetData("Iteration", ref iIteration)) return;

            //...............................................................................初始化数据


            DataTree<Point3d> centers;     //节点DataTree
            DataTree<double> CollisionDistanceNow;     //节点DataTree

            centers = new DataTree<Point3d>();
            CollisionDistanceNow = new DataTree<double>();
           

            for (int i = 0; i < iOriginCurves.Count; i++)  //对于每条初始线
            {
                List<Point3d> subtree_cen = new List<Point3d>();
                List<double> subtree_col = new List<double>();

                for (int j = 0; j < iStartCenters[i].Count; j++)
                {
                    GH_Point ghp = iStartCenters[i][j];
                    GH_Number num = iCollisionDistanceNow[i][j];

                    subtree_cen.Add(ghp.Value);
                    subtree_col.Add(num.Value);
                }

                GH_Path subPath = new GH_Path(i);
                centers.AddRange(subtree_cen, subPath);
                CollisionDistanceNow.AddRange(subtree_col, subPath);
            }



            //...............................................................................插入新的节点和数据

            for (int k = 0; k < iIteration; k++)
            {
                for (int i = 0; i < iOriginCurves.Count; i++)  //对于每条初始线
                {
                    List<Point3d> subtree_cen = new List<Point3d>();
                    List<double> subtree_col = new List<double>();

                    for (int j = 0; j < centers.Branch(i).Count; j++)
                    {
                        if (j == centers.Branch(i).Count - 1)
                        {
                            if (!iOriginCurves[i].IsClosed)    //非闭合曲线
                            {
                                subtree_cen.Add(centers.Branch(i)[j]);
                                subtree_col.Add(CollisionDistanceNow.Branch(i)[j]);
                            }
                            else   //曲线闭合
                            {
                                subtree_cen.Add(centers.Branch(i)[j]);    //加入下一个点
                                subtree_col.Add(CollisionDistanceNow.Branch(i)[j]);  //加入下一个数据

                                Point3d newCenter = 0.5 * (centers.Branch(i)[j] + centers.Branch(i)[0]);
                                double newCol = 0.5 * (CollisionDistanceNow.Branch(i)[j] + CollisionDistanceNow.Branch(i)[0]);

                                subtree_cen.Insert(subtree_cen.Count, newCenter);   //在末尾插入新的点
                                subtree_col.Insert(subtree_col.Count, newCol);   //在末尾插入新的数据
                            }
                        }
                        else
                        {
                            subtree_cen.Add(centers.Branch(i)[j]);    //加入下一个点
                            subtree_col.Add(CollisionDistanceNow.Branch(i)[j]);  //加入下一个数据

                            Point3d newCenter = 0.5 * (centers.Branch(i)[j] + centers.Branch(i)[j + 1]);
                            double newCol = 0.5 * (CollisionDistanceNow.Branch(i)[j] + CollisionDistanceNow.Branch(i)[j + 1]);

                            subtree_cen.Insert(subtree_cen.Count, newCenter);   //在末尾插入新的点
                            subtree_col.Insert(subtree_col.Count, newCol);   //在末尾插入新的数据
                        }
                    }



                    GH_Path subPath = new GH_Path(i);
                    centers.Branch(i).Clear();
                    CollisionDistanceNow.Branch(i).Clear();

                    centers.AddRange(subtree_cen, subPath);
                    CollisionDistanceNow.AddRange(subtree_col, subPath);

                }
            }

            //...........................................................................输出数据

            DA.SetDataTree(0, centers);
            DA.SetDataTree(1, CollisionDistanceNow);
        }

  
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.InsertPointsWithDataIcon; ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("c5cf4951-b324-41b7-95ff-960b72ae4cdb");
            }
        }
    }
}