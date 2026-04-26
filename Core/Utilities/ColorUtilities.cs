using Microsoft.Xna.Framework;

namespace BreadLibrary.Core.Utilities

{

    public static partial class Utilities

    {

        /// <summary>

        /// Converts a normalized Vector3 color value into a Color.

        /// 

        /// X = R, Y = G, Z = B.

        /// Alpha is assumed to be fully opaque.

        /// Expected component range is 0f to 1f.

        /// </summary>

        public static Color ToColor(this Vector3 vector)

        {

            return new Color(

                MathHelper.Clamp(vector.X, 0f, 1f),

                MathHelper.Clamp(vector.Y, 0f, 1f),

                MathHelper.Clamp(vector.Z, 0f, 1f),

                1f

            );

        }

        /// <summary>

        /// Converts a normalized Vector4 color value into a Color.

        /// 

        /// X = R, Y = G, Z = B, W = A.

        /// Expected component range is 0f to 1f.

        /// </summary>

        public static Color ToColor(this Vector4 vector)

        {

            return new Color(

                MathHelper.Clamp(vector.X, 0f, 1f),

                MathHelper.Clamp(vector.Y, 0f, 1f),

                MathHelper.Clamp(vector.Z, 0f, 1f),

                MathHelper.Clamp(vector.W, 0f, 1f)

            );

        }

     }
}