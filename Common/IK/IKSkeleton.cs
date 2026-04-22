using System;
using Microsoft.Xna.Framework;

public sealed class IKSkeleton
{
    //tfw 
    public readonly struct JointSetup
    {
        public readonly float Length;
        public readonly float CenterDegrees;
        public readonly float SwingDegrees;

        public JointSetup(float length, float centerDegrees, float swingDegrees)
        {
            Length = length;
            CenterDegrees = centerDegrees;
            SwingDegrees = swingDegrees;
        }
    }

    public struct JointLimit
    {
        public float Min;
        public float Max;

        public JointLimit(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    //highly doubt we'll ever get this high, but it might be funny
    private const int MaxSegments = 64;
    private const int DefaultIterations = 12;
    private const float SolveEpsilonSq = 0.01f;
    private const float StagnationThreshold = 0.0001f;

    private readonly int _segmentCount;

    private readonly float[] _lengths;
    private readonly JointLimit[] _limits;

    public readonly Vector2[] _joints;

    public readonly Vector2[] _lastSolvedJoints;

    private readonly float[] _localAngles;

    private readonly float _totalLength;

    private bool _initialized;

    public bool SolveFailed { get; private set; }

    public float FinalDistance { get; private set; }

    public int SegmentCount => _segmentCount;

    public int JointCount => _segmentCount + 1;

    public IKSkeleton(params (float length, JointLimit limit)[] segments)
    {
        if (segments == null || segments.Length == 0)
            throw new ArgumentException("At least one segment is required.", nameof(segments));

        if (segments.Length > MaxSegments)
            throw new ArgumentException($"Segment count exceeds max of {MaxSegments}.", nameof(segments));

        _segmentCount = segments.Length;
        _lengths = new float[_segmentCount];
        _limits = new JointLimit[_segmentCount];
        _joints = new Vector2[_segmentCount + 1];
        _lastSolvedJoints = new Vector2[_segmentCount + 1];
        _localAngles = new float[_segmentCount];

        float total = 0f;
        for (int i = 0; i < _segmentCount; i++)
        {
            _lengths[i] = segments[i].length;
            _limits[i] = segments[i].limit;
            total += segments[i].length;
        }

        _totalLength = total;
    }

    public IKSkeleton(params JointSetup[] joints)
    {
        if (joints == null || joints.Length == 0)
            throw new ArgumentException("At least one segment is required.", nameof(joints));

        if (joints.Length > MaxSegments)
            throw new ArgumentException($"Segment count exceeds max of {MaxSegments}.", nameof(joints));

        _segmentCount = joints.Length;
        _lengths = new float[_segmentCount];
        _limits = new JointLimit[_segmentCount];
        _joints = new Vector2[_segmentCount + 1];
        _lastSolvedJoints = new Vector2[_segmentCount + 1];
        _localAngles = new float[_segmentCount];

        float total = 0f;
        for (int i = 0; i < _segmentCount; i++)
        {
            _lengths[i] = joints[i].Length;
            _limits[i] = FromCentered(joints[i].CenterDegrees, joints[i].SwingDegrees);
            total += joints[i].Length;
        }

        _totalLength = total;
    }

    public Vector2 GetJointPosition(int index) => _joints[index];

    public float GetConstraintDegrees(int joint)
    {
        JointLimit limit = _limits[joint];
        return MathHelper.ToDegrees(limit.Max - limit.Min);
    }

    public void SetConstraint(int joint, float minRadians, float maxRadians)
    {
        _limits[joint] = new JointLimit(minRadians, maxRadians);
    }

    public float GetSolvedLocalAngle(int joint, Vector2 rootPosition)
    {
        float parentAngle = GetParentWorldAngle(joint, rootPosition);
        float segmentAngle = (_joints[joint + 1] - _joints[joint]).ToRotation();
        return MathHelper.WrapAngle(segmentAngle - parentAngle);
    }

    public void LockCurrentPose(Vector2 rootPosition)
    {
        for (int i = 0; i < _segmentCount; i++)
        {
            float angle = GetSolvedLocalAngle(i, rootPosition);
            _limits[i] = new JointLimit(angle, angle);
        }
    }

    public void Solve(Vector2 rootPosition, Vector2 targetPosition)
    {
        if (!_initialized)
            BuildRestPose(rootPosition);

        CopyJoints(_joints, _lastSolvedJoints);
        SolveFailed = false;

        float rootToTargetSq = Vector2.DistanceSquared(rootPosition, targetPosition);

        if (rootToTargetSq >= _totalLength * _totalLength)
        {
            SolveUnreachable(rootPosition, targetPosition);
            FinalDistance = Vector2.Distance(_joints[_segmentCount], targetPosition);
            return;
        }

        _joints[0] = rootPosition;

        float previousError = float.MaxValue;

        for (int iteration = 0; iteration < DefaultIterations; iteration++)
        {
            BackwardReach(targetPosition);
            ForwardReach(rootPosition);

            float errorSq = Vector2.DistanceSquared(_joints[_segmentCount], targetPosition);

            if (errorSq <= SolveEpsilonSq)
                break;

            if (MathF.Abs(previousError - errorSq) < StagnationThreshold)
            {
                SolveFailed = true;
                break;
            }

            previousError = errorSq;
        }

        FinalDistance = Vector2.Distance(_joints[_segmentCount], targetPosition);

        if (SolveFailed)
        {
            CopyJoints(_lastSolvedJoints, _joints);
            FinalDistance = Vector2.Distance(_joints[_segmentCount], targetPosition);
        }
        else
        {
            CacheSolvedLocalAngles(rootPosition);
        }
    }

    private void BackwardReach(Vector2 targetPosition)
    {
        _joints[_segmentCount] = targetPosition;

        for (int i = _segmentCount - 1; i >= 0; i--)
        {
            Vector2 child = _joints[i + 1];
            Vector2 current = _joints[i];

            float incomingParentAngle = GetBackwardParentAngle(i);
            float desiredAngle = (current - child).ToRotation();
            float localAngle = MathHelper.WrapAngle(desiredAngle - incomingParentAngle);
            float clampedLocal = Math.Clamp(localAngle, _limits[i].Min, _limits[i].Max);
            float worldAngle = incomingParentAngle + clampedLocal;

            _joints[i] = child + worldAngle.ToRotationVector2() * _lengths[i];
        }
    }

    private void ForwardReach(Vector2 rootPosition)
    {
        _joints[0] = rootPosition;

        for (int i = 0; i < _segmentCount; i++)
        {
            Vector2 current = _joints[i];
            Vector2 next = _joints[i + 1];

            float parentAngle = GetParentWorldAngle(i, rootPosition);
            float desiredAngle = (next - current).ToRotation();
            float localAngle = MathHelper.WrapAngle(desiredAngle - parentAngle);
            float clampedLocal = Math.Clamp(localAngle, _limits[i].Min, _limits[i].Max);
            float worldAngle = parentAngle + clampedLocal;

            _joints[i + 1] = current + worldAngle.ToRotationVector2() * _lengths[i];
        }
    }

    private void SolveUnreachable(Vector2 rootPosition, Vector2 targetPosition)
    {
        _joints[0] = rootPosition;

        float targetDirection = (targetPosition - rootPosition).ToRotation();

        for (int i = 0; i < _segmentCount; i++)
        {
            float parentAngle = GetParentWorldAngle(i, rootPosition);
            float localTowardTarget = MathHelper.WrapAngle(targetDirection - parentAngle);
            float clampedLocal = Math.Clamp(localTowardTarget, _limits[i].Min, _limits[i].Max);
            float worldAngle = parentAngle + clampedLocal;

            _joints[i + 1] = _joints[i] + worldAngle.ToRotationVector2() * _lengths[i];
        }

        CacheSolvedLocalAngles(rootPosition);
    }

    private float GetParentWorldAngle(int jointIndex, Vector2 rootPosition)
    {
        if (jointIndex <= 0)
            return 0f;

        Vector2 parentDirection = _joints[jointIndex] - _joints[jointIndex - 1];

        if (parentDirection.LengthSquared() <= 0.000001f)
            return 0f;

        return parentDirection.ToRotation();
    }

    private float GetBackwardParentAngle(int jointIndex)
    {
        if (jointIndex <= 0)
            return 0f;

        Vector2 parentDir = _joints[jointIndex] - _joints[jointIndex - 1];
        if (parentDir.LengthSquared() > 0.000001f)
            return parentDir.ToRotation();

        return 0f;
    }

    private void CacheSolvedLocalAngles(Vector2 rootPosition)
    {
        for (int i = 0; i < _segmentCount; i++)
            _localAngles[i] = GetSolvedLocalAngle(i, rootPosition);
    }

    private void BuildRestPose(Vector2 rootPosition)
    {
        _joints[0] = rootPosition;

        float accumulatedAngle = 0f;

        for (int i = 0; i < _segmentCount; i++)
        {
            JointLimit limit = _limits[i];
            float center = (limit.Min + limit.Max) * 0.5f;
            accumulatedAngle += center;
            _joints[i + 1] = _joints[i] + accumulatedAngle.ToRotationVector2() * _lengths[i];
        }

        CopyJoints(_joints, _lastSolvedJoints);
        _initialized = true;
    }

    private static JointLimit FromCentered(float centerDegrees, float swingDegrees)
    {
        float center = MathHelper.ToRadians(centerDegrees);
        float swing = MathHelper.ToRadians(swingDegrees);
        return new JointLimit(center - swing, center + swing);
    }

    private static void CopyJoints(Vector2[] from, Vector2[] to)
    {
        for (int i = 0; i < from.Length; i++)
            to[i] = from[i];
    }
}