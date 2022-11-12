using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class DifferntialOnMeshAttract : GH_Component
    {
        private DifferentialGrowthSystem myDifferentialGrowthSystem;
        public DifferntialOnMeshAttract()
        : base("DifferentialLineOnMeshAttract", "DFLMeshAttract",
             "用于任意线段在给定网格上的差分生长,添加了基于点的生长干扰，目前干扰距离按照两点之间直线距离计算，有待使用测地线距离优化",
             "CurlyKale", "01 Laplacian Growth")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("StartCurves", "SCurves", "初始曲线", GH_ParamAccess.list);
            pManager.AddMeshParameter("BaseMesh", "BMesh", "生长基准网格", GH_ParamAccess.item);
            pManager.AddPointParameter("AttractPoints", "APoints", "吸引点", GH_ParamAccess.list);
            pManager.AddNumberParameter("AttractRadius", "ARadius", "每个点吸引范围半径", GH_ParamAccess.item);
            pManager.AddIntegerParameter("MaxPointsCount", "MCount", "最大节点数", GH_ParamAccess.item);
            pManager.AddCurveParameter("Boundaries", "Boundaries", "边界控制", GH_ParamAccess.list);
            pManager.AddIntegerParameter("BoundaryDistance", "BDistance", "边界控制距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MinCollisionDistance", "MinCDistance", "最小节点碰撞距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaxCollisionDistance", "MaxCDistance", "最大节点碰撞距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MinDivideLength", "MinDLength", "最小节点分裂距离,应小于最小碰撞距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaxDivideLength", "MaxDLength", "最大节点分裂距离,应小于最大碰撞距离", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CollisionWeight", "CWeight", "碰撞权重", GH_ParamAccess.item);
            pManager.AddIntegerParameter("BendingWeight", "BWeight", "角度平滑权重", GH_ParamAccess.item);
            pManager.AddIntegerParameter("BoundaryWeight", "BYWeight", "边界控制权重", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ifUseBoundary", "ifUseBoundary", "ifUseBoundary", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("ifGrow", "ifGrow", "ifGrow", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("ifReset", "ifRest", "ifRest", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Centers", "Centers", "最终所有节点", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Polylines", "Polylines", "最终节点连线", GH_ParamAccess.list);
            pManager.AddNumberParameter("CollisionDistanceNow", "CollisionDistanceNow", "当前碰撞距离", GH_ParamAccess.tree);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iStartCurves = new List<Curve>();
            List<Point3d> iAttractPoints = new List<Point3d>();
            List<Curve> iBoundaries = new List<Curve>();    //边界List

            double iAttractRadius = 0;
            int iMaxPointsCount = 0;
            Mesh iBaseMesh = null;
            double iMinDivideLength = 0;
            double iMaxDivideLength = 0;
            double iMinCollisionDistance = 0;
            double iMaxCollisionDistance = 0;
            int iCollisionWeight = 0;
            int iBendingWeight = 0;
            int iBoundaryWeight = 0;
            int iBoundaryDistance = 0;
            bool ifUseBoundary = true;
            bool ifReset = true;
            bool ifGrow = true;


            if (!DA.GetDataList("StartCurves", iStartCurves)) return;
            if (!DA.GetData("BaseMesh", ref iBaseMesh)) return;
            if (!DA.GetDataList("AttractPoints", iAttractPoints)) return;
            if (!DA.GetData("AttractRadius", ref iAttractRadius)) return;
            if (!DA.GetData("BaseMesh", ref iBaseMesh)) return;
            if (!DA.GetData("MaxPointsCount", ref iMaxPointsCount)) return;
            if (!DA.GetDataList("Boundaries", iBoundaries)) return;
            if (!DA.GetData("BoundaryDistance", ref iBoundaryDistance)) return;
            if (!DA.GetData("MinCollisionDistance", ref iMinCollisionDistance)) return;
            if (!DA.GetData("MaxCollisionDistance", ref iMaxCollisionDistance)) return;
            if (!DA.GetData("MinDivideLength", ref iMinDivideLength)) return;
            if (!DA.GetData("MaxDivideLength", ref iMaxDivideLength)) return;
            if (!DA.GetData("CollisionWeight", ref iCollisionWeight)) return;
            if (!DA.GetData("BendingWeight", ref iBendingWeight)) return;
            if (!DA.GetData("BoundaryWeight", ref iBoundaryWeight)) return;
            if (!DA.GetData("ifUseBoundary", ref ifUseBoundary)) return;
            if (!DA.GetData("ifGrow", ref ifGrow)) return;
            if (!DA.GetData("ifReset", ref ifReset)) return;



            // ==================================================================================================
            // 获取数据



            if (ifReset || myDifferentialGrowthSystem == null)
            {
                myDifferentialGrowthSystem = new DifferentialGrowthSystem(iStartCurves);
            }

            myDifferentialGrowthSystem.AttractPoints = iAttractPoints;
            myDifferentialGrowthSystem.AttractRadius = iAttractRadius;
            myDifferentialGrowthSystem.BaseMesh = iBaseMesh;
            myDifferentialGrowthSystem.MaxPointsCount = iMaxPointsCount;
            myDifferentialGrowthSystem.Boundaries = iBoundaries;
            myDifferentialGrowthSystem.BoundaryDistance = iBoundaryDistance;
            myDifferentialGrowthSystem.MinCollisionDistance = iMinCollisionDistance;
            myDifferentialGrowthSystem.MaxCollisionDistance = iMaxCollisionDistance;
            myDifferentialGrowthSystem.MinDivideLength = iMinDivideLength;
            myDifferentialGrowthSystem.MaxDivideLength = iMaxDivideLength;
            myDifferentialGrowthSystem.CollisionWeight = iCollisionWeight;
            myDifferentialGrowthSystem.BendingWeight = iBendingWeight;
            myDifferentialGrowthSystem.BoundaryWeight = iBoundaryWeight;
            myDifferentialGrowthSystem.ifUseBoundary = ifUseBoundary;
            myDifferentialGrowthSystem.ifGrow = ifGrow;
            myDifferentialGrowthSystem.ifReset = ifReset;


            myDifferentialGrowthSystem.updateWithMeshAttract();




            //=============================================================================================

            DA.SetDataTree(0, myDifferentialGrowthSystem.Getcenters());
            DA.SetDataList(1, myDifferentialGrowthSystem.GetOutPolylines());
            DA.SetDataTree(2, myDifferentialGrowthSystem.GetcollisionDistanceNow());

        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
              
                return Properties.Resources.DifferentialLineOnMeshAttractIcon; ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("c9fe9499-2459-42a5-a6ba-ea1a69b60956");
            }
        }
    }
}