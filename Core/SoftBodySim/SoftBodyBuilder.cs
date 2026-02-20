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



    }
}

