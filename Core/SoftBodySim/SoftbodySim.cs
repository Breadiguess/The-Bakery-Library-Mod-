using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;

namespace BreadLibrary.Core.SoftBodySim
{
    public sealed class SoftbodySim
    {
        public Material Mat = new();

        public struct Node
        {
            public Vector2 Pos;
            public Vector2 PrevPos;
            public Vector2 Accel;
            public float InvMass;
            public float Radius;
        }

        public struct AttachmentConstraint
        {
            public int Node;
            public Func<Vector2> Target;
            public float Stiffness;
        }

        public enum ConstraintKind : byte
        {
            Structural,
            Bend
        }

        public struct DistanceConstraint
        {
            public int A;
            public int B;
            public float RestLength;
            public float Stiffness;
            public ConstraintKind Kind;
        }

        public sealed class ShapeMatchingCluster
        {
            public int[] Indices = Array.Empty<int>();
            public Vector2[] RestLocal = Array.Empty<Vector2>();
            public float[] Weights = Array.Empty<float>();

            public Vector2 RestCenter;
            public float Stiffness = 0.25f;
            public bool Enabled = true;
        }

        public sealed class Material
        {
            public int Iterations = 6;

            public float GravityScale = 1f;
            public float Damping = 0.985f;

            public float StructuralStiffness = 0.35f;
            public float BendStiffness = 0.15f;
            public float ShapeMatchingStiffness = 0.25f;

            public float Friction = 0.15f;
            public float Bounce = 0f;

            public float PostCollisionVelocityDamping = 0.98f;
        }

        public readonly List<Node> Nodes = new();
        public readonly List<AttachmentConstraint> Attachments = new();
        public readonly List<DistanceConstraint> Dist = new();
        public readonly List<ShapeMatchingCluster> Clusters = new();

        public Vector2 Gravity = new(0f, 0.35f);
        public float Dt = 1f;

        public int AddNode(Vector2 pos, float mass, float radius, bool pinned = false)
        {
            float invMass = pinned || mass <= 0f ? 0f : 1f / mass;

            Nodes.Add(new Node
            {
                Pos = pos,
                PrevPos = pos,
                Accel = Vector2.Zero,
                InvMass = invMass,
                Radius = Math.Max(1f, radius)
            });

            return Nodes.Count - 1;
        }
        public ref Node GetNodeRef(int index)
        {
            return ref CollectionsMarshal.AsSpan(Nodes)[index];
        }
        public void PinNode(int index, Vector2 pos)
        {
            ref Node n = ref NodesRef(index);
            n.InvMass = 0f;
            n.Pos = pos;
            n.PrevPos = pos;
            n.Accel = Vector2.Zero;
        }

        public void SetNodeMass(int index, float mass)
        {
            ref Node n = ref NodesRef(index);
            n.InvMass = mass <= 0f ? 0f : 1f / mass;
        }

        public void AddForce(int index, Vector2 force)
        {
            ref Node n = ref NodesRef(index);
            if (n.InvMass > 0f)
                n.Accel += force * n.InvMass;
        }

        public void AddLink(int a, int b, float stiffness, ConstraintKind kind)
        {
            float rest = Vector2.Distance(Nodes[a].Pos, Nodes[b].Pos);

            Dist.Add(new DistanceConstraint
            {
                A = a,
                B = b,
                RestLength = rest,
                Stiffness = MathHelper.Clamp(stiffness, 0f, 1f),
                Kind = kind
            });
        }
        public Rectangle GetWorldAabb()
        {
            if (Nodes.Count == 0)
                return Rectangle.Empty;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                minX = System.MathF.Min(minX, n.Pos.X - n.Radius);
                minY = System.MathF.Min(minY, n.Pos.Y - n.Radius);
                maxX = System.MathF.Max(maxX, n.Pos.X + n.Radius);
                maxY = System.MathF.Max(maxY, n.Pos.Y + n.Radius);
            }

            return new Rectangle(
                (int)minX,
                (int)minY,
                (int)(maxX - minX),
                (int)(maxY - minY)
            );
        }

        public ShapeMatchingCluster AddCluster(ReadOnlySpan<int> indices, float stiffness = 0.25f)
        {
            if (indices.Length == 0)
                throw new ArgumentException("Cluster must contain at least one node.", nameof(indices));

            ShapeMatchingCluster cluster = new()
            {
                Indices = indices.ToArray(),
                RestLocal = new Vector2[indices.Length],
                Weights = new float[indices.Length],
                Stiffness = MathHelper.Clamp(stiffness, 0f, 1f)
            };

            Vector2 restCenter = Vector2.Zero;
            float totalMass = 0f;

            for (int i = 0; i < indices.Length; i++)
            {
                int nodeIndex = indices[i];
                Node n = Nodes[nodeIndex];

                float mass = n.InvMass > 0f ? 1f / n.InvMass : 1f;
                cluster.Weights[i] = mass;

                restCenter += n.Pos * mass;
                totalMass += mass;
            }

            if (totalMass <= 1e-6f)
                totalMass = 1f;

            restCenter /= totalMass;
            cluster.RestCenter = restCenter;

            for (int i = 0; i < indices.Length; i++)
                cluster.RestLocal[i] = Nodes[indices[i]].Pos - restCenter;

            Clusters.Add(cluster);
            return cluster;
        }

        public void RebuildClusterRestShape(ShapeMatchingCluster cluster)
        {
            if (cluster == null || cluster.Indices.Length == 0)
                return;

            Vector2 restCenter = Vector2.Zero;
            float totalMass = 0f;

            for (int i = 0; i < cluster.Indices.Length; i++)
            {
                int nodeIndex = cluster.Indices[i];
                Node n = Nodes[nodeIndex];

                float mass = n.InvMass > 0f ? 1f / n.InvMass : 1f;
                cluster.Weights[i] = mass;

                restCenter += n.Pos * mass;
                totalMass += mass;
            }

            if (totalMass <= 1e-6f)
                totalMass = 1f;

            restCenter /= totalMass;
            cluster.RestCenter = restCenter;

            for (int i = 0; i < cluster.Indices.Length; i++)
                cluster.RestLocal[i] = Nodes[cluster.Indices[i]].Pos - restCenter;
        }
        public void BeginStep()
        {
            Integrate();
        }

        public void SolveInternalConstraints()
        {
            SolveShapeMatchingClusters();
            SolveDistanceConstraints();
            SolveAttachments();
        }

        public void SolveTileContacts()
        {
            CollideAll();
        }

        public void EndStep()
        {
            DampPostCollisionVelocities();
        }

        public void Step()
        {
            BeginStep();

            int iters = Math.Max(1, Mat.Iterations);
            for (int i = 0; i < iters; i++)
            {
                SolveInternalConstraints();
                SolveTileContacts();
            }

            EndStep();
        }
      

        private void Integrate()
        {
            Vector2 g = Gravity * Mat.GravityScale;
            float dt2 = Dt * Dt;

            for (int i = 0; i < Nodes.Count; i++)
            {
                ref Node n = ref NodesRef(i);

                if (n.InvMass <= 0f)
                {
                    n.Accel = Vector2.Zero;
                    continue;
                }

                Vector2 vel = (n.Pos - n.PrevPos) * Mat.Damping;
                Vector2 oldPos = n.Pos;

                n.Pos += vel + (n.Accel + g) * dt2;
                n.PrevPos = oldPos;
                n.Accel = Vector2.Zero;
            }
        }

        private void SolveShapeMatchingClusters()
        {
            for (int i = 0; i < Clusters.Count; i++)
            {
                ShapeMatchingCluster cluster = Clusters[i];
                if (cluster == null || !cluster.Enabled || cluster.Indices.Length == 0)
                    continue;

                SolveShapeMatchingCluster(cluster);
            }
        }

        private void SolveShapeMatchingCluster(ShapeMatchingCluster cluster)
        {
            float totalMass = 0f;
            Vector2 currentCenter = Vector2.Zero;

            for (int i = 0; i < cluster.Indices.Length; i++)
            {
                ref Node n = ref NodesRef(cluster.Indices[i]);

                float mass = n.InvMass > 0f ? 1f / n.InvMass : 1f;
                currentCenter += n.Pos * mass;
                totalMass += mass;
            }

            if (totalMass <= 1e-6f)
                return;

            currentCenter /= totalMass;

            float a = 0f;
            float b = 0f;

            for (int i = 0; i < cluster.Indices.Length; i++)
            {
                ref Node n = ref NodesRef(cluster.Indices[i]);

                float mass = n.InvMass > 0f ? 1f / n.InvMass : 1f;
                Vector2 p = n.Pos - currentCenter;
                Vector2 q = cluster.RestLocal[i];

                a += mass * Vector2.Dot(p, q);
                b += mass * Cross(q, p);
            }

            float angle = MathF.Atan2(b, a);
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            float stiffness = MathHelper.Clamp(cluster.Stiffness * Mat.ShapeMatchingStiffness, 0f, 1f);

            for (int i = 0; i < cluster.Indices.Length; i++)
            {
                ref Node n = ref NodesRef(cluster.Indices[i]);
                if (n.InvMass <= 0f)
                    continue;

                Vector2 q = cluster.RestLocal[i];
                Vector2 goal = currentCenter + Rotate(q, cos, sin);

                n.Pos += (goal - n.Pos) * stiffness;
            }
        }

        private void SolveDistanceConstraints()
        {
            for (int i = 0; i < Dist.Count; i++)
            {
                DistanceConstraint c = Dist[i];

                ref Node a = ref NodesRef(c.A);
                ref Node b = ref NodesRef(c.B);

                Vector2 delta = b.Pos - a.Pos;
                float dist = delta.Length();
                if (dist < 1e-6f)
                    continue;

                float wA = a.InvMass;
                float wB = b.InvMass;
                float wSum = wA + wB;
                if (wSum <= 0f)
                    continue;

                float kindMul = c.Kind switch
                {
                    ConstraintKind.Structural => Mat.StructuralStiffness,
                    ConstraintKind.Bend => Mat.BendStiffness,
                    _ => 1f
                };

                float s = MathHelper.Clamp(c.Stiffness * kindMul, 0f, 1f);
                float diff = (dist - c.RestLength) / dist;

                Vector2 corr = delta * (s * diff);

                if (wA > 0f)
                    a.Pos += corr * (wA / wSum);
                if (wB > 0f)
                    b.Pos -= corr * (wB / wSum);
            }
        }

        private void SolveAttachments()
        {
            for (int i = 0; i < Attachments.Count; i++)
            {
                AttachmentConstraint a = Attachments[i];

                ref Node n = ref NodesRef(a.Node);
                if (n.InvMass <= 0f)
                    continue;

                Vector2 target = a.Target();
                float s = MathHelper.Clamp(a.Stiffness, 0f, 1f);

                n.Pos += (target - n.Pos) * s;
            }
        }

        private void CollideAll()
        {
            for (int i = 0; i < Nodes.Count; i++)
                ProjectNodeOutOfTiles(i);
        }

        private void DampPostCollisionVelocities()
        {
            float d = MathHelper.Clamp(Mat.PostCollisionVelocityDamping, 0f, 1f);
            if (d >= 0.9999f)
                return;

            for (int i = 0; i < Nodes.Count; i++)
            {
                ref Node n = ref NodesRef(i);
                if (n.InvMass <= 0f)
                    continue;

                Vector2 v = n.Pos - n.PrevPos;
                v *= d;
                n.PrevPos = n.Pos - v;
            }
        }

        private void ProjectNodeOutOfTiles(int nodeIndex)
        {
            ref Node n = ref NodesRef(nodeIndex);
            if (n.InvMass <= 0f)
                return;

            float r = n.Radius;
            Rectangle nodeAabb = new(
                (int)(n.Pos.X - r),
                (int)(n.Pos.Y - r),
                (int)(r * 2f),
                (int)(r * 2f)
            );

            int minTileX = Utils.Clamp(nodeAabb.Left / 16 - 1, 0, Main.maxTilesX - 1);
            int maxTileX = Utils.Clamp(nodeAabb.Right / 16 + 1, 0, Main.maxTilesX - 1);
            int minTileY = Utils.Clamp(nodeAabb.Top / 16 - 1, 0, Main.maxTilesY - 1);
            int maxTileY = Utils.Clamp(nodeAabb.Bottom / 16 + 1, 0, Main.maxTilesY - 1);

            Vector2 pos = n.Pos;
            Vector2 prev = n.PrevPos;

            for (int tx = minTileX; tx <= maxTileX; tx++)
            {
                for (int ty = minTileY; ty <= maxTileY; ty++)
                {
                    Tile t = Main.tile[tx, ty];
                    if (t == null || !t.HasTile || t.IsActuated)
                        continue;

                    ushort type = t.TileType;
                    if (!Main.tileSolid[type] || Main.tileSolidTop[type])
                        continue;

                    Rectangle tileRect = new(tx * 16, ty * 16, 16, 16);
                    if (!CircleIntersectsAabb(pos, r, tileRect))
                        continue;

                    Vector2 nearest = new(
                        MathHelper.Clamp(pos.X, tileRect.Left, tileRect.Right),
                        MathHelper.Clamp(pos.Y, tileRect.Top, tileRect.Bottom)
                    );

                    Vector2 toCircle = pos - nearest;
                    float d2 = toCircle.LengthSquared();

                    if (d2 < 1e-6f)
                    {
                        Vector2 motion = pos - prev;
                        if (Math.Abs(motion.X) > Math.Abs(motion.Y))
                            toCircle = new Vector2(Math.Sign(motion.X), 0f);
                        else
                            toCircle = new Vector2(0f, Math.Sign(motion.Y));

                        if (toCircle == Vector2.Zero)
                            toCircle = -Vector2.UnitY;

                        d2 = 1f;
                    }

                    float d = MathF.Sqrt(d2);
                    float pen = r - d;
                    if (pen <= 0f)
                        continue;

                    Vector2 nrm = toCircle / d;
                    pos += nrm * pen;

                    Vector2 vel = pos - prev;
                    float vn = Vector2.Dot(vel, nrm);

                    Vector2 normalVel = vn * nrm;
                    Vector2 tangentVel = vel - normalVel;

                    float fr = MathHelper.Clamp(Mat.Friction, 0f, 1f);
                    float bo = MathHelper.Clamp(Mat.Bounce, 0f, 1f);

                    if (vn < 0f)
                        vel = tangentVel * (1f - fr) - normalVel * bo;
                    else
                        vel = tangentVel * (1f - fr) + normalVel;

                    prev = pos - vel;
                }
            }

            n.Pos = pos;
            n.PrevPos = prev;
        }

        public void BuildLoopLinks(IReadOnlyList<int> loopIndices, float structuralStiffness = 1f, bool addBendLinks = true, int bendStride = 2)
        {
            if (loopIndices == null || loopIndices.Count < 2)
                return;

            for (int i = 0; i < loopIndices.Count; i++)
            {
                int a = loopIndices[i];
                int b = loopIndices[(i + 1) % loopIndices.Count];
                AddLink(a, b, structuralStiffness, ConstraintKind.Structural);
            }

            if (!addBendLinks || loopIndices.Count < 4)
                return;

            bendStride = Math.Max(2, bendStride);
            for (int i = 0; i < loopIndices.Count; i++)
            {
                int a = loopIndices[i];
                int b = loopIndices[(i + bendStride) % loopIndices.Count];
                AddLink(a, b, 1f, ConstraintKind.Bend);
            }
        }

        public void BuildChainClusters(IReadOnlyList<int> orderedIndices, int clusterSize, int stride, float stiffness)
        {
            if (orderedIndices == null || orderedIndices.Count == 0)
                return;

            clusterSize = Math.Max(2, clusterSize);
            stride = Math.Max(1, stride);

            for (int start = 0; start + clusterSize <= orderedIndices.Count; start += stride)
            {
                int[] subset = new int[clusterSize];
                for (int i = 0; i < clusterSize; i++)
                    subset[i] = orderedIndices[start + i];

                AddCluster(subset, stiffness);
            }
        }

        private static float Cross(Vector2 a, Vector2 b)
            => a.X * b.Y - a.Y * b.X;

        private static Vector2 Rotate(Vector2 v, float cos, float sin)
            => new(
                cos * v.X - sin * v.Y,
                sin * v.X + cos * v.Y
            );

        private static bool CircleIntersectsAabb(Vector2 c, float r, Rectangle aabb)
        {
            float x = MathHelper.Clamp(c.X, aabb.Left, aabb.Right);
            float y = MathHelper.Clamp(c.Y, aabb.Top, aabb.Bottom);
            float dx = c.X - x;
            float dy = c.Y - y;
            return dx * dx + dy * dy <= r * r;
        }

        private ref Node NodesRef(int i) => ref CollectionsMarshal.AsSpan(Nodes)[i];
    }
}