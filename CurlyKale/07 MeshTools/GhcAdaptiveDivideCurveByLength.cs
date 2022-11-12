using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale._05_MeshTools
{
    public class GhcAdaptiveDivideCurveByLength : GH_Component
    {

        public GhcAdaptiveDivideCurveByLength()
         : base("AdaptiveDivideCurveByLength",
                "AdaptiveDivideCurveByLength",
                "用于按长度均分曲线点",
                "CurlyKale",
                "07 MeshTools")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("OriginCurves", "OCurves", "生长后曲线的树形结构，对接ReplaceCurveTreeAfterDeleteDuplicateCurves电池的输出NewCurves", GH_ParamAccess.tree);
            pManager.AddNumberParameter("DivideLength", "DivideLength", "分割曲线的长度数值", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "Tolerance", "少于divideLength多少比例时合并最后一个点和尾点,一般取值0-1", GH_ParamAccess.item,0f);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "分割后的点", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> iOriginCurves = new GH_Structure<GH_Curve>();  //原始曲线
            double iDivideLength = 0;
            double iTolerance = 0;


            if (!DA.GetDataTree("OriginCurves", out iOriginCurves)) return;
            if (!DA.GetData("DivideLength", ref iDivideLength)) return;
            if (!DA.GetData("Tolerance", ref iTolerance)) return;


            //...............................................................................初始化数据

            DataTree<Point3d> outPoints;     //节点DataTree
            outPoints = new DataTree<Point3d>();

            for (int i = 0; i < iOriginCurves.Branches.Count; i++)   //对于每个树枝
            {
                for (int j = 0; j < iOriginCurves.Branches[i].Count; j++)   //对于每个树枝(每根曲线)
                {
                    Curve curve1 = iOriginCurves.Branches[i][j].Value;
                  
                    Point3d[] points;
                    curve1.DivideByLength(iDivideLength, true, out points);   //细分每个线段,这里true添加了首尾点（不知道为啥添加不上尾点）

                    GH_Path subPath = new GH_Path(i, j);

                    List<Point3d> points_eachLine = new List<Point3d>();
                    if (points == null)
                    {
                        Point3d newP1 = curve1.PointAtStart;   //找到首尾点，线过短情况
                        Point3d newP2 = curve1.PointAtEnd;
                        points_eachLine.Add(newP1);
                        if (!curve1.IsClosed)
                        {
                            points_eachLine.Add(newP2);
                        }
                      
                    }
                    else
                    {
                        foreach (Point3d point in points)    //对于每个初始线的点
                        {
                            points_eachLine.Add(point);                       
                        }
                        Point3d newP2 = curve1.PointAtEnd;

                        if (!curve1.IsClosed)
                        {
                            points_eachLine.Add(newP2);
                        }
                    }

                    if (points_eachLine.Count > 2)   //如果至少有首尾两个点和另外一个点
                    {
                        if (curve1.IsClosed)
                        {
                            double d = points_eachLine[points_eachLine.Count - 1].DistanceTo(points_eachLine[0]);  //倒数第二点和尾点距离
                            if (d < iDivideLength * iTolerance)
                            {
                                points_eachLine.RemoveAt(points_eachLine.Count - 1); //两点小于阈值范围则移除倒数第二点
                            }
                        }
                        else
                        {
                            double d = points_eachLine[points_eachLine.Count - 2].DistanceTo(points_eachLine[points_eachLine.Count - 1]);  //倒数第二点和尾点距离
                            if (d < iDivideLength * iTolerance)
                            {
                                points_eachLine.RemoveAt(points_eachLine.Count - 2); //两点小于阈值范围则移除倒数第二点
                            }
                        }
                       
                    }
                    

                    outPoints.AddRange(points_eachLine, subPath);
                }
            }
               

            //...............................................................................输出

            DA.SetDataTree(0, outPoints);
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
                return Properties.Resources.AdaptiveDivideCurveByLengthIcon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("e54d504e-9257-425f-bd67-23141d3b1269");
            }
        }
    }
}