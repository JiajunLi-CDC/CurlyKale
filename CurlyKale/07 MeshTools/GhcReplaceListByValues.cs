using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._04_MeshTools
{
    public class GhcReplaceListByValues : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcReplaceListByValues class.
        /// </summary>
        public GhcReplaceListByValues()
          : base("ReplaceListByValues",
                "ReplaceListByValues",
                "两组数据长度相同，根据A数组的某个阈值的情况，来对B数组中的值进行更改替换",
                "CurlyKale",
                "07 MeshTools")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("ListA", "A", "进行判断的数组.", GH_ParamAccess.list);
            pManager.AddNumberParameter("ListB", "B", "进行替换更改的数组.", GH_ParamAccess.list);
            pManager.AddNumberParameter("ValueA", "VA", "对A进行判断的值.", GH_ParamAccess.item, 0d);
            pManager.AddNumberParameter("ValueB", "VB", "对B进行替换更改的值.", GH_ParamAccess.item, 0d);
            pManager.AddNumberParameter("Type", "Type", "对A进行判断的情况，0为 <valueA ,1为 >valueA .", GH_ParamAccess.item, 0d);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("OutListB", "B", "进行替换更改的数组.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> iListA = new List<double>();
            List<double> iListB = new List<double>();
            double iValueA = 0;
            double iValueB = 0;
            double iType = 0;

            
            if (!DA.GetDataList("ListA", iListA)) return;
            if (!DA.GetDataList("ListB", iListB)) return;
            if (!DA.GetData("ValueA", ref iValueA)) return;
            if (!DA.GetData("ValueB", ref iValueB)) return;
            if (!DA.GetData("Type", ref iType)) return;


            if (iType == 0)
            {
                for (int i = 0; i < iListB.Count; i++)
                {
                    if (iListA[i] < iValueA)
                    {
                        iListB[i] = iValueB;
                    }
                }
            }

            if (iType == 1)
            {
                for (int i = 0; i < iListB.Count; i++)
                {
                    if (iListA[i] > iValueA)
                    {
                        iListB[i] = iValueB;
                    }
                }
            }


            DA.SetDataList(0, iListB);
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
                return Properties.Resources.ReplaceListByValuesIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("e30353e9-9419-4b05-9604-e56ddf24c6eb");
            }
        }
    }
}