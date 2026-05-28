using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

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
                float yOffset = 0f;
                for (int j = 0; j < i && j < segmentLength.Length; j++)
                    yOffset += segmentLength[j];

                Positions[i] = start + Vector2.UnitY * yOffset;
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
    public VerletChain(int count, float segmentLength, Vector2 start, Vector2 end)
    {
        SegmentLength = new float[count - 1];
        for (int i = 0; i < count - 1; i++)
            SegmentLength[i] = segmentLength;
        Positions = new Vector2[count];
        OldPositions = new Vector2[count];
        InitializeBetween(start, end);
    }
    
    public VerletChain(int count, float[] segmentLength, Vector2 start, Vector2 end)
    {
        SegmentLength = segmentLength;
        Positions = new Vector2[count];
        OldPositions = new Vector2[count];
        InitializeBetween(start, end);
    }
    
    private void InitializeBetween(Vector2 start, Vector2 end)
    {
        if (Positions.Length == 0)
            return;
        if (Positions.Length == 1)
        {
            Positions[0] = start;
            OldPositions[0] = start;
            return;
        }
        for (int i = 0; i < Positions.Length; i++)
        {
            float completion = i / (float)(Positions.Length - 1);
            Vector2 position = Vector2.Lerp(start, end, completion);
            Positions[i] = position;
            OldPositions[i] = position;
        }
    }
        public void Simulate(Vector2 externalVelocity, Vector2 root, float gravity, float damping,
          int constraintIterations = 4, bool collideWithTiles = true, float collisionRadius = 4f,
          bool collideWithPlayers = true, float playerInfluence = 0.35f)
        {
            for (int i = 1; i < Positions.Length; i++)
            {
                Vector2 velocity = (Positions[i] - OldPositions[i]) * damping + externalVelocity;

                OldPositions[i] = Positions[i];
                Positions[i] += velocity;
                Positions[i].Y += gravity;

                if (collideWithTiles)
                    ResolvePointTileCollision(i, collisionRadius);

                if (collideWithPlayers)
                    ResolvePointPlayerCollision(i, collisionRadius, playerInfluence);
            }

            // Pin root.
            Positions[0] = root;

            for (int k = 0; k < constraintIterations; k++)
            {
                Positions[0] = root;

                for (int i = 0; i < Positions.Length - 1; i++)
                {
                    Vector2 delta = Positions[i + 1] - Positions[i];
                    float dist = delta.Length();

                    if (dist <= 0.0001f)
                        continue;

                    float error = dist - SegmentLength[i];
                    Vector2 correction = delta / dist * error * 0.5f;

                    if (i != 0)
                        Positions[i] += correction;

                    Positions[i + 1] -= correction;
                }

                if (collideWithTiles)
                {
                    for (int i = 1; i < Positions.Length; i++)
                    {
                        if (collideWithTiles)
                            ResolvePointTileCollision(i, collisionRadius);
                    }
                }
                if (collideWithPlayers)
                {
                    for (int i = 1; i < Positions.Length; i++)
                    {

                        if (collideWithPlayers)
                            ResolvePointPlayerCollision(i, collisionRadius, playerInfluence);
                    }
                }
            }
        }

        private void ResolvePointTileCollision(int index, float radius)
        {
            Vector2 pos = Positions[index];

            int minTileX = (int)((pos.X - radius) / 16f) - 1;
            int maxTileX = (int)((pos.X + radius) / 16f) + 1;
            int minTileY = (int)((pos.Y - radius) / 16f) - 1;
            int maxTileY = (int)((pos.Y + radius) / 16f) + 1;

            minTileX = Utils.Clamp(minTileX, 0, Main.maxTilesX - 1);
            maxTileX = Utils.Clamp(maxTileX, 0, Main.maxTilesX - 1);
            minTileY = Utils.Clamp(minTileY, 0, Main.maxTilesY - 1);
            maxTileY = Utils.Clamp(maxTileY, 0, Main.maxTilesY - 1);

            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile == null || !tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                        continue;

                    Rectangle tileRect = new Rectangle(x * 16, y * 16, 16, 16);
                    PushCircleOutOfRect(ref pos, radius, tileRect);
                }
            }

            Positions[index] = pos;
        }

        private static void PushCircleOutOfRect(ref Vector2 circleCenter, float radius, Rectangle rect)
        {
            float closestX = MathHelper.Clamp(circleCenter.X, rect.Left, rect.Right);
            float closestY = MathHelper.Clamp(circleCenter.Y, rect.Top, rect.Bottom);

            Vector2 closest = new Vector2(closestX, closestY);
            Vector2 diff = circleCenter - closest;
            float distSq = diff.LengthSquared();

            // Standard circle-vs-AABB overlap.
            if (distSq > 0f && distSq < radius * radius)
            {
                float dist = MathF.Sqrt(distSq);
                Vector2 normal = diff / dist;
                float push = radius - dist;
                circleCenter += normal * push;
                return;
            }

            // Circle center is inside rectangle or exactly on boundary.
            if (distSq == 0f && rect.Contains(circleCenter.ToPoint()))
            {
                float leftPen = circleCenter.X - rect.Left;
                float rightPen = rect.Right - circleCenter.X;
                float topPen = circleCenter.Y - rect.Top;
                float bottomPen = rect.Bottom - circleCenter.Y;

                float minPen = leftPen;
                Vector2 pushDir = -Vector2.UnitX;

                if (rightPen < minPen)
                {
                    minPen = rightPen;
                    pushDir = Vector2.UnitX;
                }
                if (topPen < minPen)
                {
                    minPen = topPen;
                    pushDir = -Vector2.UnitY;
                }
                if (bottomPen < minPen)
                {
                    minPen = bottomPen;
                    pushDir = Vector2.UnitY;
                }

                circleCenter += pushDir * (minPen + radius);
            }
        }

        private void ResolvePointPlayerCollision(int index, float radius, float influence)
        {
            Vector2 pos = Positions[index];

            foreach(Player player in Main.ActivePlayers)
            {
                if (player == null)
                    continue;

                if ((!player.active || player.dead))
                    continue;

                Rectangle playerRect = player.Hitbox;

                Vector2 beforePush = pos;

                if (PushCircleOutOfRectWithResult(ref pos, radius, playerRect, out Vector2 push))
                {
                    Positions[index] = pos;

                    Vector2 playerMotion = player.velocity * influence;

                    Positions[index] += playerMotion;

                    OldPositions[index] += playerMotion * 0.25f;

                    if (push.LengthSquared() > 0.0001f)
                    {
                        Vector2 normal = Vector2.Normalize(push);
                        Vector2 verletVelocity = Positions[index] - OldPositions[index];

                        float intoPlayer = Vector2.Dot(verletVelocity, -normal);
                        if (intoPlayer > 0f)
                            OldPositions[index] += normal * intoPlayer * 0.5f;
                    }
                }
            }
        }

        private static bool PushCircleOutOfRectWithResult(ref Vector2 circleCenter, float radius, Rectangle rect, out Vector2 push)
        {
            push = Vector2.Zero;

            Vector2 original = circleCenter;

            float closestX = MathHelper.Clamp(circleCenter.X, rect.Left, rect.Right);
            float closestY = MathHelper.Clamp(circleCenter.Y, rect.Top, rect.Bottom);

            Vector2 closest = new Vector2(closestX, closestY);
            Vector2 diff = circleCenter - closest;
            float distSq = diff.LengthSquared();

            if (distSq > 0f && distSq < radius * radius)
            {
                float dist = MathF.Sqrt(distSq);
                Vector2 normal = diff / dist;
                float amount = radius - dist;

                push = normal * amount;
                circleCenter += push;

                return true;
            }

            if (distSq == 0f && rect.Contains(circleCenter.ToPoint()))
            {
                float leftPen = circleCenter.X - rect.Left;
                float rightPen = rect.Right - circleCenter.X;
                float topPen = circleCenter.Y - rect.Top;
                float bottomPen = rect.Bottom - circleCenter.Y;

                float minPen = leftPen;
                Vector2 pushDir = -Vector2.UnitX;

                if (rightPen < minPen)
                {
                    minPen = rightPen;
                    pushDir = Vector2.UnitX;
                }

                if (topPen < minPen)
                {
                    minPen = topPen;
                    pushDir = -Vector2.UnitY;
                }

                if (bottomPen < minPen)
                {
                    minPen = bottomPen;
                    pushDir = Vector2.UnitY;
                }

                push = pushDir * (minPen + radius);
                circleCenter += push;

                return true;
            }

            return false;
        }
    }
}