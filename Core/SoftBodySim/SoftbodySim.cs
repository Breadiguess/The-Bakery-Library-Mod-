using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public sealed class SoftbodySim
    {
       

        public Material Mat = new Material();

        // ---------- Data ----------
        public struct Node
        {
            public Vector2 Pos;
            public Vector2 PrevPos;
            public Vector2 Accel;     // external forces accumulate here each tick
            public float InvMass;     // 0 = pinned
            public float Radius;      // collision radius in pixels
        }
        public struct AttachmentConstraint
        {
            public int Node;
            public Func<Vector2> Target;
            public float Stiffness;
        }
        // “Type tags” so the same solver can host multiple constraint sets.
        public enum ConstraintKind : byte { Structural, Bend, Area }

        //todo: ACTUALLY FUCKING USE ME YOU BITCH
        public struct DistanceConstraint
        {
            public int A, B;
            public float RestLength;
            public float Stiffness;   // 0..1 (per-constraint)
            public ConstraintKind Kind;
        }

        public struct AreaConstraint
        {
            public int A, B, C;
            public float RestArea;
            public float Stiffness;  
        }

        public readonly List<Node> Nodes = new();
        public readonly List<DistanceConstraint> Dist = new();
        public readonly List<AreaConstraint> Areas = new();
        public readonly List<AttachmentConstraint> Attachments = new();
        public Vector2 Gravity = new(0f, 0.35f);
        public float Dt = 1f;
        public float Pressure = 0f;
        public float Viscosity = 0.1f;
        #region Public stuff
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

        public void PinNode(int index, Vector2 pos)
        {
            var n = Nodes[index];
            n.InvMass = 0f;
            n.Pos = pos;
            n.PrevPos = pos;
            Nodes[index] = n;
        }

        public void AddForce(int index, Vector2 force)
        {
            var n = Nodes[index];
            if (n.InvMass > 0f)
                n.Accel += force * n.InvMass; // F = ma -> a += F * invMass
            Nodes[index] = n;
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

        public void AddArea(int a, int b, int c, float stiffness)
        {
            float area = SignedTriangleArea(Nodes[a].Pos, Nodes[b].Pos, Nodes[c].Pos);
            Areas.Add(new AreaConstraint
            {
                A = a,
                B = b,
                C = c,
                RestArea = area,
                Stiffness = MathHelper.Clamp(stiffness, 0f, 1f),
            });
        }
        private void SolveAttachments()
        {
            for (int i = 0; i < Attachments.Count; i++)
            {
                var a = Attachments[i];

                ref Node n = ref NodesRef(a.Node);
                if (n.InvMass <= 0f)
                    continue;

                Vector2 target = a.Target();

                Vector2 delta = n.Pos - target;
                float dist = delta.Length();
                if (dist < 1e-4f)
                    continue;

                float s = MathHelper.Clamp(a.Stiffness, 0f, 1f);

                n.Pos -= delta * s;
            }
        }
        #endregion

        #region Update
        public void Step()
        {
            Integrate();

            SolveViscosity();

            int iters = Math.Max(1, Mat.Iterations);
            for (int i = 0; i < iters; i++)
            {
                SolveDistanceConstraints();
                SolveAreaConstraints();
                SolvePressure();
                SolveAttachments();
                CollideAll();
            }
        }

        private void Integrate()
        {
            Vector2 g = Gravity * Mat.GravityScale;
            float dt2 = Dt * Dt;

            for (int i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                if (n.InvMass <= 0f)
                {
                    n.Accel = Vector2.Zero;
                    Nodes[i] = n;
                    continue;
                }

                Vector2 vel = (n.Pos - n.PrevPos) * Mat.Damping;
                Vector2 oldPos = n.Pos;

                n.Pos += vel + (n.Accel + g) * dt2;
                n.PrevPos = oldPos;
                n.Accel = Vector2.Zero;

                Nodes[i] = n;
            }
        }

        private void SolveDistanceConstraints()
        {
            for (int i = 0; i < Dist.Count; i++)
            {
                var c = Dist[i];

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
                a.Pos += corr * (wA / wSum);
                b.Pos -= corr * (wB / wSum);
            }
        }

        private void SolveAreaConstraints()
        {
            float s = Mat.AreaStiffness;
            if (s <= 0f)
                return;

            for (int i = 0; i < Areas.Count; i++)
            {
                var c = Areas[i];

                ref Node a = ref NodesRef(c.A);
                ref Node b = ref NodesRef(c.B);
                ref Node d = ref NodesRef(c.C);

                float wA = a.InvMass;
                float wB = b.InvMass;
                float wC = d.InvMass;

                float wSum = wA + wB + wC;
                if (wSum <= 0f)
                    continue;

                Vector2 pa = a.Pos;
                Vector2 pb = b.Pos;
                Vector2 pc = d.Pos;

                float area = 0.5f * (
                    (pb.X - pa.X) * (pc.Y - pa.Y) -
                    (pb.Y - pa.Y) * (pc.X - pa.X)
                );

                float C = area - c.RestArea;
                if (Math.Abs(C) < 1e-4f)
                    continue;

                Vector2 gradA = 0.5f * new Vector2(pb.Y - pc.Y, pc.X - pb.X);
                Vector2 gradB = 0.5f * new Vector2(pc.Y - pa.Y, pa.X - pc.X);
                Vector2 gradC = 0.5f * new Vector2(pa.Y - pb.Y, pb.X - pa.X);

                float denom =
                    wA * gradA.LengthSquared() +
                    wB * gradB.LengthSquared() +
                    wC * gradC.LengthSquared();

                if (denom < 1e-6f)
                    continue;

                float lambda = (C / denom) * s;

                a.Pos -= lambda * wA * gradA;
                b.Pos -= lambda * wB * gradB;
                d.Pos -= lambda * wC * gradC;
            }
        }

        private void CollideAll()
        {
            for (int i = 0; i < Nodes.Count; i++)
                ProjectNodeOutOfTiles(i);
        }
        //kind of useless, actually
        private void SolvePressure()
        {
            if (Pressure <= 0f)
                return;

            for (int i = 0; i < Areas.Count; i++)
            {
                var c = Areas[i];

                ref Node a = ref NodesRef(c.A);
                ref Node b = ref NodesRef(c.B);
                ref Node d = ref NodesRef(c.C);

                float wA = a.InvMass;
                float wB = b.InvMass;
                float wC = d.InvMass;

                if (wA + wB + wC <= 0f)
                    continue;

                Vector2 pa = a.Pos;
                Vector2 pb = b.Pos;
                Vector2 pc = d.Pos;

                Vector2 centroid = (pa + pb + pc) / 3f;

                Vector2 na = pa - centroid;
                Vector2 nb = pb - centroid;
                Vector2 nc = pc - centroid;

                a.Pos += na * Pressure * wA;
                b.Pos += nb * Pressure * wB;
                d.Pos += nc * Pressure * wC;
            }
        }
        private void SolveViscosity()
        {
            float k = Viscosity;
            if (k <= 0f)
                return;

            for (int i = 0; i < Dist.Count; i++)
            {
                var c = Dist[i];

                ref Node a = ref NodesRef(c.A);
                ref Node b = ref NodesRef(c.B);

                if (a.InvMass <= 0f && b.InvMass <= 0f)
                    continue;

                Vector2 va = a.Pos - a.PrevPos;
                Vector2 vb = b.Pos - b.PrevPos;

                Vector2 vdiff = vb - va;

                Vector2 impulse = vdiff * k * 0.5f;

                a.PrevPos += impulse;
                b.PrevPos -= impulse;
            }
        }
        #endregion

        #region collision and stuff
        private void ProjectNodeOutOfTiles(int nodeIndex)
        {
            var n = Nodes[nodeIndex];
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
                for (int ty = minTileY; ty <= maxTileY; ty++)
                {
                    Tile t = Main.tile[tx, ty];
                    if (t == null || !t.HasTile || t.IsActuated)
                        continue;

                    ushort type = t.TileType;

                    //just work ffs
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
                        d2 = 1f;
                    }

                    float d = (float)Math.Sqrt(d2);
                    float pen = r - d;
                    if (pen <= 0f)
                        continue;

                    Vector2 nrm = toCircle / d;
                    pos += nrm * pen;

                    Vector2 vel = n.Pos - n.PrevPos;
                    float vn = Vector2.Dot(vel, nrm);

                    if (vn < 0f)
                    {
                        vel -= vn * nrm;
                        n.PrevPos = n.Pos - vel;
                    }
                    Vector2 normalVel = Vector2.Dot(vel, nrm) * nrm;
                    Vector2 tangentVel = vel - normalVel;

                    float fr = MathHelper.Clamp(Mat.Friction, 0f, 1f);
                    float bo = MathHelper.Clamp(Mat.Bounce, 0f, 1f);

                    // remove normal velocity
                    vel = tangentVel * (1f - fr) - normalVel * bo;
                    prev = pos - vel;
                }

            n.Pos = pos;
            n.PrevPos = prev;
            Nodes[nodeIndex] = n;
        }

            private static bool CircleIntersectsAabb(Vector2 c, float r, Rectangle aabb)
            {
                float x = MathHelper.Clamp(c.X, aabb.Left, aabb.Right);
                float y = MathHelper.Clamp(c.Y, aabb.Top, aabb.Bottom);
                float dx = c.X - x;
                float dy = c.Y - y;
                return dx * dx + dy * dy <= r * r;
            }

        private static float SignedTriangleArea(Vector2 a, Vector2 b, Vector2 c)
            => 0.5f * ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));

        private ref Node NodesRef(int i) => ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Nodes)[i];
        #endregion
    }

}
