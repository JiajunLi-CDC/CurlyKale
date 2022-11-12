using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class GhcAdaptiveSubDivision : GH_Component
    {
        private MeshGrowthSystem myMeshGrowthSystem;
        public GhcAdaptiveSubDivision()
        : base("AdaptiveSubDivision",
                "AdaptiveSubD",
                "影响因子控制下的三角网格细分，引入边缘翻转",
                "CurlyKale",
                "07 MeshTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "重置生长", GH_ParamAccess.item);
            pManager.AddMeshParameter("Starting Mesh", "StartingMesh", "初始网格", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "控制生长开关", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Max. Vertex Count", "Max. Vertex Count", "最大顶点数量", GH_ParamAccess.item);
            pManager.AddNumberParameter("Collision Distance", "Collision Distance", "最大分裂距离", GH_ParamAccess.item);
            pManager.AddNumberParameter("MinSplitLength", "MinSplitLength", "边缘最小分裂距离", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "最终的网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "Distance", "测地线距离", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iReset = true;
            Mesh iStartingMesh = null;
            bool iGrow = false;
            int iMaxVertexCount = 0;
            double iCollisionDistance = 0.0;
            double iMinSplitLength = 0.0;

            DA.GetData("Reset", ref iReset);
            DA.GetData("Starting Mesh", ref iStartingMesh);
            DA.GetData("Grow", ref iGrow);
            DA.GetData("Max. Vertex Count", ref iMaxVertexCount);
            DA.GetData("Collision Distance", ref iCollisionDistance);
            DA.GetData("MinSplitLength", ref iMinSplitLength);


            //=============================================================================================

            if (iReset || myMeshGrowthSystem == null)
            myMeshGrowthSystem = new MeshGrowthSystem(iStartingMesh);

            myMeshGrowthSystem.Grow = iGrow;
            myMeshGrowthSystem.MaxVertexCount = iMaxVertexCount;
            myMeshGrowthSystem.CollisionDistance = iCollisionDistance;
            myMeshGrowthSystem.minLength = iMinSplitLength;


            Update();

            DA.SetData("Mesh", myMeshGrowthSystem.GetRhinoMesh());
            DA.SetDataList("Distance", myMeshGrowthSystem.GetGeodesicD());
        }

        private void Update()
        {
           if( myMeshGrowthSystem.frameCount == 0){
                myMeshGrowthSystem.GetGeodesic();
            }
            
            myMeshGrowthSystem.SplitAllLongEdges();
            myMeshGrowthSystem.flipEdgeControl();
            myMeshGrowthSystem.GetGeodesic();
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.MeshGrowthIcon; ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("dd85086b-f30a-4a8d-a16e-98097b8dec86");
            }
        }
    }
}