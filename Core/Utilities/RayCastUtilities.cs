using BreadLibrary.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Utilities
{
    public readonly struct RaycastHit
    {
        public readonly Point Tile;
        public readonly Vector2 WorldPosition;
        public readonly Vector2 Normal;
        public readonly float Distance;

        public RaycastHit(Point tile, Vector2 worldPosition, Vector2 normal, float distance)
        {
            Tile = tile;
            WorldPosition = worldPosition;
            Normal = normal;
            Distance = distance;
        }
    }

    public static partial class Utilities
    {
        public static RaycastHit? RaycastPreciseTo(
          Vector2 start,
          Vector2 end,
          bool ignoreHalfTiles = false,
          bool debug = false,
          bool shouldCountWater = false)
        {
            Vector2 ray = end - start;
            float rayLength = ray.Length();

            if (rayLength <= 0.001f)
                return null;

            Vector2 direction = ray / rayLength;

            int x = (int)(start.X / 16f);
            int y = (int)(start.Y / 16f);

            int endX = (int)(end.X / 16f);
            int endY = (int)(end.Y / 16f);

            x = Utils.Clamp(x, 0, Main.maxTilesX - 1);
            y = Utils.Clamp(y, 0, Main.maxTilesY - 1);
            endX = Utils.Clamp(endX, 0, Main.maxTilesX - 1);
            endY = Utils.Clamp(endY, 0, Main.maxTilesY - 1);

            int stepX = direction.X > 0f ? 1 : -1;
            int stepY = direction.Y > 0f ? 1 : -1;

            float nextTileBoundaryX = stepX > 0 ? (x + 1) * 16f : x * 16f;
            float nextTileBoundaryY = stepY > 0 ? (y + 1) * 16f  : y * 16f;

            float tMaxX = direction.X == 0f ? float.PositiveInfinity : (nextTileBoundaryX - start.X) / direction.X;

            float tMaxY = direction.Y == 0f ? float.PositiveInfinity : (nextTileBoundaryY - start.Y) / direction.Y;

            float tDeltaX = direction.X == 0f ? float.PositiveInfinity : 16f / MathF.Abs(direction.X);

            float tDeltaY = direction.Y == 0f ? float.PositiveInfinity : 16f / MathF.Abs(direction.Y);

            float travelled = 0f;
            Vector2 lastNormal = Vector2.Zero;

            while (travelled <= rayLength)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    break;

                Tile tile = Main.tile[x, y];

                if (IsRaycastBlockingTile(tile, x, y, ignoreHalfTiles, shouldCountWater))
                {
                    Vector2 hitPosition = start + direction * travelled;

                    if (debug)
                    {
                        RayCastVisualizer.Raycasts.Add(new Raycast(start / 16f, hitPosition / 16f, Color.Blue));
                    }

                    return new RaycastHit(new Point(x, y), hitPosition, lastNormal, travelled);
                }

                if (x == endX && y == endY)
                    break;

                if (tMaxX < tMaxY)
                {
                    travelled = tMaxX;
                    tMaxX += tDeltaX;
                    x += stepX;

                    // We crossed a vertical tile boundary
                    lastNormal = new Vector2(-stepX, 0f);
                }
                else
                {
                    travelled = tMaxY;
                    tMaxY += tDeltaY;
                    y += stepY;

                    // We crossed a horizontal tile boundary
                    lastNormal = new Vector2(0f, -stepY);
                }
            }

            return null;
        }

        private static bool IsRaycastBlockingTile(Tile tile, int x, int y, bool ignoreHalfTiles, bool shouldCountWater)
        {
            if (tile == null)
                return false;

            if (shouldCountWater && tile.LiquidAmount > 0)
                return true;

            if (!tile.HasTile)
                return false;

            if (ignoreHalfTiles && tile.IsHalfBlock)
                return false;

            ushort type = tile.TileType;

            if (!tile.HasUnactuatedTile)
                return false;

            if (!Main.tileSolid[type])
                return false;

            if (Main.tileCut[type] || Main.tileNoAttach[type] || Main.tileAxe[type])
                return false;

            if (!Main.tileBlockLight[type])
                return false;

            return WorldGen.SolidTile(tile)
                && Collision.SolidCollision(new Vector2(x * 16f, y * 16f), 16, 16)
                && Collision.IsWorldPointSolid(new Point(x, y).ToWorldCoordinates());
        }
    
        public static bool RaytraceTo(int x0, int y0, int x1, int y1, bool ignoreHalfTiles = false)
        {
            //Bresenham's algorithm
            var horizontalDistance = Math.Abs(x1 - x0); //Delta X
            var verticalDistance = Math.Abs(y1 - y0); //Delta Y
            var horizontalIncrement = x1 > x0 ? 1 : -1; //S1
            var verticalIncrement = y1 > y0 ? 1 : -1; //S2

            var x = x0;
            var y = y0;
            var E = horizontalDistance - verticalDistance;

            while (true)
            {
                if ((!ignoreHalfTiles || !Main.tile[x, y].IsHalfBlock))
                {
                    return false;
                }

                if (x == x1 && y == y1)
                {
                    return true;
                }

                var E2 = E * 2;

                if (E2 >= -verticalDistance)
                {
                    if (x == x1)
                    {
                        return true;
                    }

                    E -= verticalDistance;
                    x += horizontalIncrement;
                }

                if (E2 <= horizontalDistance)
                {
                    if (y == y1)
                    {
                        return true;
                    }

                    E += horizontalDistance;
                    y += verticalIncrement;
                }
            }
        }

        public static Point? RaycastTo(Vector2 start, Vector2 end, bool ignoreHalfTiles = false, bool debug = false, bool ShouldCountWater = false)
        {
            var x0 = (int)(start.X / 16f);
            var y0 = (int)(start.Y / 16f);
            var x1 = (int)(end.X / 16f);
            var y1 = (int)(end.Y / 16f);

            return RaycastTo(x0, y0, x1, y1, ignoreHalfTiles, debug, ShouldCountWater);
        }

        public static Point? RaycastTo(int x0, int y0, int x1, int y1, bool ignoreHalfTiles = false, bool debug = false, bool ShouldCountWater = false)
        {
            // Clamp the start and end points to prevent out-of-range crashes.
            x0 = Utils.Clamp(x0, 0, Main.maxTilesX - 1);
            y0 = Utils.Clamp(y0, 0, Main.maxTilesY - 1);
            x1 = Utils.Clamp(x1, 0, Main.maxTilesX - 1);
            y1 = Utils.Clamp(y1, 0, Main.maxTilesY - 1);

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = x1 > x0 ? 1 : -1;
            var sy = y1 > y0 ? 1 : -1;

            var x = x0;
            var y = y0;
            var err = dx - dy;

            while (true)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                {
                    break;
                }



                var tile = Main.tile[x, y];

                if (ShouldCountWater &&
                    tile != null &&
                    tile.LiquidAmount > 0) 
                {
                    int surfaceY = y;

                    while (surfaceY > 0)
                    {
                        Tile above = Main.tile[x, surfaceY - 1];

                        if (above == null ||
                            above.LiquidAmount == 0 ||
                            above.LiquidType != tile.LiquidType)
                            break;

                        surfaceY--;
                    }
                    if (debug)
                        RayCastVisualizer.Raycasts.Add(new Raycast(new Vector2(x0, y0), new Vector2(x, surfaceY), Color.Blue));
                    return new Point(x, surfaceY);
                }

                if (tile != null && WorldGen.SolidOrSlopedTile(tile))
                {
                    int surfaceY = y;

                    if (debug)
                        RayCastVisualizer.Raycasts.Add(new Raycast(new Vector2(x0, y0), new Vector2(x, surfaceY), Color.Blue));
                    return new Point(x, surfaceY);
                }


                if (x == x1 && y == y1)
                {
                    break;
                }

                    var e2 = err * 2;

                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            // No tile hit
            return null;
        }
    }
}
