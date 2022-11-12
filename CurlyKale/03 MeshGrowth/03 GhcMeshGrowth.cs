using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using PlanktonGh;
using Rhino.Geometry;

namespace CurlyKale
{
    public class GhcMeshGrowth : GH_Component
    {

        private MeshGrowthSystem myMeshGrowthSystem;
        public GhcMeshGrowth()
          : base("MeshGrowth",
                "MeshGrowth",
                "三角网格的差分生长",
                "CurlyKale",
                "03 MeshGrowth")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "重置生长", GH_ParamAccess.item);
            pManager.AddMeshParameter("Starting Mesh", "StartingMesh", "初始网格", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "控制生长开关", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Max. Vertex Count", "Max. Vertex Count", "最大顶点数量", GH_ParamAccess.item);
            pManager.AddNumberParameter("Collision Distance", "Collision Distance", "节点之间控制的碰撞距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MinSplitLength", "MinSplitLength", "边缘最小分裂距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("Edge Length Constraint Weight", "Edge Length Constraint Weight", "边长收缩力权重", GH_ParamAccess.item);
            pManager.AddNumberParameter("Collision Weight", "Collision Weight", "碰撞推力权重", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bending Resistance Weight", "Bending Resistance Weight", "抗弯力权重", GH_ParamAccess.item);
            pManager.AddNumberParameter("Boundary Angle Weight", "Boundary Angle Weight", "边缘角度权重", GH_ParamAccess.item);
            
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "最终的网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "Distance", "测地线距离", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iReset = true;
            Mesh iStartingMesh = null;
            bool iGrow = false;
            int iMaxVertexCount = 0;
            double iEdgeLengthConstrainWeight = 0.0;
            double iCollisionDistance = 0.0;
            double iMinSplitLength = 0.0;
            double iCollisionWeight = 0.0;
            double iBendingResistanceWeight = 0.0;
            double iBoundaryAngleWeight = 0.0;

            DA.GetData("Reset", ref iReset);
            DA.GetData("Starting Mesh", ref iStartingMesh);
            DA.GetData("Grow", ref iGrow);
            DA.GetData("Max. Vertex Count", ref iMaxVertexCount);
            DA.GetData("Edge Length Constraint Weight", ref iEdgeLengthConstrainWeight);
            DA.GetData("Collision Distance", ref iCollisionDistance);
            DA.GetData("MinSplitLength", ref iMinSplitLength);
            DA.GetData("Collision Weight", ref iCollisionWeight);
            DA.GetData("Bending Resistance Weight", ref iBendingResistanceWeight);
            DA.GetData("Boundary Angle Weight", ref iBoundaryAngleWeight);



            //=============================================================================================

            if (iReset || myMeshGrowthSystem == null)
                myMeshGrowthSystem = new MeshGrowthSystem(iStartingMesh);

            myMeshGrowthSystem.Grow = iGrow;
            myMeshGrowthSystem.MaxVertexCount = iMaxVertexCount;
            myMeshGrowthSystem.EdgeLengthConstraintWeight = iEdgeLengthConstrainWeight;
            myMeshGrowthSystem.CollisionDistance = iCollisionDistance;
            myMeshGrowthSystem.minLength = iMinSplitLength;
            myMeshGrowthSystem.CollisionWeight = iCollisionWeight;
            myMeshGrowthSystem.BendingResistanceWeight = iBendingResistanceWeight;
            myMeshGrowthSystem.BoundaryAngleWeight = iBoundaryAngleWeight;


            myMeshGrowthSystem.Update();

            DA.SetData("Mesh", myMeshGrowthSystem.GetRhinoMesh());
            DA.SetDataList("Distance", myMeshGrowthSystem.GetGeodesicD());
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.MeshGrowthIcon; ;
            }
        }


        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("cd55d0de-3d3b-4d83-ae1b-3aa8fe0ce698");
            }
        }
    }
}