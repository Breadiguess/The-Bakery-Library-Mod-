using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Verlet
{
    public class VerletChain
    {
        public Vector2[] Positions;
        public Vector2[] OldPositions;
        public float[] SegmentLength;

        public VerletChain(int count, float[] segmentLength, Vector2 start)
        {
            SegmentLength = segmentLength;

            Positions = new Vector2[count];
            OldPositions = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                Positions[i] = start + Vector2.UnitY * segmentLength[i] * i;
                OldPositions[i] = Positions[i];
            }
        }
        public VerletChain(int count, float segmentLength, Vector2 start)
        {
            SegmentLength = new float[count - 1];
            for (int i = 0; i < count - 1; i++)
                SegmentLength[i] = segmentLength;
            Positions = new Vector2[count];
            OldPositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Positions[i] = start + Vector2.UnitY * segmentLength * i;
                OldPositions[i] = Positions[i];
            }
        }
        public void Simulate(Vector2 Velocity, Vector2 root, float gravity, float damping, int constraintIterations = 4)
        {
            // integrate
            for (int i = 1; i < Positions.Length; i++)
            {
                Vector2 velocity = (Positions[i] - OldPositions[i]) * damping + Velocity;

                OldPositions[i] = Positions[i];

                Positions[i] += velocity;
                Positions[i].Y += gravity;
            }

            // pin root
            Positions[0] = root;

            // satisfy constraints
            for (int k = 0; k < constraintIterations; k++)
            {
                Positions[0] = root;

                for (int i = 0; i < Positions.Length - 1; i++)
                {
                    Vector2 delta = Positions[i + 1] - Positions[i];

                    float dist = delta.Length();
                    float error = dist - SegmentLength[i];

                    Vector2 correction = delta.SafeNormalize(Vector2.UnitY) * error * 0.5f;

                    if (i != 0)
                        Positions[i] += correction;

                    Positions[i + 1] -= correction;
                }
            }
        }
    }

}
