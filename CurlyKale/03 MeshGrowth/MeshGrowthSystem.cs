using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plankton;
using PlanktonGh;
using Rhino.Geometry;

namespace CurlyKale
{
    public class MeshGrowthSystem
    {
        public PlanktonMesh ptMesh;

        public bool Grow = false;
        public int MaxVertexCount;

        public double EdgeLengthConstraintWeight;
        public double CollisionDistance;
        public double CollisionWeight;
        public double BendingResistanceWeight;
        public double BoundaryAngleWeight;
        public double minLength;
        public double maximumDist;

        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;
        //private List<Vector3d> totalCollectionMove;
        //private List<int> CollectionCounts;
        private List<double> GeodesicDistance;
        public int frameCount = 0;

        public MeshGrowthSystem(Mesh startingMesh)
        {
            ptMesh = startingMesh.ToPlanktonMesh();
        }

        public Mesh GetRhinoMesh()
        {
            return ptMesh.ToRhinoMesh();
        }

        public List<double> GetGeodesicD()
        {
            return GeodesicDistance;
        }

        public void Update()
        {
            frameCount += 1;
          
            flipEdgeControl();   //翻转边缘
         

            if (Grow)
            {
                if (frameCount % 6 == 0)      //四次分裂一次
                {
                    GetGeodesic();
                    SplitAllLongEdges();      //分裂长边
                }
            }

            totalWeightedMoves = new List<Vector3d>();
            totalWeights = new List<double>();
            //totalCollectionMove = new List<Vector3d>();
            //CollectionCounts = new List<int>();

            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                totalWeightedMoves.Add(Vector3d.Zero);
                totalWeights.Add(0.0);
                //totalCollectionMove.Add(Vector3d.Zero);
                //CollectionCounts.Add(0);
            }

            ProcessEdgeLengthConstraint();
            ProcessCollisionUsingRTree();
            ProcessBendingResistance();
            ControlEdgeAngle();

            UpdateVertexPositionsAndVelicities();
            //GetGeodesic();
        }

        private double GetSplitDistance(int k)
        {
            maximumDist = 0;
            for (int i = 0; i < GeodesicDistance.Count; ++i)
            {
                maximumDist = Math.Max(maximumDist, GeodesicDistance[i]);          //测地线间距和最大影响距离作比较，取两个的最大值
            }

            int n1 = ptMesh.Halfedges[k].StartVertex;
            int n2 = ptMesh.Halfedges[k + 1].StartVertex;
            double d1 = GeodesicDistance[n1];
            double d2 = GeodesicDistance[n2];

            double maxLength = 0.99 * CollisionDistance;

            double tLen = (d1 + d2) * 0.5 / maximumDist * (maxLength - minLength) + minLength;      //根据每条边两个顶点的测地线平均值来获取tLen（分裂距离）
            return tLen;
        }
        private void SplitEdge(int edgeIndex)
        {
            int newHalfEdgeIndex = ptMesh.Halfedges.SplitEdge(edgeIndex);         //新的半边索引

            ptMesh.Vertices.SetVertex(             //设置新的顶点（半边的中点），添加在最后一个
                ptMesh.Vertices.Count - 1,    //在splitedge方法中已经加入了新的
                0.5 * (ptMesh.Vertices[ptMesh.Halfedges[edgeIndex].StartVertex].ToPoint3d() + ptMesh.Vertices[ptMesh.Halfedges[edgeIndex + 1].StartVertex].ToPoint3d()));

            if (ptMesh.Halfedges[edgeIndex].AdjacentFace >= 0)
                ptMesh.Faces.SplitFace(newHalfEdgeIndex, ptMesh.Halfedges[edgeIndex].PrevHalfedge);

            if (ptMesh.Halfedges[edgeIndex + 1].AdjacentFace >= 0)
                ptMesh.Faces.SplitFace(edgeIndex + 1, ptMesh.Halfedges[ptMesh.Halfedges[edgeIndex + 1].NextHalfedge].NextHalfedge);
        }

        public void SplitAllLongEdges()
        {
            int halfedgeCount = ptMesh.Halfedges.Count;

            for (int k = 0; k < halfedgeCount; k += 2)
            {
                double splitDistance = GetSplitDistance(k);   //获取每条边的分裂阈值

                if (ptMesh.Vertices.Count < MaxVertexCount &&
                    ptMesh.Halfedges.GetLength(k) > splitDistance)               //半边长度超过阈值
                {
                    if (ptMesh.Halfedges[k].AdjacentFace >= 0 && ptMesh.Halfedges[k + 1].AdjacentFace >= 0)  //如果半边在内部，半边和对边都有对应面
                    {
                        if (ptMesh.Halfedges.GetLength(k) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[k].NextHalfedge)
                            && ptMesh.Halfedges.GetLength(k) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[ptMesh.Halfedges[k].NextHalfedge].NextHalfedge)    //该半边所对应三角形中最长边
                            && ptMesh.Halfedges.GetLength(k + 1) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[k + 1].NextHalfedge)
                            && ptMesh.Halfedges.GetLength(k + 1) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[ptMesh.Halfedges[k + 1].NextHalfedge].NextHalfedge)     //半边对边所对应三角形最长边
                            )
                        {
                            SplitEdge(k);     //同为两个三角形最长边则分裂
                        }
                    }

                    if (ptMesh.Halfedges[k].AdjacentFace >= 0 && ptMesh.Halfedges[k + 1].AdjacentFace < 0)//如果半边在外部,k为inner边，k+1为outer边
                    {
                        if (ptMesh.Halfedges.GetLength(k) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[k].NextHalfedge)
                            && ptMesh.Halfedges.GetLength(k) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[ptMesh.Halfedges[k].NextHalfedge].NextHalfedge)    //该半边所对应三角形中最长边                           
                            )
                        {
                            SplitEdge(k);    //为半边所对应三角形最长则分裂
                        }
                    }

                    if (ptMesh.Halfedges[k].AdjacentFace < 0 && ptMesh.Halfedges[k + 1].AdjacentFace >= 0)//如果半边在外部,k为outer边，k+1为 inner边
                    {
                        if (ptMesh.Halfedges.GetLength(k + 1) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[k + 1].NextHalfedge)
                            && ptMesh.Halfedges.GetLength(k + 1) > ptMesh.Halfedges.GetLength(ptMesh.Halfedges[ptMesh.Halfedges[k + 1].NextHalfedge].NextHalfedge)     //半边对边所对应三角形最长边                
                             )
                        {
                            SplitEdge(k);      //为半边对边所对应三角形最长则分裂
                        }
                    }
                }
            }
        }

        private void ProcessEdgeLengthConstraint()
        {
            int halfedgeCount = ptMesh.Halfedges.Count;

            for (int k = 0; k < halfedgeCount; k += 2)
            {
                PlanktonHalfedge halfedge = ptMesh.Halfedges[k];
                int i = halfedge.StartVertex;
                int j = ptMesh.Halfedges[halfedge.NextHalfedge].StartVertex;

                Vector3d d = ptMesh.Vertices[j].ToPoint3d() - ptMesh.Vertices[i].ToPoint3d();
                if (d.Length > CollisionDistance)
                {
                    Vector3d move = EdgeLengthConstraintWeight * 0.5 * (d);
                    totalWeightedMoves[i] += move;
                    totalWeightedMoves[j] -= move;
                    totalWeights[i] += EdgeLengthConstraintWeight;
                    totalWeights[j] += EdgeLengthConstraintWeight;
                }
            }
        }



        private void ProcessCollisionUsingRTree()
        {
            RTree rTree = new RTree();     //使用RTree

            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                rTree.Insert(ptMesh.Vertices[i].ToPoint3d(), i);
            }

            List<int>[] collisionIndices = new List<int>[ptMesh.Vertices.Count];     //创建顶点个数个数组记录碰撞

            for (int i = 0; i < ptMesh.Vertices.Count; i++)
                collisionIndices[i] = new List<int>();

            for (int i = 0; i < ptMesh.Vertices.Count; i++)
                rTree.Search(
                    new Sphere(ptMesh.Vertices[i].ToPoint3d(), CollisionDistance),
                    (sender, args) => { if (i < args.Id) collisionIndices[i].Add(args.Id); });     //记录在碰撞范围内的点，只记录比i小的序号，避免重复

            for (int i = 0; i < collisionIndices.Length; i++)
            {
                foreach (int j in collisionIndices[i])
                {
                    Vector3d move = ptMesh.Vertices[j].ToPoint3d() - ptMesh.Vertices[i].ToPoint3d();
                    double currentDistance = move.Length;
                    if (currentDistance > CollisionDistance) continue;
                    move *= 0.5 * (currentDistance - CollisionDistance) / currentDistance;
                    //totalCollectionMove[i] += move;
                    //totalCollectionMove[j] -= move;
                    //CollectionCounts[i] += 1;
                    //CollectionCounts[j] += 1;

                    totalWeightedMoves[i] += move * CollisionWeight;
                    totalWeightedMoves[j] -= move * CollisionWeight;
                    totalWeights[i] += CollisionWeight;
                    totalWeights[j] += CollisionWeight;
                }
            }

            ////每个小球移动量等于总移动量除以碰撞次数
            //for (int i = 0; i < ptMesh.Vertices.Count; i++)
            //{
            //    if (CollectionCounts[i] != 0.0)
            //    {
            //        totalCollectionMove[i] /= CollectionCounts[i];         //移动速度

            //        totalWeightedMoves[i] += totalCollectionMove[i] * CollisionWeight;
            //        totalWeights[i] += CollisionWeight;   //增加权重写法
            //    }
            //}
        }

        private void ProcessBendingResistance()
        {
            int halfedgeCount = ptMesh.Halfedges.Count;
            for (int k = 0; k < halfedgeCount; k += 2)
            {
                // Skip if this edge is naked
                if (ptMesh.Halfedges[k].AdjacentFace == -1 || ptMesh.Halfedges[k + 1].AdjacentFace == -1) continue;

                int i = ptMesh.Halfedges[k].StartVertex;
                int j = ptMesh.Halfedges[k + 1].StartVertex;
                int p = ptMesh.Halfedges[ptMesh.Halfedges[k].PrevHalfedge].StartVertex;
                int q = ptMesh.Halfedges[ptMesh.Halfedges[k + 1].PrevHalfedge].StartVertex;

                Point3d vI = ptMesh.Vertices[i].ToPoint3d();
                Point3d vJ = ptMesh.Vertices[j].ToPoint3d();
                Point3d vP = ptMesh.Vertices[p].ToPoint3d();
                Point3d vQ = ptMesh.Vertices[q].ToPoint3d();

                Vector3d nP = Vector3d.CrossProduct(vJ - vI, vP - vI);
                Vector3d nQ = Vector3d.CrossProduct(vQ - vI, vJ - vI);

                Vector3d planeNormal = (nP + nQ);
                planeNormal.Unitize();

                Point3d planeOrigin = 0.25 * (vI + vJ + vP + vQ);
                Plane plane = new Plane(planeOrigin, planeNormal);
                totalWeightedMoves[i] += BendingResistanceWeight * (plane.ClosestPoint(vI) - vI);
                totalWeightedMoves[j] += BendingResistanceWeight * (plane.ClosestPoint(vJ) - vJ);
                totalWeightedMoves[p] += BendingResistanceWeight * (plane.ClosestPoint(vP) - vP);
                totalWeightedMoves[q] += BendingResistanceWeight * (plane.ClosestPoint(vQ) - vQ);
                totalWeights[i] += BendingResistanceWeight;
                totalWeights[j] += BendingResistanceWeight;
                totalWeights[p] += BendingResistanceWeight;
                totalWeights[q] += BendingResistanceWeight;
            }
        }

        private void UpdateVertexPositionsAndVelicities()
        {
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                if (totalWeights[i] == 0) continue;
                PlanktonVertex vertex = ptMesh.Vertices[i];
                Vector3d move = totalWeightedMoves[i] / totalWeights[i];
                ptMesh.Vertices.SetVertex(i, vertex.X + move.X, vertex.Y + move.Y, vertex.Z + move.Z);
            }
        }

        bool[] faceDead;
        public void flipEdgeControl()
        {
            faceDead = new bool[ptMesh.Faces.Count];
            for (int i = 0; i < faceDead.Length; i++)
            {
                faceDead[i] = false;
            }

            Mesh mesh1 = ptMesh.ToRhinoMesh();
            ptMesh = mesh1.ToPlanktonMesh();

            for (int i = 0; i < ptMesh.Faces.Count; i++)
            {
                if (!faceDead[i])
                {
                    int[] fhes = ptMesh.Faces.GetHalfedges(i);
                    int id = -1;
                    double Max = 0;
                    for (int j = 0; j < fhes.Length; j++)
                    {
                        double length = ptMesh.Halfedges.GetLength(fhes[j]);
                        if (length > Max)
                        {
                            Max = length;
                            id = fhes[j];
                        }
                    }
                    PlanktonHalfedge he = ptMesh.Halfedges[id];
                    if (!ptMesh.Halfedges.IsBoundary(getIndex(ptMesh, he)) && !ptMesh.Halfedges.IsBoundary(ptMesh.Halfedges.GetPairHalfedge(getIndex(ptMesh, he))))
                    {
                        int pairf = getPair(ptMesh, he).AdjacentFace;

                        double angle1 = getHalfEdgeAngle(ptMesh, he);    //半边对应角度
                        double angle2 = getHalfEdgeAngle(ptMesh, getPair(ptMesh, he));     //半边对边对应角度
                                                                                           //double angle3 = getHalfEdgeAngle(mesh, mesh.Halfedges[getPair(mesh, he).NextHalfedge]);
                                                                                           //double angle4 = getHalfEdgeAngle(mesh, mesh.Halfedges[getPair(mesh, he).PrevHalfedge]);


                        //判断半边是否是边界

                        if (angle1 > 0.5 * Math.PI && angle2 > 0.5 * Math.PI)
                        {
                            FlipEdge(ptMesh, he);

                        }
                        faceDead[pairf] = true;
                    }

                    faceDead[i] = true;
                }
            }

            Mesh mesh2 = ptMesh.ToRhinoMesh();
            ptMesh = mesh2.ToPlanktonMesh();
        }
        public double getHalfEdgeAngle(PlanktonMesh mesh, PlanktonHalfedge _he)
        {
            Vector3d v1 = ToVector3d(mesh.Vertices[mesh.Halfedges[_he.NextHalfedge].StartVertex].ToPoint3d());  //下一条半边顶点
            Vector3d v2 = ToVector3d(mesh.Vertices[mesh.Halfedges.EndVertex(_he.NextHalfedge)].ToPoint3d());  //下一条半边终点

            Vector3d v3 = ToVector3d(mesh.Vertices[mesh.Halfedges[_he.PrevHalfedge].StartVertex].ToPoint3d());  //下下一条半边顶点
            Vector3d v4 = ToVector3d(mesh.Vertices[mesh.Halfedges.EndVertex(_he.PrevHalfedge)].ToPoint3d());  //下下一条半边终点

            Vector3d vP = Vector3d.Subtract(v1, v2);
            Vector3d vQ = Vector3d.Subtract(v4, v3);
            double angle = Vector3d.VectorAngle(vP, vQ);    //半边对应角度

            return angle;
        }
        int v0Index;
        int v1Index;
        int v2Index;
        int v3Index;
        int f0Index;
        int f1Index;
        public void FlipEdge(PlanktonMesh mesh, PlanktonHalfedge h0)
        {

            PlanktonHalfedge h1 = mesh.Halfedges[h0.NextHalfedge];
            PlanktonHalfedge h2 = mesh.Halfedges[h1.NextHalfedge];
            PlanktonHalfedge h3 = getPair(mesh, h0);
            PlanktonHalfedge h4 = mesh.Halfedges[h3.NextHalfedge];
            PlanktonHalfedge h5 = mesh.Halfedges[h4.NextHalfedge];
            PlanktonHalfedge h6 = getPair(mesh, h1);
            PlanktonHalfedge h7 = getPair(mesh, h2);
            PlanktonHalfedge h8 = getPair(mesh, h4);
            PlanktonHalfedge h9 = getPair(mesh, h5);

            PlanktonVertex v0 = mesh.Vertices[h0.StartVertex];
            PlanktonVertex v1 = mesh.Vertices[h3.StartVertex];
            PlanktonVertex v2 = mesh.Vertices[h8.StartVertex];
            PlanktonVertex v3 = mesh.Vertices[h6.StartVertex];

            f0Index = h0.AdjacentFace;
            f1Index = h3.AdjacentFace;
            PlanktonFace f0 = mesh.Faces[f0Index];
            PlanktonFace f1 = mesh.Faces[f1Index];

            v0Index = h0.StartVertex;
            v1Index = h3.StartVertex;
            v2Index = h8.StartVertex;
            v3Index = h6.StartVertex;

            h0.StartVertex = v2Index;
            h3.StartVertex = v3Index;

            h5.StartVertex = v2Index;
            h4.StartVertex = v0Index;
            h2.StartVertex = v3Index;
            h1.StartVertex = v1Index;

            h6.StartVertex = v3Index;
            h9.StartVertex = v1Index;
            h8.StartVertex = v2Index;
            h7.StartVertex = v0Index;

            h0.NextHalfedge = getIndex(mesh, h2);
            h2.NextHalfedge = getIndex(mesh, h4);
            h4.NextHalfedge = getIndex(mesh, h0);

            h1.NextHalfedge = getIndex(mesh, h3);
            h3.NextHalfedge = getIndex(mesh, h5);
            h5.NextHalfedge = getIndex(mesh, h1);

            h4.AdjacentFace = f0Index;
            //h0.AdjacentFace = h0.AdjacentFace;
            h2.AdjacentFace = f0Index;

            h1.AdjacentFace = f1Index;
            //h3.AdjacentFace = h3.AdjacentFace;
            h5.AdjacentFace = f1Index;

            f0.FirstHalfedge = getIndex(mesh, h0);
            f1.FirstHalfedge = getIndex(mesh, h3);

            v0.OutgoingHalfedge = getIndex(mesh, h7);
            v1.OutgoingHalfedge = getIndex(mesh, h9);
            v2.OutgoingHalfedge = getIndex(mesh, h8);
            v3.OutgoingHalfedge = getIndex(mesh, h6);

        }
        public Vector3d ToVector3d(Point3d p)
        {
            Vector3d v = new Vector3d(p.X, p.Y, p.Z);
            return v;
        }
        public int getIndex(PlanktonMesh mesh, PlanktonVertex v)
        {
            int index = -1;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                if (mesh.Vertices[i].Equals(v))
                {
                    index = i;
                }
            }
            return index;
        }
        public int getIndex(PlanktonMesh mesh, PlanktonHalfedge edge)
        {
            int index = -1;
            for (int i = 0; i < mesh.Halfedges.Count; i++)
            {
                if (mesh.Halfedges[i].Equals(edge))
                {
                    index = i;
                }
            }
            return index;

        }

        public List<double> HeatGeodesicDis()
        {
            Rhino.Geometry.Mesh rmesh = ptMesh.ToRhinoMesh();
            Geometry g = new Geometry(rmesh);

            List<int> Indexes = new List<int>();
            foreach(Vertex v in g.vertices)
            {
                if (v.onBorder) {
                    Indexes.Add(v.index);
                }
            }
            double tFactor = 1;
            int source = 2;
            int manPos = 2;
            Settings.tFactor = tFactor;
            Settings.defaultSource = new List<int>();
           
            Settings.defaultSource.AddRange(Indexes);
            Settings.initialManPos = manPos;
            

            HeatGeodesics hg = new HeatGeodesics(g, Settings.useCholesky, Settings.useAccurateMultisource);

            hg.Initialize(true);

            List<Vertex> sources = new List<Vertex>();
            foreach (int ind in Settings.defaultSource)
            {
                sources.Add(g.vertices[ind]);
            }
            hg.CalculateGeodesics(sources, true);

            return hg.phi.ToList();

        }

        public List<double> GetGeodesic()
        {

            GeodesicDistance = new List<double>(HeatGeodesicDis());
            return GeodesicDistance;
        }
        public PlanktonHalfedge getPair(PlanktonMesh mesh, PlanktonHalfedge edge)
        {
            PlanktonHalfedge pair = mesh.Halfedges[mesh.Halfedges.GetPairHalfedge(getIndex(mesh, edge))];
            return pair;
        }


 
        public List<int> getAllBoundaryHalfedges()
        {
            List<int> boundsHe = new List<int>();
            for (int i = 0; i < ptMesh.Halfedges.Count; i++)
            {
                if (ptMesh.Halfedges[i].AdjacentFace < 0)
                {
                    boundsHe.Add(i);
                }
            }
            return boundsHe;
        }

        public void ControlEdgeAngle()
        {
            foreach (int index in getAllBoundaryHalfedges())
            {      
                PlanktonHalfedge he = ptMesh.Halfedges[index];
                PlanktonHalfedge henext = null;
                int nextIndex = -1;
                foreach (int n_index in ptMesh.Vertices.GetHalfedges(ptMesh.Halfedges[index].StartVertex))
                {
                    if (ptMesh.Halfedges[ptMesh.Halfedges.GetPairHalfedge(n_index)].AdjacentFace < 0)
                    {
                       
                        henext = ptMesh.Halfedges[n_index];
                        nextIndex = n_index;

                    }
                }

                
                Vector3d v1 = ToVector3d(ptMesh.Vertices[he.StartVertex].ToPoint3d());
                Vector3d v2 = ToVector3d(ptMesh.Vertices[ptMesh.Halfedges.EndVertex(index)].ToPoint3d());

                Vector3d v3 = ToVector3d(ptMesh.Vertices[ptMesh.Halfedges.EndVertex(nextIndex)].ToPoint3d());

                Vector3d vP = Vector3d.Subtract(v2, v1);
                Vector3d vQ = Vector3d.Subtract(v3, v1);
                double angle1 = Vector3d.VectorAngle(vP, vQ);
                if (angle1 < Math.PI)
                {

                    Vector3d move = Vector3d.Add(vP, vQ);
                    move = Vector3d.Multiply(move, 0.5 * (Math.PI - angle1) / angle1);

                    totalWeightedMoves[he.StartVertex] += move * BoundaryAngleWeight;                
                    totalWeights[he.StartVertex] += BoundaryAngleWeight;

                }

               

            }
        }
        // </Custom additional code> 
    }
}

