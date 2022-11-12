using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class GhcDifferentialLineImage : GH_Component
    {
        private DifferentialGrowthSystem myDifferentialGrowthSystem;
        public GhcDifferentialLineImage()
         : base("DifferentialLineImage", "DFLI",
             "用于任意线段的差分生长,可以基于图片进行生长",
              "CurlyKale", "01 Laplacian Growth")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("StartCurves", "SCurves", "初始曲线", GH_ParamAccess.list);
            pManager.AddTextParameter("ImagePath", "ImagePath", "图片位置", GH_ParamAccess.item);
            pManager.AddIntegerParameter("MaxPointsCount", "MCount", "最大节点数", GH_ParamAccess.item);
            pManager.AddRectangleParameter("RectPosition", "RPosition", "矩形边界，用于图片定位,需要输入矩形", GH_ParamAccess.item);
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
            Rectangle3d iRectPosition = new Rectangle3d();    //图片边界List
            List<Curve> iBoundaries = new List<Curve>();    //边界List

            String iImagePath = null;
            int iMaxPointsCount = 0;
            double iMinDivideLength = 0;
            double iMaxDivideLength = 0;
            double iMinCollisionDistance = 0;
            double iMaxCollisionDistance = 0;
            int iCollisionWeight = 0;
            int iBendingWeight = 0;
            int iBoundaryDistance = 0;
            int iBoundaryWeight = 0;
            bool ifUseBoundary = true;
            bool ifReset = true;
            bool ifGrow = true;


            if (!DA.GetDataList("StartCurves", iStartCurves)) return;
            if (!DA.GetData("ImagePath", ref iImagePath)) return;
            if (!DA.GetData("MaxPointsCount", ref iMaxPointsCount)) return;
            if (!DA.GetData("RectPosition", ref iRectPosition)) return;
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

            myDifferentialGrowthSystem.originImage = iImagePath;
            myDifferentialGrowthSystem.MaxPointsCount = iMaxPointsCount;
            myDifferentialGrowthSystem.rectBoundary = iRectPosition;
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


            myDifferentialGrowthSystem.updateWithImage();




            //=============================================================================================

            DA.SetDataTree(0, myDifferentialGrowthSystem.Getcenters());
            DA.SetDataList(1, myDifferentialGrowthSystem.GetOutPolylines());
            DA.SetDataTree(2, myDifferentialGrowthSystem.GetcollisionDistanceNow());

        }



        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Properties.Resources.DifferentialLineImageIcon; ;
            }
        }
        

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("a42ee63a-7244-488b-be48-f96d4482d502");
            }
        }
    }
}