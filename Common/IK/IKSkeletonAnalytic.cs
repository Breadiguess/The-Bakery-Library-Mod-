using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Common.IK
{
    public sealed class IKSkeletonAnalytic
    {
        public Vector2 Root;
        public Vector2 Joint;
        public Vector2 Tip;

        public float UpperLength;
        public float LowerLength;


        public void Solve(Vector2 target, Vector2 pole)
        {
            Vector2 rootToTarget = target - Root;
            float rawDistance = rootToTarget.Length();

            Vector2 dir = rootToTarget.SafeNormalize(Vector2.UnitX);

            float distance = MathHelper.Clamp
            (
                rawDistance,
                MathF.Abs(UpperLength - LowerLength) + 0.001f,
                UpperLength + LowerLength - 0.001f
            );

            // The actual reachable tip position.
            Vector2 solvedTip = Root + dir * distance;

            float cosTheta =
            (
                UpperLength * UpperLength +
                distance * distance -
                LowerLength * LowerLength
            ) /
            (
                2f * UpperLength * distance
            );

            float theta = MathF.Acos(MathHelper.Clamp(cosTheta, -1f, 1f));

            Vector2 perp = new Vector2(-dir.Y, dir.X);

            Vector2 rootToPole = pole - Root;

            float side = MathF.Sign(Vector2.Dot(perp, rootToPole));

            if (side == 0f)
                side = 1f;

            Vector2 jointDir = dir.RotatedBy(theta * side);

            Joint = Root + jointDir * UpperLength;
            Tip = solvedTip;
        }
    }
}
