using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CurlyKale
{
    /*
     * 更新于2022.8.18，引入了多线程运行，引入了Rtree
     * 重新优化了边界问题，现在边界重新建立为小球碰撞系统，并且再使用边界控制情况下，边界阈值范围内的线段不会再分裂
     */
    public class DifferentialGrowthSystem
    {


        public List<Curve> StartCurves;
        public List<Curve> Boundaries;

        public int MaxPointsCount;
        public double DivideLength;
        public double CollisionDistance;
        public int CollisionWeight;
        public int BendingWeight;
        public int BoundaryWeight;
        public int BoundaryDistance;
        public bool ifUseBoundary;
        public bool ifGrow;
        public bool ifReset;

        public Mesh BaseMesh;

        //.............................................................Attract所需参数
        public List<Point3d> AttractPoints;
        public double AttractRadius;
        public double MaxCollisionDistance;
        public double MinCollisionDistance;
        public double MaxDivideLength;
        public double MinDivideLength;

        //.............................................................Image所需参数
        public String originImage;
        public Rectangle3d rectBoundary;  //矩形边界
        DataTree<double> CollisionDistanceNow;     //节点DataTree
        DataTree<double> DivideLengthNow;     //节点DataTree
        List<double> allCollisionDis;

        DataTree<Point3d> centers;     //节点DataTree
        List<Curve> originCurves;     //输入曲线
        List<Polyline> outPolylines;     //输出折线
        List<Curve> boundaries;
        List<Vector3d>[] totalMoves;
        List<Vector3d>[] centerMoves;   //每次最终点移动量
        List<double>[] totalWeights;
        List<int>[] collisionCount;
        List<Point3d> boundaryCollisionPoints;   //边界控制碰撞点

        DataTree<Point3d> centerLastTime;  //输出上一帧的点

        public DifferentialGrowthSystem(List<Curve> iStartCurves)
        {
            StartCurves = iStartCurves;
        }

        public void updateWithOrWithoutBoundary()
        {
            RefreshData(DivideLength, DivideLength);


            List<Point3d> centers_allLines = new List<Point3d>();
            outPolylines = new List<Polyline>();
            centerLastTime = new DataTree<Point3d>();

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_allLines.Add(centers.Branch(i)[j]);   //所有线的节点数组
                }
            }

            List<Point3d>[] centers_eachLine = new List<Point3d>[centers.Branches.Count]; //创建每条初始线的点数组
            centerMoves = new List<Vector3d>[centers.Branches.Count];
            totalWeights = new List<double>[centers.Branches.Count];
            totalMoves = new List<Vector3d>[centers.Branches.Count];
            collisionCount = new List<int>[centers.Branches.Count];

            //................................................................................................
            RTree rTree = new RTree();     //使用RTree计算节点碰撞
            for (int i = 0; i < centers_allLines.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(centers_allLines[i], i);
            }

            RTree rTree2 = new RTree();     //使用RTree计算边界碰撞
            for (int i = 0; i < boundaryCollisionPoints.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree2.Insert(boundaryCollisionPoints[i], i);
            }
            //................................................................................................

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => calculateAllCentersMoves(centers_eachLine, centers_allLines, i, rTree, rTree2, 1));
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => updatePosition(i, centers_eachLine[i]));

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线,注意cut不能用多线程
            {

                drawLineAndCut(centers_eachLine, i, 1);

            }
        }

        public void updateWithAttractors()
        {
            RefreshData(MaxDivideLength, MinDivideLength);


            List<Point3d> centers_allLines = new List<Point3d>();
            outPolylines = new List<Polyline>();
            CollisionDistanceNow = new DataTree<double>();   //记录每个节点对应碰撞距离
            DivideLengthNow = new DataTree<double>(); //记录每个节点对应分裂距离
            centerLastTime = new DataTree<Point3d>();

            //......................创建记录明度映射值tree
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                List<double> CollisionNow_eachLine = new List<double>();
                List<double> DivideNow_eachLine = new List<double>();

                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    double collisiondis;
                    double dividedis;

                    collisiondis = GetMapDataWithRadiusControl(MinCollisionDistance, MaxCollisionDistance, AttractPoints, centers.Branch(i)[j]);  //计算映射后的值,明度越小值越小
                    dividedis = GetMapDataWithRadiusControl(MinDivideLength, MaxDivideLength, AttractPoints, centers.Branch(i)[j]);  //计算映射后的值,明度越小值越小

                    CollisionNow_eachLine.Add(collisiondis);   //每一条线的节点碰撞距离
                    DivideNow_eachLine.Add(dividedis);   //每一条线的节点碰撞距离

                }

                GH_Path subPath = new GH_Path(i);
                CollisionDistanceNow.AddRange(CollisionNow_eachLine, subPath);
                DivideLengthNow.AddRange(DivideNow_eachLine, subPath);
            }

            allCollisionDis = CollisionDistanceNow.AllData();   //拍平数据用于后续push步骤Rtree计算的索引



            //................................................................................................
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_allLines.Add(centers.Branch(i)[j]);   //所有线的节点数组
                }
            }

            List<Point3d>[] centers_eachLine = new List<Point3d>[centers.Branches.Count]; //创建每条初始线的点数组
            centerMoves = new List<Vector3d>[centers.Branches.Count];
            totalWeights = new List<double>[centers.Branches.Count];
            totalMoves = new List<Vector3d>[centers.Branches.Count];
            collisionCount = new List<int>[centers.Branches.Count];


            //................................................................................................
            RTree rTree = new RTree();     //使用RTree计算节点碰撞
            for (int i = 0; i < centers_allLines.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(centers_allLines[i], i);
            }

            RTree rTree2 = new RTree();     //使用RTree计算边界碰撞
            for (int i = 0; i < boundaryCollisionPoints.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree2.Insert(boundaryCollisionPoints[i], i);
            }
            //................................................................................................


            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => calculateAllCentersMoves(centers_eachLine, centers_allLines, i, rTree, rTree2, 2));
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => updatePosition(i, centers_eachLine[i]));

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线,注意cut不能用多线程
            {

                drawLineAndCut(centers_eachLine, i, 2);

            }

        }

        public void updateWithImage()
        {
            RefreshData(MaxDivideLength, MinDivideLength);


            List<Point3d> centers_allLines = new List<Point3d>();

            outPolylines = new List<Polyline>();
            CollisionDistanceNow = new DataTree<double>();   //记录每个节点对应碰撞距离
            DivideLengthNow = new DataTree<double>(); //记录每个节点对应分裂距离
            centerLastTime = new DataTree<Point3d>();


            //......................Load Image 载入图片
            Bitmap bitmap = new Bitmap(originImage);

            //// Lock into a GH_MemoryBitmap for fast colour access.锁定图片更快速索引，但是不知道为啥在犀牛7容易崩溃
            //GH_MemoryBitmap mem = new GH_MemoryBitmap(bitmap, System.Drawing.Drawing2D.WrapMode.Clamp);

            //......................创建记录明度映射值tree
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                List<double> CollisionNow_eachLine = new List<double>();
                List<double> DivideNow_eachLine = new List<double>();

                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    double collisiondis;
                    double dividedis;
                    if ((int)rectBoundary.Contains(centers.Branch(i)[j]) == 1)  //in 1, out 2 ,coincidence 3，如果对于该点在边界内
                    {
                        float bri = GetBrightnessinImage(bitmap, centers.Branch(i)[j]);
                        collisiondis = bri * (MaxCollisionDistance - MinCollisionDistance) + MinCollisionDistance;  //计算映射后的值,明度越小值越小
                        dividedis = bri * (MaxDivideLength - MinDivideLength) + MinDivideLength;  //计算映射后的值,明度越小值越小
                    }
                    else
                    {
                        collisiondis = MaxCollisionDistance;
                        dividedis = MaxDivideLength;
                    }


                    CollisionNow_eachLine.Add(collisiondis);   //每一条线的节点碰撞距离
                    DivideNow_eachLine.Add(dividedis);   //每一条线的节点碰撞距离

                }

                GH_Path subPath = new GH_Path(i);
                CollisionDistanceNow.AddRange(CollisionNow_eachLine, subPath);
                DivideLengthNow.AddRange(DivideNow_eachLine, subPath);
            }

            allCollisionDis = CollisionDistanceNow.AllData();   //拍平数据用于后续push步骤Rtree计算的索引

            //// You must *always* release a memory bitmap.
            //// If you make changes to the bitmap, for example by applying filters or settings pixels,
            //// you must use mem.Release(true);
            //mem.Release(false);

            //................................................................................................
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_allLines.Add(centers.Branch(i)[j]);   //所有线的节点数组
                }
            }

            List<Point3d>[] centers_eachLine = new List<Point3d>[centers.Branches.Count]; //创建每条初始线的点数组
            centerMoves = new List<Vector3d>[centers.Branches.Count];
            totalWeights = new List<double>[centers.Branches.Count];
            totalMoves = new List<Vector3d>[centers.Branches.Count];
            collisionCount = new List<int>[centers.Branches.Count];


            //................................................................................................
            RTree rTree = new RTree();     //使用RTree计算节点碰撞
            for (int i = 0; i < centers_allLines.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(centers_allLines[i], i);
            }

            RTree rTree2 = new RTree();     //使用RTree计算边界碰撞
            for (int i = 0; i < boundaryCollisionPoints.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree2.Insert(boundaryCollisionPoints[i], i);
            }
            //................................................................................................


            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => calculateAllCentersMoves(centers_eachLine, centers_allLines, i, rTree, rTree2, 2));
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => updatePosition(i, centers_eachLine[i]));

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线,注意cut不能用多线程
            {

                drawLineAndCut(centers_eachLine, i, 2);

            }

        }

        public void updateWithMesh()
        {
            RefreshDataWithMesh(DivideLength, DivideLength);

            List<Point3d> centers_allLines = new List<Point3d>();
            outPolylines = new List<Polyline>();
            centerLastTime = new DataTree<Point3d>();

            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_allLines.Add(centers.Branch(i)[j]);   //所有线的节点数组
                }
            }

            List<Point3d>[] centers_eachLine = new List<Point3d>[centers.Branches.Count]; //创建每条初始线的点数组
            centerMoves = new List<Vector3d>[centers.Branches.Count];
            totalWeights = new List<double>[centers.Branches.Count];
            totalMoves = new List<Vector3d>[centers.Branches.Count];
            collisionCount = new List<int>[centers.Branches.Count];

            //................................................................................................
            RTree rTree = new RTree();     //使用RTree计算节点碰撞
            for (int i = 0; i < centers_allLines.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(centers_allLines[i], i);
            }

            RTree rTree2 = new RTree();     //使用RTree计算边界碰撞
            for (int i = 0; i < boundaryCollisionPoints.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree2.Insert(boundaryCollisionPoints[i], i);
            }
            //................................................................................................

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => calculateAllCentersMoves(centers_eachLine, centers_allLines, i, rTree, rTree2, 1));
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => updatePosition(i, centers_eachLine[i]));


            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线,注意cut不能用多线程
            {
                //.........................................................将移动后的点拍到网格上

                List<Point3d> NewCenters_eachLine = GetClosetOnMesh(centers_eachLine[i]);

                //.........................................................装入输出DataTree

                GH_Path subPath = new GH_Path(i);
                centerLastTime.AddRange(NewCenters_eachLine, subPath);

                //..................................................................................画出每一条折线并装入输出数列
                List<Point3d> polyCenter_eachLine = new List<Point3d>(NewCenters_eachLine);
                if (originCurves[i].IsClosed)
                {
                    polyCenter_eachLine.Add(polyCenter_eachLine[0]);
                }

                Polyline polyline_each = new Polyline(polyCenter_eachLine);
                outPolylines.Add(polyline_each);

                //............................................................分裂出新的节点，将其拍到网格上并且更新
                if (ifGrow == true)
                {

                    if (originCurves[i].IsClosed)
                    {
                        cutInCircleWithMesh(NewCenters_eachLine, MaxPointsCount, DivideLength);
                        updateCenters(NewCenters_eachLine, i);
                    }
                    else
                    {
                        cutWithMesh(NewCenters_eachLine, MaxPointsCount, DivideLength);
                        updateCenters(NewCenters_eachLine, i);
                    }
                }
            }

        }

        public void updateWithMeshAttract()
        {
            RefreshData(MaxDivideLength, MinDivideLength);

            List<Point3d> centers_allLines = new List<Point3d>();
            outPolylines = new List<Polyline>();
            CollisionDistanceNow = new DataTree<double>();   //记录每个节点对应碰撞距离
            DivideLengthNow = new DataTree<double>(); //记录每个节点对应分裂距离
            centerLastTime = new DataTree<Point3d>();

            //......................创建记录距离映射值tree
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                List<double> CollisionNow_eachLine = new List<double>();
                List<double> DivideNow_eachLine = new List<double>();

                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    double collisiondis;
                    double dividedis;

                    collisiondis = GetMapDataWithRadiusControl(MinCollisionDistance, MaxCollisionDistance, AttractPoints, centers.Branch(i)[j]);  //计算映射后的值,明度越小值越小
                    dividedis = GetMapDataWithRadiusControl(MinDivideLength, MaxDivideLength, AttractPoints, centers.Branch(i)[j]);  //计算映射后的值,明度越小值越小

                    CollisionNow_eachLine.Add(collisiondis);   //每一条线的节点碰撞距离
                    DivideNow_eachLine.Add(dividedis);   //每一条线的节点碰撞距离

                }

                GH_Path subPath = new GH_Path(i);
                CollisionDistanceNow.AddRange(CollisionNow_eachLine, subPath);
                DivideLengthNow.AddRange(DivideNow_eachLine, subPath);
            }

            allCollisionDis = CollisionDistanceNow.AllData();   //拍平数据用于后续push步骤Rtree计算的索引

            //................................................................................................
            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线
            {
                for (int j = 0; j < centers.Branch(i).Count; j++)
                {
                    centers_allLines.Add(centers.Branch(i)[j]);   //所有线的节点数组
                }
            }

            List<Point3d>[] centers_eachLine = new List<Point3d>[centers.Branches.Count]; //创建每条初始线的点数组
            centerMoves = new List<Vector3d>[centers.Branches.Count];
            totalWeights = new List<double>[centers.Branches.Count];
            totalMoves = new List<Vector3d>[centers.Branches.Count];
            collisionCount = new List<int>[centers.Branches.Count];


            //................................................................................................
            RTree rTree = new RTree();     //使用RTree计算节点碰撞
            for (int i = 0; i < centers_allLines.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree.Insert(centers_allLines[i], i);
            }

            RTree rTree2 = new RTree();     //使用RTree计算边界碰撞
            for (int i = 0; i < boundaryCollisionPoints.Count; i++)     //将所有线的节点添加进RTree
            {
                rTree2.Insert(boundaryCollisionPoints[i], i);
            }
            //................................................................................................


            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => calculateAllCentersMoves(centers_eachLine, centers_allLines, i, rTree, rTree2, 2));
            System.Threading.Tasks.Parallel.For(0, centers.Branches.Count, i => updatePosition(i, centers_eachLine[i]));


            for (int i = 0; i < centers.Branches.Count; i++)  //对于每条初始线,注意cut不能用多线程
            {
                //.........................................................将移动后的点拍到网格上

                List<Point3d> NewCenters_eachLine = GetClosetOnMesh(centers_eachLine[i]);

                //.........................................................装入输出DataTree

                GH_Path subPath = new GH_Path(i);
                centerLastTime.AddRange(NewCenters_eachLine, subPath);

                //..................................................................................画出每一条折线并装入输出数列
                List<Point3d> polyCenter_eachLine = new List<Point3d>(NewCenters_eachLine);
                if (originCurves[i].IsClosed)
                {
                    polyCenter_eachLine.Add(polyCenter_eachLine[0]);
                }

                Polyline polyline_each = new Polyline(polyCenter_eachLine);
                outPolylines.Add(polyline_each);

                //............................................................分裂出新的节点，将其拍到网格上并且更新
                if (ifGrow == true)
                {
                    if (originCurves[i].IsClosed)
                    {
                        List<Point3d> newPoint_eachLine = cutInCircleWithMesh(NewCenters_eachLine, MaxPointsCount, i);
                        updateCenters(newPoint_eachLine, i);
                    }
                    else
                    {
                        List<Point3d> newPoint_eachLine = cutWithMesh(NewCenters_eachLine, MaxPointsCount, i);
                        updateCenters(newPoint_eachLine, i);
                    }
                }
            }

        }
        public void drawLineAndCut(List<Point3d>[] centers_eachLine, int index, int mode)
        {
            //.......................................................................................画出每一条折线并装入输出数列
            List<Point3d> polyCenter_eachLine = new List<Point3d>(centers_eachLine[index]);
            if (originCurves[index].IsClosed)
            {
                polyCenter_eachLine.Add(polyCenter_eachLine[0]);
            }

            Polyline polyline_each = new Polyline(polyCenter_eachLine);
            outPolylines.Add(polyline_each);

            //.......................................................................................装入输出节点DataTree

            GH_Path subPath = new GH_Path(index);
            centerLastTime.AddRange(centers_eachLine[index], subPath);

            //.........................................................................................分裂出新的节点并且更新
            if (ifGrow == true && mode == 1)
            {
                if (originCurves[index].IsClosed)
                {
                    cutInCircle(centers_eachLine[index], MaxPointsCount, DivideLength);
                    updateCenters(centers_eachLine[index], index);
                }
                else
                {
                    cut(centers_eachLine[index], MaxPointsCount, DivideLength);
                    updateCenters(centers_eachLine[index], index);
                }
            }

            //.........................................................................................分裂出新的节点并且更新
            if (ifGrow == true && mode == 2)
            {
                if (originCurves[index].IsClosed)
                {
                    List<Point3d> newPoint_eachLine = cutInCircle(centers_eachLine[index], MaxPointsCount, index);
                    updateCenters(newPoint_eachLine, index);
                }
                else
                {
                    List<Point3d> newPoint_eachLine = cut(centers_eachLine[index], MaxPointsCount, index);
                    updateCenters(newPoint_eachLine, index);
                }
            }
        }

        private void calculateAllCentersMoves(List<Point3d>[] centers_eachLine, List<Point3d> centers_allLines, int i, RTree rTree, RTree rTree2, int mode)
        {
            centers_eachLine[i] = new List<Point3d>();

            for (int j = 0; j < centers.Branch(i).Count; j++)
            {
                centers_eachLine[i].Add(centers.Branch(i)[j]);   //每一条线的节点数组
            }

            centerMoves[i] = new List<Vector3d>();
            totalWeights[i] = new List<double>();


            for (int j = 0; j < centers_eachLine[i].Count; j++)
            {
                totalWeights[i].Add(0.0);
                centerMoves[i].Add(new Vector3d(0.0, 0.0, 0.0));
            }

            if (mode == 1)  //选择是否干扰模式
            {
                push(centers_eachLine[i], centers_allLines, CollisionDistance, CollisionWeight, i, rTree);
            }
            else if (mode == 2)
            {
                pushWithMultyInfluences(centers_eachLine[i], centers_allLines, CollisionWeight, i, rTree);
            }

            if (ifUseBoundary == true)
            {
                if (originCurves[i].IsClosed)
                {
                    push(centers_eachLine[i], boundaryCollisionPoints, BoundaryDistance, BoundaryWeight, i, rTree2);
                    makeAngleFlattenInCircle(centers_eachLine[i], BendingWeight, i);
                }
                else
                {
                    push(centers_eachLine[i], boundaryCollisionPoints, BoundaryDistance, BoundaryWeight, i, rTree2);
                    makeAngleFlatten(centers_eachLine[i], BendingWeight, i);
                }
            }
            else if (ifUseBoundary == false)
            {
                if (originCurves[i].IsClosed)
                {
                    makeAngleFlattenInCircle(centers_eachLine[i], BendingWeight, i);
                }
                else
                {
                    makeAngleFlatten(centers_eachLine[i], BendingWeight, i);
                }
            }
        }

        private void updatePosition(int i, List<Point3d> centers_eachLine)
        {
            if (originCurves[i].IsClosed)
            {
                updatePosition(centers_eachLine, i);
            }
            else
            {
                updatePositionWithoutStartAndEnd(centers_eachLine, i);
            }
        }


        private List<Point3d> GetClosetOnMesh(List<Point3d> OutCenter)
        {
            List<Point3d> ClosetPoints_each = new List<Point3d>();

            for (int i = 0; i < OutCenter.Count; i++)  //对于每条初始线
            {
                Point3d newP1 = BaseMesh.ClosestPoint(OutCenter[i]);
                ClosetPoints_each.Add(newP1);
            }

            return ClosetPoints_each;
        }
        private void RefreshData(double DivideLength, double minLength)
        {
            if (ifReset == true)
            {
                originCurves = new List<Curve>(StartCurves);
                centers = new DataTree<Point3d>();  //重新获取节点的Tree

                for (int i = 0; i < originCurves.Count; i++)  //对于每条初始线
                {
                    List<Point3d> subtree = new List<Point3d>();

                    Point3d[] points;
                    originCurves[i].DivideByLength(DivideLength, true, out points);   //细分每个线段
                    if (points == null)
                    {
                        subtree.Add(originCurves[i].PointAtStart);
                        subtree.Add(originCurves[i].PointAtEnd);
                    }
                    else
                    {
                        foreach (Point3d point in points)    //对于每个初始线的点
                        {
                            subtree.Add(point);
                        }
                        if (!originCurves[i].IsClosed)
                        {
                            subtree.Add(originCurves[i].PointAtEnd);
                        }
                    }


                    GH_Path subPath = new GH_Path(i);
                    centers.AddRange(subtree, subPath);
                }



                boundaries = new List<Curve>(Boundaries);      //重新获取边界

                boundaryCollisionPoints = new List<Point3d>();  //所有碰撞点

                for (int i = 0; i < boundaries.Count; i++)  //对于每条边界线
                {

                    Point3d[] points;
                    boundaries[i].DivideByLength(0.5 * minLength, true, out points);    //对每条边界线进行divide

                    if (points == null)
                    {
                        boundaryCollisionPoints.Add(boundaries[i].PointAtStart);
                        boundaryCollisionPoints.Add(boundaries[i].PointAtEnd);
                    }
                    else
                    {
                        foreach (Point3d point in points)    //对于每个初始线的点
                        {
                            boundaryCollisionPoints.Add(point);
                        }
                        if (!boundaries[i].IsClosed)
                        {
                            boundaryCollisionPoints.Add(boundaries[i].PointAtEnd);
                        }
                    }
                }

            }
        }    //重新载入初始数据

        private void RefreshDataWithMesh(double DivideLength, double minLength)
        {
            if (ifReset == true)
            {
                originCurves = new List<Curve>(StartCurves);
                centers = new DataTree<Point3d>();  //重新获取节点的Tree

                for (int i = 0; i < originCurves.Count; i++)  //对于每条初始线
                {
                    List<Point3d> subtree = new List<Point3d>();

                    Point3d[] points;
                    originCurves[i].DivideByLength(DivideLength, true, out points);   //细分每个线段
                    if (points == null)
                    {
                        Point3d newP1 = BaseMesh.ClosestPoint(originCurves[i].PointAtStart);   //找到网格上最近点
                        Point3d newP2 = BaseMesh.ClosestPoint(originCurves[i].PointAtEnd);
                        subtree.Add(newP1);
                        subtree.Add(newP2);
                    }
                    else
                    {
                        foreach (Point3d point in points)    //对于每个初始线的点
                        {
                            Point3d newP = BaseMesh.ClosestPoint(point);
                            subtree.Add(newP);
                        }
                        if (!originCurves[i].IsClosed)
                        {
                            Point3d newP2 = BaseMesh.ClosestPoint(originCurves[i].PointAtEnd);
                            subtree.Add(newP2);
                        }
                    }


                    GH_Path subPath = new GH_Path(i);
                    centers.AddRange(subtree, subPath);
                }


                //bool ifc = BaseMesh.IsClosed;

                boundaries = new List<Curve>(Boundaries);      //重新获取边界

                boundaryCollisionPoints = new List<Point3d>();  //所有碰撞点

                for (int i = 0; i < boundaries.Count; i++)  //对于每条边界线
                {

                    Point3d[] points;
                    boundaries[i].DivideByLength(0.5 * minLength, true, out points);    //对每条边界线进行divide

                    if (points == null)
                    {
                        Point3d newP1 = BaseMesh.ClosestPoint(boundaries[i].PointAtStart);   //找到网格上最近点
                        Point3d newP2 = BaseMesh.ClosestPoint(boundaries[i].PointAtEnd);
                        boundaryCollisionPoints.Add(newP1);
                        boundaryCollisionPoints.Add(newP2);
                    }
                    else
                    {
                        foreach (Point3d point in points)    //对于每个初始线的点
                        {
                            Point3d newP1 = BaseMesh.ClosestPoint(point);
                            boundaryCollisionPoints.Add(newP1);
                        }
                        if (!boundaries[i].IsClosed)
                        {
                            Point3d newP2 = BaseMesh.ClosestPoint(boundaries[i].PointAtEnd);
                            boundaryCollisionPoints.Add(newP2);
                        }
                    }
                }

            }
        }    //重新载入初始数据

        private void updateCenters(List<Point3d> centers_eachLine, int i)
        {
            centers.Branch(i).Clear();
            GH_Path pth = new GH_Path(i);
            centers.AddRange(centers_eachLine, pth);
        }

        private void makeAngleFlatten(List<Point3d> centers, int BendingWeight, int index)
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

                    centerMoves[index][i] += move * BendingWeight;
                    totalWeights[index][i] += BendingWeight;
                }
            }
        }

        private void push(List<Point3d> centers, List<Point3d> allCenters, double CollisionDistance, int Weight, int index, RTree rTree)
        {
            totalMoves[index] = new List<Vector3d>();
            collisionCount[index] = new List<int>();

            for (int j = 0; j < centers.Count; j++)
            {
                totalMoves[index].Add(new Vector3d(0.0, 0.0, 0.0));
                collisionCount[index].Add(0);
            }

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Count, i => calculateCollision(rTree, i, centers, allCenters, index, CollisionDistance));

            //每个小球移动量等于总移动量除以碰撞次数
            for (int i = 0; i < centers.Count; i++)
            {
                if (collisionCount[index][i] != 0.0)
                {
                    totalMoves[index][i] /= collisionCount[index][i];         //移动速度

                    centerMoves[index][i] += totalMoves[index][i] * Weight;
                    totalWeights[index][i] += Weight;   //增加权重写法
                }
            }
        }

        private void calculateCollision(RTree rTree, int i, List<Point3d> centers, List<Point3d> allCenters, int index, double CollisionDistance)
        {
            List<int> collisionIndices = new List<int>();    //创建顶点个数个数组记录碰撞

            rTree.Search(
               new Sphere(centers[i], CollisionDistance),
               (sender, args) => { collisionIndices.Add(args.Id); });     //记录每个点在碰撞范围内的点

            foreach (int j in collisionIndices)
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

                    totalMoves[index][i] += move;
                    collisionCount[index][i] += 1;

                }
            }
        }

        private void pushWithMultyInfluences(List<Point3d> centers, List<Point3d> allCenters, int CollisionWeight, int index, RTree rTree)
        {

            totalMoves[index] = new List<Vector3d>();
            collisionCount[index] = new List<int>();

            for (int j = 0; j < centers.Count; j++)
            {
                totalMoves[index].Add(new Vector3d(0.0, 0.0, 0.0));
                collisionCount[index].Add(0);
            }

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, centers.Count, i => calculateCollisionWithInfluences(rTree, i, centers, allCenters, index));

            //每个小球移动量等于总移动量除以碰撞次数
            for (int i = 0; i < centers.Count; i++)
            {
                if (collisionCount[index][i] != 0.0)
                {
                    totalMoves[index][i] /= collisionCount[index][i];         //移动速度

                    centerMoves[index][i] += totalMoves[index][i] * CollisionWeight;
                    totalWeights[index][i] += CollisionWeight;   //增加权重写法
                }
            }
        } ////attractor&Image用

        private void calculateCollisionWithInfluences(RTree rTree, int i, List<Point3d> centers, List<Point3d> allCenters, int index)
        {

            List<int> collisionIndices = new List<int>();     //创建顶点个数个数组记录碰撞


            double collisionDistanceNow = CollisionDistanceNow.Branch(index)[i];

            rTree.Search(
          new Sphere(centers[i], collisionDistanceNow),
          (sender, args) => { collisionIndices.Add(args.Id); });     //记录每个点在碰撞范围内的点

            foreach (int j in collisionIndices)
            {
                double d = centers[i].DistanceTo(allCenters[j]);     //当前距离
                double d1;

                d1 = allCollisionDis[j];  //在拍平数据中寻找d1



                double d2 = 0.5 * (collisionDistanceNow + d1);
                if (d > d2)
                {
                    continue;
                }
                else
                {
                    Vector3d move = centers[i] - allCenters[j];
                    if (move.Length < 0.001) continue;
                    move.Unitize();
                    move *= 0.5 * (d2 - d);   //每个点走一半距离
                    move *= (d2 - d) / d2;

                    totalMoves[index][i] += move;
                    collisionCount[index][i] += 1;

                }
            }
        }

        private double GetMapDataWithRadiusControl(double minData, double maxData, List<Point3d> attrators, Point3d pointnow)
        {
            Point3d[] att = attrators.ToArray();
            Point3dList attractorList = new Point3dList(att);
            int indexCloset = attractorList.ClosestIndex(pointnow);    //找到最近吸引点的index

            double closetDistance = pointnow.DistanceTo(attractorList[indexCloset]);    //离最近吸引点的距离

            double d1;
            if (closetDistance <= AttractRadius)   //点在吸引半径内
            {
                d1 = closetDistance / AttractRadius * (maxData - minData) + minData;  //计算映射后的值

            }
            else
            {

                d1 = maxData;  //按最大值来算

            }
            return d1;

        }//attractor用

        private float GetBrightnessinImage(Bitmap bitmap, Point3d pointnow)
        {


            // 得到在原始坐标中的横纵坐标比例.

            double n1 = (pointnow.X - (rectBoundary.Center.X - rectBoundary.Width * 0.5)) / rectBoundary.Width;
            double n2 = (pointnow.Y - (rectBoundary.Center.Y - rectBoundary.Height * 0.5)) / rectBoundary.Height;

            // 按横纵坐标比例得到在位图坐标
            double dx = n1 * bitmap.Width;
            double dy = n2 * bitmap.Height;
            int col = (int)dx;
            int row = (int)dy;

            Color c = bitmap.GetPixel(col, row);
            float bri = c.GetBrightness();  //输出明度,范围为0-1的float值，0为黑色1为白色

            return bri;
        }//Image用

        private void cut(List<Point3d> centers, int MaxPointsCount, double DivideLength)
        {
            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);
                for (int i = 0; i < centers.Count - 1; i++)
                {
                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                    int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);
                    double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                    if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                    {
                        continue;
                    }
                    else
                    {
                        if (centers[i].DistanceTo(centers[i + 1]) > DivideLength)
                        {
                            Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                            centers.Insert(i + 1, newCenter);
                        }
                    }
                    
                }
            }
        }

        private void cutWithMesh(List<Point3d> centers, int MaxPointsCount, double DivideLength)
        {
            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);
                for (int i = 0; i < centers.Count - 1; i++)
                {
                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                    int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);
                    double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                    if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                    {
                        continue;
                    }
                    else
                    {

                        if (centers[i].DistanceTo(centers[i + 1]) > DivideLength)
                        {
                            Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                            Point3d pt1 = BaseMesh.ClosestPoint(newCenter);
                            centers.Insert(i + 1, pt1);
                        }
                    }
                }
            }
        }

        private List<Point3d> cut(List<Point3d> centers, int MaxPointsCount, int k)  //attractor&Image用
        {
            List<Point3d> NewCenters_eachLine = new List<Point3d>();  //新建一个

            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {
                   
                    if (i == centers.Count - 1)
                    {
                        NewCenters_eachLine.Add(centers[i]);
                    }
                    else
                    {
                        NewCenters_eachLine.Add(centers[i]);    //加入下一个点

                        int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            double d = 0.5 * (DivideLengthNow.Branch(k)[i] + DivideLengthNow.Branch(k)[i + 1]);
                            if (centers[i].DistanceTo(centers[i + 1]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter);   //在末尾插入新的点
                            }
                        }
                          
                    }

                }
            }
            else
            {
                NewCenters_eachLine = new List<Point3d>(centers);
            }

            return NewCenters_eachLine;
        }

        private List<Point3d> cutWithMesh(List<Point3d> centers, int MaxPointsCount, int k)  //Mesh attractor用
        {
            List<Point3d> NewCenters_eachLine = new List<Point3d>();  //新建一个

            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {
                  
                    if (i == centers.Count - 1)
                    {
                        NewCenters_eachLine.Add(centers[i]);
                    }
                    else
                    {
                        NewCenters_eachLine.Add(centers[i]);    //加入下一个点

                        int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {

                            double d = 0.5 * (DivideLengthNow.Branch(k)[i] + DivideLengthNow.Branch(k)[i + 1]);
                            if (centers[i].DistanceTo(centers[i + 1]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                Point3d newCenter1 = BaseMesh.ClosestPoint(newCenter);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter1);   //在末尾插入新的点
                            }
                        }

                    }

                }
            }
            else
            {
                NewCenters_eachLine = new List<Point3d>(centers);
            }

            return NewCenters_eachLine;
        }

        private void cutInCircle(List<Point3d> centers, int MaxPointsCount, double DivideLength)
        {

            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {
                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);                  
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);                  

                    if (i == centers.Count - 1)//如是最后一个点
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[0]);
                        double d2 = centers[0].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            if (centers[centers.Count - 1].DistanceTo(centers[0]) > DivideLength)
                            {
                                Point3d newCenter = 0.5 * (centers[centers.Count - 1] + centers[0]);
                                centers.Insert(i + 1, newCenter);
                            }
                        }
                       
                    }
                    else
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            if (centers[i].DistanceTo(centers[i + 1]) > DivideLength)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                centers.Insert(i + 1, newCenter);
                            }
                        }
                      
                    }
                }
            }
        }

        private void cutInCircleWithMesh(List<Point3d> centers, int MaxPointsCount, double DivideLength)
        {
            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {
                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);

                    if (i == centers.Count - 1)
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[0]);
                        double d2 = centers[0].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            if (centers[centers.Count - 1].DistanceTo(centers[0]) > DivideLength)
                            {
                                Point3d newCenter = 0.5 * (centers[centers.Count - 1] + centers[0]);
                                Point3d pt1 = BaseMesh.ClosestPoint(newCenter);
                                centers.Insert(i + 1, pt1);
                            }
                        }                  
                    }
                    else
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            if (centers[i].DistanceTo(centers[i + 1]) > DivideLength)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                Point3d pt1 = BaseMesh.ClosestPoint(newCenter);
                                centers.Insert(i + 1, pt1);
                            }
                        }                     
                    }
                }
            }
        }

        private List<Point3d> cutInCircle(List<Point3d> centers, int MaxPointsCount, int k)
        {

            List<Point3d> NewCenters_eachLine = new List<Point3d>();  //新建一个

            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {

                    NewCenters_eachLine.Add(centers[i]);    //加入下一个点

                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);   
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);
                   
                    if (i == centers.Count - 1)//如果是最后一个点
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[0]);
                        double d2 = centers[0].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂
                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            double d = 0.5 * (DivideLengthNow.Branch(k)[centers.Count - 1] + DivideLengthNow.Branch(k)[0]);
                            if (centers[centers.Count - 1].DistanceTo(centers[0]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[centers.Count - 1] + centers[0]);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter);   //在末尾插入新的点
                            }
                        }
                            
                    }
                    else
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂
                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            double d = 0.5 * (DivideLengthNow.Branch(k)[i] + DivideLengthNow.Branch(k)[i + 1]);
                            if (centers[i].DistanceTo(centers[i + 1]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter);
                            }
                        }
                            
                    }
                }
            }
            else
            {
                NewCenters_eachLine = new List<Point3d>(centers);
            }

            return NewCenters_eachLine;
        }//attractor&Image用

        private List<Point3d> cutInCircleWithMesh(List<Point3d> centers, int MaxPointsCount, int k)
        {

            List<Point3d> NewCenters_eachLine = new List<Point3d>();  //新建一个

            if (centers.Count < MaxPointsCount)
            {
                PointCloud boundaryPointsCloud = new PointCloud(boundaryCollisionPoints);

                for (int i = 0; i < centers.Count; i++)
                {

                    NewCenters_eachLine.Add(centers[i]);    //加入下一个点
                    int index1 = boundaryPointsCloud.ClosestPoint(centers[i]);
                    double d1 = centers[i].DistanceTo(boundaryCollisionPoints[index1]);

                    if (i == centers.Count - 1)
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[0]);
                        double d2 = centers[0].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            double d = 0.5 * (DivideLengthNow.Branch(k)[centers.Count - 1] + DivideLengthNow.Branch(k)[0]);
                            if (centers[centers.Count - 1].DistanceTo(centers[0]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[centers.Count - 1] + centers[0]);
                                Point3d newCenter1 = BaseMesh.ClosestPoint(newCenter);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter1);   //在末尾插入新的点
                            }
                        }                         
                    }
                    else
                    {
                        int index2 = boundaryPointsCloud.ClosestPoint(centers[i + 1]);
                        double d2 = centers[i + 1].DistanceTo(boundaryCollisionPoints[index2]);  //获取两个点到边界的距离，如果两个点都在边界内则不分裂

                        if (d1 < BoundaryDistance && d2 < BoundaryDistance && ifUseBoundary)
                        {
                            continue;
                        }
                        else
                        {
                            double d = 0.5 * (DivideLengthNow.Branch(k)[i] + DivideLengthNow.Branch(k)[i + 1]);
                            if (centers[i].DistanceTo(centers[i + 1]) > d)
                            {
                                Point3d newCenter = 0.5 * (centers[i] + centers[i + 1]);
                                Point3d newCenter1 = BaseMesh.ClosestPoint(newCenter);
                                NewCenters_eachLine.Insert(NewCenters_eachLine.Count, newCenter1);
                            }
                        }
                            
                    }
                }
            }
            else
            {
                NewCenters_eachLine = new List<Point3d>(centers);
            }

            return NewCenters_eachLine;
        }//Mesh attractor用

        private void makeAngleFlattenInCircle(List<Point3d> centers, int BendingWeight, int index)
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

                    centerMoves[index][i] += move * BendingWeight;
                    totalWeights[index][i] += BendingWeight;
                }
            }
        }

        private void updatePosition(List<Point3d> centers, int index)
        {

            for (int i = 0; i < centers.Count; i++)
            {
                if (totalWeights[index][i] != 0.0)
                {
                    centers[i] += centerMoves[index][i] / totalWeights[index][i];      //每个点移动
                }
            }

            updateCenters(centers, index);

        }

        private void updatePositionWithoutStartAndEnd(List<Point3d> centers, int index)
        {

            for (int i = 1; i < centers.Count - 1; i++)
            {
                if (totalWeights[index][i] != 0.0)
                {
                    centers[i] += centerMoves[index][i] / totalWeights[index][i];      //每个点移动
                }
            }

            updateCenters(centers, index);

        }

        private void updatePositionWithBoundaries(List<Point3d> centers, double BoundaryDistance, int k, int index)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                if (totalWeights[index][i] != 0.0)
                {

                    bool ifMove = true;
                    for (int j = 0; j < boundaries.Count; j++)
                    {
                        bool ifOut1 = ifOut(centers, i, boundaries[j], BoundaryDistance, index);
                        if (ifOut1 == false)     //如果下次移动在边界内
                        {
                            ifMove = true;
                            continue;
                        }
                        else                //如果下次移动出界
                        {
                            ifMove = false;
                            break;
                        }
                    }

                    if (ifMove == true)
                    {
                        centers[i] += centerMoves[index][i] / totalWeights[index][i];      //每个点移动
                    }
                }
            }

            updateCenters(centers, k);

        }

        private void updatePositionWithoutStartAndEndWithBoundaries(List<Point3d> centers, double BoundaryDistance, int k, int index)
        {
            for (int i = 1; i < centers.Count - 1; i++)
            {
                if (totalWeights[index][i] != 0.0)
                {

                    bool ifMove = true;
                    for (int j = 0; j < boundaries.Count; j++)
                    {
                        bool ifOut1 = ifOut(centers, i, boundaries[j], BoundaryDistance, index);
                        if (ifOut1 == false)     //如果下次移动在边界内
                        {
                            ifMove = true;
                            continue;
                        }
                        else                //如果下次移动出界
                        {
                            ifMove = false;
                            break;
                        }
                    }

                    if (ifMove == true)
                    {
                        centers[i] += centerMoves[index][i] / totalWeights[index][i];      //每个点移动
                    }
                }
            }

            updateCenters(centers, k);

        }

        private bool ifOut(List<Point3d> centers, int i, Curve curve1, double boundaryDistance, int index)
        {
            Point3d nextStation = Point3d.Add(centers[i], centerMoves[index][i] / totalWeights[index][i]);
            double tClosest;
            curve1.ClosestPoint(nextStation, out tClosest);
            Point3d closestPoint1 = curve1.PointAt(tClosest);    //下一帧位置在曲线上的最近点
            double distance = nextStation.DistanceTo(closestPoint1);

            double cha = distance - boundaryDistance;    //判断移动后点到垂点的距离是否小于半径，小于则出界

            Boolean ifout = false;
            if (cha >= 0)
            {
                return ifout;
            }
            else
            {
                ifout = !ifout;
                return ifout;
            }

        }


        public List<Polyline> GetOutPolylines()
        {
            return outPolylines;
        }

        public DataTree<Point3d> Getcenters()
        {
            return centerLastTime;
        }

        public DataTree<double> GetcollisionDistanceNow()
        {
            return CollisionDistanceNow;
        }
    }
}