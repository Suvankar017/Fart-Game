using UnityEngine;

public static class VectorMath
{
    /// <summary>
    /// Projects the specified vector onto a given normalized direction vector.
    /// </summary>
    /// <remarks>If <paramref name="normalizedDirection"/> is not normalized, it will be normalized 
    /// internally before performing the projection. This may introduce a slight performance  overhead if normalization
    /// is required.</remarks>
    /// <param name="vector">The vector to be projected.</param>
    /// <param name="normalizedDirection">The direction vector onto which <paramref name="vector"/> will be projected.  This vector is expected to be
    /// normalized (have a magnitude of 1.0).</param>
    /// <returns>A <see cref="Vector3"/> representing the projection of <paramref name="vector"/>  onto <paramref
    /// name="normalizedDirection"/>.</returns>
    public static Vector3 ExtractDotVector(Vector3 vector, Vector3 normalizedDirection)
    {
        if (!Mathf.Approximately(normalizedDirection.sqrMagnitude, 1.0f))
            normalizedDirection.Normalize();

        return normalizedDirection * Vector3.Dot(vector, normalizedDirection);
    }

    /// <summary>
    /// Removes the component of the vector that is aligned with the specified direction.
    /// </summary>
    /// <remarks>The <paramref name="normalizedDirection"/> parameter must be a unit vector (i.e., its
    /// magnitude must be 1). If it is not normalized, the result may be incorrect.</remarks>
    /// <param name="vector">The input vector from which the aligned component will be removed.</param>
    /// <param name="normalizedDirection">The direction vector, which must be normalized. The method removes the component of <paramref name="vector"/>
    /// that is parallel to this direction.</param>
    /// <returns>A new <see cref="Vector3"/> representing the input vector with the aligned component removed.</returns>
    public static Vector3 RemoveDotVector(Vector3 vector, Vector3 normalizedDirection)
    {
        return vector - ExtractDotVector(vector, normalizedDirection);
    }

    /// <summary>
    /// Projects a vector onto a plane defined by its normal.
    /// </summary>
    /// <remarks>The projection is calculated by removing the component of the input vector that is parallel
    /// to the plane's normal.</remarks>
    /// <param name="vector">The vector to be projected onto the plane.</param>
    /// <param name="planeNormal">The normal vector of the plane. This vector must be non-zero and will be normalized internally.</param>
    /// <returns>A <see cref="Vector3"/> representing the projection of the input vector onto the plane.</returns>
    public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
    {
        return RemoveDotVector(vector, planeNormal.normalized);
    }

    /// <summary>
    /// Projects a vector onto a plane defined by a normal vector and a point on the plane.
    /// </summary>
    /// <param name="vector">The vector to be projected onto the plane.</param>
    /// <param name="planeNormal">The normal vector of the plane. Must be non-zero.</param>
    /// <param name="planePoint">A point on the plane used to define its position in space.</param>
    /// <returns>The projection of the <paramref name="vector"/> onto the plane defined by <paramref name="planeNormal"/> and
    /// <paramref name="planePoint"/>.</returns>
    public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal, Vector3 planePoint)
    {
        return ProjectOnPlane(vector - planePoint, planeNormal) + planePoint;
    }

    /// <summary>
    /// Gradually moves a vector towards a target vector by a specified speed, scaled by the elapsed time.
    /// </summary>
    /// <remarks>This method ensures that the movement does not overshoot the target vector. The movement
    /// speed is scaled by the elapsed time to ensure frame-rate independence.</remarks>
    /// <param name="current">The current vector position.</param>
    /// <param name="target">The target vector position to move towards.</param>
    /// <param name="incrementSpeed">The speed at which the vector moves towards the target, in units per second.</param>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <returns>The updated vector position after moving towards the target. If the target is within the maximum movement
    /// distance, the target vector is returned.</returns>
    public static Vector3 IncrementVectorTowardsTargetVector(Vector3 current, Vector3 target, float incrementSpeed, float deltaTime)
    {
        float maxDelta = incrementSpeed * deltaTime;
        if (Vector3.SqrMagnitude(current - target) <= (maxDelta * maxDelta))
            return target;

        return Vector3.MoveTowards(current, target, maxDelta);
    }

    public static Vector2 ProjectOnPlane(Vector2 vector, Vector2 planeNormal)
    {
        if (!Mathf.Approximately(planeNormal.sqrMagnitude, 1.0f))
            planeNormal.Normalize();

        return vector - planeNormal * Vector2.Dot(vector, planeNormal);
    }
}
