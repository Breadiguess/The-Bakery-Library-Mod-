namespace BreadLibrary.Core.SoftBodySim
{
    public sealed class SoftbodyCollisionSettings
    {
        public bool CollideWithTiles = true;
        public bool CollideWithSoftbodies = true;
        public bool CollideWithPlayers = false;
        public bool CollideWithNPCs = false;

        public bool IgnoreDriverEntity = true;

        // 0 = only move the softbody node.
        // 1 = try to move only the entity.
        public float PlayerPushFactor = 0f;
        public float NPCPushFactor = 0f;

        public float EntityFriction = 0.15f;
        public float EntityBounce = 0f;

        // Inflates the entity hitbox used for collision.
        public int EntityPadding = 0;

        public int CollisionLayer = 1;
        public int CollisionMask = ~0;
    }
    public class SoftbodyInstance
    {
        public readonly List<int> BoundaryNodes = new();
        public SoftbodyCollisionSettings Collision = new();

        public void UpdateDriverOnly()
        {
            UpdateDriver();
        }

        public void RefreshCenter()
        {
            Center = ComputeCenter();
            HasCenter = true;
        }
        public SoftbodySim Sim { get; private set; }

        public Vector2 Center { get; private set; }
        public bool HasCenter { get; private set; }

        public enum TransformDriverMode
        {
            Manual,
            EntityCenter
        }

        public TransformDriverMode DriverMode { get; set; } = TransformDriverMode.Manual;
        public Entity DriverEntity { get; set; }
        public Vector2 DriverOffset { get; set; }


        public int[,] NodeGrid;
        public int GridWidth;
        public int GridHeight;
        

        public List<Anchor> Anchors = new();
        private readonly List<int> _attachmentConstraintIndices = new();

        public struct Anchor
        {
            public int Node;
            public Vector2 LocalOffset;
            public float LocalAngle;
            public float Stiffness;
        }

        public SoftbodyInstance(SoftbodySim sim, TransformDriverMode driverMode = TransformDriverMode.Manual)
        {
            Sim = sim;
            DriverMode = driverMode;
        }

        public void Clear()
        {
            Sim.Nodes.Clear();
            Sim.Attachments.Clear();
            Sim.Dist.Clear();
            Sim.Clusters.Clear();

            Anchors.Clear();
            _attachmentConstraintIndices.Clear();

            NodeGrid = null;
            GridWidth = 0;
            GridHeight = 0;

            vertices = null;
            indices = null;
        }
        public List<int> CreateEllipseBody(Vector2 center, int count, float radiusX, float radiusY, float mass, float nodeRadius)
        {
            Clear();

            List<int> ring = new();

            for (int i = 0; i < count; i++)
            {
                float t = MathHelper.TwoPi * i / count;
                Vector2 p = center + new Vector2(MathF.Cos(t) * radiusX, MathF.Sin(t) * radiusY);
                ring.Add(Sim.AddNode(p, mass, nodeRadius));
            }

            Sim.BuildLoopLinks(ring, 1f, addBendLinks: true, bendStride: 2);

            for (int i = 0; i < ring.Count; i++)
            {
                int[] cluster =
                {
            ring[i],
            ring[(i + 1) % ring.Count],
            ring[(i + 2) % ring.Count],
            ring[(i + 3) % ring.Count]
        };

                Sim.AddCluster(cluster, 0.9f);
            }

            BoundaryNodes.Clear();
            BoundaryNodes.AddRange(ring);

            Center = ComputeCenter();
            HasCenter = true;

            return ring;
        }
        public void CreateRectBody(Vector2 center, int width, int height, float spacing, float mass, float radius)
        {
            Clear();

            GridWidth = width;
            GridHeight = height;
            NodeGrid = new int[width, height];

            Vector2 start = center - new Vector2((width - 1) * spacing, (height - 1) * spacing) * 0.5f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2 pos = start + new Vector2(x * spacing, y * spacing);
                    NodeGrid[x, y] = Sim.AddNode(pos, mass, radius);
                }
            }

            BuildRectLinks();
            BuildRectClusters();
            BuildRectBoundary();

            Center = ComputeCenter();
            HasCenter = true;

            BuildMesh();
        }

        private void BuildRectLinks()
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int a = NodeGrid[x, y];

                    if (x + 1 < GridWidth)
                        Sim.AddLink(a, NodeGrid[x + 1, y], 1f, SoftbodySim.ConstraintKind.Structural);

                    if (y + 1 < GridHeight)
                        Sim.AddLink(a, NodeGrid[x, y + 1], 1f, SoftbodySim.ConstraintKind.Structural);

                    if (x + 1 < GridWidth && y + 1 < GridHeight)
                        Sim.AddLink(a, NodeGrid[x + 1, y + 1], 0.9f, SoftbodySim.ConstraintKind.Structural);

                    if (x + 1 < GridWidth && y - 1 >= 0)
                        Sim.AddLink(a, NodeGrid[x + 1, y - 1], 0.9f, SoftbodySim.ConstraintKind.Structural);

                    if (x + 2 < GridWidth)
                        Sim.AddLink(a, NodeGrid[x + 2, y], 1f, SoftbodySim.ConstraintKind.Bend);

                    if (y + 2 < GridHeight)
                        Sim.AddLink(a, NodeGrid[x, y + 2], 1f, SoftbodySim.ConstraintKind.Bend);
                }
            }
        }

        private void BuildRectClusters()
        {
            for (int x = 0; x < GridWidth - 1; x++)
            {
                for (int y = 0; y < GridHeight - 1; y++)
                {
                    Span<int> cluster = stackalloc int[4]
                    {
                NodeGrid[x, y],
                NodeGrid[x + 1, y],
                NodeGrid[x + 1, y + 1],
                NodeGrid[x, y + 1]
            };

                    Sim.AddCluster(cluster, 0.85f);
                }
            }
        }

        private void BuildRectBoundary()
        {
            BoundaryNodes.Clear();

            for (int x = 0; x < GridWidth; x++)
                BoundaryNodes.Add(NodeGrid[x, 0]);

            for (int y = 1; y < GridHeight; y++)
                BoundaryNodes.Add(NodeGrid[GridWidth - 1, y]);

            if (GridHeight > 1)
            {
                for (int x = GridWidth - 2; x >= 0; x--)
                    BoundaryNodes.Add(NodeGrid[x, GridHeight - 1]);
            }

            if (GridWidth > 1)
            {
                for (int y = GridHeight - 2; y >= 1; y--)
                    BoundaryNodes.Add(NodeGrid[0, y]);
            }
        }
        public void Update()
        {
            UpdateDriver();
            //UpdateAnchors();
            Sim.Step();
            Center = ComputeCenter();
            HasCenter = true;
        }
        public void TeleportCenter(Vector2 newCenter)
        {
            SetCenter(newCenter, preserveVelocity: false);
        }
        public void SetCenter(Vector2 newCenter, bool preserveVelocity = true)
        {
            Vector2 oldCenter = HasCenter ? Center : ComputeCenter();
            Vector2 delta = newCenter - oldCenter;

            Translate(delta, preserveVelocity);
            Center = newCenter;
            HasCenter = true;
        }
        public Vector2 ComputeCenter()
        {
            if (Sim.Nodes.Count == 0)
                return Vector2.Zero;

            Vector2 sum = Vector2.Zero;
            float totalMass = 0f;

            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                var n = Sim.Nodes[i];
                float mass = n.InvMass > 0f ? 1f / n.InvMass : 1f;
                sum += n.Pos * mass;
                totalMass += mass;
            }

            if (totalMass <= 1e-6f)
                return Vector2.Zero;

            return sum / totalMass;
        }

        public void Translate(Vector2 delta, bool preserveVelocity = true)
        {
            if (delta == Vector2.Zero || Sim.Nodes.Count == 0)
                return;

            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                ref var node = ref Sim.GetNodeRef(i);
                node.Pos += delta;

                if (preserveVelocity)
                    node.PrevPos += delta;
            }

            if (!preserveVelocity)
            {
                for (int i = 0; i < Sim.Nodes.Count; i++)
                {
                    ref var node = ref Sim.GetNodeRef(i);
                    node.PrevPos = node.Pos;
                }
            }

            Center = ComputeCenter();
            HasCenter = true;
        }

        private void UpdateDriver()
        {
            if (DriverMode == TransformDriverMode.EntityCenter && DriverEntity != null)
            {
                Vector2 targetCenter = DriverEntity.Center + DriverOffset;
                SetCenter(targetCenter, preserveVelocity: true);
            }
        }

        #region DrawCode
        VertexPositionColor[] vertices;
        short[] indices;
        private BasicEffect effect;
        public void Draw()
        {
            if (Main.dedServ)
                return;
            Main.spriteBatch.Begin(
          SpriteSortMode.Deferred,
          BlendState.AlphaBlend,
          Main.DefaultSamplerState,
          DepthStencilState.None,
          RasterizerState.CullNone,
          null,
          Main.GameViewMatrix.TransformationMatrix);
            if (indices is null)
            BuildMesh();
            UpdateVertexBuffer();



            GraphicsDevice gd = Main.graphics.GraphicsDevice;

            effect ??= new BasicEffect(gd)
            {
                VertexColorEnabled = true,
                Projection = Main.GameViewMatrix.TransformationMatrix,
                View = Matrix.Identity,
                World = Matrix.Identity
            };
            effect.Projection = Matrix.CreateOrthographicOffCenter(
               0, Main.screenWidth,
               Main.screenHeight, 0,
               -1f,  1f);

            effect.View = Main.GameViewMatrix.ZoomMatrix;
            Vector2 pivot = Center - Main.screenPosition;

            effect.World = Matrix.Identity;
                //Matrix.CreateTranslation(-pivot.X, -pivot.Y, 0f) *
                //Matrix.CreateScale(1f) *
                //Matrix.CreateTranslation(pivot.X, pivot.Y, 0f);


            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.SamplerStates[0] = SamplerState.PointClamp;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                gd.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length,
                    indices,
                    0,
                    indices.Length / 3
                );
            }

            for (int i = 0; i < Sim.Nodes.Count - 1; i++) 
            
            {
                Utilities.Utilities.DrawLineBetter(Main.spriteBatch, Sim.Nodes[i].Pos, Sim.Nodes[i + 1].Pos, Color.Aqua, 2f);
                if (i == Sim.Nodes.Count - 2)
                    Utilities.Utilities.DrawLineBetter(Main.spriteBatch, Sim.Nodes[^1].Pos, Sim.Nodes[0].Pos, Color.Aqua, 2f);
            }


            Main.spriteBatch.End();
        }

        void UpdateVertexBuffer()
        {
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                var n = Sim.Nodes[i];

                vertices[i].Position =
                    new Vector3(n.Pos.X - Main.screenPosition.X, n.Pos.Y - Main.screenPosition.Y, 0);

                vertices[i].Color = Color.White;
            }
        }

        public void BuildMesh()
        {
            int quadCount = (GridWidth - 1) * (GridHeight - 1);

            indices = new short[quadCount * 6];

            int k = 0;

            for (int x = 0; x < GridWidth - 1; x++)
                for (int y = 0; y < GridHeight - 1; y++)
                {
                    int a = NodeGrid[x, y];
                    int b = NodeGrid[x + 1, y];
                    int c = NodeGrid[x, y + 1];
                    int d = NodeGrid[x + 1, y + 1];

                    indices[k++] = (short)a;
                    indices[k++] = (short)b;
                    indices[k++] = (short)c;

                    indices[k++] = (short)b;
                    indices[k++] = (short)d;
                    indices[k++] = (short)c;
                }

            vertices = new VertexPositionColor[Sim.Nodes.Count];
        }

        #endregion
    }
}
