using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace CurlyKale
{
    public class GhcDifferentialCurve : GH_Component
    {

        public GhcDifferentialCurve()
          : base("DifferentialCurve", "DC",
              "用于闭合线段的弹性线增长",
             "CurlyKale", "01 Laplacian Growth")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("StartCurves", "SCurves", "初始曲线", GH_ParamAccess.list);
            pManager.AddIntegerParameter("PointCount", "PCount", "线段分裂点数", GH_ParamAccess.item);
            pManager.AddNumberParameter("DesireLength", "DirLength", "期望的线段长度", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionDistance", "CDistance", "节点碰撞距离", GH_ParamAccess.item);
            pManager.AddIntegerParameter("ExtendWeight", "EWeight", "线伸长权重", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CollisionWeight", "CWeight", "碰撞权重", GH_ParamAccess.item);
            pManager.AddIntegerParameter("BendingWeight", "BWeight", "角度平滑权重", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ifReset", "ifRest", "ifRest", GH_ParamAccess.item);
        }

        DataTree<Point3d> centers;     //节点DataTree
        List<Curve> originCurves;      //输入曲线
        List<Polyline> outPolylines;     //输出折线
        List<Vector3d> totalMoves;
        List<Vector3d> centerMoves;   //每次最终点移动量
        List<double> totalWeights;
        List<int> collisionCount;
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Centers", "Centers", "最终所有节点", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Polylines", "Polylines", "最终节点连线", GH_ParamAccess.list);
            pManager.AddPointParameter("CollisionPoints", "CPoints", "碰撞节点", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iStartCurves = new List<Curve>();
            double iDesireLength = 0;
            double iCollisionDistance = 0;
            int iExtendWeight = 0;
            int iPointCount = 0;
            int iCollisionWeight = 0;
            int iBendingWeight = 0;
            bool ifReset = false;


            DA.GetDataList("StartCurves", iStartCurves);
            DA.GetData("PointCount", ref iPointCount);
            DA.GetData("DesireLength", ref iDesireLength);
            DA.GetData("CollisionDistance", ref iCollisionDistance);
            DA.GetData("ExtendWeight", ref iExtendWeight);
            DA.GetData("CollisionWeight", ref iCollisionWeight);
            DA.GetData("BendingWeight", ref iBendingWeight);
            DA.GetData("ifReset", ref ifReset);


            // ==================================================================================================
            // 获取数据

            if (ifReset == true)
            {
                originCurves = new List<Curve>(iStartCurves);
                centers = new DataTree<Point3d>();  //重新获取节点的Tree

                for (int i = 0; i < originCurves.Count; i++)  //对于每条初始线
                {
                    List<Point3d> subtree = new List<Point3d>();

                    Point3d[] points;
                    originCurves[i].DivideByCount(iPointCount, true, out points);      //对初始曲线进行divide


                    foreach (Point3d point in points)    //对于每个初始线的点
                    {
                        subtree.Add(point);
                    }


                    GH_Path subPath = new GH_Path(i);
                    centers.AddRange(subtree, subPath);
                }
            }

            // ==================================================================================================
            // 运行求解器
            //cutInCircle(iMaxPointsCount, iDivideLength);

            List<Point3d> collisionPoints = new List<Point3d>();  //所有碰撞点

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                List<Point3d> centers_eachLine = new List<Point3d>();

                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_eachLine.Add(centers.Branch(i)[j]);   //每一条线的节点数组
                }

                if (originCurves[i].IsClosed)
                {
                    centers_eachLine.Add(centers_eachLine[0]);
                }

                Polyline polyline_each = new Polyline(centers_eachLine);
                PolylineCurve polylineCurve = polyline_each.ToPolylineCurve();

                Point3d[] points;               
                polylineCurve.DivideByLength(0.2*iCollisionDistance, true, out points);    //对每条运动后曲线进行divide

                if (points == null)
                {
                    collisionPoints.Add(originCurves[i].PointAtStart);
                    collisionPoints.Add(originCurves[i].PointAtEnd);
                }
                else
                {
                    foreach (Point3d point in points)    //对于每个初始线的点
                    {
                        collisionPoints.Add(point);
                    }
                    if (!originCurves[i].IsClosed)
                    {
                        collisionPoints.Add(originCurves[i].PointAtEnd);
                    }
                }

            }

            outPolylines = new List<Polyline>();
            for (int i = 0; i < originCurves.Count; i++)  //对于每条初始线
            {
                List<Point3d> centers_eachLine = new List<Point3d>();

                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_eachLine.Add(centers.Branch(i)[j]);   //每一条线的节点数组
                }

                totalMoves = new List<Vector3d>();
                centerMoves = new List<Vector3d>();
                totalWeights = new List<double>();
                collisionCount = new List<int>();

                for (int j = 0; j < centers_eachLine.Count; j++)
                {
                    totalMoves.Add(new Vector3d(0.0, 0.0, 0.0));
                    totalWeights.Add(0.0);
                    centerMoves.Add(new Vector3d(0.0, 0.0, 0.0));
                    collisionCount.Add(0);
                }

                push(centers_eachLine, collisionPoints, iCollisionDistance, iCollisionWeight);


                if (originCurves[i].IsClosed)
                {
                    makeAngleFlattenInCircle(centers_eachLine, iBendingWeight);
                    extendLineinCircle(centers_eachLine, iDesireLength, iExtendWeight);
                    updatePosition(centers_eachLine, i);
                }
                else
                {
                    makeAngleFlatten(centers_eachLine, iBendingWeight);
                    extendLine(centers_eachLine, iDesireLength, iExtendWeight);
                    updatePositionWithoutStartAndEnd(centers_eachLine, i);
                }
                //updatePosition(centers_eachLine, i);

                //画出每一条折线并装入输出数列
                
                if (originCurves[i].IsClosed)
                {
                    centers_eachLine.Add(centers_eachLine[0]);
                }

                Polyline polyline_each1 = new Polyline(centers_eachLine);
                outPolylines.Add(polyline_each1);
            }

            DA.SetDataTree(0, centers);
            DA.SetDataList(1, outPolylines);
            DA.SetDataList(2, collisionPoints);
        }

        private void push(List<Point3d> centers, List<Point3d> allCenters, double CollisionDistance, int CollisionWeight)
        {
            RTree rTree = new RTree();     //使用RTree
            List<int>[] collisionIndices = new List<int>[centers.Count];     //创建顶点个数个数组记录碰撞

            for (int i = 0; i < allCenters.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(allCenters[i], i);
            }

            for (int i = 0; i < centers.Count; i++)
            {
                collisionIndices[i] = new List<int>();
                rTree.Search(
                   new Sphere(centers[i], CollisionDistance*1.02),
                   (sender, args) => { collisionIndices[i].Add(args.Id); });     //记录每个点在碰撞范围内的点
            }

            for (int i = 0; i < collisionIndices.Length; i++)
            {
                foreach (int j in collisionIndices[i])
                {
                    double d = centers[i].DistanceTo(allCenters[j]);     //当前距离
                    if (d > CollisionDistance)
                    {
                        continue;
                    }
                    else
                    {
                        Vector3d move = centers[i] - allCenters[j];
                        if (move.Length < 0.001) continue;
                        move.Unitize();
                        move *= 0.5 * (CollisionDistance - d);   //每个点走一半距离
                        move *= (CollisionDistance - d) / CollisionDistance;

                        totalMoves[i] += move;
                        collisionCount[i] += 1;

                    }
                }
            }


            //每个小球移动量等于总移动量除以碰撞次数
            for (int i = 0; i < centers.Count; i++)
            {
                if (collisionCount[i] != 0.0)
                {
                    totalMoves[i] /= collisionCount[i];         //移动速度

                    centerMoves[i] += totalMoves[i] * CollisionWeight;
                    totalWeights[i] += CollisionWeight;   //增加权重写法
                }
            }
        }

        private void makeAngleFlattenInCircle(List<Point3d> centers, int BendingWeight)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                Vector3d m;
                Vector3d n;
                if (i == 0)
                {
                    m = centers[0] - centers[i + 1];
                    n = centers[0] - centers[centers.Count - 1];
                }
                else if (i == centers.Count - 1)
                {
                    m = centers[centers.Count - 1] - centers[i - 1];
                    n = centers[centers.Count - 1] - centers[0];
                }
                else
                {
                    m = centers[i] - centers[i - 1];
                    n = centers[i] - centers[i + 1];
                }
                double angle1 = Vector3d.VectorAngle(m, n);
                if (angle1 > Math.PI - 0.01)
                {
                    continue;
                }
                else
                {
                    Vector3d move = Vector3d.Add(m, n);
                    move *= -0.5 * (Math.PI - angle1) / Math.PI;

                    centerMoves[i] += move * BendingWeight;
                    totalWeights[i] += BendingWeight;
                }
            }
        }

        private void makeAngleFlatten(List<Point3d> centers, int BendingWeight)
        {
            for (int i = 1; i < centers.Count - 1; i++)
            {
                Vector3d m = centers[i] - centers[i - 1];
                Vector3d n = centers[i] - centers[i + 1];
                double angle1 = Vector3d.VectorAngle(m, n);
                if (angle1 > Math.PI - 0.01)
                {
                    continue;
                }
                else
                {
                    Vector3d move = Vector3d.Add(m, n);
                    move *= -0.5 * (Math.PI - angle1) / Math.PI;

                    centerMoves[i] += move * BendingWeight;
                    totalWeights[i] += BendingWeight;
                }
            }
        }

        private void extendLineinCircle(List<Point3d> centers, double DesireLength, int ExtendWeight)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                double d;
                Vector3d move;
                if (i == centers.Count - 1)
                {
                    d = centers[i].DistanceTo(centers[0]);
                    move = centers[i] - centers[0];
                }
                else
                {
                    d = centers[i].DistanceTo(centers[i + 1]);     //当前距离
                    move = centers[i] - centers[i + 1];
                }


                if (d < DesireLength)
                {

                    if (move.Length < 0.001) continue;
                    move.Unitize();
                    move *= 0.5 * (DesireLength - d);   //每个点走一半距离
                    move *= (DesireLength - d) / DesireLength;

                    centerMoves[i] += move * ExtendWeight;
                    totalWeights[i] += ExtendWeight;
                }
            }
        }

        private void extendLine(List<Point3d> centers, double DesireLength, int ExtendWeight)
        {
            for (int i = 0; i < centers.Count - 1; i++)
            {
                double d;
                Vector3d move;

                d = centers[i].DistanceTo(centers[i + 1]);     //当前距离
                move = centers[i] - centers[i + 1];

                if (d < DesireLength)
                {

                    if (move.Length < 0.001) continue;
                    move.Unitize();
                    move *= 0.5 * (DesireLength - d);   //每个点走一半距离
                    move *= (DesireLength - d) / DesireLength;

                    centerMoves[i] += move * ExtendWeight;
                    totalWeights[i] += ExtendWeight;
                    centerMoves[i+1] += -move * ExtendWeight;
                    totalWeights[i+1] += ExtendWeight;
                }
            }
        }

        private void updateCenters(List<Point3d> centers_eachLine, int i)
        {
            centers.Branch(i).Clear();
            GH_Path pth = new GH_Path(i);
            centers.AddRange(centers_eachLine, pth);
        }

        private void updatePosition(List<Point3d> centers, int k)
        {

            for (int i = 0; i < centers.Count; i++)
            {
                if (totalWeights[i] != 0.0)
                {
                    centers[i] += centerMoves[i] / totalWeights[i];      //每个点移动
                }
            }

            updateCenters(centers, k);

        }

        private void updatePositionWithoutStartAndEnd(List<Point3d> centers, int k)
        {

            for (int i = 1; i < centers.Count - 1; i++)
            {
                if (totalWeights[i] != 0.0)
                {
                    centers[i] += centerMoves[i] / totalWeights[i];      //每个点移动
                }
            }

            updateCenters(centers, k);

        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.DifferentialCurveIcon; ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("4678ae8b-1d2b-4bbb-8a2b-42491ffd7148");
            }
        }
    }
}