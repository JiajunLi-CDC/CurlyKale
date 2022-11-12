using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;


/*
 * Write by Jiajun Li
 * 差分生长2D，支持多线段同时生长，支持多边界控制, 引入RTree优化
 * 修改于2022.06.26
 */

namespace CurlyKale
{
    public class GhcDifferentialLineWithBoundary : GH_Component
    {
        private DifferentialGrowthSystem myDifferentialGrowthSystem;
        public GhcDifferentialLineWithBoundary()
         : base("DifferentialLineWithBoundary", "DFWB",
             "用于任意线段的差分生长,添加了边界控制",
             "CurlyKale", "01 Laplacian Growth")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("StartCurves", "SCurves", "初始曲线", GH_ParamAccess.list);
            pManager.AddIntegerParameter("MaxPointsCount", "MCount", "最大节点数", GH_ParamAccess.item);
            pManager.AddCurveParameter("Boundaries", "Boundaries", "边界控制", GH_ParamAccess.list);
            pManager.AddIntegerParameter("BoundaryDistance", "BDistance", "边界控制距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionDistance", "CDistance", "节点碰撞距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("DivideLength", "DLength", "节点分裂距离,应小于碰撞距离", GH_ParamAccess.item);
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
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iStartCurves = new List<Curve>();
            List<Curve> iBoundaries = new List<Curve>();    //边界List

            int iMaxPointsCount = 0;
            double iDivideLength = 0;
            double iCollisionDistance = 0;
            int iCollisionWeight = 0;
            int iBendingWeight = 0;
            int iBoundaryWeight = 0;
            int iBoundaryDistance = 0;
            bool ifUseBoundary = true;
            bool ifReset = true;
            bool ifGrow = true;


            if (!DA.GetDataList("StartCurves", iStartCurves)) return;
            if (!DA.GetData("MaxPointsCount", ref iMaxPointsCount)) return;
            if (!DA.GetDataList("Boundaries", iBoundaries)) return;
            if (!DA.GetData("BoundaryDistance", ref iBoundaryDistance)) return;
            if (!DA.GetData("CollisionDistance", ref iCollisionDistance)) return;
            if (!DA.GetData("DivideLength", ref iDivideLength)) return;
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

            myDifferentialGrowthSystem.MaxPointsCount = iMaxPointsCount;
            myDifferentialGrowthSystem.Boundaries = iBoundaries;
            myDifferentialGrowthSystem.BoundaryDistance = iBoundaryDistance;
            myDifferentialGrowthSystem.CollisionDistance = iCollisionDistance;
            myDifferentialGrowthSystem.DivideLength = iDivideLength;
            myDifferentialGrowthSystem.CollisionWeight = iCollisionWeight;
            myDifferentialGrowthSystem.BendingWeight = iBendingWeight;
            myDifferentialGrowthSystem.BoundaryWeight = iBoundaryWeight;
            myDifferentialGrowthSystem.ifUseBoundary = ifUseBoundary;
            myDifferentialGrowthSystem.ifGrow = ifGrow;
            myDifferentialGrowthSystem.ifReset = ifReset;

            
            myDifferentialGrowthSystem.updateWithOrWithoutBoundary();

            


            //=============================================================================================

            DA.SetDataTree(0, myDifferentialGrowthSystem.Getcenters());
            DA.SetDataList(1, myDifferentialGrowthSystem.GetOutPolylines());

        }




        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Properties.Resources.DifferentialLineIcon; ;
            }
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("233ae533-13ac-42b6-9f87-0f5369752563");
            }
        }
    }
}