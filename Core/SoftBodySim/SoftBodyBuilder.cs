using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public static class SoftbodyBuilder
    {
        public static int[,] CreateSquareLattice(
        SoftbodySim sim,
        Vector2 origin,
        int width,
        int height,
        float spacing,
        float nodeMass,
        float radius)
        {
            int[,] ids = new int[width, height];

            // Nodes
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Vector2 pos = origin + new Vector2(x * spacing, y * spacing);
                    ids[x, y] = sim.AddNode(pos, nodeMass, radius);
                }

            // Structural
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x < width - 1)
                        sim.AddLink(ids[x, y], ids[x + 1, y], sim.Mat.StructuralStiffness, SoftbodySim.ConstraintKind.Structural);

                    if (y < height - 1)
                        sim.AddLink(ids[x, y], ids[x, y + 1], sim.Mat.StructuralStiffness, SoftbodySim.ConstraintKind.Structural);
                }

            // Shear (uses ShearStiffness you added to Material)
            for (int x = 0; x < width - 1; x++)
                for (int y = 0; y < height - 1; y++)
                {
                    sim.AddLink(ids[x, y], ids[x + 1, y + 1], sim.Mat.ShearStiffness, SoftbodySim.ConstraintKind.Structural);
                    sim.AddLink(ids[x + 1, y], ids[x, y + 1], sim.Mat.ShearStiffness, SoftbodySim.ConstraintKind.Structural);
                }
            for (int x = 0; x < width - 1; x++)
                for (int y = 0; y < height - 1; y++)
                {
                    int a = ids[x, y];
                    int b = ids[x + 1, y];
                    int c = ids[x, y + 1];
                    int d = ids[x + 1, y + 1];

                    if (a == -1 || b == -1 || c == -1 || d == -1)
                        continue;

                    // two triangles per quad
                    sim.AddArea(a, b, c, sim.Mat.AreaStiffness);
                    sim.AddArea(b, d, c, sim.Mat.AreaStiffness);
                }
            // Bend (skip one)
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x < width - 2)
                        sim.AddLink(ids[x, y], ids[x + 2, y], sim.Mat.BendStiffness, SoftbodySim.ConstraintKind.Bend);

                    if (y < height - 2)
                        sim.AddLink(ids[x, y], ids[x, y + 2], sim.Mat.BendStiffness, SoftbodySim.ConstraintKind.Bend);
                }

            return ids;
        }

        public static int[,] CreateSquareLatticeShell(
    SoftbodySim sim,
    Vector2 origin,
    int width,
    int height,
    float spacing,
    float shellMass,
    float interiorMass,
    float radius)
        {
            int[,] ids = new int[width, height];

            // -------- CREATE NODES --------
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    bool isEdge =
                        x == 0 ||
                        y == 0 ||
                        x == width - 1 ||
                        y == height - 1;

                    float mass = isEdge ? shellMass : interiorMass;

                    Vector2 pos = origin + new Vector2(x * spacing, y * spacing);

                    ids[x, y] = sim.AddNode(pos, mass, radius);
                }

            // -------- STRUCTURAL --------
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x < width - 1)
                        sim.AddLink(ids[x, y], ids[x + 1, y],
                            sim.Mat.StructuralStiffness,
                            SoftbodySim.ConstraintKind.Structural);

                    if (y < height - 1)
                        sim.AddLink(ids[x, y], ids[x, y + 1],
                            sim.Mat.StructuralStiffness,
                            SoftbodySim.ConstraintKind.Structural);
                }

            // -------- SHEAR --------
            for (int x = 0; x < width - 1; x++)
                for (int y = 0; y < height - 1; y++)
                {
                    sim.AddLink(ids[x, y], ids[x + 1, y + 1],
                        sim.Mat.ShearStiffness,
                        SoftbodySim.ConstraintKind.Structural);

                    sim.AddLink(ids[x + 1, y], ids[x, y + 1],
                        sim.Mat.ShearStiffness,
                        SoftbodySim.ConstraintKind.Structural);
                }
            for (int x = 0; x < width - 1; x++)
                for (int y = 0; y < height - 1; y++)
                {
                    int a = ids[x, y];
                    int b = ids[x + 1, y];
                    int c = ids[x, y + 1];
                    int d = ids[x + 1, y + 1];

                    if (a == -1 || b == -1 || c == -1 || d == -1)
                        continue;

                    // two triangles per quad
                    sim.AddArea(a, b, c, sim.Mat.AreaStiffness);
                    sim.AddArea(b, d, c, sim.Mat.AreaStiffness);
                }
            // -------- BEND --------
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x < width - 2)
                        sim.AddLink(ids[x, y], ids[x + 2, y],
                            sim.Mat.BendStiffness,
                            SoftbodySim.ConstraintKind.Bend);

                    if (y < height - 2)
                        sim.AddLink(ids[x, y], ids[x, y + 2],
                            sim.Mat.BendStiffness,
                            SoftbodySim.ConstraintKind.Bend);
                }

            return ids;
        }

        public static int[,] CreateCircularLattice(
    SoftbodySim sim,
    Vector2 center,
    int diameterNodes,
    float spacing,
    float mass,
    float radius)
        {
            int size = diameterNodes;

            int[,] ids = new int[size, size];

            float worldRadius = (size - 1) * spacing * 0.5f;

            // ---------- CREATE NODES ----------
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Vector2 pos = center +
                        new Vector2(
                            (x - (size - 1) * 0.5f) * spacing,
                            (y - (size - 1) * 0.5f) * spacing
                        );

                    if (Vector2.Distance(pos, center) <= worldRadius)
                        ids[x, y] = sim.AddNode(pos, mass, radius);
                    else
                        ids[x, y] = -1; // mark as empty
                }

            // ---------- STRUCTURAL ----------
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    int a = ids[x, y];
                    if (a == -1) continue;

                    if (x < size - 1)
                    {
                        int b = ids[x + 1, y];
                        if (b != -1)
                            sim.AddLink(a, b,
                                sim.Mat.StructuralStiffness,
                                SoftbodySim.ConstraintKind.Structural);
                    }

                    if (y < size - 1)
                    {
                        int b = ids[x, y + 1];
                        if (b != -1)
                            sim.AddLink(a, b,
                                sim.Mat.StructuralStiffness,
                                SoftbodySim.ConstraintKind.Structural);
                    }
                }

            // ---------- SHEAR ----------
            for (int x = 0; x < size - 1; x++)
                for (int y = 0; y < size - 1; y++)
                {
                    int a = ids[x, y];
                    int b = ids[x + 1, y + 1];
                    int c = ids[x + 1, y];
                    int d = ids[x, y + 1];

                    if (a != -1 && b != -1)
                        sim.AddLink(a, b,
                            sim.Mat.ShearStiffness,
                            SoftbodySim.ConstraintKind.Structural);

                    if (c != -1 && d != -1)
                        sim.AddLink(c, d,
                            sim.Mat.ShearStiffness,
                            SoftbodySim.ConstraintKind.Structural);
                }

            // ---------- BEND ----------
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    int a = ids[x, y];
                    if (a == -1) continue;

                    if (x < size - 2)
                    {
                        int b = ids[x + 2, y];
                        if (b != -1)
                            sim.AddLink(a, b,
                                sim.Mat.BendStiffness,
                                SoftbodySim.ConstraintKind.Bend);
                    }

                    if (y < size - 2)
                    {
                        int b = ids[x, y + 2];
                        if (b != -1)
                            sim.AddLink(a, b,
                                sim.Mat.BendStiffness,
                                SoftbodySim.ConstraintKind.Bend);
                    }
                }

            return ids;
        }


        public static int[,] CreateCohesionBlob(
    SoftbodySim sim,
    Vector2 center,
    int countX,
    int countY,
    float spacing,
    float mass,
    float radius,
    int neighborRange = 1)
        {
            int[,] ids = new int[countX, countY];

            // --- create particles ---
            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                {
                    Vector2 pos = center +
                        new Vector2(
                            (x - countX * 0.5f) * spacing,
                            (y - countY * 0.5f) * spacing
                        );

                    ids[x, y] = sim.AddNode(pos, mass, radius);
                }

            // --- cohesion only ---
            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                {
                    int a = ids[x, y];

                    for (int dx = -neighborRange; dx <= neighborRange; dx++)
                        for (int dy = -neighborRange; dy <= neighborRange; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx < 0 || ny < 0 || nx >= countX || ny >= countY)
                                continue;

                            int b = ids[nx, ny];

                            float rest =
                                Vector2.Distance(
                                    sim.Nodes[a].Pos,
                                    sim.Nodes[b].Pos);

                            sim.AddLink(a, b,
                                sim.Mat.StructuralStiffness * 0.2f,
                                SoftbodySim.ConstraintKind.Structural);
                        }
                }

            return ids;
        }





        public static List<int> CreateParticleBlob(
    SoftbodySim sim,
    Vector2 center,
    int count,
    float radiusWorld,
    float mass,
    float particleRadius)
        {
            var ids = new List<int>(count);
            var rand = Main.rand;

            for (int i = 0; i < count; i++)
            {
                // random point in circle
                float a = rand.NextFloat() * MathHelper.TwoPi;
                float r = radiusWorld * MathF.Sqrt(rand.NextFloat());
                Vector2 pos = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;

                ids.Add(sim.AddNode(pos, mass, particleRadius));
            }

            return ids;
        }
    }
}

