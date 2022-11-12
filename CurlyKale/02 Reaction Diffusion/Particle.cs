using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurlyKale
{
    /*
     * LJJ修改于2022.8.7
     * 获取顶点的相连顶点采用了topology方法，不用遍历每个顶点，加快了运算速度
     * 采用了多线程写法，加快了运算速度
     * */
    public class Particle
    {

        public double A  //记录A值
        {
            get; set;
        }
        public double B  //记录B值
        {
            get; set;
        }

        Point3d point;
        double LaPlaceA, LaPlaceB;
        double dA, dB;   //A、B扩散率
        double feed_rate, kill_rate;   //A添加率与B杀死率
        double dt;  //反应步长一般取1

        List<Particle> neighbours = new List<Particle>();   //记录每个点的拓扑关系
        List<double> weights = new List<double>();
        double weightTotal;
        ReactionDiffusionOnMeshSystem simulation;

        public Particle(double A_, double B_,double dA_, double dB_, double feed_rate_,double kill_rate_,double dt_, Point3d point_, ReactionDiffusionOnMeshSystem simulation_)
        {
            A = A_;
            B = B_;
            dA = dA_;
            dB = dB_;
            feed_rate = feed_rate_;
            kill_rate = kill_rate_;
            dt = dt_;
            point = point_;
            simulation = simulation_;   
        }

        public void SetNeighboursWithTangent(int i, Mesh mesh, Vector3d tangent)
        {
            //foreach (int j in mesh.Vertices.GetConnectedVertices(i).Where(x => x != i))
            //{
            //    neighbours.Add(simulation.particles[j]);

            //    double angle = Vector3d.VectorAngle(simulation.particles[j].point - point, tangent);
            //    double t = simulation.dirFactor;
            //    double weight = Math.Sqrt(Math.Sin(angle) * Math.Sin(angle) + t * t * Math.Cos(angle) * Math.Cos(angle));

            //    weights.Add(weight);
            //    weightTotal += weight;
            //}


            //采用了topology的写法，比起直接用GetConnectedVertices的写法快了很多
            int n_c = mesh.TopologyVertices.ConnectedTopologyVertices(mesh.TopologyVertices.TopologyVertexIndex(i)).Length;
            for (int i_c = 0; i_c < n_c; i_c++)
            {
                int index = mesh.TopologyVertices.MeshVertexIndices(mesh.TopologyVertices.ConnectedTopologyVertices(mesh.TopologyVertices.TopologyVertexIndex(i))[i_c])[0];
                neighbours.Add(simulation.particles[index]);

                double angle = Vector3d.VectorAngle(simulation.particles[index].point - point, tangent);
                double t = simulation.dirFactor;
                //计算每个点的权重
                double weight = Math.Sqrt(Math.Sin(angle) * Math.Sin(angle) + t * t * Math.Cos(angle) * Math.Cos(angle));

                //另一种写法公式
                //double angle = Vector3d.VectorAngle(simulation.particles[j].point - point, tangent);
                //angle = Math.Abs(0.5 - angle / Math.PI);
                //double t = Constrain(simulation.dirFactor);
                //double weight = angle * t + 0.5 * (1 - t);
                weights.Add(weight);
                weightTotal += weight;
            }
        }

        public void SetNeighbours(int i, Mesh mesh)
        {
            //foreach (int j in mesh.Vertices.GetConnectedVertices(i).Where(x => x != i))
            //{
            //    neighbours.Add(simulation.particles[j]);
            //}

            //采用了topology的写法，比起直接用GetConnectedVertices的写法快了很多
            int n_c = mesh.TopologyVertices.ConnectedTopologyVertices(mesh.TopologyVertices.TopologyVertexIndex(i)).Length;
            for (int i_c = 0; i_c < n_c; i_c++)
            {
                int index = mesh.TopologyVertices.MeshVertexIndices(mesh.TopologyVertices.ConnectedTopologyVertices(mesh.TopologyVertices.TopologyVertexIndex(i))[i_c])[0];
                neighbours.Add(simulation.particles[index]);
            }
           
        }

        public void LaplacianWithWeight()
        {
            double nA = 0, nB = 0;
            for (int i = 0; i < neighbours.Count; i++)
            {
                nA += neighbours[i].A;
                nB += neighbours[i].B * weights[i];
            }
            nA /= neighbours.Count;
            nB /= weightTotal;

            LaPlaceA = nA - A;
            LaPlaceB = nB - B;
        }

        public void Laplacian()  //与方格矩阵的主要区别就在这一步骤上，方格矩阵是自身取-1，对角取0.05，四周取0.2，这里相连的点取1然后平均，自身取-1
        {
            double nA = 0, nB = 0;
            for (int i = 0; i < neighbours.Count; i++)
            {
                nA += neighbours[i].A;
                nB += neighbours[i].B;
            }
            nA /= neighbours.Count;
            nB /= neighbours.Count;

            LaPlaceA = nA - A;
            LaPlaceB = nB - B;
        }

        public void ReactionDiffusion()  //反应公式，原理见参考文献
        {
            double AB2 = A * B * B;
            A += dA * LaPlaceA - AB2 + feed_rate * (1 - A) * dt;
            B += dB * LaPlaceB + AB2 - (kill_rate + feed_rate) * B * dt;

            A = Constrain(A);
            B = Constrain(B);
        }

        public double Constrain(double val)
        {
            if (val < 0) return 0;
            else if (val > 1) return 1;
            return val;
        }

    }




}

