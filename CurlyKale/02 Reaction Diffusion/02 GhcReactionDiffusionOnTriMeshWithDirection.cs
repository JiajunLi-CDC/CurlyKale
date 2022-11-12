using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._02_Reaction_Diffusion
{
    public class GhcReactionDiffusionOnTriMeshWithDirection : GH_Component
    {
        private ReactionDiffusionOnMeshSystem reaction;

        public GhcReactionDiffusionOnTriMeshWithDirection()
          : base("GhcReactionDiffusionOnTriMeshWithDirection", "ReactionMesh",
              "用于基于三角网格的反应扩散算法,增加了控制曲线控制反应纹理方向",
              "CurlyKale", "02 Reaction Diffusion")
        {       
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("OriginalMesh", "OriMesh", "初始三角网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("Diffusion Rate A", "dA", "A扩散率，一维数组.", GH_ParamAccess.list, 1d);
            pManager.AddNumberParameter("Diffusion Rate B", "dB", "B扩散率，一维数组.", GH_ParamAccess.list, 0.3d);
            pManager.AddNumberParameter("Feed Rate", "F", "给进B的速率，一维数组.", GH_ParamAccess.list, 0.055d);
            pManager.AddNumberParameter("Kill Rate", "K", "杀死A的速率，一维数组.", GH_ParamAccess.list, 0.062d);
            pManager.AddVectorParameter("Tangents", "Tangents", "顶点沿干扰曲线的切线方向.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Direction Factor", "DirFactor", "控制扩散角度参数，参数 > 1 图案将跟随切线，< 1 图案将垂直于切线.", GH_ParamAccess.item,1d);
            pManager.AddIntegerParameter("Iteration Count", "IC", "迭代次数", GH_ParamAccess.item, 100);
            pManager.AddBooleanParameter("Reset Simulation", "RES", "Clear and Reload all values.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Run Simulation", "RUN", "Run Simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Out_A", "A", "反应过后每个点的A值，0-1", GH_ParamAccess.list);
            pManager.AddNumberParameter("Out_B", "B", "反应过后每个点的B值，0-1", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh iOriginalMesh = null;
            bool reset = true;
            bool run = false;

            List<double> iDA = new List<double>();
            List<double> iDB = new List<double>();
            List<double> iF = new List<double>();
            List<double> iK = new List<double>();
            List<Vector3d> iTangents = new List<Vector3d>();
            double iDir_F = 0;
            double iDT = 1;   //deltatime反应步长一般取值为1

            int iterationCount = 0;

            if (!DA.GetData("OriginalMesh", ref iOriginalMesh)) return;
            if (!DA.GetDataList("Diffusion Rate A", iDA)) return;
            if (!DA.GetDataList("Diffusion Rate B", iDB)) return;
            if (!DA.GetDataList("Feed Rate", iF)) return;
            if (!DA.GetDataList("Kill Rate", iK)) return;
            if (!DA.GetDataList("Tangents", iTangents)) return;
            if (!DA.GetData("Direction Factor", ref iDir_F)) return;
            if (!DA.GetData("Iteration Count", ref iterationCount)) return;

            if (!DA.GetData("Reset Simulation", ref reset)) return;
            if (!DA.GetData("Run Simulation", ref run)) return;

            if (reset || iOriginalMesh == null)
            {
                reaction = new ReactionDiffusionOnMeshSystem(iOriginalMesh, iDA, iDB, iF, iK, iDT, iTangents, iDir_F);
            }

            if (run)
            {
                reaction.ReactionWithDirection(iterationCount);
            }




            DA.SetDataList(0, reaction.listA);
            DA.SetDataList(1, reaction.listB);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.ReactionWithDirectionIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("fc7eacb2-d75f-46cc-9b49-0af40d8369ac");
            }
        }
    }
}