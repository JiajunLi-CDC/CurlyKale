using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
    public class ReactionDiffusionOnMeshSystem
    {
        Mesh origin_mesh;

        Random random = new Random(1);
        List<double> dA;  //A扩撒率
        List<double> dB;  //B扩散率
        List<double> feed_rate ;  //A给进速率
        List<double> kill_rate;   //B杀死速率
        double dt;  //反应步长，一般取值为1

        List<Vector3d> tangents;  //点沿干扰曲线的切线方向
        public double dirFactor;  //扩散方向

        public List<Particle> particles = new List<Particle>();


        //public List<double> outAValue;   //输出A值
        //public List<double> outBValue;   //输出B值
        public List<GH_Number> listA
        {
            get
            {
                return particles.Select(particle => new GH_Number(particle.A)).ToList();
            }
        }
        public List<GH_Number> listB
        {
            get
            {
                return particles.Select(particle => new GH_Number(particle.B)).ToList();
            }
        }

        public ReactionDiffusionOnMeshSystem(Mesh origin_mesh_, List<double> dA_, List<double> dB_, List<double> feed_rate_, List<double> kill_rate_,double dt_)
        {
            origin_mesh = origin_mesh_;
            dA = dA_;
            dB = dB_;
            feed_rate = feed_rate_;
            kill_rate = kill_rate_;
            dt = dt_;

            int size = origin_mesh_.Vertices.Count;
            for (int i = 0; i < size; i++)
            {
                double a = 1;
                double b = (random.NextDouble() < 0.05) ? 1 : 0;
                particles.Add(new Particle(a, b, dA[i], dB[i], feed_rate[i], kill_rate[i], dt , origin_mesh.Vertices[i], this));
            }

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, size, i => particles[i].SetNeighbours(i, origin_mesh_));

        }

        public ReactionDiffusionOnMeshSystem(Mesh origin_mesh_, List<double> dA_, List<double> dB_, List<double> feed_rate_, List<double> kill_rate_, double dt_, List<Vector3d> tangents_, double dirFactor_)
        {
            origin_mesh = origin_mesh_;
            dA = dA_;
            dB = dB_;
            feed_rate = feed_rate_;
            kill_rate = kill_rate_;
            dt = dt_;
            tangents = tangents_;
            dirFactor = dirFactor_;

            int size = origin_mesh_.Vertices.Count;
            for (int i = 0; i < size; i++)
            {
                double a = 1;
                double b = (random.NextDouble() < 0.05) ? 1 : 0;
                particles.Add(new Particle(a, b, dA[i], dB[i], feed_rate[i], kill_rate[i], dt, origin_mesh.Vertices[i], this));
            }

            //多线程写法
            System.Threading.Tasks.Parallel.For(0, size, i => particles[i].SetNeighboursWithTangent(i, origin_mesh,tangents[i]));

        }

        public void Reaction(int iterations)
        {
            while (iterations-- > 0)
            {
                System.Threading.Tasks.Parallel.ForEach(particles, particle => particle.Laplacian());
                System.Threading.Tasks.Parallel.ForEach(particles, particle => particle.ReactionDiffusion());
            }
        }

        public void ReactionWithDirection(int iterations)
        {
            while (iterations-- > 0)
            {
                System.Threading.Tasks.Parallel.ForEach(particles, particle => particle.LaplacianWithWeight());
                System.Threading.Tasks.Parallel.ForEach(particles, particle => particle.ReactionDiffusion());
            }
        }

        public DataTree<int> createTopology(Mesh originMesh_)
        {
            DataTree<int> verticesTopology_ = new DataTree<int>();

            for (int i_vertex = 0; i_vertex < originMesh_.Vertices.Count; i_vertex++)
            {
            
                int n_c = originMesh_.TopologyVertices.ConnectedTopologyVertices(originMesh_.TopologyVertices.TopologyVertexIndex(i_vertex)).Length;
                GH_Path pp = new GH_Path(i_vertex);
                for (int i_c = 0; i_c < n_c; i_c++)
                {
                    verticesTopology_.Add(originMesh_.TopologyVertices.MeshVertexIndices(originMesh_.TopologyVertices.ConnectedTopologyVertices(originMesh_.TopologyVertices.TopologyVertexIndex(i_vertex))[i_c])[0], pp);
                }
            }

            return verticesTopology_;
        }


    }

}