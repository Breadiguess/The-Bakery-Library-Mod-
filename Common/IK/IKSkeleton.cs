
using System.Runtime.CompilerServices;

public struct IKSkeleton
{
    public struct Constraints()
    {
        public float MinAngle = -MathF.PI;

        public float MaxAngle = MathF.PI;
    }

    [InlineArray(MaxJointCount + 1)]
    private struct PositionData
    {
        private Vector2 _;
    }

    public void SetPosition(int index, Vector2 position)
    {
        _positions[index] = position;
    }
    public bool SolveFailed { get; private set; }

    public float FinalDistance { get; private set; }

    public readonly int JointCount => _options.Length;

    public readonly int PositionCount => JointCount + 1;

    private const int MaxJointCount = 16;

    public (float length, Constraints constraints)[] _options;

    private PositionData _previousPositions;

    private PositionData _positions;

    public float _maxDistance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 Position(int index)
    {
        return _positions[index];
    }
    public void setPosition(int index, Vector2 pos)
    {
        _positions[index] = pos;
    }
    public IKSkeleton(params (float, Constraints)[] options)
    {
        if (options.Length > MaxJointCount)
        {
            throw new Exception($"MaxJointCount is less than provided options ({options.Length}).");
        }

        _options = options;

        foreach (var (length, _) in options)
        {
            _maxDistance += length;
        }
    }

    // http://www.andreasaristidou.com/FABRIK.html 
    public void Update(Vector2 startPosition, Vector2 targetEndPosition)
    {
        _previousPositions = _positions;
        SolveFailed = false;

        var dist = UpdateInner(startPosition, targetEndPosition);
        FinalDistance = MathF.Sqrt(dist);

        var outOfReach = FinalDistance > _maxDistance;
        var tooFarAfterSolve = FinalDistance > 26f;

        var distance = UpdateInner(startPosition, targetEndPosition);

        if (outOfReach || tooFarAfterSolve)
        {
            SolveFailed = true;
            _positions = _previousPositions;
        }

        if (distance > 26f)
        {
            _positions = _previousPositions;

            UpdateInner
            (
                startPosition,
                targetEndPosition + startPosition.DirectionTo(targetEndPosition) * startPosition.Distance(_positions[PositionCount - 1])
            );
        }
    }

    private float UpdateInner(Vector2 startPosition, Vector2 targetEndPosition)
    {
        var lastDistance = float.MaxValue;

        var iterations = 2 << 4;
        var distance = startPosition.DistanceSQ(targetEndPosition);

        if (distance > _maxDistance * _maxDistance)
        {
            iterations = 1;
        }

        for (var k = 0; k < iterations; k += 1)
        {
            _positions[PositionCount - 1] = targetEndPosition;
            _positions[0] = startPosition;

            float rootAngle;

            for (var i = JointCount - 1; i > 0; i -= 1)
            {
                var nextAngle = (_positions[i + 1] - _positions[i]).ToRotation();

                rootAngle = (_positions[i] - (i > 1 ? _positions[i - 1] : startPosition)).ToRotation();

                var angle = rootAngle +
                            Math.Clamp
                            (
                                MathHelper.WrapAngle(nextAngle - rootAngle),
                                _options[i].constraints.MinAngle,
                                _options[i].constraints.MaxAngle
                            );

                _positions[i] = _positions[i + 1] + (angle + MathF.PI).ToRotationVector2() * _options[i].length;
            }

            rootAngle = 0f;

            for (var i = 0; i < JointCount; i += 1)
            {
                var nextAngle = (_positions[i + 1] - _positions[i]).ToRotation();

                var angle = rootAngle +
                            Math.Clamp
                            (
                                MathHelper.WrapAngle(nextAngle - rootAngle),
                                _options[i].constraints.MinAngle,
                                _options[i].constraints.MaxAngle
                            );

                _positions[i + 1] = _positions[i] + angle.ToRotationVector2() * _options[i].length;
                rootAngle = angle;
            }

            distance = _positions[PositionCount - 1].DistanceSQ(targetEndPosition);

            if (distance <= 0.01f)
            {
                break;
            }

            // Check stagnation (solver cannot improve -> impossible pose under constraints)
            if (Math.Abs(lastDistance - distance) < 0.0001f)
            {
                SolveFailed = true;

                break;
            }

            lastDistance = distance;
        }

        return distance;
    }


    /// <summary>
    /// attempts to update the entire skeleton's constraints based on the inputted constraint.
    /// </summary>
    /// <param name="constraint"></param>
    public void SetConstraints((float, Constraints)[] options)
    {
        _options = options;
    }
    public (float, Constraints)[] GetConstraints()
    {
        return _options;

    }
    /// <summary>
    ///     attempts to mutate the values stored inside this limb's _options tuple.
    /// </summary>
    /// <param name="joint">the index of the bone to adjust parameters for</param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public void SetConstraint(int joint, float min, float max)
    {
        var (len, c) = _options[joint];
        c.MinAngle = min;
        c.MaxAngle = max;
        _options[joint] = (len, c); // assign tuple back (struct copy)
    }

    public float GetConstraint(int joint)
    {
        var max = _options[joint].constraints.MaxAngle;
        var min = _options[joint].constraints.MinAngle;

        return MathHelper.ToDegrees(max - min);
    }
   

    public float GetSolvedJointAngle(int joint, Vector2 startPosition)
    {
        float rootAngle = 0f;

        if (joint > 0)
        {
            Vector2 prevDir =
                (joint > 1 ? _positions[joint] - _positions[joint - 1]
                           : _positions[joint] - startPosition);

            rootAngle = prevDir.ToRotation();
        }

        Vector2 dir = _positions[joint + 1] - _positions[joint];
        float absoluteAngle = dir.ToRotation();

        // This is the angle actually clamped by constraints
        return MathHelper.WrapAngle(absoluteAngle - rootAngle);
    }

    public void LockCurrentPose(Vector2 startPosition)
    {
        for (int i = 0; i < JointCount; i++)
        {
            float angle = GetSolvedJointAngle(i, startPosition);
            SetConstraint(i, angle, angle);
        }
    }

}