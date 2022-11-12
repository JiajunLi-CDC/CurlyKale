using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class GhcAverage : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcAverage class.
        /// </summary>
        public GhcAverage()
          : base("Average", "Average",
              "求取平均值",
              "CurlyKale", "Method")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ifReset", "ifRest", "ifRest", GH_ParamAccess.item);
            pManager.AddVectorParameter("Velocity", "Velocity", "Velocity", GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Particle", "Particle", "Particle", GH_ParamAccess.item);
        }

        Point3d currentPosition;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool ifReset = false;
            Vector3d v = new Vector3d(0, 0, 0);

            bool success1 = DA.GetData("ifReset", ref ifReset);
            bool success2 = DA.GetData("Velocity", ref v);

            if (success1 && success2)
            {
                if (ifReset)
                    currentPosition = new Point3d(0, 0, 0);
                currentPosition += v;
                DA.SetData("Particle", currentPosition);
            }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.DifferentialLineIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("233ae533-13ac-42b6-9f87-0f5369752663"); }
        }
    }
}