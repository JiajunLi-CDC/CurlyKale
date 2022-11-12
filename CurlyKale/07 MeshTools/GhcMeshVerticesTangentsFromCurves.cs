using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurlyKale._04_MeshTools
{
    public class GhcMeshVerticesTangentsFromCurves : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GhcMeshVerticesTangentsFromCurves()
         : base("MeshVerticesTangentsFromCurves",
                "VerticesTangents",
                "用于计算干扰曲线下的网格顶点的切线方向",
                "CurlyKale",
                "07 MeshTools")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("OriginalMesh", "OriMesh", "用于计算的网格", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curves", "Curves", "干扰曲线", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("Tangents", "Tangents", "顶点沿干扰曲线的切线方向.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distance", "Distance", "顶点距离干扰曲线的最近距离", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iCurves = new List<Curve>();   //原始曲线
            Mesh iOriginalMesh = null;

            if (!DA.GetData("OriginalMesh", ref iOriginalMesh)) return;
            if (!DA.GetDataList("Curves", iCurves)) return;

            //...............................................................................初始化数据

            int size = iOriginalMesh.Vertices.Count;
            Vector3d[] tangents = new Vector3d[size];
            double[] distances = new double[size];
            for (int i = 0; i < size; ++i) tangents[i] = new Vector3d();


            System.Threading.Tasks.Parallel.For(0, size, i => getTangent(i, iOriginalMesh, iCurves, ref tangents, ref distances));

            DA.SetDataList(0, tangents.ToList());
            DA.SetDataList(1, distances.ToList());
        }

        public void getTangent(int i, Mesh _Mesh, List<Curve> _Curves, ref Vector3d[] tangents, ref double[] distances)
        {
            double distTampon = Double.MaxValue;
            double dist;
            double t;
            Vector3d vect = new Vector3d(1, 0, 0);

            if (_Curves[0].ClosestPoint(_Mesh.Vertices[i], out t))
            {
                distTampon = new Point3d(_Mesh.Vertices[i]).DistanceTo(_Curves[0].PointAt(t));
                vect = _Curves[0].TangentAt(t);
            }
            for (int j = 1; j < _Curves.Count; j++)
            {
                if (_Curves[j].ClosestPoint(_Mesh.Vertices[i], out t))
                {
                    dist = new Point3d(_Mesh.Vertices[i]).DistanceTo(_Curves[j].PointAt(t));
                    if (dist < distTampon)
                    {
                        distTampon = dist;
                        vect = _Curves[j].TangentAt(t);
                    }
                }
            }

            //重新计算方向向量
            tangents[i] = Vector3d.CrossProduct(Vector3d.CrossProduct(vect, new Vector3d(_Mesh.Normals[i])), new Vector3d(_Mesh.Normals[i]));
            //如果方向向量为0，则将其x赋值为1
            if (tangents[i].Length == 0.0) tangents[i].X = 1.0;
            distances[i] = distTampon;

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
                return Properties.Resources.CaculateMeshTangentsOnVerticesFromCurvesIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("5abaf65d-9cad-4c74-b892-bd0a8c092c4f");
            }
        }
    }
}